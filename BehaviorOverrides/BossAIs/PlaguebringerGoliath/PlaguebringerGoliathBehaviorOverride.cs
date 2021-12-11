using CalamityMod;
using CalamityMod.Events;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using PlaguebringerBoss = CalamityMod.NPCs.PlaguebringerGoliath.PlaguebringerGoliath;

namespace InfernumMode.BehaviorOverrides.BossAIs.PlaguebringerGoliath
{
	public class PlaguebringerGoliathBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<PlaguebringerBoss>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw | NPCOverrideContext.NPCFindFrame;

        public const float Phase2LifeRatio = 0.75f;
        public const float Phase3LifeRatio = 0.3f;

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
            npc.dontTakeDamage = false;

            npc.TargetClosest();

            Player target = Main.player[npc.target];

            // Fly away if the target is gone.
            if (!target.active || target.dead)
			{
                npc.velocity = Vector2.Lerp(npc.velocity, -Vector2.UnitY * 24f, 0.025f);
                npc.rotation = npc.velocity.X * 0.02f;
                if (!npc.WithinRange(target.Center, 4200f))
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
            ref float frameType = ref npc.localAI[0];

            // Continuously reset things.
            npc.damage = 0;
            frameType = (int)PBGFrameType.Fly;

            switch ((PBGAttackType)(int)attackType)
            {
                case PBGAttackType.Charge:
                    DoBehavior_Charge(npc, target, enrageFactor, ref frameType);
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
                    DoBehavior_DroneSummoning(npc, target, enrageFactor, attackTimer);
                    break;
                case PBGAttackType.CarpetBombing2:
                    DoBehavior_CarpetBombing2(npc, target, enrageFactor, ref frameType);
                    break;
                case PBGAttackType.CarpetBombing3:
                    DoBehavior_CarpetBombing3(npc, target, enrageFactor, ref frameType, attackTimer);
                    break;
                case PBGAttackType.BombConstructors:
                    DoBehavior_BombConstructors(npc, target, enrageFactor, ref attackTimer);
                    break;
            }

            attackTimer++;
            return false;
        }

        #region Specific Behaviors
        public static void DoBehavior_Charge(NPC npc, Player target, float enrageFactor, ref float frameType)
        {
            int maxChargeCount = (int)Math.Ceiling(5f + enrageFactor * 1.4f);
            int chargeTime = (int)(48f - enrageFactor * 13f);
            bool canDoDiagonalCharges = enrageFactor > 0.3f;
            float chargeSpeed = enrageFactor * 11f + 24f;
            float hoverSpeed = enrageFactor * 6f + 19f;

            if (chargeTime < 28)
                chargeTime = 28;

            ref float chargeCount = ref npc.Infernum().ExtraAI[0];
            ref float hoverOffsetY = ref npc.Infernum().ExtraAI[1];
            ref float chargeTimer = ref npc.Infernum().ExtraAI[2];
            ref float chargeState = ref npc.Infernum().ExtraAI[3];
            ref float hoverTimer = ref npc.Infernum().ExtraAI[4];
            Vector2 hoverOffset = new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 600f, hoverOffsetY);

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
                Vector2 hoverDestination = target.Center + hoverOffset;
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

                if (npc.WithinRange(hoverDestination, 255f))
                {
                    npc.velocity *= 0.935f;

                    // Do the charge.
                    if (npc.WithinRange(hoverDestination, 175f) && hoverTimer > 18f)
                    {
                        hoverTimer = 0f;
                        chargeState = 2f;
                        npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        npc.netUpdate = true;

                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/PlagueSounds/PBGDash"), target.Center);
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

                if (chargeTimer >= chargeTime)
                {
                    chargeCount++;
                    hoverOffsetY = 0f;
                    chargeTimer = 0f;
                    chargeState = 0f;
                    npc.netUpdate = true;

                    if (chargeCount > maxChargeCount)
                        GotoNextAttackState(npc);
                }
            }
        }

