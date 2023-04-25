using CalamityMod.Items;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items
{
    public class SakuraBud : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sakura Bud");
            Tooltip.SetDefault("You feel a guiding spirit trying to lead you the bloom’s home. Maybe you should follow its call?");
            SacrificeTotal = 1;
        }
        public override void SetDefaults()
        {
            Item.width = Item.height = 14;
            Item.value = CalamityGlobalItem.Rarity1BuyPrice;
            Item.rare = ItemRarityID.Gray;
            Item.Infernum_Tooltips().DeveloperItem = true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            TooltipLine nameLine = tooltips.FirstOrDefault(x => x.Name == "ItemName" && x.Mod == "Terraria");

            if (nameLine != null)
                nameLine.OverrideColor = Color.Pink;
        }
    }
}
