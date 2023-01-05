using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.Sounds;
using CalamityMod.World;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares;
using InfernumMode.GlobalInstances.Players;
using InfernumMode.ILEditingStuff;
using InfernumMode.OverridingSystem;
using InfernumMode.Projectiles;
using InfernumMode.Sounds;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityMod.NPCs.ExoMechs.Draedon;
using DraedonNPC = CalamityMod.NPCs.ExoMechs.Draedon;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
    public class DraedonBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<DraedonNPC>();

        public const int IntroSoundLength = 106;

        public const int PostBattleMusicLength = 5120;

        // Projectile damage values.
        public const int NormalShotDamage = 520;

        public const int StrongerNormalShotDamage = 540;

        public const int PowerfulShotDamage = 850;

        // Contact damage values.
        public const int AresChargeContactDamage = 650;

        public const int AresPhotonRipperContactDamage = 600;

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
                CalamityUtils.ModNPC<DraedonNPC>(npc).ShouldStartStandingUp = true;
                SoundEngine.PlaySound(CommonCalamitySounds.LaserCannonSound, playerToFollow.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 laserSpawnPosition = npc.Center - Vector2.UnitY * 44f;
                    laserSpawnPosition.X -= 180f;
                    if (npc.spriteDirection == 1)
                        laserSpawnPosition.X += 46f;

                    Utilities.NewProjectileBetter(laserSpawnPosition, (playerToFollow.Center - laserSpawnPosition).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<AresPrecisionBlast>(), StrongerNormalShotDamage, 0f);
                }

                // Stay within the world.
                npc.position.Y = MathHelper.Clamp(npc.position.Y, 150f, Main.maxTilesY * 16f - 150f);
                npc.spriteDirection = (playerToFollow.Center.X > npc.Center.X).ToDirectionInt();

                // Fly near the target.
                Vector2 hoverDestination = playerToFollow.Center + Vector2.UnitX * (playerToFollow.Center.X < npc.Center.X).ToDirectionInt() * 325f;

                // Decide sprite direction based on movement if not close enough to the desination.
                // Not deciding this here results in Draedon using the default of looking at the target he's following.
                if (npc.WithinRange(hoverDestination, 300f))
                {
                    npc.velocity *= 0.96f;

                    float moveSpeed = MathHelper.Lerp(2f, 8f, Utils.GetLerpValue(45f, 275f, npc.Distance(hoverDestination), true));
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
                    npc.ModNPC.Music = MusicLoader.GetMusicSlot(InfernumMode.CalamityMod, "Sounds/Music/DraedonAmbience");
                    InfernumMode.DraedonThemeTimer = 0f;
                }
                else
                {
                    npc.ModNPC.Music = MusicLoader.GetMusicSlot(InfernumMode.Instance, "Sounds/Music/Draedon");
                    InfernumMode.DraedonThemeTimer = 1f;
                }
            }

            // Stay within the world.
            npc.position.Y = MathHelper.Clamp(npc.position.Y, 150f, Main.maxTilesY * 16f - 150f);
            npc.spriteDirection = (playerToFollow.Center.X > npc.Center.X).ToDirectionInt();

            // Handle delays when re-appearing after being killed.
            if (killReappearDelay > 0f)
            {
                if (killReappearDelay <= 60f)
                    npc.ModNPC<DraedonNPC>().ProjectorOffset -= 14.5f;

                npc.Opacity = 0f;
                killReappearDelay--;
                if (killReappearDelay <= 0f)
                    CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.DraedonEndKillAttemptText", TextColor);
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

            // Please don't tell the rest of the multiplayer server about this Toasty I want it to be a funny moment.
            bool useMultiplayerPTJokeText = Main.netMode != NetmodeID.SinglePlayer;

            if (Main.netMode != NetmodeID.MultiplayerClient && talkTimer == TalkDelay)
            {
                if (useMultiplayerPTJokeText)
                    Utilities.DisplayText("You absolute FOOLS!", TextColor);
                else
                    CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.DraedonIntroductionText1", TextColor);
                npc.netUpdate = true;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && talkTimer == TalkDelay + DelayPerDialogLine)
            {
                if (useMultiplayerPTJokeText)
                    Utilities.DisplayText("You thought my stupid server dialog gag would go away just like THAT?!", TextColor);
                else
                    CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.DraedonIntroductionText2", TextColor);
                npc.netUpdate = true;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && talkTimer == TalkDelay + DelayPerDialogLine * 2f)
            {
                if (useMultiplayerPTJokeText)
                    Utilities.DisplayText("You are going to fight my robots NOW", TextColor);
                else
                    CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.DraedonIntroductionText3", TextColor);
                npc.netUpdate = true;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && talkTimer == TalkDelay + DelayPerDialogLine * 3f)
            {
                CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.DraedonIntroductionText4", TextColor);
                npc.netUpdate = true;
            }

            // Inform the player who summoned draedon they may choose the first mech and cause a selection UI to appear over their head.
            if (Main.netMode != NetmodeID.MultiplayerClient && talkTimer == TalkDelay + DelayPerDialogLine * 4f)
            {
                if (CalamityWorld.TalkedToDraedon)
                {
                    CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.DraedonResummonText", TextColorEdgy);
                    HatGirl.SayThingWhileOwnerIsAlive(playerToFollow, "If a certain starting combo isnt working too well, maybe experiment with another one?");
                }
                else
                {
                    Utilities.DisplayText("Now choose.", TextColorEdgy);
                    HatGirl.SayThingWhileOwnerIsAlive(playerToFollow, "Better choose well!");
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
                        if (useMultiplayerPTJokeText)
                            Utilities.DisplayText("I wanted to summon the prime mechs-", TextColor);
                        else
                            CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.DraedonExoPhase1Text1", TextColor);
                        npc.netUpdate = true;
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient && talkTimer == ExoMechPhaseDialogueTime + DelayPerDialogLine)
                    {
                        if (useMultiplayerPTJokeText)
                            Utilities.DisplayText("Unfortunately, Dominic, that stupid mf, wouldn't let me", TextColor);
                        else
                            CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.DraedonExoPhase1Text2", TextColor);
                        npc.netUpdate = true;
                    }

                    break;

                case 3:
                    if (useMultiplayerPTJokeText)
                    {
                        Utilities.DisplayText("OK", TextColor);
                        break;
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient && talkTimer == ExoMechPhaseDialogueTime)
                    {
                        Utilities.DisplayText("Your efforts are very intriguing.", TextColor);
                        npc.netUpdate = true;
                    }

                    if (talkTimer == ExoMechPhaseDialogueTime + DelayPerDialogLine)
                    {
                        SoundEngine.PlaySound(LaughSound, playerToFollow.Center);
                        Utilities.DisplayText("Go on. Continue feeding information to my machines.", TextColorEdgy);
                    }

                    break;

                case 4:
                    if (useMultiplayerPTJokeText)
                        break;

                    if (Main.netMode != NetmodeID.MultiplayerClient && talkTimer == ExoMechPhaseDialogueTime)
                    {
                        CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.DraedonExoPhase5Text1", TextColor);
                        npc.netUpdate = true;
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient && talkTimer == ExoMechPhaseDialogueTime + DelayPerDialogLine)
                    {
                        CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.DraedonExoPhase5Text2", TextColor);
                        npc.netUpdate = true;
                    }

                    break;

                case 6:

                    if (Main.netMode != NetmodeID.MultiplayerClient && talkTimer == ExoMechPhaseDialogueTime)
                    {
                        if (useMultiplayerPTJokeText)
                            Utilities.DisplayText("OK this joke is dumb I'm going to use my regular dialog now", TextColorEdgy);
                        else
                            CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.DraedonExoPhase6Text1", TextColor);
                        npc.netUpdate = true;
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient && talkTimer == ExoMechPhaseDialogueTime + DelayPerDialogLine)
                    {
                        CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.DraedonExoPhase6Text2", TextColor);
                        npc.netUpdate = true;
                    }

                    if (talkTimer == ExoMechPhaseDialogueTime + DelayPerDialogLine * 2f)
                    {
                        SoundEngine.PlaySound(LaughSound, playerToFollow.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.DraedonExoPhase6Text3", TextColor);
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

            talkTimer++;
            return false;
        }

        public static void SummonExoMech(Player playerToFollow)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetcodeHandler.SyncExoMechSummon(playerToFollow);
                return;
            }

            int secondaryMech = (int)DrawDraedonSelectionUIWithAthena.DestroyerTypeToSummon;
            if (secondaryMech == (int)ExoMech.Destroyer)
                secondaryMech = ModContent.NPCType<ThanatosHead>();
            if (secondaryMech == (int)ExoMech.Prime)
                secondaryMech = ModContent.NPCType<AresBody>();
            if (secondaryMech == (int)ExoMech.Twins)
                secondaryMech = ModContent.NPCType<Apollo>();

            switch (DrawDraedonSelectionUIWithAthena.PrimaryMechToSummon)
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

        public static void HandleDefeatStuff(NPC npc, ref float defeatTimer)
        {
            AchievementPlayer.DraedonDefeated = true;
            
            // Become vulnerable after being defeated after a certain point.
            bool hasBeenKilled = npc.localAI[2] == 1f;
            ref float hologramEffectTimer = ref npc.localAI[1];
            npc.dontTakeDamage = defeatTimer < TalkDelay * 2f + 50f || hasBeenKilled;
            npc.Calamity().CanHaveBossHealthBar = !npc.dontTakeDamage;
            npc.Calamity().ShouldCloseHPBar = hasBeenKilled;

            bool leaving = defeatTimer > DelayBeforeDefeatStandup + TalkDelay * 8f + 200f;

            // Fade away and disappear when leaving.
            if (leaving)
            {
                hologramEffectTimer = MathHelper.Clamp(hologramEffectTimer - 1f, 0f, HologramFadeinTime);
                if (hologramEffectTimer <= 0f)
                    npc.active = false;
            }

            // Fade back in as a hologram if the player tried to kill Draedon.
            else if (hasBeenKilled)
                hologramEffectTimer = MathHelper.Clamp(hologramEffectTimer + 1f, 0f, HologramFadeinTime - 5f);

            // Adjust opacity.
            npc.Opacity = hologramEffectTimer / HologramFadeinTime;
            if (hasBeenKilled)
                npc.Opacity *= 0.67f;

            // Stand up in awe after a small amount of time has passed.
            if (defeatTimer is > DelayBeforeDefeatStandup and < (TalkDelay * 3f + 50f))
                npc.ModNPC<DraedonNPC>().ShouldStartStandingUp = true;

            if (defeatTimer == DelayBeforeDefeatStandup + 50f)
                Utilities.DisplayText("Intriguing. Truly, intriguing.", TextColor);

            if (defeatTimer == DelayBeforeDefeatStandup + TalkDelay + 50f)
                Utilities.DisplayText("My magnum opera, truly and utterly defeated.", TextColor);

            if (defeatTimer == DelayBeforeDefeatStandup + TalkDelay * 2f + 50f)
                Utilities.DisplayText("This outcome was not what I had expected.", TextColor);

            // After this point Draedon becomes vulnerable.
            // He sits back down as well as he thinks for a bit.
            // Killing him will cause gore to appear but also for Draedon to come back as a hologram.
            if (defeatTimer == DelayBeforeDefeatStandup + TalkDelay * 3f + 50f)
                Utilities.DisplayText("...Excuse my introspection. I must gather my thoughts after that display.", TextColor);

            if (defeatTimer == DelayBeforeDefeatStandup + TalkDelay * 3f + 165f)
                Utilities.DisplayText("It is perhaps not irrational to infer that you are beyond my reasoning.", TextColor);

            if (defeatTimer == DelayBeforeDefeatStandup + TalkDelay * 4f + 165f)
                Utilities.DisplayText("Now.", TextColor);

            if (defeatTimer == DelayBeforeDefeatStandup + TalkDelay * 5f + 165f)
                Utilities.DisplayText("You would wish to reach the Tyrant. I cannot assist you in that.", TextColor);

            if (defeatTimer == DelayBeforeDefeatStandup + TalkDelay * 6f + 165f)
                Utilities.DisplayText("It is not a matter of spite, for I would wish nothing more than to observe such a conflict.", TextColor);

            if (defeatTimer == DelayBeforeDefeatStandup + TalkDelay * 7f + 165f)
                Utilities.DisplayText("But now, I must return to my machinery. You may use the Codebreaker if you wish to face my creations once again.", TextColor);

            if (defeatTimer == DelayBeforeDefeatStandup + TalkDelay * 8f + 165f)
                Utilities.DisplayText("In the meantime, I bid you farewell, and good luck in your future endeavors.", TextColor);
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
