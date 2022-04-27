using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.SlimeGod
{
	public class StartingCursedBall : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Cursed Orb");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 480;
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

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.Opacity = Utils.GetLerpValue(480f, 470f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 10f, Projectile.timeLeft, true) * 0.75f;

            if (Time is >= (-10f) and <= 8f)
            {
                float flySpeed = MathHelper.Lerp(10f, 21f, Utils.GetLerpValue(-10f, 8f, Time, true));
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * flySpeed;
            }

            if (Projectile.timeLeft < 35)
                Projectile.velocity *= 0.98f;
            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.timeLeft > 475)
                return false;

            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 2);
            return false;
        }

        public override Color? GetAlpha(Color lightColor) => new Color(200, 200, 200, Projectile.alpha) * Projectile.Opacity;

        public override void Kill(int timeLeft)
        {
            for (int k = 0; k < 3; k++)
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, 170, Projectile.oldVelocity.X * 0.5f, Projectile.oldVelocity.Y * 0.5f);
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(BuffID.Slimed, 180);
            target.AddBuff(BuffID.CursedInferno, 300);
        }
    }
}
