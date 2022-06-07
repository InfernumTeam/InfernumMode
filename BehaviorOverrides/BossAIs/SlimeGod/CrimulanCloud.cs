using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SlimeGod
{
	public class CrimulanCloud : ModNPC
	{
		public ref float Time => ref npc.ai[0];
		public Player Target => Main.player[npc.target];
		public override void SetStaticDefaults() => DisplayName.SetDefault("Crimulan Cloud");

		public override void SetDefaults()
		{
			npc.damage = 0;
			npc.width = npc.height = 42;
			npc.lifeMax = 20;
			npc.noTileCollide = false;
			npc.noGravity = true;
			npc.netAlways = true;
			npc.dontTakeDamage = true;
			npc.aiStyle = aiType = -1;
			npc.scale = 1f;
			npc.knockBackResist = 0f;
			npc.noTileCollide = true;
		}

		public override void AI()
		{
			if (!Main.npc.IndexInRange(CalamityGlobalNPC.slimeGodRed))
			{
				npc.active = false;
				npc.netUpdate = true;
				return;
			}

			npc.TargetClosest();

			Vector2 destination = Target.Center + Vector2.UnitX * 450f;
			npc.Center = npc.Center.MoveTowards(destination, 5f);
			if (npc.Center != destination)
				npc.velocity = (npc.velocity * 15f + npc.SafeDirectionTo(destination) * 13f) / 16f;
			else
				npc.velocity = Vector2.Zero;

			for (int i = 0; i < 4; i++)
			{
				Dust gel = Dust.NewDustDirect(npc.position, npc.width, npc.height, Main.rand.NextBool(2) ? 4 : 267);
				gel.color = Color.Red;
				gel.velocity = Main.rand.NextVector2Circular(3f, 3f);
				gel.noGravity = true;
			}

			Time++;
		}

		public override bool CheckActive() => false;
	}
}
