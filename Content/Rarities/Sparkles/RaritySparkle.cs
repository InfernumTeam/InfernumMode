using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace InfernumMode.Content.Rarities.Sparkles
{
    public class RaritySparkle
    {
        public int Time;
        public int Lifetime;
        public float MaxScale;
        public float Scale;
        public float Rotation;
        public float RotationSpeed;
        public Vector2 Position;
        public Vector2 Velocity;
        public Color DrawColor;
        public Texture2D Texture;
        public Rectangle? BaseFrame;
        public bool UseSingleFrame;

        public float TimeLeft => Lifetime - Time;

        public float LifetimeRatio => (float)Time / Lifetime;

        public void Update()
        {
            Position += Velocity;

            if (!CustomUpdate())
            {
                Time++;
                return;
            }

            // Grow rapidly
            if (Time <= 20)
                Scale = Lerp(0f, MaxScale, Time / 20f);

            // Shrink rapidly.
            if (TimeLeft <= 20)
                Scale = Lerp(0f, MaxScale, (float)TimeLeft / 20f);

            // Increase the rotation and time.
            Rotation += RotationSpeed;
            Time++;
        }

        /// <summary>
        /// Run custom update code here. This runs after the sparkles position is updated. Return false to stop the rest of the base update from running.
        /// </summary>
        /// <returns></returns>
        public virtual bool CustomUpdate() => true;

        /// <summary>
        /// Run custom drawcode here. Return false to stop base drawing from running.
        /// </summary>
        /// <param name="spriteBatch">The SpriteBatch</param>
        /// <param name="drawPosition">The position to draw.</param>
        /// <returns></returns>
        public virtual bool CustomDraw(SpriteBatch spriteBatch, Vector2 drawPosition) => true;

        public void Draw(SpriteBatch spriteBatch, Vector2 drawPosition)
        {
            if (!CustomDraw(spriteBatch, drawPosition))
                return;

            Rectangle? frame = null;
            if (BaseFrame.HasValue)
            {
                if (UseSingleFrame)
                    frame = BaseFrame.Value;
                else
                {
                    int animationFrame = (int)Math.Floor(Time / ((float)Lifetime / 6));
                    frame = new Rectangle(0, BaseFrame.Value.Y * animationFrame, BaseFrame.Value.Width, BaseFrame.Value.Height);
                }
            }
            Color drawColor = DrawColor;
            drawColor.A = 0;
            spriteBatch.Draw(Texture, drawPosition, frame, drawColor, Rotation, !frame.HasValue ? Texture.Size() * 0.5f : frame.Value.Size() * 0.5f, Scale, SpriteEffects.None, 0f);
        }
    }

}
