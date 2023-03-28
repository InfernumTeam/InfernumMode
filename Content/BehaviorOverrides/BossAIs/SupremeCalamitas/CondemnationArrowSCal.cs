using CalamityMod;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class CondemnationArrowSCal : ModProjectile
    {
        public PrimitiveTrailCopy TrailDrawer = null;

        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Arrow");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 15;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 26;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 80;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.075f, 0f, 1f);

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];
            Projectile.velocity *= 1.025f;
            Time++;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item74, Projectile.Center);
            Utilities.CreateGenericDustExplosion(Projectile.Center, 242, 10, 7f, 1.25f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float telegraphInterpolant = Utils.GetLerpValue(0f, 20f, Time, true);
            float telegraphWidth = MathHelper.Lerp(0.3f, 3f, CalamityUtils.Convert01To010(telegraphInterpolant));

            // Draw a telegraph line outward.
            if (telegraphInterpolant < 1f)
            {
                Vector2 start = Projectile.Center;
                Vector2 end = start + Projectile.velocity.SafeNormalize(Vector2.UnitY) * 5000f;
                Main.spriteBatch.DrawLineBetter(start, end, Color.Red, telegraphWidth);
            }
            return true;
        }
    }
}