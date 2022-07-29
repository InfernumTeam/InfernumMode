using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class BrimstoneElementalRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<BrimstoneElementalRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/BrimstoneElementalRelicTile";
    }
}
