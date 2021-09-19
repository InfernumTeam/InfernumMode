using CalamityMod.Events;
using CalamityMod.NPCs.Crabulon;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using CrabulonNPC = CalamityMod.NPCs.Crabulon.CrabulonIdle;

namespace InfernumMode.FuckYouModeAIs.Crabulon
{
    public class CrabulonBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<CrabulonNPC>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        #region Enumerations
        internal enum CrabulonAttackState
        {
            SpawnWait,
            JumpToTarget,
            WalkToTarget,
            CreateGroundMushrooms
        }
        #endregion

        #region AI

        public const float Phase2LifeRatio = 0.85f;
        public const float Phase3LifeRatio = 0.4f;

        public override bool PreAI(NPC npc)
        {
            // Give a visual offset to the boss.
            npc.gfxOffY = -16;

            // Emit a deep blue light idly.
            Lighting.AddLight(npc.Center, 0f, 0.3f, 0.7f);

            // Select a new target if an old one was lost.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();

            // Reset damage. Do none by default if somewhat transparent.
            npc.damage = npc.alpha > 40 ? 0 : npc.defDamage;

            npc.defense = 14;

            // If none was found or it was too far away, despawn.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead ||
                !Main.player[npc.target].active || !npc.WithinRange(Main.player[npc.target].Center, 3400f))
            {
                DoDespawnEffects(npc);
                return false;
            }

            Player target = Main.player[npc.target];

            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float attackType = ref npc.ai[2];
            ref float attackTimer = ref npc.ai[1];
            ref float hasSummonedClumpFlag = ref npc.ai[3];
            ref float jumpCount = ref npc.Infernum().ExtraAI[6];

