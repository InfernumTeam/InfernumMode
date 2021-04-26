using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Buffs
{
    public class RedSurge : ModBuff
    {
        public override void SetDefaults()
        {
            DisplayName.SetDefault("Red Surge");
            Description.SetDefault("You cannot move");
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            longerExpertDebuff = false;
            canBeCleared = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.Infernum().RedElectrified = true;
        }
    }
}
