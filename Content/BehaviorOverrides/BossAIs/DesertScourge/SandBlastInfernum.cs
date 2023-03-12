using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using CalamityMod;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.DesertScourge
{
    public class SandBlastInfernum : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sand Blast");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 1200;
            Projectile.Opacity = 0f;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Decide rotation.
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // Fade in.
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.1f, 0f, 1f);

            Dust sand = Dust.NewDustDirect(Projectile.TopLeft, Projectile.width, Projectile.height, 85, 0f, 0f, 100, default, 0.8f);
            sand.noGravity = true;
            sand.velocity *= 0f;
        }

        public override void Kill(int timeLeft) => SoundEngine.PlaySound(SoundID.Item14, Projectile.position);

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor.R = (byte)(255 * Projectile.Opacity);
            lightColor.G = (byte)(255 * Projectile.Opacity);
            lightColor.B = (byte)(255 * Projectile.Opacity);
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }
    }
}
