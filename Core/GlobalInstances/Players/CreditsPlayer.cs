using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Core.GlobalInstances.Players
{
    public class CreditsPlayer : ModPlayer
    {
        public bool CreditsHavePlayed = false;

        public override void LoadData(TagCompound tag)
        {
            CreditsHavePlayed = tag.GetBool("CreditsHavePlayed");
        }

        public override void SaveData(TagCompound tag)
        {
            tag["CreditsHavePlayed"] = CreditsHavePlayed;
        }
    }
}
