using CalamityMod;
using InfernumMode.Common.Graphics.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AstrumAureus
{
    public class MissileTelegraphLine : ModProjectile, IScreenCullDrawer
    {
        public ref float Time => ref Projectile.ai[0];

        public const int Lifetime = 27;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Telegraph");

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
            Projectile.Opacity = LumUtils.Convert01To010(Time / Lifetime) * 3f;
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;
            if (Time >= Lifetime)
                Projectile.Kill();

            Time++;
        }

        public void CullDraw(SpriteBatch spriteBatch)
        {
            float telegraphWidth = Lerp(0.3f, 3f, LumUtils.Convert01To010(Time / Lifetime));

            // Draw a telegraph line outward.
            Vector2 start = Projectile.Center;
            Vector2 end = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * 3000f;
            Main.spriteBatch.DrawLineBetter(start, end, Color.Cyan, telegraphWidth);
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
