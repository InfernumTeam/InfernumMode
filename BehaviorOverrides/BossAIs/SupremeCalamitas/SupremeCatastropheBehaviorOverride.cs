using CalamityMod.NPCs.SupremeCalamitas;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class SupremeCatastropheBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<SupremeCatastrophe>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame;

        #region AI
        public override bool PreAI(NPC npc)
        {
            SupremeCataclysmBehaviorOverride.DoAI(npc);
            return false;
        }
        #endregion AI

        #region Frames and Drawcode
        public override void FindFrame(NPC npc, int frameHeight)
        {
            ref float currentFrame = ref npc.localAI[1];
            ref float attackSpecificTimer = ref npc.Infernum().ExtraAI[5];
            ref float firingFromRight = ref npc.Infernum().ExtraAI[6];
            switch ((SupremeCataclysmBehaviorOverride.SCalBrotherAnimationType)npc.localAI[0])
            {
                case SupremeCataclysmBehaviorOverride.SCalBrotherAnimationType.HoverInPlace:
                    npc.frameCounter += 0.15;
                    if (npc.frameCounter >= 1D)
                    {
                        currentFrame = (currentFrame + 1f) % 6f;
                        npc.frameCounter = 0D;
                    }
                    break;
                case SupremeCataclysmBehaviorOverride.SCalBrotherAnimationType.AttackAnimation:
                    float slashInterpolant = Utils.InverseLerp(0f, SupremeCatastrophe.SlashCounterLimit * 2f, attackSpecificTimer + (firingFromRight != 0f ? 0f : SupremeCatastrophe.SlashCounterLimit), true);
                    currentFrame = (int)Math.Round(MathHelper.Lerp(6f, 15f, slashInterpolant));
                    break;
            }

            int xFrame = (int)currentFrame / Main.npcFrameCount[npc.type];
            int yFrame = (int)currentFrame % Main.npcFrameCount[npc.type];

            npc.frame.Width = 400;
            npc.frame.Height = 230;
            npc.frame.X = xFrame * npc.frame.Width;
            npc.frame.Y = yFrame * npc.frame.Height;
        }
        #endregion Frames and Drawcode
    }
}