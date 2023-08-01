using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Buffs
{
    public class WeaknessHex : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Hex of Weakness");
            // Description.SetDefault("Your defense and damage reduction is significantly weakened");
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }
    }
}
