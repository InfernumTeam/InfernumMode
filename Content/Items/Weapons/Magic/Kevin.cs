using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Tiles.Furniture.CraftingStations;
using InfernumMode.Content.Projectiles.Magic;
using InfernumMode.Content.Rarities.InfernumRarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Weapons.Magic
{
    // Dedicated to: smhmyhead
    public class Kevin : ModItem
    {
        public const float TargetingDistance = 884f;

        public const int LightningArea = 1800;

        public override void SetDefaults()
        {
            Item.damage = 23000;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 4;
            Item.width = 36;
            Item.height = 30;
            Item.useTime = Item.useAnimation = 21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 0f;

            Item.value = CalamityGlobalItem.RarityHotPinkBuyPrice;
            Item.rare = ModContent.RarityType<InfernumCyanSparkRarity>();

            Item.autoReuse = true;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<KevinProjectile>();
            Item.channel = true;
            Item.shootSpeed = 0f;
            Item.Infernum_Tooltips().DeveloperItem = true;
        }

        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<AshesofAnnihilation>(3).
                AddIngredient<MiracleMatter>(3).
                AddIngredient<DubiousPlating>(150).
                AddIngredient<MysteriousCircuitry>(150).
                AddTile(ModContent.TileType<DraedonsForge>()).
                Register();
        }
    }
}
