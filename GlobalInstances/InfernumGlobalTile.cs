using CalamityMod;
using InfernumMode.Systems;
using InfernumMode.Tiles;
using InfernumMode.Tiles.Abyss;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.GlobalInstances
{
    public class InfernumGlobalTile : GlobalTile
    {
        public static bool ShouldNotBreakDueToAboveTile(int x, int y)
        {
            int[] invincibleTiles = new int[]
            {
                ModContent.TileType<ProvidenceSummoner>(),
                ModContent.TileType<ProvidenceRoomDoorPedestal>(),
            };

            Tile checkTile = CalamityUtils.ParanoidTileRetrieval(x, y);
            Tile aboveTile = CalamityUtils.ParanoidTileRetrieval(x, y - 1);

            // Prevent tiles below invincible tiles from being destroyed. This is like chests in vanilla.
            return aboveTile.HasTile && checkTile.TileType != aboveTile.TileType && invincibleTiles.Contains(aboveTile.TileType);
        }

        public override bool CanExplode(int i, int j, int type)
        {
            if (ShouldNotBreakDueToAboveTile(i, j))
                return false;

            if (WorldSaveSystem.ProvidenceArena.Intersects(new(i, j, 1, 1)))
                return false;

            return base.CanExplode(i, j, type);
        }

        public override bool CanKillTile(int i, int j, int type, ref bool blockDamaged)
        {
            if (ShouldNotBreakDueToAboveTile(i, j))
                return false;

            if (WorldSaveSystem.ProvidenceArena.Intersects(new(i, j, 1, 1)))
                return false;

            return base.CanKillTile(i, j, type, ref blockDamaged);
        }

        public override void RandomUpdate(int i, int j, int type)
        {
            int num8 = WorldGen.genRand.Next((int)Main.rockLayer, (int)(Main.rockLayer + (double)Main.maxTilesY * 0.143));
            int nearbyVineCount = 0;
            for (int x = i - 15; x <= i + 15; x++)
            {
                for (int y = j - 15; y <= j - 15; y++)
                {
                    if (WorldGen.InWorld(x, y))
                    {
                        if (CalamityUtils.ParanoidTileRetrieval(x, y).HasTile &&
                            CalamityUtils.ParanoidTileRetrieval(x, y).TileType == (ushort)ModContent.TileType<SulphurousGroundVines>())
                        {
                            nearbyVineCount++;
                        }
                    }
                }
            }
            if (Main.tile[i, j - 1] != null && nearbyVineCount < 5)
            {
                if (!Main.tile[i, j - 1].HasTile && Main.tile[i, j - 1].TileType != (ushort)ModContent.TileType<SulphurousGroundVines>())
                {
                    if (Main.tile[i, j - 1].LiquidAmount == 255 &&
                        Main.tile[i, j - 1].LiquidType != LiquidID.Lava)
                    {
                        bool canGenerateVine = false;
                        for (int num52 = num8; num52 > num8 - 10; num52--)
                        {
                            if (Main.tile[i, num52].TopSlope)
                            {
                                canGenerateVine = false;
                                break;
                            }
                            if (Main.tile[i, num52].HasTile && !Main.tile[i, num52].TopSlope)
                            {
                                canGenerateVine = true;
                                break;
                            }
                        }
                        if (canGenerateVine)
                        {
                            int x = i;
                            int y = j - 1;
                            Main.tile[x, y].TileType = (ushort)ModContent.TileType<SulphurousGroundVines>();
                            Main.tile[x, y].Get<TileWallWireStateData>().HasTile = true;
                            WorldGen.SquareTileFrame(x, y, true);
                            if (Main.netMode == NetmodeID.Server)
                            {
                                NetMessage.SendTileSquare(-1, x, y, 3, TileChangeType.None);
                            }
                        }
                        Main.tile[i, j].Get<TileWallWireStateData>().Slope = SlopeType.Solid;
                        Main.tile[i, j].Get<TileWallWireStateData>().IsHalfBlock = false;
                    }
                }
            }
        }
    }
}
