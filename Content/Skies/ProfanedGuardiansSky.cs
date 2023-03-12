using CalamityMod.NPCs;
using CalamityMod.NPCs.ProfanedGuardians;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians;
using InfernumMode.Content.Projectiles.Wayfinder;
using InfernumMode.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace InfernumMode.Content.Skies
{
    public class ProfanedGuardiansSkyScene : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

        public override float GetWeight(Player player) => 0.6f;

        public override bool IsSceneEffectActive(Player player)
        {
            int ID = GuardianComboAttackManager.CommanderType;
            int npcIndex = NPC.FindFirstNPC(ID);
            NPC npc = npcIndex >= 0 ? Main.npc[npcIndex] : null;
            return npc != null;
        }

        public override void SpecialVisuals(Player player, bool isActive)
        {
            player.ManageSpecialBiomeVisuals("InfernumMode:GuardianCommander", isActive);
        }
    }

    public class ProfanedGuardiansSky : CustomSky
    {
        public class Symbol
        {
            public int Timer;
            public int Lifetime;
            public int Varient; // 0-8
            public float ColorLerpAmount;
            public float Opacity;
            public float Depth;
            public Vector2 DrawPosition;

            public Rectangle Frame => new(0, Varient * 30, 50, 30);

            public float LifetimeCompletion => Timer / (float)Lifetime;

            public void Update()
            {
                Opacity = Utils.GetLerpValue(0f, 0.1f, LifetimeCompletion, true) * Utils.GetLerpValue(1f, 0.9f, LifetimeCompletion, true);
                Timer++;
            }
        }

        private readonly List<Symbol> Symbols = new();

        private bool isActive = false;

        private float intensity = 0f;

        private static float MaxIntensity
        {
            get
            {
                return AttackerGuardianBehaviorOverride.TotalRemaininGuardians switch
                {
                    3 => 0.05f,
                    2 => 0.13f,
                    _ => 0.21f
                };
            }
        }

        public override void Activate(Vector2 position, params object[] args) => isActive = true;

        public override void Deactivate(params object[] args) => isActive = false;

        public override void Reset() => isActive = false;

        public override void Update(GameTime gameTime)
        {
            if (isActive && intensity < 1f)
                intensity = MathHelper.Clamp(intensity + 0.025f, 0f, MaxIntensity);
            else if (!isActive && intensity > 0f)
                intensity = MathHelper.Clamp(intensity - 0.025f, 0f, MaxIntensity);

            if (NPC.FindFirstNPC(ModContent.NPCType<ProfanedGuardianCommander>()) == -1)
                Deactivate();
        }

        public override Color OnTileColor(Color inColor)
        {
            return Color.Lerp(inColor, WayfinderSymbol.Colors[1], 0.5f * intensity);
        }

        public override float GetCloudAlpha() => 0f;

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.doughnutBoss) || !InfernumMode.CanUseCustomAIs)
            {
                Symbols.Clear();
                Deactivate();
                return;
            }            

            if (maxDepth >= 0 && minDepth < 0)
            {
                Texture2D skyTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Skies/ProfanedGuardiansSky").Value;
                spriteBatch.Draw(skyTexture, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White with { A = 0 } * intensity);
            }

            // Only draw the symbols in phase3.
            if (AttackerGuardianBehaviorOverride.TotalRemaininGuardians > 1)
                return;

            NPC commander = Main.npc[CalamityGlobalNPC.doughnutBoss];

            // Remove all things that should die.
            Symbols.RemoveAll(s => s.Timer >= s.Lifetime);

            float maxSymbols = MathHelper.Lerp(75, 150, 1f - (float)commander.life / commander.lifeMax);

            // Randomly spawn symbols.
            if (Main.rand.NextBool(10) && Symbols.Count < maxSymbols)
            {

                Symbols.Add(new Symbol()
                {
                    DrawPosition = Main.LocalPlayer.Center + new Vector2(Main.rand.NextFloatDirection() * 7500f, Main.rand.NextFloat(-Main.screenHeight / 2f, Main.screenHeight / 2f)),
                    Lifetime = Main.rand.Next(1500, 2100),
                    ColorLerpAmount = Main.rand.NextFloat(),
                    Depth = Main.rand.NextFloat(1.3f, 3f),
                    Varient = Main.rand.Next(0, 9)
                });
            }

            Texture2D symbolTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Skies/ProfanedSymbols").Value;
            Vector2 screenCenter = Main.screenPosition + new Vector2(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f);
            Rectangle rectangle = new(-1000, -1000, 4000, 4000);

            // Draw all symbols
            for (int i = 0; i < Symbols.Count; i++)
            {
                Symbol symbol = Symbols[i];
                symbol.Update();
                if (symbol.Depth > minDepth && symbol.Depth < maxDepth * 2f)
                {
                    Vector2 scale = new(1f / symbol.Depth, 1f / symbol.Depth);
                    Vector2 position = (symbol.DrawPosition - screenCenter) * scale + screenCenter - Main.screenPosition;
                    if (rectangle.Contains((int)position.X, (int)position.Y))
                    {
                        Color lightColor = Color.Lerp(WayfinderSymbol.Colors[0], WayfinderSymbol.Colors[1], symbol.ColorLerpAmount) * symbol.Opacity;
                        lightColor.A = 0;
                        Vector2 origin = symbol.Frame.Size() * 0.5f;
                        spriteBatch.Draw(symbolTexture, position, symbol.Frame, lightColor with { A = 35 }, 0f, origin, scale * symbol.Opacity, 0, 0f);
                    }
                }
            }
        }

        public override bool IsActive() => isActive || intensity > 0f;
    }
}
