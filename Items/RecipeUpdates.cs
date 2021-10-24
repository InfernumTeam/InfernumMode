using CalamityMod.Items.Materials;
using CalamityMod.Items.SummonItems;
using System.Linq;
using Terraria;
using Terraria.ID;
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
            // Make slime crown's recipe give 10 summon items instead of 1.
            SetRecipeResultStack(ItemID.SlimeCrown, 10);

            // Make desert medallian's recipe give 5 summon items instead of 1.
            SetRecipeResultStack(ModContent.ItemType<DriedSeafood>(), 5);

            // Make suspicious looking eye's recipe give 3 summon items instead of 1.
            SetRecipeResultStack(ItemID.SuspiciousLookingEye, 10);

            // Make decapodita sprout's recipe give 5 summon items instead of 1.
            SetRecipeResultStack(ModContent.ItemType<DecapoditaSprout>(), 5);

            // Make worm food's recipe give 6 summon items instead of 1.
            SetRecipeResultStack(ItemID.WormFood, 6);

            // Make bloody spine's recipe give 6 summon items instead of 1.
            SetRecipeResultStack(ItemID.BloodySpine, 6);

            // Make teratoma's recipe give 6 summon items instead of 1.
            SetRecipeResultStack(ModContent.ItemType<Teratoma>(), 6);

            // Make bloody worm food's recipe give 6 summon items instead of 1.
            SetRecipeResultStack(ModContent.ItemType<BloodyWormFood>(), 6);

            // Make abeemination's recipe give 8 summon items instead of 1.
            SetRecipeResultStack(ItemID.Abeemination, 8);

            // Make overloaded sludge's recipe give 4 summon items instead of 1.
            SetRecipeResultStack(ModContent.ItemType<OverloadedSludge>(), 4);

            // Make cryo key's recipe give 6 summon items instead of 1.
            SetRecipeResultStack(ModContent.ItemType<CryoKey>(), 6);

            // Make charred idol's recipe give 6 summon items instead of 1.
            SetRecipeResultStack(ModContent.ItemType<CharredIdol>(), 6);

            // Make seafood's recipe give 4 summon items instead of 1.
            SetRecipeResultStack(ModContent.ItemType<Seafood>(), 4);

            // Make mechanical summoner's recipes give 8 summon items instead of 1.
            SetRecipeResultStack(ItemID.MechanicalEye, 8);
            SetRecipeResultStack(ItemID.MechanicalSkull, 8);
            SetRecipeResultStack(ItemID.MechanicalWorm, 8);

            // Remove ingredients from boss summoners that require the boss be defeated.
            RemoveRecipeIngredient(ModContent.ItemType<Teratoma>(), ModContent.ItemType<TrueShadowScale>());
            RemoveRecipeIngredient(ModContent.ItemType<BloodyWormFood>(), ModContent.ItemType<BloodSample>());
        }
    }
}
