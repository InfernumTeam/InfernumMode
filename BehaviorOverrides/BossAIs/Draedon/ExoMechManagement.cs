using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
	public static class ExoMechManagement
	{
		public const float Phase2LifeRatio = 0.75f;
		public const float Phase3LifeRatio = 0.5f;
		public const float Phase4LifeRatio = 0.3f;
		public const float ComplementMechInvincibilityThreshold = 0.6f;
		public static bool ComplementMechIsPresent(NPC npc)
		{
			// Ares summons Thanatos.
			if (npc.type == ModContent.NPCType<AresBody>())
				return CalamityGlobalNPC.draedonExoMechWorm != -1;

			// Thanatos summons Ares.
			if (npc.type == ModContent.NPCType<ThanatosHead>())
				return CalamityGlobalNPC.draedonExoMechPrime != -1;

			// The twins summon Thanatos.
			if (npc.type == ModContent.NPCType<Apollo>() || npc.type == ModContent.NPCType<Artemis>())
				return CalamityGlobalNPC.draedonExoMechWorm != -1;

			return false;
		}

		public static NPC FindFinalMech()
		{
			int apolloID = ModContent.NPCType<Apollo>();
			int thanatosID = ModContent.NPCType<ThanatosHead>();
			int aresID = ModContent.NPCType<AresBody>();
			NPC initialMech = null;

			// Find the initial mech. If it cannot be found, return nothing.
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				if (Main.npc[i].type != apolloID && Main.npc[i].type != thanatosID && Main.npc[i].type != aresID)
					continue;
				if (!Main.npc[i].active)
					continue;

				if (Main.npc[i].Infernum().ExtraAI[11] == 0f)
				{
					initialMech = Main.npc[i];
					break;
				}
			}

			if (initialMech is null)
				return null;

			// Check to see if the initial mech believes that the final mech index is in use by a mech.
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				if (Main.npc[i].type != apolloID && Main.npc[i].type != thanatosID && Main.npc[i].type != aresID)
					continue;
				if (!Main.npc[i].active)
					continue;

				if (initialMech.Infernum().ExtraAI[12] == i)
					return Main.npc[i];
			}
			return null;
		}

		public static void SummonComplementMech(NPC npc)
		{
			// Don't summon NPCs clientside.
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			// Ares and twins summons Thanatos.
			// Only Apollo does the summoning.
			if (npc.type == ModContent.NPCType<AresBody>() || npc.type == ModContent.NPCType<Apollo>())
			{
				Vector2 thanatosSpawnPosition = Main.player[npc.target].Center + Vector2.UnitY * 2100f;
				int complementMech = NPC.NewNPC((int)thanatosSpawnPosition.X, (int)thanatosSpawnPosition.Y, ModContent.NPCType<ThanatosHead>(), 1);
				NPC thanatos = Main.npc[complementMech];
				npc.Infernum().ExtraAI[10] = complementMech;

				// Tell the newly summoned mech that it is not the initial mech and that it cannot summon more mechs on its own.
				thanatos.Infernum().ExtraAI[7] = 1f;
				thanatos.Infernum().ExtraAI[11] = 1f;
				thanatos.velocity = thanatos.SafeDirectionTo(Main.player[npc.target].Center) * 40f;

				thanatos.netUpdate = true;
			}

			// Thanatos summons Ares.
			if (npc.type == ModContent.NPCType<ThanatosHead>())
			{
				Vector2 aresSpawnPosition = Main.player[npc.target].Center - Vector2.UnitY * 1400f;
				int complementMech = NPC.NewNPC((int)aresSpawnPosition.X, (int)aresSpawnPosition.Y, ModContent.NPCType<AresBody>(), 1);
				NPC ares = Main.npc[complementMech];
				npc.Infernum().ExtraAI[10] = complementMech;

				// Tell the newly summoned mech that it is not the initial mech and that it cannot summon more mechs.
				ares.Infernum().ExtraAI[7] = 1f;
				ares.Infernum().ExtraAI[11] = 1f;

				ares.netUpdate = true;
			}
		}

		public static void SummonFinalMech(NPC npc)
		{
			// Don't summon NPCs clientside.
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			// Ares and Thanatos summon the twins.
			// Only Apollo is spawned since Apollo summons Artemis directly in its AI.
			if (npc.type == ModContent.NPCType<AresBody>() || npc.type == ModContent.NPCType<ThanatosHead>())
			{
				Vector2 apolloSpawnPosition = Main.player[npc.target].Center - Vector2.UnitY * 2100f;
				int finalMech = NPC.NewNPC((int)apolloSpawnPosition.X, (int)apolloSpawnPosition.Y, ModContent.NPCType<Apollo>(), 1);
				NPC apollo = Main.npc[finalMech];
				npc.Infernum().ExtraAI[12] = finalMech;

				// Tell the newly summoned mech that it is not the initial mech and that it cannot summon more mechs on its own.
				apollo.Infernum().ExtraAI[7] = 1f;
				apollo.Infernum().ExtraAI[11] = 1f;

				apollo.netUpdate = true;
			}

			// Twins summon Ares.
			if (npc.type == ModContent.NPCType<Apollo>())
			{
				Vector2 aresSpawnPosition = Main.player[npc.target].Center - Vector2.UnitY * 1400f;
				int finalMech = NPC.NewNPC((int)aresSpawnPosition.X, (int)aresSpawnPosition.Y, ModContent.NPCType<AresBody>(), 1);
				NPC ares = Main.npc[finalMech];
				npc.Infernum().ExtraAI[12] = finalMech;

				// Tell the newly summoned mech that it is not the initial mech and that it cannot summon more mechs.
				ares.Infernum().ExtraAI[7] = 1f;
				ares.Infernum().ExtraAI[11] = 1f;

				ares.netUpdate = true;
			}
		}

		public static int CurrentAresPhase
		{
			get
			{
				if (CalamityGlobalNPC.draedonExoMechPrime == -1)
					return 0;

				NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];
				if (FindFinalMech() == aresBody)
					return 5;
				if (ComplementMechIsPresent(aresBody) || aresBody.Infernum().ExtraAI[7] == 1f)
					return 4;
				if (aresBody.life <= aresBody.lifeMax * Phase3LifeRatio)
					return 3;
				if (aresBody.life <= aresBody.lifeMax * Phase2LifeRatio)
					return 2;

				return 1;
			}
		}

		public static int CurrentThanatosPhase
		{
			get
			{
				if (CalamityGlobalNPC.draedonExoMechWorm == -1)
					return 0;

				NPC thanatosHead = Main.npc[CalamityGlobalNPC.draedonExoMechWorm];
				if (FindFinalMech() == thanatosHead)
					return 5;
				if (ComplementMechIsPresent(thanatosHead) || thanatosHead.Infernum().ExtraAI[7] == 1f)
					return 4;
				if (thanatosHead.life <= thanatosHead.lifeMax * Phase3LifeRatio)
					return 3;
				if (thanatosHead.life <= thanatosHead.lifeMax * Phase2LifeRatio)
					return 2;

				return 1;
			}
		}

		public static int CurrentTwinsPhase
		{
			get
			{
				if (!NPC.AnyNPCs(ModContent.NPCType<Apollo>()))
					return 0;

				NPC apollo = Main.npc[NPC.FindFirstNPC(ModContent.NPCType<Apollo>())];

				// Check to ensure that Apollo's phase 2 animation has finished.
				if (apollo.ai[3] < ApolloBehaviorOverride.Phase2TransitionTime)
					return 1;

				if (FindFinalMech() == apollo)
					return 5;
				if (ComplementMechIsPresent(apollo) || apollo.Infernum().ExtraAI[7] == 1f)
					return 4;
				if (apollo.life <= apollo.lifeMax * Phase3LifeRatio)
					return 3;
				if (apollo.life <= apollo.lifeMax * Phase2LifeRatio)
					return 2;

				return 1;
			}
		}
	}
}
