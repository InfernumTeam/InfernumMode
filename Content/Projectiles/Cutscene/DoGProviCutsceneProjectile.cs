using System.IO;
using CalamityMod;
using CalamityMod.NPCs.DevourerofGods;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
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

        public Vector2 InitialPortalPosition => Projectile.Center + Vector2.UnitX * 650f;

        public Vector2 SecondPortalPosition => Projectile.Center - Vector2.UnitX * 650f;

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
            JawRotation = 0.1f;
            int startTime = InitialPortalStartTime + 60;
            if (Timer > startTime)
                DoGHeadPosition = Vector2.Lerp(InitialPortalPosition, SecondPortalPosition - Vector2.UnitX * 4000f, Utils.GetLerpValue(startTime, startTime + DoGLifetime, Timer, true));
            if (Timer == startTime)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.DoGLaughSound, Projectile.Center);
                SoundEngine.PlaySound(DevourerofGodsHead.AttackSound, Projectile.Center);
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

            float secondPortalOpacity = Utils.GetLerpValue(SecondPortalStartTime, SecondPortalStartTime + PortalFadeTime, Timer, true) * Utils.GetLerpValue(TotalLifetime, TotalLifetime - PortalFadeTime, Timer, true);
            if (secondPortalOpacity > 0f)
                DrawPortal(SecondPortalPosition - Main.screenPosition, secondPortalOpacity);

            return false;
        }

        public float GetSegmentOpacity(float xPosition) => CalamityUtils.Convert01To010(Utils.GetLerpValue(InitialPortalPosition.X + 50, SecondPortalPosition.X - 50, xPosition, true));

        public void DrawCrystal()
        {

        }

        public void DrawSegments()
        {
            Texture2D headTextureAntimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2HeadAntimatter").Value;
            Texture2D glowTextureAntimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2HeadGlowAntimatter").Value;
            Texture2D jawTextureAntimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2JawAntimatter").Value;

            Texture2D bodyTexture2Antimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2BodyAntimatter").Value;
            Texture2D glowmaskTexture2Antimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2BodyGlowAntimatter").Value;

            Texture2D tailTexture2Antimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2TailAntimatter").Value;
            Texture2D tailGlowmaskTexture2Antimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2TailGlowAntimatter").Value;

            int segmentCount = 81;
            Vector2 segmentDrawPosition = DoGHeadPosition;
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
                float segmentOpacity = GetSegmentOpacity(segmentDrawPosition.X) * Utils.GetLerpValue(InitialPortalStartTime + 60 + DoGLifetime, InitialPortalStartTime + DoGLifetime, Timer, true);
                if (segmentOpacity > 0)
                {
                    Main.spriteBatch.Draw(textureToDraw, segmentDrawPosition - Main.screenPosition, null, Color.White * segmentOpacity, -PiOver2, textureToDraw.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(glowmaskToDraw, segmentDrawPosition - Main.screenPosition, null, Color.White * segmentOpacity, -PiOver2, glowmaskToDraw.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
                }
            }

            float headOpacity = GetSegmentOpacity(DoGHeadPosition.X);
            Vector2 jawOrigin = jawTextureAntimatter.Size() * 0.5f;
            Vector2 jawPositionMain = DoGHeadPosition - Main.screenPosition;
            jawPositionMain -= headTextureAntimatter.Size() * Projectile.scale * 0.5f;
            jawPositionMain += headTextureAntimatter.Size() * 0.5f * Projectile.scale + new Vector2(0f, 4f);
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
                Main.spriteBatch.Draw(jawTextureAntimatter, jawPosition, null, Color.White * 0.7f, rotation + JawRotation * i, jawOrigin, 1f, jawSpriteEffect, 0f);
            }

            Main.spriteBatch.Draw(headTextureAntimatter, DoGHeadPosition - Main.screenPosition, null, Color.White * 0.7f * headOpacity, -PiOver2, headTextureAntimatter.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glowTextureAntimatter, DoGHeadPosition - Main.screenPosition, null, Color.White * headOpacity, -PiOver2, glowTextureAntimatter.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
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
