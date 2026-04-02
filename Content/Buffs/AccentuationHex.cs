using CalamityMod;
using CalamityMod.Cooldowns;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InfernumMode.Content.Buffs
{
    public class AccentuationHex : ModBuff
    {
        public class AccentuationHexCooldown : CooldownHandler
        {
            public static new string ID => "AccentuationHex";
            public override LocalizedText DisplayName => ModContent.GetInstance<AccentuationHex>().Description;
            public override string Texture => ModContent.GetInstance<AccentuationHex>().Texture;
            public override string OutlineTexture => "CalamityMod/Cooldowns/KillModeOutline";
            public override string OverlayTexture => "CalamityMod/Cooldowns/KillModeOverlay";
            public override bool CanTickDown => false;
            public override bool SavedWithPlayer => false;
        }
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Hex of Accentuation");
            // Description.SetDefault("Your opponent's magic attracts to you");
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
