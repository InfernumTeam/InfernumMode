using CalamityMod.Items;
using InfernumMode.Content.Projectiles.Magic;
using InfernumMode.Content.Rarities.InfernumRarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Weapons.Magic
{
    public class AridBattlecry : ModItem
    {
        public const int SharkSummonRate = 16;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Arid Battlecry");
            // Tooltip.SetDefault("Summons sharks below the cursor that fly towards enemies");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.damage = 93;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 13;
            Item.width = 36;
            Item.height = 30;
            Item.useTime = Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 0f;

            Item.value = CalamityGlobalItem.RarityCyanBuyPrice;
            Item.rare = ModContent.RarityType<InfernumVassalRarity>();

            Item.autoReuse = true;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<AridBattlecryProjectile>();
            Item.channel = true;
            Item.shootSpeed = 0f;
        }

        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;
    }
}
