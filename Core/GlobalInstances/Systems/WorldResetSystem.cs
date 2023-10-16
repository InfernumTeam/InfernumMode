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
            WorldSaveSystem.InfernumModeEnabled = false;
            WorldSaveSystem.AbyssLayer1ForestSeed = 0;
            WorldSaveSystem.AbyssLayer3CavernSeed = 0;
            //WorldSaveSystem.BlossomGardenCenter = Point.Zero;
            WorldSaveSystem.DisplayedEmodeWarningText = false;
            WorldSaveSystem.DownedBereftVassal = false;
            //WorldSaveSystem.EidolistWorshipPedestalCenter = Point.Zero;
            //WorldSaveSystem.ForbiddenArchiveCenter = Point.Zero;
            WorldSaveSystem.HasBeatenInfernumNightProvBeforeDay = false;
            WorldSaveSystem.HasBeatenInfernumProvRegularly = false;
            WorldSaveSystem.HasDefeatedEidolists = false;
            WorldSaveSystem.HasGeneratedColosseumEntrance = true;
            WorldSaveSystem.HasGeneratedProfanedShrine = true;
            WorldSaveSystem.HasOpenedLostColosseumPortal = false;
            WorldSaveSystem.HasProvidenceDoorShattered = false;
            WorldSaveSystem.HasSepulcherAnimationBeenPlayed = false;
            WorldSaveSystem.LostColosseumPortalAnimationTimer = WorldSaveSystem.LostColosseumPortalAnimationTimer;
            WorldSaveSystem.MetCalamitasAtCrags = false;
            WorldSaveSystem.MetSignusAtProfanedGarden = false;
            WorldSaveSystem.PerformedLacewingAnimation = false;
            //WorldSaveSystem.ProvidenceArena = Rectangle.Empty;
            //WorldSaveSystem.ProvidenceDoorXPosition = 0;
            //WorldSaveSystem.SquidDenCenter = Point.Zero;
            WorldSaveSystem.WayfinderGateLocation = Vector2.Zero;
            WorldSaveSystem.HasSeenDoGCutscene = false;
        }

        public override void OnWorldUnload()
        {
            WorldSaveSystem.InfernumModeEnabled = false;
            CreditManager.StopAbruptly();
        }
    }
}
