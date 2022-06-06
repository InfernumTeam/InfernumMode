using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class CondemnationArrowSCal : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Arrow");
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
            projectile.Calamity().canBreakPlayerDefense = true;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.075f, 0f, 1f);

            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 5 % Main.projFrames[projectile.type];
            projectile.velocity *= 1.025f;
            Time++;
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Item74, projectile.Center);
            Utilities.CreateGenericDustExplosion(projectile.Center, 242, 10, 7f, 1.25f);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            float telegraphInterpolant = Utils.InverseLerp(0f, 20f, Time, true);
            float telegraphWidth = MathHelper.Lerp(0.3f, 3f, CalamityUtils.Convert01To010(telegraphInterpolant));

            // Draw a telegraph line outward.
            if (telegraphInterpolant < 1f)
            {
                Vector2 start = projectile.Center;
                Vector2 end = start + projectile.velocity.SafeNormalize(Vector2.UnitY) * 5000f;
                Main.spriteBatch.DrawLineBetter(start, end, Color.Red, telegraphWidth);
            }
            return true;
        }
    }
}