using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Particles;
using InfernumMode.Assets.Sounds;
using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.Prime.PrimeHeadBehaviorOverride;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Prime
{
    public class PrimeViceBehaviorOverride : PrimeHandBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.PrimeVice;

        public override float PredictivenessFactor => 15.5f;

        public override Color TelegraphColor => Color.Yellow;

        public override void PerformAttackBehaviors(NPC npc, PrimeAttackType attackState, Player target, float attackTimer, Vector2 cannonDirection)
        {
            if (attackState == PrimeAttackType.SynchronizedMeleeArmCharges)
            {
                DoBehavior_SynchronizedMeleeArmCharges(npc, target, attackTimer);
                return;
            }
            if (attackState == PrimeAttackType.SlowSparkShrapnelMeleeCharges)
            {
                DoBehavior_SlowSparkShrapnelMeleeCharges(npc, target);
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

            // Arc around, towards the target.
            if (attackTimer >= extendTime && attackTimer < attackCycleTime)
            {
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.12f);
                npc.rotation = npc.velocity.ToRotation() - PiOver2;
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
            Vector2 hoverDestination = baseHoverPosition + hoverOffsetAngle.ToRotationVector2() * new Vector2(270f, 100f) + Vector2.UnitX * target.velocity * 25f;

            // Hover into position and look at the target. Once reached, reel back.
            if (attackSubstate == 0f)
            {
                // Initialize the hover offset angle for the first charge.
                if (notFirstCharge == 0f)
                    hoverOffsetAngle = npc.type == NPCID.PrimeSaw ? Pi : 0f;

                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(hoverDestination) * hoverSpeed, 0.1f);
                if (npc.WithinRange(hoverDestination, npc.velocity.Length() * 1.5f))
                {
                    npc.velocity = npc.SafeDirectionTo(target.Center) * -7f;
                    localTimer = 0f;
                    attackSubstate = 1f;
                    npc.netUpdate = true;
                }

                if (notFirstCharge == 1f)
                    npc.rotation = npc.AngleTo(target.Center) - PiOver2;

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

                    if (notFirstCharge == 1f)
                        npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;

                    // Use the original direction on the first charge to ensure that the telegraphs don't lie to the player.
                    else
                        npc.velocity = (npc.rotation + PiOver2).ToRotationVector2() * chargeSpeed;

                    localTimer = 0f;
                    attackSubstate = 2f;
                    npc.netUpdate = true;
                }

                if (notFirstCharge == 1f)
                    npc.rotation = npc.AngleTo(target.Center) - PiOver2;

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
                    hoverOffsetAngle = Main.rand.NextFloat(TwoPi);
                    npc.netUpdate = true;
                }

                npc.rotation = npc.velocity.ToRotation() - PiOver2;
            }
            localTimer++;
        }

        public static void DoBehavior_SlowSparkShrapnelMeleeCharges(NPC npc, Player target)
        {
            // Achieve freedom and destroy the shackles that the base AI binds this hand's movement to.
            npc.ai[2] = 1f;

            NPC head = Main.npc[(int)npc.ai[1]];

            // Do a lot of contact damage.
            npc.defDamage = 200;

            int chargeDelay = 90;
            int sparkReleaseRate = 10;
            float baseVerticalHoverOffset = 150f;
            float maxChargeSpeed = 12f;

            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float localTimer = ref npc.Infernum().ExtraAI[1];
            ref float sawSound = ref npc.localAI[3];

            float idealRotation = npc.type == NPCID.PrimeSaw ? Pi / 6f : -Pi / 6f;
            npc.rotation = npc.rotation.AngleLerp(idealRotation, 0.05f).AngleTowards(idealRotation, 0.05f);

            // At first, the arms move into a cross formation.
            if (attackSubstate == 0f)
            {
                // Don't do damage when moving into position.
                npc.damage = 0;

                Vector2 hoverDestination = head.Center + Vector2.UnitY * baseVerticalHoverOffset;
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(hoverDestination) * 16f, 0.06f);
                if (npc.WithinRange(hoverDestination, 30f))
                {
                    npc.Center = hoverDestination;
                    npc.velocity = Vector2.Zero;
                }

                npc.Center = Vector2.Lerp(npc.Center, new(head.Center.X, npc.Center.Y), 0.1f);

                if (localTimer >= chargeDelay)
                {
                    // Begin moving downward.
                    npc.velocity = Vector2.UnitY * 6f;

                    attackSubstate = 1f;
                    localTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            else
            {
                // Accelerate.
                if (npc.velocity.Y < maxChargeSpeed)
                    npc.velocity.Y *= 1.03f;

                // Move slowly towards the target.
                npc.Center = npc.Center.MoveTowards(target.Center, 8f);

                // Emit red light.
                DelegateMethods.v3_1 = Color.OrangeRed.ToVector3() * 0.76f;
                Utils.PlotTileLine(npc.Center - Vector2.UnitY * 20f, npc.Center + Vector2.UnitY * 16f, 8f, DelegateMethods.CastLight);

                // Release sparks.
                if (Main.rand.NextBool(3))
                {
                    Color sparkColor = Color.Lerp(Color.Yellow, Color.Orange, Main.rand.NextFloat(0.6f));
                    SparkParticle spark = new(npc.Center, npc.velocity.RotatedByRandom(0.78f) * Main.rand.NextFloat(1.3f, 2f), Main.rand.NextBool(), 36, 0.9f, sparkColor);
                    GeneralParticleHandler.SpawnParticle(spark);
                }

                // Release perpendicular sparks outward.
                if (Main.netMode != NetmodeID.MultiplayerClient && npc.type == NPCID.PrimeSaw && localTimer % sparkReleaseRate == sparkReleaseRate - 1f)
                {
                    int sparkID = ModContent.ProjectileType<SawSpark>();
                    Utilities.NewProjectileBetter(npc.Center, Vector2.UnitX * -7f, sparkID, SawSparkDamage, 0f);
                    Utilities.NewProjectileBetter(npc.Center, Vector2.UnitX * 7f, sparkID, SawSparkDamage, 0f);
                }

                float volumeInterpolant = Utils.GetLerpValue(195f, 150f, localTimer, true);
                bool shouldStopSawSound = volumeInterpolant <= 0f || npc.life <= 0 || !npc.active;
                if (npc.type == NPCID.PrimeSaw && sawSound == 0f && !shouldStopSawSound)
                    sawSound = SoundEngine.PlaySound(InfernumSoundRegistry.PrimeSawSound with { IsLooped = true }, npc.Center).ToFloat();

                // Update the sound telegraph's position.
                if (npc.type == NPCID.PrimeSaw && SoundEngine.TryGetActiveSound(SlotId.FromFloat(sawSound), out var t) && t.IsPlaying)
                {
                    t.Position = npc.Center;
                    t.Volume = volumeInterpolant;
                    if (shouldStopSawSound)
                        t.Stop();
                }
            }

            localTimer++;
        }
    }
}
