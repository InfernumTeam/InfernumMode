using CalamityMod;
using CalamityMod.NPCs.Leviathan;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.SummonItems
{
    public class LeviathanDetector : ModItem
    {
        public override string Texture => base.Texture + "_Animated";
        public override void SetStaticDefaults()
        {
            ItemID.Sets.SortingPriorityBossSpawns[Type] = 14; // Frost Moon
            ItemID.Sets.AnimatesAsSoul[Type] = true;
            Main.RegisterItemAnimation(Type, new DrawAnimationVertical(5, 14));
        }

        public override void SetDefaults()
        {
            Item.width = 58;
            Item.height = 58;
            Item.rare = ItemRarityID.Lime;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = false;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Wire, 100);
            recipe.AddRecipeGroup("AnyCopperBar", 10);
            recipe.AddIngredient(ItemID.Glass, 15);
            recipe.AddIngredient(ItemID.SoulofLight, 4);
            recipe.AddIngredient(ItemID.SoulofNight, 4);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }

        public override bool CanUseItem(Player player) => !NPC.AnyNPCs(ModContent.NPCType<Anahita>()) && !NPC.AnyNPCs(ModContent.NPCType<Leviathan>()) && (player.Center.X < 9000f || player.Center.X > Main.maxTilesX * 16f - 9000f);

        public override bool? UseItem(Player player)/* tModPorter Suggestion: Return null instead of false */
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 spawnPosition = player.Center - Vector2.UnitY * 350f;
                NPC.SpawnBoss((int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<Anahita>(), player.whoAmI);
            }
            return true;
        }
    }
}
