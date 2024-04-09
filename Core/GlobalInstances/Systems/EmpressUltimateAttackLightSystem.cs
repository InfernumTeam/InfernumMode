using System.Linq;
using InfernumMode.Content.BehaviorOverrides.BossAIs.EmpressOfLight;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Light;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class EmpressUltimateAttackLightSystem : ModSystem
    {
        // This is stored as a field to prevent having to do 1000 indexed projectile lookups for many, many different tiles.
        internal static bool CelestialObjectNotInSkyForNextFrame;

        public static float VerticalSunMoonOffset
        {
            get;
            set;
        }

        public override void OnModLoad()
        {
            On_TileLightScanner.GetTileLight += ActLikeTheresNoMoon;
        }

        public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
        {
            CelestialObjectNotInSkyForNextFrame = false;
            if (StolenCelestialObject.MoonIsNotInSky || StolenCelestialObject.SunIsNotInSky)
            {
                Projectile celestialObject = Utilities.AllProjectilesByID(ModContent.ProjectileType<StolenCelestialObject>()).First();
                float darknessDissipateInterpolant = Utils.GetLerpValue(8f, 90f, celestialObject.timeLeft, true) * Utils.GetLerpValue(72000f, 71960f, celestialObject.timeLeft, true);
                float fadeToBlack = darknessDissipateInterpolant * (Main.dayTime ? 1f : 0.9f);
                backgroundColor = Color.Lerp(backgroundColor, Color.Black, fadeToBlack);
                CelestialObjectNotInSkyForNextFrame = true;
            }
        }

        private void ActLikeTheresNoMoon(On_TileLightScanner.orig_GetTileLight orig, TileLightScanner self, int x, int y, out Vector3 outputColor)
        {
            orig(self, x, y, out outputColor);
            if (CelestialObjectNotInSkyForNextFrame)
            {
                Projectile celestialObject = Utilities.AllProjectilesByID(ModContent.ProjectileType<StolenCelestialObject>()).First();
                float distanceFromMoon = Vector2.Distance(new Vector2(x, y).ToWorldCoordinates(), celestialObject.Center);
                float darknessInterpolant = Utils.GetLerpValue(1200f, 720f, distanceFromMoon, true);
                float darknessDissipateInterpolant = Utils.GetLerpValue(90f, 8f, celestialObject.timeLeft, true) * Utils.GetLerpValue(72000f, 71960f, celestialObject.timeLeft, true);
                darknessInterpolant = Lerp(darknessInterpolant, 1f, darknessDissipateInterpolant);

                outputColor *= darknessInterpolant;
            }
        }
    }
}
