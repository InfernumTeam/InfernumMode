using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class DraedonRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<DraedonRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/DraedonRelicTile";
    }
}
