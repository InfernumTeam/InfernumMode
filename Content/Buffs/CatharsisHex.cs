using CalamityMod;
using CalamityMod.Cooldowns;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InfernumMode.Content.Buffs
{
    public class CatharsisHex : ModBuff
    {
        public class CatharsisHexCooldown : CooldownHandler
        {
            public static new string ID => "CatharsisHex";
            public override LocalizedText DisplayName => ModContent.GetInstance<CatharsisHex>().Description;
            public override string Texture => ModContent.GetInstance<CatharsisHex>().Texture;
            public override string OutlineTexture => "CalamityMod/Cooldowns/KillModeOutline";
            public override string OverlayTexture => "CalamityMod/Cooldowns/KillModeOverlay";
            public override bool CanTickDown => false;
            public override bool SavedWithPlayer => false;
        }
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Hex of Catharsis");
            // Description.SetDefault("Natural life regeneration is disabled and angry spirits are released from within you");
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.AddCooldown(Name, 2);
        }
    }
}
