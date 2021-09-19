using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.SlimeGod
{
    public class IchorGelWave : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Ichor Gel Wave");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 30;
            projectile.hostile = true;
			projectile.ignoreWater = true;
            projectile.timeLeft = 300;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
        }

        public override void AI()
        {
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            projectile.Opacity = Utils.InverseLerp(300f, 290f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 10f, projectile.timeLeft, true) * 0.6f;

            if (projectile.velocity.Length() < 29f)
                projectile.velocity *= 1.0215f;

            if (projectile.timeLeft < 50)
                projectile.velocity *= 0.98f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (projectile.timeLeft > 295)
                return false;

            Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 2);
            return false;
        }

        public override Color? GetAlpha(Color lightColor) => new Color(200, 200, 200, projectile.alpha) * projectile.Opacity;

        public override void Kill(int timeLeft)
        {
            for (int k = 0; k < 3; k++)
                Dust.NewDust(projectile.position + projectile.velocity, projectile.width, projectile.height, 170, projectile.oldVelocity.X * 0.5f, projectile.oldVelocity.Y * 0.5f);
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(BuffID.Slimed, 180);
            target.AddBuff(BuffID.Ichor, 180);
        }
    }
}
