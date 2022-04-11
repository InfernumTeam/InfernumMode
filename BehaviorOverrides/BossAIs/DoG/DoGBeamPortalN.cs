using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
    public class DoGBeamPortalN : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Beam Portal");
            Main.projFrames[Projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 360;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, 0f, 0.95f, 1.15f);
            if (Projectile.alpha > 100 && Time < 290)
                Projectile.alpha = Utils.Clamp(Projectile.alpha - 5, 100, 255);

            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 6 % Main.projFrames[Projectile.type];

            Time++;

            // Fly towards the nearest target every second or so.
            if (Time % 60f == 59f && Time < 180f)
            {
                Player closest = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                Projectile.velocity = Projectile.SafeDirectionTo(closest.Center) * 8.5f;
            }

            // Decelerate after the final charge.
            if (Time > 180f && Time <= 240f)
                Projectile.velocity *= 0.95f;

            // Release some cosmic dust.
            if (Time == 260f)
            {
                for (int i = 0; i < 52; i++)
                {
                    float angle = MathHelper.TwoPi / 52f * i;
                    Dust cosmicBurst = Dust.NewDustPerfect(Projectile.Center, 173, angle.ToRotationVector2() * 32f);
                    cosmicBurst.noGravity = true;
                }
            }

            // And push all close players towards the portal.
            if (Time >= 260 && Time <= 270f)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    if (Projectile.WithinRange(Main.player[i].Center, 700f))
                        Main.player[i].velocity = Main.player[i].SafeDirectionTo(Projectile.Center) * 10f;
                }
            }

            // And finally release some beams before fading out.
            if (Time == 290f)
            {
                SoundEngine.PlaySound(SoundID.Item, (int)Projectile.position.X, (int)Projectile.position.Y, 33);
                for (float i = 0f; i < 12f; i += 1f)
                {
                    float angle = MathHelper.TwoPi / 12f * i;
                    Projectile.NewProjectile(Projectile.Center, angle.ToRotationVector2() * 14.5f, ModContent.ProjectileType<DoGBeamN>(), 75, 0f);
                }
            }

            if (Time > 290f)
            {
                Projectile.alpha += 5;
                if (Projectile.alpha >= 255)
                    Projectile.Kill();
            }
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = Projectile;
    }
}
