using CalamityMod.Events;
using CalamityMod.Particles;
using InfernumMode.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Cryogen
{
    public class AuroraSpirit2 : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Aurora Spirit");
            Main.projFrames[Projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 28;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 300;
            Projectile.Opacity = 0f;
            Projectile.extraUpdates = BossRushEvent.BossRushActive ? 1 : 0;
        }

        public override void AI()
        {
            // Handle frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.Pi;
            Projectile.spriteDirection = (Math.Cos(Projectile.rotation) < 0f).ToDirectionInt();
            if (Projectile.spriteDirection == 1)
                Projectile.rotation -= MathHelper.Pi;

            // Fade in.
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.02f, 0f, 1f);

            if (Time < 55f)
            {
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                Projectile.velocity = Projectile.velocity.RotateTowards(Projectile.AngleTo(target.Center), 0.056f);
            }
            else if (Projectile.velocity.Length() < 17f)
                    Projectile.velocity *= 1.0075f;

            if (Time % 10 == 0)
            {
                // Leave a trail of particles.
                Particle iceParticle = new SnowyIceParticle(Projectile.Center, Projectile.velocity * 0.5f, Color.White, Main.rand.NextFloat(0.75f, 0.95f), 30);
                GeneralParticleHandler.SpawnParticle(iceParticle);
            }
            


            Time++;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            Color endColor = Color.Lerp(lightColor * Projectile.Opacity, Color.White, 0.55f);
            return Color.Lerp(new Color(128, 88, 160, 0) * 0.45f, endColor, Projectile.Opacity);
        }

        public override bool PreDraw(ref Color lightColor)
        {        
            Utilities.DrawAfterimagesCentered(Projectile, Color.White * Projectile.Opacity, ProjectileID.Sets.TrailingMode[Projectile.type]);
            return false;
        }
    }
}
