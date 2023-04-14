using InfernumMode.Content.Items;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Buffs
{
    public class BrimstoneBarrier : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Brimstone Barrier");
            Description.SetDefault("Your magic withers your weaponry at the cost of a strong magical barrier");
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetDamage<GenericDamageClass>() *= BrimstoneCrescentStaff.DamageMultiplier;
        }
    }
}
