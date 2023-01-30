using InfernumMode.Content.BehaviorOverrides.BossAIs.EmpressOfLight;
using Microsoft.Xna.Framework;
using System.Linq;
using Terraria;
using Terraria.Graphics.Light;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class EmpressUltimateAttackLightSystem : ModSystem
    {
        // This is stored as a field to prevent having to do 1000 indexed projectile lookups for many, many different tiles
        internal static bool MoonNotInSkyForNextFrame;

        public static float VerticalMoonOffset
        {
            get;
            set;
        }

        public override void OnModLoad()
        {
            On.Terraria.Graphics.Light.TileLightScanner.GetTileLight += ActLikeTheresNoMoon;
        }

        public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
        {
            MoonNotInSkyForNextFrame = false;
            if (TheMoon.MoonIsNotInSky)
            {
                Projectile moon = Utilities.AllProjectilesByID(ModContent.ProjectileType<TheMoon>()).First();
                float darknessDissipateInterpolant = Utils.GetLerpValue(8f, 90f, moon.timeLeft, true);
                backgroundColor = Color.Lerp(backgroundColor, Color.Black, darknessDissipateInterpolant * 0.9f);
                MoonNotInSkyForNextFrame = true;
            }
        }

        private void ActLikeTheresNoMoon(On.Terraria.Graphics.Light.TileLightScanner.orig_GetTileLight orig, TileLightScanner self, int x, int y, out Vector3 outputColor)
        {
            orig(self, x, y, out outputColor);
            if (MoonNotInSkyForNextFrame)
            {
                Projectile moon = Utilities.AllProjectilesByID(ModContent.ProjectileType<TheMoon>()).First();
                float distanceFromMoon = Vector2.Distance(new Vector2(x, y).ToWorldCoordinates(), moon.Center);
                float darknessInterpolant = Utils.GetLerpValue(1200f, 720f, distanceFromMoon, true);
                float darknessDissipateInterpolant = Utils.GetLerpValue(90f, 8f, moon.timeLeft, true);
                darknessInterpolant = MathHelper.Lerp(darknessInterpolant, 1f, darknessDissipateInterpolant);

                outputColor *= darknessInterpolant;
            }
        }
    }
}