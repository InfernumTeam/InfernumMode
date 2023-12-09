using InfernumMode.Content.Rarities.InfernumRarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Misc
{
    // Dedicated to: PurpleMattik
    public class DisenchantedTablet : ModItem
    {
        public override void SetDefaults()
        {
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.width = 36;
            Item.height = 36;
            Item.value = 0;
            Item.rare = ModContent.RarityType<InfernumPurpleBackglowRarity>();
            Item.Infernum_Tooltips().DeveloperItem = true;
        }
    }
}
