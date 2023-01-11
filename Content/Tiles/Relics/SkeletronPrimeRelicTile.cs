using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class SkeletronPrimeRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<SkeletronPrimeRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/SkeletronPrimeRelicTile";
    }
}
