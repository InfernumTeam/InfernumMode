using CalamityMod;
using CalamityMod.Items;
using InfernumMode.Projectiles.Melee;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Items.Weapons.Melee
{
    public class Myrindael : ModItem
    {
        public const int LungeTime = 36;

        public const float LungeSpeed = 36f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Myrindael");
            Tooltip.SetDefault("Performs a powerful lunge that releases lightning from the sky on enemy hits\n" +
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
            Item.useAnimation = Item.useTime = 54;
            Item.autoReuse = false;
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

        public override bool CanShoot(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

        public override bool? CanHitNPC(Player player, NPC target) => false;

        public override bool CanHitPvp(Player player, Player target) => false;
    }
}
