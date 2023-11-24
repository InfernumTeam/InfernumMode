using Microsoft.Xna.Framework;

namespace InfernumMode.Common.Graphics
{
    public readonly struct BloomLineDrawInfo
    {
        public readonly float LineRotation;

        // Measure of how wide the direct line effect should be.
        // Usually this is something tiny like 0.003 since it's in radians and you only want a tiny angular area to be covered for it to be a line and not a cone.
        public readonly float WidthFactor;

        public readonly float Opacity;

        // 0-1 measure of how intense the bloom effects near the line and starting point should be.
        public readonly float BloomIntensity;

        public readonly Color MainColor;

        public readonly Color DarkerColor;

        // The size of the invisible texture the line effect is mapped to in pixels.
        public readonly Vector2 Scale;

        public readonly float BloomOpacity = 0.425f;

        public readonly float LightStrength = 5f;

        public BloomLineDrawInfo(float rotation, float width, float opacity, float bloom, Color main, Color darker, Vector2 scale, float bloomOpacity = 0.425f, float lightStrength = 5f)
        {
            LineRotation = rotation;
            WidthFactor = width;
            Opacity = opacity;
            BloomIntensity = bloom;
            MainColor = main;
            DarkerColor = darker;
            Scale = scale;
            BloomOpacity = bloomOpacity;
            LightStrength = lightStrength;
        }
    }
}
