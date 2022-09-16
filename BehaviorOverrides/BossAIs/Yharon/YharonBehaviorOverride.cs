using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Projectiles.Boss;
using InfernumMode.GlobalInstances;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.GameContent;
using Terraria.GameContent.Events;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

using YharonBoss = CalamityMod.NPCs.Yharon.Yharon;

namespace InfernumMode.BehaviorOverrides.BossAIs.Yharon
{
    public class YharonBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<YharonBoss>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw | NPCOverrideContext.NPCFindFrame;

        #region Enumerations
        public enum YharonAttackType
        {
            SpawnEffects,
            Charge,
            FastCharge,
            FireballBurst,
            FlamethrowerAndMeteors,
            FlarenadoAndDetonatingFlameSpawn,
            FireTrailCharge,
            InfernadoAndFireShotgunBreath,
            MassiveInfernadoSummon,
            TeleportingCharge,
            SummonFlareRing,
            CarpetBombing,
            PhoenixSupercharge,
            HeatFlashRing,
            VorticesOfFlame,
            FinalDyingRoar
        }

        public enum YharonFrameDrawingType
        {
            None,
            FlapWings,
            IdleWings,
            Roar,
            OpenMouth,
        }
        #endregion

        #region Pattern Lists
        public static readonly YharonAttackType[] Subphase1Pattern = new YharonAttackType[]
        {
            YharonAttackType.Charge,
            YharonAttackType.Charge,
            YharonAttackType.Charge,
            YharonAttackType.Charge,
            YharonAttackType.FastCharge,
            YharonAttackType.FireballBurst,
            YharonAttackType.Charge,
            YharonAttackType.Charge,
            YharonAttackType.Charge,
            YharonAttackType.Charge,
            YharonAttackType.Charge,
            YharonAttackType.FlamethrowerAndMeteors,
            YharonAttackType.Charge,
            YharonAttackType.Charge,
            YharonAttackType.Charge,
            YharonAttackType.Charge,
            YharonAttackType.FastCharge,
            YharonAttackType.FlamethrowerAndMeteors
        };

        public static readonly YharonAttackType[] Subphase2Pattern = new YharonAttackType[]
        {
            YharonAttackType.Charge,
            YharonAttackType.FastCharge,
            YharonAttackType.Charge,
            YharonAttackType.FastCharge,
            YharonAttackType.Charge,
            YharonAttackType.FastCharge,
            YharonAttackType.FlarenadoAndDetonatingFlameSpawn,
            YharonAttackType.FlamethrowerAndMeteors,
            YharonAttackType.Charge,
            YharonAttackType.FastCharge,
            YharonAttackType.Charge,
            YharonAttackType.FastCharge,
            YharonAttackType.Charge,
            YharonAttackType.FastCharge,
            YharonAttackType.FireballBurst,
            YharonAttackType.FlamethrowerAndMeteors,
            YharonAttackType.Charge,
            YharonAttackType.FastCharge,
            YharonAttackType.Charge,
            YharonAttackType.FastCharge,
            YharonAttackType.Charge,
            YharonAttackType.FastCharge,
            YharonAttackType.FlarenadoAndDetonatingFlameSpawn,
            YharonAttackType.FireballBurst,
        };

        public static readonly YharonAttackType[] Subphase3Pattern = new YharonAttackType[]
        {
            YharonAttackType.FastCharge,
            YharonAttackType.FireTrailCharge,
            YharonAttackType.FastCharge,
            YharonAttackType.FireTrailCharge,
            YharonAttackType.FastCharge,
            YharonAttackType.FlamethrowerAndMeteors,
            YharonAttackType.MassiveInfernadoSummon,
            YharonAttackType.FireballBurst,
            YharonAttackType.FlarenadoAndDetonatingFlameSpawn,
            YharonAttackType.InfernadoAndFireShotgunBreath,
            YharonAttackType.FastCharge,
            YharonAttackType.FastCharge,
            YharonAttackType.FireTrailCharge,
            YharonAttackType.FastCharge,
            YharonAttackType.FireTrailCharge,
            YharonAttackType.FastCharge,
            YharonAttackType.FlamethrowerAndMeteors,
            YharonAttackType.MassiveInfernadoSummon,
            YharonAttackType.InfernadoAndFireShotgunBreath,
            YharonAttackType.FireballBurst,
            YharonAttackType.FireTrailCharge,
            YharonAttackType.FastCharge,
            YharonAttackType.FireTrailCharge,
            YharonAttackType.FastCharge,
            YharonAttackType.FlamethrowerAndMeteors,
            YharonAttackType.MassiveInfernadoSummon,
            YharonAttackType.FlarenadoAndDetonatingFlameSpawn,
            YharonAttackType.FireballBurst,
            YharonAttackType.InfernadoAndFireShotgunBreath
        };

        public static readonly YharonAttackType[] Subphase4Pattern = new YharonAttackType[]
        {
            YharonAttackType.FastCharge,
            YharonAttackType.TeleportingCharge,
            YharonAttackType.TeleportingCharge,
            YharonAttackType.FlamethrowerAndMeteors,
            YharonAttackType.InfernadoAndFireShotgunBreath,
            YharonAttackType.FastCharge,
            YharonAttackType.FastCharge,
            YharonAttackType.TeleportingCharge,
            YharonAttackType.TeleportingCharge,
            YharonAttackType.FastCharge,
            YharonAttackType.FastCharge,
            YharonAttackType.MassiveInfernadoSummon,
            YharonAttackType.FastCharge,
            YharonAttackType.TeleportingCharge,
            YharonAttackType.TeleportingCharge,
            YharonAttackType.FastCharge,
            YharonAttackType.FlamethrowerAndMeteors,
            YharonAttackType.InfernadoAndFireShotgunBreath,
            YharonAttackType.FastCharge,
            YharonAttackType.FastCharge,
            YharonAttackType.TeleportingCharge,
            YharonAttackType.TeleportingCharge,
            YharonAttackType.FastCharge,
            YharonAttackType.MassiveInfernadoSummon,
            YharonAttackType.FastCharge,
        };

        public static readonly YharonAttackType[] Subphase5Pattern = new YharonAttackType[]
        {
            YharonAttackType.SummonFlareRing,
            YharonAttackType.CarpetBombing,
            YharonAttackType.InfernadoAndFireShotgunBreath,
            YharonAttackType.FireTrailCharge,
            YharonAttackType.FireTrailCharge,
            YharonAttackType.FireTrailCharge,
            YharonAttackType.FastCharge,
            YharonAttackType.TeleportingCharge,
            YharonAttackType.FastCharge,
            YharonAttackType.FireTrailCharge,
            YharonAttackType.FireTrailCharge,
            YharonAttackType.FireTrailCharge,
            YharonAttackType.CarpetBombing,
            YharonAttackType.SummonFlareRing,
            YharonAttackType.FastCharge,
            YharonAttackType.FireTrailCharge,
            YharonAttackType.FireTrailCharge,
            YharonAttackType.FireTrailCharge,
            YharonAttackType.MassiveInfernadoSummon,
            YharonAttackType.FastCharge,
            YharonAttackType.TeleportingCharge,
            YharonAttackType.FastCharge,
            YharonAttackType.MassiveInfernadoSummon,
        };

        public static readonly YharonAttackType[] Subphase6Pattern = new YharonAttackType[]
        {
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.MassiveInfernadoSummon,
            YharonAttackType.CarpetBombing,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.InfernadoAndFireShotgunBreath,
            YharonAttackType.MassiveInfernadoSummon,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.InfernadoAndFireShotgunBreath,
            YharonAttackType.CarpetBombing,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.InfernadoAndFireShotgunBreath,
            YharonAttackType.MassiveInfernadoSummon,
            YharonAttackType.CarpetBombing,
        };

        public static readonly YharonAttackType[] Subphase7Pattern = new YharonAttackType[]
        {
            YharonAttackType.InfernadoAndFireShotgunBreath,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.HeatFlashRing,
            YharonAttackType.CarpetBombing,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.HeatFlashRing,
            YharonAttackType.CarpetBombing,
        };

        public static readonly YharonAttackType[] Subphase8Pattern = new YharonAttackType[]
        {
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.HeatFlashRing,
            YharonAttackType.VorticesOfFlame,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.HeatFlashRing,
            YharonAttackType.InfernadoAndFireShotgunBreath,
            YharonAttackType.VorticesOfFlame,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.CarpetBombing,
            YharonAttackType.VorticesOfFlame,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.VorticesOfFlame,
            YharonAttackType.InfernadoAndFireShotgunBreath,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.HeatFlashRing,
            YharonAttackType.CarpetBombing,
        };

        public static readonly YharonAttackType[] Subphase9Pattern = new YharonAttackType[]
        {
            YharonAttackType.PhoenixSupercharge,
        };

        public static readonly YharonAttackType[] LastSubphasePattern = new YharonAttackType[]
        {
            YharonAttackType.FinalDyingRoar,
        };

        public static bool InSecondPhase
        {
            get
            {
                if (GlobalNPCOverrides.Yharon == -1 || !Main.npc[GlobalNPCOverrides.Yharon].active)
                    return false;

                NPC yharon = Main.npc[GlobalNPCOverrides.Yharon];
                return yharon.Infernum().ExtraAI[HasEnteredPhase2Index] == 1f;
            }
            set
            {
                if (GlobalNPCOverrides.Yharon == -1 || !Main.npc[GlobalNPCOverrides.Yharon].active)
                    return;

                Main.npc[GlobalNPCOverrides.Yharon].Infernum().ExtraAI[HasEnteredPhase2Index] = value.ToInt();
            }
        }

        public const float Subphase2LifeRatio = 0.9375f;

        public const float Subphase3LifeRatio = 0.825f;

        public const float Subphase4LifeRatio = 0.675f;

        public const float Subphase5LifeRatio = Phase2LifeRatio;

        public const float Subphase6LifeRatio = 0.4f;

        public const float Subphase7LifeRatio = 0.3f;

        public const float Subphase8LifeRatio = 0.2f;

        public const float Subphase9LifeRatio = 0.1f;

        public const float Subphase10LifeRatio = 0.025f;

