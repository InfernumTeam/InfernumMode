using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Items
{
    public class FleshChunk : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Flesh Chunk");
            Tooltip.SetDefault("Summons the Wall of Flesh\n" +
                "Can only be used in the underworld");
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
            CreateRecipe(1).AddIngredient(ItemID.GuideVoodooDoll).AddIngredient(ModContent.ItemType<BloodOrb>(), 5).AddIngredient(ModContent.ItemType<DemonicBoneAsh>(), 5).AddIngredient(ItemID.ShadowScale, 10).Register();
            CreateRecipe(1).AddIngredient(ItemID.GuideVoodooDoll).AddIngredient(ModContent.ItemType<BloodOrb>(), 5).AddIngredient(ModContent.ItemType<DemonicBoneAsh>(), 5).AddIngredient(ItemID.TissueSample, 10).Register();
        }

        public override bool? UseItem(Player player)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
                NPC.SpawnWOF(player.Center);

            return true;
        }
    }
}
