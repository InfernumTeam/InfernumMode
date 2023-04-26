using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.HiveMind;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Boss;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Content.Projectiles.Pets;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using HiveMindBoss = CalamityMod.NPCs.HiveMind.HiveMind;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.HiveMind
{
    public class HiveMindBehaviorOverride : NPCBehaviorOverride
    {
        public enum HiveMindAttackState
        {
            SuspensionStateDrift = -1,
            Reset,
            NPCSpawnArc,
            SpinLunge,
            CloudDash,
            EaterOfSoulsWall,
            UndergroundFlameDash,
            CursedRain,
            SlowDown,
            BlobBurst
        }

        public override int NPCOverrideType => ModContent.NPCType<HiveMindBoss>();

        public static int VileClotDamage => 85;

        public static int EaterOfSoulsWallDamage => 90;

        public static int ShadeNimbusDamage => 90;

        public static int ShadeRainDamage => 90;

        public static int HiveBlobDamage => 95;

        public static int ShadeFireDamage => 100;

        public const float FinalPhaseLifeRatio = 0.2f;

        public const float HiveMindFadeoutTime = 25f;

        public const float SpinRadius = 420f;

        public const float NPCSpawnArcSpinTime = 25f;

        public const float NPCSpawnArcRotationalOffset = MathHelper.Pi / NPCSpawnArcSpinTime;

        public const float LungeSpinTotalRotations = 2f;

        public const float LungeSpinChargeDelay = 6f;

        public const float LungeSpinChargeTime = 20f;

        public const float RainDashOffset = 456f;

        public const float EaterWallSlowdownTime = 40f;

        public const float EaterWallSummoningTime = 60f;

        public const float EaterWallTotalHeight = 1900f;

        public const float MaxSlowdownTime = 60f;

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            FinalPhaseLifeRatio
        };

        public override bool PreAI(NPC npc)
        {
            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Do defense damage on contact.
            npc.Calamity().canBreakPlayerDefense = true;

            bool below20 = lifeRatio < FinalPhaseLifeRatio || npc.Infernum().ExtraAI[10] == 1f;
            ref float attackTimer = ref npc.ai[3];
            ref float slowdownCountdown = ref npc.Infernum().ExtraAI[4];
            ref float fadeoutCountdown = ref npc.Infernum().ExtraAI[6];
            ref float afterimagePulse = ref npc.Infernum().ExtraAI[7];
            ref float finalPhaseInvinciblityTime = ref npc.Infernum().ExtraAI[11];
            ref float flameColumnCountdown = ref npc.Infernum().ExtraAI[12];
            ref float enrageTimer = ref npc.Infernum().ExtraAI[14];
            ref float hasSummonedInitialBlobsFlag = ref npc.localAI[0];
            ref float digTime = ref npc.localAI[3];
            ref float shootTimer = ref npc.Infernum().ExtraAI[0];

            // Kill debuffs.
            if (target.HasBuff(BuffID.CursedInferno))
                target.ClearBuff(BuffID.CursedInferno);
            if (target.HasBuff(ModContent.BuffType<Shadowflame>()))
                target.ClearBuff(ModContent.BuffType<Shadowflame>());

            // Act as though the boss is at 20% life if that amount of life has already been reached.
            // This is done to ensure that the regeneration doesn't weaken attacks.
            if (below20 && lifeRatio > FinalPhaseLifeRatio)
                lifeRatio = FinalPhaseLifeRatio * 0.99f;

            bool outOfBiome = !target.ZoneCrimson && !target.ZoneCorrupt && !BossRushEvent.BossRushActive;
            bool enraged = enrageTimer > 300f;

            enrageTimer = MathHelper.Clamp(enrageTimer + outOfBiome.ToDirectionInt(), 0f, 480f);
            npc.defense = enraged ? -5 : 9999;
            npc.Calamity().CurrentlyEnraged = outOfBiome;
            npc.noTileCollide = true;
            npc.noGravity = true;
            npc.Calamity().DR = 0f;

            CalamityGlobalNPC.hiveMind = npc.whoAmI;

            if (below20 && npc.Infernum().ExtraAI[10] == 0f)
            {
                npc.Infernum().ExtraAI[10] = 1f;
                finalPhaseInvinciblityTime = 90f;
                HatGirl.SayThingWhileOwnerIsAlive(target, "It's still going?!");
                npc.netUpdate = true;
            }

            // Rise after entering the invincibility phase.
            if (finalPhaseInvinciblityTime == 60f)
            {
                npc.velocity = Vector2.UnitY * -12f;
                npc.netUpdate = true;
            }

            if (fadeoutCountdown > 0f)
                fadeoutCountdown--;

            // Fade away and despawn if the player dies.
            if (!target.active || target.dead)
            {
                npc.TargetClosest();
                target = Main.player[npc.target];
                if (!target.active || target.dead)
                {
                    npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * 16f, 0.1f);
                    if (!npc.WithinRange(target.Center, 600f))
                    {
                        npc.life = 0;
                        npc.active = false;
                        npc.netUpdate = true;
                    }

                    if (npc.timeLeft > 240)
                        npc.timeLeft = 240;
                    return false;
                }
            }

            if (finalPhaseInvinciblityTime > 0f)
            {
                npc.knockBackResist = 0f;
                npc.life = (int)MathHelper.Lerp(npc.lifeMax * 0.2f, npc.lifeMax * 0.4f, 1f - finalPhaseInvinciblityTime / 90f);
                npc.ai = new float[] { 0f, 0f, 0f, 0f };
                npc.Infernum().ExtraAI[0] = (int)HiveMindAttackState.SuspensionStateDrift;
                npc.Infernum().ExtraAI[1] = npc.Infernum().ExtraAI[2] = npc.Infernum().ExtraAI[3] = 0f;
                npc.Infernum().ExtraAI[15] = npc.Infernum().ExtraAI[16] = 0f;
                npc.velocity *= 0.825f;
                npc.defense = 9999;
                afterimagePulse += 0.24f;
                finalPhaseInvinciblityTime--;
                return false;
            }

            if (flameColumnCountdown > 0f)
            {
                flameColumnCountdown--;
            }

            npc.defense = below20 ? 9 : 7;

            switch ((HiveMindAttackState)(int)npc.Infernum().ExtraAI[0])
            {
                case HiveMindAttackState.SuspensionStateDrift:
                    DoBehavior_SuspensionStateDrift(npc, target, lifeRatio, ref npc.ai[0]);
                    break;
                case HiveMindAttackState.Reset:
                    DoBehavior_ResetAI(npc, lifeRatio);
                    break;
                case HiveMindAttackState.NPCSpawnArc:
                    DoBehavior_NPCSpawnArc(npc, target, enraged, ref fadeoutCountdown, ref slowdownCountdown, ref attackTimer);
                    break;
                case HiveMindAttackState.SpinLunge:
                    DoBehavior_SpinLunge(npc, target, enraged, lifeRatio, ref fadeoutCountdown, ref slowdownCountdown, ref attackTimer);
                    break;
                case HiveMindAttackState.CloudDash:
                    DoBehavior_CloudDash(npc, target, enraged, lifeRatio, ref slowdownCountdown, ref attackTimer);
                    break;
                case HiveMindAttackState.EaterOfSoulsWall:
                    DoBehavior_EaterWall(npc, target, enraged, lifeRatio, ref slowdownCountdown, ref attackTimer);
                    break;
                case HiveMindAttackState.UndergroundFlameDash:
                    DoBehavior_UndergroundFlameDash(npc, target, enraged, lifeRatio, ref attackTimer);
                    break;
                case HiveMindAttackState.CursedRain:
                    DoBehavior_CursedRain(npc, target, enraged, lifeRatio, ref flameColumnCountdown, ref attackTimer);
                    break;
                case HiveMindAttackState.SlowDown:
                    DoBehavior_SlowDown(npc, ref slowdownCountdown);
                    break;
                case HiveMindAttackState.BlobBurst:
                    DoBehavior_BlobBurst(npc, target, enraged, lifeRatio, ref fadeoutCountdown, ref slowdownCountdown, ref attackTimer);
                    break;
            }

            // Update the afterimage pulse.
            if (npc.Infernum().ExtraAI[6] <= 0f || npc.Infernum().ExtraAI[0] >= 4 && npc.Infernum().ExtraAI[0] <= 7 || below20 || npc.Infernum().ExtraAI[6] > 0f)
                afterimagePulse += 0.14f;

            return false;
        }

        public static void DoBehavior_SuspensionStateDrift(NPC npc, Player target, float lifeRatio, ref float attackTimer)
        {
            float driftTime = lifeRatio < FinalPhaseLifeRatio ? 75f : 150f;
            float reelbackTime = 45f;
            bool fadingAway = attackTimer > driftTime - reelbackTime;

            npc.alpha = Utils.Clamp(npc.alpha + fadingAway.ToInt() * 9, 0, 255);
            npc.dontTakeDamage = fadingAway;

            // Reset knockback resistance.
            if (npc.knockBackResist == 0f)
                npc.knockBackResist = 1f;

            // Fly off if hit and accelerate.
            ref float hasBeenHitFlag = ref npc.Infernum().ExtraAI[13];
            if (npc.justHit)
            {
                hasBeenHitFlag = 1f;
                npc.velocity = npc.SafeDirectionTo(target.Center) * -8.5f;
                if (attackTimer < driftTime - reelbackTime)
                {
                    attackTimer = driftTime - reelbackTime;
                    DoRoar(npc, true);
                }

                npc.netUpdate = true;
            }
            if (hasBeenHitFlag == 1f)
                npc.velocity *= 1.06f;
            else
            {
                npc.velocity = npc.SafeDirectionTo(target.Center) * 190f / reelbackTime;
                if (BossRushEvent.BossRushActive)
                    npc.velocity *= 4.5f;

                if (npc.WithinRange(target.Center, 15f))
                {
                    npc.velocity = Vector2.Zero;
                    npc.Center = target.Center;
                }
            }

            if (attackTimer >= driftTime)
            {
                // Remove knockback again after going back to the attacking.
                npc.knockBackResist = 0f;

                HiveMindAttackState nextAttack = (HiveMindAttackState)(int)npc.Infernum().ExtraAI[5];
                bool shouldBecomeInvisible =
                    nextAttack is HiveMindAttackState.NPCSpawnArc or
                    HiveMindAttackState.SpinLunge or
                    HiveMindAttackState.CloudDash or
                    HiveMindAttackState.UndergroundFlameDash or
                    HiveMindAttackState.EaterOfSoulsWall or
                    HiveMindAttackState.CursedRain or
                    HiveMindAttackState.BlobBurst;
                if (shouldBecomeInvisible)
                {
                    if (nextAttack is HiveMindAttackState.EaterOfSoulsWall or HiveMindAttackState.CursedRain)
                    {
                        npc.Center = target.Center - Vector2.UnitY * 350f;
                        npc.velocity = Vector2.Zero;
                    }
                    if (nextAttack == HiveMindAttackState.BlobBurst)
                    {
                        npc.Center = target.Center - Vector2.UnitY * 400f;
                        npc.velocity = Vector2.Zero;
                    }

                    npc.alpha = 255;
                }
                else
                    npc.alpha = 0;

                npc.Infernum().ExtraAI[0] = (int)nextAttack;
                hasBeenHitFlag = 0f;
                npc.dontTakeDamage = false;
                npc.velocity *= 0.05f;
                npc.netUpdate = true;
            }
            attackTimer++;
        }

        public static void DoBehavior_ResetAI(NPC npc, float lifeRatio)
        {
            npc.TargetClosest(false);

            HiveMindAttackState nextAttack;
            HiveMindAttackState previousAttack = (HiveMindAttackState)(int)npc.Infernum().ExtraAI[9];
            do
            {
                nextAttack = Utils.SelectRandom(Main.rand,
                    lifeRatio < 0.72f ? HiveMindAttackState.EaterOfSoulsWall : HiveMindAttackState.NPCSpawnArc,
                    lifeRatio < 0.56f && Main.rand.NextBool() ? HiveMindAttackState.UndergroundFlameDash : HiveMindAttackState.SpinLunge,
                    lifeRatio < 0.39f ? HiveMindAttackState.CursedRain : HiveMindAttackState.CloudDash);
            }
            while (nextAttack == previousAttack);

            if (lifeRatio < FinalPhaseLifeRatio && Main.rand.NextBool(4) && previousAttack != HiveMindAttackState.BlobBurst)
                nextAttack = HiveMindAttackState.BlobBurst;

            // Reset things.
            npc.ai = new float[] { 0f, 0f, 0f, 0f };
            npc.Infernum().ExtraAI[0] = -1f;
            npc.Infernum().ExtraAI[1] = npc.Infernum().ExtraAI[2] = npc.Infernum().ExtraAI[3] = 0f;
            npc.Infernum().ExtraAI[5] = (int)nextAttack;
            npc.netUpdate = true;
        }

        public static void DoBehavior_NPCSpawnArc(NPC npc, Player target, bool enraged, ref float fadeoutCountdown, ref float slowdownCountdown, ref float attackTimer)
        {
            int spawnCount = enraged ? 9 : 6;
            ref float hasFadedInFlag = ref npc.ai[1];
            ref float spinDirection = ref npc.Infernum().ExtraAI[1];
            ref float spawnedEnemyCount = ref npc.Infernum().ExtraAI[2];

            // Declare the previous attack for later.
            npc.Infernum().ExtraAI[9] = (int)npc.Infernum().ExtraAI[5];

            if (npc.alpha >= 0 && hasFadedInFlag == 0f)
            {
                npc.alpha -= 4;
                npc.Center = target.Center + Vector2.UnitY * SpinRadius;
                npc.velocity = Vector2.Zero;
                if (npc.alpha <= 0f)
                {
                    DoRoar(npc, false);
                    fadeoutCountdown = HiveMindFadeoutTime;
                    spinDirection = Main.rand.NextBool().ToDirectionInt();
                    npc.velocity = Vector2.UnitX * MathHelper.Pi * SpinRadius / NPCSpawnArcSpinTime * spinDirection;
                    npc.alpha = 0;
                    hasFadedInFlag = 1f;
                    npc.netUpdate = true;
                }
            }

            // Do the spin.
            if (hasFadedInFlag == 1f)
            {
                npc.velocity = npc.velocity.RotatedBy(NPCSpawnArcRotationalOffset * -spinDirection);
                if (attackTimer % (int)Math.Ceiling(NPCSpawnArcSpinTime / spawnCount) == (int)Math.Ceiling(NPCSpawnArcSpinTime / spawnCount) - 1)
                {
                    spawnedEnemyCount++;

                    // Spawn things if nothing is in the way of the target.
                    if (Main.netMode != NetmodeID.MultiplayerClient && Collision.CanHit(npc.Center, 1, 1, target.position, target.width, target.height))
                    {
                        if (spawnedEnemyCount is 2 or 4)
                        {
                            if (!NPC.AnyNPCs(ModContent.NPCType<DarkHeart>()))
                                NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<DarkHeart>());
                        }
                        else if (NPC.CountNPCS(NPCID.EaterofSouls) < 2)
                            NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, NPCID.EaterofSouls);
                    }

                    // Reset to the slowdown state in preparation for the next attack.
                    if (spawnedEnemyCount >= spawnCount)
                    {
                        slowdownCountdown = MaxSlowdownTime;
                        npc.Infernum().ExtraAI[0] = (int)HiveMindAttackState.SlowDown;
                        npc.netUpdate = true;
                    }
                }
            }
            attackTimer++;
        }

        public static void DoBehavior_SpinLunge(NPC npc, Player target, bool enraged, float lifeRatio, ref float fadeoutCountdown, ref float slowdownCountdown, ref float attackTimer)
        {
            int teleportDelay = 60;
            int spinTime = lifeRatio < FinalPhaseLifeRatio ? 75 : 90;
            ref float spinDirection = ref npc.Infernum().ExtraAI[1];
            ref float spinIncrement = ref npc.Infernum().ExtraAI[2];
            ref float initialSpinRotation = ref npc.Infernum().ExtraAI[3];
            ref float teleportOffsetX = ref npc.Infernum().ExtraAI[15];
            ref float teleportOffsetY = ref npc.Infernum().ExtraAI[16];

            // Delare the previous attack for later.
            npc.Infernum().ExtraAI[9] = (int)npc.Infernum().ExtraAI[5];

            // Decide where to teleport to.
            npc.dontTakeDamage = false;
            if (attackTimer == 0f)
            {
                initialSpinRotation = Main.rand.NextFloat(MathHelper.TwoPi);

                Vector2 teleportPosition = initialSpinRotation.ToRotationVector2() * SpinRadius;
                teleportOffsetX = teleportPosition.X;
                teleportOffsetY = teleportPosition.Y;
                npc.netUpdate = true;
            }

            attackTimer++;

            // Fade out and create a gross corruption cloud at the teleport point before teleporting.
            if (attackTimer <= teleportDelay)
            {
                Vector2 teleportPosition = target.Center + new Vector2(teleportOffsetX, teleportOffsetY);
                CreateTeleportTelegraph(teleportPosition, 0.65f);

                npc.Opacity = Utils.GetLerpValue(teleportDelay - 16f, 0f, attackTimer, true);
                npc.velocity *= 0.94f;
                npc.dontTakeDamage = true;
                return;
            }

            spinIncrement += (float)Math.Pow(Utils.GetLerpValue(MaxSlowdownTime + LungeSpinChargeDelay * 0.85f, MaxSlowdownTime, attackTimer - teleportDelay, true), 0.6D);

            // Decide the spin direction if it has yet to be.
            while (spinDirection == 0f)
                spinDirection = Main.rand.NextBool().ToDirectionInt();

            // Lunge.
            if (attackTimer == teleportDelay + spinTime + LungeSpinChargeDelay)
            {
                DoRoar(npc, false);

                npc.velocity = npc.SafeDirectionTo(target.Center) * SpinRadius / MaxSlowdownTime * 3.6f;
                npc.velocity *= MathHelper.Lerp(1f, 1.3f, Utils.GetLerpValue(1f, 0.6f, lifeRatio));
                if (enraged)
                    npc.velocity *= 1.45f;

                if (BossRushEvent.BossRushActive)
                    npc.velocity *= 2.5f;

                fadeoutCountdown = HiveMindFadeoutTime;
                npc.netUpdate = true;
            }

            // Do the spin.
            else if (attackTimer < teleportDelay + spinTime + LungeSpinChargeDelay)
            {
                npc.Opacity = 1f;
                npc.velocity = Vector2.Zero;
                npc.Center = target.Center + (MathHelper.TwoPi * LungeSpinTotalRotations * spinIncrement * spinDirection / spinTime + initialSpinRotation).ToRotationVector2() * SpinRadius;

                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 12f == 11f && attackTimer < teleportDelay + MaxSlowdownTime)
                {
                    Vector2 clotVelocity = npc.SafeDirectionTo(target.Center) * 2.1f;
                    int fuck = Utilities.NewProjectileBetter(npc.Center, clotVelocity, ModContent.ProjectileType<VileClot>(), VileClotDamage, 1f);
                    Main.projectile[fuck].tileCollide = false;
                }
            }

            // Reset to the slowdown state in preparation for the next attack.
            if (attackTimer > teleportDelay + spinTime + LungeSpinChargeTime + LungeSpinChargeDelay * 0.45f)
            {
                slowdownCountdown = MaxSlowdownTime;
                teleportOffsetX = 0f;
                teleportOffsetY = 0f;
                npc.Infernum().ExtraAI[0] = (int)HiveMindAttackState.SlowDown;
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_CloudDash(NPC npc, Player target, bool enraged, float lifeRatio, ref float slowdownCountdown, ref float attackTimer)
        {
            if (lifeRatio < 0.4f)
            {
                npc.Infernum().ExtraAI[0] = (int)HiveMindAttackState.CursedRain;
                npc.netUpdate = true;
                return;
            }

            int teleportDelay = 60;
            ref float cloudSummonCounter = ref npc.Infernum().ExtraAI[1];
            ref float dashDirection = ref npc.Infernum().ExtraAI[2];
            ref float teleportPositionX = ref npc.Infernum().ExtraAI[15];
            ref float teleportPositionY = ref npc.Infernum().ExtraAI[16];

            // Delare the previous attack for later.
            npc.Infernum().ExtraAI[9] = (int)npc.Infernum().ExtraAI[5];

            // Initialize by becoming invisible initially.
            if (npc.ai[0] == 0f)
            {
                npc.alpha = 255;
                npc.ai[0] = 1f;
            }

            attackTimer++;

            if (attackTimer <= teleportDelay)
            {
                // Initialize the teleport position.
                if (attackTimer == 1f)
                {
                    Vector2 teleportPosition = target.Center;
                    while (dashDirection == 0f)
                        dashDirection = Main.rand.NextBool().ToDirectionInt();

                    teleportPosition.Y -= RainDashOffset * MathHelper.Lerp(1.2f, 1.5f, Utils.GetLerpValue(1f, 0.4f, lifeRatio, true));
                    teleportPosition.X += RainDashOffset * dashDirection;
                    teleportPositionX = teleportPosition.X;
                    teleportPositionY = teleportPosition.Y;
                    npc.netUpdate = true;
                }

                // Create the teleport telegraph.
                CreateTeleportTelegraph(new(teleportPositionX, teleportPositionY));

                npc.Opacity = Utils.GetLerpValue(teleportDelay - 8f, 0f, attackTimer, true);
                npc.velocity *= 0.94f;

                return;
            }

            if (npc.alpha > 0)
            {
                npc.alpha -= 4;
                npc.Center = new(teleportPositionX, teleportPositionY);
                if (npc.alpha <= 0)
                {
                    DoRoar(npc, true);
                    npc.velocity = Vector2.UnitX * dashDirection * -10.25f;
                    npc.velocity *= MathHelper.Lerp(1f, 1.3f, Utils.GetLerpValue(1f, 0.4f, lifeRatio, true));
                    if (enraged)
                        npc.velocity *= 1.5f;
                    if (BossRushEvent.BossRushActive)
                        npc.velocity *= 2.4f;
                    npc.netUpdate = true;
                }
            }

            // Release various clouds.
            else if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 3f == 0f)
            {
                Vector2 cloudSpawnPosition = npc.Center + new Vector2(Main.rand.NextFloatDirection(), Main.rand.NextFloatDirection()) * npc.Size * 0.5f;
                int cloud = Utilities.NewProjectileBetter(cloudSpawnPosition, Vector2.Zero, ModContent.ProjectileType<ShadeNimbusHostile>(), ShadeNimbusDamage, 0, Main.myPlayer, 11, 0);
                if (!Main.projectile.IndexInRange(cloud))
                {
                    Main.projectile[cloud].ai[0] = 11f;
                    Main.projectile[cloud].netUpdate = true;
                }
                cloudSummonCounter++;
            }

            // Reset to the slowdown state in preparation for the next attack.
            if (cloudSummonCounter >= 15f)
            {
                npc.alpha = 255;
                dashDirection *= -1f;
                slowdownCountdown = MaxSlowdownTime / 2;
                teleportPositionX = 0f;
                teleportPositionY = 0f;
                npc.Infernum().ExtraAI[0] = (int)HiveMindAttackState.SlowDown;
                npc.netUpdate = true;
            }

            // Release rain.
            Vector2 rainSpawnPosition = npc.position + new Vector2(Main.rand.NextFloat(14f, npc.width - 14f), npc.height + 4f);
            Utilities.NewProjectileBetter(rainSpawnPosition, Vector2.UnitY * 3f, ModContent.ProjectileType<ShaderainHostile>(), ShadeRainDamage, 0f, Main.myPlayer, 0f, 0f);
        }

        public static void DoBehavior_EaterWall(NPC npc, Player target, bool enraged, float lifeRatio, ref float slowdownCountdown, ref float attackTimer)
        {
            ref float verticalSpawnOffset = ref npc.Infernum().ExtraAI[1];

            // Delare the previous attack for later.
            npc.Infernum().ExtraAI[9] = (int)npc.Infernum().ExtraAI[5];

            // Fade in.
            npc.alpha = Utils.Clamp(npc.alpha - 24, 0, 255);

            // Initialize by rising upward.
            if (npc.ai[0] == 0f)
            {
                npc.velocity = Vector2.UnitY * -12f;
                npc.ai[0] = 1f;
                npc.netUpdate = true;
            }

            attackTimer++;

            // Slow down after the initial rise.
            if (npc.ai[3] < EaterWallSlowdownTime)
                npc.velocity *= 0.95f;

            // Roar and prepare for the wall.
            else if (npc.ai[3] == EaterWallSlowdownTime)
            {
                DoRoar(npc, false);
                npc.velocity = Vector2.Zero;
                verticalSpawnOffset = Main.rand.NextFloat(35f);
                npc.netUpdate = true;
            }

            // And release the Eater of Souls wall.
            else
            {
                verticalSpawnOffset += EaterWallTotalHeight / EaterWallSummoningTime * (lifeRatio < FinalPhaseLifeRatio ? 4.35f : 4.15f);

                Vector2 wallSpawnOffset = new(-1200f, verticalSpawnOffset - EaterWallTotalHeight / 2f);
                Vector2 wallVelocity = Vector2.UnitX.RotatedBy(lifeRatio < FinalPhaseLifeRatio ? MathHelper.ToRadians(10f) : 0f) * 10f;

                if (enraged)
                    wallVelocity *= 1.35f;
                if (BossRushEvent.BossRushActive)
                    wallVelocity *= 1.7f;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Utilities.NewProjectileBetter(target.Center + wallSpawnOffset, wallVelocity, ModContent.ProjectileType<EaterOfSouls>(), EaterOfSoulsWallDamage, 1f);
                    Utilities.NewProjectileBetter(target.Center + wallSpawnOffset * new Vector2(-1f, 1f), wallVelocity * new Vector2(-1f, 1f), ModContent.ProjectileType<EaterOfSouls>(), EaterOfSoulsWallDamage, 1f);
                }

                // Reset to the slowdown state in preparation for the next attack.
                if (npc.ai[3] > EaterWallSlowdownTime + EaterWallSummoningTime)
                {
                    npc.alpha = 255;
                    slowdownCountdown = MaxSlowdownTime / 2;
                    npc.Infernum().ExtraAI[0] = (int)HiveMindAttackState.SlowDown;
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoBehavior_UndergroundFlameDash(NPC npc, Player target, bool enraged, float lifeRatio, ref float attackTimer)
        {
            int teleportDelay = 48;
            ref float dashDirection = ref npc.Infernum().ExtraAI[1];
            ref float initializedFlag = ref npc.Infernum().ExtraAI[2];
            ref float teleportOffsetX = ref npc.Infernum().ExtraAI[15];
            ref float teleportOffsetY = ref npc.Infernum().ExtraAI[16];

            // Delare the previous attack for later.
            npc.Infernum().ExtraAI[9] = (int)npc.Infernum().ExtraAI[5];

            if (initializedFlag == 0f)
            {
                npc.velocity = Vector2.Zero;
                dashDirection = Main.rand.NextBool().ToDirectionInt();

                float horizontalTeleportOffset = MathHelper.Lerp(510f, 400f, 1f - lifeRatio);
                if (lifeRatio < FinalPhaseLifeRatio)
                    horizontalTeleportOffset -= 75f;

                teleportOffsetX = horizontalTeleportOffset * -dashDirection;
                teleportOffsetY = 350f;
                initializedFlag = 1f;
                npc.netUpdate = true;
            }

            float waitTime = lifeRatio < FinalPhaseLifeRatio ? 70f : 50f;
            float moveTime = lifeRatio < FinalPhaseLifeRatio ? 50f : 80f;
            float dashSpeed = lifeRatio < FinalPhaseLifeRatio ? 24f : 22.25f;
            if (enraged)
            {
                waitTime = (int)(waitTime * 0.67f);
                moveTime = (int)(moveTime * 0.64f);
                dashSpeed *= 1.35f;
            }
            if (BossRushEvent.BossRushActive)
            {
                waitTime = (int)(waitTime * 0.4f);
                moveTime *= 0.5f;
                dashSpeed *= 1.64f;
            }

            attackTimer++;
            if (attackTimer <= teleportDelay)
            {
                npc.Opacity = Utils.GetLerpValue(teleportDelay - 6f, 0f, attackTimer, true);

                CreateTeleportTelegraph(target.Center + new Vector2(teleportOffsetX, teleportOffsetY));
                if (attackTimer == teleportDelay)
                {
                    npc.Center = target.Center + new Vector2(teleportOffsetX, teleportOffsetY);
                    npc.netUpdate = true;
                }
                return;
            }

            npc.alpha = Utils.Clamp(npc.alpha - 36, 0, 255);

            // Constantly shoot shade flames upward.
            if (npc.alpha <= 0)
                Utilities.NewProjectileBetter(npc.Center - Vector2.UnitY * 40f, Vector2.UnitY.RotatedByRandom(0.09f) * -9.75f, ModContent.ProjectileType<ShadeFire>(), ShadeFireDamage, 0f);

            // Roar and dash.
            if (attackTimer == teleportDelay + waitTime)
            {
                DoRoar(npc, false);
                npc.velocity = Vector2.UnitX * dashDirection * dashSpeed;
                npc.netUpdate = true;
            }

            // Release clots upward if below the necessary phase threshold.
            if (attackTimer > teleportDelay + waitTime && lifeRatio < FinalPhaseLifeRatio && attackTimer % 7f == 6f)
            {
                int vileClot = Utilities.NewProjectileBetter(npc.Center, -Vector2.UnitY.RotatedByRandom(0.4f) * Main.rand.NextFloat(7f, 9f), ModContent.ProjectileType<VileClot>(), VileClotDamage, 0f);
                Main.projectile[vileClot].tileCollide = false;
            }

            // Reset to the slowdown state in preparation for the next attack.
            if (attackTimer == teleportDelay + waitTime + moveTime)
                npc.Infernum().ExtraAI[0] = (int)HiveMindAttackState.SlowDown;
        }

        public static void DoBehavior_CursedRain(NPC npc, Player target, bool enraged, float lifeRatio, ref float flameColumnCountdown, ref float attackTimer)
        {
            ref float initializedFlag = ref npc.Infernum().ExtraAI[2];

            // Delare the previous attack for later.
            npc.Infernum().ExtraAI[9] = (int)npc.Infernum().ExtraAI[5];

            npc.velocity = Vector2.Zero;

            // Initialize by becoming invisible.
            if (initializedFlag == 0f)
            {
                npc.alpha = 255;
                npc.Center = target.Center - Vector2.UnitY * 420f;
                initializedFlag = 1f;
            }

            if (npc.alpha > 0)
            {
                npc.alpha -= 6;
                if (npc.alpha <= 0)
                {
                    DoRoar(npc, false);
                    npc.alpha = 0;
                }
                npc.netUpdate = true;
            }
            else
            {
                attackTimer++;

                int clotSpawnRate = lifeRatio < FinalPhaseLifeRatio ? 9 : 11;
                int cloudSpawnRate = lifeRatio < FinalPhaseLifeRatio ? 30 : 36;
                int attackTime = lifeRatio < FinalPhaseLifeRatio ? 210 : 270;
                if (enraged)
                {
                    clotSpawnRate /= 2;
                    cloudSpawnRate -= 14;
                }

                // Release clouds and clots.
                if (npc.ai[3] % clotSpawnRate == clotSpawnRate - 1f)
                {
                    Vector2 clotSpawnPosition = target.Center + new Vector2(Main.rand.NextFloatDirection() * 540f, Main.rand.NextFloat(-555f, -505f));
                    Vector2 clotVelocity = Vector2.UnitY.RotatedByRandom(MathHelper.ToRadians(36f)) * 8f;

                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(clot =>
                    {
                        clot.tileCollide = false;
                    });
                    Utilities.NewProjectileBetter(clotSpawnPosition, clotVelocity, ModContent.ProjectileType<VileClot>(), VileClotDamage, 1f);
                }
                if (npc.ai[3] % cloudSpawnRate == cloudSpawnRate - 1f)
                {
                    Vector2 cloudSpawnPosition = target.Center + new Vector2(Main.rand.NextFloatDirection() * 400f, Main.rand.NextFloat(-605f, -545f));
                    Utilities.NewProjectileBetter(cloudSpawnPosition, Vector2.Zero, ModContent.ProjectileType<ShadeNimbusHostile>(), ShadeNimbusDamage, 1f);
                }

                // Make flame columns appear.
                if ((int)attackTimer == 160f)
                {
                    flameColumnCountdown = (lifeRatio < FinalPhaseLifeRatio ? 4f : 3f) * 60f;
                    npc.netUpdate = true;
                }

                if (attackTimer >= attackTime)
                {
                    Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<VileClot>(), ModContent.ProjectileType<ShadeNimbusHostile>(), ModContent.ProjectileType<ShaderainHostile>());
                    npc.Infernum().ExtraAI[0] = (int)HiveMindAttackState.Reset;
                }
            }
        }

        public static void DoBehavior_SlowDown(NPC npc, ref float slowdownCountdown)
        {
            // Fade in and decelerate.
            npc.alpha = Utils.Clamp(npc.alpha - 17, 0, 255);
            if (slowdownCountdown > 0f)
            {
                npc.velocity *= 0.9f;
                slowdownCountdown--;
            }
            else
            {
                // Go pick a new attack.
                npc.Infernum().ExtraAI[0] = 0f;
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_BlobBurst(NPC npc, Player target, bool enraged, float lifeRatio, ref float fadeoutCountdown, ref float slowdownCountdown, ref float attackTimer)
        {
            int blobShootRate = 50;
            int blobShotCount = 4;
            int totalBlobsPerBurst = enraged ? 11 : 6;
            ref float hasFadedInFlag = ref npc.ai[1];
            ref float hoverOffsetAngle = ref npc.Infernum().ExtraAI[1];

            // Delare the previous attack for later.
            npc.Infernum().ExtraAI[9] = (int)npc.Infernum().ExtraAI[5];

            if (npc.alpha >= 0 && hasFadedInFlag == 0f)
            {
                npc.alpha -= 4;
                npc.velocity = Vector2.Zero;
                if (npc.alpha <= 0f)
                {
                    DoRoar(npc, true);
                    fadeoutCountdown = HiveMindFadeoutTime;
                    npc.alpha = 0;
                    hasFadedInFlag = 1f;
                    npc.netUpdate = true;
                }
                return;
            }

            attackTimer++;
            hoverOffsetAngle += MathHelper.ToRadians(5f);
            if ((int)attackTimer == 120f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<HiveMindWave>(), 0, 0f);

                SoundEngine.PlaySound(HiveMindBoss.RoarSound, npc.Center);
                SoundEngine.PlaySound(SoundID.DD2_BetsyFireballImpact with { Volume = 0.5f, Pitch = -0.4f }, npc.Center);
            }

            // Release a bunch of blobs.
            if (attackTimer > 120f && attackTimer % blobShootRate > blobShootRate / 2)
            {
                npc.velocity *= 0.95f;
                if (attackTimer % blobShootRate == blobShootRate - 5f)
                {
                    for (int i = 0; i < totalBlobsPerBurst; i++)
                    {
                        float offsetAngle = i == 0 ? 0f : Main.rand.NextFloat(-0.53f, 0.53f);
                        float shootSpeed = i == 0f ? 15.6f : Main.rand.NextFloat(9f, 12f);

                        if (lifeRatio < 0.25f)
                            shootSpeed *= 1.325f;
                        if (enraged)
                            shootSpeed *= 1.3f;
                        if (BossRushEvent.BossRushActive)
                            shootSpeed *= 2.3f;

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            Utilities.NewProjectileBetter(npc.Center, npc.SafeDirectionTo(target.Center).RotatedBy(offsetAngle) * shootSpeed, ModContent.ProjectileType<BlobProjectile>(), HiveBlobDamage, 0f);
                    }
                    SoundEngine.PlaySound(HiveMindBoss.RoarSound, npc.Center);
                }
            }
            else
                npc.SimpleFlyMovement((npc.velocity * 7f + npc.SafeDirectionTo(target.Center + hoverOffsetAngle.ToRotationVector2() * 300f) * 15f) / 8f, 0.1f);

            if (attackTimer > 120f + blobShootRate * blobShotCount)
            {
                // Go back to picking a new AI
                slowdownCountdown = MaxSlowdownTime;
                npc.Infernum().ExtraAI[0] = (int)HiveMindAttackState.SlowDown;
            }
        }

        public static void DoRoar(NPC npc, bool highPitched)
        {
            if (highPitched)
            {
                SoundEngine.PlaySound(HiveMindBoss.FastRoarSound, npc.Center);
                return;
            }

            for (int i = 0; i < 72; i++)
            {
                float angle = MathHelper.TwoPi / 72f * i;
                Dust fire = Dust.NewDustDirect(npc.Center, 1, 1, DustID.ChlorophyteWeapon, (float)Math.Cos(angle) * 15f, (float)Math.Sin(angle) * 15f);
                fire.noGravity = true;
            }
            SoundEngine.PlaySound(HiveMindBoss.RoarSound, npc.Center);
        }

        public static void CreateTeleportTelegraph(Vector2 teleportPosition, float cloudOpacity = 1f)
        {
            Color fireColor = Main.rand.NextBool() ? Color.Purple : Color.Lime;
            CloudParticle noxiousCloud = new(teleportPosition, Main.rand.NextVector2Circular(6f, 6f), fireColor * cloudOpacity, Color.DarkGray, 120, Main.rand.NextFloat(2f, 2.4f));
            GeneralParticleHandler.SpawnParticle(noxiousCloud);

            Dust fire = Dust.NewDustPerfect(teleportPosition + Main.rand.NextVector2Square(-50f, 50f), 75);
            fire.velocity = -Vector2.UnitY.RotateRandom(0.5f) * Main.rand.NextFloat(1f, 5f);
            fire.scale *= 1.66f;
            fire.noGravity = true;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            NPCID.Sets.TrailingMode[npc.type] = 1;
            NPCID.Sets.TrailCacheLength[npc.type] = 8;
            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/NPCs/HiveMind/HiveMindP2").Value;
            int frame = (int)(Main.GlobalTimeWrappedHourly * 10f) % 16;
            Rectangle frameRectangle = texture.Frame(2, 8, frame / 8, frame % 8);

            for (int i = 1; i < npc.oldPos.Length; i++)
            {
                if (npc.Infernum().ExtraAI[10] == 0f || !CalamityConfig.Instance.Afterimages)
                    break;

                float scale = npc.scale * MathHelper.Lerp(0.9f, 0.45f, i / (float)npc.oldPos.Length);
                float trailLength = MathHelper.Lerp(70f, 195f, Utils.GetLerpValue(3f, 7f, npc.velocity.Length(), true));
                if (npc.velocity.Length() < 1.8f)
                    trailLength = 8f;

                Color drawColor = Color.MediumPurple * (1f - i / (float)npc.oldPos.Length);
                drawColor.A = 0;
                drawColor *= npc.Opacity;

                Vector2 drawPosition = npc.Center + npc.velocity.SafeNormalize(Vector2.Zero) * -MathHelper.Lerp(8f, trailLength, i / (float)npc.oldPos.Length);
                Main.spriteBatch.Draw(texture, drawPosition - Main.screenPosition + new Vector2(0, npc.gfxOffY),
                    frameRectangle, drawColor, npc.rotation, frameRectangle.Size() / 2f, scale, SpriteEffects.None, 0f);
            }

            Vector2 baseDrawPosition = npc.Center - Main.screenPosition + Vector2.UnitY * npc.gfxOffY;

            // If performing the blob snipe attack
            if (npc.Infernum().ExtraAI[0] == 8f)
            {
                Main.spriteBatch.Draw(texture, npc.Center - Main.screenPosition + new Vector2(0, npc.gfxOffY),
                    frameRectangle, Color.White, npc.rotation, frameRectangle.Size() / 2f, npc.scale, SpriteEffects.None, 0f);
            }

            // If in the middle of a special attack (such as the Eater of Soul wall), or in the middle of its invincibility period after
            // going below 20% life.
            if (npc.Infernum().ExtraAI[0] >= 4f || npc.Infernum().ExtraAI[11] > 0f)
            {
                Main.spriteBatch.Draw(texture, baseDrawPosition, frameRectangle, new Color(91f / 255f, 71f / 255f, 127f / 255f, 0.3f * npc.Opacity) * npc.Opacity, npc.rotation, frameRectangle.Size() / 2f, Utilities.AngularSmoothstep(npc.Infernum().ExtraAI[7], 1f, 1.5f), SpriteEffects.None, 0f);
                npc.Infernum().ExtraAI[6] = HiveMindFadeoutTime;
            }

            // If fadeout timer is greater than 0
            else if (npc.Infernum().ExtraAI[6] > 0f)
            {
                float scale = npc.Infernum().ExtraAI[6] / HiveMindFadeoutTime / Utilities.AngularSmoothstep(npc.Infernum().ExtraAI[7], 1f, 1.5f);
                Main.spriteBatch.Draw(texture, baseDrawPosition, frameRectangle, new Color(91f / 255f, 71f / 255f, 127f / 255f, 0.3f * npc.Opacity) * npc.Opacity, npc.rotation, frameRectangle.Size() / 2f, MathHelper.Clamp(scale, 1f, 1000f), SpriteEffects.None, 0f);
                npc.Infernum().ExtraAI[6] -= 1f;
            }

            Main.spriteBatch.Draw(texture, baseDrawPosition, frameRectangle, npc.GetAlpha(lightColor), npc.rotation, frameRectangle.Size() / 2f, npc.scale, SpriteEffects.None, 0f);

            return false;
        }

        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "This is the time you would want to learn the opponents moves, use their tells to get the upper hand!";
            yield return n => "Try to push his Rain Charge away by running towards the Hive Mind, this can help keep your arena clean!";
            yield return n => "The Hive Mind begins its next attack early if you attack it; wait until it's on cooldown before you shoot!";
            yield return n =>
            {
                if (TipsManager.ShouldUseJokeText)
                    return "That didn't work, but dont worry! Hive got a plan!";
                return string.Empty;
            };
            yield return n =>
            {
                if (TipsManager.ShouldUseJokeText)
                    return "I would make a snarky comment right now, but I probably should Mind my own business...";
                return string.Empty;
            };
        }
    }
}
