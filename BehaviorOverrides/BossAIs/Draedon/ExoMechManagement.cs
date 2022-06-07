using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.ArtemisAndApollo;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.Athena;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.Thanatos;
using InfernumMode.GlobalInstances;
using Microsoft.Xna.Framework;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares.AresBodyBehaviorOverride;
using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.ArtemisAndApollo.ApolloBehaviorOverride;
using DraedonNPC = CalamityMod.NPCs.ExoMechs.Draedon;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
	public static class ExoMechManagement
	{
		public const int HasSummonedComplementMechIndex = 7;
		public const int ComplementMechIndexIndex = 10;
		public const int WasNotInitialSummonIndex = 11;
		public const int FinalMechIndexIndex = 12;
		public const int FinalPhaseTimerIndex = 16;
		public const int DeathAnimationTimerIndex = 19;
		public const int DeathAnimationHasStartedIndex = 22;

		public const int Thanatos_AttackDelayIndex = 13;

		public const int Ares_ProjectileDamageBoostIndex = 8;
		public const int Ares_LineTelegraphInterpolantIndex = 17;
		public const int Ares_LineTelegraphRotationIndex = 18;

		public const int Athena_EnragedIndex = 8;

		public const int Twins_ComplementMechEnrageTimerIndex = 15;
		public const int Twins_SideSwitchDelayIndex = 18;

		public const float Phase2LifeRatio = 0.85f;
		public const float Phase3LifeRatio = 0.625f;
		public const float Phase4LifeRatio = 0.5f;
		public const int FinalPhaseTransitionTime = 290;
		public const float ComplementMechInvincibilityThreshold = 0.5f;

		public static int CurrentAresPhase
		{
			get
			{
				if (CalamityGlobalNPC.draedonExoMechPrime == -1)
					return 0;

				NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];

				if (FindFinalMech() is null && aresBody.Infernum().ExtraAI[FinalMechIndexIndex] >= 0f)
					return TotalMechs == 1 ? 6 : 3;
				if (FindFinalMech() == aresBody)
					return 5;
				if (ComplementMechIsPresent(aresBody) || aresBody.Infernum().ExtraAI[HasSummonedComplementMechIndex] == 1f)
					return 4;
				if (aresBody.life <= aresBody.lifeMax * Phase3LifeRatio)
					return 3;
				if (aresBody.life <= aresBody.lifeMax * Phase2LifeRatio)
					return 2;

				return 1;
			}
		}

		public static int CurrentAthenaPhase
		{
			get
			{
				if (!NPC.AnyNPCs(ModContent.NPCType<AthenaNPC>()))
					return 0;

				NPC athena = Main.npc[NPC.FindFirstNPC(ModContent.NPCType<AthenaNPC>())];

				if (FindFinalMech() is null && athena.Infernum().ExtraAI[FinalMechIndexIndex] >= 0f)
					return TotalMechs == 1 ? 6 : 3;
				if (FindFinalMech() == athena)
					return 5;
				if (ComplementMechIsPresent(athena) || athena.Infernum().ExtraAI[HasSummonedComplementMechIndex] == 1f)
					return 4;
				if (athena.life <= athena.lifeMax * Phase3LifeRatio)
					return 3;
				if (athena.life <= athena.lifeMax * Phase2LifeRatio)
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

				if (FindFinalMech() is null && thanatosHead.Infernum().ExtraAI[FinalMechIndexIndex] >= 0f)
					return TotalMechs == 1 ? 6 : 3;
				if (FindFinalMech() == thanatosHead)
					return 5;
				if (ComplementMechIsPresent(thanatosHead) || thanatosHead.Infernum().ExtraAI[HasSummonedComplementMechIndex] == 1f)
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
				if (apollo.ai[3] < Phase2TransitionTime)
					return 1;

				if (FindFinalMech() is null && apollo.Infernum().ExtraAI[FinalMechIndexIndex] >= 0f)
					return TotalMechs == 1 ? 6 : 3;
				if (FindFinalMech() == apollo)
					return 5;
				if (ComplementMechIsPresent(apollo) || apollo.Infernum().ExtraAI[HasSummonedComplementMechIndex] == 1f)
					return 4;
				if (apollo.life <= apollo.lifeMax * Phase3LifeRatio)
					return 3;
				if (apollo.life <= apollo.lifeMax * Phase2LifeRatio)
					return 2;

				return 1;
			}
		}

		public static bool ExoTwinsAreEnteringSecondPhase
		{
			get
			{
				if (!NPC.AnyNPCs(ModContent.NPCType<Apollo>()))
					return false;

				NPC apollo = Main.npc[NPC.FindFirstNPC(ModContent.NPCType<Apollo>())];
				return apollo.ai[3] > 0f && apollo.ai[3] < Phase2TransitionTime;
			}
		}

		public static bool ExoTwinsAreInSecondPhase
		{
			get
			{
				if (!NPC.AnyNPCs(ModContent.NPCType<Apollo>()))
					return false;

				NPC apollo = Main.npc[NPC.FindFirstNPC(ModContent.NPCType<Apollo>())];
				return apollo.ai[3] >= Phase2TransitionTime;
			}
		}

		public static bool ExoMechIsPerformingDeathAnimation
		{
			get
			{
				for (int i = 0; i < Main.maxNPCs; i++)
				{
					if (Main.npc[i].active && Utilities.IsExoMech(Main.npc[i]) && ExoMechAIUtilities.PerformingDeathAnimation(Main.npc[i]))
						return true;
				}
				return false;
			}
		}

		public static bool ComplementMechIsPresent(NPC npc)
		{
			// Ares summons Thanatos.
			if (npc.type == ModContent.NPCType<AresBody>())
				return CalamityGlobalNPC.draedonExoMechWorm != -1;

			// Thanatos summons Ares.
			if (npc.type == ModContent.NPCType<ThanatosHead>())
				return CalamityGlobalNPC.draedonExoMechPrime != -1;

			// Athena summon the Twins.
			if (npc.type == ModContent.NPCType<AthenaNPC>())
				return GlobalNPCOverrides.Athena != -1;

			// The twins summon Thanatos.
			if (npc.type == ModContent.NPCType<Apollo>() || npc.type == ModContent.NPCType<Artemis>())
				return CalamityGlobalNPC.draedonExoMechWorm != -1;

			return false;
		}

		public static bool ShouldHaveSecondComboPhaseResistance(NPC npc)
		{
			if (npc.realLife >= 0)
				return ShouldHaveSecondComboPhaseResistance(Main.npc[npc.realLife]);

			return FindFinalMech() is null && npc.Infernum().ExtraAI[FinalMechIndexIndex] >= 0f && TotalMechs > 1 && npc == FindInitialMech();
		}

		public static NPC FindInitialMech()
		{
			int apolloID = ModContent.NPCType<Apollo>();
			int thanatosID = ModContent.NPCType<ThanatosHead>();
			int athenaID = ModContent.NPCType<AthenaNPC>();
			int aresID = ModContent.NPCType<AresBody>();
			NPC initialMech = null;

			// Find the initial mech. If it cannot be found, return nothing.
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				if (Main.npc[i].type != apolloID && Main.npc[i].type != thanatosID && Main.npc[i].type != athenaID && Main.npc[i].type != aresID)
					continue;
				if (!Main.npc[i].active)
					continue;

				if (Main.npc[i].Infernum().ExtraAI[WasNotInitialSummonIndex] == 0f)
				{
					initialMech = Main.npc[i];
					break;
				}
			}

			return initialMech;
		}

		public static NPC FindFinalMech()
		{
			NPC initialMech = FindInitialMech();

			if (initialMech is null)
				return null;

			// Check to see if the initial mech believes that the final mech index is in use by a mech.
			int apolloID = ModContent.NPCType<Apollo>();
			int thanatosID = ModContent.NPCType<ThanatosHead>();
			int athenaID = ModContent.NPCType<ThanatosHead>();
			int aresID = ModContent.NPCType<AresBody>();
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				if (Main.npc[i].type != apolloID && Main.npc[i].type != thanatosID && Main.npc[i].type != athenaID && Main.npc[i].type != aresID)
					continue;
				if (!Main.npc[i].active)
					continue;

				if (initialMech.Infernum().ExtraAI[FinalMechIndexIndex] == i)
					return Main.npc[i];
			}
			return null;
		}

		public static void ClearAwayTransitionProjectiles()
		{
			// Clear away old projectiles.
			int[] projectilesToDelete = new int[]
			{
				ModContent.ProjectileType<ArtemisLaser>(),
				ModContent.ProjectileType<ArtemisGatlingLaser>(),
				ModContent.ProjectileType<PlasmaGas>(),
				ModContent.ProjectileType<ElectricGas>(),
				ModContent.ProjectileType<TeslaSpark>(),
				ModContent.ProjectileType<AresTeslaOrb>(),
				ModContent.ProjectileType<ApolloChargeFlameExplosion>(),
				ModContent.ProjectileType<ArtemisChargeFlameExplosion>(),
				ModContent.ProjectileType<ExofireSpark>(),
				ModContent.ProjectileType<PlasmaSpark>(),
				ModContent.ProjectileType<AresRocket>(),
				ModContent.ProjectileType<AresSpinningDeathBeam>(),
				ModContent.ProjectileType<AresSpinningRedDeathray>(),
				ModContent.ProjectileType<ExolaserBomb>(),
				ModContent.ProjectileType<RefractionRotor>(),
				ModContent.ProjectileType<PulseBeamStart>(),
				ModContent.ProjectileType<ThanatosComboLaser>(),
				ModContent.ProjectileType<PulseLaser>(),
			};
			for (int i = 0; i < Main.maxProjectiles; i++)
			{
				if (projectilesToDelete.Contains(Main.projectile[i].type))
					Main.projectile[i].active = false;
			}
		}

		public static void SummonComplementMech(NPC npc)
		{
			MakeDraedonSayThings(1);

			// Don't summon NPCs clientside.
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			// Clear away projectiles.
			ClearAwayTransitionProjectiles();

			// Thanatos and twins summon Ares.
			// Only Apollo does the summoning.
			if (npc.type == ModContent.NPCType<ThanatosHead>() || npc.type == ModContent.NPCType<Apollo>())
			{
				Vector2 thanatosSpawnPosition = Main.player[npc.target].Center + Vector2.UnitY * 2100f;
				int complementMech = NPC.NewNPC((int)thanatosSpawnPosition.X, (int)thanatosSpawnPosition.Y, ModContent.NPCType<AresBody>(), 1);
				NPC ares = Main.npc[complementMech];
				npc.Infernum().ExtraAI[ComplementMechIndexIndex] = complementMech;

				// Tell the newly summoned mech that it is not the initial mech and that it cannot summon more mechs on its own.
				ares.Infernum().ExtraAI[HasSummonedComplementMechIndex] = 1f;
				ares.Infernum().ExtraAI[WasNotInitialSummonIndex] = 1f;
				ares.velocity = ares.SafeDirectionTo(Main.player[npc.target].Center) * 40f;
				ares.Opacity = 0.01f;

				ares.netUpdate = true;
			}

			// Athena summons the twins.
			if (npc.type == ModContent.NPCType<AthenaNPC>())
			{
				Vector2 apolloSpawnPosition = Main.player[npc.target].Center - Vector2.UnitY * 1400f;
				int complementMech = NPC.NewNPC((int)apolloSpawnPosition.X, (int)apolloSpawnPosition.Y, ModContent.NPCType<Apollo>(), 1);
				NPC apollo = Main.npc[complementMech];
				npc.Infernum().ExtraAI[ComplementMechIndexIndex] = complementMech;

				// Tell the newly summoned mech that it is not the initial mech and that it cannot summon more mechs.
				apollo.Infernum().ExtraAI[HasSummonedComplementMechIndex] = 1f;
				apollo.Infernum().ExtraAI[WasNotInitialSummonIndex] = 1f;

				apollo.netUpdate = true;
			}

			// Ares summons Thanatos.
			if (npc.type == ModContent.NPCType<AresBody>())
			{
				Vector2 aresSpawnPosition = Main.player[npc.target].Center - Vector2.UnitY * 1400f;
				int complementMech = NPC.NewNPC((int)aresSpawnPosition.X, (int)aresSpawnPosition.Y, ModContent.NPCType<ThanatosHead>(), 1);
				NPC thanatos = Main.npc[complementMech];
				npc.Infernum().ExtraAI[ComplementMechIndexIndex] = complementMech;

				// Tell the newly summoned mech that it is not the initial mech and that it cannot summon more mechs.
				thanatos.Infernum().ExtraAI[HasSummonedComplementMechIndex] = 1f;
				thanatos.Infernum().ExtraAI[WasNotInitialSummonIndex] = 1f;
				thanatos.Opacity = 0.01f;

				thanatos.netUpdate = true;
			}
		}

		public static void SummonFinalMech(NPC npc)
		{
			MakeDraedonSayThings(3);

			// Don't summon NPCs clientside.
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			// Clear away projectiles.
			ClearAwayTransitionProjectiles();

			// Ares and Thanatos summon the twins.
			// Only Apollo is spawned since Apollo summons Artemis directly in its AI.
			if (npc.type == ModContent.NPCType<AresBody>() || npc.type == ModContent.NPCType<ThanatosHead>())
			{
				Vector2 apolloSpawnPosition = Main.player[npc.target].Center - Vector2.UnitY * 2100f;
				int finalMech = NPC.NewNPC((int)apolloSpawnPosition.X, (int)apolloSpawnPosition.Y, ModContent.NPCType<Apollo>(), 1);
				NPC apollo = Main.npc[finalMech];
				npc.Infernum().ExtraAI[FinalMechIndexIndex] = finalMech;
				Main.npc[(int)npc.Infernum().ExtraAI[ComplementMechIndexIndex]].Infernum().ExtraAI[FinalMechIndexIndex] = finalMech;

				// Tell the newly summoned mech that it is not the initial mech and that it cannot summon more mechs on its own.
				apollo.Infernum().ExtraAI[HasSummonedComplementMechIndex] = 1f;
				apollo.Infernum().ExtraAI[WasNotInitialSummonIndex] = 1f;

				apollo.netUpdate = true;
			}

			// Athena summons Ares.
			if (npc.type == ModContent.NPCType<AthenaNPC>())
			{
				Vector2 aresSpawnPosition = Main.player[npc.target].Center - Vector2.UnitY * 1400f;
				int finalMech = NPC.NewNPC((int)aresSpawnPosition.X, (int)aresSpawnPosition.Y, ModContent.NPCType<AresBody>(), 1);
				NPC ares = Main.npc[finalMech];
				npc.Infernum().ExtraAI[FinalMechIndexIndex] = finalMech;
				Main.npc[(int)npc.Infernum().ExtraAI[ComplementMechIndexIndex]].Infernum().ExtraAI[FinalMechIndexIndex] = finalMech;

				// Tell the newly summoned mech that it is not the initial mech and that it cannot summon more mechs.
				ares.ai[0] = (int)AresBodyAttackType.IdleHover;
				ares.Infernum().ExtraAI[HasSummonedComplementMechIndex] = 1f;
				ares.Infernum().ExtraAI[WasNotInitialSummonIndex] = 1f;

				ares.netUpdate = true;
			}

			// Twins summon Thanatos.
			if (npc.type == ModContent.NPCType<Apollo>())
			{
				Vector2 thanatosSpawnPosition = Main.player[npc.target].Center - Vector2.UnitY * 1400f;
				int finalMech = NPC.NewNPC((int)thanatosSpawnPosition.X, (int)thanatosSpawnPosition.Y, ModContent.NPCType<ThanatosHead>(), 1);
				NPC thanatos = Main.npc[finalMech];
				npc.Infernum().ExtraAI[FinalMechIndexIndex] = finalMech;
				Main.npc[(int)npc.Infernum().ExtraAI[ComplementMechIndexIndex]].Infernum().ExtraAI[FinalMechIndexIndex] = finalMech;

				// Tell the newly summoned mech that it is not the initial mech and that it cannot summon more mechs.
				thanatos.Infernum().ExtraAI[HasSummonedComplementMechIndex] = 1f;
				thanatos.Infernum().ExtraAI[WasNotInitialSummonIndex] = 1f;

				thanatos.netUpdate = true;
			}
		}

		public static int TotalMechs
		{
			get
			{
				int apolloID = ModContent.NPCType<Apollo>();
				int thanatosID = ModContent.NPCType<ThanatosHead>();
				int athenaID = ModContent.NPCType<AthenaNPC>();
				int aresID = ModContent.NPCType<AresBody>();
				int count = 0;
				for (int i = 0; i < Main.maxNPCs; i++)
				{
					if (Main.npc[i].type != apolloID && Main.npc[i].type != thanatosID && Main.npc[i].type != athenaID && Main.npc[i].type != aresID)
						continue;
					if (!Main.npc[i].active || Main.npc[i].Opacity <= 0f)
						continue;

					count++;
				}

				return count;
			}
		}

		public static void MakeDraedonSayThings(int statementType)
		{
			if (!Main.npc.IndexInRange(CalamityGlobalNPC.draedon))
				return;

			if (Main.npc[CalamityGlobalNPC.draedon].localAI[0] < statementType)
			{
				Main.npc[CalamityGlobalNPC.draedon].localAI[0] = statementType;
				Main.npc[CalamityGlobalNPC.draedon].ai[0] = DraedonNPC.ExoMechPhaseDialogueTime;
			}
		}

		// Bias attacks back to a normal on completion.
		// This does not happen if the player dies to the attack.
		public static void DoPostAttackSelections(NPC npc)
		{
			Player player = Main.player[npc.target];
			if (npc.type == ModContent.NPCType<Apollo>())
			{
				var attack = (TwinsAttackType)(int)npc.ai[0];

				int attackToReinforce = -1;
				if (attack == TwinsAttackType.SpecialAttack_LaserRayScarletBursts)
					attackToReinforce = 0;
				if (attack == TwinsAttackType.SpecialAttack_PlasmaCharges)
					attackToReinforce = 1;

				if (attackToReinforce != -1)
					player.Infernum().ThanatosLaserTypeSelector.BiasAwayFrom(attackToReinforce);
			}
		}

		public static void RecordAttackDeath(Player player)
		{
			int ares = CalamityGlobalNPC.draedonExoMechPrime;
			int apollo = CalamityGlobalNPC.draedonExoMechTwinGreen;

			if (ares != -1)
			{
				var attack = (AresBodyAttackType)(int)Main.npc[ares].ai[0];
				int attackToReinforce = -1;
				if (attack == AresBodyAttackType.LaserSpinBursts)
					attackToReinforce = 0;
				if (attack == AresBodyAttackType.DirectionChangingSpinBursts)
					attackToReinforce = 1;

				if (attackToReinforce != -1)
					player.Infernum().AresSpecialAttackTypeSelector.BiasInFavorOf(attackToReinforce);
			}

			if (apollo != -1)
			{
				var attack = (TwinsAttackType)(int)Main.npc[apollo].ai[0];
				int attackToReinforce = -1;
				if (attack == TwinsAttackType.SpecialAttack_LaserRayScarletBursts)
					attackToReinforce = 0;
				if (attack == TwinsAttackType.SpecialAttack_PlasmaCharges)
					attackToReinforce = 1;

				if (attackToReinforce != -1)
					player.Infernum().TwinsSpecialAttackTypeSelector.BiasInFavorOf(attackToReinforce);
			}
		}
	}
}
