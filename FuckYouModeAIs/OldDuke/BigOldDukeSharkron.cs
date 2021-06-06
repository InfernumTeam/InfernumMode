using CalamityMod.Buffs.StatDebuffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod.Dusts;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Events;
using CalamityMod;

namespace InfernumMode.FuckYouModeAIs.OldDuke
{
	public class BigOldDukeSharkron : ModNPC
	{
		public ref float Time => ref npc.ai[0];
		public bool Phase2Variant => npc.ai[1] == 1f;
		public ref float DeathFade => ref npc.ai[2];
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Sulphurous Sharkron");
			NPCID.Sets.TrailingMode[npc.type] = 1;
		}
		
		public override void SetDefaults()
		{
			npc.aiStyle = -1;
			aiType = -1;
			npc.scale = 1.5f;
			npc.width = (int)(46 * npc.scale);
			npc.height = (int)(46 * npc.scale);
			npc.damage = 220;
			npc.defense = 100;
			npc.lifeMax = 22000;
			if (BossRushEvent.BossRushActive)
			{
				npc.lifeMax = 100000;
			}
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.knockBackResist = 0f;
			npc.alpha = 255;
			npc.noGravity = true;
			npc.noTileCollide = true;
			for (int k = 0; k < npc.buffImmune.Length; k++)
			{
				npc.buffImmune[k] = true;
			}
		}

		public override void AI()
		{
			if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead)
			{
				npc.TargetClosest(false);
				npc.netUpdate = true;
			}

			Player target = Main.player[npc.target];

			npc.alpha -= 12;
			if (npc.alpha < 0)
				npc.alpha = 0;

			int totalCharges = 3;

			Time++;
			float reelbackTime = Phase2Variant ? 35f : 45f;
			float chargeDelay = Phase2Variant ? 14f : 30f;
			float attackCycleTime = reelbackTime + chargeDelay;
			if (Time % attackCycleTime >= reelbackTime)
			{
				// Reel back.
				if (Time % attackCycleTime == reelbackTime)
				{
					npc.velocity = npc.DirectionTo(target.Center) * -7f;
					npc.rotation = npc.AngleTo(target.Center);
					npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
					if (npc.spriteDirection == -1)
						npc.rotation += MathHelper.Pi;
				}
				// And charge.
				if (Time % attackCycleTime == attackCycleTime - 1f)
				{
					npc.velocity = npc.DirectionTo(target.Center) * 26f;
					npc.rotation = npc.velocity.ToRotation();
					npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
					if (npc.spriteDirection == -1)
						npc.rotation += MathHelper.Pi;
				}
			}

			// Die after a certain amount of charges.
			if (Time >= attackCycleTime * (totalCharges + 1))
			{
				npc.life = 0;
				npc.HitEffect();
				npc.checkDead();
				npc.netUpdate = true;
			}
			DeathFade = Utils.InverseLerp(attackCycleTime * (totalCharges + 1) - 90f, attackCycleTime * (totalCharges + 1), Time, true);
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.damage = (int)(npc.damage * npc.GetExpertDamageMultiplier());
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			lightColor = Color.Lerp(lightColor, Color.Lime, DeathFade * 0.7f);
			SpriteEffects spriteEffects = SpriteEffects.FlipHorizontally;
			if (npc.spriteDirection == -1)
				spriteEffects = SpriteEffects.None;

			Texture2D sharkTexture = Main.npcTexture[npc.type];
			Vector2 origin = new Vector2(Main.npcTexture[npc.type].Width / 2, Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type] / 2);

			if (CalamityConfig.Instance.Afterimages)
			{
				for (int i = 1; i < 10; i += 2)
				{
					Color drawColor = lightColor;
					drawColor = Color.Lerp(drawColor, Color.Lime, 0.5f);
					drawColor = npc.GetAlpha(drawColor);
					drawColor *= (10f - i) / 15f;

					Vector2 drawPosition = npc.oldPos[i] + npc.Size / 2f - Main.screenPosition;
					drawPosition -= new Vector2(sharkTexture.Width, sharkTexture.Height / Main.npcFrameCount[npc.type]) * npc.scale / 2f;
					drawPosition += origin * npc.scale + Vector2.UnitY * (4f + npc.gfxOffY);
					spriteBatch.Draw(sharkTexture, drawPosition, npc.frame, drawColor, npc.rotation, origin, npc.scale, spriteEffects, 0f);
				}
			}

			Vector2 drawPosition2 = npc.Center - Main.screenPosition;
			drawPosition2 -= new Vector2(sharkTexture.Width, sharkTexture.Height / Main.npcFrameCount[npc.type]) * npc.scale / 2f;
			drawPosition2 += origin * npc.scale + new Vector2(0f, 4f + npc.gfxOffY);
			spriteBatch.Draw(sharkTexture, drawPosition2, npc.frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, spriteEffects, 0f);

			return false;
		}

		public override bool CanHitPlayer(Player target, ref int cooldownSlot)
		{
			cooldownSlot = 1;
			return npc.alpha == 0;
		}

		public override void OnHitPlayer(Player player, int damage, bool crit)
		{
			player.AddBuff(BuffID.Venom, 180, true);
			player.AddBuff(BuffID.Rabies, 180, true);
			player.AddBuff(BuffID.Poisoned, 180, true);
			player.AddBuff(ModContent.BuffType<Irradiated>(), 180);
		}

        public override bool CheckDead()
		{
			Main.PlaySound(SoundID.NPCDeath12, npc.position);

			npc.position += npc.Size * 0.5f;
			npc.width = npc.height = 96;
			npc.position -= npc.Size * 0.5f;

			for (int i = 0; i < 15; i++)
			{
				Dust acid = Dust.NewDustDirect(npc.position, npc.width, npc.height, (int)CalamityDusts.SulfurousSeaAcid, 0f, 0f, 100, default, 2f);
				acid.velocity.Y *= 6f;
				acid.velocity.X *= 3f;
				if (Main.rand.NextBool(2))
				{
					acid.scale = 0.5f;
					acid.fadeIn = 1f + Main.rand.Next(10) * 0.1f;
				}
			}

			for (int i = 0; i < 30; i++)
			{
				Dust acid = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.Blood, 0f, 0f, 100, default, 3f);
				acid.noGravity = true;
				acid.velocity.Y *= 10f;

				acid = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.Blood, 0f, 0f, 100, default, 2f);
				acid.velocity.X *= 2f;
			}

			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				int spawnX = npc.width / 2;
				int damage = Main.expertMode ? 55 : 70;
				for (int i = 0; i < 20; i++)
				{
					Projectile.NewProjectile(npc.Center + Vector2.UnitX * Main.rand.Next(-spawnX, spawnX), new Vector2(Main.rand.NextFloat(-5f, 5f), Main.rand.Next(-15, -10)), ModContent.ProjectileType<OldDukeGore>(), damage, 0f, Main.myPlayer, 0f, 0f);
				}
			}

			return true;
        }

        public override void HitEffect(int hitDirection, double damage)
		{
			for (int k = 0; k < 5; k++)
			{
				Dust.NewDust(npc.position, npc.width, npc.height, (int)CalamityDusts.SulfurousSeaAcid, hitDirection, -1f, 0, default, 1f);
			}
			if (npc.life <= 0)
			{
				for (int k = 0; k < 20; k++)
				{
					Dust.NewDust(npc.position, npc.width, npc.height, (int)CalamityDusts.SulfurousSeaAcid, hitDirection, -1f, 0, default, 1f);
				}
			}
		}
	}
}