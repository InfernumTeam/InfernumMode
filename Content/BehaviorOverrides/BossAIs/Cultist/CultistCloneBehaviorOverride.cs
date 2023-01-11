using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Cultist
{
    public class CultistCloneBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.CultistBossClone;

        #region AI

        public override bool PreAI(NPC npc)
        {
            int mainCultist = (int)npc.Infernum().ExtraAI[0];

            // Disappear if a main cultist does not exist.
            if (!Main.npc.IndexInRange(mainCultist) || !Main.npc[mainCultist].active)
            {
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            bool phase2 = Main.npc[mainCultist].ai[2] >= 2f;
            bool fadingOut = Main.npc[mainCultist].ai[1] >= 30 + CultistRitual.GetWaitTime(phase2);
            ref float phaseState = ref npc.ai[2];
            ref float transitionTimer = ref npc.ai[3];

            // Create an eye effect, sans-style.
            if (phaseState == 1f && transitionTimer >= CultistBehaviorOverride.TransitionAnimationTime + 8f || phase2)
                CultistBehaviorOverride.DoEyeEffect(npc);

            // Don't fade in completely. A small amount of translucency should remain for the sake of being able to discern
            // clones from the main boss.
            if (npc.Opacity > 0.325f)
                npc.Opacity = 0.325f;

            npc.target = Main.npc[mainCultist].target;
            npc.life = Main.npc[mainCultist].life;
            npc.lifeMax = Main.npc[mainCultist].lifeMax;
            npc.dontTakeDamage = Main.npc[mainCultist].dontTakeDamage;
            npc.ai[2] = Main.npc[mainCultist].ai[2];
            npc.ai[3] = Main.npc[mainCultist].ai[3];
            npc.localAI[0] = Main.npc[mainCultist].localAI[0];
            npc.damage = 0;

            if (fadingOut)
            {
                npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.1f, 0f, 1f);
                if (npc.Opacity <= 0f)
                    npc.active = false;
            }
            else
            {
                npc.Opacity = Main.npc[mainCultist].Opacity;
                if (npc.justHit)
                {
                    Main.npc[mainCultist].ai[1] = 30 + CultistRitual.GetWaitTime(phase2);
                    Main.npc[mainCultist].netUpdate = true;
                }
            }

            return false;
        }

        #endregion AI

        #region Drawing and Frames

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            CultistBehaviorOverride.ExtraDrawcode(npc);
            return true;
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            int frameCount = Main.npcFrameCount[npc.type];
            switch ((CultistBehaviorOverride.CultistFrameState)(int)npc.localAI[0])
            {
                case CultistBehaviorOverride.CultistFrameState.AbsorbEffect:
                    npc.frame.Y = (int)(npc.frameCounter / 5) * frameHeight;
                    if (npc.frameCounter >= 18)
                        npc.frameCounter = 18;
                    break;

                case CultistBehaviorOverride.CultistFrameState.Hover:
                    npc.frame.Y = (int)(4 + npc.frameCounter / 5) * frameHeight;
                    if (npc.frameCounter >= 14)
                        npc.frameCounter = 0;
                    break;

                case CultistBehaviorOverride.CultistFrameState.RaiseArmsUp:
                    npc.frame.Y = (int)(frameCount - 9 + npc.frameCounter / 5) * frameHeight;
                    if (npc.frameCounter >= 14)
                        npc.frameCounter = 0;
                    break;

                case CultistBehaviorOverride.CultistFrameState.HoldArmsOut:
                    npc.frame.Y = (int)(frameCount - 6 + npc.frameCounter / 5) * frameHeight;
                    if (npc.frameCounter >= 14)
                        npc.frameCounter = 0;
                    break;

                case CultistBehaviorOverride.CultistFrameState.Laugh:
                    npc.frame.Y = (int)(frameCount - 3 + npc.frameCounter / 5) * frameHeight;
                    if (npc.frameCounter >= 14)
                        npc.frameCounter = 0;
                    break;
            }

            npc.frameCounter++;
        }
        #endregion Drawing and Frames
    }
}
