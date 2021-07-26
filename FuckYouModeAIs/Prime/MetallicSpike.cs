using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Prime
{
    public class MetallicSpike : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Metal Spike");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 14;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 300;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.InverseLerp(300f, 285f, projectile.timeLeft, true);
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (projectile.velocity.Length() < 18f)
                projectile.velocity *= 1.02f;

            Lighting.AddLight(projectile.Center, Color.Red.ToVector3());
        }

        public override bool CanDamage() => projectile.alpha < 20;
    }
}
