using InfernumMode.Content.Tiles.Relics;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class EmpressOfLightRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Empress of Light Relic";

        public override string PersonalMessage => Utilities.GetLocalization("Items.EmpressOfLightRelic.PersonalMessage").Value;

        public override Color? PersonalMessageColor => Color.DeepPink;

        public override int TileID => ModContent.TileType<EmpressOfLightRelicTile>();
    }
}
