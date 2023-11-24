using CalamityMod;
using CalamityMod.NPCs.SlimeGod;
using CalamityMod.Particles;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Common.Graphics.ScreenEffects;
using InfernumMode.Content.Projectiles.Pets;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

using static CalamityMod.CalamityUtils;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.QueenSlime
{
    public class QueenSlimeBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.QueenSlimeBoss;

        public static int FallingGelDamage => 125;

        public static int SlimeMinionDamage => 125;

        public static int SmallCrystalSpikeDamage => 125;

        public static int SolidCrystalDamage => 125;

        public static int JewelBeamDamage => 130;

        public static int HallowBladeDamage => 135;

        public static int SplitFormDamage => 135;

        public static int LargeCrystalSpikeDamage => 185;

        public static int AimedLaserbeamDamage => 160;

        public static int GiantHallowLaserbeamDamage => 225;

        public const float Phase2LifeRatio = 0.625f;

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio
        };

        #region Fields, Properties, and Enumerations
        public enum QueenSlimeAttackType
        {
            SpawnAnimation,
            BasicHops,
            GeliticArmyStomp,
            FourThousandBlades, // :4000blades:
            CrystalMaze,
            SlimeCongregations,
            CrownLasers
        }

        public enum WingMotionState
        {
            RiseUpward,
            Flap
        }

        public struct QueenSlimeWing
        {
            public float WingRotation
            {
                get;
                set;
            }

            public float PreviousWingRotation
            {
                get;
                set;
            }

            public float WingRotationDifferenceMovingAverage
            {
                get;
                set;
            }

            // Piecewise function variables for determining the angular offset of wings when flapping.
            // Positive rotations = upward flaps.
            // Negative rotations = downward flaps.
            public static CurveSegment Anticipation => new(EasingType.PolyOut, 0f, -0.4f, 0.78f, 3);

            public static CurveSegment Flap => new(EasingType.PolyIn, 0.5f, Anticipation.EndingHeight(), -1.85f, 4);

            public static CurveSegment Rest => new(EasingType.PolyIn, 0.71f, Flap.EndingHeight(), 0.59f, 3);

            public static CurveSegment Recovery => new(EasingType.PolyIn, 0.9f, Rest.EndingHeight(), -0.4f - Rest.EndingHeight(), 2);

            public void Update(WingMotionState motionState, float animationCompletion, float instanceRatio)
            {
                PreviousWingRotation = WingRotation;

                switch (motionState)
                {
                    case WingMotionState.RiseUpward:
                        WingRotation = (-0.6f).AngleLerp(0.36f - instanceRatio * 0.15f, animationCompletion);
                        break;
                    case WingMotionState.Flap:
                        WingRotation = PiecewiseAnimation((animationCompletion + Lerp(instanceRatio, 0f, 0.5f)) % 1f, Anticipation, Flap, Rest, Recovery);
                        break;
                }

                WingRotationDifferenceMovingAverage = Lerp(WingRotationDifferenceMovingAverage, WingRotation - PreviousWingRotation, 0.15f);
            }
        }

        public static QueenSlimeWing[] Wings
        {
            get;
            set;
        }

        public static int WingUpdateCycleTime => 40;

        #endregion Fields, Properties, and Enumerations

        #region AI and Behaviors

        public override bool PreAI(NPC npc)
        {
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];
            bool phase2 = npc.life < npc.lifeMax * Phase2LifeRatio;
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float wingAnimationTimer = ref npc.ai[2];
            ref float crownIsAttached = ref npc.ai[3];
            ref float usingWings = ref npc.localAI[0];
            ref float wingMotionState = ref npc.localAI[1];
            ref float vibranceInterpolant = ref npc.localAI[2];

            // Initialize the wings.
            if (npc.localAI[3] == 0f)
            {
                Wings = new QueenSlimeWing[1];
                npc.localAI[3] = 1f;
                crownIsAttached = 1f;
                npc.netUpdate = true;
            }

            // Despawn if the target is gone.
            if (target.dead || !target.active)
            {
                npc.active = false;
                return false;
            }

            // Reset things every frame.
            npc.damage = npc.defDamage;
            npc.noTileCollide = true;
            npc.noGravity = true;
            npc.dontTakeDamage = false;
            npc.Calamity().DR = 0f;

            // Get rid of the fart death sound.
            npc.DeathSound = SoundID.NPCDeath1;

            switch ((QueenSlimeAttackType)attackType)
            {
                case QueenSlimeAttackType.SpawnAnimation:
                    DoBehavior_SpawnAnimation(npc, target, ref attackTimer, ref usingWings, ref wingMotionState);
                    break;
                case QueenSlimeAttackType.BasicHops:
                    DoBehavior_BasicHops(npc, target, phase2, ref attackTimer, ref wingMotionState);
                    break;
                case QueenSlimeAttackType.GeliticArmyStomp:
                    DoBehavior_GeliticArmyStomp(npc, target, phase2, ref attackTimer, ref wingMotionState);
                    break;
                case QueenSlimeAttackType.FourThousandBlades:
                    DoBehavior_FourThousandBlades(npc, target, ref attackTimer, ref wingMotionState, ref vibranceInterpolant);
                    break;
                case QueenSlimeAttackType.CrystalMaze:
                    DoBehavior_CrystalMaze(npc, target, phase2, ref attackTimer, ref wingMotionState);
                    break;
                case QueenSlimeAttackType.SlimeCongregations:
                    DoBehavior_SlimeCongregations(npc, target, ref attackTimer, ref wingMotionState, ref vibranceInterpolant);
                    break;
                case QueenSlimeAttackType.CrownLasers:
                    DoBehavior_CrownLasers(npc, target, phase2, ref attackTimer, ref wingMotionState, ref crownIsAttached);
                    break;
            }

            if (attackType != (int)QueenSlimeAttackType.SpawnAnimation)
                usingWings = 1f;

            // Perform wing updates.
            if (usingWings == 1f)
            {
                float animationCompletion = wingAnimationTimer / WingUpdateCycleTime % 1f;
                UpdateWings(npc, animationCompletion);
                if (wingAnimationTimer % WingUpdateCycleTime == (int)(QueenSlimeWing.Flap.startingX * WingUpdateCycleTime) + 4)
                    SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown with { Pitch = 0.2f, Volume = 0.1f }, npc.Center);

                wingAnimationTimer++;
            }
            else
                wingAnimationTimer = 0f;

            attackTimer++;
            return false;
        }

        public static void UpdateWings(NPC npc, float animationCompletion)
        {
            for (int i = 0; i < Wings.Length; i++)
            {
                float instanceRatio = i / (float)Wings.Length;
                if (Wings.Length <= 1)
                    instanceRatio = 0f;

                Wings[i].Update((WingMotionState)npc.localAI[1], animationCompletion, instanceRatio);

                // Release feather dust particles.
                if (animationCompletion >= QueenSlimeWing.Flap.startingX && animationCompletion < QueenSlimeWing.Recovery.startingX)
                {
                    Vector2 featherSpawnPosition = npc.Center - Vector2.UnitX.RotatedBy(npc.rotation + Wings[i].WingRotation + 0.55f).RotatedByRandom(0.12f) * Main.rand.NextFloat(110f);
                    Dust feather = Dust.NewDustPerfect(featherSpawnPosition, 267);
                    feather.velocity = Main.rand.NextVector2Circular(2f, 2f) - Vector2.UnitY * 3f;
                    feather.scale = Main.rand.NextFloat(1f, 1.25f);
                    feather.color = NPC.AI_121_QueenSlime_GetDustColor();
                    feather.noGravity = true;

                    featherSpawnPosition = npc.Center + Vector2.UnitX.RotatedBy(npc.rotation + Wings[i].WingRotation + 0.55f).RotatedByRandom(0.12f) * Main.rand.NextFloat(110f);
                    feather = Dust.NewDustPerfect(featherSpawnPosition, 267);
                    feather.velocity = Main.rand.NextVector2Circular(2f, 2f) - Vector2.UnitY * 3f;
                    feather.scale = Main.rand.NextFloat(1f, 1.25f);
                    feather.color = NPC.AI_121_QueenSlime_GetDustColor();
                    feather.noGravity = true;
                }
            }
        }

        public static void DoBehavior_SpawnAnimation(NPC npc, Player target, ref float attackTimer, ref float usingWings, ref float wingMotionState)
        {
            int slimeChargeTime = 95;
            float verticalSpawnOffset = 1600f;
            float startingFallSpeed = 8f;
            float endingFallSpeed = 38.5f;
            float fallAcceleration = 0.75f;
            ref float groundCollisionY = ref npc.Infernum().ExtraAI[0];
            ref float hasHitGround = ref npc.Infernum().ExtraAI[1];

            // Teleport above the player on the first frame.
            if (attackTimer <= 1f && hasHitGround == 0f)
            {
                npc.velocity = Vector2.UnitY * startingFallSpeed;
                npc.Center = target.Center - Vector2.UnitY * verticalSpawnOffset;
                if (npc.position.Y <= 400f)
                    npc.position.Y = 400f;

                groundCollisionY = target.Top.Y;
                npc.netUpdate = true;
            }

            // Interact with tiles again once past a certain point.
            npc.noTileCollide = npc.Bottom.Y < groundCollisionY;

            // Accelerate downward.
            if (hasHitGround == 0f)
                npc.velocity.Y = Clamp(npc.velocity.Y + fallAcceleration, startingFallSpeed, endingFallSpeed);
            else
                npc.velocity.Y = 0f;

            // Handle ground hit effects when ready.
            if (Utilities.ActualSolidCollisionTop(npc.BottomLeft, npc.width, 24) && attackTimer >= 5f && hasHitGround == 0f)
            {
                for (int i = 0; i < 60; i++)
                {
                    Color gelColor = Color.Lerp(Color.Pink, Color.HotPink, Main.rand.NextFloat());
                    Particle gelParticle = new EoCBloodParticle(npc.Center + Main.rand.NextVector2Circular(60f, 60f), -Vector2.UnitY.RotatedByRandom(0.98f) * Main.rand.NextFloat(4f, 20f), 120, Main.rand.NextFloat(0.9f, 1.2f), gelColor * 0.75f, 5f);
                    GeneralParticleHandler.SpawnParticle(gelParticle);
                }

                Utilities.CreateShockwave(npc.Center, 40, 4, 40f, false);
                SoundEngine.PlaySound(SlimeGodCore.ExitSound, target.Center);

                hasHitGround = 1f;
                attackTimer = 0f;
                npc.netUpdate = true;
            }

            // Charge energy when on the ground.
            if (hasHitGround == 1f)
            {
                if (attackTimer < slimeChargeTime)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        Vector2 dustSpawnPosition = npc.Center + Main.rand.NextVector2CircularEdge(100f, 100f);
                        Vector2 dustVelocity = (npc.Center - dustSpawnPosition).SafeNormalize(Vector2.UnitY) * 8f;
                        Dust energy = Dust.NewDustDirect(dustSpawnPosition, 2, 2, DustID.Smoke, dustVelocity.X, dustVelocity.Y, 40, NPC.AI_121_QueenSlime_GetDustColor() with { A = 125 }, 1.8f);
                        energy.position = dustSpawnPosition;
                        energy.noGravity = true;
                        energy.alpha = 250;
                        energy.velocity = dustVelocity;
                        energy.customData = npc;
                    }
                }
                else
                    usingWings = 1f;

                // Create visual effects to accompany the wings being made.
                if (attackTimer == slimeChargeTime)
                {
                    Utilities.CreateShockwave(npc.Center, 20, 4, 40f, false);
                    ScreenEffectSystem.SetBlurEffect(npc.Center, 0.9f, 25);

                    SelectNextAttack(npc);
                    SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown, npc.Center);
                    npc.velocity = Vector2.UnitY * -4f;
                }

                if (attackTimer >= slimeChargeTime + WingUpdateCycleTime)
                    wingMotionState = (int)WingMotionState.Flap;
            }
        }

        public static void DoBehavior_BasicHops(NPC npc, Player target, bool phase2, ref float attackTimer, ref float wingMotionState)
        {
            int jumpTime = 42;
            int slamDelay = 90;
            int slamHoverTime = 75;
            int crystalID = ModContent.ProjectileType<FallingCrystal>();
            float horizontalJumpSpeed = Distance(target.Center.X, npc.Center.X) * 0.012f + 16f;
            float baseVerticalJumpSpeed = 23f;
            float fallAcceleration = 0.9f;

            if (phase2)
            {
                baseVerticalJumpSpeed += 2f;
                slamDelay -= 6;
            }

            ref float jumpState = ref npc.Infernum().ExtraAI[0];
            ref float groundCollisionY = ref npc.Infernum().ExtraAI[1];
            ref float didSlamGroundHitEffects = ref npc.Infernum().ExtraAI[2];
            ref float jumpWaitDelay = ref npc.Infernum().ExtraAI[3];

            // Disable contact damage until the slam, since the hops can be so fast as to be unfair.
            if (jumpState != 3f)
                npc.damage = 0;

            // Ignore tiles while jumping.
            npc.noTileCollide = jumpState != 0f && attackTimer <= jumpTime;

            // Decide wing stuff.
            wingMotionState = (int)WingMotionState.Flap;

            // Perform ground checks. The attack does not begin until this is finished.
            if (jumpState == 0f)
            {
                attackTimer = 0f;
                npc.noGravity = false;

                jumpWaitDelay++;
                if (Utilities.ActualSolidCollisionTop(npc.BottomLeft, npc.width, 24) || jumpWaitDelay >= 96f)
                {
                    jumpState = 1f;
                    npc.netUpdate = true;
                }
            }

            // Jump above the target.
            if (attackTimer == 1f && jumpState == 1f)
            {
                npc.velocity.X = (target.Center.X > npc.Center.X).ToDirectionInt() * horizontalJumpSpeed;
                npc.velocity.Y = -baseVerticalJumpSpeed;
                if (target.Center.Y < npc.Center.Y)
                    npc.velocity.Y -= Math.Abs(target.Center.Y - npc.Center.Y) * 0.05f;
                npc.netUpdate = true;
            }

            // Accelerate downward.
            if (jumpState != 2f)
            {
                if (npc.velocity.Y == 0f)
                    npc.velocity.X *= 0.5f;
                npc.velocity.Y += fallAcceleration;
                if (npc.velocity.Y <= -28f)
                    npc.velocity.Y += 2f * fallAcceleration;
            }

            // Teleport above the player and slam down if very far from the target.
            if (!npc.WithinRange(target.Center, 2100f) && npc.noTileCollide && jumpState == 1f)
            {
                npc.velocity = Vector2.UnitY * 10f;
                npc.Center = target.Center - Vector2.UnitY * 800f;
                npc.netUpdate = true;
            }

            // Release crystals while jumping.
            if (jumpState == 1f && !Collision.SolidCollision(npc.TopLeft, npc.width, npc.height) && attackTimer % 5f == 0f && npc.velocity.Y != 0f)
            {
                SoundEngine.PlaySound(SoundID.Item28, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, crystalID, SolidCrystalDamage, 0f);
            }

            // Begin the slam.
            if (jumpState == 1f && attackTimer >= slamDelay && Utilities.ActualSolidCollisionTop(npc.BottomLeft, npc.width, 24))
            {
                jumpState = 2f;
                attackTimer = 0f;
                npc.velocity = -Vector2.UnitY * 6f;
                npc.netUpdate = true;
            }

            // Move above the target before slamming.
            if (jumpState == 2f)
            {
                Vector2 hoverDestination = target.Center - Vector2.UnitY * 350f;
                Vector2 idealVelocity = (hoverDestination - npc.Center) * Utils.Remap(attackTimer, 0f, slamHoverTime, 0.002f, 0.18f);
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.35f);
            }

            // Slam downward.
            if (jumpState == 2f && attackTimer >= slamHoverTime)
            {
                jumpState = 3f;
                attackTimer = 0f;
                groundCollisionY = target.Top.Y;
                npc.velocity = Vector2.UnitY * 3f;

                while (npc.WithinRange(target.Center, 270f))
                    npc.position.Y -= 10f;
            }

            if (jumpState == 3f)
            {
                if (didSlamGroundHitEffects == 0f && npc.velocity.Y < 27f)
                    npc.velocity.Y += 0.45f * fallAcceleration;

                npc.noTileCollide = npc.Bottom.Y < groundCollisionY;
                if (Utilities.ActualSolidCollisionTop(npc.TopLeft, npc.width, npc.height + 48) && didSlamGroundHitEffects == 0f)
                {
                    SoundEngine.PlaySound(SlimeGodCore.ExitSound, target.Center);
                    didSlamGroundHitEffects = 1f;

                    // Make all crystals fall.
                    foreach (Projectile crystal in Utilities.AllProjectilesByID(crystalID))
                    {
                        crystal.velocity = Vector2.UnitY * Main.rand.NextFloat(5f, 8f);
                        if (crystal.Center.Y >= target.Center.Y + 200f)
                            crystal.velocity *= -1f;

                        crystal.ai[0] = 1f;
                        crystal.netUpdate = true;
                    }

                    npc.netUpdate = true;
                }
            }

            if (jumpState == 3f && attackTimer >= 78f && didSlamGroundHitEffects == 1f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_GeliticArmyStomp(NPC npc, Player target, bool phase2, ref float attackTimer, ref float wingMotionState)
        {
            int jumpCount = 2;
            int slamHoverTime = 14;
            int slamAfterDelay = 140;
            int slamFlyTime = WingUpdateCycleTime * 2 - slamHoverTime;
            int fallingSlimeID = ModContent.ProjectileType<FallingSpikeSlimeProj>();
            int bouncingSlimeID = ModContent.ProjectileType<BouncingSlimeProj>();
            float maxSlimeOffset = 3443f;
            if (phase2)
                maxSlimeOffset -= 500f;

            ref float slimeSpawnAttackType = ref npc.Infernum().ExtraAI[0];
            ref float hasHitGround = ref npc.Infernum().ExtraAI[1];
            ref float jumpCounter = ref npc.Infernum().ExtraAI[2];

            // Decide wing stuff.
            wingMotionState = (int)WingMotionState.Flap;

            // Decide which slimes should be spawned.
            if (slimeSpawnAttackType == 0f)
            {
                slimeSpawnAttackType = Main.rand.NextFromList(fallingSlimeID, bouncingSlimeID);
                npc.netUpdate = true;
            }

            // Hover into position before slamming downward.
            if (attackTimer <= slamFlyTime)
            {
                Vector2 hoverDestination = target.Center - Vector2.UnitY * 350f;
                Vector2 idealVelocity = (hoverDestination - npc.Center) * Utils.Remap(attackTimer, 0f, slamFlyTime, 0.002f, 0.18f);
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.15f);

                // Disable cheap hits from redirecting.
                npc.damage = 0;
            }

            // Slow down and rise up in anticipation of the slam.
            else if (attackTimer <= slamFlyTime + slamHoverTime)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, -Vector2.UnitY * 6f, 0.24f);
                npc.velocity.X *= 0.5f;

                // Summon slimes as the anticipation begins.
                bool spawnSlimesHorizontally = slimeSpawnAttackType == fallingSlimeID || slimeSpawnAttackType == bouncingSlimeID;
                if (Main.netMode != NetmodeID.MultiplayerClient && spawnSlimesHorizontally)
                {
                    float horizontalOffset = Utils.GetLerpValue(slamFlyTime, slamFlyTime + slamHoverTime, attackTimer, true) * maxSlimeOffset;
                    if (slimeSpawnAttackType == bouncingSlimeID)
                        horizontalOffset *= 0.72f;

                    Utilities.NewProjectileBetter(npc.Top - Vector2.UnitX * horizontalOffset, Vector2.Zero, (int)slimeSpawnAttackType, SlimeMinionDamage, 0f);
                    Utilities.NewProjectileBetter(npc.Top + Vector2.UnitX * horizontalOffset, Vector2.Zero, (int)slimeSpawnAttackType, SlimeMinionDamage, 0f);
                }
            }

            // Slow downward and make the summoned slimes do things.
            if (attackTimer == slamFlyTime + slamHoverTime)
            {
                foreach (Projectile spikeSlime in Utilities.AllProjectilesByID(fallingSlimeID, bouncingSlimeID))
                {
                    spikeSlime.velocity = Vector2.UnitY * 2.5f;
                    spikeSlime.netUpdate = true;
                }
            }

            // Slam and accelerate.
            if (attackTimer >= slamFlyTime + slamHoverTime + 8f)
            {
                if (hasHitGround == 0f && npc.velocity.Y == 0f && attackTimer >= slamFlyTime + slamHoverTime + 16f)
                {
                    SoundEngine.PlaySound(SlimeGodCore.ExitSound, target.Center);
                    npc.velocity = Vector2.UnitY * -16f;
                    hasHitGround = 1f;
                    npc.netUpdate = true;
                }

                if (hasHitGround == 0f)
                    npc.velocity = Vector2.UnitY * Clamp(npc.velocity.Y * 1.1f + 2f, 0f, 40f);
                else
                    npc.velocity.Y = Clamp(npc.velocity.Y + 0.3f, -10f, 16f);

                npc.noTileCollide = false;
            }

            if (attackTimer >= slamFlyTime + slamHoverTime + slamAfterDelay)
            {
                slimeSpawnAttackType = 0f;
                attackTimer = 0f;
                hasHitGround = 0f;
                npc.netUpdate = true;

                jumpCounter++;
                if (jumpCounter >= jumpCount)
                    SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_FourThousandBlades(NPC npc, Player target, ref float attackTimer, ref float wingMotionState, ref float vibranceInterpolant)
        {
            int redirectTime = 24;
            int upwardRiseTime = 16;
            int attackDuration = HallowBladeLaserbeam.Lifetime;
            int bladeReleaseTime = 66;
            int laserChargeupTime = attackDuration - bladeReleaseTime;
            int outwardLaserShootRate = 7;
            float maxRiseSpeed = 10f;
            float bladeOffsetSpacing = Lerp(180f, 144f, 1f - npc.life / (float)npc.lifeMax);
            float maxVerticalBladeOffset = 2000f;

            bool currentlyAttacking = attackTimer >= redirectTime + upwardRiseTime + laserChargeupTime + 25f;
            bool currentlyCharging = attackTimer >= redirectTime + upwardRiseTime && attackTimer < redirectTime + upwardRiseTime + laserChargeupTime;
            ref float bladeVerticalOffset = ref npc.Infernum().ExtraAI[0];
            ref float outwardLaserShootCounter = ref npc.Infernum().ExtraAI[1];

            // Disable contact damage universally. It is not relevant for this attack.
            npc.damage = 0;

            // Increase DR due to being still.
            npc.Calamity().DR = 0.8f;

            // Decide wing stuff.
            wingMotionState = (int)WingMotionState.Flap;

            // Move to the top left/right of the player.
            if (attackTimer <= redirectTime)
            {
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 300f, -180f);
                Vector2 idealVelocity = (hoverDestination - npc.Center).ClampMagnitude(0f, 80f);
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.3f);
            }

            // Rise upward and become increasingly vibrant.
            else if (attackTimer <= redirectTime + upwardRiseTime)
            {
                vibranceInterpolant = Utils.GetLerpValue(redirectTime, redirectTime + upwardRiseTime, attackTimer, true);
                npc.Opacity = Lerp(1f, 0.5f, vibranceInterpolant);
                npc.velocity = Vector2.UnitY * Lerp(npc.velocity.Y, -maxRiseSpeed, 0.2f);
            }

            else
            {
                npc.velocity *= 0.85f;
                npc.position.X += Math.Sign(target.Center.X - npc.Center.X) * 2f;

                if (target.Infernum_Camera().CurrentScreenShakePower < 1.85f)
                    target.Infernum_Camera().CurrentScreenShakePower = 3f;
            }

            // Release the blade spawning laser thing.
            if (attackTimer == redirectTime + upwardRiseTime)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.QueenSlimeExplosionSound, target.Center);

                target.Infernum_Camera().CurrentScreenShakePower = 12f;
                ScreenEffectSystem.SetBlurEffect(npc.Center, 0.4f, 36);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Utilities.NewProjectileBetter(npc.Center, Vector2.UnitY, ModContent.ProjectileType<HallowBladeLaserbeam>(), GiantHallowLaserbeamDamage, 0f);
                    bladeVerticalOffset = -maxVerticalBladeOffset;
                    npc.netUpdate = true;
                }
            }

            HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.QueenSlimeBladeBeamTip");

            // Release blades towards the laser.
            if (Main.netMode != NetmodeID.MultiplayerClient && currentlyCharging && attackTimer % 3f == 2f)
            {
                bladeVerticalOffset += bladeOffsetSpacing;
                if (bladeVerticalOffset >= maxVerticalBladeOffset)
                {
                    bladeVerticalOffset *= -1f;
                    npc.netUpdate = true;
                }

                float hue = Utils.GetLerpValue(-maxVerticalBladeOffset, maxVerticalBladeOffset, bladeVerticalOffset) * 4f % 1f;
                Vector2 leftSpawnPosition = new(target.Center.X - 900f, npc.Center.Y + bladeVerticalOffset);
                Vector2 rightSpawnPosition = new(target.Center.X + 900f, npc.Center.Y + bladeVerticalOffset);

                if (target.Center.X < npc.Center.X + 900f)
                    Utilities.NewProjectileBetter(leftSpawnPosition, Vector2.Zero, ModContent.ProjectileType<HallowBlade>(), HallowBladeDamage, 0f, -1, 0f, hue);

                if (target.Center.X > npc.Center.X - 900f)
                    Utilities.NewProjectileBetter(rightSpawnPosition, Vector2.Zero, ModContent.ProjectileType<HallowBlade>(), HallowBladeDamage, 0f, -1, Pi, 1f - hue);
            }

            // Create an explosion and make the blades go outward.
            if (attackTimer == redirectTime + upwardRiseTime + laserChargeupTime)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.QueenSlimeExplosionSound, target.Center);

                target.Infernum_Camera().CurrentScreenShakePower = 12f;
                ScreenEffectSystem.SetBlurEffect(npc.Center, 0.4f, 36);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<QueenSlimeLightWave>(), 0, 0f);
            }

            // Release lasers outward.
            if (currentlyAttacking && (attackTimer - redirectTime + upwardRiseTime + laserChargeupTime) % outwardLaserShootRate == outwardLaserShootRate - 1f)
            {
                SoundEngine.PlaySound(SoundID.Item163, target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (float dy = -1500f; dy < 1500f; dy += bladeOffsetSpacing * 2.8f)
                    {
                        float hue = ((dy + 1500f) / 1000f + attackTimer * 0.0817f) % 1f;
                        Vector2 bladeSpawnPosition = new(npc.Center.X, target.Center.Y + dy);
                        if (outwardLaserShootCounter % 2f == 1f)
                            bladeSpawnPosition.Y -= bladeOffsetSpacing * 1.4f;

                        int bitch = Utilities.NewProjectileBetter(bladeSpawnPosition + Vector2.UnitX * 8f, Vector2.Zero, ModContent.ProjectileType<HallowBlade>(), HallowBladeDamage, 0f, -1, 0f, hue);
                        Main.projectile[bitch].localAI[1] = 3000f;

                        bitch = Utilities.NewProjectileBetter(bladeSpawnPosition - Vector2.UnitX * 8f, Vector2.Zero, ModContent.ProjectileType<HallowBlade>(), HallowBladeDamage, 0f, -1, Pi, 1f - hue);
                        Main.projectile[bitch].localAI[1] = 3000f;
                    }

                    outwardLaserShootCounter++;
                }
            }

            if (currentlyCharging && attackTimer % 105f == 0f)
                SoundEngine.PlaySound(SoundID.Item164 with { Volume = 0.6f }, target.Center);

            if (attackTimer >= redirectTime + upwardRiseTime + attackDuration - 12f)
                vibranceInterpolant = Clamp(vibranceInterpolant - 0.2f, 0f, 1f);

            if (attackTimer >= redirectTime + upwardRiseTime + attackDuration)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_CrystalMaze(NPC npc, Player target, bool phase2, ref float attackTimer, ref float wingMotionState)
        {
            int mazeSummonDelay = 30;
            int spinningCrystalReleaseRate = 90;
            int spinningCrystalCount = 4;
            int attackCycleCount = 4;

            if (phase2)
            {
                mazeSummonDelay += 15;
                spinningCrystalReleaseRate -= 6;
                spinningCrystalCount += 2;
            }

            // Disable contact damage universally. It is not relevant for this attack.
            npc.damage = 0;

            // Increase DR due to being still.
            npc.Calamity().DR = 0.8f;

            // Decide wing stuff.
            wingMotionState = (int)WingMotionState.Flap;

            //if (wingAnimationTimer >= WingUpdateCycleTime - 1f)
            //    wingAnimationTimer = WingUpdateCycleTime - 1f;

            // Hover above the target.
            Vector2 hoverDestination = target.Center - Vector2.UnitY * 375f;
            Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 24f;
            float distanceFromDestination = npc.Distance(hoverDestination);

            // Slow down before firing.
            if (distanceFromDestination < 80f)
                idealVelocity *= 0.65f;
            if (distanceFromDestination < 40f)
                idealVelocity = npc.velocity;
            npc.SimpleFlyMovement(idealVelocity, 0.85f);

            // Interact with tiles.
            npc.noGravity = false;
            npc.noTileCollide = false;

            // Create the maze of crystals.
            if (attackTimer == mazeSummonDelay)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 96; i++)
                    {
                        Vector2 crystalSpawnPosition = npc.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(100f, 1750f);
                        if (crystalSpawnPosition.WithinRange(target.Center, 300f) || Collision.SolidCollision(crystalSpawnPosition, 1, 1))
                            continue;

                        Utilities.NewProjectileBetter(crystalSpawnPosition, Vector2.Zero, ModContent.ProjectileType<FallingCrystal>(), SolidCrystalDamage, 0f);
                    }
                }

                // Give a tip.
                HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.QueenSlimeCrystalTip");
            }

            // Create the spinning lasers.
            if (attackTimer >= mazeSummonDelay && (attackTimer - mazeSummonDelay) % spinningCrystalReleaseRate == 0f)
            {
                SoundEngine.PlaySound(SoundID.Item28, target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < spinningCrystalCount; i++)
                    {
                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(crystal =>
                        {
                            crystal.ModProjectile<SpinningLaserCrystal>().SpinningCenter = target.Center;
                        });
                        Utilities.NewProjectileBetter(target.Center, Vector2.Zero, ModContent.ProjectileType<SpinningLaserCrystal>(), 0, 0f, -1, TwoPi * i / spinningCrystalCount);
                    }
                }
            }

            if (attackTimer >= mazeSummonDelay + spinningCrystalReleaseRate * attackCycleCount - 1f)
            {
                Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<FallingCrystal>());
                SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_SlimeCongregations(NPC npc, Player target, ref float attackTimer, ref float wingMotionState, ref float vibranceInterpolant)
        {
            int jitterTime = 90;
            int splitCount = 7;
            int postSplitEffectsTime = 52;
            int explodeCount = 3;
            ref float convergencePointX = ref npc.Infernum().ExtraAI[0];
            ref float convergencePointY = ref npc.Infernum().ExtraAI[1];
            ref float canReform = ref npc.Infernum().ExtraAI[2];
            ref float telegraphInterpolant = ref npc.Infernum().ExtraAI[3];
            ref float explodeCounter = ref npc.Infernum().ExtraAI[4];

            // Decide wing stuff.
            wingMotionState = (int)WingMotionState.Flap;

            // Disable damage.
            npc.damage = 0;
            npc.dontTakeDamage = true;

            // Jitter in place.
            if (attackTimer <= jitterTime)
            {
                vibranceInterpolant = Utils.GetLerpValue(0f, jitterTime - 30f, attackTimer, true);
                npc.Opacity = Utils.GetLerpValue(jitterTime - 3f, jitterTime - 30f, attackTimer, true);
                npc.Center += Main.rand.NextVector2Circular(3f, 3f);
            }

            // Split into flying slimes.
            if (attackTimer == jitterTime)
            {
                SoundEngine.PlaySound(SlimeGodCore.PossessionSound, target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 convergencePoint = target.Center + target.velocity * 50f - Vector2.UnitY * 300f;
                    while (Collision.SolidCollision(convergencePoint - Vector2.One * 200f, 400, 400))
                        convergencePoint.Y -= 30f;

                    for (int i = 0; i < splitCount; i++)
                    {
                        Vector2 splitVelocity = (TwoPi * i / splitCount + Main.rand.NextFloat(0.25f)).ToRotationVector2() * Main.rand.NextFloat(9f, 10.5f) + Main.rand.NextVector2Circular(0.8f, 0.8f);

                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(splitSlime =>
                        {
                            splitSlime.ModProjectile<QueenSlimeSplitFormProj>().ConvergencePoint = convergencePoint;
                        });
                        Utilities.NewProjectileBetter(npc.Center + splitVelocity, splitVelocity, ModContent.ProjectileType<QueenSlimeSplitFormProj>(), SplitFormDamage, 0f);
                    }

                    telegraphInterpolant = 0f;
                    convergencePointX = convergencePoint.X;
                    convergencePointY = convergencePoint.Y;
                    npc.netUpdate = true;
                }
            }

            // Prevent the attack timer from incrementing if in the split form.
            if (canReform == 0f && attackTimer >= jitterTime + 1f)
            {
                attackTimer = jitterTime + 1f;
                npc.Center = new(convergencePointX, convergencePointY);
                npc.Opacity = 0f;
            }

            // Aim the crystal telegraphs once ready to reform.
            if (canReform == 1f)
            {
                npc.Opacity = Clamp(npc.Opacity + 0.06f, 0f, 1f);
                npc.scale = npc.Opacity + 0.001f;
                telegraphInterpolant = Utils.GetLerpValue(jitterTime, jitterTime + postSplitEffectsTime - 3f, attackTimer, true);
                if (attackTimer >= jitterTime + postSplitEffectsTime)
                {
                    target.Infernum_Camera().CurrentScreenShakePower = 8f;
                    ScreenEffectSystem.SetBlurEffect(npc.Center, 0.2f, 20);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<QueenSlimeLightWave>(), 0, 0f);

                        for (int i = 0; i < 8; i++)
                        {
                            Vector2 crystalDirection = (TwoPi * i / 8f).ToRotationVector2();
                            Utilities.NewProjectileBetter(npc.Center - crystalDirection * 70f, crystalDirection, ModContent.ProjectileType<HallowCrystalSpike>(), LargeCrystalSpikeDamage, 0f);
                        }

                        for (int i = 0; i < 5; i++)
                        {
                            Vector2 crystalSpikeVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(Lerp(-0.43f, 0.43f, i / 4f)) * 8f;
                            Utilities.NewProjectileBetter(npc.Center, crystalSpikeVelocity, ModContent.ProjectileType<QueenSlimeCrystalSpike>(), SmallCrystalSpikeDamage, 0f);
                        }
                    }

                    SoundEngine.PlaySound(InfernumSoundRegistry.QueenSlimeExplosionSound with { Pitch = 0.25f }, target.Center);
                    attackTimer = jitterTime - 1f;
                    canReform = 0f;
                    explodeCounter++;
                    npc.netUpdate = true;

                    if (explodeCounter >= explodeCount)
                        SelectNextAttack(npc);
                }
            }
        }

        public static void DoBehavior_CrownLasers(NPC npc, Player target, bool phase2, ref float attackTimer, ref float wingMotionState, ref float crownIsAttached)
        {
            int gelReleaseCount = 8;
            ref float crownShouldReturn = ref npc.Infernum().ExtraAI[0];
            ref float hasSummonedCrown = ref npc.Infernum().ExtraAI[1];
            ref float projectileReleaseRate = ref npc.Infernum().ExtraAI[2];

            projectileReleaseRate = phase2 ? 60f : 90f;
            if (attackTimer <= 90f)
                projectileReleaseRate = 1000000f;

            // Decide wing stuff.
            wingMotionState = (int)WingMotionState.Flap;

            // Disable contact damage.
            npc.damage = 0;

            if (Main.netMode != NetmodeID.MultiplayerClient && hasSummonedCrown == 0f)
            {
                Utilities.NewProjectileBetter(CrownPosition(npc), -Vector2.UnitY * 3f, ModContent.ProjectileType<QueenSlimeCrown>(), 0, 0f);

                hasSummonedCrown = 1f;
                crownIsAttached = 0f;
                npc.netUpdate = true;
            }

            // Hover above the target.
            Vector2 hoverDestination = target.Center - Vector2.UnitY * 325f;
            Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 24f;
            float distanceFromDestination = npc.Distance(hoverDestination);

            // Slow down before firing.
            if (distanceFromDestination < 80f)
                idealVelocity *= 0.65f;
            if (distanceFromDestination < 40f)
                idealVelocity = npc.velocity;
            npc.SimpleFlyMovement(idealVelocity, 0.65f);

            // Release bursts of gel that fall downward.
            if (attackTimer % projectileReleaseRate == projectileReleaseRate - 1f && attackTimer < projectileReleaseRate * gelReleaseCount)
            {
                target.Infernum_Camera().CurrentScreenShakePower = 6f;
                SoundEngine.PlaySound(SoundID.NPCHit1, npc.Center);

                float horizontalSpeedOffset = 3f * Main.rand.NextFloatDirection();
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (float i = -20f; i < 20f; i += Main.rand.NextFloat(2.9f, 3.2f))
                    {
                        Vector2 gelVelocity = new(horizontalSpeedOffset + i, Main.rand.NextFloat(-12f, -10f));
                        Utilities.NewProjectileBetter(npc.Center, gelVelocity, ModContent.ProjectileType<FallingGel>(), FallingGelDamage, 0f);
                    }
                }
            }

            npc.rotation = npc.velocity.X * 0.025f;

            if (attackTimer >= projectileReleaseRate * (gelReleaseCount - 1f) - 36f)
                crownShouldReturn = 1f;

            if (attackTimer >= projectileReleaseRate * (gelReleaseCount + 3.6f))
            {
                npc.rotation = 0f;
                SelectNextAttack(npc);
            }
        }

        public static void SelectNextAttack(NPC npc)
        {
            bool phase2 = npc.life < npc.lifeMax * Phase2LifeRatio;
            QueenSlimeAttackType previousAttack = (QueenSlimeAttackType)npc.ai[0];
            QueenSlimeAttackType nextAttack = QueenSlimeAttackType.BasicHops;

            switch (previousAttack)
            {
                case QueenSlimeAttackType.SpawnAnimation:
                    nextAttack = QueenSlimeAttackType.BasicHops;
                    break;
                case QueenSlimeAttackType.BasicHops:
                    nextAttack = QueenSlimeAttackType.CrystalMaze;
                    break;
                case QueenSlimeAttackType.CrystalMaze:
                    nextAttack = QueenSlimeAttackType.CrownLasers;
                    break;
                case QueenSlimeAttackType.CrownLasers:
                    nextAttack = QueenSlimeAttackType.GeliticArmyStomp;
                    break;
                case QueenSlimeAttackType.GeliticArmyStomp:
                    nextAttack = phase2 ? QueenSlimeAttackType.FourThousandBlades : QueenSlimeAttackType.BasicHops;
                    break;
                case QueenSlimeAttackType.FourThousandBlades:
                    nextAttack = QueenSlimeAttackType.SlimeCongregations;
                    break;
                case QueenSlimeAttackType.SlimeCongregations:
                    nextAttack = QueenSlimeAttackType.BasicHops;
                    break;
            }

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.Opacity = 1f;
            npc.ai[0] = (int)nextAttack;
            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }

        #endregion AI and Behaviors

        #region Drawing and Frames

        public static Vector2 CrownPosition(NPC npc)
        {
            Vector2 crownPosition = new(npc.Center.X, npc.Top.Y - 12f);
            float crownOffset = 0f;
            int frameHeight;
            if (npc.frame.Height == 0)
                frameHeight = 122;
            else
                frameHeight = npc.frame.Height;
            switch (npc.frame.Y / frameHeight)
            {
                case 1:
                    crownOffset -= 10f;
                    break;
                case 3:
                case 5:
                case 6:
                    crownOffset += 10f;
                    break;
                case 4:
                case 12:
                case 13:
                case 14:
                case 15:
                    crownOffset += 18f;
                    break;
                case 7:
                case 8:
                    crownOffset -= 14f;
                    break;
                case 9:
                    crownOffset -= 16f;
                    break;
                case 10:
                    crownOffset -= 4f;
                    break;
                case 11:
                    crownOffset += 20f;
                    break;
                case 20:
                    crownOffset -= 14f;
                    break;
                case 21:
                case 23:
                    crownOffset -= 18f;
                    break;
                case 22:
                    crownOffset -= 22f;
                    break;
            }

            crownPosition.Y += crownOffset;
            if (npc.rotation != 0f)
                crownPosition = crownPosition.RotatedBy(npc.rotation, npc.Bottom);
            return crownPosition;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = TextureAssets.Npc[npc.type].Value;

            int frame = npc.frame.Y / npc.frame.Height;
            float vibranceInterpolant = npc.localAI[2];
            QueenSlimeAttackType currentAttack = (QueenSlimeAttackType)npc.ai[0];
            Rectangle frameThing = texture.Frame(2, Main.npcFrameCount[npc.type], frame / Main.npcFrameCount[npc.type], frame % Main.npcFrameCount[npc.type]);
            frameThing.Inflate(0, -2);
            Vector2 origin = frameThing.Size() * new Vector2(0.5f, 1f);
            Color color = Color.Lerp(lightColor, Color.White, 0.5f);

            // Incorporate vibrancy into the colors.
            color.A = (byte)(color.A * (1f - vibranceInterpolant));

            // Draw the telegraph lines if necessary.
            if (currentAttack == QueenSlimeAttackType.SlimeCongregations && npc.Infernum().ExtraAI[3] > 0f)
            {
                float telegraphInterpolant = npc.Infernum().ExtraAI[3];
                for (int i = 0; i < 8; i++)
                {
                    float laserRotation = -TwoPi * i / 8f;
                    BloomLineDrawInfo lineInfo = new(rotation: laserRotation,
                        width: 0.002f + Pow(telegraphInterpolant, 4f) * (Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.001f + 0.001f),
                        bloom: Lerp(0.3f, 0.4f, telegraphInterpolant),
                        scale: Vector2.One * telegraphInterpolant * 3600f,
                        main: Color.Lerp(Color.HotPink, Color.SkyBlue, telegraphInterpolant * 0.3f),
                        darker: Color.Purple,
                        opacity: Sqrt(telegraphInterpolant),
                        bloomOpacity: 0.45f,
                        lightStrength: 6.67f);

                    Utilities.DrawBloomLineTelegraph(npc.Center - Main.screenPosition, lineInfo, true, Vector2.One * 750f);
                }
            }

            // Draw individual wings.
            if (npc.localAI[0] == 1f)
            {
                for (int i = 0; i < Wings.Length; i++)
                    DrawWings(npc.Center - Main.screenPosition, Wings[i].WingRotation, Wings[i].WingRotationDifferenceMovingAverage, npc.rotation, 1f, npc.Opacity);
            }

            float crystalDrawOffset = 0f;
            Texture2D crystalTexture = TextureAssets.Extra[186].Value;
            Rectangle crystalFrame = crystalTexture.Frame();
            Vector2 crystalOrigin = crystalFrame.Size() * 0.5f;
            Vector2 crystalDrawPosition = npc.Center;
            switch (frame)
            {
                case 1:
                case 6:
                    crystalDrawOffset -= 10f;
                    break;
                case 3:
                case 5:
                    crystalDrawOffset += 10f;
                    break;
                case 4:
                case 12:
                case 13:
                case 14:
                case 15:
                    crystalDrawOffset += 18f;
                    break;
                case 7:
                case 8:
                    crystalDrawOffset -= 14f;
                    break;
                case 9:
                    crystalDrawOffset -= 16f;
                    break;
                case 10:
                    crystalDrawOffset -= 18f;
                    break;
                case 11:
                    crystalDrawOffset += 20f;
                    break;
                case 20:
                    crystalDrawOffset -= 14f;
                    break;
                case 21:
                case 23:
                    crystalDrawOffset -= 18f;
                    break;
                case 22:
                    crystalDrawOffset -= 22f;
                    break;
            }

            crystalDrawPosition.Y += crystalDrawOffset;
            if (npc.rotation != 0f)
                crystalDrawPosition = crystalDrawPosition.RotatedBy(npc.rotation, npc.Bottom);

            spriteBatch.Draw(crystalTexture, crystalDrawPosition - Main.screenPosition, crystalFrame, color, npc.rotation, crystalOrigin, 1f, SpriteEffects.FlipHorizontally, 0f);
            GameShaders.Misc["QueenSlime"].Apply();

            spriteBatch.EnterShaderRegion();

            // Draw afterimages.
            int afterimageCount = 9;
            for (int i = afterimageCount - 1; i >= 0; i--)
            {
                // The shader can be a bit weird when many afterimages are applied on top of each other, so a fade-out effect is applied based on how clumped together they are.
                float opacity = Utils.Remap(npc.position.Distance(npc.oldPos[1]), 3f, 18f, 0.3f, 0.67f);
                Color localColor = npc.GetAlpha(color) * (1f - i / (float)afterimageCount) * opacity;

                Vector2 drawBottom = Vector2.Lerp(npc.oldPos[i], npc.position, 0.6f) + new Vector2(npc.width * 0.5f, npc.height) - Main.screenPosition;
                drawBottom.Y += 2f;

                DrawData drawData = new(texture, drawBottom, frameThing, localColor, npc.rotation, origin, npc.scale, SpriteEffects.FlipHorizontally, 0);
                GameShaders.Misc["QueenSlime"].Apply(drawData);
                drawData.Draw(spriteBatch);
            }
            spriteBatch.ExitShaderRegion();

            // Draw the crown.
            Texture2D crownTexture = TextureAssets.Extra[177].Value;
            frameThing = crownTexture.Frame();
            origin = frameThing.Size() * 0.5f;

            if (npc.ai[3] == 1f)
                spriteBatch.Draw(crownTexture, CrownPosition(npc) - Main.screenPosition, frameThing, color * npc.Opacity, npc.rotation, origin, 1f, SpriteEffects.FlipHorizontally, 0f);
            return false;
        }

        public static void DrawWings(Vector2 drawPosition, float wingRotation, float rotationDifferenceMovingAverage, float generalRotation, float fadeInterpolant, float opacity)
        {
            Main.spriteBatch.SetBlendState(BlendState.Additive);

            Texture2D wingsTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/AdultEidolonWyrm/TerminusWing").Value;
            Vector2 leftWingOrigin = wingsTexture.Size() * new Vector2(1f, 0.86f);
            Vector2 rightWingOrigin = leftWingOrigin;
            rightWingOrigin.X = wingsTexture.Width - rightWingOrigin.X;
            Color wingsDrawColor = Color.Lerp(Color.Transparent, Color.HotPink, fadeInterpolant);
            Color wingsDrawColorWeak = Color.Lerp(Color.Transparent, Color.Cyan * 0.6f, fadeInterpolant);

            // Wings become squished the faster they're moving, to give an illusion of 3D motion.
            float squishOffset = MathF.Min(0.7f, Math.Abs(rotationDifferenceMovingAverage) * 3.5f);

            // Draw multiple instances of the wings. This includes afterimages based on how quickly they're flapping.
            Vector2 scale = new Vector2(1f, 1f - squishOffset) * fadeInterpolant;
            for (int i = 4; i >= 0; i--)
            {
                // Make wings slightly brighter when they're moving at a fast angular pace.
                Color wingColor = Color.Lerp(wingsDrawColor, wingsDrawColorWeak, i / 4f) * Utils.Remap(rotationDifferenceMovingAverage, 0f, 0.04f, 0.66f, 0.75f) * opacity;

                float rotationOffset = i * MathF.Min(rotationDifferenceMovingAverage, 0.16f) * (1f - squishOffset) * 0.5f;
                float currentWingRotation = wingRotation + rotationOffset;

                Main.spriteBatch.Draw(wingsTexture, drawPosition, null, wingColor, generalRotation + currentWingRotation, leftWingOrigin, scale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(wingsTexture, drawPosition, null, wingColor, generalRotation - currentWingRotation, rightWingOrigin, scale, SpriteEffects.FlipHorizontally, 0f);
            }

            Main.spriteBatch.ResetBlendState();
        }

        #endregion Drawing and Frames

        #region Misc Utilities

        public static bool InPhase2(NPC npc) => npc.life < npc.lifeMax * Phase2LifeRatio;

        public static bool OnSolidGround(NPC npc)
        {
            bool solidGround = false;
            for (int i = -8; i < 8; i++)
            {
                Tile ground = ParanoidTileRetrieval((int)(npc.Bottom.X / 16f) + i, (int)(npc.Bottom.Y / 16f) + 1);
                bool notAFuckingTree = ground.TileType is not TileID.Trees and not TileID.PineTree and not TileID.PalmTree;
                if (ground.HasUnactuatedTile && notAFuckingTree && (Main.tileSolid[ground.TileType] || Main.tileSolidTop[ground.TileType]))
                {
                    solidGround = true;
                    break;
                }
            }
            return solidGround;
        }
        #endregion Misc Utilities

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Mods.InfernumMode.PetDialog.QueenSlimeTip1";
        }
        #endregion Tips
    }
}
