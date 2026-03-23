using CalamityMod;
using CalamityMod.Cooldowns;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InfernumMode.Content.Buffs
{
    public class ZealHex : ModBuff
    {
        public class ZealHexCooldown : CooldownHandler
        {
            public static new string ID => "ZealHex";
            public override LocalizedText DisplayName => ModContent.GetInstance<ZealHex>().Description;
            public override string Texture => ModContent.GetInstance<ZealHex>().Texture;
            public override string OutlineTexture => "CalamityMod/Cooldowns/KillModeOutline";
            public override string OverlayTexture => "CalamityMod/Cooldowns/KillModeOverlay";
            public override bool CanTickDown => false;
            public override bool SavedWithPlayer => false;
        }
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Hex of Zeal");
            // Description.SetDefault("Your opponent's magic accelerates wildly");
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
