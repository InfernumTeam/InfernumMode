using CalamityMod;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Destroyer
{
	public class DestroyerHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.TheDestroyer;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        #region Enumerations
        public enum DestroyerAttackType
        {
            FlyAttack,
            DivingAttack,
            LaserBarrage,
            ProbeBombing,
            SuperchargedProbes
        }
        #endregion

        #region AI

        internal static readonly DestroyerAttackType[] Phase1AttackPattern = new DestroyerAttackType[]
        {
            DestroyerAttackType.FlyAttack,
        };

        internal static readonly DestroyerAttackType[] Phase2AttackPattern = new DestroyerAttackType[]
        {
            DestroyerAttackType.FlyAttack,
            DestroyerAttackType.LaserBarrage,
            DestroyerAttackType.ProbeBombing,
            DestroyerAttackType.DivingAttack,
        };

        internal static readonly DestroyerAttackType[] Phase3AttackPattern = new DestroyerAttackType[]
        {
            DestroyerAttackType.FlyAttack,
            DestroyerAttackType.DivingAttack,
            DestroyerAttackType.SuperchargedProbes,
            DestroyerAttackType.ProbeBombing,
            DestroyerAttackType.LaserBarrage,
            DestroyerAttackType.SuperchargedProbes,
            DestroyerAttackType.FlyAttack,
            DestroyerAttackType.ProbeBombing,
        };

        internal const int BodySegmentCount = 60;

        public override bool PreAI(NPC npc)
        {
            Player target = Main.player[npc.target];
            npc.damage = npc.defDamage;
            npc.dontTakeDamage = false;

            npc.alpha = Utils.Clamp(npc.alpha - 20, 0, 255);

            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool phase2 = lifeRatio < 0.75f;
            bool phase3 = lifeRatio < 0.4f;

            ref float attackTimer = ref npc.ai[2];
            ref float spawnedSegmentsFlag = ref npc.ai[3];

            if (spawnedSegmentsFlag == 0f)
            {
                SpawnDestroyerSegments(npc);
                spawnedSegmentsFlag = 1f;
                npc.netUpdate = true;
            }

            if (!target.active || target.dead || Main.dayTime)
            {
                npc.TargetClosest(false);
                target = Main.player[npc.target];
                if (!target.active || target.dead || Main.dayTime)
                {
                    npc.velocity.X *= 0.98f;
                    npc.velocity.Y += 0.22f;

                    if (npc.timeLeft > 240)
                        npc.timeLeft = 240;

                    npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                    return false;
                }
            }

            void goToNextAIState()
            {
                // You cannot use ref locals inside of a delegate context.
                // You should be able to find most important, universal locals above, anyway.
                // Any others that don't have an explicit reference above are exclusively for
                // AI state manipulation.

                npc.ai[3]++;

                DestroyerAttackType[] patternToUse = phase2 ? Phase2AttackPattern : Phase1AttackPattern;
                if (phase3)
                    patternToUse = Phase3AttackPattern;
                DestroyerAttackType nextAttackType = patternToUse[(int)(npc.ai[3] % patternToUse.Length)];

                // Going to the next AI state.
                npc.ai[1] = (int)nextAttackType;

                // Resetting the attack timer.
                npc.ai[2] = 0f;

                // And the misc ai slots.
                for (int i = 0; i < 5; i++)
                {
                    npc.Infernum().ExtraAI[i] = 0f;
                }
            }

            switch ((DestroyerAttackType)(int)npc.ai[1])
            {
                case DestroyerAttackType.FlyAttack:
                    float turnSpeed = (!npc.WithinRange(target.Center, 220f)).ToInt() * 0.039f;
                    float moveSpeed = npc.velocity.Length();

                    if (npc.WithinRange(target.Center, 285f))
                        moveSpeed *= 1.02f;
                    else if (npc.velocity.Length() > 14f)
                        moveSpeed *= 0.98f;

                    moveSpeed = MathHelper.Clamp(moveSpeed, 10f, 19f);

                    npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), turnSpeed, true) * moveSpeed;
                    npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 90f == 89f)
                    {
                        for (int i = 0; i < (phase2 ? 2 : 1); i++)
                        {
                            int probe = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCID.Probe);
                            Main.npc[probe].velocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.52f) * 12f;
                        }
                    }

                    if (attackTimer >= 420f)
                        goToNextAIState();
                    break;
                case DestroyerAttackType.DivingAttack:
                    int diveTime = 200;
                    int ascendTime = 150;
                    float maxDiveDescendSpeed = 18f;
                    float diveAcceleration = 0.3f;
                    float maxDiveAscendSpeed = 30.5f;

                    if (attackTimer < diveTime)
                    {
                        if (Math.Abs(npc.velocity.X) > 2f)
                            npc.velocity.X *= 0.97f;
                        if (npc.velocity.Y < maxDiveDescendSpeed)
                            npc.velocity.Y += diveAcceleration;
                    }
                    else if (attackTimer < diveTime + ascendTime)
                    {
                        Vector2 idealVelocity = Vector2.Lerp(Vector2.UnitY, -Vector2.UnitX * Math.Sign(target.Center.X - npc.Center.X), 0.3f) * -maxDiveAscendSpeed;

                        if (attackTimer < diveTime + ascendTime - 30f)
                            npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), MathHelper.Pi * 0.016f, true) * MathHelper.Lerp(npc.velocity.Length(), maxDiveAscendSpeed, 0.1f);

                        // Create shake effects for players.
                        Main.LocalPlayer.Infernum().CurrentScreenShakePower = Utils.InverseLerp(diveTime + ascendTime / 2, diveTime + ascendTime, attackTimer, true);
                        Main.LocalPlayer.Infernum().CurrentScreenShakePower = MathHelper.Lerp(Main.LocalPlayer.Infernum().CurrentScreenShakePower, 2f, 7f);
                        Main.LocalPlayer.Infernum().CurrentScreenShakePower *= Utils.InverseLerp(2000f, 1100f, npc.Distance(Main.LocalPlayer.Center), true);

                        if (attackTimer == diveTime + ascendTime - 15f)
                            Main.PlaySound(SoundID.DD2_ExplosiveTrapExplode, target.Center);

                        if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= diveTime + ascendTime - 30f)
                        {
                            for (int i = 0; i < 4; i++)
                                Utilities.NewProjectileBetter(npc.Center, npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.8f) * 17f, ModContent.ProjectileType<DestroyerBomb>(), 0, 0f);
                        }
                    }

                    npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

                    if (attackTimer >= diveTime + ascendTime + 40f)
                        goToNextAIState();
                    break;
                case DestroyerAttackType.LaserBarrage:
                    Vector2 destination;
                    if (attackTimer <= 90f)
                    {
                        destination = target.Center + Vector2.UnitY * 400f;
                        destination.X -= Math.Sign(target.Center.X - npc.Center.X) * 2300f;
                        if (npc.WithinRange(destination, 23f))
                        {
                            npc.velocity.X = Math.Sign(target.Center.X - npc.Center.X) * MathHelper.Lerp(17f, 12f, 1f - lifeRatio);
                            npc.velocity.Y = 11f;
                            attackTimer = 90f;
                        }
                        else
                        {
                            npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(destination) * 20f, 0.05f);
                            attackTimer--;
                        }
                    }
                    else
                        npc.velocity.Y *= 0.98f;

                    npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                    if (attackTimer >= 450f)
                        goToNextAIState();
                    break;
                case DestroyerAttackType.ProbeBombing:
                    destination = target.Center + (attackTimer * MathHelper.TwoPi / 150f).ToRotationVector2() * MathHelper.Lerp(1580f, 2700f, Utils.InverseLerp(360f, 420f, attackTimer, true));
                    npc.velocity = npc.SafeDirectionTo(destination) * MathHelper.Min(MathHelper.Lerp(31f, 15f, Utils.InverseLerp(360f, 420f, attackTimer, true)), npc.Distance(destination));
                    if (npc.WithinRange(destination, 30f))
                        npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                    else
                        npc.rotation = npc.rotation.AngleTowards((attackTimer + 7f) * MathHelper.TwoPi / 150f + MathHelper.PiOver2, 0.15f);

                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 45f == 44f)
                    {
                        int probeCount = (int)MathHelper.Lerp(1f, 3f, 1f - lifeRatio);
                        for (int i = 0; i < probeCount; i++)
                        {
                            int probe = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCID.Probe);
                            Main.npc[probe].velocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.45f) * Main.rand.NextFloat(9f, 16f);
                        }
                    }

                    if (attackTimer >= 425f)
                        goToNextAIState();
                    break;
                case DestroyerAttackType.SuperchargedProbes:
                    destination = target.Center + (attackTimer * MathHelper.TwoPi / 150f).ToRotationVector2() * MathHelper.Lerp(1580f, 2700f, Utils.InverseLerp(360f, 420f, attackTimer, true));
                    npc.velocity = npc.SafeDirectionTo(destination) * MathHelper.Min(MathHelper.Lerp(31f, 15f, Utils.InverseLerp(360f, 420f, attackTimer, true)), npc.Distance(destination));
                    if (npc.WithinRange(destination, 30f))
                        npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                    else
                        npc.rotation = npc.rotation.AngleTowards((attackTimer + 7f) * MathHelper.TwoPi / 150f + MathHelper.PiOver2, 0.15f);

                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 90f)
                    {
                        int probeCount = (int)Math.Round(MathHelper.Lerp(2f, 4f, 1f - lifeRatio));
                        for (int i = 0; i < probeCount; i++)
                        {
                            int probe = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<SuperchargedProbe>());
                            Main.npc[probe].velocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.45f) * Main.rand.NextFloat(9f, 16f);
                        }
                    }

                    if (attackTimer >= SuperchargedProbe.Lifetime + 90f)
                        goToNextAIState();
                    break;
            }

            attackTimer++;
            return false;
        }

        internal static void SpawnDestroyerSegments(NPC head)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int previousSegmentIndex = head.whoAmI;
            for (int i = 0; i < BodySegmentCount + 1; i++)
            {
                int newSegment;
                if (i >= 0 && i < BodySegmentCount)
                    newSegment = NPC.NewNPC((int)head.position.X + (head.width / 2), (int)head.position.Y + (head.height / 2), NPCID.TheDestroyerBody, head.whoAmI);
                else
                    newSegment = NPC.NewNPC((int)head.position.X + (head.width / 2), (int)head.position.Y + (head.height / 2), NPCID.TheDestroyerTail, head.whoAmI);

                Main.npc[newSegment].realLife = head.whoAmI;

                // Set the ahead segment.
                Main.npc[newSegment].ai[1] = previousSegmentIndex;
                Main.npc[previousSegmentIndex].ai[0] = newSegment;

                // And the segment number.
                Main.npc[newSegment].localAI[0] = i;

                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, newSegment, 0f, 0f, 0f, 0);

                previousSegmentIndex = newSegment;
            }
        }
        #endregion
    }
}