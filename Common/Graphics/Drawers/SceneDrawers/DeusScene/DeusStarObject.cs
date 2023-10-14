using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;
using Terraria;
using ReLogic.Content;

namespace InfernumMode.Common.Graphics.Drawers.SceneDrawers.DeusScene
{
    public class DeusStarObject : BaseSceneObject
    {
        public static Texture2D BackgroundStarTexture
        {
            get;
            private set;
        }

        public DeusStarObject(Vector2 position, Vector2 velocity, Vector2 scale, int lifetime, float depth, float rotation, float rotationSpeed)
            : base(position, velocity, scale, lifetime, depth, rotation, rotationSpeed)
        {
            BackgroundStarTexture ??= ModContent.Request<Texture2D>("InfernumMode/Common/Graphics/Drawers/SceneDrawers/DeusScene/Textures/AstrumStar", AssetRequestMode.ImmediateLoad).Value;

        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 drawPosition, Vector2 screenCenter, Vector2 scale)
        {
            Color mainColor = Color.Lerp(Color.Orange, Color.Cyan, 1f - RandomSeed) with { A = 0 };
            Color bloomColor = Color.Lerp(mainColor, Color.White, 0.5f) with { A = 0 } ;
            Vector2 scale2 = Scale * Lerp(1f, 1.2f, (1f + Sin(PI * (Main.GlobalTimeWrappedHourly * 12f * RandomSeed))) * 0.5f);
            float opacity = Utils.GetLerpValue(0, 30, Timer, true) * Utils.GetLerpValue(Lifetime, Lifetime - 30, Timer, true);
            opacity *= Pow(scale.X, 0.75f);
            Texture2D starTexture = InfernumTextureRegistry.Gleam.Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;

            // The + shape stars typically have is actually caused by lens flare from what is viewing the star (such as eyes, or a camera lens). Therefore, it cannot rotate.
            spriteBatch.Draw(BackgroundStarTexture, drawPosition, null, bloomColor * opacity, PiOver2, BackgroundStarTexture.Size() * 0.5f, scale2 * 1.2f, SpriteEffects.None, 0f);
            spriteBatch.Draw(bloom, drawPosition, null, mainColor * opacity * 0.6f, Rotation, bloom.Size() * 0.5f, scale2 * 0.6f, SpriteEffects.None, 0f);
            spriteBatch.Draw(starTexture, drawPosition, null, bloomColor * opacity, 0f, starTexture.Size() * 0.5f, scale2, SpriteEffects.None, 0f);
            spriteBatch.Draw(starTexture, drawPosition, null, bloomColor * opacity, PiOver2, starTexture.Size() * 0.5f, scale2 * 1.2f, SpriteEffects.None, 0f);

            spriteBatch.Draw(bloom, drawPosition, null, mainColor * opacity * 0.6f, Rotation, bloom.Size() * 0.5f, scale2 * 0.3f, SpriteEffects.None, 0f);
            spriteBatch.Draw(starTexture, drawPosition, null, bloomColor * opacity, 0f, starTexture.Size() * 0.5f, scale2 * 0.5f, SpriteEffects.None, 0f);
            spriteBatch.Draw(starTexture, drawPosition, null, bloomColor * opacity, PiOver2, starTexture.Size() * 0.5f, scale2 * 0.6f, SpriteEffects.None, 0f);
        }
    }
}
