using CalamityMod;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.MinibossAIs.Ogre
{
    public class RisingFireball : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Fireball");

        public override void SetDefaults()
        {
            projectile.width = 20;
            projectile.height = 80;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 300;
            projectile.penetrate = -1;
            projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            Lighting.AddLight(projectile.Center, Color.Blue.ToVector3() * 0.84f);

            projectile.Opacity = (float)Math.Sin(MathHelper.Pi * projectile.timeLeft / 300f) * 10f;
            if (projectile.Opacity > 1f)
                projectile.Opacity = 1f;

            if (projectile.frameCounter++ % 5 == 4)
                projectile.frame = (projectile.frame + 1) % Main.projFrames[projectile.type];

            projectile.velocity.X *= 0.996f;
            projectile.velocity.Y = MathHelper.Clamp(projectile.velocity.Y - 0.37f, -27f, 17f);
            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;
        }

        public override bool CanDamage() => projectile.Opacity > 0.75f;

        public override Color? GetAlpha(Color lightColor) => Color.White * projectile.Opacity;

        public override void Kill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 30; i++)
            {
                Dust dust = Dust.NewDustPerfect(projectile.Center, DustID.Fire);
                dust.velocity = Main.rand.NextVector2Circular(8f, 8f);
                dust.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            projectile.RotatingHitboxCollision(targetHitbox.TopLeft(), targetHitbox.Size());

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(BuffID.OnFire, 180);
    }
}
