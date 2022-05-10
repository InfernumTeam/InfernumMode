using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Plantera
{
    public class Petal : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Petal");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 16;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.timeLeft = 300;
            projectile.penetrate = -1;
        }

        public override void AI()
        {
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White;
    }
}
