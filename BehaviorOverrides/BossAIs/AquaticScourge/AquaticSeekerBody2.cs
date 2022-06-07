using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AquaticScourge
{
	public class AquaticSeekerBody2 : ModNPC
	{
		public override void SetStaticDefaults() => DisplayName.SetDefault("Aquatic Seeker");

		public override void SetDefaults()
		{
			npc.damage = 60;
			npc.width = 16;
			npc.height = 16;
			npc.defense = 10;
			npc.lifeMax = AquaticSeekerHead2.TotalLife;
			npc.aiStyle = aiType = -1;
			npc.knockBackResist = 0f;
			npc.alpha = 255;
			npc.behindTiles = true;
			npc.noGravity = true;
			npc.noTileCollide = true;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.netAlways = true;
			npc.dontCountMe = true;
		}

		public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) => false;

		public static void DoSegmentBehavior(NPC npc)
		{
			if (!Main.npc.IndexInRange((int)npc.ai[1]) || !Main.npc[(int)npc.ai[1]].active)
			{
				npc.life = 0;
				npc.HitEffect(0, 10.0);
				npc.active = false;
				npc.netUpdate = true;
				return;
			}

			NPC aheadSegment = Main.npc[(int)npc.ai[1]];
			npc.target = aheadSegment.target;

			if (aheadSegment.alpha < 128)
				npc.alpha = Utils.Clamp(npc.alpha - 42, 0, 255);

			npc.defense = aheadSegment.defense;
			npc.dontTakeDamage = aheadSegment.dontTakeDamage;

			Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
			if (aheadSegment.rotation != npc.rotation)
				directionToNextSegment = directionToNextSegment.RotatedBy(MathHelper.WrapAngle(aheadSegment.rotation - npc.rotation) * 0.075f);

			npc.rotation = directionToNextSegment.ToRotation() + MathHelper.PiOver2;
			npc.Center = aheadSegment.Center - directionToNextSegment.SafeNormalize(Vector2.Zero) * npc.width * npc.scale;
		}

		public override void AI() => DoSegmentBehavior(npc);

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale) => npc.lifeMax = AquaticSeekerHead2.TotalLife;

		public override bool CheckActive()
		{
			return false;
		}

		public override bool PreNPCLoot()
		{
			return false;
		}

		public override void HitEffect(int hitDirection, double damage)
		{
			for (int k = 0; k < 3; k++)
			{
				Dust.NewDust(npc.position, npc.width, npc.height, DustID.Blood, hitDirection, -1f, 0, default, 1f);
			}
			if (npc.life <= 0)
			{
				for (int k = 0; k < 10; k++)
				{
					Dust.NewDust(npc.position, npc.width, npc.height, DustID.Blood, hitDirection, -1f, 0, default, 1f);
				}
				Gore.NewGore(npc.position, npc.velocity, InfernumMode.CalamityMod.GetGoreSlot("Gores/AquaticScourgeGores/AquaticSeekerBody"), 1f);
			}
		}
	}
}
