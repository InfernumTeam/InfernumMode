using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Events;
using CalamityMod.Projectiles.Boss;
using InfernumMode.BehaviorOverrides.BossAIs.Guardians;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Twins
{
    public static class TwinsAttackSynchronizer
    {
        public enum TwinsAttackState
        {
            ChargeRedirect,
            DownwardCharge,
            SwitchCharges,
            Spin,
            RedirectingLasersAndFireRain,
            RedirectingLasersAndFlameCharge,
            LazilyObserve
        }

        public static int GetAttackLength(TwinsAttackState state)
        {
            switch (state)
            {
                case TwinsAttackState.ChargeRedirect:
                    return (int)MathHelper.Lerp(135f, 105f, 1f - CombinedLifeRatio);
                case TwinsAttackState.DownwardCharge:
                    return (int)MathHelper.Lerp(105f, 75f, 1f - CombinedLifeRatio);
                case TwinsAttackState.SwitchCharges:
                    return 360;
                case TwinsAttackState.Spin:
                    return 400;
                case TwinsAttackState.RedirectingLasersAndFireRain:
                    return 220;
                case TwinsAttackState.RedirectingLasersAndFlameCharge:
                    return 420;
                case TwinsAttackState.LazilyObserve:
                    return InPhase2 ? 1 : 150;
            }
            return 1;
        }

        private static int _targetIndex = -1;
        public static int SpazmatismIndex = -1;
        public static int RetinazerIndex = -1;
        public static int UniversalStateIndex = 0;
        public static int UniversalAttackTimer = 0;
        public static TwinsAttackState CurrentAttackState;
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
                if (SpazmatismIndex == -1 && RetinazerIndex != -1)
                    return true;
                if (SpazmatismIndex != -1 && RetinazerIndex == -1)
                    return true;

                float spazmatismLifeRatio = Main.npc[SpazmatismIndex].life / (float)Main.npc[SpazmatismIndex].lifeMax;
                float retinzerLifeRatio = Main.npc[RetinazerIndex].life / (float)Main.npc[RetinazerIndex].lifeMax;
                return spazmatismLifeRatio < Phase2LifeRatioThreshold || retinzerLifeRatio < Phase2LifeRatioThreshold;
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

            // Go to the next AI state.
            if (UniversalAttackTimer >= GetAttackLength(CurrentAttackState))
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

                ChooseNextAIState();
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

            SpazmatismIndex = RetinazerIndex = -1;
        }
        #endregion

        #region AI

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

            npc.buffImmune[ModContent.BuffType<CrushDepth>()] = true;

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

            chargeFlameRotation = chargeFlameRotation.AngleLerp(npc.rotation, 0.05f).AngleTowards(npc.rotation, 0.1f);

            // Entering phase 2 effect.
            if (lifeRatio < Phase2LifeRatioThreshold && phase2Timer < Phase2TransitionTime)
            {
                phase2Timer++;
                phase2TransitionSpin = Utils.InverseLerp(0f, Phase2TransitionTime / 2f, phase2Timer, true);
                phase2TransitionSpin *= Utils.InverseLerp(Phase2TransitionTime, Phase2TransitionTime / 2f, phase2Timer, true);
                phase2TransitionSpin *= 0.4f;

                CurrentAttackState = TwinsAttackState.ChargeRedirect;
                UniversalAttackTimer = 0;

                npc.rotation += phase2TransitionSpin;
                npc.velocity *= 0.97f;

                // Go to phase 2 and explode into metal, blood, and gore.
                if (phase2Timer == (int)(Phase2TransitionTime / 2))
                {
                    Main.PlaySound(SoundID.NPCHit1, (int)npc.position.X, (int)npc.position.Y);

                    for (int i = 0; i < 2; i++)
                    {
                        Gore.NewGore(npc.position, Main.rand.NextVector2Circular(6f, 6f), 143, 1f);
                        Gore.NewGore(npc.position, Main.rand.NextVector2Circular(6f, 6f), 7, 1f);
                        Gore.NewGore(npc.position, Main.rand.NextVector2Circular(6f, 6f), 6, 1f);
                    }

                    for (int i = 0; i < 20; i++)
                        Dust.NewDust(npc.position, npc.width, npc.height, 5, Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-6f, 6f), 0, default, 1f);

                    Main.PlaySound(SoundID.Roar, (int)npc.position.X, (int)npc.position.Y, 0, 1f, 0f);
                }

                chargeFlameTimer = 0f;
                return false;
            }

            if (inPhase2)
                npc.HitSound = SoundID.NPCHit4;

            // Despawn effect.
            if (shouldDespawn)
            {
                if (npc.timeLeft > 90)
                    npc.timeLeft = 90;

                npc.velocity.Y -= 0.4f;
                npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
                return false;
            }

            // Reset the boss' stuff every frame. It can be changed later as necessary.
            chargingFlag = 0f;
            npc.damage = npc.defDamage;
            npc.dontTakeDamage = !otherMechIsInPhase2 && inPhase2;
            npc.noTileCollide = true;

            bool alone = (!NPC.AnyNPCs(NPCID.Spazmatism) && isRetinazer) || (!NPC.AnyNPCs(NPCID.Retinazer) && isSpazmatism);
            bool otherTwinHasCreatedShield = false;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (!Main.npc[i].active)
                    continue;
                if (Main.npc[i].type != NPCID.Retinazer && Main.npc[i].type != NPCID.Spazmatism)
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
                {
                    int shield = Projectile.NewProjectile(npc.Center, Vector2.Zero, ModContent.ProjectileType<TwinsShield>(), 0, 0f, 255);
                    Main.projectile[shield].ai[0] = npc.whoAmI;
                }
                Main.NewText($"{(npc.type == NPCID.Spazmatism ? "SPA-MK1" : "RET-MK1")}: DEFENSES PENETRATED. INITIATING PROCEDURE SHLD-17ECF9.", npc.type == NPCID.Spazmatism ? Color.LimeGreen : Color.IndianRed);
                hasStartedHealFlag = 1f;
            }

            if (healCountdown > 0f)
            {
                if (alone)
                {
                    npc.life = (int)MathHelper.Lerp(npc.lifeMax * 0.05f, npc.lifeMax * 0.3f, 1f - healCountdown / TwinsShield.HealTime);
                    if (healCountdown == TwinsShield.HealTime - 5)
                        Main.NewText($"{(npc.type == NPCID.Spazmatism ? "SPA-MK1" : "RET-MK1")}: ERROR DETECTING SECONDARY UNIT. BURNING EXCESS FUEL RESERVES.", npc.type == NPCID.Spazmatism ? Color.LimeGreen : Color.IndianRed);
                    healCountdown--;
                }
                else
                    npc.life = (int)(npc.lifeMax * 0.05f);
            }

            if (hasStartedHealFlag == 1f && healCountdown > 0f)
            {
                npc.Opacity = MathHelper.Lerp(npc.Opacity, 1f, 0.15f);
                npc.dontTakeDamage = true;
            }

            if (alone)
            {
                // Regenerate orbs.
                if (hasStartedHealFlag == 1f && overdriveTimer < 120f)
                {
                    npc.velocity *= 0.97f;
                    npc.rotation = npc.AngleTo(Target.Center) - MathHelper.PiOver2;

                    if (healCountdown <= 0f)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient && overdriveTimer == 105f)
                        {
                            int explosion = Projectile.NewProjectile(npc.Center, Vector2.Zero, ModContent.ProjectileType<TwinsEnergyExplosion>(), 0, 0f);
                            Main.projectile[explosion].ai[0] = npc.type;

                            if (npc.type == NPCID.Retinazer)
                            {
                                NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<EnergyOrb>(), 0, npc.whoAmI, 1f);
                                NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<EnergyOrb>(), 0, npc.whoAmI, -1f);
                            }
                            else if (npc.type == NPCID.Spazmatism)
                                NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<CursedOrb>(), 0, npc.whoAmI);
                        }

                        overdriveTimer++;
                    }
                    return false;
                }

                if (npc.type == NPCID.Spazmatism)
                    DoAI_SpazmatismAlone(npc, ref chargingFlag);
                else
                    DoAI_RetinazerAlone(npc, ref chargingFlag);

                // Progress with the charge animation.
                chargeFlameTimer = MathHelper.Clamp(chargeFlameTimer + (chargingFlag == 1f ? 1f : -3f), 0f, 15f);
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
                case TwinsAttackState.RedirectingLasersAndFireRain:
                    DoBehavior_RedirectingLasersAndFireRain(npc, isSpazmatism, isRetinazer);
                    break;
                case TwinsAttackState.RedirectingLasersAndFlameCharge:
                    if (!DoBehavior_RedirectingLasersAndFlameCharge(npc, isSpazmatism, ref chargingFlag))
                        return false;
                    break;
                case TwinsAttackState.LazilyObserve:
                    Vector2 destination = Target.Center + new Vector2(isRetinazer ? -540f : 540f, -500f);

                    Vector2 hoverVelocity = npc.SafeDirectionTo(destination - npc.velocity) * 9f;
                    npc.SimpleFlyMovement(hoverVelocity, 0.16f);
                    npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(Target.Center) - MathHelper.PiOver2, MathHelper.TwoPi / 10f);
                    break;
            }

            // Progress with the charge animation.
            chargeFlameTimer = MathHelper.Clamp(chargeFlameTimer + (chargingFlag == 1f ? 1f : -3f), 0f, 15f);

            return false;
        }

        #region Specific Attacks
        public static void DoBehavior_ChargeRedirect(NPC npc, bool isSpazmatism)
        {
            Vector2 hoverDestination = Target.Center - Vector2.UnitY * 235f;
            hoverDestination.X += isSpazmatism.ToDirectionInt() * 620f;

            // Disable contact damage.
            npc.damage = 0;

            // Fly towards the destination.
            if (!npc.WithinRange(hoverDestination, 72f))
            {
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 24f, 0.8f);
                npc.rotation = npc.AngleTo(Target.Center) - MathHelper.PiOver2;
            }
            else
            {
                npc.velocity *= 0.925f;
                npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(Target.Center) - MathHelper.PiOver2, MathHelper.Pi / 12f);
            }

            // Relase some projectiles while hovering to pass the time.
            if (Main.netMode != NetmodeID.MultiplayerClient && UniversalAttackTimer % 30 == 29 && npc.WithinRange(hoverDestination, 160f))
            {
                if (isSpazmatism)
                {
                    float shootSpeed = MathHelper.Lerp(9f, 15.7f, 1f - CombinedLifeRatio);
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 shootVelocity = npc.SafeDirectionTo(Target.Center).RotatedBy(MathHelper.Lerp(-0.45f, 0.45f, i / 3f)) * shootSpeed;
                        Utilities.NewProjectileBetter(npc.Center + shootVelocity * 4f, shootVelocity, ProjectileID.CursedFlameHostile, 135, 0f);
                    }
                }
                else
                {
                    float shootSpeed = MathHelper.Lerp(10.75f, 16f, 1f - CombinedLifeRatio);
                    Vector2 shootVelocity = npc.SafeDirectionTo(Target.Center) * shootSpeed;
                    Utilities.NewProjectileBetter(npc.Center + shootVelocity * 4f, shootVelocity, ProjectileID.DeathLaser, 130, 0f);
                }
            }
        }

        public static void DoBehavior_DownwardCharge(NPC npc)
        {
            int chargeDelay = 30;
            float reelbackSpeed = 6f;
            float chargeSpeed = MathHelper.Lerp(16.5f, 23f, 1f - CombinedLifeRatio);
            if (BossRushEvent.BossRushActive)
                chargeSpeed *= 1.8f;

            if (UniversalAttackTimer < chargeDelay - 1)
            {
                npc.velocity = npc.SafeDirectionTo(Target.Center) * -reelbackSpeed;
                npc.rotation = npc.AngleTo(Target.Center) - MathHelper.PiOver2;
            }
            if (UniversalAttackTimer == chargeDelay)
            {
                npc.velocity = npc.SafeDirectionTo(Target.Center) * chargeSpeed;
                npc.rotation = npc.AngleTo(Target.Center) - MathHelper.PiOver2;
                Main.PlaySound(SoundID.Roar, npc.Center, 0);
            }
            if (UniversalAttackTimer > chargeDelay)
                npc.velocity *= 1.007f;
        }

        public static void DoBehavior_SwitchCharges(NPC npc, bool isSpazmatism, bool isRetinazer, ref float chargingFlag)
        {
            int redirectTime = 50;
            int chargeTime = 55;
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
                    Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(16f, npc.Distance(hoverDestination));
                    npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.1f);
                    npc.velocity = npc.velocity.MoveTowards(idealVelocity, 2.6f);
                }
                else
                {
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 36f, 3.7f);
                    npc.Center = npc.Center.MoveTowards(hoverDestination, 10f);
                }

                npc.rotation = npc.AngleTo(Target.Center) - MathHelper.PiOver2;

                // Disable contact damage while redirecting.
                npc.damage = 0;

                // Charge.
                if (chargeSpecificWrappedAttackTimer == redirectTime && willCharge)
                {
                    npc.damage = npc.defDamage;

                    float chargeSpeed = MathHelper.Lerp(23f, 28f, 1f - CombinedLifeRatio);
                    if (BossRushEvent.BossRushActive)
                        chargeSpeed *= 1.8f;
                    npc.velocity = npc.SafeDirectionTo(Target.Center + Target.velocity * 6.6f) * chargeSpeed;
                    npc.rotation = npc.AngleTo(Target.Center) - MathHelper.PiOver2;
                    npc.netUpdate = true;

                    Main.PlaySound(SoundID.Roar, npc.Center, 0);
                }
            }
            else if (willCharge)
                chargingFlag = 1f;
        }

        public static void DoBehavior_Spin(NPC npc, bool isSpazmatism, ref float chargingFlag)
        {
            int redirectTime = 60;
            int spinTime = 240;
            int spinSlowdownTime = 35;
            int reelbackTime = 30;
            int chargeTime = 55;
            float spinSlowdownInterpolant = Utils.InverseLerp(redirectTime + spinTime, redirectTime + spinTime - spinSlowdownTime, UniversalAttackTimer, true);
            float spinAngularVelocity = MathHelper.Lerp(MathHelper.ToRadians(1.5f), MathHelper.ToRadians(3f), 1f - CombinedLifeRatio);
            ref float spinRotation = ref npc.ai[0];
            ref float spinDirection = ref npc.ai[1];

            // Don't deal contact damage until charging.
            if (UniversalAttackTimer < redirectTime + spinTime)
                npc.damage = 0;

            // Initialize the spin rotation for both eyes.
            if (UniversalAttackTimer == 1)
            {
                spinRotation = Main.rand.NextFloat(MathHelper.TwoPi);

                NPC otherEye = GetMyOther(npc);
                if (otherEye != null)
                {
                    otherEye.ai[0] = spinRotation + MathHelper.Pi;
                    otherEye.netUpdate = true;
                }
                npc.netUpdate = true;
            }

            Vector2 destination = Target.Center + spinRotation.ToRotationVector2() * 610f;
            if (UniversalAttackTimer >= redirectTime && UniversalAttackTimer <= redirectTime + spinTime)
            {
                // Update the spin direction for both eyes.
                if (spinDirection == 0f && isSpazmatism)
                {
                    spinDirection = (Math.Cos(npc.AngleTo(Target.Center)) > 0).ToDirectionInt();
                    NPC otherEye = GetMyOther(npc);
                    if (otherEye != null)
                        otherEye.ai[1] = spinDirection;
                }

                // Increment the spin rotation.
                spinRotation += spinAngularVelocity * spinDirection * spinSlowdownInterpolant;

                // Relase some projectiles while spinning to pass the time.
                int fireRate = BossRushEvent.BossRushActive ? 18 : 35;
                if (Main.netMode != NetmodeID.MultiplayerClient && UniversalAttackTimer % fireRate == fireRate - 1)
                {
                    float shootSpeed = MathHelper.Lerp(8.5f, 11f, 1f - CombinedLifeRatio);
                    Vector2 shootVelocity = npc.SafeDirectionTo(Target.Center) * shootSpeed;
                    int projectileType = isSpazmatism ? ProjectileID.CursedFlameHostile : ProjectileID.DeathLaser;

                    int proj = Utilities.NewProjectileBetter(npc.Center + shootVelocity * 4f, shootVelocity, projectileType, 130, 0f);
                    Main.projectile[proj].tileCollide = false;
                }
            }

            // Adjust position for the spin.
            if (UniversalAttackTimer < redirectTime + spinTime)
            {
                npc.Center = Vector2.Lerp(npc.Center, destination, 0.033f);
                if (UniversalAttackTimer >= redirectTime)
                    npc.Center = Vector2.Lerp(npc.Center, destination, 0.08f);

                npc.velocity = Vector2.Zero;
                npc.rotation = npc.AngleTo(Target.Center) - MathHelper.PiOver2;
            }

            // Reel back.
            if (UniversalAttackTimer >= redirectTime + spinTime && UniversalAttackTimer < redirectTime + spinTime + reelbackTime)
            {
                npc.velocity = npc.SafeDirectionTo(Target.Center) * -6f;
                npc.rotation = npc.AngleTo(Target.Center) - MathHelper.PiOver2;
            }

            // And charge.
            if (UniversalAttackTimer == redirectTime + spinTime + reelbackTime)
            {
                float chargeSpeed = MathHelper.Lerp(21f, 26.5f, 1f - CombinedLifeRatio);
                if (BossRushEvent.BossRushActive)
                    chargeSpeed *= 1.8f;

                npc.velocity = npc.SafeDirectionTo(Target.Center + Target.velocity * 6f) * chargeSpeed;
                npc.rotation = npc.AngleTo(Target.Center) - MathHelper.PiOver2;
                Main.PlaySound(SoundID.Roar, npc.Center, 0);
            }

            if (UniversalAttackTimer >= redirectTime + spinTime + reelbackTime && UniversalAttackTimer < redirectTime + spinTime + reelbackTime + chargeTime)
                chargingFlag = 1f;
        }

        public static void DoBehavior_RedirectingLasersAndFireRain(NPC npc, bool isSpazmatism, bool isRetinazer)
        {
            int redirectTime = 60;
            int shootRate = isSpazmatism ? 8 : 22;

            // Redirect. Spazmatism hovers above the target while Retinazer hovers to the side in anticipation of a charge.
            // Contact damage is disabled during this.
            if (UniversalAttackTimer < redirectTime)
            {
                Vector2 destination = Target.Center - Vector2.UnitY * 455f;
                destination.X += isRetinazer.ToDirectionInt() * 1337f;

                if (!npc.WithinRange(destination, 48f))
                    npc.rotation = npc.AngleTo(Target.Center) - MathHelper.PiOver2;
                else
                    npc.rotation = npc.rotation.AngleTowards(isRetinazer ? MathHelper.PiOver2 : 0f, MathHelper.TwoPi / 20f);

                npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * 33f, 2.1f);
                npc.damage = 0;
            }

            // Charge and adjust rotation.
            if (UniversalAttackTimer == redirectTime)
            {
                npc.rotation = npc.rotation.AngleTowards(isRetinazer ? MathHelper.PiOver2 : 0f, MathHelper.TwoPi / 4f);
                npc.velocity = isRetinazer ? Vector2.UnitX * -17f : Vector2.UnitX * 17f;
            }

            // Release bursts of lasers and cursed fire.
            if (Main.netMode != NetmodeID.MultiplayerClient && UniversalAttackTimer > redirectTime && UniversalAttackTimer % shootRate == shootRate - 1)
            {
                float shootSpeed = isRetinazer ? 5f : 16f;
                if (BossRushEvent.BossRushActive)
                    shootSpeed *= 1.6f;
                Vector2 shootVelocity = (npc.rotation + MathHelper.PiOver2).ToRotationVector2() * shootSpeed;
                int projectileType = isRetinazer ? ModContent.ProjectileType<ScavengerLaser>() : ProjectileID.CursedFlameHostile;
                Utilities.NewProjectileBetter(npc.Center + shootVelocity * 7f, shootVelocity, projectileType, 135, 0f);
            }
        }

        public static bool DoBehavior_RedirectingLasersAndFlameCharge(NPC npc, bool isSpazmatism, ref float chargingFlag)
        {
            if (UniversalAttackTimer < 60f)
            {
                npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(Target.Center) - MathHelper.PiOver2, MathHelper.TwoPi / 10f);
                npc.velocity *= 0.95f;
                return false;
            }

            if (isSpazmatism)
            {
                int chargeTime = 90;
                ref float offsetAngle = ref npc.ai[0];
                ref float chargingTime = ref npc.ai[1];

                Vector2 destination = Target.Center - Vector2.UnitY.RotatedBy(offsetAngle) * 740f;
                if (npc.WithinRange(destination, 40f))
                {
                    offsetAngle = Main.rand.NextFloat(MathHelper.ToRadians(-34f), MathHelper.ToRadians(34f));
                    npc.Center = destination;
                    npc.velocity = npc.SafeDirectionTo(Target.Center + Target.velocity * 7f) * 21f;
                    npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
                    npc.noTileCollide = false;

                    if (chargingTime == 0f)
                        Main.PlaySound(SoundID.ForceRoar, (int)npc.Center.X, (int)npc.Center.Y, -1, 1f, 0f);
                    chargingTime++;
                }
                else if (chargingTime == 0f)
                {
                    npc.noTileCollide = npc.Bottom.Y > Target.Top.Y - 180f;
                    npc.velocity = npc.SafeDirectionTo(destination) * MathHelper.Lerp(npc.velocity.Length(), 14f, 0.15f);
                    npc.rotation = npc.AngleTo(Target.Center) - MathHelper.PiOver2;
                }
                else if (chargingTime > 0)
                {
                    chargingTime++;
                    if (chargingTime >= chargeTime)
                        chargingTime = 0f;
                    chargingFlag = 1f;

                    npc.damage += 35;

                    Tile tile = CalamityUtils.ParanoidTileRetrieval((int)npc.Center.X / 16, (int)npc.Center.Y / 16);
                    bool platformFuck = (TileID.Sets.Platforms[tile.type] || Main.tileSolidTop[tile.type]) && tile.nactive() && npc.Center.Y > Target.Center.Y;
                    if (platformFuck || Collision.SolidCollision(npc.position, npc.width, npc.height) || npc.Center.Y > Target.Center.Y + 800f)
                    {
                        chargingTime = 0f;
                        Collision.HitTiles(npc.position + npc.velocity, npc.velocity, npc.width, npc.height);
                        npc.velocity = Vector2.Zero;
                        npc.netUpdate = true;

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int totalProjectiles = 18;
                            for (int i = 0; i < totalProjectiles; i++)
                            {
                                Vector2 shootVelocity = npc.SafeDirectionTo(Target.Center).RotatedBy(MathHelper.TwoPi * i / totalProjectiles) * 14.5f;
                                if (BossRushEvent.BossRushActive)
                                    shootVelocity *= 2.2f;
                                int fire = Utilities.NewProjectileBetter(npc.Center, shootVelocity, ProjectileID.CursedFlameHostile, 140, 0f);
                                Main.projectile[fire].ignoreWater = true;
                            }
                        }
                    }
                }
            }
            else
            {
                Vector2 destination = Target.Center - Vector2.UnitY * 500f;
                if (npc.WithinRange(destination, 40f))
                {
                    npc.Center = destination;
                    npc.velocity = npc.SafeDirectionTo(Target.Center + Target.velocity * 7f) * 21f;
                    npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
                    npc.noTileCollide = false;

                    int fireRate = 25;
                    if (Main.netMode != NetmodeID.MultiplayerClient && UniversalAttackTimer % fireRate == fireRate - 1)
                    {
                        float shootSpeed = 8.5f;
                        Vector2 shootVelocity = (npc.rotation + MathHelper.PiOver2).ToRotationVector2() * shootSpeed;
                        Utilities.NewProjectileBetter(npc.Center + shootVelocity * 12f, shootVelocity, ProjectileID.DeathLaser, 130, 0f);
                    }
                    else
                    {
                        Vector2 toAimTowards = Target.Center + Target.velocity * 30f;
                        npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(toAimTowards) - MathHelper.PiOver2, MathHelper.TwoPi / 80f);
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient && UniversalAttackTimer % 50f == 49f)
                        Utilities.NewProjectileBetter(npc.Center, Vector2.UnitY * -9f, ModContent.ProjectileType<ScavengerLaser>(), 145, 0f);
                }
                else
                {
                    npc.velocity = npc.SafeDirectionTo(destination) * MathHelper.Lerp(npc.velocity.Length(), 23f, 0.15f);
                    npc.rotation = npc.AngleTo(Target.Center) - MathHelper.PiOver2;
                }
            }

            return true;
        }
        #endregion Specific Attacks

        #region Retinazer
        public enum RetinazerAttackState
        {
            LaserBurstHover,
            LaserBarrage,
            DanceOfLightnings
        }

        public static void DoAI_RetinazerAlone(NPC npc, ref float chargingFlag)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float attackTimer = ref npc.Infernum().ExtraAI[10];
            ref float attackState = ref npc.Infernum().ExtraAI[11];
            ref float lightningAttackTimer = ref npc.Infernum().ExtraAI[12];
            ref float burstCounter = ref npc.Infernum().ExtraAI[13];

            int lightningShootRate = (int)MathHelper.Lerp(300, 150, Utils.InverseLerp(0.5f, 0.1f, lifeRatio));
            if (Main.netMode != NetmodeID.MultiplayerClient && lightningAttackTimer >= lightningShootRate)
            {
                Utilities.NewProjectileBetter(Target.Center + new Vector2(Main.rand.NextFloat(600f, 960f) * Math.Sign(Target.velocity.X), -1400f), Vector2.UnitY, ModContent.ProjectileType<LightningTelegraph>(), 0, 0f);
                lightningAttackTimer = 0f;
            }

            lightningAttackTimer++;

            switch ((RetinazerAttackState)(int)attackState)
            {
                case RetinazerAttackState.LaserBurstHover:
                    List<Vector2> potentialDestinations = new List<Vector2>()
                    {
                        Target.Center + Vector2.UnitX * -600f,
                        Target.Center + Vector2.UnitX * 600f,
                        Target.Center + Vector2.UnitY * -600f,
                        Target.Center + Vector2.UnitY * 600f,
                    };
                    float minDistance = 99999999f;
                    Vector2 destination = npc.Center;
                    foreach (Vector2 potentialDestination in potentialDestinations)
                    {
                        if (npc.DistanceSQ(potentialDestination) < minDistance)
                        {
                            destination = potentialDestination;
                            minDistance = npc.DistanceSQ(potentialDestination);
                        }
                    }

                    npc.rotation = npc.AngleTo(Target.Center) - MathHelper.PiOver2;

                    float hoverSpeed = MathHelper.Lerp(14f, 27f, 1f - lifeRatio);
                    float hoverAcceleration = MathHelper.Lerp(0.35f, 0.9f, 1f - lifeRatio);
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * hoverSpeed, hoverAcceleration);

                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 60f == 59f)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            Vector2 laserVelocity = npc.SafeDirectionTo(Target.Center).RotatedBy(MathHelper.Lerp(-0.5f, 0.5f, i / 2f)) * 9f;
                            if (BossRushEvent.BossRushActive)
                                laserVelocity *= 2.25f;
                            int laser = Utilities.NewProjectileBetter(npc.Center + laserVelocity * 8f, laserVelocity, ProjectileID.DeathLaser, 140, 0f);
                            Main.projectile[laser].tileCollide = false;
                        }
                        burstCounter++;

                        if (burstCounter > 2)
                        {
                            if (Main.rand.NextBool(3) || burstCounter >= 4)
                            {
                                attackTimer = 0f;
                                burstCounter = 0f;
                                attackState = Main.rand.NextBool() ? (int)RetinazerAttackState.LaserBarrage : (int)RetinazerAttackState.DanceOfLightnings;
                                npc.netUpdate = true;
                            }
                        }
                    }
                    break;
                case RetinazerAttackState.LaserBarrage:
                    destination = Target.Center - Vector2.UnitY * 400f;
                    destination.X -= Math.Sign(Target.Center.X - npc.Center.X) * 1100f;
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * 28f, 1.1f);

                    if (npc.WithinRange(destination, 38f))
                    {
                        npc.Center = destination;
                        npc.velocity = Vector2.Zero;
                    }

                    int barrageBurstTime = 120;
                    if (attackTimer < AimedDeathray.TelegraphTime + 30f)
                        npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(Target.Center + Target.velocity * 27f) - MathHelper.PiOver2, 0.1f);

                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer < barrageBurstTime && attackTimer % 15f == 14f)
                    {
                        Vector2 laserVelocity = npc.SafeDirectionTo(Target.Center).RotatedByRandom(MathHelper.ToRadians(10f)) * 10f;
                        if (BossRushEvent.BossRushActive)
                            laserVelocity *= 2.1f;
                        int laser = Utilities.NewProjectileBetter(npc.Center + laserVelocity * 3.6f, laserVelocity, ProjectileID.DeathLaser, 140, 0f);
                        Main.projectile[laser].tileCollide = false;
                    }
                    if (attackTimer == 30f && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int deathRay = Utilities.NewProjectileBetter(npc.Center + npc.SafeDirectionTo(Target.Center) * 48f, npc.SafeDirectionTo(Target.Center), ModContent.ProjectileType<AimedDeathray>(), 140, 0f);
                        Main.projectile[deathRay].ai[1] = npc.whoAmI;
                    }

                    if (attackTimer >= AimedDeathray.TelegraphTime + AimedDeathray.LaserDamageTime + 45f)
                    {
                        attackTimer = 0f;
                        attackState = (int)RetinazerAttackState.DanceOfLightnings;
                        npc.netUpdate = true;
                    }
                    break;
                case RetinazerAttackState.DanceOfLightnings:
                    if (attackTimer <= 60f)
                    {
                        destination = Target.Center - Vector2.UnitY * 500f;
                        npc.velocity = npc.SafeDirectionTo(destination) * 23f;

                        if (npc.WithinRange(destination, 32f))
                        {
                            npc.Center = destination;
                            npc.velocity = Vector2.Zero;
                        }
                        npc.rotation = npc.AngleTo(Target.Center) - MathHelper.PiOver2;
                        npc.Opacity = 1f - attackTimer / 60f;
                    }

                    if (attackTimer > 60f && attackTimer < 240f)
                        npc.velocity *= 0.97f;

                    if (attackTimer >= 240f && attackTimer <= 270f)
                    {
                        npc.Opacity = Utils.InverseLerp(240f, 270f, attackTimer, true);
                        npc.rotation = npc.AngleTo(Target.Center) - MathHelper.PiOver2;
                    }
                    if (attackTimer == 270f)
                    {
                        npc.velocity = npc.SafeDirectionTo(Target.Center - Vector2.UnitY * 250f) * 19f;
                        if (BossRushEvent.BossRushActive)
                            npc.velocity *= 1.8f;
                        npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
                        npc.netUpdate = true;
                    }
                    if (attackTimer >= 270f)
                    {
                        chargingFlag = 1f;
                        if (attackTimer < 400f && attackTimer % 20f == 19f && npc.velocity.Length() > 13f)
                        {
                            Main.PlaySound(SoundID.Item33, npc.Center);
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                                Utilities.NewProjectileBetter(npc.Center, Vector2.UnitY * -9f, ModContent.ProjectileType<ScavengerLaser>(), 145, 0f);
                        }
                    }

                    npc.dontTakeDamage = npc.Opacity <= 0f;

                    if (attackTimer >= 450f)
                    {
                        attackTimer = 0f;
                        attackState = (int)RetinazerAttackState.LaserBurstHover;
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
            CursedFlameCarpetBomb
        }

        public static void DoAI_SpazmatismAlone(NPC npc, ref float chargingFlag)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float attackTimer = ref npc.Infernum().ExtraAI[10];
            ref float attackState = ref npc.Infernum().ExtraAI[11];
            ref float fireballShootTimer = ref npc.Infernum().ExtraAI[12];
            ref float orbResummonTimer = ref npc.Infernum().ExtraAI[16];

            int fireballShootRate = (int)MathHelper.Lerp(300, 120, Utils.InverseLerp(0.5f, 0.1f, lifeRatio));
            if (Main.netMode != NetmodeID.MultiplayerClient && fireballShootTimer >= fireballShootRate)
            {
                Utilities.NewProjectileBetter(Target.Center - Vector2.UnitX * 1300f, Vector2.UnitX * 11f, ModContent.ProjectileType<CursedFlameBurstTelegraph>(), 0, 0f);
                Utilities.NewProjectileBetter(Target.Center + Vector2.UnitX * 1300f, Vector2.UnitX * -11f, ModContent.ProjectileType<CursedFlameBurstTelegraph>(), 0, 0f);
                fireballShootTimer = 0f;
            }

            if (!NPC.AnyNPCs(ModContent.NPCType<CursedOrb>()))
                orbResummonTimer++;
            else
                orbResummonTimer = 0f;

            if (Main.netMode != NetmodeID.MultiplayerClient && orbResummonTimer > 900f)
            {
                NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<CursedOrb>(), 0, npc.whoAmI);
                orbResummonTimer = 0f;
            }

            fireballShootTimer++;

            switch ((SpazmatismAttackState)(int)attackState)
            {
                case SpazmatismAttackState.MobileChargePhase:
                    int hoverTime = 90;
                    int chargeDelayTime = (int)MathHelper.SmoothStep(45f, 25f, 1f - lifeRatio);
                    int chargeTime = (int)MathHelper.SmoothStep(60f, 40f, 1f - lifeRatio);
                    int slowdownTime = 35;
                    ref float chargeDestinationX = ref npc.Infernum().ExtraAI[13];
                    ref float chargeDestinationY = ref npc.Infernum().ExtraAI[14];

                    // Lazily hover next to the player.
                    if (attackTimer < hoverTime)
                    {
                        float hoverSpeed = MathHelper.SmoothStep(10.5f, 16.7f, 1f - lifeRatio);
                        float hoverAcceleration = MathHelper.SmoothStep(0.46f, 0.9f, 1f - lifeRatio);
                        if (BossRushEvent.BossRushActive)
                        {
                            hoverSpeed *= 2f;
                            hoverAcceleration *= 2f;
                        }

                        Vector2 destination = Target.Center;
                        destination.X -= Math.Sign(Target.Center.X - npc.Center.X) * 600f;
                        destination.Y -= npc.velocity.Length() * 20f;
                        npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * hoverSpeed, hoverAcceleration);
                        npc.rotation = npc.AngleTo(Target.Center) - MathHelper.PiOver2;
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

                        float aimAngleToDestination = npc.AngleTo(new Vector2(chargeDestinationX, chargeDestinationY)) - MathHelper.PiOver2;
                        npc.rotation = npc.rotation.AngleLerp(aimAngleToDestination, 0.15f);
                        npc.velocity *= 0.925f;
                    }

                    // Charge.
                    if (attackTimer == hoverTime + chargeDelayTime)
                    {
                        Vector2 chargeDestination = new Vector2(chargeDestinationX, chargeDestinationY);
                        npc.velocity = npc.SafeDirectionTo(chargeDestination);
                        npc.velocity *= 18f + npc.Distance(Target.Center) * 0.01f;
                        if (BossRushEvent.BossRushActive)
                            npc.velocity *= 1.8f;

                        npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
                        npc.netUpdate = true;

                        // Roar.
                        Main.PlaySound(SoundID.Roar, (int)Target.Center.X, (int)Target.Center.Y, 0);
                    }

                    // And slow down.
                    if (attackTimer >= hoverTime + chargeDelayTime + chargeTime)
                    {
                        npc.velocity *= 0.94f;

                        // Roar and release fire outward.
                        if (attackTimer == hoverTime + chargeDelayTime + chargeTime + slowdownTime / 2)
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                for (int i = 0; i < 6; i++)
                                {
                                    Vector2 shootDirection = (MathHelper.TwoPi * i / 6f).ToRotationVector2();
                                    Utilities.NewProjectileBetter(npc.Center + shootDirection * 50f, shootDirection * 16f, ModContent.ProjectileType<CursedFlameBurst>(), 140, 0f);
                                }
                            }

                            Main.PlaySound(SoundID.Roar, (int)Target.Center.X, (int)Target.Center.Y, 0);
                            Main.PlaySound(SoundID.DD2_FlameburstTowerShot, Target.Center);
                        }

                        npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(Target.Center) - MathHelper.PiOver2, 0.15f);

                        // And finally go to the next AI state.
                        if (attackTimer >= hoverTime + chargeDelayTime + chargeTime + slowdownTime)
                        {
                            chargeDestinationX = chargeDestinationY = 0f;
                            attackTimer = 0;
                            attackState = (int)SpazmatismAttackState.HellfireBursts;
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
                        float hoverSpeed = MathHelper.SmoothStep(15.5f, 22f, 1f - lifeRatio);
                        float hoverAcceleration = MathHelper.SmoothStep(0.56f, 0.9f, 1f - lifeRatio);
                        Vector2 destination = Target.Center;
                        destination.X -= Math.Sign(Target.Center.X - npc.Center.X) * 570f;
                        destination.Y -= npc.velocity.Length() * 20f;
                        npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * hoverSpeed, hoverAcceleration);
                        npc.rotation = npc.AngleTo(Target.Center) - MathHelper.PiOver2;
                    }

                    // Slow down and look at the player.
                    else if (attackTimer >= hoverTime)
                    {
                        npc.velocity *= 0.94f;
                        npc.rotation = npc.AngleTo(Target.Center) - MathHelper.PiOver2;
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
                                attackTimer = 0;
                                attackState = (int)SpazmatismAttackState.CursedFlameCarpetBomb;
                                npc.netUpdate = true;
                            }

                            // Release bursts of fire.
                            else
                            {
                                if (Main.netMode != NetmodeID.MultiplayerClient)
                                {
                                    for (int i = 0; i < fireballsPerBurst; i++)
                                    {
                                        float offsetAngle = MathHelper.Lerp(-0.87f, 0.87f, i / (float)fireballsPerBurst);
                                        Vector2 shootVelocity = npc.SafeDirectionTo(Target.Center).RotatedBy(offsetAngle) * 16f;
                                        if (BossRushEvent.BossRushActive)
                                            shootVelocity *= 1.8f;
                                        Utilities.NewProjectileBetter(npc.Center + shootVelocity * 5f, shootVelocity, ModContent.ProjectileType<HomingCursedFlameBurst>(), 145, 0f);
                                    }
                                }

                                Main.PlaySound(SoundID.DD2_BetsyFireballShot, Target.Center);
                            }
                        }
                    }
                    break;
                case SpazmatismAttackState.CursedFlameCarpetBomb:
                    int redirectTime = 240;
                    int carpetBombTime = 90;
                    int carpetBombRate = 7;
                    float carpetBombSpeed = MathHelper.SmoothStep(13f, 19f, 1f - lifeRatio);
                    float carpetBombChargeSpeed = MathHelper.SmoothStep(17f, 22f, 1f - lifeRatio);
                    if (BossRushEvent.BossRushActive)
                    {
                        carpetBombSpeed *= 1.8f;
                        carpetBombChargeSpeed *= 1.6f;
                    }

                    if (attackTimer < redirectTime)
                    {
                        Vector2 destination = Target.Center - Vector2.UnitY * 380f;
                        float idealAngle = npc.AngleTo(destination) - MathHelper.PiOver2;
                        destination.X -= (Target.Center.X > npc.Center.X).ToDirectionInt() * 870f;
                        npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(destination) * 16f, 0.056f);

                        npc.rotation = npc.rotation.AngleLerp(idealAngle, 0.08f);

                        if (npc.WithinRange(destination, 24f) && attackTimer < redirectTime - 75f)
                        {
                            attackTimer = redirectTime - 75f;
                            npc.netUpdate = true;
                        }

                        // Create cursed dust at the mouth.
                        if (attackTimer > redirectTime - 75f)
                        {
                            Dust cursedflame = Dust.NewDustPerfect(npc.Center + (npc.rotation + MathHelper.PiOver2).ToRotationVector2() * 45f, 267);
                            cursedflame.color = Color.Lerp(Color.Green, Color.GreenYellow, Main.rand.NextFloat());
                            cursedflame.velocity = (npc.rotation + MathHelper.PiOver2 + Main.rand.NextFloat(-0.5f, 0.5f)).ToRotationVector2();
                            cursedflame.velocity *= Main.rand.NextFloat(2f, 5f);
                            cursedflame.scale *= 1.2f;
                            cursedflame.noGravity = true;
                        }
                    }

                    if (attackTimer == redirectTime)
                    {
                        Vector2 flyVelocity = npc.SafeDirectionTo(Target.Center);
                        flyVelocity.Y *= 0.2f;
                        flyVelocity = flyVelocity.SafeNormalize(Vector2.UnitX * (npc.velocity.X > 0).ToDirectionInt());
                        npc.velocity = flyVelocity * carpetBombChargeSpeed;
                        npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;

                        // Roar and begin carpet bombing.
                        Main.PlaySound(SoundID.Roar, (int)npc.position.X, (int)npc.position.Y, 0, 1f, 0f);
                    }

                    if (attackTimer > redirectTime)
                        npc.velocity = (npc.rotation + MathHelper.PiOver2).ToRotationVector2() * carpetBombChargeSpeed;

                    if (attackTimer > redirectTime && attackTimer % carpetBombRate == carpetBombRate - 1)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 spawnPosition = npc.Center + npc.velocity.SafeNormalize(Vector2.Zero) * 120f;
                            Vector2 shootVelocity = npc.velocity.SafeNormalize((npc.rotation + MathHelper.PiOver2).ToRotationVector2()) * carpetBombSpeed;
                            Utilities.NewProjectileBetter(spawnPosition, shootVelocity, ModContent.ProjectileType<CursedBomb>(), 145, 0f);
                        }
                        Main.PlaySound(SoundID.DD2_BetsyFireballShot, Target.Center);
                    }

                    if (attackTimer > redirectTime + carpetBombTime)
                    {
                        attackTimer = 0;
                        attackState = (int)SpazmatismAttackState.MobileChargePhase;
                        npc.netUpdate = true;
                    }
                    break;
            }

            attackTimer++;
        }
        #endregion

        #endregion

        #region Helper Methods

        public static bool PersonallyInPhase2(NPC npc)
        {
            return npc.Infernum().ExtraAI[0] >= Phase2TransitionTime * 0.5f;
        }

        public static void ChooseNextAIState()
        {
            if (InPhase2)
            {
                switch (UniversalStateIndex)
                {
                    case 0:
                        CurrentAttackState = TwinsAttackState.ChargeRedirect;
                        break;
                    case 1:
                        CurrentAttackState = TwinsAttackState.DownwardCharge;
                        break;
                    case 2:
                        CurrentAttackState = TwinsAttackState.DownwardCharge;
                        break;
                    case 3:
                        CurrentAttackState = TwinsAttackState.Spin;
                        break;
                    case 4:
                        CurrentAttackState = TwinsAttackState.RedirectingLasersAndFireRain;
                        break;
                    case 5:
                        CurrentAttackState = TwinsAttackState.SwitchCharges;
                        break;
                    case 6:
                        CurrentAttackState = TwinsAttackState.Spin;
                        break;
                    case 7:
                        CurrentAttackState = TwinsAttackState.ChargeRedirect;
                        break;
                    case 8:
                        CurrentAttackState = TwinsAttackState.DownwardCharge;
                        break;
                    case 9:
                        CurrentAttackState = TwinsAttackState.DownwardCharge;
                        break;
                    case 10:
                        CurrentAttackState = TwinsAttackState.SwitchCharges;
                        break;
                    case 11:
                        CurrentAttackState = TwinsAttackState.RedirectingLasersAndFireRain;
                        break;

                    default:
                        CurrentAttackState = TwinsAttackState.ChargeRedirect;
                        break;
                }

                if (CombinedLifeRatio < Phase3LifeRatioThreshold && UniversalStateIndex % 4 == 3)
                    CurrentAttackState = TwinsAttackState.RedirectingLasersAndFlameCharge;
            }
            else
            {
                switch (UniversalStateIndex)
                {
                    case 0:
                        CurrentAttackState = TwinsAttackState.ChargeRedirect;
                        break;
                    case 1:
                        CurrentAttackState = TwinsAttackState.DownwardCharge;
                        break;
                    case 2:
                        CurrentAttackState = TwinsAttackState.DownwardCharge;
                        break;
                    case 3:
                        CurrentAttackState = TwinsAttackState.Spin;
                        break;
                    case 4:
                        CurrentAttackState = TwinsAttackState.ChargeRedirect;
                        break;
                    case 5:
                        CurrentAttackState = TwinsAttackState.DownwardCharge;
                        break;
                    case 6:
                        CurrentAttackState = TwinsAttackState.DownwardCharge;
                        break;
                    case 7:
                        CurrentAttackState = TwinsAttackState.SwitchCharges;
                        break;
                    case 8:
                        CurrentAttackState = TwinsAttackState.ChargeRedirect;
                        break;
                    case 9:
                        CurrentAttackState = TwinsAttackState.DownwardCharge;
                        break;
                    case 10:
                        CurrentAttackState = TwinsAttackState.Spin;
                        break;
                    case 11:
                        CurrentAttackState = TwinsAttackState.SwitchCharges;
                        break;

                    default:
                        CurrentAttackState = TwinsAttackState.ChargeRedirect;
                        break;
                }
            }

            if (UniversalStateIndex % 5 == 4 && !InPhase2 && !BossRushEvent.BossRushActive)
                CurrentAttackState = TwinsAttackState.LazilyObserve;
        }

        public static NPC GetMyOther(NPC npc)
        {
            if (npc.type == NPCID.Retinazer)
                return SpazmatismIndex == -1 ? Main.npc[NPC.FindFirstNPC(NPCID.Spazmatism)] : Main.npc[SpazmatismIndex];
            if (npc.type == NPCID.Spazmatism)
                return RetinazerIndex == -1 ? Main.npc[NPC.FindFirstNPC(NPCID.Retinazer)] : Main.npc[RetinazerIndex];
            return null;
        }
        #endregion
    }
}
