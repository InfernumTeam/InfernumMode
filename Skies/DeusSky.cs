using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace InfernumMode.Skies
{
    public class DeusSky : CustomSky
    {
        private struct AstralStar
        {
            public bool Blue;

            public Vector2 Position;

            public float Depth;

            public float SinOffset;

            public float AlphaFrequency;

            public float AlphaAmplitude;
        }

        private bool isActive = false;
        private float intensity = 0f;
        private AstralStar[] Stars;

        public override void Update(GameTime gameTime)
        {
            if (isActive && intensity < 1f && !Main.dayTime)
                intensity += 0.01f;
            else if (!isActive && intensity > 0f)
                intensity -= 0.01f;
        }

        private float GetIntensity() => intensity * 0.85f;

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            if (maxDepth < 3.40282347E+38f || minDepth > 3.40282347E+38f)
                return;

            spriteBatch.Draw(Main.blackTileTexture, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Black * GetIntensity());

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

            float scale = Math.Min(1f, (Main.screenPosition.Y - 1000f) / 1000f);
            Vector2 drawOffset = Main.screenPosition + new Vector2(Main.screenWidth >> 1, Main.screenHeight >> 1);
            Rectangle rectangle = new Rectangle(-1000, -1000, 4000, 4000);
            Texture2D starTexture = ModContent.GetTexture("InfernumMode/ExtraTextures/Gleam");
            for (int j = startingDrawIndex; j < endingDrawIndex; j++)
            {
                Vector2 baseScale = new Vector2(1f / Stars[j].Depth, 1.1f / Stars[j].Depth) * 0.5f;
                Vector2 drawPosition = (Stars[j].Position - drawOffset) * baseScale + drawOffset - Main.screenPosition;
                if (rectangle.Contains((int)drawPosition.X, (int)drawPosition.Y))
                {
                    float scaleOscilation = (float)Math.Sin((Stars[j].AlphaFrequency * Main.GlobalTime + Stars[j].SinOffset) * Stars[j].AlphaAmplitude + Stars[j].AlphaAmplitude);
                    float fade = (float)Math.Sin(Stars[j].AlphaFrequency * Main.GlobalTime * 5f + Stars[j].SinOffset) * 0.1f - 0.1f;
                    fade = MathHelper.Clamp(fade, 0f, 1f);
                    scaleOscilation = MathHelper.Clamp(scaleOscilation, 0f, 1f);

                    Color drawColor = Stars[j].Blue ? new Color(109, 242, 196) : new Color(234, 119, 93);
                    if (j % 72 == 71)
                        drawColor = Stars[j].Blue ? Color.MediumPurple : Color.Goldenrod;
                    drawColor *= scaleOscilation * (1f - fade) * intensity * 0.8f;
                    drawColor.A = 0;

                    Vector2 starScaleBase = new Vector2((baseScale.X * 0.5f + 0.5f) * (scaleOscilation * 0.3f + 0.7f)) * 0.67f;
                    Vector2 smallScale = starScaleBase * new Vector2(0.8f, 1.25f);
                    Vector2 largeScale = starScaleBase * new Vector2(0.8f, 2.3f + j % 14 / 14f * 0.6f);
                    if (j % 32 == 31)
                    {
                        smallScale.Y *= 1.7f;
                        largeScale.Y *= 1.35f;
                    }

                    spriteBatch.Draw(starTexture, drawPosition, null, drawColor, MathHelper.PiOver2, starTexture.Size() * 0.5f, largeScale, SpriteEffects.None, 0);
                    spriteBatch.Draw(starTexture, drawPosition, null, drawColor, 0f, starTexture.Size() * 0.5f, smallScale, SpriteEffects.None, 0);
                    spriteBatch.Draw(starTexture, drawPosition, null, drawColor, MathHelper.PiOver2, starTexture.Size() * 0.5f, largeScale * 0.6f, SpriteEffects.None, 0);
                    spriteBatch.Draw(starTexture, drawPosition, null, drawColor, 0f, starTexture.Size() * 0.5f, smallScale * 0.6f, SpriteEffects.None, 0);
                }
            }
        }

        public override float GetCloudAlpha() => 1f - intensity;

        public override void Activate(Vector2 position, params object[] args)
        {
            isActive = true;
            int horizontalArea = 300;
            int verticalArea = 30;
            Stars = new AstralStar[horizontalArea * verticalArea];
            int starIndex = 0;
            for (int i = 0; i < horizontalArea; i++)
            {
                float horizontalRatio = i / (float)horizontalArea;
                for (int j = 0; j < verticalArea; j++)
                {
                    float verticalRatio = j / (float)verticalArea;
                    Stars[starIndex].Position.X = horizontalRatio * Main.maxTilesX * 16f;
                    Stars[starIndex].Position.Y = MathHelper.Lerp((float)Main.worldSurface * 16f, -12450f, verticalRatio * verticalRatio);
                    Stars[starIndex].Depth = Main.rand.NextFloat() * 8f + 1.5f;
                    Stars[starIndex].SinOffset = Main.rand.NextFloat() * MathHelper.TwoPi;
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
            return isActive || intensity > 0f;
        }
    }
}
