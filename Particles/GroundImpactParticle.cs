using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Particles
{
    public class GroundImpactParticle : Particle
    {
        public float Opacity => Utils.InverseLerp(0f, 0.12f, LifetimeCompletion, true) * Utils.InverseLerp(1f, 0.34f, LifetimeCompletion, true);

        public override string Texture => "InfernumMode/ExtraTextures/HollowCircleSoftEdge";

        public override bool UseAdditiveBlend => true;

        public override bool UseCustomDraw => true;

        public override bool SetLifetime => true;

        public GroundImpactParticle(Vector2 position, Vector2 direction, Color color, int lifeTime, float scale)
        {
            Position = position;
            Velocity = Vector2.Zero;
            Color = color;
            Scale = scale;
            Lifetime = lifeTime;
            Rotation = direction.ToRotation();
        }

        public override void Update() => Scale = (Scale + 0.075f) * 1.02f;

        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Texture2D texture = ModContent.GetTexture(Texture);
            Vector2 scale = new Vector2(0.24f, 1f) * Scale;
            Vector2 drawPosition = Position - Main.screenPosition;
            spriteBatch.Draw(texture, drawPosition, null, Color * Opacity, Rotation, texture.Size() * 0.5f, scale, 0, 0f);
        }
    }
}
