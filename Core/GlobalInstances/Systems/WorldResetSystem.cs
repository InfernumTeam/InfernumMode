using CalamityMod.NPCs.AstrumAureus;
using CalamityMod.NPCs.Yharon;
using InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians;
using InfernumMode.Content.Credits;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class WorldResetSystem : ModSystem
    {
        // For some reason, resetting worldgen stuff here seems to fully reset it, implying it isn't being correctly saved?
        // TODO: Look into this.
        public override void OnWorldLoad()
        {
            ResetEverything();
        }

        public override void OnWorldUnload()
        {
            ResetEverything();
            CreditManager.StopAbruptly();
        }

        public override void PreUpdateNPCs()
        {
            static void ResetSavedIndex(ref int index, int type, int type2 = -1)
            {
                if (index >= 0)
                {
                    // If the index npc is not active, reset the index.
                    if (!Main.npc[index].active)
                        index = -1;

                    // Else, if this is -1,
                    else if (type2 == -1)
                    {
                        // If the index is not the correct type, reset the index.
                        if (Main.npc[index].type != type)
                            index = -1;
                    }
                    else
                    {
                        if (Main.npc[type].type != type && Main.npc[index].type != type2)
                            index = -1;
                    }
                }
            }

            ResetSavedIndex(ref GlobalNPCOverrides.Cryogen, ModContent.NPCType<CalamityMod.NPCs.Cryogen.Cryogen>());
            ResetSavedIndex(ref GlobalNPCOverrides.AstrumAureus, ModContent.NPCType<AstrumAureus>());
            ResetSavedIndex(ref GlobalNPCOverrides.ProfanedCrystal, ModContent.NPCType<HealerShieldCrystal>());
            ResetSavedIndex(ref GlobalNPCOverrides.Yharon, ModContent.NPCType<Yharon>());
        }

        internal static void ResetEverything()
        {
            WorldSaveSystem.InfernumModeEnabled = false;
            WorldSaveSystem.HasGeneratedProfanedShrine = false;
            WorldSaveSystem.HasGeneratedColosseumEntrance = false;
            WorldSaveSystem.HasBeatenInfernumProvRegularly = false;
            WorldSaveSystem.HasBeatenInfernumNightProvBeforeDay = false;
            WorldSaveSystem.HasProvidenceDoorShattered = false;
            WorldSaveSystem.HasSepulcherAnimationBeenPlayed = false;
            WorldSaveSystem.InPostAEWUpdateWorld = false;
            WorldSaveSystem.HasOpenedLostColosseumPortal = false;
            WorldSaveSystem.DownedBereftVassal = false;
            WorldSaveSystem.DisplayedEmodeWarningText = false;
            WorldSaveSystem.PerformedLacewingAnimation = false;
            WorldSaveSystem.MetSignusAtProfanedGarden = false;
            WorldSaveSystem.MetCalamitasAtCrags = false;
            WorldSaveSystem.HasSeenDoGCutscene = false;
            WorldSaveSystem.HasSeenPostMechsCutscene = false;
            //WorldSaveSystem.ProvidenceArena = Rectangle.Empty;
            //WorldSaveSystem.ProvidenceDoorXPosition = 0;
            WorldSaveSystem.AbyssLayer1ForestSeed = 0;
            WorldSaveSystem.AbyssLayer3CavernSeed = 0;
            //WorldSaveSystem.SquidDenCenter = Point.Zero;
            //WorldSaveSystem.EidolistWorshipPedestalCenter = Point.Zero;
            //WorldSaveSystem.ForbiddenArchiveCenter = Point.Zero;
            //WorldSaveSystem.BlossomGardenCenter = Point.Zero;
            WorldSaveSystem.HasDefeatedEidolists = false;
            WorldSaveSystem.LostColosseumPortalAnimationTimer = WorldSaveSystem.LostColosseumPortalAnimationTimer;
            WorldSaveSystem.WayfinderGateLocation = Vector2.Zero;
        }
    }
}
