using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Buffs
{
    public class DarkFlames : ModBuff
    {
        public override void SetDefaults()
        {
            DisplayName.SetDefault("Dark Flames");
            Description.SetDefault("Your body is consumed by a forsaken inferno. Your defense is lowered");
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            longerExpertDebuff = false;
        }

        public override void Update(Player player, ref int buffIndex) => player.Infernum().DarkFlames = true;
    }
}
