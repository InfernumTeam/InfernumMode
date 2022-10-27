using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class BereftVassalRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<BereftVassalRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/BereftVassalRelicTile";
    }
}
