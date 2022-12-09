using Terraria;
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
    
    public class LostColosseumSurfaceBGStyle : ModSurfaceBackgroundStyle
    {
        public override int ChooseFarTexture() => BackgroundTextureLoader.GetBackgroundSlot("InfernumMode/Backgrounds/LostColosseumBGObjects");

        public override int ChooseMiddleTexture() => BackgroundTextureLoader.GetBackgroundSlot("InfernumMode/Backgrounds/LostColosseumBGObjects");

        public override int ChooseCloseTexture(ref float scale, ref double parallax, ref float a, ref float b)
        {
            b = 700f;
            return BackgroundTextureLoader.GetBackgroundSlot("InfernumMode/Backgrounds/LostColosseumBGObjects");
        }

        public override void ModifyFarFades(float[] fades, float transitionSpeed)
        {
            // This just fades in the background and fades out other backgrounds.
            for (int i = 0; i < fades.Length; i++)
            {
                if (i == Slot)
                {
                    fades[i] += transitionSpeed;
                    if (fades[i] > 1f)
                        fades[i] = 1f;
                }
                else
                {
                    fades[i] -= transitionSpeed;
                    if (fades[i] < 0f)
                        fades[i] = 0f;
                }
            }
        }
    }
}
