using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.World;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using DraedonNPC = CalamityMod.NPCs.ExoMechs.Draedon;
using static CalamityMod.NPCs.ExoMechs.Draedon;
using InfernumMode.Buffs;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
    public class DraedonBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<DraedonNPC>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            // Set the whoAmI variable.
            CalamityGlobalNPC.draedon = npc.whoAmI;

            // Prevent stupid natural despawns.
            npc.timeLeft = 3600;

            // Define variables.
            ref float talkTimer = ref npc.ai[0];
            ref float hologramEffectTimer = ref npc.localAI[1];
            ref float killReappearDelay = ref npc.localAI[3];

            // Decide an initial target and play a teleport sound on the first frame.
            Player playerToFollow = Main.player[npc.target];
            if (talkTimer == 0f)
            {
                npc.TargetClosest(false);
                playerToFollow = Main.player[npc.target];
                 Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/DraedonTeleport"), playerToFollow.Center);
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

            // Stay within the world.
            npc.position.Y = MathHelper.Clamp(npc.position.Y, 150f, Main.maxTilesY * 16f - 150f);

            npc.spriteDirection = (playerToFollow.Center.X < npc.Center.X).ToDirectionInt();

            // Handle delays when re-appearing after being killed.
            if (killReappearDelay > 0f)
            {
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
                npc.Opacity = Utils.InverseLerp(0f, 8f, talkTimer, true);
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
                CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.DraedonIntroductionText1", TextColor);
                npc.netUpdate = true;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && talkTimer == TalkDelay + DelayPerDialogLine)
            {
                CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.DraedonIntroductionText2", TextColor);
                npc.netUpdate = true;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && talkTimer == TalkDelay + DelayPerDialogLine * 2f)
            {
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
                    CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.DraedonResummonText", TextColorEdgy);
                else
                    Main.NewText("My creations will not forget your failures. Choose wisely.", TextColorEdgy);

                // Mark Draedon as talked to.
                if (!CalamityWorld.TalkedToDraedon)
                {
                    CalamityWorld.TalkedToDraedon = true;
                    CalamityNetcode.SyncWorld();
                }

                npc.netUpdate = true;
            }

            // Wait for the player to select an exo mech.
            if (talkTimer >= ExoMechChooseDelay && talkTimer < ExoMechChooseDelay + 8f && CalamityWorld.DraedonMechToSummon == ExoMech.None)
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
            if (talkTimer > ExoMechChooseDelay + 8f && talkTimer < ExoMechPhaseDialogueTime)
            {
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = Utils.InverseLerp(4200f, 1400f, Main.LocalPlayer.Distance(playerToFollow.Center), true) * 18f;
                Main.LocalPlayer.Calamity().GeneralScreenShakePower *= Utils.InverseLerp(ExoMechChooseDelay + 5f, ExoMechPhaseDialogueTime, talkTimer, true);
            }

            // Summon the selected exo mech.
            if (talkTimer == ExoMechChooseDelay + 10f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    npc.ModNPC<DraedonNPC>().SummonExoMech();

                if (Main.netMode != NetmodeID.Server)
                {
                    var sound = Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/FlareSound"), playerToFollow.Center);

                    if (sound != null)
                        sound.Volume = MathHelper.Clamp(sound.Volume * 1.55f, 0f, 1f);
                }
            }

            // Dialogue lines depending on what phase the exo mechs are at.
            switch ((int)npc.localAI[0])
            {
                case 1:

                    if (Main.netMode != NetmodeID.MultiplayerClient && talkTimer == ExoMechPhaseDialogueTime)
                    {
                        CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.DraedonExoPhase1Text1", TextColor);
                        npc.netUpdate = true;
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient && talkTimer == ExoMechPhaseDialogueTime + DelayPerDialogLine)
                    {
                        CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.DraedonExoPhase1Text2", TextColor);
                        npc.netUpdate = true;
                    }

                    break;

                case 3:

                    if (Main.netMode != NetmodeID.MultiplayerClient && talkTimer == ExoMechPhaseDialogueTime)
                    {
                        Main.NewText("Your efforts are very intriguing.", TextColor);
                        npc.netUpdate = true;
                    }

                    if (talkTimer == ExoMechPhaseDialogueTime + DelayPerDialogLine)
                    {
                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/DraedonLaugh"), playerToFollow.Center);
                        Main.NewText("Go on. Continue feeding information to my machines.", TextColorEdgy);
                    }

                    break;

                case 4:

                    if (Main.netMode != NetmodeID.MultiplayerClient && talkTimer == ExoMechPhaseDialogueTime)
                    {
                        CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.DraedonExoPhase4Text1", TextColor);
                        npc.netUpdate = true;
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient && talkTimer == ExoMechPhaseDialogueTime + DelayPerDialogLine)
                    {
                        CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.DraedonExoPhase4Text2", TextColor);
                        npc.netUpdate = true;
                    }

                    break;

                case 5:

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
                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/DraedonLaugh"), playerToFollow.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.DraedonExoPhase6Text3", TextColor);
                            npc.netUpdate = true;
                        }
                    }

                    break;
            }

            // Disable rage and adrenaline past a point.
            if (ExoMechManagement.CurrentThanatosPhase >= 3 || ExoMechManagement.CurrentAresPhase >= 3 || ExoMechManagement.CurrentTwinsPhase >= 3)
                playerToFollow.Infernum().MakeAnxious(45);

            if (talkTimer > ExoMechChooseDelay + 10f && !ExoMechIsPresent)
            {
                npc.ModNPC<DraedonNPC>().HandleDefeatStuff();
                npc.ModNPC<DraedonNPC>().DefeatTimer++;
            }

            if (!ExoMechIsPresent && npc.ModNPC<DraedonNPC>().DefeatTimer <= 0f)
                npc.modNPC.music = InfernumMode.CalamityMod.GetSoundSlot(SoundType.Music, "Sounds/Music/DraedonAmbience");
            if (ExoMechIsPresent)
                npc.modNPC.music = InfernumMode.Instance.GetSoundSlot(SoundType.Music, "Sounds/Music/ExoMechBosses");

            talkTimer++;
            return false;
        }
    }
}