            if (Main.netMode != NetmodeID.MultiplayerClient && hasSummonedClumpFlag == 0f && lifeRatio < Phase2LifeRatio)
            {
                Vector2 spawnPosition = npc.Center + Main.rand.NextVector2Circular(25f, 25f);
                NPC.NewNPC((int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<FungalClump>(), ai0: npc.whoAmI);
                hasSummonedClumpFlag = 1f;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && hasSummonedClumpFlag == 1f && lifeRatio < Phase3LifeRatio)
            {
                Vector2 spawnPosition = npc.Center + Main.rand.NextVector2Circular(25f, 25f);
                NPC.NewNPC((int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<FungalClump>(), ai0: npc.whoAmI);
                hasSummonedClumpFlag = 2f;
            }

            bool enraged = !target.ZoneGlowshroom && npc.Top.Y / 16 < Main.worldSurface && !BossRushEvent.BossRushActive;
            npc.alpha = Utils.Clamp(npc.alpha - 12, 0, 255);

            switch ((CrabulonAttackState)(int)attackType)
            {
                case CrabulonAttackState.SpawnWait:
                    DoAttack_SpawnWait(npc, attackTimer);
                    npc.ai[0] = 1f;
                    break;
                case CrabulonAttackState.JumpToTarget:
                    DoAttack_JumpToTarget(npc, target, attackTimer, enraged, ref jumpCount);
                    npc.ai[0] = 3f;
                    break;
                case CrabulonAttackState.WalkToTarget:
                    DoAttack_WalkToTarget(npc, target, attackTimer, enraged);
                    npc.ai[0] = 2f;
                    break;
                case CrabulonAttackState.CreateGroundMushrooms:
                    DoAttack_CreateGroundMushrooms(npc, target, ref attackTimer, enraged);
                    npc.ai[0] = 1f;
                    break;
            }
            attackTimer++;
            return false;
        }

        #region Specific Attacks
        internal static void DoDespawnEffects(NPC npc)
        {
            npc.noTileCollide = true;
            npc.noGravity = false;
            npc.alpha = Utils.Clamp(npc.alpha + 20, 0, 255);
            npc.damage = 0;
            if (npc.timeLeft > 45)
                npc.timeLeft = 45;
        }

        internal static void DoAttack_SpawnWait(NPC npc, float attackTimer)
        {
            if (attackTimer == 0f)
                npc.alpha = 255;
            npc.damage = 0;

            // Idly emit mushroom dust off of Crabulon.
            Dust spore = Dust.NewDustDirect(npc.position, npc.width, npc.height, 56);
            spore.velocity = -Vector2.UnitY * Main.rand.NextFloat(0.4f, 2.7f);
            spore.noGravity = true;
            spore.scale = MathHelper.Lerp(0.75f, 1.45f, Utils.InverseLerp(npc.Top.Y, npc.Bottom.Y, spore.position.Y));

            if (attackTimer >= 210f || npc.justHit)
                GotoNextAttackState(npc);
        }

        internal static void DoAttack_JumpToTarget(NPC npc, Player target, float attackTimer, bool enraged, ref float jumpCount)
        {
            // Rapidly decelerate for the first half second or so prior to the jump.
            if (attackTimer < 30f)
            {
                npc.velocity.X *= 0.9f;
                return;
            }

            float lifeRatio = npc.life / (float)npc.lifeMax;
            float jumpSpeed = MathHelper.Lerp(13.5f, 18.75f, 1f - lifeRatio);
            float extraGravity = MathHelper.Lerp(0f, 0.45f, 1f - lifeRatio);
            float jumpAngularImprecision = MathHelper.Lerp(0.1f, 0f, Utils.InverseLerp(0f, 0.7f, 1f - lifeRatio));

            jumpSpeed += MathHelper.Clamp((npc.Top.Y - target.Top.Y) * 0.02f, 0f, 12f);

            if (enraged)
            {
                extraGravity += 0.18f;
                jumpSpeed += 2.8f;
                jumpAngularImprecision *= 0.25f;
            }

            ref float hasJumpedFlag = ref npc.Infernum().ExtraAI[0];
            ref float hasHitGroundFlag = ref npc.Infernum().ExtraAI[1];
            ref float jumpTimer = ref npc.Infernum().ExtraAI[2];
            if (Main.netMode != NetmodeID.MultiplayerClient && npc.velocity.Y == 0f && hasJumpedFlag == 0f)
            {
                npc.position.Y -= 16f;
                npc.velocity = Utilities.GetProjectilePhysicsFiringVelocity(npc.Center, target.Center + target.velocity * 20f, extraGravity + 0.3f, jumpSpeed, out _);
                npc.velocity = npc.velocity.RotatedByRandom(jumpAngularImprecision);
                hasJumpedFlag = 1f;

                npc.netUpdate = true;
            }

            if (npc.velocity.Y != 0f)
                npc.velocity.Y += extraGravity;

            if (hasJumpedFlag == 1f)
            {
                // Don't interact with any obstacles in the way if above the target.
                npc.noTileCollide = npc.Bottom.Y < target.Top.Y && hasHitGroundFlag == 0f;
                if (jumpTimer < 8f)
                    npc.noTileCollide = true;

                // Do more damage since Crabulon is essentially trying to squish the target.
                npc.damage = npc.defDamage + 36;

                if (npc.velocity.Y == 0f)
                {
                    // Make some visual and auditory effects when hitting the ground.
                    if (hasHitGroundFlag == 0f)
                    {
                        Main.PlaySound(SoundID.Item14, npc.Center);
                        for (int i = 0; i < 36; i++)
                        {
                            Vector2 dustSpawnPosition = Vector2.Lerp(npc.BottomLeft, npc.BottomRight, i / 36f);
                            Dust stompMushroomDust = Dust.NewDustDirect(dustSpawnPosition, 4, 4, 56);
                            stompMushroomDust.velocity = Vector2.UnitY * Main.rand.NextFloatDirection() * npc.velocity.Length() * 0.5f;
                            stompMushroomDust.scale = 1.8f;
                            stompMushroomDust.fadeIn = 1.2f;
                            stompMushroomDust.noGravity = true;
                        }

                        // Optionally, if below a certain life ratio or enraged, release mushrooms into the air.
                        bool tooManyShrooms = NPC.CountNPCS(ModContent.NPCType<CrabShroom>()) > 10;
                        if (Main.netMode != NetmodeID.MultiplayerClient && (lifeRatio < Phase2LifeRatio || enraged))
                        {
                            for (int i = 0; i < 2; i++)
                            {
                                if (tooManyShrooms)
                                    break;

                                Vector2 spawnPosition = npc.Center + Main.rand.NextVector2Circular(npc.width, npc.height) * 0.5f;
                                int shroom = NPC.NewNPC((int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<CrabShroom>());
                                if (Main.npc.IndexInRange(shroom))
                                {
                                    Main.npc[shroom].velocity = -Vector2.UnitY.RotatedByRandom(0.36f) * Main.rand.NextFloat(3f, 6f);
                                    Main.npc[shroom].netUpdate = true;
                                }
                            }
                        }
                        jumpCount++;

                        if (Main.netMode != NetmodeID.MultiplayerClient && jumpCount % 3f == 2f)
                        {
                            for (int i = 0; i < 15; i++)
                            {
                                Vector2 spawnPosition = npc.Center + Main.rand.NextVector2Circular(npc.width, npc.height) * 0.45f;
                                Vector2 sporeShootVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(6f, 12f);
                                int cloud = Utilities.NewProjectileBetter(spawnPosition, sporeShootVelocity, ModContent.ProjectileType<SporeCloud>(), 75, 0f);
                                if (Main.projectile.IndexInRange(cloud))
                                    Main.projectile[cloud].ai[0] = Main.rand.Next(3);
                            }
                        }

                        hasHitGroundFlag = 1f;
                        npc.netUpdate = true;
                    }

                    npc.velocity.X *= 0.9f;
                    if (Math.Abs(npc.velocity.X) < 0.2f)
                        GotoNextAttackState(npc);
                }
                jumpTimer++;
            }
            else
                jumpTimer = 0f;
        }

        internal static void DoAttack_WalkToTarget(NPC npc, Player target, float attackTimer, bool enraged)
        {
            npc.direction = (target.Center.X > npc.Center.X).ToDirectionInt();

            float horizontalDistanceFromTarget = MathHelper.Distance(target.Center.X, npc.Center.X);
            bool shouldSlowDown = horizontalDistanceFromTarget < 50f;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float walkSpeed = MathHelper.Lerp(2.4f, 5.6f, 1f - lifeRatio);
            if (enraged)
                walkSpeed += 1.5f;
            walkSpeed += horizontalDistanceFromTarget * 0.004f;
            walkSpeed *= npc.SafeDirectionTo(target.Center).X;

            // Release spores into the air after a specific life ratio is passed.
            if (lifeRatio < Phase2LifeRatio)
            {
                bool canShoot = attackTimer % 120f >= 80f && attackTimer % 8f == 7f;
                shouldSlowDown = attackTimer % 120f >= 60f;
                float shootPower = MathHelper.Lerp(5f, 10f, Utils.InverseLerp(80f, 120f, attackTimer % 120f, true));
                if (Main.netMode != NetmodeID.MultiplayerClient && canShoot)
                {
                    Vector2 shootVelocity = npc.SafeDirectionTo(target.Center) * shootPower;
                    shootVelocity.X += npc.SafeDirectionTo(target.Center).X * shootPower * 0.45f;
                    shootVelocity.Y -= shootPower * 0.75f;
                    Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<MushBomb>(), 70, 0f);
                }
            }

            if (shouldSlowDown)
            {
                npc.velocity.X *= 0.8f;
                if (Math.Abs(npc.velocity.X) < 0.1f)
                    npc.velocity.X = 0f;
            }
            else
                npc.velocity.X = (npc.velocity.X * 20f + walkSpeed) / 21f;

            Vector2 checkArea = new Vector2(80f);
            Vector2 checkTopLeft = npc.Center - checkArea * 0.5f;
            npc.noTileCollide = Collision.SolidCollision(checkTopLeft, (int)checkArea.X, (int)checkArea.Y);

            // Walk upwards to reach the target if below them.
            if (npc.Center.Y > target.Bottom.Y && npc.velocity.Y > -14f)
                npc.velocity.Y -= 0.15f;

            // Otherwise slow down vertical movement.
            else
                npc.velocity.Y *= 0.96f;

            if (attackTimer >= 160f || npc.collideX || target.Center.Y < npc.Top.Y - 200f || target.Center.Y > npc.Bottom.Y + 80f)
                GotoNextAttackState(npc);
        }

        internal static void DoAttack_CreateGroundMushrooms(NPC npc, Player target, ref float attackTimer, bool enraged)
        {
            // Rapidly decelerate for the first second or so prior to the summon.
            if (attackTimer < 45f)
            {
                npc.velocity.X *= 0.9f;
                return;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 75f)
            {
                for (float dx = -1000f; dx < 1000f; dx += enraged ? 250f : 360f)
                {
                    Vector2 spawnPosition = target.Bottom + Vector2.UnitX * dx;
                    Utilities.NewProjectileBetter(spawnPosition, Vector2.Zero, ModContent.ProjectileType<MushroomPillar>(), 70, 0f);
                }
            }

            if (attackTimer >= 120f)
                GotoNextAttackState(npc);
        }
        #endregion Specific Attacks

        #region AI Utility Methods

        internal static void GotoNextAttackState(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            CrabulonAttackState currentAttackState = (CrabulonAttackState)(int)npc.ai[2];
            CrabulonAttackState newAttackState = CrabulonAttackState.JumpToTarget;
            switch (currentAttackState)
            {
                case CrabulonAttackState.SpawnWait:
                    newAttackState = CrabulonAttackState.WalkToTarget;
                    break;
                case CrabulonAttackState.WalkToTarget:
                    newAttackState = CrabulonAttackState.JumpToTarget;
                    if (lifeRatio < Phase2LifeRatio && Main.rand.NextFloat() < 0.45f)
                        newAttackState = CrabulonAttackState.CreateGroundMushrooms;
                    break;
                case CrabulonAttackState.CreateGroundMushrooms:
                    newAttackState = CrabulonAttackState.WalkToTarget;
                    break;
                case CrabulonAttackState.JumpToTarget:
                    newAttackState = CrabulonAttackState.WalkToTarget;
                    break;
            }

            npc.ai[2] = (int)newAttackState;
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
        }
        #endregion AI Utility Methods

        #endregion AI
    }
}
