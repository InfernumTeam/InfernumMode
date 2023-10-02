using System.IO;
using CalamityMod;
using CalamityMod.NPCs.DevourerofGods;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.ScreenEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Cutscene
{
    public class DoGProviCutsceneProjectile : ModProjectile
    {
        public ref float Timer => ref Projectile.ai[0];

        public ref float JawRotation => ref Projectile.ai[1];

        public static int DoGLifetime => 90;

        public static int TotalLifetime => 340;

        public static int InitialPortalStartTime => 70;

        public static int InitialPortalEndTime => 200;

        public static int SecondPortalStartTime => 150;

        public static float PortalFadeTime => 20;

        public Vector2 DoGHeadPosition
        {
            get;
            set;
        }

        public Vector2 InitialPortalPosition => Projectile.Center + Vector2.UnitX * 900f;

        public Vector2 SecondPortalPosition => Projectile.Center - Vector2.UnitX * 650f;

        private static Projectile myself;

        public static Projectile Myself
        {
            get
            {
                if (myself == null || !myself.active)
                    return null;

                return myself;
            }
            private set => myself = value;
        }

        public override string Texture => InfernumTextureRegistry.InvisPath;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 1;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.Opacity = 0;
            Projectile.timeLeft = TotalLifetime;
        }

        public override void AI()
        {
            Myself = Projectile;
            if (Timer == 0f)
            {
                JawRotation = 0.05f;
            }
            int startTime = InitialPortalStartTime + 60;
            int jawOpenTime = 60;
            int slowddownTime = 150;
            int chompTime = 20;
            int whiteningWait = 40;
            if  (Timer == startTime)
                SoundEngine.PlaySound(DevourerofGodsHead.AttackSound, Projectile.Center);
            if (Timer > startTime)
            {
                DoGHeadPosition += Projectile.velocity; //Vector2.Lerp(InitialPortalPosition, InitialPortalPosition - Vector2.UnitX * 600f, Utils.GetLerpValue(startTime, startTime + DoGLifetime, Timer, true));

                if (Timer < startTime + slowddownTime)
                {
                    Projectile.velocity *= 0.96f;
                    JawRotation = Lerp(0.05f, 0.75f, ((Timer - startTime) / jawOpenTime).Saturate());

                }
                else if (Timer <= startTime + slowddownTime + chompTime)
                {
                    float interpolant = (Timer - startTime - slowddownTime) / chompTime;
                    Projectile.velocity = Vector2.Lerp(Vector2.UnitX * -10f, Vector2.Zero, interpolant);
                    JawRotation = Lerp(0.75f, -0.03f, interpolant.Saturate());
                }

                if (Timer > startTime)
                    CeaselessVoidWhiteningEffect.WhiteningInterpolant = (Timer / startTime + whiteningWait).Saturate();
            }

            if (Timer == startTime + slowddownTime + (int)(chompTime * 0.5f))
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.DoGLaughSound, Projectile.Center);
                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceScreamSound, Projectile.Center);

            }
            Timer++;
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            DoGHeadPosition = reader.ReadVector2();
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(DoGHeadPosition);
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            // Draw the crystal shards.
            DrawCrystal();
            // Draw DoG.
            if (Timer > InitialPortalStartTime + 60f)
                DrawSegments();

            // Draw the first portal.
            float firstPortalOpacity = Utils.GetLerpValue(InitialPortalStartTime, InitialPortalStartTime + PortalFadeTime, Timer, true) * Utils.GetLerpValue(InitialPortalEndTime, InitialPortalEndTime - PortalFadeTime, Timer, true);
            if (firstPortalOpacity > 0f)
                DrawPortal(InitialPortalPosition - Main.screenPosition, firstPortalOpacity);

            //float secondPortalOpacity = Utils.GetLerpValue(SecondPortalStartTime, SecondPortalStartTime + PortalFadeTime, Timer, true) * Utils.GetLerpValue(TotalLifetime, TotalLifetime - PortalFadeTime, Timer, true);
            //if (secondPortalOpacity > 0f)
            //    DrawPortal(SecondPortalPosition - Main.screenPosition, secondPortalOpacity);

            return false;
        }

        public void DrawBlackOverlays(float opacity)
        {
            DrawCrystal(Color.Black * opacity);

            if (Timer > InitialPortalStartTime + 60f)
            {
                int startTime = InitialPortalStartTime + 60;
                int slowddownTime = 150;
                int chompTime = 20;

                int fullTime = startTime + slowddownTime + (int)(chompTime * 0.5f);

                opacity *= Utils.GetLerpValue(fullTime + 10, fullTime, Timer, true);
                DrawSegments(Color.Black * opacity);
            }
        }

        public float GetSegmentOpacity(float xPosition) => CalamityUtils.Convert01To010(Utils.GetLerpValue(InitialPortalPosition.X + 50, SecondPortalPosition.X - 50, xPosition, true));

        public void DrawCrystal(Color? overrideColor = null)
        {
            //int crystalAmount = 11;

            //Vector2[] shardOffsets = new Vector2[11]
            //{
            //    Vector2.Zero,
            //    Vector2.Zero,
            //    Vector2.Zero,
            //    Vector2.Zero,
            //    Vector2.Zero,
            //    Vector2.Zero,
            //    Vector2.Zero,
            //    Vector2.Zero,
            //    Vector2.Zero,
            //    Vector2.Zero,
            //    Vector2.Zero
            //};

            //Vector2 baseShardPosition = Projectile.Center + Vector2.UnitY * -250f - Main.screenPosition;

            //for (int i = 1; i <= crystalAmount; i++)
            //{
            //    Texture2D shardTexture = ModContent.Request<Texture2D>($"InfernumMode/Content/Projectiles/Cutscene/CrystalShards/CrystalBreak{i}").Value;

            //    Main.spriteBatch.Draw(shardTexture, baseShardPosition + shardOffsets[i - 1], null, Color.White, 0f, shardTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            //}

            Texture2D crystalTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Providence/ProvidenceCrystal").Value;

            // Draw with a shader if the override color is set.
            if (overrideColor != null)
            {
                int startTime = InitialPortalStartTime + 60;
                int slowddownTime = 150;
                int chompTime = 20;
                float threshold = 1f;
                if (Timer >= startTime + slowddownTime + (int)(chompTime * 0.5f))
                    threshold = 0.45f;

                Main.spriteBatch.EnterShaderRegion();
                Effect crack = InfernumEffectsRegistry.CrystalCrackShader.GetShader().Shader;
                crack.Parameters["resolution"]?.SetValue(Utilities.CreatePixelationResolution(crystalTexture.Size()));
                crack.Parameters["threshold"]?.SetValue(threshold);
                Utilities.SetTexture1(InfernumTextureRegistry.WavyNoise.Value);
                crack.CurrentTechnique.Passes[0].Apply();
                //Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, crack, Main.GameViewMatrix.TransformationMatrix);
            }

            Main.spriteBatch.Draw(crystalTexture, Projectile.Center - Main.screenPosition, null, overrideColor ?? Color.White, 0f, crystalTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);

            if (overrideColor != null)
                Main.spriteBatch.ExitShaderRegion();
        }

        public void DrawSegments(Color? overrideColor = null)
        {
            Texture2D headTextureAntimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2Head").Value;
            Texture2D glowTextureAntimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2HeadGlow").Value;
            Texture2D jawTextureAntimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2Jaw").Value;

            Texture2D bodyTexture2Antimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2Body").Value;
            Texture2D glowmaskTexture2Antimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2BodyGlow").Value;

            Texture2D tailTexture2Antimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2Tail").Value;
            Texture2D tailGlowmaskTexture2Antimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2TailGlow").Value;

            int segmentCount = 81;
            Vector2 segmentDrawPosition = DoGHeadPosition + InitialPortalPosition;
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
            Vector2 jawPositionMain = DoGHeadPosition + InitialPortalPosition - Main.screenPosition;
            jawPositionMain -= headTextureAntimatter.Size() * Projectile.scale * 0.5f;
            jawPositionMain += headTextureAntimatter.Size() * 0.5f * Projectile.scale;
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

            Main.spriteBatch.Draw(headTextureAntimatter, DoGHeadPosition + InitialPortalPosition - Main.screenPosition, null, overrideColor ?? Color.White * 0.7f * headOpacity, -PiOver2, headTextureAntimatter.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glowTextureAntimatter, DoGHeadPosition + InitialPortalPosition - Main.screenPosition, null, overrideColor ?? Color.White * headOpacity, -PiOver2, glowTextureAntimatter.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
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
    }
}
