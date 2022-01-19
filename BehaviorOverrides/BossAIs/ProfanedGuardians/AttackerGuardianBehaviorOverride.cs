using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
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
            FireCast,
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

            switch ((AttackGuardianAttackState)attackState)
            {
                case AttackGuardianAttackState.Phase1Charges:
                    DoBehavior_Phase1Charges(npc, target, ref attackTimer);
                    break;
                case AttackGuardianAttackState.Phase2Transition:
                    DoBehavior_Phase2Transition(npc, target, ref attackTimer);
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

            npc.ai[0] = (int)AttackGuardianAttackState.FireCast;
            attackTimer = 0f;
            npc.netUpdate = true;
        }
    }
}
