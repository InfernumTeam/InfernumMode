using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Particles
{
    public class ElectricExplosionRing : Particle
    {
        public Color[] Colors;

        public float ScaleExpansionFactor = 1f;

        public override bool UseAdditiveBlend => true;
        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;
        public override string Texture => "InfernumMode/ExtraTextures/DistortedBloomRing";

        public ElectricExplosionRing(Vector2 position, Vector2 velocity, Color[] colors, float scale, int lifeTime, float scaleExpansionFactor = 1f)
        {
            Position = position;
            Velocity = velocity;
            Colors = colors;
            Scale = scale;
            Lifetime = lifeTime;
            ScaleExpansionFactor = scaleExpansionFactor;
        }

        public override void Update()
        {
            float rotationSlowdownFactor = Utilities.Remap(Scale, 3f, 6f, 1f, 0.1f);
            Rotation += MathHelper.Pi * rotationSlowdownFactor / 64f;
            if (Scale > 2f)
                Scale += ScaleExpansionFactor * 0.018f;
        }

        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            int ringCount = (int)(Scale * 24f);
            if (ringCount > 100)
                ringCount = 100;

            float innerRingScaleFactor = MathHelper.Lerp(0.3f, 0f, Scale / (Scale + 1f));
            float fadeout = Utils.InverseLerp(0f, 24f, Time, true);
            float scaleFadeout = fadeout;
            float opacityFadeout = fadeout * Utils.InverseLerp(1f, 0.7f, LifetimeCompletion, true);
            if (Scale < 2f)
                scaleFadeout *= Utils.InverseLerp(1f, 0.7f, LifetimeCompletion, true);

            Texture2D texture = ModContent.GetTexture(Texture);
            for (int i = 0; i < ringCount; i++)
            {
                float scale = MathHelper.Lerp(1f, innerRingScaleFactor, i / (float)(ringCount - 1f)) * scaleFadeout * Scale;
                Color color = CalamityUtils.MulticolorLerp((i / (float)(ringCount - 1f) + Main.GlobalTime * 0.7f) % 1f, Colors) * opacityFadeout * 0.45f;
                float rotation = Rotation * MathHelper.Lerp(0.5f, 1f, i / (float)(ringCount - 1f)) * (i % 2 == 0).ToDirectionInt();
                Vector2 drawPosition = Position - Main.screenPosition;
                spriteBatch.Draw(texture, drawPosition, null, color, rotation, texture.Size() * 0.5f, scale, 0, 0f);
            }
        }
    }
}