        public static void DoBehavior_MissileLaunch(NPC npc, Player target, ref float attackTimer, float enrageFactor, ref float frameType)
        {
            int attackCycleCount = enrageFactor > 1f - Phase3LifeRatio ? 1 : 2;
            int missileShootRate = (int)(14f - enrageFactor * 6f);
            float missileShootSpeed = enrageFactor * 5f + 16f;
            ref float attackState = ref npc.Infernum().ExtraAI[0];
            ref float bombingCount = ref npc.Infernum().ExtraAI[1];
            ref float missileShootTimer = ref npc.Infernum().ExtraAI[2];
            ref float attackCycleCounter = ref npc.Infernum().ExtraAI[3];

            if (missileShootRate < 6)
                missileShootRate = 6;

            frameType = (int)PBGFrameType.Fly;
            switch ((int)attackState)
			{
                // Attempt to hover near the target.
                case 0:
                    Vector2 hoverOffset = new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 420f, -360f);
                    Vector2 hoverDestination = target.Center + hoverOffset;
                    npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 21f, 1f);
                    npc.Center = npc.Center.MoveTowards(hoverDestination, 4f);

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
                        Main.PlaySound(SoundID.Item11, npc.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
						{
                            Vector2 abdomenPosition = npc.Center + Vector2.UnitY.RotatedBy(npc.rotation) * new Vector2(npc.spriteDirection, 1f) * 108f;
                            Vector2 shootDirection = (abdomenPosition - npc.Center).SafeNormalize(Vector2.UnitY);
                            shootDirection = shootDirection.RotateTowards(npc.SafeDirectionTo(target.Center).ToRotation(), Main.rand.NextFloat(0.74f, 1.04f));
                            Vector2 shootVelocity = shootDirection.RotatedByRandom(0.31f) * missileShootSpeed;
                            Utilities.NewProjectileBetter(abdomenPosition, shootVelocity, ModContent.ProjectileType<RedirectingPlagueMissile>(), 160, 0f);
                        }
                        missileShootTimer = 0f;
                        npc.netUpdate = true;
                    }

                    if (attackTimer >= 120f)
					{
                        attackCycleCounter++;
                        if (attackCycleCounter >= attackCycleCount)
                            GotoNextAttackState(npc);
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
            int attackCycleCount = enrageFactor > 1f - Phase3LifeRatio ? 1 : 2;
            int vomitShootRate = (int)(55f - enrageFactor * 25f);
            float vomitShootSpeed = 11f;
            ref float attackState = ref npc.Infernum().ExtraAI[0];
            ref float bombingCount = ref npc.Infernum().ExtraAI[1];
            ref float vomitShootTimer = ref npc.Infernum().ExtraAI[2];
            ref float attackCycleCounter = ref npc.Infernum().ExtraAI[3];

            if (vomitShootRate < 15)
                vomitShootRate = 15;

            frameType = (int)PBGFrameType.Fly;
            switch ((int)attackState)
            {
                // Attempt to hover near the target.
                case 0:
                    Vector2 hoverOffset = new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 420f, -360f);
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
                    if (vomitShootTimer >= vomitShootRate)
                    {
                        Main.PlaySound(SoundID.Item11, npc.Center);

                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 mouthPosition = npc.Center;
                            mouthPosition += Vector2.UnitY.RotatedBy(npc.rotation) * 18f;
                            mouthPosition -= Vector2.UnitX.RotatedBy(npc.rotation) * npc.spriteDirection * -68f;
                            Vector2 shootVelocity = (target.Center - mouthPosition).SafeNormalize(Vector2.UnitY) * vomitShootSpeed;
                            Utilities.NewProjectileBetter(mouthPosition, shootVelocity, ModContent.ProjectileType<PlagueVomit>(), 160, 0f);
                        }
                        vomitShootTimer = 0f;
                        npc.netUpdate = true;
                    }

                    if (attackTimer >= 180f)
                    {
                        attackCycleCounter++;
                        if (attackCycleCounter >= attackCycleCount)
                            GotoNextAttackState(npc);
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
            Vector2 hoverOffset = new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * horizontalOffset, -325f);

            // Do initializations.
            if (Main.netMode != NetmodeID.MultiplayerClient && chargeState == 0f)
            {
                chargeState = 1f;
                npc.netUpdate = true;
            }

