using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class DragonfollyRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<DragonfollyRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/DragonfollyRelicTile";
    }
}
