using CalamityMod.Items;
using InfernumMode.Content.Projectiles.Melee;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Weapons.Melee
{
    public class Jawbone : ModItem
    {
        public enum SwingType
        {
            Downward = -1,
            Upward = 1
        }

        private SwingType CurrentSwing;
        // TODO - Change this to the actual sprite.
        public override string Texture => "CalamityMod/Items/Weapons/Melee/Brimlash";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Jawbone");
            Tooltip.SetDefault("Press LMB to perform a combo of swings\n" +
                "Hold LMB to throw the weapon outwards and dash towards it\n" +
                "Hold RMB to launch a hook out that will stick to enemies and pull you in" +
                "HowAreYouHere");
            SacrificeTotal = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 72;
            Item.height = 72;
            Item.damage = 5000;
            Item.DamageType = DamageClass.MeleeNoSpeed;
            Item.knockBack = 6;
            Item.channel = true;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.useAnimation = Item.useTime = 32;
            Item.autoReuse = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.shoot = ModContent.ProjectileType<JawboneHoldout>();
            Item.shootSpeed = 12f;
            Item.useTime = Item.useAnimation = 22;
            Item.UseSound = SoundID.Item1;
            Item.value = CalamityGlobalItem.Rarity16BuyPrice;
            Item.Infernum_Tooltips().DeveloperItem = true;
            CurrentSwing = SwingType.Downward;
        }

        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[ModContent.ProjectileType<JawboneHoldout>()] <= 0;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, (float)CurrentSwing);

            //DEBUG, START CREDITS.
            Credits.CreditManager.BeginCredits();

            if ((float)CurrentSwing == 1f)
                CurrentSwing = SwingType.Downward;
            else
                CurrentSwing = SwingType.Upward;

            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            foreach (TooltipLine line in tooltips)
            {
                if (line.Text == null)
                    continue;

                if (line.Text.StartsWith("HowAreYouHere"))
                    line.OverrideColor = Color.DarkRed;
            }
        }
    }
}
