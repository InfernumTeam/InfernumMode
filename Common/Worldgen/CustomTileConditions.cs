using CalamityMod;
using Terraria;
using Terraria.ID;
using Terraria.WorldBuilding;

namespace InfernumMode.Common.Worldgen
{
    public class CustomTileConditions
    {
        public class ActiveAndNotActuated : GenCondition
        {
            protected override bool CheckValidity(int x, int y) => CalamityUtils.ParanoidTileRetrieval(x, y).HasUnactuatedTile;
        }

        public class NotPlatform : GenCondition
        {
            protected override bool CheckValidity(int x, int y) => !TileID.Sets.Platforms[CalamityUtils.ParanoidTileRetrieval(x, y).TileType];
        }

        public class IsSolidOrSolidTop : GenCondition
        {
            protected override bool CheckValidity(int x, int y)
            {
                Tile tile = CalamityUtils.ParanoidTileRetrieval(x, y);
                return tile.HasUnactuatedTile && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]);
            }
        }

        public class IsAir : GenCondition
        {
            protected override bool CheckValidity(int x, int y)
            {
                Tile tile = CalamityUtils.ParanoidTileRetrieval(x, y);
                return !tile.HasTile && tile.LiquidAmount <= 0;
            }
        }

        public class IsWater : GenCondition
        {
            protected override bool CheckValidity(int x, int y)
            {
                Tile tile = CalamityUtils.ParanoidTileRetrieval(x, y);
                return tile.LiquidAmount >= 200 && !(tile.LiquidType == LiquidID.Honey) && !(tile.LiquidType == LiquidID.Lava);
            }
        }

        public class IsWaterOrSolid : GenCondition
        {
            protected override bool CheckValidity(int x, int y)
            {
                Tile tile = CalamityUtils.ParanoidTileRetrieval(x, y);
                return tile.LiquidAmount >= 200 && !(tile.LiquidType == LiquidID.Honey) && !(tile.LiquidType == LiquidID.Lava) || tile.HasUnactuatedTile && Main.tileSolid[tile.TileType];
            }
        }

        public class IsLavaOrSolid : GenCondition
        {
            protected override bool CheckValidity(int x, int y)
            {
                Tile tile = CalamityUtils.ParanoidTileRetrieval(x, y);
                return tile.LiquidAmount >= 200 && tile.LiquidType == LiquidID.Lava || tile.HasUnactuatedTile && Main.tileSolid[tile.TileType];
            }
        }
    }
}
