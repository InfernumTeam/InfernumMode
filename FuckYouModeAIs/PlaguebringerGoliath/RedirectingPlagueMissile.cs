using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.PlaguebringerGoliath
{
    public class RedirectingPlagueMissile : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public Player Target => Main.player[Player.FindClosest(projectile.Center, 1, 1)];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Missile");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 24;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 210;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.InverseLerp(0f, 12f, Time, true);
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (projectile.Hitbox.Intersects(Target.Hitbox))
                projectile.Kill();

            if (Time < 30f)
                projectile.velocity *= 1.01f;
            if (Time >= 30f)
            {
                float newSpeed = projectile.velocity.Length() * 1.0045f;
                if (newSpeed < 12f)
                    newSpeed = 12f;
                if (newSpeed > 25f)
                    newSpeed = 25f;
                projectile.velocity = (projectile.velocity * 19f + projectile.SafeDirectionTo(Target.Center) * newSpeed) / 20f;
            }

            projectile.tileCollide = Time > 105f;
            Time++;
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Item14, projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Utilities.NewProjectileBetter(projectile.Center, Vector2.Zero, ModContent.ProjectileType<LargePlagueExplosion>(), 150, 0f);
        }

        public override bool CanDamage() => projectile.Opacity >= 0.8f;
    }
}
