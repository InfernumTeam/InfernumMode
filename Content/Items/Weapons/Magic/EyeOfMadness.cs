using CalamityMod.Events;
using CalamityMod.Items;
using CalamityMod.Rarities;
using InfernumMode.Content.Projectiles.Magic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Weapons.Magic
{
    public class EyeOfMadness : ModItem
    {
        public const float TargetingDistance = 1400f;

        public override void SetStaticDefaults()
        {
            // This thing canonically has the Mrrp Lore Rework contained within it! Unfortunately, the player is too stupid to unlock it.
            //string tooltip = "Releases barrages of shadow tendrils that ensnare enemies within their range\n" +
            //    "A sacred artifact once used by the Eidolists. Somehow, it contains within it knowledge of aeons past\n" +
            //    "Unfortunately, the full extent of its contents are beyond your understanding, but it would seem that you have an affinity for its\n" +
            //    "arcane powers, even if usage of them costs a little bit of your sanity";
            // DisplayName.SetDefault("Eye of Madness");
            // Tooltip.SetDefault(tooltip);
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.damage = 448;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 7;
            Item.width = 38;
            Item.height = 40;
            Item.useTime = Item.useAnimation = 19;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.UseSound = BossRushEvent.BossSummonSound;
            Item.knockBack = 0f;

            Item.value = CalamityGlobalItem.RarityVioletBuyPrice;
            Item.rare = ModContent.RarityType<BurnishedAuric>();

            Item.autoReuse = true;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<EyeOfMadnessProj>();
            Item.channel = true;
            Item.shootSpeed = 0f;
        }

        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

        public override bool PreDrawTooltipLine(DrawableTooltipLine line, ref int yOffset)
        {
            if (line.Name is "Tooltip1" or "Tooltip2" or "Tooltip3")
            {
                IllusionersReverie.DrawLine(line, Vector2.UnitY * yOffset);
                return false;
            }
            return true;
        }
    }
}
