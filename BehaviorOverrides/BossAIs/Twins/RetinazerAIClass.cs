using CalamityMod;
using CalamityMod.Events;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;

namespace InfernumMode.BehaviorOverrides.BossAIs.Twins
{
    public class RetinazerAIClass : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.Retinazer;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw | NPCOverrideContext.NPCFindFrame;

        public override bool PreAI(NPC npc) => TwinsAttackSynchronizer.DoAI(npc);

        public static float FlameTrailWidthFunctionBig(NPC npc, float completionRatio)
        {
            return MathHelper.SmoothStep(60f, 22f, completionRatio) * npc.Infernum().ExtraAI[6] / 15f;
        }

        public static Color FlameTrailColorFunctionBig(NPC npc, float completionRatio)
        {
            float trailOpacity = Utils.InverseLerp(0.8f, 0.27f, completionRatio, true) * Utils.InverseLerp(0f, 0.067f, completionRatio, true) * 0.9f;
            Color startingColor = Color.Lerp(Color.White, Color.IndianRed, 0.25f);
            Color middleColor = Color.Lerp(Color.Red, Color.White, 0.35f);
            Color endColor = Color.Lerp(Color.Crimson, Color.White, 0.47f);
            Color color = CalamityUtils.MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * (npc.Infernum().ExtraAI[6] / 15f) * trailOpacity;
            color.A = 0;
            return color;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.npcTexture[npc.type];
            if (npc.Infernum().OptionalPrimitiveDrawer is null)
            {
                npc.Infernum().OptionalPrimitiveDrawer = new PrimitiveTrailCopy(completionRatio => FlameTrailWidthFunctionBig(npc, completionRatio),
                    completionRatio => FlameTrailColorFunctionBig(npc, completionRatio),
                    null, true, GameShaders.Misc["Infernum:TwinsFlameTrail"]);
            }
            else if (npc.Infernum().ExtraAI[6] > 0f)
            {
                GameShaders.Misc["Infernum:TwinsFlameTrail"].UseImage("Images/Misc/Perlin");

                Vector2 drawStart = npc.Center;
                Vector2 drawEnd = drawStart - (npc.Infernum().ExtraAI[7] + MathHelper.PiOver2).ToRotationVector2() * npc.Infernum().ExtraAI[6] / 15f * 560f;
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
                spriteBatch.Draw(texture, drawPosition - Main.screenPosition, npc.frame, npc.GetAlpha(drawColor), rotation, origin, npc.scale, SpriteEffects.None, 0f);
            }

            int totalInstancesToDraw = 1;
            Color color = lightColor;
            float overdriveTimer = npc.Infernum().ExtraAI[4];
            if (!BossRushEvent.BossRushActive && overdriveTimer > 0f)
            {
                float fadeCompletion = overdriveTimer / TwinsShield.HealTime;
                totalInstancesToDraw += (int)MathHelper.Lerp(1f, 40f, fadeCompletion);

                Color endColor = Color.Lerp(Color.White, Color.IndianRed, overdriveTimer / TwinsShield.HealTime);
                endColor.A = 0;

                color = Color.Lerp(color, endColor * (3f / totalInstancesToDraw), fadeCompletion);
                color.A = 0;
            }

            for (int i = 0; i < totalInstancesToDraw; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / totalInstancesToDraw).ToRotationVector2() * 3f;
                drawOffset *= MathHelper.Lerp(0.85f, 1.2f, (float)Math.Sin(MathHelper.TwoPi * i / totalInstancesToDraw + Main.GlobalTime * 3f) * 0.5f + 0.5f);
                drawInstance(npc.Center + drawOffset, color, npc.rotation);
            }
            return false;
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frameCounter++;
            npc.frame.Y = (int)npc.frameCounter % 21 / 7 * frameHeight;

            if (TwinsAttackSynchronizer.PersonallyInPhase2(npc))
                npc.frame.Y += frameHeight * 3;
        }
    }
}
