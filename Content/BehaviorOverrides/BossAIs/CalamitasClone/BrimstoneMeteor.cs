using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Boss;
using InfernumMode.Common.Graphics.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasClone
{
    public class BrimstoneMeteor : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Brimstone Meteor");
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 36;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];

            Projectile.Opacity = Utils.GetLerpValue(0f, 12f, Projectile.timeLeft, true);
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            // Explode once past the tile collision line.
            Projectile.tileCollide = Projectile.Top.Y >= Projectile.ai[1];

            Time++;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Projectile.Opacity >= 1f;

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = Color.Lerp(lightColor, Color.White, 0.5f);
            lightColor.A /= 3;
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 1);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(HolyBlast.ImpactSound, Projectile.Center);

            for (int i = 0; i < 6; i++)
            {
                Color fireColor = Main.rand.NextBool() ? Color.HotPink : Color.Red;
                CloudParticle fireCloud = new(Projectile.Center, (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 2f + Main.rand.NextVector2Circular(0.3f, 0.3f), fireColor, Color.DarkGray, 33, Main.rand.NextFloat(1.8f, 2f))
                {
                    Rotation = Main.rand.NextFloat(MathHelper.TwoPi)
                };
                GeneralParticleHandler.SpawnParticle(fireCloud);
            }
        }

        public override bool ShouldUpdatePosition() => true;
    }
}
