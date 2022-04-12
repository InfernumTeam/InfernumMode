using CalamityMod;
using InfernumMode.Buffs;
using InfernumMode.Dusts;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Ravager
{
    public class DarkMagicFireball : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Magic Fireball");
            Main.projFrames[Projectile.type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 84;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 360;
            Projectile.penetrate = -1;
            Projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, Color.Blue.ToVector3() * 0.84f);

            Projectile.Opacity = (float)Math.Sin(MathHelper.Pi * Projectile.timeLeft / 360f) * 7f;
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;

            if (Projectile.frameCounter++ % 5 == 4)
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];

            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
        }

        public override bool? CanDamage() => Projectile.Opacity > 0.75f ? null : false;

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override void Kill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 30; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<RavagerMagicDust>());
                dust.velocity = Main.rand.NextVector2Circular(8f, 8f);
                dust.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            Projectile.RotatingHitboxCollision(targetHitbox.TopLeft(), targetHitbox.Size());

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<DarkFlames>(), 180);
    }
}
