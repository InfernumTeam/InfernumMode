using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumDeus
{
	public class AstralFlame2 : ModProjectile
	{
		public ref float Time => ref projectile.ai[0];
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Astral Flame");
			Main.projFrames[projectile.type] = 4;
			ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
			ProjectileID.Sets.TrailingMode[projectile.type] = 0;
		}

		public override void SetDefaults()
		{
			projectile.width = 50;
			projectile.height = 50;
			projectile.hostile = true;
			projectile.ignoreWater = true;
			projectile.tileCollide = false;
			projectile.alpha = 100;
			projectile.penetrate = 1;
			projectile.timeLeft = 485;
		}

		public override void AI()
		{
			projectile.frameCounter++;
			if (projectile.frameCounter > 4)
			{
				projectile.frame = (projectile.frame + 1) % Main.projFrames[projectile.type];
				projectile.frameCounter = 0;
			}

			projectile.spriteDirection = (projectile.velocity.X > 0f).ToDirectionInt();
			projectile.rotation = projectile.velocity.ToRotation();
			if (projectile.spriteDirection == -1)
				projectile.rotation += MathHelper.Pi;

			Lighting.AddLight(projectile.Center, 0.3f, 0.5f, 0.1f);

			Player closestPlayer = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
			if (Time > 85f && Time < 145f)
				projectile.velocity = (projectile.velocity * 41f + projectile.SafeDirectionTo(closestPlayer.Center) * 15f) / 42f;

			if (Time > 150f && projectile.velocity.Length() < 20f)
				projectile.velocity *= 1.01f;

			Time++;
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 1);
			return false;
		}

		public override Color? GetAlpha(Color lightColor) => new Color(255, 255, 255, projectile.alpha);

		public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 180);

		public override void Kill(int timeLeft)
		{
			Main.PlaySound(SoundID.Zombie, (int)projectile.position.X, (int)projectile.position.Y, 103, 1f, 0f);

			projectile.position = projectile.Center;
			projectile.width = projectile.height = 96;
			projectile.position -= projectile.Size * 0.5f;

			for (int i = 0; i < 2; i++)
				Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, ModContent.DustType<AstralOrange>(), 0f, 0f, 50, default, 1f);

			for (int i = 0; i < 20; i++)
			{
				Dust fire = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, ModContent.DustType<AstralOrange>(), 0f, 0f, 0, default, 1.5f);
				fire.noGravity = true;
				fire.velocity *= 3f;
			}
			projectile.Damage();
		}
	}
}
