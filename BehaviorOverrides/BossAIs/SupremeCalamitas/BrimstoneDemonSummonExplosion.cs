using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class BrimstoneDemonSummonExplosion : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Brimstone Explosion");
            Main.projFrames[projectile.type] = 7;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 48;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 480;
        }

        public override void AI()
        {
            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 5;
            if (projectile.frame >= Main.projFrames[projectile.type])
                projectile.Kill();

            Time++;

            // Create brimstone fire dust.
            Vector2 fireSpawnPosition = projectile.Bottom - Vector2.UnitY * 20f + Main.rand.NextVector2Circular(24f, 10f);
            Vector2 fireDustVelocity = (fireSpawnPosition - projectile.Bottom).SafeNormalize(-Vector2.UnitY).RotatedByRandom(0.19f);
            fireDustVelocity *= Main.rand.NextFloat(2f, 7f);

            Dust brimstoneFire = Dust.NewDustPerfect(fireSpawnPosition, 267);
            brimstoneFire.velocity = fireDustVelocity;
            brimstoneFire.color = Color.Lerp(Color.Red, Color.Yellow, Main.rand.NextFloat(0.7f));
            brimstoneFire.scale = Main.rand.NextFloat(1.05f, 1.45f);
            brimstoneFire.fadeIn = 0.5f;
            brimstoneFire.noGravity = true;
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Utilities.NewProjectileBetter(projectile.Center, Vector2.Zero, ModContent.ProjectileType<SuicideBomberDemonHostile>(), 650, 0f);
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * projectile.Opacity;
    }
}
