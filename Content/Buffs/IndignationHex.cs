using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Buffs
{
    public class IndignationHex : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Hex of Indignation");
            // Description.SetDefault("You are haunted by a soul seeker");
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }
    }
}
