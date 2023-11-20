using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace InfernumMode.Content.Rarities.Sparkles
{
    public class CodeSymbol : RaritySparkle
    {
        public int FrameOffset
        {
            get;
            set;
        }

        public CodeSymbol(int lifetime, float scale, Vector2 position, Vector2 velocity)
        {
            Lifetime = lifetime;
            Scale = 0;
            MaxScale = scale;
            Rotation = 0f;
            RotationSpeed = 0f;
            Position = position;
            Velocity = velocity;
            DrawColor = Color.LightCyan;
            Texture = RarityTextureRegistry.CodeSymbolTexture;
            BaseFrame = new(0, 10, 10, 10);
            FrameOffset = Main.rand.Next(50);
        }

        public override bool CustomUpdate()
        {
            Scale = MaxScale;
            Time++;
            return false;
        }

        public override bool CustomDraw(SpriteBatch spriteBatch, Vector2 drawPosition)
        {
            int animationFrame = ((int)Math.Floor(Time / ((float)Lifetime / 2)) + FrameOffset) % 11;
            Rectangle frame = new(0, BaseFrame.Value.Y * animationFrame, BaseFrame.Value.Width, BaseFrame.Value.Height);
            Color drawColor = DrawColor * Utils.GetLerpValue(0f, 12f, TimeLeft, true);
            drawColor.A = 0;
            spriteBatch.Draw(Texture, drawPosition, frame, drawColor, 0f, frame.Size() * 0.5f, Scale, 0, 0f);
            return false;
        }
    }
}
