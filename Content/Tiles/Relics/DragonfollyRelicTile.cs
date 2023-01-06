using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class DragonfollyRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<DragonfollyRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/DragonfollyRelicTile";
    }
}
