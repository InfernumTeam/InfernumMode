using CalamityMod.Items.Weapons.Rogue;
using InfernumMode.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items
{
    public class BrimstoneCrescentStaff : RogueWeapon
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Brimstone Crescent Staff");
            Tooltip.SetDefault("todo");
            SacrificeTotal = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 120;
            Item.height = 114;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<BrimstoneCrescentStaffProj>();

            Item.value = ItemRarityID.LightPurple;
            //Item.rare = ModContent.RarityType<InfernumRedSparkRarity>();
            Item.Infernum_Tooltips().DeveloperItem = true;
        }

        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }
    }
}
