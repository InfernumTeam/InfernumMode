using CalamityMod;
using CalamityMod.FluidSimulation;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Content.BehaviorOverrides.BossAIs.EmpressOfLight;
using InfernumMode.Core;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Skies
{
    public class EmpressOfLightSkyScene : ModSceneEffect
    {
        public override bool IsSceneEffectActive(Player player)
        {
            int empressID = NPCID.HallowBoss;
            int empress = NPC.FindFirstNPC(empressID);
            NPC empressNPC = empress >= 0 ? Main.npc[empress] : null;
            bool enabled = empressNPC != null && EmpressOfLightBehaviorOverride.InPhase2(empressNPC);
            return enabled;
        }

        public override void SpecialVisuals(Player player, bool isActive)
        {
            player.ManageSpecialBiomeVisuals("InfernumMode:EmpressOfLight", isActive);
        }
    }

    public class EmpressOfLightSky : CustomSky
    {
        public class Fairy
        {
            public int Direction;
            public int Timer;
            public int Lifetime;
            public float Hue;
            public float Opacity;
            public float Depth;
            public Vector2 DrawPosition;
            public Vector2 Velocity;

            public int Frame => Timer / 50 % 4;

            public float LifetimeCompletion => Timer / (float)Lifetime;

            public void Update()
            {
                Hue = (Hue + 0.0004f) % 1f;
                Opacity = Utils.GetLerpValue(0f, 0.025f, LifetimeCompletion, true) * Utils.GetLerpValue(1f, 0.92f, LifetimeCompletion, true);

                if (Timer % 350f > 320f)
                    Velocity = Velocity.RotatedBy(MathHelper.Pi / 425f);

                DrawPosition += Velocity;
                Direction = (Velocity.X > 0f).ToDirectionInt();
                Timer++;
            }
        }

        public class Light
        {
            public int Timer;
            public int Lifetime;
            public float Hue;
            public float Opacity;
            public float Depth;
            public Vector2 DrawPosition;

            public float LifetimeCompletion => Timer / (float)Lifetime;

            public void Update()
            {
                Opacity = Utils.GetLerpValue(0f, 0.1f, LifetimeCompletion, true) * Utils.GetLerpValue(1f, 0.9f, LifetimeCompletion, true);
                Timer++;
            }
        }

        public bool isActive = false;
        public float Intensity = 0f;
        public float AuroraOpacity;
        public List<Fairy> Fairies = new();
        public List<Light> Lights = new();

        public FluidField AuroraField = null;

        public override void Update(GameTime gameTime)
        {
            if (isActive && Intensity < 1f)
                Intensity += 0.01f;
            else if (!isActive && Intensity > 0f)
                Intensity -= 0.01f;
            Intensity = MathHelper.Clamp(Intensity, 0f, 1f);
        }

        public override Color OnTileColor(Color inColor)
        {
            return inColor;
        }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            int eol = NPC.FindFirstNPC(NPCID.HallowBoss);
            if (eol == -1 || !EmpressOfLightBehaviorOverride.InPhase2(Main.npc[eol]) || !InfernumMode.CanUseCustomAIs)
            {
                Lights.Clear();
                Fairies.Clear();
                Deactivate();
                return;
            }

            NPC eolNPC = Main.npc[eol];

            AuroraOpacity = MathHelper.Clamp(AuroraOpacity - InfernumConfig.Instance.ReducedGraphicsConfig.ToDirectionInt() * 0.02f, 0f, 1f);
            if (!EmpressOfLightBehaviorOverride.InPhase3(eolNPC))
            {
                AuroraOpacity = 0f;
                Lights.Clear();
            }

            int maxFairies = (int)MathHelper.Lerp(90f, 175f, Main.npc[eol].life / (float)Main.npc[eol].lifeMax);
            int maxLights = maxFairies + 65;
            Vector2 screenCenter = Main.screenPosition + new Vector2(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f);
            Rectangle rectangle = new(-1000, -1000, 4000, 4000);

            // Remove all things that should die.
            Fairies.RemoveAll(f => f.Timer >= f.Lifetime);
            Lights.RemoveAll(l => l.Timer >= l.Lifetime);

            // Randomly spawn fairies.
            if (Main.rand.NextBool(8) && Fairies.Count < maxFairies)
            {
                Fairies.Add(new Fairy()
                {
                    DrawPosition = Main.LocalPlayer.Center + new Vector2(Main.rand.NextFloatDirection() * 7500f, Main.rand.NextFloat(400f, 600f)),
                    Velocity = -Vector2.UnitY.RotatedByRandom(0.6f) * Main.rand.NextFloat(1f, 5f),
                    Lifetime = Main.rand.Next(2000, 2900),
                    Hue = Main.rand.NextFloat(),
                    Depth = Main.rand.NextFloat(1.8f, 13f)
                });
            }

            // Randomly spawn lights in the third phase.
            if (Main.rand.NextBool(16) && Lights.Count < maxLights && EmpressOfLightBehaviorOverride.InPhase3(eolNPC))
            {
                Lights.Add(new Light()
                {
                    DrawPosition = Main.LocalPlayer.Center + new Vector2(Main.rand.NextFloatDirection() * 7500f, Main.rand.NextFloat(-3300f, -1000f)),
                    Lifetime = Main.rand.Next(1450, 2100),
                    Hue = Main.rand.NextFloat(),
                    Depth = Main.rand.NextFloat(0.85f, 3f)
                });
            }

            // Draw an aurora in the background.
            if (AuroraOpacity > 0f)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.Default, RasterizerState.CullClockwise);

                DrawAurora();

                Main.spriteBatch.End();
                Main.spriteBatch.Begin();
            }

            // Draw all fairies.
            Texture2D texture = ModContent.Request<Texture2D>("InfernumMode/Content/Skies/Fairy").Value;
            Texture2D glowmaskTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Skies/FairyGlowmask").Value;
            for (int i = 0; i < Fairies.Count; i++)
            {
                Fairies[i].Update();
                if (Fairies[i].Depth > minDepth && Fairies[i].Depth < maxDepth * 2f)
                {
                    Vector2 fairyScale = new Vector2(1f / Fairies[i].Depth, 0.9f / Fairies[i].Depth) * 0.4f;
                    Vector2 position = (Fairies[i].DrawPosition - screenCenter) * fairyScale + screenCenter - Main.screenPosition;
                    SpriteEffects direction = Fairies[i].Direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                    if (rectangle.Contains((int)position.X, (int)position.Y))
                    {
                        Color glowmaskColor = Main.hslToRgb(Fairies[i].Hue, 1f, 0.5f) * Fairies[i].Opacity;
                        glowmaskColor.A /= 5;
                        Rectangle frame = texture.Frame(1, 4, 0, Fairies[i].Frame);
                        Vector2 origin = frame.Size() * 0.5f;

                        spriteBatch.Draw(texture, position, frame, Color.White * Fairies[i].Opacity, 0f, origin, fairyScale.X * 5f, direction, 0f);
                        spriteBatch.Draw(glowmaskTexture, position, frame, glowmaskColor, 0f, origin, fairyScale.X * 5f, direction, 0f);
                    }
                }
            }

            // Draw all lights.
            texture = InfernumTextureRegistry.EmpressStar.Value;

            for (int i = 0; i < Lights.Count; i++)
            {
                Lights[i].Update();
                if (Lights[i].Depth > minDepth && Lights[i].Depth < maxDepth * 2f)
                {
                    Vector2 lightScale = new Vector2(1f / Lights[i].Depth, 0.9f / Lights[i].Depth) * 0.25f;
                    Vector2 position = (Lights[i].DrawPosition - screenCenter) * lightScale + screenCenter - Main.screenPosition;
                    if (rectangle.Contains((int)position.X, (int)position.Y))
                    {
                        Color lightColor = Main.hslToRgb(Lights[i].Hue, 1f, 0.6f) * Lights[i].Opacity;
                        lightColor.A = 0;
                        Vector2 origin = texture.Size() * 0.5f;

                        spriteBatch.Draw(texture, position, null, lightColor, 0f, origin, lightScale.X * new Vector2(2f, 3f) * Lights[i].Opacity, 0, 0f);
                    }
                }
            }
        }

        public void DrawAurora()
        {
            int screenWidth = Main.instance.GraphicsDevice.Viewport.Width;

            int size = 180;
            FluidFieldManager.AdjustSizeRelativeToGraphicsQuality(ref size);
            if (AuroraField is null || AuroraField.Size != size)
            {
                float auroraScale = MathHelper.Max(screenWidth, Main.screenHeight) / size;
                AuroraField = FluidFieldManager.CreateField(size, auroraScale, 0.1f, 0.1f, 0.98f);
            }

            AuroraField.ShouldUpdate = true;
            AuroraField.UpdateAction = () =>
            {
                for (int i = 0; i < AuroraField.Size; i += 6)
                {
                    float xInterpolant = i / (float)AuroraField.Size;
                    Color auroraColor = Main.hslToRgb((xInterpolant * 0.5f + Main.GlobalTimeWrappedHourly * 0.09f) % 1f, 1f, Main.rand.NextFloat(0.56f, 0.95f));
                    Vector2 fluidVelocity = Vector2.UnitY;
                    fluidVelocity *= MathHelper.Lerp(0.4f, 0.75f, (float)Math.Sin(xInterpolant * 23.3f + Main.GlobalTimeWrappedHourly * 0.8f) * 0.5f + 0.5f);
                    fluidVelocity += Main.rand.NextVector2Circular(2f, 0.2f);
                    fluidVelocity *= (float)Math.Pow(CalamityUtils.Convert01To010(xInterpolant), 0.15);

                    if (Main.rand.NextBool(180))
                        fluidVelocity.Y *= 10f;

                    AuroraField.CreateSource(i, 1, 1f, auroraColor, fluidVelocity);
                }
            };

            typeof(FluidField).GetMethod("Draw").Invoke(AuroraField, new object[]
            {
                new Vector2(screenWidth * 0.5f, screenWidth * 0.4f), true, Matrix.Identity, Matrix.Identity
            });
        }

        public override float GetCloudAlpha()
        {
            return 0f;
        }

        public override void Activate(Vector2 position, params object[] args)
        {
            isActive = true;
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
