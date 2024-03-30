using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Content.BehaviorOverrides.BossAIs.EmpressOfLight;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
                    Velocity = Velocity.RotatedBy(Pi / 425f);

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

        public bool isActive;
        public float Intensity;
        public List<Fairy> Fairies = [];
        public List<Light> Lights = [];

        public override void Update(GameTime gameTime)
        {
            if (isActive && Intensity < 1f)
                Intensity += 0.01f;
            else if (!isActive && Intensity > 0f)
                Intensity -= 0.01f;
            Intensity = Clamp(Intensity, 0f, 1f);
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

            if (!EmpressOfLightBehaviorOverride.InPhase3(eolNPC))
                Lights.Clear();

            int maxFairies = (int)Lerp(90f, 175f, Main.npc[eol].life / (float)Main.npc[eol].lifeMax);
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
