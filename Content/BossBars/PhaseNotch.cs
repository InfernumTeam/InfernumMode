using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using static InfernumMode.Content.BossBars.BossBarManager;

namespace InfernumMode.Content.BossBars
{
    public class PhaseNotch
    {
        public class PhaseNotchSparkle
        {
            public int Time;
            public int Lifetime;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Scale;
            public float MaxScale;
            public float Rotation;
            public float RotationSpeed;
            public Color DrawColor;

            public int TimeLeft => Lifetime - Time;

            public PhaseNotchSparkle(Vector2 position, Vector2 velocity)
            {
                Position = position;
                Velocity = velocity;
                Time = 0;
                Lifetime = Main.rand.Next(30, 90);
                MaxScale = Main.rand.NextFloat(0.4f, 0.7f);
                Rotation = Main.rand.NextFloat(TwoPi);
                RotationSpeed = Main.rand.NextFloat(0.03f, 0.06f);
                DrawColor = Color.Lerp(Color.Gold, Color.DarkGoldenrod, Main.rand.NextFloat());
            }

            public void Draw(SpriteBatch spriteBatch)
            {
                Time++;

                Position += Velocity;
                Velocity *= 0.98f;

                Rotation += RotationSpeed;

                // Grow rapidly
                if (Time <= 20)
                    Scale = Lerp(0f, MaxScale, Time / 20f);
                // Shrink rapidly.
                if (TimeLeft <= 30)
                    Scale = Lerp(0f, MaxScale, TimeLeft / 30f);

                Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/Particles/ThinSparkle").Value;
                spriteBatch.Draw(texture, Position, null, DrawColor with { A = 0 }, Rotation, texture.Size() * 0.5f, Scale, SpriteEffects.None, 0f);
            }
        }

        public List<PhaseNotchSparkle> Sparkles;

        public bool PoppedOff;

        public int PoppingOffTimer;

        public float Opacity;

        public PhaseNotch()
        {
            Sparkles = new();
            PoppedOff = false;
            PoppingOffTimer = 0;
            Opacity = 0;
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 position, Color drawColor, bool shouldPopOff, bool barIsClosing)
        {
            bool createSparkles = false;
            bool inverseSparkles = false;
            if (shouldPopOff && !PoppedOff)
            {
                PoppedOff = true;
                createSparkles = true;
            }
            if (!shouldPopOff && PoppedOff)
            {
                PoppedOff = false;
                if (!barIsClosing)
                {
                    createSparkles = true;
                    inverseSparkles = true;
                }
            }

            Opacity = Clamp(Opacity + (-PoppedOff.ToDirectionInt() * 0.1f), 0f, 1f);

            if (PoppedOff || Opacity < 1f)
                spriteBatch.Draw(PhaseIndicatorNotch, position, null, drawColor, 0f, PhaseIndicatorNotch.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            if (Opacity > 0f)
                spriteBatch.Draw(PhaseIndicatorPlate, position, null, drawColor * Opacity, 0f, PhaseIndicatorPlate.Size() * 0.5f, 1f, SpriteEffects.None, 0f);

            if (createSparkles)
            {
                for (int i = 0; i < 50; i++)
                {
                    Vector2 sparklePosition = Main.rand.NextVector2FromRectangle(new Rectangle((int)(position.X - PhaseIndicatorNotch.Width * 0.5f), (int)(position.Y - PhaseIndicatorNotch.Height * 0.5f),
                        PhaseIndicatorNotch.Width, PhaseIndicatorNotch.Height));
                    
                    Vector2 velocity = inverseSparkles ? sparklePosition.DirectionTo(position) * Main.rand.NextFloat(0.2f, 0.5f) : 
                        Main.rand.NextFloat(TwoPi).ToRotationVector2() * Main.rand.NextFloat(0.2f, 0.5f);
                    
                    Sparkles.Add(new PhaseNotchSparkle(sparklePosition, velocity));
                }
            }

            foreach (var sparkle in Sparkles)
                sparkle.Draw(spriteBatch);

            Sparkles.RemoveAll(s => s.TimeLeft <= 0);
        }
    }
}
