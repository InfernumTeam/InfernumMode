using CalamityMod.Items.LoreItems;
using Terraria;
using Terraria.ID;

namespace InfernumMode.Items
{
    public class KnowledgeGreatSandShark : LoreItem
    {
        public override void SetStaticDefaults()
        {
            SacrificeTotal = 1;
            DisplayName.SetDefault("The Great Sand Shark");
            Tooltip.SetDefault("Nature proves itself to be a formidable mistress.\n" +
                "Even in the most extreme of environmental circumstances it seems that there is no shortage of such displays of resilience.");
            ItemID.Sets.ItemNoGravity[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.rare = ItemRarityID.Yellow;
            Item.consumable = false;
        }

        public override bool CanUseItem(Player player) => false;
    }
}
