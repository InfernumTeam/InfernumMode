using CalamityMod.NPCs.DevourerofGods;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.ScreenEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Skies
{
    public class DoGSkyScene : ModSceneEffect
    {
        public override bool IsSceneEffectActive(Player player) => CosmicBackgroundSystem.EffectIsActive || CosmicBackgroundSystem.MonolithIntensity > 0f;

        // FUCK YOU FUCK YOU
        public override SceneEffectPriority Priority => SceneEffectPriority.BiomeLow;

        public override float GetWeight(Player player) => 0.9f;

        public override void SpecialVisuals(Player player, bool isActive)
        {
            if (!CosmicBackgroundSystem.EffectIsActive)
                CosmicBackgroundSystem.MonolithIntensity = Clamp(CosmicBackgroundSystem.MonolithIntensity - 0.02f, 0f, 1f);
            player.ManageSpecialBiomeVisuals("InfernumMode:DoG", isActive);
        }
    }

    public class DoGSkyInfernum : CustomSky
    {
        public class Lightning
        {
            public int Lifetime;
            public float Depth;
            public Vector2 Position;
            public Color LightningColor;
        }

        public bool isActive;
        public float Intensity;
        public int EdgyWormIndex = -1;
        public List<Lightning> LightningBolts = new();

        public static void CreateLightningBolt(Color color, int count = 1, bool playSound = false)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            for (int i = 0; i < count; i++)
            {
                Lightning lightning = new()
                {
                    Lifetime = 30,
                    Depth = Main.rand.NextFloat(1.5f, 10f),
                    Position = new Vector2(Main.LocalPlayer.Center.X + Main.rand.NextFloatDirection() * 5000f, Main.rand.NextFloat(4850f)),
                    LightningColor = color
                };
                (SkyManager.Instance["InfernumMode:DoG"] as DoGSkyInfernum).LightningBolts.Add(lightning);
            }

            if (playSound && !Main.gamePaused)
                SoundEngine.PlaySound(InfernumSoundRegistry.CalThunderStrikeSound with { Volume = 0.5f }, Main.LocalPlayer.Center);
        }

        public override void Update(GameTime gameTime)
        {
            if (isActive && Intensity < 1f)
                Intensity += 0.01f;
            else if (!isActive && Intensity > 0f)
                Intensity -= 0.01f;

            for (int i = 0; i < LightningBolts.Count; i++)
            {
                LightningBolts[i].Lifetime--;
            }
            LightningBolts.RemoveAll(l => l.Lifetime <= 0);
        }

        private float GetIntensity()
        {
            UpdatePIndex();
            return 1f;
        }

        public override Color OnTileColor(Color inColor)
        {
            float Intensity = GetIntensity();
            return new Color(Vector4.Lerp(new Vector4(0.5f, 0.8f, 1f, 1f), inColor.ToVector4(), 1f - Intensity));
        }

        private bool UpdatePIndex()
        {
            int ProvType = ModContent.NPCType<DevourerofGodsHead>();
            if (EdgyWormIndex >= 0 && Main.npc[EdgyWormIndex].active && Main.npc[EdgyWormIndex].type == ProvType)
            {
                return true;
            }
            EdgyWormIndex = -1;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == ProvType)
                {
                    EdgyWormIndex = i;
                    break;
                }
            }
            return EdgyWormIndex != -1;
        }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            Texture2D flashTexture = ModContent.Request<Texture2D>("Terraria/Images/Misc/VortexSky/Flash").Value;
            Texture2D boltTexture = ModContent.Request<Texture2D>("Terraria/Images/Misc/VortexSky/Bolt").Value;

            // Draw lightning bolts.
            float spaceFade = Math.Min(1f, (Main.screenPosition.Y - 300f) / 300f);
            Vector2 screenCenter = Main.screenPosition + new Vector2(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f);
            Rectangle rectangle = new(-1000, -1000, 4000, 4000);

            LightningBolts.RemoveAll(l => l.Lifetime <= 0);

            for (int i = 0; i < LightningBolts.Count; i++)
            {
                Vector2 boltScale = new(1f / LightningBolts[i].Depth, 0.9f / LightningBolts[i].Depth);
                Vector2 position = (LightningBolts[i].Position - screenCenter) * boltScale + screenCenter - Main.screenPosition;
                if (rectangle.Contains((int)position.X, (int)position.Y))
                {
                    Texture2D texture = boltTexture;
                    int life = LightningBolts[i].Lifetime;
                    if (life > 24 && life % 2 == 0)
                        texture = flashTexture;

                    float opacity = life * spaceFade / 20f;
                    Main.spriteBatch.Draw(texture, position, null, LightningBolts[i].LightningColor * opacity, 0f, Vector2.Zero, boltScale.X * 5f, SpriteEffects.None, 0f);
                }
            }

            if (CosmicBackgroundSystem.EffectIsActive || CosmicBackgroundSystem.MonolithIntensity > 0f)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicWrap, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);

                CosmicBackgroundSystem.Draw();

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicWrap, DepthStencilState.None, Main.Rasterizer, null, Utilities.GetCustomSkyBackgroundMatrix());
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
