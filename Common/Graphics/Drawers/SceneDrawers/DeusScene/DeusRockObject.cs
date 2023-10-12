using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics.Drawers.SceneDrawers.DeusScene
{
    public class DeusRockObject : BaseSceneObject
    {
        public DeusRockObject(Vector2 position, Vector2 velocity, Vector2 scale, int lifetime, float depth, float rotation, float rotationSpeed)
            : base(position, velocity, scale, lifetime, depth, rotation, rotationSpeed)
        {
            Variant = Main.rand.Next(0, 12);
            if (Main.rand.NextBool(200))
                Variant = 12;

            if (RockTextures == null)
            {
                RockTextures = new Texture2D[13];
                for (int i = 0; i < RockTextures.Length; i++)
                    RockTextures[i] = ModContent.Request<Texture2D>($"InfernumMode/Common/Graphics/Drawers/SceneDrawers/DeusScene/Textures/AstrumChunk{i}", AssetRequestMode.ImmediateLoad).Value;
            }
        }

        public static Texture2D[] RockTextures
        {
            get;
            private set;
        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 drawPosition, Vector2 screenCenter, Vector2 scale)
        {
            Texture2D texture = RockTextures[Variant];

            Vector2 afterimagePosition = drawPosition;
            int afterimageAmount = (int)(15f * Utils.GetLerpValue(0f, 1.5f, Velocity.Length(), true));

            float growTime = 40;
            float extraScale = Utils.GetLerpValue(0f, growTime, Timer, true) * Utils.GetLerpValue(Lifetime, Lifetime - growTime, Timer, true);

            if (Timer <= growTime)
                extraScale = Utilities.EaseOutBounce(extraScale);
            else
                extraScale = Utilities.EaseInOutCubic(extraScale);

            for (int i = 0; i < afterimageAmount; i++)
            {
                float fade = (1f - (float)i / afterimageAmount);
                Color afterimageColor = Color.White with { A = 0 } * Scale.X * fade * 0.2f;
                spriteBatch.Draw(texture, afterimagePosition, null, afterimageColor, Rotation, texture.Size() * 0.5f, Scale * extraScale, SpriteEffects.None, 0f);

                // This is done so that it scales properly from world position to screen position.
                Vector2 positionModifier = Position - Velocity * (i + 1f);
                afterimagePosition = (positionModifier - screenCenter) * scale + screenCenter - Main.screenPosition;
            }

            spriteBatch.Draw(texture, drawPosition, null, Color.White * Pow(Scale.X, 0.5f), Rotation, texture.Size() * 0.5f, Scale * extraScale, SpriteEffects.None, 0f);
        }
    }
}
