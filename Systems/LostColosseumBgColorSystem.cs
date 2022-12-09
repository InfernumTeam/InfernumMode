using InfernumMode.Subworlds;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Systems
{
    public class LostColosseumBgColorSystem : ModSystem
    {
        public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
        {
            if (!SubworldSystem.IsActive<LostColosseum>())
                return;

            backgroundColor = Color.Lerp(new(229, 195, 146), new(184, 143, 164), LostColosseum.SunsetInterpolant);
        }
    }
}