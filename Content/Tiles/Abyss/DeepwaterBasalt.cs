using CalamityMod;
using CalamityMod.Tiles.Abyss;
using InfernumMode.Content.Items.Placeables;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Abyss
{
    public class DeepwaterBasalt : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;

            CalamityUtils.MergeWithGeneral(Type);
            CalamityUtils.MergeWithAbyss(Type);
            CalamityUtils.SetMerge(Type, ModContent.TileType<Voidstone>());

            DustType = 1;
            ItemDrop = ModContent.ItemType<DeepwaterBasaltItem>();
            AddMapEntry(new Color(84, 90, 127));
            MineResist = 10f;
            HitSound = SoundID.Tink;
        }

        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = fail ? 1 : 3;
        }
    }
}
