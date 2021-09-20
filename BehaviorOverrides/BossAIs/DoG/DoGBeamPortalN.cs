using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using CalamityMod;

namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
    public class DoGBeamPortalN : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Beam Portal");
            Main.projFrames[projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            projectile.width = 80;
            projectile.height = 80;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.timeLeft = 360;
            projectile.penetrate = -1;
            projectile.alpha = 255;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            Lighting.AddLight(projectile.Center, 0f, 0.95f, 1.15f);
            if (projectile.alpha > 100 && Time < 290)
                projectile.alpha = Utils.Clamp(projectile.alpha - 5, 100, 255);

            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 6 % Main.projFrames[projectile.type];

            Time++;

            // Fly towards the nearest target every second or so.
            if (Time % 60f == 59f && Time < 180f)
            {
                Player closest = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
                projectile.velocity = projectile.DirectionTo(closest.Center) * 8.5f;
            }

            // Decelerate after the final charge.
            if (Time > 180f && Time <= 240f)
                projectile.velocity *= 0.95f;

            // Release some cosmic dust.
            if (Time == 260f)
            {
                for (int i = 0; i < 52; i++)
                {
                    float angle = MathHelper.TwoPi / 52f * i;
                    Dust cosmicBurst = Dust.NewDustPerfect(projectile.Center, 173, angle.ToRotationVector2() * 32f);
                    cosmicBurst.noGravity = true;
                }
            }

            // And push all close players towards the portal.
            if (Time >= 260 && Time <= 270f)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    if (projectile.WithinRange(Main.player[i].Center, 700f))
                        Main.player[i].velocity = Main.player[i].SafeDirectionTo(projectile.Center) * 10f;
                }
            }

            // And finally release some beams before fading out.
            if (Time == 290f)
            {
                Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 33);
                for (float i = 0f; i < 12f; i += 1f)
                {
                    float angle = MathHelper.TwoPi / 12f * i;
                    Projectile.NewProjectile(projectile.Center, angle.ToRotationVector2() * 8f, ModContent.ProjectileType<DoGBeamN>(), 75, 0f);
                }
            }

            if (Time > 290f)
            {
                projectile.alpha += 5;
                if (projectile.alpha >= 255)
                    projectile.Kill();
            }
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;
    }
}
