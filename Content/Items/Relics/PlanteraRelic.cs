using InfernumMode.Content.Tiles.Relics;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class PlanteraRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Plantera Relic";

        public override string PersonalMessage => "Be proud of your death count!\n" +
            "The more you die, the more you're learning. Keep going!";

        public override Color? PersonalMessageColor => Color.Lerp(Color.Lime, Color.White, 0.5f);

        public override int TileID => ModContent.TileType<PlanteraRelicTile>();
    }
}
