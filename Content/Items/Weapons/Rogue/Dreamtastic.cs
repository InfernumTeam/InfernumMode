using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Items.Weapons.Rogue;
using InfernumMode.Content.Projectiles.Rogue;
using InfernumMode.Content.Rarities.InfernumRarities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Weapons.Rogue
{
    public class Dreamtastic : RogueWeapon
    {
        public const int BeamNoHomeTime = 20;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dreamtastic");
            Tooltip.SetDefault("Summons two dorks that fire energy bolts at enemies\n" +
                "The book also releases energy bolts of its own");
            SacrificeTotal = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 54;
            Item.height = 54;
            Item.damage = 5000;
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
        }

        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }
    }
}
