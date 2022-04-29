using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Perforators
{
    public class IchorBolt : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ichor Spit");
            Main.projFrames[Projectile.type] = 3;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 12;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 300;
            Projectile.Opacity = 0f;
        }

        public override void AI()
        {
            // Handle frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];

            // Decide rotation.
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            // Accelerate.
            if (Projectile.velocity.Length() < 20f)
                Projectile.velocity *= 1.022f;

            // Fade in.
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.1f, 0f, 1f);
        }

        public override Color? GetAlpha(Color lightColor) => new(255, 255, 255, Projectile.alpha);

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type]);
            return false;
        }
    }
}
