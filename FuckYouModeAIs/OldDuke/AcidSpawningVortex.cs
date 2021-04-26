using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.OldDuke
{
	public class AcidSpawningVortex : ModProjectile
    {
		public Vector2 Destination => new Vector2(projectile.ai[0], projectile.ai[1]);
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sulphurous Vortex");
			ProjectileID.Sets.TrailCacheLength[projectile.type] = 6;
			ProjectileID.Sets.TrailingMode[projectile.type] = 0;
		}

        public override void SetDefaults()
        {
            projectile.width = 408;
            projectile.height = 408;
			projectile.scale = 0.004f;
			projectile.hostile = true;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
			projectile.ignoreWater = true;
			projectile.timeLeft = 120;
			cooldownSlot = 1;
		}

        public override void AI()
        {
			projectile.scale = Utils.InverseLerp(90f, 75f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 25f, projectile.timeLeft, true);
			projectile.Opacity = projectile.scale;

			projectile.rotation -= 0.1f * projectile.Opacity;

			float lightAmt = 2f * projectile.scale;
			Lighting.AddLight(projectile.Center, lightAmt, lightAmt * 2f, lightAmt);

			if (projectile.timeLeft % 2 == 1 && projectile.scale > 0.67f && Main.myPlayer == projectile.owner)
				Utilities.NewProjectileBetter(projectile.Center, Main.rand.NextVector2CircularEdge(12f, 12f), ModContent.ProjectileType<HomingAcid>(), 305, 0f);
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			CalamityGlobalProjectile.DrawCenteredAndAfterimage(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 1);
			return false;
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			float dist1 = Vector2.Distance(projectile.Center, targetHitbox.TopLeft());
			float dist2 = Vector2.Distance(projectile.Center, targetHitbox.TopRight());
			float dist3 = Vector2.Distance(projectile.Center, targetHitbox.BottomLeft());
			float dist4 = Vector2.Distance(projectile.Center, targetHitbox.BottomRight());

			float minDist = dist1;
			if (dist2 < minDist)
				minDist = dist2;
			if (dist3 < minDist)
				minDist = dist3;
			if (dist4 < minDist)
				minDist = dist4;

			return minDist <= 210f * projectile.scale;
		}

		public override void OnHitPlayer(Player target, int damage, bool crit)
        {
			target.AddBuff(ModContent.BuffType<Irradiated>(), 600);
		}

		public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
		{
			target.Calamity().lastProjectileHit = projectile;
		}
	}
}
