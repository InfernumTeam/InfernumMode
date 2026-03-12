using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.NPCs;
using CalamityMod.NPCs.PrimordialWyrm;
using CalamityMod.Systems;
using CalamityMod.World;
using InfernumMode.Content.Achievements;
using InfernumMode.Core.GlobalInstances.Players;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace InfernumMode.Core.ILEditingStuff
{
    internal sealed class PermitOldDukeRainHook : ModSystem
    {
        public static MethodInfo? CalamityWorldPostUpdate = typeof(WorldMiscUpdateSystem).GetMethod("PostUpdateWorld", Utilities.UniversalBindingFlags);
        public static ILHook? PermitOldDukeRain_IL_Hook;
        public override void OnModLoad()
        {
            if (CalamityWorldPostUpdate != null)
            {
                PermitOldDukeRain_IL_Hook = new(CalamityWorldPostUpdate, PermitOldDukeRain_IL);
                PermitOldDukeRain_IL_Hook?.Apply();
            }
        }

        public static void PermitOldDukeRain_IL(ILContext context)
        {
            ILCursor cursor = new(context);

            if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchStsfld<Main>("raining")))
                return;

            int start = cursor.Index - 1;

            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchCall<CalamityNetcode>("SyncWorld")))
                return;

            int end = cursor.Index;
            cursor.Goto(start);
            cursor.RemoveRange(end - start);
            cursor.Emit(OpCodes.Nop);
        }
    }

    internal sealed class AchievementMenuUIHookEdit : ModSystem
    {
        private static bool justHovered;

        public override void Load() => On_AchievementAdvisor.DrawOneAchievement += AchievementAdvisor_DrawOneAchievement;

        private void AchievementAdvisor_DrawOneAchievement(On_AchievementAdvisor.orig_DrawOneAchievement orig, AchievementAdvisor self, SpriteBatch spriteBatch, Vector2 position, bool large)
        {
            // Vanilla scaling code.
            float scale = 0.35f;
            if (large)
                scale = 0.75f;

            // Draw the icon and get if it is being hovered.
            DrawAchievementIcon(spriteBatch, position + new Vector2(10f, 5) * scale, scale, out bool hovered);

            // If not hovering, return.
            if (!hovered)
                return;

            // Add mouse text.
            Main.hoverItemName = Utilities.GetLocalization("UI.WishesHoverTextButton").Value;

            // Handle clicking the icon.
            if (!PlayerInput.IgnoreMouseInterface)
            {
                Main.player[Main.myPlayer].mouseInterface = true;
                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    SoundEngine.PlaySound(SoundID.MenuOpen);
                    OpenAchievementMenu();
                }
            }
        }

        private static void DrawAchievementIcon(SpriteBatch spriteBatch, Vector2 position, float scale, out bool hovered)
        {
            // Set hovered to false by default.
            hovered = false;
            // Sort the color to draw in
            Color drawColor = Color.White;
            if (!hovered)
                drawColor = new Color(220, 220, 220, 220);

            // Set the base vertical offset, and get all of the textures.
            int verticalOffset = 66;
            Texture2D achievementTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Achievements/Textures/Achievement", AssetRequestMode.AsyncLoad).Value;
            Texture2D borderTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Achievements/Textures/InfernumAchievement_Border", AssetRequestMode.AsyncLoad).Value;
            Texture2D hoverTexture = Main.Assets.Request<Texture2D>("Images/UI/Achievement_Borders_MouseHoverThin").Value;

            // Get a local copy of the main, ordered achievement list.
            List<Achievement> achievements = AchievementPlayer.GetAchievementsList();
            Achievement currentAchievement = null;

            // Loop through the list.
            foreach (var achievement in achievements)
            {
                // If the achievement isnt completed yet.
                if (!achievement.IsCompleted)
                {
                    // Set this, declaring that one has been picked.
                    currentAchievement = achievement;
                    // Set the vertical offset based on the achievements position.
                    verticalOffset *= achievement.PositionInMainList;
                    break;
                }
            }

            // Get the correct frame to draw based on the above.
            Rectangle achievementFrame;
            if (currentAchievement is null)
            {
                verticalOffset *= achievements.Count - 1;
                achievementFrame = new(0, verticalOffset, 66, 66);
            }
            else
                achievementFrame = new(66, verticalOffset, 66, 66);

            // If the mouse is hovering over the area.
            if (Main.MouseScreen.Between(position, position + achievementFrame.Size() * scale))
            {
                Main.LocalPlayer.mouseInterface = true;
                hovered = true;
            }

            // Vanilla offset to make the border draw in the correct position
            Vector2 borderOffset = new Vector2(-4f) * scale;

            // Draw the achievement and the border.
            spriteBatch.Draw(achievementTexture, position, achievementFrame, drawColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(borderTexture, position + borderOffset, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            // If hovering
            if (hovered)
            {
                // If not hovering the frame before, play a sound.
                if (!justHovered)
                    SoundEngine.PlaySound(SoundID.MenuTick);

                // Set this as true, to prevent the sound being played every frame the mouse is hovering.
                justHovered = true;

                // Draw the hover texture.
                spriteBatch.Draw(hoverTexture, position + borderOffset + new Vector2(-1, -1), null, new Color(255, 147, 68), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
            else
                // Else, reset this to allow the sound to play again.
                justHovered = false;
        }

        internal static void OpenAchievementMenu()
        {
            // All of this is what lets the menu be the only thing on screen.
            Main.ingameOptionsWindow = false;
            IngameFancyUI.CoverNextFrame();
            Main.playerInventory = false;
            Main.editChest = false;
            Main.npcChatText = string.Empty;
            Main.ClosePlayerChat();
            Main.chatText = string.Empty;
            Main.inFancyUI = true;

            // Set this as the current UI state to draw. This is the custom UIState.
            Main.InGameUI.SetState(UIRenderingSystem.CurrentAchievementUI);
        }
    }

    internal sealed class SoundVolumeFalloffHookEdit : ModSystem
    {
        public override void Load() => On_ActiveSound.Update += ActiveSound_Update;

        private static List<string> SoundStylesToEdit =>
        [
            "InfernumMode/Assets/Sounds/Custom/WayfinderGateLoop",
            "InfernumMode/Assets/Sounds/Custom/ProvidenceDoorSoundLoop"
        ];

        // Ideally this would be an IL but I dont have a copy of the correct source version to look at the IL.
        private void ActiveSound_Update(On_ActiveSound.orig_Update orig, ActiveSound self)
        {
            if (!Program.IsMainThread)
                typeof(ActiveSound).GetMethod("RunOnMainThreadAndWait", BindingFlags.Static | BindingFlags.NonPublic).Invoke(self, [(Action)self.Update]);
            else
            {
                if (self.Sound == null || self.Sound.IsDisposed)
                    return;

                Vector2 screenMiddle = Main.screenPosition + new Vector2(Main.screenWidth / 2, Main.screenHeight / 2);
                float volumeModifier = 1f;
                if (self.Position.HasValue)
                {
                    float panValue = (self.Position.Value.X - screenMiddle.X) / (Main.screenWidth * 0.5f);
                    panValue = Clamp(panValue, -1f, 1f);
                    self.Sound.Pan = panValue;

                    // Modified code:
                    // ---->
                    float distance;
                    if (SoundStylesToEdit.Contains(self.Style.SoundPath))
                        distance = Vector2.Distance(self.Position.Value, screenMiddle) * 2 + 800;
                    else
                        distance = Vector2.Distance(self.Position.Value, screenMiddle);
                    // <----
                    volumeModifier = 1f - distance / (Main.screenWidth * 1.5f);
                }

                volumeModifier *= self.Style.Volume * self.Volume;
                switch (self.Style.Type)
                {
                    case SoundType.Sound:
                        volumeModifier *= Main.soundVolume;
                        break;
                    case SoundType.Ambient:
                        volumeModifier *= Main.ambientVolume;
                        if (Main.gameInactive)
                            volumeModifier = 0f;

                        break;
                    case SoundType.Music:
                        volumeModifier *= Main.musicVolume;
                        break;
                }

                volumeModifier = Clamp(volumeModifier, 0f, 1f);
                self.Sound.Volume = volumeModifier;
            }
        }
    }

    internal sealed class DisableAbyssLayer1PoisonHook : ModSystem
    {
        public static MethodInfo? CalUpdateBadLifeRegenMethod = typeof(CalamityPlayer).GetMethod("UpdateBadLifeRegen", Utilities.UniversalBindingFlags);
        public delegate void Orig_CalUpdateBadLifeRegenMethod(CalamityPlayer self);
        public static Hook? DisableAbyssLayer1Poison_Detour_Hook;

        public override void OnModLoad()
        {
            if (CalUpdateBadLifeRegenMethod != null)
            {
                DisableAbyssLayer1Poison_Detour_Hook = new(CalUpdateBadLifeRegenMethod, UpdateBadLifeRegen_Detour);
                DisableAbyssLayer1Poison_Detour_Hook?.Apply();
            }
        }

        public void UpdateBadLifeRegen_Detour(Orig_CalUpdateBadLifeRegenMethod orig, CalamityPlayer self)
        {
            bool abyssalDivingSuit = self.abyssalDivingSuit; // Save old value to reset after
            if (self.ZoneAbyssLayer1)
                self.abyssalDivingSuit = true; // Gives immunity to specifically Sulphur water poison, which we want to prevent if you're in Abyss Layer 1.
                                               // Does not affect anything else in UpdateBadLifeRegen, which means using it is fine.
            orig(self);
            self.abyssalDivingSuit = abyssalDivingSuit; // Reset value

        }
    }

    internal sealed class DisableAbyssEffectsDuringWyrm : ModSystem
    {
        public static MethodInfo? CalAbyssEffectsMethod = typeof(CalamityPlayer).GetMethod("AbyssEffects", LumUtils.UniversalBindingFlags);
        public delegate void Orig_CalAbyssEffectsMethod(CalamityPlayer self);
        public static Hook? DisableAbyssEffectsDuringWyrm_Detour_Hook;

        public override void OnModLoad()
        {
            if (CalAbyssEffectsMethod != null)
            {
                DisableAbyssEffectsDuringWyrm_Detour_Hook = new(CalAbyssEffectsMethod, AbyssEffects_Detour);
                DisableAbyssEffectsDuringWyrm_Detour_Hook?.Apply();
            }
        }

        private void AbyssEffects_Detour(Orig_CalAbyssEffectsMethod orig, CalamityPlayer self)
        {
            int wyrm = CalamityGlobalNPC.adultEidolonWyrmHead;
            if (InfernumMode.CanUseCustomAIs && wyrm >= 0 && wyrm < Main.maxNPCs)
            {
                NPC Wyrm = Main.npc[wyrm];
                if (Wyrm != null && Wyrm.active && Wyrm.type == ModContent.NPCType<PrimordialWyrmHead>() && Wyrm.WithinRange(self.Player.Center, 40000f))
                {
                    self.abyssBreathCD = 0;
                    self.abyssDeath = false;
                    return;
                }
            }
            orig(self);
        }
    }

    /*internal sealed class DisableDoGSkyHook : ModSystem
    {
        public static Hook? DisableDoGSky_Detour_Hook;

        public override void OnModLoad()
        {
            DisableDoGSky_Detour_Hook = new(CalDoGSkyUpdateDoGIndex, CalDoGSkyUpdateDoGIndex_Detour);
            DisableDoGSky_Detour_Hook?.Apply();
        }
        private bool CalDoGSkyUpdateDoGIndex_Detour(Orig_CalDoGSkyUpdateDoGIndex orig, DoGSky self)
        {
            if (InfernumMode.CanUseCustomAIs && !InfernumConfig.Instance.ReducedGraphicsConfig)
            {
                return false;
            }
            return orig(self);
        }
    }*/

    internal sealed class AvoidLabGardenIntersectHook : ModSystem
    {
        public static MethodInfo? CalShouldAvoidLocation = typeof(DraedonStructures).GetMethod("ShouldAvoidLocation", Utilities.UniversalBindingFlags);
        public delegate bool Orig_CalShouldAvoidLocation(Point placementPoint, bool careAboutLava = true);
        public static Hook? AvoidLabGardenIntersect_Detour_Hook;

        public override void OnModLoad()
        {
            if (CalShouldAvoidLocation != null)
            {
                AvoidLabGardenIntersect_Detour_Hook = new(CalShouldAvoidLocation, CalShouldAvoidLocation_Detour);
                AvoidLabGardenIntersect_Detour_Hook?.Apply();
            }
        }
        public static bool CalShouldAvoidLocation_Detour(Orig_CalShouldAvoidLocation orig, Point placementPoint, bool careAboutLava = true)
        {
            bool value = orig(placementPoint, careAboutLava);
            if (WorldSaveSystem.ProvidenceArena.Contains(placementPoint))
                value = true;
            return value;
        }
    }
    /*internal sealed class DisableGSSMessageHook : ModSystem
    {
        public static Hook? DisableGSSMessage_Detour_Hook;

        public override void OnModLoad()
        {
            DisableGSSMessage_Detour_Hook = new(CalDisplayLocalizedText, CalDisplayLocalizedText_Detour);
            DisableGSSMessage_Detour_Hook?.Apply();
        }
        public static void CalDisplayLocalizedText_Detour(Orig_CalDisplayLocalizedText orig, string key, Color? textColor = null)
        {
            if (key == "Mods.CalamityMod.Status.Progression.SandSharkText3")
                return;
            orig(key, textColor);
        }
    }*/
}
