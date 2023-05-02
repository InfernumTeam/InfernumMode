using CalamityMod.Items;
using InfernumMode.Content.Items.Accessories;
using InfernumMode.Content.Projectiles.Generic;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items
{
    public class SakuraBud : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sakura Bud");
            Tooltip.SetDefault("You feel a guiding spirit trying to lead you the bloom's home, within the heart of the jungle\n" +
                "If you find it, toss it in the pond");
            SacrificeTotal = 1;
        }
        public override void SetDefaults()
        {
            Item.width = Item.height = 14;
            Item.value = CalamityGlobalItem.Rarity1BuyPrice;
            Item.rare = ItemRarityID.Gray;
            Item.Infernum_Tooltips().DeveloperItem = true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            TooltipLine nameLine = tooltips.FirstOrDefault(x => x.Name == "ItemName" && x.Mod == "Terraria");

            if (nameLine != null)
                nameLine.OverrideColor = Color.Pink;
        }

        public override void Update(ref float gravity, ref float maxFallSpeed)
        {
            bool inSpecialGarden = Item.position.Distance(WorldSaveSystem.BlossomGardenCenter.ToWorldCoordinates()) <= 3200f && WorldSaveSystem.BlossomGardenCenter != Point.Zero;
            if (inSpecialGarden && Collision.WetCollision(Item.TopLeft, Item.width, Item.height))
            {
                // Create bubbles and magic.
                int numDust = 50;
                for (int i = 0; i < numDust; i++)
                {
                    Vector2 ringVelocity = (MathHelper.TwoPi * i / numDust).ToRotationVector2().RotatedBy(Item.velocity.ToRotation() + MathHelper.PiOver2) * 5f;
                    Dust ringDust = Dust.NewDustPerfect(Item.position, 211, ringVelocity, 100, default, 1.25f);
                    ringDust.noGravity = true;
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Gore bubble = Gore.NewGorePerfect(Item.GetSource_FromThis(), Item.position + Main.rand.NextVector2Circular(120f, 120f), -Vector2.UnitY * 0.4f + Main.rand.NextVector2Circular(1f, 1f) * 0.75f, 411);
                        bubble.timeLeft = Main.rand.Next(8, 14);
                        bubble.scale = Main.rand.NextFloat(0.6f, 1f) * 1.2f;
                        bubble.type = Main.rand.NextBool(3) ? 412 : 411;
                    }

                    for (int i = 0; i < 8; i++)
                        Utilities.NewProjectileBetter(Item.position + Main.rand.NextVector2Circular(10f, 10f), Main.rand.NextVector2Circular(1.6f, 2f) - Vector2.UnitY * 6f, ModContent.ProjectileType<CherryBlossomPetal>(), 0, 0f, Main.myPlayer);
                }

                SoundEngine.PlaySound(SoundID.Item165, Item.position);
                int oldStack = Item.stack;
                Item.SetDefaults(ModContent.ItemType<SakuraBloom>());
                Item.stack = oldStack;
            }
        }
    }
}
