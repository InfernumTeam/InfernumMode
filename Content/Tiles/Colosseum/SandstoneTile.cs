using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Colosseum
{
    public class SandstoneTile : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = true;
            Main.tileMergeDirt[Type] = true;
            Main.tileBlockLight[Type] = true;

            CalamityUtils.SetMerge(Type, TileID.Sand);
            CalamityUtils.SetMerge(Type, TileID.Sandstone);
            CalamityUtils.SetMerge(Type, TileID.SandstoneBrick);
            CalamityUtils.SetMerge(Type, TileID.SmoothSandstone);
            CalamityUtils.SetMerge(Type, TileID.EbonstoneBrick);
            CalamityUtils.SetMerge(Type, TileID.IronBrick);
            CalamityUtils.SetMerge(Type, TileID.Platforms);
            CalamityUtils.SetMerge(Type, TileID.GraniteBlock);

            AddMapEntry(new Color(198, 124, 78));
        }
    }
}
