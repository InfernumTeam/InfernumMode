using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SlimeGod
{
    public class IchorGelWave : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Ichor Gel Wave");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 300;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.Opacity = Utils.GetLerpValue(300f, 290f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 10f, Projectile.timeLeft, true) * 0.6f;

            if (Projectile.velocity.Length() < 29f)
                Projectile.velocity *= BossRushEvent.BossRushActive ? 1.0325f : 1.0215f;

            if (Projectile.timeLeft < 50)
                Projectile.velocity *= 0.98f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (Projectile.timeLeft > 295)
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
            target.AddBuff(BuffID.Ichor, 180);
        }
    }
}
