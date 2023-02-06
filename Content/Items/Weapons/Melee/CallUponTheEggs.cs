using CalamityMod.Items;
using InfernumMode.Content.Projectiles.Melee;
using InfernumMode.Content.Rarities.InfernumRarities;
using InfernumMode.Core.GlobalInstances.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Weapons.Melee
{
    public class CallUponTheEggs : ModItem
    {
        public const string FlavorText = "[c/f0ad56:This weapon is to be wielded by only those who shall take upon the task of watching over the weak ones]";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Call Upon The Eggs");
            Tooltip.SetDefault(FlavorText);
        }

        public override void SetDefaults()
        {
            Item.damage = 50;
            Item.DamageType = DamageClass.Melee;
            Item.width = Item.height = 108;
            Item.shoot = 21;
            Item.shootSpeed = 23;
            Item.knockBack = 3;
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.rare = ModContent.RarityType<InfernumEggRarity>();
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = CalamityGlobalItem.RarityYellowBuyPrice;
            Item.Infernum_Tooltips().DeveloperItem = true;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                if (player.altFunctionUse == 0)
                    return null;

                EggPlayer eggPlayer = player.Infernum_Egg();
                if (eggPlayer.EggShieldActive || eggPlayer.EggShieldCooldown > 0)
                    return false;

                eggPlayer.ToggleEggShield(true);
            }
            return null;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 0)
            {
                int normalEggType = ModContent.ProjectileType<EggProjectile>();
                int goldEggType = ModContent.ProjectileType<EggGoldProjectile>();
                for (int i = 0; i < 5; i++)
                {
                    if (Main.rand.NextBool(50))
                        type = goldEggType;
                    else
                        type = normalEggType;

                    Vector2 center = new(player.Center.X + (player.Center.X - Main.MouseWorld.X) * -0.25f + Main.rand.NextFloat(-150f, 150f), player.Center.Y - 600);
                    Vector2 shootVelocity = center.DirectionTo(Main.MouseWorld).RotatedBy(Main.rand.NextFloat(-0.3f, 0.3f)) * Item.shootSpeed * Main.rand.NextFloat(0.9f, 1.1f);

                    Projectile.NewProjectile(source, center, shootVelocity, type, damage, knockback, player.whoAmI);
                }
            }
            return false;
        }
    }
}
