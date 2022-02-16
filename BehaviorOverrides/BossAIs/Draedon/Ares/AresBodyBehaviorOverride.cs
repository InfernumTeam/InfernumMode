using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Skies;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.ArtemisAndApollo;
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

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares
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
            HoverCharge,
            LaserSpinBursts,
            DirectionChangingSpinBursts,
            RadianceLaserBursts
        }

        public override int NPCOverrideType => ModContent.NPCType<AresBody>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame | NPCOverrideContext.NPCPreDraw;

        public const float Phase1ArmChargeupTime = 240f;
        public const int ProjectileDamageBoostIndex = 8;
        public static int ProjectileDamageBoost
        {
            get
            {
                if (!Main.npc.IndexInRange(CalamityGlobalNPC.draedonExoMechPrime))
                    return 0;

                return (int)Main.npc[CalamityGlobalNPC.draedonExoMechPrime].Infernum().ExtraAI[ProjectileDamageBoostIndex];
            }
            set
            {
                if (!Main.npc.IndexInRange(CalamityGlobalNPC.draedonExoMechPrime))
                    return;

                Main.npc[CalamityGlobalNPC.draedonExoMechPrime].Infernum().ExtraAI[ProjectileDamageBoostIndex] = value;
            }
        }

        #region AI
        public override bool PreAI(NPC npc)
        {
            // Define the life ratio.
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Define the whoAmI variable.
            CalamityGlobalNPC.draedonExoMechPrime = npc.whoAmI;

            // Reset damage.
            npc.damage = 0;
            npc.Calamity().canBreakPlayerDefense = true;

            // Reset frame states.
            ref float frameType = ref npc.localAI[0];
            frameType = (int)AresBodyFrameType.Normal;

            // Define attack variables.
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float armsHaveBeenSummoned = ref npc.ai[3];
            ref float armCycleCounter = ref npc.Infernum().ExtraAI[5];
            ref float armCycleTimer = ref npc.Infernum().ExtraAI[6];
            ref float hasSummonedComplementMech = ref npc.Infernum().ExtraAI[ExoMechManagement.HasSummonedComplementMechIndex];
            ref float complementMechIndex = ref npc.Infernum().ExtraAI[ExoMechManagement.ComplementMechIndexIndex];
            ref float wasNotInitialSummon = ref npc.Infernum().ExtraAI[ExoMechManagement.WasNotInitialSummonIndex];
            ref float finalMechIndex = ref npc.Infernum().ExtraAI[ExoMechManagement.FinalMechIndexIndex];
            ref float enraged = ref npc.Infernum().ExtraAI[13];
            ref float backarmSwapTimer = ref npc.Infernum().ExtraAI[14];
            ref float laserPulseArmAreSwapped = ref npc.Infernum().ExtraAI[15];
            ref float finalPhaseAnimationTime = ref npc.Infernum().ExtraAI[ExoMechManagement.FinalPhaseTimerIndex];
            NPC initialMech = ExoMechManagement.FindInitialMech();
            NPC complementMech = complementMechIndex >= 0 && Main.npc[(int)complementMechIndex].active ? Main.npc[(int)complementMechIndex] : null;
            NPC finalMech = ExoMechManagement.FindFinalMech();

            // Make the laser and pulse arms swap sometimes.
            if (backarmSwapTimer > 960f)
            {
                backarmSwapTimer = 0f;
                laserPulseArmAreSwapped = laserPulseArmAreSwapped == 0f ? 1f : 0f;
                npc.netUpdate = true;
            }
            backarmSwapTimer++;

            // Become more resistant to damage as necessary.
            npc.takenDamageMultiplier = 1f;
            if (ExoMechManagement.ShouldHaveSecondComboPhaseResistance(npc))
                npc.takenDamageMultiplier *= 0.5f;

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
                            lol = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<AresPulseCannon>(), npc.whoAmI);
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
            ProjectileDamageBoost = ExoMechManagement.CurrentAresPhase >= 4 ? 50 : 0;

            // Despawn if the target is gone.
            if (!target.active || target.dead)
            {
                npc.TargetClosest(false);
                target = Main.player[npc.target];
                if (!target.active || target.dead)
                    npc.active = false;
            }

            // Handle the final phase transition.
            if (finalPhaseAnimationTime < ExoMechManagement.FinalPhaseTransitionTime && ExoMechManagement.CurrentAresPhase >= 6)
            {
                attackState = (int)AresBodyAttackType.IdleHover;
                finalPhaseAnimationTime++;
                npc.dontTakeDamage = true;
                DoBehavior_DoFinalPhaseTransition(npc, target, ref frameType, finalPhaseAnimationTime);
                return false;
            }

            // Use combo attacks as necessary.
            if (ExoMechManagement.TotalMechs >= 2 && (int)attackState < 100)
            {
                attackTimer = 0f;

                if (initialMech.whoAmI == npc.whoAmI)
                    SelectNextAttack(npc);

                attackState = initialMech.ai[0];
                npc.netUpdate = true;
            }

            // Reset the attack type if it was a combo attack but the respective mech is no longer present.
            if (((finalMech != null && finalMech.Opacity > 0f) || ExoMechManagement.CurrentAresPhase >= 6) && attackState >= 100f)
            {
                attackTimer = 0f;
                attackState = 0f;
                npc.netUpdate = true;
            }

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

            // Perform specific behaviors.
            switch ((AresBodyAttackType)(int)attackState)
            {
                case AresBodyAttackType.IdleHover:
                    DoBehavior_IdleHover(npc, target, ref attackTimer);
                    break;
                case AresBodyAttackType.HoverCharge:
                    DoBehavior_HoverCharge(npc, target, ref attackTimer);
                    break;
                case AresBodyAttackType.RadianceLaserBursts:
                    DoBehavior_RadianceLaserBursts(npc, target, ref attackTimer, ref frameType);
                    break;
                case AresBodyAttackType.LaserSpinBursts:
                case AresBodyAttackType.DirectionChangingSpinBursts:
                    DoBehavior_LaserSpinBursts(npc, target, ref enraged, ref attackTimer, ref frameType);
                    break;
            }

            // Perform specific combo attack behaviors.
            if (ExoMechComboAttackContent.UseTwinsAresComboAttack(npc, 1f, ref attackTimer, ref frameType))
                SelectNextAttack(npc);
            if (ExoMechComboAttackContent.UseThanatosAresComboAttack(npc, ref attackTimer, ref frameType))
                SelectNextAttack(npc);
            npc.rotation = npc.velocity.X * 0.0065f;

            attackTimer++;
            return false;
        }

        public static void DoBehavior_DoFinalPhaseTransition(NPC npc, Player target, ref float frame, float phaseTransitionAnimationTime)
        {
            npc.velocity *= 0.925f;
            npc.rotation = 0f;

            // Determine frames.
            frame = (int)AresBodyFrameType.Laugh;

            // Play the transition sound at the start.
            if (phaseTransitionAnimationTime == 3f)
                Main.PlaySound(InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ExoMechFinalPhaseChargeup"), target.Center);
        }

        public static void DoBehavior_IdleHover(NPC npc, Player target, ref float attackTimer)
        {
            int attackTime = ExoMechManagement.CurrentAresPhase >= 5 ? 1350 : 1200;
            Vector2 hoverDestination = target.Center - Vector2.UnitY * 450f;
            ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 24f, 75f);

            if (attackTimer > attackTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_HoverCharge(NPC npc, Player target, ref float attackTimer)
        {
            int chargeCount = 7;
            int hoverTime = 35;
            int chargeTime = 35;
            int contactDamage = 550;
            float hoverSpeed = 65f;
            float chargeSpeed = 38f;

            if (ExoMechManagement.CurrentAresPhase >= 3)
            {
                chargeSpeed += 4f;
            }
            if (ExoMechManagement.CurrentAresPhase >= 5)
            {
                chargeTime -= 2;
                chargeSpeed += 4f;
            }
            if (ExoMechManagement.CurrentAresPhase >= 6)
            {
                chargeCount++;
                chargeTime -= 4;
                contactDamage += 100;
                chargeSpeed += 4f;
            }

            // Have a bit longer of a delay for the first charge.
            if (attackTimer < hoverTime + chargeTime)
                hoverTime += 35;

            float wrappedTime = attackTimer % (hoverTime + chargeTime);

            if (wrappedTime < hoverTime - 15f || attackTimer >= (hoverTime + chargeTime) * chargeCount)
            {
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 300f, -420f);
                npc.Center = npc.Center.MoveTowards(hoverDestination, hoverTime * 0.3f);
                npc.velocity = (npc.velocity * 4f + npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(npc.Distance(hoverDestination), hoverSpeed)) / 5f;
            }
            else if (wrappedTime < hoverTime)
                npc.velocity *= 0.94f;

            else
            {
                npc.damage = contactDamage;
                if (wrappedTime == hoverTime + 1f)
                {
                    // Create lightning bolts in the sky.
                    int lightningBoltCount = ExoMechManagement.CurrentAresPhase >= 6 ? 35 : 20;
                    if (Main.netMode != NetmodeID.Server)
                        ExoMechsSky.CreateLightningBolt(lightningBoltCount, true);

                    npc.velocity = npc.SafeDirectionTo(target.Center + target.velocity * 20f) * chargeSpeed;
                    npc.netUpdate = true;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 7; i++)
                        {
                            Vector2 shootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.TwoPi * i / 7f) * 8f;
                            Vector2 coreSpawnPosition = npc.Center + Vector2.UnitY * 26f;
                            Utilities.NewProjectileBetter(coreSpawnPosition, shootVelocity, ModContent.ProjectileType<TeslaSpark>(), 550, 0f);

                            shootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.TwoPi * (i + 0.5f) / 7f) * 8f;
                            Utilities.NewProjectileBetter(coreSpawnPosition, shootVelocity, ModContent.ProjectileType<TeslaSpark>(), 550, 0f);
                        }
                    }
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/ELRFire"), target.Center);
                }
                npc.velocity *= 1.015f;
            }

            if (attackTimer >= (hoverTime + chargeTime) * chargeCount + 105f)
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
                        int deathray = Utilities.NewProjectileBetter(npc.Center, laserDirection, ModContent.ProjectileType<AresDeathBeam>(), 960, 0f);
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
                        Utilities.NewProjectileBetter(npc.Center, sparkVelocity, ModContent.ProjectileType<TeslaSpark>(), 550, 0f);
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

            // Create laser bursts.
            if (attackTimer == shootDelay + telegraphTime - 1f)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/TeslaCannonFire"), target.Center);

                // Create lightning bolts in the sky.
                int lightningBoltCount = ExoMechManagement.CurrentAresPhase >= 6 ? 55 : 30;
                if (Main.netMode != NetmodeID.Server)
                    ExoMechsSky.CreateLightningBolt(lightningBoltCount, true);

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
                Utilities.NewProjectileBetter(spawnPosition, Vector2.Zero, ModContent.ProjectileType<AresBeamExplosion>(), 550, 0f);
            }

            // Make the laser spin.
            float adjustedTimer = attackTimer - (shootDelay + telegraphTime);
            float spinSpeed = Utils.InverseLerp(0f, 420f, adjustedTimer, true) * MathHelper.Pi / 190f;
            if (npc.ai[0] == (int)AresBodyAttackType.DirectionChangingSpinBursts)
            {
                if (adjustedTimer == (int)(spinTime * 0.5f) - 60)
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/PlagueSounds/PBGNukeWarning"), target.Center);

                    // Create lightning bolts in the sky.
                    int lightningBoltCount = ExoMechManagement.CurrentAresPhase >= 6 ? 55 : 30;
                    if (Main.netMode != NetmodeID.Server)
                        ExoMechsSky.CreateLightningBolt(lightningBoltCount, true);
                }

                if (adjustedTimer < spinTime * 0.5f)
                    spinSpeed *= Utils.InverseLerp(spinTime * 0.5f, spinTime * 0.5f - 45f, adjustedTimer, true);
                else
                    spinSpeed *= -Utils.InverseLerp(spinTime * 0.5f, spinTime * 0.5f + 45f, adjustedTimer, true);
                spinSpeed *= 0.84f;
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
            if (oldAttackType == AresBodyAttackType.IdleHover)
            {
                if (Main.rand.NextBool(3) || ExoMechManagement.CurrentAresPhase < 3)
                    npc.ai[0] = (int)AresBodyAttackType.HoverCharge;
                else if (ExoMechManagement.CurrentAresPhase >= 3)
                {
                    bool complementMechIsPresent = ExoMechManagement.ComplementMechIsPresent(npc);
                    NPC finalMech = ExoMechManagement.FindFinalMech();
                    if (finalMech == npc)
                        finalMech = null;

                    // Use a laser spin if alone. Otherwise, use the radiance burst attack.
                    if ((!complementMechIsPresent && finalMech is null) || ExoMechManagement.CurrentAresPhase == 5)
                        npc.ai[0] = Main.player[npc.target].Infernum().AresSpecialAttackTypeSelector.MakeSelection() + 2f;
                    else
                        npc.ai[0] = (int)AresBodyAttackType.RadianceLaserBursts;
                }
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
                return true;

            NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];

            int thanatosIndex = NPC.FindFirstNPC(ModContent.NPCType<ThanatosHead>());
            if (thanatosIndex >= 0 && aresBody.ai[0] >= 100f && Main.npc[thanatosIndex].Infernum().ExtraAI[13] < 240f)
                return true;

            // The pulse and laser arm are disabled for 2.5 seconds once they swap.
            if (aresBody.Infernum().ExtraAI[14] < 150f && (npc.type == ModContent.NPCType<AresLaserCannon>() || npc.type == ModContent.NPCType<AresPulseCannon>()))
                return true;

            // If Ares is specifically using a combo attack that specifies certain arms should be active, go based on which ones should be active.
            if (ExoMechComboAttackContent.AffectedAresArms.TryGetValue((ExoMechComboAttackContent.ExoMechComboAttackType)aresBody.ai[0], out int[] activeArms))
                return !activeArms.Contains(npc.type);

            bool chargingUp = aresBody.Infernum().ExtraAI[ExoMechManagement.FinalPhaseTimerIndex] > 0f &&
                aresBody.Infernum().ExtraAI[ExoMechManagement.FinalPhaseTimerIndex] < ExoMechManagement.FinalPhaseTransitionTime;
            if (aresBody.ai[0] == (int)AresBodyAttackType.RadianceLaserBursts ||
                aresBody.ai[0] == (int)AresBodyAttackType.HoverCharge ||
                aresBody.ai[0] == (int)AresBodyAttackType.LaserSpinBursts ||
                aresBody.ai[0] == (int)AresBodyAttackType.DirectionChangingSpinBursts ||
                aresBody.ai[0] == (int)ExoMechComboAttackContent.ExoMechComboAttackType.AresTwins_DualLaserCharges ||
                aresBody.ai[0] == (int)ExoMechComboAttackContent.ExoMechComboAttackType.ThanatosAres_LaserCircle ||
                chargingUp)
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

            if (aresBody.Opacity <= 0f)
                return true;

            if (ExoMechManagement.CurrentAresPhase <= 2)
                return false;

            // Rotate arm usability as follows (This only applies before phase 5):
            // Pulse Cannon and Laser Cannon,
            // Laser Cannon and Tesla Cannon,
            // Tesla Cannon and Plasma Flamethrower
            // Plasma Flamethrower and Pulse Cannon.

            // If during or after phase 5, rotate arm usability like this instead:
            // Pulse Cannon, Laser Cannon, and Tesla Cannon,
            // Laser Cannon, Tesla Cannon, and Plasma Flamethrower,
            // Tesla Cannon, Plasma Flamethrower, and Pulse Cannon

            if (ExoMechManagement.CurrentAresPhase < 5)
            {
                switch ((int)aresBody.Infernum().ExtraAI[5] % 4)
                {
                    case 0:
                        return npc.type != ModContent.NPCType<AresPulseCannon>() && npc.type != ModContent.NPCType<AresLaserCannon>();
                    case 1:
                        return npc.type != ModContent.NPCType<AresLaserCannon>() && npc.type != ModContent.NPCType<AresTeslaCannon>();
                    case 2:
                        return npc.type != ModContent.NPCType<AresTeslaCannon>() && npc.type != ModContent.NPCType<AresPlasmaFlamethrower>();
                    case 3:
                        return npc.type != ModContent.NPCType<AresPlasmaFlamethrower>() && npc.type != ModContent.NPCType<AresPulseCannon>();
                }
            }
            else
            {
                switch ((int)aresBody.Infernum().ExtraAI[5] % 3)
                {
                    case 0:
                        return npc.type != ModContent.NPCType<AresPulseCannon>() && npc.type != ModContent.NPCType<AresLaserCannon>() && npc.type != ModContent.NPCType<AresTeslaCannon>();
                    case 1:
                        return npc.type != ModContent.NPCType<AresLaserCannon>() && npc.type != ModContent.NPCType<AresTeslaCannon>() && npc.type != ModContent.NPCType<AresPlasmaFlamethrower>();
                    case 2:
                        return npc.type != ModContent.NPCType<AresTeslaCannon>() && npc.type != ModContent.NPCType<AresPlasmaFlamethrower>() && npc.type != ModContent.NPCType<AresPulseCannon>();
                }
            }
            return false;
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

        public static float FlameTrailWidthFunctionBig(NPC npc, float completionRatio)
        {
            return MathHelper.SmoothStep(60f, 22f, completionRatio) * Utils.InverseLerp(0f, 15f, npc.Infernum().ExtraAI[0], true);
        }

        public static Color FlameTrailColorFunctionBig(NPC npc, float completionRatio)
        {
            float trailOpacity = Utils.InverseLerp(0.8f, 0.27f, completionRatio, true) * Utils.InverseLerp(0f, 0.067f, completionRatio, true) * 1.3f;
            Color startingColor = Color.Lerp(Color.White, Color.Yellow, 0.25f);
            Color middleColor = Color.Lerp(Color.Orange, Color.White, 0.35f);
            Color endColor = Color.Lerp(Color.Red, Color.White, 0.17f);
            Color color = CalamityUtils.MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * Utils.InverseLerp(0f, 15f, npc.Infernum().ExtraAI[0], true) * trailOpacity;
            color.A = 0;
            return color;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            // Draw arms.
            int laserArm = NPC.FindFirstNPC(ModContent.NPCType<AresLaserCannon>());
            int pulseArm = NPC.FindFirstNPC(ModContent.NPCType<AresPulseCannon>());
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

                // Pulse arm.
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
            if (pulseArm != -1)
                DrawArmFunction.Invoke(npc.modNPC, new object[] { spriteBatch, Main.npc[pulseArm].Center, armGlowmaskColor, armProperties[1].Item1, armProperties[1].Item2 });
            if (teslaArm != -1)
                DrawArmFunction.Invoke(npc.modNPC, new object[] { spriteBatch, Main.npc[teslaArm].Center, armGlowmaskColor, armProperties[2].Item1, armProperties[2].Item2 });
            if (plasmaArm != -1)
                DrawArmFunction.Invoke(npc.modNPC, new object[] { spriteBatch, Main.npc[plasmaArm].Center, armGlowmaskColor, armProperties[3].Item1, armProperties[3].Item2 });

            Texture2D texture = Main.npcTexture[npc.type];
            Rectangle frame = npc.frame;
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 center = npc.Center - Main.screenPosition;
            int numAfterimages = 5;

            float finalPhaseGlowInterpolant = Utils.InverseLerp(0f, ExoMechManagement.FinalPhaseTransitionTime * 0.75f, npc.Infernum().ExtraAI[ExoMechManagement.FinalPhaseTimerIndex], true);
            if (finalPhaseGlowInterpolant > 0f)
            {
                float backAfterimageOffset = finalPhaseGlowInterpolant * 6f;
                for (int i = 0; i < 8; i++)
                {
                    Color color = Main.hslToRgb((i / 8f + Main.GlobalTime * 0.6f) % 1f, 1f, 0.56f) * 0.5f;
                    color.A = 0;
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 8f + Main.GlobalTime * 0.8f).ToRotationVector2() * backAfterimageOffset;
                    spriteBatch.Draw(texture, center + drawOffset, frame, npc.GetAlpha(color), npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
                }
            }

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
