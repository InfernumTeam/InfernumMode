using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Items.SummonItems
{
    public class FleshChunk : ModItem
    {
        public override void SetStaticDefaults()
        {
            SacrificeTotal = 1;
            DisplayName.SetDefault("Flesh Chunk");
            Tooltip.SetDefault("Summons the Wall of Flesh\n" +
                "Can only be used in the underworld\n" +
                "Not consumable");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.rare = ItemRarityID.Orange;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = false;
        }

        public override bool CanUseItem(Player player) => player.ZoneUnderworldHeight;

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.GuideVoodooDoll);
            recipe.AddIngredient(ModContent.ItemType<BloodOrb>(), 5);
            recipe.AddIngredient(ModContent.ItemType<DemonicBoneAsh>(), 5);
            recipe.AddIngredient(ItemID.ShadowScale, 10);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();

            recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.GuideVoodooDoll);
            recipe.AddIngredient(ModContent.ItemType<BloodOrb>(), 5);
            recipe.AddIngredient(ModContent.ItemType<DemonicBoneAsh>(), 5);
            recipe.AddIngredient(ItemID.TissueSample, 10);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }

        public override bool? UseItem(Player player)/* tModPorter Suggestion: Return null instead of false */
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
                NPC.SpawnWOF(player.Center);

            return true;
        }
    }
}
