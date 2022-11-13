using CalamityMod;
using CalamityMod.Dusts;
using InfernumMode.Items.Placeables;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InfernumMode.Tiles
{
    public class ProfanedFountainTile : ModTile
    {
        public override void SetStaticDefaults()
        {
            CalamityUtils.SetUpFountain(this);
            AddMapEntry(Color.Yellow, Language.GetText("MapObject.WaterFountain"));
            AnimationFrameHeight = 72;
        }

        public override void NearbyEffects(int i, int j, bool closer)
        {
            if (Main.tile[i, j].TileFrameX < 36)
                Main.LocalPlayer.Infernum().ProfanedLavaFountain = true;
        }

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

        public override bool CreateDust(int i, int j, ref int type)
        {
            for (int k = 0; k < 2; k++)
            {
                Dust fire = Dust.NewDustPerfect(new Vector2(i, j).ToWorldCoordinates() + Main.rand.NextVector2Circular(8f, 8f), (int)CalamityDusts.ProfanedFire);
                fire.scale = 1.8f;
                fire.velocity = Main.rand.NextVector2Circular(3f, 3f);
                fire.noGravity = true;
            }
            return false;
        }

        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = fail ? 1 : 3;
        }

        public override void AnimateTile(ref int frame, ref int frameCounter)
        {
            frameCounter++;
            if (frameCounter >= 6)
            {
                frame = (frame + 1) % 4;
                frameCounter = 0;
            }
        }

        public override void KillMultiTile(int i, int j, int frameX, int frameY)
        {
            Item.NewItem(new EntitySource_TileBreak(i, j), i * 16, j * 16, 16, 32, ModContent.ItemType<ProfanedFountainItem>());
        }

        public override void HitWire(int i, int j)
        {
            CalamityUtils.LightHitWire(Type, i, j, 2, 4);
        }

        public override bool RightClick(int i, int j)
        {
            CalamityUtils.LightHitWire(Type, i, j, 2, 4);
            return true;
        }

        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ModContent.ItemType<ProfanedFountainItem>();
        }
    }
}
