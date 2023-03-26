using CalamityMod.Items;
using InfernumMode.Content.Rarities.InfernumRarities;
using InfernumMode.Core.GlobalInstances.Players;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Accessories
{
    internal class FlowerOfTheOcean : ModItem
    {
        // TODO: Replace with actual sprite.
        public override string Texture => "InfernumMode/Content/Items/Accessories/FlowerOfTheOceanTempSprite";

        public override void SetStaticDefaults()
        {
            SacrificeTotal = 1;
            DisplayName.SetDefault("Flower of the Ocean");
            Tooltip.SetDefault($"Grants vastly increased visibility while underwater");
        }

        public override void SetDefaults()
        {
            Item.width = 36;
            Item.height = 36;
            Item.value = CalamityGlobalItem.Rarity9BuyPrice;
            Item.rare = ModContent.RarityType<InfernumOceanFlowerRarity>();
            Item.accessory = true;
            Item.Infernum_Tooltips().DeveloperItem = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            FlowerOceanPlayer modPlayer = player.GetModPlayer<FlowerOceanPlayer>();
            modPlayer.MechanicsActive = true;
            modPlayer.VisualsActive = !hideVisual;
        }

        public override void UpdateVanity(Player player) =>
            player.GetModPlayer<FlowerOceanPlayer>().VisualsActive = true;
    }
}
