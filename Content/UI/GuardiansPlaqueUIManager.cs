using InfernumMode.Content.Projectiles.Wayfinder;
using InfernumMode.Content.Tiles;
using InfernumMode.Core.GlobalInstances.Players;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.UI
{
    public static class GuardiansPlaqueUIManager
    {
        public static float Opacity
        {
            get;
            private set;
        } = 0f;

        public static float MaxDistance => 100f;

        public static float TextPadding => 170f;

        public static float TextAreaWidth => 800f;

        public static int YOffsetPerLine => 30;

        public const string TextToDrawPart1 = "Three disciples. One mind. One deity. One purpose. Tempered by the holy flames of Providence, an ";

        public const string TextToColor = "ancient artifact";

        public const string TextToDrawPart2 = " is crystalized, with the sole purpose of initiating the Ritual at the cliff of this Temple";

        public static Vector2 PlaqueWorldPosition => new(WorldSaveSystem.ProvidenceArena.X * 16f + 4360f, WorldSaveSystem.ProvidenceArena.Y * 16f + 1921f);

        public static Texture2D BackgroundTexture => ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/UI/ChatBackgroundGreyscale").Value;

        public static Player Player => Main.LocalPlayer;

        public static bool ShouldDraw => Player.Infernum_UI().DrawPlaqueUI;

        public static void Draw(SpriteBatch spriteBatch)
        {
            if (!ShouldDraw)
            {
                // Keep drawing while the opacity fades out.
                Opacity = MathHelper.Clamp(Opacity - 0.2f, 0f, 1f);
                if (Opacity == 0f)
                    return;
            }
            else
                // Increase the opacity.
                Opacity = MathHelper.Clamp(Opacity + 0.2f, 0f, 1f);

            // If far enough away, close.
            if (Player.Distance(PlaqueWorldPosition) > MaxDistance && ShouldDraw)
                CloseUI();

            string combinedLine = TextToDrawPart1 + TextToColor + TextToDrawPart2;
            string[] lines = Utils.WordwrapString(combinedLine, FontAssets.MouseText.Value, 460, 4, out var lineCount);

            spriteBatch.Draw(BackgroundTexture, new Vector2(Main.screenWidth / 2 - BackgroundTexture.Width * 0.5f, 100f), new(0, 0, BackgroundTexture.Width, (lineCount + 1) * YOffsetPerLine), WayfinderSymbol.Colors[2] * Opacity, 0f, Vector2.Zero, 1f, 0, 0f);
            spriteBatch.Draw(BackgroundTexture, new Vector2(Main.screenWidth / 2 - BackgroundTexture.Width * 0.5f, 100 + (lineCount + 1) * YOffsetPerLine), new(0, BackgroundTexture.Height - YOffsetPerLine, BackgroundTexture.Width, 30), WayfinderSymbol.Colors[2] * Opacity, 0f, Vector2.Zero, 1f, 0, 0f);


            for (int i = 0; i < lineCount + 1; i++)
            { 
                if (lines[i] != null)
                    Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, lines[i], TextPadding + (int)(Main.screenWidth - TextAreaWidth) / 2, 120 + i * YOffsetPerLine, WayfinderSymbol.Colors[0] * Opacity, Color.Black * Opacity, Vector2.Zero);
            }
        }

        private static void CloseUI()
        {
            SoundEngine.PlaySound(SoundID.MenuClose);
            Player.Infernum_UI().DrawPlaqueUI = false;
        }
    }
}
