using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.StormWeaver
{
    public class WeaverSpark : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Spark");

        public override void SetDefaults()
        {
            projectile.width = 72;
            projectile.height = 72;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.timeLeft = 300;
            projectile.Opacity = 0f;
        }

        public override void AI()
        {
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.1f, 0f, 1f);

            if (projectile.velocity.Length() < 25f)
                projectile.velocity *= 1.015f;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 255, 255, 127) * projectile.Opacity;
        }
    }
}
