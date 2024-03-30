using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Events;
using CalamityMod.Projectiles.Boss;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Ravager;
using InfernumMode.Content.Projectiles.Pets;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.EoW
{
    public class EoWHeadBehaviorOverride : NPCBehaviorOverride
    {
        public enum EoWAttackState
        {
            CursedBombBurst,
            VineCharge,
            ShadowOrbSummon,
            RainHover,
            DownwardSlam
        }

        public override int NPCOverrideType => NPCID.EaterofWorldsHead;

        public static int CursedCinderDamage => 90;

        public static int ShadeNimbusDamage => 90;

        public static int CorruptThornVineDamage => 95;

        public static int CursedFlameBombDamage => 95;

        public static int ShockwaveDamage => 140;

        // This is applicable to all split worms as well.
        // Since split worms share HP, the total amount of HP of the boss is equal to Worm HP * (Total Splits + 1).
        public const int TotalLifeAcrossWorm = 4000;
        public const int TotalLifeAcrossWormBossRush = 376810;
        public const int BaseBodySegmentCount = 32;
        public const int TotalSplitsToPerform = 2;

        public const int EnrageTimerIndex = 5;

        public const int AttackStateIndex = 6;

        public const int AttackTimerIndex = 7;

        public override bool PreAI(NPC npc)
        {
            ref float splitCounter = ref npc.ai[2];
            ref float segmentCount = ref npc.ai[3];
            ref float initializedFlag = ref npc.localAI[0];
            ref float enrageTimer = ref npc.Infernum().ExtraAI[EnrageTimerIndex];
            ref float attackState = ref npc.Infernum().ExtraAI[AttackStateIndex];
            ref float attackTimer = ref npc.Infernum().ExtraAI[AttackTimerIndex];

            // Fuck.
            npc.Calamity().newAI[1] = Clamp(npc.Calamity().newAI[1] + 8f, 0f, 720f);
            npc.dontTakeDamage = npc.Calamity().newAI[1] < 700f;
            npc.damage = npc.dontTakeDamage ? 0 : npc.defDamage;

            // Fade in.
            npc.Opacity = Clamp(npc.Opacity + 0.15f, 0f, 1f);

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
                enrageTimer = Clamp(enrageTimer - 2.4f, 0f, 480f);
            else
                enrageTimer = Clamp(enrageTimer + 1f, 0f, 480f);

            bool enraged = enrageTimer >= 300f;
            npc.Calamity().CurrentlyEnraged = outOfBiome;

            if (target.dead)
            {
                npc.TargetClosestIfTargetIsInvalid();
                target = Main.player[npc.target];
                if (!target.dead)
                    return false;

                DoAttack_Despawn(npc);
                return false;
            }

            if (target.HasBuff(ModContent.BuffType<Shadowflame>()))
                target.ClearBuff(ModContent.BuffType<Shadowflame>());

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
                    DoAttack_RainHover(npc, target, splitCounter, ref attackTimer);
                    break;
                case EoWAttackState.DownwardSlam:
                    DoAttack_DownwardSlam(npc, target, enraged, ref attackTimer);
                    break;
            }

            // Split into two and two different life ratios.
            if (npc.realLife != -1)
            {
                npc.life = Main.npc[npc.realLife].life;
                npc.lifeMax = Main.npc[npc.realLife].lifeMax;
                npc.Infernum().ExtraAI[AttackStateIndex] = Main.npc[npc.realLife].Infernum().ExtraAI[AttackStateIndex];
                npc.Infernum().ExtraAI[AttackTimerIndex] = Main.npc[npc.realLife].Infernum().ExtraAI[AttackTimerIndex];
            }

            npc.rotation = npc.rotation.AngleLerp(npc.velocity.ToRotation() + PiOver2, 0.05f);
            npc.rotation = npc.rotation.AngleTowards(npc.velocity.ToRotation() + PiOver2, 0.15f);
            attackTimer++;

            return false;
        }

        public static bool PerformDeathEffect(NPC npc)
        {
            if (npc.realLife != -1 && Main.npc[npc.realLife].Infernum().ExtraAI[9] == 0f)
            {
                Main.npc[npc.realLife].NPCLoot();
                Main.npc[npc.realLife].Infernum().ExtraAI[9] = 1f;
                return false;
            }

            if (npc.ai[2] >= 2f)
            {
                npc.boss = true;

                if (npc.Infernum().ExtraAI[10] == 0f)
                {
                    npc.Infernum().ExtraAI[10] = 1f;
                    if (!BossRushEvent.BossRushActive)
                        npc.NPCLoot();
                }
            }

            else if (npc.realLife == -1 && npc.Infernum().ExtraAI[10] == 0f)
            {
                npc.Infernum().ExtraAI[10] = 1f;
                HandleSplit(npc, ref npc.ai[2]);
            }

            return npc.ai[2] >= 2f;
        }

        #region Attacks
        public static void DoAttack_CursedBombBurst(NPC npc, Player target, float splitCounter, bool enraged, ref float attackTimer)
        {
            int totalFireballsPerBurst = 1;
            float flySpeed = enraged ? 11f : 8f;
            float turnSpeedFactor = enraged ? 1.7f : 1f;
            flySpeed *= Lerp(1f, 1.35f, splitCounter / TotalSplitsToPerform);
            if (splitCounter == 0f)
            {
                flySpeed *= 1.15f;
                turnSpeedFactor *= 1.15f;
            }

            // Do default movement.
            DoDefaultMovement(npc, target, flySpeed, turnSpeedFactor);

            // Periodically release fireballs.
            int shootRate = splitCounter >= TotalSplitsToPerform - 1f ? 72 : 90;
            if (splitCounter == TotalSplitsToPerform)
                shootRate += 18;

            if (BossRushEvent.BossRushActive)
                shootRate = 38;

            if (attackTimer % shootRate == shootRate - 1f)
            {
                SoundEngine.PlaySound(SoundID.Item20, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < totalFireballsPerBurst; i++)
                    {
                        float shootOffsetAngle = 0f;
                        if (totalFireballsPerBurst > 1f)
                            shootOffsetAngle = Lerp(-0.84f, 0.84f, i / (float)(totalFireballsPerBurst - 1f));
                        Vector2 shootVelocity = npc.SafeDirectionTo(target.Center, -Vector2.UnitY).RotatedBy(shootOffsetAngle) * 7f;
                        Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<CursedFlameBomb>(), CursedFlameBombDamage, 0f);
                    }
                }
            }

            if (attackTimer >= 540f)
                SelectNextAttack(npc);
        }

        public static void DoAttack_VineCharge(NPC npc, Player target, float splitCounter, bool enraged, ref float attackTimer)
        {
            float flySpeed = enraged ? 15f : 9f;
            float turnSpeedFactor = enraged ? 1.3f : 0.85f;
            flySpeed *= Lerp(1f, 1.2f, splitCounter / TotalSplitsToPerform);

            if (attackTimer < 75f)
                flySpeed *= 0.6f;

            if (BossRushEvent.BossRushActive)
                flySpeed *= 2.15f;

            // Have the main head generate a bunch of thorns at the beginning.
            if (Main.netMode != NetmodeID.MultiplayerClient && npc.realLife == -1 && attackTimer == 15f)
            {
                float spacing = enraged ? 380f : 520f;
                spacing *= Lerp(1f, 1.5f, splitCounter / TotalSplitsToPerform);
                for (float dx = -2000f; dx < 2000f; dx += spacing)
                {
                    Vector2 spawnPosition = target.Bottom + Vector2.UnitX * dx;
                    Utilities.NewProjectileBetter(spawnPosition, Vector2.Zero, ModContent.ProjectileType<CorruptThorn>(), CorruptThornVineDamage, 0f);
                }
            }

            // Periodically shoot small flames.
            if (attackTimer % 120f == 119f)
            {
                SoundEngine.PlaySound(SoundID.Item20, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 shootVelocity = Main.rand.NextVector2CircularEdge(6f, 6f);
                        if (BossRushEvent.BossRushActive)
                            shootVelocity *= 3.2f;
                        Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<CursedBullet>(), CursedCinderDamage, 0f);
                    }
                }
            }

            float offsetAngle = Lerp(-0.76f, 0.76f, npc.whoAmI % 4f / 4f);
            offsetAngle *= Utils.GetLerpValue(100f, 350f, npc.Distance(target.Center), true);

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
                    SoundEngine.PlaySound(SoundID.Roar, target.Center);
                    npc.soundDelay = 80;
                }
            }

            if (attackTimer >= 360f)
                SelectNextAttack(npc);
        }

        public static void DoAttack_ShadowOrbSummon(NPC npc, Player target, float splitCounter, bool enraged, ref float attackTimer)
        {
            float flySpeed = enraged ? 12f : 8.25f;
            float turnSpeedFactor = enraged ? 1.2f : 0.8f;
            flySpeed *= Lerp(1f, 1.275f, splitCounter / TotalSplitsToPerform);
            if (splitCounter == 0f)
            {
                flySpeed *= 1.15f;
                turnSpeedFactor *= 1.15f;
            }

            if (BossRushEvent.BossRushActive)
                SelectNextAttack(npc);

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
                SelectNextAttack(npc);
        }

        public static void DoAttack_RainHover(NPC npc, Player target, float splitCounter, ref float attackTimer)
        {
            // Hover above the player.
            Vector2 hoverDestination = target.Center - Vector2.UnitY * 300f + target.velocity * 25f;
            float offsetAngle = Lerp(-0.76f, 0.76f, npc.whoAmI % 4f / 4f);
            offsetAngle *= Utils.GetLerpValue(70f, 240f, npc.Distance(hoverDestination), true);

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
                Utilities.NewProjectileBetter(cloudSpawnPosition, Vector2.Zero, ModContent.ProjectileType<ShadeNimbusHostile>(), ShadeNimbusDamage, 0f);
                HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.EoWRainTip");
            }

            if (attackTimer >= 480f)
                SelectNextAttack(npc);
        }


        public static void DoAttack_DownwardSlam(NPC npc, Player target, bool enraged, ref float attackTimer)
        {
            ref float wasPreviouslyInTiles = ref npc.Infernum().ExtraAI[11];

            int riseTime = 75;

            if (BossRushEvent.BossRushActive)
                riseTime -= 25;

            // Rise upward in anticipation of slamming into the target.
            if (attackTimer < riseTime)
            {
                float riseSpeed = !Collision.SolidCollision(npc.Center, 2, 2) ? 19f : 9f;
                npc.velocity.Y = Lerp(npc.velocity.Y, -riseSpeed, 0.045f);
                if (Distance(npc.Center.X, target.Center.X) > 300f)
                    npc.velocity.X = (npc.velocity.X * 24f + npc.SafeDirectionTo(target.Center).X * 10.5f) / 25f;
            }

            // Slam back down after the rise ends.
            if (attackTimer >= riseTime)
            {
                bool inTiles = Collision.SolidCollision(npc.Center, 2, 2);
                if (npc.velocity.Y < 26f)
                    npc.velocity.Y += enraged ? 0.9f : 0.5f;
                if (inTiles)
                    npc.velocity.Y = Clamp(npc.velocity.Y, -8f, 8f);

                // Release a shockwave and dark hearts once tiles have been hit.
                if (inTiles && wasPreviouslyInTiles == 0f)
                {
                    SoundEngine.PlaySound(SoundID.Item62, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<StompShockwave>(), ShockwaveDamage, 0f);
                        wasPreviouslyInTiles = 1f;
                    }

                    if (Distance(npc.Center.X, target.Center.X) > 240f)
                        npc.velocity.X = (npc.velocity.X * 21f + npc.SafeDirectionTo(target.Center).X * 10.5f) / 22f;
                    npc.netUpdate = true;
                }
            }

            if (wasPreviouslyInTiles == 1f && attackTimer < 600f)
                attackTimer = 600f;

            if (attackTimer > 660f)
                SelectNextAttack(npc);
        }
        #endregion

        #region AI Utility Methods

        public static void DoAttack_Despawn(NPC npc)
        {
            if (npc.timeLeft > 200)
                npc.timeLeft = 200;

            npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * 30f, 0.12f);
            npc.rotation = npc.velocity.ToRotation() + PiOver2;
            if (!npc.WithinRange(Main.player[npc.target].Center, 2200f))
                npc.active = false;
        }

        public static void DoDefaultMovement(NPC npc, Player target, float flySpeed, float turnSpeedFactor)
        {
            float offsetAngle = Lerp(-0.76f, 0.76f, npc.whoAmI % 4f / 4f);
            offsetAngle *= Utils.GetLerpValue(100f, 350f, npc.Distance(target.Center), true);

            Vector2 idealVelocity = npc.SafeDirectionTo(target.Center) * flySpeed * 0.95f;

            // Avoid other worm heads.
            Vector2 pushAway = Vector2.Zero;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                bool isEoW = n.type is NPCID.EaterofWorldsHead or NPCID.EaterofWorldsHead;
                if (isEoW && i != npc.whoAmI)
                    pushAway += npc.SafeDirectionTo(n.Center, Vector2.UnitY) * Utils.GetLerpValue(190f, 90f, npc.Distance(n.Center), true) * -4f;
            }
            idealVelocity += pushAway;

            idealVelocity *= 1f + npc.Distance(target.Center) / 2400f;
            if (BossRushEvent.BossRushActive)
                idealVelocity *= 2.1f;
            if (!npc.WithinRange(target.Center, 400f) || npc.velocity == Vector2.Zero || npc.velocity.Length() < 5f)
            {
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center) + offsetAngle, turnSpeedFactor * 0.023f, true) * idealVelocity.Length();
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, turnSpeedFactor * 0.03f);
                npc.velocity = npc.velocity.MoveTowards(idealVelocity, turnSpeedFactor * 0.56f);
            }

            if (npc.WithinRange(target.Center, 400f) && npc.velocity.Length() < idealVelocity.Length() * 1.6f)
                npc.velocity *= 1.03f;
        }

        public static void SelectNextAttack(NPC npc)
        {
            float splitCounter = npc.ai[2];
            EoWAttackState oldAttackState = (EoWAttackState)(int)npc.ai[0];

            List<EoWAttackState> possibleAttacks =
            [
                EoWAttackState.CursedBombBurst,
                EoWAttackState.VineCharge,
                EoWAttackState.ShadowOrbSummon,
            ];
            possibleAttacks.AddWithCondition(EoWAttackState.RainHover, splitCounter >= 1f);

            for (int i = 0; i < 2; i++)
                possibleAttacks.AddWithCondition(EoWAttackState.DownwardSlam, splitCounter >= 2f);
            possibleAttacks.RemoveAll(p => p == oldAttackState);

            npc.TargetClosest();
            npc.Infernum().ExtraAI[AttackStateIndex] = (int)possibleAttacks[Main.rand.Next(possibleAttacks.Count)];
            npc.Infernum().ExtraAI[AttackTimerIndex] = 0f;

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
            int realLife = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, NPCID.EaterofWorldsHead, 1, ai2: splitCounter, ai3: npc.ai[3] * 0.5f, Target: npc.target);
            for (int i = 0; i < wormCount - 1; i++)
            {
                int secondWorm = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, NPCID.EaterofWorldsHead, 1, ai2: splitCounter, ai3: npc.ai[3] * 0.5f, Target: npc.target);
                if (Main.npc.IndexInRange(secondWorm))
                {
                    Main.npc[secondWorm].realLife = realLife;
                    Main.npc[secondWorm].velocity = Main.rand.NextVector2CircularEdge(6f, 6f);
                }
            }

            Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<CorruptThorn>());

            npc.netUpdate = true;
        }

        public static void CreateSegments(NPC npc, int segmentCount, int bodyType, int tailType)
        {
            int previousIndex = npc.whoAmI;
            for (int i = 0; i < segmentCount + 1; i++)
            {
                int nextIndex;
                if (i < segmentCount)
                    nextIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, bodyType, npc.whoAmI);
                else
                    nextIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, tailType, npc.whoAmI);

                // The head.
                Main.npc[nextIndex].ai[2] = npc.whoAmI;

                // And the ahead segment.
                Main.npc[nextIndex].ai[1] = previousIndex;
                Main.npc[nextIndex].realLife = npc.realLife >= 0 ? npc.realLife : npc.whoAmI;

                // Mark an index based on whether it can be split at a specific split counter value.

                // Small worm split indices.
                if (i is (BaseBodySegmentCount / 4) or (BaseBodySegmentCount * 3 / 4))
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

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Mods.InfernumMode.PetDialog.EoWTip1";
            yield return n => "Mods.InfernumMode.PetDialog.EoWTip2";

            yield return n =>
            {
                if (TipsManager.ShouldUseJokeText)
                    return "Mods.InfernumMode.PetDialog.EoWJokeTip1";
                return string.Empty;
            };
        }
        #endregion Tips
    }
}
