using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Placeables.Ores;
using InfernumMode.Content.Items.Misc;
using InfernumMode.Content.Rarities.InfernumRarities;
using InfernumMode.Core.GlobalInstances.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Accessories
{
    public class Purity : ModItem
    {
        public const string FieldName = "Purity";

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;

            InfernumPlayer.ResetEffectsEvent += (InfernumPlayer player) => player.SetValue<bool>(FieldName, false);

            InfernumPlayer.AccessoryUpdateEvent += (InfernumPlayer player) =>
            {
                if (player.GetValue<bool>(FieldName))
                {
                    player.Player.GetDamage<GenericDamageClass>() += 0.3f;
                    player.Player.GetAttackSpeed<GenericDamageClass>() += 0.3f;
                    player.Player.buffImmune[ModContent.BuffType<Nightwither>()] = true;
                }
            };

            InfernumPlayer.ModifyHitNPCWithItemEvent += (InfernumPlayer player, Item item, NPC target, ref NPC.HitModifiers modifiers) =>
            {
                if (player.GetValue<bool>(FieldName))
                    modifiers.DisableCrit();
            };

            InfernumPlayer.ModifyHitNPCWithProjEvent += (InfernumPlayer player, Projectile proj, NPC target, ref NPC.HitModifiers modifiers) =>
            {
                if (player.GetValue<bool>(FieldName))
                    modifiers.DisableCrit();
            };
        }

        public override void SetDefaults()
        {
            Item.accessory = true;
            Item.width = 60;
            Item.height = 56;
            Item.rare = ModContent.RarityType<InfernumPurityRarity>();
            Item.value = CalamityGlobalItem.RarityTurquoiseBuyPrice;
            Item.Infernum_Tooltips().DeveloperItem = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual) => player.Infernum().SetValue<bool>("Purity", true);

        public override void AddRecipes()
        {
            CreateRecipe().
                AddTile(TileID.LunarCraftingStation).
                AddIngredient(ModContent.ItemType<LunarCoin>()).
                AddIngredient(ModContent.ItemType<ExodiumCluster>(), 25).
                AddIngredient(ModContent.ItemType<CoreofEleum>(), 10).
                Register();
        }
    }
}
