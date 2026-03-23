using CalamityMod;
using CalamityMod.Cooldowns;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InfernumMode.Content.Buffs
{
    public class WeaknessHex : ModBuff
    {
        public class WeaknessHexCooldown : CooldownHandler
        {
            public static new string ID => "WeaknessHex";
            public override LocalizedText DisplayName => ModContent.GetInstance<WeaknessHex>().Description;
            public override string Texture => ModContent.GetInstance<WeaknessHex>().Texture;
            public override string OutlineTexture => "CalamityMod/Cooldowns/KillModeOutline";
            public override string OverlayTexture => "CalamityMod/Cooldowns/KillModeOverlay";
            public override bool CanTickDown => false;
            public override bool SavedWithPlayer => false;
        }
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Hex of Weakness");
            // Description.SetDefault("Your defense and damage reduction is significantly weakened");
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
