using InfernumMode.BehaviorOverrides.BossAIs.EmpressOfLight;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace InfernumMode.Skies
{
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
                Opacity = Utils.InverseLerp(0f, 0.025f, LifetimeCompletion, true) * Utils.InverseLerp(1f, 0.92f, LifetimeCompletion, true);

                if (Timer % 350f > 320f)
                    Velocity = Velocity.RotatedBy(MathHelper.Pi / 425f);

                DrawPosition += Velocity;
                Direction = (Velocity.X > 0f).ToDirectionInt();
                Timer++;
            }
        }

        public bool isActive = false;
        public float Intensity = 0f;
        public List<Fairy> Fairies = new List<Fairy>();

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
            int eol = NPC.FindFirstNPC(ModContent.NPCType<EmpressOfLightNPC>());
            if (eol == -1)
            {
                Fairies.Clear();
                return;
            }

            int maxFairies = (int)MathHelper.Lerp(120f, 300f, Main.npc[eol].life / (float)Main.npc[eol].lifeMax);
            Vector2 screenCenter = Main.screenPosition + new Vector2(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f);
            Rectangle rectangle = new Rectangle(-1000, -1000, 4000, 4000);

            // Remove all fairies that should die.
            Fairies.RemoveAll(f => f.Timer >= f.Lifetime);

            // Randomly spawn fairies.
            if (Main.rand.NextBool(8) && Fairies.Count < maxFairies)
            {
                Fairies.Add(new Fairy()
                {
                    DrawPosition = Main.LocalPlayer.Center + new Vector2(Main.rand.NextFloatDirection() * 7500f, Main.rand.NextFloat(400f, 600f)),
                    Velocity = -Vector2.UnitY.RotatedByRandom(0.6f) * Main.rand.NextFloat(1f, 5f),
                    Lifetime = Main.rand.Next(1450, 2100),
                    Hue = Main.rand.NextFloat(),
                    Depth = Main.rand.NextFloat(1.2f, 13f)
                });
            }

            // Draw all fairies.
            Texture2D texture = ModContent.GetTexture("InfernumMode/ExtraTextures/Fairy");
            Texture2D glowmaskTexture = ModContent.GetTexture("InfernumMode/ExtraTextures/FairyGlowmask");
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
