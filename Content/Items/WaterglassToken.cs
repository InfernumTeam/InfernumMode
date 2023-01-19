using CalamityMod;
using InfernumMode.Content.Projectiles;
using InfernumMode.Content.Rarities.InfernumRarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items
{
    public class WaterglassToken : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Waterglass Token");
            Tooltip.SetDefault("Teleports you to the Lost Colosseum and back\n" +
                "This item cannot be used when being held by the mouse and must be in the hotbar");
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
            Item.rare = ModContent.RarityType<InfernumVassalRarity>();

            Item.autoReuse = true;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<WaterglassTokenProjectile>();
        }

        public override bool CanUseItem(Player player)
        {
            if (player.ownedProjectileCounts[Item.shoot] >= 1 || !InfernumMode.CanUseCustomAIs)
                return false;

            // Entering/exiting subworlds appears to reset the mouse item for some reason, meaning that if you use this item
            // that way it'll be functionally distroyed, which we don't want.
            if (!Main.mouseItem.IsAir && Main.mouseItem.type == Type)
                return false;

            return true;
        }
    }
}
