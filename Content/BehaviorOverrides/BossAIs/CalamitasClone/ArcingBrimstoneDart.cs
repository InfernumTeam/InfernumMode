using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasClone
{
    public class ArcingBrimstoneDart : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Brimstone Dart");
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
            Projectile.Opacity = 0f;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.2f, 0f, 1f);

            Projectile.velocity = Projectile.velocity.RotatedBy(Projectile.ai[0]);
            if (Projectile.velocity.Length() < 16f)
                Projectile.velocity *= 1.04f;

            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Projectile.DrawBackglow(Color.HotPink with { A = 0 }, Projectile.Opacity * 2f);
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor);
            return false;
        }
    }
}
