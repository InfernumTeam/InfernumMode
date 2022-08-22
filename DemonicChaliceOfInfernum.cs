using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode
{
    [LegacyName("Death2")]
    public class DemonicChaliceOfInfernum : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Demonic Chalice of Infernum");
            Tooltip.SetDefault("You will see this again");
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(6, 8));
        }

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Red;
            Item.width = 50;
            Item.height = 96;
            Item.maxStack = 1;
            Item.consumable = false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips) => tooltips.FirstOrDefault(x => x.Name == "Tooltip0" && x.Mod == "Terraria").OverrideColor = Color.DarkRed;
    }
}
