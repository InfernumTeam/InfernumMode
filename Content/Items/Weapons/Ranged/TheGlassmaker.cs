using CalamityMod.Items;
using InfernumMode.Content.Projectiles.Ranged;
using InfernumMode.Content.Rarities.InfernumRarities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Weapons.Ranged
{
    public class TheGlassmaker : ModItem
    {
#pragma warning disable CS0649
        internal static bool TransformsSandIntoGlass;
#pragma warning restore CS0649

        public override void SetDefaults()
        {
            Item.damage = 99;
            Item.knockBack = 1f;
            Item.DamageType = DamageClass.Ranged;
            Item.autoReuse = true;
            Item.useTime = 4;
            Item.useAnimation = 4;
            Item.useAmmo = AmmoID.Gel;
            Item.shootSpeed = 5.6f;
            Item.channel = true;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<GlassmakerHoldout>();

            Item.width = 64;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.value = CalamityGlobalItem.Rarity9BuyPrice;
            Item.rare = ModContent.RarityType<InfernumVassalRarity>();
        }

        public override Vector2? HoldoutOffset() => -Vector2.UnitX * 4f;

        public override bool CanConsumeAmmo(Item ammo, Player player) => Main.rand.NextFloat() >= 0.9f;

        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;
    }
}
