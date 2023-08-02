using CalamityMod.Events;
using CalamityMod.Items;
using CalamityMod.Rarities;
using InfernumMode.Assets.Effects;
using InfernumMode.Content.BehaviorOverrides.BossAIs.AdultEidolonWyrm;
using InfernumMode.Content.Projectiles.Magic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace InfernumMode.Content.Items.Weapons.Magic
{
    public class IllusionersReverie : ModItem
    {
        public const float TargetingDistance = 2300f;

        public override void SetStaticDefaults()
        {
            //string tooltip = "Casts shadow illusions of yourself that hunt down nearby enemies before dispersing into clouds of dark magic\n" +
            //    "You shouldn't see this\n" +
            //    "Its existence compels one to wonder what knowledge has been lost to the mysterious, watery depths";
            // DisplayName.SetDefault("Illusioner's Reverie");
            // Tooltip.SetDefault(tooltip);
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(6, 12));
            ItemID.Sets.AnimatesAsSoul[Type] = true;
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.damage = 720;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 10;
            Item.width = 38;
            Item.height = 40;
            Item.useTime = Item.useAnimation = 120;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.UseSound = BossRushEvent.BossSummonSound;
            Item.knockBack = 0f;

            Item.value = CalamityGlobalItem.RarityVioletBuyPrice;
            Item.rare = ModContent.RarityType<Violet>();

            Item.autoReuse = true;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<IllusionersReverieProj>();
            Item.channel = true;
            Item.shootSpeed = 0f;
        }

        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

        public override bool PreDrawTooltipLine(DrawableTooltipLine line, ref int yOffset)
        {
            if (line.Name == "Tooltip1")
            {
                Vector2 drawOffset = Vector2.UnitY * yOffset;
                drawOffset.X += DrawLine(line, drawOffset, "An ");

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.SamplerStateForCursor, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

                // Apply glitch effects to the word "incomprehensibly".
                var displacementShader = InfernumEffectsRegistry.NoiseDisplacementShader;
                displacementShader.UseColor(Color.Purple);
                displacementShader.UseImage1("Images/Misc/Perlin");
                displacementShader.UseImage2("Images/Misc/noise");
                displacementShader.Shader.Parameters["noiseIntensity"].SetValue(2f);
                displacementShader.Shader.Parameters["horizontalDisplacementFactor"].SetValue(0.0094f);
                displacementShader.Apply();
                drawOffset.X += DrawLine(line, drawOffset, "incomprehensibly ");

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.SamplerStateForCursor, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

                drawOffset.X += DrawLine(line, drawOffset, "old tome. Somehow, in spite of its supposed age, it appears to be completely unscathed");

                return false;
            }
            if (line.Name == "Tooltip2")
            {
                DrawLine(line, Vector2.UnitY * yOffset);
                return false;
            }
            return true;
        }

        public static float DrawLine(DrawableTooltipLine line, Vector2 drawOffset, string overridingText = null)
        {
            Color textOuterColor = Color.Black;

            // Get the text of the tooltip line.
            string text = overridingText ?? line.Text;
            Vector2 textPosition = new Vector2(line.X, line.Y) + drawOffset;

            // Get an offset to the afterimageOffset based on a sine wave.
            float sine = (float)((1f + Math.Sin(Main.GlobalTimeWrappedHourly * 2.5f)) * 0.5f);
            float sineOffset = Lerp(0.4f, 0.775f, sine);

            // Draw text backglow effects.
            for (int i = 0; i < 12; i++)
            {
                Vector2 afterimageOffset = (TwoPi * i / 12f).ToRotationVector2() * (2f * sineOffset);

                // Draw the text. Rotate the position based on i.
                ChatManager.DrawColorCodedString(Main.spriteBatch, line.Font, text, (textPosition + afterimageOffset).RotatedBy(TwoPi * (i / 12)), textOuterColor * 0.9f, line.Rotation, line.Origin, line.BaseScale);
            }

            // Draw the main inner text.
            ChatManager.DrawColorCodedString(Main.spriteBatch, line.Font, text, textPosition, AEWHeadBehaviorOverride.LoreTooltipColor, line.Rotation, line.Origin, line.BaseScale);

            return line.Font.MeasureString(text).X * line.BaseScale.X;
        }
    }
}
