using CalamityMod;
using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AquaticScourge
{
    public class FallingAcid : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public bool Telegraphed => Projectile.ai[1] == 1f;

        public static int TelegraphTime => 30;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Acid");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 480;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
            
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 35f, Time, true) * Utils.GetLerpValue(0f, 56f, Projectile.timeLeft, true);

            // Fall downward.
            Projectile.velocity.X *= 0.987f;
            Projectile.velocity.Y = Clamp(Projectile.velocity.Y + 0.4f, -40f, 13f);
            Projectile.rotation = Projectile.velocity.ToRotation() - PiOver2;
            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Cast a telegraph line before the acceleration gets super strong if necessary.
            float opacity = LumUtils.Convert01To010(Time / TelegraphTime);
            Vector2 drawPosition = Projectile.position + Projectile.Size * 0.5f - Main.screenPosition;
            if (opacity > 0f && Telegraphed)
            {
                Texture2D invisible = InfernumTextureRegistry.Invisible.Value;
                Effect laserScopeEffect = Filters.Scene["CalamityMod:PixelatedSightLine"].GetShader().Shader;
                laserScopeEffect.Parameters["sampleTexture2"].SetValue(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/CertifiedCrustyNoise").Value);
                laserScopeEffect.Parameters["noiseOffset"].SetValue(Main.GameUpdateCount * -0.003f);
                laserScopeEffect.Parameters["mainOpacity"].SetValue(Sqrt(opacity));
                laserScopeEffect.Parameters["Resolution"].SetValue(new Vector2(425f));
                laserScopeEffect.Parameters["laserAngle"].SetValue(-Projectile.velocity.ToRotation());
                laserScopeEffect.Parameters["laserWidth"].SetValue(0.003f + Pow(opacity, 4f) * (Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.001f + 0.001f));
                laserScopeEffect.Parameters["laserLightStrenght"].SetValue(5f);
                laserScopeEffect.Parameters["color"].SetValue(Color.Lerp(Color.Lime, Color.Olive, 0.7f).ToVector3());
                laserScopeEffect.Parameters["darkerColor"].SetValue(Color.Lerp(Color.Yellow, Color.SaddleBrown, 0.8f).ToVector3());
                laserScopeEffect.Parameters["bloomSize"].SetValue(0.06f + (1f - opacity) * 0.1f);
                laserScopeEffect.Parameters["bloomMaxOpacity"].SetValue(0.4f);
                laserScopeEffect.Parameters["bloomFadeStrenght"].SetValue(3f);

                Main.spriteBatch.EnterShaderRegion(BlendState.Additive);

                laserScopeEffect.CurrentTechnique.Passes[0].Apply();
                Main.spriteBatch.Draw(invisible, drawPosition, null, Color.White, 0f, invisible.Size() * 0.5f, opacity * 2500f, SpriteEffects.None, 0f);
                Main.spriteBatch.ExitShaderRegion();
            }

            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Color backAfterimageColor = Projectile.GetAlpha(new Color(85, 224, 60, 0) * 0.5f);
            for (int i = 0; i < 8; i++)
            {
                Vector2 drawOffset = (TwoPi * i / 8f).ToRotationVector2() * 4f;
                Main.spriteBatch.Draw(texture, drawPosition + drawOffset, null, backAfterimageColor, Projectile.rotation, origin, Projectile.scale, 0, 0f);
            }
            Utilities.DrawAfterimagesCentered(Projectile, new Color(117, 95, 133, 184) * Projectile.Opacity, ProjectileID.Sets.TrailingMode[Projectile.type], 2);

            return false;
        }
    }
}
