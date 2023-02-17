using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class LightCleaveTelegraph : ModProjectile, IAboveWaterProjectileDrawer
    {
        public float TelegraphLength
        {
            get;
            set;
        }

        public ref float Time => ref Projectile.localAI[0];

        public ref float Lifetime => ref Projectile.ai[1];

        public static float MaxTelegraphLength => 3000f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Telegraph");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 900;
        }

        public override void AI()
        {
            TelegraphLength = MathHelper.Clamp(TelegraphLength + 160f, 0f, Projectile.ai[0]);
            Projectile.Opacity = Utils.GetLerpValue(0f, 12f, Time, true);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (Time >= Lifetime)
                Projectile.Kill();

            Time++;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawAboveWater(SpriteBatch spriteBatch)
        {
            Main.spriteBatch.SetBlendState(BlendState.Additive);

            // Draw the telegraph line.
            Texture2D line = InfernumTextureRegistry.BloomLineSmall.Value;

            float telegraphWidth = Projectile.Opacity * 6f + Utils.GetLerpValue(-32f, -3f, Time - Lifetime, true) * 60f;
            Color telegraphColor = Color.Coral * Projectile.Opacity;
            Vector2 beamOrigin = new(line.Width / 2f, line.Height);
            Vector2 beamScale = new(telegraphWidth / line.Width, TelegraphLength / line.Height);
            Main.spriteBatch.Draw(line, Projectile.Center - Main.screenPosition, null, telegraphColor, Projectile.rotation, beamOrigin, beamScale, 0, 0f);
            Main.spriteBatch.Draw(line, Projectile.Center - Main.screenPosition, null, Color.Wheat * Projectile.Opacity, Projectile.rotation, beamOrigin, beamScale * new Vector2(0.3f, 1f), 0, 0f);
            Main.spriteBatch.Draw(line, Projectile.Center - Main.screenPosition, null, Color.White * Projectile.Opacity, Projectile.rotation, beamOrigin, beamScale * new Vector2(0.1f, 1f), 0, 0f);

            Main.spriteBatch.ResetBlendState();
        }
    }
}