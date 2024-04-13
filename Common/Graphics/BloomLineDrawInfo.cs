using Microsoft.Xna.Framework;

namespace InfernumMode.Common.Graphics
{
    public readonly struct BloomLineDrawInfo(float rotation, float width, float opacity, float bloom, Color main, Color darker, Vector2 scale, float bloomOpacity = 0.425f, float lightStrength = 5f)
    {
        public readonly float LineRotation = rotation;

        // Measure of how wide the direct line effect should be.
        // Usually this is something tiny like 0.003 since it's in radians and you only want a tiny angular area to be covered for it to be a line and not a cone.
        public readonly float WidthFactor = width;

        public readonly float Opacity = opacity;

        // 0-1 measure of how intense the bloom effects near the line and starting point should be.
        public readonly float BloomIntensity = bloom;

        public readonly Color MainColor = main;

        public readonly Color DarkerColor = darker;

        // The size of the invisible texture the line effect is mapped to in pixels.
        public readonly Vector2 Scale = scale;

        public readonly float BloomOpacity = bloomOpacity;

        public readonly float LightStrength = lightStrength;
    }
}
