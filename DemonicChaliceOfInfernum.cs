using InfernumMode.Tiles;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode
{
    public class DemonicChaliceOfInfernum : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Demonic Chalice of Infernum");
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(6, 8));
            SacrificeTotal = 1;
        }

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Red;
            Item.width = 50;
            Item.height = 96;
            Item.maxStack = 999;
            Item.DefaultToPlaceableTile(ModContent.TileType<InfernalChaliceTile>());
        }
    }
}
