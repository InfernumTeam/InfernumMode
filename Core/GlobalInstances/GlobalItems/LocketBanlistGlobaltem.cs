using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Systems.Collections;
using InfernumMode.Content.Items.Weapons.Rogue;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.GlobalItems
{
    public class LocketBanlistGlobaltem : GlobalItem
    {
        public override void SetStaticDefaults()
        {
            CalamityItemSets.DisablesVeneratedLocketEffect[ModContent.ItemType<Dreamtastic>()] = true;
            CalamityItemSets.DisablesVeneratedLocketEffect[ModContent.ItemType<StormMaidensRetribution>()] = true;
        }
    }
}
