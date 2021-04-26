using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Clone
{
	public class HomingBrimstoneDart : ModProjectile
    {
        public bool Homing => projectile.timeLeft < 190;
		public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Brimstone Dart");
            Main.projFrames[projectile.type] = 4;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 2;
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 18;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 320;
            projectile.alpha = 255;
			cooldownSlot = 1;
        }

		public override void AI()
        {
            if (Homing)
			{
                Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
                projectile.velocity = Vector2.Lerp(projectile.velocity, projectile.SafeDirectionTo(target.Center) * 14f, 0.03f);
                projectile.velocity = projectile.velocity.SafeNormalize(-Vector2.UnitY) * 14f;
			}
			else if (projectile.velocity.Length() < 8.7f)
				projectile.velocity *= 1.04f;

			projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;

			projectile.frameCounter++;
            if (projectile.frameCounter % 5 == 4)
                projectile.frame = (projectile.frame + 1) % Main.projFrames[projectile.type];

            if (projectile.timeLeft < 40f)
                projectile.Opacity = MathHelper.Clamp(projectile.timeLeft / 40f, 0f, 1f);
            else
                projectile.alpha = Utils.Clamp(projectile.alpha - 20, 0, 255);

			Lighting.AddLight(projectile.Center, Color.Red.ToVector3() * 0.75f);
        }

		public override bool CanHitPlayer(Player target) => projectile.Opacity == 1f;

		public override void OnHitPlayer(Player target, int damage, bool crit)
        {
			if (projectile.Opacity != 1f)
				return;

            target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 120);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
			lightColor.R = (byte)(255 * projectile.Opacity);
			CalamityGlobalProjectile.DrawCenteredAndAfterimage(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 1);
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;
    }
}
