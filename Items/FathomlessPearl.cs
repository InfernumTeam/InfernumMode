using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;
using CalamityMod.NPCs.SunkenSea;
using CalamityMod.Items.Placeables;
using CalamityMod.Items.Materials;
using CalamityMod.NPCs.Abyss;

namespace InfernumMode.Items
{
    public class FathomlessPearl : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fathomless Pearl");
            Tooltip.SetDefault("Summons the Adult Eidolon Wyrm\n" +
                "Not consumable");
        }

        public override void SetDefaults()
        {
            item.width = 32;
            item.height = 32;
            item.rare = ItemRarityID.Purple;
            item.Calamity().customRarity = CalamityRarity.Violet;
            item.useAnimation = 45;
            item.useTime = 45;
            item.useStyle = ItemUseStyleID.HoldingUp;
            item.consumable = false;
        }

        public override bool CanUseItem(Player player) => !NPC.AnyNPCs(ModContent.NPCType<EidolonWyrmHeadHuge>());

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<Voidstone>(), 20);
            recipe.AddIngredient(ModContent.ItemType<ShadowspecBar>(), 3);
            recipe.needWater = true;
            recipe.SetResult(this);
            recipe.AddRecipe();
        }

        public override bool UseItem(Player player)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 spawnPosition = player.Center - Vector2.UnitY * 950f;
                NPC.NewNPC((int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<EidolonWyrmHeadHuge>());
            }
            return true;
        }
    }
}
