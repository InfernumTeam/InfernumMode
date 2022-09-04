using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Cooldowns;
using CalamityMod.Dusts;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.OldDuke;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

using OldDukeBoss = CalamityMod.NPCs.OldDuke.OldDuke;

namespace InfernumMode.BehaviorOverrides.BossAIs.OldDuke
{
    public class OldDukeBehaviorOverride : NPCBehaviorOverride
    {
        public enum OldDukeAttackState
        {
            SpawnAnimation,
            AttackSelectionWait,
            Charge,
            AcidBelch,
            AcidBubbleFountain,
            SharkronSpinSummon,
            ToothBallVomit,
            GoreAndAcidSpit,
            FastRegularCharge,
            TeleportPause,
        }

        public enum OldDukeFrameType
        {
            FlapWings,
            Charge,
            Roar,
        }

        public override int NPCOverrideType => ModContent.NPCType<OldDukeBoss>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame | NPCOverrideContext.NPCPreDraw;

        public const float Phase2LifeRatio = 0.75f;
        public const float Phase3LifeRatio = 0.375f;
        public const float Phase4LifeRatio = 0.2f;
        public const float PhaseTransitionTime = 150f;

        public const float TeleportPauseTime = 30f;

        #region Phase Patterns
        public static readonly List<OldDukeAttackState> Phase1AttackPattern = new()
        {
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.AcidBelch,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.ToothBallVomit,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.SharkronSpinSummon,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.ToothBallVomit,
        };

        public static readonly List<OldDukeAttackState> Phase2AttackPattern = new()
        {
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.FastRegularCharge,
            OldDukeAttackState.FastRegularCharge,
            OldDukeAttackState.ToothBallVomit,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.SharkronSpinSummon,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.FastRegularCharge,
            OldDukeAttackState.GoreAndAcidSpit,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.FastRegularCharge,
            OldDukeAttackState.AcidBelch,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.FastRegularCharge,
            OldDukeAttackState.FastRegularCharge,
            OldDukeAttackState.ToothBallVomit,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.FastRegularCharge,
            OldDukeAttackState.GoreAndAcidSpit,
        };

        public static readonly List<OldDukeAttackState> Phase3AttackPattern = new()
        {
            OldDukeAttackState.TeleportPause,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.FastRegularCharge,
            OldDukeAttackState.ToothBallVomit,
            OldDukeAttackState.TeleportPause,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.FastRegularCharge,
            OldDukeAttackState.GoreAndAcidSpit,
            OldDukeAttackState.TeleportPause,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.FastRegularCharge,
            OldDukeAttackState.TeleportPause,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.FastRegularCharge,
            OldDukeAttackState.FastRegularCharge,
            OldDukeAttackState.ToothBallVomit,
            OldDukeAttackState.TeleportPause,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.FastRegularCharge,
            OldDukeAttackState.TeleportPause,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.FastRegularCharge,
            OldDukeAttackState.FastRegularCharge,
            OldDukeAttackState.SharkronSpinSummon,
        };

        public static readonly List<OldDukeAttackState> Phase4AttackPattern = new()
        {
            OldDukeAttackState.TeleportPause,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
        };
        #endregion Phase Patterns

