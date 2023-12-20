using CalamityMod;
using CalamityMod.Events;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Primitives;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.Twins.TwinsAttackSynchronizer;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Twins
{
    public class RetinazerAIClass : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.Retinazer;

        public override int? NPCTypeToDeferToForTips => NPCID.Spazmatism;

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatioThreshold,
            Phase3LifeRatioThreshold
        };

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
            Color startingColor = Color.Lerp(Color.White, Color.IndianRed, 0.25f);
            Color middleColor = Color.Lerp(Color.Red, Color.White, 0.35f);
            Color endColor = Color.Lerp(Color.Crimson, Color.White, 0.47f);
            Color color = CalamityUtils.MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * (npc.Infernum().ExtraAI[6] / 15f) * trailOpacity;
            color.A = 0;
            return color;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            // Draw even if offscreen, to ensure that the telegraph is seen.
            NPCID.Sets.MustAlwaysDraw[npc.type] = true;

            // Reset afterimage lengths.
            NPCID.Sets.TrailingMode[npc.type] = 3;
            NPCID.Sets.TrailCacheLength[npc.type] = 7;
            if (npc.oldPos.Length != NPCID.Sets.TrailCacheLength[npc.type])
            {
                npc.oldPos = new Vector2[NPCID.Sets.TrailCacheLength[npc.type]];
                npc.oldRot = new float[NPCID.Sets.TrailCacheLength[npc.type]];
            }

            Texture2D texture = TextureAssets.Npc[npc.type].Value;

            // Draw the fire trail at the back once ready.
            if (npc.Infernum().OptionalPrimitiveDrawer is null)
            {
                npc.Infernum().OptionalPrimitiveDrawer = new PrimitiveTrailCopy(completionRatio => FlameTrailWidthFunctionBig(npc, completionRatio),
                    completionRatio => FlameTrailColorFunctionBig(npc, completionRatio),
                    null, true, InfernumEffectsRegistry.TwinsFlameTrailVertexShader);
            }
            else if (npc.Infernum().ExtraAI[6] > 0f)
            {
                InfernumEffectsRegistry.TwinsFlameTrailVertexShader.UseImage1("Images/Misc/Perlin");

                Vector2 drawStart = npc.Center;
                Vector2 drawEnd = drawStart - (npc.Infernum().ExtraAI[7] + PiOver2).ToRotationVector2() * npc.Infernum().ExtraAI[6] / 15f * 560f;
                Vector2[] drawPositions = new Vector2[]
                {
                    drawStart,
                    Vector2.Lerp(drawStart, drawEnd, 0.2f),
                    Vector2.Lerp(drawStart, drawEnd, 0.4f),
                    Vector2.Lerp(drawStart, drawEnd, 0.6f),
                    Vector2.Lerp(drawStart, drawEnd, 0.8f),
                    drawEnd
                };
                npc.Infernum().OptionalPrimitiveDrawer.Draw(drawPositions, -Main.screenPosition, 70);
            }

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
                float fadeCompletion = overdriveTimer / TwinsShield.HealTime;
                totalInstancesToDraw += (int)Lerp(1f, 40f, fadeCompletion);

                Color endColor = Color.Lerp(Color.White, Color.IndianRed, overdriveTimer / TwinsShield.HealTime);
                endColor.A = 0;

                color = Color.Lerp(color, endColor * (3f / totalInstancesToDraw), fadeCompletion);
                color.A = 0;
            }

            for (int i = 0; i < totalInstancesToDraw; i++)
            {
                Vector2 drawOffset = (TwoPi * i / totalInstancesToDraw).ToRotationVector2() * 3f;
                drawOffset *= Lerp(0.85f, 1.2f, Sin(TwoPi * i / totalInstancesToDraw + Main.GlobalTimeWrappedHourly * 3f) * 0.5f + 0.5f);
                drawInstance(npc.Center + drawOffset, color, npc.rotation);
            }

            ref float telegraphDirection = ref npc.Infernum().ExtraAI[RetinazerTelegraphDirectionIndex];
            ref float telegraphOpacity = ref npc.Infernum().ExtraAI[RetinazerTelegraphOpacityIndex];
            bool validTelegraphAttack = InFinalPhase || CurrentAttackState == TwinsAttackState.FlamethrowerBurst;
            if (CurrentAttackState == TwinsAttackState.DeathAnimation && !InFinalPhase)
                telegraphOpacity = 0f;
            if (!validTelegraphAttack)
            {
                telegraphOpacity = Clamp(telegraphOpacity - 0.1f, 0f, 1f);
                telegraphDirection = npc.rotation + PiOver2;
            }

            if (telegraphOpacity > 0f)
            {
                Main.spriteBatch.SetBlendState(BlendState.Additive);

                Texture2D laserTelegraph = InfernumTextureRegistry.BloomLineSmall.Value;

                Vector2 origin = laserTelegraph.Size() * new Vector2(0.5f, 0f);
                Vector2 scaleInner = new(telegraphOpacity * 0.3f, RetinazerAimedDeathray.LaserLengthConst / laserTelegraph.Height);
                Vector2 scaleOuter = scaleInner * new Vector2(2.2f, 1f);

                Color colorOuter = Color.Lerp(Color.Red, Color.White, 0.32f);
                Color colorInner = Color.Lerp(colorOuter, Color.White, 0.75f);
                Vector2 telegraphStart = npc.Center + (npc.rotation + PiOver2).ToRotationVector2() * npc.scale * 88f;

                Main.EntitySpriteDraw(laserTelegraph, telegraphStart - Main.screenPosition, null, colorOuter, telegraphDirection - PiOver2, origin, scaleOuter, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(laserTelegraph, telegraphStart - Main.screenPosition, null, colorInner, telegraphDirection - PiOver2, origin, scaleInner, SpriteEffects.None, 0);
                Main.spriteBatch.ResetBlendState();
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

        #region Death Effects
        public override bool CheckDead(NPC npc) => HandleDeathEffects(npc);
        #endregion Death Effects
    }
}
