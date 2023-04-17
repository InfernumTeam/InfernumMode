using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Buffs
{
    public class BrimstoneExhaustion : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Brimstone Exhaustion");
            Description.SetDefault("You cannot resummon the Brimstone Crescent Staff's forcefield");
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = true;
        }
    }
}
