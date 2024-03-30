using CalamityMod;
using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.BrimstoneElemental
{
    public class RedFlameTelegraph : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public ref float TelegraphLength => ref Projectile.ai[1];

        public static int TelegraphTime => 25;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // Hello, github reader!
        // public override void SetStaticDefaults() => DisplayName.SetDefault("Fuck you nobody is going to ever see this name");

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = TelegraphTime;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            // Cast a telegraph line before the acceleration gets super strong.
            float opacity = Pow(LumUtils.Convert01To010(Time / TelegraphTime), 0.4f);
            Texture2D invisible = InfernumTextureRegistry.Invisible.Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            Effect laserScopeEffect = Filters.Scene["CalamityMod:PixelatedSightLine"].GetShader().Shader;
            laserScopeEffect.Parameters["sampleTexture2"].SetValue(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/CertifiedCrustyNoise").Value);
            laserScopeEffect.Parameters["noiseOffset"].SetValue(Main.GameUpdateCount * -0.003f);
            laserScopeEffect.Parameters["mainOpacity"].SetValue(Sqrt(opacity));
            laserScopeEffect.Parameters["Resolution"].SetValue(new Vector2(425f));
            laserScopeEffect.Parameters["laserAngle"].SetValue(-Projectile.velocity.ToRotation());
            laserScopeEffect.Parameters["laserWidth"].SetValue(0.003f + Pow(opacity, 4f) * (Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.001f + 0.001f));
            laserScopeEffect.Parameters["laserLightStrenght"].SetValue(5f);
            laserScopeEffect.Parameters["color"].SetValue(Color.Lerp(Color.Red, Color.Yellow, Projectile.identity / 7f % 1f * 0.6f).ToVector3());
            laserScopeEffect.Parameters["darkerColor"].SetValue(Color.Lerp(Color.Yellow, Color.Red, 0.65f).ToVector3());
            laserScopeEffect.Parameters["bloomSize"].SetValue(0.1f + (1f - opacity) * 0.18f);
            laserScopeEffect.Parameters["bloomMaxOpacity"].SetValue(0.4f);
            laserScopeEffect.Parameters["bloomFadeStrenght"].SetValue(3f);

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);

            laserScopeEffect.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Draw(invisible, drawPosition, null, Color.White, 0f, invisible.Size() * 0.5f, opacity * TelegraphLength, SpriteEffects.None, 0f);
            Main.spriteBatch.ExitShaderRegion();
            Time++;
            return false;
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
