using CalamityMod.Tiles.Abyss;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Systems
{
    public class TileLightingSystem : ModSystem
    {
        public override void UpdateUI(GameTime gameTime)
        {
            if (Main.netMode == NetmodeID.Server)
                return;
            
            int crystalID = ModContent.TileType<LumenylCrystals>();
            for (int dx = -80; dx < 80; dx++)
            {
                for (int dy = -80; dy < 80; dy++)
                {
                    int i = (int)(Main.LocalPlayer.Center.X / 16f + dx);
                    int j = (int)(Main.LocalPlayer.Center.Y / 16f + dy);
                    if (!WorldGen.InWorld(i, j, 1))
                        continue;

                    Tile t = Main.tile[i, j];
                    if (t.TileType != crystalID)
                        continue;

                    Texture2D texture = TextureAssets.Tile[crystalID].Value;
                    Vector2 drawPosition = new Vector2(i, j) * 16f;
                    Rectangle tileFrame = new(t.TileFrameX, t.TileFrameY, 18, 18);
                    ScreenSaturationBlurSystem.ThingsToDrawOnTopOfBlur.Add(new(texture, drawPosition, tileFrame, Color.White * 0.75f, 0f, Vector2.Zero, 1f, 0, 0));
                }
            }
        }
    }
}