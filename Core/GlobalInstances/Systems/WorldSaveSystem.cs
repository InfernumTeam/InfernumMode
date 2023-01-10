using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class WorldSaveSystem : ModSystem
    {
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

        public static bool HasBeatedInfernumProvRegularly
        {
            get;
            set;
        }

        public static bool HasBeatedInfernumNightProvBeforeDay
        {
            get;
            set;
        }

        public static bool InfernumMode
        {
            get;
            set;
        } = false;

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
        } = false;

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

        public static bool DownedBereftVassal
        {
            get;
            set;
        }

        public static int LostColosseumPortalAnimationTimer
        {
            get;
            set;
        } = LostColosseumPortalAnimationTime;

        public const int LostColosseumPortalAnimationTime = 150;

        public override void SaveWorldData(TagCompound tag)
        {
            var downed = new List<string>();
            if (InfernumMode)
                downed.Add("InfernumModeActive");
            if (HasGeneratedProfanedShrine)
                downed.Add("HasGeneratedProfanedShrine");
            if (HasGeneratedColosseumEntrance)
                downed.Add("HasGeneratedColosseumEntrance");
            if (HasBeatedInfernumProvRegularly)
                downed.Add("HasBeatedInfernumProvRegularly");
            if (HasBeatedInfernumNightProvBeforeDay)
                downed.Add("HasBeatedInfernumNightProvBeforeDay");
            if (HasProvidenceDoorShattered)
                downed.Add("HasProvidenceDoorShattered");
            if (HasSepulcherAnimationBeenPlayed)
                downed.Add("HasSepulcherAnimationBeenPlayed");
            if (HasOpenedLostColosseumPortal)
                downed.Add("HasOpenedLostColosseumPortal");
            if (DownedBereftVassal)
                downed.Add("DownedBereftVassal");

            tag["downed"] = downed;
            tag["ProvidenceArenaX"] = ProvidenceArena.X;
            tag["ProvidenceArenaY"] = ProvidenceArena.Y;
            tag["ProvidenceArenaWidth"] = ProvidenceArena.Width;
            tag["ProvidenceArenaHeight"] = ProvidenceArena.Height;
            tag["ProvidenceDoorXPosition"] = ProvidenceDoorXPosition;
            tag["DreamgateLocationX"] = WayfinderGateLocation.X;
            tag["DreamgateLocationY"] = WayfinderGateLocation.Y;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            var downed = tag.GetList<string>("downed");
            InfernumMode = downed.Contains("InfernumModeActive");
            HasGeneratedProfanedShrine = downed.Contains("HasGeneratedProfanedShrine");
            HasGeneratedColosseumEntrance = downed.Contains("HasGeneratedColosseumEntrance");
            HasBeatedInfernumProvRegularly = downed.Contains("HasBeatedInfernumProvRegularly");
            HasBeatedInfernumNightProvBeforeDay = downed.Contains("HasBeatedInfernumNightProvBeforeDay");
            HasProvidenceDoorShattered = downed.Contains("HasProvidenceDoorShattered");
            HasSepulcherAnimationBeenPlayed = downed.Contains("HasSepulcherAnimationBeenPlayed");
            HasOpenedLostColosseumPortal = downed.Contains("HasOpenedLostColosseumPortal");
            DownedBereftVassal = downed.Contains("DownedBereftVassal");

            ProvidenceArena = new(tag.GetInt("ProvidenceArenaX"), tag.GetInt("ProvidenceArenaY"), tag.GetInt("ProvidenceArenaWidth"), tag.GetInt("ProvidenceArenaHeight"));
            ProvidenceDoorXPosition = tag.GetInt("ProvidenceDoorXPosition");
            WayfinderGateLocation = new(tag.GetFloat("DreamgateLocationX"), tag.GetFloat("DreamgateLocationY"));
        }

        public override void OnWorldLoad()
        {
            InfernumMode = false;
            HasGeneratedProfanedShrine = false;
            HasGeneratedColosseumEntrance = false;
            HasBeatedInfernumProvRegularly = false;
            HasBeatedInfernumNightProvBeforeDay = false;
            HasProvidenceDoorShattered = false;
            HasSepulcherAnimationBeenPlayed = false;
            HasOpenedLostColosseumPortal = false;
            DownedBereftVassal = false;

            ProvidenceArena = Rectangle.Empty;
            ProvidenceDoorXPosition = 0;
            WayfinderGateLocation = Vector2.Zero;
        }
    }
}