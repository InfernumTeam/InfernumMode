using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.Particles;
using CalamityMod.Sounds;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Common.Graphics.ScreenEffects;
using InfernumMode.Content.Projectiles.Pets;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Twins
{
    public static class TwinsAttackSynchronizer
    {
        public enum TwinsAttackState
        {
            ChargeRedirect,
            DownwardCharge,
            SwitchCharges,
            Spin,
            FlamethrowerBurst,
            ChaoticFireAndDownwardLaser,
            LazilyObserve,
            DeathAnimation
        }

        internal static int _targetIndex = -1;

        public static int SpazmatismIndex
        {
            get;
            set;
        }

        public static int RetinazerIndex
        {
            get;
            set;
        }

        public static int UniversalStateIndex
        {
            get;
            set;
        }

        public static int UniversalAttackTimer
        {
            get;
            set;
        }

        public static float BackgroundColorIntensity
        {
            get;
            set;
        }

        public static TwinsAttackState CurrentAttackState
        {
            get;
            set;
        }

        public static Player Target => Main.player[_targetIndex];

        public const int AttackSequenceLength = 12;

        public static bool InPhase2
        {
            get
            {
                // If no eyes are alive, nothing is enraged.
                if (SpazmatismIndex == -1 && RetinazerIndex == -1)
                    return false;

                // If only one eye is active, become enraged.
                if (InFinalPhase)
                    return true;

                float spazmatismLifeRatio = Main.npc[SpazmatismIndex].life / (float)Main.npc[SpazmatismIndex].lifeMax;
                float retinzerLifeRatio = Main.npc[RetinazerIndex].life / (float)Main.npc[RetinazerIndex].lifeMax;
                return spazmatismLifeRatio < Phase2LifeRatioThreshold || retinzerLifeRatio < Phase2LifeRatioThreshold;
            }
        }

        public static bool InFinalPhase
        {
            get
            {
                if (SpazmatismIndex == -1 && RetinazerIndex != -1)
                    return true;
                if (SpazmatismIndex != -1 && RetinazerIndex == -1)
                    return true;
                return false;
            }
        }

        public static float CombinedLifeRatio
        {
            get
            {
                int spazmatismIndex = NPC.FindFirstNPC(NPCID.Spazmatism);
                int retinazerIndex = NPC.FindFirstNPC(NPCID.Retinazer);

                // If no eyes are alive, pretend that they're all alive and at 100% health.
                if (spazmatismIndex == -1 && retinazerIndex == -1)
                    return 1f;

                // If only one eye is active, only incorporate their life ratio.
                if (spazmatismIndex == -1 && retinazerIndex != -1)
                    return Main.npc[retinazerIndex].life / (float)Main.npc[retinazerIndex].lifeMax;
                if (spazmatismIndex != -1 && retinazerIndex == -1)
                    return Main.npc[spazmatismIndex].life / (float)Main.npc[spazmatismIndex].lifeMax;

                float spazmatismLifeRatio = Main.npc[spazmatismIndex].life / (float)Main.npc[spazmatismIndex].lifeMax;
                float retinzerLifeRatio = Main.npc[retinazerIndex].life / (float)Main.npc[retinazerIndex].lifeMax;

                return (spazmatismLifeRatio + retinzerLifeRatio) * 0.5f;
            }
        }

        #region Updating
        public static void DoUniversalUpdate()
        {
            if (Main.gamePaused)
                return;

            UniversalAttackTimer++;

            if (RetinazerIndex == -1 && SpazmatismIndex == -1)
            {
                CurrentAttackState = TwinsAttackState.ChargeRedirect;
                return;
            }

            if (_targetIndex == -1 || Target.dead || !Target.active)
            {
                Vector2 searchPosition;
                if (RetinazerIndex == -1)
                    searchPosition = Main.npc[SpazmatismIndex].Center;
                else if (SpazmatismIndex == -1)
                    searchPosition = Main.npc[RetinazerIndex].Center;
                else
                    searchPosition = Vector2.Lerp(Main.npc[SpazmatismIndex].Center, Main.npc[RetinazerIndex].Center, 0.5f);

                _targetIndex = Player.FindClosest(searchPosition, 1, 1);
            }
        }

        public static void PostUpdateEffects()
        {
            // If Retinazer and Spazmatism are already dead, reset their appropriate AI data.
            if (SpazmatismIndex == -1 && RetinazerIndex == -1)
            {
                UniversalStateIndex = 0;
                UniversalAttackTimer = 0;
            }

            // Adjust the background color intensity based on whether one of the eyes is enraged or not.
            BackgroundColorIntensity = Clamp(BackgroundColorIntensity + InFinalPhase.ToDirectionInt() * 0.012f, 0f, 1f);

            SpazmatismIndex = RetinazerIndex = -1;
        }
        #endregion

        #region AI

        public static int SmallLaserDamage => 135;

        public static int CursedCinderDamage => 135;

        public static int CursedFireballDamage => 135;

        public static int RedLightningDamage => 140;

        public static int CursedFlameBurstDamage => 140;

        public static int FlamethrowerDamage => 200;

        public static int BigDeathrayDamage => 250;

        public const int AfterimageDrawInterpolantIndex = 8;

        public const int RetinazerTelegraphDirectionIndex = 13;

        public const int RetinazerTelegraphOpacityIndex = 14;

        public const float Phase2TransitionTime = 200f;

        public const float Phase2LifeRatioThreshold = 0.75f;

        public const float Phase3LifeRatioThreshold = 0.425f;

        public static bool DoAI(NPC npc)
        {
            bool isSpazmatism = npc.type == NPCID.Spazmatism;
            bool isRetinazer = npc.type == NPCID.Retinazer;
            npc.target = _targetIndex;

            if (isSpazmatism)
                SpazmatismIndex = npc.whoAmI;
            if (isRetinazer)
                RetinazerIndex = npc.whoAmI;

            if (npc.whoAmI != SpazmatismIndex && npc.whoAmI != RetinazerIndex)
            {
                npc.active = false;
                return false;
            }

            bool shouldDespawn = Main.dayTime || _targetIndex == -1 || !Target.active || Target.dead;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool inPhase2 = PersonallyInPhase2(npc);
            bool otherMechIsInPhase2 = true;
            if (isSpazmatism && NPC.AnyNPCs(NPCID.Retinazer))
                otherMechIsInPhase2 = PersonallyInPhase2(Main.npc[NPC.FindFirstNPC(NPCID.Retinazer)]);
            if (isRetinazer && NPC.AnyNPCs(NPCID.Spazmatism))
                otherMechIsInPhase2 = PersonallyInPhase2(Main.npc[NPC.FindFirstNPC(NPCID.Spazmatism)]);

            ref float phase2Timer = ref npc.Infernum().ExtraAI[0];
            ref float phase2TransitionSpin = ref npc.Infernum().ExtraAI[1];
            ref float healCountdown = ref npc.Infernum().ExtraAI[2];
            ref float hasStartedHealFlag = ref npc.Infernum().ExtraAI[3];
            ref float overdriveTimer = ref npc.Infernum().ExtraAI[4];
            ref float chargingFlag = ref npc.Infernum().ExtraAI[5];
            ref float chargeFlameTimer = ref npc.Infernum().ExtraAI[6];
            ref float chargeFlameRotation = ref npc.Infernum().ExtraAI[7];
            ref float afterimageInterpolant = ref npc.Infernum().ExtraAI[AfterimageDrawInterpolantIndex];

            chargeFlameRotation = chargeFlameRotation.AngleLerp(npc.rotation, 0.05f).AngleTowards(npc.rotation, 0.1f);

            // Entering phase 2 effect.
            if (lifeRatio < Phase2LifeRatioThreshold && phase2Timer < Phase2TransitionTime)
            {
                phase2Timer++;
                phase2TransitionSpin = Utils.GetLerpValue(0f, Phase2TransitionTime / 2f, phase2Timer, true);
                phase2TransitionSpin *= Utils.GetLerpValue(Phase2TransitionTime, Phase2TransitionTime / 2f, phase2Timer, true) * 0.5f;

                CurrentAttackState = TwinsAttackState.ChargeRedirect;
                UniversalAttackTimer = 0;

                npc.rotation += phase2TransitionSpin;
                npc.velocity *= 0.97f;

                // Go to phase 2 and explode into metal, blood, and gore.
                if (Main.netMode != NetmodeID.Server && phase2Timer == (int)(Phase2TransitionTime / 2))
                {
                    SoundEngine.PlaySound(SoundID.NPCHit1, npc.Center);

                    for (int i = 0; i < 2; i++)
                    {
                        Gore.NewGore(npc.GetSource_FromAI(), npc.position, Main.rand.NextVector2Circular(6f, 6f), isRetinazer ? 143 : 144, 1f);
                        Gore.NewGore(npc.GetSource_FromAI(), npc.position, Main.rand.NextVector2Circular(6f, 6f), 7, 1f);
                        Gore.NewGore(npc.GetSource_FromAI(), npc.position, Main.rand.NextVector2Circular(6f, 6f), 6, 1f);
                    }

                    for (int i = 0; i < 20; i++)
                        Dust.NewDust(npc.position, npc.width, npc.height, DustID.Blood, Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-6f, 6f), 0, default, 1f);

                    SoundEngine.PlaySound(Apollo.MissileLaunchSound, Target.Center);
                }

                chargeFlameTimer = 0f;
                return false;
            }

            if (inPhase2)
                npc.HitSound = SoundID.NPCHit4;

            // Despawn effect.
            if (shouldDespawn)
            {
                npc.velocity.Y -= 0.4f;
                npc.rotation = npc.velocity.ToRotation() - PiOver2;
                if (npc.velocity.Y <= -27f)
                {
                    if (NPC.AnyNPCs(NPCID.Spazmatism))
                        SilenceSpazmatismFlameSounds(Main.npc[NPC.FindFirstNPC(NPCID.Spazmatism)]);
                    npc.active = false;
                }

                return false;
            }

            // Reset the boss' stuff every frame. It can be changed later as necessary.
            chargingFlag = 0f;
            npc.damage = npc.defDamage;
            npc.dontTakeDamage = !otherMechIsInPhase2 && inPhase2;
            npc.noTileCollide = true;

            bool alone = !NPC.AnyNPCs(NPCID.Spazmatism) && isRetinazer || !NPC.AnyNPCs(NPCID.Retinazer) && isSpazmatism;
            bool otherTwinHasCreatedShield = false;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (!Main.npc[i].active)
                    continue;
                if (Main.npc[i].type is not NPCID.Retinazer and not NPCID.Spazmatism)
                    continue;
                if (Main.npc[i].type == npc.type)
                    continue;

                if (Main.npc[i].Infernum().ExtraAI[3] == 1f)
                {
                    otherTwinHasCreatedShield = true;
                    break;
                }
            }

            if (lifeRatio < 0.05f && hasStartedHealFlag == 0f && !otherTwinHasCreatedShield)
            {
                healCountdown = TwinsShield.HealTime;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<TwinsShield>(), 0, 0f, -1, npc.whoAmI);

                string localizationKey = "Mods.InfernumMode.Status.TwinsForcefieldSummonSpazmatism";
                Color textColor = Color.LimeGreen;
                if (npc.type == NPCID.Retinazer)
                {
                    localizationKey = "Mods.InfernumMode.Status.TwinsForcefieldSummonRetinazer";
                    textColor = Color.IndianRed;
                }

                LumUtils.BroadcastLocalizedText(localizationKey, textColor);
                hasStartedHealFlag = 1f;
            }

            if (healCountdown > 0f)
            {
                if (alone)
                {
                    npc.life = (int)Lerp(npc.lifeMax * 0.05f, npc.lifeMax * 0.3f, 1f - healCountdown / TwinsShield.HealTime);
                    if (healCountdown == TwinsShield.HealTime - 5)
                    {
                        string localizationKey = "Mods.InfernumMode.Status.TwinsFinalPhaseSpazmatism";
                        Color textColor = Color.LimeGreen;
                        if (npc.type == NPCID.Retinazer)
                        {
                            localizationKey = "Mods.InfernumMode.Status.TwinsFinalPhaseRetinazer";
                            textColor = Color.IndianRed;
                        }
                        LumUtils.BroadcastLocalizedText(localizationKey, textColor);

                        HatGirl.SayThingWhileOwnerIsAlive(Target, "Mods.InfernumMode.PetDialog.TwinsFinalPhaseTip");
                    }
                    healCountdown--;

                    if (healCountdown == TwinsShield.HealTime - 1f)
                    {
                        npc.Center = Target.Center - Vector2.UnitY * 700f;
                        npc.velocity = Vector2.UnitY * 6f;
                        npc.netUpdate = true;
                    }
                }
                else
                    npc.life = (int)(npc.lifeMax * 0.05f);

                npc.dontTakeDamage = true;
            }

            if (hasStartedHealFlag == 1f && healCountdown > 0f)
            {
                npc.Opacity = Lerp(npc.Opacity, 1f, 0.15f);
                npc.dontTakeDamage = true;
            }

            if (alone && CurrentAttackState != TwinsAttackState.DeathAnimation)
            {
                if (hasStartedHealFlag == 1f && overdriveTimer < 30f)
                {
                    npc.velocity *= 0.97f;
                    npc.rotation = npc.AngleTo(Target.Center) - PiOver2;

                    if (healCountdown <= 0f)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient && overdriveTimer == 15f)
                        {
                            int explosion = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<TwinsEnergyExplosion>(), 0, 0f);
                            Main.projectile[explosion].ai[0] = npc.type;
                        }

                        overdriveTimer++;
                    }
                    return false;
                }

                if (npc.type == NPCID.Spazmatism)
                    DoBehavior_SpazmatismAlone(npc, ref chargingFlag);
                else
                    DoBehavior_RetinazerAlone(npc);

                // Progress with the charge animation.
                chargeFlameTimer = Clamp(chargeFlameTimer + (chargingFlag == 1f ? 1f : -3f), 0f, 15f);
                return false;
            }

            switch (CurrentAttackState)
            {
                case TwinsAttackState.ChargeRedirect:
                    DoBehavior_ChargeRedirect(npc, isSpazmatism);
                    break;
                case TwinsAttackState.DownwardCharge:
                    DoBehavior_DownwardCharge(npc);
                    break;
                case TwinsAttackState.SwitchCharges:
                    DoBehavior_SwitchCharges(npc, isSpazmatism, isRetinazer, ref chargingFlag);
                    break;
                case TwinsAttackState.Spin:
                    DoBehavior_Spin(npc, isSpazmatism, ref chargingFlag);
                    break;
                case TwinsAttackState.FlamethrowerBurst:
                    DoBehavior_FlamethrowerBurst(npc, isSpazmatism, isRetinazer, ref afterimageInterpolant);
                    break;
                case TwinsAttackState.ChaoticFireAndDownwardLaser:
                    if (!DoBehavior_ChaoticFireAndDownwardLaser(npc, isSpazmatism))
                        return false;
                    break;
                case TwinsAttackState.LazilyObserve:
                    Vector2 destination = Target.Center + new Vector2(isRetinazer ? -540f : 540f, -500f);

                    Vector2 hoverVelocity = npc.SafeDirectionTo(destination - npc.velocity) * 9f;
                    npc.SimpleFlyMovement(hoverVelocity, 0.16f);
                    npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(Target.Center) - PiOver2, TwoPi / 10f);

                    if (InPhase2 || UniversalAttackTimer >= 150f)
                        SelectNextAttack();
                    break;
                case TwinsAttackState.DeathAnimation:
                    DoBehavior_DeathAnimation(npc);
                    break;
            }

            // Progress with the charge animation.
            chargeFlameTimer = Clamp(chargeFlameTimer + (chargingFlag == 1f ? 1f : -3f), 0f, 15f);

            return false;
        }

        #region Specific Attacks
        public static void DoBehavior_ChargeRedirect(NPC npc, bool isSpazmatism)
        {
            int attackDuration = (int)Lerp(155f, 105f, 1f - CombinedLifeRatio);
            Vector2 hoverDestination = Target.Center + new Vector2(isSpazmatism.ToDirectionInt() * 620f, -235f);

            // Disable contact damage.
            npc.damage = 0;

            // Fly towards the destination.
            npc.Center = Vector2.Lerp(npc.Center, hoverDestination, Utils.GetLerpValue(npc.Distance(hoverDestination), 96f, 360f) * 0.0425f);
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 24f, 0.8f);
            npc.rotation = npc.AngleTo(Target.Center) - PiOver2;

            // Relase some projectiles while hovering to pass the time.
            if (UniversalAttackTimer % 30f == 29f && UniversalAttackTimer >= 60f)
            {
                if (!isSpazmatism)
                    SoundEngine.PlaySound(CommonCalamitySounds.ExoLaserShootSound, npc.Center);

                if (isSpazmatism)
                {
                    float shootSpeed = Lerp(9f, 15.7f, 1f - CombinedLifeRatio);
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 shootVelocity = npc.SafeDirectionTo(Target.Center).RotatedBy(Lerp(-0.45f, 0.45f, i / 3f)) * shootSpeed;
                        Utilities.NewProjectileBetter(npc.Center + shootVelocity * 4f, shootVelocity, ProjectileID.CursedFlameHostile, CursedFireballDamage, 0f);
                    }
                }
                else if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float shootSpeed = Lerp(10.75f, 16f, 1f - CombinedLifeRatio);
                    Vector2 shootVelocity = npc.SafeDirectionTo(Target.Center) * shootSpeed;
                    Utilities.NewProjectileBetter(npc.Center + shootVelocity * 4f, shootVelocity, ModContent.ProjectileType<RetinazerLaser>(), SmallLaserDamage, 0f);
                }
                npc.netUpdate = true;
            }

            if (UniversalAttackTimer >= attackDuration)
                SelectNextAttack();
        }

        public static void DoBehavior_DownwardCharge(NPC npc)
        {
            int chargeDelay = 30;
            int attackDuration = (int)Lerp(105f, 75f, 1f - CombinedLifeRatio);
            float reelbackSpeed = 7f;
            float chargeSpeed = Lerp(19.5f, 24f, 1f - CombinedLifeRatio);
            if (BossRushEvent.BossRushActive)
                chargeSpeed *= 1.8f;

            if (UniversalAttackTimer < chargeDelay - 1f)
            {
                if (UniversalAttackTimer == 1f)
                    SoundEngine.PlaySound(Artemis.ChargeTelegraphSound, npc.Center);

                npc.velocity = npc.SafeDirectionTo(Target.Center) * -reelbackSpeed;
                npc.rotation = npc.AngleTo(Target.Center) - PiOver2;
            }
            if (UniversalAttackTimer == chargeDelay)
            {
                npc.velocity = npc.SafeDirectionTo(Target.Center) * chargeSpeed;
                npc.netUpdate = true;

                SoundEngine.PlaySound(Artemis.ChargeSound, npc.Center);
            }

            // Accelerate after charging.
            if (UniversalAttackTimer > chargeDelay)
            {
                npc.velocity *= 1.007f;
                npc.rotation = npc.velocity.ToRotation() - PiOver2;
            }

            if (UniversalAttackTimer >= attackDuration)
                SelectNextAttack();
        }

        public static void DoBehavior_SwitchCharges(NPC npc, bool isSpazmatism, bool isRetinazer, ref float chargingFlag)
        {
            int redirectTime = 48;
            int chargeTime = 55;
            int attackDuration = 360;
            bool willCharge = isSpazmatism;

            int chargeSpecificWrappedAttackTimer = UniversalAttackTimer % (redirectTime + chargeTime);
            int cycleBasedWrappedAttackTimer = UniversalAttackTimer % ((redirectTime + chargeTime) * 2);
            float xOffsetDirection = cycleBasedWrappedAttackTimer > redirectTime + chargeTime ? -1f : 1f;

            Vector2 hoverDestination = Target.Center;

            // Decide who will charge.
            if (cycleBasedWrappedAttackTimer > redirectTime + chargeTime)
                willCharge = isRetinazer;

            hoverDestination += willCharge ? Vector2.UnitX * 480f * xOffsetDirection : Vector2.UnitY * -780f;

            // Redirect.
            if (chargeSpecificWrappedAttackTimer <= redirectTime)
            {
                // Hover to the ideal position and effectively lock in place when really close.
                if (npc.WithinRange(hoverDestination, 120f))
                {
                    Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * MathF.Min(32f, npc.Distance(hoverDestination));
                    npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.16f);
                    npc.velocity = npc.velocity.MoveTowards(idealVelocity, 3.2f);
                }
                else
                {
                    npc.Center = Vector2.Lerp(npc.Center, hoverDestination, 0.06f);
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 36f, 3.7f);
                    npc.Center = npc.Center.MoveTowards(hoverDestination, 10f);
                }

                npc.rotation = npc.AngleTo(Target.Center) - PiOver2;

                // Disable contact damage while redirecting.
                npc.damage = 0;

                // Charge.
                if (chargeSpecificWrappedAttackTimer == redirectTime && willCharge)
                {
                    npc.damage = npc.defDamage;

                    float chargeSpeed = Lerp(23f, 28f, 1f - CombinedLifeRatio);
                    if (BossRushEvent.BossRushActive)
                        chargeSpeed *= 1.8f;
                    npc.velocity = npc.SafeDirectionTo(Target.Center + Target.velocity * 6.6f) * chargeSpeed;
                    npc.rotation = npc.AngleTo(Target.Center) - PiOver2;
                    npc.netUpdate = true;

                    SoundEngine.PlaySound(Artemis.ChargeSound, npc.Center);
                }
            }
            else if (willCharge)
                chargingFlag = 1f;

            if (!willCharge)
                npc.damage = 0;

            if (UniversalAttackTimer >= attackDuration)
                SelectNextAttack();
        }

        public static void DoBehavior_Spin(NPC npc, bool isSpazmatism, ref float chargingFlag)
        {
            int redirectTime = 60;
            int spinTime = 240;
            int spinSlowdownTime = 35;
            int reelbackTime = 30;
            int chargeTime = 55;
            int attackDuration = 400;
            float spinSlowdownInterpolant = Utils.GetLerpValue(redirectTime + spinTime, redirectTime + spinTime - spinSlowdownTime, UniversalAttackTimer, true);
            float spinAngularVelocity = Lerp(ToRadians(1.84f), ToRadians(4.44f), 1f - CombinedLifeRatio);
            ref float spinRotation = ref npc.ai[0];
            ref float spinDirection = ref npc.ai[1];

            // Don't deal contact damage until charging.
            if (UniversalAttackTimer < redirectTime + spinTime)
                npc.damage = 0;

            // Initialize the spin rotation for both eyes.
            NPC otherEye = GetOtherTwin(npc);
            if (UniversalAttackTimer == 1)
            {
                spinRotation = Main.rand.NextFloat(TwoPi);

                if (otherEye != null)
                {
                    otherEye.ai[0] = spinRotation + Pi;
                    otherEye.netUpdate = true;
                }
                spinDirection = 1.1f;
                npc.netUpdate = true;
            }

            if (!isSpazmatism)
                spinDirection = otherEye.ai[1];

            // Update the spin direction for both eyes.
            if (spinDirection >= 1.1f && isSpazmatism)
            {
                spinDirection = (Target.Center.X < npc.Center.X).ToDirectionInt();
                if (otherEye != null)
                    otherEye.ai[1] = spinDirection;
            }

            Vector2 destination = Target.Center + spinRotation.ToRotationVector2() * 610f;
            if (UniversalAttackTimer >= redirectTime && UniversalAttackTimer <= redirectTime + spinTime)
            {
                // Increment the spin rotation.
                spinRotation += spinAngularVelocity * spinDirection * spinSlowdownInterpolant;

                // Relase some projectiles while spinning to pass the time.
                int fireRate = BossRushEvent.BossRushActive ? 18 : 30;

                if (UniversalAttackTimer % fireRate == fireRate - 1f)
                {
                    SoundEngine.PlaySound(CommonCalamitySounds.ExoLaserShootSound, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float shootSpeed = Lerp(9f, 11.5f, 1f - CombinedLifeRatio);
                        Vector2 shootVelocity = npc.SafeDirectionTo(Target.Center) * shootSpeed;
                        int projectileType = isSpazmatism ? ProjectileID.CursedFlameHostile : ModContent.ProjectileType<RetinazerLaser>();
                        int proj = Utilities.NewProjectileBetter(npc.Center + shootVelocity * 4f, shootVelocity, projectileType, SmallLaserDamage, 0f);
                        Main.projectile[proj].tileCollide = false;
                    }
                }
            }

            // Adjust position for the spin.
            if (UniversalAttackTimer < redirectTime + spinTime)
            {
                npc.Center = Vector2.Lerp(npc.Center, destination, 0.033f);
                if (UniversalAttackTimer >= redirectTime)
                    npc.Center = Vector2.Lerp(npc.Center, destination, 0.08f);

                npc.velocity = Vector2.Zero;
                npc.rotation = npc.AngleTo(Target.Center) - PiOver2;
            }

            // Reel back.
            if (UniversalAttackTimer >= redirectTime + spinTime && UniversalAttackTimer < redirectTime + spinTime + reelbackTime)
            {
                npc.velocity = npc.SafeDirectionTo(Target.Center) * -6f;
                npc.rotation = npc.AngleTo(Target.Center) - PiOver2;
                npc.netUpdate = true;
            }

            // And charge.
            if (UniversalAttackTimer == redirectTime + spinTime + reelbackTime)
            {
                float chargeSpeed = Lerp(21f, 26.5f, 1f - CombinedLifeRatio);
                if (BossRushEvent.BossRushActive)
                    chargeSpeed *= 1.8f;

                npc.velocity = npc.SafeDirectionTo(Target.Center + Target.velocity * 6f) * chargeSpeed;
                npc.rotation = npc.AngleTo(Target.Center) - PiOver2;
                npc.netUpdate = true;

                SoundEngine.PlaySound(Artemis.ChargeSound, npc.Center);
            }

            if (UniversalAttackTimer >= redirectTime + spinTime + reelbackTime && UniversalAttackTimer < redirectTime + spinTime + reelbackTime + chargeTime)
                chargingFlag = 1f;

            if (UniversalAttackTimer >= attackDuration)
                SelectNextAttack();
        }

        public static void DoBehavior_FlamethrowerBurst(NPC npc, bool isSpazmatism, bool isRetinazer, ref float afterimageInterpolant)
        {
            int redirectTime = 90;
            int attackTelegraphTime = 36;
            int chargeTime = 104;
            int totalCharges = 3;
            int totalLaserBurstsUntilExhaustion = 5;
            int baseExhaustCountdown = 90;
            int telegraphTime = (int)Lerp(42f, 28f, 1f - CombinedLifeRatio);
            bool stopAttackingYouStupidRobots = UniversalAttackTimer >= redirectTime + attackTelegraphTime + totalCharges * chargeTime - 75;
            float laserShootSpeed = Lerp(8.5f, 11f, 1f - CombinedLifeRatio);
            float chargeSpeed = Lerp(15f, 18.75f, 1f - CombinedLifeRatio);
            float chargeAngularVelocity = Lerp(0.006f, 0.0093f, 1f - CombinedLifeRatio);

            // Only applies to Spazmatism.
            ref float chargeTimer = ref npc.ai[0];

            // Only applies to Retinazer.
            ref float telegraphShootTimer = ref npc.ai[0];
            ref float telegraphShootCounter = ref npc.ai[1];
            ref float laserShootExhaustionCountdown = ref npc.ai[2];
            ref float telegraphDirection = ref npc.Infernum().ExtraAI[RetinazerTelegraphDirectionIndex];
            ref float telegraphOpacity = ref npc.Infernum().ExtraAI[RetinazerTelegraphOpacityIndex];

            // Move into position to the top left/right of the target.
            if (UniversalAttackTimer < redirectTime)
            {
                Vector2 hoverDestination = Target.Center + new Vector2(isSpazmatism.ToDirectionInt() * 600f, -200f);
                npc.Center = Vector2.Lerp(npc.Center, hoverDestination, 0.02f);
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.Zero.MoveTowards(hoverDestination - npc.Center, 20f), 0.15f);
                npc.rotation = npc.AngleTo(Target.Center) - PiOver2;
                afterimageInterpolant = 0f;

                // Disable damage.
                npc.damage = 0;

                return;
            }

            // Have both twins perform telegraphs.
            if (UniversalAttackTimer < redirectTime + attackTelegraphTime)
            {
                // Retinazer simply slows down and continues look at the player, charging energy at its laser cannon.
                if (isRetinazer)
                {
                    npc.velocity *= 0.96f;
                    npc.rotation = npc.AngleTo(Target.Center) - PiOver2;
                }

                // Spazmatism reels back and prepares its flamethrower in anticipation of the charge.
                if (isSpazmatism)
                {
                    afterimageInterpolant = Clamp(afterimageInterpolant + 0.1f, 0f, 1f);

                    if (UniversalAttackTimer == redirectTime + 1f)
                        SoundEngine.PlaySound(Artemis.ChargeTelegraphSound, Target.Center);

                    // Reel back.
                    float reelBackSpeed = Lerp(3f, 20f, Utils.GetLerpValue(16f, attackTelegraphTime - 10f, UniversalAttackTimer - redirectTime, true));
                    npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(Target.Center) * -reelBackSpeed, 0.2f);
                    npc.rotation = npc.AngleTo(Target.Center) - PiOver2;
                }
                chargeTimer = 0f;

                // Disable damage.
                npc.damage = 0;

                return;
            }

            // Spazmatism performs arcing flamethrower sweeps while Retinazer hovers near the target and releases laser bursts.
            if (isSpazmatism)
            {
                // Do the charge.
                if (chargeTimer <= 1f)
                {
                    SoundEngine.PlaySound(Artemis.ChargeSound, Target.Center);
                    npc.velocity = npc.SafeDirectionTo(Target.Center) * chargeSpeed;
                    npc.netUpdate = true;
                }

                if (chargeTimer >= chargeTime - 36f)
                {
                    npc.velocity *= 0.93f;
                    npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(Target.Center) - PiOver2, 0.18f);
                }

                // Arc towards the target after the charge has happened.
                else
                {
                    npc.rotation = npc.velocity.ToRotation() - PiOver2;
                    npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(Target.Center), chargeAngularVelocity);
                }

                if (chargeTimer >= chargeTime)
                {
                    chargeTimer = 0f;
                    npc.velocity = npc.SafeDirectionTo(Target.Center) * 4f;
                    npc.netUpdate = true;
                }

                // Emit flamethrower particles.
                if (Main.netMode != NetmodeID.MultiplayerClient && chargeTimer >= 5f && npc.velocity.Length() >= 15f)
                {
                    Vector2 flamethrowerSpawnPosition = npc.Center + npc.velocity * 12f;
                    Utilities.NewProjectileBetter(flamethrowerSpawnPosition, npc.velocity.SafeNormalize(Vector2.UnitY) * 2.4f + npc.velocity / 11f, ModContent.ProjectileType<SpazmatismFlamethrower>(), FlamethrowerDamage, 0f, -1);
                }
                chargeTimer++;
            }

            if (isRetinazer)
            {
                // Disable damage.
                npc.damage = 0;

                // Decide telegraph variables.
                if (laserShootExhaustionCountdown >= 1f)
                {
                    // Charge above the target.
                    if (laserShootExhaustionCountdown >= baseExhaustCountdown - 16f)
                        npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(Target.Center - Vector2.UnitY * 350f) * chargeSpeed * 1.2f, 0.2f);

                    laserShootExhaustionCountdown--;
                    afterimageInterpolant = 1f;
                    npc.rotation = npc.velocity.ToRotation() - PiOver2;
                }
                else
                {
                    // Attempt to look at the target.
                    npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(Target.Center) - PiOver2, 0.12f);
                    telegraphShootTimer++;
                    afterimageInterpolant = Clamp(afterimageInterpolant - 0.05f, 0f, 1f);
                }
                telegraphDirection = npc.rotation + PiOver2;
                telegraphOpacity = Utils.GetLerpValue(0f, telegraphTime * 0.75f, telegraphShootTimer, true);

                if (stopAttackingYouStupidRobots)
                {
                    telegraphShootTimer = 0f;
                    telegraphOpacity = 0f;
                }

                // Shoot a burst of lasers once the telegraph should disappear.
                if (telegraphShootTimer >= telegraphTime)
                {
                    SoundEngine.PlaySound(Artemis.LaserShotgunSound, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 aimDirection = telegraphDirection.ToRotationVector2();
                        for (int i = 0; i < 5; i++)
                        {
                            float shootOffsetAngle = Lerp(-0.95f, 0.95f, i / 4f);
                            Vector2 laserShootVelocity = aimDirection.RotatedBy(shootOffsetAngle) * laserShootSpeed;

                            ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(laser =>
                            {
                                laser.tileCollide = false;
                            });
                            Utilities.NewProjectileBetter(npc.Center + aimDirection * 88f, laserShootVelocity * 0.775f, ModContent.ProjectileType<RetinazerLaser>(), 145, 0f);
                        }

                        npc.velocity -= aimDirection * 6f;
                        telegraphShootTimer = 0f;
                        telegraphOpacity = 0f;
                        telegraphShootCounter++;
                        if (telegraphShootCounter >= totalLaserBurstsUntilExhaustion)
                        {
                            telegraphShootCounter = 0f;
                            laserShootExhaustionCountdown = baseExhaustCountdown;
                        }

                        npc.netUpdate = true;
                    }
                }

                // Drift towards the target.
                if (npc.velocity.Length() > 9f && laserShootExhaustionCountdown <= 0f)
                    npc.velocity *= 0.94f;
                if (!npc.WithinRange(Target.Center, 400f))
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(Target.Center) * 5f, 0.16f);
                else if (!Target.HoldingTrueMeleeWeapon())
                    npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(Target.Center) * -9f, 0.07f);
            }

            if (UniversalAttackTimer >= redirectTime + attackTelegraphTime + totalCharges * chargeTime)
            {
                SelectNextAttack();
                telegraphOpacity = 0f;
            }
        }

        public static bool DoBehavior_ChaoticFireAndDownwardLaser(NPC npc, bool isSpazmatism)
        {
            int shootDelay = 60;
            int attackDuration = 540;
            int attackCycleTime = 120;
            int slowdownTime = 15;
            int laserChargeUpTime = 44;
            int fireballReleaseRate = 2;
            int spazmatismIndex = NPC.FindFirstNPC(NPCID.Spazmatism);
            bool readyToAttack = UniversalAttackTimer >= shootDelay;
            float spinRadius = 100f;
            float maxSpinAngularVelocity = ToRadians(5.4f);
            float fireballShootSpeed = 9f;
            ref float centerOfMassX = ref Main.npc[spazmatismIndex].ai[0];
            ref float centerOfMassY = ref Main.npc[spazmatismIndex].ai[1];
            ref float spinRotation = ref Main.npc[spazmatismIndex].ai[2];
            ref float attackCountdown = ref Main.npc[spazmatismIndex].ai[3];
            bool isAttacking = attackCountdown >= 1f;

            // Disable contact universally. It is not relevant for this attack.
            npc.damage = 0;

            // Easy temporary variable to allow vector math to be done more efficiently. The X and Y values inherit whatever this becomes at the end of the update frame.
            Vector2 nextCenterOfMass = new(centerOfMassX, centerOfMassY);
            if (nextCenterOfMass == Vector2.Zero || UniversalAttackTimer <= 1f)
            {
                nextCenterOfMass = npc.Center;
                attackCountdown = 0f;
            }

            // Spazmatism stores the relevant update information. To prevent two updates per frame, only it will perform those updates.
            // This may lead to a one-frame buffer for the information Retinazer has, but that shouldn't matter in practice.
            Vector2 hoverDestination = Target.Center + new Vector2((npc.Center.X > Target.Center.X).ToDirectionInt() * 450f, -270f);
            if (readyToAttack)
                hoverDestination = Target.Center - Vector2.UnitY * 350f;

            if (isSpazmatism)
            {
                float slowdownFactor = Utils.GetLerpValue(attackCycleTime - slowdownTime, attackCycleTime - 1f, attackCountdown, true);
                if (isAttacking)
                    attackCountdown--;
                else
                    slowdownFactor = 1f;

                // Update the spin.
                spinRotation += Utils.GetLerpValue(0f, shootDelay * 0.5f, UniversalAttackTimer, true) * slowdownFactor * maxSpinAngularVelocity * (Target.Center.X > npc.Center.X).ToDirectionInt();

                // Update the center of mass. It starts at the top left/right of the target before transitioning to above them.
                nextCenterOfMass = Vector2.Lerp(nextCenterOfMass, hoverDestination, slowdownFactor * 0.04f).MoveTowards(hoverDestination, slowdownFactor * 10f);
            }

            // Spin in place around the center of mass.
            float localSpinRotation = spinRotation;
            if (!isSpazmatism)
                localSpinRotation += Pi;
            Vector2 spinDestination = nextCenterOfMass + localSpinRotation.ToRotationVector2() * spinRadius;
            npc.Center = Vector2.Lerp(npc.Center, spinDestination, 0.04f);
            npc.velocity = Vector2.Zero.MoveTowards(spinDestination - npc.Center, 16f);

            // Prepare to attack if not already attacking and sufficiently close to the hover destination.
            // This specifically waits until Spazmatism is pointing up (meaning Retinazer is pointing down) before attacking as well, to ensure that
            // the laser points downward as well.

            // For context of where the remainingAngleFromSlowdown value comes from, it represents the amount of angular motion that will inevitably elapse
            // when slowing down based on slowdownFactor above. The exact value can be determined by summation, like this:
            // maxSpinAngularVelocity * (slowdownTime - 1) / slowdownTime +
            // maxSpinAngularVelocity * (slowdownTime - 2) / slowdownTime +
            // maxSpinAngularVelocity * (slowdownTime - 3) / slowdownTime +
            // ...
            // Luckily, this summation does not need to be calculated manually and can be easily expressed as simply (slowdownTime - 1) / 2. Factoring in
            // the maxSpinAngularVelocity again gives us the final answer.
            float remainingAngleFromSlowdown = (slowdownTime - 1f) * maxSpinAngularVelocity * 0.5f;
            bool pointingUpwards = spinRotation.ToRotationVector2().AngleBetween(-Vector2.UnitY) < remainingAngleFromSlowdown;

            if (isSpazmatism && readyToAttack && nextCenterOfMass.WithinRange(hoverDestination, 75f) && !isAttacking && pointingUpwards)
            {
                attackCountdown = attackCycleTime;
                npc.netUpdate = true;
            }

            // Slow down if attacking.
            if (isAttacking)
                npc.velocity *= 0.9f;

            // Otherwise look at the target.
            else
                npc.rotation = npc.AngleTo(Target.Center) - PiOver2;

            // Spazmatism circles in place and releases accelerating fireballs outward when attacking.
            if (isSpazmatism && isAttacking)
            {
                npc.rotation += (Target.Center.X > npc.Center.X).ToDirectionInt() * maxSpinAngularVelocity;
                if (UniversalAttackTimer % fireballReleaseRate == 0f)
                {
                    if (UniversalAttackTimer % (fireballReleaseRate * 3f) == 0f)
                        SoundEngine.PlaySound(CommonCalamitySounds.ExoPlasmaShootSound, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 shootVelocity = (npc.rotation + PiOver2 + Main.rand.NextFloatDirection() * 0.026f).ToRotationVector2() * fireballShootSpeed;
                        Utilities.NewProjectileBetter(npc.Center + shootVelocity * 4f, shootVelocity, ModContent.ProjectileType<CursedFireballBomb>(), CursedFireballDamage, 0f);
                    }
                }
            }

            // Retinazer releases a laserbeam downward that explodes with tiles.
            if (!isSpazmatism && isAttacking)
            {
                npc.rotation = npc.rotation.AngleTowards(0f, 0.01f);

                // Charge up energy at first.
                if (attackCountdown >= attackCycleTime - laserChargeUpTime)
                {
                    float chargeUpInterpolant = Utils.GetLerpValue(attackCycleTime, attackCycleTime - laserChargeUpTime, attackCountdown, true);
                    Vector2 endOfLaserCannon = npc.Center + (npc.rotation + PiOver2).ToRotationVector2() * 80f;
                    for (int i = 0; i < 2; i++)
                    {
                        if (Main.rand.NextFloat() > chargeUpInterpolant)
                            continue;

                        Color laserEnergyColor = Color.Lerp(Color.Red, Color.Yellow, Main.rand.NextFloat(0.64f));
                        Vector2 laserEnergySpawnPosition = endOfLaserCannon + Main.rand.NextVector2Unit() * Main.rand.NextFloat(80f, 132f);
                        Vector2 laserEnergyVelocity = (endOfLaserCannon - laserEnergySpawnPosition) * 0.04f;
                        SquishyLightParticle laserEnergy = new(laserEnergySpawnPosition, laserEnergyVelocity, 1.5f, laserEnergyColor, 36, 1f, 4f);
                        GeneralParticleHandler.SpawnParticle(laserEnergy);
                    }
                }

                // Move upward when firing the laser as an indicator that it's reactive force is moving Retinazer backwards.
                else
                    npc.velocity = -Vector2.UnitY * 10f;

                // Cast the laserbeam.
                if (attackCountdown == attackCycleTime - laserChargeUpTime)
                {
                    SoundEngine.PlaySound(AresLaserCannon.LaserbeamShootSound, Target.Center);
                    Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 9f;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        foreach (Projectile laser in Utilities.AllProjectilesByID(ModContent.ProjectileType<RetinazerLaser>()).Where(p => p.ai[1] == 1f))
                            laser.Kill();

                        Utilities.NewProjectileBetter(npc.Center, Vector2.UnitY, ModContent.ProjectileType<RetinazerGroundDeathray>(), BigDeathrayDamage, 0f, -1, 0f, npc.whoAmI);
                    }
                }
            }

            // Copy the new center of mass from the temporary vector.
            centerOfMassX = nextCenterOfMass.X;
            centerOfMassY = nextCenterOfMass.Y;

            if (UniversalAttackTimer >= attackDuration && !isAttacking)
            {
                Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<RetinazerLaser>());
                SelectNextAttack();
            }

            return true;
        }

        public static void DoBehavior_DeathAnimation(NPC npc)
        {
            int slowdownTime = 60;
            int jitterTime = 120;
            int lensFlareTime = TwinsLensFlare.Lifetime;
            bool otherTwinExists = NPC.AnyNPCs(NPCID.Spazmatism) && NPC.AnyNPCs(NPCID.Retinazer);

            // Both twins should temporarily close their HP bars.
            npc.Calamity().ShouldCloseHPBar = true;

            // The mech that is still alive fucks off during this attack, flying into the sky and temporarily disappearing.
            if (npc.Infernum().ExtraAI[3] == 1f && otherTwinExists)
            {
                npc.damage = 0;
                npc.dontTakeDamage = true;
                npc.Opacity = Utils.GetLerpValue(56f, 0f, UniversalAttackTimer, true);

                // Rise into the air if visible. Otherwise, hover above the target.
                if (npc.Opacity > 0f)
                    npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * -23f, 0.035f);
                else
                    npc.Center = Target.Center - Vector2.UnitY * 2500f;

                if (UniversalAttackTimer == slowdownTime + jitterTime + lensFlareTime)
                {
                    npc.Center = Target.Center - Vector2.UnitY * 600f;
                    npc.velocity = Vector2.Zero;
                    npc.netUpdate = true;
                }

                return;
            }

            // Play a malfunction sound on the first frame.
            if (UniversalAttackTimer == 1f)
                SoundEngine.PlaySound(AresBody.EnragedSound with { Volume = 1.5f });

            // Slow down and look at the target at first.
            if (UniversalAttackTimer < slowdownTime)
            {
                npc.velocity *= 0.9f;
                npc.rotation = npc.AngleTo(Target.Center) - PiOver2;
            }
            else
                npc.velocity = Vector2.Zero;

            // Disable damage.
            npc.damage = 0;
            npc.dontTakeDamage = true;

            // Create explosion effects on top of Spazmatism and jitter.
            if (UniversalAttackTimer >= slowdownTime)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient && UniversalAttackTimer % 4f == 3f)
                {
                    int explosion = Utilities.NewProjectileBetter(npc.Center + Main.rand.NextVector2Square(-80f, 80f), Vector2.Zero, ModContent.ProjectileType<TwinsSpriteExplosion>(), 0, 0f);
                    if (Main.projectile.IndexInRange(explosion))
                        Main.projectile[explosion].ModProjectile<TwinsSpriteExplosion>().SpazmatismVariant = npc.type == NPCID.Spazmatism;
                }
                npc.Center += Main.rand.NextVector2Circular(3f, 3f);
            }

            // Create a lens flare on top of Spazmatism that briefly fades in and out.
            if (UniversalAttackTimer == slowdownTime + jitterTime)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.CalThunderStrikeSound, Target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int lensFlare = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<TwinsLensFlare>(), 0, 0f);
                    if (Main.projectile.IndexInRange(lensFlare))
                        Main.projectile[lensFlare].ModProjectile<TwinsLensFlare>().SpazmatismVariant = npc.type == NPCID.Spazmatism;
                }
            }

            // Release an incredibly violent explosion and die.
            if (UniversalAttackTimer == slowdownTime + jitterTime + lensFlareTime)
            {
                Color mainExplosionColor = npc.type == NPCID.Spazmatism ? Color.YellowGreen : Color.Red;
                Utilities.CreateShockwave(npc.Center, 40, 10, 30f);
                GeneralParticleHandler.SpawnParticle(new ElectricExplosionRing(npc.Center, Vector2.Zero, [Color.Gray, mainExplosionColor * 0.6f], 2.3f, 75, 0.4f));

                CurrentAttackState = TwinsAttackState.LazilyObserve;
                UniversalAttackTimer = 0;
                npc.life = 0;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    npc.StrikeInstantKill();

                npc.active = false;

                if (!otherTwinExists)
                {
                    npc.NPCLoot();
                    if (npc.type == NPCID.Retinazer && BossRushEvent.BossRushActive)
                        BossRushEvent.BossRushStage++;
                }
            }
        }
        #endregion Specific Attacks

        #region Retinazer
        public enum RetinazerAttackState
        {
            SwiftLaserBursts,
            BigAimedLaserbeam,
            AgileLaserbeamSweeps,
        }

        public static void DoBehavior_RetinazerAlone(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            Vector2 endOfLaserCannon = npc.Center + (npc.rotation + PiOver2).ToRotationVector2() * 80f;
            ref float attackTimer = ref npc.Infernum().ExtraAI[10];
            ref float attackState = ref npc.Infernum().ExtraAI[11];
            ref float burstCounter = ref npc.Infernum().ExtraAI[12];
            ref float telegraphDirection = ref npc.Infernum().ExtraAI[RetinazerTelegraphDirectionIndex];
            ref float telegraphOpacity = ref npc.Infernum().ExtraAI[RetinazerTelegraphOpacityIndex];
            ref float laserbeamShootCounter = ref npc.Infernum().ExtraAI[15];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[16];
            ref float laserBarrageCounter = ref npc.Infernum().ExtraAI[17];

            switch ((RetinazerAttackState)(int)attackState)
            {
                case RetinazerAttackState.SwiftLaserBursts:
                    int laserBarrageCount = 6;
                    int laserShootRate = 5;
                    int laserShootDelay = 16;
                    int laserShootTime = 48;

                    if (laserBarrageCounter <= 0f)
                        laserShootDelay += 45;

                    // Disable contact universally. It is not relevant for this attack.
                    npc.damage = 0;

                    // Hover into position for the barrage.
                    Vector2 hoverDestination = Target.Center + (TwoPi * laserBarrageCounter / 4f + PiOver4).ToRotationVector2() * new Vector2(660f, 575f);
                    if (attackTimer <= laserShootDelay)
                    {
                        npc.Center = Vector2.Lerp(npc.Center, hoverDestination, 0.115f);
                        npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 18f, 0.25f);

                        float chargeUpInterpolant = attackTimer / laserShootDelay;
                        for (int i = 0; i < 2; i++)
                        {
                            if (Main.rand.NextFloat() > chargeUpInterpolant)
                                continue;

                            Color laserEnergyColor = Color.Lerp(Color.Red, Color.Yellow, Main.rand.NextFloat(0.64f));
                            Vector2 laserEnergySpawnPosition = endOfLaserCannon + Main.rand.NextVector2Unit() * Main.rand.NextFloat(80f, 132f);
                            Vector2 laserEnergyVelocity = (endOfLaserCannon - laserEnergySpawnPosition) * 0.04f;
                            SquishyLightParticle laserEnergy = new(laserEnergySpawnPosition, laserEnergyVelocity, 0.8f, laserEnergyColor, 30, 1f, 4f);
                            GeneralParticleHandler.SpawnParticle(laserEnergy);
                        }
                    }

                    // Release laser bursts.
                    else
                    {
                        if (attackTimer == laserShootDelay + 1f)
                        {
                            SoundEngine.PlaySound(AresTeslaCannon.TeslaOrbShootSound, Target.Center);
                            Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 4f;
                            ScreenEffectSystem.SetBlurEffect(npc.Center, 0.25f, 16);

                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                for (int i = 0; i < 4; i++)
                                {
                                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(laser =>
                                    {
                                        laser.tileCollide = false;
                                    });
                                    Utilities.NewProjectileBetter(Target.Center + (TwoPi * i / 4f).ToRotationVector2() * 750f, (TwoPi * i / 4f).ToRotationVector2() * -7f, ModContent.ProjectileType<RetinazerLaser>(), SmallLaserDamage, 0f, -1, 0f, 1f);
                                }
                            }
                        }

                        npc.Center = Vector2.Lerp(npc.Center, hoverDestination, 0.196f);
                        npc.velocity *= 0.9f;

                        if (attackTimer % laserShootRate == 0f)
                        {
                            SoundEngine.PlaySound(CommonCalamitySounds.ExoLaserShootSound with { Volume = 0.85f }, Target.Center);
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(laser =>
                                {
                                    laser.tileCollide = false;
                                });
                                Utilities.NewProjectileBetter(endOfLaserCannon, npc.SafeDirectionTo(Target.Center) * 6f, ModContent.ProjectileType<RetinazerLaser>(), SmallLaserDamage, 0f);
                            }
                        }

                        if (attackTimer >= laserShootDelay + laserShootTime)
                        {
                            attackTimer = 0f;
                            laserBarrageCounter++;
                            if (laserBarrageCounter >= laserBarrageCount)
                            {
                                SoundEngine.PlaySound(Artemis.AttackSelectionSound with { Volume = 2f }, Target.Center);
                                attackState = (int)RetinazerAttackState.BigAimedLaserbeam;
                                laserBarrageCounter = 0f;
                            }

                            npc.netUpdate = true;
                        }
                    }

                    // Look at the target.
                    npc.rotation = npc.AngleTo(Target.Center) - PiOver2;

                    break;
                case RetinazerAttackState.BigAimedLaserbeam:
                    int telegraphAimTime = 60;
                    laserShootRate = 24;
                    int laserbeamShootCount = 2;
                    Vector2 destination = Target.Center - Vector2.UnitY * 360f;
                    destination.X -= Math.Sign(Target.Center.X - npc.Center.X) * 600f;
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * 28f, 1.1f);

                    // Aim for longer on the first shot.
                    if (laserbeamShootCounter <= 0f)
                        telegraphAimTime += 48;

                    if (npc.WithinRange(destination, 38f))
                    {
                        npc.Center = destination;
                        npc.velocity = Vector2.Zero;
                    }

                    int barrageBurstTime = 120;
                    if (attackTimer < telegraphAimTime)
                        npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(Target.Center + Target.velocity * 27f) - PiOver2, 0.1f);

                    if (attackTimer < barrageBurstTime && attackTimer % laserShootRate == laserShootRate - 1f)
                    {
                        SoundEngine.PlaySound(CommonCalamitySounds.ExoLaserShootSound with { Volume = 0.8f }, npc.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 laserVelocity = npc.SafeDirectionTo(Target.Center).RotatedByRandom(0.12f) * 7f;
                            if (BossRushEvent.BossRushActive)
                                laserVelocity *= 2.1f;
                            int laser = Utilities.NewProjectileBetter(npc.Center + laserVelocity * 3.6f, laserVelocity, ModContent.ProjectileType<RetinazerLaser>(), SmallLaserDamage, 0f);
                            Main.projectile[laser].tileCollide = false;
                        }
                    }

                    // Aim telegraphs.
                    if (attackTimer >= 30f && attackTimer < telegraphAimTime)
                    {
                        // Initialize the telegraph direction.
                        if (attackTimer == 30f)
                        {
                            telegraphDirection = npc.AngleTo(Target.Center);
                            telegraphOpacity = 0.01f;
                            npc.netUpdate = true;
                        }

                        telegraphOpacity = Clamp(telegraphOpacity + 0.04f, 0f, 1f);
                        telegraphDirection = npc.rotation + PiOver2;
                    }

                    // Create a powerful boom effect and release the aimed deathray.
                    if (attackTimer == telegraphAimTime)
                    {
                        Utilities.CreateShockwave(npc.Center, 2, 5, 142f, false);
                        Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 8f;
                        ScreenEffectSystem.SetFlashEffect(npc.Center, 3f, 20);

                        SoundEngine.PlaySound(AresLaserCannon.LaserbeamShootSound with { Volume = 0.67f }, Target.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Utilities.NewProjectileBetter(npc.Center + npc.SafeDirectionTo(Target.Center) * 48f, npc.SafeDirectionTo(Target.Center), ModContent.ProjectileType<RetinazerAimedDeathray>(), BigDeathrayDamage, 0f, -1, 0f, npc.whoAmI);

                            telegraphOpacity = 0f;
                            npc.netUpdate = true;
                        }
                    }

                    if (attackTimer >= telegraphAimTime + RetinazerAimedDeathray.LifetimeConst + 48f)
                    {
                        attackTimer = 0f;

                        if (laserbeamShootCounter < laserbeamShootCount - 1f)
                            laserbeamShootCounter++;
                        else
                        {
                            SoundEngine.PlaySound(Artemis.AttackSelectionSound with { Volume = 2f }, Target.Center);
                            attackState = (int)RetinazerAttackState.AgileLaserbeamSweeps;
                            laserbeamShootCounter = 0f;
                        }
                        npc.netUpdate = true;
                    }
                    break;
                case RetinazerAttackState.AgileLaserbeamSweeps:
                    int chargeCount = 5;
                    int chargeUpTime = 36;
                    int chargeTime = RetinazerAimedDeathray2.LifetimeConst;
                    int laserCircleCount = 7;
                    int laserCircleReleaseRate = 15;
                    float chargeSpeed = 29f;
                    float arcAngularVelocity = 0.0243f;

                    if (chargeCounter <= 0f)
                        chargeUpTime += 42;

                    // Attempt to loosely hover near the target and charge up energy before charging and releasing a laser.
                    if (attackTimer <= chargeUpTime)
                    {
                        // Hover near the target. Once sufficiently close to performing the attack Retinazer will slow down.
                        if (attackTimer >= chargeUpTime - 10f)
                            npc.velocity *= 0.85f;
                        else
                        {
                            hoverDestination = Target.Center + Target.SafeDirectionTo(npc.Center, -Vector2.UnitY) * 400f;
                            npc.Center = Vector2.Lerp(npc.Center, hoverDestination, 0.135f);
                            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 19f, 0.36f);
                        }

                        float chargeUpInterpolant = attackTimer / chargeUpTime;
                        for (int i = 0; i < 2; i++)
                        {
                            if (Main.rand.NextFloat() > chargeUpInterpolant)
                                continue;

                            Color laserEnergyColor = Color.Lerp(Color.Red, Color.Yellow, Main.rand.NextFloat(0.64f));
                            Vector2 laserEnergySpawnPosition = endOfLaserCannon + Main.rand.NextVector2Unit() * Main.rand.NextFloat(80f, 132f);
                            Vector2 laserEnergyVelocity = (endOfLaserCannon - laserEnergySpawnPosition) * 0.04f;
                            SquishyLightParticle laserEnergy = new(laserEnergySpawnPosition, laserEnergyVelocity, 1.5f, laserEnergyColor, 36, 1f, 4f);
                            GeneralParticleHandler.SpawnParticle(laserEnergy);
                        }

                        // Attempt to look at the target.
                        float idealRotation = npc.AngleTo(Target.Center) - PiOver2;
                        float turnSpeedFactor = Utils.Remap(attackTimer, 0f, 7f, 16f, 1f);
                        npc.rotation = npc.rotation.AngleLerp(idealRotation, turnSpeedFactor * 0.008f).AngleTowards(idealRotation, turnSpeedFactor * 0.014f);

                        // Telegraph the aim direction to the target.
                        telegraphOpacity = Utils.GetLerpValue(0f, chargeUpTime * 0.6f, attackTimer, true);
                        telegraphDirection = npc.rotation + PiOver2;
                    }

                    // Charge and release the laserbeam.
                    if (attackTimer == chargeUpTime)
                    {
                        Utilities.CreateShockwave(npc.Center, 2, 8, 145f, false);
                        Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 8f;
                        ScreenEffectSystem.SetFlashEffect(npc.Center, 2f, 20);

                        SoundEngine.PlaySound(Artemis.ChargeSound, Target.Center);
                        SoundEngine.PlaySound(AresLaserCannon.LaserbeamShootSound with { Volume = 0.67f }, Target.Center);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            npc.velocity = telegraphDirection.ToRotationVector2() * chargeSpeed;
                            telegraphOpacity = 0f;
                            Utilities.NewProjectileBetter(npc.Center, telegraphDirection.ToRotationVector2(), ModContent.ProjectileType<RetinazerAimedDeathray2>(), BigDeathrayDamage, 0f, -1, 0f, npc.whoAmI);

                            npc.netUpdate = true;
                        }
                    }

                    // Arc around after charging, spinning the laserbeam.
                    if (attackTimer >= chargeUpTime + 1f)
                    {
                        npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(Target.Center), arcAngularVelocity);
                        npc.rotation = npc.velocity.ToRotation() - PiOver2;

                        if (attackTimer % laserCircleReleaseRate == laserCircleReleaseRate - 1f)
                        {
                            SoundEngine.PlaySound(Artemis.LaserShotgunSound with { Volume = 0.8f }, npc.Center);
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                for (int i = 0; i < laserCircleCount; i++)
                                {
                                    Vector2 shootVelocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(TwoPi * i / laserCircleCount) * 6f;
                                    Utilities.NewProjectileBetter(npc.Center + shootVelocity * 12f, shootVelocity, ModContent.ProjectileType<RetinazerLaser>(), SmallLaserDamage, 0f);
                                }
                            }
                        }
                    }

                    // Slow down before transitioning to the next attack.
                    if (attackTimer >= chargeUpTime + chargeTime - 10f)
                        npc.velocity *= 0.9f;

                    if (attackTimer >= chargeUpTime + chargeTime)
                    {
                        attackTimer = 0f;
                        chargeCounter++;
                        if (chargeCounter >= chargeCount)
                        {
                            SoundEngine.PlaySound(Artemis.AttackSelectionSound with { Volume = 2f }, Target.Center);
                            chargeCounter = 0f;
                            attackState = (int)RetinazerAttackState.SwiftLaserBursts;
                        }

                        npc.netUpdate = true;
                    }

                    break;
            }

            attackTimer++;
        }
        #endregion

        #region Spazmatism
        public enum SpazmatismAttackState
        {
            MobileChargePhase,
            HellfireBursts,
            CursedFlameSpin
        }

        public static void DoBehavior_SpazmatismAlone(NPC npc, ref float chargingFlag)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float attackTimer = ref npc.Infernum().ExtraAI[10];
            ref float attackState = ref npc.Infernum().ExtraAI[11];
            ref float fireballShootTimer = ref npc.Infernum().ExtraAI[12];

            int fireballShootRate = (int)Lerp(300, 120, Utils.GetLerpValue(0.5f, 0.1f, lifeRatio));
            if (Main.netMode != NetmodeID.MultiplayerClient && fireballShootTimer >= fireballShootRate)
            {
                Utilities.NewProjectileBetter(Target.Center - Vector2.UnitX * 1300f, Vector2.UnitX * 11f, ModContent.ProjectileType<CursedFlameBurstTelegraph>(), 0, 0f);
                Utilities.NewProjectileBetter(Target.Center + Vector2.UnitX * 1300f, Vector2.UnitX * -11f, ModContent.ProjectileType<CursedFlameBurstTelegraph>(), 0, 0f);
                fireballShootTimer = 0f;
            }

            fireballShootTimer++;

            switch ((SpazmatismAttackState)(int)attackState)
            {
                case SpazmatismAttackState.MobileChargePhase:
                    int hoverTime = 90;
                    int chargeDelayTime = (int)SmoothStep(24f, 16f, 1f - lifeRatio);
                    int chargeTime = (int)SmoothStep(50f, 32f, 1f - lifeRatio);
                    int slowdownTime = 35;
                    int chargeCount = 3;
                    ref float chargeDestinationX = ref npc.Infernum().ExtraAI[13];
                    ref float chargeDestinationY = ref npc.Infernum().ExtraAI[14];
                    ref float chargeCounter = ref npc.Infernum().ExtraAI[15];

                    // Lazily hover next to the player.
                    if (attackTimer < hoverTime)
                    {
                        float hoverSpeed = SmoothStep(15.5f, 20.7f, 1f - lifeRatio);
                        float hoverAcceleration = SmoothStep(0.46f, 0.9f, 1f - lifeRatio);
                        if (BossRushEvent.BossRushActive)
                        {
                            hoverSpeed *= 2f;
                            hoverAcceleration *= 2f;
                        }

                        Vector2 destination = Target.Center;
                        destination.X -= Math.Sign(Target.Center.X - npc.Center.X) * 600f;
                        destination.Y -= npc.velocity.Length() * 20f;
                        npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * hoverSpeed, hoverAcceleration);
                        npc.rotation = npc.AngleTo(Target.Center) - PiOver2;
                    }

                    // Aim towards a location near the player.
                    else if (attackTimer < hoverTime + chargeDelayTime)
                    {
                        if (chargeDestinationX == 0f || chargeDestinationY == 0f)
                        {
                            Vector2 destinationOffset = Main.rand.NextVector2Circular(120f, 120f);
                            chargeDestinationX = Target.Center.X + destinationOffset.X;
                            chargeDestinationY = Target.Center.Y + destinationOffset.Y;
                        }

                        float aimAngleToDestination = npc.AngleTo(new Vector2(chargeDestinationX, chargeDestinationY)) - PiOver2;
                        npc.rotation = npc.rotation.AngleLerp(aimAngleToDestination, 0.15f);
                        npc.velocity *= 0.925f;
                    }

                    // Charge.
                    if (attackTimer == hoverTime + chargeDelayTime)
                    {
                        Vector2 chargeDestination = new(chargeDestinationX, chargeDestinationY);
                        npc.velocity = npc.SafeDirectionTo(chargeDestination);
                        npc.velocity *= 22.5f + npc.Distance(Target.Center) * 0.01f;
                        if (BossRushEvent.BossRushActive)
                            npc.velocity *= 1.8f;

                        npc.rotation = npc.velocity.ToRotation() - PiOver2;
                        npc.netUpdate = true;

                        // Create a charge sound.
                        SoundEngine.PlaySound(Artemis.ChargeSound, Target.Center);
                    }

                    // And slow down.
                    if (attackTimer >= hoverTime + chargeDelayTime + chargeTime)
                    {
                        npc.velocity *= 0.94f;

                        // Release fire outward.
                        if (attackTimer == hoverTime + chargeDelayTime + chargeTime + slowdownTime / 2)
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                for (int i = 0; i < 9; i++)
                                {
                                    Vector2 shootDirection = (TwoPi * i / 9f).ToRotationVector2();
                                    Utilities.NewProjectileBetter(npc.Center + shootDirection * 50f, shootDirection * 18f, ModContent.ProjectileType<CursedFlameBurst>(), CursedFireballDamage, 0f);
                                }
                            }

                            SoundEngine.PlaySound(CommonCalamitySounds.ExoPlasmaShootSound, Target.Center);
                        }

                        npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(Target.Center) - PiOver2, 0.15f);

                        // And finally go to the next AI state.
                        if (attackTimer >= hoverTime + chargeDelayTime + chargeTime + slowdownTime)
                        {
                            chargeCounter++;
                            chargeDestinationX = chargeDestinationY = 0f;
                            attackTimer = 0f;

                            if (chargeCounter >= chargeCount)
                            {
                                attackState = (int)SpazmatismAttackState.HellfireBursts;
                                chargeCounter = 0f;
                            }
                            npc.netUpdate = true;
                        }
                    }
                    else if (attackTimer >= hoverTime + chargeDelayTime)
                        chargingFlag = 1f;
                    break;
                case SpazmatismAttackState.HellfireBursts:
                    hoverTime = 60;
                    int aimTime = 40;
                    int burstFireRate = 25;
                    int totalBursts = 2;
                    int fireballsPerBurst = 3;
                    ref float burstTimer = ref npc.Infernum().ExtraAI[13];
                    ref float burstCount = ref npc.Infernum().ExtraAI[14];

                    if (lifeRatio < 0.55f)
                    {
                        burstFireRate = 25;
                        totalBursts = 3;
                    }
                    if (lifeRatio < 0.33f)
                    {
                        burstFireRate = 16;
                        fireballsPerBurst = 4;
                    }
                    if (lifeRatio < 0.125f)
                        burstFireRate = 12;

                    // Keenly hover next to the player.
                    if (attackTimer < hoverTime)
                    {
                        float hoverSpeed = SmoothStep(15.5f, 22f, 1f - lifeRatio);
                        float hoverAcceleration = SmoothStep(0.56f, 0.9f, 1f - lifeRatio);
                        Vector2 destination = Target.Center;
                        destination.X -= Math.Sign(Target.Center.X - npc.Center.X) * 570f;
                        destination.Y -= npc.velocity.Length() * 20f;
                        npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * hoverSpeed, hoverAcceleration);
                        npc.rotation = npc.AngleTo(Target.Center) - PiOver2;
                    }

                    // Slow down and look at the player.
                    else if (attackTimer >= hoverTime)
                    {
                        npc.velocity *= 0.94f;
                        npc.rotation = npc.AngleTo(Target.Center) - PiOver2;
                    }

                    if (attackTimer >= hoverTime + aimTime)
                    {
                        burstTimer++;
                        if (burstTimer >= burstFireRate)
                        {
                            burstTimer = 0f;
                            burstCount++;
                            if (burstCount >= totalBursts)
                            {
                                burstTimer = burstCount = 0f;
                                attackTimer = 0f;
                                attackState = (int)SpazmatismAttackState.CursedFlameSpin;
                                npc.netUpdate = true;
                            }

                            // Release bursts of fire.
                            else
                            {
                                if (Main.netMode != NetmodeID.MultiplayerClient)
                                {
                                    for (int i = 0; i < fireballsPerBurst; i++)
                                    {
                                        float offsetAngle = Lerp(-0.96f, 0.96f, i / (float)fireballsPerBurst);
                                        Vector2 shootVelocity = npc.SafeDirectionTo(Target.Center).RotatedBy(offsetAngle) * 21f;
                                        if (BossRushEvent.BossRushActive)
                                            shootVelocity *= 1.8f;
                                        Utilities.NewProjectileBetter(npc.Center + shootVelocity * 5f, shootVelocity, ModContent.ProjectileType<HomingCursedFlameBurst>(), CursedFlameBurstDamage, 0f);
                                    }
                                }

                                SoundEngine.PlaySound(CommonCalamitySounds.ExoPlasmaShootSound, Target.Center);
                            }
                        }
                    }
                    break;
                case SpazmatismAttackState.CursedFlameSpin:
                    int windUpTime = 67;
                    int spinTime = 360;
                    int cinderReleaseRate = 60;
                    int cinderReleaseCount = 12;
                    ref float flamethrowerIntroSoundSlot = ref npc.localAI[0];
                    ref float flamethrowerSoundSlot = ref npc.localAI[1];

                    // Attempt to fly near the target while spinning and releasing fire outward.
                    float windUpInterpolant = Utils.GetLerpValue(0f, windUpTime, attackTimer, true);
                    if (!npc.WithinRange(Target.Center, 300f))
                        npc.SimpleFlyMovement(npc.SafeDirectionTo(Target.Center) * windUpInterpolant * 15f, windUpInterpolant * 0.19f);

                    // SPEEEEEEEEEEEEEEEEEEEEN
                    npc.rotation += windUpInterpolant * 0.18f;

                    // Handle sound effects.
                    if (attackTimer == 1f)
                        flamethrowerIntroSoundSlot = SoundEngine.PlaySound(InfernumSoundRegistry.GlassmakerFireStartSound with { Volume = 0.85f }, npc.Center).ToFloat();
                    else
                    {
                        bool startSoundBeingPlayed = SoundEngine.TryGetActiveSound(SlotId.FromFloat(flamethrowerIntroSoundSlot), out var startSound) && startSound.IsPlaying;
                        if (startSoundBeingPlayed)
                            startSound.Position = npc.Center;
                        else
                        {
                            // Update the sound telegraph's position.
                            if (SoundEngine.TryGetActiveSound(SlotId.FromFloat(flamethrowerSoundSlot), out var t) && t.IsPlaying)
                                t.Position = npc.Center;
                            else
                                flamethrowerSoundSlot = SoundEngine.PlaySound(InfernumSoundRegistry.GlassmakerFireSound with { Volume = 1.2f }, npc.Center).ToFloat();
                        }
                    }

                    // Release the flamethrower breath.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float flamethrowerSpeed = windUpInterpolant * 2f;
                        Utilities.NewProjectileBetter(npc.Center, (npc.rotation + PiOver2).ToRotationVector2() * flamethrowerSpeed + npc.velocity / 11f, ModContent.ProjectileType<SpazmatismFlamethrower>(), FlamethrowerDamage, 0f, -1);
                    }

                    // Periodically shoot cinders.
                    if (attackTimer % cinderReleaseRate == cinderReleaseRate - 1f && !npc.WithinRange(Target.Center, 300f))
                    {
                        SoundEngine.PlaySound(ProfanedGuardianCommander.DashSound with { Pitch = 0.125f }, npc.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            float shootOffsetAngle = Main.rand.NextFloat(TwoPi);
                            for (int i = 0; i < cinderReleaseCount; i++)
                            {
                                Vector2 shootVelocity = (TwoPi * i / cinderReleaseCount + shootOffsetAngle).ToRotationVector2() * 9.6f;
                                Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<CursedCinder>(), CursedCinderDamage, 0f);
                            }
                        }
                    }

                    if (attackTimer > spinTime)
                    {
                        SilenceSpazmatismFlameSounds(npc);
                        attackTimer = 0f;
                        attackState = (int)SpazmatismAttackState.MobileChargePhase;
                        npc.netUpdate = true;
                    }
                    break;
            }

            attackTimer++;
        }

        public static void SilenceSpazmatismFlameSounds(NPC spazmatism)
        {
            for (int i = 0; i < 2; i++)
            {
                if (SoundEngine.TryGetActiveSound(SlotId.FromFloat(spazmatism.localAI[i]), out var t) && t.IsPlaying)
                    t.Stop();
            }
            SoundEngine.PlaySound(InfernumSoundRegistry.GlassmakerFireEndSound with { Volume = 0.85f }, spazmatism.Center);
        }
        #endregion

        #endregion

        #region Helper Methods
        public static bool PersonallyInPhase2(NPC npc)
        {
            return npc.Infernum().ExtraAI[0] >= Phase2TransitionTime * 0.5f;
        }

        public static void SelectNextAttack()
        {
            UniversalAttackTimer = 0;
            UniversalStateIndex = (UniversalStateIndex + 1) % AttackSequenceLength;

            // Reset all optional AI values.
            if (SpazmatismIndex != -1)
            {
                for (int i = 0; i < NPC.maxAI; i++)
                    Main.npc[SpazmatismIndex].ai[i] = 0f;
                Main.npc[SpazmatismIndex].velocity = Vector2.Zero;
                Main.npc[SpazmatismIndex].netUpdate = true;
            }
            if (RetinazerIndex != -1)
            {
                for (int i = 0; i < NPC.maxAI; i++)
                    Main.npc[RetinazerIndex].ai[i] = 0f;
                Main.npc[RetinazerIndex].velocity = Vector2.Zero;
                Main.npc[RetinazerIndex].netUpdate = true;
            }

            if (InPhase2)
            {
                CurrentAttackState = UniversalStateIndex switch
                {
                    0 => TwinsAttackState.ChargeRedirect,
                    1 => TwinsAttackState.DownwardCharge,
                    2 => TwinsAttackState.DownwardCharge,
                    3 => TwinsAttackState.Spin,
                    4 => TwinsAttackState.FlamethrowerBurst,
                    5 => TwinsAttackState.SwitchCharges,
                    6 => TwinsAttackState.Spin,
                    7 => TwinsAttackState.ChargeRedirect,
                    8 => TwinsAttackState.DownwardCharge,
                    9 => TwinsAttackState.DownwardCharge,
                    10 => TwinsAttackState.SwitchCharges,
                    11 => TwinsAttackState.FlamethrowerBurst,
                    _ => TwinsAttackState.ChargeRedirect,
                };
                if (CombinedLifeRatio < Phase3LifeRatioThreshold && UniversalStateIndex % 4 == 3)
                    CurrentAttackState = TwinsAttackState.ChaoticFireAndDownwardLaser;
            }
            else
            {
                CurrentAttackState = UniversalStateIndex switch
                {
                    0 => TwinsAttackState.ChargeRedirect,
                    1 => TwinsAttackState.DownwardCharge,
                    2 => TwinsAttackState.DownwardCharge,
                    3 => TwinsAttackState.Spin,
                    4 => TwinsAttackState.ChargeRedirect,
                    5 => TwinsAttackState.DownwardCharge,
                    6 => TwinsAttackState.DownwardCharge,
                    7 => TwinsAttackState.SwitchCharges,
                    8 => TwinsAttackState.ChargeRedirect,
                    9 => TwinsAttackState.DownwardCharge,
                    10 => TwinsAttackState.Spin,
                    11 => TwinsAttackState.SwitchCharges,
                    _ => TwinsAttackState.ChargeRedirect,
                };
            }

            if (UniversalStateIndex % 5 == 4 && !InPhase2 && !BossRushEvent.BossRushActive)
                CurrentAttackState = TwinsAttackState.LazilyObserve;
        }

        public static NPC GetOtherTwin(NPC npc)
        {
            if (npc.type == NPCID.Retinazer)
                return SpazmatismIndex == -1 ? Main.npc[NPC.FindFirstNPC(NPCID.Spazmatism)] : Main.npc[SpazmatismIndex];
            if (npc.type == NPCID.Spazmatism)
                return RetinazerIndex == -1 ? Main.npc[NPC.FindFirstNPC(NPCID.Retinazer)] : Main.npc[RetinazerIndex];
            return null;
        }

        public static bool PrepareForDeathAnimation(NPC npc)
        {
            if (CurrentAttackState == TwinsAttackState.DeathAnimation)
                return true;

            UniversalAttackTimer = 0;
            CurrentAttackState = TwinsAttackState.DeathAnimation;

            npc.dontTakeDamage = true;
            npc.life = npc.lifeMax;
            npc.damage = 0;
            npc.netUpdate = true;
            return false;
        }
        #endregion

        #region Death Effects
        public static bool HandleDeathEffects(NPC npc)
        {
            if (npc.type == NPCID.Spazmatism)
                SilenceSpazmatismFlameSounds(npc);

            bool otherTwinHasCreatedShield = false;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (!Main.npc[i].active)
                    continue;
                if (Main.npc[i].type is not NPCID.Retinazer and not NPCID.Spazmatism)
                    continue;
                if (Main.npc[i].type == npc.type)
                    continue;

                if (Main.npc[i].Infernum().ExtraAI[3] == 1f)
                {
                    otherTwinHasCreatedShield = true;
                    break;
                }
            }

            if (npc.Infernum().ExtraAI[3] == 0f && !otherTwinHasCreatedShield)
            {
                npc.life = 1;
                npc.active = true;
                npc.netUpdate = true;
                npc.dontTakeDamage = true;
                return false;
            }

            return PrepareForDeathAnimation(npc);
        }
        #endregion Death Effects
    }
}
