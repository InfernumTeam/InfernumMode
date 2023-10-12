using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.NPCs;
using CalamityMod.NPCs.DevourerofGods;
using InfernumMode.Content.BossIntroScreens;
using InfernumMode.Content.Skies;
using InfernumMode.Core.GlobalInstances;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.Netcode.Packets;
using InfernumMode.Core.Netcode;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using DoGHead = CalamityMod.NPCs.DevourerofGods.DevourerofGodsHead;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.DoG
{
    public class DoGPhase1HeadBehaviorOverride : NPCBehaviorOverride
    {
        public enum Phase2TransitionState
        {
            NotEnteringPhase2,
            NeedsToSummonPortal,
            EnteringPortal
        }

        public static Phase2TransitionState CurrentPhase2TransitionState
        {
            get
            {
                if (CalamityGlobalNPC.DoGHead < 0)
                    return Phase2TransitionState.NotEnteringPhase2;

                NPC npc = Main.npc[CalamityGlobalNPC.DoGHead];
                return (Phase2TransitionState)npc.Infernum().ExtraAI[Phase2TransitionStateIndex];
            }
            set
            {
                if (CalamityGlobalNPC.DoGHead < 0)
                    return;

                NPC npc = Main.npc[CalamityGlobalNPC.DoGHead];
                npc.Infernum().ExtraAI[Phase2TransitionStateIndex] = (int)value;
                npc.netUpdate = true;
            }
        }

        public override int NPCOverrideType => ModContent.NPCType<DoGHead>();

        public static int GeneralPortalIndex
        {
            get;
            set;
        }

        public static int ChargePortalIndex
        {
            get;
            set;
        }

        public static int AcceleratingFireballDamage => 380;

        public static int DeathLaserDamage => 400;

        public const float Phase2LifeRatio = 0.8f;

        public const int PassiveMovementTimeP1 = 420;

        public const int AggressiveMovementTimeP1 = 600;

        // Define a bunch of AI indices. This is slightly cursed due to how much DoG's AI has.
        public const int UniversalFightTimerIndex = 0;

        public const int CurrentFlyAccelerationIndex = 1;

        public const int JawRotationIndex = 2;

        public const int ChompEffectsCountdownIndex = 3;

        public const int Phase2TransitionStateIndex = 4;

        public const int InPhase2FlagIndex = 6;

        public const int PhaseCycleTimerIndex = 7;

        public const int PassiveAttackDelayTimerIndex = 8;

        public const int PerformingSpecialAttackFlagIndex = 9;

        public const int SpecialAttackTimerIndex = 10;

        public const int SpecialAttackTypeIndex = 11;

        public const int HasEnteredFinalPhaseFlagIndex = 12;

        public const int AnimationMoveDelayIndex = 13;

        public const int HasPerformedSpecialAttackYetFlagIndex = 14;

        public const int Phase2IntroductionAnimationTimerIndex = 15;

        public const int DeathAnimationTimerIndex = 16;

        public const int DestroyedSegmentsCountIndex = 17;

        public const int InitialUncoilTimerIndex = 18;

        public const int ForceDoGIntoPhase2PortalTimerIndex = 19;

        public const int HasTeleportedAboveTargetFlagIndex = 20;

        public const int HasSpawnedSegmentsIndex = 21;

        public const int ChargeGatePortalTelegraphTimeIndex = 23;

        public const int SegmentNumberIndex = 24;

        public const int BodySegmentFadeTypeIndex = 25;

        public const int AntimatterFormInterpolantIndex = 26;

        public const int SentinelAttackTimerIndex = 27;

        public const int Phase2AggressiveChargeCycleCounterIndex = 28;

        public const int PerpendicularPortalAttackStateIndex = 29;

        public const int PerpendicularPortalAttackTimerIndex = 30;

        public const int PerpendicularPortalAngleIndex = 31;

        public const int PreviousSpecialAttackTypeIndex = 32;

        public const int PreviousSnapAngleIndex = 33;

        public const int TimeSinceLastSnapIndex = 34;

        public const int DamageImmunityCountdownIndex = 35;

        public const int BodySegmentDefense = 70;

        public const float BodySegmentDR = 0.925f;

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio,
            DoGPhase2HeadBehaviorOverride.FinalPhaseLifeRatio
        };

        #region Loading
        public override void Load()
        {
            GlobalNPCOverrides.BossHeadSlotEvent += RedefineMapSlotConditions;
            GlobalNPCOverrides.StrikeNPCEvent += UpdateLifeTriggers;
        }

        private void RedefineMapSlotConditions(NPC npc, ref int index)
        {
            bool isDoG = npc.type == ModContent.NPCType<DoGHead>() || npc.type == ModContent.NPCType<DevourerofGodsBody>() || npc.type == ModContent.NPCType<DevourerofGodsTail>();
            if (isDoG)
            {
                if (npc.Opacity <= 0.02f)
                {
                    index = -1;
                    return;
                }

                int p1HeadIcon = ModContent.GetModBossHeadSlot("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP1HeadMapIcon");
                int p1TailIcon = ModContent.GetModBossHeadSlot("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP1TailMapIcon");
                int p2HeadIcon = ModContent.GetModBossHeadSlot("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2HeadMapIcon");
                int p2BodyIcon = ModContent.GetModBossHeadSlot("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2BodyMapIcon");
                int p2TailIcon = ModContent.GetModBossHeadSlot("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2TailMapIcon");
                bool inPhase2 = DoGPhase2HeadBehaviorOverride.InPhase2;

                if (npc.type == ModContent.NPCType<DoGHead>())
                    index = inPhase2 ? p2HeadIcon : p1HeadIcon;
                else if (npc.type == ModContent.NPCType<DevourerofGodsBody>())
                    index = inPhase2 ? p2BodyIcon : -1;
                else if (npc.type == ModContent.NPCType<DevourerofGodsTail>())
                    index = inPhase2 ? p2TailIcon : p1TailIcon;
            }
        }

        private bool UpdateLifeTriggers(NPC npc, ref NPC.HitModifiers modifiers)
        {
            // Make DoG enter the second phase once ready.
            bool isDoG = npc.type == ModContent.NPCType<DoGHead>() || npc.type == ModContent.NPCType<DevourerofGodsBody>() || npc.type == ModContent.NPCType<DevourerofGodsTail>();
            return !isDoG || HandleDoGLifeBasedHitTriggers(npc, modifiers.FinalDamage.Base, ref modifiers);
        }

        // HandleDoGLifeBasedHitTriggers will never be run serverside, thus ensuring DoG will never properly change phase. This sends a packet to the server to run the intended health phase change calculations.
        public static void UpdateDoGPhaseServer(int npcIndex, double damage)
        {
            NPC npc = Main.npc[npcIndex];
            int life = npc.realLife >= 0 ? Main.npc[npc.realLife].life : npc.life;
            if (life - damage <= npc.lifeMax * Phase2LifeRatio && !DoGPhase2HeadBehaviorOverride.InPhase2 && CurrentPhase2TransitionState == Phase2TransitionState.NotEnteringPhase2)
            {
                npc.dontTakeDamage = true;
                CurrentPhase2TransitionState = Phase2TransitionState.NeedsToSummonPortal;
                npc.netUpdate = true;
                return;
            }

            // Disable damage and start the death animation if the hit would kill DoG.
            if (life - damage <= 1000 && DoGPhase2HeadBehaviorOverride.InPhase2)
            {
                npc.dontTakeDamage = true;
                if (npc.Infernum().ExtraAI[DeathAnimationTimerIndex] == 0f)
                {
                    SoundEngine.PlaySound(DoGHead.SpawnSound, npc.Center);
                    npc.Infernum().ExtraAI[DeathAnimationTimerIndex] = 1f;
                }
                npc.netUpdate = true;
                return;
            }
        }

        public static bool HandleDoGLifeBasedHitTriggers(NPC npc, double realDamage, ref NPC.HitModifiers modifiers)
        {
            int life = npc.realLife >= 0 ? Main.npc[npc.realLife].life : npc.life;

            // Disable damage and enter phase 2 if the hit would bring DoG down to a sufficiently low quantity of HP.
            if (life - realDamage <= npc.lifeMax * Phase2LifeRatio && !DoGPhase2HeadBehaviorOverride.InPhase2 && CurrentPhase2TransitionState == Phase2TransitionState.NotEnteringPhase2)
            {
                modifiers.FinalDamage.Base *= 0;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.dontTakeDamage = true;
                    CurrentPhase2TransitionState = Phase2TransitionState.NeedsToSummonPortal;
                }
                else
                    PacketManager.SendPacket<SyncDoGPacket>(npc.whoAmI, realDamage);
                return false;
            }

            // Disable damage and start the death animation if the hit would kill DoG.
            if (life - realDamage <= 1000 && DoGPhase2HeadBehaviorOverride.InPhase2)
            {
                modifiers.FinalDamage.Base *= 0;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.dontTakeDamage = true;
                    if (npc.Infernum().ExtraAI[DeathAnimationTimerIndex] == 0f)
                    {
                        SoundEngine.PlaySound(DoGHead.SpawnSound, npc.Center);
                        npc.Infernum().ExtraAI[DeathAnimationTimerIndex] = 1f;
                    }
                }
                else
                    PacketManager.SendPacket<SyncDoGPacket>(npc.whoAmI, realDamage);

                return false;
            }
            return true;
        }
        #endregion Loading

        #region AI
        public override void SetDefaults(NPC npc)
        {
            // Set defaults that, if were to be changed by Calamity, would cause significant issues to the fight.
            npc.width = 104;
            npc.height = 104;
            npc.scale = 1.2f;
            npc.Opacity = 0f;
            npc.defense = 0;
            npc.Calamity().DR = 0.3f;
            npc.takenDamageMultiplier = 2f;
        }

        public override bool PreAI(NPC npc)
        {
            ref float universalFightTimer = ref npc.Infernum().ExtraAI[UniversalFightTimerIndex];
            ref float flyAcceleration = ref npc.Infernum().ExtraAI[CurrentFlyAccelerationIndex];
            ref float jawRotation = ref npc.Infernum().ExtraAI[JawRotationIndex];
            ref float chompEffectsCountdown = ref npc.Infernum().ExtraAI[ChompEffectsCountdownIndex];
            ref float phaseCycleTimer = ref npc.Infernum().ExtraAI[PhaseCycleTimerIndex];
            ref float passiveAttackDelay = ref npc.Infernum().ExtraAI[PassiveAttackDelayTimerIndex];
            ref float uncoilTimer = ref npc.Infernum().ExtraAI[InitialUncoilTimerIndex];
            ref float segmentFadeType = ref npc.Infernum().ExtraAI[BodySegmentFadeTypeIndex];
            ref float getInTheFuckingPortalTimer = ref npc.Infernum().ExtraAI[ForceDoGIntoPhase2PortalTimerIndex];

            // Increment timers.
            universalFightTimer++;
            phaseCycleTimer++;
            passiveAttackDelay++;

            // Adjust scale.
            npc.scale = 1.2f;

            // Adjust DR and defense.
            npc.defense = 0;
            npc.Calamity().DR = 0.3f;
            npc.takenDamageMultiplier = 2f;

            // Declare this NPC as the occupant of the DoG whoAmI index.
            CalamityGlobalNPC.DoGHead = npc.whoAmI;

            // Stop rain, because DoG doesn't like it when rain detracts from him trying to snap your head off.
            if (Main.raining)
                Main.raining = false;

            // Prevent the Godslayer Inferno and Whispering Death debuff from being a problem by completely disabling both for the target.
            if (Main.player[npc.target].HasBuff(ModContent.BuffType<GodSlayerInferno>()))
                Main.player[npc.target].ClearBuff(ModContent.BuffType<GodSlayerInferno>());
            if (Main.player[npc.target].HasBuff(ModContent.BuffType<WhisperingDeath>()))
                Main.player[npc.target].ClearBuff(ModContent.BuffType<WhisperingDeath>());

            // Disable most debuffs.
            DoGPhase1BodyBehaviorOverride.KillUnbalancedDebuffs(npc);

            // Emit light.
            Lighting.AddLight((int)(npc.Center.X / 16f), (int)(npc.Center.Y / 16f), 0.2f, 0.05f, 0.2f);

            // Reset the NPC index that stores this segment's true HP.
            if (npc.ai[3] > 0f)
                npc.realLife = (int)npc.ai[3];

            npc.dontTakeDamage = CurrentPhase2TransitionState == Phase2TransitionState.EnteringPortal;

            // Determine the hitbox size.
            npc.Size = Vector2.One * 132f;

            // Defer all further execution to the second phase AI manager if in the second phase.
            if (DoGPhase2HeadBehaviorOverride.InPhase2)
            {
                CalamityGlobalNPC.DoGP2 = npc.whoAmI;
                npc.Calamity().CanHaveBossHealthBar = true;
                npc.ModNPC<DoGHead>().Phase2Started = true;
                npc.Size = Vector2.One * 176f;
                return DoGPhase2HeadBehaviorOverride.Phase2AI(npc, ref phaseCycleTimer, ref passiveAttackDelay, ref segmentFadeType, ref universalFightTimer);
            }

            // Set music.
            npc.ModNPC.Music = (InfernumMode.CalamityMod as CalamityMod.CalamityMod).GetMusicFromMusicMod("DevourerOfGodsP1") ?? MusicID.LunarBoss;

            // Do through the portal once ready to enter the second phase.
            if (CurrentPhase2TransitionState != Phase2TransitionState.NotEnteringPhase2)
            {
                // Set music.
                npc.ModNPC.Music = (InfernumMode.CalamityMod as CalamityMod.CalamityMod).GetMusicFromMusicMod("DevourerOfGodsP2") ?? MusicID.LunarBoss;
                CalamityGlobalNPC.DoGP2 = npc.whoAmI;

                HandlePhase2TransitionEffect(npc);
                getInTheFuckingPortalTimer++;
                if (getInTheFuckingPortalTimer >= 540f)
                {
                    DoGPhase2HeadBehaviorOverride.InPhase2 = true;
                    CurrentPhase2TransitionState = Phase2TransitionState.NotEnteringPhase2;
                }

                return false;
            }

            // Reset opacity.
            npc.Opacity = Lerp(npc.Opacity, 1f, 0.25f);

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Teleport to the sides of the target on the very first frame. This ensures that DoG will always be in a consistent spot before the fight begins.
            if (npc.Infernum().ExtraAI[HasTeleportedAboveTargetFlagIndex] == 0f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.Center = target.Center - Vector2.UnitX * target.direction * 3200f;

                    // Bring segments to the teleport position.
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (Main.npc[i].active && (Main.npc[i].type == ModContent.NPCType<DevourerofGodsBody>() || Main.npc[i].type == ModContent.NPCType<DevourerofGodsTail>()))
                        {
                            Main.npc[i].Center = npc.Center;
                            Main.npc[i].netUpdate = true;
                        }
                    }
                }
                npc.Infernum().ExtraAI[HasTeleportedAboveTargetFlagIndex] = 1f;
                npc.netUpdate = true;
            }

            // Stay away from the target if the screen is being obstructed by the intro animation.
            if (IntroScreenManager.ScreenIsObstructed && universalFightTimer == 1f)
            {
                npc.dontTakeDamage = true;
                npc.Center = target.Center - Vector2.UnitX * target.direction * 3200f;
                npc.netUpdate = true;
            }

            npc.damage = npc.dontTakeDamage ? 0 : 800;

            // Spawn segments
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (npc.Infernum().ExtraAI[HasSpawnedSegmentsIndex] == 0f && npc.ai[0] == 0f)
                {
                    int previousSegment = npc.whoAmI;
                    for (int segmentSpawn = 0; segmentSpawn < 81; segmentSpawn++)
                    {
                        int segment;
                        if (segmentSpawn is >= 0 and < 80)
                            segment = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.position.X + npc.width / 2, (int)npc.position.Y + npc.height / 2, InfernumMode.CalamityMod.Find<ModNPC>("DevourerofGodsBody").Type, npc.whoAmI);
                        else
                            segment = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.position.X + npc.width / 2, (int)npc.position.Y + npc.height / 2, InfernumMode.CalamityMod.Find<ModNPC>("DevourerofGodsTail").Type, npc.whoAmI);

                        Main.npc[segment].realLife = npc.whoAmI;
                        Main.npc[segment].ai[2] = npc.whoAmI;
                        Main.npc[segment].ai[1] = previousSegment;
                        Main.npc[previousSegment].ai[0] = segment;
                        Main.npc[segment].Infernum().ExtraAI[SegmentNumberIndex] = 80f - segmentSpawn;
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, segment, 0f, 0f, 0f, 0);
                        previousSegment = segment;
                    }
                    npc.Infernum().ExtraAI[HasSpawnedSegmentsIndex] = 1f;
                }
            }

            // Chomping after attempting to eat the player.
            bool chomping = !npc.dontTakeDamage && DoGPhase2HeadBehaviorOverride.DoChomp(npc, ref chompEffectsCountdown, ref jawRotation);

            // Despawn if no valid target exists.
            if (target.dead || !target.active)
                DoGPhase2HeadBehaviorOverride.Despawn(npc);

            // Initially uncoil.
            else if (uncoilTimer < 45f)
            {
                uncoilTimer++;
                npc.velocity = Vector2.Lerp(npc.velocity, -Vector2.UnitY * 27f, 0.125f);

                GeneralPortalIndex = -1;
                ChargePortalIndex = -1;
            }
            else if (phaseCycleTimer % (PassiveMovementTimeP1 + AggressiveMovementTimeP1) < AggressiveMovementTimeP1)
            {
                bool dontChompYet = phaseCycleTimer % (PassiveMovementTimeP1 + AggressiveMovementTimeP1) < 90f;
                if (phaseCycleTimer % (PassiveMovementTimeP1 + AggressiveMovementTimeP1) == 1f)
                    DoGSkyInfernum.CreateLightningBolt(new Color(1f, 0f, 0f, 0.2f), 16, true);

                DoGPhase2HeadBehaviorOverride.DoAggressiveFlyMovement(npc, target, dontChompYet, chomping, ref jawRotation, ref chompEffectsCountdown, ref universalFightTimer, ref flyAcceleration);
            }
            else
            {
                if (phaseCycleTimer % (PassiveMovementTimeP1 + AggressiveMovementTimeP1) == AggressiveMovementTimeP1 + 1f)
                    DoGSkyInfernum.CreateLightningBolt(Color.White, 16, true);

                DoGPhase2HeadBehaviorOverride.DoPassiveFlyMovement(npc, ref jawRotation, ref chompEffectsCountdown, false);

                // Idly release laserbeams.
                if (phaseCycleTimer % 150f == 0f && passiveAttackDelay >= 300f)
                {
                    SoundEngine.PlaySound(SoundID.Item12, target.position);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 16; i++)
                        {
                            Vector2 spawnOffset = (TwoPi * i / 16f).ToRotationVector2() * 1650f + Main.rand.NextVector2Circular(130f, 130f);
                            Vector2 laserShootVelocity = spawnOffset.SafeNormalize(Vector2.UnitY) * -Main.rand.NextFloat(20f, 24f) + Main.rand.NextVector2Circular(2f, 2f);

                            ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(laser =>
                            {
                                laser.MaxUpdates = 3;
                            });
                            Utilities.NewProjectileBetter(target.Center + spawnOffset, laserShootVelocity, ModContent.ProjectileType<DoGDeathInfernum>(), DeathLaserDamage, 0f);
                        }
                    }
                }
            }

            npc.rotation = npc.velocity.ToRotation() + PiOver2;
            return false;
        }

        public static void HandlePhase2TransitionEffect(NPC npc)
        {
            npc.Calamity().CanHaveBossHealthBar = false;
            npc.velocity = npc.velocity.ClampMagnitude(32f, 60f);
            npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * Lerp(npc.velocity.Length(), 50f, 0.1f);
            npc.damage = 0;

            // Summon the portal and become fully opaque if the portal hasn't been created yet.
            if (CurrentPhase2TransitionState == Phase2TransitionState.NeedsToSummonPortal)
            {
                // Spawn the portal.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 spawnPosition = npc.Center + npc.velocity.SafeNormalize(Vector2.UnitX) * 2150f;

                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(portal =>
                    {
                        portal.localAI[0] = 1f;
                        portal.localAI[1] = DoGPhase2IntroPortalGate.Phase2AnimationTime;
                        portal.ModProjectile<DoGChargeGate>().IsGeneralPortalIndex = true;
                    });
                    Utilities.NewProjectileBetter(spawnPosition, Vector2.Zero, ModContent.ProjectileType<DoGChargeGate>(), 0, 0f);
                }

                int headType = ModContent.NPCType<DoGHead>();
                int bodyType = ModContent.NPCType<DevourerofGodsBody>();
                int tailType = ModContent.NPCType<DevourerofGodsTail>();
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && (Main.npc[i].type == headType || Main.npc[i].type == bodyType || Main.npc[i].type == tailType))
                    {
                        Main.npc[i].Opacity = 1f;
                        Main.npc[i].netUpdate = true;
                    }
                }

                npc.Opacity = 1f;
                CurrentPhase2TransitionState = Phase2TransitionState.EnteringPortal;
            }

            // Enter the portal if it's being touched.
            if (GeneralPortalIndex >= 0 && Main.projectile[GeneralPortalIndex].Hitbox.Intersects(npc.Hitbox))
                npc.alpha = Utils.Clamp(npc.alpha + 140, 0, 255);

            // Vanish if the target died in the middle of the transition.
            if (Main.player[npc.target].dead || !Main.player[npc.target].active)
            {
                npc.TargetClosest();
                if (Main.player[npc.target].dead || !Main.player[npc.target].active)
                    npc.active = false;
            }
        }

        #endregion AI

        #region Drawing
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            if (DoGPhase2HeadBehaviorOverride.InPhase2)
                return DoGPhase2HeadBehaviorOverride.PreDraw(npc, lightColor);

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            float jawRotation = npc.Infernum().ExtraAI[JawRotationIndex];

            Texture2D headTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP1Head").Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 headTextureOrigin = headTexture.Size() * 0.5f;

            Texture2D jawTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP1Jaw").Value;
            Vector2 jawOrigin = jawTexture.Size() * 0.5f;

            for (int i = -1; i <= 1; i += 2)
            {
                float jawBaseOffset = 20f;
                SpriteEffects jawSpriteEffect = spriteEffects;
                if (i == 1)
                {
                    jawSpriteEffect |= SpriteEffects.FlipHorizontally;
                    jawBaseOffset *= -1f;
                }
                Vector2 jawPosition = drawPosition;
                jawPosition += Vector2.UnitX.RotatedBy(npc.rotation + jawRotation * i) * (18f + i * (34f + jawBaseOffset + Sin(jawRotation) * 20f));
                jawPosition -= Vector2.UnitY.RotatedBy(npc.rotation) * (16f + Sin(jawRotation) * 20f);
                Main.spriteBatch.Draw(jawTexture, jawPosition, null, lightColor, npc.rotation + jawRotation * i, jawOrigin, npc.scale, jawSpriteEffect, 0f);
            }

            Rectangle headFrame = headTexture.Frame();
            Main.spriteBatch.Draw(headTexture, drawPosition, headFrame, npc.GetAlpha(lightColor), npc.rotation, headTextureOrigin, npc.scale, spriteEffects, 0f);

            Texture2D glowmaskTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP1HeadGlow").Value;
            Main.spriteBatch.Draw(glowmaskTexture, drawPosition, headFrame, Color.White, npc.rotation, headTextureOrigin, npc.scale, spriteEffects, 0f);

            return false;
        }
        #endregion Drawing

        #region Death Effects
        public override bool CheckDead(NPC npc)
        {
            // StrikeNPC stuff will handle the rest of this. This just exists to ensure that DoG doesn't die early.
            npc.life = 1;
            npc.dontTakeDamage = true;
            npc.active = true;
            npc.netUpdate = true;
            return false;
        }
        #endregion Death Effects

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Mods.InfernumMode.PetDialog.DoGTip1";
            yield return n =>
            {
                if (!Main.LocalPlayer.HasDash())
                    return "Mods.InfernumMode.PetDialog.DoGDashTip";
                else if (!Main.LocalPlayer.HasShieldBash())
                    return "Mods.InfernumMode.PetDialog.DoGBashTip";
                return string.Empty;
            };
            yield return n =>
            {
                if (TipsManager.ShouldUseJokeText)
                    return "Mods.InfernumMode.PetDialog.DoGJokeTip1";
                return string.Empty;
            };
        }
        #endregion Tips
    }
}
