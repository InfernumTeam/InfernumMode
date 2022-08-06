using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class SignusRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<SignusRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/SignusRelicTile";
    }
}
