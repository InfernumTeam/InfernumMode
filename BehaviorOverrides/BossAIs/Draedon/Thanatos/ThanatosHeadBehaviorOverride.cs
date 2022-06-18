using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.Particles;
using CalamityMod.Skies;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares;
using InfernumMode.OverridingSystem;
using InfernumMode.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Thanatos
{
    public class ThanatosHeadBehaviorOverride : NPCBehaviorOverride
    {
        public enum ThanatosFrameType
        {
            Closed,
            Open
        }

        public enum ThanatosHeadAttackType
        {
            AggressiveCharge,
            TopwardSlam,
            LaserBarrage,
            ExoBomb,
            ExoLightBarrage,
            RefractionRotorRays,
            MaximumOverdrive
        }

        public override int NPCOverrideType => ModContent.NPCType<ThanatosHead>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame | NPCOverrideContext.NPCPreDraw;

        public const int SegmentCount = 100;
        public const int TransitionSoundDelay = 80;
        public const float OpenSegmentDR = 0f;
        public const float ClosedSegmentDR = 0.98f;

        public override bool PreAI(NPC npc)
        {
            // Define the life ratio.
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Define the whoAmI variable.
            CalamityGlobalNPC.draedonExoMechWorm = npc.whoAmI;

            // Reset frame states.
            ref float frameType = ref npc.localAI[0];
            frameType = (int)ThanatosFrameType.Closed;

            // Reset damage.
            npc.defDamage = 775;
            npc.damage = npc.defDamage;

            // Define attack variables.
            bool performingDeathAnimation = ExoMechAIUtilities.PerformingDeathAnimation(npc);
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float segmentsSpawned = ref npc.ai[2];
            ref float hasSummonedComplementMech = ref npc.Infernum().ExtraAI[ExoMechManagement.HasSummonedComplementMechIndex];
            ref float complementMechIndex = ref npc.Infernum().ExtraAI[ExoMechManagement.ComplementMechIndexIndex];
            ref float wasNotInitialSummon = ref npc.Infernum().ExtraAI[ExoMechManagement.WasNotInitialSummonIndex];
            ref float finalMechIndex = ref npc.Infernum().ExtraAI[ExoMechManagement.FinalMechIndexIndex];
            ref float attackDelay = ref npc.Infernum().ExtraAI[ExoMechManagement.Thanatos_AttackDelayIndex];
            ref float finalPhaseAnimationTime = ref npc.Infernum().ExtraAI[ExoMechManagement.FinalPhaseTimerIndex];
            ref float secondComboPhaseResistanceBoostFlag = ref npc.Infernum().ExtraAI[17];
            ref float deathAnimationTimer = ref npc.Infernum().ExtraAI[ExoMechManagement.DeathAnimationTimerIndex];

            NPC initialMech = ExoMechManagement.FindInitialMech();
            NPC complementMech = complementMechIndex >= 0 && Main.npc[(int)complementMechIndex].active && Utilities.IsExoMech(Main.npc[(int)complementMechIndex]) ? Main.npc[(int)complementMechIndex] : null;
            NPC finalMech = ExoMechManagement.FindFinalMech();

            // Define rotation and direction.
            int oldDirection = npc.direction;
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
            npc.direction = npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
            if (oldDirection != npc.direction)
                npc.netUpdate = true;

            // Create segments.
            if (Main.netMode != NetmodeID.MultiplayerClient && segmentsSpawned == 0f)
            {
                int previous = npc.whoAmI;
                for (int i = 0; i < SegmentCount; i++)
                {
                    int lol;
                    if (i < SegmentCount - 1)
                    {
                        if (i % 2 == 0)
                            lol = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<ThanatosBody1>(), npc.whoAmI);
                        else
                            lol = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<ThanatosBody2>(), npc.whoAmI);
                    }
                    else
                        lol = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<ThanatosTail>(), npc.whoAmI);

                    Main.npc[lol].realLife = npc.whoAmI;
                    Main.npc[lol].ai[0] = i;
                    Main.npc[lol].ai[1] = previous;
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, lol, 0f, 0f, 0f, 0);
                    previous = lol;
                }

                npc.ai[0] = (int)ThanatosHeadAttackType.LaserBarrage;
                finalMechIndex = -1f;
                complementMechIndex = -1f;
                segmentsSpawned++;
                npc.netUpdate = true;
            }

            // Summon the complement mech and reset things once ready.
            if (hasSummonedComplementMech == 0f && lifeRatio < ExoMechManagement.Phase4LifeRatio)
            {
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

            // Get a target.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Become invincible if the complement mech is at high enough health or if in the middle of a death animation.
            npc.dontTakeDamage = ExoMechAIUtilities.PerformingDeathAnimation(npc);
            if (complementMechIndex >= 0 && Main.npc[(int)complementMechIndex].active && Main.npc[(int)complementMechIndex].life > Main.npc[(int)complementMechIndex].lifeMax * ExoMechManagement.ComplementMechInvincibilityThreshold)
                npc.dontTakeDamage = true;

            // Become invincible and disappear if necessary.
            npc.Calamity().newAI[1] = 0f;
            if (ExoMechAIUtilities.ShouldExoMechVanish(npc))
            {
                npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.08f, 0f, 1f);
                if (npc.Opacity <= 0f)
                    npc.Center = target.Center + Vector2.UnitY * 1600f;

                attackTimer = 0f;
                attackState = (int)ThanatosHeadAttackType.AggressiveCharge;
                npc.Calamity().newAI[1] = (int)ThanatosHead.SecondaryPhase.PassiveAndImmune;
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

            // Have a brief period of immortality before attacking to allow for time to uncoil.
            if (attackDelay < 240f && !performingDeathAnimation)
            {
                npc.dontTakeDamage = true;
                npc.damage = 0;
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                attackDelay++;
                DoProjectileShootInterceptionMovement(npc, target, Utils.InverseLerp(270f, 100f, attackDelay, true) * 2.5f);
                return false;
            }

            // Handle the final phase transition.
            if (finalPhaseAnimationTime < ExoMechManagement.FinalPhaseTransitionTime && ExoMechManagement.CurrentThanatosPhase >= 6 && !ExoMechManagement.ExoMechIsPerformingDeathAnimation)
            {
                frameType = (int)ThanatosFrameType.Closed;
                attackState = (int)ThanatosHeadAttackType.AggressiveCharge;
                finalPhaseAnimationTime++;
                npc.damage = 0;
                npc.dontTakeDamage = true;
                DoBehavior_DoFinalPhaseTransition(npc, target, finalPhaseAnimationTime);

                // The delay before returning is to ensure that DR code is executed that reflects the fact that Thanatos' head segment is closed.
                if (finalPhaseAnimationTime >= 3f)
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

            if (((finalMech != null && finalMech.Opacity > 0f) || ExoMechManagement.CurrentThanatosPhase >= 6) && attackState >= 100f)
            {
                attackTimer = 0f;
                attackState = 0f;
                npc.netUpdate = true;
            }

            // Handle smoke venting and open/closed DR.
            npc.Calamity().DR = ClosedSegmentDR;
            npc.Calamity().unbreakableDR = true;
            npc.chaseable = false;
            npc.defense = 0;
            npc.takenDamageMultiplier = 1f;
            npc.ModNPC<ThanatosHead>().SmokeDrawer.ParticleSpawnRate = 9999999;

            // Become vulnerable on the map.
            typeof(ThanatosHead).GetField("vulnerable", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(npc.modNPC, frameType == (int)ThanatosFrameType.Open);

            if (!performingDeathAnimation)
            {
                switch ((ThanatosHeadAttackType)(int)attackState)
                {
                    case ThanatosHeadAttackType.AggressiveCharge:
                        DoBehavior_AggressiveCharge(npc, target, ref attackTimer, ref frameType);
                        break;
                    case ThanatosHeadAttackType.TopwardSlam:
                        DoBehavior_TopwardSlam(npc, target, ref attackTimer, ref frameType);
                        break;
                    case ThanatosHeadAttackType.LaserBarrage:
                        DoBehavior_LaserBarrage(npc, target, ref attackTimer, ref frameType);
                        break;
                    case ThanatosHeadAttackType.ExoBomb:
                        DoBehavior_ExoBomb(npc, target, ref attackTimer, ref frameType);
                        break;
                    case ThanatosHeadAttackType.RefractionRotorRays:
                        DoBehavior_RefractionRotorRays(npc, target, ref attackTimer, ref frameType);
                        break;
                    case ThanatosHeadAttackType.ExoLightBarrage:
                        DoBehavior_ExoLightBarrage(npc, target, ref attackTimer, ref frameType);
                        break;
                    case ThanatosHeadAttackType.MaximumOverdrive:
                        DoBehavior_MaximumOverdrive(npc, target, ref attackTimer, ref frameType);
                        break;
                }

                if (ExoMechComboAttackContent.UseThanatosAresComboAttack(npc, ref attackTimer, ref frameType))
                    SelectNextAttack(npc);
            }
            else
            {
                DoBehavior_DeathAnimation(npc, target, ref deathAnimationTimer, ref frameType);
                deathAnimationTimer++;
            }

            if (frameType == (int)ThanatosFrameType.Open)
            {
                // Emit light.
                Lighting.AddLight(npc.Center, 0.35f * npc.Opacity, 0.05f * npc.Opacity, 0.05f * npc.Opacity);

                // Emit smoke.
                npc.takenDamageMultiplier = 103.184f;
                if (npc.Opacity > 0.6f)
                {
                    npc.ModNPC<ThanatosHead>().SmokeDrawer.BaseMoveRotation = npc.rotation - MathHelper.PiOver2;
                    npc.ModNPC<ThanatosHead>().SmokeDrawer.ParticleSpawnRate = 5;
                }
                npc.Calamity().DR = OpenSegmentDR - 0.125f;
                npc.Calamity().unbreakableDR = false;
                npc.chaseable = true;
            }
            // Emit light.
            else
                Lighting.AddLight(npc.Center, 0.05f * npc.Opacity, 0.2f * npc.Opacity, 0.2f * npc.Opacity);

            npc.ModNPC<ThanatosHead>().SmokeDrawer.Update();

            secondComboPhaseResistanceBoostFlag = 0f;
            if (ExoMechManagement.ShouldHaveSecondComboPhaseResistance(npc) && npc == complementMech)
            {
                secondComboPhaseResistanceBoostFlag = 1f;
                npc.takenDamageMultiplier *= 0.5f;
            }

            attackTimer++;

            return false;
        }

        public static void DoBehavior_DeathAnimation(NPC npc, Player target, ref float deathAnimationTimer, ref float frameType)
        {
            bool isHead = npc.type == ModContent.NPCType<ThanatosHead>();
            bool closeToDying = deathAnimationTimer >= 180f;

            // Close the HP bar.
            npc.Calamity().ShouldCloseHPBar = true;

            // Use close to the minimum HP.
            npc.life = 50000;

            // Disable contact damage.
            npc.dontTakeDamage = true;
            npc.damage = 0;

            // Slowly attempt to approach the target.
            if (isHead && !closeToDying)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * 6f, 0.056f);
                if (npc.velocity.Length() > 10f)
                    npc.velocity *= 0.96f;
            }

            // Release bursts of energy sometimes.
            if (deathAnimationTimer >= 35f && !closeToDying && Main.rand.NextBool(800))
            {
                Main.PlaySound(SoundID.DD2_KoboldExplosion, npc.Center);
                GeneralParticleHandler.SpawnParticle(new PulseRing(npc.Center, Vector2.Zero, Color.Red, 0f, 3.5f, 50));

                for (int i = 0; i < 8; i++)
                {
                    Vector2 sparkVelocity = -Vector2.UnitY.RotatedByRandom(1.23f) * Main.rand.NextFloat(6f, 14f);
                    GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(npc.Center, sparkVelocity, 1f, Color.Red, 55, 1f, 5f));
                }
            }

            if (deathAnimationTimer == 240f && isHead)
            {
                int thanatosHeadID = ModContent.NPCType<ThanatosHead>();
                int thanatosBodyID = ModContent.NPCType<ThanatosBody2>();

                // Play an explosion sound.
                var sound = Main.PlaySound(InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/WyrmElectricCharge"), npc.Center);
                if (sound != null)
                    CalamityUtils.SafeVolumeChange(ref sound, 1.75f);

                Color[] explosionColorPalette = (Color[])CalamityUtils.ExoPalette.Clone();
                for (int j = 0; j < explosionColorPalette.Length; j++)
                    explosionColorPalette[j] = Color.Lerp(explosionColorPalette[j], Color.Red, 0.3f);

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (!Main.npc[i].active)
                        continue;

                    if ((Main.npc[i].type == thanatosBodyID && i % 3 == 0) || Main.npc[i].type == thanatosHeadID)
                        GeneralParticleHandler.SpawnParticle(new ElectricExplosionRing(Main.npc[i].Center, Vector2.Zero, explosionColorPalette, 2.1f, 90, 0.4f));
                }
            }

            if (deathAnimationTimer >= 260f)
            {
                npc.life = 0;
                npc.HitEffect();
                npc.StrikeNPC(10, 0f, 1);
                npc.checkDead();
            }

            // Open vents (pretty sus ngl).
            frameType = (int)ThanatosFrameType.Open;
        }

        public static void DoBehavior_AggressiveCharge(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            // Decide frames.
            frameType = (int)ThanatosFrameType.Open;

            // Handle movement.
            DoAggressiveChargeMovement(npc, target, attackTimer, 1f);

            // Play a sound prior to switching attacks.
            if (attackTimer == 720f - TransitionSoundDelay)
                Main.PlaySound(InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ThanatosTransition"), target.Center);

            if (attackTimer > 720f)
                SelectNextAttack(npc);
        }

        // TODO -- This attack is quite janky at the moment. Consider changing it before adding it back to the attack pool.
        public static void DoBehavior_TopwardSlam(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            // Decide frames.
            frameType = (int)ThanatosFrameType.Open;

            int hoverRedirectTime = 270;
            float redirectSpeedMultiplier = 1f;
            float initialChargeSpeed = 36f;
            float maxChargeSpeed = 62f;
            int timeToReachMaxChargeSpeed = 30;
            int chargeTime = 56;
            int chargeCount = 4;
            bool canFireSparks = false;

            ref float chargeCounter = ref npc.Infernum().ExtraAI[0];

            if (ExoMechManagement.CurrentThanatosPhase >= 2)
            {
                hoverRedirectTime -= 40;
                redirectSpeedMultiplier += 0.075f;
                initialChargeSpeed += 5f;
                maxChargeSpeed += 3f;
            }
            if (ExoMechManagement.CurrentThanatosPhase >= 3)
            {
                redirectSpeedMultiplier += 0.1f;
                initialChargeSpeed += 3f;
                maxChargeSpeed += 3.5f;
                chargeTime -= 8;
                canFireSparks = true;
            }
            if (ExoMechManagement.CurrentThanatosPhase >= 5)
            {
                timeToReachMaxChargeSpeed -= 5;
                redirectSpeedMultiplier += 0.1f;
                initialChargeSpeed += 6f;
                maxChargeSpeed += 4.5f;
                chargeTime -= 8;
            }
            if (ExoMechManagement.CurrentThanatosPhase >= 6)
            {
                timeToReachMaxChargeSpeed -= 6;
                redirectSpeedMultiplier += 0.1f;
                initialChargeSpeed += 3.5f;
                maxChargeSpeed += 5f;
                chargeTime -= 7;
                chargeCount++;
            }

            float chargeAcceleration = (float)Math.Pow(maxChargeSpeed / initialChargeSpeed, 1D / timeToReachMaxChargeSpeed);
            Vector2 hoverOffset = new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 500f, -400f);
            Vector2 hoverDestination = target.Center + hoverOffset;

            // Attempt to get into position for a charge.
            if (attackTimer < hoverRedirectTime)
            {
                float idealHoverSpeed = MathHelper.Lerp(43.5f, 80f, attackTimer / hoverRedirectTime);
                idealHoverSpeed *= Utils.InverseLerp(35f, 300f, npc.Distance(target.Center), true) * redirectSpeedMultiplier;

                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * MathHelper.Lerp(npc.velocity.Length(), idealHoverSpeed, 0.135f);
                npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), 0.024f, true) * idealVelocity.Length();
                npc.velocity = npc.velocity.MoveTowards(idealVelocity, redirectSpeedMultiplier * 10f);

                // Play a telegraph sound.
                if (npc.WithinRange(hoverDestination, 1400f) && attackTimer > 35f && attackTimer < hoverRedirectTime - 90f)
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(Terraria.ModLoader.SoundType.Item, "Sounds/Item/LargeWeaponFire"), npc.Center);
                    attackTimer = hoverRedirectTime - 90f;
                    npc.netUpdate = true;
                }

                // Stop hovering if close to the hover destination and prepare the charge.
                if (npc.WithinRange(hoverDestination, 130f) && attackTimer > 115f && npc.velocity.AngleBetween(idealVelocity) < 0.44f)
                {
                    attackTimer = hoverRedirectTime;
                    idealVelocity = npc.SafeDirectionTo(target.Center + target.velocity * 12f) * initialChargeSpeed;
                    npc.velocity = npc.velocity.MoveTowards(idealVelocity, 7.5f).RotateTowards(idealVelocity.ToRotation(), MathHelper.Pi * 0.66f);
                    npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * initialChargeSpeed;

                    npc.netUpdate = true;
                }
            }

            // Play a telegraph sound and release sparks after the charge begins.
            if (attackTimer == hoverRedirectTime + 1f)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(Terraria.ModLoader.SoundType.Item, "Sounds/Item/PlasmaGrenadeExplosion"), target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient && canFireSparks)
                {
                    for (int i = 0; i < 18; i++)
                    {
                        Vector2 sparkVelocity = (MathHelper.TwoPi * i / 18f).ToRotationVector2() * 24f;
                        Utilities.NewProjectileBetter(npc.Center, sparkVelocity, ModContent.ProjectileType<LaserSpark>(), 500, 0f);
                    }
                }
            }

            // Accelerate after the charge has begun.
            if (attackTimer > hoverRedirectTime && npc.velocity.Length() < maxChargeSpeed)
                npc.velocity *= chargeAcceleration;

            // Play a sound prior to switching attacks.
            if (chargeCounter >= chargeCount - 1f && attackTimer == hoverRedirectTime + 1f)
                Main.PlaySound(InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ThanatosTransition"), target.Center);

            if (attackTimer > hoverRedirectTime + chargeTime)
            {
                npc.velocity = npc.velocity.ClampMagnitude(0f, 35f) * 0.56f;
                chargeCounter++;
                if (chargeCounter >= chargeCount)
                    SelectNextAttack(npc);

                attackTimer = 0f;
            }
        }

        public static void DoBehavior_LaserBarrage(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            // Decide frames.
            frameType = (int)ThanatosFrameType.Closed;

            int segmentShootDelay = 100;
            ref float totalSegmentsToFire = ref npc.Infernum().ExtraAI[0];
            ref float segmentFireTime = ref npc.Infernum().ExtraAI[1];
            ref float segmentFireCountdown = ref npc.Infernum().ExtraAI[2];

            if (ExoMechManagement.CurrentThanatosPhase == 4)
                segmentShootDelay += 60;

            // Temporarily disable damage.
            if (attackTimer < 150f)
                npc.damage = 0;

            // Do movement.
            DoProjectileShootInterceptionMovement(npc, target);

            // Select segment shoot attributes.
            if (attackTimer % segmentShootDelay == segmentShootDelay - 1f)
            {
                totalSegmentsToFire = 20f;
                segmentFireTime = 75f;

                if (ExoMechManagement.CurrentThanatosPhase >= 2)
                    totalSegmentsToFire += 3f;
                if (ExoMechManagement.CurrentThanatosPhase >= 3)
                    totalSegmentsToFire += 3f;
                if (ExoMechManagement.CurrentThanatosPhase >= 5)
                {
                    totalSegmentsToFire += 3f;
                    segmentFireTime += 10f;
                }
                if (ExoMechManagement.CurrentThanatosPhase >= 6)
                {
                    totalSegmentsToFire += 5f;
                    segmentFireTime += 8f;
                }

                segmentFireCountdown = segmentFireTime;
                npc.netUpdate = true;
            }

            if (segmentFireCountdown > 0f)
                segmentFireCountdown--;

            // Play a sound prior to switching attacks.
            if (attackTimer == 600f - TransitionSoundDelay)
                Main.PlaySound(InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ThanatosTransition"), target.Center);

            if (attackTimer > 600f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_ExoBomb(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            // Decide frames.
            frameType = (int)ThanatosFrameType.Open;

            int initialRedirectTime = 360;
            int spinBufferTime = 300;
            int intendedSpinTime = 105;
            int postSpinChargeTime = 165;
            float chargeSpeed = 37f;
            float spinSpeed = 51f;
            float totalRotations = 1f;

            if (ExoMechManagement.CurrentThanatosPhase >= 2)
            {
                intendedSpinTime -= 15;
                chargeSpeed += 6f;
                spinSpeed += 5f;
            }
            if (ExoMechManagement.CurrentThanatosPhase >= 3)
            {
                intendedSpinTime -= 15;
                chargeSpeed += 4f;
                spinSpeed += 5f;
            }
            if (ExoMechManagement.CurrentThanatosPhase >= 5)
            {
                intendedSpinTime -= 15;
                spinSpeed += 5f;
            }
            if (ExoMechManagement.CurrentThanatosPhase >= 6)
            {
                intendedSpinTime -= 25;
                chargeSpeed += 8f;
                spinSpeed += 5f;
            }

            Vector2 hoverOffset = new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 800f, -340f);
            Vector2 hoverDestination = target.Center + hoverOffset;
            ref float spinTime = ref npc.Infernum().ExtraAI[0];

            // Initialize the spin time. This is done via a variable because it's possible that the spin time will otherwise switch if Thanatos changes subphases mid-attack, potentially
            // resulting in strange behaviors.
            if (spinTime == 0f)
            {
                spinTime = intendedSpinTime;
                npc.netUpdate = true;
            }

            // Attempt to get into position for a charge.
            if (attackTimer < initialRedirectTime)
            {
                // Disable contact damage.
                npc.damage = 0;

                float idealHoverSpeed = MathHelper.Lerp(43.5f, 72.5f, attackTimer / initialRedirectTime);
                idealHoverSpeed *= Utils.InverseLerp(35f, 300f, npc.Distance(target.Center), true);

                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * MathHelper.Lerp(npc.velocity.Length(), idealHoverSpeed, 0.135f);
                npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), 0.045f, true) * idealVelocity.Length();
                npc.velocity = npc.velocity.MoveTowards(idealVelocity, 3f);

                // Stop hovering if close to the hover destination and prepare the spin.
                if (npc.WithinRange(hoverDestination, 90f) && attackTimer > 45f)
                {
                    attackTimer = initialRedirectTime;
                    npc.velocity = npc.SafeDirectionTo(target.Center) * -spinSpeed;
                    npc.netUpdate = true;
                }
            }

            // Create the exo bomb.
            if (attackTimer == initialRedirectTime + 1f)
            {
                Vector2 bombSpawnPosition = npc.Center + npc.velocity.RotatedBy(MathHelper.PiOver2) * spinTime / totalRotations / MathHelper.TwoPi;
                int bomb = Utilities.NewProjectileBetter(bombSpawnPosition, Vector2.Zero, ModContent.ProjectileType<ExolaserBomb>(), 1000, 0f);
                if (Main.projectile.IndexInRange(bomb))
                    Main.projectile[bomb].ModProjectile<ExolaserBomb>().GrowTime = (int)spinTime;
            }

            // Spin.
            if (attackTimer >= initialRedirectTime && attackTimer < initialRedirectTime + spinBufferTime)
            {
                npc.velocity = npc.velocity.RotatedBy(MathHelper.TwoPi * totalRotations / spinTime);

                if (attackTimer >= initialRedirectTime + spinTime && npc.velocity.AngleBetween(npc.SafeDirectionTo(target.Center)) < 0.1f)
                {
                    npc.velocity = npc.SafeDirectionTo(target.Center) * npc.velocity.Length();

                    float bombSpeed = MathHelper.Lerp(15f, 35f, Utils.InverseLerp(750f, 1500f, npc.Distance(target.Center), true));
                    foreach (Projectile exoBomb in Utilities.AllProjectilesByID(ModContent.ProjectileType<ExolaserBomb>()))
                    {
                        exoBomb.velocity = exoBomb.SafeDirectionTo(target.Center + target.velocity * 25f) * bombSpeed;
                        exoBomb.netUpdate = true;
                    }
                    attackTimer = initialRedirectTime + spinBufferTime;
                    npc.netUpdate = true;
                }
            }

            // Charge.
            if (attackTimer >= initialRedirectTime + spinBufferTime)
                npc.velocity *= npc.velocity.Length() > chargeSpeed ? 0.98f : 1.02f;

            // Play a sound prior to switching attacks.
            if (attackTimer == initialRedirectTime + spinBufferTime + postSpinChargeTime - TransitionSoundDelay)
                Main.PlaySound(InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ThanatosTransition"), target.Center);

            if (attackTimer == initialRedirectTime + spinBufferTime + postSpinChargeTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_RefractionRotorRays(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            // Decide frames.
            frameType = (int)ThanatosFrameType.Open;

            int slowdownTime = 100;
            int chargePreparationTime = 60;
            int redirectTime = 90;
            int chargeTime = 45;
            int attackShiftDelay = 120;
            int lasersPerRotor = 5;
            int rotorReleaseRate = 7;
            int chargeCount = 2;
            float initialChargeSpeed = 13f;
            float finalChargeSpeed = 26f;
            float rotorSpeed = 25f;
            ref float chargeCounter = ref npc.Infernum().ExtraAI[0];

            if (ExoMechManagement.CurrentThanatosPhase >= 2)
            {
                slowdownTime -= 15;
                redirectTime -= 5;
            }

            if (ExoMechManagement.CurrentThanatosPhase >= 3)
            {
                slowdownTime -= 10;
                chargePreparationTime -= 8;
                redirectTime -= 5;
                chargeTime -= 5;
                finalChargeSpeed += 4f;
            }

            if (ExoMechManagement.CurrentThanatosPhase >= 5)
            {
                slowdownTime -= 12;
                chargePreparationTime -= 8;
                redirectTime -= 5;
                lasersPerRotor++;
                rotorReleaseRate--;
                chargeCount++;
                finalChargeSpeed += 4f;
            }

            if (ExoMechManagement.CurrentThanatosPhase >= 6)
            {
                slowdownTime -= 12;
                chargePreparationTime -= 8;
                redirectTime -= 5;
                chargeTime -= 8;
                lasersPerRotor++;
                finalChargeSpeed += 4f;
            }

            // Approach the player at an increasingly slow speed.
            if (attackTimer < slowdownTime)
            {
                float speedMultiplier = MathHelper.Lerp(0.75f, 0.385f, attackTimer / slowdownTime);
                DoAggressiveChargeMovement(npc, target, attackTimer, speedMultiplier);
            }

            // Continue moving in the current direction, but continue slowing down.
            if (attackTimer >= slowdownTime && attackTimer < slowdownTime + chargePreparationTime)
                npc.velocity = (npc.velocity * 0.96f).ClampMagnitude(8f, 27f);

            // Play a telegraph sound to alert the player of the impending charge.
            if (attackTimer == slowdownTime + chargePreparationTime / 2)
                Main.PlaySound(InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ExoMechImpendingDeathSound"), target.Center);

            // Begin the charge.
            if (attackTimer >= slowdownTime + chargePreparationTime && attackTimer < slowdownTime + chargePreparationTime + redirectTime)
            {
                Vector2 idealVelocity = npc.SafeDirectionTo(target.Center) * initialChargeSpeed;
                npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), 0.075f).MoveTowards(idealVelocity, 2.75f);

                // Sometimes charge early if aimed at the target and the redirect is more than halfway done.
                if (attackTimer >= chargePreparationTime + redirectTime * 0.5f && npc.velocity.AngleBetween(idealVelocity) < 0.15f)
                {
                    npc.velocity = idealVelocity;
                    attackTimer = slowdownTime + chargePreparationTime + redirectTime;
                    npc.netUpdate = true;
                }
            }

            // Charge and release refraction rotors.
            if (attackTimer >= slowdownTime + chargePreparationTime + redirectTime)
            {
                npc.velocity = npc.velocity.MoveTowards(npc.velocity.SafeNormalize(Vector2.UnitY) * finalChargeSpeed, 2f);

                if (attackTimer % rotorReleaseRate == rotorReleaseRate - 1f)
                {
                    var segments = (from n in Main.npc.Take(Main.maxNPCs)
                                    where n.active && n.type == ModContent.NPCType<ThanatosBody1>() && !n.WithinRange(target.Center, 400f) && n.WithinRange(target.Center, 1200f)
                                    orderby n.Distance(target.Center)
                                    select n).ToList();

                    if (Main.netMode != NetmodeID.MultiplayerClient && segments.Count > 1)
                    {
                        NPC segmentToFireFrom = segments[Main.rand.Next(0, segments.Count / 3)];
                        Vector2 rotorShootVelocity = segmentToFireFrom.SafeDirectionTo(target.Center).RotatedByRandom(1.6f) * rotorSpeed;
                        int rotor = Utilities.NewProjectileBetter(segmentToFireFrom.Center, rotorShootVelocity, ModContent.ProjectileType<RefractionRotor>(), 0, 0f);
                        if (Main.projectile.IndexInRange(rotor))
                            Main.projectile[rotor].ai[0] = lasersPerRotor;
                    }
                }
            }

            // Play a sound prior to switching attacks.
            if (attackTimer == slowdownTime + chargePreparationTime + redirectTime + chargeTime + attackShiftDelay - TransitionSoundDelay && chargeCounter >= chargeCount - 1f)
                Main.PlaySound(InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ThanatosTransition"), target.Center);

            // Perform the attack again if necessary.
            if (attackTimer >= slowdownTime + chargePreparationTime + redirectTime + chargeTime + attackShiftDelay)
            {
                chargeCounter++;
                attackTimer = 0f;
                if (chargeCounter >= chargeCount)
                    SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_ExoLightBarrage(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            // Decide frames.
            frameType = (int)ThanatosFrameType.Open;

            // Disable contact damage.
            npc.damage = 0;

            int initialRedirectTime = 360;
            int lightTelegraphTime = 105;
            int lightLaserFireDelay = 20;
            int lightLaserShootTime = LightOverloadRay.Lifetime;
            int redirectCount = 3;
            float pointAtTargetSpeed = 2f;
            float lightRaySpreadDegrees = 125f;
            ref float hoverOffsetDirection = ref npc.Infernum().ExtraAI[0];
            ref float redirectCounter = ref npc.Infernum().ExtraAI[3];

            // Initialize a hover offset direction.
            if (hoverOffsetDirection == 0f)
            {
                hoverOffsetDirection = Main.rand.Next(4) * MathHelper.TwoPi / 4f + MathHelper.PiOver4;
                npc.netUpdate = true;
            }

            int totalLightRays = (int)(lightRaySpreadDegrees * 0.257f);
            float lightRaySpread = MathHelper.ToRadians(lightRaySpreadDegrees);
            Vector2 outerHoverOffset = hoverOffsetDirection.ToRotationVector2() * 1200f;
            Vector2 outerHoverDestination = target.Center + outerHoverOffset;

            // Clamp Thanatos' position to stay in the world.
            // This is very important, as the telegraph might simply not appear if Thanatos is too high up.
            if (npc.position.Y < 600f)
                npc.position.Y = 600f;

            // Attempt to get into position for the light attack.
            if (attackTimer < initialRedirectTime)
            {
                float idealHoverSpeed = MathHelper.Lerp(43.5f, 72.5f, attackTimer / initialRedirectTime);
                idealHoverSpeed *= Utils.InverseLerp(35f, 300f, npc.Distance(target.Center), true);

                Vector2 idealVelocity = npc.SafeDirectionTo(outerHoverDestination) * MathHelper.Lerp(npc.velocity.Length(), idealHoverSpeed, 0.135f);
                npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), 0.045f, true) * idealVelocity.Length();
                npc.velocity = npc.velocity.MoveTowards(idealVelocity, 8f);

                // Stop hovering if close to the hover destination and prepare to move towards the target.
                if (npc.WithinRange(outerHoverDestination, 90f) && attackTimer > 45f)
                {
                    attackTimer = initialRedirectTime;
                    npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * npc.velocity.Length(), 0.85f);
                    npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(npc.velocity.Length(), pointAtTargetSpeed, 0.4f);
                    npc.netUpdate = true;
                }
            }

            // Slow down, move towards the target (while maintaining the current direction) and create a laser telegraph.
            if (attackTimer >= initialRedirectTime && attackTimer < initialRedirectTime + lightTelegraphTime)
            {
                // Create light telegraphs.
                if (attackTimer == initialRedirectTime + 1f)
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(Terraria.ModLoader.SoundType.Item, "Sounds/Item/CrystylCharge"), npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < totalLightRays; i++)
                        {
                            float lightRayAngularOffset = MathHelper.Lerp(-lightRaySpread, lightRaySpread, i / (float)(totalLightRays - 1f));

                            int lightRayTelegraph = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<LightRayTelegraph>(), 0, 0f);
                            if (Main.projectile.IndexInRange(lightRayTelegraph))
                            {
                                Main.projectile[lightRayTelegraph].ModProjectile<LightRayTelegraph>().RayHue = i / (float)(totalLightRays - 1f);
                                Main.projectile[lightRayTelegraph].ModProjectile<LightRayTelegraph>().MaximumSpread = lightRayAngularOffset;
                                Main.projectile[lightRayTelegraph].ModProjectile<LightRayTelegraph>().Lifetime = lightTelegraphTime;
                                Main.projectile[lightRayTelegraph].netUpdate = true;
                            }
                        }
                    }
                }

                // Approach the ideal position.
                npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(npc.velocity.Length(), pointAtTargetSpeed, 0.05f);
            }

            // Create a massive laser.
            if (attackTimer == initialRedirectTime + lightTelegraphTime + lightLaserFireDelay)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(Terraria.ModLoader.SoundType.Item, "Sounds/Item/TeslaCannonFire"), npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<OverloadBoom>(), 0, 0f);

                    int light = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<LightOverloadRay>(), 1200, 0f);
                    if (Main.projectile.IndexInRange(light))
                        Main.projectile[light].ModProjectile<LightOverloadRay>().LaserSpread = lightRaySpread * 0.53f;
                }
            }

            // Create explosions that make sparks after the lasers are fired.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= initialRedirectTime + lightTelegraphTime + lightLaserFireDelay && attackTimer % 12f == 11f)
            {
                Vector2 targetDirection = target.velocity.SafeNormalize(Main.rand.NextVector2Unit());
                Vector2 spawnPosition = target.Center - targetDirection.RotatedByRandom(1.1f) * Main.rand.NextFloat(325f, 650f) * new Vector2(1f, 0.6f);
                Utilities.NewProjectileBetter(spawnPosition, Vector2.Zero, ModContent.ProjectileType<AresBeamExplosion>(), 550, 0f);
            }

            // Play a sound prior to switching attacks.
            if (attackTimer == initialRedirectTime + lightTelegraphTime + lightLaserShootTime + lightLaserFireDelay - TransitionSoundDelay && redirectCounter >= redirectCount - 1f)
                Main.PlaySound(InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ThanatosTransition"), target.Center);

            if (attackTimer >= initialRedirectTime + lightTelegraphTime + lightLaserShootTime + lightLaserFireDelay)
            {
                attackTimer = 0f;
                hoverOffsetDirection += MathHelper.PiOver2;
                redirectCounter++;
                if (redirectCounter >= redirectCount)
                    SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_MaximumOverdrive(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            // Dash or die.
            npc.damage = 900;

            int attackTime = 720;
            int cooloffTime = 360;
            float chargeSpeedInterpolant = Utils.InverseLerp(0f, 45f, attackTimer, true) * Utils.InverseLerp(attackTime, attackTime - 45f, attackTimer, true);
            float chargeSpeedFactor = MathHelper.Lerp(0.3f, 1.3f, chargeSpeedInterpolant);

            ref float coolingOff = ref npc.Infernum().ExtraAI[0];

            // Play a telegraph before the attack begins as a warning.
            if (attackTimer == 1f)
                Main.PlaySound(InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ExoMechImpendingDeathSound"), target.Center);

            // Decide frames.
            frameType = (int)ThanatosFrameType.Open;

            // Decide whether to cool off or not.
            coolingOff = (attackTimer > attackTime - 12f).ToInt();

            // Handle movement.
            DoAggressiveChargeMovement(npc, target, attackTimer, chargeSpeedFactor);

            // Periodically release lasers from the sides.
            if (Main.netMode != NetmodeID.MultiplayerClient && coolingOff == 0f && attackTimer % 60f == 59f)
            {
                for (int i = 0; i < 3; i++)
                {
                    int type = ModContent.ProjectileType<DetatchedThanatosLaser>();
                    float shootSpeed = 19f;
                    Vector2 projectileDestination = target.Center;
                    Vector2 spawnPosition = target.Center + Main.rand.NextVector2CircularEdge(1500f, 1500f);
                    int laser = Utilities.NewProjectileBetter(spawnPosition, npc.SafeDirectionTo(projectileDestination) * shootSpeed, type, 600, 0f, Main.myPlayer, 0f, npc.whoAmI);
                    if (Main.projectile.IndexInRange(laser))
                    {
                        Main.projectile[laser].owner = npc.target;
                        Main.projectile[laser].ModProjectile<DetatchedThanatosLaser>().InitialDestination = projectileDestination;
                    }
                }
            }

            // Create lightning bolts in the sky.
            if (Main.netMode != NetmodeID.Server && attackTimer % 3f == 2f)
                ExoMechsSky.CreateLightningBolt();

            // Play a sound prior to switching attacks.
            if (attackTimer == attackTime + cooloffTime - TransitionSoundDelay)
                Main.PlaySound(InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ThanatosTransition"), target.Center);

            if (attackTimer > attackTime + cooloffTime)
                SelectNextAttack(npc);
        }

        public static void DoProjectileShootInterceptionMovement(NPC npc, Player target, float speedMultiplier = 1f)
        {
            // Attempt to intercept the target.
            Vector2 hoverDestination = target.Center + target.velocity.SafeNormalize(Vector2.UnitX * target.direction) * new Vector2(675f, 550f);
            hoverDestination.Y -= 550f;

            float idealFlySpeed = 17f;

            if (ExoMechManagement.CurrentThanatosPhase == 4)
                idealFlySpeed *= 0.7f;
            else
            {
                if (ExoMechManagement.CurrentThanatosPhase >= 2)
                    idealFlySpeed *= 1.2f;
            }
            if (ExoMechManagement.CurrentThanatosPhase >= 3)
                idealFlySpeed *= 1.2f;
            if (ExoMechManagement.CurrentThanatosPhase >= 5)
                idealFlySpeed *= 1.225f;

            idealFlySpeed += npc.Distance(target.Center) * 0.004f;
            idealFlySpeed *= speedMultiplier;

            // Move towards the target if far away from them.
            if (!npc.WithinRange(target.Center, 1600f))
                hoverDestination = target.Center;

            if (!npc.WithinRange(hoverDestination, 210f))
            {
                float flySpeed = MathHelper.Lerp(npc.velocity.Length(), idealFlySpeed, 0.05f);
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(hoverDestination), flySpeed / 580f, true) * flySpeed;
            }
            else
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(hoverDestination) * idealFlySpeed, idealFlySpeed / 24f);
        }

        public static void DoAggressiveChargeMovement(NPC npc, Player target, float attackTimer, float speedMultiplier = 1f)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float flyAcceleration = MathHelper.Lerp(0.045f, 0.037f, lifeRatio);
            float idealFlySpeed = MathHelper.Lerp(13f, 9.6f, lifeRatio);
            float generalSpeedFactor = Utils.InverseLerp(0f, 35f, attackTimer, true) * 0.825f + 1f;

            Vector2 destination = target.Center;

            float distanceFromDestination = npc.Distance(destination);
            if (!npc.WithinRange(destination, 550f))
            {
                distanceFromDestination = npc.Distance(destination);
                flyAcceleration *= 1.2f;
            }

            // Charge if the player is far away.
            // Don't do this at the start of the fight though. Doing so might lead to an unfair
            // charge.
            if (distanceFromDestination > 1750f && attackTimer > 90f)
                idealFlySpeed = 22f;

            if (ExoMechManagement.CurrentThanatosPhase == 4)
            {
                generalSpeedFactor *= 0.75f;
                flyAcceleration *= 0.5f;
            }
            else
            {
                if (ExoMechManagement.CurrentThanatosPhase >= 2)
                    generalSpeedFactor *= 1.1f;
                if (ExoMechManagement.CurrentThanatosPhase >= 5)
                {
                    generalSpeedFactor *= 1.18f;
                    flyAcceleration *= 1.18f;
                }
            }

            // Enforce a lower bound on the speed factor.
            if (generalSpeedFactor < 1f)
                generalSpeedFactor = 1f;
            generalSpeedFactor *= speedMultiplier * 0.825f;
            flyAcceleration *= 1f + (speedMultiplier - 1f) * 1.3f;

            float directionToPlayerOrthogonality = Vector2.Dot(npc.velocity.SafeNormalize(Vector2.Zero), npc.SafeDirectionTo(destination));

            // Adjust the speed based on how the direction towards the target compares to the direction of the
            // current velocity. This check is unnecessary once close to the target, which would prompt a snap/charge.
            if (!npc.WithinRange(destination, 250f))
            {
                float flySpeed = npc.velocity.Length();
                if (flySpeed < 13f)
                    flySpeed += 0.06f;

                if (flySpeed > 15f)
                    flySpeed -= 0.065f;

                if (directionToPlayerOrthogonality < 0.85f && directionToPlayerOrthogonality > 0.5f)
                    flySpeed += 0.16f;

                if (directionToPlayerOrthogonality < 0.5f && directionToPlayerOrthogonality > -0.7f)
                    flySpeed -= 0.1f;

                flySpeed = MathHelper.Clamp(flySpeed, 12f, 19f) * generalSpeedFactor;
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(destination), flyAcceleration, true) * flySpeed;
            }

            if (!npc.WithinRange(target.Center, 200f))
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(target.Center) * npc.velocity.Length(), generalSpeedFactor);

            // Lunge if near the player.
            bool canCharge = ExoMechManagement.CurrentThanatosPhase != 4 && directionToPlayerOrthogonality > 0.75f && distanceFromDestination < 400f;
            if (canCharge && npc.velocity.Length() < idealFlySpeed * generalSpeedFactor * 1.8f)
                npc.velocity *= 1.2f;
        }

        public static void DoBehavior_DoFinalPhaseTransition(NPC npc, Player target, float phaseTransitionAnimationTime)
        {
            DoProjectileShootInterceptionMovement(npc, target, 0.6f);

            // Play the transition sound at the start.
            if (phaseTransitionAnimationTime == 3f)
                Main.PlaySound(InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ExoMechFinalPhaseChargeup"), target.Center);
        }

        public static void SelectNextAttack(NPC npc)
        {
            ref float previousSpecialAttack = ref npc.Infernum().ExtraAI[18];

            ThanatosHeadAttackType oldAttackType = (ThanatosHeadAttackType)(int)npc.ai[0];
            // Update learning stuff.
            ExoMechManagement.DoPostAttackSelections(npc);

            bool wasCharging = oldAttackType == ThanatosHeadAttackType.AggressiveCharge ||
                oldAttackType == ThanatosHeadAttackType.MaximumOverdrive;

            if (ExoMechComboAttackContent.ShouldSelectComboAttack(npc, out ExoMechComboAttackContent.ExoMechComboAttackType newAttack))
                npc.ai[0] = (int)newAttack;

            else if (wasCharging || Main.rand.NextBool())
            {
                do
                {
                    //npc.ai[0] = (int)ThanatosHeadAttackType.TopwardSlam;
                    if (Main.rand.NextBool())
                        npc.ai[0] = (int)ThanatosHeadAttackType.LaserBarrage;
                    if (Main.rand.NextBool())
                        npc.ai[0] = (int)ThanatosHeadAttackType.RefractionRotorRays;
                    if (Main.rand.NextBool(3) && ExoMechManagement.CurrentThanatosPhase >= 3)
                        npc.ai[0] = (int)ThanatosHeadAttackType.ExoBomb;
                    if (Main.rand.NextBool(3) && ExoMechManagement.CurrentThanatosPhase >= 5)
                        npc.ai[0] = (int)ThanatosHeadAttackType.ExoLightBarrage;
                }
                while (npc.ai[0] == previousSpecialAttack);
                previousSpecialAttack = npc.ai[0];
            }
            else
            {
                npc.ai[0] = (int)ThanatosHeadAttackType.AggressiveCharge;
                if (ExoMechManagement.CurrentThanatosPhase >= 6)
                    npc.ai[0] = (int)ThanatosHeadAttackType.MaximumOverdrive;
            }

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            // Swap between venting and non-venting frames.
            npc.frameCounter++;
            if (npc.localAI[0] == (int)ThanatosFrameType.Closed)
            {
                if (npc.frameCounter >= 6D)
                {
                    npc.frame.Y -= frameHeight;
                    npc.frameCounter = 0D;
                }
                if (npc.frame.Y < 0)
                    npc.frame.Y = 0;
            }
            else
            {
                if (npc.frameCounter >= 6D)
                {
                    npc.frame.Y += frameHeight;
                    npc.frameCounter = 0D;
                }
                int finalFrame = Main.npcFrameCount[npc.type] - 1;

                // Play a vent sound (sus).
                if (Main.netMode != NetmodeID.Server && npc.frame.Y == frameHeight * (finalFrame - 1))
                {
                    SoundEffectInstance sound = Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ThanatosVent"), npc.Center);
                    if (sound != null)
                        sound.Volume *= 0.4f;
                }

                if (npc.frame.Y >= frameHeight * finalFrame)
                    npc.frame.Y = frameHeight * finalFrame;
            }
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            Texture2D texture = Main.npcTexture[npc.type];
            Vector2 origin = npc.frame.Size() * 0.5f;

            Vector2 center = npc.Center - Main.screenPosition;

            ExoMechAIUtilities.DrawFinalPhaseGlow(spriteBatch, npc, texture, center, npc.frame, origin);
            Main.spriteBatch.Draw(texture, center, npc.frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, spriteEffects, 0f);

            texture = ModContent.GetTexture("CalamityMod/NPCs/ExoMechs/Thanatos/ThanatosHeadGlow");
            Main.spriteBatch.Draw(texture, center, npc.frame, Color.White * npc.Opacity, npc.rotation, origin, npc.scale, spriteEffects, 0f);
            npc.ModNPC<ThanatosHead>().SmokeDrawer.DrawSet(npc.Center);
            return false;
        }
    }
}
