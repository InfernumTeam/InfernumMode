using CalamityMod;
using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Core.GlobalInstances.Players
{
    public class SkeletronSummonerGiftPlayer : ModPlayer
    {
        public bool WasGivenDungeonsCurse
        {
            get;
            set;
        }

        public override void SaveData(TagCompound tag)
        {
            var flagData = new List<string>();
            flagData.AddWithCondition("WasGivenDungeonsCurse", WasGivenDungeonsCurse);

            tag["FlagData"] = flagData;
        }

        public override void LoadData(TagCompound tag)
        {
            var flagData = tag.GetList<string>("FlagData");

            WasGivenDungeonsCurse = flagData.Contains("WasGivenDungeonsCurse");
        }
    }
}
