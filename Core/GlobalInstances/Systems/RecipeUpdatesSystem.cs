using CalamityMod;
using CalamityMod.Items.Accessories;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Placeables;
using CalamityMod.Items.SummonItems;
using CalamityMod.Items.SummonItems.Invasion;
using CalamityMod.Items.Tools.ClimateChange;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Items.Weapons.Rogue;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items
{
    public class RecipeUpdatesSystem : ModSystem
    {
        public static void SetRecipeResultStack(int itemType, int newStack)
        {
            Main.recipe.Where(x => x.createItem.type == itemType).ToList().ForEach(s =>
            {
                s.createItem.stack = newStack;
            });
        }

        public static void AddRecipeIngredient(int itemType, int ingredientType, int ingredientStack = 1)
        {
            Main.recipe.Where(x => x.createItem.type == itemType).ToList().ForEach(s =>
            {
                s.AddIngredient(ingredientType, ingredientStack);
            });
        }

        public static void RemoveRecipeIngredient(int itemType, int ingredientType)
        {
            Main.recipe.Where(x => x.createItem.type == itemType).ToList().ForEach(s =>
            {
                s.RemoveIngredient(ingredientType);
            });
        }

        public override void AddRecipes()
        {
            IncreaseBossSummonerYields();

            // Make the sandstorm's core post-Cultist.
            AddRecipeIngredient(ModContent.ItemType<SandstormsCore>(), ItemID.FragmentSolar);

            // Create recipes for certain abyss items that are normally found in chests.

            // Anechoic Plating and the Depth Charm are interchangeable because they're both arbitrary metallic things and there's little reason to give a damn
            // about them beyond recipe bloat.
            Recipe.Create(ModContent.ItemType<AnechoicPlating>()).
                AddIngredient<SulphuricScale>(5).
                AddRecipeGroup(CalamityRecipes.AnySilverBar, 10).
                AddTile(TileID.Anvils).
                Register();

            Recipe.Create(ModContent.ItemType<DepthCharm>()).
                AddIngredient<SulphuricScale>(5).
                AddRecipeGroup(CalamityRecipes.AnySilverBar, 10).
                AddTile(TileID.Anvils).
                Register();

            Recipe.Create(ModContent.ItemType<TorrentialTear>()).
                AddIngredient<CausticTear>().
                AddIngredient<AbyssGravel>(10).
                AddCondition(Recipe.Condition.NearWater).
                Register();

            Recipe.Create(ModContent.ItemType<Archerfish>()).
                AddIngredient(ItemID.Bone, 15).
                AddIngredient(ItemID.Minishark).
                AddIngredient<SulphuricScale>(5).
                AddRecipeGroup(CalamityRecipes.AnyGoldBar, 10).
                AddTile(TileID.Anvils).
                Register();

            Recipe.Create(ModContent.ItemType<BallOFugu>()).
                AddIngredient(ItemID.Bone, 15).
                AddIngredient<UrchinStinger>(20).
                AddIngredient<SulphuricScale>(5).
                AddIngredient<AbyssGravel>(10).
                AddTile(TileID.Anvils).
                Register();
        }

        internal static void IncreaseBossSummonerYields()
        {
            // Remove ingredients from boss summoners that require the boss be defeated.
            RemoveRecipeIngredient(ModContent.ItemType<Teratoma>(), ModContent.ItemType<RottenMatter>());
            RemoveRecipeIngredient(ModContent.ItemType<BloodyWormFood>(), ModContent.ItemType<BloodSample>());
        }
    }
}
