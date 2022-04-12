using CalamityMod;
using CalamityMod.Buffs.StatBuffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Buffs
{
    public class Anxiety : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Anxiety");
            Description.SetDefault("You cannot use Rage or Adrenaline");
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.Calamity().rage = 0f;
            player.Calamity().adrenaline = 0f;
            if (player.HasBuff(ModContent.BuffType<RageMode>()))
                player.ClearBuff(ModContent.BuffType<RageMode>());
            if (player.HasBuff(ModContent.BuffType<AdrenalineMode>()))
                player.ClearBuff(ModContent.BuffType<AdrenalineMode>());
        }
    }
}
