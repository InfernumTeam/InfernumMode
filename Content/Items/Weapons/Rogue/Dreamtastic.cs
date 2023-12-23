using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Placeables.FurnitureStratus;
using CalamityMod.Items.Weapons.Rogue;
using InfernumMode.Content.Projectiles.Rogue;
using InfernumMode.Content.Rarities.InfernumRarities;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Weapons.Rogue
{
    // Dedicated to: Piky
    public class Dreamtastic : RogueWeapon
    {
        public const int BeamNoHomeTime = 20;

        public override void SetDefaults()
        {
            Item.width = 54;
            Item.height = 54;
            Item.damage = 772;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.knockBack = 1f;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<DreamtasticProj>();
            Item.shootSpeed = 26f;
            Item.DamageType = RogueDamageClass.Instance;
            Item.rare = ModContent.RarityType<InfernumDreamtasticRarity>();
            Item.value = CalamityGlobalItem.RarityHotPinkBuyPrice;
            Item.Infernum_Tooltips().DeveloperItem = true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            foreach (TooltipLine line in tooltips.Where((x) => (x.Name == "Tooltip2" || x.Name == "Tooltip3") && x.Mod == "Terraria"))
                line.OverrideColor = CalamityUtils.ColorSwap(new(75, 38, 158), new(172, 64, 118), 1.5f);
        }

        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            player.Calamity().ConsumeStealthByAttacking();
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddTile(TileID.Bookcases).
                AddIngredient(ModContent.ItemType<StratusBookcase>()).
                AddIngredient(ModContent.ItemType<StratusBed>(), 2).
                AddIngredient(ModContent.ItemType<StratusChandelier>()).
                AddIngredient(ModContent.ItemType<StratusPiano>()).
                AddIngredient(ModContent.ItemType<NightsGaze>()).
                AddIngredient(ModContent.ItemType<AuricBar>(), 5).
                Register();
        }
    }
}
