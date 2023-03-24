using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Events;
using CalamityMod.Items.Weapons.Ranged;
using InfernumMode.Assets.Sounds;
using InfernumMode.Content.Projectiles;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

using PlaguebringerBoss = CalamityMod.NPCs.PlaguebringerGoliath.PlaguebringerGoliath;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.PlaguebringerGoliath
{
    public class PlaguebringerGoliathBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<PlaguebringerBoss>();

        public const float Phase2LifeRatio = 0.75f;

        public const float Phase3LifeRatio = 0.3f;

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio,
            Phase3LifeRatio
        };

        #region Enumerations
        public enum PBGAttackType
        {
            Charge,
            MissileLaunch,
            PlagueVomit,
            CarpetBombing,
            ExplodingPlagueChargers,
            DroneSummoning,
            CarpetBombing2,
            CarpetBombing3,
            BombConstructors,
        }

        public enum PBGFrameType
        {
            Fly,
            Charge
        }
        #endregion Enumerations

        #region AI
        public override bool PreAI(NPC npc)
        {
            npc.damage = npc.defDamage;
            npc.defense = npc.defDefense;
            npc.dontTakeDamage = false;

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Main this mod removes a lot of debuffs, huh?
            if (target.HasBuff(ModContent.BuffType<Plague>()))
                target.ClearBuff(ModContent.BuffType<Plague>());

            // Fly away if the target is gone.
            if (!target.active || target.dead)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, -Vector2.UnitY * 29f, 0.08f);
                npc.rotation = npc.velocity.X * 0.02f;
                if (!npc.WithinRange(target.Center, 3000f))
                    npc.active = false;

                return false;
            }

            float lifeRatio = npc.life / (float)npc.lifeMax;
            float enrageFactor = 1f - lifeRatio;
            if (target.Center.Y < Main.worldSurface * 16f && !BossRushEvent.BossRushActive)
            {
                npc.Calamity().CurrentlyEnraged = true;
                enrageFactor = 1.5f;
            }
            if (BossRushEvent.BossRushActive)
                enrageFactor = 2.25f;

            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float attackDelay = ref npc.ai[2];
            ref float frameType = ref npc.localAI[0];

            // Continuously reset things.
            npc.damage = 0;
            frameType = (int)PBGFrameType.Fly;

            switch ((PBGAttackType)(int)attackType)
            {
                case PBGAttackType.Charge:
                    DoBehavior_Charge(npc, target, attackDelay < 100f, enrageFactor, ref frameType);
                    break;
                case PBGAttackType.MissileLaunch:
                    DoBehavior_MissileLaunch(npc, target, ref attackTimer, enrageFactor, ref frameType);
                    break;
                case PBGAttackType.PlagueVomit:
                    DoBehavior_PlagueVomit(npc, target, ref attackTimer, enrageFactor, ref frameType);
                    break;
                case PBGAttackType.CarpetBombing:
                    DoBehavior_CarpetBombing(npc, target, enrageFactor, ref frameType);
                    break;
                case PBGAttackType.ExplodingPlagueChargers:
                    DoBehavior_ExplodingPlagueChargers(npc, target, enrageFactor, ref frameType);
                    break;
                case PBGAttackType.DroneSummoning:
                    DoBehavior_DroneSummoning(npc, target, attackTimer);
                    break;
                case PBGAttackType.CarpetBombing2:
                    DoBehavior_CarpetBombing2(npc, target, enrageFactor, ref frameType);
                    break;
                case PBGAttackType.CarpetBombing3:
                    DoBehavior_CarpetBombing3(npc, target, enrageFactor, ref frameType, attackTimer);
                    break;
                case PBGAttackType.BombConstructors:
                    DoBehavior_BombConstructors(npc, target, ref attackTimer);
                    break;
            }

            attackDelay++;
            attackTimer++;
            return false;
        }

        #region Specific Behaviors
        public static void DoBehavior_Charge(NPC npc, Player target, bool shouldntChargeYet, float enrageFactor, ref float frameType)
        {
            int maxChargeCount = (int)Math.Ceiling(5f + enrageFactor * 1.4f);
            int chargeTime = (int)(48f - enrageFactor * 15f);
            bool canDoDiagonalCharges = enrageFactor > 0.3f;
            float chargeSpeed = enrageFactor * 12.5f + 28f;
            float hoverSpeed = enrageFactor * 6f + 19f;

            if (chargeTime < 26)
                chargeTime = 26;

            ref float chargeCount = ref npc.Infernum().ExtraAI[0];
            ref float hoverOffsetY = ref npc.Infernum().ExtraAI[1];
            ref float chargeTimer = ref npc.Infernum().ExtraAI[2];
            ref float chargeState = ref npc.Infernum().ExtraAI[3];
            ref float hoverTimer = ref npc.Infernum().ExtraAI[4];
            Vector2 hoverOffset = new((target.Center.X < npc.Center.X).ToDirectionInt() * 660f, hoverOffsetY);

            if (NPC.AnyNPCs(ModContent.NPCType<SmallDrone>()))
            {
                chargeSpeed *= 0.625f;
                chargeTime += 20;
            }

            // Do initializations.
            if (Main.netMode != NetmodeID.MultiplayerClient && chargeState == 0f)
            {
                hoverOffsetY = canDoDiagonalCharges ? -400f : 0f;
                if (chargeCount % 2 == 1)
                    hoverOffsetY = 0f;
                chargeState = 1f;
                npc.netUpdate = true;
            }

            // Hover until reaching the destination.
            if (chargeState == 1f)
            {
                bool fuckingChargeAnyway = hoverTimer > 90f;
                Vector2 hoverDestination = target.Center + hoverOffset;
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

                if (npc.WithinRange(hoverDestination, 255f) || fuckingChargeAnyway)
                {
                    npc.velocity *= 0.935f;

                    // Do the charge.
                    if (npc.WithinRange(hoverDestination, 175f) && hoverTimer > 18f && !shouldntChargeYet || fuckingChargeAnyway)
                    {
                        hoverTimer = 0f;
                        chargeState = 2f;
                        npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        npc.netUpdate = true;

                        SoundEngine.PlaySound(PlaguebringerBoss.DashSound, target.Center);
                    }
                }
                else
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, hoverSpeed / 6.5f);
                npc.Center = npc.Center.MoveTowards(hoverDestination, 12f);
                npc.rotation = npc.velocity.X * 0.0125f;
                hoverTimer++;
            }

            // Charge behavior.
            if (chargeState == 2f)
            {
                frameType = (int)PBGFrameType.Charge;

                npc.damage = npc.defDamage;
                npc.rotation = npc.velocity.ToRotation();
                if (npc.spriteDirection == -1)
                    npc.rotation += MathHelper.Pi;

                chargeTimer++;

                // Slow down before transitioning back to hovering.
                if (chargeTimer > chargeTime - 15f)
                    npc.velocity *= 0.97f;

                if (Main.netMode != NetmodeID.MultiplayerClient && chargeTimer % 6f == 5f)
                    Utilities.NewProjectileBetter(npc.Center, Main.rand.NextVector2Circular(5f, 5f), ModContent.ProjectileType<PlagueCloud>(), 170, 0f);

                if (chargeTimer >= chargeTime)
                {
                    chargeCount++;
                    hoverOffsetY = 0f;
                    chargeTimer = 0f;
                    chargeState = 0f;
                    npc.netUpdate = true;

                    if (chargeCount > maxChargeCount)
                        SelectNextAttack(npc);
                }
            }
        }

        public static void DoBehavior_MissileLaunch(NPC npc, Player target, ref float attackTimer, float enrageFactor, ref float frameType)
        {
            int attackCycleCount = 1;
            int missileShootRate = (int)(14f - enrageFactor * 6f);
            float missileShootSpeed = enrageFactor * 5f + 16f;
            ref float attackState = ref npc.Infernum().ExtraAI[0];
            ref float bombingCount = ref npc.Infernum().ExtraAI[1];
            ref float missileShootTimer = ref npc.Infernum().ExtraAI[2];
            ref float attackCycleCounter = ref npc.Infernum().ExtraAI[3];

            if (missileShootRate < 6)
                missileShootRate = 6;

            npc.defense += 16;
            frameType = (int)PBGFrameType.Fly;

            switch ((int)attackState)
            {
                // Attempt to hover near the target.
                case 0:
                    Vector2 hoverOffset = new((target.Center.X < npc.Center.X).ToDirectionInt() * 720f, -360f);
                    Vector2 hoverDestination = target.Center + hoverOffset;
                    npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 29f, 2f);
                    npc.Center = npc.Center.MoveTowards(hoverDestination, 5f);

                    // Make the attack go by way quicker once in position.
                    if (npc.WithinRange(hoverDestination, 35f))
                        attackTimer += 3f;

                    missileShootTimer = 0f;
                    if (attackTimer >= 120f)
                    {
                        attackState++;
                        attackTimer = 0f;
                    }
                    break;

                // Slow down and release a bunch of missiles.
                case 1:
                    npc.velocity = npc.velocity.MoveTowards(Vector2.Zero, 0.5f) * 0.95f;

                    missileShootTimer++;
                    if (missileShootTimer >= missileShootRate)
                    {
                        SoundEngine.PlaySound(SoundID.Item11, npc.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 abdomenPosition = npc.Center + Vector2.UnitY.RotatedBy(npc.rotation) * new Vector2(npc.spriteDirection, 1f) * 108f;
                            Vector2 shootDirection = (abdomenPosition - npc.Center).SafeNormalize(Vector2.UnitY);
                            shootDirection = shootDirection.RotateTowards(npc.SafeDirectionTo(target.Center).ToRotation(), Main.rand.NextFloat(0.74f, 1.04f));
                            Vector2 shootVelocity = shootDirection.RotatedByRandom(0.31f) * missileShootSpeed;
                            Utilities.NewProjectileBetter(abdomenPosition, shootVelocity, ModContent.ProjectileType<RedirectingPlagueMissile>(), 175, 0f);
                        }
                        missileShootTimer = 0f;
                        npc.netUpdate = true;
                    }

                    if (attackTimer >= 120f)
                    {
                        attackCycleCounter++;
                        if (attackCycleCounter >= attackCycleCount)
                            SelectNextAttack(npc);
                        else
                        {
                            attackTimer = 0f;
                            attackState = 0f;
                        }
                        npc.netUpdate = true;
                    }
                    break;
            }

            // Determine rotation.
            npc.rotation = npc.velocity.X * 0.0125f;
        }

        public static void DoBehavior_PlagueVomit(NPC npc, Player target, ref float attackTimer, float enrageFactor, ref float frameType)
        {
            int attackCycleCount = 1;
            int vomitShootRate = (int)(55f - enrageFactor * 29f);
            float vomitShootSpeed = 14f;
            ref float attackState = ref npc.Infernum().ExtraAI[0];
            ref float bombingCount = ref npc.Infernum().ExtraAI[1];
            ref float vomitShootTimer = ref npc.Infernum().ExtraAI[2];
            ref float attackCycleCounter = ref npc.Infernum().ExtraAI[3];

            if (vomitShootRate < 12)
                vomitShootRate = 12;

            npc.defense += 16;
            frameType = (int)PBGFrameType.Fly;

            switch ((int)attackState)
            {
                // Attempt to hover near the target.
                case 0:
                    Vector2 hoverOffset = new((target.Center.X < npc.Center.X).ToDirectionInt() * 420f, -360f);
                    Vector2 hoverDestination = target.Center + hoverOffset;
                    npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 21f, 1f);
                    npc.Center = npc.Center.MoveTowards(hoverDestination, 4f);

                    // Make the attack go by way quicker once in position.
                    if (npc.WithinRange(hoverDestination, 35f))
                        attackTimer += 4f;

                    vomitShootTimer = 0f;
                    if (attackTimer >= 120f)
                    {
                        attackState++;
                        attackTimer = 0f;
                    }
                    break;

                // Slow down and release a bunch of vomits.
                case 1:
                    npc.velocity = npc.velocity.MoveTowards(Vector2.Zero, 0.5f) * 0.95f;

                    vomitShootTimer++;
                    if (vomitShootTimer >= vomitShootRate && attackTimer <= 136f)
                    {
                        SoundEngine.PlaySound(SoundID.Item11, npc.Center);

                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 mouthPosition = npc.Center;
                            mouthPosition += Vector2.UnitY.RotatedBy(npc.rotation) * 18f;
                            mouthPosition -= Vector2.UnitX.RotatedBy(npc.rotation) * npc.spriteDirection * -68f;
                            Vector2 shootVelocity = (target.Center - mouthPosition).SafeNormalize(Vector2.UnitY) * vomitShootSpeed;
                            Utilities.NewProjectileBetter(mouthPosition, shootVelocity, ModContent.ProjectileType<PlagueVomit>(), 180, 0f);
                        }
                        vomitShootTimer = 0f;
                        npc.netUpdate = true;
                    }

                    if (attackTimer >= 210f)
                    {
                        attackCycleCounter++;
                        if (attackCycleCounter >= attackCycleCount)
                            SelectNextAttack(npc);
                        else
                        {
                            attackTimer = 0f;
                            attackState = 0f;
                        }
                        npc.netUpdate = true;
                    }
                    break;
            }

            // Determine rotation.
            npc.rotation = npc.velocity.X * 0.0125f;
        }

        public static void DoBehavior_CarpetBombing(NPC npc, Player target, float enrageFactor, ref float frameType)
        {
            int maxChargeCount = (int)Math.Ceiling(2f + enrageFactor * 1.1f);
            int chargeTime = (int)(68f + enrageFactor * 16f);
            float chargeSpeed = enrageFactor * 2.75f + 27.5f;
            float horizontalOffset = 750f;
            float hoverSpeed = enrageFactor * 6f + 19f;
            if (enrageFactor > 0.7f)
                horizontalOffset = 900f;
            if (enrageFactor > 1f)
                horizontalOffset = 1100f;
            if (chargeTime > 84)
                chargeTime = 84;

            ref float chargeCount = ref npc.Infernum().ExtraAI[0];
            ref float chargeTimer = ref npc.Infernum().ExtraAI[1];
            ref float chargeState = ref npc.Infernum().ExtraAI[2];
            ref float hoverTimer = ref npc.Infernum().ExtraAI[3];
            Vector2 hoverOffset = new((target.Center.X < npc.Center.X).ToDirectionInt() * horizontalOffset, -325f);

            // Do initializations.
            if (Main.netMode != NetmodeID.MultiplayerClient && chargeState == 0f)
            {
                chargeState = 1f;
                npc.netUpdate = true;
            }

            // Hover until reaching the destination.
            if (chargeState == 1f)
            {
                bool fuckingChargeAnyway = hoverTimer > 75f;
                Vector2 hoverDestination = target.Center + hoverOffset;
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

                if (npc.WithinRange(hoverDestination, 195f) || fuckingChargeAnyway)
                {
                    npc.velocity *= 0.95f;

                    // Do the charge.
                    if (npc.WithinRange(hoverDestination, 135f) && hoverTimer > 30f || fuckingChargeAnyway)
                    {
                        hoverTimer = 0f;
                        chargeState = 2f;
                        npc.velocity = Vector2.UnitX * npc.SafeDirectionTo(target.Center, Vector2.UnitX).X * chargeSpeed;
                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        npc.netUpdate = true;

                        SoundEngine.PlaySound(PlaguebringerBoss.DashSound, target.Center);
                    }
                }
                else
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, hoverSpeed / 20f);
                npc.Center = npc.Center.MoveTowards(hoverDestination, 3f);
                npc.rotation = npc.velocity.X * 0.0125f;
                hoverTimer++;
            }

            // Charge behavior.
            if (chargeState == 2f)
            {
                frameType = (int)PBGFrameType.Charge;

                npc.damage = npc.defDamage;
                npc.rotation = npc.velocity.ToRotation();
                if (npc.spriteDirection == -1)
                    npc.rotation += MathHelper.Pi;

                chargeTimer++;

                // Slow down before transitioning back to hovering.
                if (chargeTimer > chargeTime - 15f)
                    npc.velocity *= 0.97f;

                // Otherwise, release missiles.
                else if (Main.netMode != NetmodeID.MultiplayerClient && chargeTimer % 10f == 9f)
                {
                    Vector2 missileShootVelocity = new(npc.velocity.X * 0.6f, 15f);
                    missileShootVelocity += Main.rand.NextVector2Circular(1.25f, 1.25f);
                    Utilities.NewProjectileBetter(npc.Center + missileShootVelocity * 2f, missileShootVelocity, ModContent.ProjectileType<PlagueMissile>(), 180, 0f);
                }

                if (chargeTimer >= chargeTime)
                {
                    chargeCount++;
                    chargeTimer = 0f;
                    chargeState = 0f;
                    npc.netUpdate = true;

                    if (chargeCount > maxChargeCount)
                        SelectNextAttack(npc);
                }
            }
        }

        public static void DoBehavior_ExplodingPlagueChargers(NPC npc, Player target, float enrageFactor, ref float frameType)
        {
            int attackCycleCount = enrageFactor > 1f - Phase3LifeRatio ? 2 : 3;
            int chargeTime = (int)(56f - enrageFactor * 17f);
            int summonRate = (int)(24f - enrageFactor * 9f);
            float chargeSpeed = enrageFactor * 4f + 25f;
            float hoverSpeed = enrageFactor * 6f + 19f;

            ref float chargeCount = ref npc.Infernum().ExtraAI[0];
            ref float hoverOffsetY = ref npc.Infernum().ExtraAI[1];
            ref float chargeTimer = ref npc.Infernum().ExtraAI[2];
            ref float chargeState = ref npc.Infernum().ExtraAI[3];
            ref float hoverTimer = ref npc.Infernum().ExtraAI[4];
            ref float summonTimer = ref npc.Infernum().ExtraAI[5];
            Vector2 hoverOffset = new((target.Center.X < npc.Center.X).ToDirectionInt() * 470f, hoverOffsetY);

            // Do initializations.
            if (Main.netMode != NetmodeID.MultiplayerClient && chargeState == 0f)
            {
                hoverOffsetY = 400f * (chargeCount % 2f == 0f).ToDirectionInt();
                chargeState = 1f;
                summonTimer = 0f;
                npc.netUpdate = true;
            }

            // Hover until reaching the destination.
            if (chargeState == 1f)
            {
                bool fuckingChargeAnyway = hoverTimer > 25f;
                Vector2 hoverDestination = target.Center + hoverOffset;
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

                if (npc.WithinRange(hoverDestination, 255f) || hoverTimer > 50f || fuckingChargeAnyway)
                {
                    npc.velocity *= 0.935f;

                    // Do the charge.
                    if (npc.WithinRange(hoverDestination, 175f) && hoverTimer > 18f || fuckingChargeAnyway)
                    {
                        hoverTimer = 0f;
                        chargeState = 2f;
                        npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        npc.netUpdate = true;

                        SoundEngine.PlaySound(PlaguebringerBoss.DashSound, target.Center);
                    }
                }
                else
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, hoverSpeed / 7f);
                npc.Center = npc.Center.MoveTowards(hoverDestination, 12f);
                npc.rotation = npc.velocity.X * 0.0125f;
                hoverTimer++;
            }

            // Charge behavior.
            if (chargeState == 2f)
            {
                frameType = (int)PBGFrameType.Charge;

                npc.damage = npc.defDamage;
                npc.rotation = npc.velocity.ToRotation();
                if (npc.spriteDirection == -1)
                    npc.rotation += MathHelper.Pi;

                chargeTimer++;

                // Slow down before transitioning back to hovering.
                if (chargeTimer > chargeTime - 15f)
                    npc.velocity *= 0.97f;

                if (chargeTimer >= chargeTime)
                {
                    chargeCount++;
                    hoverOffsetY = 0f;
                    chargeTimer = 0f;
                    chargeState++;
                    npc.netUpdate = true;

                    if (chargeCount > attackCycleCount)
                        SelectNextAttack(npc);
                }
            }

            // Slow down and summon a bunch of explosive plague chargers.
            if (chargeState == 3f)
            {
                summonTimer++;
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                npc.velocity *= 0.95f;
                npc.rotation = npc.velocity.X * 0.0125f;

                if (summonTimer % summonRate == summonRate - 1f)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient && NPC.CountNPCS(ModContent.NPCType<ExplosivePlagueCharger>()) < 9)
                    {
                        Vector2 chargerSpawnPosition = npc.Center + Main.rand.NextVector2Unit() * new Vector2(2f, 1f) * Main.rand.NextFloat(100f, 180f);
                        NPC.NewNPC(npc.GetSource_FromAI(), (int)chargerSpawnPosition.X, (int)chargerSpawnPosition.Y, ModContent.NPCType<ExplosivePlagueCharger>());
                    }
                }

                if (summonTimer >= 120f)
                {
                    summonTimer = 0f;
                    chargeState = 0f;
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoBehavior_DroneSummoning(NPC npc, Player target, float attackTimer)
        {
            void summonDrones(int droneSummonCount, int moveIncrement, int spinDirection, float angularOffsetPerIncrement)
            {
                List<int> drones = new();
                for (int i = 0; i < droneSummonCount; i++)
                {
                    int drone = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<SmallDrone>());
                    Main.npc[drone].target = npc.target;
                    drones.Add(drone);
                }

                for (int i = 0; i < drones.Count; i++)
                {
                    Main.npc[drones[i]].ai[0] = -35f;
                    Main.npc[drones[i]].ai[1] = drones[(i + 1) % drones.Count];
                    Main.npc[drones[i]].ai[2] = MathHelper.TwoPi * (i + angularOffsetPerIncrement) / drones.Count;
                    Main.npc[drones[i]].ModNPC<SmallDrone>().SpinDirection = spinDirection;
                    Main.npc[drones[i]].ModNPC<SmallDrone>().MoveIncrement = moveIncrement;
                }
            }

            // Slow down.
            npc.velocity *= 0.97f;
            npc.rotation = npc.velocity.X * 0.0125f;

            // Summon drones once ready.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int moveIncrement = (int)Math.Round((attackTimer - 75f) / SmallDrone.TimeOffsetPerIncrement);
                if (attackTimer == 75f)
                {
                    summonDrones(4, moveIncrement, 1, 0.5f);
                    HatGirl.SayThingWhileOwnerIsAlive(target, "Those drones are building a jail around you, destroying one will give you an opening!");
                }
                if (attackTimer == SmallDrone.TimeOffsetPerIncrement + 75f)
                    summonDrones(4, moveIncrement, 1, 0f);
                if (attackTimer == SmallDrone.TimeOffsetPerIncrement * 2f + 75f)
                    summonDrones(6, moveIncrement, 1, Main.rand.NextFloat());
            }

            if (attackTimer >= SmallDrone.TimeOffsetPerIncrement * 2f + 375f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_CarpetBombing2(NPC npc, Player target, float enrageFactor, ref float frameType)
        {
            int attackCycleCount = 3;
            int chargeTime = (int)(56f - enrageFactor * 22f);
            int bombingDelay = (int)(65f - enrageFactor * 22f);
            float chargeSpeed = enrageFactor * 4f + 24f;
            float hoverSpeed = enrageFactor * 6f + 19f;

            ref float chargeCount = ref npc.Infernum().ExtraAI[0];
            ref float hoverOffsetY = ref npc.Infernum().ExtraAI[1];
            ref float chargeTimer = ref npc.Infernum().ExtraAI[2];
            ref float chargeState = ref npc.Infernum().ExtraAI[3];
            ref float hoverTimer = ref npc.Infernum().ExtraAI[4];
            ref float bombingTimer = ref npc.Infernum().ExtraAI[5];
            Vector2 hoverOffset = new((target.Center.X < npc.Center.X).ToDirectionInt() * 470f, hoverOffsetY);

            // Do initializations.
            if (Main.netMode != NetmodeID.MultiplayerClient && chargeState == 0f)
            {
                hoverOffsetY = 400f * (chargeCount % 2f == 0f).ToDirectionInt();
                chargeState = 1f;
                bombingTimer = 0f;
                Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<RedirectingPlagueMissile>());
                npc.netUpdate = true;
            }

            // Hover until reaching the destination.
            if (chargeState == 1f)
            {
                bool fuckingChargeAnyway = hoverTimer > 60f;
                Vector2 hoverDestination = target.Center + hoverOffset;
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

                if (npc.WithinRange(hoverDestination, 255f) || fuckingChargeAnyway)
                {
                    npc.velocity *= 0.935f;

                    // Do the charge.
                    if (npc.WithinRange(hoverDestination, 175f) && hoverTimer > 18f || fuckingChargeAnyway)
                    {
                        hoverTimer = 0f;
                        chargeState = 2f;
                        npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        npc.netUpdate = true;

                        SoundEngine.PlaySound(PlaguebringerBoss.DashSound, target.Center);
                    }
                }
                else
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, hoverSpeed / 7f);
                npc.Center = npc.Center.MoveTowards(hoverDestination, 12f);
                npc.rotation = npc.velocity.X * 0.0125f;
                hoverTimer++;
            }

            // Charge behavior.
            if (chargeState == 2f)
            {
                frameType = (int)PBGFrameType.Charge;

                npc.damage = npc.defDamage;
                npc.rotation = npc.velocity.ToRotation();
                if (npc.spriteDirection == -1)
                    npc.rotation += MathHelper.Pi;

                chargeTimer++;

                // Slow down before transitioning back to hovering.
                if (chargeTimer > chargeTime - 15f)
                    npc.velocity *= 0.97f;

                if (chargeTimer >= chargeTime)
                {
                    chargeCount++;
                    hoverOffsetY = 0f;
                    chargeTimer = 0f;
                    chargeState++;
                    npc.netUpdate = true;

                    if (chargeCount >= attackCycleCount)
                        SelectNextAttack(npc);
                }
            }

            // Slow down and create bomb telegraphs.
            if (chargeState == 3f)
            {
                bombingTimer++;
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                npc.velocity *= 0.95f;
                npc.rotation = npc.velocity.X * 0.0125f;

                if (bombingTimer == 1f)
                {
                    float bombRotation = Main.rand.NextBool(2).ToInt() * Main.rand.NextFloatDirection() * 0.16f;
                    for (float horizontalOffset = -1900f; horizontalOffset < 1900f; horizontalOffset += 90f)
                    {
                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(telegraph => telegraph.rotation = bombRotation + Main.rand.NextFloatDirection() * 0.022f);
                        Utilities.NewProjectileBetter(target.Center + Vector2.UnitX * horizontalOffset, Vector2.Zero, ModContent.ProjectileType<BombingTelegraph>(), 0, 0f, target.whoAmI, bombingDelay);
                    }
                }

                if (bombingTimer == bombingDelay)
                    SoundEngine.PlaySound(HandheldTank.UseSound, target.Center);

                if (bombingTimer > bombingDelay + 90f)
                {
                    bombingTimer = 0f;
                    chargeState = 0f;
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoBehavior_CarpetBombing3(NPC npc, Player target, float enrageFactor, ref float frameType, float attackTimer)
        {
            int chargeCount = 6;
            int chargeTime = (int)(130f - enrageFactor * 24f);
            float chargeSpeed = enrageFactor * 6.25f + 18f;
            float hoverSpeed = enrageFactor * 6f + 19f;

            ref float chargeCounter = ref npc.Infernum().ExtraAI[0];
            ref float chargeTimer = ref npc.Infernum().ExtraAI[1];
            ref float chargeState = ref npc.Infernum().ExtraAI[2];
            ref float hoverTimer = ref npc.Infernum().ExtraAI[3];
            ref float bombingTimer = ref npc.Infernum().ExtraAI[4];
            Vector2 hoverOffset = new((target.Center.X < npc.Center.X).ToDirectionInt() * 470f, 0f);

            // Create a wave visual effect.
            if (attackTimer == 1f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<PlagueWave>(), 0, 0f);
                SoundEngine.PlaySound(SoundID.Roar, npc.Center);
            }

            // Do initializations.
            if (Main.netMode != NetmodeID.MultiplayerClient && chargeState == 0f)
            {
                chargeState = 1f;
                bombingTimer = 0f;
                npc.netUpdate = true;
            }

            // Hover until reaching the destination.
            if (chargeState == 1f)
            {
                bool fuckingChargeAnyway = hoverTimer > 60f;
                Vector2 hoverDestination = target.Center + hoverOffset;
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

                if (npc.WithinRange(hoverDestination, 255f) || fuckingChargeAnyway)
                {
                    npc.velocity *= 0.935f;

                    // Do the charge.
                    if (npc.WithinRange(hoverDestination, 175f) && hoverTimer > 18f || fuckingChargeAnyway)
                    {
                        hoverTimer = 0f;
                        chargeState = 2f;
                        npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        npc.netUpdate = true;

                        SoundEngine.PlaySound(PlaguebringerBoss.DashSound, target.Center);
                    }
                }
                else
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, hoverSpeed / 7f);
                npc.Center = npc.Center.MoveTowards(hoverDestination, 12f);
                npc.rotation = npc.velocity.X * 0.0125f;
                hoverTimer++;
            }

            // Charge behavior.
            if (chargeState == 2f)
            {
                frameType = (int)PBGFrameType.Charge;

                // Do more contact damage than usual.
                npc.damage = (int)(npc.defDamage * 1.55);
                npc.rotation = npc.velocity.ToRotation();
                if (npc.spriteDirection == -1)
                    npc.rotation += MathHelper.Pi;

                chargeTimer++;

                // Slow down before transitioning back to hovering.
                if (chargeTimer > chargeTime - 15f)
                    npc.velocity *= 0.97f;

                if (chargeTimer > chargeTime)
                {
                    chargeState = 0f;
                    chargeTimer = 0f;
                    chargeCounter++;
                    npc.netUpdate = true;
                }

                if (chargeCounter > chargeCount)
                    SelectNextAttack(npc);
            }

            bool canReleaseBombs = attackTimer % 16f == 15f;
            if (canReleaseBombs)
            {
                SoundEngine.PlaySound(SoundID.Item45, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float bombRotation = Main.rand.NextBool(2).ToInt() * Main.rand.NextFloatDirection() * 0.14f;
                    float horizontalOffset = Main.rand.NextFloatDirection() * 580f;

                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(telegraph => telegraph.rotation = bombRotation);
                    Utilities.NewProjectileBetter(target.Center + Vector2.UnitX * horizontalOffset, Vector2.Zero, ModContent.ProjectileType<BombingTelegraph>(), 0, 0f, target.whoAmI, 40f, 1f);
                }
            }
        }

        public static void DoBehavior_BombConstructors(NPC npc, Player target, ref float attackTimer)
        {
            // Move over the target.
            Vector2 hoverDestination = target.Center - Vector2.UnitY * 420f;
            if (!npc.WithinRange(hoverDestination, 195f))
            {
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 19f, 0.7f);
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
            }
            npc.rotation = npc.velocity.X * 0.0125f;

            // Release a swarm of drones and a nuke.
            if (attackTimer == 1f)
            {
                HatGirl.SayThingWhileOwnerIsAlive(target, "Destroy those builder drones before the whole jungle goes kablooey!");

                Utilities.DisplayText("NUCLEAR CORE GENERATED. INITIATING BUILD PROCEDURE!", Color.Lime);
                SoundEngine.PlaySound(InfernumSoundRegistry.PBGMechanicalWarning, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 4; i++)
                        NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<BuilderDroneSmall>(), npc.whoAmI, Target: npc.target);
                    NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<BuilderDroneBig>(), npc.whoAmI, Target: npc.target);
                    NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y + 24, ModContent.NPCType<PlagueNuke>(), npc.whoAmI, Target: npc.target);
                }
                npc.netUpdate = true;
            }

            if (attackTimer > 10f && attackTimer < PlagueNuke.BuildTime + PlagueNuke.ExplodeDelay && !NPC.AnyNPCs(ModContent.NPCType<PlagueNuke>()))
            {
                attackTimer = PlagueNuke.BuildTime + PlagueNuke.ExplodeDelay;
                npc.netUpdate = true;
            }

            if (attackTimer > PlagueNuke.BuildTime + PlagueNuke.ExplodeDelay + 120f)
                SelectNextAttack(npc);
        }

        public static void SelectNextAttack(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            PBGAttackType currentAttackState = (PBGAttackType)(int)npc.ai[0];
            PBGAttackType newAttackState = PBGAttackType.Charge;
            switch (currentAttackState)
            {
                case PBGAttackType.Charge:
                    newAttackState = PBGAttackType.MissileLaunch;
                    if (lifeRatio < Phase3LifeRatio)
                        newAttackState = PBGAttackType.CarpetBombing3;
                    break;
                case PBGAttackType.MissileLaunch:
                    newAttackState = PBGAttackType.CarpetBombing;
                    if (Main.rand.NextBool(2) && lifeRatio < Phase2LifeRatio)
                        newAttackState = PBGAttackType.CarpetBombing2;
                    break;
                case PBGAttackType.CarpetBombing:
                case PBGAttackType.CarpetBombing2:
                case PBGAttackType.CarpetBombing3:
                    newAttackState = PBGAttackType.PlagueVomit;
                    if (lifeRatio < Phase3LifeRatio)
                        newAttackState = PBGAttackType.DroneSummoning;
                    break;
                case PBGAttackType.PlagueVomit:
                    newAttackState = PBGAttackType.Charge;
                    if (lifeRatio < Phase2LifeRatio)
                        newAttackState = PBGAttackType.DroneSummoning;
                    break;
                case PBGAttackType.DroneSummoning:
                    newAttackState = PBGAttackType.ExplodingPlagueChargers;
                    break;
                case PBGAttackType.ExplodingPlagueChargers:
                    newAttackState = PBGAttackType.Charge;
                    if (lifeRatio < Phase3LifeRatio)
                        newAttackState = PBGAttackType.BombConstructors;
                    break;
                case PBGAttackType.BombConstructors:
                    newAttackState = PBGAttackType.Charge;
                    break;
            }

            npc.TargetClosest();
            npc.ai[0] = (int)newAttackState;
            npc.ai[1] = 0f;
            for (int i = 0; i < 8; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
        }
        #endregion Specific Behaviors

        #endregion AI

        #region Frames and Drawcode

        public override void FindFrame(NPC npc, int frameHeight)
        {
            bool charging = npc.localAI[0] == (int)PBGFrameType.Charge;
            int width = !charging ? 532 / 2 : 644 / 2;
            int height = !charging ? 768 / 3 : 636 / 3;
            npc.frameCounter += charging ? 1.8f : 1f;

            if (npc.frameCounter > 4.0)
            {
                npc.frame.Y += height;
                npc.frameCounter = 0.0;
            }
            if (npc.frame.Y >= height * 3)
            {
                npc.frame.Y = 0;
                npc.frame.X = npc.frame.X == 0 ? width : 0;
            }
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            bool charging = npc.localAI[0] == (int)PBGFrameType.Charge;
            ref float previousFrameType = ref npc.localAI[1];
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Texture2D glowTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/PlaguebringerGoliath/PlaguebringerGoliathGlow").Value;
            if (charging)
            {
                texture = ModContent.Request<Texture2D>("CalamityMod/NPCs/PlaguebringerGoliath/PlaguebringerGoliathChargeTex").Value;
                glowTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/PlaguebringerGoliath/PlaguebringerGoliathChargeTexGlow").Value;
            }

            // Reset frames when frame types change.
            if (previousFrameType != npc.localAI[0])
            {
                npc.frame.X = 0;
                npc.frame.Y = 0;
                previousFrameType = npc.localAI[0];
            }

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            int frameCount = 3;
            int afterimageCount = 10;
            Rectangle frame = new(npc.frame.X, npc.frame.Y, texture.Width / 2, texture.Height / frameCount);
            Vector2 origin = frame.Size() / 2f;

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < afterimageCount; i += 2)
                {
                    Color color38 = npc.GetAlpha(Color.Lerp(lightColor, Color.White, 0.5f)) * ((afterimageCount - i) / 15f);
                    Vector2 drawPosition = npc.oldPos[i] + npc.Size * 0.5f - Main.screenPosition;
                    Main.spriteBatch.Draw(texture, drawPosition, frame, color38, npc.rotation, origin, npc.scale, spriteEffects, 0f);
                }
            }

            Vector2 baseDrawPosition = npc.Center - Main.screenPosition;
            Main.spriteBatch.Draw(texture, baseDrawPosition, frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, spriteEffects, 0f);
            Main.spriteBatch.Draw(glowTexture, baseDrawPosition, frame, npc.GetAlpha(Color.White), npc.rotation, origin, npc.scale, spriteEffects, 0f);
            return false;
        }
        #endregion Frames and Drawcode
    }
}