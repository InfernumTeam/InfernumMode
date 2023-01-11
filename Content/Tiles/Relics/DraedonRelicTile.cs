using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class DraedonRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<DraedonRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/DraedonRelicTile";
    }
}
