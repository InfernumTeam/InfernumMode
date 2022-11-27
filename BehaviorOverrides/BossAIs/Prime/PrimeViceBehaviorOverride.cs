using CalamityMod.Events;
using CalamityMod.Items.Weapons.Ranged;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using static InfernumMode.BehaviorOverrides.BossAIs.Prime.PrimeHeadBehaviorOverride;

namespace InfernumMode.BehaviorOverrides.BossAIs.Prime
{
    public class PrimeViceBehaviorOverride : PrimeHandBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.PrimeVice;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public override float PredictivenessFactor => 18f;

        public override Color TelegraphColor => Color.Yellow;

        public override void PerformAttackBehaviors(NPC npc, PrimeAttackType attackState, Player target, float attackTimer, Vector2 cannonDirection)
        {
            if (attackState == PrimeAttackType.SynchronizedMeleeArmCharges)
            {
                DoBehavior_SynchronizedMeleeArmCharges(npc, target, attackTimer);
                return;
            }

            int extendTime = 50;
            int arcTime = 120;
            int attackCycleTime = extendTime + arcTime;
            float chargeSpeed = 20.5f;
            float arcSpeed = 10f;

            if (attackTimer < extendTime + arcTime)
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

            // Arc around, towards the target.
            if (attackTimer >= extendTime && attackTimer < attackCycleTime)
            {
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.12f);
                npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
                if (npc.velocity.Length() > arcSpeed)
                    npc.velocity *= 0.97f;
            }

            // Stun the vice if it was hit.
            if (attackTimer >= extendTime && npc.justHit)
                npc.velocity *= 0.1f;
        }

        public static void DoBehavior_SynchronizedMeleeArmCharges(NPC npc, Player target, float attackTimer)
        {
            // Achieve freedom and destroy the shackles that the base AI binds this hand's movement to.
            npc.ai[2] = 1f;

            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float hoverOffsetAngle = ref npc.Infernum().ExtraAI[1];
            ref float localTimer = ref npc.Infernum().ExtraAI[2];
            ref float notFirstCharge = ref npc.Infernum().ExtraAI[3];

            int chargeTime = 36;
            float hoverSpeed = 33f;
            float chargeSpeed = 24f;
            Vector2 baseHoverPosition = Main.npc[(int)npc.ai[1]].Center + ArmPositionOrdering[npc.type];
            Vector2 hoverDestination = baseHoverPosition + hoverOffsetAngle.ToRotationVector2() * new Vector2(270f, 100f);

            // Hover into position and look at the target. Once reached, reel back.
            if (attackSubstate == 0f)
            {
                // Initialize the hover offset angle for the first charge.
                if (notFirstCharge == 0f)
                    hoverOffsetAngle = npc.type == NPCID.PrimeSaw ? MathHelper.Pi : 0f;

                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(hoverDestination) * hoverSpeed, 0.1f);
                if (npc.WithinRange(hoverDestination, npc.velocity.Length() * 1.5f))
                {
                    npc.velocity = npc.SafeDirectionTo(target.Center) * -7f;
                    localTimer = 0f;
                    attackSubstate = 1f;
                    npc.netUpdate = true;
                }

                if (notFirstCharge == 1f)
                    npc.rotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;

                // Don't do damage when hovering.
                npc.damage = 0;
            }

            // Reel back and decelerate.
            if (attackSubstate == 1f)
            {
                npc.velocity *= 0.975f;

                int reelBackTime = 40;
                if (localTimer >= reelBackTime)
                {
                    SoundEngine.PlaySound(ScorchedEarth.ShootSound, npc.Center);
                    Utilities.CreateShockwave(npc.Center, 2, 90, 300, false);

                    if (notFirstCharge == 1f)
                        npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;

                    // Use the original direction on the first charge to ensure that the telegraphs don't lie to the player.
                    else
                        npc.velocity = (npc.rotation + MathHelper.PiOver2).ToRotationVector2() * chargeSpeed;

                    localTimer = 0f;
                    attackSubstate = 2f;
                    npc.netUpdate = true;
                }

                if (notFirstCharge == 1f)
                    npc.rotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;

                // Play motor revving sounds.
                if (attackTimer % 5f == 4f)
                    SoundEngine.PlaySound(SoundID.Item22, npc.Center);

                // Don't do damage when reeling back.
                npc.damage = 0;
            }

            // Charge at the target and explode once a tile is hit.
            if (attackSubstate == 2f)
            {
                if (localTimer >= chargeTime)
                {
                    attackSubstate = 0f;
                    localTimer = 0f;
                    notFirstCharge = 1f;
                    hoverOffsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                    npc.netUpdate = true;
                }

                npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
            }
            localTimer++;
        }
    }
}