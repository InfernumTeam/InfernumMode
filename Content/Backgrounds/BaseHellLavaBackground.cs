using Microsoft.Xna.Framework;

namespace InfernumMode.Content.Backgrounds
{
    public abstract class BaseHellLavaBackground
    {
        public abstract bool IsActive { get; }

        public abstract Color LavaColor { get; }
    }
}
