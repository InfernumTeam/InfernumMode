using InfernumMode.Tiles.Relics;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Items.Relics
{
    public class YharonRelic : BaseRelicItem
	{
        public override string DisplayNameToUse => "Infernal Yharon Relic";

        public override string PersonalMessage => "Work in progress";

        public override Color? PersonalMessageColor => Color.Lerp(Color.Orange, Color.Yellow, ((float)Math.Cos(Main.GlobalTimeWrappedHourly * 1.7f) * 0.5f + 0.5f) * 0.6f);

        public override int TileID => ModContent.TileType<YharonRelicTile>();
	}
}
