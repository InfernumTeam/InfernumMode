using CalamityMod;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using static InfernumMode.ILEditingStuff.HookManager;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;
using ReLogic.Content;
using Microsoft.Xna.Framework;
using Terraria.UI;
using System.Collections.Generic;
using InfernumMode.Achievements;
using Terraria.Audio;
using Terraria.ID;
using Terraria.GameInput;
using InfernumMode.Systems;
using Terraria.GameContent;

namespace InfernumMode.ILEditingStuff
{
    public class PermitOldDukeRainHook : IHookEdit
    {
        internal static void PermitODRain(ILContext il)
        {
            ILCursor cursor = new(il);

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

        public void Load() => CalamityWorldPostUpdate += PermitODRain;

        public void Unload() => CalamityWorldPostUpdate -= PermitODRain;
    }

    public class NerfShellfishStaffDebuffHook : IHookEdit
    {
        internal static void NerfShellfishStaff(ILContext il)
        {
            ILCursor cursor = new(il);

            for (int j = 0; j < 2; j++)
            {
                if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchLdcI4(250)))
                    return;
            }

            cursor.Remove();
            cursor.Emit(OpCodes.Ldc_I4, 150);

            if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchLdcI4(50)))
                return;

            cursor.Remove();
            cursor.Emit(OpCodes.Ldc_I4, 30);
        }

        public void Load() => CalamityNPCLifeRegen += NerfShellfishStaff;

        public void Unload() => CalamityNPCLifeRegen -= NerfShellfishStaff;
    }
    
    public class AchievementMenuUIHookEdit : IHookEdit
    {
        private static bool justHovered;

        public void Load()
        {
            On.Terraria.UI.AchievementAdvisor.DrawOneAchievement += AchievementAdvisor_DrawOneAchievement;
        }
        
        public void Unload()
        {
            On.Terraria.UI.AchievementAdvisor.DrawOneAchievement -= AchievementAdvisor_DrawOneAchievement;
        }

        private void AchievementAdvisor_DrawOneAchievement(On.Terraria.UI.AchievementAdvisor.orig_DrawOneAchievement orig, AchievementAdvisor self, SpriteBatch spriteBatch, Vector2 position, bool large)
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
            Main.hoverItemName = "Open Death Wishes";

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
            Texture2D achievementTexture = ModContent.Request<Texture2D>("InfernumMode/Achievements/Textures/Achievement", AssetRequestMode.AsyncLoad).Value;
            Texture2D borderTexture = ModContent.Request<Texture2D>("InfernumMode/Achievements/Textures/InfernumAchievement_Border", AssetRequestMode.AsyncLoad).Value;
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
            Main.InGameUI.SetState(UIRenderingSystem.achievementUIManager);
        }
    }
}