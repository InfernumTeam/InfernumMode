using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public abstract class BaseRelicItem : ModItem
    {
        public virtual string PersonalMessage => null;

        public virtual Color? PersonalMessageColor => null;

        public abstract string DisplayNameToUse { get; }

        public abstract int TileID { get; }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault(DisplayNameToUse);
            Tooltip.SetDefault("GetsChanged");
            SacrificeTotal = 1;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            TooltipLine obj = tooltips.FirstOrDefault((x) => x.Name == "Tooltip0" && x.Mod == "Terraria");
            obj.Text = PersonalMessage ?? Utilities.InfernalRelicText;
            obj.OverrideColor = PersonalMessageColor ?? Color.DarkRed;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileID, 0);
            Item.width = 30;
            Item.height = 44;
            Item.maxStack = 999;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.buyPrice(0, 5, 0, 0);
        }
    }
}
