using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class LunaticCultistRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<LunaticCultistRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/LunaticCultistRelicTile";
    }
}
