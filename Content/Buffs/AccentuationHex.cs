using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Buffs
{
    public class AccentuationHex : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Hex of Accentuation");
            Description.SetDefault("Your opponent's magic attracts to you");
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }
    }
}