        #region AI
        public override bool PreAI(NPC npc)
        {
            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool outOfOcean = false;
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float frameType = ref npc.localAI[0];
            ref float phaseTransitionState = ref npc.Infernum().ExtraAI[6];
            ref float phaseTransitionTimer = ref npc.Infernum().ExtraAI[7];
            ref float phaseTransitionSharkSpawnOffset = ref npc.Infernum().ExtraAI[8];
            ref float despawnTimer = ref npc.Infernum().ExtraAI[9];

            if (target.HasBuff(ModContent.BuffType<Irradiated>()))
                target.ClearBuff(ModContent.BuffType<Irradiated>());

            // Enter new phases.
            if (phaseTransitionState == 0f && lifeRatio < Phase2LifeRatio)
            {
                phaseTransitionTimer = 1f;
                phaseTransitionState = 1f;
                attackTimer = 0f;

                npc.netUpdate = true;
                return false;
            }

            if (phaseTransitionState == 1f && lifeRatio < Phase3LifeRatio)
            {
                phaseTransitionTimer = 1f;
                phaseTransitionState = 2f;
                CleanupLeftoverEntities();
                SelectNextAttack(npc);
                attackTimer = 0f;

                if (Main.netMode != NetmodeID.MultiplayerClient && !BossRushEvent.BossRushActive)
                {
                    CalamityUtils.StartRain(true);
                    Main.cloudBGActive = 1f;
                    Main.numCloudsTemp = 160;
                    Main.numClouds = Main.numCloudsTemp;
                    Main.windSpeedCurrent = 1.04f;
                    Main.windSpeedTarget = Main.windSpeedCurrent;
                    Main.maxRaining = 0.87f;
                }

                npc.netUpdate = true;
                return false;
            }
            if (phaseTransitionState == 2f && lifeRatio < Phase4LifeRatio)
            {
                npc.ai[0] = 0f;
                npc.ai[2] = 0f;
                npc.ai[3] = 0f;
                phaseTransitionTimer = 1f;
                phaseTransitionState = 3f;
                CleanupLeftoverEntities();
                SelectNextAttack(npc);
                attackTimer = 0f;

                npc.netUpdate = true;
                return false;
            }

            if (!target.active || target.dead || !target.WithinRange(npc.Center, 6800f))
            {
                frameType = (int)OldDukeFrameType.FlapWings;
                npc.frameCounter++;

                npc.Opacity = 1f - despawnTimer / 30f;
                npc.rotation *= 0.7f;
                despawnTimer++;

                if (npc.Opacity <= 0f)
                {
                    CleanupLeftoverEntities();
                    npc.life = 0;
                    npc.active = false;
                    npc.netUpdate = true;
                }
                npc.velocity = Vector2.Lerp(npc.velocity, -Vector2.UnitY * 4f, 0.25f);

                npc.netUpdate = true;
                return false;
            }

            if (despawnTimer > 0f)
            {
                npc.Opacity = 1f;
                despawnTimer = 0f;
            }

            // Reset variables. They may be changed by behaviors below.
            npc.Calamity().DR = 0.15f;
            npc.dontTakeDamage = outOfOcean;
            npc.Calamity().CurrentlyEnraged = outOfOcean;
            npc.damage = npc.defDamage;

            // Reset the hitbox.
            npc.width = 166;
            npc.height = 166;

            // Handle phase transitions.
            if (phaseTransitionTimer > 0f)
            {
                npc.dontTakeDamage = true;

                npc.ai[0] = 0f;
                npc.ai[2] = 0f;
                npc.ai[3] = 0f;
                npc.damage = 0;
                DoBehavior_PhaseTransitionEffects(npc, phaseTransitionTimer, ref frameType, ref phaseTransitionSharkSpawnOffset);
                phaseTransitionTimer++;

                if (phaseTransitionTimer >= PhaseTransitionTime)
                {
                    phaseTransitionSharkSpawnOffset = 0f;
                    phaseTransitionTimer = 0f;
                    npc.Center = target.Center - target.velocity.SafeNormalize(Main.rand.NextVector2Unit()) * 350f;
                    npc.velocity = Vector2.Zero;
                    npc.netUpdate = true;
                }

                return false;
            }

            bool inPhase2 = outOfOcean || phaseTransitionState >= 1f;
            bool inPhase3 = outOfOcean || phaseTransitionState >= 2f;
            bool inPhase4 = outOfOcean || phaseTransitionState >= 3f;

            if (inPhase3)
            {
                if (phaseTransitionState >= 2f)
                {
                    Main.rainTime = 300;
                    Main.maxRaining = 0.87f;
                }

                npc.damage = (int)(npc.damage * 1.15);
                if (inPhase4)
                    npc.damage = (int)(npc.damage * 1.15);
            }
            else
                CalamityMod.CalamityMod.StopRain();

            // Become fully opaque in contexts where the boss should not need to be transparent.
            if (!inPhase3 && attackState != (int)OldDukeAttackState.SpawnAnimation)
                npc.Opacity = 1f;

            // Define a general-purpose mouth position vector.
            Vector2 mouthPosition = npc.Center + new Vector2((float)Math.Cos(npc.rotation) * (npc.width + 28f) * -npc.spriteDirection * 0.5f, 50f);
            
            switch ((OldDukeAttackState)(int)attackState)
            {
                case OldDukeAttackState.SpawnAnimation:
                    DoBehavior_SpawnEffects(npc, target, attackTimer, ref frameType);
                    break;
                case OldDukeAttackState.AttackSelectionWait:
                    DoBehavior_AttackSelectionWait(npc, target, inPhase4, attackTimer, ref frameType);
                    break;
                case OldDukeAttackState.Charge:
                    DoBehavior_Charge(npc, target, inPhase2, inPhase3, inPhase4, attackTimer, ref frameType);
                    break;
                case OldDukeAttackState.FastRegularCharge:
                    DoBehavior_FastRegularCharge(npc, target, attackTimer, ref frameType);
                    break;
                case OldDukeAttackState.AcidBelch:
                    DoBehavior_AcidBelch(npc, target, inPhase2, mouthPosition, attackTimer, ref frameType);
                    break;
                case OldDukeAttackState.AcidBubbleFountain:
                    DoBehavior_AcidBubbleFountain(npc, target, inPhase2, attackTimer, ref frameType);
                    break;
                case OldDukeAttackState.SharkronSpinSummon:
                    DoBehavior_SharkronSpinSummon(npc, target, attackTimer, ref frameType);
                    break;
                case OldDukeAttackState.ToothBallVomit:
                    DoBehavior_ToothBallVomit(npc, target, inPhase2, mouthPosition, attackTimer, ref frameType);
                    break;
                case OldDukeAttackState.GoreAndAcidSpit:
                    DoBehavior_GoreAndAcidSpit(npc, target, inPhase2, mouthPosition, attackTimer, ref frameType);
                    break;
                case OldDukeAttackState.TeleportPause:
                    DoBehavior_TeleportPause(npc, target, attackTimer, ref frameType);
                    break;
            }

            attackTimer++;

            return false;
        }

        #region Specific Behaviors

