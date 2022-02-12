using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.MinibossAIs.Ogre
{
    public class BouncingSpitBall : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Spit Ball");

        public override void SetDefaults()
        {
            projectile.width = 20;
            projectile.height = 20;
            projectile.hostile = true;
            projectile.tileCollide = true;
            projectile.ignoreWater = true;
            projectile.timeLeft = 470;
            projectile.penetrate = -1;
            projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.InverseLerp(470f, 463f, projectile.timeLeft, true);
            projectile.velocity.Y += 0.2f;
            projectile.rotation += projectile.velocity.X * 0.04f;
        }

        public override bool CanDamage() => projectile.Opacity > 0.75f;

        public override Color? GetAlpha(Color lightColor) => Color.White * projectile.Opacity;

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            projectile.velocity.Y = MathHelper.Clamp(oldVelocity.Y * -1.2f, -28f, 28f);
            projectile.velocity.Y += projectile.SafeDirectionTo(Main.player[Player.FindClosest(projectile.Center, 1, 1)].Center).Y * 10f;
            projectile.netUpdate = true;

            Main.PlaySound(SoundID.Item17, projectile.Center);
            for (int i = 0; i < 4; i++)
            {
                int dustType = Utils.SelectRandom(Main.rand, new int[]
                {
                        4,
                        256
                });
                Dust spit = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, dustType, projectile.velocity.X, projectile.velocity.Y, 100);
                spit.velocity = spit.velocity / 4f + projectile.velocity / 2f - Vector2.UnitY * 5f;
                spit.scale = Main.rand.NextFloat(1f, 1.45f);
                spit.position = projectile.Center;
                spit.position += Main.rand.NextVector2Circular(projectile.width, projectile.width) * 2f;
                spit.noLight = true;
                if (spit.type == 4)
                    spit.color = new Color(80, 170, 40, 120);
            }
            return false;
        }
    }
}
