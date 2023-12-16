using InfernumMode.Content.Walls;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Placeables
{
    public class ProfanedTempleBackgroundWall : ModItem
    {
        public override string Texture => "InfernumMode/Content/Items/Placeables/SakuraTreetop";

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableWall(ModContent.WallType<ProfanedTempleBGWall>());

            Item.width = 16;
            Item.height = 16;
            Item.maxStack = 9999;
            Item.value = Item.buyPrice(0, 0, 0, 50);
        }
    }
}
