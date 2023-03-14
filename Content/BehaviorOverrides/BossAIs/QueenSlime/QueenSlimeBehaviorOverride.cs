using CalamityMod;
using CalamityMod.NPCs.SlimeGod;
using CalamityMod.Particles;
using InfernumMode.Common.Graphics;
using InfernumMode.Common.Graphics.Particles;
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

        public const float Phase2LifeRatio = 0.625f;

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio
        };

        #region Fields, Properties, and Enumerations
        public enum QueenSlimeAttackType
        {
            SpawnAnimation,
            BasicHops
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

            public static CurveSegment Flap => new(EasingType.PolyIn, 0.5f, Anticipation.EndingHeight(), -2.09f, 4);

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
                        WingRotation = PiecewiseAnimation((animationCompletion + MathHelper.Lerp(instanceRatio, 0f, 0.5f)) % 1f, Anticipation, Flap, Rest, Recovery);
                        break;
                }

                WingRotationDifferenceMovingAverage = MathHelper.Lerp(WingRotationDifferenceMovingAverage, WingRotation - PreviousWingRotation, 0.15f);
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
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float wingAnimationTimer = ref npc.ai[2];
            ref float usingWings = ref npc.localAI[0];
            ref float wingMotionState = ref npc.localAI[1];

            if (npc.localAI[3] == 0f)
            {
                Wings = new QueenSlimeWing[1];
                npc.localAI[3] = 1f;
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

            switch ((QueenSlimeAttackType)attackType)
            {
                case QueenSlimeAttackType.SpawnAnimation:
                    DoBehavior_SpawnAnimation(npc, target, ref attackTimer, ref usingWings, ref wingMotionState);
                    break;
                case QueenSlimeAttackType.BasicHops:
                    DoBehavior_BasicHops(npc, target, ref attackTimer, ref wingMotionState);
                    break;
            }

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
                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + fallAcceleration, startingFallSpeed, endingFallSpeed);
            else
                npc.velocity.Y = 0f;

            // Handle ground hit effects when ready.
            if (npc.collideY && attackTimer >= 5f && hasHitGround == 0f)
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

        public static void DoBehavior_BasicHops(NPC npc, Player target, ref float attackTimer, ref float wingMotionState)
        {
            int jumpTime = 24;
            int slamDelay = 30;
            int slamHoverTime = 35;
            int crystalID = ModContent.ProjectileType<FalllingCrystal>();
            float horizontalJumpSpeed = MathHelper.Distance(target.Center.X, npc.Center.X) * 0.012f + 16f;
            float baseVerticalJumpSpeed = 23f;
            float fallAcceleration = 0.9f;
            ref float jumpState = ref npc.Infernum().ExtraAI[0];
            ref float groundCollisionY = ref npc.Infernum().ExtraAI[1];
            ref float didSlamGroundHitEffects = ref npc.Infernum().ExtraAI[2];

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

                if (Collision.SolidCollision(npc.BottomLeft, npc.width, 4))
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
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, crystalID, 140, 0f);
            }
            
            // Begin the slam.
            if (jumpState == 1f && attackTimer >= slamDelay + 45f && Math.Abs(npc.velocity.Y) <= 0.9f)
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
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.15f);
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
                if (Utilities.ActualSolidCollisionTop(npc.TopLeft, npc.width, npc.height + 16) && didSlamGroundHitEffects == 0f)
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

            if (jumpState == 3f && attackTimer >= 108f && didSlamGroundHitEffects == 1f)
                SelectNextAttack(npc);
        }

        public static void SelectNextAttack(NPC npc)
        {
            QueenSlimeAttackType previousAttack = (QueenSlimeAttackType)npc.ai[0];
            QueenSlimeAttackType nextAttack = QueenSlimeAttackType.BasicHops;

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
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
            Vector2 drawBottom = npc.Bottom - Main.screenPosition;
            drawBottom.Y += 2f;
            int frame = npc.frame.Y / npc.frame.Height;
            Rectangle frameThing = texture.Frame(2, Main.npcFrameCount[npc.type], frame / Main.npcFrameCount[npc.type], frame % Main.npcFrameCount[npc.type]);
            frameThing.Inflate(0, -2);
            Vector2 origin = frameThing.Size() * new Vector2(0.5f, 1f);
            Color color = Color.Lerp(Color.White, lightColor, 0.5f);
            if (npc.localAI[0] == 1f)
            {
                for (int i = 0; i < Wings.Length; i++)
                    DrawWings(npc.Center - Main.screenPosition, Wings[i].WingRotation, Wings[i].WingRotationDifferenceMovingAverage, npc.rotation, 1f);
            }

            Texture2D crystalTexture = TextureAssets.Extra[186].Value;
            Rectangle crystalFrame = crystalTexture.Frame();
            Vector2 crystalOrigin = crystalFrame.Size() * 0.5f;
            Vector2 crystalDrawPosition = npc.Center;
            float crystalDrawOffset = 0f;
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

            crystalDrawPosition -= Main.screenPosition;

            spriteBatch.Draw(crystalTexture, crystalDrawPosition, crystalFrame, color, npc.rotation, crystalOrigin, 1f, SpriteEffects.FlipHorizontally, 0f);
            GameShaders.Misc["QueenSlime"].Apply();

            spriteBatch.EnterShaderRegion();

            DrawData drawData = new(texture, drawBottom, frameThing, npc.GetAlpha(color), npc.rotation, origin, npc.scale, SpriteEffects.FlipHorizontally, 0);
            GameShaders.Misc["QueenSlime"].Apply(drawData);
            drawData.Draw(spriteBatch);
            spriteBatch.ExitShaderRegion();

            Texture2D crownTexture = TextureAssets.Extra[177].Value;
            frameThing = crownTexture.Frame();
            origin = frameThing.Size() * 0.5f;

            spriteBatch.Draw(crownTexture, CrownPosition(npc) - Main.screenPosition, frameThing, color, npc.rotation, origin, 1f, SpriteEffects.FlipHorizontally, 0f);
            return false;
        }

        public static void DrawWings(Vector2 drawPosition, float wingRotation, float rotationDifferenceMovingAverage, float generalRotation, float fadeInterpolant)
        {
            Main.spriteBatch.SetBlendState(BlendState.Additive);

            Texture2D wingsTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/AdultEidolonWyrm/TerminusWing").Value;
            Vector2 leftWingOrigin = wingsTexture.Size() * new Vector2(1f, 0.86f);
            Vector2 rightWingOrigin = leftWingOrigin;
            rightWingOrigin.X = wingsTexture.Width - rightWingOrigin.X;
            Color wingsDrawColor = Color.Lerp(Color.Transparent, Color.HotPink, fadeInterpolant);
            Color wingsDrawColorWeak = Color.Lerp(Color.Transparent, Color.Cyan * 0.6f, fadeInterpolant);

            // Wings become squished the faster they're moving, to give an illusion of 3D motion.
            float squishOffset = MathHelper.Min(0.7f, Math.Abs(rotationDifferenceMovingAverage) * 3.5f);

            // Draw multiple instances of the wings. This includes afterimages based on how quickly they're flapping.
            Vector2 scale = new Vector2(1f, 1f - squishOffset) * fadeInterpolant;
            for (int i = 4; i >= 0; i--)
            {
                // Make wings slightly brighter when they're moving at a fast angular pace.
                Color wingColor = Color.Lerp(wingsDrawColor, wingsDrawColorWeak, i / 4f) * Utils.Remap(rotationDifferenceMovingAverage, 0f, 0.04f, 0.66f, 0.75f);

                float rotationOffset = i * MathHelper.Min(rotationDifferenceMovingAverage, 0.16f) * (1f - squishOffset) * 0.5f;
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
                Tile ground = CalamityUtils.ParanoidTileRetrieval((int)(npc.Bottom.X / 16f) + i, (int)(npc.Bottom.Y / 16f) + 1);
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
            yield return n => "Keep your feet working! This gelatinous queen will stop at nothing to crush her foes!";
            yield return n => "Short hops may help better than trying to fly away from all the crystal shrapnel!";
        }
        #endregion Tips
    }
}
