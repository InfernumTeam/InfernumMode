using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Yharon
{
    public class InfernadoSpawner : ModProjectile
    {
        public bool HomeInOnTarget => Projectile.ai[0] == 1f;

        public ref float Time => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Big Flare");
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 100;
            Projectile.height = 100;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            Projectile.scale = 0.15f;
            Projectile.tileCollide = false;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Decide frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Type];

            // Fade in.
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.04f, 0f, 1f);

            // Grow to maximum size.
            Projectile.scale = Lerp(Projectile.scale, 1.25f, 0.05f);

            // Move towards the target if necessary.
            if (HomeInOnTarget)
            {
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                float flySpeed = Time * 0.145f + 9f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * flySpeed, 0.062f);

                // Release the tornado if close enough to the target.
                if (Projectile.WithinRange(target.Center, 64f))
                    Projectile.Kill();
            }

            Time++;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, Main.DiscoG, 53, Projectile.alpha);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 1);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(BigFlare.FlareSound);

            if (Main.netMode != NetmodeID.MultiplayerClient)
                Utilities.NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<DraconicInfernado>(), YharonBehaviorOverride.InfernadoDamage, 0f);
        }
    }
}
