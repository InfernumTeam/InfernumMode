using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class SignusRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<SignusRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/SignusRelicTile";
    }
}
