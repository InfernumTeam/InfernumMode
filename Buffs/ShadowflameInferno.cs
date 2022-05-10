using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Buffs
{
    public class ShadowflameInferno : ModBuff
    {
        public override void SetDefaults()
        {
            DisplayName.SetDefault("Shadowflame Inferno");
            Description.SetDefault("Rapidly losing life");
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            longerExpertDebuff = false;
            canBeCleared = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.Infernum().ShadowflameInferno = true;
        }
    }
}
