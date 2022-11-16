using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace InfernumMode.Tiles.Abyss
{
    public class AbyssalCoral : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.addTile(Type);
            AddMapEntry(new Color(70, 206, 160));

            base.SetStaticDefaults();
        }
    }
}
