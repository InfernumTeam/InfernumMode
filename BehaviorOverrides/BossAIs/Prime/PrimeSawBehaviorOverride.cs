using CalamityMod.Items.Weapons.Ranged;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using static InfernumMode.BehaviorOverrides.BossAIs.Prime.PrimeHeadBehaviorOverride;

namespace InfernumMode.BehaviorOverrides.BossAIs.Prime
{
    public class PrimeSawBehaviorOverride : PrimeHandBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.PrimeSaw;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw | NPCOverrideContext.NPCCheckDead;

        public override float PredictivenessFactor => 15.5f;

        public override Color TelegraphColor => Color.Yellow;
        
        public override void PerformAttackBehaviors(NPC npc, PrimeAttackType attackState, Player target, float attackTimer, bool pissed, Vector2 cannonDirection)
        {
            if (attackState == PrimeAttackType.SynchronizedMeleeArmCharges)
            {
                PrimeViceBehaviorOverride.DoBehavior_SynchronizedMeleeArmCharges(npc, target, pissed, attackTimer);
                return;
            }
            if (attackState == PrimeAttackType.SlowSparkShrapnelMeleeCharges)
            {
                PrimeViceBehaviorOverride.DoBehavior_SlowSparkShrapnelMeleeCharges(npc, target, pissed);
                return;
            }

            int extendTime = 20;
            int sawTime = 150;
            float chargeSpeed = 18f;
            float sawSpeed = 29f;

			if (npc.life < npc.lifeMax * Phase2LifeRatio && !pissed)
			{
                chargeSpeed += 2.7f;
                sawSpeed += 3f;
			}

			if (pissed)
            {
                extendTime -= 2;
                sawTime -= 66;
				chargeSpeed += 4f;
                sawSpeed += 6f;
            }

            // Do more contact damage.
            npc.defDamage = 150;

            if (attackTimer < extendTime + sawTime)
                npc.ai[2] = 1f;
            else
                npc.damage = 0;

            // Extend outward.
            if (attackTimer == 1f)
            {
                SoundEngine.PlaySound(ScorchedEarth.ShootSound, npc.Center);
                npc.velocity = cannonDirection * chargeSpeed;
                npc.netUpdate = true;
            }
            
            // Quickly attempt to saw through the target if sufficiently close.
            if (attackTimer >= extendTime && npc.velocity.Y != 0f && MathHelper.Distance(target.Center.Y, npc.Center.Y) < 42f && !pissed)
            {
                npc.velocity = Vector2.UnitX * (target.Center.X > npc.Center.X).ToDirectionInt() * sawSpeed * 0.35f;
                npc.netUpdate = true;
            }

            // Acclerate the saw.
            if (attackTimer >= extendTime && npc.velocity.Y == 0f && Math.Abs(npc.velocity.X) < sawSpeed && !pissed)
                npc.velocity *= 1.036f;

            // Stun the saw if it was hit.
            if (attackTimer >= extendTime && npc.justHit)
                npc.velocity *= 0.1f;
        }

        public override bool CheckDead(NPC npc)
        {
            if (SoundEngine.TryGetActiveSound(SlotId.FromFloat(npc.Infernum().ExtraAI[2]), out var t) && t.IsPlaying)
                t.Stop();

            return true;
        }
    }
}