using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class BrimstoneElementalRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<BrimstoneElementalRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/BrimstoneElementalRelicTile";
    }
}
