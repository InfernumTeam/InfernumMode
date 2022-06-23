using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Crabulon
{
    public class HomingSpore : ModProjectile
    {
        public float HomePower => Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Spore");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 8;
            Projectile.scale = 0.8f;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.Opacity = 0f;
        }

        public override void AI()
        {
            HomeInOnTarget();

            Lighting.AddLight(Projectile.Center, Color.CornflowerBlue.ToVector3() * Projectile.Opacity * 0.5f);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            if (Projectile.timeLeft > 10f)
                Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.1f, 0f, 1f);
            else
                Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity - 0.1f, 0f, 1f);
        }

        public void HomeInOnTarget()
        {
            float homeSpeed = MathHelper.Lerp(3.5f, 6.75f, HomePower);
            if (BossRushEvent.BossRushActive)
                homeSpeed *= 3f;

            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (Projectile.timeLeft <= 105 && !Projectile.WithinRange(target.Center, 55f))
                Projectile.velocity = (Projectile.velocity * 15f + Projectile.SafeDirectionTo(target.Center) * homeSpeed) / 16f;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type]);
            return false;
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Projectile.alpha < 20;
    }
}
