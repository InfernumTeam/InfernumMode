using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.NPCs.OldDuke;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.OldDuke
{
	public class SharkSummonVortex : ModProjectile
	{
		public ref float Time => ref projectile.ai[0];
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
			projectile.Opacity = (float)Math.Sin(MathHelper.Pi * Time / 120f) * 3f;
			if (projectile.Opacity > 1f)
				projectile.Opacity = 1f;

			projectile.rotation -= projectile.Opacity * 0.1f;

			float brightnessFactor = projectile.scale * 2f;
			Lighting.AddLight(projectile.Center, brightnessFactor, brightnessFactor * 2f, brightnessFactor);

			if (Time == 0f)
				Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/OldDukeVortex"), projectile.Center);

			if (Main.netMode != NetmodeID.MultiplayerClient && Time % 12f == 11f)
            {
				Vector2 sharkVelocity = (MathHelper.TwoPi * Time / 120f).ToRotationVector2() * 8f;
				int shark = NPC.NewNPC((int)projectile.Center.X, (int)projectile.Center.Y, ModContent.NPCType<OldDukeSharkron>());
				if (Main.npc.IndexInRange(shark))
                {
					Main.npc[shark].velocity = sharkVelocity;
					Main.npc[shark].ai[1] = 1f;
					Main.npc[shark].netUpdate = true;
                }
            }

			Time++;
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 1);
			return false;
		}

		public override bool CanHitPlayer(Player target) => projectile.Opacity > 0f;

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

		public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<Irradiated>(), 600);

		public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;
	}
}
