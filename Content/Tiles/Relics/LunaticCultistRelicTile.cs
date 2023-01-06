using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class LunaticCultistRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<LunaticCultistRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/LunaticCultistRelicTile";
    }
}
