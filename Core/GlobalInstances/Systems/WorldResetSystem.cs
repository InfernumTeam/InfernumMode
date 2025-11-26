using InfernumMode.Content.Credits;
using Microsoft.Xna.Framework;
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
            WorldSaveSystem.DownedDreadnautilus = false;
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
