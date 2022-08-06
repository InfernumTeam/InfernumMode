using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class DevourerOfGodsRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<DevourerOfGodsRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/DevourerOfGodsRelicTile";
    }
}
