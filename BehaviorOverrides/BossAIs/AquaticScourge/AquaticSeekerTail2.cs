using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AquaticScourge
{
	public class AquaticSeekerTail2 : ModNPC
	{
		public override void SetStaticDefaults() => DisplayName.SetDefault("Aquatic Seeker");

		public override void SetDefaults()
		{
			npc.damage = 50;
			npc.width = 16;
			npc.height = 22;
			npc.defense = 20;
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

		public override void AI() => AquaticSeekerBody2.DoSegmentBehavior(npc);

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale) => npc.lifeMax = AquaticSeekerHead2.TotalLife;

		public override bool CheckActive() => false;

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
				Gore.NewGore(npc.position, npc.velocity, InfernumMode.CalamityMod.GetGoreSlot("Gores/AquaticScourgeGores/AquaticSeekerTail"), 1f);
			}
		}
	}
}
