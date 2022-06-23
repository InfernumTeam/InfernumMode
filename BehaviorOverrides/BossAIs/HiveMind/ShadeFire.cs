using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
namespace InfernumMode.BehaviorOverrides.BossAIs.HiveMind
{
    public class ShadeFire : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fire");
        }

        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 6;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 3;
            Projectile.timeLeft = 80;
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, Projectile.Opacity * 0.15f, 0f, Projectile.Opacity * 0.2f);
            if (Projectile.timeLeft > 80)
                Projectile.timeLeft = 80;

            Time++;
            if (Time <= 7f)
                return;

            float fireScale = MathHelper.Lerp(0.25f, 1f, Utils.GetLerpValue(8f, 10f, Time, true));
            if (Main.rand.NextBool(2))
            {
                for (int i = 0; i < 3; i++)
                {
                    int dustType = i == 2 ? 157 : 14;
                    Dust shadeFire = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, dustType, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default, 1f);
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
                    shadeFire.velocity += Projectile.velocity;
                    if (!shadeFire.noGravity)
                        shadeFire.velocity *= 0.5f;
                }
            }
        }
    }
}
