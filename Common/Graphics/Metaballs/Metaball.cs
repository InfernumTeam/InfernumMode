using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics.Metaballs
{
    public class Metaball
    {
        public static Texture2D MetaballTexture => ModContent.Request<Texture2D>("InfernumMode/Common/Graphics/Metaballs/Metaball").Value;

        public static Texture2D LightingTexture => ModContent.Request<Texture2D>("InfernumMode/Common/Graphics/Metaballs/MetaballSolid").Value;

        public int Timer;

        public float Rotation;

        public float Size;

        public float GravityStrength;

        public bool IgnoreTiles;

        public bool NaturallyDie;

        public Vector2 Center;

        public Vector2 Velocity;

        public Vector2 Scale;

        public Metaball(Vector2 position, Vector2 velocity, float size, bool ignoreTiles = false, float gravityStrength = 0f)
        {
            Center = position;
            Velocity = velocity;
            Size = size;
            IgnoreTiles = ignoreTiles;
            GravityStrength = gravityStrength;
            Scale = Vector2.One;
        }

        public bool CollidingWithTiles()
        {
            if (IgnoreTiles)
                return false;

            if (Collision.SolidTiles(Center, (int)Size / 2, (int)Size / 2))
                return true;

            return false;
        }

        public void DrawNormal(SpriteBatch spriteBatch, Color drawColor)
        {
            spriteBatch.Draw(MetaballTexture, Center - Main.screenPosition, null, drawColor, Rotation, MetaballTexture.Size() * 0.5f, new Vector2(Size * 2f) * Scale / MetaballTexture.Size(), SpriteEffects.None, 0f);
        }

        public void DrawForLighting(SpriteBatch spriteBatch, Color? lightingOverride = null)
        {
            Color color = lightingOverride ?? Lighting.GetColor(Center.ToTileCoordinates());
            spriteBatch.Draw(LightingTexture, Center - Main.screenPosition, null, color, Rotation, MetaballTexture.Size() * 0.5f, new Vector2(Size * 2f) * Scale / LightingTexture.Size(), SpriteEffects.None, 0f);
        }
    }
}
