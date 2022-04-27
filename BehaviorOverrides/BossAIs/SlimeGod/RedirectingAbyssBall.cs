using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.SlimeGod
{
	public class RedirectingAbyssBall : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Abyss Orb");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 360;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                SoundEngine.PlaySound(SoundID.Item33, Projectile.Center);
                Projectile.localAI[0] = 1f;
            }

            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.Opacity = Utils.GetLerpValue(360f, 340f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 20f, Projectile.timeLeft, true) * 0.8f;

            if (Projectile.timeLeft is < 270 and > 225)
                Projectile.velocity = Projectile.velocity.MoveTowards(Projectile.SafeDirectionTo(target.Center) * 11f, 0.55f);

            if (Projectile.timeLeft < 35)
                Projectile.velocity *= 0.98f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.timeLeft > 355)
                return false;

            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 2);
            return false;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            Color color = Color.Pink * Projectile.Opacity;
            color.A = 0;
            return color;
        }

        public override void Kill(int timeLeft)
        {
            for (int k = 0; k < 3; k++)
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, 4, Projectile.oldVelocity.X * 0.5f, Projectile.oldVelocity.Y * 0.5f);
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<Shadowflame>(), 180);
        }
    }
}
