using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs.AstrumDeus;
using InfernumMode.BehaviorOverrides.BossAIs.AstrumDeus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace InfernumMode.Skies
{
    public class DeusSkyScene : ModSceneEffect
    {
        public override bool IsSceneEffectActive(Player player) => NPC.AnyNPCs(ModContent.NPCType<AstrumDeusHead>()) && !BossRushEvent.BossRushActive;

        public override void SpecialVisuals(Player player)
        {
            player.ManageSpecialBiomeVisuals("InfernumMode:Deus", IsSceneEffectActive(player));
        }
    }

    public class DeusSky : CustomSky
    {
        private struct AstralStar
        {
            public bool Blue;

            public Vector2 Position;

            public float Depth;

            public float AlphaPhaseShift;

            public float AlphaFrequency;

            public float AlphaAmplitude;
        }

        private bool isActive = false;
        private float intensity = 0f;
        private float nebulaIntensity = 0f;
        private int nebulaTimer = 0;
        private AstralStar[] Stars;

        public override void Update(GameTime gameTime)
        {
            if (isActive && intensity < 1f && !Main.dayTime)
                intensity += 0.01f;
            else if (!isActive && intensity > 0f)
                intensity -= 0.01f;

            int deus = NPC.FindFirstNPC(ModContent.NPCType<AstrumDeusHeadSpectral>());
            if (isActive && nebulaIntensity < 1f && !Main.dayTime && deus != -1 && Main.npc[deus].life < Main.npc[deus].lifeMax * AstrumDeusHeadBehaviorOverride.Phase2LifeThreshold)
                nebulaIntensity += 0.01f;
            else if (!isActive && intensity > 0f)
                nebulaIntensity -= 0.01f;

            intensity = MathHelper.Clamp(intensity, 0f, 1f);
            nebulaIntensity = MathHelper.Clamp(nebulaIntensity, 0f, 1f);

            if (nebulaIntensity <= 0f)
                nebulaTimer = 0;
            else
                nebulaTimer++;
        }

        private float GetIntensity() => intensity * 0.85f;

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            if (maxDepth < float.MaxValue || minDepth > float.MaxValue)
                return;

            spriteBatch.Draw(TextureAssets.BlackTile.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Black * GetIntensity());

            // Draw nebulous gas behind everything if Deus is below a certain life threshold.
            spriteBatch.SetBlendState(BlendState.Additive);
            for (int i = 0; i < 135; i++)
            {
                Texture2D gasTexture = ModContent.GetTexture($"InfernumMode/ExtraTextures/NebulaGas{(i % 2 == 0 ? "1" : "2")}");
                Vector2 drawPosition = new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
                float drawOutwardness = Utils.GetLerpValue(0.45f, 1.1f, i % 18f / 18f) * Utils.GetLerpValue(0f, 180f, nebulaTimer, true);
                drawPosition += (MathHelper.TwoPi * 7f * i / 75f).ToRotationVector2() * MathHelper.Max(Main.screenWidth, Main.screenHeight) * drawOutwardness;
                float rotation = MathHelper.TwoPi * (drawOutwardness + i % 18f / 18f);
                float scale = Utils.GetLerpValue(0.8f, 1.15f, i % 15f / 15f) * Utils.GetLerpValue(-40f, 130f, nebulaTimer, true);
                Color drawColor = CalamityUtils.MulticolorLerp(i / 29f % 0.999f, new Color(109, 242, 196), new Color(234, 119, 93), Color.MediumPurple) * nebulaIntensity * 0.28f;

                spriteBatch.Draw(gasTexture, drawPosition, null, drawColor, rotation, gasTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }
            spriteBatch.ResetBlendState();

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
            Texture2D starTexture = ModContent.GetTexture("InfernumMode/ExtraTextures/Gleam");
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
                    float opacity = (float)Math.Sin((Stars[j].AlphaFrequency * Main.GlobalTimeWrappedHourly + Stars[j].AlphaPhaseShift) * Stars[j].AlphaAmplitude + Stars[j].AlphaAmplitude);
                    float minorFade = (float)Math.Sin(Stars[j].AlphaFrequency * Main.GlobalTimeWrappedHourly * 5f + Stars[j].AlphaPhaseShift) * 0.1f - 0.1f;
                    minorFade = MathHelper.Clamp(minorFade, 0f, 1f);
                    opacity = MathHelper.Clamp(opacity, 0f, 1f);

                    Color drawColor = Stars[j].Blue ? new Color(109, 242, 196) : new Color(234, 119, 93);

                    // Every so often change the stars to purple/yellow instead of blue/orange, as a fun reference of sorts.
                    if (j % 140 == 139)
                        drawColor = Stars[j].Blue ? Color.MediumPurple : Color.Goldenrod;

                    drawColor *= opacity * (1f - minorFade) * intensity * 0.56f;
                    if (!Main.BackgroundEnabled)
                        drawColor *= 0.45f;

                    drawColor.A = 0;

                    Vector2 starScaleBase = new Vector2((baseScale.X * 0.5f + 0.5f) * MathHelper.Lerp(opacity, 0.7f, 1f)) * 0.67f;
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
            int horizontalArea = 60;
            int verticalArea = 12;
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
                    Stars[starIndex].AlphaPhaseShift = Main.rand.NextFloat() * MathHelper.TwoPi;
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
