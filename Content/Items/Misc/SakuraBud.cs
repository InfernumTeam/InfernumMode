using CalamityMod.Items;
using InfernumMode.Content.Items.Accessories;
using InfernumMode.Content.Projectiles.Generic;
using InfernumMode.Content.Rarities.Sparkles;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Misc
{
    // Dedicated to: Nyastra (Joey)
    public class SakuraBud : ModItem
    {
        private readonly List<RaritySparkle> SpiritSparkles = new();
        private readonly List<RaritySparkle> WaterSparkles = new();

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
            {
                Vector2 currentPosition = Main.LocalPlayer.Center;
                Vector2 gardencenter = WorldSaveSystem.BlossomGardenCenter.ToWorldCoordinates();
                float distance = currentPosition.Distance(gardencenter);
                float lerpAmount = Utils.GetLerpValue(15000, 1000, distance, true);
                nameLine.OverrideColor = Color.Lerp(Color.Gray, Color.Pink, lerpAmount);
            }
        }

        public override bool PreDrawTooltipLine(DrawableTooltipLine line, ref int yOffset)
        { 
            SpiritSparkles.RemoveAll(s => s.Time >= s.Lifetime);
            WaterSparkles.RemoveAll(s => s.Time >= s.Lifetime);
            Vector2 drawOffset = Vector2.UnitY * yOffset;

            // Detecting the line like this is really bad. Not sure how else to do it though.
            if (line.Text.StartsWith("You feel"))
            {                
                drawOffset.X += SakuraBloom.DrawLine(line, drawOffset, SpiritSparkles, Utilities.GetLocalization("Items.SakuraBud.TooltipEffect.FirstText").Value);
                drawOffset.X += SakuraBloom.DrawLine(line, drawOffset, SpiritSparkles, Utilities.GetLocalization("Items.SakuraBud.TooltipEffect.SecondText").Value, true);
                drawOffset.X += SakuraBloom.DrawLine(line, drawOffset, SpiritSparkles, Utilities.GetLocalization("Items.SakuraBud.TooltipEffect.ThirdText").Value);
                return false;
            }

            if (line.Text.StartsWith("Maybe you"))
            {
                if (Main.LocalPlayer.WithinRange(WorldSaveSystem.BlossomGardenCenter.ToWorldCoordinates(), 3200f))
                {
                    drawOffset.X += SakuraBloom.DrawLine(line, drawOffset, WaterSparkles, Utilities.GetLocalization("Items.SakuraBud.TooltipEffect.FourthText").Value);
                    drawOffset.X += SakuraBloom.DrawLine(line, drawOffset, WaterSparkles, Utilities.GetLocalization("Items.SakuraBud.TooltipEffect.FifthText").Value, true, overrideColor: new(26, 169, 208));
                }
                else
                    SakuraBloom.DrawLine(line, drawOffset, WaterSparkles, "Maybe you should follow its call?");

                return false;
            }

            return true;
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
                    Vector2 ringVelocity = (TwoPi * i / numDust).ToRotationVector2().RotatedBy(Item.velocity.ToRotation() + PiOver2) * 5f;
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
