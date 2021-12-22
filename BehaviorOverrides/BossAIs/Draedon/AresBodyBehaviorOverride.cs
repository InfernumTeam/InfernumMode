using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using DraedonNPC = CalamityMod.NPCs.ExoMechs.Draedon;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
    public class AresBodyBehaviorOverride : NPCBehaviorOverride
    {
        public enum AresBodyFrameType
        {
            Normal,
            Laugh
        }

        public enum AresBodyAttackType
        {
            IdleHover,
            LaserSpinBursts,
            DirectionChangingSpinBursts,
            RadianceLaserBursts
        }

        public override int NPCOverrideType => ModContent.NPCType<AresBody>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame | NPCOverrideContext.NPCPreDraw;

        public const float Phase1ArmChargeupTime = 150f;

        #region AI
        public override bool PreAI(NPC npc)
        {
            // Define the life ratio.
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Define the whoAmI variable.
            CalamityGlobalNPC.draedonExoMechPrime = npc.whoAmI;

            // Reset frame states.
            ref float frameType = ref npc.localAI[0];
            frameType = (int)AresBodyFrameType.Normal;

            // Define attack variables.
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float armsHaveBeenSummoned = ref npc.ai[3];
            ref float armCycleCounter = ref npc.Infernum().ExtraAI[5];
            ref float armCycleTimer = ref npc.Infernum().ExtraAI[6];
            ref float projectileDamageBoost = ref npc.Infernum().ExtraAI[8];
            ref float hasSummonedComplementMech = ref npc.Infernum().ExtraAI[ExoMechManagement.HasSummonedComplementMechIndex];
            ref float complementMechIndex = ref npc.Infernum().ExtraAI[ExoMechManagement.ComplementMechIndexIndex];
            ref float wasNotInitialSummon = ref npc.Infernum().ExtraAI[ExoMechManagement.WasNotInitialSummonIndex];
            ref float finalMechIndex = ref npc.Infernum().ExtraAI[ExoMechManagement.FinalMechIndexIndex];
            ref float enraged = ref npc.Infernum().ExtraAI[13];
            ref float backarmSwapTimer = ref npc.Infernum().ExtraAI[14];
            ref float laserGaussArmAreSwapped = ref npc.Infernum().ExtraAI[15];
            NPC initialMech = ExoMechManagement.FindInitialMech();
            NPC complementMech = complementMechIndex >= 0 && Main.npc[(int)complementMechIndex].active ? Main.npc[(int)complementMechIndex] : null;
            NPC finalMech = ExoMechManagement.FindFinalMech();

            // Go through the attack cycle.
            if (armCycleTimer >= 600f)
            {
                armCycleCounter += enraged == 1f ? 6f : 1f;
                armCycleTimer = 0f;
            }
            else
                armCycleTimer++;

            if (initialMech != null && initialMech.type == ModContent.NPCType<Apollo>() && initialMech.Infernum().ExtraAI[ApolloBehaviorOverride.ComplementMechEnrageTimerIndex] > 0f)
                enraged = 1f;

            // Mark the laser and gauss arms swap sometimes.
            if (backarmSwapTimer > 960f)
            {
                backarmSwapTimer = 0f;
                laserGaussArmAreSwapped = laserGaussArmAreSwapped == 0f ? 1f : 0f;
                npc.netUpdate = true;
            }
            backarmSwapTimer++;

            if (Main.netMode != NetmodeID.MultiplayerClient && armsHaveBeenSummoned == 0f)
            {
                int totalArms = 4;
                for (int i = 0; i < totalArms; i++)
                {
                    int lol = 0;
                    switch (i)
                    {
                        case 0:
                            lol = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<AresLaserCannon>(), npc.whoAmI);
                            break;
                        case 1:
                            lol = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<AresPlasmaFlamethrower>(), npc.whoAmI);
                            break;
                        case 2:
                            lol = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<AresTeslaCannon>(), npc.whoAmI);
                            break;
                        case 3:
                            lol = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<AresGaussNuke>(), npc.whoAmI);
                            break;
                        default:
                            break;
                    }

                    Main.npc[lol].realLife = npc.whoAmI;
                    Main.npc[lol].netUpdate = true;
                }
                complementMechIndex = -1f;
                finalMechIndex = -1f;
                armsHaveBeenSummoned = 1f;
                npc.netUpdate = true;
            }

            // Summon the complement mech and reset things once ready.
            if (hasSummonedComplementMech == 0f && lifeRatio < ExoMechManagement.Phase4LifeRatio)
            {
                if (attackState != (int)AresBodyAttackType.IdleHover)
                {
                    // Destroy all lasers and telegraphs.
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        if ((Main.projectile[i].type == ModContent.ProjectileType<AresDeathBeamTelegraph>() || Main.projectile[i].type == ModContent.ProjectileType<AresSpinningDeathBeam>()) && Main.projectile[i].active)
                            Main.projectile[i].Kill();
                    }
                }

                ExoMechManagement.SummonComplementMech(npc);
                hasSummonedComplementMech = 1f;
                attackTimer = 0f;
                SelectNextAttack(npc);
                npc.netUpdate = true;
            }

            // Summon the final mech once ready.
            if (wasNotInitialSummon == 0f && finalMechIndex == -1f && complementMech != null && complementMech.life / (float)complementMech?.lifeMax < ExoMechManagement.ComplementMechInvincibilityThreshold)
            {
                ExoMechManagement.SummonFinalMech(npc);
                npc.netUpdate = true;
            }

            // Become invincible if the complement mech is at high enough health.
            npc.dontTakeDamage = false;
            if (complementMechIndex >= 0 && Main.npc[(int)complementMechIndex].active && Main.npc[(int)complementMechIndex].life > Main.npc[(int)complementMechIndex].lifeMax * ExoMechManagement.ComplementMechInvincibilityThreshold)
                npc.dontTakeDamage = true;

            // Get a target.
            npc.TargetClosest(false);
            Player target = Main.player[npc.target];

            // Become invincible and disappear if the final mech is present.
            npc.Calamity().newAI[1] = 0f;
            if (finalMech != null && finalMech != npc)
            {
                npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.08f, 0f, 1f);
                if (npc.Opacity <= 0f)
                    npc.Center = target.Center - Vector2.UnitY * 3200f;

                attackTimer = 0f;
                attackState = (int)AresBodyAttackType.IdleHover;
                npc.Calamity().newAI[1] = (int)AresBody.SecondaryPhase.PassiveAndImmune;
                npc.Calamity().ShouldCloseHPBar = true;
                npc.dontTakeDamage = true;
            }
            else
                npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.08f, 0f, 1f);

            // Reset things.
            projectileDamageBoost = ExoMechManagement.CurrentAresPhase >= 4 ? 50f : 0f;

            // Despawn if the target is gone.
            if (!target.active || target.dead)
            {
                npc.TargetClosest(false);
                target = Main.player[npc.target];
                if (!target.active || target.dead)
                    npc.active = false;
            }

            // Use combo attacks as necessary.
            if (initialMech != null && initialMech.ai[0] >= 100f && (int)attackState < 100)
            {
                attackTimer = 0f;
                attackState = ExoMechManagement.FindInitialMech().ai[0];
                npc.netUpdate = true;
            }

            if (finalMech != null && finalMech.Opacity > 0f && attackState >= 100f)
            {
                attackTimer = 0f;
                attackState = 0f;
                npc.netUpdate = true;
            }

            // Perform specific behaviors.
            switch ((AresBodyAttackType)(int)attackState)
            {
                case AresBodyAttackType.IdleHover:
                    DoBehavior_IdleHover(npc, target, ref attackTimer);
                    break;
                case AresBodyAttackType.RadianceLaserBursts:
                    DoBehavior_RadianceLaserBursts(npc, target, ref attackTimer, ref frameType);
                    break;
                case AresBodyAttackType.LaserSpinBursts:
                case AresBodyAttackType.DirectionChangingSpinBursts:
                    DoBehavior_LaserSpinBursts(npc, target, ref enraged, ref attackTimer, ref frameType);
                    break;
            }

            switch ((ExoMechComboAttackContent.ExoMechComboAttackType)attackState)
            {
                case ExoMechComboAttackContent.ExoMechComboAttackType.AresTwins_PressureLaser:
                    if (ExoMechComboAttackContent.DoBehavior_AresTwins_PressureLaser(npc, target, 1f, ref attackTimer, ref frameType))
                        SelectNextAttack(npc);
                    break;
                case ExoMechComboAttackContent.ExoMechComboAttackType.AresTwins_DualLaserCharges:
                    if (ExoMechComboAttackContent.DoBehavior_AresTwins_DualLaserCharges(npc, target, 1f, ref attackTimer, ref frameType))
                        SelectNextAttack(npc);
                    break;
                case ExoMechComboAttackContent.ExoMechComboAttackType.AresTwins_CircleAttack:
                    if (ExoMechComboAttackContent.DoBehavior_AresTwins_CircleAttack(npc, target, 1f, ref attackTimer, ref frameType))
                        SelectNextAttack(npc);
                    break;
                case ExoMechComboAttackContent.ExoMechComboAttackType.AresTwins_ElectromagneticPlasmaStar:
                    if (ExoMechComboAttackContent.DoBehavior_AresTwins_ElectromagneticPlasmaStar(npc, target, 1f, ref attackTimer, ref frameType))
                        SelectNextAttack(npc);
                    break;
            }

            if (ExoMechComboAttackContent.UseThanatosAresComboAttack(npc, ref attackTimer, ref frameType))
                SelectNextAttack(npc);

            attackTimer++;
            return false;
        }

        public static void DoBehavior_IdleHover(NPC npc, Player target, ref float attackTimer)
        {
            int attackTime = ExoMechManagement.CurrentAresPhase >= 5 ? 1200 : 1500;
            Vector2 hoverDestination = target.Center - Vector2.UnitY * 450f;
            DoHoverMovement(npc, hoverDestination, 24f, 75f);

            if (attackTimer > attackTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_RadianceLaserBursts(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            int totalBursts = 9;
            int shootTime = 450;
            int shootDelay = 125;
            int telegraphTime = 35;
            int laserLifetime = shootTime / totalBursts - telegraphTime;
            int totalLasers = 27;
            int totalSparks = 25 + (int)(npc.Distance(target.Center) * 0.02f);

            if (ExoMechManagement.CurrentAresPhase <= 4)
            {
                totalBursts -= 2;
                telegraphTime += 10;
                shootDelay += 25;
                shootTime += 100;
                totalLasers -= 11;
                totalSparks -= 8;
            }

            float wrappedAttackTimer = (attackTimer - shootDelay) % (shootTime / totalBursts);

            ref float generalAngularOffset = ref npc.Infernum().ExtraAI[0];

            // Slow down.
            npc.velocity *= 0.935f;

            // Enforce an initial delay prior to firing.
            if (attackTimer < shootDelay)
                return;

            // Laugh.
            frameType = (int)AresBodyFrameType.Laugh;

            // Create telegraphs.
            if (Main.netMode != NetmodeID.MultiplayerClient && wrappedAttackTimer == 0f)
            {
                generalAngularOffset = Main.rand.NextFloat(MathHelper.TwoPi);
                for (int i = 0; i < totalLasers; i++)
                {
                    Vector2 laserDirection = (MathHelper.TwoPi * i / totalLasers + generalAngularOffset).ToRotationVector2();
                    int telegraph = Utilities.NewProjectileBetter(npc.Center, laserDirection, ModContent.ProjectileType<AresDeathBeamTelegraph>(), 0, 0f);
                    if (Main.projectile.IndexInRange(telegraph))
                    {
                        Main.projectile[telegraph].ai[1] = npc.whoAmI;
                        Main.projectile[telegraph].localAI[0] = telegraphTime;
                        Main.projectile[telegraph].netUpdate = true;
                    }
                }
                npc.netUpdate = true;
            }

            // Create laser bursts and tesla sparks.
            if (wrappedAttackTimer == telegraphTime - 1f)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/TeslaCannonFire"), target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < totalLasers; i++)
                    {
                        Vector2 laserDirection = (MathHelper.TwoPi * i / totalLasers + generalAngularOffset).ToRotationVector2();
                        int deathray = Utilities.NewProjectileBetter(npc.Center, laserDirection, ModContent.ProjectileType<AresDeathBeam>(), 900, 0f);
                        if (Main.projectile.IndexInRange(deathray))
                        {
                            Main.projectile[deathray].ai[1] = npc.whoAmI;
                            Main.projectile[deathray].ModProjectile<AresDeathBeam>().LifetimeThing = laserLifetime;
                            Main.projectile[deathray].netUpdate = true;
                        }
                    }
                    for (int i = 0; i < totalSparks; i++)
                    {
                        float sparkShootSpeed = npc.Distance(target.Center) * 0.02f + 20f;
                        Vector2 sparkVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.TwoPi * i / totalSparks) * sparkShootSpeed;
                        Utilities.NewProjectileBetter(npc.Center, sparkVelocity, ModContent.ProjectileType<TeslaSpark>(), 600, 0f);
                    }
                    npc.netUpdate = true;
                }
            }

            if (attackTimer >= shootTime + shootDelay - 1f)
            {
                // Destroy all lasers and telegraphs.
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if ((Main.projectile[i].type == ModContent.ProjectileType<AresDeathBeamTelegraph>() || Main.projectile[i].type == ModContent.ProjectileType<AresDeathBeam>()) && Main.projectile[i].active)
                        Main.projectile[i].Kill();
                }
                SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_LaserSpinBursts(NPC npc, Player target, ref float enraged, ref float attackTimer, ref float frameType)
        {
            int shootDelay = 90;
            int telegraphTime = 60;
            int spinTime = 600;
            int repositionTime = 50;
            int totalLasers = 13;
            int burstReleaseRate = 50;

            if (ExoMechManagement.CurrentAresPhase >= 5)
                burstReleaseRate -= 8;
            if (ExoMechManagement.CurrentAresPhase >= 6)
                burstReleaseRate -= 8;

            ref float generalAngularOffset = ref npc.Infernum().ExtraAI[0];
            ref float laserDirectionSign = ref npc.Infernum().ExtraAI[1];

            // Slow down.
            npc.velocity *= 0.935f;

            // Determine an initial direction.
            if (laserDirectionSign == 0f)
            {
                laserDirectionSign = Main.rand.NextBool().ToDirectionInt();
                npc.netUpdate = true;
            }

            // Enforce an initial delay prior to firing.
            if (attackTimer < shootDelay)
                return;

            // Delete projectiles after the delay has concluded.
            if (attackTimer == shootDelay + 1f)
            {
                List<int> projectilesToDelete = new List<int>()
                {
                    ModContent.ProjectileType<AresPlasmaFireball>(),
                    ModContent.ProjectileType<AresPlasmaFireball2>(),
                    ModContent.ProjectileType<PlasmaGas>(),
                    ModContent.ProjectileType<AresTeslaOrb>(),
                    ModContent.ProjectileType<ElectricGas>(),
                    ModContent.ProjectileType<AresGaussNukeProjectile>(),
                    ModContent.ProjectileType<AresGaussNukeProjectileBoom>(),
                    ModContent.ProjectileType<CannonLaser>(),
                };

                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].active && projectilesToDelete.Contains(Main.projectile[i].type))
                        Main.projectile[i].active = false;
                }
            }

            // Laugh.
            frameType = (int)AresBodyFrameType.Laugh;

            // Create telegraphs.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == shootDelay + 1f)
            {
                generalAngularOffset = Main.rand.NextFloat(MathHelper.TwoPi);
                for (int i = 0; i < totalLasers; i++)
                {
                    Vector2 laserDirection = (MathHelper.TwoPi * i / totalLasers).ToRotationVector2();
                    int telegraph = Utilities.NewProjectileBetter(npc.Center, laserDirection, ModContent.ProjectileType<AresDeathBeamTelegraph>(), 0, 0f);
                    if (Main.projectile.IndexInRange(telegraph))
                    {
                        Main.projectile[telegraph].ai[1] = npc.whoAmI;
                        Main.projectile[telegraph].localAI[0] = telegraphTime;
                        Main.projectile[telegraph].netUpdate = true;
                    }
                }
                npc.netUpdate = true;
            }

            // Create laser bursts and tesla sparks.
            if (attackTimer == shootDelay + telegraphTime - 1f)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/TeslaCannonFire"), target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    generalAngularOffset = 0f;
                    for (int i = 0; i < totalLasers; i++)
                    {
                        Vector2 laserDirection = (MathHelper.TwoPi * i / totalLasers).ToRotationVector2();
                        int deathray = Utilities.NewProjectileBetter(npc.Center, laserDirection, ModContent.ProjectileType<AresSpinningDeathBeam>(), 900, 0f);
                        if (Main.projectile.IndexInRange(deathray))
                        {
                            Main.projectile[deathray].ai[1] = npc.whoAmI;
                            Main.projectile[deathray].ModProjectile<AresSpinningDeathBeam>().LifetimeThing = spinTime;
                            Main.projectile[deathray].netUpdate = true;
                        }
                    }
                    npc.netUpdate = true;
                }
            }

            // Idly create explosions around the target.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % burstReleaseRate == burstReleaseRate - 1f && attackTimer > shootDelay + telegraphTime + 60f)
            {
                Vector2 targetDirection = target.velocity.SafeNormalize(Main.rand.NextVector2Unit());
                Vector2 spawnPosition = target.Center - targetDirection.RotatedByRandom(1.1f) * Main.rand.NextFloat(325f, 650f) * new Vector2(1f, 0.6f);
                Utilities.NewProjectileBetter(spawnPosition, Vector2.Zero, ModContent.ProjectileType<AresBeamExplosion>(), 600, 0f);
            }

            // Make the laser spin.
            float adjustedTimer = attackTimer - (shootDelay + telegraphTime);
            float spinSpeed = Utils.InverseLerp(0f, 420f, adjustedTimer, true) * MathHelper.Pi / 145f;
            if (npc.ai[0] == (int)AresBodyAttackType.DirectionChangingSpinBursts)
            {
                if (adjustedTimer < spinTime * 0.5f)
                    spinSpeed *= Utils.InverseLerp(spinTime * 0.5f, spinTime * 0.5f - 45f, adjustedTimer, true);
                else
                    spinSpeed *= -Utils.InverseLerp(spinTime * 0.5f, spinTime * 0.5f + 45f, adjustedTimer, true);
                spinSpeed *= 0.7f;
            }

            generalAngularOffset += spinSpeed * laserDirectionSign;

            // Get pissed off if the player attempts to leave the laser circle.
            if (!npc.WithinRange(target.Center, AresDeathBeamTelegraph.TelegraphWidth + 135f) && enraged == 0f)
            {
                if (Main.player[Main.myPlayer].active && !Main.player[Main.myPlayer].dead)
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/AresEnraged"), target.Center);

                // Have Draedon comment on the player's attempts to escape.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.DraedonAresEnrageText", DraedonNPC.TextColorEdgy);

                enraged = 1f;
                npc.netUpdate = true;
            }

            if (attackTimer >= shootDelay + telegraphTime + spinTime + repositionTime)
            {
                // Destroy all lasers and telegraphs.
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if ((Main.projectile[i].type == ModContent.ProjectileType<AresDeathBeamTelegraph>() || Main.projectile[i].type == ModContent.ProjectileType<AresSpinningDeathBeam>()) && Main.projectile[i].active)
                        Main.projectile[i].Kill();
                }
                SelectNextAttack(npc);
            }
        }

        public static void SelectNextAttack(NPC npc)
        {
            AresBodyAttackType oldAttackType = (AresBodyAttackType)(int)npc.ai[0];
            npc.ai[0] = (int)AresBodyAttackType.IdleHover;
            if (oldAttackType == AresBodyAttackType.IdleHover && ExoMechManagement.CurrentAresPhase >= 3)
            {
                bool complementMechIsPresent = ExoMechManagement.ComplementMechIsPresent(npc);
                NPC finalMech = ExoMechManagement.FindFinalMech();
                if (finalMech == npc)
                    finalMech = null;

                // Use a laser spin if alone. Otherwise, use the radiance burst attack.
                if ((!complementMechIsPresent && finalMech is null) || ExoMechManagement.CurrentAresPhase == 5)
                    npc.ai[0] = Main.player[npc.target].Infernum().AresSpecialAttackTypeSelector.MakeSelection() + 1f;
                else
                    npc.ai[0] = (int)AresBodyAttackType.RadianceLaserBursts;
            }

            if (ExoMechComboAttackContent.ShouldSelectComboAttack(npc, out ExoMechComboAttackContent.ExoMechComboAttackType newAttack))
                npc.ai[0] = (int)newAttack;

            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            // Stop being enraged after an idle hover.
            if (oldAttackType == AresBodyAttackType.IdleHover || (int)oldAttackType >= 100f)
                npc.Infernum().ExtraAI[13] = 0f;

            npc.netUpdate = true;
        }

        public static bool ArmIsDisabled(NPC npc)
        {
            if (CalamityGlobalNPC.draedonExoMechPrime == -1)
                return false;

            NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];

            if (aresBody.ai[0] == (int)AresBodyAttackType.RadianceLaserBursts ||
                aresBody.ai[0] == (int)AresBodyAttackType.LaserSpinBursts ||
                aresBody.ai[0] == (int)AresBodyAttackType.DirectionChangingSpinBursts ||
                aresBody.ai[0] == (int)ExoMechComboAttackContent.ExoMechComboAttackType.AresTwins_PressureLaser ||
                aresBody.ai[0] == (int)ExoMechComboAttackContent.ExoMechComboAttackType.AresTwins_DualLaserCharges)
            {
                return true;
            }

            // Only the tesla and plasma arms may fire during this attack, and only after the delay has concluded (which is present in the form of a binary switch in ExtraAI[0]).
            if (aresBody.ai[0] == (int)ExoMechComboAttackContent.ExoMechComboAttackType.AresTwins_CircleAttack)
            {
                if (npc.type == ModContent.NPCType<AresTeslaCannon>() || npc.type == ModContent.NPCType<AresPlasmaFlamethrower>())
                    return aresBody.Infernum().ExtraAI[0] == 0f;

                return true;
            }

            if (aresBody.ai[0] == (int)ExoMechComboAttackContent.ExoMechComboAttackType.AresTwins_ElectromagneticPlasmaStar)
            {
                if (npc.type == ModContent.NPCType<AresLaserCannon>() || npc.type == ModContent.NPCType<AresGaussNuke>())
                    return aresBody.ai[1] < ElectromagneticStar.ChargeupTime + 60f;
                return true;
            }

            // If Ares is specifically using a combo attack that specifies certain arms should be active, go based on which ones should be active.
            if (ExoMechComboAttackContent.AffectedAresArms.TryGetValue((ExoMechComboAttackContent.ExoMechComboAttackType)aresBody.ai[0], out int[] activeArms))
                return activeArms.Contains(npc.type);

            if (aresBody.Opacity <= 0f)
                return true;

            if (ExoMechManagement.CurrentAresPhase <= 2)
                return false;

            // The gauss and laser arm are disabled for 2.5 seconds once they swap.
            if (aresBody.Infernum().ExtraAI[14] < 150f)
                return false;

            // Rotate arm usability as follows (This only applies before phase 5):
            // Gauss Nuke and Laser Cannon,
            // Laser Cannon and Tesla Cannon,
            // Tesla Cannon and Plasma Flamethrower
            // Plasma Flamethrower and Gauss Nuke.

            // If during or after phase 5, rotate arm usability like this instead:
            // Gauss Nuke, Laser Cannon, and Tesla Cannon,
            // Laser Cannon, Tesla Cannon, and Plasma Flamethrower,
            // Tesla Cannon, Plasma Flamethrower, and Gauss Nuke

            if (ExoMechManagement.CurrentAresPhase < 5)
            {
                switch ((int)aresBody.Infernum().ExtraAI[5] % 4)
                {
                    case 0:
                        return npc.type != ModContent.NPCType<AresGaussNuke>() && npc.type != ModContent.NPCType<AresLaserCannon>();
                    case 1:
                        return npc.type != ModContent.NPCType<AresLaserCannon>() && npc.type != ModContent.NPCType<AresTeslaCannon>();
                    case 2:
                        return npc.type != ModContent.NPCType<AresTeslaCannon>() && npc.type != ModContent.NPCType<AresPlasmaFlamethrower>();
                    case 3:
                        return npc.type != ModContent.NPCType<AresPlasmaFlamethrower>() && npc.type != ModContent.NPCType<AresGaussNuke>();
                }
            }
            else
            {
                switch ((int)aresBody.Infernum().ExtraAI[5] % 3)
                {
                    case 0:
                        return npc.type != ModContent.NPCType<AresGaussNuke>() && npc.type != ModContent.NPCType<AresLaserCannon>() && npc.type != ModContent.NPCType<AresTeslaCannon>();
                    case 1:
                        return npc.type != ModContent.NPCType<AresLaserCannon>() && npc.type != ModContent.NPCType<AresTeslaCannon>() && npc.type != ModContent.NPCType<AresPlasmaFlamethrower>();
                    case 2:
                        return npc.type != ModContent.NPCType<AresTeslaCannon>() && npc.type != ModContent.NPCType<AresPlasmaFlamethrower>() && npc.type != ModContent.NPCType<AresGaussNuke>();
                }
            }
            return false;
        }

        public static void DoHoverMovement(NPC npc, Vector2 destination, float flySpeed, float hyperSpeedCap)
        {
            float distanceFromDestination = npc.Distance(destination);
            float hyperSpeedInterpolant = Utils.InverseLerp(50f, 2400f, distanceFromDestination, true);

            // Scale up velocity over time if too far from destination.
            float speedUpFactor = Utils.InverseLerp(50f, 1600f, npc.Distance(destination), true) * 1.76f;
            flySpeed *= 1f + speedUpFactor;

            // Reduce speed when very close to the destination, to prevent swerving movement.
            if (flySpeed > distanceFromDestination)
                flySpeed = distanceFromDestination;

            // Define the max velocity.
            Vector2 maxVelocity = (destination - npc.Center) / 24f;
            if (maxVelocity.Length() > hyperSpeedCap)
                maxVelocity = maxVelocity.SafeNormalize(Vector2.Zero) * hyperSpeedCap;

            npc.velocity = Vector2.Lerp(npc.SafeDirectionTo(destination) * flySpeed, maxVelocity, hyperSpeedInterpolant);
        }
        #endregion AI

        #region Frames and Drawcode
        public override void FindFrame(NPC npc, int frameHeight)
        {
            int framesInNormalState = 11;
            ref float currentFrame = ref npc.localAI[2];

            npc.frameCounter++;
            switch ((AresBodyFrameType)(int)npc.localAI[0])
            {
                case AresBodyFrameType.Normal:
                    if (npc.frameCounter >= 6D)
                    {
                        // Reset the frame counter.
                        npc.frameCounter = 0D;

                        // Increment the frame.
                        currentFrame++;

                        // Reset the frames to frame 0 after the animation cycle for the normal phase has concluded.
                        if (currentFrame > framesInNormalState)
                            currentFrame = 0;
                    }
                    break;
                case AresBodyFrameType.Laugh:
                    if (currentFrame <= 35 || currentFrame >= 47)
                        currentFrame = 36f;

                    if (npc.frameCounter >= 6D)
                    {
                        // Reset the frame counter.
                        npc.frameCounter = 0D;

                        // Increment the frame.
                        currentFrame++;
                    }
                    break;
            }

            npc.frame = new Rectangle(npc.width * (int)(currentFrame / 8), npc.height * (int)(currentFrame % 8), npc.width, npc.height);
        }

        public static MethodInfo DrawArmFunction = typeof(AresBody).GetMethod("DrawArm", BindingFlags.Public | BindingFlags.Instance);

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            // Draw arms.
            int laserArm = NPC.FindFirstNPC(ModContent.NPCType<AresLaserCannon>());
            int gaussArm = NPC.FindFirstNPC(ModContent.NPCType<AresGaussNuke>());
            int teslaArm = NPC.FindFirstNPC(ModContent.NPCType<AresTeslaCannon>());
            int plasmaArm = NPC.FindFirstNPC(ModContent.NPCType<AresPlasmaFlamethrower>());
            Color afterimageBaseColor = Color.White;

            // Become red if enraged.
            if (npc.Infernum().ExtraAI[13] == 1f)
                afterimageBaseColor = Color.Red;

            Color armGlowmaskColor = afterimageBaseColor;
            armGlowmaskColor.A = 184;

            (int, bool)[] armProperties = new (int, bool)[]
            {
                // Laser arm.
                (-1, true),

                // Gauss arm.
                (1, true),

                // Telsa arm.
                (-1, false),

                // Plasma arm.
                (1, false),
            };

            // Swap arms as necessary
            if (npc.Infernum().ExtraAI[15] == 1f)
            {
                armProperties[0] = (1, true);
                armProperties[1] = (-1, true);
            }

            if (laserArm != -1)
                DrawArmFunction.Invoke(npc.modNPC, new object[] { spriteBatch, Main.npc[laserArm].Center, armGlowmaskColor, armProperties[0].Item1, armProperties[0].Item2 });
            if (gaussArm != -1)
                DrawArmFunction.Invoke(npc.modNPC, new object[] { spriteBatch, Main.npc[gaussArm].Center, armGlowmaskColor, armProperties[1].Item1, armProperties[1].Item2 });
            if (teslaArm != -1)
                DrawArmFunction.Invoke(npc.modNPC, new object[] { spriteBatch, Main.npc[teslaArm].Center, armGlowmaskColor, armProperties[2].Item1, armProperties[2].Item2 });
            if (plasmaArm != -1)
                DrawArmFunction.Invoke(npc.modNPC, new object[] { spriteBatch, Main.npc[plasmaArm].Center, armGlowmaskColor, armProperties[3].Item1, armProperties[3].Item2 });

            Texture2D texture = Main.npcTexture[npc.type];
            Rectangle frame = npc.frame;
            Vector2 origin = frame.Size() * 0.5f;
            int numAfterimages = 5;

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < numAfterimages; i += 2)
                {
                    Color afterimageColor = lightColor;
                    afterimageColor = Color.Lerp(afterimageColor, afterimageBaseColor, 0.5f);
                    afterimageColor = npc.GetAlpha(afterimageColor);
                    afterimageColor *= (numAfterimages - i) / 15f;
                    Vector2 afterimageCenter = npc.oldPos[i] + npc.frame.Size() * 0.5f - Main.screenPosition;
                    spriteBatch.Draw(texture, afterimageCenter, npc.frame, afterimageColor, npc.oldRot[i], origin, npc.scale, SpriteEffects.None, 0f);
                }
            }

            Vector2 center = npc.Center - Main.screenPosition;
            spriteBatch.Draw(texture, center, frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);

            texture = ModContent.GetTexture("CalamityMod/NPCs/ExoMechs/Ares/AresBodyGlow");

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < numAfterimages; i += 2)
                {
                    Color afterimageColor = npc.GetAlpha(Color.Lerp(lightColor, afterimageBaseColor, 0.5f)) * ((numAfterimages - i) / 15f);
                    Vector2 afterimageCenter = npc.oldPos[i] + npc.frame.Size() * 0.5f - Main.screenPosition;
                    spriteBatch.Draw(texture, afterimageCenter, npc.frame, afterimageColor, npc.oldRot[i], origin, npc.scale, SpriteEffects.None, 0f);
                }
            }

            spriteBatch.Draw(texture, center, frame, afterimageBaseColor * npc.Opacity, npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);

            return false;
        }
        #endregion Frames and Drawcode
    }
}
