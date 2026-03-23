using CalamityMod;
using CalamityMod.Cooldowns;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InfernumMode.Content.Buffs
{
    public class IndignationHex : ModBuff
    {
        public class IndignationHexCooldown : CooldownHandler
        {
            public static new string ID => "IndignationHex";
            public override LocalizedText DisplayName => ModContent.GetInstance<IndignationHex>().Description;
            public override string Texture => ModContent.GetInstance<IndignationHex>().Texture;
            public override string OutlineTexture => "CalamityMod/Cooldowns/KillModeOutline";
            public override string OverlayTexture => "CalamityMod/Cooldowns/KillModeOverlay";
            public override bool CanTickDown => false;
            public override bool SavedWithPlayer => false;
        }
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Hex of Indignation");
            // Description.SetDefault("You are haunted by a soul seeker");
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
