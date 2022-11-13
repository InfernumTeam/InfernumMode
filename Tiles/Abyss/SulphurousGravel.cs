using CalamityMod;
using CalamityMod.Tiles.Abyss;
using InfernumMode.Items.Placeables;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Abyss
{
    public class SulphurousGravel : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;

            CalamityUtils.MergeWithGeneral(Type);
            CalamityUtils.MergeWithAbyss(Type);
            CalamityUtils.SetMerge(Type, ModContent.TileType<AbyssGravel>());

            DustType = 32;
            ItemDrop = ModContent.ItemType<SulphurousGravelItem>();
            AddMapEntry(new Color(113, 90, 71));
            MineResist = 1.3f;
            HitSound = SoundID.Dig;
        }

        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = fail ? 1 : 3;
        }

        public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
        {
            TileFraming.CustomMergeFrame(i, j, Type, ModContent.TileType<SulphurousGravel>(), false, false, false, false, resetFrame);
            return false;
        }
    }
}
