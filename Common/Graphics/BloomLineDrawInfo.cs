using Microsoft.Xna.Framework;

namespace InfernumMode.Common.Graphics
{
    public struct BloomLineDrawInfo
    {
        public float LineRotation
        {
            get;
            set;
        }

        // Measure of how wide the direct line effect should be.
        // Usually this is something tiny like 0.003 since it's in radians and you only want a tiny angular area to be covered for it to be a line and not a cone.
        public float WidthFactor
        {
            get;
            set;
        }

        public float Opacity
        {
            get;
            set;
        }

        // 0-1 measure of how intense the bloom effects near the line and starting point should be.
        public float BloomIntensity
        {
            get;
            set;
        }

        public Color MainColor
        {
            get;
            set;
        }

        public Color DarkerColor
        {
            get;
            set;
        }

        // The size of the invisible texture the line effect is mapped to in pixels.
        public Vector2 Scale
        {
            get;
            set;
        }

        public float BloomOpacity
        {
            get;
            set;
        } = 0.425f;

        public float LightStrength
        {
            get;
            set;
        } = 5f;

        public BloomLineDrawInfo(float rotation, float width, float opacity, float bloom, Color main, Color darker, float bloomOpacity = 0.425f, float lightStrength = 5f)
        {
            LineRotation = rotation;
            WidthFactor = width;
            Opacity = opacity;
            BloomIntensity = bloom;
            MainColor = main;
            DarkerColor = darker;
            LightStrength = lightStrength;
            BloomOpacity = bloomOpacity;
        }
    }
}