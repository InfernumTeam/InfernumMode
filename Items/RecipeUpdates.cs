using CalamityMod.Items.Materials;
using CalamityMod.Items.SummonItems;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Items
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

        public static void RemoveRecipeIngredient(int itemType, int ingredientType)
        {
            Main.recipe.Where(x => x.createItem.type == itemType).ToList().ForEach(s =>
            {
                for (int i = 0; i < s.requiredItem.Length; i++)
                {
                    if (s.requiredItem[i].type == ingredientType)
                        s.requiredItem[i] = new Item();
                }
            });
        }

        internal static void Update()
        {
            IncreaseBossSummonerYields();
        }

        internal static void IncreaseBossSummonerYields()
        {
            // Remove ingredients from boss summoners that require the boss be defeated.
            RemoveRecipeIngredient(ModContent.ItemType<Teratoma>(), ModContent.ItemType<TrueShadowScale>());
            RemoveRecipeIngredient(ModContent.ItemType<BloodyWormFood>(), ModContent.ItemType<BloodSample>());
        }
    }
}
