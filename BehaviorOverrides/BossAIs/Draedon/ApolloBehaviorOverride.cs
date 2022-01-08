using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.Projectiles.Boss;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
    public class ApolloBehaviorOverride : NPCBehaviorOverride
    {
        public enum TwinsAttackType
        {
            BasicShots,
            FireCharge,
            SpecialAttack_PlasmaCharges,
            SpecialAttack_LaserRayScarletBursts,
            SpecialAttack_GatlingLaserAndPlasmaFlames
        }

        public override int NPCOverrideType => ModContent.NPCType<Apollo>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw | NPCOverrideContext.NPCFindFrame;

        public const int Phase2TransitionTime = 270;
        public const int ComplementMechEnrageTimerIndex = 15;

        #region AI
        public override bool PreAI(NPC npc)
        {
            // Define the life ratio.
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Define the whoAmI variable.
            CalamityGlobalNPC.draedonExoMechTwinGreen = npc.whoAmI;

            // Define attack variables.
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float hoverSide = ref npc.ai[2];
            ref float phaseTransitionAnimationTime = ref npc.ai[3];
            ref float frame = ref npc.localAI[0];
            ref float hasDoneInitializations = ref npc.Infernum().ExtraAI[5];
            ref float hasSummonedComplementMech = ref npc.Infernum().ExtraAI[ExoMechManagement.HasSummonedComplementMechIndex];
            ref float complementMechIndex = ref npc.Infernum().ExtraAI[ExoMechManagement.ComplementMechIndexIndex];
            ref float wasNotInitialSummon = ref npc.Infernum().ExtraAI[ExoMechManagement.WasNotInitialSummonIndex];
            ref float finalMechIndex = ref npc.Infernum().ExtraAI[ExoMechManagement.FinalMechIndexIndex];
            ref float enrageTimer = ref npc.Infernum().ExtraAI[ComplementMechEnrageTimerIndex];
            NPC initialMech = ExoMechManagement.FindInitialMech();
            NPC complementMech = complementMechIndex >= 0 && Main.npc[(int)complementMechIndex].active ? Main.npc[(int)complementMechIndex] : null;
            NPC finalMech = ExoMechManagement.FindFinalMech();
            
            if (Main.netMode != NetmodeID.MultiplayerClient && hasDoneInitializations == 0f)
            {
                hoverSide = 1f;
                complementMechIndex = -1f;
                finalMechIndex = -1f;
                hasDoneInitializations = 1f;

                int artemis = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<Artemis>(), npc.whoAmI);
                if (Main.npc.IndexInRange(artemis))
                    Main.npc[artemis].realLife = npc.whoAmI;

                npc.netUpdate = true;
            }

            // Reset things.
            npc.damage = 0;
            npc.defDamage = 640;
            npc.dontTakeDamage = false;
            npc.Calamity().newAI[0] = (int)Apollo.Phase.ChargeCombo;

            // Decrement the enrage timer.
            if (enrageTimer > 0f)
                enrageTimer--;

            // Summon the complement mech and reset things once ready.
            if (hasSummonedComplementMech == 0f && lifeRatio < ExoMechManagement.Phase4LifeRatio)
            {
                ExoMechManagement.SummonComplementMech(npc);
                hasSummonedComplementMech = 1f;
                attackTimer = 0f;
                SelectNextAttack(npc);

                // Clear away projectiles to prevent lingering, unfair things so that the combo attacks have a clean, open area.
                List<int> projectilesToDelete = new List<int>()
                {
                    ModContent.ProjectileType<ArtemisLaser>(),
                    ModContent.ProjectileType<ApolloPlasmaFireball>(),
                    ModContent.ProjectileType<AresPlasmaBolt>(),
                    ModContent.ProjectileType<ApolloChargeFlameExplosion>(),
                    ModContent.ProjectileType<ArtemisChargeFlameExplosion>(),
                    ModContent.ProjectileType<ExofireSpark>(),
                    ModContent.ProjectileType<PlasmaSpark>(),
                };

                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].active && projectilesToDelete.Contains(Main.projectile[i].type))
                        Main.projectile[i].active = false;
                }

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
                    npc.Center = target.Center - Vector2.UnitY * 2700f;

                attackTimer = 0f;
                attackState = (int)TwinsAttackType.BasicShots;
                npc.Calamity().newAI[1] = (int)Apollo.SecondaryPhase.PassiveAndImmune;
                npc.ModNPC<Apollo>().ChargeComboFlash = 0f;
                npc.Calamity().ShouldCloseHPBar = true;
                npc.dontTakeDamage = true;
            }
            else
                npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.08f, 0f, 1f);

            // Despawn if the target is gone.
            if (!target.active || target.dead)
            {
                npc.TargetClosest(false);
                target = Main.player[npc.target];
                if (!target.active || target.dead)
                    npc.active = false;
            }

            // Handle the second phase transition.
            if (phaseTransitionAnimationTime < Phase2TransitionTime && lifeRatio < ExoMechManagement.Phase3LifeRatio)
            {
                if (phaseTransitionAnimationTime == 1f)
                    SelectNextAttack(npc);

                npc.ModNPC<Apollo>().ChargeComboFlash = 0f;
                attackState = (int)TwinsAttackType.BasicShots;
                phaseTransitionAnimationTime++;
                npc.dontTakeDamage = true;
                DoBehavior_DoPhaseTransition(npc, target, ref frame, hoverSide, phaseTransitionAnimationTime);
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

            if (((finalMech != null && finalMech.Opacity > 0f) || ExoMechManagement.CurrentTwinsPhase >= 6) && attackState >= 100f)
            {
                attackTimer = 0f;
                attackState = 0f;
                npc.netUpdate = true;
            }

            switch ((TwinsAttackType)(int)attackState)
            {
                case TwinsAttackType.BasicShots:
                    DoBehavior_BasicShots(npc, target, false, hoverSide, ref frame, ref attackTimer);
                    break;
                case TwinsAttackType.FireCharge:
                    DoBehavior_FireCharge(npc, target, hoverSide, ref frame, ref attackTimer);
                    break;
                case TwinsAttackType.SpecialAttack_PlasmaCharges:
                    DoBehavior_PlasmaCharges(npc, target, hoverSide, ref frame, ref attackTimer);
                    break;
                case TwinsAttackType.SpecialAttack_LaserRayScarletBursts:
                    DoBehavior_LaserRayScarletBursts(npc, target, ref frame, ref attackTimer);
                    break;
                case TwinsAttackType.SpecialAttack_GatlingLaserAndPlasmaFlames:
                    DoBehavior_GatlingLaserAndPlasmaFlames(npc, target, hoverSide, ref frame, ref attackTimer);
                    break;
            }

            switch ((ExoMechComboAttackContent.ExoMechComboAttackType)attackState)
            {
                case ExoMechComboAttackContent.ExoMechComboAttackType.AresTwins_PressureLaser:
                    if (ExoMechComboAttackContent.DoBehavior_AresTwins_PressureLaser(npc, target, hoverSide, ref attackTimer, ref frame))
                        SelectNextAttack(npc);
                    break;
                case ExoMechComboAttackContent.ExoMechComboAttackType.AresTwins_DualLaserCharges:
                    if (ExoMechComboAttackContent.DoBehavior_AresTwins_DualLaserCharges(npc, target, hoverSide, ref attackTimer, ref frame))
                        SelectNextAttack(npc);
                    break;
                case ExoMechComboAttackContent.ExoMechComboAttackType.AresTwins_CircleAttack:
                    if (ExoMechComboAttackContent.DoBehavior_AresTwins_CircleAttack(npc, target, hoverSide, ref attackTimer, ref frame))
                        SelectNextAttack(npc);
                    break;
                case ExoMechComboAttackContent.ExoMechComboAttackType.AresTwins_ElectromagneticPlasmaStar:
                    if (ExoMechComboAttackContent.DoBehavior_AresTwins_ElectromagneticPlasmaStar(npc, target, hoverSide, ref attackTimer, ref frame))
                        SelectNextAttack(npc);
                    break;
            }

            attackTimer++;
            return false;
        }

        public static void DoBehavior_DoPhaseTransition(NPC npc, Player target, ref float frame, float hoverSide, float phaseTransitionAnimationTime)
        {
            int startingFrame = 30;
            int endingFrame = 59;
            Vector2 hoverDestination = target.Center + Vector2.UnitX * hoverSide * 780f;

            // Determine rotation.
            npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;

            // Move to the appropriate side of the target.
            AresBodyBehaviorOverride.DoHoverMovement(npc, hoverDestination, 30f, 84f);

            // Determine frames.
            frame = (int)Math.Round(MathHelper.Lerp(startingFrame, endingFrame, phaseTransitionAnimationTime / Phase2TransitionTime));

            // Create the pupil gore thing.
            int pupilPopoffTime = (int)(Phase2TransitionTime * Utils.InverseLerp(startingFrame, endingFrame, 37.5f));
            int chargeupSoundTime = (int)(Phase2TransitionTime * Utils.InverseLerp(startingFrame, endingFrame, 46.5f));
            if (phaseTransitionAnimationTime == pupilPopoffTime)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/LargeWeaponFire"), npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int lensType = ModContent.ProjectileType<BrokenApolloLens>();
                    Vector2 lensDirection = (npc.rotation - MathHelper.PiOver2).ToRotationVector2();
                    if (npc.type == ModContent.NPCType<Artemis>())
                        lensType = ModContent.ProjectileType<BrokenArtemisLens>();

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Projectile.NewProjectile(npc.Center + lensDirection * 70f, lensDirection * 24f, lensType, 0, 0f);
                    npc.netUpdate = true;
                }
            }

            if (phaseTransitionAnimationTime >= chargeupSoundTime && phaseTransitionAnimationTime <= chargeupSoundTime + 40f && phaseTransitionAnimationTime % 16f == 15f)
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/GatlingLaserFireStart"), npc.Center);

            if (phaseTransitionAnimationTime == chargeupSoundTime + 75f)
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/GatlingLaserFireEnd"), npc.Center);
        }

        public static void DoBehavior_BasicShots(NPC npc, Player target, bool calmTheFuckDown, float hoverSide, ref float frame, ref float attackTimer)
        {
            int totalShots = 8;
            float shootRate = 52f;
            float projectileShootSpeed = 10f;
            float predictivenessFactor = 18.5f;
            if (npc.type == ModContent.NPCType<Artemis>())
                predictivenessFactor = 24f;

            Vector2 aimDestination = target.Center + target.velocity * predictivenessFactor;
            Vector2 aimDirection = npc.SafeDirectionTo(aimDestination);

            if (ExoMechManagement.CurrentTwinsPhase >= 2)
                shootRate -= 12f;
            if (ExoMechManagement.CurrentTwinsPhase == 3)
            {
                shootRate -= 8f;
                projectileShootSpeed *= 1.3f;
            }
            if (ExoMechManagement.CurrentTwinsPhase >= 5)
            {
                shootRate -= 20f;
                projectileShootSpeed *= 1.35f;
            }
            if (ExoMechManagement.CurrentTwinsPhase >= 6)
            {
                shootRate -= 5f;
                totalShots += 8;
            }

            // Artemis' projectiles do not speed up.
            if (npc.type == ModContent.NPCType<Artemis>())
                projectileShootSpeed = 10f;

            if (calmTheFuckDown)
                shootRate += 25;

            ref float hoverOffsetX = ref npc.Infernum().ExtraAI[0];
            ref float hoverOffsetY = ref npc.Infernum().ExtraAI[1];
            ref float shootCounter = ref npc.Infernum().ExtraAI[2];
            ref float generalAttackTimer = ref npc.Infernum().ExtraAI[3];

            generalAttackTimer++;
            if (shootCounter < 0f)
                shootCounter = 0f;

            Vector2 hoverDestination = target.Center;
            hoverDestination.X += hoverOffsetX;
            hoverDestination.Y += hoverOffsetY;
            hoverDestination += Vector2.UnitX * hoverSide * 780f;

            // Determine rotation.
            npc.rotation = aimDirection.ToRotation() + MathHelper.PiOver2;

            // Move to the appropriate side of the target.
            AresBodyBehaviorOverride.DoHoverMovement(npc, hoverDestination, 30f, 84f);

            // Fire a plasma burst/laser shot and select a new offset.
            if (attackTimer >= shootRate)
            {
                if (npc.type == ModContent.NPCType<Apollo>())
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PlasmaCasterFire"), npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 plasmaShootVelocity = aimDirection * projectileShootSpeed;
                        int plasma = Utilities.NewProjectileBetter(npc.Center + aimDirection * 70f, plasmaShootVelocity, ModContent.ProjectileType<ApolloPlasmaFireball>(), 500, 0f);
                        if (Main.projectile.IndexInRange(plasma))
                            Main.projectile[plasma].ai[0] = shootCounter % 2f;
                    }
                }
                else if (npc.WithinRange(hoverDestination, 100f))
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/LaserCannon"), npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 laserShootVelocity = aimDirection * projectileShootSpeed * 0.8f;
                        int laser = Utilities.NewProjectileBetter(npc.Center + aimDirection * 70f, laserShootVelocity, ModContent.ProjectileType<ArtemisLaser>(), 500, 0f);
                        if (Main.projectile.IndexInRange(laser))
                        {
                            Main.projectile[laser].ModProjectile<ArtemisLaser>().InitialDestination = aimDestination + aimDirection * 1000f;
                            Main.projectile[laser].ai[1] = npc.whoAmI;
                            Main.projectile[laser].netUpdate = true;
                        }
                    }
                }

                hoverOffsetX = Main.rand.NextFloat(-50f, 50f);
                hoverOffsetY = Main.rand.NextFloat(-250f, 250f);
                attackTimer = 0f;
                shootCounter++;
                npc.netUpdate = true;
            }

            // Calculate frames.
            frame = (int)Math.Round(MathHelper.Lerp(20f, 29f, attackTimer / shootRate));
            if (ExoMechManagement.CurrentTwinsPhase >= 2)
                frame += 60f;

            if (shootCounter >= totalShots)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_FireCharge(NPC npc, Player target, float hoverSide, ref float frame, ref float attackTimer)
        {
            int waitTime = 12;
            int chargeTime = 45;
            int totalCharges = 3;
            float chargeSpeed = 42f;
            float chargePredictiveness = 20f;
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            Vector2 hoverDestination = target.Center - Vector2.UnitY * 300f;

            ref float attackDelay = ref npc.Infernum().ExtraAI[1];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[1];

            if (chargeCounter == 0f)
                hoverDestination.X += hoverSide * 540f;
            else
                hoverDestination.X += (target.Center.X < npc.Center.X).ToDirectionInt() * 540f;

            switch ((int)attackSubstate)
            {
                // Hover into position.
                case 0:
                    npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;

                    // Hover to the top left/right of the target.
                    AresBodyBehaviorOverride.DoHoverMovement(npc, hoverDestination, 48f, 92f);

                    npc.frameCounter++;
                    frame = (int)Math.Round(MathHelper.Lerp(10f, 19f, (float)npc.frameCounter / 36f % 1f));

                    // Once sufficiently close, go to the next attack substate.
                    if (npc.WithinRange(hoverDestination, 50f))
                    {
                        npc.velocity = Vector2.Zero;
                        attackSubstate = 1f;
                        attackTimer = 0f;
                        npc.netUpdate = true;
                    }
                    break;

                // Wait in place for a short period of time.
                case 1:
                    npc.rotation = npc.AngleTo(target.Center + target.velocity * chargePredictiveness) + MathHelper.PiOver2;

                    // Decide frames.
                    npc.frameCounter++;
                    frame = (int)Math.Round(MathHelper.Lerp(20f, 29f, (float)npc.frameCounter / 36f % 1f));

                    // Calculate the charge flash.
                    if (npc.type == ModContent.NPCType<Artemis>())
                        npc.ModNPC<Artemis>().ChargeFlash = MathHelper.Clamp(attackTimer / waitTime, 0f, 1f);
                    else
                        npc.ModNPC<Apollo>().ChargeComboFlash = MathHelper.Clamp(attackTimer / waitTime, 0f, 1f);

                    if (attackTimer >= waitTime && attackDelay >= 45f)
                    {
                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/ELRFire"), npc.Center);
                        npc.velocity = npc.SafeDirectionTo(target.Center + target.velocity * chargePredictiveness) * chargeSpeed;
                        attackSubstate = 2f;
                        attackTimer = 0f;
                        npc.netUpdate = true;
                    }
                    break;

                // Release fire.
                case 2:
                    npc.damage = npc.defDamage;

                    // Decide frames.
                    npc.frameCounter++;
                    frame = (int)Math.Round(MathHelper.Lerp(20f, 29f, (float)npc.frameCounter / 36f % 1f));

                    // Calculate the charge flash.
                    if (npc.type == ModContent.NPCType<Artemis>())
                        npc.ModNPC<Artemis>().ChargeFlash = Utils.InverseLerp(chargeTime, chargeTime - 10f, attackTimer, true);
                    else
                        npc.ModNPC<Apollo>().ChargeComboFlash = Utils.InverseLerp(chargeTime, chargeTime - 10f, attackTimer, true);

                    // Emit exploding frames.
                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 8f == 7f)
                    {
                        int explosionType = ModContent.ProjectileType<ApolloChargeFlameExplosion>();
                        if (npc.type == ModContent.NPCType<Artemis>())
                            explosionType = ModContent.ProjectileType<ArtemisChargeFlameExplosion>();

                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, explosionType, 500, 0f);
                        npc.netUpdate = true;
                    }

                    if (attackTimer >= chargeTime)
                    {
                        attackTimer = 0f;
                        attackSubstate = 0f;
                        chargeCounter++;
                        npc.netUpdate = true;

                        if (chargeCounter >= totalCharges)
                            SelectNextAttack(npc);
                    }
                    break;
            }

            // Update frames for the second phase.
            if (ExoMechManagement.CurrentTwinsPhase >= 2 && frame <= 30f)
                frame += 60f;
            attackDelay++;
        }

        public static void DoBehavior_PlasmaCharges(NPC npc, Player target, float hoverSide, ref float frame, ref float attackTimer)
        {
            // Make Artemis go away so Apollo can do its attack without interference.
            if (npc.type == ModContent.NPCType<Artemis>())
            {
                if (ExoMechManagement.CurrentTwinsPhase >= 5)
                {
                    DoBehavior_BasicShots(npc, target, true, -1f, ref frame, ref attackTimer);
                    attackTimer++;
                }
                else
                {
                    npc.dontTakeDamage = true;
                    npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.16f, 0f, 1f);
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center - Vector2.UnitY * 450f) * 40f, 1.25f);
                }
                return;
            }

            int waitTime = 8;
            int chargeTime = 45;
            int totalCharges = 6;
            int sparkCount = 18;
            int homingSparkCount = 3;
            float chargeSpeed = 54f;
            float chargePredictiveness = 10f;
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            Vector2 hoverDestination = target.Center - Vector2.UnitY * 300f;

            if (ExoMechManagement.CurrentTwinsPhase == 3)
                chargeSpeed += 2.5f;
            if (ExoMechManagement.CurrentTwinsPhase >= 5)
            {
                chargeTime -= 5;
                totalCharges--;
                homingSparkCount++;
                chargeSpeed -= 4f;
            }
            if (ExoMechManagement.CurrentTwinsPhase >= 6)
                totalCharges--;

            ref float attackDelay = ref npc.Infernum().ExtraAI[1];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[2];

            if (chargeCounter == 0f)
                hoverDestination.X += hoverSide * 540f;
            else
                hoverDestination.X += (target.Center.X < npc.Center.X).ToDirectionInt() * 540f;

            switch ((int)attackSubstate)
            {
                // Hover into position.
                case 0:
                    npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;

                    // Hover to the top left/right of the target.
                    AresBodyBehaviorOverride.DoHoverMovement(npc, hoverDestination, 50f, 92f);

                    npc.frameCounter++;
                    frame = (int)Math.Round(MathHelper.Lerp(70f, 79f, (float)npc.frameCounter / 36f % 1f));

                    // Once sufficiently close, go to the next attack substate.
                    if (npc.WithinRange(hoverDestination, 50f))
                    {
                        npc.velocity = Vector2.Zero;
                        attackSubstate = 1f;
                        attackTimer = 0f;
                        npc.netUpdate = true;
                    }
                    break;

                // Wait in place for a short period of time.
                case 1:
                    npc.rotation = npc.AngleTo(target.Center + target.velocity * chargePredictiveness) + MathHelper.PiOver2;

                    // Decide frames.
                    npc.frameCounter++;
                    frame = (int)Math.Round(MathHelper.Lerp(80f, 89f, (float)npc.frameCounter / 36f % 1f));

                    // Calculate the charge flash.
                    npc.ModNPC<Apollo>().ChargeComboFlash = MathHelper.Clamp(attackTimer / waitTime, 0f, 1f);

                    // Charge and release sparks.
                    if (attackTimer >= waitTime && attackDelay >= 45f)
                    {
                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/ELRFire"), npc.Center);

                        npc.velocity = npc.SafeDirectionTo(target.Center + target.velocity * chargePredictiveness) * chargeSpeed;

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            float offsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                            for (int i = 0; i < sparkCount; i++)
                            {
                                Vector2 sparkShootVelocity = (MathHelper.TwoPi * i / sparkCount + offsetAngle).ToRotationVector2() * 16f;
                                Utilities.NewProjectileBetter(npc.Center + sparkShootVelocity * 10f, sparkShootVelocity, ModContent.ProjectileType<AcceleratingPlasmaSpark>(), 530, 0f);
                            }

                            for (int i = 0; i < homingSparkCount; i++)
                            {
                                Vector2 sparkShootVelocity = (MathHelper.TwoPi * i / homingSparkCount + offsetAngle).ToRotationVector2() * 10f;
                                Utilities.NewProjectileBetter(npc.Center + sparkShootVelocity * 10f, sparkShootVelocity, ModContent.ProjectileType<PlasmaSpark>(), 530, 0f);
                            }
                        }

                        attackSubstate = 2f;
                        attackTimer = 0f;
                        npc.netUpdate = true;
                    }
                    break;

                // Release fire.
                case 2:
                    npc.damage = npc.defDamage;

                    // Decide frames.
                    npc.frameCounter++;
                    frame = (int)Math.Round(MathHelper.Lerp(80f, 89f, (float)npc.frameCounter / 36f % 1f));

                    // Calculate the charge flash.
                    npc.ModNPC<Apollo>().ChargeComboFlash = Utils.InverseLerp(chargeTime, chargeTime - 10f, attackTimer, true);

                    if (attackTimer >= chargeTime)
                    {
                        attackTimer = 0f;
                        attackSubstate = 0f;
                        chargeCounter++;
                        npc.netUpdate = true;

                        if (chargeCounter >= totalCharges)
                            SelectNextAttack(npc);
                    }
                    break;
            }
            attackDelay++;
        }

        public static void DoBehavior_LaserRayScarletBursts(NPC npc, Player target, ref float frame, ref float attackTimer)
        {
            // Make Apollo go away so Artemis can do its attack without interference.
            if (npc.type == ModContent.NPCType<Apollo>())
            {
                if (ExoMechManagement.CurrentTwinsPhase >= 5)
                    DoBehavior_BasicShots(npc, target, true, 1f, ref frame, ref attackTimer);
                else
                {
                    npc.dontTakeDamage = true;
                    npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.16f, 0f, 1f);
                    npc.ai[1] = Main.npc[NPC.FindFirstNPC(ModContent.NPCType<Artemis>())].ai[1];
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center - Vector2.UnitY * 450f) * 40f, 1.25f);
                }
                return;
            }

            int shootDelay = 48;
            int burstReleaseRate = 30;
            float spinRadius = 540f;
            float spinArc = MathHelper.Pi * 1.15f;

            npc.dontTakeDamage = false;

            if (ExoMechManagement.CurrentTwinsPhase >= 3)
                spinArc *= 1.1f;
            if (ExoMechManagement.CurrentTwinsPhase == 4)
                burstReleaseRate += 10;
            if (ExoMechManagement.CurrentTwinsPhase >= 5)
            {
                spinArc *= 1.2f;
                burstReleaseRate -= 8;
            }
            if (ExoMechManagement.CurrentTwinsPhase >= 6)
                burstReleaseRate -= 4;

            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float spinDirection = ref npc.Infernum().ExtraAI[1];
            ref float spinningPointX = ref npc.Infernum().ExtraAI[2];
            ref float spinningPointY = ref npc.Infernum().ExtraAI[3];
            ref float verticalOffsetDirection = ref npc.Infernum().ExtraAI[4];

            if (verticalOffsetDirection == 0f)
            {
                verticalOffsetDirection = Main.rand.NextBool().ToDirectionInt();
                npc.netUpdate = true;
            }

            Vector2 hoverDestination = target.Center - Vector2.UnitY * verticalOffsetDirection * spinRadius;

            switch ((int)attackSubstate)
            {
                // Hover into position.
                case 0:
                    // Play a telegraph sound on the first frame of the laserbeam.
                    // This is do so that the player can be perfectly aware that a sweep is coming.
                    if (attackTimer == 1f)
                        Main.PlaySound(InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ArtemisLaserSweepTelegraph"), target.Center);

                    npc.frameCounter++;
                    frame = (int)Math.Round(MathHelper.Lerp(70f, 79f, (float)npc.frameCounter / 36f % 1f));

                    npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;
                    AresBodyBehaviorOverride.DoHoverMovement(npc, hoverDestination, 30f, 84f);

                    // Begin hovering in place once sufficiently close to the hover position.
                    if (npc.WithinRange(hoverDestination, 50f))
                    {
                        npc.velocity = Vector2.Zero;
                        npc.Center = hoverDestination;
                        npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;
                        spinningPointX = target.Center.X;
                        spinningPointY = target.Center.Y;
                        attackTimer = 0f;
                        attackSubstate = 1f;
                        npc.netUpdate = true;
                    }
                    break;

                // Stay in place for a brief moment.
                case 1:
                    npc.frameCounter++;
                    frame = (int)Math.Round(MathHelper.Lerp(70f, 79f, (float)npc.frameCounter / 36f % 1f));

                    // Fire the laser.
                    if (attackTimer >= shootDelay)
                    {
                        attackTimer = 0f;
                        attackSubstate = 2f;
                        spinDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        npc.netUpdate = true;

                        // Play the moon lord laser sound (cringe).
                        Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, 104);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int type = ModContent.ProjectileType<ArtemisSpinLaser>();
                            int laser = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, type, 1100, 0f, Main.myPlayer, npc.whoAmI);
                            if (Main.projectile.IndexInRange(laser))
                            {
                                Main.projectile[laser].ai[0] = npc.whoAmI;
                                Main.projectile[laser].ai[1] = spinDirection;
                            }
                        }
                    }
                    break;

                // Spin 2 win.
                case 2:
                    npc.frameCounter++;
                    frame = (int)Math.Round(MathHelper.Lerp(80f, 89f, (float)npc.frameCounter / 32f % 1f));

                    float spinAngle = (float)Math.Pow(attackTimer / ArtemisSpinLaser.LaserLifetime, 1.2f) * spinArc * -spinDirection * verticalOffsetDirection;
                    npc.velocity = spinAngle.ToRotationVector2() * MathHelper.TwoPi * spinRadius / ArtemisSpinLaser.LaserLifetime * -spinDirection;
                    npc.rotation = npc.AngleTo(new Vector2(spinningPointX, spinningPointY)) + MathHelper.PiOver2;

                    if (attackTimer >= ArtemisSpinLaser.LaserLifetime - 16f)
                        SelectNextAttack(npc);
                    break;
            }

            // Release orange explosions everywhere periodically.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % burstReleaseRate == burstReleaseRate - 1f)
            {
                Vector2 targetDirection = target.velocity.SafeNormalize(Main.rand.NextVector2Unit());
                Vector2 spawnPosition = target.Center - targetDirection.RotatedByRandom(1.1f) * Main.rand.NextFloat(325f, 725f) * new Vector2(1f, 0.6f);
                Utilities.NewProjectileBetter(spawnPosition, Vector2.Zero, ModContent.ProjectileType<ArtemisChargeFlameExplosion>(), 530, 0f);
            }
            attackTimer++;
        }

        public static void DoBehavior_GatlingLaserAndPlasmaFlames(NPC npc, Player target, float hoverSide, ref float frame, ref float attackTimer)
        {
            int shootTime = 420;
            Vector2 hoverDestination = target.Center + new Vector2(hoverSide * 750f, -100f);
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float hoverOffsetX = ref npc.Infernum().ExtraAI[1];
            ref float hoverOffsetY = ref npc.Infernum().ExtraAI[2];

            switch ((int)attackSubstate)
            {
                // Hover into position.
                case 0:
                    npc.frameCounter++;
                    frame = (int)Math.Round(MathHelper.Lerp(70f, 79f, (float)npc.frameCounter / 36f % 1f));

                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 40f, 1.15f);
                    npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;

                    if (attackTimer >= 90f)
                    {
                        attackSubstate = 1f;
                        attackTimer = 0f;
                        npc.netUpdate = true;
                    }
                    break;

                // Begin firing.
                case 1:
                    npc.frameCounter++;
                    frame = (int)Math.Round(MathHelper.Lerp(80f, 89f, (float)npc.frameCounter / 30f % 1f));

                    // Reset the hover offset periodically.
                    if (attackTimer % 90f == 89f)
                    {
                        hoverOffsetX = Main.rand.NextFloat(-50f, 50f);
                        hoverOffsetY = Main.rand.NextFloat(-520f, 520f);
                    }

                    // Fire a machine-gun of lasers.
                    if (npc.type == ModContent.NPCType<Artemis>())
                    {
                        int laserShootRate = 14;
                        float laserShootSpeed = 6f;
                        float predictivenessFactor = 18.5f;
                        Vector2 aimDestination = target.Center + target.velocity * new Vector2(predictivenessFactor, predictivenessFactor * 2.6f);
                        Vector2 aimDirection = npc.SafeDirectionTo(aimDestination);
                        npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;

                        if (ExoMechManagement.CurrentTwinsPhase == 4)
                            laserShootRate += 7;
                        if (ExoMechManagement.CurrentTwinsPhase >= 5)
                            laserShootRate -= 3;
                        if (ExoMechManagement.CurrentTwinsPhase >= 6)
                            laserShootRate -= 4;

                        // Do movement.
                        AresBodyBehaviorOverride.DoHoverMovement(npc, hoverDestination + new Vector2(hoverOffsetX, hoverOffsetY), 32f, 84f);

                        // Play a laser preparation sound.
                        if (attackTimer == 15f)
                            Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/GatlingLaserFireStart"), target.Center);

                        // Play the laser fire loop.
                        if (attackTimer >= 15f && attackTimer % 70f == 20f)
                            Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/GatlingLaserFireLoop"), target.Center);

                        if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= 15f && attackTimer % laserShootRate == laserShootRate - 1f)
                        {
                            for (int i = -1; i <= 1; i++)
                            {
                                int laser = Utilities.NewProjectileBetter(npc.Center + aimDirection * 70f, aimDirection * laserShootSpeed, ModContent.ProjectileType<ArtemisGatlingLaser>(), 550, 0f);
                                if (Main.projectile.IndexInRange(laser))
                                {
                                    Main.projectile[laser].ModProjectile<ArtemisGatlingLaser>().InitialDestination = aimDestination;
                                    Main.projectile[laser].localAI[0] = i;
                                    Main.projectile[laser].ai[1] = npc.whoAmI;
                                    Main.projectile[laser].netUpdate = true;
                                }
                            }
                        }
                    }

                    // Release streams of plasma blasts rapid-fire.
                    else
                    {
                        int plasmaShootRate = 40;
                        float plasmaShootSpeed = 10f;
                        float predictivenessFactor = 25f;
                        Vector2 aimDestination = target.Center + target.velocity * predictivenessFactor;
                        Vector2 aimDirection = npc.SafeDirectionTo(aimDestination);
                        npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;

                        if (ExoMechManagement.CurrentTwinsPhase == 4)
                            plasmaShootRate += 16;
                        if (ExoMechManagement.CurrentTwinsPhase >= 5)
                            plasmaShootRate -= 8;

                        // Do movement.
                        AresBodyBehaviorOverride.DoHoverMovement(npc, hoverDestination + new Vector2(hoverOffsetX, hoverOffsetY), 32f, 84f);

                        if (attackTimer >= 15f && attackTimer % plasmaShootRate == plasmaShootRate - 1f)
                        {
                            Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PlasmaCasterFire"), npc.Center);

                            if (Main.netMode != NetmodeID.MultiplayerClient)
                                Utilities.NewProjectileBetter(npc.Center + aimDirection * 70f, aimDirection * plasmaShootSpeed, ModContent.ProjectileType<AresPlasmaFireball>(), 550, 0f);
                        }

                        if (attackTimer >= shootTime)
                            SelectNextAttack(npc);
                    }
                    break;
            }
        }

        public static void SelectNextAttack(NPC npc)
        {
            // Reset the frame counter, in case it was used in the previous attack.
            npc.frameCounter = 0f;
            
            TwinsAttackType oldAttackType = (TwinsAttackType)(int)npc.ai[0];

            // Update learning stuff.
            ExoMechManagement.DoPostAttackSelections(npc);

            npc.ai[0] = (int)TwinsAttackType.BasicShots;
            if (oldAttackType == TwinsAttackType.BasicShots)
            {
                npc.ai[0] = (int)TwinsAttackType.FireCharge;
                if (ExoMechManagement.CurrentTwinsPhase >= 2 && Main.rand.NextBool())
                    npc.ai[0] = Main.player[npc.target].Infernum().TwinsSpecialAttackTypeSelector.MakeSelection() + 2;
                if (ExoMechManagement.CurrentTwinsPhase >= 3 && Main.rand.NextBool(3))
                    npc.ai[0] = (int)TwinsAttackType.SpecialAttack_GatlingLaserAndPlasmaFlames;
            }

            if (ExoMechComboAttackContent.ShouldSelectComboAttack(npc, out ExoMechComboAttackContent.ExoMechComboAttackType newAttack))
            {
                npc.ai[0] = (int)newAttack;
                if (npc.type == ModContent.NPCType<Apollo>())
                {
                    NPC artemis = Main.npc[NPC.FindFirstNPC(ModContent.NPCType<Artemis>())];
                    artemis.ai[0] = npc.ai[0];
                }
            }

            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            // Increment the attack counter. It is used when determining if the mechs should swap sides.
            npc.Infernum().ExtraAI[5]++;

            // Inform Apollo of the attack state change if Artemis calls this method, as Artemis' state relies on Apollo's.
            if (npc.type == ModContent.NPCType<Artemis>() && Main.npc.IndexInRange(npc.realLife) && Main.npc[npc.realLife].active && Main.npc[npc.realLife].type != ModContent.NPCType<Artemis>())
                SelectNextAttack(Main.npc[npc.realLife]);
            else
            {
                NPC artemis = Main.npc[NPC.FindFirstNPC(ModContent.NPCType<Artemis>())];
                artemis.ai[0] = npc.ai[0];
                artemis.ai[1] = 0f;
                for (int i = 0; i < 5; i++)
                    artemis.Infernum().ExtraAI[i] = 0f;

                if (npc.Infernum().ExtraAI[5] % 5f == 2f)
                    npc.ai[2] *= -1f;
                artemis.netUpdate = true;
            }

            npc.netUpdate = true;
        }

        #endregion AI

        #region Frames and Drawcode
        public override void FindFrame(NPC npc, int frameHeight)
        {
            int frameX = (int)npc.localAI[0] / 9;
            int frameY = (int)npc.localAI[0] % 9;
            npc.frame = new Rectangle(npc.width * frameX, npc.height * frameY, npc.width, npc.height);
        }

        public static float FlameTrailWidthFunction(NPC npc, float completionRatio) => MathHelper.SmoothStep(21f, 8f, completionRatio) * npc.ModNPC<Apollo>().ChargeComboFlash * npc.Opacity;

        public static float FlameTrailWidthFunctionBig(NPC npc, float completionRatio) => MathHelper.SmoothStep(34f, 12f, completionRatio) * npc.ModNPC<Apollo>().ChargeComboFlash * npc.Opacity;

        public float RibbonTrailWidthFunction(float completionRatio)
        {
            float baseWidth = Utils.InverseLerp(1f, 0.54f, completionRatio, true) * 5f;
            float endTipWidth = CalamityUtils.Convert01To010(Utils.InverseLerp(0.96f, 0.89f, completionRatio, true)) * 2.4f;
            return baseWidth + endTipWidth;
        }

        public static Color FlameTrailColorFunction(NPC npc, float completionRatio)
        {
            float trailOpacity = Utils.InverseLerp(0.8f, 0.27f, completionRatio, true) * Utils.InverseLerp(0f, 0.067f, completionRatio, true) * npc.Opacity;
            Color startingColor = Color.Lerp(Color.White, Color.Cyan, 0.27f);
            Color middleColor = Color.Lerp(Color.Orange, Color.ForestGreen, 0.74f);
            Color endColor = Color.Lime;
            return CalamityUtils.MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * npc.ModNPC<Apollo>().ChargeComboFlash * trailOpacity;
        }

        public static Color FlameTrailColorFunctionBig(NPC npc, float completionRatio)
        {
            float trailOpacity = Utils.InverseLerp(0.8f, 0.27f, completionRatio, true) * Utils.InverseLerp(0f, 0.067f, completionRatio, true) * 0.56f;
            Color startingColor = Color.Lerp(Color.White, Color.Cyan, 0.25f);
            Color middleColor = Color.Lerp(Color.Blue, Color.White, 0.35f);
            Color endColor = Color.Lerp(Color.DarkBlue, Color.White, 0.47f);
            Color color = CalamityUtils.MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * npc.ModNPC<Apollo>().ChargeComboFlash * trailOpacity;
            color.A = 0;
            return color;
        }

        public static Color RibbonTrailColorFunction(NPC npc, float completionRatio)
        {
            Color startingColor = new Color(34, 40, 48);
            Color endColor = new Color(40, 160, 32);
            return Color.Lerp(startingColor, endColor, (float)Math.Pow(completionRatio, 1.5D)) * npc.Opacity;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            // Declare the trail drawers if they have yet to be defined.
            if (npc.ModNPC<Apollo>().ChargeFlameTrail is null)
                npc.ModNPC<Apollo>().ChargeFlameTrail = new PrimitiveTrail(c => FlameTrailWidthFunction(npc, c), c => FlameTrailColorFunction(npc, c), null, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);

            if (npc.ModNPC<Apollo>().ChargeFlameTrailBig is null)
                npc.ModNPC<Apollo>().ChargeFlameTrailBig = new PrimitiveTrail(c => FlameTrailWidthFunctionBig(npc, c), c => FlameTrailColorFunctionBig(npc, c), null, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);

            if (npc.ModNPC<Apollo>().RibbonTrail is null)
                npc.ModNPC<Apollo>().RibbonTrail = new PrimitiveTrail(RibbonTrailWidthFunction, c => RibbonTrailColorFunction(npc, c));

            // Prepare the flame trail shader with its map texture.
            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(ModContent.GetTexture("CalamityMod/ExtraTextures/ScarletDevilStreak"));

            int numAfterimages = npc.ModNPC<Apollo>().ChargeComboFlash > 0f ? 0 : 5;
            Texture2D texture = Main.npcTexture[npc.type];
            Rectangle frame = npc.frame;
            Vector2 origin = npc.Size * 0.5f;
            Vector2 center = npc.Center - Main.screenPosition;
            Color afterimageBaseColor = ExoMechComboAttackContent.EnrageTimer > 0f ? Color.Red : Color.White;

            // Draws a single instance of a regular, non-glowmask based Apollo.
            // This is created to allow easy duplication of them when drawing the charge.
            void drawInstance(Vector2 drawOffset, Color baseColor)
            {
                if (CalamityConfig.Instance.Afterimages)
                {
                    for (int i = 1; i < numAfterimages; i += 2)
                    {
                        Color afterimageColor = npc.GetAlpha(Color.Lerp(baseColor, afterimageBaseColor, 0.5f)) * ((numAfterimages - i) / 15f);
                        Vector2 afterimageCenter = npc.oldPos[i] + frame.Size() * 0.5f - Main.screenPosition;
                        spriteBatch.Draw(texture, afterimageCenter, npc.frame, afterimageColor, npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
                    }
                }

                spriteBatch.Draw(texture, center + drawOffset, frame, npc.GetAlpha(baseColor), npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            }

            // Draw ribbons near the main thruster
            for (int direction = -1; direction <= 1; direction += 2)
            {
                Vector2 ribbonOffset = -Vector2.UnitY.RotatedBy(npc.rotation) * 14f;
                ribbonOffset += Vector2.UnitX.RotatedBy(npc.rotation) * direction * 26f;

                float currentSegmentRotation = npc.rotation;
                List<Vector2> ribbonDrawPositions = new List<Vector2>();
                for (int i = 0; i < 12; i++)
                {
                    float ribbonCompletionRatio = i / 12f;
                    float wrappedAngularOffset = MathHelper.WrapAngle(npc.oldRot[i + 1] - currentSegmentRotation) * 0.3f;
                    float segmentRotationOffset = MathHelper.Clamp(wrappedAngularOffset, -0.12f, 0.12f);

                    // Add a sinusoidal offset that goes based on time and completion ratio to create a waving-flag-like effect.
                    // This is dampened for the first few points to prevent weird offsets. It is also dampened by high velocity.
                    float sinusoidalRotationOffset = (float)Math.Sin(ribbonCompletionRatio * 2.22f + Main.GlobalTime * 3.4f) * 1.36f;
                    float sinusoidalRotationOffsetFactor = Utils.InverseLerp(0f, 0.37f, ribbonCompletionRatio, true) * direction * 24f;
                    sinusoidalRotationOffsetFactor *= Utils.InverseLerp(24f, 16f, npc.velocity.Length(), true);

                    Vector2 sinusoidalOffset = Vector2.UnitY.RotatedBy(npc.rotation + sinusoidalRotationOffset) * sinusoidalRotationOffsetFactor;
                    Vector2 ribbonSegmentOffset = Vector2.UnitY.RotatedBy(currentSegmentRotation) * ribbonCompletionRatio * 540f + sinusoidalOffset;
                    ribbonDrawPositions.Add(npc.Center + ribbonSegmentOffset + ribbonOffset);

                    currentSegmentRotation += segmentRotationOffset;
                }
                npc.ModNPC<Apollo>().RibbonTrail.Draw(ribbonDrawPositions, -Main.screenPosition, 66);
            }

            int instanceCount = (int)MathHelper.Lerp(1f, 15f, npc.ModNPC<Apollo>().ChargeComboFlash);
            Color baseInstanceColor = Color.Lerp(lightColor, Color.White, npc.ModNPC<Apollo>().ChargeComboFlash);
            baseInstanceColor.A = (byte)(int)(255f - npc.ModNPC<Apollo>().ChargeComboFlash * 255f);

            spriteBatch.EnterShaderRegion();

            drawInstance(Vector2.Zero, baseInstanceColor);
            if (instanceCount > 1)
            {
                baseInstanceColor *= 0.04f;
                float backAfterimageOffset = MathHelper.SmoothStep(0f, 2f, npc.ModNPC<Apollo>().ChargeComboFlash);
                for (int i = 0; i < instanceCount; i++)
                {
                    Vector2 drawOffset = (MathHelper.TwoPi * i / instanceCount + Main.GlobalTime * 0.8f).ToRotationVector2() * backAfterimageOffset;
                    drawInstance(drawOffset, baseInstanceColor);
                }
            }

            texture = ModContent.GetTexture("CalamityMod/NPCs/ExoMechs/Apollo/ApolloGlow");
            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < numAfterimages; i += 2)
                {
                    Color afterimageColor = npc.GetAlpha(Color.Lerp(lightColor, afterimageBaseColor, 0.5f)) * ((numAfterimages - i) / 15f);
                    Vector2 afterimageCenter = npc.oldPos[i] + frame.Size() * 0.5f - Main.screenPosition;
                    spriteBatch.Draw(texture, afterimageCenter, npc.frame, afterimageColor, npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
                }
            }

            spriteBatch.Draw(texture, center, frame, afterimageBaseColor * npc.Opacity, npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);

            spriteBatch.ExitShaderRegion();

            // Draw a flame trail on the thrusters if needed. This happens during charges.
            if (npc.ModNPC<Apollo>().ChargeComboFlash > 0f)
            {
                for (int direction = -1; direction <= 1; direction++)
                {
                    Vector2 baseDrawOffset = new Vector2(0f, direction == 0f ? 18f : 60f).RotatedBy(npc.rotation);
                    baseDrawOffset += new Vector2(direction * 64f, 0f).RotatedBy(npc.rotation);

                    float backFlameLength = direction == 0f ? 700f : 190f;
                    Vector2 drawStart = npc.Center + baseDrawOffset;
                    Vector2 drawEnd = drawStart - (npc.rotation - MathHelper.PiOver2).ToRotationVector2() * npc.ModNPC<Apollo>().ChargeComboFlash * backFlameLength;
                    Vector2[] drawPositions = new Vector2[]
                    {
                        drawStart,
                        drawEnd
                    };

                    if (direction == 0)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            Vector2 drawOffset = (MathHelper.TwoPi * i / 4f).ToRotationVector2() * 8f;
                            npc.ModNPC<Apollo>().ChargeFlameTrailBig.Draw(drawPositions, drawOffset - Main.screenPosition, 70);
                        }
                    }
                    else
                        npc.ModNPC<Apollo>().ChargeFlameTrail.Draw(drawPositions, -Main.screenPosition, 70);
                }
            }

            return false;
        }
        #endregion Frames and Drawcode
    }
}
