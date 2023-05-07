using InfernumMode.Content.Items.Relics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class AEWRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<AEWRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/AEWRelicTile";
    }
}