            // Hover until reaching the destination.
            if (chargeState == 1f)
            {
                Vector2 hoverDestination = target.Center + hoverOffset;
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

                if (npc.WithinRange(hoverDestination, 195f) || hoverTimer >= 75f)
                {
                    npc.velocity *= 0.95f;

                    // Do the charge.
                    if (npc.WithinRange(hoverDestination, 135f) && hoverTimer > 30f)
                    {
                        hoverTimer = 0f;
                        chargeState = 2f;
                        npc.velocity = Vector2.UnitX * npc.SafeDirectionTo(target.Center, Vector2.UnitX).X * chargeSpeed;
                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        npc.netUpdate = true;

                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/PlagueSounds/PBGDash"), target.Center);
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
				else if (Main.netMode != NetmodeID.MultiplayerClient && chargeTimer % 12f == 11f)
				{
                    Vector2 missileShootVelocity = new Vector2(npc.velocity.X * 0.6f, 12f);
                    Utilities.NewProjectileBetter(npc.Center + missileShootVelocity * 2f, missileShootVelocity, ModContent.ProjectileType<PlagueMissile>(), 160, 0f);
				}

                if (chargeTimer >= chargeTime)
                {
                    chargeCount++;
                    chargeTimer = 0f;
                    chargeState = 0f;
                    npc.netUpdate = true;

                    if (chargeCount > maxChargeCount)
                        GotoNextAttackState(npc);
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
            Vector2 hoverOffset = new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 470f, hoverOffsetY);

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
                Vector2 hoverDestination = target.Center + hoverOffset;
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

                if (npc.WithinRange(hoverDestination, 255f) || hoverTimer > 50f)
                {
                    npc.velocity *= 0.935f;

                    // Do the charge.
                    if (npc.WithinRange(hoverDestination, 175f) && hoverTimer > 18f)
                    {
                        hoverTimer = 0f;
                        chargeState = 2f;
                        npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        npc.netUpdate = true;

                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/PlagueSounds/PBGDash"), target.Center);
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
                        GotoNextAttackState(npc);
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
                        NPC.NewNPC((int)chargerSpawnPosition.X, (int)chargerSpawnPosition.Y, ModContent.NPCType<ExplosivePlagueCharger>());
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

        public static void DoBehavior_DroneSummoning(NPC npc, Player target, float enrageFactor, float attackTimer)
		{
            int droneSummonCount = 4;

            // Slow down.
            npc.velocity *= 0.97f;
            npc.rotation = npc.velocity.X * 0.0125f;

            // Summon drones once ready.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 75f)
			{
                List<int> drones = new List<int>();
                for (int i = 0; i < droneSummonCount; i++)
				{
                    int drone = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<SmallDrone>());
                    Main.npc[drone].target = npc.target;
                    drones.Add(drone);
                }

                for (int i = 0; i < drones.Count; i++)
				{
                    Main.npc[drones[i]].ai[0] = -35f;
                    Main.npc[drones[i]].ai[1] = drones[(i + 1) % drones.Count];
                    Main.npc[drones[i]].ai[2] = MathHelper.TwoPi * (i + 0.5f) / drones.Count;
				}
            }

            if (attackTimer >= SmallDrone.LaserAttackTime + 125f)
                GotoNextAttackState(npc);
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
            Vector2 hoverOffset = new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 470f, hoverOffsetY);

            // Do initializations.
            if (Main.netMode != NetmodeID.MultiplayerClient && chargeState == 0f)
            {
                hoverOffsetY = 400f * (chargeCount % 2f == 0f).ToDirectionInt();
                chargeState = 1f;
                bombingTimer = 0f;
                npc.netUpdate = true;
            }

            // Hover until reaching the destination.
            if (chargeState == 1f)
            {
                Vector2 hoverDestination = target.Center + hoverOffset;
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

                if (npc.WithinRange(hoverDestination, 255f) || hoverTimer > 50f)
                {
                    npc.velocity *= 0.935f;

                    // Do the charge.
                    if (npc.WithinRange(hoverDestination, 175f) && hoverTimer > 18f)
                    {
                        hoverTimer = 0f;
                        chargeState = 2f;
                        npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        npc.netUpdate = true;

                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/PlagueSounds/PBGDash"), target.Center);
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
                        GotoNextAttackState(npc);
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
                        int telegraph = Utilities.NewProjectileBetter(target.Center + Vector2.UnitX * horizontalOffset, Vector2.Zero, ModContent.ProjectileType<BombingTelegraph>(), 0, 0f);
                        if (Main.projectile.IndexInRange(telegraph))
						{
                            Main.projectile[telegraph].ai[0] = bombingDelay;
                            Main.projectile[telegraph].owner = target.whoAmI;
                            Main.projectile[telegraph].rotation = bombRotation;
                        }
					}
                }

                if (bombingTimer == bombingDelay)
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/TankCannon"), target.Center);

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
            Vector2 hoverOffset = new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 470f, 0f);

            // Create a wave visual effect.
            if (attackTimer == 1f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<PlagueWave>(), 0, 0f);
                Main.PlaySound(SoundID.Roar, npc.Center, 0);
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
                Vector2 hoverDestination = target.Center + hoverOffset;
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

                if (npc.WithinRange(hoverDestination, 255f) || hoverTimer > 50f)
                {
                    npc.velocity *= 0.935f;

                    // Do the charge.
                    if (npc.WithinRange(hoverDestination, 175f) && hoverTimer > 18f)
                    {
                        hoverTimer = 0f;
                        chargeState = 2f;
                        npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        npc.netUpdate = true;

                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/PlagueSounds/PBGDash"), target.Center);
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
                    GotoNextAttackState(npc);
            }

            bool canReleaseBombs = attackTimer % 16f == 15f;
            if (canReleaseBombs)
            {
                Main.PlaySound(SoundID.Item45, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float bombRotation = Main.rand.NextBool(2).ToInt() * Main.rand.NextFloatDirection() * 0.14f;
                    float horizontalOffset = Main.rand.NextFloatDirection() * 580f;
                    int telegraph = Utilities.NewProjectileBetter(target.Center + Vector2.UnitX * horizontalOffset, Vector2.Zero, ModContent.ProjectileType<BombingTelegraph>(), 0, 0f);
                    if (Main.projectile.IndexInRange(telegraph))
                    {
                        Main.projectile[telegraph].ai[0] = 40f;
                        Main.projectile[telegraph].ai[1] = 1f;
                        Main.projectile[telegraph].owner = target.whoAmI;
                        Main.projectile[telegraph].rotation = bombRotation;
                    }
                }
            }
        }

        public static void DoBehavior_BombConstructors(NPC npc, Player target, float enrageFactor, ref float attackTimer)
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
                Main.NewText("NUCLEAR CORE GENERATED. INITIATING BUILD PROCEDURE!", Color.Lime);
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/PlagueSounds/PBGNukeWarning"), target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
				{
                    for (int i = 0; i < 4; i++)
                        NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<BuilderDroneSmall>(), npc.whoAmI, Target: npc.target);
                    NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<BuilderDroneBig>(), npc.whoAmI, Target: npc.target);
                    NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y + 24, ModContent.NPCType<PlagueNuke>(), npc.whoAmI, Target: npc.target);
                }
                npc.netUpdate = true;
			}

