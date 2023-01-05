using InfernumMode.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Items
{
    public class WaterglassToken : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Waterglass Token");
            Tooltip.SetDefault("Teleports you to the Lost Colosseum and back");
            SacrificeTotal = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 36;
            Item.height = 30;
            Item.useTime = Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;

            Item.value = 0;
            Item.rare = ItemRarityID.Cyan;

            Item.autoReuse = true;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<WaterglassTokenProjectile>();
        }
        
        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0 && InfernumMode.CanUseCustomAIs;
    }
}
