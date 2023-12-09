using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.DataStructures;
using InfernumMode.Content.Cooldowns;
using InfernumMode.Content.Dusts;
using InfernumMode.Content.Projectiles.Melee;
using InfernumMode.Content.Rarities.InfernumRarities;
using InfernumMode.Core.GlobalInstances.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Weapons.Melee
{
    // Dedicated to: Jareto15
    public class CallUponTheEggs : ModItem
    {
        public const int EggShieldCooldown = 1200;

        public const int MaxEggShieldHits = 3;

        public const string FlavorText = "[c/f0ad56:This weapon is to be wielded by only those who shall take upon the task of watching over the weak ones]";

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;

            InfernumPlayer.PreUpdateEvent += (InfernumPlayer player) =>
            {
                Referenced<float> eggShieldOpacity = player.GetRefValue<float>("EggShieldOpacity");
                bool eggShieldActive = player.GetValue<bool>("EggShieldActive");

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
                {
                    SoundStyle crackSound = currentEggShieldHits.Value switch
                    {
                        0 => InfernumSoundRegistry.EggCrack1,
                        1 => InfernumSoundRegistry.EggCrack2,
                        _ => InfernumSoundRegistry.EggCrack3,
                    };
                    
                    SoundEngine.PlaySound(crackSound with {PitchVariance = 0.2f}, player.Player.Center);
                    currentEggShieldHits.Value++;

                    for (int i = 0; i < 20; i++)
                    {
                        Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 5f);
                        Dust.NewDust(Main.rand.NextVector2FromRectangle(player.Player.Hitbox), 4, 4, ModContent.DustType<EggDust>(), velocity.X, velocity.Y * 0.3f, Scale: 2f);
                    }
                }
            };

            InfernumPlayer.PostUpdateEvent += (InfernumPlayer player) =>
            {
                Referenced<bool> shieldActive = player.GetRefValue<bool>("EggShieldActive");
                if (shieldActive.Value && (player.Player.ActiveItem() == null || player.Player.ActiveItem().type != Type))
                {
                    ToggleEggShield(player, false);
                    SoundEngine.PlaySound(InfernumSoundRegistry.EggCrack3 with { PitchVariance = 0.2f }, player.Player.Center); 
                }
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

        public override bool CanUseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                if (player.altFunctionUse == 0)
                    return true;

                InfernumPlayer eggPlayer = player.Infernum();
                if (eggPlayer.GetValue<bool>("EggShieldActive") || player.HasCooldown(EggShieldRecharge.ID))
                    return false;

                ToggleEggShield(eggPlayer, true);

                return false;
            }
            return true;
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

            if (!status)
            {
                if (Main.myPlayer == player.Player.whoAmI)
                {
                    for (int i = 1; i < 4; i++)
                        Gore.NewGore(player.Player.GetSource_FromThis(), player.Player.Center, player.Player.velocity, InfernumMode.Instance.Find<ModGore>("EggGore" + i).Type);
                }
                player.Player.AddCooldown(EggShieldRecharge.ID, EggShieldCooldown);
            }
        }
    }
}
