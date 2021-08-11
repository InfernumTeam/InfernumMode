using InfernumMode.Buffs;
using InfernumMode.Dusts;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Ravager
{
    public class DarkMagicFireball : ModProjectile
    {
        public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Dark Magic Fireball");
            Main.projFrames[projectile.type] = 5;
        }

        public override void SetDefaults()
        {
            projectile.width = 36;
            projectile.height = 36;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 360;
        }

        public override void AI()
        {
            projectile.Opacity = (float)Math.Sin(MathHelper.Pi * projectile.timeLeft / 360f) * 7f;
            if (projectile.Opacity > 1f)
                projectile.Opacity = 1f;

            if (projectile.frameCounter++ % 5 == 4)
                projectile.frame = (projectile.frame + 1) % Main.projFrames[projectile.type];

            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;
        }

        public override bool CanDamage() => projectile.Opacity > 0.75f;

        public override void Kill(int timeLeft)
		{
            if (Main.dedServ)
                return;

            for (int i = 0; i < 30; i++)
            {
                Dust dust = Dust.NewDustPerfect(projectile.Center, ModContent.DustType<RavagerMagicDust>());
                dust.velocity = Main.rand.NextVector2Circular(8f, 8f);
                dust.noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<DarkFlames>(), 180);
    }
}
