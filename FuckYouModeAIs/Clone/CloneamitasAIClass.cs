using CalamityMod.Dusts;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Calamitas;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Clone
{
	public class CloneamitasAIClass
    {
        #region Enumerations
        public enum CloneAttackType
        {
            LaserAttackMovement,
            ChargeAttackMovement,
            BurningBrimstoneChargeAttackMovement,
            DemonicVisionMovement,
        }
        #endregion

        #region AI

        #region Main Boss

		[OverrideAppliesTo("CalamitasRun3", typeof(CloneamitasAIClass), "CloneAI", EntityOverrideContext.NPCAI, true)]
        public static bool CloneAI(NPC npc)
		{
            DetermineTarget(npc, out bool despawning);
            if (despawning)
                return false;

            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float secondaryEnemySpawnCounter = ref npc.ai[2];

            npc.damage = npc.defDamage;
            Player target = Main.player[npc.target];
            CalamityGlobalNPC.calamitas = npc.whoAmI;

            HandleCustomSpawns(npc, target, ref secondaryEnemySpawnCounter);

            switch ((CloneAttackType)(int)attackType)
            {
                case CloneAttackType.LaserAttackMovement:
                    DoAttack_LaserMovement(npc, target, ref attackTimer);
                    break;
                case CloneAttackType.ChargeAttackMovement:
                case CloneAttackType.BurningBrimstoneChargeAttackMovement:
                    DoAttack_ChargeMovement(npc, target, attackType == (int)CloneAttackType.BurningBrimstoneChargeAttackMovement, ref attackTimer);
                    break;
                case CloneAttackType.DemonicVisionMovement:
                    DoAttack_DemonicVision(npc, target, ref attackTimer);
                    break;
            }
            attackTimer++;
            return false;
		}

        internal static void DetermineTarget(NPC npc, out bool despawning)
		{
            despawning = false;

            // Aquire a new target if the current one is dead or inactive.
            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
            {
                npc.TargetClosest(false);

                // If no possible target was found or it's time to despawn, fly away.
                if (Main.dayTime || npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                {
                    if (npc.velocity.Y > 3f)
                        npc.velocity.Y = 3f;
                    npc.velocity.Y -= 0.15f;
                    if (npc.velocity.Y < -16f)
                        npc.velocity.Y = -16f;

                    if (npc.timeLeft > 60)
                        npc.timeLeft = 60;
                    npc.rotation = npc.rotation.AngleTowards(npc.velocity.ToRotation() - MathHelper.PiOver2, 0.15f);
                    despawning = true;
                }
                npc.netUpdate = true;
            }
            else
                npc.timeLeft = 7200;
        }

        internal static void HandleCustomSpawns(NPC npc, Player target, ref float secondaryEnemySpawnCounter)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            float lifeRatio = npc.life / (float)npc.lifeMax;

            Vector2 potentialSpawnPosition = target.Center + Main.rand.NextVector2CircularEdge(1400f, 1000f);

            // Summon cataclysm.
            if (secondaryEnemySpawnCounter == 0f && lifeRatio < 0.8f)
            {
                NPC.NewNPC((int)potentialSpawnPosition.X, (int)potentialSpawnPosition.Y, ModContent.NPCType<CalamitasRun>());
                secondaryEnemySpawnCounter = 1f;
                npc.netUpdate = true;
            }

            // Summon catastrophe.
            if (secondaryEnemySpawnCounter == 1f && lifeRatio < 0.55f)
            {
                NPC.NewNPC((int)potentialSpawnPosition.X, (int)potentialSpawnPosition.Y, ModContent.NPCType<CalamitasRun2>());
                secondaryEnemySpawnCounter = 2f;
                npc.netUpdate = true;
            }

            // Resummon cataclysm and catastrophe.
            if (secondaryEnemySpawnCounter == 2f && lifeRatio < 0.3f)
            {
                NPC.NewNPC((int)potentialSpawnPosition.X, (int)potentialSpawnPosition.Y, ModContent.NPCType<CalamitasRun>());
                NPC.NewNPC((int)potentialSpawnPosition.X, (int)potentialSpawnPosition.Y, ModContent.NPCType<CalamitasRun2>());
                secondaryEnemySpawnCounter = 3f;
                npc.netUpdate = true;
            }
        }

        internal static void GoToNextAttackState(NPC npc)
        {
            npc.velocity = Vector2.Zero;
            CloneAttackType oldAttackType = (CloneAttackType)(int)npc.ai[0];
            CloneAttackType newAttackType = CloneAttackType.ChargeAttackMovement;
            switch (oldAttackType)
			{
                case CloneAttackType.ChargeAttackMovement:
                    newAttackType = CloneAttackType.LaserAttackMovement;
                    break;
                case CloneAttackType.LaserAttackMovement:
                    newAttackType = CloneAttackType.BurningBrimstoneChargeAttackMovement;
                    break;
                case CloneAttackType.BurningBrimstoneChargeAttackMovement:
                    newAttackType = CloneAttackType.DemonicVisionMovement;
                    break;
                case CloneAttackType.DemonicVisionMovement:
                    newAttackType = CloneAttackType.ChargeAttackMovement;
                    break;
            }
            npc.ai[0] = (int)newAttackType;
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
        }

		#region Specific Attacks
        internal static void DoAttack_LaserMovement(NPC npc, Player target, ref float attackTimer)
        {
            float maxSwipeOffset = 0.71f;
            float idealAngle = npc.AngleTo(target.Center) - MathHelper.PiOver2;
            if (attackTimer < 60f)
            {
                npc.Opacity = 1f - attackTimer / 60f;
                npc.rotation = idealAngle;
            }

            // Teleport to the side of the player.
            if (attackTimer == 60f)
			{
                npc.Opacity = 1f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
				{
                    npc.Center = target.Center + Main.rand.NextVector2CircularEdge(600f, 600f);
                    npc.netUpdate = true;
                }
                npc.rotation = idealAngle;
            }

            Vector2 pupilPosition = npc.Center + (npc.rotation + MathHelper.PiOver2).ToRotationVector2() * 56f;

            if (attackTimer > 60f && attackTimer < 210f)
            {
                if (!npc.WithinRange(target.Center, 800f))
                {
                    npc.velocity = Vector2.Lerp(npc.velocity, npc.DirectionTo(target.Center) * 18f, 0.04f);
                    idealAngle = npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
                }
                else
                    npc.velocity *= 0.93f;
                idealAngle -= MathHelper.SmoothStep(0f, maxSwipeOffset, Utils.InverseLerp(120f, 200f, attackTimer, true));

                npc.rotation = npc.rotation.AngleLerp(idealAngle, 0.1f);
                npc.rotation = npc.rotation.AngleTowards(idealAngle, 0.12f);

                int totalChargeDust = attackTimer > 150f ? 6 : 3;

                // Make charge dust near the pupil.
                for (int i = 0; i < totalChargeDust; i++)
				{
                    Dust brimstone = Dust.NewDustPerfect(pupilPosition + Main.rand.NextVector2CircularEdge(16f, 16f), (int)CalamityDusts.Brimstone);
                    brimstone.velocity = (pupilPosition - brimstone.position).SafeNormalize(Vector2.Zero) * 3f;
                    brimstone.scale = Main.rand.NextFloat(0.85f, 1.05f);
                    brimstone.noGravity = true;
                }

                if (attackTimer == 140f)
                {
                    // Play a telegraph sound before firing the laser.
                    Main.PlaySound(SoundID.Item117, target.Center);
                }
            }

            ref float swipeSpeed = ref npc.Infernum().ExtraAI[0];

            // Release the laser.
            if (attackTimer == 210f)
            {
                Vector2 shootDirection = (target.Center - pupilPosition).SafeNormalize(Vector2.UnitY);
                swipeSpeed = MathHelper.ToRadians(1.1f);
                shootDirection = shootDirection.RotatedBy(-maxSwipeOffset);

                npc.rotation = shootDirection.ToRotation() - MathHelper.PiOver2;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int laser = Utilities.NewProjectileBetter(pupilPosition, shootDirection, ModContent.ProjectileType<BrimstoneRay2>(), 110, 0f);

                    Main.projectile[laser].ai[0] = swipeSpeed;
                    Main.projectile[laser].ai[1] = npc.whoAmI;
                    Main.projectile[laser].netUpdate = true;
                    npc.netUpdate = true;
                }

                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/LaserCannon"), npc.Center);
            }

            if (attackTimer >= 210f && attackTimer <= 210f + BrimstoneRay2.LaserLifetime)
            {
                npc.rotation += swipeSpeed;
                npc.velocity *= 0.95f;
            }
            if (attackTimer >= 210f + BrimstoneRay2.LaserLifetime)
            {
                npc.rotation = npc.rotation.AngleLerp(idealAngle, 0.1f);
                npc.rotation = npc.rotation.AngleTowards(idealAngle, 0.15f);
            }

            if (attackTimer >= 335f + BrimstoneRay2.LaserLifetime)
                GoToNextAttackState(npc);
        }

        internal static void DoAttack_ChargeMovement(NPC npc, Player target, bool burningBrimstoneMode, ref float attackTimer)
		{
            int maxCharges = 11;
            if (burningBrimstoneMode)
                maxCharges = 9;
            int chargeRate = 55;
            float chargeSpeed = 12f;
            ref float chargeCount = ref npc.Infernum().ExtraAI[0];

            if (attackTimer % chargeRate > chargeRate - 20f || attackTimer <= 2f)
            {
                npc.velocity *= 0.94f;
                npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(target.Center) - MathHelper.PiOver2, 0.25f);
            }
            else
                npc.velocity *= 1.015f;

            if (attackTimer % chargeRate == 1f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient && burningBrimstoneMode && chargeCount == 0f)
				{
                    for (int i = 0; i < 9; i++)
                    {
                        int orb = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<BrimstoneOrb>(), 95, 0f);
                        Main.projectile[orb].ai[1] = MathHelper.TwoPi * i / 9f;
                        Main.projectile[orb].netUpdate = true;
                    }
                }

                chargeCount++;
                if (chargeCount < maxCharges)
                {
                    npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                    npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            Vector2 dartShootVelocity = (MathHelper.TwoPi * (i + 0.5f) / 4f).ToRotationVector2() * chargeSpeed * 0.6f;
                            if (burningBrimstoneMode)
                                dartShootVelocity *= 0.6f;
                            Utilities.NewProjectileBetter(npc.Center + dartShootVelocity * 5f, dartShootVelocity, ModContent.ProjectileType<HomingBrimstoneDart>(), 95, 0f);
                        }

                        // Release all orbs.
                        if (chargeCount == maxCharges - 2)
                        {
                            int orbType = ModContent.ProjectileType<BrimstoneOrb>();
                            for (int i = 0; i < Main.maxProjectiles; i++)
                            {
                                if (Main.projectile[i].type != orbType || !Main.projectile[i].active)
                                    continue;

                                Main.projectile[i].ai[0] = 1f;
                                Main.projectile[i].velocity = npc.velocity * 1.9f;
                                Main.projectile[i].netUpdate = true;
                            }
                        }
                    }
                }
                else
                    GoToNextAttackState(npc);
                npc.netUpdate = true;
            }
		}

        internal static void DoAttack_DemonicVision(NPC npc, Player target, ref float attackTimer)
        {
            Vector2 destination = target.Center + (npc.velocity.ToRotation() + MathHelper.PiOver4).ToRotationVector2() * 385f;
            destination -= npc.velocity * 2f;

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 90f >= 40f && attackTimer % 10f == 0f)
            {
                float offsetAngle = MathHelper.Lerp(-0.82f, 0.98f, Utils.InverseLerp(40f, 90f, attackTimer % 90f, true));
                Vector2 spawnPosition = target.Center;
                int seeker = Utilities.NewProjectileBetter(spawnPosition, Vector2.Zero, ModContent.ProjectileType<LifeSeekerProj>(), 105, 0f, npc.target);
                Main.projectile[seeker].owner = npc.target;
                Main.projectile[seeker].ai[0] = 160f - attackTimer % 150f;
                Main.projectile[seeker].ai[1] = offsetAngle;
            }

            if (attackTimer >= 90f * 4f + 15f)
                GoToNextAttackState(npc);

            if (!npc.WithinRange(destination, 200f))
                npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * 22f, 0.3f);
            npc.rotation = npc.rotation.AngleTowards(npc.velocity.ToRotation() - MathHelper.PiOver2, 0.35f);
        }
        #endregion Specific Attacks

        #endregion Main Boss

        #region Cataclysm

        [OverrideAppliesTo("CalamitasRun", typeof(CloneamitasAIClass), "CataclysmCloneAI", EntityOverrideContext.NPCAI, true)]
        public static bool CataclysmCloneAI(NPC npc)
        {
            bool otherBrotherExists = NPC.AnyNPCs(ModContent.NPCType<CalamitasRun2>());
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float attackSubstate = ref npc.ai[2];
            CalamityGlobalNPC.cataclysm = npc.whoAmI;

            // Disappear if Calamitas is gone.
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.calamitas) || !Main.npc[CalamityGlobalNPC.calamitas].active)
			{
                npc.active = false;
                npc.netUpdate = true;
                return false;
			}

            npc.target = Main.npc[CalamityGlobalNPC.calamitas].target;
            Player target = Main.player[npc.target];

            // Horizontal charges.
            if (attackState == 0f)
			{
                // Redirect to the side of the target.
                if (attackSubstate == 0f)
				{
                    Vector2 destination = target.Center;
                    destination.X += (npc.Center.X > target.Center.X).ToDirectionInt() * 500f;
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * 15f, 0.25f);

                    // Repel away from the player if too close.
                    if (npc.WithinRange(target.Center, 150f))
                        npc.velocity -= npc.SafeDirectionTo(target.Center, Vector2.UnitY) * 1.6f;

                    // Begin charging at the target if close to the destination.
                    if (npc.WithinRange(destination, 40f))
					{
                        npc.velocity = npc.DirectionTo(target.Center) * (otherBrotherExists ? 19.5f : 23f);
                        attackTimer = 0f;
                        attackSubstate = 1f;
                        npc.netUpdate = true;
					}
				}

                // Let the charge happen for a bit.
				else if (attackTimer >= 50f)
                {
                    npc.velocity *= 0.95f;
                    if (attackTimer >= 70f)
                    {
                        npc.velocity *= 0.5f;
                        attackTimer = 0f;
                        attackSubstate = 0f;
                        attackState = 1f;
                        npc.netUpdate = true;
                    }
                }
                npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
                npc.damage = npc.defDamage;
            }

            // U-shaped swoop while releasing brimstone flames upward.
			else
            {
                // Aim upwards.
                npc.rotation = npc.rotation.AngleTowards(MathHelper.Pi, 0.25f);
                npc.damage = 0;

                Vector2 left = target.Center - Vector2.UnitX * 1200f;
                Vector2 right = target.Center + Vector2.UnitX * 1200f;
                Vector2 destination = Vector2.CatmullRom(left - Vector2.UnitY * 1600f, left, right, right - Vector2.UnitY * 1600f, attackTimer / 240f);
                npc.Center = Vector2.Lerp(npc.Center, destination, 0.1f);

                int shootRate = otherBrotherExists ? 10 : 7;
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % shootRate == shootRate - 1f && Math.Abs(npc.rotation - MathHelper.Pi) < 0.05f)
				{
                    float shootSpeed = 17f;
                    Vector2 shootVelocity = Utilities.GetProjectilePhysicsFiringVelocity(npc.Center, target.Center, FallingBrimstoneFireblast.Gravity, shootSpeed, out _);
                    Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<FallingBrimstoneFireblast>(), 90, 0f);
				}

                if (attackTimer >= 240f)
                {
                    attackTimer = 0f;
                    attackSubstate = 0f;
                    attackState = 0f;
                    npc.netUpdate = true;
                }
            }

            attackTimer++;
            return false;
        }
        #endregion

        #endregion AI
    }
}
