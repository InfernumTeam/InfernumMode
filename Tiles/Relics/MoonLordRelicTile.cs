using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class MoonLordRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<MoonLordRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/MoonLordRelicTile";
    }
}
