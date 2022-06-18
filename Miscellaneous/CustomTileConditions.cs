using CalamityMod;
using Terraria;
using Terraria.ID;
using Terraria.World.Generation;

namespace InfernumMode.Miscellaneous
{
    public class CustomTileConditions
    {
        public class ActiveAndNotActuated : GenCondition
        {
            protected override bool CheckValidity(int x, int y) => CalamityUtils.ParanoidTileRetrieval(x, y).nactive();
        }

        public class NotPlatform : GenCondition
        {
            protected override bool CheckValidity(int x, int y) => !TileID.Sets.Platforms[CalamityUtils.ParanoidTileRetrieval(x, y).type];
        }

        public class IsSolidOrSolidTop : GenCondition
        {
            protected override bool CheckValidity(int x, int y)
            {
                Tile tile = CalamityUtils.ParanoidTileRetrieval(x, y);
                return tile.nactive() && (Main.tileSolid[tile.type] || Main.tileSolidTop[tile.type]);
            }
        }

        public class IsWater : GenCondition
        {
            protected override bool CheckValidity(int x, int y)
            {
                Tile tile = CalamityUtils.ParanoidTileRetrieval(x, y);
                return (tile.liquid >= 200 && !tile.honey() && !tile.lava());
            }
        }

        public class IsWaterOrSolid : GenCondition
        {
            protected override bool CheckValidity(int x, int y)
            {
                Tile tile = CalamityUtils.ParanoidTileRetrieval(x, y);
                return (tile.liquid >= 200 && !tile.honey() && !tile.lava()) || (tile.nactive() && Main.tileSolid[tile.type]);
            }
        }

        public class IsLavaOrSolid : GenCondition
        {
            protected override bool CheckValidity(int x, int y)
            {
                Tile tile = CalamityUtils.ParanoidTileRetrieval(x, y);
                return (tile.liquid >= 200 && tile.lava()) || (tile.nactive() && Main.tileSolid[tile.type]);
            }
        }
    }
}
