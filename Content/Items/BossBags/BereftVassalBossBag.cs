using CalamityMod;
using CalamityMod.Items.Placeables.Furniture.DevPaintings;
using InfernumMode.Content.BehaviorOverrides.BossAIs.GreatSandShark;
using InfernumMode.Content.Items.Accessories;
using InfernumMode.Content.Items.Misc;
using InfernumMode.Content.Items.Weapons.Magic;
using InfernumMode.Content.Items.Weapons.Melee;
using InfernumMode.Content.Items.Weapons.Ranged;
using InfernumMode.Content.Items.Weapons.Rogue;
using InfernumMode.Content.Items.Weapons.Summoner;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.BossBags
{
    public class BereftVassalBossBag : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 3;
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

        public override void PostUpdate() => Item.TreasureBagLightAndDust();

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            return CalamityUtils.DrawTreasureBagInWorld(Item, spriteBatch, ref rotation, ref scale, whoAmI);
        }

        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ModContent.ItemType<CherishedSealocket>());
            itemLoot.Add(ModContent.ItemType<WaterglassToken>());

            itemLoot.Add(ItemDropRule.CoinsBasedOnNPCValue(ModContent.NPCType<BereftVassal>()));

            int[] weapons = new int[]
{
                ModContent.ItemType<AridBattlecry>(),
                ModContent.ItemType<Myrindael>(),
                ModContent.ItemType<TheGlassmaker>(),
                ModContent.ItemType<WanderersShell>(),
                ModContent.ItemType<Perditus>()
            };

            itemLoot.Add(DropHelper.CalamityStyle(new Fraction(1, 2), weapons));

            //itemLoot.Add(ModContent.ItemType<Myrindael>(), new Fraction(1, 2));
            //itemLoot.Add(ModContent.ItemType<TheGlassmaker>(), new Fraction(1, 2));
            //itemLoot.Add(ModContent.ItemType<AridBattlecry>(), new Fraction(1, 2));
            //itemLoot.Add(ModContent.ItemType<WanderersShell>(), new Fraction(1, 2));
            //itemLoot.Add(ModContent.ItemType<Perditus>(), new Fraction(1, 2));

            itemLoot.AddRevBagAccessories();

            itemLoot.Add(ModContent.ItemType<ThankYouPainting>(), ThankYouPainting.DropInt);
        }
    }
}
