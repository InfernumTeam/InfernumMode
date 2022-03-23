using Microsoft.Xna.Framework;
using System;

namespace InfernumMode.BossIntroScreens
{
    public struct TextColorData
    {
        public Func<float, Color> ColorSelectionFunction;
        public TextColorData(Color color)
        {
            ColorSelectionFunction = _ => color;
        }

        public TextColorData(Func<float, Color> colorSelectionFunction)
        {
            ColorSelectionFunction = colorSelectionFunction;
        }

        public Color Calculate(float completionRatio) => ColorSelectionFunction(completionRatio);

        public static implicit operator TextColorData(Color c) => new TextColorData(c);
    }
}
