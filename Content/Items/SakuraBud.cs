using CalamityMod.Items;
using InfernumMode.Content.Items.Accessories;
using InfernumMode.Core.GlobalInstances.Systems;
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
            Tooltip.SetDefault("You feel a guiding spirit trying to lead you the bloom's home, within the heart of the jungle\n" +
                "If you find it, toss it in the pond");
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

        public override void Update(ref float gravity, ref float maxFallSpeed)
        {
            bool inSpecialGarden = Item.position.Distance(WorldSaveSystem.BlossomGardenCenter.ToWorldCoordinates()) <= 3200f && WorldSaveSystem.BlossomGardenCenter != Point.Zero;
            if (inSpecialGarden && Collision.WetCollision(Item.TopLeft, Item.width, Item.height))
            {
                int oldStack = Item.stack;
                Item.SetDefaults(ModContent.ItemType<SakuraBloom>());
                Item.stack = oldStack;
            }
        }
    }
}
