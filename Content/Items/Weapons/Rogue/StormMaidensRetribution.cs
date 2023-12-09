using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Items.Weapons.Rogue;
using InfernumMode.Content.Projectiles.Rogue;
using InfernumMode.Content.Rarities.InfernumRarities;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Weapons.Rogue
{
    // Dedicated to: Arixanew
    public class StormMaidensRetribution : RogueWeapon
    {
        public const float StealthStrikeTargetingDistance = 2400f;

        public override void SetStaticDefaults()
        {
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(5, 8));
            ItemID.Sets.AnimatesAsSoul[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 144;
            Item.height = 144;
            Item.damage = 8456;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.useAnimation = 50;
            Item.useTime = 50;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 9f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<StormMaidensRetributionProj>();
            Item.shootSpeed = 56f;
            Item.DamageType = RogueDamageClass.Instance;

            Item.value = CalamityGlobalItem.RarityHotPinkBuyPrice;
            Item.rare = ModContent.RarityType<InfernumRedSparkRarity>();
            Item.Infernum_Tooltips().DeveloperItem = true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var poemTooltips = tooltips.Where(x => x.Name.Contains("Tooltip") && x.Mod == "Terraria");
            foreach (var tooltip in poemTooltips)
            {
                int tooltipLineIndex = (int)char.GetNumericValue(tooltip.Name.Last());
                if (tooltipLineIndex >= 2)
                    tooltip.OverrideColor = Color.Lerp(Color.OrangeRed, Color.HotPink, 0.75f);
            }
        }

        public override bool CanUseItem(Player player)
        {
            // Don't create more spears if one is being aimed.
            foreach (Projectile spear in Utilities.AllProjectilesByID(Item.shoot))
            {
                if (spear.owner != player.whoAmI || spear.ModProjectile<StormMaidensRetributionProj>().CurrentState != StormMaidensRetributionProj.BehaviorState.Aim)
                    continue;

                return false;
            }

            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(spear =>
            {
                spear.ModProjectile<StormMaidensRetributionProj>().CreatedByStealthStrike = player.Calamity().StealthStrikeAvailable();
            });
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            player.Calamity().ConsumeStealthByAttacking();
            return false;
        }
    }
}
