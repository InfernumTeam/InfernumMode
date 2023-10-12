using CalamityMod;
using CalamityMod.NPCs.ExoMechs;
using CalamityMod.UI.DraedonSummoning;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using static InfernumMode.Core.ILEditingStuff.HookManager;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class CustomExoMechSelectionSystem : ModSystem
    {
        public static ExoMech? PrimaryMechToSummon
        {
            get;
            set;
        }

        public static ExoMech? DestroyerTypeToSummon
        {
            get;
            set;
        }

        internal static void DrawSelectionUI(ILContext context)
        {
            ILCursor cursor = new(context);
            cursor.EmitDelegate(DrawWrapper);
            cursor.Emit(OpCodes.Ret);
        }

        public static void DrawWrapper()
        {
            Vector2 drawAreaVerticalOffset = Vector2.UnitY * 105f;
            Vector2 baseDrawPosition = Main.LocalPlayer.Top + drawAreaVerticalOffset - Main.screenPosition;
            Vector2 destroyerIconDrawOffset = new(-78f, -124f);
            Vector2 primeIconDrawOffset = new(0f, -140f);
            Vector2 twinsIconDrawOffset = new(78f, -124f);

            if (InfernumMode.CanUseCustomAIs)
            {
                bool hoveringOverAnyIcon = false;
                for (int i = 0; i < 3; i++)
                {
                    Vector2 iconDrawOffset = new(Lerp(-92f, 92f, i / 2f), -145f);
                    hoveringOverAnyIcon |= HandleInteractionWithButton(baseDrawPosition + iconDrawOffset, i + 1, PrimaryMechToSummon == null);
                }

                // Reset the selections if the player clicks on something other than the icons.
                if (!hoveringOverAnyIcon && Main.mouseLeft && Main.mouseLeftRelease)
                    PrimaryMechToSummon = DestroyerTypeToSummon = null;
                if (!hoveringOverAnyIcon)
                    ExoMechSelectionUI.HoverSoundMechType = null;

                var font = FontAssets.MouseText.Value;
                string pickTwoText = Utilities.GetLocalization("ExoMechsFight.PickTwoText").Value;
                Vector2 pickToDrawPosition = baseDrawPosition - Vector2.UnitY * 250f;
                foreach (string line in Utils.WordwrapString(pickTwoText, font, 600, 10, out _))
                {
                    if (string.IsNullOrEmpty(line))
                        continue;

                    Vector2 textArea = font.MeasureString(line);
                    ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, line, pickToDrawPosition - Vector2.UnitX * textArea * 0.5f, Draedon.TextColorEdgy, 0f, textArea * new Vector2(0f, 0.5f), Vector2.One);
                    pickToDrawPosition.Y += 50f;
                }
                return;
            }

            ExoMechSelectionUI.HandleInteractionWithButton(baseDrawPosition + destroyerIconDrawOffset, ExoMech.Destroyer);
            ExoMechSelectionUI.HandleInteractionWithButton(baseDrawPosition + primeIconDrawOffset, ExoMech.Prime);
            ExoMechSelectionUI.HandleInteractionWithButton(baseDrawPosition + twinsIconDrawOffset, ExoMech.Twins);
        }

        public static bool HandleInteractionWithButton(Vector2 drawPosition, int exoMech, bool selectingPrimaryMech)
        {
            float iconScale;
            string description;
            Texture2D iconMechTexture;
            SoundStyle hoverSound;

            switch (exoMech)
            {
                case 1:
                    iconScale = ExoMechSelectionUI.DestroyerIconScale;
                    iconMechTexture = ModContent.Request<Texture2D>("CalamityMod/UI/DraedonSummoning/HeadIcon_THanos").Value;
                    description = Utilities.GetLocalization("ExoMechsFight.ThanatosDesc").Value;
                    hoverSound = ExoMechSelectionUI.ThanatosHoverSound;
                    break;
                case 2:
                    iconScale = ExoMechSelectionUI.PrimeIconScale;
                    iconMechTexture = ModContent.Request<Texture2D>("CalamityMod/UI/DraedonSummoning/HeadIcon_Ares").Value;
                    description = Utilities.GetLocalization("ExoMechsFight.AresDesc").Value;
                    hoverSound = ExoMechSelectionUI.AresHoverSound;
                    break;
                case 3:
                default:
                    iconScale = ExoMechSelectionUI.TwinsIconScale;
                    iconMechTexture = ModContent.Request<Texture2D>("CalamityMod/UI/DraedonSummoning/HeadIcon_ArtemisApollo").Value;
                    description = Utilities.GetLocalization("ExoMechsFight.ArtemisApolloDesc").Value;
                    hoverSound = ExoMechSelectionUI.TwinsHoverSound;
                    break;
            }

            // Check for mouse collision/clicks.
            Rectangle clickArea = Utils.CenteredRectangle(drawPosition, iconMechTexture.Size() * iconScale * 0.9f);

            // Check if the mouse is hovering over the contact button area.
            bool alreadySelected = (int)(PrimaryMechToSummon ?? (ExoMech)999) == exoMech || (int)(DestroyerTypeToSummon ?? (ExoMech)999) == exoMech;
            bool hoveringOverIcon = ExoMechSelectionUI.MouseScreenArea.Intersects(clickArea);
            if (hoveringOverIcon)
            {
                // If so, cause the button to inflate a little bit.
                iconScale = Clamp(iconScale + 0.0375f, 1f, 1.35f);

                // Play the hover sound.
                if (ExoMechSelectionUI.HoverSoundMechType != (ExoMech)exoMech)
                {
                    ExoMechSelectionUI.HoverSoundMechType = (ExoMech)exoMech;
                    SoundEngine.PlaySound(hoverSound with
                    {
                        Volume = 1.5f
                    });
                }

                // Make the selection known if a click is done and the icon isn't already in use.
                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    if (selectingPrimaryMech)
                        PrimaryMechToSummon = alreadySelected ? null : (ExoMech)exoMech;
                    else
                        DestroyerTypeToSummon = alreadySelected ? null : (ExoMech)exoMech;

                    int draedon = NPC.FindFirstNPC(ModContent.NPCType<Draedon>());
                    if (draedon != -1 && PrimaryMechToSummon.HasValue && DestroyerTypeToSummon.HasValue)
                    {
                        Main.npc[draedon].ai[0] = Draedon.ExoMechChooseDelay + 8f;
                        Main.npc[draedon].netSpam = 0;
                        Main.npc[draedon].netUpdate = true;

                        DraedonBehaviorOverride.SummonExoMech(Main.player[Main.npc[draedon].target]);
                        PrimaryMechToSummon = DestroyerTypeToSummon = null;
                    }
                }
                Main.blockMouse = Main.LocalPlayer.mouseInterface = true;
            }

            // Otherwise, if not hovering and not selected, cause the button to deflate back to its normal size.
            else if (!alreadySelected)
                iconScale = Clamp(iconScale - 0.05f, 1f, 1.2f);

            // Draw the icon with the new scale.
            Color iconColor = alreadySelected ? Color.Black * 0.8f : Color.White;
            if (alreadySelected)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector2 drawOffset = -Vector2.UnitY.RotatedBy(TwoPi * i / 4f) * iconScale * 2f;
                    Main.spriteBatch.Draw(iconMechTexture, drawPosition + drawOffset, null, Color.Red with
                    {
                        A = 80
                    }, 0f, iconMechTexture.Size() * 0.5f, iconScale, SpriteEffects.None, 0f);
                }
            }

            Main.spriteBatch.Draw(iconMechTexture, drawPosition, null, iconColor, 0f, iconMechTexture.Size() * 0.5f, iconScale, SpriteEffects.None, 0f);

            // Draw the descrption if hovering over the icon.
            if (hoveringOverIcon)
            {
                drawPosition.X -= FontAssets.MouseText.Value.MeasureString(description).X * 0.5f;
                drawPosition.Y += 36f;
                Utils.DrawBorderStringFourWay(Main.spriteBatch, FontAssets.MouseText.Value, description, drawPosition.X, drawPosition.Y, ExoMechSelectionUI.HoverTextColor, Color.Black, Vector2.Zero, 1f);
            }

            // And update to reflect the new scale.
            switch (exoMech)
            {
                case 1:
                    ExoMechSelectionUI.DestroyerIconScale = iconScale;
                    break;
                case 2:
                    ExoMechSelectionUI.PrimeIconScale = iconScale;
                    break;
                case 3:
                    ExoMechSelectionUI.TwinsIconScale = iconScale;
                    break;
            }
            return hoveringOverIcon;
        }

        public override void OnModLoad() => ExoMechSelectionUIDraw += DrawSelectionUI;

        public override void Unload() => ExoMechSelectionUIDraw -= DrawSelectionUI;
    }
}
