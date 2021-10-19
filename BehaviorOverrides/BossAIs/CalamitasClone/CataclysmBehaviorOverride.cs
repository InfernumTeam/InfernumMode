using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Calamitas;
using CalamityMod.World;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.CalamitasClone
{
    public class CataclysmBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<CalamitasRun>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        #region Enumerations
        public enum CataclysmAttackType
        {
            VerticalCharges,
            BrimstoneFireBurst
        }
        #endregion

        #region AI

        public override bool PreAI(NPC npc)
        {
            // Do targeting.
            npc.TargetClosest();
            Player target = Main.player[npc.target];

            CalamityGlobalNPC.cataclysm = npc.whoAmI;

            if (!target.active || target.dead || !npc.WithinRange(target.Center, 7200f) || CalamityGlobalNPC.calamitas == -1 || !Main.npc[CalamityGlobalNPC.calamitas].active)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * -28f, 0.08f);
                if (!npc.WithinRange(target.Center, 1450f) || CalamityGlobalNPC.calamitas == -1 || !Main.npc[CalamityGlobalNPC.calamitas].active)
                {
                    npc.life = 0;
                    npc.active = false;
                    npc.netUpdate = true;
                }
                return false;
            }

            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool shouldBeBuffed = CalamityWorld.downedProvidence && !BossRushEvent.BossRushActive && CalamitasCloneBehaviorOverride.ReadyToUseBuffedAI;
            bool otherBrotherIsPresent = NPC.AnyNPCs(ModContent.NPCType<CalamitasRun2>());
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];

            // Reset things.
            npc.damage = npc.defDamage;
            npc.dontTakeDamage = false;

            switch ((CataclysmAttackType)(int)attackType)
            {
                case CataclysmAttackType.VerticalCharges:
                    DoBehavior_HorizontalCharges(npc, target, lifeRatio, otherBrotherIsPresent, shouldBeBuffed, ref attackTimer);
                    break;
                case CataclysmAttackType.BrimstoneFireBurst:
                    npc.damage = 0;
                    DoBehavior_BrimstoneFireBurst(npc, target, lifeRatio, otherBrotherIsPresent, shouldBeBuffed, ref attackTimer);
                    break;
            }

            attackTimer++;
            return false;
        }

        public static void DoBehavior_HorizontalCharges(NPC npc, Player target, float lifeRatio, bool otherBrotherIsPresent, bool shouldBeBuffed, ref float attackTimer)
        {
            float horizontalChargeOffset = 450f;
            float redirectSpeed = 19f;
            float chargeSpeed = MathHelper.Lerp(21f, 25f, 1f - lifeRatio);
            int chargeTime = 40;
            int chargeSlowdownTime = 15;
            int chargeCount = 3;

            if (otherBrotherIsPresent)
                chargeSpeed *= 0.75f;

            if (shouldBeBuffed)
            {
                chargeSpeed *= 1.35f;
                redirectSpeed += 6f;
                chargeCount--;
            }

            ref float attackState = ref npc.Infernum().ExtraAI[0];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[1];

            switch ((int)attackState)
            {
                // Hover into position.
                case 0:
                    Vector2 hoverDestination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * horizontalChargeOffset;
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * redirectSpeed, redirectSpeed / 20f);

                    float idealRotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;
                    npc.rotation = npc.rotation.AngleLerp(idealRotation, 0.08f).AngleTowards(idealRotation, 0.15f);

                    if (attackTimer > 240f || npc.WithinRange(hoverDestination, 60f))
                    {
                        Main.PlaySound(SoundID.Roar, npc.Center, 0);
                        npc.velocity = npc.SafeDirectionTo(target.Center, -Vector2.UnitY) * chargeSpeed;
                        attackTimer = 0f;
                        attackState = 1f;
                    }
                    break;

                // Do the charge.
                case 1:
                    npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;

                    // Slow down after the charge has ended and look at the target.
                    if (attackTimer > chargeTime)
                    {
                        npc.velocity = npc.velocity.MoveTowards(Vector2.Zero, 0.1f) * 0.96f;
                        idealRotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;
                        npc.rotation = npc.rotation.AngleLerp(idealRotation, 0.08f).AngleTowards(idealRotation, 0.15f);
                    }

                    // Go to the next attack once done slowing down.
                    if (attackTimer > chargeTime + chargeSlowdownTime)
                    {
                        chargeCounter++;
                        attackTimer = 0f;
                        attackState = 0f;
                        if (chargeCounter >= chargeCount)
                            SelectNewAttack(npc);
                        npc.netUpdate = true;
                    }
                    break;
            }
        }

        public static void DoBehavior_BrimstoneFireBurst(NPC npc, Player target, float lifeRatio, bool otherBrotherIsPresent, bool shouldBeBuffed, ref float attackTimer)
        {
            int attackCycleCount = 3;
            int hoverTime = 210;
            float hoverHorizontalOffset = 485f;
            float hoverSpeed = 15f;
            float fireballSpeed = MathHelper.Lerp(6.5f, 10f, 1f - lifeRatio);
            int fireballReleaseRate = 65;
            int fireballReleaseTime = 180;

            if (otherBrotherIsPresent)
			{
                hoverHorizontalOffset += 60f;
                fireballReleaseRate += 40;
			}

            if (shouldBeBuffed)
            {
                attackCycleCount--;
                fireballReleaseRate /= 2;
                hoverSpeed += 9f;
                fireballSpeed += 8.5f;
            }

            ref float attackCycleCounter = ref npc.Infernum().ExtraAI[0];
            ref float attackSubstate = ref npc.Infernum().ExtraAI[1];

            // Attempt to hover to the side of the target.
            Vector2 hoverDestination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * hoverHorizontalOffset;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, hoverSpeed / 45f);
            npc.rotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;

            // Prepare the attack after either enough time has passed or if sufficiently close to the hover destination.
            // This is done to ensure that the attack begins once the boss is close to the target.
            if (attackSubstate == 0f && (attackTimer > hoverTime || npc.WithinRange(hoverDestination, 60f)))
            {
                attackSubstate = 1f;
                attackTimer = 0f;
                npc.netUpdate = true;
            }

            // Release fireballs.
            if (attackSubstate == 1f)
            {
                if (attackTimer % fireballReleaseRate == fireballReleaseRate - 1f)
                {
                    Main.PlaySound(SoundID.Item73, target.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int fireballDamage = shouldBeBuffed ? 340 : 145;
                        Vector2 shootVelocity = npc.SafeDirectionTo(target.Center, -Vector2.UnitY) * fireballSpeed;

                        Utilities.NewProjectileBetter(npc.Center + shootVelocity * 2f, shootVelocity, ModContent.ProjectileType<ExplodingBrimstoneFireball>(), fireballDamage, 0f);
                    }
                }

                if (attackTimer > fireballReleaseTime)
                {
                    attackTimer = 0f;
                    attackSubstate = 0f;
                    attackCycleCounter++;

                    if (attackCycleCounter > attackCycleCount)
                        SelectNewAttack(npc);
                    npc.netUpdate = true;
                }
            }
        }

        public static void SelectNewAttack(NPC npc)
        {
            List<CataclysmAttackType> possibleAttacks = new List<CataclysmAttackType>
            {
                CataclysmAttackType.BrimstoneFireBurst,
                CataclysmAttackType.VerticalCharges
            };

            if (possibleAttacks.Count > 1)
                possibleAttacks.Remove((CataclysmAttackType)(int)npc.ai[0]);

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            npc.ai[0] = (int)Main.rand.Next(possibleAttacks);
            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }
        #endregion AI
    }
}
