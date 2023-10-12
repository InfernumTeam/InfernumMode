using InfernumMode.Core;
using Terraria;
using Terraria.ModLoader;
using InfernumMode.Common.Graphics.Drawers.SceneDrawers.DeusScene;
using InfernumMode.Common.Graphics.Drawers;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace InfernumMode.Common.Graphics.ScreenEffects
{
    /// <summary>
    /// I don't really like this being its own thing, but for some reason it did NOT work else, and I don't have the energy anymore to do that.
    /// </summary>
    public class AstralDimensionSystem : ModSystem
    {
        public static float MonolithIntensity
        {
            get;
            set;
        }

        public static bool EffectIsActive
        {
            get
            {
                if (Main.gameMenu || InfernumConfig.Instance.ReducedGraphicsConfig)
                    return false;

                return Main.LocalPlayer.Infernum_Biome().AstralMonolithEffect;
            }
        }

        public static void Draw()
        {
            if (!EffectIsActive && MonolithIntensity <= 0f)
                return;
          
            if (EffectIsActive)
                MonolithIntensity = Clamp(MonolithIntensity + 0.01f, 0f, 1f);
            float intensity = MonolithIntensity;

            Texture2D texture = DrawerManager.GetSceneDrawer<DeusMonolithSceneDrawSystem>().MainTarget;
            Main.spriteBatch.Draw(texture, Vector2.Zero, Color.White * intensity);
        }
    }
}
