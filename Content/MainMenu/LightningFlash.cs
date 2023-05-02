using InfernumMode.Assets.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;

namespace InfernumMode.Content.MainMenu
{
    public static class LightningFlash
    {
        private static int timeLeft;

        internal static int SoundTime;
        internal static float DistanceModifier;

        public static int TimeLeft
        {
            get => timeLeft;
            set
            {
                if (value < 0)
                    value = 0;

                timeLeft = value;
            }
        }

        internal static void Draw(Vector2 drawOffset, float scale)
        {
            if (timeLeft > 0)
            {
                timeLeft--;
                float opacity = timeLeft >= 30 ? 1 - (timeLeft - 30f) / 5f : (timeLeft - 5f) / 30f;
                // Give the opacity a slight random flicker.
                opacity *= Main.rand.NextFloat(0.95f, 1.05f);
                Main.spriteBatch.Draw(InfernumMainMenu.BackgroundTexture, drawOffset, null, Color.LightGray with { A = 0 } * opacity * DistanceModifier, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

                // Don't play the sound if tabbed out because it is super annoying.
                if (timeLeft == SoundTime && Main.instance.IsActive)
                {
                    SoundStyle thunder = Main.rand.Next(3) switch
                    {
                        0 => InfernumSoundRegistry.ThunderRumble,
                        1 => InfernumSoundRegistry.ThunderRumble2,
                        _ => InfernumSoundRegistry.ThunderRumble3
                    };
                    SoundEngine.PlaySound(thunder with { Volume = 0.75f * DistanceModifier, PitchVariance = 0.4f });
                }
            }
        }
    }
}
