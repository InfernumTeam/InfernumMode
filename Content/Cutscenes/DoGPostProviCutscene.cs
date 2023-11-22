using CalamityMod.NPCs;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.Particles;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Common.Graphics.ScreenEffects;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Providence;
using InfernumMode.Content.Projectiles.Wayfinder;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.Content.Cutscenes
{
    public class DoGPostProviCutscene : Cutscene
    {
        #region Instance Fields
        public float JawRotation;

        public Vector2 Velocity;

        public Vector2 DoGHeadPosition;
        #endregion

        #region Static Properties
        public static int InitialPortalStartTime => 270;

        public static int InitialPortalEndTime => 360;

        public static float PortalFadeTime => 20;

        public static int StartTime => InitialPortalStartTime + 60;

        public static int JawOpenTime => 60;

        public static int SlowddownTime => 70;

        public static int ChompTime => 15;

        public static int AfterHoldTime => 50;

        public static int WhiteningWait => 45;

        public static int RocksDelay => 90;

        public static Vector2 InitialPortalOffset => Vector2.UnitX * 650f;

        public float FirstPortalOpacity => Utils.GetLerpValue(InitialPortalStartTime, InitialPortalStartTime + PortalFadeTime, Timer, true) * Utils.GetLerpValue(InitialPortalEndTime, InitialPortalEndTime - PortalFadeTime * 2f, Timer, true);

        public static Color TimeColor
        {
            get
            {
                Color timeColor = Color.Lerp(WayfinderSymbol.Colors[0], WayfinderSymbol.Colors[2], 0.2f);
                if (ProvidenceBehaviorOverride.IsEnraged)
                    timeColor = Color.DeepSkyBlue;
                return timeColor;
            }
        }
        #endregion

        #region Overrides
        public override int CutsceneLength => StartTime + SlowddownTime + ChompTime + AfterHoldTime;

        public override void OnBegin()
        {
            // Ensure these are appropriately reset.
            JawRotation = 0.05f;
            Velocity = Vector2.UnitX * -30;
            DoGHeadPosition = Vector2.Zero;

            ScreenEffectSystem.SetMovieBarEffect(0.15f, CutsceneLength, timer =>
                Utils.GetLerpValue(0f, InitialPortalStartTime, timer, true) * Utilities.EaseInOutCubic(Utils.GetLerpValue(CutsceneLength, CutsceneLength - 30, timer, true)));
        }

        public override void OnEnd() => WorldSaveSystem.HasSeenDoGCutscene = true;

        public override void Update()
        {
            // End suddenly if provi is dead.
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.holyBoss) || !Main.npc[CalamityGlobalNPC.holyBoss].active)
            {
                EndAbruptly = true;
                return;
            }

            Vector2 crystalCenter = Main.npc[CalamityGlobalNPC.holyBoss].Center + Vector2.UnitY * 55f;

            if (Timer > RocksDelay && Timer < StartTime + WhiteningWait)
            {
                int rockSpawnRate = (int)Lerp(10f, 1f, (Timer - RocksDelay) / (StartTime + WhiteningWait - RocksDelay));

                if (Timer % rockSpawnRate == 0)
                {
                    Vector2 position;
                    do
                    {
                        position = crystalCenter + Main.rand.NextVector2Circular(600f, 600f);
                    }
                    while (position.WithinRange(crystalCenter, 200f));

                    int lifeTime = 90;
                    Vector2 velocity = position.DirectionTo(crystalCenter) * (position.Distance(crystalCenter) / lifeTime) * 1.2f;
                    ProfanedRockParticle rock = new(position, velocity, Color.White, Main.rand.NextFloat(1.2f, 1.5f), lifeTime, gravity: false, fadeIn: true);
                    GeneralParticleHandler.SpawnParticle(rock);

                    for (int j = 0; j < 3; j++)
                    {
                        position = crystalCenter + Main.rand.NextVector2Circular(40f, 40f);

                        var fire = new MediumMistParticle(position, Vector2.Zero, TimeColor, Color.Gray, Main.rand.NextFloat(0.8f, 1.2f), 210f);
                        GeneralParticleHandler.SpawnParticle(fire);
                    }
                }
            }

            float zoom = Lerp(0f, 0.45f, ((float)Timer / InitialPortalStartTime).Saturate());
            ZoomSystem.SetZoomEffect(zoom);

            if (Timer == StartTime)
            {
                SoundEngine.PlaySound(DevourerofGodsHead.AttackSound, crystalCenter);

                // Make the player go flying away from dog.
                if (Main.LocalPlayer.WithinRange(crystalCenter, 10000f))
                {
                    Vector2 pushbackCenter = InitialPortalOffset + crystalCenter;
                    float pushbackForce = 50f * Lerp(0.65f, 1f, Utils.GetLerpValue(0f, 1000f, Main.LocalPlayer.Distance(pushbackCenter), true));
                    Main.LocalPlayer.velocity += Main.LocalPlayer.SafeDirectionTo(pushbackCenter) * -pushbackForce;
                }

                for (int i = 0; i < 70; i++)
                {
                    float scale = Main.rand.NextFloat(0.8f, 1.16f);
                    Color particleColor = Color.Lerp(Color.Fuchsia, Color.Cyan, Main.rand.NextFloat(0.1f, 0.9f));
                    Vector2 particleSpawnOffset = Main.rand.NextVector2Circular(50, 50) * new Vector2(1.75f, 0.75f);
                    Vector2 particleVelocity = particleSpawnOffset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(1.5f, 3f);
                    SquishyLightParticle light = new(InitialPortalOffset + crystalCenter + particleSpawnOffset, particleVelocity, scale, particleColor, 60, 1f, 3f);
                    GeneralParticleHandler.SpawnParticle(light);
                }
            }
            if (Timer > StartTime)
            {
                DoGHeadPosition += Velocity;

                // Perform whitening effects
                if (Timer > StartTime + WhiteningWait)
                    CeaselessVoidWhiteningEffect.WhiteningInterpolant = Lerp(0f, 1f, (((float)Timer - StartTime - WhiteningWait) / 5).Saturate());

                // Initially, slow down and open the jaw.
                if (Timer < StartTime + SlowddownTime)
                {
                    Velocity *= 0.961f;
                    JawRotation = Lerp(0.05f, 0.75f, (((float)Timer - StartTime) / JawOpenTime).Saturate());

                }
                // Then, lunge forward and bite down.
                else if (Timer <= StartTime + SlowddownTime + ChompTime)
                {
                    float interpolant = ((float)Timer - StartTime - SlowddownTime) / ChompTime;
                    Velocity = Vector2.Lerp(Vector2.UnitX * -13f, Vector2.Zero, interpolant);
                    JawRotation = Lerp(0.75f, -0.1f, interpolant.Saturate());
                }
            }

            if (Timer == StartTime + SlowddownTime + (int)(ChompTime * 0.5f))
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.DoGLaughSound, crystalCenter);
                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceScreamSound, crystalCenter);

                if (Main.npc.IndexInRange(CalamityGlobalNPC.holyBoss))
                {
                    Player target = Main.player[Main.npc[CalamityGlobalNPC.holyBoss].target];
                    target.Infernum_Camera().CurrentScreenShakePower = 20f;
                }

                ScreenEffectSystem.SetBlurEffect(crystalCenter, 1.5f, 120);
                ScreenEffectSystem.SetFlashEffect(crystalCenter, 1.5f, 120);
            }

            if (FirstPortalOpacity > 0 && Timer < StartTime)
            {
                // Spawn portal particles.
                for (int i = 0; i < 3; i++)
                {
                    if (Main.rand.NextFloat() > FirstPortalOpacity)
                        continue;

                    float scale = Main.rand.NextFloat(0.5f, 0.66f);
                    Color particleColor = Color.Lerp(Color.Fuchsia, Color.Cyan, Main.rand.NextFloat(0.1f, 0.9f));
                    Vector2 particleSpawnOffset = Main.rand.NextVector2Unit() * Main.rand.NextFloat(0.15f, 1f) * 712f;
                    Vector2 particleVelocity = particleSpawnOffset * -0.05f;
                    SquishyLightParticle light = new(InitialPortalOffset + crystalCenter + particleSpawnOffset, particleVelocity, scale, particleColor, 40, 1f, 4f);
                    GeneralParticleHandler.SpawnParticle(light);
                }
            }
        }

        public override void DrawToWorld(SpriteBatch spriteBatch)
        {
            Vector2 crystalCenter = Main.npc[CalamityGlobalNPC.holyBoss].Center + Vector2.UnitY * 55f;

            // Draw the crystal.
            DrawCrystal(crystalCenter);

            // Draw DoG.
            if (Timer > InitialPortalStartTime + 60f)
                DrawSegments(crystalCenter);

            // Draw the first portal.
            if (FirstPortalOpacity > 0f)
                DrawPortal(InitialPortalOffset + crystalCenter - Main.screenPosition, FirstPortalOpacity);
        }
        #endregion

        #region Drawing
        public void DrawBlackOverlays(float opacity)
        {
            Vector2 crystalCenter = Main.npc[CalamityGlobalNPC.holyBoss].Center + Vector2.UnitY * 55f;

            DrawCrystal(crystalCenter, Color.Black * opacity);

            if (Timer > InitialPortalStartTime + 60f)
            {
                int fullTime = StartTime + SlowddownTime + (int)(ChompTime * 0.5f);

                opacity *= Utils.GetLerpValue(fullTime + 10, fullTime, Timer, true);
                DrawSegments(crystalCenter, Color.Black * opacity);
            }
        }

        public void DrawCrystal(Vector2 crystalCenter, Color? overrideColor = null)
        {
            float backstuffOpacity = 1f - CeaselessVoidWhiteningEffect.WhiteningInterpolant;

            Texture2D fatCrystalTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Providence/ProvidenceCrystal").Value;
            Texture2D bloomTexture = InfernumTextureRegistry.BloomFlare.Value;

            Main.spriteBatch.Draw(bloomTexture, crystalCenter - Main.screenPosition, null, TimeColor with { A = 0 } * Lerp(0.3f, 0.6f, (1f + Sin(PI * Main.GlobalTimeWrappedHourly * 1.1f)) * 0.5f) *
                backstuffOpacity, Main.GlobalTimeWrappedHourly, bloomTexture.Size() * 0.5f, 0.3f, SpriteEffects.None, 0f);

            Main.spriteBatch.Draw(bloomTexture, crystalCenter - Main.screenPosition, null, Color.Lerp(TimeColor, Color.White, 0.3f) with { A = 0 } *
                Lerp(0.3f, 0.6f, (1f + Sin(PI * Main.GlobalTimeWrappedHourly * 1.4f)) * 0.5f) * backstuffOpacity,
                -Main.GlobalTimeWrappedHourly, bloomTexture.Size() * 0.5f, 0.3f, SpriteEffects.None, 0f);

            float crystalScale = Lerp(0.9f, 1.1f, (1f + Sin(PI * Main.GlobalTimeWrappedHourly * 0.85f)) * 0.5f);

            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = (TwoPi * i / 8f + Main.GlobalTimeWrappedHourly).ToRotationVector2() * Lerp(4f, 10f, (1f + Sin(PI * Main.GlobalTimeWrappedHourly)) * 0.5f);
                Color glowColor = Color.LightPink with { A = 0 } * 0.5f;
                Main.spriteBatch.Draw(fatCrystalTexture, crystalCenter + offset - Main.screenPosition, null, glowColor * backstuffOpacity, 0f, fatCrystalTexture.Size() * 0.5f, crystalScale, SpriteEffects.None, 0f);
            }

            float threshold = 0.65f;
            if (Timer >= StartTime + SlowddownTime + (int)(ChompTime * 0.5f))
            {
                threshold = 0.53f;
                if (Timer >= StartTime + SlowddownTime + (int)(ChompTime * 0.5f) + AfterHoldTime / 3)
                {
                    threshold = Lerp(threshold, 0.0f, Utilities.EaseInOutCubic(((Timer - StartTime - SlowddownTime - ChompTime / 2 - AfterHoldTime / 3) / (ChompTime / 2 + (AfterHoldTime / 3) * 2)).Saturate()));
                }
            }

            Main.spriteBatch.EnterShaderRegion();
            Effect crack = InfernumEffectsRegistry.CrystalCrackShader.GetShader().Shader;
            crack.Parameters["resolution"]?.SetValue(Utilities.CreatePixelationResolution(fatCrystalTexture.Size()));
            crack.Parameters["threshold"]?.SetValue(threshold);
            Utilities.SetTexture1(InfernumTextureRegistry.WavyNoise.Value);
            crack.CurrentTechnique.Passes[0].Apply();

            Main.spriteBatch.Draw(fatCrystalTexture, crystalCenter - Main.screenPosition, null, overrideColor ?? Color.White, 0f, fatCrystalTexture.Size() * 0.5f, crystalScale, SpriteEffects.None, 0f);

            Main.spriteBatch.ExitShaderRegion();
        }

        public void DrawSegments(Vector2 crystalCenter, Color? overrideColor = null)
        {
            Texture2D headTextureAntimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2Head").Value;
            Texture2D glowTextureAntimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2HeadGlow").Value;
            Texture2D jawTextureAntimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2Jaw").Value;

            Texture2D bodyTexture2Antimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2Body").Value;
            Texture2D glowmaskTexture2Antimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2BodyGlow").Value;

            Texture2D tailTexture2Antimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2Tail").Value;
            Texture2D tailGlowmaskTexture2Antimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2TailGlow").Value;

            int segmentCount = 81;
            Vector2 segmentDrawPosition = DoGHeadPosition + InitialPortalOffset + crystalCenter + Vector2.UnitX * 250f;
            for (int i = 0; i < segmentCount; i++)
            {
                Texture2D textureToDraw = bodyTexture2Antimatter;
                Texture2D glowmaskToDraw = glowmaskTexture2Antimatter;
                if (i == segmentCount - 1)
                {
                    textureToDraw = tailTexture2Antimatter;
                    glowmaskToDraw = tailGlowmaskTexture2Antimatter;
                }

                segmentDrawPosition += Vector2.UnitX * textureToDraw.Width * 0.8f;
                float segmentOpacity = 1f; /*GetSegmentOpacity(segmentDrawPosition.X) * */ //Utils.GetLerpValue(InitialPortalStartTime + 60 + DoGLifetime, InitialPortalStartTime + DoGLifetime, Timer, true);
                if (segmentOpacity > 0)
                {
                    Main.spriteBatch.Draw(textureToDraw, segmentDrawPosition - Main.screenPosition, null, overrideColor ?? Color.White * segmentOpacity, -PiOver2, textureToDraw.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(glowmaskToDraw, segmentDrawPosition - Main.screenPosition, null, overrideColor ?? Color.White * segmentOpacity, -PiOver2, glowmaskToDraw.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
                }
            }

            float headOpacity = 1f;//GetSegmentOpacity(DoGHeadPosition.X);
            Vector2 jawOrigin = jawTextureAntimatter.Size() * 0.5f;
            Vector2 jawPositionMain = DoGHeadPosition + InitialPortalOffset + crystalCenter + Vector2.UnitX * 250f - Main.screenPosition;
            jawPositionMain -= headTextureAntimatter.Size() * 0.5f;
            jawPositionMain += headTextureAntimatter.Size() * 0.5f;

            // Draw each jaw.
            for (int i = -1; i <= 1; i += 2)
            {
                float jawBaseOffset = 42f;
                SpriteEffects jawSpriteEffect = SpriteEffects.None;
                if (i == 1)
                    jawSpriteEffect |= SpriteEffects.FlipHorizontally;

                float rotation = (-Vector2.UnitY).ToRotation();
                Vector2 jawPosition = jawPositionMain;
                jawPosition += Vector2.UnitX.RotatedBy(rotation + JawRotation * i) * i * (jawBaseOffset + Sin(JawRotation) * 24f);
                jawPosition -= Vector2.UnitY.RotatedBy(rotation) * (58f + Sin(JawRotation) * 30f);
                Main.spriteBatch.Draw(jawTextureAntimatter, jawPosition, null, overrideColor ?? Color.White * 0.7f, rotation + JawRotation * i, jawOrigin, 1f, jawSpriteEffect, 0f);
            }

            Main.spriteBatch.Draw(headTextureAntimatter, DoGHeadPosition + InitialPortalOffset + crystalCenter + Vector2.UnitX * 250f - Main.screenPosition, null, overrideColor ?? Color.White * 0.7f * headOpacity, -PiOver2, headTextureAntimatter.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glowTextureAntimatter, DoGHeadPosition + InitialPortalOffset + crystalCenter + Vector2.UnitX * 250f - Main.screenPosition, null, overrideColor ?? Color.White * headOpacity, -PiOver2, glowTextureAntimatter.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
        }

        public static void DrawPortal(Vector2 portalPosition, float opacity)
        {
            Texture2D noiseTexture = InfernumTextureRegistry.VoronoiShapes.Value;
            Vector2 origin = noiseTexture.Size() * 0.5f;

            Main.spriteBatch.EnterShaderRegion();

            GameShaders.Misc["CalamityMod:DoGPortal"].UseOpacity(opacity);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseColor(new Color(0.2f, 1f, 1f, 0f));
            GameShaders.Misc["CalamityMod:DoGPortal"].UseSecondaryColor(new Color(1f, 0.2f, 1f, 0f));
            GameShaders.Misc["CalamityMod:DoGPortal"].Apply();

            Main.spriteBatch.Draw(noiseTexture, portalPosition, null, Color.White, 0f, origin, new Vector2(1.25f, 2.75f) * opacity, SpriteEffects.None, 0f);
            Main.spriteBatch.ExitShaderRegion();
        }
        #endregion
    }
}
