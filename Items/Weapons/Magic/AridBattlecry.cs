using CalamityMod.Items;
using InfernumMode.Projectiles.Magic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Items.Weapons.Magic
{
    public class AridBattlecry : ModItem
    {
        public const int SharkSummonRate = 16;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Arid Battlecry");
            Tooltip.SetDefault("Summons sharks below the cursor that fly towards enemies");
            SacrificeTotal = 1;
        }

        public override void SetDefaults()
        {
            Item.damage = 135;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 4;
            Item.width = 36;
            Item.height = 30;
            Item.useTime = Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 0f;

            Item.value = CalamityGlobalItem.Rarity9BuyPrice;
            Item.rare = ItemRarityID.Cyan;

            Item.autoReuse = true;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<AridBattlecryProjectile>();
            Item.channel = true;
            Item.shootSpeed = 0f;
        }
        
        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;
    }
}
