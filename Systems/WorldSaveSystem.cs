using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Systems
{
    public class WorldSaveSystem : ModSystem
    {
        public override void SaveWorldData(TagCompound tag)
        {
            var downed = new List<string>();
            if (PoDWorld.InfernumMode)
                downed.Add("InfernumModeActive");
            tag["downed"] = downed;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            var downed = tag.GetList<string>("downed");
            PoDWorld.InfernumMode = downed.Contains("fuckYouMode");
        }
    }
}