using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Buffs
{
    public class ShadowflameInferno : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shadowflame Inferno");
            Description.SetDefault("Rapidly losing life");
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.Infernum().ShadowflameInferno = true;
        }
    }
}
