using CalamityMod;
using CalamityMod.Items;
using InfernumMode.Content.Rarities.InfernumRarities;
using InfernumMode.Core.GlobalInstances.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Accessories
{
    // Dedicated to: LGL
    public class FlowerOfTheOcean : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;

            InfernumPlayer.ResetEffectsEvent += (InfernumPlayer player) =>
            {
                player.SetValue<bool>("FlowerOceanMechanicsActive", false);
                player.SetValue<bool>("FlowerOceanVisualsActive", false);
            };

            InfernumPlayer.AccessoryUpdateEvent += (InfernumPlayer player) =>
            {
                // If underwater and not in the last zone of the abyss.
                if (player.Player.wet && !player.Player.Calamity().ZoneAbyssLayer4 && player.GetValue<bool>("FlowerOceanMechanicsActive"))
                    Lighting.AddLight((int)(player.Player.Center.X / 16f), (int)(player.Player.Center.Y / 16f), TorchID.White, 20f);
            };
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
            player.Infernum().SetValue<bool>("FlowerOceanMechanicsActive", true);
            player.Infernum().SetValue<bool>("FlowerOceanVisualsActive", !hideVisual);
        }

        public override void UpdateVanity(Player player) => player.Infernum().SetValue<bool>("FlowerOceanVisualsActive", true);
    }
}
