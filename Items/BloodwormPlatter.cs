using CalamityMod;
using CalamityMod.Items.SummonItems;
using CalamityMod.NPCs.OldDuke;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Items
{
    public class BloodwormPlatter : ModItem
    {
        public override void SetStaticDefaults()
        {
            SacrificeTotal = 1;
            DisplayName.SetDefault("Bloodworm Platter");
            Tooltip.SetDefault("Summons the Old Duke\n" +
                "Can only be used in the Sulphurous Sea\n" +
                "Not consumable");
        }

        public override void SetDefaults()
        {
            Item.width = 38;
            Item.height = 54;
            Item.rare = ItemRarityID.Red;
            Item.useAnimation = 40;
            Item.useTime = 40;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.consumable = false;
            Item.Calamity().customRarity = CalamityRarity.PureGreen;
        }

        public override bool CanUseItem(Player player) => !NPC.AnyNPCs(ModContent.NPCType<OldDuke>()) && player.Calamity().ZoneSulphur;

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<BloodwormItem>(), 3);
            recipe.AddIngredient(ItemID.FoodPlatter);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }

        public override bool? UseItem(Player player)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 spawnPosition = player.Center - Vector2.UnitY * 800f;
                NPC.NewNPC(player.GetSource_ItemUse(Item), (int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<OldDuke>());
            }
            return true;
        }
    }
}
