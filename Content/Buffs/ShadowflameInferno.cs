using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Buffs
{
    public class ShadowflameInferno : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Shadowflame Inferno");
            // Description.SetDefault("Rapidly losing life");
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.Infernum_Debuff().ShadowflameInferno = true;
        }
    }
}
