using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs.HiveMind;
using CalamityMod.Projectiles.Boss;
using InfernumMode.BehaviorOverrides.BossAIs.Ravager;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.EoW
{
    public class EoWHeadBehaviorOverride : NPCBehaviorOverride
    {
        public enum EoWAttackState
        {
            CursedBombBurst,
            VineCharge,
            ShadowOrbSummon,
            RainHover,
            DarkHeartSlam
        }

        public override int NPCOverrideType => NPCID.EaterofWorldsHead;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        // This is applicable to all split worms as well.
        // Since split worms share HP, the total amount of HP of the boss is equal to Worm HP * (Total Splits + 1).
        public const int TotalLifeAcrossWorm = 4000;
        public const int TotalLifeAcrossWormBossRush = 376810;
        public const int BaseBodySegmentCount = 40;
        public const int TotalSplitsToPerform = 2;

        public override bool PreAI(NPC npc)
        {
            ref float attackState = ref npc.Infernum().ExtraAI[7];
            ref float attackTimer = ref npc.Infernum().ExtraAI[8];
            ref float splitCounter = ref npc.ai[2];
            ref float segmentCount = ref npc.ai[3];
            ref float initializedFlag = ref npc.localAI[0];
            ref float enrageTimer = ref npc.Infernum().ExtraAI[6];

            // Fuck.
            npc.Calamity().newAI[1] = MathHelper.Clamp(npc.Calamity().newAI[1] + 8f, 0f, 720f);
            npc.dontTakeDamage = npc.Calamity().newAI[1] < 700f;

            // Perform initialization logic.
            if (Main.netMode != NetmodeID.MultiplayerClient && initializedFlag == 0f)
            {
                if (segmentCount == 0f)
                    segmentCount = BaseBodySegmentCount;

                CreateSegments(npc, (int)segmentCount, NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail);
                npc.TargetClosest(false);
                initializedFlag = 1f;
                attackState = 0f;
            }

            Player target = Main.player[npc.target];
            bool outOfBiome = !target.ZoneCrimson && !target.ZoneCorrupt && !BossRushEvent.BossRushActive;
            if (!outOfBiome)
                enrageTimer = MathHelper.Clamp(enrageTimer - 2.4f, 0f, 480f);
            else
                enrageTimer = MathHelper.Clamp(enrageTimer + 1f, 0f, 480f);

            bool enraged = enrageTimer >= 300f;
            npc.Calamity().CurrentlyEnraged = outOfBiome;

            if (target.dead)
            {
                DoAttack_Despawn(npc);
                return false;
            }

            switch ((EoWAttackState)(int)attackState)
            {
                case EoWAttackState.CursedBombBurst:
                    DoAttack_CursedBombBurst(npc, target, splitCounter, enraged, ref attackTimer);
                    break;
                case EoWAttackState.VineCharge:
                    DoAttack_VineCharge(npc, target, splitCounter, enraged, ref attackTimer);
                    break;
                case EoWAttackState.ShadowOrbSummon:
                    DoAttack_ShadowOrbSummon(npc, target, splitCounter, enraged, ref attackTimer);
                    break;
                case EoWAttackState.RainHover:
                    DoAttack_RainHover(npc, target, splitCounter, enraged, ref attackTimer);
                    break;
                case EoWAttackState.DarkHeartSlam:
                    DoAttack_DarkHeartSlam(npc, target, splitCounter, enraged, ref attackTimer);
                    break;
            }

            // Split into two and two different life ratios.
            if (npc.realLife != -1)
            {
                npc.life = Main.npc[npc.realLife].life;
                npc.lifeMax = Main.npc[npc.realLife].lifeMax;
                npc.Infernum().ExtraAI[7] = Main.npc[npc.realLife].Infernum().ExtraAI[7];
                npc.Infernum().ExtraAI[8] = Main.npc[npc.realLife].Infernum().ExtraAI[8];
            }

            npc.rotation = npc.rotation.AngleLerp(npc.velocity.ToRotation() + MathHelper.PiOver2, 0.05f);
            npc.rotation = npc.rotation.AngleTowards(npc.velocity.ToRotation() + MathHelper.PiOver2, 0.15f);
            attackTimer++;

            return false;
        }

        #region Attacks
        public static void DoAttack_CursedBombBurst(NPC npc, Player target, float splitCounter, bool enraged, ref float attackTimer)
        {
            int totalFireballsPerBurst = (int)(TotalSplitsToPerform - splitCounter + 1f);
            float flySpeed = enraged ? 11f : 8f;
            float turnSpeedFactor = enraged ? 1.7f : 1f;
            flySpeed *= MathHelper.Lerp(1f, 1.425f, splitCounter / TotalSplitsToPerform);
            if (splitCounter == 0f)
            {
                flySpeed *= 1.15f;
                turnSpeedFactor *= 1.15f;
            }

            // Do default movement.
            DoDefaultMovement(npc, target, flySpeed, turnSpeedFactor);

            // Periodically release fireballs.
            int fireRate = splitCounter >= TotalSplitsToPerform - 1f ? 92 : 120;
            if (BossRushEvent.BossRushActive)
                fireRate = 32;

            if (attackTimer % fireRate == fireRate - 1f)
            {
                Main.PlaySound(SoundID.Item20, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < totalFireballsPerBurst; i++)
                    {
                        float shootOffsetAngle = 0f;
                        if (totalFireballsPerBurst > 1f)
                            shootOffsetAngle = MathHelper.Lerp(-0.84f, 0.84f, i / (float)(totalFireballsPerBurst - 1f));
                        Vector2 shootVelocity = npc.SafeDirectionTo(target.Center, -Vector2.UnitY).RotatedBy(shootOffsetAngle) * 7f;
                        Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<CursedFlameBomb>(), 85, 0f);
                    }
                }
            }

            if (attackTimer >= 720f)
                GotoNextAttackState(npc);
        }

        public static void DoAttack_VineCharge(NPC npc, Player target, float splitCounter, bool enraged, ref float attackTimer)
        {
            float flySpeed = enraged ? 15f : 9f;
            float turnSpeedFactor = enraged ? 1.3f : 0.85f;
            flySpeed *= MathHelper.Lerp(1f, 1.2f, splitCounter / TotalSplitsToPerform);

            if (attackTimer < 75f)
                flySpeed *= 0.6f;

            if (BossRushEvent.BossRushActive)
                flySpeed *= 2.15f;

            // Have the main head generate a bunch of thorns at the beginning.
            if (Main.netMode != NetmodeID.MultiplayerClient && npc.realLife == -1 && attackTimer == 15f)
            {
                float spacing = enraged ? 380f : 520f;
                spacing *= MathHelper.Lerp(1f, 1.5f, splitCounter / TotalSplitsToPerform);
                for (float dx = -2000f; dx < 2000f; dx += spacing)
                {
                    Vector2 spawnPosition = target.Bottom + Vector2.UnitX * dx;
                    Utilities.NewProjectileBetter(spawnPosition, Vector2.Zero, ModContent.ProjectileType<CorruptThorn>(), 90, 0f);
                }
            }

            // Periodically shoot small flames.
            if (attackTimer % 120f == 119f)
            {
                Main.PlaySound(SoundID.Item20, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 shootVelocity = Main.rand.NextVector2CircularEdge(6f, 6f);
                        if (BossRushEvent.BossRushActive)
                            shootVelocity *= 3.2f;
                        Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<CursedBullet>(), 85, 0f);
                    }
                }
            }

            float offsetAngle = MathHelper.Lerp(-0.76f, 0.76f, npc.whoAmI % 4f / 4f);
            offsetAngle *= Utils.InverseLerp(100f, 350f, npc.Distance(target.Center), true);

            Vector2 idealVelocity = npc.SafeDirectionTo(target.Center) * flySpeed;
            if (!npc.WithinRange(target.Center, 400f) || npc.velocity == Vector2.Zero || npc.velocity.Length() < 5f)
            {
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center) + offsetAngle, turnSpeedFactor * 0.018f, true) * idealVelocity.Length();
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, turnSpeedFactor * 0.025f);
            }

            // Charge and do a roar sound.
            else if (npc.velocity.Length() < flySpeed * 2.1f)
            {
                npc.velocity *= 1.018f;
                if (npc.soundDelay <= 0)
                {
                    Main.PlaySound(SoundID.Roar, target.Center, 0);
                    npc.soundDelay = 80;
                }
            }

            if (attackTimer >= 520f)
                GotoNextAttackState(npc);
        }

        public static void DoAttack_ShadowOrbSummon(NPC npc, Player target, float splitCounter, bool enraged, ref float attackTimer)
        {
            float flySpeed = enraged ? 12f : 8.25f;
            float turnSpeedFactor = enraged ? 1.2f : 0.8f;
            flySpeed *= MathHelper.Lerp(1f, 1.275f, splitCounter / TotalSplitsToPerform);
            if (splitCounter == 0f)
            {
                flySpeed *= 1.15f;
                turnSpeedFactor *= 1.15f;
            }

            if (BossRushEvent.BossRushActive)
                GotoNextAttackState(npc);

            DoDefaultMovement(npc, target, flySpeed, turnSpeedFactor);

            // Spawn a shadow orb that'll summon an enemy near the target.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 90f)
            {
                int orbCount = enraged ? 2 : 1;
                for (int i = 0; i < orbCount; i++)
                {
                    for (int j = 0; j < 825; j++)
                    {
                        Vector2 spawnPosition = target.Center + Main.rand.NextVector2Circular(360f, 360f);

                        // The first 800 checks avoid spawning in positions that cannot hit the target with a raycast.
                        if (j < 800 && !Collision.CanHit(spawnPosition, 1, 1, target.Center, 1, 1))
                            continue;

                        Utilities.NewProjectileBetter(spawnPosition, Vector2.Zero, ModContent.ProjectileType<ShadowOrb>(), 0, 0f);
                        break;
                    }
                }
            }

            // Move around normally for a bit afterwards.
            // The spawned enemies may interfere with later attacks if not killed in time.
            if (attackTimer >= 520f)
                GotoNextAttackState(npc);
        }

        public static void DoAttack_RainHover(NPC npc, Player target, float splitCounter, bool enraged, ref float attackTimer)
        {
            // Hover above the player.
            Vector2 hoverDestination = target.Center - Vector2.UnitY * 300f + target.velocity * 25f;
            float offsetAngle = MathHelper.Lerp(-0.76f, 0.76f, npc.whoAmI % 4f / 4f);
            offsetAngle *= Utils.InverseLerp(70f, 240f, npc.Distance(hoverDestination), true);

            Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 12f;
            if (splitCounter == 2f)
                idealVelocity *= 0.8f;

            if (BossRushEvent.BossRushActive)
                idealVelocity *= 2f;

            if (!npc.WithinRange(hoverDestination, 225f) || npc.velocity == Vector2.Zero || npc.velocity.Length() < 5f)
            {
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center) + offsetAngle, 0.018f, true) * idealVelocity.Length();
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.025f);
            }

            // And release rain clouds.
            int rainReleaseRate = splitCounter >= 1f ? 67 : 35;
            if (BossRushEvent.BossRushActive)
                rainReleaseRate /= 2;
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % rainReleaseRate == rainReleaseRate - 1f && npc.Center.Y < target.Center.Y - 185f)
            {
                Vector2 cloudSpawnPosition = npc.Center + Main.rand.NextVector2Circular(npc.width, npc.height) * 0.45f;
                Utilities.NewProjectileBetter(cloudSpawnPosition, Vector2.Zero, ModContent.ProjectileType<ShadeNimbusHostile>(), 85, 0f);
            }

            if (attackTimer >= 480f)
                GotoNextAttackState(npc);
        }


        public static void DoAttack_DarkHeartSlam(NPC npc, Player target, float splitCounter, bool enraged, ref float attackTimer)
        {
            ref float wasPreviouslyInTiles = ref npc.Infernum().ExtraAI[11];

            int riseTime = 75;

            if (BossRushEvent.BossRushActive)
                riseTime -= 25;

            // Rise upward in anticipation of slamming into the target.
            if (attackTimer < riseTime)
            {
                float riseSpeed = !Collision.SolidCollision(npc.Center, 2, 2) ? 19f : 9f;
                npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, -riseSpeed, 0.045f);
                if (MathHelper.Distance(npc.Center.X, target.Center.X) > 300f)
                    npc.velocity.X = (npc.velocity.X * 24f + npc.SafeDirectionTo(target.Center).X * 10.5f) / 25f;
            }

            // Slam back down after the rise ends.
            if (attackTimer >= riseTime)
            {
                bool inTiles = Collision.SolidCollision(npc.Center, 2, 2);
                if (npc.velocity.Y < 26f)
                    npc.velocity.Y += enraged ? 0.9f : 0.5f;
                if (inTiles)
                    npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y, -8f, 8f);

                // Release a shockwave and dark hearts once tiles have been hit.
                if (inTiles && wasPreviouslyInTiles == 0f)
                {
                    Main.PlaySound(SoundID.Item62, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<StompShockwave>(), 105, 0f);

                        // Release 5 dark hearts if none currently exist.
                        if (!NPC.AnyNPCs(ModContent.NPCType<DarkHeart>()))
                        {
                            for (int i = 0; i < 5; i++)
                            {
                                Vector2 initialSeekerVelocity = (MathHelper.TwoPi * i / 5f).ToRotationVector2() * 8f;
                                Vector2 spawnPosition = npc.Center + initialSeekerVelocity * 2f;
                                int seeker = NPC.NewNPC((int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<DarkHeart>(), 1);
                                if (Main.npc.IndexInRange(seeker))
                                    Main.npc[seeker].velocity = initialSeekerVelocity;
                            }
                        }
                        wasPreviouslyInTiles = 1f;
                    }

                    if (MathHelper.Distance(npc.Center.X, target.Center.X) > 240f)
                        npc.velocity.X = (npc.velocity.X * 21f + npc.SafeDirectionTo(target.Center).X * 10.5f) / 22f;
                    npc.netUpdate = true;
                }
            }

            if (wasPreviouslyInTiles == 1f && attackTimer < 600f)
                attackTimer = 600f;

            if (attackTimer > 660f)
                GotoNextAttackState(npc);
        }
        #endregion

        #region AI Utility Methods

        public static void DoAttack_Despawn(NPC npc)
        {
            if (npc.timeLeft > 200)
                npc.timeLeft = 200;

            npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * 30f, 0.12f);
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
            if (!npc.WithinRange(Main.player[npc.target].Center, 2200f))
                npc.active = false;
        }

        public static void DoDefaultMovement(NPC npc, Player target, float flySpeed, float turnSpeedFactor)
        {
            float offsetAngle = MathHelper.Lerp(-0.76f, 0.76f, npc.whoAmI % 4f / 4f);
            offsetAngle *= Utils.InverseLerp(100f, 350f, npc.Distance(target.Center), true);

            Vector2 idealVelocity = npc.SafeDirectionTo(target.Center) * flySpeed * 0.95f;

            // Avoid other worm heads.
            Vector2 pushAway = Vector2.Zero;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].type == npc.type && i != npc.whoAmI)
                    pushAway += npc.SafeDirectionTo(Main.npc[i].Center, Vector2.UnitY) * Utils.InverseLerp(135f, 45f, npc.Distance(Main.npc[i].Center), true) * 1.8f;
            }
            idealVelocity += pushAway;

            idealVelocity *= 1f + npc.Distance(target.Center) / 2400f;
            if (BossRushEvent.BossRushActive)
                idealVelocity *= 2.1f;
            if (!npc.WithinRange(target.Center, 320f) || npc.velocity == Vector2.Zero || npc.velocity.Length() < 5f)
            {
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center) + offsetAngle, turnSpeedFactor * 0.023f, true) * idealVelocity.Length();
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, turnSpeedFactor * 0.03f);
                npc.velocity = npc.velocity.MoveTowards(idealVelocity, turnSpeedFactor * 0.56f);
            }

            if (npc.WithinRange(target.Center, 320f) && npc.velocity.Length() < idealVelocity.Length() * 1.6f)
                npc.velocity *= 1.03f;
        }

        public static void GotoNextAttackState(NPC npc)
        {
            float splitCounter = npc.ai[2];
            EoWAttackState oldAttackState = (EoWAttackState)(int)npc.ai[0];

            List<EoWAttackState> possibleAttacks = new List<EoWAttackState>
            {
                EoWAttackState.CursedBombBurst,
                EoWAttackState.VineCharge,
                EoWAttackState.ShadowOrbSummon,
            };
            possibleAttacks.AddWithCondition(EoWAttackState.RainHover, splitCounter >= 1f);

            for (int i = 0; i < 2; i++)
                possibleAttacks.AddWithCondition(EoWAttackState.DarkHeartSlam, splitCounter >= 2f);
            possibleAttacks.RemoveAll(p => p == oldAttackState);

            npc.Infernum().ExtraAI[7] = (int)possibleAttacks[Main.rand.Next(possibleAttacks.Count)];
            npc.Infernum().ExtraAI[8] = 0f;

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
        }

        public static void HandleSplit(NPC npc, ref float splitCounter)
        {
            // Delete all segments and create two new worms.
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].realLife == npc.whoAmI || i == npc.whoAmI)
                {
                    Main.npc[i].life = 0;
                    Main.npc[i].checkDead();
                    Main.npc[i].active = false;
                }
            }

            splitCounter++;

            // Create new worms with linked HP.
            int wormCount = (int)Math.Pow(2D, splitCounter);
            int realLife = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCID.EaterofWorldsHead, 1, ai2: splitCounter, ai3: npc.ai[3] * 0.5f, Target: npc.target);
            for (int i = 0; i < wormCount - 1; i++)
            {
                int secondWorm = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCID.EaterofWorldsHead, 1, ai2: splitCounter, ai3: npc.ai[3] * 0.5f, Target: npc.target);
                if (Main.npc.IndexInRange(secondWorm))
                {
                    Main.npc[secondWorm].realLife = realLife;
                    Main.npc[secondWorm].velocity = Main.rand.NextVector2CircularEdge(6f, 6f);
                }
            }

            npc.netUpdate = true;
        }

        public static void CreateSegments(NPC npc, int segmentCount, int bodyType, int tailType)
        {
            int previousIndex = npc.whoAmI;
            for (int i = 0; i < segmentCount + 1; i++)
            {
                int nextIndex;
                if (i < segmentCount)
                    nextIndex = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, bodyType, npc.whoAmI);
                else
                    nextIndex = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, tailType, npc.whoAmI);

                // The head.
                Main.npc[nextIndex].ai[2] = npc.whoAmI;

                // And the ahead segment.
                Main.npc[nextIndex].ai[1] = previousIndex;
                Main.npc[nextIndex].realLife = npc.realLife >= 0 ? npc.realLife : npc.whoAmI;

                // Mark an index based on whether it can be split at a specific split counter value.

                // Small worm split indices.
                if (i == BaseBodySegmentCount / 4 || i == BaseBodySegmentCount * 3 / 4)
                    Main.npc[nextIndex].ai[3] = 2f;

                // Medium worm split index.
                if (i == BaseBodySegmentCount / 2)
                    Main.npc[nextIndex].ai[3] = 1f;

                Main.npc[previousIndex].ai[0] = nextIndex;

                // Force sync the new segment into existence.
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, nextIndex, 0f, 0f, 0f, 0);

                previousIndex = nextIndex;
            }
        }

        #endregion AI Utility Methods
    }
}
