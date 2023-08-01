using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.Sounds;
using CalamityMod.World;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.Sounds;
using InfernumMode.Content.Achievements;
using InfernumMode.Content.Achievements.InfernumAchievements;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares;
using InfernumMode.Content.Credits;
using InfernumMode.Content.Projectiles.Pets;
using InfernumMode.Core;
using InfernumMode.Core.GlobalInstances.Players;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.Netcode;
using InfernumMode.Core.Netcode.Packets;
using InfernumMode.Core.OverridingSystem;
using InfernumMode.Core.TrackedMusic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityMod.NPCs.ExoMechs.Draedon;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.ExoMechAIUtilities;
using DraedonNPC = CalamityMod.NPCs.ExoMechs.Draedon;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon
{
    public class DraedonBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<DraedonNPC>();

        public const int IntroSoundLength = 106;

        public const int PostBattleMusicLength = 5120;

        // Projectile damage values.
        public const int NormalShotDamage = 540;

        public const int StrongerNormalShotDamage = 560;

        public const int AresEnergySlashDamage = 640;

        public const int PowerfulShotDamage = 850;

        // Contact damage values.
        public const int AresEnergyKatanaContactDamage = 650;

        public const int TwinsChargeContactDamage = 600;

        public const int ThanatosHeadDamage = 800;

        public const int ThanatosHeadDamageMaximumOverdrive = 960;

        // Exo Mech text colors.
        public static readonly Color ApolloTextColor = new(44, 172, 36);

        public static readonly Color ArtemisTextColor = new(246, 137, 24);

        public static readonly Color AresTextColor = new(197, 72, 64);

        public static readonly Color ThanatosTextColor = new(72, 104, 196);

        public override bool PreAI(NPC npc)
        {
            // Set the whoAmI variable.
            CalamityGlobalNPC.draedon = npc.whoAmI;

            // Prevent stupid natural despawns.
            npc.timeLeft = 4200;

            // Define variables.
            ref float talkTimer = ref npc.ai[0];
            ref float hologramEffectTimer = ref npc.localAI[1];
            ref float killReappearDelay = ref npc.localAI[3];
            ref float musicDelay = ref npc.Infernum().ExtraAI[0];
            ref float isPissed = ref npc.Infernum().ExtraAI[1];

            // Decide an initial target and play a teleport sound on the first frame.
            Player playerToFollow = Main.player[npc.target];
            if (talkTimer == 0f)
            {
                npc.TargetClosest(false);
                playerToFollow = Main.player[npc.target];
                SoundEngine.PlaySound(TeleportSound, playerToFollow.Center);
            }

            // Pick someone else to pay attention to if the old target is gone.
            if (playerToFollow.dead || !playerToFollow.active)
            {
                npc.TargetClosest(false);
                playerToFollow = Main.player[npc.target];

                // Fuck off if no living target exists.
                if (playerToFollow.dead || !playerToFollow.active)
                {
                    npc.life = 0;
                    npc.active = false;
                    return false;
                }
            }

            // Kill the player if pissed.
            if (isPissed == 1f)
            {
                npc.ModNPC<DraedonNPC>().ShouldStartStandingUp = true;
                SoundEngine.PlaySound(CommonCalamitySounds.LaserCannonSound with { MaxInstances = 45, Volume = 0.15f }, playerToFollow.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 laserSpawnPosition = npc.Center - Vector2.UnitY * 80f;
                    laserSpawnPosition.X += 40f;
                    if (npc.spriteDirection == 1)
                        laserSpawnPosition.X -= 70f;

                    Utilities.NewProjectileBetter(laserSpawnPosition, (playerToFollow.Center - laserSpawnPosition).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<AresPrecisionBlast>(), StrongerNormalShotDamage, 0f);
                }

                // Stay within the world.
                npc.position.Y = Clamp(npc.position.Y, 150f, Main.maxTilesY * 16f - 150f);
                npc.spriteDirection = (playerToFollow.Center.X < npc.Center.X).ToDirectionInt();

                // Fly near the target.
                Vector2 hoverDestination = playerToFollow.Center + Vector2.UnitX * (playerToFollow.Center.X < npc.Center.X).ToDirectionInt() * 325f;

                // Decide sprite direction based on movement if not close enough to the desination.
                // Not deciding this here results in Draedon using the default of looking at the target he's following.
                if (npc.WithinRange(hoverDestination, 300f))
                {
                    npc.velocity *= 0.96f;

                    float moveSpeed = Lerp(2f, 8f, Utils.GetLerpValue(45f, 275f, npc.Distance(hoverDestination), true));
                    npc.Center = npc.Center.MoveTowards(hoverDestination, moveSpeed);
                }
                else
                {
                    float flySpeed = 32f;
                    Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * flySpeed;
                    npc.SimpleFlyMovement(idealVelocity, flySpeed / 400f);
                    npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.045f);
                }

                return false;
            }

            if (!ExoMechIsPresent)
            {
                npc.ModNPC.SceneEffectPriority = SceneEffectPriority.BossHigh;
                if (npc.ModNPC<DraedonNPC>().DefeatTimer <= 0f)
                {
                    npc.ModNPC.Music = MusicLoader.GetMusicSlot(InfernumMode.CalamityMod, "Sounds/Music/DraedonsAmbience");
                    InfernumMode.DraedonThemeTimer = 0f;
                }
                else
                {
                    npc.ModNPC.Music = MusicLoader.GetMusicSlot(InfernumMode.Instance, "Sounds/Music/Draedon");
                    InfernumMode.DraedonThemeTimer = 1f;
                }
            }

            // Stay within the world.
            npc.position.Y = Clamp(npc.position.Y, 150f, Main.maxTilesY * 16f - 150f);
            npc.spriteDirection = (playerToFollow.Center.X < npc.Center.X).ToDirectionInt();

            // Handle delays when re-appearing after being killed.
            if (killReappearDelay > 0f)
            {
                if (killReappearDelay <= 60f)
                    npc.ModNPC<DraedonNPC>().ProjectorOffset -= 14.5f;

                npc.Opacity = 0f;
                killReappearDelay--;
                if (killReappearDelay <= 0f)
                    CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.Status.Boss.DraedonEndKillAttemptText", TextColor);
                return false;
            }

            // Synchronize the hologram effect and talk timer at the beginning.
            // Also calculate opacity.
            if (talkTimer <= HologramFadeinTime)
            {
                hologramEffectTimer = talkTimer;
                npc.Opacity = Utils.GetLerpValue(0f, 8f, talkTimer, true);
            }

            // Play the stand up animation after teleportation.
            if (talkTimer == HologramFadeinTime + 5f)
                npc.ModNPC<DraedonNPC>().ShouldStartStandingUp = true;

            // Gloss over the arbitrary details and just get to the Exo Mech selection if Draedon has already been talked to.
            if (CalamityWorld.TalkedToDraedon && talkTimer > 70 && talkTimer < TalkDelay * 4f - 25f)
            {
                talkTimer = TalkDelay * 4f - 25f;
                npc.netUpdate = true;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && talkTimer == TalkDelay)
            {
                CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.Status.Boss.DraedonIntroductionText1", TextColor);
                npc.netUpdate = true;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && talkTimer == TalkDelay + DelayPerDialogLine)
            {
                CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.Status.Boss.DraedonIntroductionText2", TextColor);
                npc.netUpdate = true;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && talkTimer == TalkDelay + DelayPerDialogLine * 2f)
            {
                CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.Status.Boss.DraedonIntroductionText3", TextColor);
                npc.netUpdate = true;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && talkTimer == TalkDelay + DelayPerDialogLine * 3f)
            {
                CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.Status.Boss.DraedonIntroductionText4", TextColor);
                npc.netUpdate = true;
            }

            // Inform the player who summoned draedon they may choose the first mech and cause a selection UI to appear over their head.
            if (Main.netMode != NetmodeID.MultiplayerClient && talkTimer == TalkDelay + DelayPerDialogLine * 4f)
            {
                if (CalamityWorld.TalkedToDraedon)
                {
                    CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.Status.Boss.DraedonResummonText", TextColorEdgy);
                    HatGirl.SayThingWhileOwnerIsAlive(playerToFollow, "Mods.InfernumMode.PetDialog.DraedonChooseTip2");
                }
                else
                {
                    CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.Status.Boss.DraedonIntroductionText5", TextColorEdgy);
                    HatGirl.SayThingWhileOwnerIsAlive(playerToFollow, "Mods.InfernumMode.PetDialog.DraedonChooseTip1");
                }

                // Mark Draedon as talked to.
                if (!CalamityWorld.TalkedToDraedon)
                {
                    CalamityWorld.TalkedToDraedon = true;
                    CalamityNetcode.SyncWorld();
                }

                npc.netUpdate = true;
            }

            // Wait for the player to select an exo mech.
            if (talkTimer >= ExoMechChooseDelay && talkTimer < ExoMechChooseDelay + 8f && CalamityWorld.DraedonMechToSummon == ExoMech.None && ExoMechManagement.TotalMechs <= 0)
            {
                playerToFollow.Calamity().AbleToSelectExoMech = true;
                talkTimer = ExoMechChooseDelay;
            }

            // Fly around once the exo mechs have been spawned.
            if (ExoMechIsPresent || npc.ModNPC<DraedonNPC>().DefeatTimer > 0f)
            {
                npc.ModNPC<DraedonNPC>().FlyAroundInGamerChair();
                npc.ai[3]++;
            }

            // Make the screen rumble and summon the exo mechs.
            if (talkTimer is > (ExoMechChooseDelay + 8f) and < ExoMechPhaseDialogueTime)
            {
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = Utils.GetLerpValue(4200f, 1400f, Main.LocalPlayer.Distance(playerToFollow.Center), true) * 18f;
                Main.LocalPlayer.Calamity().GeneralScreenShakePower *= Utils.GetLerpValue(ExoMechChooseDelay + 5f, ExoMechPhaseDialogueTime, talkTimer, true);
            }

            // Summon the selected exo mech.
            if (talkTimer == ExoMechChooseDelay + 9f)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    SoundEngine.PlaySound(CommonCalamitySounds.FlareSound with { Volume = 1.55f }, playerToFollow.Center);
                    SoundEngine.PlaySound(InfernumSoundRegistry.ExoMechIntroSound with { Volume = 1.5f });
                }
            }

            // Increment the music delay.
            if (talkTimer >= ExoMechChooseDelay + 10f)
                musicDelay++;

            // Dialogue lines depending on what phase the exo mechs are at.
            switch ((int)npc.localAI[0])
            {
                case 1:

                    if (Main.netMode != NetmodeID.MultiplayerClient && talkTimer == ExoMechPhaseDialogueTime)
                    {
                        CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.Status.Boss.DraedonExoPhase1Text1", TextColor);
                        npc.netUpdate = true;
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient && talkTimer == ExoMechPhaseDialogueTime + DelayPerDialogLine)
                    {
                        CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.Status.Boss.DraedonExoPhase1Text2", TextColor);
                        npc.netUpdate = true;
                    }

                    break;

                case 3:
                    if (Main.netMode != NetmodeID.MultiplayerClient && talkTimer == ExoMechPhaseDialogueTime)
                    {
                        CalamityUtils.DisplayLocalizedText("Mods.InfernumMode.Status.DraedonEffortMock1", TextColor);
                        npc.netUpdate = true;
                    }

                    if (talkTimer == ExoMechPhaseDialogueTime + DelayPerDialogLine)
                    {
                        SoundEngine.PlaySound(LaughSound, playerToFollow.Center);
                        CalamityUtils.DisplayLocalizedText("Mods.InfernumMode.Status.DraedonEffortMock2", TextColorEdgy);
                    }

                    break;

                case 4:
                    if (Main.netMode != NetmodeID.MultiplayerClient && talkTimer == ExoMechPhaseDialogueTime)
                    {
                        CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.Status.Boss.DraedonExoPhase5Text1", TextColor);
                        npc.netUpdate = true;
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient && talkTimer == ExoMechPhaseDialogueTime + DelayPerDialogLine)
                    {
                        CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.Status.Boss.DraedonExoPhase5Text2", TextColor);
                        npc.netUpdate = true;
                    }

                    break;

                case 6:

                    if (Main.netMode != NetmodeID.MultiplayerClient && talkTimer == ExoMechPhaseDialogueTime)
                    {
                        CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.Status.Boss.DraedonExoPhase6Text1", TextColor);
                        npc.netUpdate = true;
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient && talkTimer == ExoMechPhaseDialogueTime + DelayPerDialogLine)
                    {
                        CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.Status.Boss.DraedonExoPhase6Text2", TextColor);
                        npc.netUpdate = true;
                    }

                    if (talkTimer == ExoMechPhaseDialogueTime + DelayPerDialogLine * 2f)
                    {
                        SoundEngine.PlaySound(LaughSound, playerToFollow.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.Status.Boss.DraedonExoPhase6Text3", TextColor);
                            npc.netUpdate = true;
                        }
                    }

                    break;
            }

            if (talkTimer > ExoMechChooseDelay + 10f && !ExoMechIsPresent)
            {
                HandleDefeatStuff(npc, ref npc.ModNPC<DraedonNPC>().DefeatTimer);
                npc.ModNPC<DraedonNPC>().DefeatTimer++;
            }

            // Set the screenshader based on the current song section.
            if (ExoMechIsPresent && Main.netMode != NetmodeID.Server)
            {
                if (TrackedMusicManager.TryGetSongInformation(out var songInfo) && songInfo.SongSections.Any(s => s.Key.WithinRange(TrackedMusicManager.SongElapsedTime)))
                {
                    if (!InfernumEffectsRegistry.ScreenBorderShader.IsActive() && !InfernumConfig.Instance.ReducedGraphicsConfig)
                    {
                        Vector2 focusPoint = Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
                        Filters.Scene.Activate("InfernumMode:ScreenBorder", focusPoint);

                        // Get the section(s) where the current elapsed time is in.
                        var section = songInfo.SongSections.Keys.Where(s => s.WithinRange(TrackedMusicManager.SongElapsedTime));

                        // Get the type of section we are in from the first (as it could potentially be more than one) key found above in the Dictonary.
                        int mechType = 0;
                        if (songInfo.SongSections.TryGetValue(section.FirstOrDefault(), out int mech))
                            mechType = mech;

                        float intensity = 0.5f;
                        float saturation = 1f;

                        // The hue of the colors. These use HSL for needing to sync a single value as opposed to 3.
                        // These don't really need to be so precise but it is what windows calculator gave me so.
                        float blue = 0.666666667f;
                        float green = 0.25f;
                        float orange = 0.0444444444f;
                        float rgb = (1 + Main.GlobalTimeWrappedHourly * 0.3f + 35 * 0.54f) % 1f;
                        float transitionLength = 10f;
                        ref float currentHue = ref npc.Infernum().ExtraAI[ExoMechManagement.CurrentHueIndex];
                        ref float previousHue = ref npc.Infernum().ExtraAI[ExoMechManagement.PreviousHueIndex];
                        ref float hueTimer = ref npc.Infernum().ExtraAI[ExoMechManagement.HueTimerIndex];

                        float timerInterpolant = hueTimer / transitionLength;

                        // Get the hue that should be transitioned to.
                        var newHue = (ExoMechMusicPhases)mechType switch
                        {
                            ExoMechMusicPhases.Thanatos => blue,
                            ExoMechMusicPhases.Twins => green,
                            ExoMechMusicPhases.Ares => orange,
                            ExoMechMusicPhases.AllThree => rgb,
                            _ => 0f,
                        };

                        // Transition to the new hue.
                        if (hueTimer < transitionLength && currentHue != newHue)
                        {
                            currentHue = Lerp(previousHue, newHue, timerInterpolant);

                            // If the mech type is draedon, also change the saturation. This is because white has a saturation of 0, while the
                            // other colors share one of 1.
                            if ((ExoMechMusicPhases)mechType is ExoMechMusicPhases.Draedon)
                                saturation = currentHue;
                            hueTimer++;
                        }
                        // When the transition time has elapsed, update the hue variables to the current hue.
                        else
                        {
                            previousHue = newHue;
                            currentHue = newHue;
                            // Also keep setting the saturation at 0 if needed.
                            if ((ExoMechMusicPhases)mechType is ExoMechMusicPhases.Draedon)
                                saturation = 0f;
                            // Reset the timer.
                            hueTimer = 0;
                        }

                        // The draedon all mechs mech type should have a lower luminosity.
                        float luminosity = 0.5f;
                        if ((ExoMechMusicPhases)mechType == ExoMechMusicPhases.AllThree)
                            luminosity = 0.36f;

                        // Set the shader color, opactiy, image, and intensity.
                        InfernumEffectsRegistry.ScreenBorderShader.GetShader().UseColor(Main.hslToRgb(currentHue, saturation, luminosity));
                        InfernumEffectsRegistry.ScreenBorderShader.GetShader().UseOpacity(1f);
                        InfernumEffectsRegistry.ScreenBorderShader.GetShader().UseImage(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/TechyNoise").Value, 0, SamplerState.AnisotropicWrap);
                        InfernumEffectsRegistry.ScreenBorderShader.GetShader().UseIntensity(intensity);
                    }
                }

                // For some reason, screen shaders have a several frames delay after deactivating before they actually vanish. This is fucking annoying.
                // Setting the shader's opacity to 0 if it should be gone seems to "fix" it, but its still actually being ran so its more of a bandaid fix.
                else
                {
                    if (Main.netMode != NetmodeID.Server)
                    {
                        InfernumEffectsRegistry.ScreenBorderShader.GetShader().UseOpacity(0f);
                        InfernumEffectsRegistry.ScreenBorderShader.GetShader().UseIntensity(0f);
                    }
                    // Reset the previous hue.
                    npc.Infernum().ExtraAI[ExoMechManagement.PreviousHueIndex] = 0;
                }
            }

            talkTimer++;
            return false;
        }

        public static void SummonExoMech(Player playerToFollow)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient && Main.myPlayer == playerToFollow.whoAmI)
            {
                PacketManager.SendPacket<ExoMechSelectionPacket>();
                return;
            }

            int secondaryMech = (int)CustomExoMechSelectionSystem.DestroyerTypeToSummon;
            if (secondaryMech == (int)ExoMech.Destroyer)
                secondaryMech = ModContent.NPCType<ThanatosHead>();
            if (secondaryMech == (int)ExoMech.Prime)
                secondaryMech = ModContent.NPCType<AresBody>();
            if (secondaryMech == (int)ExoMech.Twins)
                secondaryMech = ModContent.NPCType<Apollo>();

            switch (CustomExoMechSelectionSystem.PrimaryMechToSummon)
            {
                // Summon Thanatos underground.
                case ExoMech.Destroyer:
                    Vector2 thanatosSpawnPosition = playerToFollow.Center + Vector2.UnitY * 2100f;
                    NPC thanatos = CalamityUtils.SpawnBossBetter(thanatosSpawnPosition, ModContent.NPCType<ThanatosHead>());
                    if (thanatos != null)
                    {
                        thanatos.velocity = thanatos.SafeDirectionTo(playerToFollow.Center) * 40f;
                        thanatos.Infernum().ExtraAI[ExoMechManagement.InitialMechNPCTypeIndex] = thanatos.type;
                        thanatos.Infernum().ExtraAI[ExoMechManagement.SecondaryMechNPCTypeIndex] = secondaryMech;
                    }
                    break;

                // Summon Ares in the sky, directly above the player.
                case ExoMech.Prime:
                    Vector2 aresSpawnPosition = playerToFollow.Center - Vector2.UnitY * 1400f;
                    NPC ares = CalamityUtils.SpawnBossBetter(aresSpawnPosition, ModContent.NPCType<AresBody>());
                    if (ares != null)
                    {
                        ares.Infernum().ExtraAI[ExoMechManagement.InitialMechNPCTypeIndex] = ares.type;
                        ares.Infernum().ExtraAI[ExoMechManagement.SecondaryMechNPCTypeIndex] = secondaryMech;
                    }
                    break;

                // Summon Apollo and Artemis above the player to their sides.
                case ExoMech.Twins:
                    Vector2 artemisSpawnPosition = playerToFollow.Center + new Vector2(-1100f, -1600f);
                    Vector2 apolloSpawnPosition = playerToFollow.Center + new Vector2(1100f, -1600f);
                    CalamityUtils.SpawnBossBetter(artemisSpawnPosition, ModContent.NPCType<Artemis>());
                    NPC apollo = CalamityUtils.SpawnBossBetter(apolloSpawnPosition, ModContent.NPCType<Apollo>());
                    if (apollo != null)
                    {
                        apollo.Infernum().ExtraAI[ExoMechManagement.InitialMechNPCTypeIndex] = apollo.type;
                        apollo.Infernum().ExtraAI[ExoMechManagement.SecondaryMechNPCTypeIndex] = secondaryMech;
                    }
                    break;
            }
        }

        public static bool HasEarnedHyperplaneMatrix()
        {
            // Check to see if any player has completed the Lab Rat and defeat all bosses achievement.
            bool allExosCondition = false;
            bool allBossesCondition = false;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (!Main.player[i].active)
                    continue;

                Player p = Main.player[i];
                foreach (var achievement in p.GetModPlayer<AchievementPlayer>().AchievementInstances)
                {
                    if (achievement.GetType() == typeof(ExoPathAchievement) && achievement.IsCompleted)
                        allExosCondition = true;
                    if (achievement.GetType() == typeof(KillAllBossesAchievement) && achievement.IsCompleted)
                        allBossesCondition = true;
                }
            }

            return allExosCondition && allBossesCondition;
        }

        public static void HandleDefeatStuff(NPC npc, ref float defeatTimer)
        {
            AchievementPlayer.DraedonDefeated = true;

            // Become vulnerable after being defeated after a certain point.
            bool hasBeenKilled = npc.localAI[2] == 1f;
            bool earnedHyperplaneMatrix = HasEarnedHyperplaneMatrix();
            ref float hologramEffectTimer = ref npc.localAI[1];
            npc.dontTakeDamage = defeatTimer < TalkDelay * 2f + 50f || hasBeenKilled || earnedHyperplaneMatrix;
            npc.Calamity().CanHaveBossHealthBar = !npc.dontTakeDamage;
            npc.Calamity().ShouldCloseHPBar = hasBeenKilled;

            bool leaving = defeatTimer > DelayBeforeDefeatStandup + TalkDelay * 8f + 200f;

            // Fade away and disappear when leaving.
            if (leaving)
            {
                hologramEffectTimer = Clamp(hologramEffectTimer - 1f, 0f, HologramFadeinTime);
                if (hologramEffectTimer <= 0f)
                {
                    // Begin the credits if scal is dead.
                    if (DownedBossSystem.downedCalamitas)
                        CreditManager.BeginCredits();
                    npc.active = false;
                }
            }

            // Fade back in as a hologram if the player tried to kill Draedon.
            else if (hasBeenKilled)
                hologramEffectTimer = Clamp(hologramEffectTimer + 1f, 0f, HologramFadeinTime - 5f);

            // Adjust opacity.
            npc.Opacity = hologramEffectTimer / HologramFadeinTime;
            if (hasBeenKilled)
                npc.Opacity *= 0.67f;

            // Stand up in awe after a small amount of time has passed.
            if (defeatTimer is > DelayBeforeDefeatStandup and < (TalkDelay * 3f + 50f))
                npc.ModNPC<DraedonNPC>().ShouldStartStandingUp = true;

            if (defeatTimer == DelayBeforeDefeatStandup + 50f)
            {
                CalamityUtils.DisplayLocalizedText("Mods.InfernumMode.Status.DraedonDefeat1", TextColor);
            }

            if (defeatTimer == DelayBeforeDefeatStandup + TalkDelay + 50f)
            {
                if (earnedHyperplaneMatrix)
                    CalamityUtils.DisplayLocalizedText("Mods.InfernumMode.Status.DraedonDefeat2HyperplaneMatrix", TextColor);
                else
                    CalamityUtils.DisplayLocalizedText("Mods.InfernumMode.Status.DraedonDefeat2", TextColor);
            }

            if (defeatTimer == DelayBeforeDefeatStandup + TalkDelay * 2f + 50f)
            {
                if (earnedHyperplaneMatrix)
                    CalamityUtils.DisplayLocalizedText("Mods.InfernumMode.Status.DraedonDefeat3HyperplaneMatrix", TextColor);
                else
                    CalamityUtils.DisplayLocalizedText("Mods.InfernumMode.Status.DraedonDefeat3", TextColor);
            }

            // After this point Draedon becomes vulnerable.
            // He sits back down as well as he thinks for a bit.
            // Killing him will cause gore to appear but also for Draedon to come back as a hologram.
            if (defeatTimer == DelayBeforeDefeatStandup + TalkDelay * 3f + 50f)
            {
                if (earnedHyperplaneMatrix)
                    CalamityUtils.DisplayLocalizedText("Mods.InfernumMode.Status.DraedonDefeat4HyperplaneMatrix", TextColor);
                else
                    CalamityUtils.DisplayLocalizedText("Mods.InfernumMode.Status.DraedonDefeat4", TextColor);
            }

            if (defeatTimer == DelayBeforeDefeatStandup + TalkDelay * 3f + 165f)
            {
                if (earnedHyperplaneMatrix)
                    AchievementPlayer.ExtraUpdateHandler(Main.LocalPlayer, AchievementUpdateCheck.NPCKill, npc.whoAmI);
                else
                    CalamityUtils.DisplayLocalizedText("Mods.InfernumMode.Status.DraedonDefeat5", TextColor);
            }

            if (defeatTimer == DelayBeforeDefeatStandup + TalkDelay * 4f + 165f)
            {
                if (earnedHyperplaneMatrix)
                    CalamityUtils.DisplayLocalizedText("Mods.InfernumMode.Status.DraedonDefeat6HyperplaneMatrix", TextColor);
                else
                    CalamityUtils.DisplayLocalizedText("Mods.InfernumMode.Status.DraedonDefeat6", TextColor);
            }

            if (defeatTimer == DelayBeforeDefeatStandup + TalkDelay * 5f + 165f)
            {
                if (earnedHyperplaneMatrix)
                {
                    CalamityUtils.DisplayLocalizedText("Mods.InfernumMode.Status.DraedonDefeat7HyperplaneMatrix", TextColorEdgy);
                    defeatTimer = DelayBeforeDefeatStandup + TalkDelay * 6f + 166f;
                    npc.netUpdate = true;
                }
                else
                    CalamityUtils.DisplayLocalizedText("Mods.InfernumMode.Status.DraedonDefeat7", TextColor);
            }

            if (defeatTimer == DelayBeforeDefeatStandup + TalkDelay * 6f + 165f)
                CalamityUtils.DisplayLocalizedText("Mods.InfernumMode.Status.DraedonDefeat8", TextColor);

            if (defeatTimer == DelayBeforeDefeatStandup + TalkDelay * 7f + 165f)
                CalamityUtils.DisplayLocalizedText("Mods.InfernumMode.Status.DraedonDefeat9", TextColor);

            if (defeatTimer == DelayBeforeDefeatStandup + TalkDelay * 8f + 165f)
                CalamityUtils.DisplayLocalizedText("Mods.InfernumMode.Status.DraedonDefeat10", TextColor);
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frame.Width = 100;

            int xFrame = npc.frame.X / npc.frame.Width;
            int yFrame = npc.frame.Y / frameHeight;
            int frame = xFrame * Main.npcFrameCount[npc.type] + yFrame;

            // Prepare to stand up if called for and not already doing so.
            if (npc.ModNPC<DraedonNPC>().ShouldStartStandingUp && frame > 23)
                frame = 0;

            int frameChangeDelay = 7;
            bool shouldNotSitDown = npc.ModNPC<DraedonNPC>().DefeatTimer is > DelayBeforeDefeatStandup and < (TalkDelay * 3f + 10f) || npc.Infernum().ExtraAI[1] == 1f;

            npc.frameCounter++;
            if (npc.frameCounter >= frameChangeDelay)
            {
                frame++;

                if (!npc.ModNPC<DraedonNPC>().ShouldStartStandingUp && (frame < 23 || frame > 47))
                    frame = 23;

                // Do the sit animation infinitely if Draedon should not sit down again.
                if (shouldNotSitDown && frame >= 16)
                    frame = 11;

                if (frame >= 23 && npc.ModNPC<DraedonNPC>().ShouldStartStandingUp)
                {
                    frame = 0;
                    npc.ModNPC<DraedonNPC>().ShouldStartStandingUp = false;
                }

                npc.frameCounter = 0;
            }

            npc.frame.X = frame / Main.npcFrameCount[npc.type] * npc.frame.Width;
            npc.frame.Y = frame % Main.npcFrameCount[npc.type] * frameHeight;
        }
    }
}
