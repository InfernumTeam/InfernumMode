using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;
using InfernumMode.Core.OverridingSystem;
using InfernumMode.Content.Buffs;
using InfernumMode.Core.GlobalInstances.Players;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.TrackedMusic;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.Sounds;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Yharon;
using ProvidenceBoss = CalamityMod.NPCs.Providence.Providence;
using System.Linq;
using InfernumMode.Common.Graphics;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Providence
{
    public class ProvidenceBehaviorOverride : NPCBehaviorOverride
    {
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

            public int StartingTime => Section.StartInFrames;

            public int EndingTime => Section.EndInFrames;

            public ProvidenceAttackSection(SongSection section, ProvidenceAttackType attackType)
            {
                Section = section;
                AttackToUse = attackType;
            }
        }

        public override int NPCOverrideType => ModContent.NPCType<ProvidenceBoss>();

        public const float Phase2LifeRatio = 0.6f;

        #region Enumerations
        public enum ProvidenceAttackType
        {
            SpawnEffect,

            EnterFireFormBulletHell,
            EnvironmentalFireEffects,
            ExplodingSpears,
            CleansingFireballBombardment,
            CooldownState,
            SpiralOfExplodingHolyBombs,

            EnterHolyMagicForm,
            RockMagicRitual,
            ErraticMagicBursts,
            DogmaLaserBursts, // Blast TBOI attack idea real???

            EnterLightForm,
            LavaGeysersWithLightShards,
            FinalPhaseRadianceBursts,

            RestartCycle
        }

        public enum ProvidenceFrameDrawingType
        {
            WingFlapping,
            CocoonState
        }
        #endregion

        #region AI

        public const int AuraTime = 300;

        public const int CocoonDefense = 620;

        public const int DeathEffectTimerIndex = 5;

        public const int WasSummonedAtNightFlagIndex = 6;

        public const int LavaHeightIndex = 7;

        public static readonly Color[] NightPalette = new Color[] { new Color(119, 232, 194), new Color(117, 201, 229), new Color(117, 93, 229) };

        public static bool IsEnraged => !Main.dayTime || BossRushEvent.BossRushActive;

        public static List<ProvidenceAttackSection> AttackStates => new()
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
            new(new(BaseTrackedMusic.TimeFormat(1, 11, 0), BaseTrackedMusic.TimeFormat(1, 16, 0)), ProvidenceAttackType.EnterHolyMagicForm),
            new(new(BaseTrackedMusic.TimeFormat(1, 16, 0), BaseTrackedMusic.TimeFormat(1, 28, 0)), ProvidenceAttackType.RockMagicRitual),
            new(new(BaseTrackedMusic.TimeFormat(1, 28, 0), BaseTrackedMusic.TimeFormat(1, 41, 0)), ProvidenceAttackType.ErraticMagicBursts),
            new(new(BaseTrackedMusic.TimeFormat(1, 41, 0), BaseTrackedMusic.TimeFormat(1, 56, 0)), ProvidenceAttackType.DogmaLaserBursts),

            // Light form.
            new(new(BaseTrackedMusic.TimeFormat(1, 56, 0), BaseTrackedMusic.TimeFormat(1, 58, 0)), ProvidenceAttackType.EnterLightForm),
            new(new(BaseTrackedMusic.TimeFormat(1, 58, 0), BaseTrackedMusic.TimeFormat(2, 10, 0)), ProvidenceAttackType.LavaGeysersWithLightShards),
            new(new(BaseTrackedMusic.TimeFormat(2, 10, 0), BaseTrackedMusic.TimeFormat(2, 21, 0)), ProvidenceAttackType.FinalPhaseRadianceBursts),

            // Cycle restart.
            new(new(BaseTrackedMusic.TimeFormat(2, 21, 0), BaseTrackedMusic.TimeFormat(2, 23, 0)), ProvidenceAttackType.RestartCycle),
        };

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio
        };

        public static void GetLocalAttackInformation(NPC npc, out ProvidenceAttackType currentAttack, out int localAttackTimer, out int localAttackDuration)
        {
            bool syncsWithMusic = Main.netMode == NetmodeID.SinglePlayer && InfernumMode.CalMusicModIsActive && Main.musicVolume > 0f;
            ref float attackTimer = ref npc.ai[1];
            if (syncsWithMusic)
                attackTimer = (int)(TrackedMusicManager.SongElapsedTime.TotalMilliseconds * 0.06f);

            // Increment the attack timer manually if it shouldn't sync with the music.
            else
            {
                attackTimer++;
                if (attackTimer >= AttackStates.Last().EndingTime)
                    attackTimer = 0f;
            }

            // Split the attack timer into sections, and then calculate the local attack timer and current attack based on that.
            // attackTimer isn't used in the queries here since those cannot take ref local variables.
            var attackSection = AttackStates.First(a => npc.ai[1] >= a.StartingTime && npc.ai[1] < a.EndingTime);
            currentAttack = attackSection.AttackToUse;
            localAttackTimer = (int)(attackTimer - attackSection.StartingTime);
            localAttackDuration = attackSection.EndingTime - attackSection.StartingTime;
        }

        public override bool PreAI(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float attackStateTimer = ref npc.ai[2];
            ref float drawState = ref npc.localAI[0];
            ref float burnIntensity = ref npc.localAI[3];
            ref float deathEffectTimer = ref npc.Infernum().ExtraAI[DeathEffectTimerIndex];
            ref float wasSummonedAtNight = ref npc.Infernum().ExtraAI[WasSummonedAtNightFlagIndex];
            ref float lavaHeight = ref npc.Infernum().ExtraAI[LavaHeightIndex];

            bool shouldDespawnAtNight = wasSummonedAtNight == 0f && IsEnraged && attackType != (int)ProvidenceAttackType.SpawnEffect;
            bool shouldDespawnAtDay = wasSummonedAtNight == 1f && !IsEnraged && attackType != (int)ProvidenceAttackType.SpawnEffect;
            bool shouldDespawnBecauseOfTime = shouldDespawnAtNight || shouldDespawnAtDay;

            Vector2 crystalCenter = npc.Center + new Vector2(8f, 56f);

            // Define arena variables.
            Vector2 arenaTopLeft = WorldSaveSystem.ProvidenceArena.TopLeft() * 16f + new Vector2(68f, 32f);
            Vector2 arenaBottomRight = WorldSaveSystem.ProvidenceArena.BottomRight() * 16f + new Vector2(8f, 52f);
            Vector2 arenaCenter = WorldSaveSystem.ProvidenceArena.Center() * 16f + Vector2.One * 8f;
            Rectangle arenaArea = new((int)arenaTopLeft.X, (int)arenaTopLeft.Y, (int)(arenaBottomRight.X - arenaTopLeft.X), (int)(arenaBottomRight.Y - arenaTopLeft.Y));

            // Reset various things every frame. They can be changed later as needed.
            npc.width = 600;
            npc.height = 450;
            npc.defense = 50;
            npc.dontTakeDamage = false;
            npc.Calamity().DR = BossRushEvent.BossRushActive ? 0.65f : 0.35f;
            npc.Infernum().Arena = arenaArea;
            if (drawState == (int)ProvidenceFrameDrawingType.CocoonState)
                npc.defense = CocoonDefense;

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

                player.wingTime = player.wingTimeMax;
                player.AddBuff(ModContent.BuffType<ElysianGrace>(), 10);
            }

            // For a few frames Providence will play Boss 1 due to the custom music system. Don't allow this.
            if (Main.netMode != NetmodeID.Server)
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

            // Use the screen saturation effect.
            npc.Infernum().ShouldUseSaturationBlur = true;

            // Enable the distortion filter if it isnt active and the player's config permits it.
            if (Main.netMode != NetmodeID.Server && !InfernumEffectsRegistry.ScreenDistortionScreenShader.IsActive() && Main.UseHeatDistortion)
            {
                Filters.Scene.Activate("InfernumMode:ScreenDistortion", Main.LocalPlayer.Center);
                InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().UseImage("Images/Extra_193");
                InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().Shader.Parameters["distortionAmount"].SetValue(4);
                InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().Shader.Parameters["wiggleSpeed"].SetValue(2);
            }

            // Set the global NPC index to this NPC. Used as a means of lowering the need for loops.
            CalamityGlobalNPC.holyBoss = npc.whoAmI;

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

            // Death effects.
            if (lifeRatio < 0.04f)
            {
                if (deathEffectTimer == 1)
                {
                    AchievementPlayer.ProviDefeated = true;

                    if (wasSummonedAtNight == 1)
                        AchievementPlayer.NightProviDefeated = true;
                }
                npc.Opacity = 1f;
                npc.rotation = npc.rotation.AngleTowards(0f, 0.02f);
                if (deathEffectTimer == 1f && !Main.dedServ)
                    SoundEngine.PlaySound(SoundID.DD2_DefeatScene with { Volume = 1.65f }, target.Center);

                // Delete all fire blenders.
                Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<HolyFireBeam>());

                deathEffectTimer++;

                // Delete remaining projectiles with a shockwave.
                if (deathEffectTimer == 96)
                {
                    int[] typesToDelete = new int[]
                    {
                        ModContent.ProjectileType<AcceleratingCrystalShard>(),
                        ModContent.ProjectileType<FallingCrystalShard>(),
                        ModContent.ProjectileType<HolySunExplosion>(),
                        ModContent.ProjectileType<ProfanedSpear>(),
                        ModContent.ProjectileType<HolyBlast>(),
                    };
                    Utilities.DeleteAllProjectiles(false, typesToDelete);
                }

                burnIntensity = Utils.GetLerpValue(0f, 45f, deathEffectTimer, true);
                npc.life = (int)MathHelper.Lerp(npc.lifeMax * 0.04f - 1f, 1f, Utils.GetLerpValue(0f, 435f, deathEffectTimer, true));
                npc.dontTakeDamage = true;
                npc.velocity *= 0.9f;

                // Move towards the player if inside of walls, to ensure that the loot is obtainable.
                if (Collision.SolidCollision(npc.TopLeft, npc.width, npc.height))
                    npc.Center = npc.Center.MoveTowards(target.Center, 10f);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int shootRate = (int)MathHelper.Lerp(12f, 5f, Utils.GetLerpValue(0f, 250f, deathEffectTimer, true));
                    if (deathEffectTimer % shootRate == shootRate - 1 || deathEffectTimer == 92f)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            int shootType = ModContent.ProjectileType<SwirlingFire>();
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
                                    ReleaseSparkles(npc.Center, 6, 18f);
                                    SoundEngine.PlaySound(CommonCalamitySounds.FlareSound, target.Center);
                                    SoundEngine.PlaySound(HolyBlast.ImpactSound, target.Center);
                                }
                            }

                            Vector2 shootVelocity = Main.rand.NextVector2CircularEdge(7f, 7f) * Main.rand.NextFloat(0.7f, 1.3f);
                            if (Vector2.Dot(shootVelocity.SafeNormalize(Vector2.Zero), npc.SafeDirectionTo(target.Center)) < 0.5f)
                                shootVelocity *= 1.7f;

                            Utilities.NewProjectileBetter(npc.Center, shootVelocity, shootType, 0, 0f, 255);
                        }
                    }
                }

                if (deathEffectTimer >= 320f && deathEffectTimer <= 360f && deathEffectTimer % 10f == 0f)
                {
                    int sparkleCount = (int)MathHelper.Lerp(10f, 30f, Main.gfxQuality);
                    int boomChance = (int)MathHelper.Lerp(8f, 3f, Main.gfxQuality);
                    if (Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool(boomChance))
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<ProvBoomDeath>(), 0, 0f);

                    ReleaseSparkles(npc.Center, sparkleCount, 18f);
                    SoundEngine.PlaySound(CommonCalamitySounds.FlareSound, target.Center);
                    SoundEngine.PlaySound(HolyBlast.ImpactSound, target.Center);
                }

                if (deathEffectTimer >= 370f)
                    npc.Opacity *= 0.97f;

                if (Main.netMode != NetmodeID.MultiplayerClient && deathEffectTimer == 400f)
                {
                    ReleaseSparkles(npc.Center, 80, 22f);
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<DyingSun>(), 0, 0f, 255);
                }

                if (deathEffectTimer >= 435f)
                {
                    npc.active = false;
                    if (!target.dead)
                    {
                        npc.HitEffect();
                        npc.NPCLoot();
                    }
                    npc.netUpdate = true;
                    return false;
                }

                return false;
            }

            // Determine attack information based on the current music, if it's playing.
            GetLocalAttackInformation(npc, out ProvidenceAttackType currentAttack, out int localAttackTimer, out int localAttackDuration);

            // Reset things if the attack changed.
            if (attackType != (int)currentAttack)
            {
                for (int i = 0; i < 5; i++)
                    npc.Infernum().ExtraAI[i] = 0f;

                attackType = (int)currentAttack;
                npc.netUpdate = true;
            }

            // Execute attack patterns.
            switch ((ProvidenceAttackType)attackType)
            {
                case ProvidenceAttackType.EnterFireFormBulletHell:
                    DoBehavior_EnterFireFormBulletHell(npc, target, lifeRatio, localAttackTimer, localAttackDuration, ref drawState, ref lavaHeight);
                    break;
            }
            npc.rotation = npc.velocity.X * 0.003f;

            return false;
        }

        public static void DoBehavior_EnterFireFormBulletHell(NPC npc, Player target, float lifeRatio, int localAttackTimer, int localAttackDuration, ref float drawState, ref float lavaHeight)
        {
            // Enter the cocoon.
            drawState = (int)ProvidenceFrameDrawingType.CocoonState;

            npc.Opacity = 1f;

            // Create the lava on the first frame.
            if (localAttackTimer == 1)
            {
                // Play the burn sound universally.
                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceBurnSound);

                if (CalamityConfig.Instance.Screenshake)
                {
                    Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 15f;
                    ScreenEffectSystem.SetFlashEffect(npc.Center, 3f, 45);
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<ProfanedLava>(), 350, 0f);

                // Rise above the lava.
                npc.velocity = Vector2.UnitY * -13f;
                npc.netUpdate = true;
            }

            lavaHeight = MathHelper.Lerp(lavaHeight, 1400f, 0.012f);

            npc.velocity.Y *= 0.97f;
        }

        public static void DoVanillaFlightMovement(NPC npc, Player target, bool stayAwayFromTarget, ref float flightPath, float speedFactor = 1f)
        {
            // Reset the flight path direction.
            if (flightPath == 0)
            {
                flightPath = (npc.Center.X < target.Center.X).ToDirectionInt();
                npc.netUpdate = true;
            }

            float verticalDistanceFromTarget = target.position.Y - npc.Bottom.Y;
            float horizontalDistanceFromTarget = MathHelper.Distance(target.Center.X, npc.Center.X);
            float horizontalDistanceDirChangeThreshold = 800f;

            // Increase distance from target as necessary.
            if (stayAwayFromTarget)
                horizontalDistanceDirChangeThreshold += 240f;

            // Change X movement path if far enough away from target.
            if (npc.Center.X < target.Center.X && flightPath < 0 && horizontalDistanceFromTarget > horizontalDistanceDirChangeThreshold)
                flightPath = 0;
            if (npc.Center.X > target.Center.X && flightPath > 0 && horizontalDistanceFromTarget > horizontalDistanceDirChangeThreshold)
                flightPath = 0;

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
            npc.velocity.X = MathHelper.Clamp(npc.velocity.X + flightPath * acceleration, -maxFlySpeed, maxFlySpeed);
            if (verticalDistanceFromTarget < 150f)
                npc.velocity.Y -= 0.2f;
            if (verticalDistanceFromTarget > 220f)
                npc.velocity.Y += 0.4f;

            npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y, -6f, 6f);
        }

        public static void ReleaseSparkles(Vector2 sparkleSpawnPosition, int sparkleCount, float maxSpraySpeed)
        {
            // Prevent projectiles from spawning client-side.
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < sparkleCount; i++)
                Utilities.NewProjectileBetter(sparkleSpawnPosition, Main.rand.NextVector2Circular(maxSpraySpeed, maxSpraySpeed), ModContent.ProjectileType<YharonMajesticSparkle>(), 0, 0f);
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
            float deathEffectInterpolant = Utils.GetLerpValue(0f, 35f, npc.Infernum().ExtraAI[DeathEffectTimerIndex], true);

            if (!IsEnraged)
            {
                Color c = Color.Lerp(new Color(255, 120, 0, 128), deathEffectColor, deathEffectInterpolant);
                Main.spriteBatch.Draw(wingTexture, baseDrawPosition, frame, c * npc.Opacity, npc.rotation, drawOrigin, npc.scale, spriteEffects, 0f);
            }
            else
            {
                Color nightWingColor = Color.Lerp(new Color(0, 255, 191, 0), deathEffectColor, deathEffectInterpolant) * npc.Opacity;
                Main.spriteBatch.Draw(wingTexture, baseDrawPosition, frame, nightWingColor, npc.rotation, drawOrigin, npc.scale, spriteEffects, 0f);
                for (int i = 0; i < 6; i++)
                {
                    Vector2 wingOffset = (MathHelper.TwoPi * i / 6f + Main.GlobalTimeWrappedHourly * 0.72f).ToRotationVector2() * npc.Opacity * wingVibrance * 4f;
                    Main.spriteBatch.Draw(wingTexture, baseDrawPosition + wingOffset, frame, nightWingColor * 0.55f, npc.rotation, drawOrigin, npc.scale, spriteEffects, 0f);
                }
            }
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            string baseTextureString = "CalamityMod/NPCs/Providence/";
            string baseGlowTextureString = baseTextureString + "Glowmasks/";
            string rockTextureString = "InfernumMode/Content/BehaviorOverrides/BossAIs/Providence/Sheets/ProvidenceRock";

            string getTextureString = baseTextureString + "Providence";
            string getTextureGlowString;
            string getTextureGlow2String;

            bool useDefenseFrames = npc.localAI[1] == 1f;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            ProvidenceAttackType attackType = (ProvidenceAttackType)(int)npc.ai[0];

            ref float burnIntensity = ref npc.localAI[3];

            void drawProvidenceInstance(Vector2 baseDrawPosition, int frameOffset, Color baseDrawColor)
            {
                rockTextureString = "InfernumMode/Content/BehaviorOverrides/BossAIs/Providence/Sheets/";
                if (npc.localAI[0] == (int)ProvidenceFrameDrawingType.CocoonState)
                {
                    if (!useDefenseFrames)
                    {
                        rockTextureString += "ProvidenceDefenseRock";
                        getTextureString = baseTextureString + "ProvidenceDefense";
                        getTextureGlowString = baseGlowTextureString + "ProvidenceDefenseGlow";
                        getTextureGlow2String = baseGlowTextureString + "ProvidenceDefenseGlow2";
                    }
                    else
                    {
                        rockTextureString += "ProvidenceDefenseAltRock";
                        getTextureString = baseTextureString + "ProvidenceDefenseAlt";
                        getTextureGlowString = baseGlowTextureString + "ProvidenceDefenseAltGlow";
                        getTextureGlow2String = baseGlowTextureString + "ProvidenceDefenseAltGlow2";
                    }
                }
                else
                {
                    if (npc.localAI[2] == 0f)
                    {
                        rockTextureString += "ProvidenceRock";
                        getTextureGlowString = baseGlowTextureString + "ProvidenceGlow";
                        getTextureGlow2String = baseGlowTextureString + "ProvidenceGlow2";
                    }
                    else if (npc.localAI[2] == 1f)
                    {
                        getTextureString = baseTextureString + "ProvidenceAlt";
                        rockTextureString += "ProvidenceAltRock";
                        getTextureGlowString = baseGlowTextureString + "ProvidenceAltGlow";
                        getTextureGlow2String = baseGlowTextureString + "ProvidenceAltGlow2";
                    }
                    else if (npc.localAI[2] == 2f)
                    {
                        rockTextureString += "ProvidenceAttackRock";
                        getTextureString = baseTextureString + "ProvidenceAttack";
                        getTextureGlowString = baseGlowTextureString + "ProvidenceAttackGlow";
                        getTextureGlow2String = baseGlowTextureString + "ProvidenceAttackGlow2";
                    }
                    else
                    {
                        rockTextureString += "ProvidenceAttackAltRock";
                        getTextureString = baseTextureString + "ProvidenceAttackAlt";
                        getTextureGlowString = baseGlowTextureString + "ProvidenceAttackAltGlow";
                        getTextureGlow2String = baseGlowTextureString + "ProvidenceAttackAltGlow2";
                    }
                }

                float wingVibrance = 1f;
                if (attackType == ProvidenceAttackType.SpawnEffect)
                    wingVibrance = npc.ai[1] / AuraTime;

                getTextureGlowString += "Night";

                Texture2D generalTexture = ModContent.Request<Texture2D>(getTextureString).Value;
                Texture2D crystalTexture = ModContent.Request<Texture2D>(getTextureGlow2String).Value;
                Texture2D wingTexture = ModContent.Request<Texture2D>(getTextureGlowString).Value;
                Texture2D fatCrystalTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Providence/ProvidenceCrystal").Value;

                SpriteEffects spriteEffects = SpriteEffects.None;
                if (npc.spriteDirection == 1)
                    spriteEffects = SpriteEffects.FlipHorizontally;

                Vector2 drawOrigin = npc.frame.Size() * 0.5f;

                // Draw the crystal behind everything. It will appear if providence is herself invisible.
                if (npc.localAI[3] <= 0f)
                {
                    Vector2 crystalOrigin = fatCrystalTexture.Size() * 0.5f;
                    Vector2 crystalDrawPosition = npc.Center - Main.screenPosition;
                    Main.spriteBatch.Draw(fatCrystalTexture, crystalDrawPosition, null, Color.White, npc.rotation, crystalOrigin, npc.scale, spriteEffects, 0f);
                }

                int frameHeight = generalTexture.Height / 3;
                if (frameHeight <= 0)
                    frameHeight = 1;

                Rectangle frame = generalTexture.Frame(1, 3, 0, (npc.frame.Y / frameHeight + frameOffset) % 3);

                // Draw the base texture.
                baseDrawColor *= npc.Opacity;
                Main.spriteBatch.Draw(generalTexture, baseDrawPosition, frame, baseDrawColor, npc.rotation, drawOrigin, npc.scale, spriteEffects, 0f);

                // Draw the wings.
                DrawProvidenceWings(npc, wingTexture, wingVibrance, baseDrawPosition, frame, drawOrigin, spriteEffects);

                // Draw the crystals.
                for (int i = 0; i < 9; i++)
                {
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 9f).ToRotationVector2() * 2f;
                    Main.spriteBatch.Draw(crystalTexture, baseDrawPosition + drawOffset, frame, Color.White with { A = 0 } * npc.Opacity, npc.rotation, drawOrigin, npc.scale, spriteEffects, 0f);
                }
            }

            int totalProvidencesToDraw = (int)MathHelper.Lerp(1f, 30f, burnIntensity);
            for (int i = 0; i < totalProvidencesToDraw; i++)
            {
                float offsetAngle = MathHelper.TwoPi * i * 2f / totalProvidencesToDraw;
                float drawOffsetScalar = (float)Math.Sin(offsetAngle * 6f + Main.GlobalTimeWrappedHourly * MathHelper.Pi);
                drawOffsetScalar *= (float)Math.Pow(burnIntensity, 3f) * 36f;
                drawOffsetScalar *= MathHelper.Lerp(1f, 2f, 1f - lifeRatio);

                Vector2 drawOffset = offsetAngle.ToRotationVector2() * drawOffsetScalar;
                if (totalProvidencesToDraw <= 1)
                    drawOffset = Vector2.Zero;

                Vector2 drawPosition = npc.Center - Main.screenPosition + drawOffset;

                Color baseColor = Color.White * (MathHelper.Lerp(0.4f, 0.8f, burnIntensity) / totalProvidencesToDraw * 7f);
                baseColor.A = 0;
                baseColor = Color.Lerp(Color.White, baseColor, burnIntensity);
                if (IsEnraged)
                    baseColor = Color.Lerp(baseColor, Color.Cyan with { A = 0 }, 0.5f);

                drawProvidenceInstance(drawPosition, 0, baseColor);
            }

            // Draw the rock texture above the bloom effects.
            Texture2D rockTexture = ModContent.Request<Texture2D>(rockTextureString).Value;
            float opacity = Utils.GetLerpValue(0.038f, 0.04f, lifeRatio, true) * 0.6f;
            ScreenOverlaysSystem.ThingsToDrawOnTopOfBlur.Add(new(rockTexture, npc.Center - Main.screenPosition, npc.frame, npc.GetAlpha(Color.White) * opacity, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, 0, 0));

            return false;
        }
        #endregion

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Don't worry about hooking to the walls or anything like that. Providence provides you with unlimited flight time!";
        }
        #endregion Tips
    }
}
