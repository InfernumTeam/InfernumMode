using CalamityMod.Events;
using InfernumMode.Assets.Effects;
using InfernumMode.Common.Graphics.Primitives;
using InfernumMode.Core.GlobalInstances;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.Twins.TwinsAttackSynchronizer;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Twins
{
    public class SpazmatismAIClass : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.Spazmatism;

        public override float[] PhaseLifeRatioThresholds =>
        [
            Phase2LifeRatioThreshold,
            Phase3LifeRatioThreshold
        ];

        #region Loading
        public override void Load()
        {
            GlobalNPCOverrides.BossHeadSlotEvent += ChangeMapIconConditions;
        }

        private void ChangeMapIconConditions(NPC npc, ref int index)
        {
            if (npc.type == NPCID.Spazmatism)
            {
                if (npc.Opacity <= 0f)
                    index = -1;
                else if (PersonallyInPhase2(npc))
                    index = 21;
                else
                    index = 20;
            }
            if (npc.type == NPCID.Retinazer)
            {
                if (npc.Opacity <= 0f)
                    index = -1;
                else if (PersonallyInPhase2(npc))
                    index = 16;
                else
                    index = 15;
            }
        }
        #endregion Loading

        #region AI
        public override bool PreAI(NPC npc) => DoAI(npc);
        #endregion AI

        #region Frames and Drawcode
        public static float FlameTrailWidthFunctionBig(NPC npc, float completionRatio)
        {
            return SmoothStep(60f, 22f, completionRatio) * npc.Infernum().ExtraAI[6] / 15f;
        }

        public static Color FlameTrailColorFunctionBig(NPC npc, float completionRatio)
        {
            float trailOpacity = Utils.GetLerpValue(0.8f, 0.27f, completionRatio, true) * Utils.GetLerpValue(0f, 0.067f, completionRatio, true) * 0.9f;
            Color startingColor = Color.Lerp(Color.White, Color.LightGreen, 0.25f);
            Color middleColor = Color.Lerp(Color.Lime, Color.White, 0.35f);
            Color endColor = Color.Lerp(Color.ForestGreen, Color.White, 0.47f);
            Color color = LumUtils.MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * (npc.Infernum().ExtraAI[6] / 15f) * trailOpacity;
            color.A = 0;
            return color;
        }

        public static void DrawChainsBetweenTwins(NPC npc)
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (!n.active || npc.whoAmI == i || n.type != NPCID.Retinazer || n.type != NPCID.Spazmatism)
                    continue;

                bool chainIsTooLong = !npc.WithinRange(n.Center, 2000f);
                float rotation = npc.AngleTo(n.Center) - PiOver2;
                Vector2 currentChainPosition = npc.Center;
                Texture2D chainTexture = TextureAssets.Chain12.Value;

                // Iteratively draw the chain until it's sufficiently close to the other twin.
                while (!chainIsTooLong)
                {
                    float distanceFromDestination = Vector2.Distance(currentChainPosition, n.Center);
                    if (distanceFromDestination < 40f)
                        break;

                    currentChainPosition += (n.Center - currentChainPosition).SafeNormalize(Vector2.Zero) * chainTexture.Height;
                    Color chainColor = npc.GetAlpha(Color.White);
                    Main.spriteBatch.Draw(chainTexture, currentChainPosition - Main.screenPosition, null, chainColor, rotation, chainTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
                }
            }
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            // Reset afterimage lengths.
            NPCID.Sets.TrailingMode[npc.type] = 3;
            NPCID.Sets.TrailCacheLength[npc.type] = 5;
            if (npc.oldPos.Length != NPCID.Sets.TrailCacheLength[npc.type])
            {
                npc.oldPos = new Vector2[NPCID.Sets.TrailCacheLength[npc.type]];
                npc.oldRot = new float[NPCID.Sets.TrailCacheLength[npc.type]];
            }

            // Have Spazmatism draw the chain between the two twins.
            DrawChainsBetweenTwins(npc);

            // Initialize the flame tail drawer.
            if (npc.Infernum().OptionalPrimitiveDrawer is null)
            {
                npc.Infernum().OptionalPrimitiveDrawer = new PrimitiveTrailCopy(completionRatio => FlameTrailWidthFunctionBig(npc, completionRatio),
                    completionRatio => FlameTrailColorFunctionBig(npc, completionRatio),
                    null, true, InfernumEffectsRegistry.TwinsFlameTrailVertexShader);
            }

            // Draw the flame tail if necessary.
            else if (npc.Infernum().ExtraAI[6] > 0f)
            {
                InfernumEffectsRegistry.TwinsFlameTrailVertexShader.UseImage1("Images/Misc/Perlin");

                Vector2 drawStart = npc.Center;
                Vector2 drawEnd = drawStart - (npc.Infernum().ExtraAI[7] + PiOver2).ToRotationVector2() * npc.Infernum().ExtraAI[6] / 15f * 560f;
                Vector2[] drawPositions =
                [
                    drawStart,
                    Vector2.Lerp(drawStart, drawEnd, 0.2f),
                    Vector2.Lerp(drawStart, drawEnd, 0.4f),
                    Vector2.Lerp(drawStart, drawEnd, 0.6f),
                    Vector2.Lerp(drawStart, drawEnd, 0.8f),
                    drawEnd
                ];
                npc.Infernum().OptionalPrimitiveDrawer.Draw(drawPositions, -Main.screenPosition, 70);
            }

            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            void drawInstance(Vector2 drawPosition, Color drawColor, float rotation)
            {
                Vector2 origin = texture.Size() * 0.5f / new Vector2(1f, Main.npcFrameCount[npc.type]);
                Main.spriteBatch.Draw(texture, drawPosition - Main.screenPosition, npc.frame, npc.GetAlpha(drawColor), rotation, origin, npc.scale, SpriteEffects.None, 0f);
            }

            // Draw afterimages if necessary. This must be drawn before the main instance is.
            float afterimageInterpolant = npc.Infernum().ExtraAI[AfterimageDrawInterpolantIndex];
            if (afterimageInterpolant > 0f)
            {
                for (int i = npc.oldPos.Length - 1; i >= 1; i--)
                {
                    Color afterimageColor = lightColor * (1f - i / (float)npc.oldPos.Length) * 0.6f;
                    Vector2 afterimageDrawPosition = Vector2.Lerp(npc.oldPos[i] + npc.Size * 0.5f, npc.Center, 1f - afterimageInterpolant);
                    drawInstance(afterimageDrawPosition, afterimageColor, npc.oldRot[i]);
                }
            }

            // Draw more instances with increasingly powerful additive blending to create a glow effect.
            int totalInstancesToDraw = 1;
            Color color = lightColor;
            float overdriveTimer = npc.Infernum().ExtraAI[4];
            if (!BossRushEvent.BossRushActive && overdriveTimer > 0f)
            {
                color = Color.YellowGreen;
                float fadeCompletion = Utils.GetLerpValue(0f, 60f, UniversalAttackTimer, true);
                if (overdriveTimer > 0f)
                    fadeCompletion = overdriveTimer / TwinsShield.HealTime;

                totalInstancesToDraw += (int)Lerp(1f, 20f, fadeCompletion);

                Color endColor = Color.LimeGreen;
                if (overdriveTimer > 0f)
                {
                    endColor = Color.Lerp(Color.White, Color.Green, overdriveTimer / TwinsShield.HealTime);
                    endColor.A = 0;
                }

                color = Color.Lerp(color, endColor * (4f / totalInstancesToDraw), fadeCompletion);
                color.A = 0;
            }

            for (int i = 0; i < totalInstancesToDraw; i++)
            {
                Vector2 drawOffset = (TwoPi * i / totalInstancesToDraw).ToRotationVector2() * 3f;
                drawOffset *= Lerp(0.85f, 1.2f, Sin(TwoPi * i / totalInstancesToDraw + Main.GlobalTimeWrappedHourly * 3f) * 0.5f + 0.5f);
                drawInstance(npc.Center + drawOffset, color * npc.Opacity, npc.rotation);
            }
            return false;
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frameCounter++;
            npc.frame.Y = (int)npc.frameCounter % 21 / 7 * frameHeight;

            if (PersonallyInPhase2(npc))
                npc.frame.Y += frameHeight * 3;
        }
        #endregion Frames and Drawcode

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Mods.InfernumMode.PetDialog.TwinsTip1";
            yield return n => "Mods.InfernumMode.PetDialog.TwinsFinalOptionTip";
        }
        #endregion Tips

        #region Death Effects
        public override bool CheckDead(NPC npc) => HandleDeathEffects(npc);
        #endregion Death Effects
    }
}
