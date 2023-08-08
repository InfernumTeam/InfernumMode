using InfernumMode.Content.Projectiles.Generic;
using InfernumMode.Content.Rarities.InfernumRarities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Misc
{
    public class BrimstoneCrescentStaff : ModItem
    {
        public static int SpinTime => 54;

        public static int RaiseUpwardsTime => 27;

        public static int DebuffTime => 15;

        public static int ExplosionBaseDamage => 600;

        public static int MaxForcefieldHits => 3;

        public static int ForcefieldCreationDelayAfterBreak => 15;

        public static float ForcefieldDRMultiplier => 0.66f;

        public static float DamageMultiplier => 0.33f;

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
            Item.autoReuse = false;
            Item.shoot = ModContent.ProjectileType<BrimstoneCrescentStaffProj>();

            Item.value = ItemRarityID.LightPurple;
            Item.rare = ModContent.RarityType<InfernumScarletSparkleRarity>();
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
