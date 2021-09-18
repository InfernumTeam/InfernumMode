using CalamityMod;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.EoW
{
	public class EoWHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.EaterofWorldsHead;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public const int TotalLifeAcrossWorm = 23000;
        public const int BodySegmentCount = 40;
        public const float MediumSplitLifeRatio = 0.6f;
        public const float SmallSplitLifeRatio = 0.3f;

        public override bool PreAI(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float attackTimer = ref npc.ai[0];
            ref float splitCounter = ref npc.ai[1];
            ref float segmentCount = ref npc.ai[2];
            ref float initializedFlag = ref npc.localAI[0];

            // Perform initialization logic.
            if (Main.netMode != NetmodeID.MultiplayerClient && initializedFlag == 0f)
            {
                if (segmentCount == 0f)
                    segmentCount = BodySegmentCount;

                CreateSegments(npc, (int)segmentCount, NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail);
                npc.TargetClosest(false);
                initializedFlag = 1f;
            }

            Player target = Main.player[npc.target];

            DoMovement(npc, target);

            // Split into two and two different life ratios.
            if (npc.realLife == -1)
            {
                if (lifeRatio < MediumSplitLifeRatio && splitCounter == 0f)
                    HandleSplit(npc, ref splitCounter);
                if (lifeRatio < SmallSplitLifeRatio && splitCounter == 1f)
                    HandleSplit(npc, ref splitCounter);
            }

            npc.rotation = npc.rotation.AngleLerp(npc.velocity.ToRotation() + MathHelper.PiOver2, 0.05f);
            npc.rotation = npc.rotation.AngleTowards(npc.velocity.ToRotation() + MathHelper.PiOver2, 0.15f);
            attackTimer++;

            return false;
        }

        #region AI Utility Methods

        public static void DoAttack_Despawn(NPC npc)
        {
            if (npc.timeLeft > 200)
                npc.timeLeft = 200;
            npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * 16f, 0.06f);
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public static void DoMovement(NPC npc, Player target)
        {
            Vector2 idealVelocity = npc.SafeDirectionTo(target.Center) * 14f;
            idealVelocity.Y *= 1.7f;
            npc.SimpleFlyMovement(idealVelocity, 0.125f);
        }

        public static void HandleSplit(NPC npc, ref float splitCounter)
        {
            splitCounter++;

            // Delete all segments and create two new worms.
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].realLife != npc.whoAmI)
                    continue;

                Main.npc[i].life = 0;
                Main.npc[i].checkDead();
                Main.npc[i].active = false;
            }

            int realLife = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCID.EaterofWorldsHead, 1, ai1: splitCounter, ai2: npc.ai[2] * 0.5f, Target: npc.target);
            for (int i = 0; i < Math.Pow(2D, splitCounter) - 1f; i++)
            {
                int secondWorm = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCID.EaterofWorldsHead, 1, ai1: splitCounter, ai2: npc.ai[2] * 0.5f, Target: npc.target);
                if (Main.npc.IndexInRange(secondWorm))
                    Main.npc[secondWorm].realLife = npc.whoAmI;
            }

            npc.netUpdate = true;
        }

        public static void CreateSegments(NPC npc, int segmentCount, int bodyType, int tailType)
		{
            int previousIndex = npc.whoAmI;
            for (int i = 0; i < segmentCount + 1; i++)
            {
                int nextIndex;
                if (i < segmentCount)
                    nextIndex = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, bodyType, npc.whoAmI);
                else
                    nextIndex = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, tailType, npc.whoAmI);

                // Save the behind segment.
                if (i != 0)
                    Main.npc[previousIndex].ai[0] = nextIndex;

                // The head.
                Main.npc[nextIndex].ai[2] = npc.whoAmI;

                // And the ahead segment.
                Main.npc[nextIndex].ai[1] = previousIndex;
                Main.npc[nextIndex].realLife = npc.whoAmI;

                // Mark an index based on whether it can be split at a specific split counter value.

                // Small worm split indices.
                if (i == BodySegmentCount / 4 || i == BodySegmentCount * 3 / 4)
                    Main.npc[nextIndex].ai[3] = 2f;

                // Medium worm split index.
                if (i == BodySegmentCount / 2)
                    Main.npc[nextIndex].ai[3] = 1f;

                Main.npc[previousIndex].ai[0] = nextIndex;

                // Force sync the new segment into existence.
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, nextIndex, 0f, 0f, 0f, 0);

                previousIndex = nextIndex;
            }
        }

		#endregion AI Utility Methods
	}
}
