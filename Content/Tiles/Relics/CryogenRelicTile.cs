using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class CryogenRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<CryogenRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/CryogenRelicTile";
    }
}
