using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SlimeGod
{
    public class RedirectingAbyssBall : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Abyss Orb");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 24;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.timeLeft = 360;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
        }

        public override void AI()
        {
            if (projectile.localAI[0] == 0f)
            {
                Main.PlaySound(SoundID.Item33, projectile.Center);
                projectile.localAI[0] = 1f;
            }

            Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];

            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            projectile.Opacity = Utils.InverseLerp(360f, 340f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 20f, projectile.timeLeft, true) * 0.8f;

            if (projectile.timeLeft < 270 && projectile.timeLeft > 225)
                projectile.velocity = projectile.velocity.MoveTowards(projectile.SafeDirectionTo(target.Center) * 11f, 0.55f);

            if (projectile.timeLeft < 35)
                projectile.velocity *= 0.98f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (projectile.timeLeft > 355)
                return false;

            Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 2);
            return false;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            Color color = Color.Pink * projectile.Opacity;
            color.A = 0;
            return color;
        }

        public override void Kill(int timeLeft)
        {
            for (int k = 0; k < 3; k++)
                Dust.NewDust(projectile.position + projectile.velocity, projectile.width, projectile.height, 4, projectile.oldVelocity.X * 0.5f, projectile.oldVelocity.Y * 0.5f);
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<Shadowflame>(), 180);
        }
    }
}
