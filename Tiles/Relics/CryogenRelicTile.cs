using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class CryogenRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<CryogenRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/CryogenRelicTile";
    }
}
