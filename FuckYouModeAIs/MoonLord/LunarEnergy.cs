using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
namespace InfernumMode.FuckYouModeAIs.MoonLord
{
    public class LunarEnergy : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Lunar Energy");
        }

        public override void SetDefaults()
        {
            projectile.width = 4;
            projectile.height = 4;
            projectile.ignoreWater = true;
            projectile.alpha = 255;
            projectile.penetrate = 1;
            projectile.extraUpdates = 2;
            projectile.timeLeft = 600;
        }

        public override void AI()
        {
            Vector2 endPosition = Main.npc[(int)projectile.ai[0]].Center;
            Vector2 distanceNormalized = endPosition - projectile.Center;
            if (distanceNormalized.Length() < projectile.velocity.Length())
            {
                projectile.Kill();
                return;
            }
            distanceNormalized.Normalize();
            distanceNormalized *= MathHelper.Max(18f, Main.npc[(int)projectile.ai[0]].velocity.Length() * 1.6f);
            projectile.velocity = Vector2.Lerp(projectile.velocity, distanceNormalized, 0.1f);
            for (int k = 0; k < 2; k++)
            {
                int idx = Dust.NewDust(projectile.Center, 0, 0, 227, 0f, 0f, 100, default, 1f);
                Main.dust[idx].noGravity = true;
                Dust dust = Main.dust[idx];
                dust.position += new Vector2(4f);
                dust = Main.dust[idx];
                dust.scale += Main.rand.NextFloat();
            }
        }
    }
}
