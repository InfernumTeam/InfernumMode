using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Buffs
{
    public class DarkFlames : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Flames");
            Description.SetDefault("Your body is consumed by a forsaken inferno. Your defense is lowered");
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex) => player.Infernum_Debuff().DarkFlames = true;
    }
}
