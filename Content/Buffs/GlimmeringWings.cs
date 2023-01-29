using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Buffs
{
    public class GlimmeringWings : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Glimmering Wings");
            Description.SetDefault("You have infinite flight time");
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }
    }
}
