using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Pets
{
    public class DisenchantedTablet : ModItem
    {
        public override void SetStaticDefaults()
        {
            SacrificeTotal = 1;
            DisplayName.SetDefault("Disenchanted Tablet");
            Tooltip.SetDefault("Contains a complex network of intricately woven memories, locked away\n" +
                "Can be enchanted at a tablet altar, located somewhere on the surface");
        }

        public override void SetDefaults()
        {
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.width = 36;
            Item.height = 36;
            Item.value = 0;
            Item.rare = ItemRarityID.Pink;
        }
    }
}
