using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Buffs
{
    public class RedSurge : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Red Surge");
            Description.SetDefault("You cannot move");
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.Infernum().RedElectrified = true;
        }
    }
}