        public static readonly Dictionary<YharonAttackType[], Func<NPC, bool>> SubphaseTable = new()
        {
            [Subphase1Pattern] = (npc) => npc.life / (float)npc.lifeMax > Subphase2LifeRatio && !InSecondPhase,
            [Subphase2Pattern] = (npc) => npc.life / (float)npc.lifeMax > Subphase3LifeRatio && npc.life / (float)npc.lifeMax <= Subphase2LifeRatio && !InSecondPhase,
            [Subphase3Pattern] = (npc) => npc.life / (float)npc.lifeMax > Subphase4LifeRatio && npc.life / (float)npc.lifeMax <= Subphase3LifeRatio && !InSecondPhase,
            [Subphase4Pattern] = (npc) => npc.life / (float)npc.lifeMax > Subphase5LifeRatio && npc.life / (float)npc.lifeMax <= Subphase4LifeRatio && !InSecondPhase,

            [Subphase5Pattern] = (npc) => (npc.life / (float)npc.lifeMax > Subphase6LifeRatio || npc.Infernum().ExtraAI[InvincibilityTimerIndex] > 0f) && InSecondPhase,
            [Subphase6Pattern] = (npc) => npc.life / (float)npc.lifeMax > Subphase7LifeRatio && npc.life / (float)npc.lifeMax <= Subphase6LifeRatio && InSecondPhase,
            [Subphase7Pattern] = (npc) => npc.life / (float)npc.lifeMax > Subphase8LifeRatio && npc.life / (float)npc.lifeMax <= Subphase7LifeRatio && InSecondPhase,
            [Subphase8Pattern] = (npc) => npc.life / (float)npc.lifeMax > Subphase9LifeRatio && npc.life / (float)npc.lifeMax <= Subphase8LifeRatio && InSecondPhase,
            [Subphase9Pattern] = (npc) => npc.life / (float)npc.lifeMax > Subphase10LifeRatio && npc.life / (float)npc.lifeMax <= Subphase9LifeRatio && InSecondPhase,
            [LastSubphasePattern] = (npc) => npc.life / (float)npc.lifeMax <= Subphase10LifeRatio && InSecondPhase,
        };

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Subphase2LifeRatio,
            Subphase3LifeRatio,
            Subphase4LifeRatio,
            Subphase5LifeRatio,
            Subphase6LifeRatio,
            Subphase7LifeRatio,
            Subphase8LifeRatio,
            Subphase9LifeRatio,
            Subphase10LifeRatio,
        };
        #endregion

        public const int TransitionDRBoostTime = 120;

        public const int Phase2InvincibilityTime = 300;

        // Various look-up constants for ExtraAI variables.
        // TODO -- Consider expanding this practice a bit to other AIs? This pattern solves the problem of having random hardcoded indices scattered about in the global NPC classes.
        public const int SpecialFrameTypeIndex = 5;

        public const int AttackCycleIndexIndex = 6;

        public const int HasEnteredPhase2Index = 7;

        public const int HasEnteredFinalPhaseFlag = 8;

        public const int InvincibilityTimerIndex = 9;

        public const int FireFormInterpolantIndex = 10;

        public const int IllusionCountIndex = 11;

        public const int SubphaseTransitionTimerIndex = 12;

        public const int SubphaseIndexIndex = 13;

        public const int TeleportChargeCounterIndex = 14;

        public const int TransitionDRBoostCountdownIndex = 15;

        public const int ShouldPerformBerserkChargesIndex = 16;

        public const int HasExplodedFlagIndex = 17;

        public const int HasTeleportedAbovePlayerFlagIndex = 18;

        public const int DesperationPhaseAttackDelayIndex = 19;

        public const float Phase2LifeRatio = 0.5f;

        public const float BaseDR = 0.3f;

        // Factor for how much Yharon deceleratrs once a charge concludes.
        // This exists as a way of reducing Yharon's momentum after a charge so that he can more easily get into position for the next charge.
        // The lower this value is, the quicker his charges will be.
        public const float PostChargeDecelerationFactor = 0.42f;

        #region AI

        public override bool PreAI(NPC npc)
        {
            // Stop rain if it's happen so it doesn't obstruct the fight (also because Yharon is heat oriented).
            CalamityMod.CalamityMod.StopRain();

            // Aquire a new target if the current one is dead or inactive.
            // If no target exists, fly away.
            npc.TargetClosestIfTargetIsInvalid();
            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
            {
                npc.velocity.Y -= 1.3f;
                npc.rotation = npc.rotation.AngleLerp(0f, 0.2f);
                if (!npc.WithinRange(Main.player[npc.target].Center, 2200f))
                {
                    // Delete projectiles when disappearing, so that there isn't anything still around if the player wants to immediately challenge Yharon again.
                    ClearAllEntities();
                    npc.active = false;
                }
                return false;
            }

            // Prevent stupid fucking natural despawns.
            npc.timeLeft = 7200;

            Player target = Main.player[npc.target];

            float lifeRatio = npc.life / (float)npc.lifeMax;

            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float specialFrameType = ref npc.Infernum().ExtraAI[SpecialFrameTypeIndex];
            ref float invincibilityTime = ref npc.Infernum().ExtraAI[InvincibilityTimerIndex];
            ref float fireIntensity = ref npc.Infernum().ExtraAI[FireFormInterpolantIndex];
            ref float subphaseTransitionTimer = ref npc.Infernum().ExtraAI[SubphaseTransitionTimerIndex];
            ref float currentSubphase = ref npc.Infernum().ExtraAI[SubphaseIndexIndex];
            ref float teleportChargeCounter = ref npc.Infernum().ExtraAI[TeleportChargeCounterIndex];
            ref float transitionDRCountdown = ref npc.Infernum().ExtraAI[TransitionDRBoostCountdownIndex];
            ref float shouldPerformBerserkCharges = ref npc.Infernum().ExtraAI[ShouldPerformBerserkChargesIndex];

            // Go to phase 2 if at 50%.
            if (!InSecondPhase && lifeRatio < Phase2LifeRatio)
            {
                InSecondPhase = true;
                
                Utilities.DisplayText("The air is scorching your skin...", Color.Orange);
                
                // Reset the attack cycle index.
                npc.Infernum().ExtraAI[AttackCycleIndexIndex] = 0f;
                SelectNextAttack(npc, ref attackType);

                // And spawn a lot of cool sparkles.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 180; i++)
                    {
                        Vector2 sparkleSpawnPosition = npc.Center + Main.rand.NextVector2Circular(240f, 240f);
                        Utilities.NewProjectileBetter(sparkleSpawnPosition, Main.rand.NextVector2Circular(28f, 28f), ModContent.ProjectileType<MajesticSparkleBig>(), 0, 0f);
                    }
                }

                // Use the awesome vocals music.
                if (ModLoader.TryGetMod("CalamityModMusic", out Mod calamityModMusic))
                    npc.ModNPC.Music = MusicLoader.GetMusicSlot(calamityModMusic, "Sounds/Music/YharonP2");
                else
                    npc.ModNPC.Music = MusicID.LunarBoss;

                // Activate the invincibility countdown.
                invincibilityTime = Phase2InvincibilityTime;
            }

            // Manually set the Yharon index.
            // The global NPC class technically does this on its own but it has a one-frame disparity when it does so.
            // Without this, the subphase table check will fail because none of the conditions will be valid since it checks this variable when running the
            // InSecondPhase property check. Once the table check fails Yharon's AI will throw an exception and the game will delete him from existence.
            // Not an ideal situation.
            GlobalNPCOverrides.Yharon = npc.whoAmI;

            // Perform the aforementioned attack pattern lookup.
            YharonAttackType[] patternToUse = SubphaseTable.First(table => table.Value(npc)).Key;
            float oldSubphase = currentSubphase;
            currentSubphase = SubphaseTable.Keys.ToList().IndexOf(patternToUse);
            YharonAttackType nextAttackType = patternToUse[(int)((attackType + 1) % patternToUse.Length)];

            // Transition to the next subphase if necessary.
            if (oldSubphase != currentSubphase)
            {
                subphaseTransitionTimer = TransitionDRBoostTime;

                // Reset the attack cycle index for subphase 4.
                if (currentSubphase == 3f)
                {
                    npc.Infernum().ExtraAI[AttackCycleIndexIndex] = 0f;
                    SelectNextAttack(npc, ref attackType);
                }

                // Clear away projectiles in subphase 9.
                if (Main.netMode != NetmodeID.MultiplayerClient && currentSubphase == 8f)
                {
                    attackTimer = 0f;
                    shouldPerformBerserkCharges = 1f;
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<YharonBoom>(), 0, 0f);
                    ClearAllEntities();
                    SelectNextAttack(npc, ref attackType);
                }

                if (currentSubphase == 9f)
                {
                    attackTimer = 0f;
                    SelectNextAttack(npc, ref attackType);
                }
                transitionDRCountdown = TransitionDRBoostTime;
                npc.netUpdate = true;
            }

            // Determine DR. This becomes very powerful as Yharon transitions to a new attack, to prevent skipping multiple subphases with adrenaline.
            npc.Calamity().DR = MathHelper.Lerp(BaseDR, 0.9999f, (float)Math.Pow(transitionDRCountdown / TransitionDRBoostTime, 0.3));

            // Decrement the transition DR countdown. This doesn't happen if Yharon is still in his brief invincibility period.
            if (transitionDRCountdown > 0f && !npc.dontTakeDamage)
                transitionDRCountdown--;

            // Become engulphed in flames when in the phase 2 invincibility phase.
            if (invincibilityTime > 0f)
            {
                float fadeIn = Utils.GetLerpValue(Phase2InvincibilityTime, Phase2InvincibilityTime - 45f, invincibilityTime, true);
                float fadeOut = Utils.GetLerpValue(0f, 45f, invincibilityTime, true);
                fireIntensity = fadeIn * fadeOut;
            }

            // Slow down and transition to the next subphase as necessary.
            // Following this Yharon will recieve a DR boost. The code for this up a small bit.
            if (subphaseTransitionTimer > 0)
            {
                npc.damage = 0;
                npc.rotation = npc.rotation.AngleTowards(0f, 0.2f);
                npc.velocity *= 0.925f;

                if (invincibilityTime <= 0f)
                    fireIntensity = Utils.GetLerpValue(TransitionDRBoostTime, TransitionDRBoostTime - 75f, subphaseTransitionTimer, true);
                specialFrameType = (int)YharonFrameDrawingType.FlapWings;

                if (subphaseTransitionTimer < 18f)
                {
                    specialFrameType = (int)YharonFrameDrawingType.Roar;
                    if (subphaseTransitionTimer == 9)
                        SoundEngine.PlaySound(YharonBoss.RoarSound, npc.Center);
                }

                npc.dontTakeDamage = true;
                subphaseTransitionTimer--;
                return false;
            }

            // If Yharon is not invincible perform regular fire intensity checks.
            if (invincibilityTime <= 0f)
            {
                // If Yharon has his subphase transition DR boost, make the fire intensity reflect that.
                if (transitionDRCountdown > 0f)
                    fireIntensity = transitionDRCountdown / TransitionDRBoostTime;

                // If not, and Yharon ins't performing a heat-based attack, have the fire intensity naturally dissipate.
                // Certain attacks may override this manually.
                else if (nextAttackType is not YharonAttackType.PhoenixSupercharge and not YharonAttackType.HeatFlashRing)
                    fireIntensity = MathHelper.Lerp(fireIntensity, 0f, 0.075f);
            }

            // Adjust various values before doing anything else. If these need to be changed later in certain attacks, they will be.
            npc.dontTakeDamage = false;
            Filters.Scene["HeatDistortion"].GetShader().UseIntensity(0.5f);
            npc.Infernum().ExtraAI[IllusionCountIndex] = 0f;

            // Define various attack-specific variables.
            float chargeSpeed = 46f;
            int chargeDelay = 32;
            int chargeTime = 28;
            float fastChargeSpeedMultiplier = 1.4f;

            int fireballBreathShootDelay = 34;
            int totalFireballBreaths = 12;
            float fireballBreathShootRate = 5f;

            int totalShotgunBursts = 3;
            int shotgunBurstFireRate = 30;

            int infernadoAttackPowerupTime = 90;

            float splittingMeteorBombingSpeed = 30f;
            int splittingMeteorRiseTime = 90;
            int splittingMeteorBombTime = 72;

            // Determine important phase variables.
            bool phase2 = npc.Infernum().ExtraAI[HasEnteredPhase2Index] == 1f;
            bool berserkChargeMode = shouldPerformBerserkCharges == 1f;
            if (!berserkChargeMode)
            {
                berserkChargeMode = phase2 && lifeRatio < Subphase9LifeRatio && lifeRatio >= Subphase10LifeRatio && attackType != (float)YharonAttackType.PhoenixSupercharge && invincibilityTime <= 0f;
                shouldPerformBerserkCharges = berserkChargeMode.ToInt();
            }
            if (berserkChargeMode)
            {
                berserkChargeMode = lifeRatio >= Subphase10LifeRatio;
                shouldPerformBerserkCharges = berserkChargeMode.ToInt();
            }

            // Calculate the position of Yharon's mouth. This used when doing fire breath attacks and such.
            Vector2 offsetCenter = npc.Center + npc.SafeDirectionTo(target.Center) * (npc.width * 0.5f + 10f);
            Vector2 mouthPosition = new(offsetCenter.X + npc.direction * 60f, offsetCenter.Y - 15f);

            bool enraged = ArenaSpawnAndEnrageCheck(npc, target);
            npc.Calamity().CurrentlyEnraged = enraged;

            // Perform special behaviors for specific charges based on what is being used.
            switch ((YharonAttackType)(int)attackType)
            {
                case YharonAttackType.FastCharge:
                    chargeDelay = 60;
                    break;
                case YharonAttackType.PhoenixSupercharge:
                    chargeDelay = berserkChargeMode ? 26 : 34;
                    chargeTime = berserkChargeMode ? 20 : 24;
                    fastChargeSpeedMultiplier = berserkChargeMode ? 1.4f : 1.7f;
                    break;
            }

            // Buff charges in subphase 6 and onwards.
            if (currentSubphase >= 5f)
                fastChargeSpeedMultiplier *= 1.125f;

            // Kill the player if enraged.
            if (enraged)
            {
                npc.damage = npc.defDamage * 50;
                npc.dontTakeDamage = true;
                chargeSpeed += 37f;
                chargeDelay /= 2;
                chargeTime = 18;

                fireballBreathShootDelay = 25;
                totalFireballBreaths = 25;
                fireballBreathShootRate = 3f;

                shotgunBurstFireRate = 15;

                splittingMeteorBombingSpeed = 40f;
            }
            
            // Reset damage to its default value as it was at the time the NPC was created.
            // Further npc damage manipulation can be done later if necessary.
            else
                npc.damage = npc.defDamage;

            // Buff charges if in phase 2.
            if (phase2)
            {
                chargeDelay = (int)(chargeDelay * 0.8);
                chargeSpeed += 2.7f;
                fastChargeSpeedMultiplier += 0.08f;
            }

            // Multiplicatively reduce the fast charge speed multiplier. This is easier than changing individual variables above when testing.
            fastChargeSpeedMultiplier *= 0.875f;

            // Disable damage for a while in phase 2. Also release some sparkles for visual flair.
            if (invincibilityTime > 0f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 sparkleSpawnPosition = npc.Center + Main.rand.NextVector2Circular(180f, 180f);
                    Utilities.NewProjectileBetter(sparkleSpawnPosition, Main.rand.NextVector2Circular(12f, 12f), ModContent.ProjectileType<YharonMajesticSparkle>(), 0, 0f);
                }
                npc.dontTakeDamage = true;
                invincibilityTime--;
            }
            
            switch ((YharonAttackType)(int)attackType)
            {
                // The attack only happens when Yharon spawns.
                case YharonAttackType.SpawnEffects:
                    DoBehavior_SpawnEffects(npc, ref attackType, ref attackTimer);
                    break;
                case YharonAttackType.Charge:
                case YharonAttackType.TeleportingCharge:
                case YharonAttackType.FireTrailCharge:
                    DoBehavior_ChargesAndTeleportCharges(npc, target, chargeDelay, chargeTime, chargeSpeed, teleportChargeCounter, ref fireIntensity, ref attackTimer, ref attackType, ref specialFrameType);
                    break;
                case YharonAttackType.FastCharge:
                case YharonAttackType.PhoenixSupercharge:
                    DoBehavior_FastCharges(npc, target, berserkChargeMode, chargeDelay, chargeTime, chargeSpeed * fastChargeSpeedMultiplier, ref fireIntensity, ref attackTimer, ref attackType, ref specialFrameType);
                    break;
                case YharonAttackType.FireballBurst:
                    DoBehavior_FireballBurst(npc, target, mouthPosition, fireballBreathShootDelay, fireballBreathShootRate, totalFireballBreaths, ref attackTimer, ref attackType, ref specialFrameType);
                    break;
                case YharonAttackType.FlamethrowerAndMeteors:
                    DoBehavior_FlamethrowerAndMeteors(npc, target, mouthPosition, ref attackTimer, ref attackType, ref specialFrameType);
                    break;
                case YharonAttackType.FlarenadoAndDetonatingFlameSpawn:
                    DoBehavior_FlarenadoAndDetonatingFlameSpawn(npc, mouthPosition, ref attackTimer, ref attackType, ref specialFrameType);
                    break;
                case YharonAttackType.InfernadoAndFireShotgunBreath:
                    DoBehavior_InfernadoAndFireShotgunBreath(npc, target, mouthPosition, enraged, fireballBreathShootDelay, shotgunBurstFireRate, totalShotgunBursts, ref attackTimer, ref attackType, ref specialFrameType);
                    break;
                case YharonAttackType.MassiveInfernadoSummon:
                    DoBehavior_MassiveInfernadoSummon(npc, infernadoAttackPowerupTime, ref attackTimer, ref attackType, ref specialFrameType);
                    break;
                case YharonAttackType.SummonFlareRing:
                    DoBehavior_SummonFlareRing(npc, ref attackType);
                    break;
                case YharonAttackType.CarpetBombing:
                    DoBehavior_CarpetBombing(npc, target, splittingMeteorRiseTime, splittingMeteorBombingSpeed, splittingMeteorBombTime, ref attackTimer, ref attackType, ref specialFrameType);
                    break;
                case YharonAttackType.HeatFlashRing:
                    DoBehavior_HeatFlashRing(npc, target, chargeDelay, ref fireIntensity, ref attackTimer, ref attackType, ref specialFrameType);
                    break;
                case YharonAttackType.VorticesOfFlame:
                    DoBehavior_VorticesOfFlame(npc, target, ref attackTimer, ref attackType, ref specialFrameType);
                    break;
                case YharonAttackType.FinalDyingRoar:
                    DoBehavior_FinalDyingRoar(npc);
                    break;
            }
            attackTimer++;
            return false;
        }

        public static void DoBehavior_SpawnEffects(NPC npc, ref float attackType, ref float attackTimer)
        {
            int spawnEffectsTime = 336;

            // Disable damage.
            npc.dontTakeDamage = true;
            npc.damage = 0;

            // Idly spawn pretty sparkles.
            if (Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool())
            {
                Vector2 sparkleSpawnPosition = npc.Center + Main.rand.NextVector2Circular(210f, 210f);
                Utilities.NewProjectileBetter(sparkleSpawnPosition, Main.rand.NextVector2Circular(18f, 18f), ModContent.ProjectileType<YharonMajesticSparkle>(), 0, 0f);
            }

            // Begin attacking after enough time has passed.
            if (attackTimer >= spawnEffectsTime)
            {
                // Spawn a circle of fire bombs instead of flare dust.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 16; i++)
                    {
                        float angle = MathHelper.TwoPi / 16f * i;
                        Utilities.NewProjectileBetter(npc.Center + angle.ToRotationVector2() * 60f, angle.ToRotationVector2() * 15f, ModContent.ProjectileType<FlareBomb>(), 480, 0f);
                    }
                }
                SelectNextAttack(npc, ref attackType);
            }

            // Disable contact damage.
            npc.damage = 0;
        }

        public static void DoBehavior_ChargesAndTeleportCharges(NPC npc, Player target, float chargeDelay, float chargeTime, float chargeSpeed, float teleportChargeCounter, ref float fireIntensity, ref float attackTimer, ref float attackType, ref float specialFrameType)
        {
            bool releaseFire = (YharonAttackType)(int)attackType == YharonAttackType.FireTrailCharge;
            float predictivenessFactor = 0f;
            if ((YharonAttackType)(int)attackType != YharonAttackType.TeleportingCharge)
            {
                chargeDelay = (int)(chargeDelay * 0.8f);
                predictivenessFactor = 4.25f;
            }
            else
                chargeDelay += 18f;

            if (releaseFire)
                chargeDelay += 25f;

            Vector2 chargeDestination = target.Center + target.velocity * predictivenessFactor;

            // Slow down and rotate towards the player.
            if (attackTimer < chargeDelay)
            {
                npc.damage = 0;
                npc.velocity *= 0.93f;
                npc.spriteDirection = (target.Center.X - npc.Center.X < 0).ToDirectionInt();
                npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(chargeDestination) + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi, 0.1f);

                // Have Yharon engulph himself in flames before charging if he will release fire.
                if (releaseFire)
                    fireIntensity = MathHelper.Max(fireIntensity, attackTimer / chargeDelay);

                // Teleport prior to the charge happening if the attack calls for it.
                if (attackTimer == chargeDelay - 35f && (YharonAttackType)(int)attackType == YharonAttackType.TeleportingCharge)
                {
                    Vector2 offsetDirection = target.velocity.SafeNormalize(Main.rand.NextVector2Unit());

                    // If a teleport charge was done beforehand randomize the offset direction if the
                    // player is descending. This still has an uncommon chance to end up in a similar direction as the one
                    // initially chosen.
                    if (teleportChargeCounter > 0f && offsetDirection.AngleBetween(Vector2.UnitY) < MathHelper.Pi / 15f)
                    {
                        do
                        {
                            offsetDirection = Main.rand.NextVector2Unit();
                        }
                        while (Math.Abs(Vector2.Dot(offsetDirection, Vector2.UnitY)) > 0.6f);
                    }

                    npc.Center = target.Center + offsetDirection * 560f;
                    npc.velocity = Vector2.Zero;
                    npc.netUpdate = true;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 30; i++)
                        {
                            Vector2 sparkleSpawnPosition = npc.Center + Main.rand.NextVector2Circular(180f, 180f);
                            Utilities.NewProjectileBetter(sparkleSpawnPosition, Main.rand.NextVector2Circular(12f, 12f), ModContent.ProjectileType<YharonMajesticSparkle>(), 0, 0f);
                        }
                    }

                    npc.alpha = 255;
                }

                // Fade in.
                npc.alpha = Utils.Clamp(npc.alpha - 28, 0, 255);

                specialFrameType = (int)YharonFrameDrawingType.FlapWings;
            }

            // Charge at the target.
            else if (attackTimer == chargeDelay)
            {
                npc.velocity = npc.SafeDirectionTo(chargeDestination) * (chargeSpeed + npc.Distance(target.Center) * 0.0108f);
                npc.rotation = npc.AngleTo(chargeDestination) + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi;
                specialFrameType = (int)YharonFrameDrawingType.IdleWings;
                if (releaseFire)
                    SoundEngine.PlaySound(YharonBoss.OrbSound, target.Center);

                npc.netUpdate = true;

                SoundEngine.PlaySound(YharonBoss.ShortRoarSound, npc.Center);
            }
            else if (attackTimer >= chargeDelay + chargeTime)
                SelectNextAttack(npc, ref attackType);

            // Release dragon fire from behind.
            if (attackTimer >= chargeDelay && releaseFire)
            {
                fireIntensity = MathHelper.Max(fireIntensity, Utils.GetLerpValue(chargeDelay + chargeTime - 1f, chargeDelay + chargeTime - 8f, attackTimer, true));
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center + Main.rand.NextVector2Circular(16f, 16f), npc.velocity * 0.3f, ModContent.ProjectileType<LingeringDragonFlames>(), 500, 0f);
            }
        }

        public static void DoBehavior_FastCharges(NPC npc, Player target, bool berserkChargeMode, float chargeDelay, float chargeTime, float chargeSpeed, ref float fireIntensity, ref float attackTimer, ref float attackType, ref float specialFrameType)
        {
            if ((YharonAttackType)(int)attackType == YharonAttackType.PhoenixSupercharge)
                chargeDelay = (int)(chargeDelay * 0.8f);
            else if (attackTimer == 1f)
                SoundEngine.PlaySound(YharonBoss.ShortRoarSound with { Pitch = -0.56f, Volume = 1.6f }, target.Center);

            // Slow down and rotate towards the player.
            if (attackTimer < chargeDelay)
            {
                npc.spriteDirection = (npc.Center.X > target.Center.X).ToDirectionInt();
                ref float xAimOffset = ref npc.Infernum().ExtraAI[0];
                if (xAimOffset == 0f)
                    xAimOffset = (berserkChargeMode ? 920f : 620f) * Math.Sign((npc.Center - target.Center).X);

                // Transform into a phoenix flame form if doing a phoenix supercharge.
                if ((YharonAttackType)(int)attackType == YharonAttackType.PhoenixSupercharge)
                    fireIntensity = MathHelper.Max(fireIntensity, Utils.GetLerpValue(0f, chargeDelay - 1f, attackTimer, true));

                // Hover to the top left/right of the target and look at them.
                Vector2 destination = target.Center + new Vector2(xAimOffset, berserkChargeMode ? -480f : -240f);
                Vector2 idealVelocity = npc.SafeDirectionTo(destination - npc.velocity) * 26f;
                npc.SimpleFlyMovement(idealVelocity, 1.1f);

                npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(target.Center) + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi, 0.1f);
                specialFrameType = (int)YharonFrameDrawingType.FlapWings;
            }

            // Charge at the target.
            else if (attackTimer == chargeDelay)
            {
                npc.velocity = npc.SafeDirectionTo(target.Center) * (chargeSpeed + npc.Distance(target.Center) * 0.0056f);
                npc.rotation = npc.AngleTo(target.Center) + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi;
                specialFrameType = (int)YharonFrameDrawingType.IdleWings;

                SoundEngine.PlaySound(YharonBoss.ShortRoarSound, npc.Center);

                npc.netUpdate = true;
            }

            // Create sparkles and create heat distortion when charging if doing a phoenix supercharge.
            else if ((YharonAttackType)(int)attackType == YharonAttackType.PhoenixSupercharge && attackTimer < chargeDelay + chargeTime)
            {
                fireIntensity = 1f;
                float competionRatio = Utils.GetLerpValue(chargeDelay, chargeDelay + chargeTime, attackTimer, true);
                Filters.Scene["HeatDistortion"].GetShader().UseIntensity(0.5f + CalamityUtils.Convert01To010(competionRatio) * 3f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 sparkleSpawnPosition = npc.Center + Main.rand.NextVector2Circular(240f, 240f);
                        Utilities.NewProjectileBetter(sparkleSpawnPosition, Main.rand.NextVector2Circular(28f, 28f), ModContent.ProjectileType<MajesticSparkleBig>(), 0, 0f);
                    }
                }
            }

            if (attackTimer >= chargeDelay + chargeTime)
            {
                npc.velocity *= PostChargeDecelerationFactor;
                SelectNextAttack(npc, ref attackType);
            }
        }

        public static void DoBehavior_FireballBurst(NPC npc, Player target, Vector2 mouthPosition, float fireballBreathShootDelay, float fireballBreathShootRate, float totalFireballBreaths, ref float attackTimer, ref float attackType, ref float specialFrameType)
        {
            // Slow down quickly, rotate towards a horizontal orientation, and then spawn a bunch of fire during the initial delay.
            if (attackTimer < fireballBreathShootDelay)
            {
                specialFrameType = (int)YharonFrameDrawingType.FlapWings;
                npc.velocity *= 0.955f;
                npc.rotation = npc.rotation.AngleTowards(0f, 0.1f);
                return;
            }

            specialFrameType = (int)YharonFrameDrawingType.OpenMouth;

            // Hover to the top left/right of the target and look at them.
            npc.spriteDirection = (npc.Center.X > target.Center.X).ToDirectionInt();
            ref float xAimOffset = ref npc.Infernum().ExtraAI[0];
            if (xAimOffset == 0f)
                xAimOffset = 560f * Math.Sign((npc.Center - target.Center).X);

            Vector2 destination = target.Center + new Vector2(xAimOffset, -270f);
            Vector2 idealVelocity = npc.SafeDirectionTo(destination - npc.velocity) * 26f;
            npc.SimpleFlyMovement(idealVelocity, 1f);

            npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(target.Center) + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi, 0.1f);

            // Release a burst of fireballs.
            if (attackTimer % fireballBreathShootRate == fireballBreathShootRate - 1)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 fireballShootVelocity = npc.SafeDirectionTo(target.Center).RotatedByRandom(0.37f) * 16f;
                    int fireball = Utilities.NewProjectileBetter(mouthPosition, fireballShootVelocity, ProjectileID.CultistBossFireBall, 450, 0f);
                    Main.projectile[fireball].tileCollide = false;
                }
                SoundEngine.PlaySound(YharonBoss.ShortRoarSound, npc.Center);
            }

            if (attackTimer >= fireballBreathShootDelay + fireballBreathShootRate * totalFireballBreaths)
                SelectNextAttack(npc, ref attackType);
        }

        public static void DoBehavior_FlamethrowerAndMeteors(NPC npc, Player target, Vector2 mouthPosition, ref float attackTimer, ref float attackType, ref float specialFrameType)
        {
            int totalFlamethrowerBursts = 2;
            int flamethrowerHoverTime = 75;
            float flamethrowerFlySpeed = 55.5f;
            float wrappedAttackTimer = attackTimer % (flamethrowerHoverTime + YharonFlamethrower.Lifetime + 15f);

            // Look at the target and hover towards the top left/right of the target.
            if (wrappedAttackTimer < flamethrowerHoverTime + 15f)
            {
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 1560f, -375f);
                specialFrameType = (int)YharonFrameDrawingType.FlapWings;

                npc.spriteDirection = (npc.Center.X > target.Center.X).ToDirectionInt();
                npc.rotation = npc.AngleTo(target.Center) + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi;
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination - npc.velocity) * 36f, 1.1f);

                // Begin the delay if the destination is reached.
                if (npc.WithinRange(hoverDestination, 50f) && wrappedAttackTimer < flamethrowerHoverTime - 2f)
                    attackTimer += flamethrowerHoverTime - wrappedAttackTimer - 1f;

                // Release fire and smoke from the mouth as a telegraph.
                for (int i = 0; i < 3; i++)
                {
                    Vector2 dustSpawnPosition = mouthPosition + Main.rand.NextVector2Circular(8f, 8f);
                    Vector2 dustVelocity = npc.SafeDirectionTo(dustSpawnPosition).RotatedByRandom(0.45f) * Main.rand.NextFloat(2f, 5f);
                    Dust hotStuff = Dust.NewDustPerfect(dustSpawnPosition, Main.rand.NextBool() ? 31 : 6);
                    hotStuff.velocity = dustVelocity + npc.velocity;
                    hotStuff.fadeIn = 0.8f;
                    hotStuff.scale = Main.rand.NextFloat(1f, 1.45f);
                    hotStuff.alpha = 200;
                }

                return;
            }

            // Begin the charge and breathe fire after a tiny delay.
            if (wrappedAttackTimer == flamethrowerHoverTime + 15f)
            {
                npc.velocity = npc.SafeDirectionTo(target.Center) * flamethrowerFlySpeed;

                specialFrameType = (int)YharonFrameDrawingType.OpenMouth;

                SoundEngine.PlaySound(YharonBoss.FireSound, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int flamethrower = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<YharonFlamethrower>(), 540, 0f);
                    if (Main.projectile.IndexInRange(flamethrower))
                        Main.projectile[flamethrower].ai[1] = npc.whoAmI;
                }
            }

            npc.rotation = npc.velocity.ToRotation() + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi;

            if (attackTimer >= totalFlamethrowerBursts * (flamethrowerHoverTime + YharonFlamethrower.Lifetime + 15f) - 3f)
                SelectNextAttack(npc, ref attackType);
        }

        public static void DoBehavior_FlarenadoAndDetonatingFlameSpawn(NPC npc, Vector2 mouthPosition, ref float attackTimer, ref float attackType, ref float specialFrameType)
        {
            float flarenadoSpawnDelay = 15f;

            specialFrameType = (int)YharonFrameDrawingType.FlapWings;

            // Slow down quickly during the delay and approach a 0 rotation.
            if (attackTimer < flarenadoSpawnDelay)
            {
                npc.velocity *= 0.9f;
                npc.rotation = npc.rotation.AngleTowards(0f, 0.1f);
            }

            // Release 3 tornado spawners after the initial delay concludes.
            else if (attackTimer == flarenadoSpawnDelay)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 tornadoSpawnerShootVelocity = (MathHelper.TwoPi / 3f * i).ToRotationVector2() * 7f;
                        Utilities.NewProjectileBetter(mouthPosition, tornadoSpawnerShootVelocity, ModContent.ProjectileType<BigFlare>(), 0, 0f, Main.myPlayer, 1f, npc.target + 1);
                    }
                }
                SoundEngine.PlaySound(YharonBoss.RoarSound, npc.Center);
            }

            // Release detonating flares.
            else if (attackTimer < flarenadoSpawnDelay + 45f)
            {
                if ((attackTimer - flarenadoSpawnDelay) % 10f == 9f)
                    SoundEngine.PlaySound(YharonBoss.ShortRoarSound, npc.Center);
            }
            else
                SelectNextAttack(npc, ref attackType);
        }
        public static void DoBehavior_InfernadoAndFireShotgunBreath(NPC npc, Player target, Vector2 mouthPosition, bool enraged, float fireballBreathShootDelay, float shotgunBurstFireRate, float totalShotgunBursts, ref float attackTimer, ref float attackType, ref float specialFrameType)
        {
            if (attackTimer < fireballBreathShootDelay)
            {
                npc.velocity *= 0.955f;
                npc.rotation = npc.rotation.AngleTowards(0f, 0.1f);
                specialFrameType = (int)YharonFrameDrawingType.FlapWings;
                return;
            }

            // Hover to the top left/right of the target and look at them.
            npc.spriteDirection = (npc.Center.X > target.Center.X).ToDirectionInt();
            ref float xAimOffset = ref npc.Infernum().ExtraAI[0];
            if (xAimOffset == 0f)
                xAimOffset = 770f * Math.Sign((npc.Center - target.Center).X);

            Vector2 destination = target.Center + new Vector2(xAimOffset, -360f);
            Vector2 idealVelocity = npc.SafeDirectionTo(destination - npc.velocity) * 17f;
            npc.SimpleFlyMovement(idealVelocity, 1f);

            npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(target.Center) + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi, 0.1f);

            // Release a large infernado flare from the mouth.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == fireballBreathShootDelay)
                Utilities.NewProjectileBetter(mouthPosition, npc.SafeDirectionTo(mouthPosition) * 8f, ModContent.ProjectileType<BigFlare2>(), 0, 0f, Main.myPlayer, 1f, npc.target + 1);

            // Release a shotgun spread of fireballs.
            if ((attackTimer - fireballBreathShootDelay) % shotgunBurstFireRate == shotgunBurstFireRate - 1f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int fireballCount = Main.rand.Next(7, 12 + 1);
                    float angleSpread = Main.rand.NextFloat(0.3f, 0.55f);
                    for (int i = 0; i < fireballCount; i++)
                    {
                        float offsetAngle = MathHelper.Lerp(-angleSpread, angleSpread, i / (float)fireballCount);
                        float burstSpeed = Main.rand.NextFloat(12f, 19f);
                        if (enraged)
                            burstSpeed *= Main.rand.NextFloat(1.5f, 2.7f);

                        Vector2 burstVelocity = npc.SafeDirectionTo(mouthPosition).RotatedBy(offsetAngle) * burstSpeed;
                        int fire = Utilities.NewProjectileBetter(mouthPosition, burstVelocity, ModContent.ProjectileType<DragonFireball>(), 500, 0f, Main.myPlayer);
                        Main.projectile[fire].tileCollide = false;
                    }
                }
                if (attackTimer >= fireballBreathShootDelay + shotgunBurstFireRate * totalShotgunBursts - 1f)
                    SelectNextAttack(npc, ref attackType);

                SoundEngine.PlaySound(YharonBoss.ShortRoarSound, npc.Center);
            }
            specialFrameType = (int)YharonFrameDrawingType.Roar;
        }

        public static void DoBehavior_MassiveInfernadoSummon(NPC npc, float infernadoAttackPowerupTime, ref float attackTimer, ref float attackType, ref float specialFrameType)
        {
            // Slow down and charge up.
            if (attackTimer < infernadoAttackPowerupTime)
            {
                npc.velocity *= 0.955f;
                npc.rotation = npc.rotation.AngleTowards(0f, 0.1f);
                if (attackTimer % 4f == 3f)
                {
                    for (int i = 0; i < 100; i++)
                    {
                        float angle = MathHelper.TwoPi * i / 100f;
                        float intensity = Main.rand.NextFloat();
                        Vector2 fireSpawnPosition = npc.Center + angle.ToRotationVector2() * Main.rand.NextFloat(720f, 900f);
                        Vector2 fireVelocity = (angle - MathHelper.Pi).ToRotationVector2() * (29f + 11f * intensity);

                        Dust fire = Dust.NewDustPerfect(fireSpawnPosition, 6, fireVelocity);
                        fire.scale = 0.9f;
                        fire.fadeIn = 1.15f + intensity * 0.3f;
                        fire.noGravity = true;
                    }
                }
                specialFrameType = (int)YharonFrameDrawingType.FlapWings;
            }

            // Release the energy, spawn some charging infernados, and go to the next attack state.
            if (attackTimer == infernadoAttackPowerupTime)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 28; i++)
                    {
                        float angle = MathHelper.TwoPi / 28f * i;
                        float speed = Main.rand.NextFloat(12f, 15f);
                        Utilities.NewProjectileBetter(npc.Center + angle.ToRotationVector2() * 40f, angle.ToRotationVector2() * speed, ModContent.ProjectileType<FlareBomb>(), 540, 0f);
                    }

                    for (int i = 0; i < 3; i++)
                    {
                        float angle = MathHelper.TwoPi / 3f * i;
                        Vector2 flareSpawnPosition = npc.Center + angle.ToRotationVector2() * 600f;
                        Utilities.NewProjectileBetter(flareSpawnPosition, angle.ToRotationVector2().RotatedByRandom(0.03f) * Vector2.Zero, ModContent.ProjectileType<ChargeFlare>(), 0, 0f, Main.myPlayer);
                    }
                }
                SoundEngine.PlaySound(YharonBoss.RoarSound, npc.Center);
                SelectNextAttack(npc, ref attackType);
            }
        }

        public static void DoBehavior_SummonFlareRing(NPC npc, ref float attackType)
        {
            SelectNextAttack(npc, ref attackType);
        }

        public static void DoBehavior_CarpetBombing(NPC npc, Player target, float splittingMeteorRiseTime, float splittingMeteorBombingSpeed, float splittingMeteorBombTime, ref float attackTimer, ref float attackType, ref float specialFrameType)
        {
            int directionToDestination = (npc.Center.X > target.Center.X).ToDirectionInt();
            bool morePowerfulMeteors = npc.life < npc.lifeMax * 0.35f;

            // Fly towards the hover destination near the target.
            if (attackTimer < splittingMeteorRiseTime)
            {
                Vector2 destination = target.Center + new Vector2(directionToDestination * 750f, -300f);
                Vector2 idealVelocity = npc.SafeDirectionTo(destination) * splittingMeteorBombingSpeed;

                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.035f);
                npc.rotation = npc.rotation.AngleTowards(0f, 0.25f);

                npc.spriteDirection = (npc.Center.X > target.Center.X).ToDirectionInt();

                // Once it has been reached, change the attack timer to begin the carpet bombing.
                if (npc.WithinRange(destination, 32f))
                    attackTimer = splittingMeteorRiseTime - 1f;
                specialFrameType = (int)YharonFrameDrawingType.FlapWings;
            }

            // Begin flying horizontally.
            else if (attackTimer == splittingMeteorRiseTime)
            {
                Vector2 velocity = npc.SafeDirectionTo(target.Center);
                velocity.Y *= 0.3f;
                velocity = velocity.SafeNormalize(Vector2.UnitX * npc.spriteDirection);

                specialFrameType = (int)YharonFrameDrawingType.OpenMouth;

                npc.velocity = velocity * splittingMeteorBombingSpeed;
                if (morePowerfulMeteors)
                    npc.velocity *= 1.45f;
            }

            // And vomit meteors.
            else
            {
                npc.position.X += npc.SafeDirectionTo(target.Center).X * 7f;
                npc.position.Y += npc.SafeDirectionTo(target.Center + Vector2.UnitY * -400f).Y * 6f;
                npc.spriteDirection = (npc.velocity.X < 0f).ToDirectionInt();
                npc.rotation = npc.velocity.ToRotation() + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi;

                int fireballReleaseRate = morePowerfulMeteors ? 4 : 7;
                if (attackTimer % fireballReleaseRate == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center + npc.velocity * 3f, npc.velocity, ModContent.ProjectileType<YharonFireball>(), 515, 0f, Main.myPlayer, 0f, 0f);
            }
            if (attackTimer >= splittingMeteorRiseTime + splittingMeteorBombTime)
                SelectNextAttack(npc, ref attackType);
        }

        public static void DoBehavior_HeatFlashRing(NPC npc, Player target, float chargeDelay, ref float fireIntensity, ref float attackTimer, ref float attackType, ref float specialFrameType)
        {
            float heatFlashStartDelay = 45f;
            float heatFlashIdleDelay = 30f;
            float heatFlashFlashTime = 40f;
            float heatFlashEndDelay = 27f;
            int heatFlashTotalFlames = 24;

            // Don't do contact damage during the heat flash ring, to prevent cheap shots.
            npc.damage = 0;

            if (attackTimer >= heatFlashIdleDelay + heatFlashStartDelay + heatFlashFlashTime + heatFlashEndDelay)
                SelectNextAttack(npc, ref attackType);
            specialFrameType = (int)YharonFrameDrawingType.FlapWings;

            // Attempt to fly above the target.
            if (attackTimer < heatFlashIdleDelay)
            {
                npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(target.Center) + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi, 0.1f);
                npc.spriteDirection = 1;

                Vector2 destinationAbovePlayer = target.Center - Vector2.UnitY * 420f - npc.Center;
                npc.SimpleFlyMovement((destinationAbovePlayer - npc.velocity).SafeNormalize(Vector2.Zero) * 36f, 3.5f);
                return;
            }

            // After hovering, create a burst of flames around the target.
            // Teleport above the player if somewhat far away from them.
            if (attackTimer == heatFlashIdleDelay && !npc.WithinRange(target.Center, 360f))
            {
                npc.Center = target.Center - Vector2.UnitY * 420f;
                npc.velocity = Vector2.Zero;
                npc.netUpdate = true;
                if (!Main.dedServ)
                {
                    for (int j = 0; j < 30; j++)
                    {
                        float angle = MathHelper.TwoPi * j / 30f;
                        Dust dust = Dust.NewDustDirect(target.Center, 0, 0, ModContent.DustType<FinalFlame>(), 0f, 0f, 100, default, 3f);
                        dust.noGravity = true;
                        dust.noLight = true;
                        dust.fadeIn = 1.2f;
                        dust.velocity = angle.ToRotationVector2() * 5f;
                    }
                }
            }

            // Slow down.
            npc.velocity *= 0.95f;

            // Transform into a phoenix flame form.
            fireIntensity = MathHelper.Max(fireIntensity, Utils.GetLerpValue(0f, chargeDelay - 1f, attackTimer, true));

            // Rapidly approach a 0 rotation.
            npc.rotation = npc.rotation.AngleTowards(0f, 0.7f);

            // Create a ring of flames at the zenith of the flash.
            if (attackTimer >= heatFlashIdleDelay + heatFlashStartDelay && attackTimer <= heatFlashIdleDelay + heatFlashStartDelay + heatFlashFlashTime)
            {
                float brightness = CalamityUtils.Convert01To010(Utils.GetLerpValue(heatFlashIdleDelay + heatFlashStartDelay, heatFlashIdleDelay + heatFlashStartDelay + heatFlashFlashTime, attackTimer, true));
                bool atMaximumBrightness = attackTimer == heatFlashIdleDelay + heatFlashStartDelay + heatFlashFlashTime / 2;

                // Immediately create the ring of flames if the brightness is at its maximum.
                if (atMaximumBrightness)
                {
                    // The outwardness of the ring is dependant on the speed of the target. However, the additive boost cannot exceed a certain amount.
                    float outwardness = 550f + MathHelper.Min(target.velocity.Length(), 40f) * 12f;
                    for (int i = 0; i < heatFlashTotalFlames; i++)
                    {
                        float angle = MathHelper.TwoPi * i / heatFlashTotalFlames;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            float angleFromTarget = angle.ToRotationVector2().AngleBetween(target.velocity);
                            Utilities.NewProjectileBetter(target.Center + angle.ToRotationVector2() * outwardness, Vector2.Zero, ModContent.ProjectileType<YharonHeatFlashFireball>(), 595, 0f, Main.myPlayer);

                            // Create a cluster of flames that appear in the direction the target is currently moving.
                            // This makes it harder to maneuver through the burst.
                            if (angleFromTarget <= MathHelper.TwoPi / heatFlashTotalFlames)
                            {
                                for (int j = 0; j < Main.rand.Next(11, 17 + 1); j++)
                                {
                                    float newAngle = angle + Main.rand.NextFloatDirection() * angleFromTarget;
                                    Utilities.NewProjectileBetter(target.Center + newAngle.ToRotationVector2() * outwardness, Vector2.Zero, ModContent.ProjectileType<YharonHeatFlashFireball>(), 595, 0f, Main.myPlayer);
                                }
                            }
                        }

                        // Emit some dust on top of the target.
                        else if (!Main.dedServ)
                        {
                            for (int j = 0; j < 14; j++)
                            {
                                float offsetAngle = MathHelper.TwoPi * j / 14f;
                                Vector2 offsetOutwardness = offsetAngle.ToRotationVector2() * outwardness;

                                Dust fire = Dust.NewDustDirect(target.Center + offsetOutwardness, 0, 0, ModContent.DustType<FinalFlame>(), 0f, 0f, 100, default, 2.1f);
                                fire.noGravity = true;
                                fire.noLight = true;
                                fire.velocity = offsetAngle.ToRotationVector2() * 5f;
                            }
                        }
                    }
                    SoundEngine.PlaySound(BigFlare.FlareSound, target.Center);
                }
                MoonlordDeathDrama.RequestLight(brightness, target.Center);
            }
        }

        public static void DoBehavior_VorticesOfFlame(NPC npc, Player target, ref float attackTimer, ref float attackType, ref float specialFrameType)
        {
            int totalFlameVortices = 3;
            int totalFlameWaves = 7;
            float flameVortexSpawnDelay = 60f;

            npc.velocity *= 0.85f;
            npc.spriteDirection = -1;
            npc.rotation = npc.rotation.AngleTowards(0f, 0.185f);
            npc.damage = 0;

            // Teleport above the player if somewhat far away from them.
            if (attackTimer == 1f && !npc.WithinRange(target.Center, 360f))
            {
                npc.Center = target.Center - Vector2.UnitY * 480f;
                if (!Main.dedServ)
                {
                    for (int j = 0; j < 30; j++)
                    {
                        Dust fire = Dust.NewDustDirect(target.Center, 0, 0, ModContent.DustType<FinalFlame>(), 0f, 0f, 100, default, 3f);
                        fire.noGravity = true;
                        fire.noLight = true;
                        fire.fadeIn = 1.2f;
                        fire.velocity = (MathHelper.TwoPi * j / 30f).ToRotationVector2() * 5f;
                    }
                }
                npc.netUpdate = true;
                specialFrameType = (int)YharonFrameDrawingType.FlapWings;
            }

            // Spawn vortices of doom. They periodically shoot homing fire projectiles and are telegraphed prior to spawning.
            if (attackTimer == flameVortexSpawnDelay && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(YharonBoss.OrbSound);

                for (int i = 0; i < totalFlameVortices; i++)
                {
                    float angle = MathHelper.TwoPi * i / totalFlameVortices;
                    Utilities.NewProjectileBetter(target.Center + angle.ToRotationVector2() * 1780f, Vector2.Zero, ModContent.ProjectileType<VortexOfFlame>(), 800, 0f, Main.myPlayer);
                    int telegraph = Utilities.NewProjectileBetter(target.Center, angle.ToRotationVector2(), ModContent.ProjectileType<VortexTelegraphBeam>(), 0, 0f, Main.myPlayer);
                    if (Main.projectile.IndexInRange(telegraph))
                    {
                        Main.projectile[telegraph].velocity = angle.ToRotationVector2();
                        Main.projectile[telegraph].ai[1] = 1780f;
                    }
                }
            }

            // Emit splitting fireballs from the side in a fashion similar to that of Old Duke's shark summoning attack.
            if (attackTimer > flameVortexSpawnDelay && attackTimer % 7 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float horizontalOffset = (attackTimer - flameVortexSpawnDelay) / 7f * 205f + 260f;
                Vector2 fireballSpawnPosition = npc.Center + new Vector2(horizontalOffset, -90f);
                if (!target.WithinRange(fireballSpawnPosition, 350f))
                    Utilities.NewProjectileBetter(fireballSpawnPosition, Vector2.UnitY.RotatedBy(-0.18f) * -20f, ModContent.ProjectileType<YharonFireball>(), 525, 0f, Main.myPlayer);

                fireballSpawnPosition = npc.Center + new Vector2(-horizontalOffset, -90f);
                if (!target.WithinRange(fireballSpawnPosition, 350f))
                    Utilities.NewProjectileBetter(fireballSpawnPosition, Vector2.UnitY.RotatedBy(0.18f) * -20f, ModContent.ProjectileType<YharonFireball>(), 525, 0f, Main.myPlayer);
            }
            if (attackTimer > flameVortexSpawnDelay + totalFlameWaves * 7)
                SelectNextAttack(npc, ref attackType);
        }

        public static void DoBehavior_FinalDyingRoar(NPC npc)
        {
            npc.dontTakeDamage = true;
            Filters.Scene["HeatDistortion"].GetShader().UseIntensity(3f);

            float lifeRatio = npc.life / (float)npc.lifeMax;

            Player target = Main.player[npc.target];

            int totalCharges = 8;
            int chargeCycleTime = 78;
            float confusingChargeSpeed = 53.5f;

            float splittingMeteorHoverSpeed = 24f;
            float splittingMeteorBombingSpeed = 37.5f;
            float splittingMeteorRiseTime = 120f;
            float splittingMeteorBombTime = 90f;
            int fireballReleaseRate = 3;
            int totalTimeSpentPerCarpetBomb = (int)(splittingMeteorRiseTime + splittingMeteorBombTime);

            int totalBerserkCharges = 10;
            int berserkChargeTime = 54;
            float berserkChargeSpeed = 42f;

            ref float attackTimer = ref npc.ai[1];
            ref float specialFrameType = ref npc.Infernum().ExtraAI[SpecialFrameTypeIndex];
            ref float finalAttackCompletionState = ref npc.Infernum().ExtraAI[0];
            ref float totalMeteorBomings = ref npc.Infernum().ExtraAI[1];
            ref float fireIntensity = ref npc.Infernum().ExtraAI[FireFormInterpolantIndex];
            ref float hasCreatedExplosionFlag = ref npc.Infernum().ExtraAI[HasExplodedFlagIndex];
            ref float hasTeleportedFlag = ref npc.Infernum().ExtraAI[HasTeleportedAbovePlayerFlagIndex];
            ref float attackDelay = ref npc.Infernum().ExtraAI[DesperationPhaseAttackDelayIndex];

            npc.damage = npc.defDamage;

            if (attackDelay < 45f)
            {
                npc.damage = 0;
                attackTimer = 0f;
                if (attackDelay == 1f)
                {
                    npc.velocity = Vector2.Zero;
                    npc.rotation = 0f;
                    Vector2 teleportPosition = target.Center - Vector2.UnitY * 445f;
                    Dust.QuickDustLine(npc.Center, teleportPosition, 250f, Color.Orange);
                    npc.Center = teleportPosition;
                    npc.netUpdate = true;
                }
                attackDelay++;
                return;
            }

            // First, create two heat mirages that circle the target and charge at them multiple times.
            // This is intended to confuse them.
            if (attackTimer <= totalCharges * chargeCycleTime)
            {
                // Create a text indicator.
                if (finalAttackCompletionState != 1f)
                {
                    npc.life = (int)(npc.lifeMax * 0.025);
                    if (Main.netMode == NetmodeID.SinglePlayer)
                        Utilities.DisplayText("The heat is surging...", Color.Orange);
                    else if (Main.netMode == NetmodeID.Server)
                        ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("The heat is surging..."), Color.Orange);
                    finalAttackCompletionState = 1f;
                }

                float wrappedAttackTimer = attackTimer % chargeCycleTime;
                if (wrappedAttackTimer < 30f)
                {
                    npc.spriteDirection = (npc.Center.X > target.Center.X).ToDirectionInt();
                    ref float xAimOffset = ref npc.Infernum().ExtraAI[0];
                    if (xAimOffset == 0f)
                        xAimOffset = 890f * Math.Sign((npc.Center - target.Center).X);

                    float offsetAngle = 0f;

                    // Angularly offset the hover destination based on the time to make it harder to predict.
                    if (wrappedAttackTimer >= 9f)
                        offsetAngle = MathHelper.Lerp(0f, MathHelper.ToRadians(25f), Utils.GetLerpValue(9f, 24f, wrappedAttackTimer));

                    Vector2 destination = target.Center + new Vector2(xAimOffset, -890f).RotatedBy(offsetAngle) - npc.Center;
                    Vector2 idealVelocity = Vector2.Normalize(destination - npc.velocity) * 29.5f;
                    npc.SimpleFlyMovement(idealVelocity, 1f);
                    npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(target.Center) + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi, 0.1f);
                    specialFrameType = (int)YharonFrameDrawingType.FlapWings;
                }

                // Charge at the target.
                if (wrappedAttackTimer == 27f)
                {
                    npc.velocity = npc.SafeDirectionTo(target.Center) * confusingChargeSpeed;
                    npc.rotation = npc.AngleTo(target.Center) + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi;
                    specialFrameType = (int)YharonFrameDrawingType.IdleWings;
                    SoundEngine.PlaySound(YharonBoss.ShortRoarSound, npc.Center);
                }

                if (wrappedAttackTimer == chargeCycleTime - 1f)
                    npc.velocity *= PostChargeDecelerationFactor;

                // Define the total instance count; 2 clones and the original.
                npc.Infernum().ExtraAI[IllusionCountIndex] = 3f;
            }

            // Then, perform a series of carpet bombs all over the arena.
            else if (attackTimer <= totalCharges * chargeCycleTime + totalTimeSpentPerCarpetBomb)
            {
                // Begin to fade into magic sparkles.
                npc.Opacity = MathHelper.Lerp(npc.Opacity, 0.65f, 0.15f);
                npc.life = (int)MathHelper.Lerp(npc.life, npc.lifeMax * 0.0125f, 0.0125f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 sparkleSpawnPosition = npc.Center + Main.rand.NextVector2Circular(180f, 180f);
                    Utilities.NewProjectileBetter(sparkleSpawnPosition, Main.rand.NextVector2Circular(12f, 12f), ModContent.ProjectileType<YharonMajesticSparkle>(), 0, 0f);
                }

                // Fly towards the hover destination near the target.
                float adjustedAttackTimer = attackTimer - totalCharges * chargeCycleTime;
                float directionToDestination = (npc.Center.X > target.Center.X).ToDirectionInt();
                if (adjustedAttackTimer < splittingMeteorRiseTime)
                {
                    Vector2 destination = target.Center + new Vector2(directionToDestination * 750f, -300f);
                    Vector2 idealVelocity = npc.SafeDirectionTo(destination) * splittingMeteorHoverSpeed;

                    npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.035f);
                    npc.rotation = npc.rotation.AngleTowards(0f, 0.25f);

                    npc.spriteDirection = (npc.Center.X > target.Center.X).ToDirectionInt();

                    // Once it has been reached, change the attack timer to begin the carpet bombing.
                    if (npc.WithinRange(destination, 32f))
                        attackTimer = splittingMeteorRiseTime + totalCharges * chargeCycleTime - 1f;
                    specialFrameType = (int)YharonFrameDrawingType.FlapWings;
                }

                // Begin flying horizontally.
                else if (adjustedAttackTimer == splittingMeteorRiseTime)
                {
                    Vector2 chargeDirection = npc.SafeDirectionTo(target.Center);
                    chargeDirection.Y *= 0.15f;
                    chargeDirection = chargeDirection.SafeNormalize(Vector2.UnitX * npc.spriteDirection);

                    specialFrameType = (int)YharonFrameDrawingType.OpenMouth;

                    npc.velocity = chargeDirection * splittingMeteorBombingSpeed;
                }

                // And vomit meteors.
                else
                {
                    npc.position.X += npc.SafeDirectionTo(target.Center).X * 7f;
                    npc.position.Y += npc.SafeDirectionTo(target.Center + Vector2.UnitY * -400f).Y * 6f;
                    npc.spriteDirection = (npc.velocity.X < 0f).ToDirectionInt();
                    npc.rotation = npc.velocity.ToRotation() + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi;

                    if (adjustedAttackTimer % fireballReleaseRate == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(npc.Center + npc.velocity * 3f, npc.velocity, ModContent.ProjectileType<YharonFireball>(), 565, 0f, Main.myPlayer, 0f, 0f);

                    if (adjustedAttackTimer >= splittingMeteorRiseTime + splittingMeteorBombTime && totalMeteorBomings < 3)
                    {
                        attackTimer = totalCharges * chargeCycleTime;
                        totalMeteorBomings++;
                    }
                }
            }

            // Lastly, do multiple final, powerful charges.
            else if (attackTimer <= totalCharges * chargeCycleTime + totalTimeSpentPerCarpetBomb + totalBerserkCharges * berserkChargeTime)
            {
                finalAttackCompletionState = 0f;

                // Fade into magic sparkles more heavily.
                npc.Opacity = MathHelper.Lerp(npc.Opacity, 0.3f, 0.15f);
                npc.life = (int)MathHelper.Lerp(npc.life, npc.lifeMax * 0.005f, 0.025f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 sparkleSpawnPosition = npc.Center + Main.rand.NextVector2Circular(180f, 180f);
                        Utilities.NewProjectileBetter(sparkleSpawnPosition, Main.rand.NextVector2Circular(12f, 12f), ModContent.ProjectileType<YharonMajesticSparkle>(), 0, 0f);
                    }
                }

                // Gain a longer trail and even more intense fire.
                fireIntensity = 1.5f;

                // Hover and charge.
                float wrappedAttackTimer = attackTimer % berserkChargeTime;
                if (wrappedAttackTimer < 20)
                {
                    npc.spriteDirection = (npc.Center.X > target.Center.X).ToDirectionInt();
                    ref float xAimOffset = ref npc.Infernum().ExtraAI[0];
                    if (xAimOffset == 0f)
                        xAimOffset = 870f * Math.Sign((npc.Center - target.Center).X);

                    Vector2 destination = target.Center + new Vector2(xAimOffset, -890f) - npc.Center;
                    Vector2 idealVelocity = Vector2.Normalize(destination - npc.velocity) * 23f;
                    npc.SimpleFlyMovement(idealVelocity, 1f);
                    npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(target.Center) + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi, 0.1f);
                    specialFrameType = (int)YharonFrameDrawingType.FlapWings;
                }
                if (wrappedAttackTimer == 20)
                {
                    npc.velocity = npc.SafeDirectionTo(target.Center) * berserkChargeSpeed;
                    npc.rotation = npc.AngleTo(target.Center) + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi;
                    specialFrameType = (int)YharonFrameDrawingType.IdleWings;
                    SoundEngine.PlaySound(YharonBoss.ShortRoarSound, npc.Center);
                }
            }

            // After all final charges are complete, slow down, emit many sparkles/flames and fart explosions, and die.
            else
            {
                if (Main.netMode != NetmodeID.MultiplayerClient && hasTeleportedFlag == 0f)
                {
                    Vector2 oldPosition = npc.Center;
                    for (int i = 0; i < 15; i++)
                    {
                        Vector2 sparkleSpawnPosition = npc.Center + Main.rand.NextVector2Circular(280f, 280f);
                        Utilities.NewProjectileBetter(sparkleSpawnPosition, Main.rand.NextVector2Circular(42f, 42f), ModContent.ProjectileType<MajesticSparkleBig>(), 0, 0f);
                    }

                    npc.Center = target.Center - Vector2.UnitY * 1500f;
                    npc.velocity *= 0.3f;
                    Dust.QuickDustLine(oldPosition, npc.Center, 275, Color.Orange);
                    hasTeleportedFlag = 1f;
                    npc.netUpdate = true;
                }

                int preAttackTime = totalCharges * chargeCycleTime + totalTimeSpentPerCarpetBomb + totalBerserkCharges * 45;
                ref float pulseDeathEffectCooldown = ref npc.Infernum().ExtraAI[1];

                npc.Opacity = MathHelper.Lerp(npc.Opacity, 0.035f, 0.2f);
                if (finalAttackCompletionState != 1f)
                {
                    npc.spriteDirection = (npc.Center.X > target.Center.X).ToDirectionInt();
                    npc.velocity = npc.SafeDirectionTo(target.Center) * npc.Distance(target.Center) / 40f;
                    npc.rotation = npc.AngleTo(target.Center) + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi;
                    npc.netUpdate = true;
                    finalAttackCompletionState = 1f;
                }

                npc.damage = 0;
                npc.velocity *= 0.95f;
                npc.rotation = npc.rotation.AngleTowards(0f, 0.04f);
                npc.life = (int)MathHelper.Lerp(npc.life, 0, 0.007f);

                if (npc.life is <= 1000 and > 1)
                    npc.life = 1;

                specialFrameType = (int)YharonFrameDrawingType.FlapWings;
                if (lifeRatio < 0.004f && pulseDeathEffectCooldown <= 0)
                {
                    pulseDeathEffectCooldown = 8f;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 15; i++)
                        {
                            Vector2 sparkleSpawnPosition = npc.Center + Main.rand.NextVector2Circular(280f, 280f);
                            Utilities.NewProjectileBetter(sparkleSpawnPosition, Main.rand.NextVector2Circular(42f, 42f), ModContent.ProjectileType<MajesticSparkleBig>(), 0, 0f);
                        }

                        if (Main.rand.NextBool(12))
                            Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<YharonBoom>(), 0, 0f);
                    }
                    SoundEngine.PlaySound(YharonBoss.ShortRoarSound, npc.Center);
                }
                else if (pulseDeathEffectCooldown > 0)
                {
                    specialFrameType = (int)YharonFrameDrawingType.OpenMouth;
                    pulseDeathEffectCooldown--;
                }

                if (npc.life < 2000 && hasCreatedExplosionFlag == 0f)
                {
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<YharonFlameExplosion>(), 0, 0f);

                    // Release a burst of very strong fireballs
                    for (int i = 0; i < 45; i++)
                    {
                        Vector2 fireallVelocity = (MathHelper.TwoPi * i / 45f).ToRotationVector2() * 11f;
                        int fireball = Utilities.NewProjectileBetter(npc.Center, fireallVelocity, ModContent.ProjectileType<FlareDust>(), 640, 0f);
                        if (Main.projectile.IndexInRange(fireball))
                        {
                            Main.projectile[fireball].owner = target.whoAmI;
                            Main.projectile[fireball].ai[0] = 2f;
                        }
                    }

                    hasCreatedExplosionFlag = 1f;
                    npc.netUpdate = true;
                }

                // Emit very strong fireballs.
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer > preAttackTime + 100f)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        int fireball = Utilities.NewProjectileBetter(npc.Center, Main.rand.NextVector2CircularEdge(19f, 19f), ModContent.ProjectileType<FlareDust>(), 640, 0f);
                        if (Main.projectile.IndexInRange(fireball))
                        {
                            Main.projectile[fireball].owner = target.whoAmI;
                            Main.projectile[fireball].ai[0] = 2f;
                        }
                    }
                }

                if (npc.life <= 0)
                {
                    npc.life = 0;
                    npc.HitEffect();
                    npc.checkDead();
                    npc.active = false;

                    // YOU SHALL HAVE HEARD MY FINAL DYINNNG ROOOOARRRRR
                    SoundEngine.PlaySound(YharonBoss.RoarSound, npc.Center);
                }
            }
        }

        public static void ClearAllEntities()
        {
            int[] projectilesToDelete = new int[]
            {
                ProjectileID.CultistBossFireBall,
                ModContent.ProjectileType<YharonFireball>(),
                ModContent.ProjectileType<YharonFireball2>(),
                ModContent.ProjectileType<Infernado>(),
                ModContent.ProjectileType<Infernado2>(),
                ModContent.ProjectileType<Flare>(),
                ModContent.ProjectileType<BigFlare>(),
                ModContent.ProjectileType<BigFlare2>(),
                ModContent.ProjectileType<YharonHeatFlashFireball>(),
                ModContent.ProjectileType<VortexOfFlame>()
            };
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && projectilesToDelete.Contains(Main.projectile[i].type))
                {
                    Main.projectile[i].active = false;
                    Main.projectile[i].netUpdate = true;
                }
            }
        }

        public static bool ArenaSpawnAndEnrageCheck(NPC npc, Player player)
        {
            ref float enraged01Flag = ref npc.ai[2];
            ref float spawnedArena01Flag = ref npc.ai[3];

            // Create the arena, but not as a multiplayer client.
            // In single player, the arena gets created and never gets synced because it's single player.
            if (spawnedArena01Flag == 0f)
            {
                spawnedArena01Flag = 1f;
                enraged01Flag = 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int width = 9000;
                    npc.Infernum().Arena.X = (int)(player.Center.X - width * 0.5f);
                    npc.Infernum().Arena.Y = (int)(player.Center.Y - 160000f);
                    npc.Infernum().Arena.Width = width;
                    npc.Infernum().Arena.Height = 320000;

                    Projectile.NewProjectile(npc.GetSource_FromAI(), player.Center.X + width * 0.5f, player.Center.Y + 100f, 0f, 0f, ModContent.ProjectileType<SkyFlareRevenge>(), 0, 0f, Main.myPlayer, 0f, 0f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), player.Center.X - width * 0.5f, player.Center.Y + 100f, 0f, 0f, ModContent.ProjectileType<SkyFlareRevenge>(), 0, 0f, Main.myPlayer, 0f, 0f);
                }

                // Force Yharon to send a sync packet so that the arena gets sent immediately
                npc.netUpdate = true;
            }
            // Enrage code doesn't run on frame 1 so that Yharon won't be enraged for 1 frame in multiplayer
            else
            {
                var arena = npc.Infernum().Arena;
                enraged01Flag = (!player.Hitbox.Intersects(arena)).ToInt();
                if (enraged01Flag == 1f)
                    return true;
            }
            return false;
        }

        public static void SelectNextAttack(NPC npc, ref float attackType)
        {
            ref float attackTypeIndex = ref npc.Infernum().ExtraAI[AttackCycleIndexIndex];
            ref float teleportChargeCounter = ref npc.Infernum().ExtraAI[TeleportChargeCounterIndex];
            attackTypeIndex++;

            if ((YharonAttackType)(int)attackType == YharonAttackType.TeleportingCharge)
                teleportChargeCounter++;
            else
                teleportChargeCounter = 0f;

            bool patternExists = SubphaseTable.Any(table => table.Value(npc));
            YharonAttackType[] patternToUse = !patternExists ? SubphaseTable.First().Key : SubphaseTable.First(table => table.Value(npc)).Key;
            attackType = (int)patternToUse[(int)(attackTypeIndex % patternToUse.Length)];

            // Reset the attack timer and subphase specific variables.
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
        }
        #endregion

        #region Frames and Drawcode
        public override void FindFrame(NPC npc, int frameHeight)
        {
            if ((YharonAttackType)(int)npc.ai[0] == YharonAttackType.SpawnEffects)
            {
                // Open mouth for a little bit.
                if (npc.frameCounter is >= 30 and <= 40)
                    npc.frame.Y = 0;

                // Otherwise flap wings.
                else if (npc.frameCounter % 5 == 4)
                {
                    npc.frame.Y += frameHeight;

                    if (npc.frame.Y >= frameHeight * 5)
                        npc.frame.Y = 0;
                }
            }
            else
            {
                switch ((YharonFrameDrawingType)npc.Infernum().ExtraAI[SpecialFrameTypeIndex])
                {
                    case YharonFrameDrawingType.FlapWings:
                        if (npc.frameCounter % 6 == 5)
                        {
                            npc.frame.Y += frameHeight;
                            if (npc.frame.Y >= 4 * frameHeight)
                                npc.frame.Y = 0;
                        }
                        break;
                    case YharonFrameDrawingType.IdleWings:
                        npc.frame.Y = 5 * frameHeight;
                        break;
                    case YharonFrameDrawingType.Roar:
                        if (npc.frameCounter % 18 < 9)
                            npc.frame.Y = 5 * frameHeight;
                        else npc.frame.Y = 6 * frameHeight;
                        break;
                    case YharonFrameDrawingType.OpenMouth:
                        npc.frame.Y = 5 * frameHeight;
                        break;
                }
            }
            npc.frameCounter++;
        }

        public static void DrawInstance(NPC npc, Vector2? position = null, float rotationOffset = 0f, bool changeDirection = false)
        {
            YharonAttackType attackType = (YharonAttackType)npc.ai[0];
            Texture2D tex = ModContent.Request<Texture2D>(npc.ModNPC.Texture).Value;

            // Use defaults for the draw position.
            position ??= npc.Center;

            // Define draw variables.
            Vector2 origin = npc.frame.Size() * 0.5f;
            SpriteEffects spriteEffects = npc.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            if (changeDirection)
                spriteEffects = npc.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            // Determine variables for the fire effect.
            int afterimageCount = 1;
            float afterimageOffsetMax = 32f;
            float fireIntensity = npc.Infernum().ExtraAI[FireFormInterpolantIndex];
            bool inLastSubphases = npc.life / (float)npc.lifeMax <= 0.2f;
            if (inLastSubphases)
                fireIntensity = MathHelper.Max(fireIntensity, 0.8f);

            if (fireIntensity > 0f)
                afterimageCount += (int)(fireIntensity * 8f);

            Main.spriteBatch.EnterShaderRegion();

            Color burnColor = Color.Orange;
            float phase2InvincibilityCountdown = npc.Infernum().ExtraAI[InvincibilityTimerIndex];
            if (phase2InvincibilityCountdown > 0f)
            {
                float backBackToRegularColor = Utils.GetLerpValue(75f, 0f, phase2InvincibilityCountdown, true);
                Color phase2Color = Color.Lerp(Color.Pink, Color.Yellow, (float)Math.Cos(Main.GlobalTimeWrappedHourly * 2.3f) * 0.5f + 0.5f);
                burnColor = Color.Lerp(phase2Color, burnColor, backBackToRegularColor);
            }
            if (npc.life < npc.lifeMax * 0.2f)
                burnColor = Color.LightYellow;
            if (attackType == YharonAttackType.FireTrailCharge)
                burnColor = Color.OrangeRed;
            if (npc.life < npc.lifeMax * 0.075f)
            {
                burnColor = Color.White;
                fireIntensity += 0.325f;
            }

            GameShaders.Misc["Infernum:YharonBurn"].UseOpacity(fireIntensity * 0.7f);
            GameShaders.Misc["Infernum:YharonBurn"].SetShaderTexture(ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/CultistRayMap"));
            GameShaders.Misc["Infernum:YharonBurn"].UseColor(burnColor * 0.7f);
            GameShaders.Misc["Infernum:YharonBurn"].UseSecondaryColor(Color.White * 0.12f);
            GameShaders.Misc["Infernum:YharonBurn"].Shader.Parameters["uTimeFactor"].SetValue(1.1f);
            GameShaders.Misc["Infernum:YharonBurn"].Shader.Parameters["uZoomFactor"].SetValue(new Vector2(1f, 1f));
            GameShaders.Misc["Infernum:YharonBurn"].Shader.Parameters["uNoiseReadZoomFactor"].SetValue(new Vector2(0.2f, 0.2f));
            GameShaders.Misc["Infernum:YharonBurn"].Shader.Parameters["uSecondaryLavaPower"].SetValue(10f);
            GameShaders.Misc["Infernum:YharonBurn"].Shader.Parameters["uZoomFactorSecondary"].SetValue(0.5f);
            GameShaders.Misc["Infernum:YharonBurn"].Shader.Parameters["uNPCRectangle"].SetValue(new Vector4(npc.frame.X, npc.frame.Y, npc.frame.Width, npc.frame.Height));
            GameShaders.Misc["Infernum:YharonBurn"].Shader.Parameters["uActualImageSize0"].SetValue(tex.Size());
            GameShaders.Misc["Infernum:YharonBurn"].Apply();

            float opacity = npc.Opacity;

            // Draw backglow textures.
            if (fireIntensity > 0f)
            {
                for (int i = afterimageCount - 1; i >= 0; i--)
                {
                    float afterimageOpacity = 1f;
                    if (afterimageCount >= 2)
                        afterimageOpacity = i / (float)(afterimageCount - 1f);

                    Color color = npc.GetAlpha(Color.White) * (1f - afterimageOpacity);
                    Color afterimageColor = color;
                    Vector2 drawPosition = position.Value - Main.screenPosition;
                    drawPosition -= npc.velocity * 0.6f * i;
                    for (int j = 0; j < 6; j++)
                    {
                        Vector2 circularDrawOffset = (MathHelper.TwoPi * j / 6f).ToRotationVector2() * fireIntensity * afterimageOpacity * afterimageOffsetMax;
                        Color offsetColor = afterimageColor * opacity * 0.4f;
                        offsetColor.A = 0;
                        Main.spriteBatch.Draw(tex, drawPosition + circularDrawOffset, npc.frame, offsetColor, npc.rotation + rotationOffset, origin, npc.scale, spriteEffects, 0f);
                    }
                }
            }

            // Draw afterimages.
            for (int i = afterimageCount - 1; i >= 0; i--)
            {
                float afterimageOpacity = 0f;
                if (afterimageCount >= 2)
                    afterimageOpacity = i / (float)(afterimageCount - 1f);

                Color color = npc.GetAlpha(Color.White) * (1f - afterimageOpacity);
                Color afterimageColor = color;
                if (i == 0 && afterimageCount >= 2)
                    afterimageColor.A = 184;

                Vector2 drawPosition = position.Value - Main.screenPosition;
                drawPosition -= npc.velocity * i * 1.16f;

                Main.spriteBatch.Draw(tex, drawPosition, npc.frame, afterimageColor * opacity, npc.rotation + rotationOffset, origin, npc.scale, spriteEffects, 0f);
            }

            Main.spriteBatch.ExitShaderRegion();
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            int illusionCount = (int)npc.Infernum().ExtraAI[IllusionCountIndex];
            if (illusionCount > 0)
            {
                Player player = Main.player[npc.target];
                for (int i = 0; i < illusionCount; i++)
                {
                    float offsetAngle = MathHelper.TwoPi * i / illusionCount;
                    float distanceFromPlayer = npc.Distance(player.Center);
                    Vector2 directionFromPlayer = npc.DirectionFrom(player.Center);
                    Vector2 drawPosition = Main.player[npc.target].Center + directionFromPlayer.RotatedBy(offsetAngle) * distanceFromPlayer;
                    DrawInstance(npc, drawPosition, offsetAngle, (drawPosition.X > player.Center.X).ToDirectionInt() != npc.spriteDirection);
                }
            }
            else
                DrawInstance(npc);

            // Draw the death animation white twinkle effect.
            float giantTwinkleSize = Utils.GetLerpValue(2650f, 2000f, npc.life, true) * Utils.GetLerpValue(1500f, 2000f, npc.life, true);
            if (giantTwinkleSize > 0f)
            {
                float twinkleScale = giantTwinkleSize * 4.75f;
                Texture2D twinkleTexture = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/LargeStar").Value;
                Vector2 drawPosition = npc.Center - Main.screenPosition;
                float secondaryTwinkleRotation = Main.GlobalTimeWrappedHourly * 7.13f;

                Main.spriteBatch.SetBlendState(BlendState.Additive);

                for (int i = 0; i < 2; i++)
                {
                    Main.spriteBatch.Draw(twinkleTexture, drawPosition, null, Color.White, 0f, twinkleTexture.Size() * 0.5f, twinkleScale * new Vector2(1f, 1.85f), SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(twinkleTexture, drawPosition, null, Color.White, secondaryTwinkleRotation, twinkleTexture.Size() * 0.5f, twinkleScale * new Vector2(1.3f, 1f), SpriteEffects.None, 0f);
                }
                Main.spriteBatch.ResetBlendState();
            }

            return false;
        }
        #endregion
    }
}