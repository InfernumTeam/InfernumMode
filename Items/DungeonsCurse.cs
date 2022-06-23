using CalamityMod;
using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Items
{
    public class DungeonsCurse : ModItem
    {
        public int frameCounter = 0;
        public int frame = 0;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dungeon's Curse");
            Tooltip.SetDefault("Summons Skeletron\n" +
                "Can only be used at night\n" +
                "Not consumable");
        }

        public override void SetDefaults()
        {
            Item.width = 38;
            Item.height = 54;
            Item.rare = ItemRarityID.Green;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.consumable = false;
        }

        public override bool CanUseItem(Player player) => !NPC.AnyNPCs(NPCID.SkeletronHead) && !Main.dayTime;

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<AncientBoneDust>(), 3);
            recipe.AddIngredient(ItemID.Vertebrae, 7);
            recipe.Register();

            recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<AncientBoneDust>(), 3);
            recipe.AddIngredient(ItemID.RottenChunk, 7);
            recipe.Register();
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frameI, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture = ModContent.GetTexture("InfernumMode/Items/DungeonsCurse_Animated");
            Rectangle f = Item.GetCurrentFrame(ref frame, ref frameCounter, 8, 5);
            spriteBatch.Draw(texture, position, f, Color.White, 0f, f.Size() * new Vector2(0.16f, 0.25f), scale, SpriteEffects.None, 0);
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D texture = ModContent.GetTexture("InfernumMode/Items/DungeonsCurse_Animated");
            spriteBatch.Draw(texture, Item.position - Main.screenPosition, Item.GetCurrentFrame(ref frame, ref frameCounter, 8, 5), lightColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0);
            return false;
        }

        public override bool? UseItem(Player player)/* tModPorter Suggestion: Return null instead of false */
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 spawnPosition = player.Center - Vector2.UnitY * 800f;
                NPC.NewNPC(npc.GetSource_FromAI(), (int)spawnPosition.X, (int)spawnPosition.Y, NPCID.SkeletronHead);
            }
            return true;
        }
    }
}
