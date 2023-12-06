using CalamityMod.Items;
using InfernumMode.Content.Buffs;
using InfernumMode.Content.Projectiles.Summoner;
using InfernumMode.Content.Rarities.InfernumRarities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Weapons.Summoner
{
    public class Perditus : ModItem
    {
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(PerditusTagBuff.TagDamage, PerditusTagBuff.CritChance);

        // 23 whip tag, 10% crit.
        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.SummonMeleeSpeed;
            Item.damage = 180;
            Item.knockBack = 4;

            Item.shootSpeed = 12;

            Item.autoReuse = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.UseSound = SoundID.Item152;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<PerditusProjectile>();

            Item.rare = ModContent.RarityType<InfernumVassalRarity>();
            Item.value = CalamityGlobalItem.Rarity9BuyPrice;
        }

        public override bool MeleePrefix() => true;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
            => player.ownedProjectileCounts[type] < 1;
    }
}
