using InfernumMode.Buffs;
using InfernumMode.Dusts;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Ravager
{
    public class RedSoul : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Soul");
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.scale = 1.5f;
            Projectile.width = Projectile.height = 26;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 360;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, Color.Crimson.ToVector3() * 0.56f);

            Projectile.Opacity = (float)Math.Sin(MathHelper.Pi * Projectile.timeLeft / 360f) * 8f;
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;

            if (Projectile.frameCounter++ % 5 == 4)
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];

            Projectile.rotation = Projectile.velocity.ToRotation();

            if (Time < 60f)
            {
                Player closestTarget = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                Projectile.velocity = Projectile.velocity.RotateTowards(Projectile.AngleTo(closestTarget.Center), 0.042f);
            }
            else if (Projectile.velocity.Length() < 17f)
                Projectile.velocity *= 1.01195f;

            Time++;
        }

        public override bool? CanDamage() => Projectile.Opacity > 0.75f ? null : false;

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override void Kill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 16; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<RavagerMagicDust>());
                dust.velocity = Main.rand.NextVector2Circular(5f, 5f);
                dust.noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<DarkFlames>(), 120);
    }
}
