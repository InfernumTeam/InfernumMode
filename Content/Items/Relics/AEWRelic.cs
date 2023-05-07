using InfernumMode.Content.Tiles.Relics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class AEWRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Adult Eidolon Wyrm Relic";

        public override int TileID => ModContent.TileType<AEWRelicTile>();
    }
}
