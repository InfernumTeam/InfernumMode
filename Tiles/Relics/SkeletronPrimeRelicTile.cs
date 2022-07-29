using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class SkeletronPrimeRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<SkeletronPrimeRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/SkeletronPrimeRelicTile";
    }
}
