using CalamityMod.Items;
using CalamityMod.Items.Materials;
using InfernumMode.Common.DataStructures;
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
        public const int EggShieldMaxCooldown = 1200;

        public const int MaxEggShieldHits = 3;

        public const string FlavorText = "[c/f0ad56:This weapon is to be wielded by only those who shall take upon the task of watching over the weak ones]";

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;

            InfernumPlayer.PreUpdateEvent += (InfernumPlayer player) =>
            {
                Referenced<int> eggShieldCooldown = player.GetRefValue<int>("EggShieldCooldown");
                Referenced<float> eggShieldOpacity = player.GetRefValue<float>("EggShieldOpacity");
                bool eggShieldActive = player.GetValue<bool>("EggShieldActive");

                if (eggShieldCooldown.Value > 0)
                    eggShieldCooldown.Value--;

                // Deactivate the shield if half the cooldown has been reached.
                if (eggShieldActive && eggShieldCooldown.Value == EggShieldMaxCooldown / 2)
                    ToggleEggShield(player, false);

                // If the max hits have been taken, disable the shield and reset the current hits taken.
                if (player.GetValue<int>("CurrentEggShieldHits") >= MaxEggShieldHits)
                    ToggleEggShield(player, false);

                // Sort out the opacity.
                if (eggShieldActive)
                    eggShieldOpacity.Value = Clamp(eggShieldOpacity.Value + 0.1f, 0f, 1f);
                else
                    eggShieldOpacity.Value = Clamp(eggShieldOpacity.Value - 0.1f, 0f, 1f);
            };

            InfernumPlayer.AccessoryUpdateEvent += (InfernumPlayer player) =>
            {
                if (player.GetValue<bool>("EggShieldActive"))
                {
                    player.Player.statDefense += 100;
                    player.Player.GetDamage<GenericDamageClass>() *= 0.5f;
                }
            };

            InfernumPlayer.ModifyHurtEvent += (InfernumPlayer player, ref Player.HurtModifiers modifiers) =>
            {
                Referenced<int> currentEggShieldHits = player.GetRefValue<int>("CurrentEggShieldHits");

                if (player.GetValue<bool>("EggShieldActive"))
                    currentEggShieldHits.Value++;
            };
        }

        public override void SetDefaults()
        {
            Item.damage = 50;
            Item.DamageType = DamageClass.Melee;
            Item.width = Item.height = 108;
            Item.shoot = ProjectileID.Bone;
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

                InfernumPlayer eggPlayer = player.Infernum();
                if (eggPlayer.GetValue<bool>("EggShieldActive") || eggPlayer.GetValue<int>("EggShieldCooldown") > 0)
                    return false;

                ToggleEggShield(eggPlayer, true);
            }
            return null;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Cutscenes.CutsceneManager.QueueCutscene(ModContent.GetInstance<Cutscenes.DraedonPostMechsCutscene>());
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

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Starfury)
                .AddIngredient(ModContent.ItemType<LivingShard>(), 10)
                .AddIngredient(ModContent.ItemType<LifeAlloy>(), 5)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }

        public static void ToggleEggShield(InfernumPlayer player, bool status)
        {
            player.SetValue<bool>("EggShieldActive", status);
            player.SetValue<int>("CurrentEggShieldHits",  0);

            if (status)
                player.SetValue<int>("EggShieldCooldown", EggShieldMaxCooldown);
        }
    }
}
