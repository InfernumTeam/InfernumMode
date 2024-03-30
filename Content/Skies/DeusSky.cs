using CalamityMod.Events;
using CalamityMod.NPCs.AstrumDeus;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Content.BehaviorOverrides.BossAIs.AstrumDeus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace InfernumMode.Content.Skies
{
    public class DeusSkyScene : ModSceneEffect
    {
        public override bool IsSceneEffectActive(Player player) => NPC.AnyNPCs(ModContent.NPCType<AstrumDeusHead>()) && !BossRushEvent.BossRushActive;

        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

        public override void SpecialVisuals(Player player, bool isActive)
        {
            player.ManageSpecialBiomeVisuals("InfernumMode:Deus", isActive);
            if (isActive)
                SkyManager.Instance["InfernumMode:Deus"].Update(new());
        }
    }

    public class DeusSky : CustomSky
    {
        public struct AstralStar
        {
            public bool Blue;

            public Vector2 Position;

            public float Depth;

            public float AlphaPhaseShift;

            public float AlphaFrequency;

            public float AlphaAmplitude;
        }

        private float nebulaIntensity = 1f;
        private int nebulaTimer;
        private AstralStar[] Stars;
        public bool isActive = true;
        public float Intensity;
        public int DeusIndex = -1;

        public override void Update(GameTime gameTime)
        {
            if (isActive && Intensity < 1f)
                Intensity += 0.01f;
            else if (!isActive && Intensity > 0f)
                Intensity -= 0.01f;
            nebulaIntensity = Clamp(nebulaIntensity + isActive.ToDirectionInt() * 0.01f, 0f, 1f);

            if (DeusIndex < 0)
            {
                if (nebulaTimer >= 1)
                    nebulaTimer--;
                return;
            }

            if (nebulaIntensity <= 0f || Main.npc[DeusIndex].life >= Main.npc[DeusIndex].lifeMax * AstrumDeusHeadBehaviorOverride.Phase2LifeRatio)
                nebulaTimer = 0;
            else
                nebulaTimer++;
        }

        private float GetIntensity()
        {
            return UpdatePIndex() ? Main.npc[DeusIndex].Infernum().ExtraAI[8] : 0.5f;
        }

        public override Color OnTileColor(Color inColor) => inColor;

        private bool UpdatePIndex()
        {
            int ProvType = ModContent.NPCType<AstrumDeusHead>();
            if (DeusIndex >= 0 && Main.npc[DeusIndex].active && Main.npc[DeusIndex].type == ProvType)
            {
                return true;
            }
            DeusIndex = -1;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == ProvType)
                {
                    DeusIndex = i;
                    break;
                }
            }
            return DeusIndex != -1;
        }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            if (maxDepth >= 0 && minDepth < 0)
            {
                float Intensity = GetIntensity();
                Main.spriteBatch.Draw(TextureAssets.BlackTile.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Black * Intensity);
            }

            if (maxDepth < float.MaxValue || minDepth > float.MaxValue)
                return;

            // Draw nebulous gas behind everything if Deus is below a certain life threshold.
            if (nebulaTimer > 0f)
            {
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Utilities.GetCustomSkyBackgroundMatrix());
                for (int i = 0; i < 185; i++)
                {
                    Texture2D gasTexture = ModContent.Request<Texture2D>($"InfernumMode/Assets/ExtraTextures/GreyscaleObjects/NebulaGas{(i % 2 == 0 ? "1" : "2")}").Value;
                    Vector2 drawPosition = new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
                    float drawOutwardness = Utils.GetLerpValue(0.45f, 1.1f, i % 18f / 18f) * Utils.GetLerpValue(0f, 180f, nebulaTimer, true);
                    drawPosition += (TwoPi * 7f * i / 75f).ToRotationVector2() * MathF.Max(Main.screenWidth, Main.screenHeight) * drawOutwardness;
                    float rotation = TwoPi * (drawOutwardness + i % 18f / 18f);
                    float scale = Utils.GetLerpValue(0.8f, 1.15f, i % 15f / 15f) * Utils.GetLerpValue(-40f, 130f, nebulaTimer, true);
                    Color drawColor = LumUtils.MulticolorLerp(i / 29f % 0.999f, new Color(109, 242, 196), new Color(234, 119, 93), Color.MediumPurple) * nebulaIntensity * 0.28f;

                    Main.spriteBatch.Draw(gasTexture, drawPosition, null, drawColor, rotation, gasTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                }
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Utilities.GetCustomSkyBackgroundMatrix());
            }

            int startingDrawIndex = -1;
            int endingDrawIndex = 0;
            for (int i = 0; i < Stars.Length; i++)
            {
                float depth = Stars[i].Depth;
                if (startingDrawIndex == -1 && depth < maxDepth)
                    startingDrawIndex = i;

                if (depth <= minDepth)
                    break;

                endingDrawIndex = i;
            }
            if (startingDrawIndex == -1)
                return;

            Vector2 drawOffset = Main.screenPosition + new Vector2(Main.screenWidth >> 1, Main.screenHeight >> 1);
            Rectangle rectangle = new(-1000, -1000, 4000, 4000);
            Texture2D starTexture = InfernumTextureRegistry.Gleam.Value;
            for (int j = startingDrawIndex; j < endingDrawIndex; j++)
            {
                // Draw less stars if the background is disabled, to prevent too much visual distraction.
                if (!Main.BackgroundEnabled && j % 4 != 0)
                    continue;

                Vector2 baseScale = new Vector2(1f / Stars[j].Depth, 1.1f / Stars[j].Depth) * 0.5f;
                Vector2 drawPosition = (Stars[j].Position - drawOffset) * baseScale + drawOffset - Main.screenPosition;
                if (rectangle.Contains((int)drawPosition.X, (int)drawPosition.Y))
                {
                    // Handle alpha pulsing. This is what allows the stars to appear, disappear, and reappear.
                    float opacity = Sin((Stars[j].AlphaFrequency * Main.GlobalTimeWrappedHourly + Stars[j].AlphaPhaseShift) * Stars[j].AlphaAmplitude + Stars[j].AlphaAmplitude);
                    float minorFade = Sin(Stars[j].AlphaFrequency * Main.GlobalTimeWrappedHourly * 5f + Stars[j].AlphaPhaseShift) * 0.1f - 0.1f;
                    minorFade = Clamp(minorFade, 0f, 1f);
                    opacity = Clamp(opacity, 0f, 1f);

                    Color drawColor = Stars[j].Blue ? new Color(109, 242, 196) : new Color(234, 119, 93);

                    // Every so often change the stars to purple/yellow instead of blue/orange, as a fun reference of sorts.
                    if (j % 140 == 139)
                        drawColor = Stars[j].Blue ? Color.MediumPurple : Color.Goldenrod;

                    drawColor *= opacity * (1f - minorFade) * Intensity * 0.56f;
                    if (!Main.BackgroundEnabled)
                        drawColor *= 0.45f;

                    drawColor.A = 0;

                    Vector2 starScaleBase = new Vector2((baseScale.X * 0.5f + 0.5f) * Lerp(opacity, 0.7f, 1f)) * 0.67f;
                    Vector2 smallScale = starScaleBase * new Vector2(0.8f, 1.25f);
                    Vector2 largeScale = starScaleBase * new Vector2(0.8f, 2.3f + j % 14 / 14f * 0.6f);
                    if (j % 32 == 31)
                    {
                        smallScale.Y *= 1.7f;
                        largeScale.Y *= 1.35f;
                    }

                    Main.spriteBatch.Draw(starTexture, drawPosition, null, drawColor, PiOver2, starTexture.Size() * 0.5f, largeScale, SpriteEffects.None, 0);
                    Main.spriteBatch.Draw(starTexture, drawPosition, null, drawColor, 0f, starTexture.Size() * 0.5f, smallScale, SpriteEffects.None, 0);
                    Main.spriteBatch.Draw(starTexture, drawPosition, null, drawColor, PiOver2, starTexture.Size() * 0.5f, largeScale * 0.6f, SpriteEffects.None, 0);
                    Main.spriteBatch.Draw(starTexture, drawPosition, null, drawColor, 0f, starTexture.Size() * 0.5f, smallScale * 0.6f, SpriteEffects.None, 0);
                }
            }
        }

        public override float GetCloudAlpha()
        {
            return 0f;
        }

        public override void Activate(Vector2 position, params object[] args)
        {
            isActive = true;
            int horizontalArea = 90;
            int verticalArea = 22;
            Stars = new AstralStar[horizontalArea * verticalArea];
            int starIndex = 0;
            for (int i = 0; i < horizontalArea; i++)
            {
                float horizontalRatio = i / (float)horizontalArea;
                for (int j = 0; j < verticalArea; j++)
                {
                    float verticalRatio = j / (float)verticalArea;
                    Stars[starIndex].Position.X = horizontalRatio * Main.maxTilesX * 16f;
                    Stars[starIndex].Position.Y = Lerp((float)Main.worldSurface * 16f, -12450f, verticalRatio * verticalRatio);
                    Stars[starIndex].Depth = Main.rand.NextFloat() * 8f + 1.5f;
                    Stars[starIndex].AlphaPhaseShift = Main.rand.NextFloat() * TwoPi;
                    Stars[starIndex].AlphaAmplitude = Main.rand.NextFloat() * 5f;
                    Stars[starIndex].AlphaFrequency = Main.rand.NextFloat() + 0.35f;
                    Stars[starIndex].Blue = Main.rand.NextBool(2);
                    starIndex++;
                }
            }
            Array.Sort(Stars, new Comparison<AstralStar>((m1, m2) => m2.Depth.CompareTo(m1.Depth)));
        }

        public override void Deactivate(params object[] args)
        {
            isActive = false;
        }

        public override void Reset()
        {
            isActive = false;
        }

        public override bool IsActive()
        {
            return isActive || Intensity > 0f;
        }
    }
}
