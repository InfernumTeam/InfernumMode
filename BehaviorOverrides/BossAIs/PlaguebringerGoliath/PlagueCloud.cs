using System;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.PlaguebringerGoliath
{
    public class PlagueCloud : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Plague Cloud");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 24;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 60;
        }

        public override void AI()
        {
            projectile.Opacity = (float)Math.Sqrt(projectile.timeLeft / 60f);
            projectile.rotation += projectile.velocity.Y * 0.015f;
            projectile.velocity *= 0.98f;
        }

        public override bool CanDamage() => projectile.Opacity >= 0.4f;
    }
}
