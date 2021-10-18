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
    public class CatastropheBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<CalamitasRun2>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        #region Enumerations
        public enum CatastropheAttackType
        {
            BrimstoneCarpetBombing,
            VerticalCharges
        }
        #endregion

        #region AI

        public const float Phase2LifeRatio = 0.7f;

        public override bool PreAI(NPC npc)
        {
            // Do targeting.
            npc.TargetClosest();
            Player target = Main.player[npc.target];

            CalamityGlobalNPC.catastrophe = npc.whoAmI;

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
            bool otherBrotherIsPresent = NPC.AnyNPCs(ModContent.NPCType<CalamitasRun>());
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];

            // Reset things.
            npc.damage = npc.defDamage;
            npc.dontTakeDamage = false;

            switch ((CatastropheAttackType)(int)attackType)
            {
                case CatastropheAttackType.BrimstoneCarpetBombing:
                    npc.damage = 0;
                    DoBehavior_BrimstoneCarpetBombing(npc, target, lifeRatio, otherBrotherIsPresent, shouldBeBuffed, ref attackTimer);
                    break;
                case CatastropheAttackType.VerticalCharges:
                    DoBehavior_VerticalCharges(npc, target, lifeRatio, otherBrotherIsPresent, shouldBeBuffed, ref attackTimer);
                    break;
            }

            attackTimer++;
            return false;
        }

        public static void DoBehavior_BrimstoneCarpetBombing(NPC npc, Player target, float lifeRatio, bool otherBrotherIsPresent, bool shouldBeBuffed, ref float attackTimer)
        {
            int redirectTime = 240;
            float redirectSpeed = 16f;
            int carpetBombTime = 115;
            int carpetBombRate = 7;
            float carpetBombSpeed = MathHelper.SmoothStep(16f, 21f, 1f - lifeRatio);
            float carpetBombChargeSpeed = MathHelper.SmoothStep(20f, 23f, 1f - lifeRatio);

            if (otherBrotherIsPresent)
            {
                carpetBombRate += 6;
                carpetBombSpeed *= 0.65f;
            }

            if (shouldBeBuffed)
            {
                redirectSpeed += 7f;
                carpetBombChargeSpeed *= 1.35f;
            }

            if (attackTimer < redirectTime)
            {
                Vector2 destination = target.Center - Vector2.UnitY * 380f;
                float idealAngle = npc.AngleTo(destination) - MathHelper.PiOver2;
                destination.X -= (target.Center.X > npc.Center.X).ToDirectionInt() * 870f;
                npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * redirectSpeed, redirectSpeed / 40f);

                npc.rotation = npc.rotation.AngleLerp(idealAngle, 0.08f);

                if (npc.WithinRange(destination, 24f) && attackTimer < redirectTime - 75f)
                {
                    attackTimer = redirectTime - 75f;
                    npc.netUpdate = true;
                }

                // Create brimstone dust.
                if (attackTimer > redirectTime - 75f)
                {
                    Dust shadowflame = Dust.NewDustPerfect(npc.Center + (npc.rotation + MathHelper.PiOver2).ToRotationVector2() * 45f, 267);
                    shadowflame.color = Color.Red;
                    shadowflame.velocity = (npc.rotation + MathHelper.PiOver2 + Main.rand.NextFloat(-0.5f, 0.5f)).ToRotationVector2();
                    shadowflame.velocity *= Main.rand.NextFloat(2f, 5f);
                    shadowflame.scale *= 1.2f;
                    shadowflame.noGravity = true;
                }
            }

            if (attackTimer == redirectTime)
            {
                Vector2 flyVelocity = npc.SafeDirectionTo(target.Center);
                flyVelocity.Y *= 0.1f;
                flyVelocity = flyVelocity.SafeNormalize(Vector2.UnitX * (npc.velocity.X > 0).ToDirectionInt());
                npc.velocity = flyVelocity * carpetBombChargeSpeed;
                npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;

                // Roar and begin carpet bombing.
                Main.PlaySound(SoundID.Roar, (int)npc.position.X, (int)npc.position.Y, 0, 1f, 0f);
            }

            if (attackTimer > redirectTime)
                npc.velocity = (npc.rotation + MathHelper.PiOver2).ToRotationVector2() * carpetBombChargeSpeed;

            if (attackTimer > redirectTime && attackTimer % carpetBombRate == carpetBombRate - 1)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 spawnPosition = npc.Center + npc.velocity.SafeNormalize(Vector2.Zero) * 120f;
                    Vector2 shootVelocity = npc.velocity.SafeNormalize((npc.rotation + MathHelper.PiOver2).ToRotationVector2()) * carpetBombSpeed;
                    Utilities.NewProjectileBetter(spawnPosition, shootVelocity, ModContent.ProjectileType<BrimstoneBomb>(), 140, 0f);
                }
                Main.PlaySound(SoundID.DD2_BetsyFireballShot, target.Center);
            }

            if (attackTimer > redirectTime + carpetBombTime)
                SelectNewAttack(npc);
        }

        public static void DoBehavior_VerticalCharges(NPC npc, Player target, float lifeRatio, bool otherBrotherIsPresent, bool shouldBeBuffed, ref float attackTimer)
        {
            float verticalChargeOffset = 380f;
            float redirectSpeed = 17f;
            float chargeSpeed = MathHelper.Lerp(19.5f, 24f, 1f - lifeRatio);
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
                    Vector2 hoverDestination = target.Center + Vector2.UnitY * (target.Center.Y < npc.Center.X).ToDirectionInt() * verticalChargeOffset;
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * redirectSpeed, redirectSpeed / 40f);

                    float idealRotation = npc.AngleTo(hoverDestination) - MathHelper.PiOver2;
                    if (npc.WithinRange(hoverDestination, 250f))
                        idealRotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;

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

        public static void SelectNewAttack(NPC npc)
        {
            List<CatastropheAttackType> possibleAttacks = new List<CatastropheAttackType>
            {
                CatastropheAttackType.BrimstoneCarpetBombing,
                CatastropheAttackType.VerticalCharges
            };

            if (possibleAttacks.Count > 1)
                possibleAttacks.Remove((CatastropheAttackType)(int)npc.ai[0]);

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            npc.ai[0] = (int)Main.rand.Next(possibleAttacks);
            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }
        #endregion AI
    }
}
