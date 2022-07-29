using CalamityMod;
using CalamityMod.NPCs.SupremeCalamitas;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class SepulcherHeadBehaviorOverride : NPCBehaviorOverride
    {
        public enum SepulcherAttackType
        {
            AttackDelay,
            ErraticCharges,
            PerpendicularBoneCharges,
            SoulBarrages
        }

        public const int minLength = 29;

        public const int maxLength = minLength + 1;

        public override int NPCOverrideType => ModContent.NPCType<SepulcherHead>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];
            ref float attackState = ref npc.ai[1];
            ref float attackTimer = ref npc.ai[2];
            ref float hasSummonedSegments = ref npc.Infernum().ExtraAI[5];

            if (Main.netMode != NetmodeID.MultiplayerClient && hasSummonedSegments == 0f)
            {
                SummonSegments(npc);
                hasSummonedSegments = 1f;
            }

            switch ((SepulcherAttackType)attackState)
            {
                case SepulcherAttackType.AttackDelay:
                    DoBehavior_AttackDelay(npc, target, ref attackTimer);
                    break;
                case SepulcherAttackType.ErraticCharges:
                    DoBehavior_ErraticCharges(npc, target, ref attackTimer);
                    break;
            }
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
            attackTimer++;

            return false;
        }

        public static void DoBehavior_AttackDelay(NPC npc, Player target, ref float attackTimer)
        {
            // Fade in.
            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.1f, 0f, 1f);

            float chargeInterpolant = Utils.GetLerpValue(90f, 160f, attackTimer, true);
            Vector2 idealVelocity = npc.SafeDirectionTo(target.Center) * new Vector2(7f, -13f);

            if (!npc.WithinRange(target.Center, 180f))
                idealVelocity = Vector2.Lerp(idealVelocity, npc.SafeDirectionTo(target.Center) * 18f, chargeInterpolant);

            npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.035f);

            if (attackTimer >= 210f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_ErraticCharges(NPC npc, Player target, ref float attackTimer)
        {
            int chargeCount = 3;
            int erraticMovementTime = 120;
            int chargeRedirectTime = 12;
            int chargeTime = 54;
            float chargeSpeed = 42f;
            float moveSpeed = chargeSpeed * 0.425f;
            ref float chargeCounter = ref npc.Infernum().ExtraAI[0];

            // Erratically hover around.
            if (attackTimer < erraticMovementTime)
            {
                if (attackTimer % 30f >= 25f)
                    npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center), 0.25f).SafeNormalize(Vector2.UnitY) * moveSpeed;

                if (!npc.WithinRange(target.Center, 200f))
                    npc.velocity = npc.velocity.RotatedBy(CalamityUtils.AperiodicSin(MathHelper.TwoPi * attackTimer / 100f) * 0.27f);
                return;
            }

            // Charge towards the target.
            if (attackTimer < erraticMovementTime + chargeRedirectTime)
            {
                Vector2 idealVelocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.1f);
                if (npc.velocity.Length() < chargeSpeed * 0.4f)
                    npc.velocity *= 1.32f;
                if (attackTimer == erraticMovementTime + chargeRedirectTime - 1f)
                {
                    npc.velocity = idealVelocity;
                    npc.netUpdate = true;
                }    

                return;
            }
            
            if (attackTimer >= erraticMovementTime + chargeTime)
            {
                chargeCounter++;
                attackTimer = 0f;
            }
        }

        public static void SummonSegments(NPC npc)
        {
            int previousSegment = npc.whoAmI;
            float rotationalOffset = 0f;
            float passedVar = 0f;
            for (int i = 0; i < maxLength; i++)
            {
                int lol;
                if (i >= 0 && i < minLength && i % 2 == 1)
                {
                    lol = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<SepulcherBodyEnergyBall>(), npc.whoAmI);
                    Main.npc[lol].localAI[0] += passedVar;
                    passedVar += 36f;
                }
                else if (i is >= 0 and < minLength)
                {
                    lol = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<SepulcherBody>(), npc.whoAmI);
                    Main.npc[lol].localAI[3] = i;
                }
                else
                    lol = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<SepulcherTail>(), npc.whoAmI);

                // Create arms.
                if (i >= 3 && i % 4 == 0)
                {
                    NPC segment = Main.npc[lol];
                    int arm = NPC.NewNPC(npc.GetSource_FromAI(), (int)segment.Center.X, (int)segment.Center.Y, ModContent.NPCType<SepulcherArm>(), lol);
                    if (Main.npc.IndexInRange(arm))
                    {
                        Main.npc[arm].ai[0] = lol;
                        Main.npc[arm].direction = 1;
                        Main.npc[arm].rotation = rotationalOffset;
                    }

                    rotationalOffset += MathHelper.Pi / 6f;

                    arm = NPC.NewNPC(npc.GetSource_FromAI(), (int)segment.Center.X, (int)segment.Center.Y, ModContent.NPCType<SepulcherArm>(), lol);
                    if (Main.npc.IndexInRange(arm))
                    {
                        Main.npc[arm].ai[0] = lol;
                        Main.npc[arm].direction = -1;
                        Main.npc[arm].rotation = rotationalOffset + MathHelper.Pi;
                    }

                    rotationalOffset += MathHelper.Pi / 6f;
                    rotationalOffset = MathHelper.WrapAngle(rotationalOffset);
                }

                Main.npc[lol].realLife = npc.whoAmI;
                Main.npc[lol].ai[2] = npc.whoAmI;
                Main.npc[lol].ai[1] = previousSegment;
                Main.npc[previousSegment].ai[0] = lol;
                previousSegment = lol;
            }
        }

        public static void SelectNextAttack(NPC npc)
        {
            npc.ai[2] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            switch ((SepulcherAttackType)npc.ai[1])
            {
                case SepulcherAttackType.AttackDelay:
                    npc.ai[1] = (int)SepulcherAttackType.ErraticCharges;
                    break;
                case SepulcherAttackType.ErraticCharges:
                    npc.ai[1] = (int)SepulcherAttackType.PerpendicularBoneCharges;
                    break;
                case SepulcherAttackType.PerpendicularBoneCharges:
                    npc.ai[1] = (int)SepulcherAttackType.SoulBarrages;
                    break;
                case SepulcherAttackType.SoulBarrages:
                    npc.ai[1] = (int)SepulcherAttackType.ErraticCharges;
                    break;
            }

            npc.netUpdate = true;
        }
    }
}