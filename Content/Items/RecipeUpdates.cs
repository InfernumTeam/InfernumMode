using CalamityMod.Items.Materials;
using CalamityMod.Items.SummonItems;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items
{
    public static class RecipeUpdates
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

        internal static void Update()
        {
            IncreaseBossSummonerYields();

            // Make the sandstorm's core post-Cultist.
            AddRecipeIngredient(ModContent.ItemType<SandstormsCore>(), ItemID.FragmentSolar);
        }

        internal static void IncreaseBossSummonerYields()
        {
            // Remove ingredients from boss summoners that require the boss be defeated.
            RemoveRecipeIngredient(ModContent.ItemType<Teratoma>(), ModContent.ItemType<RottenMatter>());
            RemoveRecipeIngredient(ModContent.ItemType<BloodyWormFood>(), ModContent.ItemType<BloodSample>());
        }
    }
}