        public static void DoBehavior_PhaseTransitionEffects(NPC npc, float phaseTransitionTimer, ref float frameType, ref float phaseTransitionSharkSpawnOffset)
        {
            // Disable damage.
            npc.damage = 0;

            // Slow down and rotate towards 0 degrees.
            npc.velocity *= 0.965f;
            npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, 0f, 0.02f);
            npc.rotation = npc.rotation.AngleLerp(0f, 0.1f).AngleTowards(0f, 0.15f);

            // Decide frames.
            if (phaseTransitionTimer is < (PhaseTransitionTime - 48f) and > (PhaseTransitionTime - 65f))
                frameType = (int)OldDukeFrameType.Roar;
            else
            {
                frameType = (int)OldDukeFrameType.FlapWings;
                npc.frameCounter++;
            }

            // Roar and summon sharks below the boss.
            if (phaseTransitionTimer == PhaseTransitionTime - 60f)
                SoundEngine.PlaySound(OldDukeBoss.RoarSound, Main.player[npc.target].Center);

            if (phaseTransitionTimer >= PhaseTransitionTime - 60f && npc.life > npc.lifeMax * Phase3LifeRatio)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient && phaseTransitionTimer % 6f == 0f)
                {
                    phaseTransitionSharkSpawnOffset += 135f;
                    Vector2 spawnOffset = new(phaseTransitionSharkSpawnOffset + 50f, 340f);
                    NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.Center.X + spawnOffset.X), (int)(npc.Center.Y + spawnOffset.Y), ModContent.NPCType<SulphurousSharkron>(), 0, 0f, 0f, 1f, -18f, 255);
                    NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.Center.X - spawnOffset.X), (int)(npc.Center.Y + spawnOffset.Y), ModContent.NPCType<SulphurousSharkron>(), 0, 0f, 0f, -1f, -18f, 255);
                }
            }
        }

        public static void DoBehavior_SpawnEffects(NPC npc, Player target, float attackTimer, ref float frameType)
        {
            // Don't do damge during the spawn animation.
            npc.damage = 0;

            if (attackTimer < 20f)
            {
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();

                npc.alpha -= 5;
                if (Collision.SolidCollision(npc.position, npc.width, npc.height))
                    npc.alpha += 15;
                npc.alpha = Utils.Clamp(npc.alpha, 0, 224);
                npc.velocity = -Vector2.UnitY * 6f;
            }
            else
                npc.velocity *= 0.97f;

            // Right before and after the spawn animation dust stuff, roar.
            if (attackTimer is > 52f and < 64f)
                frameType = (int)OldDukeFrameType.Roar;
            
            // Otherwise, flap wings.
            else
            {
                frameType = (int)OldDukeFrameType.FlapWings;
                npc.frameCounter++;
            }

            // Play a sound and emit sulphurous acid dust.
            if (attackTimer == 55f)
            {
                for (int i = 0; i < 36; i++)
                {
                    Vector2 dustSpawnPosition = npc.Center + (Vector2.Normalize(npc.velocity) * new Vector2(npc.width / 2f, npc.height) * 0.4f).RotatedBy(MathHelper.TwoPi * i / 36f);
                    Dust acid = Dust.NewDustPerfect(dustSpawnPosition, (int)CalamityDusts.SulfurousSeaAcid);
                    acid.noGravity = true;
                    acid.noLight = true;
                    acid.velocity = npc.SafeDirectionTo(dustSpawnPosition) * 3f;
                }

                SoundEngine.PlaySound(OldDukeBoss.VomitSound, npc.Center);
            }

            if (attackTimer >= 75f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_AttackSelectionWait(NPC npc, Player target, bool inPhase4, float attackTimer, ref float frameType)
        {
            if (attackTimer >= 25f)
                npc.damage = 0;

            OldDukeAttackState upcomingAttack = (OldDukeAttackState)(int)npc.ai[2];
            bool goingToCharge = upcomingAttack is OldDukeAttackState.Charge or OldDukeAttackState.FastRegularCharge;
            int waitDelay = 45;
            if (goingToCharge)
                waitDelay = 30;
            if (upcomingAttack == OldDukeAttackState.TeleportPause)
                waitDelay = 32;
            if (inPhase4)
                waitDelay -= 4;
            if (BossRushEvent.BossRushActive)
                waitDelay = (int)(waitDelay * 0.56f);

            ref float horizontalHoverOffset = ref npc.Infernum().ExtraAI[0];

            // Hover near the target.
            if (horizontalHoverOffset == 0f)
                horizontalHoverOffset = Math.Sign(target.Center.X - npc.Center.X) * 500f;
            Vector2 hoverDestination = target.Center + new Vector2(horizontalHoverOffset, -350f) - npc.velocity;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 22f, 1.05f);

            // Look at the target.
            npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
            npc.rotation = npc.AngleTo(target.Center);
            if (upcomingAttack == OldDukeAttackState.Charge)
                npc.rotation = npc.AngleTo(target.Center + target.velocity * 20f);

            if (npc.spriteDirection == 1)
                npc.rotation += MathHelper.Pi;

            // Handle frames.
            frameType = (int)OldDukeFrameType.FlapWings;
            npc.frameCounter++;

            if (attackTimer >= waitDelay)
            {
                if (upcomingAttack is OldDukeAttackState.FastRegularCharge)
                    SoundEngine.PlaySound(OldDukeBoss.VomitSound with { Volume = 1.5f, Pitch = -0.225f }, target.Center);
                SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_Charge(NPC npc, Player target, bool inPhase2, bool inPhase3, bool inPhase4, float attackTimer, ref float frameType)
        {
            int chargeTime = 21;
            float chargeSpeed = 36f;
            float aimAheadFactor = 0.95f;

            if (inPhase2)
            {
                chargeTime -= 3;
                chargeSpeed += 4f;
            }
            if (inPhase3)
            {
                chargeTime -= inPhase4 ? 6 : 5;
                chargeSpeed += inPhase4 ? 10.5f : 5f;
            }
            if (BossRushEvent.BossRushActive)
            {
                chargeTime -= 3;
                chargeSpeed += 6f;
            }

            // Speed up the farther away the target is.
            chargeSpeed += npc.Distance(target.Center) * 0.00775f;

            if (attackTimer >= chargeTime)
            {
                SelectNextAttack(npc);
                return;
            }

            frameType = (int)OldDukeFrameType.Charge;

            // Do the charge on the first frame.
            if (attackTimer == 1f)
            {
                int chargeDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                npc.velocity = npc.SafeDirectionTo(target.Center + target.velocity * aimAheadFactor * 11f) * chargeSpeed;
                npc.spriteDirection = chargeDirection;

                npc.rotation = npc.velocity.ToRotation();
                if (npc.spriteDirection == 1)
                    npc.rotation += MathHelper.Pi;
                npc.netUpdate = true;

                return;
            }

            // Otherwise accelerate and emit sulphurous dust.
            npc.velocity *= 1.01f;

            // Spawn dust
            int dustCount = 7;
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 dustSpawnPosition = npc.Center + (Vector2.Normalize(npc.velocity) * new Vector2((npc.width + 50) / 2f, npc.height) * 0.75f).RotatedBy(MathHelper.TwoPi * i / dustCount);
                Vector2 dustVelocity = (Main.rand.NextFloatDirection() * MathHelper.PiOver2).ToRotationVector2() * Main.rand.NextFloat(3f, 8f);
                Dust acid = Dust.NewDustPerfect(dustSpawnPosition + dustVelocity, (int)CalamityDusts.SulfurousSeaAcid, dustVelocity);
                acid.scale *= 1.45f;
                acid.velocity *= 0.25f;
                acid.velocity -= npc.velocity;
                acid.noGravity = true;
                acid.noLight = true;
            }
        }

        public static void DoBehavior_FastRegularCharge(NPC npc, Player target, float attackTimer, ref float frameType)
        {
            int chargeTime = 23;
            float chargeSpeed = 56f;
            if (BossRushEvent.BossRushActive)
            {
                chargeTime -= 3;
                chargeSpeed += 4.7f;
            }

            if (attackTimer >= chargeTime)
            {
                SelectNextAttack(npc);
                return;
            }

            frameType = (int)OldDukeFrameType.Charge;

            // Do the charge on the first frame.
            if (attackTimer == 1f)
            {
                int chargeDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                npc.velocity = npc.SafeDirectionTo(target.Center + target.velocity) * chargeSpeed;
                npc.spriteDirection = chargeDirection;

                npc.rotation = npc.velocity.ToRotation();
                if (npc.spriteDirection == 1)
                    npc.rotation += MathHelper.Pi;
                npc.netUpdate = true;

                return;
            }

            // Otherwise accelerate and emit sulphurous dust.
            npc.velocity *= 1.01f;

            // Spawn dust
            int dustCount = 7;
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 dustSpawnPosition = npc.Center + (Vector2.Normalize(npc.velocity) * new Vector2((npc.width + 50) / 2f, npc.height) * 0.75f).RotatedBy(MathHelper.TwoPi * i / dustCount);
                Vector2 dustVelocity = (Main.rand.NextFloatDirection() * MathHelper.PiOver2).ToRotationVector2() * Main.rand.NextFloat(3f, 8f);
                Dust acid = Dust.NewDustPerfect(dustSpawnPosition + dustVelocity, (int)CalamityDusts.SulfurousSeaAcid, dustVelocity);
                acid.scale *= 1.45f;
                acid.velocity *= 0.25f;
                acid.velocity -= npc.velocity;
                acid.noGravity = true;
                acid.noLight = true;
            }
        }

        public static void DoBehavior_AcidBelch(NPC npc, Player target, bool inPhase2, Vector2 mouthPosition, float attackTimer, ref float frameType)
        {
            npc.damage = 0;

            int shootDelay = inPhase2 ? 40 : 50;
            int belchCount = inPhase2 ? 5 : 4;
            int belchRate = inPhase2 ? 14 : 23;
            if (BossRushEvent.BossRushActive)
                belchRate -= 5;

            // Hover near the target.
            Vector2 hoverDestination = target.Center + new Vector2(Math.Sign(npc.Center.X - target.Center.X) * 500f, -300f) - npc.velocity;
            if (!npc.WithinRange(hoverDestination, 45f))
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 14.5f, 0.8f);

            // Handle frames.
            if (attackTimer <= shootDelay)
            {
                npc.frameCounter += 1.5f;
                frameType = (int)OldDukeFrameType.FlapWings;
            }
            else
            {
                if (attackTimer == shootDelay + 1f)
                    SoundEngine.PlaySound(OldDukeBoss.VomitSound, npc.Center);

                frameType = (int)OldDukeFrameType.Roar;
            }

            // Look at the target.
            npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
            npc.rotation = npc.AngleTo(target.Center);
            if (npc.spriteDirection == 1)
                npc.rotation += MathHelper.Pi;

            // Release balls of acid at the target from the mouth.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer > shootDelay && (attackTimer - shootDelay) % belchRate == belchRate - 1f)
            {
                Vector2 shootVelocity = (mouthPosition - npc.Center).SafeNormalize(Vector2.UnitX * npc.spriteDirection) * 19f;
                Utilities.NewProjectileBetter(mouthPosition, shootVelocity, ModContent.ProjectileType<SulphuricBlob>(), 320, 0f);
            }

            if (attackTimer >= shootDelay + belchRate * (belchCount + 0.7f))
                SelectNextAttack(npc);
        }

        public static void DoBehavior_AcidBubbleFountain(NPC npc, Player target, bool inPhase2, float attackTimer, ref float frameType)
        {
            npc.damage = 0;

            int shootDelay = inPhase2 ? 40 : 55;
            int bubbleCount = inPhase2 ? 12 : 15;
            int bubbleSummonRate = inPhase2 ? 9 : 15;
            if (BossRushEvent.BossRushActive)
                bubbleSummonRate -= 2;

            // Hover near the target.
            Vector2 hoverDestination = target.Center + new Vector2(Math.Sign(npc.Center.X - target.Center.X) * 500f, -300f) - npc.velocity;
            if (!npc.WithinRange(hoverDestination, 45f))
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 14.5f, 0.75f);

            // Look at the target.
            npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
            npc.rotation = npc.AngleTo(target.Center);
            if (npc.spriteDirection == 1)
                npc.rotation += MathHelper.Pi;

            // Handle frames.
            npc.frameCounter += 1.5f;
            frameType = (int)OldDukeFrameType.FlapWings;

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer > shootDelay && (attackTimer - shootDelay) % bubbleSummonRate == bubbleSummonRate - 1f)
            {
                Vector2 bubbleSpawnPosition = target.Center + new Vector2(Main.rand.NextFloatDirection() * 1000f + target.velocity.X * 60f, 800f);
                Vector2 bubbleVelocity = -Vector2.UnitY * Main.rand.NextFloat(10.5f, 13.5f);
                if (inPhase2)
                    bubbleVelocity *= 1.15f;
                if (BossRushEvent.BossRushActive)
                    bubbleVelocity *= 1.6f;

                Utilities.NewProjectileBetter(bubbleSpawnPosition, bubbleVelocity, ModContent.ProjectileType<AcidFountainBubble>(), 320, 0f);
            }

            if (attackTimer >= shootDelay + bubbleSummonRate * (bubbleCount + 0.5f))
                SelectNextAttack(npc);
        }

        public static void DoBehavior_SharkronSpinSummon(NPC npc, Player target, float attackTimer, ref float frameType)
        {
            npc.damage = 0;

            int spinTime = 72;
            float spinSpeed = 34f;
            float totalRotations = 2f;

            if (attackTimer == 31f)
            {
                // Prepare a charge to reset speed.
                int chargeDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                npc.velocity = npc.SafeDirectionTo(target.Center) * spinSpeed;
                npc.spriteDirection = chargeDirection;

                npc.rotation = npc.velocity.ToRotation();
                if (npc.spriteDirection == 1)
                    npc.rotation += MathHelper.Pi;
                npc.netUpdate = true;

                // Play sounds and spawn Tooth Balls and a Vortex
                SoundEngine.PlaySound(OldDukeBoss.RoarSound, npc.Center);

                Vector2 vortexSpawnPosition = npc.Center + npc.velocity.RotatedBy(npc.spriteDirection * MathHelper.PiOver2) * spinTime / totalRotations / MathHelper.TwoPi;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Utilities.NewProjectileBetter(vortexSpawnPosition, Vector2.Zero, ModContent.ProjectileType<SharkSummonVortex>(), 480, 0f);

                    // Release sharks from above.
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 spawnPosition = target.Center + new Vector2(Main.rand.NextFloatDirection() * 1000f, -1050f);
                        int shark = NPC.NewNPC(npc.GetSource_FromAI(), (int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<SulphurousSharkron>());
                        if (Main.npc.IndexInRange(shark))
                        {
                            Main.npc[shark].velocity = Main.rand.NextVector2CircularEdge(8f, 8f);
                            Main.npc[shark].ai[1] = 1f;
                            Main.npc[shark].netUpdate = true;
                        }
                    }
                }
            }

            frameType = (int)OldDukeFrameType.Charge;

            if (attackTimer < spinTime + 30f && attackTimer >= 30f)
            {
                float rotationalOffset = npc.spriteDirection * MathHelper.TwoPi / spinTime * totalRotations;
                npc.velocity = npc.velocity.RotatedBy(rotationalOffset);
                npc.rotation += rotationalOffset;
            }

            if (attackTimer >= spinTime + 60f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_ToothBallVomit(NPC npc, Player target, bool inPhase3, Vector2 mouthPosition, float attackTimer, ref float frameType)
        {
            npc.damage = 0;

            int shootDelay = inPhase3 ? 42 : 55;
            int belchCount = inPhase3 ? 7 : 5;
            int belchRate = inPhase3 ? 18 : 30;
            if (BossRushEvent.BossRushActive)
                belchRate -= 8;

            // Hover near the target.
            Vector2 hoverDestination = target.Center + new Vector2(Math.Sign(npc.Center.X - target.Center.X) * 500f, -300f) - npc.velocity;
            if (!npc.WithinRange(hoverDestination, 45f))
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 14.5f, 0.75f);

            // Handle frames.
            if (attackTimer <= shootDelay)
            {
                npc.frameCounter += 1.5f;
                frameType = (int)OldDukeFrameType.FlapWings;
            }
            else
            {
                if (attackTimer == shootDelay + 1f)
                    SoundEngine.PlaySound(OldDukeBoss.VomitSound, npc.Center);

                frameType = (int)OldDukeFrameType.Roar;
            }

            // Look at the target.
            npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
            npc.rotation = npc.AngleTo(target.Center);
            if (npc.spriteDirection == 1)
                npc.rotation += MathHelper.Pi;

            // Release tooth balls at the target from the mouth.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer > shootDelay && (attackTimer - shootDelay) % belchRate == belchRate - 1f)
            {
                Vector2 shootVelocity = (mouthPosition - npc.Center).SafeNormalize(Vector2.UnitX * npc.spriteDirection) * 11f;
                int toothBall = NPC.NewNPC(npc.GetSource_FromAI(), (int)mouthPosition.X, (int)mouthPosition.Y, ModContent.NPCType<OldDukeToothBall>());
                if (Main.npc.IndexInRange(toothBall))
                    Main.npc[toothBall].velocity = shootVelocity;
            }

            if (attackTimer >= shootDelay + belchRate * (belchCount + 0.7f))
                SelectNextAttack(npc);
        }

        public static void DoBehavior_GoreAndAcidSpit(NPC npc, Player target, bool inPhase3, Vector2 mouthPosition, float attackTimer, ref float frameType)
        {
            npc.damage = 0;

            int goreShootDelay = 92;
            int goreCount = inPhase3 ? 50 : 32;
            npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
            npc.velocity *= 0.97f;

            if (attackTimer == 1f && !npc.WithinRange(target.Center, 1360f))
            {
                SelectNextAttack(npc);
                return;
            }

            float idealRotation = MathHelper.Pi / 6f * npc.spriteDirection;
            if (npc.rotation > MathHelper.Pi)
                npc.rotation -= MathHelper.Pi;
            if (npc.rotation < -MathHelper.Pi)
                npc.rotation += MathHelper.Pi;
            npc.rotation = npc.rotation.AngleLerp(idealRotation, 0.1f).AngleTowards(idealRotation, 0.1f);

            if (attackTimer < goreShootDelay - 5f)
            {
                frameType = (int)OldDukeFrameType.FlapWings;
                npc.frameCounter++;
            }
            else
                frameType = (int)OldDukeFrameType.Roar;

            if (attackTimer == goreShootDelay)
            {
                SoundEngine.PlaySound(OldDukeBoss.VomitSound, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < goreCount; i++)
                    {
                        Vector2 goreVelocity = idealRotation.ToRotationVector2().RotatedByRandom(0.43f) * -npc.spriteDirection * Main.rand.NextFloat(10f, 15.6f);
                        Utilities.NewProjectileBetter(mouthPosition, goreVelocity, ModContent.ProjectileType<OldDukeGore>(), 345, 0f);
                    }
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer > goreShootDelay && attackTimer < goreShootDelay + 30f)
            {
                Vector2 acidVelocity = idealRotation.ToRotationVector2().RotatedByRandom(0.43f) * -npc.spriteDirection * Main.rand.NextFloat(13f, 18f);
                Utilities.NewProjectileBetter(mouthPosition, acidVelocity, ModContent.ProjectileType<HomingAcid>(), 325, 0f);
            }

            if (attackTimer >= goreShootDelay + 30f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_TeleportPause(NPC npc, Player target, float attackTimer, ref float frameType)
        {
            npc.damage = 0;
            npc.dontTakeDamage = true;

            int fadeTime = 15;
            if (attackTimer <= fadeTime)
                npc.Opacity = Utils.GetLerpValue(12f, 0f, attackTimer, true);
            else if (attackTimer <= fadeTime * 2f)
                npc.Opacity = Utils.GetLerpValue(12f, 24f, attackTimer, true);

            // Decide frames.
            if (attackTimer > fadeTime - 4f && attackTimer < fadeTime + 4f)
                frameType = (int)OldDukeFrameType.Roar;
            else
            {
                frameType = (int)OldDukeFrameType.FlapWings;
                npc.frameCounter++;
            }

            npc.velocity *= 0.95f;

            // Teleport.
            if (attackTimer == fadeTime)
            {
                SoundEngine.PlaySound(OldDukeBoss.RoarSound, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.Center = target.Center + new Vector2(Math.Sign(npc.Center.X - target.Center.X) * -560f, -200f);
                    npc.netUpdate = true;
                }
            }

            // Look at the target.
            npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
            npc.rotation = npc.AngleTo(target.Center);
            if (npc.spriteDirection == 1)
                npc.rotation += MathHelper.Pi;

            if (attackTimer >= fadeTime * 2f)
                SelectNextAttack(npc);
        }
        #endregion Specific Behaviors

        #region Utilities
        public static void SelectNextAttack(NPC npc)
        {
            bool inPhase2 = npc.Infernum().ExtraAI[6] == 1f;
            bool inPhase3 = npc.Infernum().ExtraAI[6] == 2f;
            bool inPhase4 = npc.Infernum().ExtraAI[6] == 3f;
            OldDukeAttackState oldAttackState = (OldDukeAttackState)npc.ai[0];
            OldDukeAttackState newAttackState;

            List<OldDukeAttackState> attackPattern = Phase1AttackPattern;
            if (inPhase2)
                attackPattern = Phase2AttackPattern;
            if (inPhase3)
                attackPattern = Phase3AttackPattern;
            if (inPhase4)
                attackPattern = Phase4AttackPattern;
            newAttackState = attackPattern[(int)(npc.ai[3] + 1) % attackPattern.Count];

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.ai[1] = 0f;
            if (oldAttackState == OldDukeAttackState.AttackSelectionWait)
            {
                npc.ai[0] = npc.ai[2];
                npc.ai[2] = 0f;
            }
            else
            {
                npc.TargetClosest();
                npc.ai[0] = (int)OldDukeAttackState.AttackSelectionWait;
                npc.ai[2] = (int)newAttackState;
                npc.ai[3]++;
            }
            npc.netUpdate = true;
        }

        public static void CleanupLeftoverEntities()
        {
            // Clear a bunch of stray projectiles.
            int sharkronID = ModContent.NPCType<SulphurousSharkron>();
            int toothBallID = ModContent.NPCType<OldDukeToothBall>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                bool npcTypeThatShouldDisappear = Main.npc[i].type == sharkronID || Main.npc[i].type == toothBallID;
                if (Main.npc[i].active && npcTypeThatShouldDisappear)
                    Main.npc[i].active = false;
            }
            Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<HomingAcid>(), ModContent.ProjectileType<SandPoisonCloudOldDuke>(), ModContent.ProjectileType<SulphuricBlob>(), ModContent.ProjectileType<SharkSummonVortex>(),
                ModContent.ProjectileType<OldDukeGore>());
        }

        #endregion Utilities

        #endregion AI

        #region Frames and Drawcode

        public override void FindFrame(NPC npc, int frameHeight)
        {
            switch ((OldDukeFrameType)(int)npc.localAI[0])
            {
                case OldDukeFrameType.FlapWings:
                    if (npc.frameCounter >= 8)
                    {
                        npc.frameCounter = 0;
                        npc.frame.Y += frameHeight;
                    }
                    if (npc.frame.Y >= frameHeight * 6)
                        npc.frame.Y = 0;
                    break;

                case OldDukeFrameType.Charge:
                    npc.frame.Y = frameHeight * 2;
                    npc.frameCounter = 0;
                    break;
                case OldDukeFrameType.Roar:
                    npc.frame.Y = frameHeight * 6;
                    npc.frameCounter = 0;
                    break;
            }
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            ref float attackTimer = ref npc.ai[1];
            ref float phaseTransitionState = ref npc.Infernum().ExtraAI[6];
            ref float phaseTransitionTimer = ref npc.Infernum().ExtraAI[7];
            bool inTheMiddleOfPhaseTransition = phaseTransitionTimer > 0f;
            OldDukeAttackState currentAttack = (OldDukeAttackState)(int)npc.ai[0];

            SpriteEffects spriteEffects = npc.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Texture2D eyeTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/OldDuke/OldDukeGlow").Value;
            Vector2 origin = new(texture.Width / 2, texture.Height / Main.npcFrameCount[npc.type] / 2);
            Color color = lightColor;
            Color eyeColor = Color.Lerp(Color.White, Color.Yellow, 0.5f);
            Color afterimageEndColor = Color.White;
            float rotationalAfterimageFade = 0f;
            bool inPhase2 = phaseTransitionState >= 1f && (phaseTransitionTimer == 0f || phaseTransitionTimer > PhaseTransitionTime - 60f || phaseTransitionState >= 2f);
            bool inPhase3 = phaseTransitionState >= 2f && (phaseTransitionTimer == 0f || phaseTransitionTimer > PhaseTransitionTime - 60f);

            if (inPhase3)
                color = CalamityGlobalNPC.buffColor(color, 0.4f, 0.8f, 0.4f, 1f);

            else if (inPhase2)
                color = CalamityGlobalNPC.buffColor(color, 0.5f, 0.7f, 0.5f, 1f);

            else if (inTheMiddleOfPhaseTransition && phaseTransitionTimer >= PhaseTransitionTime - 60f)
            {
                float transitionFade = Utils.GetLerpValue(PhaseTransitionTime - 60f, PhaseTransitionTime, phaseTransitionTimer, true);
                color = CalamityGlobalNPC.buffColor(color, 1f - 0.5f * transitionFade, 1f - 0.3f * transitionFade, 1f - 0.5f * transitionFade, 1f);
            }

            int afterimageCount = 10;
            if (currentAttack == OldDukeAttackState.SpawnAnimation)
                afterimageCount = 0;

            if (currentAttack == OldDukeAttackState.AttackSelectionWait)
                afterimageCount = 7;

            if (currentAttack is OldDukeAttackState.AcidBubbleFountain or OldDukeAttackState.AcidBelch)
                afterimageCount = 4;

            if (currentAttack == OldDukeAttackState.Charge || inPhase3)
            {
                afterimageEndColor = Color.Lime;
                rotationalAfterimageFade = 0.5f;
            }
            else
                color = lightColor;

            // Draw necessary afterimages.
            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < afterimageCount; i += 2)
                {
                    Color afterimageColor = npc.GetAlpha(Color.Lerp(color, afterimageEndColor, rotationalAfterimageFade)) * ((afterimageCount - i) / 15f);
                    Vector2 afterimageDrawPosition = npc.oldPos[i] + new Vector2(npc.width, npc.height) / 2f - Main.screenPosition;
                    Main.spriteBatch.Draw(texture, afterimageDrawPosition, npc.frame, afterimageColor, npc.rotation, origin, npc.scale, spriteEffects, 0f);
                }

                // Draw eye afterimages.
                if (inPhase2)
                {
                    for (int i = 1; i < afterimageCount; i += 2)
                    {
                        Color afterimageColor = eyeColor;
                        if (!inPhase3)
                            afterimageColor = npc.GetAlpha(afterimageColor);
                        afterimageColor *= (afterimageCount - i) / 15f;

                        Vector2 afterimageDrawPosition = npc.oldPos[i] + new Vector2(npc.width, npc.height) / 2f - Main.screenPosition;
                        Main.spriteBatch.Draw(eyeTexture, afterimageDrawPosition, npc.frame, afterimageColor, npc.rotation, origin, npc.scale, spriteEffects, 0f);
                    }
                }
            }

            int rotationalOffsetImageCount = 0;
            float rotationalOffsetFade = 0f;
            float rotationalOffsetOutwardness = 0f;

            if (currentAttack == OldDukeAttackState.SpawnAnimation)
                rotationalOffsetImageCount = 0;

            if (inTheMiddleOfPhaseTransition && phaseTransitionTimer > 60f)
            {
                rotationalOffsetImageCount = 6;
                rotationalOffsetFade = (float)Math.Sin(MathHelper.Pi * Utils.GetLerpValue(60f, PhaseTransitionTime, phaseTransitionTimer, true)) / 3f;
                rotationalOffsetOutwardness = 60f;
            }

            if (currentAttack == OldDukeAttackState.TeleportPause)
            {
                rotationalOffsetImageCount = 6;
                rotationalOffsetFade = (float)Math.Sin(MathHelper.Pi * attackTimer / TeleportPauseTime) / 3f;
                rotationalOffsetOutwardness = 20f;
            }

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 0; i < rotationalOffsetImageCount; i++)
                {
                    Color rotationalAfterimageColor = npc.GetAlpha(Color.Lerp(lightColor, afterimageEndColor, rotationalAfterimageFade)) * rotationalOffsetFade;
                    Vector2 rotationalDrawPosition = npc.Center - Main.screenPosition;
                    rotationalDrawPosition += (i / (float)rotationalOffsetImageCount * MathHelper.TwoPi + npc.rotation).ToRotationVector2() * rotationalOffsetOutwardness * rotationalOffsetFade;
                    Main.spriteBatch.Draw(texture, rotationalDrawPosition, npc.frame, rotationalAfterimageColor, npc.rotation, origin, npc.scale, spriteEffects, 0f);

                    Color eyeAfterimageColor = eyeColor;
                    if (!inPhase3)
                        eyeAfterimageColor = npc.GetAlpha(eyeAfterimageColor);
                    eyeAfterimageColor *= rotationalOffsetFade;

                    // Draw eye afterimages.
                    if (inPhase2)
                        Main.spriteBatch.Draw(eyeTexture, rotationalDrawPosition, npc.frame, eyeAfterimageColor, npc.rotation, origin, npc.scale, spriteEffects, 0f);
                }
            }

            Color phase3Color = npc.GetAlpha(Color.Lerp(lightColor, afterimageEndColor, rotationalAfterimageFade));
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Main.spriteBatch.Draw(texture, drawPosition, npc.frame, inPhase3 ? phase3Color : npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, spriteEffects, 0f);

            if (inPhase2)
                Main.spriteBatch.Draw(eyeTexture, drawPosition, npc.frame, inPhase3 ? eyeColor : npc.GetAlpha(eyeColor), npc.rotation, origin, npc.scale, spriteEffects, 0f);

            return false;
        }

        #endregion Frames and Drawcode
    }
}
