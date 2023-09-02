using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalamityMod;
using InfernumMode.Content.Items.Weapons.Rogue;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.GlobalItems
{
    public class LocketBanlistGlobaltem : GlobalItem
    {
        public override void SetStaticDefaults()
        {
            CalamityLists.VeneratedLocketBanlist.AddRange(new List<int>
            {
                ModContent.ItemType<Dreamtastic>(),
                ModContent.ItemType<StormMaidensRetribution>()
            });
        }
    }
}
