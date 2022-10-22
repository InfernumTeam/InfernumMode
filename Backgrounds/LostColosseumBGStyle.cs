using Terraria.ModLoader;

namespace InfernumMode.Backgrounds
{
    public class LostColosseumBGStyle : ModUndergroundBackgroundStyle
    {
        public override void FillTextureArray(int[] textureSlots)
        {
            for (int i = 0; i <= 3; i++)
                textureSlots[i] = BackgroundTextureLoader.GetBackgroundSlot("CalamityMod/Backgrounds/AstralUG" + i.ToString());
        }
    }
}
