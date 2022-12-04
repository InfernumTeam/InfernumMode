using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Systems
{
    public class WorldSaveSystem : ModSystem
    {
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
        } = false;

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

        public override void SaveWorldData(TagCompound tag)
        {
            var downed = new List<string>();
            if (InfernumMode)
                downed.Add("InfernumModeActive");
            if (HasGeneratedProfanedShrine)
                downed.Add("HasGeneratedProfanedShrine");
            if (HasBeatedInfernumProvRegularly)
                downed.Add("HasBeatedInfernumProvRegularly");
            if (HasBeatedInfernumNightProvBeforeDay)
                downed.Add("HasBeatedInfernumNightProvBeforeDay");
            if (HasProvidenceDoorShattered)
                downed.Add("HasProvidenceDoorShattered");
            if (HasSepulcherAnimationBeenPlayed)
                downed.Add("HasSepulcherAnimationBeenPlayed");
            if (InPostAEWUpdateWorld)
                downed.Add("InPostAEWUpdateWorld");

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
        }

        public override void LoadWorldData(TagCompound tag)
        {
            var downed = tag.GetList<string>("downed");
            InfernumMode = downed.Contains("InfernumModeActive");
            HasGeneratedProfanedShrine = downed.Contains("HasGeneratedProfanedShrine");
            HasBeatedInfernumProvRegularly = downed.Contains("HasBeatedInfernumProvRegularly");
            HasBeatedInfernumNightProvBeforeDay = downed.Contains("HasBeatedInfernumNightProvBeforeDay");
            HasProvidenceDoorShattered = downed.Contains("HasProvidenceDoorShattered");
            HasSepulcherAnimationBeenPlayed = downed.Contains("HasSepulcherAnimationBeenPlayed");
            InPostAEWUpdateWorld = downed.Contains("InPostAEWUpdateWorld");

            ProvidenceArena = new(tag.GetInt("ProvidenceArenaX"), tag.GetInt("ProvidenceArenaY"), tag.GetInt("ProvidenceArenaWidth"), tag.GetInt("ProvidenceArenaHeight"));
            ProvidenceDoorXPosition = tag.GetInt("ProvidenceDoorXPosition");

            AbyssLayer1ForestSeed = tag.GetInt("AbyssLayer1ForestSeed");
            AbyssLayer3CavernSeed = tag.GetInt("AbyssLayer3CavernSeed");
            SquidDenCenter = new(tag.GetInt("SquidDenCenterX"), tag.GetInt("SquidDenCenterY"));
            EidolistWorshipPedestalCenter = new(tag.GetInt("EidolistWorshipPedestalCenterX"), tag.GetInt("EidolistWorshipPedestalCenterY"));

            WayfinderGateLocation = new(tag.GetFloat("DreamgateLocationX"), tag.GetFloat("DreamgateLocationY"));
        }

        public override void OnWorldLoad()
        {
            InfernumMode = false;
            HasGeneratedProfanedShrine = false;
            HasBeatedInfernumProvRegularly = false;
            HasBeatedInfernumNightProvBeforeDay = false;
            HasProvidenceDoorShattered = false;
            HasSepulcherAnimationBeenPlayed = false;
            ProvidenceArena = Rectangle.Empty;
            ProvidenceDoorXPosition = 0;
            WayfinderGateLocation = Vector2.Zero;
        }
    }
}