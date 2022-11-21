using CalamityMod;
using CalamityMod.Items.Placeables.Furniture.DevPaintings;
using InfernumMode.BehaviorOverrides.BossAIs.GreatSandShark;
using InfernumMode.Items.Relics;
using InfernumMode.Items.Weapons.Magic;
using InfernumMode.Items.Weapons.Melee;
using InfernumMode.Items.Weapons.Ranged;
using InfernumMode.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Items
{
    public class BereftVassalBossBag : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Treasure Bag (Bereft Vassal)");
            Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
            SacrificeTotal = 3;
            ItemID.Sets.BossBag[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.maxStack = 999;
            Item.consumable = true;
            Item.width = 36;
            Item.height = 32;
            Item.expert = true;
            Item.rare = ItemRarityID.Cyan;
        }

        public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup)
        {
            itemGroup = ContentSamples.CreativeHelper.ItemGroup.BossBags;
        }

        public override bool CanRightClick()
        {
            return true;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return Color.Lerp(lightColor, Color.White, 0.4f);
        }
        public override void PostUpdate()
        {
            base.Item.TreasureBagLightAndDust();
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            return CalamityUtils.DrawTreasureBagInWorld(base.Item, spriteBatch, ref rotation, ref scale, whoAmI);
        }
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.CoinsBasedOnNPCValue(ModContent.NPCType<BereftVassal>()));
            itemLoot.Add(DropHelper.CalamityStyle(DropHelper.BagWeaponDropRateFraction, ModContent.ItemType<Myrindael>(), ModContent.ItemType<TheGlassmaker>(), ModContent.ItemType<AridBattlecry>()));
            itemLoot.AddRevBagAccessories();
            itemLoot.Add(ModContent.ItemType<ThankYouPainting>(), 100);
        }
    }
}
