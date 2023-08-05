using InfernumMode.Content.Tiles.Relics;
using System;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    internal class NuclearTerrorRelic :  BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Nuclear Terror Relic";

        public override int TileID => ModContent.TileType<NuclearTerrorRelicTile>();
    }
}
