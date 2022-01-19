using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class AttackerGuardianBehaviorOverride : NPCBehaviorOverride
    {
        public enum AttackGuardianAttackState
        {
            Phase1Charges,
            Phase2Transition,
            SpearBarrage,
            RayZap
        }

        public static int TotalRemaininGuardians =>
            NPC.AnyNPCs(ModContent.NPCType<ProfanedGuardianBoss>()).ToInt() +
            NPC.AnyNPCs(ModContent.NPCType<ProfanedGuardianBoss2>()).ToInt() +
            NPC.AnyNPCs(ModContent.NPCType<ProfanedGuardianBoss3>()).ToInt();

        public override int NPCOverrideType => ModContent.NPCType<ProfanedGuardianBoss>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            CalamityGlobalNPC.doughnutBoss = npc.whoAmI;

            // Summon the defender and healer guardian.
            if (Main.netMode != NetmodeID.MultiplayerClient && npc.localAI[1] == 0f)
            {
                NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<ProfanedGuardianBoss3>());
                NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<ProfanedGuardianBoss2>());
                npc.localAI[1] = 1f;
            }

            npc.TargetClosest();

            // Despawn if no valid target exists.
            npc.timeLeft = 3600;
            Player target = Main.player[npc.target];
            if (!target.active || target.dead)
            {
                npc.TargetClosest();
                target = Main.player[npc.target];
                if (!target.active || target.dead)
                {
                    npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y - 0.4f, -20f, 6f);
                    if (npc.timeLeft < 180)
                        npc.timeLeft = 180;
                    return false;
                }
            }

            // Don't take damage if below the second phase threshold and other guardianas are around.
            npc.dontTakeDamage = false;
            if (npc.life < npc.lifeMax * 0.75f && TotalRemaininGuardians >= 2f)
                npc.dontTakeDamage = true;

            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float shouldHandsBeInvisibleFlag = ref npc.localAI[2];

            shouldHandsBeInvisibleFlag = 0f;
            switch ((AttackGuardianAttackState)attackState)
            {
                case AttackGuardianAttackState.Phase1Charges:
                    DoBehavior_Phase1Charges(npc, target, ref attackTimer);
                    break;
                case AttackGuardianAttackState.Phase2Transition:
                    DoBehavior_Phase2Transition(npc, target, ref attackTimer);
                    break;
                case AttackGuardianAttackState.SpearBarrage:
                    DoBehavior_SpinCharge(npc, target, ref attackTimer, ref shouldHandsBeInvisibleFlag);
                    break;
            }
            attackTimer++;

            return false;
        }

        public static void DoBehavior_Phase1Charges(NPC npc, Player target, ref float attackTimer)
        {
            npc.spriteDirection = (npc.velocity.X > 0).ToDirectionInt();
            npc.damage = npc.defDamage;

            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];

            // Enter the next phase once alone.
            if (TotalRemaininGuardians <= 1f)
            {
                npc.ai[0] = (int)AttackGuardianAttackState.Phase2Transition;
                npc.velocity = npc.velocity.ClampMagnitude(0f, 23f);
                attackTimer = 0f;
                npc.netUpdate = true;
            }

            // Line up for the charge.
            if (attackSubstate == 0f)
            {
                npc.damage = 0;

                int xOffsetDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                Vector2 destination = target.Center + Vector2.UnitX * 540f * xOffsetDirection;

                float distanceFromDestination = npc.Distance(destination);
                Vector2 linearVelocityToDestination = npc.SafeDirectionTo(destination) * MathHelper.Min(15f + target.velocity.Length() * 0.5f, distanceFromDestination);
                npc.velocity = Vector2.Lerp(linearVelocityToDestination, (destination - npc.Center) / 15f, Utils.InverseLerp(180f, 420f, distanceFromDestination, true));

                // Prepare to charge.
                if (npc.WithinRange(destination, 12f + target.velocity.Length() * 0.5f))
                {
                    Main.PlaySound(SoundID.Item45, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        npc.velocity = npc.SafeDirectionTo(target.Center + target.velocity * 9f) * 19f;

                        // Release a burst of spears outward.
                        int spearBurstCount = 3;
                        float spearBurstSpread = MathHelper.ToRadians(21f);
                        if (TotalRemaininGuardians <= 2)
                        {
                            spearBurstCount += 4;
                            spearBurstSpread += MathHelper.ToRadians(10f);
                        }

                        for (int i = 0; i < spearBurstCount; i++)
                        {
                            float offsetAngle = MathHelper.Lerp(-spearBurstSpread, spearBurstSpread, i / (float)spearBurstCount);
                            Vector2 shootVelocity = npc.SafeDirectionTo(target.Center + target.velocity.Y * new Vector2(8f, 36f)).RotatedBy(offsetAngle) * 9f;
                            Utilities.NewProjectileBetter(npc.Center + shootVelocity * 2f, shootVelocity, ModContent.ProjectileType<ProfanedSpear>(), 225, 0f);
                        }

                        attackTimer = 0f;
                        attackSubstate = 1f;
                        npc.netUpdate = true;
                    }
                }
            }

            // Charge.
            if (attackSubstate == 1f)
            {
                npc.velocity *= 1.018f;
                if (attackTimer >= 50f)
                {
                    attackSubstate = 0f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoBehavior_Phase2Transition(NPC npc, Player target, ref float attackTimer)
        {
            float phase2TransitionTime = 180f;
            if (attackTimer < phase2TransitionTime)
            {
                npc.velocity *= 0.9f;

                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 45 == 0)
                {
                    float shootSpeed = BossRushEvent.BossRushActive ? 17f : 12f;
                    for (int i = 0; i < 12; i++)
                    {
                        Vector2 shootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.TwoPi * i / 11f) * shootSpeed;
                        Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<ProfanedSpear>(), 170, 0f);
                    }
                }
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == phase2TransitionTime - 45)
                {
                    NPC.NewNPC((int)npc.Center.X - 160, (int)npc.Center.Y, ModContent.NPCType<EtherealHand>(), 0, -1);
                    NPC.NewNPC((int)npc.Center.X + 160, (int)npc.Center.Y, ModContent.NPCType<EtherealHand>(), 0, 1);
                }
                return;
            }

            npc.ai[0] = (int)AttackGuardianAttackState.SpearBarrage;
            attackTimer = 0f;
            npc.netUpdate = true;
        }

        public static void DoBehavior_SpinCharge(NPC npc, Player target, ref float attackTimer, ref float shouldHandsBeInvisibleFlag)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float arcDirection = ref npc.ai[2];
            shouldHandsBeInvisibleFlag = (attackTimer > 45f).ToInt();

            // Fade out.
            if (attackTimer <= 30f)
                npc.velocity *= 0.96f;

            // Reel back.
            if (attackTimer == 30f)
            {
                npc.Center = target.Center + (MathHelper.PiOver2 * Main.rand.Next(4)).ToRotationVector2() * 600f;
                npc.velocity = -npc.SafeDirectionTo(target.Center);
                npc.rotation = npc.AngleTo(target.Center);
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                if (npc.spriteDirection == -1)
                    npc.rotation += MathHelper.Pi;

                npc.netUpdate = true;
            }

            // Move back and re-appear.
            if (attackTimer > 30f && attackTimer < 75f)
            {
                npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(1f, 6f, Utils.InverseLerp(30f, 75, attackTimer, true));
                npc.alpha = Utils.Clamp(npc.alpha - 15, 0, 255);
            }

            // Charge and fire a spear.
            if (attackTimer == 75f)
            {
                arcDirection = (Math.Cos(npc.AngleTo(target.Center)) > 0).ToDirectionInt();
                npc.velocity = npc.SafeDirectionTo(target.Center) * 24f;
                if (BossRushEvent.BossRushActive)
                    npc.velocity *= 1.5f;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 spawnPosition = npc.Center - npc.velocity.SafeNormalize(Vector2.Zero) * 40f;
                    Utilities.NewProjectileBetter(spawnPosition, npc.velocity.SafeNormalize(Vector2.Zero) * 40f, ModContent.ProjectileType<ProfanedSpear>(), 210, 0f);
                    Utilities.NewProjectileBetter(spawnPosition, npc.velocity.SafeNormalize(Vector2.Zero) * 47f, ModContent.ProjectileType<ProfanedSpear>(), 200, 0f);
                }

                npc.netUpdate = true;
            }

            // Arc around a bit.
            if (attackTimer >= 75f && attackTimer < 150f)
            {
                npc.velocity = npc.velocity.RotatedBy(arcDirection * MathHelper.TwoPi / 75f);

                if (!npc.WithinRange(target.Center, 180f))
                    npc.Center += npc.SafeDirectionTo(target.Center) * (12f + target.velocity.Length() * 0.15f);

                npc.rotation = npc.velocity.ToRotation();
                if (npc.spriteDirection == -1)
                    npc.rotation += MathHelper.Pi;

                int lightReleaseTime = 24;
                int spearReleaseTime = -1;
                if (lifeRatio < 0.65f)
                    lightReleaseTime -= 4;
                if (lifeRatio < 0.45f)
                {
                    lightReleaseTime -= 4;
                    spearReleaseTime = 50;
                }
                if (lifeRatio < 0.25f)
                    lightReleaseTime -= 5;

                // Release crystal lights when spinning.
                if (attackTimer % lightReleaseTime == 0)
                {
                    Main.PlaySound(SoundID.Item101, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 shootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.PiOver2 * Main.rand.NextFloatDirection()) * 3f;
                        int shot = Utilities.NewProjectileBetter(npc.Center + shootVelocity * 3f, shootVelocity, ModContent.ProjectileType<MagicCrystalShot>(), 160, 0f);
                        Main.projectile[shot].ai[1] = Main.rand.NextFloat();
                    }
                }

                if (Main.netMode != NetmodeID.MultiplayerClient && spearReleaseTime >= 1f && attackTimer % spearReleaseTime == 0f)
                {
                    for (int i = 0; i < 18; i++)
                    {
                        Vector2 shootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.TwoPi * i / 18f) * 19f;
                        Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<ProfanedSpear>(), 170, 0f);
                    }
                }
            }

            // Slow down and fade out again.
            if (attackTimer >= 150f)
            {
                npc.velocity *= 0.94f;
                npc.alpha = Utils.Clamp(npc.alpha + 30, 0, 255);
            }

            // Prepare for the next attack.
            if (attackTimer >= 180f)
            {
                attackTimer = 0f;
                npc.Center = target.Center - Vector2.UnitY * 400f;

                npc.ai[0] = (int)AttackGuardianAttackState.SpearBarrage;
                arcDirection = 0f;

                npc.alpha = 0;
                npc.rotation = 0f;
                npc.velocity = Vector2.Zero;
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                npc.netUpdate = true;
            }
        }
    }
}
