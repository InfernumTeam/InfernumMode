using InfernumMode.Content.Tiles.Relics;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class SkeletronRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Skeletron Relic";

        public override string PersonalMessage => "The first major roadblock. You are better now than before you faced it. Did you have fun learning its patterns?";

        public override Color? PersonalMessageColor => Color.Violet;

        public override int TileID => ModContent.TileType<SkeletronRelicTile>();
    }
}
