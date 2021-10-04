using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.PlaguebringerGoliath
{
    public class PlagueMissile : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public Player Target => Main.player[Player.FindClosest(projectile.Center, 1, 1)];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Missile");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 24;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 210;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.InverseLerp(0f, 12f, Time, true);
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (projectile.Hitbox.Intersects(Target.Hitbox))
                projectile.Kill();

            if (Time > 30f)
                projectile.velocity *= 1.01f;

            projectile.tileCollide = Time > 34f;
            Time++;
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Item14, projectile.Center);
            for (int i = 0; i < 20; i++)
            {
                Dust dust = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(35f, 35f), 89);
                dust.velocity = Main.rand.NextVector2Circular(6f, 6f);
                dust.scale = Main.rand.NextFloat(1.1f, 1.35f);
                dust.noGravity = true;
            }
        }

        public override bool CanDamage() => projectile.Opacity >= 0.8f;
    }
}
