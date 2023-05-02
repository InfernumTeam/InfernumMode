using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.NPCs;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Boss;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics;
using InfernumMode.Content.Projectiles.Pets;
using InfernumMode.Core;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using InfernumMode.Core.GlobalInstances;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.GameContent.Events;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

using YharonBoss = CalamityMod.NPCs.Yharon.Yharon;
using InfernumMode.Content.Skies;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Content.Dusts;
using InfernumMode.Assets.Sounds;
using InfernumMode.Content.Projectiles;
using InfernumMode.Content.Projectiles.Generic;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Yharon
{
    public class YharonBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<YharonBoss>();

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
            MassiveInfernadoSummon,
            TeleportingCharge,

            EnterSecondPhase,
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

        public static readonly YharonAttackType[] Subphase2Pattern = new YharonAttackType[]
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
            YharonAttackType.FastCharge,
            YharonAttackType.FastCharge,
            YharonAttackType.FireTrailCharge,
            YharonAttackType.FastCharge,
            YharonAttackType.FireTrailCharge,
            YharonAttackType.FastCharge,
            YharonAttackType.FlamethrowerAndMeteors,
            YharonAttackType.MassiveInfernadoSummon,
            YharonAttackType.FireballBurst,
            YharonAttackType.FireTrailCharge,
            YharonAttackType.FastCharge,
            YharonAttackType.FireTrailCharge,
            YharonAttackType.FastCharge,
            YharonAttackType.FlamethrowerAndMeteors,
            YharonAttackType.MassiveInfernadoSummon,
            YharonAttackType.FlarenadoAndDetonatingFlameSpawn,
            YharonAttackType.FireballBurst,
        };

        public static readonly YharonAttackType[] Subphase3Pattern = new YharonAttackType[]
        {
            YharonAttackType.FastCharge,
            YharonAttackType.TeleportingCharge,
            YharonAttackType.TeleportingCharge,
            YharonAttackType.FlamethrowerAndMeteors,
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
            YharonAttackType.FastCharge,
            YharonAttackType.FastCharge,
            YharonAttackType.TeleportingCharge,
            YharonAttackType.TeleportingCharge,
            YharonAttackType.FastCharge,
            YharonAttackType.MassiveInfernadoSummon,
            YharonAttackType.FastCharge,
        };

        public static readonly YharonAttackType[] Subphase4Pattern = new YharonAttackType[]
        {
            YharonAttackType.CarpetBombing,
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

        public static readonly YharonAttackType[] Subphase5Pattern = new YharonAttackType[]
        {
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

        public static readonly YharonAttackType[] Subphase6Pattern = new YharonAttackType[]
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
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.HeatFlashRing,
            YharonAttackType.CarpetBombing,
        };

        public static readonly YharonAttackType[] Subphase7Pattern = new YharonAttackType[]
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
                return yharon.Infernum().ExtraAI[HasEnteredPhase2Index] == 1f && yharon.ai[0] != (int)YharonAttackType.EnterSecondPhase;
            }
            set
            {
                if (GlobalNPCOverrides.Yharon == -1 || !Main.npc[GlobalNPCOverrides.Yharon].active)
                    return;

                Main.npc[GlobalNPCOverrides.Yharon].Infernum().ExtraAI[HasEnteredPhase2Index] = value.ToInt();
            }
        }

        public const float Subphase2LifeRatio = 0.75f;

        public const float Subphase3LifeRatio = 0.45f;

        public const float Subphase4LifeRatio = Phase2LifeRatio;

        public const float Subphase5LifeRatio = 0.8f;

        public const float Subphase6LifeRatio = 0.4f;

        public const float Subphase7LifeRatio = 0.15f;

        public const float Subphase8LifeRatio = 0.025f;

        public static readonly Dictionary<YharonAttackType[], Func<NPC, bool>> SubphaseTable = new()
        {
            [Subphase1Pattern] = (npc) => npc.life / (float)npc.lifeMax > Subphase2LifeRatio && !InSecondPhase,
            [Subphase2Pattern] = (npc) => npc.life / (float)npc.lifeMax > Subphase3LifeRatio && npc.life / (float)npc.lifeMax <= Subphase2LifeRatio && !InSecondPhase,
            [Subphase3Pattern] = (npc) => npc.life / (float)npc.lifeMax <= Subphase2LifeRatio && !InSecondPhase,

            [Subphase4Pattern] = (npc) => (npc.life / (float)npc.lifeMax > Subphase5LifeRatio || npc.Infernum().ExtraAI[InvincibilityTimerIndex] > 0f) && (InSecondPhase || npc.ai[0] == (int)YharonAttackType.SpawnEffects),
            [Subphase5Pattern] = (npc) => npc.life / (float)npc.lifeMax > Subphase6LifeRatio && npc.life / (float)npc.lifeMax <= Subphase5LifeRatio && InSecondPhase,
            [Subphase6Pattern] = (npc) => npc.life / (float)npc.lifeMax > Subphase7LifeRatio && npc.life / (float)npc.lifeMax <= Subphase6LifeRatio && InSecondPhase,
            [Subphase7Pattern] = (npc) => npc.life / (float)npc.lifeMax > Subphase8LifeRatio && npc.life / (float)npc.lifeMax <= Subphase7LifeRatio && InSecondPhase,
            [LastSubphasePattern] = (npc) => npc.life / (float)npc.lifeMax <= Subphase8LifeRatio && InSecondPhase,
        };

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Subphase2LifeRatio,
            Subphase3LifeRatio,
            Subphase4LifeRatio,
            Subphase5LifeRatio,
            Subphase6LifeRatio,
            Subphase7LifeRatio,
            Subphase8LifeRatio
        };
        #endregion

        public const int TransitionDRBoostTime = 120;

        public const int Phase2InvincibilityTime = 360;

        // Various look-up constants for ExtraAI variables.
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

        public const int TeleportOffsetAngleIndex = 20;

        public const int HasGottenNearPlayerIndex = 21;

        public const int PlayerChargeMarkCenterXIndex = 22;

        public const int PlayerChargeMarkCenterYIndex = 23;

        public const int FadeToAshInterpolantIndex = 24;

        public const float Phase2LifeRatio = 0.1f;

        public const float BaseDR = 0.3f;

        // Factor for how much Yharon deceleratrs once a charge concludes.
        // This exists as a way of reducing Yharon's momentum after a charge so that he can more easily get into position for the next charge.
        // The closer to 1 this value is, the quicker his charges will be.
        public const float PostChargeDecelerationFactor = 0.42f;

        public const int RegularFireballDamage = 500;

        public const int FlamethrowerDamage = 550;

        public const int InfernadoDamage = 550;

        public const int HeatFlashFireballDamage = 575;

        public const int DeathAnimationFireballDamage = 600;

        public const int FlameVortexDamage = 750;

        #region Loading
        public override void Load()
        {
            GlobalNPCOverrides.BossHeadSlotEvent += DisableMapIconDuringDesperation;
            GlobalNPCOverrides.StrikeNPCEvent += DisableNaturalYharonDeath;
            GlobalNPCOverrides.OnKillEvent += DisplayAEWNotificationText;
        }

        private void DisplayAEWNotificationText(NPC npc)
        {
            if (DownedBossSystem.downedYharon || npc.type != ModContent.NPCType<YharonBoss>())
                return;

            Utilities.DisplayText("A primordial light shimmers at the nadir of the abyssal depths...", Color.Lerp(Color.LightCoral, Color.Wheat, 0.6f));
        }

        private void DisableMapIconDuringDesperation(NPC npc, ref int index)
        {
            // Prevent Yharon from showing himself amongst his illusions in the desperation phase.
            if (npc.type == ModContent.NPCType<YharonBoss>())
            {
                if (npc.life / (float)npc.lifeMax <= Subphase8LifeRatio && InSecondPhase)
                    index = -1;
            }
        }

        private bool DisableNaturalYharonDeath(NPC npc, ref double damage, int realDamage, int defense, ref float knockback, int hitDirection, ref bool crit)
        {
            if (npc.type == ModContent.NPCType<YharonBoss>())
            {
                if (npc.life - realDamage <= 0)
                    npc.NPCLoot();
            }
            return false;
        }
        #endregion Loading

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

            // Set music.
            CalamityGlobalNPC.yharon = npc.whoAmI;
            CalamityGlobalNPC.yharonP2 = -1;

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
            ref float teleportOffsetAngle = ref npc.Infernum().ExtraAI[TeleportOffsetAngleIndex];
            ref float hasGottenNearPlayer = ref npc.Infernum().ExtraAI[HasGottenNearPlayerIndex];

            // Go to phase 2 if close to death.
            if (npc.Infernum().ExtraAI[HasEnteredPhase2Index] == 0f && lifeRatio < Phase2LifeRatio)
            {
                HatGirl.SayThingWhileOwnerIsAlive(target, "Better stay near the edges of the arena during those carpet bomb flames, That should keep them out of the way!");

                // Set Yharon's private phase 2 flag that base Calamity uses.
                // This is necessary to ensure that the special phase 2 name is used.
                typeof(YharonBoss).GetField("startSecondAI", Utilities.UniversalBindingFlags).SetValue(npc.ModNPC, true);

                npc.Infernum().ExtraAI[HasEnteredPhase2Index] = 1f;

                // Enter the second phase animation state.
                npc.Infernum().ExtraAI[AttackCycleIndexIndex] = 0f;
                SelectNextAttack(npc, ref attackType);
                attackType = (int)YharonAttackType.EnterSecondPhase;

                // Spawn a lot of cool sparkles.
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

            if (InSecondPhase)
            {
                CalamityGlobalNPC.yharon = -1;
                CalamityGlobalNPC.yharonP2 = npc.whoAmI;
            }

            // Perform the aforementioned attack pattern lookup.
            YharonAttackType[] patternToUse = SubphaseTable.First(table => table.Value(npc)).Key;
            float oldSubphase = currentSubphase;
            currentSubphase = SubphaseTable.Keys.ToList().IndexOf(patternToUse);
            YharonAttackType nextAttackType = patternToUse[(int)((attackType + 1) % patternToUse.Length)];

            // Transition to the next subphase if necessary.
            if (oldSubphase != currentSubphase && attackType != (int)YharonAttackType.EnterSecondPhase)
            {
                // Clear away projectiles in subphase 4 and 7.
                if (Main.netMode != NetmodeID.MultiplayerClient && (currentSubphase == 3f || currentSubphase == 6f))
                {
                    attackTimer = 0f;
                    if (currentSubphase == 8f)
                    {
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<YharonBoom>(), 0, 0f);
                        shouldPerformBerserkCharges = 1f;
                        npc.netUpdate = true;
                    }
                    ClearAllEntities();
                }

                // Reset the attack cycle for the next subphase.
                if (currentSubphase != 3f)
                {
                    subphaseTransitionTimer = TransitionDRBoostTime;
                    transitionDRCountdown = TransitionDRBoostTime;
                    npc.Infernum().ExtraAI[AttackCycleIndexIndex] = -1f;
                    SelectNextAttack(npc, ref attackType);
                }

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

            // Perform various screen shader manipulations.
            // Ensure this isnt done on the server, as it will throw a null reference error.
            if (Main.netMode != NetmodeID.Server)
            {
                Filters.Scene["HeatDistortion"].GetShader().UseIntensity(0.5f);

                // This screen shader kind of sucks. Please turn it off.
                Filters.Scene["CalamityMod:Yharon"].Deactivate();
            }

            // Slow down and transition to the next subphase as necessary.
            // Following this Yharon will recieve a DR boost. The code for this up a small bit.
            if (subphaseTransitionTimer > 0 && attackType != (int)YharonAttackType.EnterSecondPhase)
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
                    if (subphaseTransitionTimer == 9f && currentSubphase != 3f)
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

                // If not, and Yharon isn't performing a heat-based attack, have the fire intensity naturally dissipate.
                // Certain attacks may override this manually.
                else if (nextAttackType is not YharonAttackType.PhoenixSupercharge and not YharonAttackType.HeatFlashRing)
                    fireIntensity = MathHelper.Lerp(fireIntensity, 0f, 0.075f);
            }

            // Adjust various values before doing anything else. If these need to be changed later in certain attacks, they will be.
            npc.dontTakeDamage = false;
            npc.Infernum().ExtraAI[IllusionCountIndex] = 0f;

            // Define various attack-specific variables.
            float chargeSpeed = 46f;
            int chargeDelay = 32;
            int chargeTime = 28;
            float fastChargeSpeedMultiplier = 1.4f;

            int fireballBreathShootDelay = 34;
            int totalFireballBreaths = 12;
            float fireballBreathShootRate = 5f;

            int infernadoAttackPowerupTime = 90;

            float splittingMeteorBombingSpeed = 30f;
            int splittingMeteorRiseTime = 90;
            int splittingMeteorBombTime = 72;

            // Determine important phase variables.
            bool berserkChargeMode = shouldPerformBerserkCharges == 1f;
            if (!berserkChargeMode)
            {
                berserkChargeMode = InSecondPhase && lifeRatio >= Subphase8LifeRatio && lifeRatio < Subphase7LifeRatio && attackType != (float)YharonAttackType.PhoenixSupercharge && invincibilityTime <= 0f;
                shouldPerformBerserkCharges = berserkChargeMode.ToInt();
            }
            if (berserkChargeMode)
            {
                berserkChargeMode = lifeRatio >= Subphase6LifeRatio;
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

            // Buff charges in subphase 4 and onwards.
            if (currentSubphase >= 3f)
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

                splittingMeteorBombingSpeed = 40f;
            }

            // Reset damage to its default value as it was at the time the NPC was created.
            // Further npc damage manipulation can be done later if necessary.
            else
                npc.damage = npc.defDamage;

            // Buff charges if in phase 2.
            if (InSecondPhase)
            {
                chargeDelay = (int)(chargeDelay * 0.8);
                chargeSpeed += 2.7f;
                fastChargeSpeedMultiplier += 0.08f;
            }

            // Multiplicatively reduce the fast charge speed multiplier. This is easier than changing individual variables above when testing.
            fastChargeSpeedMultiplier *= 0.875f;

            // Disable damage for a while and heal in phase 2. Also release some sparkles for visual flair.
            if (invincibilityTime > 0f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient && invincibilityTime <= Phase2InvincibilityTime - 5f)
                {
                    Vector2 sparkleSpawnPosition = npc.Center + Main.rand.NextVector2Circular(180f, 180f);
                    Utilities.NewProjectileBetter(sparkleSpawnPosition, Main.rand.NextVector2Circular(12f, 12f), ModContent.ProjectileType<YharonMajesticSparkle>(), 0, 0f);
                }
                npc.dontTakeDamage = true;
                invincibilityTime--;

                // Heal up again.
                npc.life = (int)MathHelper.Lerp(npc.lifeMax * 0.1f, npc.lifeMax, 1f - invincibilityTime / Phase2InvincibilityTime);
            }

            // Create blossoms from the sky if in the last subphase.
            if (currentSubphase == 6f && Main.rand.NextBool(3) && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 blossomSpawnPosition = target.Center + new Vector2(Main.rand.NextFloatDirection() * 1000f, -800f);
                Vector2 blossomVelocity = Vector2.UnitY.RotatedByRandom(1.23f) * Main.rand.NextFloat(0.5f, 6f);
                Projectile.NewProjectile(npc.GetSource_FromThis(), blossomSpawnPosition, blossomVelocity, ModContent.ProjectileType<DraconicBlossomPetal>(), 0, 0f, target.whoAmI);
            }

            switch ((YharonAttackType)(int)attackType)
            {
                // The attack only happens when Yharon spawns.
                case YharonAttackType.SpawnEffects:
                    DoBehavior_SpawnEffects(npc, target, ref attackType, ref attackTimer, ref specialFrameType, ref fireIntensity);
                    break;
                case YharonAttackType.Charge:
                case YharonAttackType.TeleportingCharge:
                case YharonAttackType.FireTrailCharge:
                    DoBehavior_ChargesAndTeleportCharges(npc, target, chargeDelay, chargeTime, chargeSpeed, teleportChargeCounter, ref fireIntensity, ref attackTimer, ref attackType, ref specialFrameType, ref teleportOffsetAngle, ref hasGottenNearPlayer);
                    break;
                case YharonAttackType.FastCharge:
                case YharonAttackType.PhoenixSupercharge:
                    DoBehavior_FastCharges(npc, target, berserkChargeMode, chargeDelay, chargeTime, chargeSpeed * fastChargeSpeedMultiplier, ref fireIntensity, ref attackTimer, ref attackType, ref specialFrameType, ref hasGottenNearPlayer);
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
                case YharonAttackType.MassiveInfernadoSummon:
                    DoBehavior_MassiveInfernadoSummon(npc, infernadoAttackPowerupTime, ref attackTimer, ref attackType, ref specialFrameType);
                    break;

                case YharonAttackType.EnterSecondPhase:
                    DoBehavior_EnterSecondPhase(npc, target, ref attackType, ref attackTimer, ref specialFrameType, ref fireIntensity, ref invincibilityTime);
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

        public static void MarkChargeZone(NPC npc, Player target)
        {
            npc.Infernum().ExtraAI[PlayerChargeMarkCenterXIndex] = target.Center.X;
            npc.Infernum().ExtraAI[PlayerChargeMarkCenterYIndex] = target.Center.Y;
            npc.netUpdate = true;
        }

        public static Vector2 GetChargeZone(NPC npc)
        {
            float x = npc.Infernum().ExtraAI[PlayerChargeMarkCenterXIndex];
            float y = npc.Infernum().ExtraAI[PlayerChargeMarkCenterYIndex];
            return new(x, y);
        }

        public static void DoBehavior_SpawnEffects(NPC npc, Player target, ref float attackType, ref float attackTimer, ref float specialFrameType, ref float fireIntensity)
        {
            int cameraPanDelay = 4;
            int cameraPanTime = 240;
            int wingFlapTime = 150;
            int chargeDelay = 25;
            int chargeTime = 24;

            // Disable damage.
            npc.dontTakeDamage = true;
            npc.damage = 0;

            // Teleport above the target on the first frame.
            if (attackTimer <= 2f)
            {
                npc.spriteDirection = (target.Center.X - npc.Center.X < 0).ToDirectionInt();
                npc.Center = target.Center + new Vector2(target.direction * 1240f, -500f);
                npc.velocity = Vector2.UnitY * 27f;
                npc.netUpdate = true;
            }

            // Flap wings at first.
            if (attackTimer <= wingFlapTime)
            {
                fireIntensity = Utils.GetLerpValue(45f, wingFlapTime - 10f, attackTimer, true);
                specialFrameType = (int)YharonFrameDrawingType.FlapWings;

                // Create sparkles in accordance to the fire intensity.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < (int)(fireIntensity * 9f); i++)
                    {
                        Vector2 sparkleSpawnPosition = npc.Center + Main.rand.NextVector2Circular(180f, 180f);
                        Utilities.NewProjectileBetter(sparkleSpawnPosition, Main.rand.NextVector2Circular(12f, 12f), ModContent.ProjectileType<YharonMajesticSparkle>(), 0, 0f);
                    }
                }

                // Disable music.
                npc.ModNPC.Music = MusicLoader.GetMusicSlot(InfernumMode.Instance, "Sounds/Music/Nothing");
                CalamityGlobalNPC.yharon = -1;
            }
            else
            {
                specialFrameType = (int)YharonFrameDrawingType.OpenMouth;
                if (attackTimer == wingFlapTime + 1f)
                {
                    SoundEngine.PlaySound(YharonBoss.RoarSound);
                    ScreenEffectSystem.SetBlurEffect(npc.Center, 3f, 96);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<YharonBoom>(), 0, 0f);
                }
            }

            // Move the camera.
            if (attackTimer >= cameraPanDelay)
            {
                float cameraPanInterpolant = Utils.GetLerpValue(cameraPanDelay, cameraPanDelay + 10f, attackTimer, true) * Utils.GetLerpValue(cameraPanTime, cameraPanTime - 10f, attackTimer, true);
                target.Infernum_Camera().ScreenFocusInterpolant = cameraPanInterpolant;
                target.Infernum_Camera().ScreenFocusPosition = npc.Center;
            }

            // Perform the charge after camera effects are done.
            if (attackTimer == cameraPanTime + chargeDelay)
            {
                SoundEngine.PlaySound(YharonBoss.OrbSound);
                ScreenEffectSystem.SetFlashEffect(npc.Center, 3f, 30);
                npc.velocity = npc.SafeDirectionTo(target.Center) * 36f;
                npc.netUpdate = true;
            }

            if (attackTimer <= cameraPanTime + chargeDelay)
            {
                // Decelerate.
                npc.velocity *= 0.93f;
            }
            else if (attackTimer <= cameraPanTime + chargeTime + chargeDelay)
            {
                // Accelerate and rotate after the charge.
                npc.velocity *= 1.044f;
                npc.rotation = npc.velocity.ToRotation();
                if (npc.spriteDirection == 1)
                    npc.rotation += MathHelper.Pi;
            }
            else
            {
                npc.Infernum().ExtraAI[AttackCycleIndexIndex] = -1f;
                SelectNextAttack(npc, ref attackType);
            }
        }

        public static void DoBehavior_ChargesAndTeleportCharges(NPC npc, Player target, float chargeDelay, float chargeTime, float chargeSpeed, float teleportChargeCounter, ref float fireIntensity,
            ref float attackTimer, ref float attackType, ref float specialFrameType, ref float offsetDirection, ref float hasGottenNearPlayer)
        {
            float teleportOffset = 560f;
            bool teleporting = (YharonAttackType)(int)attackType == YharonAttackType.TeleportingCharge;
            bool releaseFire = (YharonAttackType)(int)attackType == YharonAttackType.FireTrailCharge;
            float predictivenessFactor = 0f;
            if (!teleporting)
            {
                chargeDelay = (int)(chargeDelay * 0.8f);
                predictivenessFactor = 4.25f;
            }
            else
                chargeDelay += 26f;

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

                // Prepare the teleport position.
                if (attackTimer == (int)(chargeDelay * 0.3f) && teleporting)
                {
                    offsetDirection = target.velocity.SafeNormalize(Main.rand.NextVector2Unit()).ToRotation();

                    // If a teleport charge was done beforehand randomize the offset direction if the
                    // player is descending. This still has an uncommon chance to end up in a similar direction as the one
                    // initially chosen.
                    if (teleportChargeCounter > 0f && Math.Abs(offsetDirection) < MathHelper.Pi / 15f)
                    {
                        do
                        {
                            offsetDirection = Main.rand.NextFloat(MathHelper.TwoPi);
                        }
                        while (Math.Abs(Vector2.Dot(offsetDirection.ToRotationVector2(), Vector2.UnitY)) > 0.6f);
                    }
                }

                // Create the teleport telegraph.
                if (attackTimer < chargeDelay - 30f && attackTimer >= chargeDelay * 0.3f && teleporting)
                {
                    Vector2 teleportPosition = target.Center + offsetDirection.ToRotationVector2() * teleportOffset;
                    for (int i = 0; i < 6; i++)
                    {
                        Color fireColor = Main.rand.NextBool() ? Color.White : Color.Yellow;
                        CloudParticle fireCloud = new(teleportPosition, Main.rand.NextVector2Circular(12f, 12f), fireColor * 0.85f, Color.DarkGray, 120, Main.rand.NextFloat(2f, 2.4f));
                        GeneralParticleHandler.SpawnParticle(fireCloud);

                        Dust fire = Dust.NewDustPerfect(teleportPosition + Main.rand.NextVector2Square(-96f, 96f), 75);
                        fire.velocity = -Vector2.UnitY.RotateRandom(0.6f) * Main.rand.NextFloat(1f, 10f);
                        fire.scale *= 1.66f;
                        fire.noGravity = true;
                    }
                }

                // Teleport prior to the charge happening if the attack calls for it.
                if (attackTimer == (int)(chargeDelay - 30f) && teleporting)
                {
                    npc.Center = target.Center + offsetDirection.ToRotationVector2() * teleportOffset;
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
                    Utilities.NewProjectileBetter(npc.Center + Main.rand.NextVector2Circular(16f, 16f), npc.velocity * 0.3f, ModContent.ProjectileType<LingeringDragonFlames>(), RegularFireballDamage, 0f);
            }

            // Slow down after sufficiently far away from the target.
            float slowdownRange = 800f;
            if (hasGottenNearPlayer != 1f && npc.WithinRange(target.Center, slowdownRange - 350f) && attackTimer >= chargeDelay + 25f)
            {
                hasGottenNearPlayer = 1f;
                MarkChargeZone(npc, target);
                npc.netUpdate = true;
            }

            if (attackTimer >= chargeDelay && !teleporting && !npc.WithinRange(GetChargeZone(npc), slowdownRange) && hasGottenNearPlayer == 1f)
                npc.velocity *= 0.95f;
        }

        public static void DoBehavior_FastCharges(NPC npc, Player target, bool berserkChargeMode, float chargeDelay, float chargeTime, float chargeSpeed, ref float fireIntensity, ref float attackTimer, ref float attackType, ref float specialFrameType, ref float hasGottenNearPlayer)
        {
            bool phoenixSupercharge = (YharonAttackType)(int)attackType == YharonAttackType.PhoenixSupercharge;
            if (phoenixSupercharge)
            {
                chargeDelay = (int)(chargeDelay * 0.8f);
                if (attackTimer == 1f)
                    HatGirl.SayThingWhileOwnerIsAlive(target, "This speed is crazy! Make sure you know when it starts; you might get jumpscared!");
            }
            else if (attackTimer == 1f)
                SoundEngine.PlaySound(YharonBoss.ShortRoarSound with { Pitch = -0.56f, Volume = 1.6f }, target.Center);

            // Slow down and rotate towards the player.
            if (attackTimer < chargeDelay)
            {
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                ref float xAimOffset = ref npc.Infernum().ExtraAI[0];
                if (xAimOffset == 0f)
                    xAimOffset = (berserkChargeMode ? 920f : 620f) * Math.Sign((npc.Center - target.Center).X);

                // Transform into a phoenix flame form if doing a phoenix supercharge.
                if (phoenixSupercharge)
                    fireIntensity = MathHelper.Max(fireIntensity, Utils.GetLerpValue(0f, chargeDelay - 1f, attackTimer, true));

                // Hover to the top left/right of the target and look at them.
                Vector2 destination = target.Center + new Vector2(xAimOffset, berserkChargeMode ? -480f : -240f);
                Vector2 idealVelocity = npc.SafeDirectionTo(destination - npc.velocity) * 18.5f;
                npc.SimpleFlyMovement(idealVelocity, 0.54f);

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
                YharonSky.CreateSmokeBurst();

                npc.netUpdate = true;
            }

            // Create sparkles and create heat distortion when charging if doing a phoenix supercharge.
            else if (phoenixSupercharge && attackTimer < chargeDelay + chargeTime)
            {
                fireIntensity = 1f;
                float competionRatio = Utils.GetLerpValue(chargeDelay, chargeDelay + chargeTime, attackTimer, true);

                // Ensure this isnt loaded on the server, as it will throw a null reference error.
                if (Main.netMode != NetmodeID.Server)
                    Filters.Scene["HeatDistortion"].GetShader().UseIntensity(0.5f + CalamityUtils.Convert01To010(competionRatio) * 3f);
                if (Main.netMode != NetmodeID.MultiplayerClient && npc.Infernum().ExtraAI[SubphaseIndexIndex] <= 5f)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 sparkleSpawnPosition = npc.Center + Main.rand.NextVector2Circular(240f, 240f);
                        Utilities.NewProjectileBetter(sparkleSpawnPosition, Main.rand.NextVector2Circular(28f, 28f), ModContent.ProjectileType<MajesticSparkleBig>(), 0, 0f);
                    }
                }

                // Make any draconic petals move a bit forward.
                foreach (Projectile petal in Utilities.AllProjectilesByID(ModContent.ProjectileType<DraconicBlossomPetal>()))
                {
                    float distanceFromYharon = petal.Distance(npc.Center);
                    float distanceTaper = Utils.GetLerpValue(600f, 350f, distanceFromYharon, true);
                    petal.velocity += npc.velocity.RotatedByRandom(1.02f) * Main.rand.NextFloat(0.01f, 0.02f) * distanceTaper;
                }
            }

            // Slow down after sufficiently far away from the target.
            float slowdownRange = 800f;
            if (hasGottenNearPlayer != 1f && npc.WithinRange(target.Center, slowdownRange - 350f) && attackTimer >= chargeDelay + 23f)
            {
                hasGottenNearPlayer = 1f;
                MarkChargeZone(npc, target);
                npc.netUpdate = true;
            }

            if (attackTimer >= chargeDelay && !npc.WithinRange(GetChargeZone(npc), slowdownRange) && hasGottenNearPlayer == 1f)
                npc.velocity *= 0.95f;

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
            npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
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
                    Vector2 fireballShootVelocity = npc.SafeDirectionTo(target.Center).RotatedByRandom(0.37f) * 28f;

                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(fireball => fireball.tileCollide = false);
                    Utilities.NewProjectileBetter(mouthPosition, fireballShootVelocity, ModContent.ProjectileType<HomingFireball>(), RegularFireballDamage, 0f);

                    int numberOfParticles = 6;
                    for (int i = 0; i < numberOfParticles; i++)
                    {
                        HeavySmokeParticle smokeParticle = new(mouthPosition, fireballShootVelocity, Color.Gray, 5, Main.rand.NextFloat(0.75f, 0.95f), 1);
                        GeneralParticleHandler.SpawnParticle(smokeParticle);
                    }
                }
                SoundEngine.PlaySound(YharonBoss.ShortRoarSound, npc.Center);
            }

            if (attackTimer >= fireballBreathShootDelay + fireballBreathShootRate * totalFireballBreaths)
                SelectNextAttack(npc, ref attackType);
        }

        public static void DoBehavior_FlamethrowerAndMeteors(NPC npc, Player target, Vector2 mouthPosition, ref float attackTimer, ref float attackType, ref float specialFrameType)
        {
            int totalFlamethrowerBursts = 1;
            int flamethrowerHoverTime = 75;
            float flamethrowerFlySpeed = 55.5f;
            float wrappedAttackTimer = attackTimer % (flamethrowerHoverTime + YharonFlamethrower.Lifetime + 36f);

            // Look at the target and hover towards the top left/right of the target.
            if (wrappedAttackTimer < flamethrowerHoverTime + 15f)
            {
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 1560f, -375f);
                specialFrameType = (int)YharonFrameDrawingType.FlapWings;

                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
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
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<YharonFlamethrower>(), FlamethrowerDamage, 0f, -1, 0f, npc.whoAmI);
            }

            // Slow down once the flamethrower is gone.
            if (wrappedAttackTimer >= flamethrowerHoverTime + YharonFlamethrower.Lifetime + 15f)
                npc.velocity *= 0.95f;

            // Decide the current rotation.
            npc.rotation = npc.velocity.ToRotation() + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi;

            if (attackTimer >= totalFlamethrowerBursts * (flamethrowerHoverTime + YharonFlamethrower.Lifetime + 36f) - 3f)
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
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 tornadoSpawnerShootVelocity = (MathHelper.TwoPi / 2f * i).ToRotationVector2() * 7f;
                        Utilities.NewProjectileBetter(mouthPosition, tornadoSpawnerShootVelocity, ModContent.ProjectileType<InfernadoSpawner>(), 0, 0f, -1, 1f);
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
                        Utilities.NewProjectileBetter(npc.Center + angle.ToRotationVector2() * 40f, angle.ToRotationVector2() * speed, ModContent.ProjectileType<FlareBomb>(), RegularFireballDamage, 0f);
                    }

                    for (int i = 0; i < 3; i++)
                    {
                        float angle = MathHelper.TwoPi / 3f * i;
                        Vector2 flareSpawnPosition = npc.Center + angle.ToRotationVector2() * 600f;
                        Utilities.NewProjectileBetter(flareSpawnPosition, angle.ToRotationVector2().RotatedByRandom(0.03f) * Vector2.Zero, ModContent.ProjectileType<InfernadoSpawner>(), 0, 0f, Main.myPlayer);
                    }
                }
                SoundEngine.PlaySound(YharonBoss.RoarSound, npc.Center);
                SelectNextAttack(npc, ref attackType);
            }
        }

        public static void DoBehavior_EnterSecondPhase(NPC npc, Player target, ref float attackType, ref float attackTimer, ref float specialFrameType, ref float fireIntensity, ref float invincibilityTime)
        {
            int cameraPanDelay1 = 4;
            int cameraPanTime1 = 240;
            int wingFlapTime = 90;
            int fadeToAshTime = 210;
            int fadeOutTime = 150;
            int energyChargeTime = 360;
            ref float fadeToAshInterpolant = ref npc.Infernum().ExtraAI[FadeToAshInterpolantIndex];

            // Disable damage.
            npc.dontTakeDamage = true;
            npc.damage = 0;

            // Rotate in place.
            npc.rotation = npc.rotation.AngleTowards(0f, 0.03f);

            // Close the HP bar.
            npc.Calamity().ShouldCloseHPBar = true;

            // Disable music.
            npc.ModNPC.Music = MusicLoader.GetMusicSlot(InfernumMode.Instance, "Sounds/Music/Nothing");
            CalamityGlobalNPC.yharon = -1;
            CalamityGlobalNPC.yharonP2 = -1;

            // Create a violent sound on the first frame to try and obscure the sudden music stop.
            if (attackTimer == 1f)
                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceLavaEruptionSound with { Volume = 2f });

            // Clear all entities and look at the target before the animation begins.
            if (attackTimer <= 20f)
            {
                npc.spriteDirection = (target.Center.X - npc.Center.X < 0).ToDirectionInt();
                ClearAllEntities();
            }

            // Slow to a crawl.
            npc.velocity *= 0.86f;

            // Use the open mouth frames and roar after enough time has passed.
            if (attackTimer <= wingFlapTime)
                specialFrameType = (int)YharonFrameDrawingType.FlapWings;
            else
            {
                if (attackTimer == wingFlapTime + 1f)
                {
                    SoundEngine.PlaySound(YharonBoss.RoarSound with { Volume = 3f });
                    ScreenEffectSystem.SetFlashEffect(npc.Center, 1.5f, 48);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<YharonBoom>(), 0, 0f);
                }

                specialFrameType = (int)YharonFrameDrawingType.OpenMouth;
            }

            // Make Yharon fade to ash.
            if (attackTimer <= fadeToAshTime)
                fadeToAshInterpolant = MathF.Pow(Utils.GetLerpValue(0f, fadeToAshTime, attackTimer, true), 1.43f);

            // Emit a bunch of ash particles.
            if (fadeToAshInterpolant >= 0.3f)
            {
                fireIntensity *= 0.98f;
                for (int i = 0; i < (int)(fadeToAshInterpolant * npc.Opacity * 8f); i++)
                {
                    Dust dragonAsh = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Square(-180f, 180f), ModContent.DustType<BrimstoneCinderDust>());
                    dragonAsh.velocity += Vector2.UnitX.RotatedByRandom(0.9f) * npc.spriteDirection * Main.rand.NextFloat(6f, 20f);
                    dragonAsh.scale = 0.7f + dragonAsh.velocity.Length() * 0.032f;
                    dragonAsh.noGravity = true;
                }
            }

            // Move the camera.
            if (attackTimer >= cameraPanDelay1)
            {
                float cameraPanInterpolant = Utils.GetLerpValue(cameraPanDelay1, cameraPanDelay1 + 10f, attackTimer, true) * Utils.GetLerpValue(cameraPanTime1, cameraPanTime1 - 10f, attackTimer, true);
                target.Infernum_Camera().ScreenFocusInterpolant = cameraPanInterpolant;
                target.Infernum_Camera().ScreenFocusPosition = npc.Center;
            }

            // Have Yharon disappear before flying into the sky as a bunch of auric energy.
            if (attackTimer >= cameraPanTime1 && attackTimer <= cameraPanTime1 + fadeOutTime)
            {
                float oldOpacity = npc.Opacity;
                npc.alpha = Utils.Clamp(npc.alpha + 5, 0, 255);
                if (npc.Opacity <= 0f && oldOpacity > 0.01f)
                {
                    for (int i = 0; i < 50; i++)
                    {
                        Color fireColor = Main.rand.NextBool() ? Color.White : Color.Yellow;
                        CloudParticle fireCloud = new(npc.Center, Main.rand.NextVector2Circular(9f, 9f) - Vector2.UnitY * Main.rand.NextFloat(10f, 40f), fireColor * 0.85f, Color.DarkGray, 60, Main.rand.NextFloat(2f, 2.4f));
                        GeneralParticleHandler.SpawnParticle(fireCloud);
                    }

                    SoundEngine.PlaySound(InfernumSoundRegistry.SizzleSound with { Volume = 3f });
                    npc.Center = target.Center - Vector2.UnitY * 900f;
                    npc.velocity = Vector2.Zero;
                    npc.netUpdate = true;
                }
            }

            // Charge up energy in the new position after the teleport.
            // The camera pans to Yharon again during this, and the music begins.
            if (attackTimer >= cameraPanTime1 + fadeOutTime && attackTimer <= cameraPanTime1 + fadeOutTime + energyChargeTime)
            {
                float adjustedTimer = attackTimer - cameraPanTime1 - fadeOutTime;
                float cameraPanInterpolant = Utils.GetLerpValue(0f, 10f, adjustedTimer, true) * Utils.GetLerpValue(energyChargeTime, energyChargeTime - 10f, adjustedTimer, true);
                target.Infernum_Camera().ScreenFocusInterpolant = cameraPanInterpolant;
                target.Infernum_Camera().ScreenFocusPosition = npc.Center;

                // Make the background get progressively brighter.
                float flashIntensity = Utils.GetLerpValue(0f, energyChargeTime - 30f, adjustedTimer, true);
                ScreenEffectSystem.SetFlashEffect(npc.Center, flashIntensity * 1.05f, 25);

                // Create a bunch of lava in the background.
                if (attackTimer % 45f == 44f)
                {
                    YharonSky.CreateSmokeBurst();
                    target.Infernum_Camera().CurrentScreenShakePower = 12f;
                }

                // Create pulse rungs and bloom periodically.
                if (attackTimer % 10f == 0f)
                {
                    Color energyColor = Color.Lerp(Color.Orange, Color.Red, Main.rand.NextFloat(0.9f));
                    PulseRing ring = new(npc.Center, Vector2.Zero, energyColor, 10f, 0f, 60);
                    GeneralParticleHandler.SpawnParticle(ring);

                    StrongBloom bloom = new(npc.Center, Vector2.Zero, energyColor, 3f, 15);
                    GeneralParticleHandler.SpawnParticle(bloom);
                }

                // Enable the distortion filter if it isnt active and the player's config permits it.
                if (Main.netMode != NetmodeID.Server && !InfernumEffectsRegistry.ScreenDistortionScreenShader.IsActive() && Main.UseHeatDistortion)
                {
                    Filters.Scene.Activate("InfernumMode:ScreenDistortion", Main.LocalPlayer.Center);
                    InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().UseImage("Images/Extra_193");
                    InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().Shader.Parameters["distortionAmount"].SetValue(cameraPanInterpolant * 20f);
                    InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().Shader.Parameters["wiggleSpeed"].SetValue(5f);
                }

                // Have Yharon fade back in in his lava form.
                fadeToAshInterpolant = MathHelper.Clamp(fadeToAshInterpolant - 0.05f, 0f, 1f);
                fireIntensity = MathF.Pow(flashIntensity, 0.7f);
                npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.01f, 0f, 0.6f);

                // Let the music play.
                CalamityGlobalNPC.yharonP2 = npc.whoAmI;

                // Let the HP bar reappear.
                npc.Calamity().ShouldCloseHPBar = false;
            }

            // Lock the invicibility timer in place before he charges energy.
            else
            {
                npc.life = (int)MathF.Round(npc.lifeMax * Phase2LifeRatio);
                invincibilityTime = Phase2InvincibilityTime;
            }

            // Reset the attack cycle for the next subphase.
            if (attackTimer >= cameraPanTime1 + fadeOutTime + energyChargeTime)
            {
                npc.Opacity = 1f;

                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceLavaEruptionSound with { Volume = 2f });

                npc.Infernum().ExtraAI[AttackCycleIndexIndex] = -1f;
                target.Infernum_Camera().CurrentScreenShakePower = 16f;

                Utilities.DisplayText("The air is scorching your skin...", Color.Orange);
                SelectNextAttack(npc, ref attackType);

                SoundEngine.PlaySound(YharonBoss.RoarSound with { Volume = 3f });
                ScreenEffectSystem.SetFlashEffect(npc.Center, 1f, 210);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<YharonBoom>(), 0, 0f);
            }
        }

        public static void DoBehavior_CarpetBombing(NPC npc, Player target, float splittingMeteorRiseTime, float splittingMeteorBombingSpeed, float splittingMeteorBombTime, ref float attackTimer, ref float attackType, ref float specialFrameType)
        {
            int directionToDestination = (target.Center.X < npc.Center.X).ToDirectionInt();
            bool morePowerfulMeteors = npc.life < npc.lifeMax * Subphase6LifeRatio;

            // Fly towards the hover destination near the target.
            if (attackTimer < splittingMeteorRiseTime)
            {
                Vector2 destination = target.Center + new Vector2(directionToDestination * 750f, -300f);
                Vector2 idealVelocity = npc.SafeDirectionTo(destination) * splittingMeteorBombingSpeed;

                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.035f);
                npc.rotation = npc.rotation.AngleTowards(0f, 0.25f);

                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();

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
                    Utilities.NewProjectileBetter(npc.Center + npc.velocity * 3f, npc.velocity, ModContent.ProjectileType<YharonFireball>(), RegularFireballDamage, 0f, Main.myPlayer, 0f, 0f);
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

                // Give a tip.
                if (attackTimer == heatFlashIdleDelay - 3f)
                    HatGirl.SayThingWhileOwnerIsAlive(target, "Don't let the flashbang faze you! Keep your eyes peeled for where the embers are!");

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
                            Utilities.NewProjectileBetter(target.Center + angle.ToRotationVector2() * outwardness, Vector2.Zero, ModContent.ProjectileType<YharonHeatFlashFireball>(), HeatFlashFireballDamage, 0f, Main.myPlayer);

                            // Create a cluster of flames that appear in the direction the target is currently moving.
                            // This makes it harder to maneuver through the burst.
                            if (angleFromTarget <= MathHelper.TwoPi / heatFlashTotalFlames)
                            {
                                for (int j = 0; j < Main.rand.Next(11, 17 + 1); j++)
                                {
                                    float newAngle = angle + Main.rand.NextFloatDirection() * angleFromTarget;
                                    Utilities.NewProjectileBetter(target.Center + newAngle.ToRotationVector2() * outwardness, Vector2.Zero, ModContent.ProjectileType<YharonHeatFlashFireball>(), HeatFlashFireballDamage, 0f, Main.myPlayer);
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
                if (InfernumConfig.Instance.FlashbangOverlays)
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
                    Utilities.NewProjectileBetter(target.Center + angle.ToRotationVector2() * 1780f, Vector2.Zero, ModContent.ProjectileType<VortexOfFlame>(), FlameVortexDamage, 0f, Main.myPlayer);
                    Utilities.NewProjectileBetter(target.Center, angle.ToRotationVector2(), ModContent.ProjectileType<VortexTelegraphBeam>(), 0, 0f, -1, 0f, 1780f);
                }
            }

            // Emit splitting fireballs from the side in a fashion similar to that of Old Duke's shark summoning attack.
            if (attackTimer > flameVortexSpawnDelay && attackTimer % 7 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float horizontalOffset = (attackTimer - flameVortexSpawnDelay) / 7f * 205f + 260f;
                Vector2 fireballSpawnPosition = npc.Center + new Vector2(horizontalOffset, -90f);
                if (!target.WithinRange(fireballSpawnPosition, 350f))
                    Utilities.NewProjectileBetter(fireballSpawnPosition, Vector2.UnitY.RotatedBy(-0.18f) * -20f, ModContent.ProjectileType<YharonFireball>(), RegularFireballDamage, 0f, Main.myPlayer);

                fireballSpawnPosition = npc.Center + new Vector2(-horizontalOffset, -90f);
                if (!target.WithinRange(fireballSpawnPosition, 350f))
                    Utilities.NewProjectileBetter(fireballSpawnPosition, Vector2.UnitY.RotatedBy(0.18f) * -20f, ModContent.ProjectileType<YharonFireball>(), RegularFireballDamage, 0f, Main.myPlayer);
            }
            if (attackTimer > flameVortexSpawnDelay + totalFlameWaves * 7)
                SelectNextAttack(npc, ref attackType);
        }

        public static void DoBehavior_FinalDyingRoar(NPC npc)
        {
            npc.dontTakeDamage = true;
            // Ensure this isnt loaded on the server, as it will throw a null reference error.
            if (Main.netMode != NetmodeID.Server)
                Filters.Scene["HeatDistortion"].GetShader().UseIntensity(3f);

            float lifeRatio = npc.life / (float)npc.lifeMax;

            Player target = Main.player[npc.target];

            int totalCharges = 0;
            int chargeCycleTime = 0;
            float confusingChargeSpeed = 53.5f;

            float splittingMeteorHoverSpeed = 24f;
            float splittingMeteorBombingSpeed = 37.5f;
            float splittingMeteorRiseTime = 0f;
            float splittingMeteorBombTime = 0f;
            int fireballReleaseRate = 3;
            int totalTimeSpentPerCarpetBomb = (int)(splittingMeteorRiseTime + splittingMeteorBombTime);

            int totalBerserkCharges = 0;
            int berserkChargeTime = 0;
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
                // Give a tip.
                if (attackDelay == 40f)
                    HatGirl.SayThingWhileOwnerIsAlive(target, "Yharon's burning some serious energy now! Stay focused!");

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
                        Utilities.NewProjectileBetter(npc.Center + npc.velocity * 3f, npc.velocity, ModContent.ProjectileType<YharonFireball>(), RegularFireballDamage, 0f, Main.myPlayer, 0f, 0f);

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
                if (wrappedAttackTimer < 20f)
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

            // After all final charges are complete, slow down and die.
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
                if (lifeRatio < 0.004f && pulseDeathEffectCooldown <= 0 && attackTimer >= preAttackTime + 180f)
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

                if (npc.life < 6000 && hasCreatedExplosionFlag == 0f)
                {
                    if (Main.myPlayer == target.whoAmI)
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<YharonFlameExplosion>(), 0, 0f);

                    hasCreatedExplosionFlag = 1f;
                    npc.netUpdate = true;
                }

                // Determine the opacity.
                float idealOpacity = Utils.Remap(attackTimer - preAttackTime, 150f, 420f, 1f, 0.035f);
                npc.Opacity = MathHelper.Lerp(npc.Opacity, idealOpacity, 0.2f);

                // Do some camera pan effects.
                if (Main.LocalPlayer.WithinRange(target.Center, 8400f))
                {
                    float cameraPanInterpolant = Utils.GetLerpValue(0f, 8f, attackTimer - preAttackTime, true) * Utils.GetLerpValue(180f, 175f, attackTimer - preAttackTime, true);
                    Main.LocalPlayer.Infernum_Camera().ScreenFocusInterpolant = cameraPanInterpolant;
                    Main.LocalPlayer.Infernum_Camera().ScreenFocusPosition = npc.Center;

                    // Emit a strong roar.
                    if (attackTimer == preAttackTime + 96f)
                    {
                        ScreenEffectSystem.SetFlashEffect(npc.Center, 1f, 42);
                        SoundEngine.PlaySound(YharonBoss.DeathSound with { Volume = 4f, Pitch = -0.15f });
                        SoundEngine.PlaySound(YharonBoss.RoarSound with { Volume = 3f, Pitch = -0.125f });

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<YharonBoom>(), 0, 0f);
                    }

                    if (attackTimer >= preAttackTime + 96f)
                        specialFrameType = (int)YharonFrameDrawingType.OpenMouth;
                }

                // Emit very strong fireballs.
                if (Main.myPlayer == target.whoAmI && attackTimer > preAttackTime + 180f)
                {
                    for (int i = 0; i < 3; i++)
                        Utilities.NewProjectileBetter(npc.Center, Main.rand.NextVector2CircularEdge(16f, 16f), ModContent.ProjectileType<FlareBomb>(), DeathAnimationFireballDamage, 0f, target.whoAmI, -1f);
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
                ModContent.ProjectileType<DraconicInfernado>(),
                ModContent.ProjectileType<DragonFireball>(),
                ModContent.ProjectileType<HomingFireball>(),
                ModContent.ProjectileType<YharonFireball>(),
                ModContent.ProjectileType<YharonFireball2>(),
                ModContent.ProjectileType<Infernado>(),
                ModContent.ProjectileType<Infernado2>(),
                ModContent.ProjectileType<InfernadoSpawner>(),
                ModContent.ProjectileType<Flare>(),
                ModContent.ProjectileType<FlareBomb>(),
                ModContent.ProjectileType<BigFlare>(),
                ModContent.ProjectileType<BigFlare2>(),
                ModContent.ProjectileType<YharonHeatFlashFireball>(),
                ModContent.ProjectileType<VortexOfFlame>(),
                ModContent.ProjectileType<RedirectingYharonMeteor>(),
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

            // Clear the charge zone mark for future attacks.
            npc.Infernum().ExtraAI[PlayerChargeMarkCenterXIndex] = 0f;
            npc.Infernum().ExtraAI[PlayerChargeMarkCenterYIndex] = 0f;

            // Reset the attack timer and subphase specific variables.
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.Infernum().ExtraAI[HasGottenNearPlayerIndex] = 0f;
            npc.netUpdate = true;
        }
        #endregion

        #region Frames and Drawcode
        public override void FindFrame(NPC npc, int frameHeight)
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
            npc.frameCounter++;
        }

        public static void DrawInstance(NPC npc, Vector2? position = null, float rotationOffset = 0f, bool changeDirection = false)
        {
            YharonAttackType attackType = (YharonAttackType)npc.ai[0];
            Texture2D tex = ModContent.Request<Texture2D>(npc.ModNPC.Texture).Value;
            if (Utilities.IsAprilFirst())
                tex = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Yharon/GrandYharon").Value;

            // Use defaults for the draw position.
            position ??= npc.Center;

            // Define draw variables.
            Vector2 origin = npc.frame.Size() * 0.5f;
            SpriteEffects spriteEffects = npc.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            if (changeDirection)
                spriteEffects = npc.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            // Determine variables for the fire effect.
            int afterimageCount = 7;
            float afterimageOffsetMax = 30f;
            float fireIntensity = npc.Infernum().ExtraAI[FireFormInterpolantIndex];
            bool inLastSubphases = npc.life / (float)npc.lifeMax <= Subphase7LifeRatio && InSecondPhase && npc.Infernum().ExtraAI[InvincibilityTimerIndex] <= 0f;
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
                Color phase2Color = Color.Lerp(Color.Pink, Color.Wheat, (float)Math.Cos(Main.GlobalTimeWrappedHourly * 2.3f) * 0.5f + 0.5f);
                burnColor = Color.Lerp(phase2Color, burnColor, backBackToRegularColor);
            }
            if (npc.life < npc.lifeMax * Subphase7LifeRatio)
                burnColor = Color.Lerp(Main.hslToRgb(Main.GlobalTimeWrappedHourly * 0.3f % 1f, 1f, 0.7f), Color.Pink, 0.55f);
            if (attackType == YharonAttackType.FireTrailCharge)
                burnColor = Color.OrangeRed;
            if (npc.life < npc.lifeMax * 0.075f)
                fireIntensity += 0.325f;

            // Use the molten burn effect for certain parts of the fight. This does not happen if the config favors performance over visual quality.
            if (!InfernumConfig.Instance.ReducedGraphicsConfig)
            {
                InfernumEffectsRegistry.YharonBurnShader.UseOpacity(fireIntensity * 0.7f);
                InfernumEffectsRegistry.YharonBurnShader.SetShaderTexture(InfernumTextureRegistry.CultistRayMap);
                InfernumEffectsRegistry.YharonBurnShader.UseColor(burnColor * 0.7f);
                InfernumEffectsRegistry.YharonBurnShader.UseSecondaryColor(Color.White * 0.12f);
                InfernumEffectsRegistry.YharonBurnShader.Shader.Parameters["uTimeFactor"].SetValue(1.1f);
                InfernumEffectsRegistry.YharonBurnShader.Shader.Parameters["uZoomFactor"].SetValue(new Vector2(1f, 1f));
                InfernumEffectsRegistry.YharonBurnShader.Shader.Parameters["uNoiseReadZoomFactor"].SetValue(new Vector2(0.2f, 0.2f));
                InfernumEffectsRegistry.YharonBurnShader.Shader.Parameters["uSecondaryLavaPower"].SetValue(10f);
                InfernumEffectsRegistry.YharonBurnShader.Shader.Parameters["uZoomFactorSecondary"].SetValue(0.5f);
                InfernumEffectsRegistry.YharonBurnShader.Shader.Parameters["uNPCRectangle"].SetValue(new Vector4(npc.frame.X, npc.frame.Y, npc.frame.Width, npc.frame.Height));
                InfernumEffectsRegistry.YharonBurnShader.Shader.Parameters["uActualImageSize0"].SetValue(tex.Size());
                InfernumEffectsRegistry.YharonBurnShader.Apply();
            }

            float opacity = npc.Opacity * MathHelper.Lerp(1f, 0.067f, npc.Infernum().ExtraAI[FadeToAshInterpolantIndex]);

            // Draw backglow textures.
            if (fireIntensity > 0.01f)
            {
                for (int i = afterimageCount - 1; i >= 0; i--)
                {
                    float afterimageOpacity = 1f;
                    if (afterimageCount >= 2)
                        afterimageOpacity = i / (float)(afterimageCount - 1f);

                    Color color = npc.GetAlpha(Color.White) * (1f - afterimageOpacity);
                    color = Color.Lerp(color, new(60, 60, 60, 255), npc.Infernum().ExtraAI[FadeToAshInterpolantIndex]);

                    Vector2 drawPosition = position.Value - Main.screenPosition;
                    drawPosition -= npc.velocity * 0.6f * i;
                    for (int j = 0; j < 6; j++)
                    {
                        Vector2 circularDrawOffset = (MathHelper.TwoPi * j / 6f).ToRotationVector2() * fireIntensity * afterimageOpacity * afterimageOffsetMax;
                        Color offsetColor = color * opacity * 0.4f;
                        offsetColor.A = 0;
                        Main.spriteBatch.Draw(tex, drawPosition + circularDrawOffset, npc.frame, offsetColor, npc.rotation + rotationOffset, origin, npc.scale, spriteEffects, 0f);
                    }
                }
            }

            // Draw afterimages.
            for (int i = afterimageCount - 1; i >= 0; i--)
            {
                float afterimageOpacity = 1f;
                if (afterimageCount >= 2)
                    afterimageOpacity = MathF.Pow(1f - i / (float)(afterimageCount - 1f), 2f);

                Color color = npc.GetAlpha(Color.White) * afterimageOpacity;
                Color afterimageColor = color;
                if (i == 0 && fireIntensity > 0.01f)
                    afterimageColor.A = 184;
                afterimageColor = Color.Lerp(afterimageColor, new(60, 60, 60, 255), npc.Infernum().ExtraAI[FadeToAshInterpolantIndex]);

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
                Texture2D twinkleTexture = InfernumTextureRegistry.LargeStar.Value;
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

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n =>
            {
                if (n.life < n.lifeMax * Subphase8LifeRatio)
                    return "AND IF I SHOULD DIE BEFORE YOU CONTINUEE, YOU SHALL HAV-... Wait, you died? Come on, I was on a roll here!";
                return string.Empty;
            };
        }
        #endregion Tips
    }
}