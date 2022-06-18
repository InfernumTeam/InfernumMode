using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
namespace InfernumMode.BehaviorOverrides.BossAIs.HiveMind
{
    public class ShadeFire : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fire");
        }

        public override void SetDefaults()
        {
            projectile.width = 6;
            projectile.height = 6;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.ranged = true;
            projectile.penetrate = -1;
            projectile.extraUpdates = 3;
            projectile.timeLeft = 80;
            projectile.tileCollide = false;
        }

        public override void AI()
        {
            Lighting.AddLight(projectile.Center, projectile.Opacity * 0.15f, 0f, projectile.Opacity * 0.2f);
            if (projectile.timeLeft > 80)
                projectile.timeLeft = 80;

            Time++;
            if (Time <= 7f)
                return;

            float fireScale = MathHelper.Lerp(0.25f, 1f, Utils.InverseLerp(8f, 10f, Time, true));
            if (Main.rand.NextBool(2))
            {
                for (int i = 0; i < 3; i++)
                {
                    int dustType = i == 2 ? 157 : 14;
                    Dust shadeFire = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, dustType, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 100, default, 1f);
                    if (Main.rand.NextBool(3))
                    {
                        shadeFire.noGravity = true;
                        shadeFire.scale *= 1.75f;
                        shadeFire.velocity *= 2f;
                    }
                    else
                        shadeFire.scale *= 0.5f;

                    shadeFire.velocity *= 1.2f;
                    shadeFire.scale *= fireScale;
                    shadeFire.velocity += projectile.velocity;
                    if (!shadeFire.noGravity)
                        shadeFire.velocity *= 0.5f;
                }
            }
        }
    }
}