            if (attackTimer > PlagueNuke.BuildTime + PlagueNuke.ExplodeDelay + 120f)
                GotoNextAttackState(npc);
        }

        public static void GotoNextAttackState(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            PBGAttackType currentAttackState = (PBGAttackType)(int)npc.ai[0];
            PBGAttackType newAttackState = PBGAttackType.Charge;
            switch (currentAttackState)
            {
                case PBGAttackType.Charge:
                    newAttackState = PBGAttackType.MissileLaunch;
                    break;
                case PBGAttackType.MissileLaunch:
                    newAttackState = PBGAttackType.CarpetBombing;
                    if (Main.rand.NextBool(2) && lifeRatio < Phase2LifeRatio)
                        newAttackState = PBGAttackType.CarpetBombing2;
                    if (lifeRatio < Phase3LifeRatio)
                        newAttackState = PBGAttackType.CarpetBombing3;
                    break;
                case PBGAttackType.CarpetBombing:
                case PBGAttackType.CarpetBombing2:
                case PBGAttackType.CarpetBombing3:
                    newAttackState = PBGAttackType.PlagueVomit;
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
            int width = !charging ? (532 / 2) : (644 / 2);
            int height = !charging ? (768 / 3) : (636 / 3);
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
            Texture2D texture = Main.npcTexture[npc.type];
            Texture2D glowTexture = ModContent.GetTexture("CalamityMod/NPCs/PlaguebringerGoliath/PlaguebringerGoliathGlow");
            if (charging)
            {
                texture = ModContent.GetTexture("CalamityMod/NPCs/PlaguebringerGoliath/PlaguebringerGoliathChargeTex");
                glowTexture = ModContent.GetTexture("CalamityMod/NPCs/PlaguebringerGoliath/PlaguebringerGoliathChargeTexGlow");
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
            Rectangle frame = new Rectangle(npc.frame.X, npc.frame.Y, texture.Width / 2, texture.Height / frameCount);
            Vector2 origin = frame.Size() / 2f;

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < afterimageCount; i += 2)
                {
                    Color color38 = npc.GetAlpha(Color.Lerp(lightColor, Color.White, 0.5f)) * ((afterimageCount - i) / 15f);
                    Vector2 drawPosition = npc.oldPos[i] + npc.Size * 0.5f - Main.screenPosition;
                    spriteBatch.Draw(texture, drawPosition, frame, color38, npc.rotation, origin, npc.scale, spriteEffects, 0f);
                }
            }

            Vector2 baseDrawPosition = npc.Center - Main.screenPosition;
            spriteBatch.Draw(texture, baseDrawPosition, frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, spriteEffects, 0f);
            spriteBatch.Draw(glowTexture, baseDrawPosition, frame, npc.GetAlpha(Color.White), npc.rotation, origin, npc.scale, spriteEffects, 0f);
            return false;
        }
        #endregion Frames and Drawcode
    }
}