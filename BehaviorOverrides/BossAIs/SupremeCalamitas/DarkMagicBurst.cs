using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class DarkMagicBurst : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Magic Burst");
            Main.projFrames[projectile.type] = 4;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 15;
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 26;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 80;
        }

        public override void AI()
        {
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.075f, 0f, 1f);

            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 5 % Main.projFrames[projectile.type];
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Item74, projectile.Center);
            Utilities.CreateGenericDustExplosion(projectile.Center, 242, 10, 7f, 1.25f);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
            for (int i = 0; i < 10; i++)
            {
                int dartDamage = 540;
                Vector2 shootVelocity = projectile.SafeDirectionTo(target.Center).RotatedByRandom(0.77f) * projectile.velocity.Length() * Main.rand.NextFloat(0.65f, 0.85f);
                int dart = Utilities.NewProjectileBetter(projectile.Center + shootVelocity, shootVelocity, ModContent.ProjectileType<BrimstoneBarrage>(), dartDamage, 0f);
                if (Main.projectile.IndexInRange(dart))
                {
                    Main.projectile[dart].ai[0] = 1f;
                    Main.projectile[dart].tileCollide = false;
                    Main.projectile[dart].netUpdate = true;
                }
            }
        }
    }
}
