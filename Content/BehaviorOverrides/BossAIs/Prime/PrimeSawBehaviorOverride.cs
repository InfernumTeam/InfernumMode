using CalamityMod.Items.Weapons.Ranged;
using InfernumMode.Core.GlobalInstances;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.Prime.PrimeHeadBehaviorOverride;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Prime
{
    public class PrimeSawBehaviorOverride : PrimeHandBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.PrimeSaw;

        public override float PredictivenessFactor => 15.5f;

        public override Color TelegraphColor => Color.Yellow;

        public override void Load()
        {
            GlobalNPCOverrides.HitEffectsEvent += UpdateSawSound;
        }

        private void UpdateSawSound(NPC npc, ref NPC.HitInfo hit)
        {
            // Ensure that Prime's saw ends the saw sound if it's unexpectedly killed.
            if (npc.type == NPCID.PrimeSaw && npc.life <= 0)
                PrimeViceBehaviorOverride.DoBehavior_SlowSparkShrapnelMeleeCharges(npc, Main.player[npc.target]);
        }

        public override void PerformAttackBehaviors(NPC npc, PrimeAttackType attackState, Player target, float attackTimer, Vector2 cannonDirection)
        {
            if (attackState == PrimeAttackType.SynchronizedMeleeArmCharges)
            {
                PrimeViceBehaviorOverride.DoBehavior_SynchronizedMeleeArmCharges(npc, target, attackTimer);
                return;
            }
            if (attackState == PrimeAttackType.SlowSparkShrapnelMeleeCharges)
            {
                PrimeViceBehaviorOverride.DoBehavior_SlowSparkShrapnelMeleeCharges(npc, target);
                return;
            }

            int extendTime = 50;
            int arcTime = 120;
            float chargeSpeed = 22.5f;
            float arcSpeed = 10f;

            if (npc.life < npc.lifeMax * Phase2LifeRatio)
            {
                chargeSpeed += 2.7f;
                arcSpeed += 3f;
            }

            // Do more contact damage.
            npc.defDamage = 150;

            int attackCycleTime = extendTime + arcTime;
            if (attackTimer < attackCycleTime)
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

            //// Quickly attempt to saw through the target if sufficiently close.
            //if (attackTimer >= extendTime && npc.velocity.Y != 0f && Distance(target.Center.Y, npc.Center.Y) < 42f)
            //{
            //    npc.velocity = Vector2.UnitX * (target.Center.X > npc.Center.X).ToDirectionInt() * sawSpeed * 0.35f;
            //    npc.netUpdate = true;
            //}

            // Arc around, towards the target.
            if (attackTimer >= extendTime && attackTimer < attackCycleTime)
            {
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.12f);
                npc.rotation = npc.velocity.ToRotation() - PiOver2;
                if (npc.velocity.Length() > arcSpeed)
                    npc.velocity *= 0.97f;
            }

            //// Acclerate the saw.
            //if (attackTimer >= extendTime && npc.velocity.Y == 0f && Math.Abs(npc.velocity.X) < sawSpeed)
            //    npc.velocity *= 1.036f;

            // Stun the saw if it was hit.
            if (attackTimer >= extendTime && npc.justHit)
                npc.velocity *= 0.1f;
        }
    }
}
