using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    [LegacyName("CalamitasCloneRelicTile")]
    public class ForgottenShadowOfCalamitasRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<ForgottenShadowOfCalamitasRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/ForgottenShadowOfCalamitasRelicTile";
    }
}
