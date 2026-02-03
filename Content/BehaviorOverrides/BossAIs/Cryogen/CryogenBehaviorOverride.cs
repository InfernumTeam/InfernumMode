using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Events;
using CalamityMod.Particles;
using CalamityMod.World;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Content.Projectiles;
using InfernumMode.Content.Projectiles.Pets;
using InfernumMode.Core.GlobalInstances;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using CryogenBoss = CalamityMod.NPCs.Cryogen.Cryogen;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Cryogen
{
    public class CryogenBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<CryogenBoss>();

        public const int IceRainDamage = 130;

        public const int IceBombDamage = 135;

        public const int IcicleSpikeDamage = 135;

        public const int AuroraSpiritDamage = 140;

        public const int IcePillarDamage = 150;

        public const float Phase2LifeRatio = 0.9f;

        public const float Phase3LifeRatio = 0.7f;

        public const float Phase4LifeRatio = 0.55f;

        public const float Phase5LifeRatio = 0.4f;

        public const float Phase6LifeRatio = 0.25f;

        public override float[] PhaseLifeRatioThresholds =>
        [
            Phase2LifeRatio,
            Phase3LifeRatio,
            Phase4LifeRatio,
            Phase5LifeRatio,
            Phase6LifeRatio,
        ];

        #region Enumerations
        public enum CryogenAttackState
        {
            IcicleCircleBurst,
            PredictiveIcicles,
            TeleportAndReleaseIceBombs,
            ShatteringIcePillars,
            IcicleTeleportDashes,
            HorizontalDash,
            AuroraBulletHell,
            EternalWinter
        }
        #endregion

        #region Attack Cycles

        // Why does this boss have so many subphases anyway?
        public static CryogenAttackState[] Subphase1AttackCycle =>
        [
            CryogenAttackState.IcicleCircleBurst,
            CryogenAttackState.PredictiveIcicles,
            CryogenAttackState.IcicleCircleBurst,
            CryogenAttackState.TeleportAndReleaseIceBombs,
        ];

        public static CryogenAttackState[] Subphase2AttackCycle =>
        [
            CryogenAttackState.IcicleCircleBurst,
            CryogenAttackState.ShatteringIcePillars,
            CryogenAttackState.TeleportAndReleaseIceBombs,
            CryogenAttackState.PredictiveIcicles,
            CryogenAttackState.IcicleCircleBurst,
            CryogenAttackState.TeleportAndReleaseIceBombs,
            CryogenAttackState.IcicleCircleBurst,
            CryogenAttackState.PredictiveIcicles,
            CryogenAttackState.ShatteringIcePillars,
        ];

        public static CryogenAttackState[] Subphase3AttackCycle =>
        [
            CryogenAttackState.ShatteringIcePillars,
            CryogenAttackState.IcicleCircleBurst,
            CryogenAttackState.ShatteringIcePillars,
            CryogenAttackState.IcicleTeleportDashes,
            CryogenAttackState.PredictiveIcicles,
            CryogenAttackState.TeleportAndReleaseIceBombs,
            CryogenAttackState.IcicleCircleBurst,
            CryogenAttackState.PredictiveIcicles,
            CryogenAttackState.TeleportAndReleaseIceBombs,
            CryogenAttackState.IcicleTeleportDashes,
        ];

        public static CryogenAttackState[] Subphase4AttackCycle =>
        [
            CryogenAttackState.HorizontalDash,
            CryogenAttackState.ShatteringIcePillars,
            CryogenAttackState.TeleportAndReleaseIceBombs,
            CryogenAttackState.IcicleCircleBurst,
            CryogenAttackState.ShatteringIcePillars,
            CryogenAttackState.IcicleTeleportDashes,
            CryogenAttackState.TeleportAndReleaseIceBombs,
            CryogenAttackState.HorizontalDash,
            CryogenAttackState.IcicleCircleBurst,
            CryogenAttackState.IcicleTeleportDashes,
        ];

        public static CryogenAttackState[] Subphase5AttackCycle =>
        [
            CryogenAttackState.HorizontalDash,
            CryogenAttackState.IcicleTeleportDashes,
            CryogenAttackState.HorizontalDash,
            CryogenAttackState.AuroraBulletHell,
            CryogenAttackState.IcicleCircleBurst,
            CryogenAttackState.IcicleTeleportDashes,
            CryogenAttackState.AuroraBulletHell
        ];

        public static CryogenAttackState[] Subphase6AttackCycle =>
        [
            CryogenAttackState.IcicleTeleportDashes,
            CryogenAttackState.AuroraBulletHell,
            CryogenAttackState.EternalWinter,
            CryogenAttackState.IcicleCircleBurst,
            CryogenAttackState.IcicleTeleportDashes,
            CryogenAttackState.AuroraBulletHell,
            CryogenAttackState.IcicleCircleBurst,
            CryogenAttackState.EternalWinter,
        ];

        #endregion Attack Cycles

        #region Loading
        public override void Load()
        {
            GlobalNPCOverrides.BossHeadSlotEvent += UseCustomMapIcon;
        }

        private void UseCustomMapIcon(NPC npc, ref int index)
        {
            // Have Cryogen use a custom map icon.
            if (npc.type == ModContent.NPCType<CryogenBoss>())
                index = ModContent.GetModBossHeadSlot("InfernumMode/Content/BehaviorOverrides/BossAIs/Cryogen/CryogenMapIcon");
        }
        #endregion Loading

        #region AI
        public override void SetDefaults(NPC npc)
        {
            // Set defaults that, if were to be changed by Calamity, would cause significant issues to the fight.
            npc.width = 86;
            npc.height = 88;
            npc.scale = 1f;
            npc.Opacity = 1f;
            npc.defense = 15;
            npc.DR_NERD(0.3f);
        }

        public override bool PreAI(NPC npc)
        {
            // Emit white light.
            Lighting.AddLight(npc.Center, Color.White.ToVector3());

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Fly away if the target is gone.
            if (!target.active || target.dead)
            {
                npc.velocity.Y -= 0.4f;
                npc.rotation = npc.velocity.X * 0.02f;
                if (!npc.WithinRange(target.Center, 4200f))
                    npc.active = false;

                return false;
            }

            // Set the whoAmI index.
            GlobalNPCOverrides.Cryogen = npc.whoAmI;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float subphaseState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float attackState = ref npc.ai[2];
            ref float enrageTimer = ref npc.ai[3];
            ref float hitEffectCooldown = ref npc.Infernum().ExtraAI[6];

            if (!BossRushEvent.BossRushActive)
                enrageTimer = Utils.Clamp(enrageTimer - target.ZoneSnow.ToDirectionInt(), 0, 660);

            // Decrease the hit effect cooldown
            if (hitEffectCooldown > 0)
                hitEffectCooldown--;

            // Make a blizzard happen.
            Utilities.StartRain();

            // Slowly going insane.
            if (target.HasBuff(BuffID.Slow))
                target.ClearBuff(BuffID.Slow);

            // Spawn snowflakes.
            target.CreateCinderParticles(lifeRatio, new SnowflakeCinder());

            // Become invincible if the target has been outside of the snow biome for too long.
            npc.dontTakeDamage = enrageTimer >= 600f;
            npc.Calamity().CurrentlyEnraged = !target.ZoneSnow && !BossRushEvent.BossRushActive;

            // Handle subphase transitions.
            HandleSubphaseTransitions(npc, lifeRatio, ref subphaseState, ref attackState, ref attackTimer);

            // Reset damage every frame.
            npc.damage = npc.defDamage;

            // Determine the attack power and cycle pattern to use based on the current subphase.
            float attackPower = 1f;
            CryogenAttackState[] attackCycle = Subphase1AttackCycle;
            if (subphaseState == 1f)
            {
                attackPower = Lerp(1.35f, 2f, 1f - npc.life / (float)npc.lifeMax);
                attackCycle = Subphase2AttackCycle;
            }
            else if (subphaseState == 2f)
            {
                attackPower = Lerp(1.35f, 2f, 1f - npc.life / (float)npc.lifeMax);
                attackCycle = Subphase3AttackCycle;
            }
            else if (subphaseState == 3f)
            {
                attackPower = Lerp(1.425f, 2f, 1f - npc.life / (float)npc.lifeMax);
                attackCycle = Subphase4AttackCycle;
            }
            else if (subphaseState == 4f)
            {
                attackPower = Lerp(1.425f, 2f, 1f - npc.life / (float)npc.lifeMax);
                attackCycle = Subphase5AttackCycle;
            }
            else if (subphaseState == 5f)
            {
                attackPower = Lerp(1.5f, 2f, 1f - npc.life / (float)npc.lifeMax);
                attackCycle = Subphase6AttackCycle;
            }

            // WHY DOES THIS SILLY ICE CUBE HAVE 30% DR IN BASE???
            npc.Calamity().DR = 0.1075f;

            switch (attackCycle[(int)attackState % attackCycle.Length])
            {
                case CryogenAttackState.IcicleCircleBurst:
                    DoAttack_IcicleCircleBurst(npc, target, ref attackTimer, ref attackState, attackPower);
                    break;
                case CryogenAttackState.PredictiveIcicles:
                    DoAttack_PredictiveIcicles(npc, target, ref attackTimer, ref attackState, attackPower);
                    break;
                case CryogenAttackState.TeleportAndReleaseIceBombs:
                    DoAttack_TeleportAndReleaseIceBombs(npc, target, ref attackTimer, ref attackState, attackPower);
                    break;
                case CryogenAttackState.ShatteringIcePillars:
                    DoAttack_ShatteringIcePillars(npc, target, ref attackTimer, ref attackState, attackPower);
                    break;
                case CryogenAttackState.IcicleTeleportDashes:
                    DoAttack_IcicleTeleportDashes(npc, target, ref attackTimer, ref attackState, attackPower);
                    break;
                case CryogenAttackState.HorizontalDash:
                    DoAttack_HorizontalDash(npc, target, ref attackTimer, ref attackState, attackPower);
                    break;
                case CryogenAttackState.AuroraBulletHell:
                    DoAttack_AuroraBulletHell(npc, target, ref attackTimer, ref attackState, attackPower);
                    break;
                case CryogenAttackState.EternalWinter:
                    DoAttack_EternalWinter(npc, target, ref attackTimer, ref attackState, attackPower);
                    break;
            }
            attackTimer++;

            if (npc.damage == 0)
                npc.Opacity = Lerp(npc.Opacity, 0.55f, 0.1f);
            else
                npc.Opacity = Lerp(npc.Opacity, 1f, 0.1f);

            CalamityWorld.StopRain();
            return false;
        }

        public static void HandleSubphaseTransitions(NPC npc, float lifeRatio, ref float subphaseState, ref float attackState, ref float attackTimer)
        {
            int trueSubphaseState = 0;
            if (lifeRatio < Phase2LifeRatio)
                trueSubphaseState++;
            if (lifeRatio < Phase3LifeRatio)
                trueSubphaseState++;
            if (lifeRatio < Phase4LifeRatio)
                trueSubphaseState++;
            if (lifeRatio < Phase5LifeRatio)
                trueSubphaseState++;
            if (lifeRatio < Phase6LifeRatio)
                trueSubphaseState++;

            while (subphaseState < trueSubphaseState)
            {
                EmitIceParticles(npc.Center, 3.5f, 40);

                // Emit gores at the start as necessary.
                if (Main.netMode != NetmodeID.Server && subphaseState == 0f)
                {
                    for (int i = 1; i <= 5; i++)
                        Gore.NewGore(npc.GetSource_FromAI(), npc.Center, npc.velocity, InfernumMode.Instance.Find<ModGore>("CryogenChainGore" + i).Type, npc.scale);

                    SoundEngine.PlaySound(CryogenBoss.TransitionSound, npc.Center);
                }

                if (Main.netMode != NetmodeID.Server && subphaseState == 1f)
                {
                    for (int i = 1; i <= 7; i++)
                        Gore.NewGore(npc.GetSource_FromAI(), npc.Center, npc.velocity, InfernumMode.Instance.Find<ModGore>("CryogenGore" + i).Type, npc.scale);
                }

                // Reset everything and sync.
                npc.frame.Y = 0;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    attackState = 0f;
                    attackTimer = 0f;
                    subphaseState++;

                    for (int i = 0; i < 5; i++)
                        npc.Infernum().ExtraAI[i] = 0f;

                    npc.netUpdate = true;
                }
                else
                    // Stop the multiplayer client getting stuck in an inf while loop and crashing.
                    subphaseState++;
            }
        }

        public static void DoAttack_IcicleCircleBurst(NPC npc, Player target, ref float attackTimer, ref float attackState, float attackPower)
        {
            float zeroBasedAttackPower = attackPower - 1f;
            int burstCount = 3;
            int burstCreationRate = 120 - (int)(zeroBasedAttackPower * 25f);
            int icicleCount = 11 + (int)(zeroBasedAttackPower * 4f);
            Vector2 destination = target.Center + new Vector2(target.velocity.X * 80f, -400f);

            // Move to the side of the target instead of right on top of them if below the target to prevent
            // EoL-esque bullshit hits.
            if (npc.Center.Y > target.Center.Y - 60f)
                destination.X = target.Center.X + (target.Center.X < npc.Center.X).ToDirectionInt() * 480f;

            if (!npc.WithinRange(destination, 90f))
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(destination) * 15f, 1.4f);
            else
                npc.velocity *= 0.96f;
            npc.rotation = npc.velocity.X * 0.02f;
            npc.damage = 0;

            if (attackTimer % burstCreationRate == burstCreationRate - 1f && attackTimer < burstCreationRate * burstCount + 60f)
            {
                EmitIceParticles(npc.Center, 3.5f, 25);
                SoundEngine.PlaySound(CryogenBoss.ShieldRegenSound, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float angleOffset = Main.rand.NextFloat(TwoPi);
                    for (int i = 0; i < icicleCount; i++)
                    {
                        float icicleFireDirection = TwoPi * i / icicleCount + npc.AngleTo(target.Center) + angleOffset;
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<IcicleSpike>(), IcicleSpikeDamage, 0f, -1, icicleFireDirection, npc.whoAmI);

                        icicleFireDirection = TwoPi * (i + 0.5f) / icicleCount + npc.AngleTo(target.Center) + angleOffset;
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<IcicleSpike>(), IcicleSpikeDamage, 0f, -1, icicleFireDirection, npc.whoAmI);
                    }

                    // Create a seconnd set of rings in later phases.
                    if (npc.life < npc.lifeMax * Phase4LifeRatio)
                    {
                        for (int i = 0; i < icicleCount; i++)
                        {
                            float icicleFireDirection = TwoPi * i / icicleCount + npc.AngleTo(target.Center) + angleOffset;

                            ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(icicle =>
                            {
                                icicle.ModProjectile<IcicleSpike>().InwardRadiusOffset = 42f;
                                icicle.ModProjectile<IcicleSpike>().Time = -20f;
                            });
                            Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<IcicleSpike>(), IcicleSpikeDamage, 0f, -1, icicleFireDirection, npc.whoAmI);
                        }
                    }
                }
            }

            if (attackTimer >= burstCreationRate * burstCount + 108f)
            {
                Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<IcicleSpike>());
                attackTimer = 0f;
                attackState++;
                npc.TargetClosest();
                npc.netUpdate = true;
            }
        }

        public static void DoAttack_PredictiveIcicles(NPC npc, Player target, ref float attackTimer, ref float attackState, float attackPower)
        {
            float zeroBasedAttackPower = attackPower - 1f;
            int burstCount = 6;
            int burstCreationRate = 120 - (int)(zeroBasedAttackPower * 12f);
            int icicleCount = 6 + (int)(zeroBasedAttackPower * 4f);
            float angularOffsetRandomness = 0.58f;
            if (BossRushEvent.BossRushActive)
            {
                icicleCount *= 2;
                angularOffsetRandomness = 0.97f;
            }

            Vector2 destination = target.Center - Vector2.UnitY * 320f;

            // Move to the side of the target instead of right on top of them if below the target to prevent
            // EoL-esque bullshit hits.
            if (npc.Center.Y > target.Center.Y - 60f)
                destination.X = target.Center.X + (target.Center.X < npc.Center.X).ToDirectionInt() * 480f;

            if (!npc.WithinRange(destination, 60f))
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(destination) * 15f, 1.4f);
            else
                npc.velocity *= 0.945f;
            npc.rotation = npc.velocity.X * 0.02f;
            npc.damage = 0;

            if (attackTimer % burstCreationRate == burstCreationRate - 1f)
            {
                EmitIceParticles(npc.Center, 3.5f, 25);
                SoundEngine.PlaySound(SoundID.Item28, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < icicleCount; i++)
                    {
                        Vector2 icicleVelocity = -Vector2.UnitY.RotatedByRandom(angularOffsetRandomness) * Main.rand.NextFloat(7f, 11f);
                        Utilities.NewProjectileBetter(npc.Center, icicleVelocity, ModContent.ProjectileType<AimedIcicleSpike>(), IcicleSpikeDamage, 0f, -1, 0f, i / (float)icicleCount * 90f);
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 icicleVelocity = (TwoPi * i / 4f + PiOver4).ToRotationVector2() * 6f;
                        Utilities.NewProjectileBetter(npc.Center, icicleVelocity, ModContent.ProjectileType<AimedIcicleSpike>(), IcicleSpikeDamage, 0f);
                    }
                }
            }


            if (attackTimer >= burstCreationRate * burstCount + 60f)
            {
                attackTimer = 0f;
                attackState++;
                npc.TargetClosest();
                npc.netUpdate = true;
            }
        }

        public static void DoAttack_TeleportAndReleaseIceBombs(NPC npc, Player target, ref float attackTimer, ref float attackState, float attackPower)
        {
            float zeroBasedAttackPower = attackPower - 1f;
            int idleBombReleaseRate = 60 - (int)(zeroBasedAttackPower * 25f);
            int teleportWaitTime = 240 - (int)(zeroBasedAttackPower * 60f);
            int teleportTelegraphTime = teleportWaitTime - 90;

            ref float teleportPositionX = ref npc.Infernum().ExtraAI[0];
            ref float teleportPositionY = ref npc.Infernum().ExtraAI[1];

            // Idly release bombs.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % idleBombReleaseRate == idleBombReleaseRate - 1f)
            {
                Vector2 bombVelocity = npc.SafeDirectionTo(target.Center) * 12f;
                Utilities.NewProjectileBetter(npc.Center, bombVelocity, ModContent.ProjectileType<IceBomb2>(), IceBombDamage, 0f);
            }

            // Decide a teleport postion and emit teleport particles there.
            if (attackTimer >= teleportTelegraphTime && attackTimer < teleportWaitTime)
            {
                if (teleportPositionX == 0f || teleportPositionY == 0f)
                {
                    Vector2 teleportPosition = target.Center + target.velocity.SafeNormalize(Main.rand.NextVector2Unit()) * Main.rand.NextFloat(400f, 450f);
                    teleportPositionX = teleportPosition.X;
                    teleportPositionY = teleportPosition.Y;
                }
                else
                    EmitIceParticles(new Vector2(teleportPositionX, teleportPositionY), 3f, 6);

                npc.Opacity = Utils.GetLerpValue(teleportWaitTime - 1f, teleportWaitTime - 45f, attackTimer, true);
            }

            // Do the teleport.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == teleportWaitTime)
            {
                npc.Center = new Vector2(teleportPositionX, teleportPositionY);
                npc.velocity = -Vector2.UnitY.RotateTowards(npc.AngleTo(target.Center), Pi / 3f) * 7f;

                for (int i = 0; i < 6; i++)
                {
                    Vector2 bombVelocity = (TwoPi * i / 6f).ToRotationVector2() * 11f;
                    Utilities.NewProjectileBetter(npc.Center, bombVelocity, ModContent.ProjectileType<IceBomb2>(), IceBombDamage, 0f);
                }

                SoundEngine.PlaySound(SoundID.Item8, npc.Center);

                teleportPositionX = 0f;
                teleportPositionY = 0f;
                npc.netUpdate = true;
            }

            if (!npc.WithinRange(target.Center, 75f))
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * 5.5f, 0.075f);

            npc.rotation = npc.velocity.X * 0.03f;

            if (attackTimer >= teleportWaitTime + 144f)
            {
                attackTimer = 0f;
                attackState++;
                npc.TargetClosest();
                npc.netUpdate = true;
            }
        }

        public static void DoAttack_ShatteringIcePillars(NPC npc, Player target, ref float attackTimer, ref float attackState, float attackPower)
        {
            float zeroBasedAttackPower = attackPower - 1f;
            int burstCount = 3;
            int burstCreationRate = 160 - (int)(zeroBasedAttackPower * 25f);
            int pillarCreationRate = 135 - (int)(zeroBasedAttackPower * 30f);
            int icicleCount = 6 + (int)(zeroBasedAttackPower * 3f);
            float pillarHorizontalOffset = 750f - zeroBasedAttackPower * 130f;
            ref float icePillarCreationTimer = ref npc.Infernum().ExtraAI[0];

            Vector2 destination = target.Center + new Vector2(target.velocity.X * 80f, -425f);

            // Move to the side of the target instead of right on top of them if below the target to prevent
            // EoL-esque bullshit hits.
            if (npc.Center.Y > target.Center.Y - 60f)
                destination.X = target.Center.X + (target.Center.X < npc.Center.X).ToDirectionInt() * 480f;

            if (!npc.WithinRange(destination, 90f))
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(destination) * 15f, 1.4f);
            else
                npc.velocity *= 0.96f;
            npc.rotation = npc.velocity.X * 0.02f;
            npc.damage = 0;

            // Increment timers.
            icePillarCreationTimer++;

            if (attackTimer % burstCreationRate == burstCreationRate - 1f && attackTimer < burstCreationRate * burstCount)
            {
                EmitIceParticles(npc.Center, 3.5f, 25);
                SoundEngine.PlaySound(CryogenBoss.ShieldRegenSound, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float angleOffset = Main.rand.NextFloat(TwoPi);
                    for (int i = 0; i < icicleCount; i++)
                    {
                        int icicle = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<IcicleSpike>(), IcicleSpikeDamage, 0f);
                        if (Main.projectile.IndexInRange(icicleCount))
                        {
                            Main.projectile[icicle].ai[0] = TwoPi * i / icicleCount + npc.AngleTo(target.Center) + angleOffset;
                            Main.projectile[icicle].ai[1] = npc.whoAmI;
                            Main.projectile[icicle].localAI[1] = BossRushEvent.BossRushActive ? 1.7f : 1f;
                            Main.projectile[icicle].netUpdate = true;
                        }

                        icicle = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<IcicleSpike>(), IcicleSpikeDamage, 0f);
                        if (Main.projectile.IndexInRange(icicleCount))
                        {
                            Main.projectile[icicle].ai[0] = TwoPi * (i + 0.5f) / icicleCount + npc.AngleTo(target.Center) + angleOffset;
                            Main.projectile[icicle].ai[1] = npc.whoAmI;
                            Main.projectile[icicle].localAI[1] = BossRushEvent.BossRushActive ? 1.122f : 0.66f;
                            Main.projectile[icicle].netUpdate = true;
                        }
                    }

                    // Create a seconnd set of rings in later phases.
                    if (npc.life < npc.lifeMax * Phase3LifeRatio)
                    {
                        icicleCount -= 2;
                        for (int i = 0; i < icicleCount; i++)
                        {
                            float icicleFireDirection = TwoPi * i / icicleCount + npc.AngleTo(target.Center) + angleOffset;

                            ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(icicle =>
                            {
                                icicle.ModProjectile<IcicleSpike>().InwardRadiusOffset = 42f;
                                icicle.ModProjectile<IcicleSpike>().Time = -20f;
                            });
                            Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<IcicleSpike>(), IcicleSpikeDamage, 0f, -1, icicleFireDirection, npc.whoAmI);
                        }
                    }
                }
            }

            // Periodically create two pillars.
            if (icePillarCreationTimer >= pillarCreationRate && attackTimer < burstCreationRate * burstCount)
            {
                for (int i = -1; i <= 1; i += 2)
                {
                    Vector2 spawnPosition = target.Center + Vector2.UnitX * pillarHorizontalOffset * i;
                    Utilities.NewProjectileBetter(spawnPosition, Vector2.Zero, ModContent.ProjectileType<IcePillar>(), IcePillarDamage, 0f);
                }
                icePillarCreationTimer = 0f;
                npc.netUpdate = true;
            }

            if (attackTimer >= burstCreationRate * burstCount + 204f)
            {
                Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<IcicleSpike>());
                icePillarCreationTimer = 0f;
                attackTimer = 0f;
                attackState++;
                npc.TargetClosest();
                npc.netUpdate = true;
            }
        }

        public static void DoAttack_IcicleTeleportDashes(NPC npc, Player target, ref float attackTimer, ref float attackState, float attackPower)
        {
            float zeroBasedAttackPower = attackPower - 1f;
            int intialTeleportDelay = 90;
            int teleportDelay = 60;
            int teleportCount = 8;
            float verticalTeleportOffset = Lerp(560f, 700f, zeroBasedAttackPower);
            int spikeReleaseRate = zeroBasedAttackPower > 0.8f ? 15 : 20;
            Vector2 initialTeleportOffset = target.Center - Vector2.UnitY * 350f;

            ref float teleportCounter = ref npc.Infernum().ExtraAI[0];
            ref float teleportTimer = ref npc.Infernum().ExtraAI[1];

            npc.damage = 0;

            // Drift towards the top of the target.
            if (attackTimer < intialTeleportDelay)
            {
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(initialTeleportOffset) * 8f, 0.35f);
                npc.rotation = npc.velocity.X * 0.02f;
                npc.Opacity = Utils.GetLerpValue(intialTeleportDelay - 1f, intialTeleportDelay - 24f, attackTimer, true);
            }
            else
            {
                teleportTimer++;
                npc.Opacity = Utils.GetLerpValue(teleportDelay - 1f, teleportDelay - 24f, teleportTimer, true);

                // Periodically release redirecting icicles.
                if (attackTimer % spikeReleaseRate == spikeReleaseRate - 1f)
                {
                    SoundEngine.PlaySound(SoundID.Item28, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 icicleShootVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 4f);
                        Utilities.NewProjectileBetter(npc.Center + icicleShootVelocity * 4f, icicleShootVelocity, ModContent.ProjectileType<AimedIcicleSpike>(), IcicleSpikeDamage, 0f);
                    }
                }
            }

            Vector2 teleportOffset = Vector2.UnitY * (teleportCounter % 2f == 0f).ToDirectionInt() * verticalTeleportOffset;
            EmitIceParticles(target.Center + teleportOffset, 3f, 6);

            // Reset opacity and teleport after the delay is finished.
            if (attackTimer == intialTeleportDelay || teleportTimer >= teleportDelay)
            {
                teleportTimer = 0f;
                teleportCounter++;

                Vector2 flyOffset = teleportOffset.RotatedBy(PiOver2);

                npc.Opacity = 1f;
                npc.Center = target.Center + teleportOffset;
                npc.velocity = npc.SafeDirectionTo(target.Center + flyOffset) * npc.Distance(target.Center + flyOffset) / teleportDelay;
                npc.netUpdate = true;

                if (teleportCounter >= teleportCount)
                {
                    teleportCounter = 0f;
                    attackTimer = 0f;
                    attackState++;

                    npc.TargetClosest();
                    target = Main.player[npc.target];
                    npc.Center = target.Center - Vector2.UnitY * 550f;
                    npc.velocity *= 0.15f;

                    npc.netUpdate = true;
                }

                EmitIceParticles(npc.Center, 4f, 40);
            }
        }

        public static void DoAttack_HorizontalDash(NPC npc, Player target, ref float attackTimer, ref float attackState, float attackPower)
        {
            float zeroBasedAttackPower = attackPower - 1f;
            int chargeCount = 3;
            float chargeSpeed = Lerp(20.75f, 28f, zeroBasedAttackPower);
            if (BossRushEvent.BossRushActive)
                chargeSpeed *= 1.45f;

            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[1];

            // Line up for the charge.
            if (attackSubstate == 0f)
            {
                float verticalOffsetLeniance = 65f;
                float flySpeed = 14f;
                float flyInertia = 4f;
                float horizontalOffset = 700f;
                Vector2 destination = target.Center - Vector2.UnitX * Math.Sign(target.Center.X - npc.Center.X) * horizontalOffset;

                // Fly towards the destination beside the player.
                npc.velocity = (npc.velocity * (flyInertia - 1f) + npc.SafeDirectionTo(destination) * flySpeed) / flyInertia;
                npc.rotation = npc.velocity.X * 0.02f;

                // If within a good approximation of the player's position, prepare charging.
                if (Math.Abs(npc.Center.X - target.Center.X) > horizontalOffset - 50f && Math.Abs(npc.Center.Y - target.Center.Y) < verticalOffsetLeniance)
                {
                    attackSubstate = 1f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            // Spin before charging.
            if (attackSubstate == 1f)
            {
                npc.velocity *= 0.95f;
                npc.rotation += Math.Sign(npc.SafeDirectionTo(target.Center).X) * attackTimer / 67f;
                if (attackTimer >= 20f)
                {
                    attackSubstate = 2f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            // Prepare for the charge.
            if (attackSubstate == 2f)
            {
                int chargeDelay = 10;
                float flyInertia = 4f;
                Vector2 chargeVelocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                npc.velocity = (npc.velocity * (flyInertia - 1f) + chargeVelocity) / flyInertia;
                npc.rotation = npc.velocity.X * 0.02f;

                if (attackTimer >= chargeDelay)
                {
                    // Play a charge sound.
                    SoundEngine.PlaySound(SoundID.Item28, npc.Center);

                    attackTimer = 0f;
                    attackSubstate = 3f;
                    npc.velocity = chargeVelocity;
                    if (Main.rand.NextBool(3))
                        npc.velocity *= 1.5f;

                    npc.netUpdate = true;
                }
            }

            // Do the actual charge.
            if (attackSubstate == 3f)
            {
                // Release redirecting icicles perpendicularly.
                if (attackTimer % 15f == 14f)
                {
                    SoundEngine.PlaySound(SoundID.Item72, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Utilities.NewProjectileBetter(npc.Center, -Vector2.UnitY * 7f, ModContent.ProjectileType<AimedIcicleSpike>(), IcicleSpikeDamage, 0f);
                        Utilities.NewProjectileBetter(npc.Center, Vector2.UnitY * 7f, ModContent.ProjectileType<AimedIcicleSpike>(), IcicleSpikeDamage, 0f);
                    }
                }

                npc.rotation += npc.velocity.X * 0.0133f;
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= 75f)
                {
                    attackTimer = 0f;
                    attackSubstate = 0f;
                    chargeCounter++;

                    if (chargeCounter > chargeCount)
                    {
                        npc.TargetClosest();
                        chargeCounter = 0f;
                        attackState++;
                    }

                    npc.netUpdate = true;
                }
            }
        }

        public static void DoAttack_AuroraBulletHell(NPC npc, Player target, ref float attackTimer, ref float attackState, float attackPower)
        {
            float zeroBasedAttackPower = attackPower - 1f;
            int shootDelay = 90;
            int spiritSummonTime = (int)(540 + zeroBasedAttackPower * 210f);
            int spiritSummonRate = (int)(19f - zeroBasedAttackPower * 3f);
            Vector2 destination = target.Center + new Vector2(target.velocity.X * 80f, -355f);

            // Move to the side of the target instead of right on top of them if below the target to prevent
            // EoL-esque bullshit hits.
            if (npc.Center.Y > target.Center.Y - 60f)
                destination.X = target.Center.X + (target.Center.X < npc.Center.X).ToDirectionInt() * 480f;

            if (!npc.WithinRange(destination, 90f))
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(destination) * 12.5f, 1f);
            else
                npc.velocity *= 0.96f;
            npc.rotation = npc.velocity.X * 0.02f;

            HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.CryogenBlizzardTip");

            bool canShoot = attackTimer > shootDelay && attackTimer < spiritSummonTime;
            if (Main.netMode != NetmodeID.MultiplayerClient && canShoot && attackTimer % spiritSummonRate == spiritSummonRate - 1f)
            {
                bool dontCurve = Main.rand.NextBool(10);
                Vector2 spiritSpawnPosition = target.Center - Vector2.UnitY * Main.rand.NextFloat(450f);
                spiritSpawnPosition.X += Main.rand.NextBool(2).ToDirectionInt() * 825f;
                Vector2 spiritVelocity = Vector2.UnitX * Math.Sign(target.Center.X - spiritSpawnPosition.X) * 6.5f;
                if (dontCurve)
                {
                    spiritSpawnPosition = target.Center + Vector2.UnitX * Main.rand.NextBool().ToDirectionInt() * 800f;
                    spiritVelocity = Vector2.UnitX * Math.Sign(target.Center.X - spiritSpawnPosition.X) * 13.5f;
                }

                Utilities.NewProjectileBetter(spiritSpawnPosition, spiritVelocity, ModContent.ProjectileType<AuroraSpirit>(), AuroraSpiritDamage, 0f, -1, 0f, dontCurve.ToInt());
            }

            if (attackTimer >= spiritSummonTime + 90f)
            {
                attackTimer = 0f;
                attackState++;
                npc.TargetClosest();
                npc.netUpdate = true;
            }
        }

        public static void DoAttack_EternalWinter(NPC npc, Player target, ref float attackTimer, ref float attackState, float attackPower)
        {
            int chargeCount = 8;
            float chargeSpeed = Lerp(20f, 25f, attackPower - 1f);
            ref float chargeCounter = ref npc.Infernum().ExtraAI[0];

            // Spin around and charge at the target periodically.
            if (attackTimer >= 60f)
            {
                npc.velocity = npc.SafeDirectionTo(target.Center, -Vector2.UnitY) * chargeSpeed;
                chargeCounter++;
                attackTimer = 0f;

                if (chargeCounter > chargeCount)
                {
                    chargeCounter = 0f;
                    attackState++;
                    npc.TargetClosest();
                    npc.velocity *= 0.2f;
                    npc.netUpdate = true;
                }

                npc.netUpdate = true;
            }

            // Periodically release spirits and icicles.
            if (attackTimer % 60f == 50f && !npc.WithinRange(target.Center, 180f))
            {
                EmitIceParticles(npc.Center, 7f, 60);
                SoundEngine.PlaySound(SoundID.Item28, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int projectileType = Main.rand.NextBool(3) ? ModContent.ProjectileType<AimedIcicleSpike>() : ModContent.ProjectileType<AuroraSpirit2>();
                    int projectileCount = projectileType == ModContent.ProjectileType<AimedIcicleSpike>() ? 3 : 2;

                    for (int i = 0; i < projectileCount; i++)
                    {
                        Vector2 projectileVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(6.5f, 10.5f);
                        Vector2 spawnPosition = npc.Center + projectileVelocity * 4f;
                        int projectile = Utilities.NewProjectileBetter(spawnPosition, projectileVelocity, projectileType, IcicleSpikeDamage, 0f);
                        if (projectileType == ModContent.ProjectileType<AimedIcicleSpike>() && Main.projectile.IndexInRange(projectile))
                        {
                            Main.projectile[projectile].ai[1] = 15f;
                            Main.projectile[projectile].netUpdate = true;
                        }
                    }
                }
            }

            if (attackTimer > 42f)
            {
                npc.velocity *= 0.95f;
                npc.rotation = npc.velocity.X * 0.1f;
            }
            else
                npc.rotation += npc.direction * 0.25f;
        }

        public static void EmitIceParticles(Vector2 position, float speed, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Dust ice = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(62f, 62f), DustID.AncientLight);
                ice.color = Color.Lerp(Color.White, Color.Cyan, Main.rand.NextFloat(0.15f, 0.7f));
                ice.velocity = Main.rand.NextVector2Circular(speed, speed) - Vector2.UnitY * 1.6f;
                ice.scale = Main.rand.NextFloat(1.2f, 1.6f);
                ice.fadeIn = 1.5f;
                ice.noGravity = true;
            }
        }

        public static void OnHitIceParticles(NPC npc, Projectile projectile, bool wasACrit)
        {
            if (npc.Infernum().ExtraAI[6] > 0f)
                return;

            int particleCount = wasACrit ? 30 : 20;
            Vector2 direction = projectile.oldVelocity.SafeNormalize(Main.rand.NextVector2Unit());

            for (int i = 0; i < particleCount; i++)
            {
                Vector2 velocity = -direction * Main.rand.NextFloat(2f, 6f) + npc.velocity;

                // Add a bit of randomness, but weight towards going in a cone from the hit zone.
                Vector2 finalVelocity = Main.rand.NextBool(3) ? velocity.RotatedBy(Main.rand.NextFloat(TwoPi)) : velocity.RotatedBy(Main.rand.NextFloat(-0.6f, 0.6f));
                Particle iceParticle = new SnowyIceParticle(projectile.position, finalVelocity, Color.White, Main.rand.NextFloat(0.75f, 0.95f), 30);
                GeneralParticleHandler.SpawnParticle(iceParticle);
            }

            npc.Infernum().ExtraAI[6] = 15;
        }
        #endregion AI

        #region Drawing

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Main.projFrames[npc.type] = 12;

            Texture2D subphase1Texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Cryogen/Cryogen1").Value;
            Texture2D subphase1TextureGlow = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Cryogen/Cryogen1Glow").Value;

            Texture2D subphase2Texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Cryogen/Cryogen2").Value;
            Texture2D subphase2TextureGlow = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Cryogen/Cryogen2Glow").Value;

            Texture2D subphase3Texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Cryogen/Cryogen3").Value;
            Texture2D subphase3TextureGlow = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Cryogen/Cryogen3Glow").Value;

            Texture2D subphase4Texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Cryogen/Cryogen4").Value;
            Texture2D subphase4TextureGlow = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Cryogen/Cryogen4Glow").Value;

            Texture2D subphase5Texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Cryogen/Cryogen5").Value;
            Texture2D subphase5TextureGlow = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Cryogen/Cryogen5Glow").Value;

            Texture2D subphase6Texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Cryogen/Cryogen6").Value;
            Texture2D subphase6TextureGlow = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Cryogen/Cryogen6Glow").Value;

            Texture2D drawTexture = subphase1Texture;
            Texture2D glowTexture = subphase1TextureGlow;
            switch ((int)npc.ai[0])
            {
                case 0:
                    drawTexture = subphase1Texture;
                    glowTexture = subphase1TextureGlow;
                    break;
                case 1:
                    drawTexture = subphase2Texture;
                    glowTexture = subphase2TextureGlow;
                    break;
                case 2:
                    drawTexture = subphase3Texture;
                    glowTexture = subphase3TextureGlow;
                    break;
                case 3:
                    drawTexture = subphase4Texture;
                    glowTexture = subphase4TextureGlow;
                    break;
                case 4:
                    drawTexture = subphase5Texture;
                    glowTexture = subphase5TextureGlow;
                    break;
                case 5:
                    drawTexture = subphase6Texture;
                    glowTexture = subphase6TextureGlow;
                    break;
            }

            npc.frame.Width = drawTexture.Width;
            npc.frame.Height = drawTexture.Height / Main.projFrames[npc.type];
            npc.frameCounter++;
            if (npc.frameCounter >= 4)
            {
                npc.frame.Y += npc.frame.Height;
                if (npc.frame.Y >= Main.projFrames[npc.type] * npc.frame.Height)
                    npc.frame.Y = 0;

                npc.frameCounter = 0;
            }

            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Main.spriteBatch.Draw(drawTexture, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glowTexture, drawPosition, npc.frame, npc.GetAlpha(Color.White), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            return false;
        }

        #endregion

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Mods.InfernumMode.PetDialog.CryogenTip1";
            yield return n => "Mods.InfernumMode.PetDialog.CryogenTip2";

            yield return n =>
            {
                if (TipsManager.ShouldUseJokeText)
                    return "Mods.InfernumMode.PetDialog.CryogenJokeTip1";
                return string.Empty;
            };
            yield return n =>
            {
                if (TipsManager.ShouldUseJokeText)
                    return "Mods.InfernumMode.PetDialog.CryogenJokeTip2";
                return string.Empty;
            };
        }
        #endregion Tips
    }
}
