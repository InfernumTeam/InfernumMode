using CalamityMod.NPCs.CeaselessVoid;
using CalamityMod.NPCs.Signus;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class WorldSaveSystem : ModSystem
    {
        private static bool infernumModeEnabled;

        public static int AbyssLayer1ForestSeed
        {
            get;
            set;
        }

        public static int AbyssLayer3CavernSeed
        {
            get;
            set;
        }

        public static bool HasGeneratedProfanedShrine
        {
            get;
            set;
        }

        public static bool HasGeneratedColosseumEntrance
        {
            get;
            set;
        }

        public static bool HasBeatenInfernumProvRegularly
        {
            get;
            set;
        }

        public static bool HasBeatenInfernumNightProvBeforeDay
        {
            get;
            set;
        }

        public static bool InfernumModeEnabled
        {
            get => infernumModeEnabled;
            set
            {
                if (!value)
                {
                    int signusID = ModContent.NPCType<Signus>();
                    int ceaselessVoidID = ModContent.NPCType<CeaselessVoid>();
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC n = Main.npc[i];
                        if (n.active && (n.type == signusID || n.type == ceaselessVoidID))
                            n.active = false;
                    }

                    CalamityBossHPBarChangesSystem.UndoBarChanges();
                }
                else
                {
                    CalamityBossHPBarChangesSystem.PerformBarChanges();
                    CeaselessVoidArchivesSpawnSystem.WaitingForPlayersToLeaveArchives = true;
                }

                infernumModeEnabled = value;
            }
        }

        public static Rectangle ProvidenceArena
        {
            get;
            set;
        } = Rectangle.Empty;

        public static int ProvidenceDoorXPosition
        {
            get;
            set;
        }

        public static bool HasSepulcherAnimationBeenPlayed
        {
            get;
            set;
        }

        public static bool HasProvidenceDoorShattered
        {
            get;
            set;
        }

        public static bool HasDefeatedEidolists
        {
            get;
            set;
        }

        public static Point SquidDenCenter
        {
            get;
            set;
        }

        public static Point EidolistWorshipPedestalCenter
        {
            get;
            set;
        }

        // This value is only set to true in new worldgen code. All prior worlds will never naturally have this flag enabled.
        // This is done to allow backwards compatibility with old Abyss worldgen.
        public static bool InPostAEWUpdateWorld
        {
            get;
            set;
        }

        public static Vector2 WayfinderGateLocation
        {
            get;
            set;
        } = Vector2.Zero;

        public static bool HasOpenedLostColosseumPortal
        {
            get;
            set;
        }

        // Use a backing field, for ToastyQoL support.
        private static bool downedBereftVassal;

        public static bool DownedBereftVassal
        {
            get => downedBereftVassal;
            set => downedBereftVassal = value;
        }

        public static bool DisplayedEmodeWarningText
        {
            get;
            set;
        }

        public static int LostColosseumPortalAnimationTimer
        {
            get;
            set;
        } = LostColosseumPortalAnimationTime;

        public static bool PerformedLacewingAnimation
        {
            get;
            set;
        }

        public static bool MetSignusAtProfanedGarden
        {
            get;
            set;
        }

        public static bool MetCalamitasAtCrags
        {
            get;
            set;
        }

        public static Point ForbiddenArchiveCenter
        {
            get;
            set;
        }

        public static Point BlossomGardenCenter
        {
            get;
            set;
        }

        public static bool HasSeenDoGCutscene
        {
            get;
            set;
        }

        public static bool HasSeenPostMechsCutscene
        {
            get;
            set;
        }

        public const int LostColosseumPortalAnimationTime = 150;

        public override void SaveWorldData(TagCompound tag)
        {
            var downed = new List<string>();
            if (InfernumModeEnabled)
                downed.Add("InfernumModeActive");
            if (HasGeneratedProfanedShrine)
                downed.Add("HasGeneratedProfanedShrine");
            if (HasGeneratedColosseumEntrance)
                downed.Add("HasGeneratedColosseumEntrance");
            if (HasBeatenInfernumProvRegularly)
                downed.Add("HasBeatenInfernumProvRegularly");
            if (HasBeatenInfernumNightProvBeforeDay)
                downed.Add("HasBeatenInfernumNightProvBeforeDay");
            if (HasProvidenceDoorShattered)
                downed.Add("HasProvidenceDoorShattered");
            if (HasSepulcherAnimationBeenPlayed)
                downed.Add("HasSepulcherAnimationBeenPlayed");
            if (InPostAEWUpdateWorld)
                downed.Add("InPostAEWUpdateWorld");
            if (HasOpenedLostColosseumPortal)
                downed.Add("HasOpenedLostColosseumPortal");
            if (DownedBereftVassal)
                downed.Add("DownedBereftVassal");
            if (DisplayedEmodeWarningText)
                downed.Add("DisplayedEmodeWarningText");
            if (PerformedLacewingAnimation)
                downed.Add("PerformedLacewingAnimation");
            if (MetSignusAtProfanedGarden)
                downed.Add("MetSignusAtProfanedGarden");
            if (MetCalamitasAtCrags)
                downed.Add("MetCalamitasAtCrags");
            if (HasSeenDoGCutscene)
                downed.Add("HasSeenDoGCutscene");
            if (HasSeenPostMechsCutscene)
                downed.Add("HasSeenPostMechsCutscene");

            tag["downed"] = downed;
            tag["ProvidenceArenaX"] = ProvidenceArena.X;
            tag["ProvidenceArenaY"] = ProvidenceArena.Y;
            tag["ProvidenceArenaWidth"] = ProvidenceArena.Width;
            tag["ProvidenceArenaHeight"] = ProvidenceArena.Height;
            tag["ProvidenceDoorXPosition"] = ProvidenceDoorXPosition;

            tag["AbyssLayer1ForestSeed"] = AbyssLayer1ForestSeed;
            tag["AbyssLayer3CavernSeed"] = AbyssLayer3CavernSeed;
            tag["SquidDenCenterX"] = SquidDenCenter.X;
            tag["SquidDenCenterY"] = SquidDenCenter.Y;
            tag["EidolistWorshipPedestalCenterX"] = EidolistWorshipPedestalCenter.X;
            tag["EidolistWorshipPedestalCenterY"] = EidolistWorshipPedestalCenter.Y;

            tag["DreamgateLocationX"] = WayfinderGateLocation.X;
            tag["DreamgateLocationY"] = WayfinderGateLocation.Y;

            tag["ForbiddenArchiveCenterX"] = ForbiddenArchiveCenter.X;
            tag["ForbiddenArchiveCenterY"] = ForbiddenArchiveCenter.Y;
            tag["BlossomGardenCenterX"] = BlossomGardenCenter.X;
            tag["BlossomGardenCenterY"] = BlossomGardenCenter.Y;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            var downed = tag.GetList<string>("downed");
            InfernumModeEnabled = downed.Contains("InfernumModeActive");
            HasGeneratedProfanedShrine = downed.Contains("HasGeneratedProfanedShrine");
            HasGeneratedColosseumEntrance = downed.Contains("HasGeneratedColosseumEntrance");

            // This used to be internally represented with a spelling error in the NBT data.
            // As such, a legacy check is used to ensure that world data that has the old string is not discarded.
            HasBeatenInfernumProvRegularly = downed.Contains("HasBeatedInfernumProvRegularly") || downed.Contains("HasBeatenInfernumProvRegularly");
            HasBeatenInfernumNightProvBeforeDay = downed.Contains("HasBeatedInfernumNightProvBeforeDay") || downed.Contains("HasBeatenInfernumNightProvBeforeDay");

            HasProvidenceDoorShattered = downed.Contains("HasProvidenceDoorShattered");
            HasSepulcherAnimationBeenPlayed = downed.Contains("HasSepulcherAnimationBeenPlayed");
            InPostAEWUpdateWorld = downed.Contains("InPostAEWUpdateWorld");
            HasOpenedLostColosseumPortal = downed.Contains("HasOpenedLostColosseumPortal");
            DownedBereftVassal = downed.Contains("DownedBereftVassal");
            DisplayedEmodeWarningText = downed.Contains("DisplayedEmodeWarningText");
            PerformedLacewingAnimation = downed.Contains("PerformedLacewingAnimation");
            MetSignusAtProfanedGarden = downed.Contains("MetSignusAtProfanedGarden");
            MetCalamitasAtCrags = downed.Contains("MetCalamitasAtCrags");
            HasSeenDoGCutscene = downed.Contains("HasSeenDoGCutscene");
            HasSeenPostMechsCutscene = downed.Contains("HasSeenPostMechsCutscene");

            ProvidenceArena = new(tag.GetInt("ProvidenceArenaX"), tag.GetInt("ProvidenceArenaY"), tag.GetInt("ProvidenceArenaWidth"), tag.GetInt("ProvidenceArenaHeight"));
            ProvidenceDoorXPosition = tag.GetInt("ProvidenceDoorXPosition");

            AbyssLayer1ForestSeed = tag.GetInt("AbyssLayer1ForestSeed");
            AbyssLayer3CavernSeed = tag.GetInt("AbyssLayer3CavernSeed");
            SquidDenCenter = new(tag.GetInt("SquidDenCenterX"), tag.GetInt("SquidDenCenterY"));
            EidolistWorshipPedestalCenter = new(tag.GetInt("EidolistWorshipPedestalCenterX"), tag.GetInt("EidolistWorshipPedestalCenterY"));

            WayfinderGateLocation = new(tag.GetFloat("DreamgateLocationX"), tag.GetFloat("DreamgateLocationY"));

            ForbiddenArchiveCenter = new(tag.GetInt("ForbiddenArchiveCenterX"), tag.GetInt("ForbiddenArchiveCenterY"));

            BlossomGardenCenter = new(tag.GetInt("BlossomGardenCenterX"), tag.GetInt("BlossomGardenCenterY"));
            FargosBridgeModcallSupport();
        }

        public static void FargosBridgeModcallSupport()
        {
            var mutant = InfernumMode.FargosMutantMod;
            if (mutant == null || ProvidenceArena == Rectangle.Empty)
                return;

            string command = "AddIndestructibleRectangle";
            mutant.Call(command, ProvidenceArena.ToWorldCoords());
        }
    }
}
