using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.NPCs.Providence;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Sounds;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.AttemptRecording;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Common.Graphics.ScreenEffects;
using InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Yharon;
using InfernumMode.Content.Credits;
using InfernumMode.Content.Cutscenes;
using InfernumMode.Content.Projectiles.Pets;
using InfernumMode.Content.Projectiles.Wayfinder;
using InfernumMode.Core.GlobalInstances;
using InfernumMode.Core.GlobalInstances.Players;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using InfernumMode.Core.TrackedMusic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Events;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using ProvidenceBoss = CalamityMod.NPCs.Providence.Providence;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Providence
{
    public class ProvidenceBehaviorOverride : NPCBehaviorOverride
    {
        #region Structures
        public struct ProvidenceAttackSection
        {
            public SongSection Section
            {
                get;
                set;
            }

            public ProvidenceAttackType AttackToUse
            {
                get;
                set;
            }

            public readonly int StartingTime => Section.StartInFrames;

            public readonly int EndingTime => Section.EndInFrames;

            public ProvidenceAttackSection(SongSection section, ProvidenceAttackType attackType)
            {
                Section = section;
                AttackToUse = attackType;
            }
        }

        public struct ProvidenceAttackInformation
        {
            public int LocalAttackTimer
            {
                get;
                set;
            }

            public int LocalAttackDuration
            {
                get;
                set;
            }

            public ProvidenceAttackType CurrentAttack
            {
                get;
                set;
            }

            public ProvidenceAttackInformation(int localTimer, int localDuration, ProvidenceAttackType attack)
            {
                LocalAttackTimer = localTimer;
                LocalAttackDuration = localDuration;
                CurrentAttack = attack;
            }
        }
        #endregion Structures

        #region Enumerations
        public enum ProvidenceAttackType
        {
            // Phase 1.
            FireEnergyCharge,
            CinderAndBombBarrages,
            AcceleratingCrystalFan,
            AttackGuardiansSpearSlam,
            HealerGuardianCrystalBarrage,

            // Phase 2.
            EnterFireFormBulletHell,
            EnvironmentalFireEffects,
            CleansingFireballBombardment,
            CooldownState,
            ExplodingSpears,
            SpiralOfExplodingHolyBombs,

            EnterHolyMagicForm,
            RockMagicRitual,
            ErraticMagicBursts,
            DogmaLaserBursts, // Blast TBOI attack idea real???

            EnterLightForm,
            FinalPhaseRadianceBursts,

            CrystalForm
        }

        public enum ProvidenceFrameDrawingType
        {
            WingFlapping,
            CocoonState
        }

        public enum SpearAttackState
        {
            LookAtTarget,
            SpinInPlace,
            Charge
        }

        public enum HealerGuardianAttackState
        {
            SpinInPlace,
            WaitAndReleaseTelegraph,
            ShootCrystals
        }
        #endregion

        public override int NPCOverrideType => ModContent.NPCType<ProvidenceBoss>();

        #region Loading
        public override void Load()
        {
            GlobalNPCOverrides.OnKillEvent += DetermineNightDefeatStatus;
            GlobalNPCOverrides.StrikeNPCEvent += PerformDamageRestrictions;
            //TrackedMusicManager.PauseInUIConditionEvent += AddMusicPauseCondition;
        }

        private bool PerformDamageRestrictions(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (npc.type == ModContent.NPCType<ProvidenceBoss>() && npc.defense >= 60)
                modifiers.FinalDamage.Base = (int)Math.Max(modifiers.FinalDamage.Base - npc.defense / 2, 1D);
             return true;
        }

        private void DetermineNightDefeatStatus(NPC npc)
        {
            // Determine whether Providence was defeated at night first. Her infernal relic will give a baffled comment if this happens.
            if (npc.type == ModContent.NPCType<ProvidenceBoss>())
            {
                if (!Main.dayTime && !WorldSaveSystem.HasBeatenInfernumProvRegularly)
                    WorldSaveSystem.HasBeatenInfernumNightProvBeforeDay = true;
                WorldSaveSystem.HasBeatenInfernumProvRegularly = true;
                CalamityNetcode.SyncWorld();
            }
        }

        //private bool AddMusicPauseCondition()
        //{
        //    string songName = TrackedMusicManager.TrackedSong.Name;
        //    return (songName.Contains("Providence") || songName.Contains("Guardians")) && CalamityGlobalNPC.holyBoss != -1;
        //}
        #endregion Loading

        #region AI
        public const int AuraTime = 300;

        public const int CocoonDefense = 600;

        // The defense Providence has when in her cocoon phases is significantly reduced after the attack cycles complete once, to alleviate the need for the player to just sit and wait/dodge, being
        // unable to do meaningful damage.
        public const int CocoonDefenseAfterFullCycle = 250;

        public const int DeathEffectTimerIndex = 5;

        public const int WasSummonedAtNightFlagIndex = 6;

        public const int LavaHeightIndex = 7;

        public const int FlightPathIndex = 8;

        public const int RockReformOffsetIndex = 9;

        public const int HasCompletedCycleIndex = 10;

        public const int HasEnteredPhase2Index = 11;

        public const int StartedWithMusicDisabledIndex = 12;

        public const int DeathAnimationGlowIntensityIndex = 13;

        public const float DefaultLavaHeight = 1400f;

        public const float HighestLavaHeight = 2284f;

        public const float Phase2LifeRatio = 0.7f;

        public const float DeathAnimationLifeRatio = 0.04f;

        public static int CrystalShardDamage => IsEnraged ? 450 : 265;

        public static int CinderDamage => IsEnraged ? 420 : 240;

        public static int SmallLavaBlobDamage => IsEnraged ? 420 : 240;

        public static int BasicFireballDamage => IsEnraged ? 450 : 265;

        public static int BigFireballDamage => IsEnraged ? 490 : 300;

        public static int HolySpearDamage => IsEnraged ? 500 : 300;

        public static int HolyCrossDamage => IsEnraged ? 450 : 275;

        public static int CrystalMagicDamage => IsEnraged ? 450 : 275;

        public static int CrystalSpikeDamage => IsEnraged ? 500 : 300;

        public static int MagicRockDamage => IsEnraged ? 450 : 275;

        public static int MagicLaserbeamDamage => IsEnraged ? 1000 : 500;

        public static bool IsEnraged => !Main.dayTime || BossRushEvent.BossRushActive;

        public static bool SyncAttacksWithMusic => false;//Main.netMode == NetmodeID.SinglePlayer && InfernumMode.CalMusicModIsActive && Main.musicVolume > 0f && !BossRushEvent.BossRushActive;

        public static readonly Color[] NightPalette = new[]
        {
            new Color(119, 232, 194),
            new Color(117, 201, 229),
            new Color(117, 93, 229)
        };

        public static List<ProvidenceAttackSection> Phase1AttackStates => new()
        {
            // Quiet section.
            new(new(BaseTrackedMusic.TimeFormat(0, 0, 0), BaseTrackedMusic.TimeFormat(0, 21, 0)), ProvidenceAttackType.FireEnergyCharge),

            // Attack sections.
            new(new(BaseTrackedMusic.TimeFormat(0, 21, 0), BaseTrackedMusic.TimeFormat(0, 32, 0)), ProvidenceAttackType.CinderAndBombBarrages),
            new(new(BaseTrackedMusic.TimeFormat(0, 32, 0), BaseTrackedMusic.TimeFormat(0, 42, 667)), ProvidenceAttackType.AcceleratingCrystalFan),
            new(new(BaseTrackedMusic.TimeFormat(0, 42, 667), BaseTrackedMusic.TimeFormat(0, 54, 333)), ProvidenceAttackType.AttackGuardiansSpearSlam),
            new(new(BaseTrackedMusic.TimeFormat(0, 54, 333), BaseTrackedMusic.TimeFormat(1, 5, 0)), ProvidenceAttackType.HealerGuardianCrystalBarrage),
            new(new(BaseTrackedMusic.TimeFormat(1, 5, 0), BaseTrackedMusic.TimeFormat(1, 13, 0)), ProvidenceAttackType.CinderAndBombBarrages),
            new(new(BaseTrackedMusic.TimeFormat(1, 13, 0), BaseTrackedMusic.TimeFormat(1, 24, 333)), ProvidenceAttackType.HealerGuardianCrystalBarrage),
            new(new(BaseTrackedMusic.TimeFormat(1, 24, 333), BaseTrackedMusic.TimeFormat(1, 46, 667)), ProvidenceAttackType.AttackGuardiansSpearSlam),
        };

        public static List<ProvidenceAttackSection> Phase2AttackStates => new()
        {
            // Quiet section, prelude to fire form.
            new(new(BaseTrackedMusic.TimeFormat(0, 0, 0), BaseTrackedMusic.TimeFormat(0, 20, 0)), ProvidenceAttackType.EnterFireFormBulletHell),
            new(new(BaseTrackedMusic.TimeFormat(0, 20, 0), BaseTrackedMusic.TimeFormat(0, 32, 0)), ProvidenceAttackType.EnvironmentalFireEffects),

            // Fire form.
            new(new(BaseTrackedMusic.TimeFormat(0, 32, 0), BaseTrackedMusic.TimeFormat(0, 40, 0)), ProvidenceAttackType.CleansingFireballBombardment),
            new(new(BaseTrackedMusic.TimeFormat(0, 40, 0), BaseTrackedMusic.TimeFormat(0, 43, 0)), ProvidenceAttackType.CooldownState),
            new(new(BaseTrackedMusic.TimeFormat(0, 43, 0), BaseTrackedMusic.TimeFormat(0, 51, 0)), ProvidenceAttackType.ExplodingSpears),
            new(new(BaseTrackedMusic.TimeFormat(0, 51, 0), BaseTrackedMusic.TimeFormat(0, 53, 0)), ProvidenceAttackType.CooldownState),
            new(new(BaseTrackedMusic.TimeFormat(0, 53, 0), BaseTrackedMusic.TimeFormat(1, 3, 0)), ProvidenceAttackType.SpiralOfExplodingHolyBombs),
            new(new(BaseTrackedMusic.TimeFormat(1, 3, 0), BaseTrackedMusic.TimeFormat(1, 11, 0)), ProvidenceAttackType.ExplodingSpears),

            // Holy magic form.
            new(new(BaseTrackedMusic.TimeFormat(1, 11, 0), BaseTrackedMusic.TimeFormat(1, 16, 360)), ProvidenceAttackType.EnterHolyMagicForm),
            new(new(BaseTrackedMusic.TimeFormat(1, 16, 360), BaseTrackedMusic.TimeFormat(1, 29, 0)), ProvidenceAttackType.RockMagicRitual),
            new(new(BaseTrackedMusic.TimeFormat(1, 29, 0), BaseTrackedMusic.TimeFormat(1, 37, 0)), ProvidenceAttackType.ErraticMagicBursts),
            new(new(BaseTrackedMusic.TimeFormat(1, 37, 0), BaseTrackedMusic.TimeFormat(1, 58, 0)), ProvidenceAttackType.DogmaLaserBursts),

            // Light form and cycle restart.
            new(new(BaseTrackedMusic.TimeFormat(1, 58, 0), BaseTrackedMusic.TimeFormat(2, 1, 0)), ProvidenceAttackType.EnterLightForm),
            new(new(BaseTrackedMusic.TimeFormat(2, 1, 0), BaseTrackedMusic.TimeFormat(2, 23, 0)), ProvidenceAttackType.FinalPhaseRadianceBursts)
        };

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio
        };

        public override void SetDefaults(NPC npc)
        {
            // Set defaults that, if were to be changed by Calamity, would cause significant issues to the fight.
            npc.width = 600;
            npc.height = 450;
            npc.scale = 1f;
            npc.defense = 50;
            npc.DR_NERD(0.3f);
            npc.Opacity = 0f;
        }

        public override bool PreAI(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float lifeRatioP1Adjusted = Utils.GetLerpValue(Phase2LifeRatio, 1f, lifeRatio);
            float lifeRatioP2Adjusted = Clamp(lifeRatio, 0f, Phase2LifeRatio) / Phase2LifeRatio;
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float attackStateTimer = ref npc.ai[2];
            ref float initialized = ref npc.ai[3];
            ref float drawState = ref npc.localAI[0];
            ref float burnIntensity = ref npc.localAI[3];
            ref float deathEffectTimer = ref npc.Infernum().ExtraAI[DeathEffectTimerIndex];
            ref float wasSummonedAtNight = ref npc.Infernum().ExtraAI[WasSummonedAtNightFlagIndex];
            ref float lavaHeight = ref npc.Infernum().ExtraAI[LavaHeightIndex];
            ref float flightPath = ref npc.Infernum().ExtraAI[FlightPathIndex];
            ref float rockReformOffset = ref npc.Infernum().ExtraAI[RockReformOffsetIndex];
            ref float hasCompletedCycle = ref npc.Infernum().ExtraAI[HasCompletedCycleIndex];
            ref float hasEnteredPhase2 = ref npc.Infernum().ExtraAI[HasEnteredPhase2Index];
            ref float deathAnimationGlowIntensity = ref npc.Infernum().ExtraAI[DeathAnimationGlowIntensityIndex];

            bool shouldDespawnAtNight = wasSummonedAtNight == 0f && IsEnraged && attackType != (int)ProvidenceAttackType.EnterFireFormBulletHell;
            bool shouldDespawnAtDay = wasSummonedAtNight == 1f && !IsEnraged && attackType != (int)ProvidenceAttackType.EnterFireFormBulletHell;
            bool shouldDespawnBecauseOfTime = (shouldDespawnAtNight || shouldDespawnAtDay) && !BossRushEvent.BossRushActive;
            bool inDeathCutscene = attackType == (int)ProvidenceAttackType.CrystalForm;

            Vector2 crystalCenter = npc.Center + new Vector2(8f, 56f);

            // Define arena variables.
            Vector2 arenaTopLeft = WorldSaveSystem.ProvidenceArena.TopLeft() * 16f + new Vector2(68f, 32f);
            Vector2 arenaBottomRight = WorldSaveSystem.ProvidenceArena.BottomRight() * 16f + new Vector2(8f, 52f);
            Vector2 arenaTopCenter = new Vector2(WorldSaveSystem.ProvidenceArena.Center().X + 405f, WorldSaveSystem.ProvidenceArena.Top + 56) * 16f + Vector2.One * 8f;
            Rectangle arenaArea = new((int)arenaTopLeft.X, (int)arenaTopLeft.Y, (int)(arenaBottomRight.X - arenaTopLeft.X), (int)(arenaBottomRight.Y - arenaTopLeft.Y));

            // Reset various things every frame. They can be changed later as needed.
            npc.defense = 50;
            npc.dontTakeDamage = false;
            npc.Calamity().DR = BossRushEvent.BossRushActive ? 0.65f : 0.3f;
            npc.Infernum().Arena = arenaArea;
            if (drawState == (int)ProvidenceFrameDrawingType.CocoonState)
                npc.defense = hasCompletedCycle == 1f ? CocoonDefenseAfterFullCycle : CocoonDefense;

            // Handle intialization effects.
            if (initialized == 0f)
            {
                // Reset the music if the player tries to be clever and fuck up the patterns by using a music box.
                bool playingSpecialSong = TrackedMusicManager.TrackedSong is not null && (TrackedMusicManager.TrackedSong.Name.Contains("Providence") || TrackedMusicManager.TrackedSong.Name.Contains("Guardians"));
                if (Main.netMode == NetmodeID.SinglePlayer && playingSpecialSong && MediaPlayer.State == MediaState.Playing)
                {
                    MediaPlayer.Stop();
                    MediaPlayer.Play(TrackedMusicManager.TrackedSong);
                }

                initialized = 1f;
                wasSummonedAtNight = Main.dayTime ? 0f : 1f;
                npc.netUpdate = true;
            }

            // Handle phase 2 transition effects.
            if (hasEnteredPhase2 == 0f && lifeRatio < Phase2LifeRatio)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceLavaEruptionSound with
                {
                    Pitch = 0.5f
                });

                Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 18f;
                ScreenEffectSystem.SetBlurEffect(npc.Center, 1.2f, 25);
                ScreenEffectSystem.SetFlashEffect(npc.Center, 5f, 50);
                MoonlordDeathDrama.RequestLight(5f, Main.LocalPlayer.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    ClearEntities();
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<ProvBoomDeath>(), 0, 0f);
                    hasEnteredPhase2 = 1f;
                    hasCompletedCycle = 0f;
                    attackTimer = 0f;
                    npc.Size = new Vector2(600f, 450f);
                    npc.netUpdate = true;
                }

                //if (Main.netMode != NetmodeID.Server)
                //    MediaPlayer.Stop();
            }

            drawState = (int)ProvidenceFrameDrawingType.WingFlapping;

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Give targets infinite flight time.
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.dead || !player.active || !npc.WithinRange(player.Center, 10000f))
                    continue;

                player.DoInfiniteFlightCheck(IsEnraged ? Color.Turquoise : Color.Yellow);
            }

            // For a few frames Providence will play Boss 1 due to the custom music system. Don't allow this.
            if (Main.netMode != NetmodeID.Server && InfernumMode.CalMusicModIsActive)
                Main.musicFade[MusicID.Boss1] = 0f;

            // Despawn if the nearest target is incredibly far away.
            if (!npc.WithinRange(target.Center, 9600f))
                npc.active = false;

            // Keep the target within the arena.
            if (!WorldSaveSystem.ProvidenceArena.IsEmpty && npc.WithinRange(target.Center, 9600f) && Main.netMode == NetmodeID.SinglePlayer)
            {
                if (target.position.X < arenaArea.Left)
                    target.position.X = arenaArea.Left;
                if (target.position.X + target.width > arenaArea.Right)
                    target.position.X = arenaArea.Right - target.width;

                if (target.position.Y < arenaArea.Top)
                    target.position.Y = arenaArea.Top;
                if (target.position.Y + target.height > arenaArea.Bottom)
                    target.position.Y = arenaArea.Bottom - target.width;
            }

            // End rain.
            CalamityMod.CalamityMod.StopRain();

            // Be enraged at night.
            npc.Calamity().CurrentlyEnraged = IsEnraged;

            // Enable the distortion filter if it isnt active and the player's config permits it.
            if (Main.netMode != NetmodeID.Server && !InfernumEffectsRegistry.ScreenDistortionScreenShader.IsActive() && Main.UseHeatDistortion)
            {
                Filters.Scene.Activate("InfernumMode:ScreenDistortion", Main.LocalPlayer.Center);
                InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().UseImage("Images/Extra_193");

                // Slowly vanish during the death animation.
                float strength = 4f;
                if (deathEffectTimer > 0f)
                    strength *= Utils.GetLerpValue(435f, 0f, deathEffectTimer, true);
                if (inDeathCutscene)
                    strength = 0f;

                InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().Shader.Parameters["distortionAmount"].SetValue(strength);
                InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().Shader.Parameters["wiggleSpeed"].SetValue(2f);
            }

            // Set the global NPC index to this NPC. Used as a means of lowering the need for loops.
            CalamityGlobalNPC.holyBoss = npc.whoAmI;

            // Despawn if the players are dead or if the time changed.
            if (!target.dead && !shouldDespawnBecauseOfTime)
                npc.timeLeft = 1800;
            else
            {
                npc.velocity.Y -= 0.4f;
                if (npc.timeLeft > 90)
                    npc.timeLeft = 90;

                // Disappear if sufficiently far away from the target.
                if (!npc.WithinRange(target.Center, 1350f))
                    npc.active = false;

                return false;
            }

            // Perform the death animation.
            if (lifeRatio < DeathAnimationLifeRatio && !inDeathCutscene)
            {
                DoBehavior_DeathAnimation(npc, target, ref deathEffectTimer, wasSummonedAtNight == 1f, ref deathAnimationGlowIntensity);
                deathEffectTimer++;
                return false;
            }

            // Determine attack information based on the current music, if it's playing.
            ProvidenceAttackInformation attackInfo = GetLocalAttackInformation(npc);
            int localAttackTimer = attackInfo.LocalAttackTimer;
            int localAttackDuration = attackInfo.LocalAttackDuration;

            // Reset things if the attack changed.
            if (attackType != (int)attackInfo.CurrentAttack && !inDeathCutscene)
            {
                for (int i = 0; i < 5; i++)
                    npc.Infernum().ExtraAI[i] = 0f;

                attackType = (int)attackInfo.CurrentAttack;
                npc.netUpdate = true;
            }

            // Stay inside the world.
            if (npc.position.X >= Main.maxTilesX * 16f - 800f)
                npc.position.X = Main.maxTilesX * 16f - 800f;

            // Execute attack patterns.
            switch ((ProvidenceAttackType)attackType)
            {
                // Phase 1.
                case ProvidenceAttackType.FireEnergyCharge:
                    DoBehavior_FireEnergyCharge(npc, target, lifeRatioP1Adjusted, localAttackTimer, localAttackDuration, ref drawState, ref burnIntensity);
                    break;
                case ProvidenceAttackType.CinderAndBombBarrages:
                    DoBehavior_CinderAndBombBarrages(npc, target, lifeRatioP1Adjusted, localAttackTimer, localAttackDuration, ref flightPath);
                    break;
                case ProvidenceAttackType.AcceleratingCrystalFan:
                    DoBehavior_AcceleratingCrystalFan(npc, target, crystalCenter, lifeRatioP1Adjusted, ref flightPath);
                    break;
                case ProvidenceAttackType.AttackGuardiansSpearSlam:
                    DoBehavior_AttackGuardiansSpearSlam(npc, target, lifeRatioP1Adjusted, localAttackTimer, localAttackDuration, ref drawState, ref flightPath);
                    break;
                case ProvidenceAttackType.HealerGuardianCrystalBarrage:
                    DoBehavior_HealerGuardianCrystalBarrage(npc, target, lifeRatioP1Adjusted, localAttackTimer, localAttackDuration, ref drawState, ref flightPath);
                    break;

                // Phase 2.
                case ProvidenceAttackType.EnterFireFormBulletHell:
                    if (lifeRatio > DeathAnimationLifeRatio)
                    {
                        burnIntensity *= 0.94f;
                        if (burnIntensity < 0.004f)
                            burnIntensity = 0f;
                    }
                    DoBehavior_EnterFireFormBulletHell(npc, target, arenaTopCenter, lifeRatioP2Adjusted, localAttackTimer, localAttackDuration, ref drawState, ref lavaHeight);
                    break;
                case ProvidenceAttackType.EnvironmentalFireEffects:
                    DoBehavior_EnvironmentalFireEffects(npc, target, localAttackTimer, localAttackDuration, ref drawState);
                    break;
                case ProvidenceAttackType.CleansingFireballBombardment:
                    DoBehavior_CleansingFireballBombardment(npc, target, lifeRatioP2Adjusted, localAttackTimer, localAttackDuration, ref flightPath);
                    break;
                case ProvidenceAttackType.CooldownState:
                    DoBehavior_CooldownState(npc);
                    break;
                case ProvidenceAttackType.ExplodingSpears:
                    DoBehavior_ExplodingSpears(npc, target, lifeRatioP2Adjusted, localAttackTimer, ref flightPath);
                    break;
                case ProvidenceAttackType.SpiralOfExplodingHolyBombs:
                    DoBehavior_SpiralOfExplodingHolyBombs(npc, target, arenaTopCenter, lifeRatioP2Adjusted, localAttackTimer, localAttackDuration, ref drawState);
                    break;

                case ProvidenceAttackType.EnterHolyMagicForm:
                    DoBehavior_EnterHolyMagicForm(npc, target, localAttackTimer, localAttackDuration, ref drawState);
                    break;
                case ProvidenceAttackType.RockMagicRitual:
                    DoBehavior_RockMagicRitual(npc, target, localAttackTimer, localAttackDuration);
                    break;
                case ProvidenceAttackType.ErraticMagicBursts:
                    DoBehavior_ErraticMagicBursts(npc, target, lifeRatioP2Adjusted, localAttackTimer, localAttackDuration);
                    break;
                case ProvidenceAttackType.DogmaLaserBursts:
                    DoBehavior_DogmaLaserBursts(npc, target, lifeRatioP2Adjusted, (int)attackTimer, localAttackTimer, localAttackDuration);
                    break;

                case ProvidenceAttackType.EnterLightForm:
                    DoBehavior_EnterLightForm(npc, target, localAttackTimer, ref drawState, ref burnIntensity, ref rockReformOffset);
                    break;
                case ProvidenceAttackType.FinalPhaseRadianceBursts:
                    DoBehavior_FinalPhaseRadianceBursts(npc, target, arenaTopCenter, localAttackTimer, localAttackDuration, ref lavaHeight, ref hasCompletedCycle);
                    break;

                case ProvidenceAttackType.CrystalForm:
                    DoBehavior_CrystalForm(npc, target, ref deathEffectTimer);
                    deathEffectTimer++;
                    break;
            }

            // Rotate slightly in the direction of horizontal movement.
            npc.rotation = npc.velocity.X * 0.003f;

            return false;
        }

        public static ProvidenceAttackInformation GetLocalAttackInformation(NPC npc)
        {
            var attackCycle = Phase1AttackStates;
            if (npc.life < npc.lifeMax * Phase2LifeRatio)
                attackCycle = Phase2AttackStates;

            ref float attackTimer = ref npc.ai[1];
            ref float startedWithMusicDisabled = ref npc.Infernum().ExtraAI[StartedWithMusicDisabledIndex];
            if (SyncAttacksWithMusic && startedWithMusicDisabled == 0f)
                attackTimer = (int)Math.Round(TrackedMusicManager.SongElapsedTime.TotalMilliseconds * 0.06f);

            // Increment the attack timer manually if it shouldn't sync with the music.
            else
            {
                attackTimer++;
                if (attackTimer >= attackCycle.Last().EndingTime)
                    attackTimer = 0f;
                startedWithMusicDisabled = 1f;
            }

            // Split the attack timer into sections, and then calculate the local attack timer and current attack based on that.
            // attackTimer isn't used directly in the queries here since lambda expressions cannot take ref local variables.
            var attackSection = attackCycle.FirstOrDefault(a => npc.ai[1] >= a.StartingTime && npc.ai[1] < a.EndingTime);
            if (attackSection.StartingTime == 0 && attackSection.EndingTime == 0)
                attackSection = attackCycle[0];

            ProvidenceAttackType currentAttack = attackSection.AttackToUse;
            int localAttackTimer = (int)(attackTimer - attackSection.StartingTime);
            int localAttackDuration = attackSection.EndingTime - attackSection.StartingTime;
            return new(localAttackTimer, localAttackDuration, currentAttack);
        }

        public static void DoBehavior_DeathAnimation(NPC npc, Player target, ref float deathEffectTimer, bool wasSummonedAtNight, ref float burnIntensity)
        {
            ref float lavaHeight = ref npc.Infernum().ExtraAI[LavaHeightIndex];
            ref float originalLavaHeight = ref npc.Infernum().ExtraAI[14];

            float attackLength = 435f;

            // Mark death effects on the first frame of the animation.
            if (deathEffectTimer == 1f)
            {
                typeof(MoonlordDeathDrama).GetField("whitening", Utilities.UniversalBindingFlags).SetValue(null, 1f);

                AchievementPlayer.ProviDefeated = true;

                if (wasSummonedAtNight)
                    AchievementPlayer.NightProviDefeated = true;

                originalLavaHeight = lavaHeight;

                lavaHeight = 0f;

                BlockerSystem.Start(false, true, () => NPC.AnyNPCs(ModContent.NPCType<ProvidenceBoss>()));

                npc.Center = new Vector2(WorldSaveSystem.ProvidenceArena.Center().X + 395f, WorldSaveSystem.ProvidenceArena.Top + 85) * 16f + Vector2.One * 8f;
                ReleaseSparkles(npc.Center, 150, 100f);
                ClearEntities();
            }

            npc.Opacity = 1f;
            npc.rotation = 0f;

            ZoomSystem.SetZoomEffect(Lerp(0.15f, 0f, Utils.GetLerpValue(0f, attackLength, deathEffectTimer, true)));

            if (deathEffectTimer == 1f && !Main.dedServ)
            {
                SoundEngine.PlaySound(ProvidenceBoss.DeathAnimationSound with
                {
                    Volume = 1.8f
                }, target.Center);
            }

            // Cause the screen to focus on the crystal.
            if (target.WithinRange(npc.Center, 5000f))
            {
                target.Infernum_Camera().ScreenFocusPosition = npc.Center;
                target.Infernum_Camera().ScreenFocusHoldInPlaceTime = 60;
                target.Infernum_Camera().ScreenFocusInterpolant = 1f;
            }

            // Mark Providence as defeated at night. This is necessary for ensuring that the moonlight dye drops.
            npc.ModNPC<ProvidenceBoss>().hasTakenDaytimeDamage = wasSummonedAtNight;

            burnIntensity = MathF.Max(burnIntensity, Utils.GetLerpValue(0f, 45f, deathEffectTimer, true));
            npc.life = (int)Lerp(npc.lifeMax * DeathAnimationLifeRatio - 1f, 1f, Utils.GetLerpValue(0f, 435f, deathEffectTimer, true));
            npc.dontTakeDamage = true;
            npc.velocity = Vector2.Zero;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int shootRate = (int)Lerp(12f, 5f, Utils.GetLerpValue(0f, 250f, deathEffectTimer, true));
                if (deathEffectTimer % shootRate == shootRate - 1 || deathEffectTimer == 92f)
                {
                    target.Infernum_Camera().CurrentScreenShakePower = 2f;

                    for (int i = 0; i < 3; i++)
                    {
                        int shootType = ModContent.ProjectileType<SwirlingFire>();

                        Vector2 shootVelocity = Main.rand.NextVector2CircularEdge(7f, 7f) * Main.rand.NextFloat(0.7f, 1.3f);
                        if (Vector2.Dot(shootVelocity.SafeNormalize(Vector2.Zero), npc.SafeDirectionTo(target.Center)) < 0.5f)
                            shootVelocity *= 1.7f;

                        if (Main.rand.NextBool(150) && deathEffectTimer >= 110f || deathEffectTimer == 92f)
                        {

                            if (deathEffectTimer >= 320f)
                            {
                                shootType = ModContent.ProjectileType<YharonBoom>();
                                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceHolyBlastShootSound, target.Center);
                            }
                            else
                            {
                                shootType = ModContent.ProjectileType<ProvBoomDeath>();
                                shootVelocity = Vector2.Zero;
                                ReleaseSparkles(npc.Center, 6, 18f);
                                SoundEngine.PlaySound(CommonCalamitySounds.FlareSound, target.Center);
                                SoundEngine.PlaySound(HolyBlast.ImpactSound, target.Center);
                            }
                            target.Infernum_Camera().CurrentScreenShakePower = 8f;

                        }

                        Utilities.NewProjectileBetter(npc.Center, shootVelocity, shootType, 0, 0f, 255);
                    }
                }
            }

            if (deathEffectTimer >= 320f && deathEffectTimer <= 410f)
            {
                if (deathEffectTimer >= 340f && deathEffectTimer % 10f == 1f)
                {
                    GeneralParticleHandler.SpawnParticle(new BurstParticle(npc.Center, Vector2.Zero, DoGPostProviCutscene.TimeColor, 36, true));
                    ScreenEffectSystem.SetBlurEffect(npc.Center, 1f, 30);
                    ScreenEffectSystem.SetFlashEffect(npc.Center, 1.3f, 30);
                    target.Infernum_Camera().CurrentScreenShakePower = 10f;

                    ReleaseSparkles(npc.Center, 6, 150);
                    SoundEngine.PlaySound(CommonCalamitySounds.FlareSound, target.Center);
                    SoundEngine.PlaySound(HolyBlast.ImpactSound with { Pitch = 0.2f}, target.Center);
                }

                if (deathEffectTimer <= 360f && deathEffectTimer % 10f == 0f)
                {
                    int sparkleCount = (int)Lerp(10f, 30f, Main.gfxQuality);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<ProvBoomDeath>(), 0, 0f);

                    ReleaseSparkles(npc.Center, sparkleCount, 50f);
                    SoundEngine.PlaySound(CommonCalamitySounds.FlareSound, target.Center);
                    SoundEngine.PlaySound(HolyBlast.ImpactSound, target.Center);
                }

            }

            if (Main.netMode != NetmodeID.MultiplayerClient && deathEffectTimer == 400f)
            {
                ReleaseSparkles(npc.Center, 80, 22f);
                Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<DyingSun>(), 0, 0f, 255);
            }


            if (deathEffectTimer >= attackLength)
            {
                if (WorldSaveSystem.HasSeenDoGCutscene || BossRushEvent.BossRushActive)
                    DoBehavior_DropLootAndDie(npc, target);
                else
                {
                    npc.ai[0] = (float)ProvidenceAttackType.CrystalForm;
                    npc.ai[1] = 0f;
                    npc.netUpdate = true;
                    deathEffectTimer = 0f;
                }
            }
        }

        public static void DoBehavior_CrystalForm(NPC npc, Player target, ref float deathEffectsTimer)
        {
            int dieTime = ModContent.GetInstance<DoGPostProviCutscene>().CutsceneLength;

            int dogHeadType = ModContent.NPCType<DevourerofGodsHead>();

            npc.velocity = Vector2.Zero;
            npc.dontTakeDamage = true;
            npc.damage = 0;
            npc.Calamity().ShouldCloseHPBar = true;
            npc.ShowNameOnHover = false;

            // Cause the screen to focus on the crystal.
            if (target.WithinRange(npc.Center, 5000f) && !NPC.AnyNPCs(dogHeadType))
            {
                target.Infernum_Camera().ScreenFocusPosition = npc.Center + Vector2.UnitY * 55f;
                target.Infernum_Camera().ScreenFocusHoldInPlaceTime = 45;

                target.Infernum_Camera().ScreenFocusInterpolant = 1f;
            }

            if (deathEffectsTimer < DoGPostProviCutscene.StartTime + DoGPostProviCutscene.SlowddownTime + (int)(DoGPostProviCutscene.ChompTime * 0.5f))
            {
                // Periodically emit shockwaves, similar to the crystal hearts in Celeste.
                if (deathEffectsTimer % 120f == 67f)
                    Utilities.CreateShockwave(npc.Center, 2, 7, 18f, false);
                else if (deathEffectsTimer % 120f == 89f)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.TerminusPulseSound with { Pitch = 0.9f }, npc.Center);
                    SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceBurnSound with { Pitch = 0.3f }, npc.Center);
                }
            }

            if (deathEffectsTimer == 1)
                CutsceneManager.QueueCutscene(ModContent.GetInstance<DoGPostProviCutscene>());

            if (deathEffectsTimer >= dieTime)
                DoBehavior_DropLootAndDie(npc, target);
        }

        private static void DoBehavior_DropLootAndDie(NPC npc, Player target)
        {
            for (int i = 0; i < 2; i++)
                GuardianComboAttackManager.CreateFireExplosion(npc.Center, true);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<ProvBoomDeath>(), 0, 0f);
                Utilities.CreateShockwave(npc.Center, 3, 13, 150, false);

                for (int i = 0; i < 80; i++)
                {
                    Vector2 shootVelocity = Main.rand.NextVector2CircularEdge(7f, 7f) * Main.rand.NextFloat(0.7f, 1.3f);
                    if (Vector2.Dot(shootVelocity.SafeNormalize(Vector2.Zero), npc.SafeDirectionTo(target.Center)) < 0.5f)
                        shootVelocity *= 1.7f;

                    Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<SwirlingFire>(), 0, 0f, 255);
                }
                npc.Center += Vector2.UnitY * 55f;

                npc.active = false;
                if (!target.dead)
                {
                    npc.HitEffect();
                    npc.NPCLoot();
                }
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_FireEnergyCharge(NPC npc, Player target, float lifeRatio, int localAttackTimer, int localAttackDuration, ref float drawState, ref float burnIntensity)
        {
            int shootDelay = 60;
            int fireballCircleShootRate = GetBPMTimeMultiplier(4);
            float attackCompletion = localAttackTimer / (float)localAttackDuration;
            bool attackIsAboutToEnd = localAttackTimer >= localAttackDuration - 6f;
            bool canShootCircle = attackCompletion >= 0.5f;
            float circleShootSpeed = Lerp(6f, 9.25f, 1f - lifeRatio);
            ref float shootTimer = ref npc.Infernum().ExtraAI[0];
            ref float performedInitializations = ref npc.Infernum().ExtraAI[1];
            ref float performedEndEffects = ref npc.Infernum().ExtraAI[2];
            ref float ringShootTimer = ref npc.Infernum().ExtraAI[3];

            int waveReleaseRate = GetBPMTimeMultiplier(2);
            int fireballShootRate = (int)Lerp(14f, 8f, attackCompletion);
            int fireballCircleShootCount = (int)Lerp(12f, 18f, attackCompletion);
            float fireballShootSpeedBoost = (1f - lifeRatio) * 4f + attackCompletion * 4f;

            // Enter the cocoon.
            drawState = (int)ProvidenceFrameDrawingType.CocoonState;

            // Become fully opaque.
            npc.Opacity = 1f;
            burnIntensity = Pow(Utils.GetLerpValue(90f, 0f, localAttackTimer, true), 0.12f) * 0.96f;

            npc.Calamity().DR = 0.90f;

            if (localAttackTimer <= 5f)
                performedEndEffects = 0f;

            // Rise on the first frame.
            if (performedInitializations == 0f && !Utilities.AnyProjectiles(ModContent.ProjectileType<ProfanedLava>()))
            {
                // Play the burn sound universally.
                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceBurnSound);
                Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 15f;
                ScreenEffectSystem.SetFlashEffect(npc.Center, 3f, 45);

                performedInitializations = 1f;
                npc.position = npc.Center;
                npc.Size = new Vector2(48f, 108f);
                npc.position -= npc.Size * 0.5f;
                npc.velocity = Vector2.UnitY * -12f;
                npc.netUpdate = true;
            }

            // Slow down after the initial rise effect.
            npc.velocity *= 0.97f;

            // Release fireballs around the player that converge in on Providence.
            shootTimer++;
            if (Main.netMode != NetmodeID.MultiplayerClient && localAttackTimer >= shootDelay && shootTimer >= fireballShootRate && !attackIsAboutToEnd)
            {
                Vector2 fireballSpawnPosition = target.Center + Main.rand.NextVector2CircularEdge(150f, 150f) * Main.rand.NextFloat(0.9f, 1f) + target.velocity * 90f;
                fireballSpawnPosition += npc.SafeDirectionTo(target.Center) * 1050f;

                float fireballShootSpeed = npc.Distance(fireballSpawnPosition) * 0.004f + fireballShootSpeedBoost + 4f;
                float minSpeed = IsEnraged ? 16f : 10f;
                if (fireballShootSpeed < minSpeed)
                    fireballShootSpeed = minSpeed;

                Vector2 fireballSpiralVelocity = (npc.Center - fireballSpawnPosition).SafeNormalize(Vector2.UnitY) * fireballShootSpeed;
                while (target.WithinRange(fireballSpawnPosition, 750f))
                    fireballSpawnPosition -= fireballSpiralVelocity;

                Utilities.NewProjectileBetter(fireballSpawnPosition, fireballSpiralVelocity, ModContent.ProjectileType<HolyBasicFireball>(), BasicFireballDamage, 0f, -1, 0f, 1f);

                shootTimer = 0f;

                // The frequency of these projectile firing conditions may be enough to trigger the anti NPC packet spam system that Terraria uses.
                // Consequently, that system is ignored for this specific sync.
                npc.netSpam = 0;
                npc.netUpdate = true;
            }

            if (localAttackTimer % waveReleaseRate == waveReleaseRate - 1f && !attackIsAboutToEnd)
            {
                // Play a sizzle sound and create light effects.
                SoundEngine.PlaySound(InfernumSoundRegistry.SizzleSound with
                {
                    MaxInstances = 1
                });
                target.Infernum_Camera().CurrentScreenShakePower = 3f;
                ScreenEffectSystem.SetFlashEffect(npc.Center, 1f, 10);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<ProvidenceWave>(), 0, 0f);
            }

            // Jitter in place after a while.
            float jitterInterpolant = Utils.GetLerpValue(0.55f, 0.95f, attackCompletion, true);
            npc.Center += Main.rand.NextVector2Unit() * jitterInterpolant * 4f;

            // Destroy all fireballs when the attack is about to end.
            if (performedEndEffects == 0f && attackIsAboutToEnd)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceBurnSound);
                target.Infernum_Camera().CurrentScreenShakePower = 20f;
                ScreenEffectSystem.SetBlurEffect(npc.Center, 1f, 30);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<ProvidenceWave>(), 0, 0f);
                Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<HolyBasicFireball>());

                performedEndEffects = 1f;
                npc.Size = new(600f, 450f);
                npc.Center = target.Center - Vector2.UnitY * 400f;
                ReleaseSparkles(npc.Center, 80, 75f);

                npc.netUpdate = true;
            }

            // Release fireball circles periodically.
            if (canShootCircle)
                ringShootTimer++;
            if (canShootCircle && ringShootTimer % fireballCircleShootRate == 0f)
            {
                // Play a sizzle sound and create light effects to accompany the circle.
                SoundEngine.PlaySound(InfernumSoundRegistry.SizzleSound);
                target.Infernum_Camera().CurrentScreenShakePower = 3f;
                ScreenEffectSystem.SetFlashEffect(npc.Center, 1f, fireballCircleShootRate / 3);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float shootOffsetAngle = (ringShootTimer % (fireballCircleShootRate * 2f) == 0f) ? Pi / fireballCircleShootCount : 0f;
                    for (int i = 0; i < fireballCircleShootCount; i++)
                    {
                        Vector2 fireballCircleVelocity = (TwoPi * i / fireballCircleShootCount + shootOffsetAngle).ToRotationVector2() * circleShootSpeed;
                        Utilities.NewProjectileBetter(npc.Center, fireballCircleVelocity, ModContent.ProjectileType<HolyBasicFireball>(), BasicFireballDamage, 0f);
                    }
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<ProvidenceWave>(), 0, 0f);
                }
            }
        }

        public static void DoBehavior_CinderAndBombBarrages(NPC npc, Player target, float lifeRatio, int localAttackTimer, int localAttackDuration, ref float flightPath)
        {
            int shootRate = 80;
            int cinderShootRate = GetBPMTimeMultiplier(2);
            int bombShootRate = GetBPMTimeMultiplier(lifeRatio < 0.5f ? 3 : 4);
            bool doneAttacking = localAttackTimer >= localAttackDuration - 10;
            float cinderSpacing = Lerp(225f, 180f, 1f - lifeRatio);
            float bombShootSpeed = Lerp(17f, 21.5f, 1f - lifeRatio);
            float bombExplosionRadius = Lerp(1300f, 1776f, 1f - lifeRatio);
            ref float hasDonePhaseTransitionEffects = ref npc.Infernum().ExtraAI[0];
            ref float bombShootCounter = ref npc.Infernum().ExtraAI[1];
            ref float cinderShootTimer = ref npc.Infernum().ExtraAI[2];

            // Fly above the target.
            DoVanillaFlightMovement(npc, target, true, ref flightPath);

            // Release cinders from above the target periodically.
            cinderShootTimer++;
            if (localAttackTimer >= shootRate && cinderShootTimer >= cinderShootRate && !doneAttacking)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.SizzleSound, target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (float dx = -2000f; dx < 2000f; dx += cinderSpacing + Main.rand.NextFloat(50f))
                    {
                        Vector2 cinderSpawnPosition = target.Center + new Vector2(dx, -Main.rand.NextFloat(850f, 900f));
                        Utilities.NewProjectileBetter(cinderSpawnPosition, Vector2.UnitY.RotatedByRandom(0.06f) * 4f, ModContent.ProjectileType<HolyCinder>(), CinderDamage, 0f);
                    }
                    cinderShootTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            // Release bombs at the target.
            if (localAttackTimer >= shootRate && localAttackTimer % bombShootRate == 0f && !doneAttacking)
            {
                Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 7.5f;
                ScreenEffectSystem.SetFlashEffect(npc.Center, 1.2f, 35);

                for (int i = 0; i < 32; i++)
                {
                    Color fireColor = Main.rand.NextBool() ? Color.Yellow : Color.Orange;
                    CloudParticle fireCloud = new(npc.Center, (TwoPi * i / 32f).ToRotationVector2() * 14.5f, fireColor, Color.DarkGray, 45, Main.rand.NextFloat(2.5f, 3.2f));
                    GeneralParticleHandler.SpawnParticle(fireCloud);
                }

                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceHolyBlastShootSound, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 aimDestination = target.Center;
                    if (bombShootCounter % 2f == 1f)
                    {
                        aimDestination += target.velocity * 50f;
                        bombShootSpeed *= 0.667f;
                    }

                    Vector2 bombShootVelocity = npc.SafeDirectionTo(aimDestination) * bombShootSpeed;
                    Utilities.NewProjectileBetter(npc.Center + bombShootVelocity, bombShootVelocity, ModContent.ProjectileType<HolyBomb>(), 0, 0f, -1, bombExplosionRadius);

                    bombShootCounter++;
                    npc.netUpdate = true;
                }
            }

            // Delete everything when ready to transition to the next attack.
            // Also do some very, very strong transition effects.
            if (doneAttacking)
            {
                // Make all bombs that aren't close to the target explode.
                // Once that are close to the target simply disappear, since the player can't reasonably expect the sudden explosion in their face.
                foreach (Projectile bomb in Utilities.AllProjectilesByID(ModContent.ProjectileType<HolyBomb>()))
                {
                    if (bomb.WithinRange(target.Center, bombExplosionRadius + 100f))
                        bomb.active = false;
                    else
                        bomb.Kill();
                }
                Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<HolyCinder>());

                if (hasDonePhaseTransitionEffects == 0f)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceHolyBlastShootSound);

                    Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 18f;
                    ScreenEffectSystem.SetBlurEffect(npc.Center, 0.9f, 40);
                    hasDonePhaseTransitionEffects = 1f;
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoBehavior_AcceleratingCrystalFan(NPC npc, Player target, Vector2 crystalCenter, float lifeRatio, ref float flightPath)
        {
            int crystalFireDelay = GetBPMTimeMultiplier(4);
            int crystalReleaseRate = 2;
            int crystalReleaseCount = 20;
            float maxFanOffsetAngle = 1.37f;
            float crystalSpeed = Lerp(13.5f, 15.75f, 1f - lifeRatio);

            if (IsEnraged)
            {
                crystalReleaseRate = 1;
                maxFanOffsetAngle += 0.24f;
                crystalSpeed += 4f;
            }

            ref float attackTimer = ref npc.Infernum().ExtraAI[0];
            ref float initialDirection = ref npc.Infernum().ExtraAI[1];

            // Create the visual effects right before the crystals fire.
            attackTimer++;
            if (attackTimer == crystalFireDelay)
            {
                Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 6f;
                ScreenEffectSystem.SetFlashEffect(npc.Center, 2f, 20);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(bomb =>
                    {
                        bomb.timeLeft = 95;
                    });
                    Utilities.NewProjectileBetter(npc.Center, Vector2.UnitY * 0.001f, ModContent.ProjectileType<HolyBomb>(), 0, 0f, -1, 1360f);
                }
            }

            // Release a fan of crystals.
            if (attackTimer >= crystalFireDelay)
            {
                // Slow down.
                npc.velocity *= 0.92f;

                // Recede away from the target if they're close.
                if (npc.WithinRange(target.Center, 360f))
                    npc.Center -= npc.SafeDirectionTo(target.Center, -Vector2.UnitY) * 7.5f;

                // Decide an initial direction angle and play a sound to accommodate the crystals.
                if (attackTimer == crystalFireDelay)
                {
                    SoundEngine.PlaySound(SoundID.Item164, npc.Center);
                    initialDirection = (target.Center - crystalCenter).ToRotation();
                    npc.netUpdate = true;
                }

                // Cast dust outward that projects the radial area where the crystals will fire.
                Vector2 leftDirection = (initialDirection - maxFanOffsetAngle).ToRotationVector2();
                Vector2 rightDirection = (initialDirection + maxFanOffsetAngle).ToRotationVector2();
                for (int i = 0; i < 4; i++)
                {
                    Vector2 fireSpawnPosition = npc.Center + leftDirection * Main.rand.NextFloat(1250f);
                    Dust fire = Dust.NewDustPerfect(fireSpawnPosition, !IsEnraged ? 222 : 221);
                    fire.scale = 1.5f;
                    fire.fadeIn = 0.4f;
                    fire.velocity = leftDirection * Main.rand.NextFloat(8f);
                    fire.noGravity = true;

                    fireSpawnPosition = npc.Center + rightDirection * Main.rand.NextFloat(1250f);
                    fire = Dust.NewDustPerfect(fireSpawnPosition, !IsEnraged ? 222 : 221);
                    fire.scale = 1.5f;
                    fire.fadeIn = 0.4f;
                    fire.velocity = rightDirection * Main.rand.NextFloat(8f);
                    fire.noGravity = true;
                }

                // Recreate crystals.
                if (attackTimer % crystalReleaseRate == crystalReleaseRate - 1f)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float fanInterpolant = Utils.GetLerpValue(0f, crystalReleaseRate * crystalReleaseCount, attackTimer - crystalFireDelay, true);
                        float offsetAngle = Lerp(-maxFanOffsetAngle, maxFanOffsetAngle, fanInterpolant);
                        Vector2 shootVelocity = (initialDirection + offsetAngle).ToRotationVector2() * crystalSpeed;
                        Utilities.NewProjectileBetter(crystalCenter, shootVelocity, ModContent.ProjectileType<AcceleratingCrystalShard>(), CrystalShardDamage, 0f);
                        Utilities.NewProjectileBetter(crystalCenter, shootVelocity.SafeNormalize(Vector2.UnitY) * 0.01f, ModContent.ProjectileType<CrystalTelegraphLine>(), 0, 0f, -1, 0f, 30f);
                    }
                }

                if (attackTimer >= crystalFireDelay + crystalReleaseRate * crystalReleaseCount)
                {
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            // Fly around.
            else
                DoVanillaFlightMovement(npc, target, true, ref flightPath, 1.6f);
        }

        public static void DoBehavior_AttackGuardiansSpearSlam(NPC npc, Player target, float lifeRatio, int localAttackTimer, int localAttackDuration, ref float drawState, ref float flightPath)
        {
            int chargeUpTime = 150;
            int guardianCount = 3;
            int guardiansSpearSpinTimeStart = GetBPMTimeMultiplier(2);
            int guardiansSpearAimTime = GetBPMTimeMultiplier(2);
            int guardiansPostShootLiveTime = GetBPMTimeMultiplier(3);
            int guardiansKamikazeDelay = GetBPMTimeMultiplier(6);
            int attackCycle = guardiansSpearSpinTimeStart + guardiansSpearAimTime + guardiansPostShootLiveTime + guardiansKamikazeDelay;
            int attackCycleTimer = (localAttackTimer - chargeUpTime) % attackCycle;
            bool doneAttacking = localAttackTimer >= localAttackDuration - 90;
            float flightSpeedFactor = 1f;
            float spearShootSpeed = Lerp(21f, 26f, 1f - lifeRatio);

            if (IsEnraged)
            {
                chargeUpTime -= 70;
                spearShootSpeed += 6f;
            }

            ref float spearAttackState = ref npc.Infernum().ExtraAI[0];
            ref float spearSmearInterpolant = ref npc.Infernum().ExtraAI[1];
            ref float guardiansVerticalOffset = ref npc.Infernum().ExtraAI[2];
            ref float guardiansShouldExplode = ref npc.Infernum().ExtraAI[3];

            // Teleport above the player at first.
            if (localAttackTimer == 1f)
            {
                // Destroy any leftover crystals.
                Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<AcceleratingCrystalShard>());

                npc.Center = target.Center - Vector2.UnitY * 400f;
                npc.velocity = Vector2.Zero;
                ReleaseSparkles(npc.Center, 85, 67f);
                npc.netUpdate = true;
            }

            // Enter the cocoon and wait.
            if (localAttackTimer < chargeUpTime)
            {
                flightSpeedFactor = 0f;
                drawState = (int)ProvidenceFrameDrawingType.CocoonState;
                npc.velocity.Y = 0f;
            }

            // Summon the attacker guardians.
            if (!doneAttacking && localAttackTimer >= chargeUpTime && !NPC.AnyNPCs(ModContent.NPCType<ProvSpawnOffense>()) && guardiansShouldExplode == 0f)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceBurnSound);
                Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 15f;
                ScreenEffectSystem.SetFlashEffect(npc.Center, 2f, 24);

                for (int i = 0; i < guardianCount; i++)
                {
                    float spawnOffsetRadius = 485f;
                    float spawnOffsetAngle = Lerp(-1f, 1f, i / (float)(guardianCount - 1f)) * PiOver4;
                    Color ashColor = IsEnraged ? Color.Turquoise : new Color(255, 191, 73);
                    Vector2 guardianSpawnPosition = npc.Center - Vector2.UnitY.RotatedBy(spawnOffsetAngle) * spawnOffsetRadius;
                    for (int j = 0; j < 67; j++)
                    {
                        Vector2 ashSpawnPosition = guardianSpawnPosition + Main.rand.NextVector2Circular(100f, 100f);
                        Vector2 ashVelocity = npc.SafeDirectionTo(ashSpawnPosition) * Main.rand.NextFloat(1.5f, 2f);
                        Particle ash = new MediumMistParticle(ashSpawnPosition, ashVelocity, ashColor, Color.Gray, Main.rand.NextFloat(0.7f, 0.9f), 200f, Main.rand.NextFloat(-0.04f, 0.04f));
                        GeneralParticleHandler.SpawnParticle(ash);
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        guardiansVerticalOffset = 0f;
                        npc.netUpdate = true;

                        int guardian = NPC.NewNPC(npc.GetSource_FromThis(), (int)guardianSpawnPosition.X, (int)guardianSpawnPosition.Y, ModContent.NPCType<ProvSpawnOffense>(), npc.whoAmI, spawnOffsetRadius, spawnOffsetAngle);
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, guardian);
                    }
                }
            }

            // Kill all spears and guardians if the attack needs to end.
            if (doneAttacking)
            {
                Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<CommanderSpear2>());
                Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<ProfanedSpearInfernum>());

                int guardianID = ModContent.NPCType<ProvSpawnOffense>();
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n.active && n.type == guardianID)
                        n.active = false;
                }
            }

            if (localAttackTimer >= chargeUpTime && !doneAttacking)
            {
                // Make all guardians spin their spears at first.
                if (attackCycleTimer < guardiansSpearSpinTimeStart)
                {
                    guardiansShouldExplode = 0f;
                    guardiansVerticalOffset = 0f;
                    spearAttackState = (int)SpearAttackState.SpinInPlace;
                    spearSmearInterpolant = Utils.GetLerpValue(0f, 8f, attackCycleTimer, true) * Utils.GetLerpValue(-1f, -8f, attackCycleTimer - guardiansSpearSpinTimeStart, true);
                    flightSpeedFactor = Utils.GetLerpValue(-8f, -20f, attackCycleTimer - guardiansSpearSpinTimeStart, true);
                }

                // Have all Guardians aim their spears at the player and create some wacky Jojo Menacing particles for personality.
                else if (attackCycleTimer < guardiansSpearSpinTimeStart + guardiansSpearAimTime)
                {
                    spearSmearInterpolant = 0f;
                    flightSpeedFactor = 0f;
                    guardiansVerticalOffset = 0f;
                    spearAttackState = (int)SpearAttackState.LookAtTarget;

                    // Create the particles and play a metal sound initially.
                    if (attackCycleTimer == guardiansSpearSpinTimeStart + 1)
                    {
                        SoundEngine.PlaySound(InfernumSoundRegistry.VassalSlashSound with
                        {
                            Pitch = -0.125f
                        }, target.Center);

                        int guardianID = ModContent.NPCType<ProvSpawnOffense>();
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            NPC n = Main.npc[i];
                            if (n.active && n.type == guardianID)
                            {
                                for (int j = 0; j < 4; j++)
                                {
                                    Vector2 position = n.Center + n.SafeDirectionTo(target.Center) * 50f + Main.rand.NextVector2Circular(120f, 120f);
                                    Vector2 velocity = -Vector2.UnitY.RotatedBy(Main.rand.NextFloat(0.1f, 0.4f)) * Main.rand.NextFloat(1.5f, 2f);
                                    Color color = Color.Lerp(WayfinderSymbol.Colors[0], WayfinderSymbol.Colors[1], Main.rand.NextFloat());
                                    Particle jojo = new ProfanedSymbolParticle(position, velocity, color, 0.8f, 60);
                                    GeneralParticleHandler.SpawnParticle(jojo);
                                }
                            }
                        }
                    }
                }

                // Make the guardians throw all their spears.
                else if (attackCycleTimer < guardiansSpearSpinTimeStart + guardiansSpearAimTime + guardiansPostShootLiveTime)
                {
                    // Make the Guardians rise upward in anticipation.
                    guardiansVerticalOffset = (guardiansVerticalOffset - 7.5f) * 1.03f;
                    if (guardiansVerticalOffset < -300f)
                        guardiansVerticalOffset = -300f;
                    spearAttackState = (int)SpearAttackState.Charge;

                    if (attackCycleTimer >= guardiansSpearSpinTimeStart + guardiansSpearAimTime)
                    {
                        foreach (Projectile spear in Utilities.AllProjectilesByID(ModContent.ProjectileType<CommanderSpear2>()))
                        {
                            if (spear.velocity.Length() >= 1f)
                                continue;

                            for (int i = 0; i < 32; i++)
                            {
                                Color fireColor = Main.rand.NextBool() ? Color.Yellow : Color.Orange;
                                CloudParticle fireCloud = new(spear.Center, (TwoPi * i / 32f).ToRotationVector2() * 14.5f, fireColor, Color.DarkGray, 45, Main.rand.NextFloat(2.5f, 3.2f));
                                GeneralParticleHandler.SpawnParticle(fireCloud);
                            }

                            spear.velocity = spear.SafeDirectionTo(target.Center) * spearShootSpeed;
                            spear.netUpdate = true;
                        }
                    }
                }

                // Have Providence go all LTG and instruct the guardians to kill themselves NOW!
                else
                {
                    guardiansShouldExplode = 1f;
                    guardiansVerticalOffset += 25f;
                    if (guardiansVerticalOffset >= 0f)
                        guardiansVerticalOffset *= 1.05f;
                    flightSpeedFactor = 0f;
                }
            }

            // Fly above the target.
            DoVanillaFlightMovement(npc, target, false, ref flightPath, flightSpeedFactor);
        }

        public static void DoBehavior_HealerGuardianCrystalBarrage(NPC npc, Player target, float lifeRatio, int localAttackTimer, int localAttackDuration, ref float drawState, ref float flightPath)
        {
            int chargeUpTime = 150;
            int guardianCount = 4;
            int guardiansSpinTime = GetBPMTimeMultiplier(2);
            int guardiansTelegraphTime = GetBPMTimeMultiplier(3);
            int guardiansPostShootLiveTime = GetBPMTimeMultiplier(4);
            int guardiansDeathAnimationTime = GetBPMTimeMultiplier(4);
            int attackCycle = guardiansSpinTime + guardiansTelegraphTime + guardiansPostShootLiveTime + guardiansDeathAnimationTime;
            int attackCycleTimer = (localAttackTimer - chargeUpTime) % attackCycle;

            if (localAttackTimer >= localAttackDuration - guardiansDeathAnimationTime - 10)
            {
                attackCycleTimer = guardiansSpinTime + guardiansTelegraphTime + guardiansPostShootLiveTime + 5;
                npc.Infernum().ExtraAI[3] = 0f;
            }

            float flightSpeedFactor = Lerp(1f, 1.45f, 1f - lifeRatio);
            ref float healerAttackState = ref npc.Infernum().ExtraAI[0];
            ref float spinAngularOffset = ref npc.Infernum().ExtraAI[1];
            ref float spinRadiusOffset = ref npc.Infernum().ExtraAI[2];
            ref float telegraphInterpolant = ref npc.Infernum().ExtraAI[3];
            ref float createdSpikes = ref npc.Infernum().ExtraAI[4];

            npc.Opacity = 1f;

            // Teleport above the player at first.
            if (localAttackTimer == 1f)
            {
                // Destroy any leftover crystals.
                Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<AcceleratingCrystalShard>());

                npc.Center = target.Center - Vector2.UnitY * 400f;
                npc.velocity = Vector2.Zero;
                ReleaseSparkles(npc.Center, 85, 67f);
                npc.netUpdate = true;
            }

            // Enter the cocoon and wait.
            if (localAttackTimer < chargeUpTime)
            {
                createdSpikes = 0f;
                flightSpeedFactor = 0f;
                drawState = (int)ProvidenceFrameDrawingType.CocoonState;
                npc.velocity.Y = 0f;
            }

            // Summon the healer guardians.
            if (localAttackTimer >= chargeUpTime && !NPC.AnyNPCs(ModContent.NPCType<ProvSpawnHealer>()) && attackCycleTimer <= guardiansSpinTime + guardiansTelegraphTime + guardiansPostShootLiveTime)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceHolyRaySound);
                Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 15f;
                ScreenEffectSystem.SetFlashEffect(npc.Center, 2.4f, 32);

                // Reset things from the previous cycle.
                spinAngularOffset = 0f;
                spinRadiusOffset = 0f;
                telegraphInterpolant = 0f;
                healerAttackState = (int)HealerGuardianAttackState.SpinInPlace;
                npc.netUpdate = true;

                for (int i = 0; i < guardianCount; i++)
                {
                    float spawnOffsetRadius = 350f;
                    float spawnOffsetAngle = TwoPi * i / guardianCount + PiOver4;
                    Color ashColor = IsEnraged ? Color.Turquoise : new Color(255, 191, 73);
                    Vector2 guardianSpawnPosition = npc.Center - Vector2.UnitY.RotatedBy(spawnOffsetAngle) * spawnOffsetRadius;
                    for (int j = 0; j < 60; j++)
                    {
                        Vector2 ashSpawnPosition = guardianSpawnPosition + Main.rand.NextVector2Circular(100f, 100f);
                        Vector2 ashVelocity = npc.SafeDirectionTo(ashSpawnPosition) * Main.rand.NextFloat(1.5f, 2f);
                        Particle ash = new MediumMistParticle(ashSpawnPosition, ashVelocity, ashColor, Color.Gray, Main.rand.NextFloat(0.7f, 0.9f), 200f, Main.rand.NextFloat(-0.04f, 0.04f));
                        GeneralParticleHandler.SpawnParticle(ash);
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int guardian = NPC.NewNPC(npc.GetSource_FromThis(), (int)guardianSpawnPosition.X, (int)guardianSpawnPosition.Y, ModContent.NPCType<ProvSpawnHealer>(), npc.whoAmI, spawnOffsetRadius, spawnOffsetAngle);
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, guardian);
                        ReleaseSparkles(guardianSpawnPosition, 25, 36f);
                    }
                }
            }

            // Make the guardians spin in place.
            if (attackCycleTimer <= guardiansSpinTime)
            {
                float spinCompletion = attackCycleTimer / (float)guardiansSpinTime;
                float spinOffsetAngleInterpolant = Utilities.UltrasmoothStep(spinCompletion);
                spinAngularOffset = Pi * spinOffsetAngleInterpolant;
                healerAttackState = (int)HealerGuardianAttackState.SpinInPlace;
            }

            // Make the guardians sit and release telegraphs at the target.
            else if (attackCycleTimer <= guardiansSpinTime + guardiansTelegraphTime)
            {
                createdSpikes = 0f;
                float subphaseCompletion = Utils.GetLerpValue(0f, guardiansTelegraphTime, attackCycleTimer - guardiansSpinTime, true);
                telegraphInterpolant = Utils.GetLerpValue(0f, 0.6f, subphaseCompletion, true) * Utils.GetLerpValue(1f, 0.85f, subphaseCompletion, true);
                healerAttackState = (int)HealerGuardianAttackState.WaitAndReleaseTelegraph;

                // Slow down the flying motion near the end of the telegraph casting.
                flightSpeedFactor = Utils.GetLerpValue(0.9f, 0.65f, subphaseCompletion, true);
            }

            // Wait for the crystal spikes to be shot.
            else if (attackCycleTimer <= guardiansSpinTime + guardiansTelegraphTime + guardiansPostShootLiveTime)
            {
                healerAttackState = (int)HealerGuardianAttackState.ShootCrystals;

                // Have the guardians all shoot crystal spikes.
                if (attackCycleTimer >= guardiansSpinTime + guardiansTelegraphTime + 1 && createdSpikes == 0f)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.SizzleSound, target.Center);
                    SoundEngine.PlaySound(SoundID.Item101, target.Center);
                    ScreenEffectSystem.SetFlashEffect(npc.Center, 2f, 20);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int guardianID = ModContent.NPCType<ProvSpawnHealer>();
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            NPC n = Main.npc[i];
                            if (n.type != guardianID || !n.active)
                                continue;

                            Vector2 spikeSpawnPosition = ProvidenceHealerGuardianBehaviorOverride.GetCrystalPosition(n);
                            Vector2 spikeDirection = n.ai[2].ToRotationVector2();
                            Utilities.NewProjectileBetter(spikeSpawnPosition - spikeDirection * 50f, spikeDirection, ModContent.ProjectileType<HolyCrystalSpike>(), CrystalSpikeDamage, 0f);
                        }
                    }
                    createdSpikes = 1f;
                    npc.netUpdate = true;
                }

                // Cease any and all movement.
                flightSpeedFactor = 0f;
                npc.velocity.Y = 0f;
            }

            // Have Providence instruct the guardians to fly away.
            else
            {
                healerAttackState = (int)HealerGuardianAttackState.ShootCrystals;

                spinRadiusOffset += 25f;
                spinAngularOffset += Pi * 0.02f;
                createdSpikes = 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient && spinRadiusOffset >= 1400f)
                {
                    int guardianID = ModContent.NPCType<ProvSpawnHealer>();
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC n = Main.npc[i];
                        if (n.type != guardianID || !n.active)
                            continue;

                        for (int j = 0; j < 20; j++)
                        {
                            Vector2 ashSpawnPosition = n.Center + Main.rand.NextVector2Circular(100f, 100f);
                            Vector2 ashVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.5f, 2f);
                            Particle ash = new MediumMistParticle(ashSpawnPosition, ashVelocity, new Color(255, 191, 73), Color.Gray, Main.rand.NextFloat(0.7f, 0.9f), 200f, Main.rand.NextFloat(-0.04f, 0.04f));
                            GeneralParticleHandler.SpawnParticle(ash);
                        }
                        n.active = false;
                    }
                }
            }

            // Fly above the target.
            DoVanillaFlightMovement(npc, target, false, ref flightPath, flightSpeedFactor);
        }

        public static void DoBehavior_EnterFireFormBulletHell(NPC npc, Player target, Vector2 arenaTopCenter, float lifeRatio, int localAttackTimer, int localAttackDuration, ref float drawState, ref float lavaHeight)
        {
            int shootDelay = 75;
            int startingShootCycle = 67;
            int endingShootCycle = 36;
            int fireballCircleShootRate = GetBPMTimeMultiplier(4);
            float idealLavaHeight = DefaultLavaHeight;
            float attackCompletion = localAttackTimer / (float)localAttackDuration;
            ref float shootTimer = ref npc.Infernum().ExtraAI[0];
            ref float cycleTimer = ref npc.Infernum().ExtraAI[1];
            ref float shootCycle = ref npc.Infernum().ExtraAI[2];
            ref float performedInitializations = ref npc.Infernum().ExtraAI[3];

            // Initialize the shoot cycle value.
            if (shootCycle <= 0f)
                shootCycle = startingShootCycle;

            int fireballCircleShootCount = (int)Lerp(12f, 22f, attackCompletion);
            int shootRate = (int)Lerp(6f, 3f, attackCompletion);
            float spiralShootSpeed = Lerp(3.5f, 4.5f, attackCompletion);
            float circleShootSpeed = spiralShootSpeed * 1.36f;
            bool canShootCircle = attackCompletion >= 0.5f;

            npc.Calamity().DR = 0.90f;

            // Make the attack faster according to life ratio.
            // This may seem unintuitive since it's a "quiet" attack but there's always the possibility that the player won't kill Providence within one
            // music cycle, meaning that she could have significantly weakened HP by the time this happens a second or third time.
            spiralShootSpeed += (1f - lifeRatio) * 2.85f;
            circleShootSpeed += (1f - lifeRatio) * 3.72f;

            if (IsEnraged)
            {
                spiralShootSpeed += 6f;
                circleShootSpeed += 8f;
            }

            // Enter the cocoon.
            drawState = (int)ProvidenceFrameDrawingType.CocoonState;

            // Be fully opaque from the start.
            npc.Opacity = 1f;

            // Create the lava on the first frame.
            if (performedInitializations == 0f && !Utilities.AnyProjectiles(ModContent.ProjectileType<ProfanedLava>()))
            {
                // Play the burn sound universally.
                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceBurnSound);
                Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 15f;
                ScreenEffectSystem.SetFlashEffect(npc.Center, 3f, 45);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    CalamityUtils.DisplayLocalizedText("Mods.InfernumMode.Status.ProvidenceLavaRise", IsEnraged ? Color.SkyBlue : Color.Orange);
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<ProfanedLava>(), 350, 0f);
                }

                // Rise above the lava.
                performedInitializations = 1f;
                npc.Center = arenaTopCenter + Vector2.UnitY * 600f;
                ReleaseSparkles(npc.Center, 100, 90f);

                npc.velocity = Vector2.UnitY * -13f;
                npc.netUpdate = true;
            }

            // Slow down after the initial rise effect.
            npc.velocity *= 0.97f;

            // Make the lava rise upward.
            lavaHeight = Lerp(lavaHeight, idealLavaHeight, lavaHeight > idealLavaHeight ? 0.018f : 0.006f);

            // Begin firing bursts of holy fireballs once the shoot delay has elapsed.
            if (localAttackTimer >= shootDelay)
            {
                shootTimer++;
                cycleTimer++;

                if (Main.netMode != NetmodeID.MultiplayerClient && shootTimer >= shootRate)
                {
                    Vector2 fireballSpiralVelocity = -Vector2.UnitY.RotatedBy(TwoPi * cycleTimer / shootCycle) * spiralShootSpeed;
                    Utilities.NewProjectileBetter(npc.Center, fireballSpiralVelocity, ModContent.ProjectileType<HolyBasicFireball>(), BasicFireballDamage, 0f);
                    Utilities.NewProjectileBetter(npc.Center, -fireballSpiralVelocity, ModContent.ProjectileType<HolyBasicFireball>(), BasicFireballDamage, 0f);

                    shootTimer = 0f;

                    // The frequency of these projectile firing conditions may be enough to trigger the anti NPC packet spam system that Terraria uses.
                    // Consequently, that system is ignored for this specific sync.
                    npc.netSpam = 0;
                    npc.netUpdate = true;
                }

                // Reset the cycle and calculate the duration of the next one if it's finished.
                if (cycleTimer >= shootCycle)
                {
                    shootCycle = Lerp(startingShootCycle, endingShootCycle, attackCompletion);
                    cycleTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            // Release fireball circles and rocks if necessary.
            if (canShootCircle && localAttackTimer % fireballCircleShootRate == 0f)
            {
                // Play a sizzle sound and create light effects to accompany the circle.
                SoundEngine.PlaySound(InfernumSoundRegistry.SizzleSound);
                target.Infernum_Camera().CurrentScreenShakePower = 3f;
                ScreenEffectSystem.SetFlashEffect(npc.Center, 1f, fireballCircleShootRate / 3);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float shootOffsetAngle = (localAttackTimer % (fireballCircleShootRate * 2f) == 0f) ? Pi / fireballCircleShootCount : 0f;
                    for (int i = 0; i < fireballCircleShootCount; i++)
                    {
                        Vector2 fireballCircleVelocity = (TwoPi * i / fireballCircleShootCount + shootOffsetAngle).ToRotationVector2() * circleShootSpeed;
                        Utilities.NewProjectileBetter(npc.Center, fireballCircleVelocity, ModContent.ProjectileType<HolyBasicFireball>(), BasicFireballDamage, 0f);
                    }

                    // Release rocks from above.
                    for (float dx = -900f; dx < 900f; dx += Main.rand.NextFloat(237f, 300f))
                    {
                        Vector2 rockSpawnPosition = target.Center + new Vector2(dx, -820f);
                        Vector2 rockVelocity = Vector2.UnitY.RotateRandom(0.06f) * 6f;
                        int projID = !Main.rand.NextBool(4) ? ModContent.ProjectileType<HolyCinder>() : ModContent.ProjectileType<AcceleratingMagicProfanedRock>();
                        Utilities.NewProjectileBetter(rockSpawnPosition, rockVelocity, projID, MagicRockDamage, 0f);
                    }
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<ProvidenceWave>(), 0, 0f);
                }
            }
        }

        public static void DoBehavior_EnvironmentalFireEffects(NPC npc, Player target, int localAttackTimer, int localAttackDuration, ref float drawState)
        {
            float attackCompletion = localAttackTimer / (float)localAttackDuration;
            float bombExplosionRadius = 560f;
            bool doneAttacking = attackCompletion >= 0.98f;
            ref float bombCreationTimer = ref npc.Infernum().ExtraAI[0];
            ref float hasDonePhaseTransitionEffects = ref npc.Infernum().ExtraAI[1];

            int bombReleaseRate = (int)Lerp(22f, 15f, attackCompletion);
            if (IsEnraged)
            {
                bombReleaseRate -= 8;
                bombExplosionRadius += 196f;
            }

            // Clear fireballs from the previous attack at first.
            if (localAttackTimer <= 5)
                Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<HolyBasicFireball>());

            // Stay in the cocoon.
            drawState = (int)ProvidenceFrameDrawingType.CocoonState;

            npc.Calamity().DR = 0.80f;

            if (!doneAttacking)
                bombCreationTimer++;
            if (bombCreationTimer >= bombReleaseRate)
            {
                // Release the bombs. They spawn in general a bit ahead of the player so that you can just run back and forth on the arena.
                // Also shoot a single holy fireball from below, as though it's from the lava.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 aimAheadOffset = target.velocity * new Vector2(60f, 10f);
                    Vector2 bombSpawnPosition = target.Center + aimAheadOffset + Main.rand.NextVector2Unit() * Main.rand.NextFloat(120f, 650f);
                    Utilities.NewProjectileBetter(bombSpawnPosition, Vector2.UnitY * 0.01f, ModContent.ProjectileType<HolyBomb>(), 0, 0f, -1, bombExplosionRadius);
                    Utilities.NewProjectileBetter(target.Center + new Vector2(Main.rand.NextFloatDirection() * 100f, 640f), -Vector2.UnitY * 6.4f, ModContent.ProjectileType<HolyBasicFireball>(), BasicFireballDamage, 0f);

                    bombCreationTimer = 0f;

                    // The frequency of these projectile firing conditions may be enough to trigger the anti NPC packet spam system that Terraria uses.
                    // Consequently, that system is ignored for this specific sync.
                    npc.netSpam = 0;
                    npc.netUpdate = true;
                }
            }

            // Delete everything when ready to transition to the next attack.
            // Also do some very, very strong transition effects.
            if (doneAttacking)
            {
                // Make all bombs that aren't close to the target explode.
                // Once that are close to the target simply disappear, since the player can't reasonably expect the sudden explosion in their face.
                foreach (Projectile bomb in Utilities.AllProjectilesByID(ModContent.ProjectileType<HolyBomb>()))
                {
                    if (bomb.WithinRange(target.Center, bombExplosionRadius + 100f))
                        bomb.active = false;
                    else
                        bomb.Kill();
                }
                Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<HolyBasicFireball>());

                if (hasDonePhaseTransitionEffects == 0f)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceBurnSound);
                    SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceHolyBlastShootSound);
                    Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 18f;
                    ScreenEffectSystem.SetBlurEffect(npc.Center, 0.9f, 40);

                    hasDonePhaseTransitionEffects = 1f;
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoBehavior_CleansingFireballBombardment(NPC npc, Player target, float lifeRatio, int localAttackTimer, int localAttackDuration, ref float flightPath)
        {
            int attackDelay = GetBPMTimeMultiplier(4);
            int fireballBPMShootMultiplier = IsEnraged ? 1 : 2;
            int timeToReachLava = 56;
            if (lifeRatio < 0.5f)
                timeToReachLava -= 8;

            int fireballShootRate = GetBPMTimeMultiplier(fireballBPMShootMultiplier);
            ref float shootTimer = ref npc.Infernum().ExtraAI[0];

            // Fly above the target.
            DoVanillaFlightMovement(npc, target, true, ref flightPath);

            // Release the fireballs.
            if (localAttackTimer >= attackDelay && localAttackTimer <= localAttackDuration - attackDelay && shootTimer % fireballShootRate == 0f && !npc.WithinRange(target.Center, 400f))
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceHolyBlastShootSound, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 fireballDirection = npc.SafeDirectionTo(target.Center);
                    IEnumerable<Projectile> lavaProjectiles = Utilities.AllProjectilesByID(ModContent.ProjectileType<ProfanedLava>());

                    // If there is lava, check to see the distance it'll take for the fireball to reach it.
                    // This distance calculation will be used to determine the speed of the fireball.
                    float distanceToLava = 500f;
                    if (lavaProjectiles.Any())
                    {
                        Projectile lava = lavaProjectiles.First();

                        for (distanceToLava = 0f; distanceToLava < 3000f; distanceToLava += 10f)
                        {
                            Rectangle checkArea = Utils.CenteredRectangle(npc.Center + fireballDirection * distanceToLava, Vector2.One);
                            if (lava.Colliding(lava.Hitbox, checkArea))
                                break;
                        }
                    }

                    // Calculate the speed of the fireball such that it reaches the lava in a certain amount of time.
                    // This value has hard limits to prevent comically sluggish movement and outright telefrags.
                    float fireballSpeed = Clamp(distanceToLava / timeToReachLava, 7f, 27f);
                    Vector2 fireballVelocity = npc.SafeDirectionTo(target.Center) * fireballSpeed;
                    Utilities.NewProjectileBetter(npc.Center, fireballVelocity, ModContent.ProjectileType<CleansingFireball>(), BigFireballDamage, 0f);
                }
            }

            // Give a tip.
            if (shootTimer == 1f)
                HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.ProvidenceExplosionTip");

            shootTimer++;
        }

        public static void DoBehavior_CooldownState(NPC npc)
        {
            // Simply slow down.
            npc.velocity *= 0.9f;
        }

        public static void DoBehavior_ExplodingSpears(NPC npc, Player target, float lifeRatio, int localAttackTimer, ref float flightPath)
        {
            int shootDelay = GetBPMTimeMultiplier(4);
            int shootRate = GetBPMTimeMultiplier(8);
            float spearShootSpeed = Lerp(18.5f, 21.5f, 1f - lifeRatio);

            if (IsEnraged)
            {
                shootDelay /= 2;
                shootRate /= 2;
                spearShootSpeed += 10f;
            }

            ref float shootTimer = ref npc.Infernum().ExtraAI[0];

            // Fly above the target.
            DoVanillaFlightMovement(npc, target, true, ref flightPath);

            // Release spears at the target. This waits until Providence isn't inside of blocks.
            shootTimer++;
            if (Collision.SolidCollision(npc.Center - Vector2.UnitY * 10f, npc.width, 10) && shootTimer >= shootRate - 1f && npc.Center.Y >= WorldSaveSystem.ProvidenceArena.Top * 16f + 400f)
                shootTimer = shootRate - 1f;

            if (localAttackTimer >= shootDelay && shootTimer >= shootRate && !npc.WithinRange(target.Center, 345f))
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceBurnSound);
                Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 7.5f;
                ScreenEffectSystem.SetFlashEffect(npc.Center, 1.2f, 35);

                for (int i = 0; i < 32; i++)
                {
                    Color fireColor = Main.rand.NextBool() ? Color.Yellow : Color.Orange;
                    CloudParticle fireCloud = new(npc.Center, (TwoPi * i / 32f).ToRotationVector2() * 14.5f, fireColor, Color.DarkGray, 45, Main.rand.NextFloat(2.5f, 3.2f));
                    GeneralParticleHandler.SpawnParticle(fireCloud);
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center, npc.SafeDirectionTo(target.Center) * spearShootSpeed, ModContent.ProjectileType<HolySpear>(), HolySpearDamage, 0f);

                shootTimer = 0f;
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_SpiralOfExplodingHolyBombs(NPC npc, Player target, Vector2 arenaTopCenter, float lifeRatio, int localAttackTimer, int localAttackDuration, ref float drawState)
        {
            int shootRateMultipler = lifeRatio < 0.5f ? 3 : 4;
            int shootCycle = GetBPMTimeMultiplier(8);
            int cinderShootRate = GetBPMTimeMultiplier(shootRateMultipler) - 9;
            int shootRate = (int)Lerp(11f, 9f, 1f - lifeRatio);
            float spiralShootSpeed = Lerp(17f, 20f, 1f - lifeRatio);
            float bombExplosionRadius = Lerp(875f, 1240f, 1f - lifeRatio);
            ref float shootTimer = ref npc.Infernum().ExtraAI[0];
            ref float cycleTimer = ref npc.Infernum().ExtraAI[1];
            ref float hasDoneAttackEndEffects = ref npc.Infernum().ExtraAI[2];

            if (IsEnraged)
            {
                cinderShootRate -= 15;
                bombExplosionRadius += 195f;
            }

            // Stay in the cocoon once close enough to the top-center of the arena.
            bool attackIsAboutToEnd = localAttackTimer >= localAttackDuration * 0.96f;
            bool canAttack = !attackIsAboutToEnd && npc.WithinRange(arenaTopCenter, 96f);
            if (canAttack)
            {
                drawState = (int)ProvidenceFrameDrawingType.CocoonState;
                npc.velocity *= 0.85f;
                npc.Calamity().DR = 0.90f;
            }
            else
                npc.SimpleFlyMovement(npc.SafeDirectionTo(arenaTopCenter) * 26f, 0.3f);

            if (canAttack)
            {
                hasDoneAttackEndEffects = 0f;
                shootTimer++;
                cycleTimer++;

                if (Main.netMode != NetmodeID.MultiplayerClient && shootTimer >= shootRate)
                {
                    // Release a spiral of three bombs.
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 bombSpiralVelocity = -Vector2.UnitY.RotatedBy(TwoPi * cycleTimer / shootCycle + TwoPi * i / 3f) * Main.rand.NextFloat(0.5f, 1f) * spiralShootSpeed;
                        Utilities.NewProjectileBetter(npc.Center, -bombSpiralVelocity, ModContent.ProjectileType<HolyBomb>(), 0, 0f, -1, bombExplosionRadius);
                    }

                    shootTimer = 0f;
                }

                // Release cinders from the ceiling and side periodically.
                if (cycleTimer % cinderShootRate == cinderShootRate - 1f)
                {
                    bool targetIsCloseToCeiling = Distance(target.Center.Y, WorldSaveSystem.ProvidenceArena.Y * 16f + 700f) < 450f;
                    SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceBurnSound, target.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        // Ceiling cinders.
                        for (float dx = -1400f; dx < 1400f; dx += Main.rand.NextFloat(108f, 145f))
                        {
                            float ySpawnPosition = WorldSaveSystem.ProvidenceArena.Y * 16f + 48f;
                            Vector2 cinderVelocity = Vector2.UnitY * 4f;
                            if (targetIsCloseToCeiling)
                            {
                                ySpawnPosition = target.Center.Y + 1000f;
                                cinderVelocity.Y *= -1f;
                            }

                            Utilities.NewProjectileBetter(new Vector2(target.Center.X + dx, ySpawnPosition), cinderVelocity, ModContent.ProjectileType<HolyCinder>(), CinderDamage, 0f);
                        }

                        // Side cinders.
                        for (float dy = -1400f; dy < 1400f; dy += Main.rand.NextFloat(167f, 208f))
                        {
                            Vector2 cinderVelocity = Vector2.UnitX * 4f;
                            Utilities.NewProjectileBetter(new Vector2(target.Center.X - 1400, target.Center.Y + dy), cinderVelocity, ModContent.ProjectileType<HolyCinder>(), CinderDamage, 0f);
                        }
                    }
                }
            }

            // Make all bombs disappear when the attack is almost done.
            if (attackIsAboutToEnd)
            {
                if (hasDoneAttackEndEffects == 0f)
                {
                    Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 18f;
                    ScreenEffectSystem.SetBlurEffect(npc.Center, 0.9f, 32);
                    hasDoneAttackEndEffects = 1f;
                    npc.netUpdate = true;
                }
                Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<HolyBomb>());
                Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<HolyCinder>());
            }
        }

        public static void DoBehavior_EnterHolyMagicForm(NPC npc, Player target, int localAttackTimer, int localAttackDuration, ref float drawState)
        {
            float attackCompletion = localAttackTimer / (float)localAttackDuration;
            bool attackIsAlmostDone = attackCompletion >= 0.97f;
            ref float startingY = ref npc.Infernum().ExtraAI[0];
            ref float hasPerformedTeleport = ref npc.Infernum().ExtraAI[1];

            // Stay in the cocoon during this attack.
            drawState = (int)ProvidenceFrameDrawingType.CocoonState;

            npc.Calamity().DR = 0.90f;

            // Slow down.
            npc.velocity.X *= 0.85f;

            if (hasPerformedTeleport == 0f)
            {
                // Perform the teleport effect once ready.
                if (attackIsAlmostDone)
                {
                    npc.Size = new Vector2(48f, 108f);
                    npc.Center = target.Center - Vector2.UnitY * 400f;
                    npc.velocity = Vector2.Zero;

                    ReleaseSparkles(npc.Center, 100, 35f);

                    Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 15f;
                    ScreenEffectSystem.SetBlurEffect(npc.Center, 0.75f, 45);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<ProvidenceWave>(), 0, 0f);

                    hasPerformedTeleport = 1f;
                    npc.netUpdate = true;
                }
                else
                {
                    // Initialize the Y position keyframe.
                    if (localAttackTimer <= 8f)
                    {
                        startingY = npc.position.Y;
                        npc.netUpdate = true;
                    }
                    else
                        npc.position.Y = Lerp(startingY, WorldSaveSystem.ProvidenceArena.Top * 16f + 1650f, Pow(attackCompletion, 1.54f));
                }
            }

            // Play a rumble sound and give a tip.
            if (npc.Infernum().ExtraAI[2] == 0f)
            {
                HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.ProvidenceFinalPhaseTip");
                SoundEngine.PlaySound(InfernumSoundRegistry.LeviathanRumbleSound);
                npc.Infernum().ExtraAI[2] = 1f;
            }

            // Create screenshake effects.
            if (!attackIsAlmostDone)
                target.Infernum_Camera().CurrentScreenShakePower = Lerp(1f, 10f, Pow(attackCompletion, 2.1f));

            // Transform into the crystal at the end of the attack.
            npc.Opacity = Utils.GetLerpValue(0.95f, 0.7f, attackCompletion, true);
        }

        public static void DoBehavior_RockMagicRitual(NPC npc, Player target, int localAttackTimer, int localAttackDuration)
        {
            int ritualTime = HolyRitual.Lifetime;
            int rockCount = 13;
            int crossShootRate = GetBPMTimeMultiplier(IsEnraged ? 2 : 3);
            int rockCycleTime = 300;

            ref float hasPerformedRitual = ref npc.Infernum().ExtraAI[0];
            ref float runeStripOpacity = ref npc.Infernum().ExtraAI[1];
            ref float backglowTelegraphInterpolant = ref npc.Infernum().ExtraAI[2];
            ref float crossShootCounter = ref npc.Infernum().ExtraAI[3];
            ref float rockCounter = ref npc.Infernum().ExtraAI[4];

            // Make the rune fade in.
            runeStripOpacity = Utils.GetLerpValue(0f, 45f, localAttackTimer, true);

            // Create the ritual.
            if (hasPerformedRitual == 0f)
            {
                Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 12f;
                ScreenEffectSystem.SetFlashEffect(npc.Center, 3f, 36);

                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceBurnSound with
                {
                    Volume = 1.5f
                });

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<HolyRitual>(), 0, 0f);

                hasPerformedRitual = 1f;
            }

            // Loosely hover above the player after the ritual.
            Vector2 hoverDestination = target.Center - Vector2.UnitY * 450f;
            hoverDestination.X += Sin(TwoPi * localAttackTimer / 180f) * 180f;
            hoverDestination.Y += Sin(TwoPi * localAttackTimer / 105f + 0.75f) * 40f;
            if (localAttackTimer >= ritualTime)
            {
                npc.Center = Vector2.Lerp(npc.Center, hoverDestination, 0.11f);
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 12f, 0.18f);
                crossShootCounter++;
                rockCounter++;
                backglowTelegraphInterpolant = Utils.GetLerpValue(crossShootRate - 40f, crossShootRate, crossShootCounter, true);
            }
            else
                npc.velocity *= 0.8f;

            // Shoot crosses at the target in bursts.
            if (crossShootCounter >= crossShootRate)
            {
                Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 6f;
                ScreenEffectSystem.SetFlashEffect(npc.Center, 2f, 15);

                SoundEngine.PlaySound(InfernumSoundRegistry.SizzleSound, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        int crossCount = (int)Lerp(18f, 7f, i / 3f);

                        // Every second burst should vary in direction for more interesting spacing.
                        float shootOffsetAngle = Pi / crossCount;
                        float crossShootSpeed = Lerp(1.85f, 4.2f, i / 3f);
                        if (i % 2 == 0)
                            shootOffsetAngle = 0f;

                        if (IsEnraged)
                            crossShootSpeed *= 1.6f;

                        for (int j = 0; j < crossCount; j++)
                        {
                            Vector2 crossVelocity = (TwoPi * j / crossCount + shootOffsetAngle).ToRotationVector2() * crossShootSpeed;
                            Utilities.NewProjectileBetter(npc.Center + crossVelocity, crossVelocity, ModContent.ProjectileType<HolyCross>(), HolyCrossDamage, 0f);
                        }
                    }
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<ProvidenceWave>(), 0, 0f);
                    ReleaseSparkles(npc.Center, 35, 16f);
                }

                crossShootCounter = 0f;
                npc.netUpdate = true;
            }

            // Summon rocks that circle around the crystal after the ritual ends.
            int rockCycleTimer = (int)rockCounter % rockCycleTime;
            if (localAttackTimer >= ritualTime && rockCycleTimer == 1f)
            {
                SoundEngine.PlaySound(ProfanedGuardianDefender.RockShieldSpawnSound with
                {
                    Volume = 2f
                }, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float rockSpawnOffsetAngle = Main.rand.NextFloat(TwoPi);
                    for (int i = 0; i < rockCount; i++)
                    {
                        int rockType = Main.rand.Next(1, 7);
                        int rocc = NPC.NewNPC(npc.GetSource_FromThis(), 50, 50, ModContent.NPCType<ProfanedRocks>(), npc.whoAmI, 1080f, TwoPi * i / rockCount + rockSpawnOffsetAngle, rockType);
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, rocc);
                    }
                }
            }

            // Make all rocks fuck off in anticipation of the new ones when necessary.
            if ((rockCycleTimer == rockCycleTime - 120f || localAttackTimer == localAttackDuration - 30) && NPC.AnyNPCs(ModContent.NPCType<ProfanedRocks>()))
            {
                int rockID = ModContent.NPCType<ProfanedRocks>();
                bool rockWasChanged = false;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n.active && n.type == rockID && n.Infernum().ExtraAI[0] == 0f)
                    {
                        n.Infernum().ExtraAI[0] = 1f;
                        n.netUpdate = true;
                        rockWasChanged = true;
                    }
                }

                if (rockWasChanged)
                {
                    SoundEngine.PlaySound(ProfanedGuardianDefender.ShieldDeathSound with
                    {
                        Volume = 2f
                    }, target.Center);
                }
            }
        }

        public static void DoBehavior_ErraticMagicBursts(NPC npc, Player target, float lifeRatio, int localAttackTimer, int localAttackDuration)
        {
            int energyChargeupTime = 90;
            int hoverRedirectDelay = GetBPMTimeMultiplier(IsEnraged ? 2 : 3);
            int magicBurstCount = 11;
            float magicBurstSpeed = Lerp(11f, 17.5f, 1f - lifeRatio);
            float fieldExplosionRadius = Lerp(1000f, 1275f, 1f - lifeRatio);
            float attackCompletion = localAttackTimer / (float)localAttackDuration;
            bool attackIsAboutToEnd = attackCompletion >= 0.97f;
            Vector2 maxHoverOffset = new(350f, 125f);

            if (IsEnraged)
                magicBurstSpeed *= 1.5f;

            ref float hoverOffsetX = ref npc.Infernum().ExtraAI[0];
            ref float hoverOffsetY = ref npc.Infernum().ExtraAI[1];
            ref float hasPerformedExplosion = ref npc.Infernum().ExtraAI[2];
            ref float hoverRedirectCountdown = ref npc.Infernum().ExtraAI[3];

            // Charge up energy before attacking.
            if (localAttackTimer < energyChargeupTime)
            {
                float chargeUpInterpolant = Utils.GetLerpValue(0f, energyChargeupTime, localAttackTimer, true);
                for (int i = 0; i < 2; i++)
                {
                    if (Main.rand.NextFloat() > chargeUpInterpolant)
                        continue;

                    Color energyColor = Color.Lerp(Color.Pink, Color.Yellow, Main.rand.NextFloat(0.7f));
                    Vector2 energySpawnPosition = npc.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(116f, 172f);
                    Vector2 energyVelocity = (npc.Center - energySpawnPosition) * 0.034f;
                    SquishyLightParticle laserEnergy = new(energySpawnPosition, energyVelocity, 1.5f, energyColor, 36, 1f, 4f);
                    GeneralParticleHandler.SpawnParticle(laserEnergy);
                }
                npc.velocity = Vector2.Zero;
                return;
            }

            // Do the explosion on the first frame after charging up.
            if (hasPerformedExplosion == 0f)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceBurnSound, npc.Center);
                Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 12f;
                ScreenEffectSystem.SetBlurEffect(npc.Center, 0.6f, 32);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<ProvidenceWave>(), 0, 0f);

                // Decide the first hover offset.
                Vector2 hoverOffset = Main.rand.NextVector2Unit() * maxHoverOffset * Main.rand.NextFloat();
                hoverOffsetX = hoverOffset.X;
                hoverOffsetY = hoverOffset.Y;
                hasPerformedExplosion = 1f;
                hoverRedirectCountdown = hoverRedirectDelay;
                npc.netUpdate = true;
            }

            // Make the hover countdown go down. Once it's finished and Providence is done moving, release magic spirals and energy fields and prepare for the next redirect.
            // This part also handles movement.
            if (hoverRedirectCountdown >= 1f)
            {
                // Hover above the target. At the very beginning Providence will jitter in place, similar to Mettaton.
                bool jitterInPlace = hoverRedirectCountdown >= hoverRedirectDelay - 35f;

                if (jitterInPlace)
                {
                    npc.velocity *= 0.85f;
                    npc.Center += Main.rand.NextVector2Circular(3f, 3f);
                }
                else
                {
                    float movementSpeedInterpolant = Utils.GetLerpValue(12f, 50f, hoverRedirectCountdown, true);
                    Vector2 hoverDestination = target.Center + new Vector2(hoverOffsetX, hoverOffsetY) - Vector2.UnitY * 320f;
                    npc.Center = Vector2.Lerp(npc.Center, hoverDestination, movementSpeedInterpolant * 0.12f);
                    if (movementSpeedInterpolant > 0f)
                        npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * movementSpeedInterpolant * 15f, 0.2f);
                    else
                        npc.velocity *= 0.9f;
                }

                hoverRedirectCountdown--;

                if (hoverRedirectCountdown <= 0f && !attackIsAboutToEnd)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceBurnSound, target.Center);

                    // Release the field and magic bursts.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float burstShootOffsetAngle = Main.rand.NextFloat(TwoPi);
                        for (int i = 0; i < magicBurstCount; i++)
                        {
                            Vector2 magicBurstVelocity = (TwoPi * i / magicBurstCount + burstShootOffsetAngle).ToRotationVector2() * magicBurstSpeed;
                            Utilities.NewProjectileBetter(npc.Center, magicBurstVelocity, ModContent.ProjectileType<MagicSpiralCrystalShot>(), CrystalMagicDamage, 0f, -1, 0f, 1f);
                            Utilities.NewProjectileBetter(npc.Center, magicBurstVelocity, ModContent.ProjectileType<MagicSpiralCrystalShot>(), CrystalMagicDamage, 0f, -1, 0f, -1f);
                        }

                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(bomb =>
                        {
                            bomb.timeLeft = 360;
                        });
                        Utilities.NewProjectileBetter(npc.Center, Vector2.UnitY * 0.001f, ModContent.ProjectileType<HolyBomb>(), 0, 0f, -1, fieldExplosionRadius);
                    }

                    Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 10f;
                    ScreenEffectSystem.SetBlurEffect(npc.Center, 0.42f, 12);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<ProvidenceWave>(), 0, 0f);

                    Vector2 hoverOffset = Main.rand.NextVector2Unit() * maxHoverOffset * Main.rand.NextFloat();
                    hoverOffsetX = hoverOffset.X;
                    hoverOffsetY = hoverOffset.Y;
                    hoverRedirectCountdown = hoverRedirectDelay;
                    npc.netUpdate = true;
                }
            }

            if (attackIsAboutToEnd)
                Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<HolyBomb>());
        }

        public static void DoBehavior_DogmaLaserBursts(NPC npc, Player target, float lifeRatio, int attackTimer, int localAttackTimer, int localAttackDuration)
        {
            int energyChargeupTime = 30;
            int laserCount = (int)Lerp(14f, 21f, 1f - lifeRatio);
            float telegraphMaxAngularVelocity = ToRadians(1.2f);
            bool attackIsAboutToEnd = localAttackTimer >= localAttackDuration - 90;
            ref float vfxDelayCountdown = ref npc.Infernum().ExtraAI[0];
            ref float countdownUntilNextLaser = ref npc.Infernum().ExtraAI[1];

            // Charge up energy before attacking.
            if (localAttackTimer < energyChargeupTime)
            {
                float chargeUpInterpolant = Utils.GetLerpValue(0f, energyChargeupTime, localAttackTimer, true);
                for (int i = 0; i < 2; i++)
                {
                    if (Main.rand.NextFloat() > chargeUpInterpolant)
                        continue;

                    Color energyColor = Color.Lerp(Color.Pink, Color.Yellow, Main.rand.NextFloat(0.7f));
                    Vector2 energySpawnPosition = npc.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(116f, 166f);
                    Vector2 energyVelocity = (npc.Center - energySpawnPosition) * 0.032f;
                    SquishyLightParticle laserEnergy = new(energySpawnPosition, energyVelocity, 1.5f, energyColor, 32, 1f, 4f);
                    GeneralParticleHandler.SpawnParticle(laserEnergy);
                }
                npc.velocity = Vector2.Zero;
                countdownUntilNextLaser = 96f;
                return;
            }

            // Cast the laser telegraphs.
            int telegraphTime = IsEnraged ? 48 : 55;
            int laserShootTime = 35;
            bool bellIsPlaying = SyncAttacksWithMusic && ProvidenceTrackedMusic.Bells.Any(b => attackTimer >= b.StartInFrames && attackTimer < b.EndInFrames);
            bool shootLaser = bellIsPlaying;
            if (bellIsPlaying)
            {
                int bellIndex = ProvidenceTrackedMusic.Bells.FindIndex(b => attackTimer >= b.StartInFrames && attackTimer < b.EndInFrames);
                if (bellIndex < ProvidenceTrackedMusic.Bells.Count - 1)
                    telegraphTime = ProvidenceTrackedMusic.Bells[bellIndex + 1].StartInFrames - ProvidenceTrackedMusic.Bells[bellIndex].StartInFrames;
                if (bellIndex < ProvidenceTrackedMusic.Bells.Count - 2)
                    laserShootTime = ProvidenceTrackedMusic.Bells[bellIndex + 2].StartInFrames - ProvidenceTrackedMusic.Bells[bellIndex + 1].StartInFrames;
            }

            // Force a laser to be shot if there hasn't been one in a while. This is done to prevent awkward transition points in the song without bells from messing with fight flow.
            if (countdownUntilNextLaser > 0f)
                countdownUntilNextLaser--;

            if (countdownUntilNextLaser <= 0f)
                shootLaser = true;

            // Release slow fireballs from the lava below.
            if (Main.netMode != NetmodeID.MultiplayerClient && localAttackTimer % 18 == 17)
            {
                Vector2 fireballSpawnPosition = target.Center + new Vector2(Main.rand.NextFloatDirection() * 400f, 800f);
                Utilities.NewProjectileBetter(fireballSpawnPosition, -Vector2.UnitY * 4f, ModContent.ProjectileType<HolyBasicFireball>(), BasicFireballDamage, 0f);
            }

            if (shootLaser && !attackIsAboutToEnd && !Utilities.AnyProjectiles(ModContent.ProjectileType<HolyMagicLaserbeam>()))
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceBurnSound, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < laserCount; i++)
                    {
                        float angularVelocity = Main.rand.NextFloat(0.65f, 1f) * Main.rand.NextFromList(-1f, 1f) * telegraphMaxAngularVelocity;
                        Vector2 laserDirection = (TwoPi * i / laserCount + Main.rand.NextFloatDirection() * 0.16f).ToRotationVector2();

                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(laser =>
                        {
                            laser.ModProjectile<HolyMagicLaserbeam>().LaserTelegraphTime = telegraphTime;
                            laser.ModProjectile<HolyMagicLaserbeam>().LaserShootTime = laserShootTime;
                        });
                        Utilities.NewProjectileBetter(npc.Center, laserDirection, ModContent.ProjectileType<HolyMagicLaserbeam>(), MagicLaserbeamDamage, 0f, -1, angularVelocity);
                    }
                    countdownUntilNextLaser = 96f;
                    vfxDelayCountdown = telegraphTime;
                    npc.netUpdate = true;
                }
            }

            // Perform intensity effects and an explosion sound to go with the firing of the lasers.
            if (vfxDelayCountdown > 0f)
            {
                vfxDelayCountdown--;
                if (vfxDelayCountdown <= 0f && CalamityConfig.Instance.Screenshake && !attackIsAboutToEnd && Utilities.AnyProjectiles(ModContent.ProjectileType<HolyMagicLaserbeam>()))
                {
                    Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 15f;
                    ScreenEffectSystem.SetFlashEffect(npc.Center, 3f, 30);
                    SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceLavaEruptionSound with
                    {
                        Volume = 0.55f,
                        Pitch = -0.3f
                    }, target.Center);
                }
            }
        }

        public static void DoBehavior_EnterLightForm(NPC npc, Player target, int localAttackTimer, ref float drawState, ref float burnIntensity, ref float rockReformOffset)
        {
            int rockReformDelay = 60;
            int rockReformTime = 75;
            int burnDelay = 45;

            // Hover above the target.
            float moveSpeedInterpolant = Utils.GetLerpValue(60f, 150f, localAttackTimer, true);
            Vector2 hoverDestination = target.Center - Vector2.UnitY * 360f;
            npc.velocity *= 0.9f;
            if (moveSpeedInterpolant > 0f)
                npc.Center = Vector2.Lerp(npc.Center.MoveTowards(hoverDestination, moveSpeedInterpolant * 11f), hoverDestination, moveSpeedInterpolant * 0.123f);

            if (localAttackTimer <= 5f && npc.width != 600f)
            {
                npc.position = npc.Center;
                npc.Size = new Vector2(600f, 450f);
                npc.position -= npc.Size * 0.5f;
            }

            // Stay in the cocoon during this attack by default.
            drawState = (int)ProvidenceFrameDrawingType.CocoonState;

            npc.Calamity().DR = 0.90f;

            // Play a rock reform sound once the shell is about to come back.
            if (localAttackTimer == rockReformDelay)
                SoundEngine.PlaySound(ProfanedGuardianDefender.RockShieldSpawnSound);

            // Make Provi's rock shell reappear.
            rockReformOffset = 10000000f;
            if (localAttackTimer >= rockReformDelay)
            {
                float rockReformInterpolant = Pow(Utils.GetLerpValue(rockReformDelay, rockReformDelay + rockReformTime - 30f, localAttackTimer, true), 0.05f);
                rockReformOffset = (1f - rockReformInterpolant) * 6600f + 0.01f;
            }

            // Create violent effects when the rock is sufficiently reformed.
            // This is half done for aesthetic purposes, half done to obscure the fact that Providence suddenly jumps back to her fire wings animation in a way that looks jank lol
            if (localAttackTimer == rockReformDelay + rockReformTime)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceBurnSound);
                Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 18f;
                ScreenEffectSystem.SetFlashEffect(npc.Center, 5f, 45);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<ProvidenceWave>(), 0, 0f);
            }

            // Begin burning once the shell is back.
            if (localAttackTimer >= rockReformDelay + rockReformTime)
            {
                rockReformOffset = 0f;
                burnIntensity = Utils.GetLerpValue(rockReformDelay + rockReformTime, rockReformDelay + rockReformTime + burnDelay, localAttackTimer, true) * 0.87f;
                drawState = (int)ProvidenceFrameDrawingType.WingFlapping;
                npc.Opacity = 1f;
            }
            else
                burnIntensity = 0f;
        }

        public static void DoBehavior_FinalPhaseRadianceBursts(NPC npc, Player target, Vector2 arenaTopCenter, int localAttackTimer, int localAttackDuration, ref float lavaHeight, ref float hasCompletedCycle)
        {
            int shootDelay = 75;
            int startingBombShootRate = 18;
            int endingBombShootRate = 10;
            int startingLaserShootRate = 180;
            int endingLaserShootRate = 120;
            float holyBombRadius = 600f;
            float attackCompletion = localAttackTimer / (float)localAttackDuration;
            bool attackIsAboutToEnd = attackCompletion >= 0.85f;
            ref float bombShootTimer = ref npc.Infernum().ExtraAI[0];
            ref float laserShootTimer = ref npc.Infernum().ExtraAI[1];
            ref float vfxDelayCountdown = ref npc.Infernum().ExtraAI[2];
            ref float recordingStarted = ref npc.Infernum().ExtraAI[3];

            int bombShootRate = (int)Lerp(startingBombShootRate, endingBombShootRate, attackCompletion);
            int laserShootRate = (int)Lerp(startingLaserShootRate, endingLaserShootRate, attackCompletion);
            if (IsEnraged)
                laserShootRate -= 40;

            // Mark a full phase 2 cycle as complete once this attack nears its end.
            // This makes her defense during the cocoon phases weaker, so that you don't need to wait during the transition periods.
            if (attackIsAboutToEnd && hasCompletedCycle == 0f)
            {
                hasCompletedCycle = 1f;
                npc.netUpdate = true;
            }

            // Start recording.
            if (attackCompletion >= 0.5f && recordingStarted == 0f)
            {
                recordingStarted = 1f;
                CreditManager.StartRecordingFootageForCredits(ScreenCapturer.RecordingBoss.Provi);
                npc.netUpdate = true;
            }

            // Make the lava rise upward.
            lavaHeight = Lerp(DefaultLavaHeight, HighestLavaHeight, Utils.GetLerpValue(0f, 0.88f, attackCompletion, true));

            // Move towards the hover destination.
            npc.velocity *= 0.92f;
            Vector2 hoverDestination = arenaTopCenter + Vector2.UnitY * (2300f - lavaHeight);
            npc.Center = Vector2.Lerp(npc.Center, npc.Center.MoveTowards(hoverDestination, 10f), 0.025f);

            // Begin firing bursts of holy bombs once the shoot delay has elapsed.
            if (localAttackTimer >= shootDelay && !attackIsAboutToEnd)
            {
                if (!Utilities.AnyProjectiles(ModContent.ProjectileType<HolyMagicLaserbeam>()))
                    bombShootTimer++;

                laserShootTimer++;

                if (bombShootTimer >= bombShootRate)
                {
                    Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 4f;

                    // Release a holy bomb and a bunch of lava blobs.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 holyBombShootVelocity = -Vector2.UnitY.RotatedByRandom(0.13f) * 18f;
                        Vector2 holyBombSpawnPosition = new(target.Center.X + Main.rand.NextFloatDirection() * 510f + target.velocity.X * 60f, arenaTopCenter.Y + 2200f - lavaHeight);

                        Utilities.NewProjectileBetter(holyBombSpawnPosition, holyBombShootVelocity, ModContent.ProjectileType<HolyBomb>(), 0, 0f, -1, holyBombRadius);
                        for (int i = 0; i < 12; i++)
                        {
                            int lavaLifetime = Main.rand.Next(120, 167);
                            float blobSize = Lerp(11f, 30f, Pow(Main.rand.NextFloat(), 1.85f));
                            if (Main.rand.NextBool(6))
                                blobSize *= 1.36f;
                            Vector2 lavaVelocity = -Vector2.UnitY.RotatedByRandom(0.5f) * Main.rand.NextFloat(4f, 5f);
                            Utilities.NewProjectileBetter(holyBombSpawnPosition + Main.rand.NextVector2Circular(30f, 30f), lavaVelocity, ModContent.ProjectileType<ProfanedLavaBlob>(), SmallLavaBlobDamage, 0f, -1, lavaLifetime, blobSize);
                        }

                        bombShootTimer = 0f;

                        // The frequency of these projectile firing conditions may be enough to trigger the anti NPC packet spam system that Terraria uses.
                        // Consequently, that system is ignored for this specific sync.
                        npc.netSpam = 0;
                        npc.netUpdate = true;
                    }
                }

                // Release laserbeams.
                if (laserShootTimer >= laserShootRate)
                {
                    int telegraphTime = 45;
                    int laserShootTime = 20;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            float angularVelocity = Main.rand.NextFloat(0.4f, 1f) * Main.rand.NextFromList(-1f, 1f) * ToRadians(0.33f);
                            Vector2 laserDirection = npc.SafeDirectionTo(target.Center);

                            ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(laser =>
                            {
                                laser.ModProjectile<HolyMagicLaserbeam>().LaserTelegraphTime = telegraphTime;
                                laser.ModProjectile<HolyMagicLaserbeam>().LaserShootTime = laserShootTime;
                            });
                            Utilities.NewProjectileBetter(npc.Center, laserDirection, ModContent.ProjectileType<HolyMagicLaserbeam>(), MagicLaserbeamDamage, 0f, -1, angularVelocity);
                        }
                    }

                    laserShootTimer = 0f;
                    vfxDelayCountdown = telegraphTime;
                    npc.netUpdate = true;
                }

                // Perform intensity effects and an explosion sound to go with the firing of the lasers.
                if (vfxDelayCountdown > 0f)
                {
                    vfxDelayCountdown--;
                    if (vfxDelayCountdown <= 0f && Utilities.AnyProjectiles(ModContent.ProjectileType<HolyMagicLaserbeam>()))
                    {
                        if (CalamityConfig.Instance.Screenshake)
                        {
                            Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 15f;
                            ScreenEffectSystem.SetFlashEffect(npc.Center, 3f, 30);
                        }
                        Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<HolyBomb>());
                        SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceLavaEruptionSound with
                        {
                            Volume = 0.65f,
                            Pitch = -0.3f
                        }, target.Center);
                    }
                }
            }
        }

        public static void DoVanillaFlightMovement(NPC npc, Player target, bool stayAwayFromTarget, ref float flightPath, float speedFactor = 1f)
        {
            // Reset the flight path direction.
            if (flightPath == 0f)
            {
                flightPath = (npc.Center.X < target.Center.X).ToDirectionInt();
                npc.netUpdate = true;
            }

            float verticalDistanceFromTarget = target.position.Y - npc.Bottom.Y;
            float horizontalDistanceFromTarget = Distance(target.Center.X, npc.Center.X);
            float horizontalDistanceDirChangeThreshold = 350f;

            // Increase distance from target as necessary.
            if (stayAwayFromTarget)
                horizontalDistanceDirChangeThreshold += 200f;

            // Change X movement path if far enough away from target.
            if (npc.Center.X < target.Center.X && flightPath < 0 && horizontalDistanceFromTarget > horizontalDistanceDirChangeThreshold)
                flightPath = 0f;
            if (npc.Center.X > target.Center.X && flightPath > 0 && horizontalDistanceFromTarget > horizontalDistanceDirChangeThreshold)
                flightPath = 0f;

            // Velocity and acceleration.
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float accelerationBoost = (1f - lifeRatio) * 0.4f;
            float speedBoost = (1f - lifeRatio) * 7.5f;
            float acceleration = (accelerationBoost + 1.15f) * speedFactor;
            float maxFlySpeed = (speedBoost + 17f) * speedFactor;

            // Fly faster at night.
            if (IsEnraged)
            {
                maxFlySpeed *= 1.35f;
                acceleration *= 1.35f;
            }

            // Fly faster at night.
            if (IsEnraged)
            {
                maxFlySpeed *= 1.35f;
                acceleration *= 1.35f;
            }

            // Don't stray too far from the target.
            npc.velocity.X = Clamp(npc.velocity.X + flightPath * acceleration, -maxFlySpeed, maxFlySpeed);
            if (verticalDistanceFromTarget < 50f)
                npc.velocity.Y -= 0.2f;
            if (verticalDistanceFromTarget > 120f)
                npc.velocity.Y += 0.4f;

            npc.velocity.Y = Clamp(npc.velocity.Y, -6f, 6f);
        }

        // A value of two would be double beat, a value of four would be quadruple beat, etc.
        // This works in both phases since Unholy Ambush and Unholy Insurgency have the same BPM.
        public static int GetBPMTimeMultiplier(float beatFactor) =>
            (int)Math.Round(3600f / ProvidenceTrackedMusic.BeatsPerMinuteStatic * beatFactor);

        public static void ReleaseSparkles(Vector2 sparkleSpawnPosition, int sparkleCount, float maxSpraySpeed)
        {
            // Prevent projectiles from spawning client-side.
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < sparkleCount; i++)
                Utilities.NewProjectileBetter(sparkleSpawnPosition, Main.rand.NextVector2Circular(maxSpraySpeed, maxSpraySpeed), ModContent.ProjectileType<YharonMajesticSparkle>(), 0, 0f);
        }

        public static void ClearEntities()
        {
            // Delete all Guardians and rocks.
            List<int> guardianIDs = new()
            {
                ModContent.NPCType<ProvSpawnHealer>(),
                ModContent.NPCType<ProvSpawnOffense>(),
                ModContent.NPCType<ProfanedRocks>(),
            };
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n.active && guardianIDs.Contains(n.type))
                    n.active = false;
            }

            for (int i = 0; i < 3; i++)
            {
                Utilities.DeleteAllProjectiles(false,
                    ModContent.ProjectileType<AcceleratingCrystalShard>(),
                    ModContent.ProjectileType<AcceleratingMagicProfanedRock>(),
                    ModContent.ProjectileType<CleansingFireball>(),
                    ModContent.ProjectileType<CommanderSpear2>(),
                    ModContent.ProjectileType<FallingCrystalShard>(),
                    ModContent.ProjectileType<HolyBasicFireball>(),
                    ModContent.ProjectileType<HolyBomb>(),
                    ModContent.ProjectileType<HolyCinder>(),
                    ModContent.ProjectileType<HolyCross>(),
                    ModContent.ProjectileType<HolyCrystalSpike>(),
                    ModContent.ProjectileType<HolyMagicLaserbeam>(),
                    ModContent.ProjectileType<HolyRitual>(),
                    ModContent.ProjectileType<HolySpear>(),
                    ModContent.ProjectileType<HolySpearFirePillar>(),
                    ModContent.ProjectileType<ProfanedLavaBlob>(),
                    ModContent.ProjectileType<HolySunExplosion>());
            }
        }
        #endregion

        #region Drawing

        public override void FindFrame(NPC npc, int frameHeight)
        {
            ref float drawState = ref npc.localAI[0];
            bool useDefenseFrames = npc.localAI[1] == 1f;
            ref float frameUsed = ref npc.localAI[2];

            if (drawState == (int)ProvidenceFrameDrawingType.CocoonState)
            {
                if (!useDefenseFrames)
                {
                    npc.frameCounter += 1.0;
                    if (npc.frameCounter > 8.0)
                    {
                        npc.frame.Y += frameHeight;
                        npc.frameCounter = 0.0;
                    }
                    if (npc.frame.Y >= frameHeight * 3)
                    {
                        npc.frame.Y = 0;
                        npc.localAI[1] = 1f;
                    }
                }
                else
                {
                    npc.frameCounter += 1.0;
                    if (npc.frameCounter > 8.0)
                    {
                        npc.frame.Y += frameHeight;
                        npc.frameCounter = 0.0;
                    }
                    if (npc.frame.Y >= frameHeight * 2)
                        npc.frame.Y = frameHeight * 2;
                }
            }
            else
            {
                if (useDefenseFrames)
                    npc.localAI[1] = 0f;

                npc.frameCounter += npc.Infernum().ExtraAI[DeathEffectTimerIndex] > 0f ? 0.6 : 1.0;
                if (npc.frameCounter > 5.0)
                {
                    npc.frameCounter = 0.0;
                    npc.frame.Y += frameHeight;
                }
                if (npc.frame.Y >= frameHeight * 3)
                {
                    npc.frame.Y = 0;
                    frameUsed++;
                }
                if (frameUsed > 3)
                    frameUsed = 0;
            }
        }

        // Visceral rage. Debugging doesn't work for unexplained reasons due to local functions unless this external method is used.
        public static void DrawProvidenceWings(NPC npc, Texture2D wingTexture, float wingVibrance, Vector2 baseDrawPosition, Rectangle frame, Vector2 drawOrigin, SpriteEffects spriteEffects)
        {
            Color deathEffectColor = new(6, 6, 6, 0);
            float deathEffectInterpolant = npc.localAI[3];

            if (!IsEnraged)
            {
                Color c = Color.Lerp(new Color(255, 120, 0, 128), deathEffectColor, deathEffectInterpolant);
                Main.spriteBatch.Draw(wingTexture, baseDrawPosition, frame, c * npc.Opacity, npc.rotation, drawOrigin, npc.scale, spriteEffects, 0f);
            }
            else
            {
                Color nightWingColor = Color.Lerp(new Color(0, 204, 191, 0), deathEffectColor, deathEffectInterpolant) * npc.Opacity;
                Main.spriteBatch.Draw(wingTexture, baseDrawPosition, frame, nightWingColor, npc.rotation, drawOrigin, npc.scale, spriteEffects, 0f);
                for (int i = 0; i < 6; i++)
                {
                    Vector2 wingOffset = (TwoPi * i / 6f + Main.GlobalTimeWrappedHourly * 0.72f).ToRotationVector2() * npc.Opacity * wingVibrance * 4f;
                    Main.spriteBatch.Draw(wingTexture, baseDrawPosition + wingOffset, frame, nightWingColor * 0.8f, npc.rotation, drawOrigin, npc.scale, spriteEffects, 0f);
                }
            }
        }

        public static float RuneHeightFunction(float _) => 26f;

        public static Color RuneColorFunction(NPC n, float _) => Color.Lerp(Color.Yellow, Color.Wheat, 0.8f) * (1f - n.Opacity) * n.Infernum().ExtraAI[1];

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor) => false;
        #endregion

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Mods.InfernumMode.PetDialog.ProvidenceTip1";
            yield return n => "Mods.InfernumMode.PetDialog.ProvidenceTip2";
            yield return n =>
            {
                if (Main.dayTime && Main.time >= Main.dayLength - 3600D)
                    return "Mods.InfernumMode.PetDialog.ProvidenceDuskWarningTip";
                if (!Main.dayTime && Main.time >= Main.nightLength - 3600D)
                    return "Mods.InfernumMode.PetDialog.ProvidenceDawnWarningTip";
                return string.Empty;
            };
        }
        #endregion Tips
    }
}
