using CalamityMod;
using CalamityMod.Items;
using InfernumMode.Content.Projectiles.Melee;
using Microsoft.Xna.Framework;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Weapons.Melee
{
    public class Myrindael : ModItem
    {
        public const int LungeTime = 20;

        public const int SpinTime = 45;

        public const float LungeSpeed = 50f;

        public const float TargetHomeDistance = 720f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Myrindael");
            Tooltip.SetDefault("Holding left click spins the spear before throwing it. After being thrown, enemy hits release lightning and sparks towards enemies\n" +
                "Right clicks perform a powerful lunge that releases lightning from the sky on enemy hits\n" +
                "To fight, to destroy, is to take away from the world\n" +
                "And yet, when the world itself clashes against you, what else can you do?");
            SacrificeTotal = 1;
            ItemID.Sets.BonusAttackSpeedMultiplier[Item.type] = 0.33f;
            ItemID.Sets.Spears[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.damage = 105;
            Item.knockBack = 4.5f;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = Item.useTime = 32;
            Item.autoReuse = true;
            Item.channel = true;
            Item.shoot = ModContent.ProjectileType<MyrindaelBonkProjectile>();
            Item.shootSpeed = 12f;

            Item.value = CalamityGlobalItem.Rarity9BuyPrice;
            Item.rare = ItemRarityID.Cyan;

            Item.width = Item.height = 68;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.UseSound = SoundID.Item1;
            Item.noMelee = true;
            Item.useTurn = true;
            Item.noUseGraphic = true;
        }

        public override void HoldItem(Player player)
        {
            Item.channel = true;
            player.Calamity().rightClickListener = true;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanShoot(Player player)
        {
            int spinID = ModContent.ProjectileType<MyrindaelSpinProjectile>();
            if (player.ownedProjectileCounts[Item.shoot] > 0)
                return false;

            if (Main.projectile.Take(Main.maxProjectiles).Any(p => p.owner == player.whoAmI && p.active && p.type == spinID && p.ModProjectile<MyrindaelSpinProjectile>().SpinCompletion < 1f))
                return false;

            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse != 2)
            {
                Projectile.NewProjectile(source, position, velocity.SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<MyrindaelSpinProjectile>(), damage, knockback, player.whoAmI);
                return false;
            }

            Projectile.NewProjectile(source, position, velocity.SafeNormalize(Vector2.UnitY), type, damage, knockback, player.whoAmI);
            return false;
        }

        public override bool? CanHitNPC(Player player, NPC target) => false;

        public override bool CanHitPvp(Player player, Player target) => false;
    }
}
