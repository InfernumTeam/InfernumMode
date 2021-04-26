using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;

namespace InfernumMode.FuckYouModeAIs.QueenBee
{
	public class QueenBeeAIClass
    {
		#region Enumerations
        internal enum QueenBeeAttackState
        {
            HorizontalCharge,
		}

        internal enum QueenBeeFrameType
        {
            HorizontalCharge,
            UpwardFly,
        }
        #endregion

        #region AI

        [OverrideAppliesTo(NPCID.QueenBee, typeof(QueenBeeAIClass), "QueenBeeAI", EntityOverrideContext.NPCAI, true)]
        public static bool QueenBeeAI(NPC npc)
        {
            // Select a new target if an old one was lost.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();

            // If none was found or it was too far away, despawn.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || 
                !Main.player[npc.target].active || !npc.WithinRange(Main.player[npc.target].Center, 3400f))
			{
                DoDespawnEffects(npc);
                return false;
			}

            Player target = Main.player[npc.target];

            npc.dontTakeDamage = !target.ZoneCrimson && !target.ZoneCorrupt;

            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float frameType = ref npc.localAI[0];

            switch ((QueenBeeAttackState)(int)attackType)
			{
                case QueenBeeAttackState.HorizontalCharge:
                    DoAttack_HorizontalCharge(npc, target, ref frameType, ref attackTimer);
                    break;
            }
            attackTimer++;
            return false;
		}

        #region Specific Attacks
        internal static void DoDespawnEffects(NPC npc)
		{
            npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, 17f, 0.1f);
            npc.damage = 0;
            if (npc.timeLeft > 180)
                npc.timeLeft = 180;
        }
        
        internal static void DoAttack_HorizontalCharge(NPC npc, Player target, ref float frameType, ref float attackTimer)
        {
            ref float attackState = ref npc.Infernum().ExtraAI[0];
            ref float speedBoost = ref npc.Infernum().ExtraAI[1];
            ref float totalChargesDone = ref npc.Infernum().ExtraAI[2];

            // Line up.
            if (attackState == 0f)
            {
                Vector2 destination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * 320f;
                npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * 12f, 0.35f);

                frameType = (int)QueenBeeFrameType.UpwardFly;
                if (npc.WithinRange(destination, 40f) || Math.Abs(target.Center.Y - npc.Center.Y) < 10f)
                {
                    npc.velocity = npc.SafeDirectionTo(target.Center, Vector2.UnitX) * 18.5f;
                    npc.velocity.Y *= 0.25f;
                    attackState = 1f;
                    frameType = (int)QueenBeeFrameType.HorizontalCharge;
                    npc.netUpdate = true;
                }
                npc.spriteDirection = Math.Sign(npc.velocity.X);
            }

            // Do the charge.
            else
            {
                speedBoost += 0.004f;
                npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitX) * (npc.velocity.Length() + speedBoost);

                frameType = (int)QueenBeeFrameType.HorizontalCharge;
                if (Math.Abs(npc.Center.X - target.Center.X) > 540f)
                {
                    npc.velocity *= 0.5f;
                    attackState = 0f;
                    speedBoost = 0f;
                    totalChargesDone++;
                    npc.netUpdate = true;
                }
            }

            if (totalChargesDone >= 3f)
                GotoNextAttackState(npc);
        }
        #endregion Specific Attacks

        #region AI Utility Methods

        internal const float Subphase2LifeRatio = 0.8f;
        internal const float Subphase3LifeRatio = 0.45f;
        internal const float Subphase4LifeRatio = 0.2f;
        internal static void GotoNextAttackState(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;

            QueenBeeAttackState oldAttackType = (QueenBeeAttackState)(int)npc.ai[0];
            QueenBeeAttackState newAttackType = QueenBeeAttackState.HorizontalCharge;
            switch (oldAttackType)
            {
                case QueenBeeAttackState.HorizontalCharge:
                    newAttackType = QueenBeeAttackState.HorizontalCharge;
                    break;
            }

            npc.ai[0] = (int)newAttackType;
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
        }

        #endregion AI Utility Methods

        #endregion AI

        #region Drawing and Frames

        [OverrideAppliesTo(NPCID.QueenBee, typeof(QueenBeeAIClass), "QueenBeePreDraw", EntityOverrideContext.NPCFindFrame)]
        public static void QueenBeePreDraw(NPC npc, int frameHeight)
        {
            npc.frameCounter++;
            if (npc.frameCounter % 5 == 4)
                npc.frame.Y += frameHeight;
            switch ((QueenBeeFrameType)(int)npc.localAI[0])
            {
                case QueenBeeFrameType.UpwardFly:
                    if (npc.frame.Y < frameHeight * 4)
                        npc.frame.Y = frameHeight * 4;
                    if (npc.frame.Y >= frameHeight * Main.npcFrameCount[npc.type])
                        npc.frame.Y = frameHeight * 4;
                    break;
                case QueenBeeFrameType.HorizontalCharge:
                    if (npc.frame.Y >= frameHeight * 4)
                        npc.frame.Y = 0;
                    break;
            }
        }
        #endregion
    }
}
