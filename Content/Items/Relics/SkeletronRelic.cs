using InfernumMode.Content.Tiles.Relics;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class SkeletronRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Skeletron Relic";

        public override string PersonalMessage => Utilities.GetLocalization("Items.SkeletronRelic.PersonalMessage").Value;

        public override Color? PersonalMessageColor => Color.Violet;

        public override int TileID => ModContent.TileType<SkeletronRelicTile>();
    }
}
