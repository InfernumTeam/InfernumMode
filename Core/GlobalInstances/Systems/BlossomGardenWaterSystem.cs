using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class BlossomGardenWaterSystem : ModSystem
    {
        public override void OnModLoad()
        {
            On.Terraria.Graphics.Light.TileLightScanner.GetTileLight += AddLightToGardenWater;
        }

        private void AddLightToGardenWater(On.Terraria.Graphics.Light.TileLightScanner.orig_GetTileLight orig, Terraria.Graphics.Light.TileLightScanner self, int x, int y, out Vector3 outputColor)
        {
            orig(self, x, y, out outputColor);

            Tile t = CalamityUtils.ParanoidTileRetrieval(x, y);
            if (Main.LocalPlayer.WithinRange(WorldSaveSystem.BlossomGardenCenter.ToWorldCoordinates(), 3200f) && !t.HasTile && t.LiquidAmount >= 1)
            {
                // Randomly create foam.
                if (!Main.gamePaused && Main.rand.NextBool(240))
                {
                    MediumMistParticle mist = new(new Vector2(x * 16f + 8, y * 16f + 8) + Main.rand.NextVector2Circular(8f, 8f), Main.rand.NextVector2Circular(1f, 1f) - Vector2.UnitY * 0.2f, Color.LightCyan, Color.White, 0.1f, 185f)
                    {
                        Rotation = Main.rand.NextFloat(MathHelper.TwoPi)
                    };
                    GeneralParticleHandler.SpawnParticle(mist);
                }

                outputColor = Vector3.Lerp(outputColor, Color.Cyan.ToVector3(), 0.6f);
            }
        }
    }
}