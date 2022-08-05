using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Systems
{
    public class WorldSaveSystem : ModSystem
    {
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

            tag["downed"] = downed;
            tag["ProvidenceArenaX"] = ProvidenceArena.X;
            tag["ProvidenceArenaY"] = ProvidenceArena.Y;
            tag["ProvidenceArenaWidth"] = ProvidenceArena.Width;
            tag["ProvidenceArenaHeight"] = ProvidenceArena.Height;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            var downed = tag.GetList<string>("downed");
            InfernumMode = downed.Contains("InfernumModeActive");
            HasGeneratedProfanedShrine = downed.Contains("HasGeneratedProfanedShrine");
            HasBeatedInfernumProvRegularly = downed.Contains("HasBeatedInfernumProvRegularly");
            HasBeatedInfernumNightProvBeforeDay = downed.Contains("HasBeatedInfernumNightProvBeforeDay");
            ProvidenceArena = new(tag.GetInt("ProvidenceArenaX"), tag.GetInt("ProvidenceArenaY"), tag.GetInt("ProvidenceArenaWidth"), tag.GetInt("ProvidenceArenaHeight"));
        }
    }
}