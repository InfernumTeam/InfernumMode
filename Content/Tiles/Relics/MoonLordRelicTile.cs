using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class MoonLordRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<MoonLordRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/MoonLordRelicTile";
    }
}
