using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics.Particles
{
    public class ProfanedRockParticle : Particle
    {
        public Color OriginalColor;

        public float RotationSpeed;

        public float Opacity;

        public bool Gravity;

        public bool FadeIn;

        public override int FrameVariants => 6;

        public override bool SetLifetime => true;

        public override bool UseCustomDraw => true;

        public override string Texture => "InfernumMode/Common/Graphics/Particles/ProfanedRockParticle";

        public ProfanedRockParticle(Vector2 position, Vector2 velocity, Color color, float scale, int lifeTime, float rotationSpeed = 0.2f, bool gravity = true, bool fadeIn = false)
        {
            Position = position;
            Velocity = velocity;
            Color = OriginalColor = color;
            Scale = scale;
            Lifetime = lifeTime;
            Rotation = Main.rand.NextFloat(TwoPi); ;
            RotationSpeed = rotationSpeed;
            Opacity = fadeIn ? 0f : 1f;
            Variant = Main.rand.Next(6);
            Gravity = gravity;
            FadeIn = fadeIn;
        }

        public override void Update()
        {
            Velocity *= 0.99f;
            if (Gravity)
            {
                Velocity.X *= 0.94f;
                Velocity.Y += 0.1f;
            }

            Rotation += RotationSpeed * (Velocity.X > 0 ? 1f : -1f);

            if (FadeIn && Time <= 15f)
                Opacity = Lerp(0f, 1f, Time / 15f);

            if (LifetimeCompletion >= 0.8f)
            {
                Color = OriginalColor * (1 - ((LifetimeCompletion - 0.8f) / 0.2f));
            }
        }

        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = texture.Frame(1, FrameVariants, 0, Variant);
            spriteBatch.Draw(texture, Position - Main.screenPosition, frame, Color * Opacity, Rotation, frame.Size() * 0.5f, Scale, SpriteEffects.None, 0f);
        }
    }
}
