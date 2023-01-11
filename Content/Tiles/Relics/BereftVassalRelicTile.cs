using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class BereftVassalRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<BereftVassalRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/BereftVassalRelicTile";
    }
}
