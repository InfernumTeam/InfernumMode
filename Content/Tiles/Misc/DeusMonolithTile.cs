using CalamityMod.Dusts;
using CalamityMod;
using InfernumMode.Content.Items.Placeables;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Terraria;
using Microsoft.Xna.Framework;
using Terraria.Enums;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.Audio;
using Terraria.ID;

namespace InfernumMode.Content.Tiles.Misc
{
    public class DeusMonolithTile : ModTile
    {
        public static int FrameWidth => 18 * 2;

        public static int FrameHeight => 18 * 2;

        public static Texture2D AnimationTexture
        {
            get;
            private set;
        }

        public static int FrameCounter
        {
            get;
            set;
        }

        public static int Frame
        {
            get;
            set;
        }

        public override void Load()
        {
            AnimationTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Tiles/Misc/DeusMonolithTileAnimation", AssetRequestMode.ImmediateLoad).Value;
        }

        public override void Unload()
        {
            AnimationTexture = null;
        }

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileSpelunker[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.Origin = new Point16(0, 1);
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16 };
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.addTile(Type);
            AddMapEntry(new Color(92, 59, 156));
        }

        public override void NearbyEffects(int i, int j, bool closer)
        {
            if (Main.tile[i, j].TileFrameX >= 36)
                Main.LocalPlayer.Infernum_Biome().AstralMonolithEffect = true;
        }

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

        public override bool CreateDust(int i, int j, ref int type)
        {
            for (int k = 0; k < 2; k++)
            {
                Dust fire = Dust.NewDustPerfect(new Vector2(i, j).ToWorldCoordinates() + Main.rand.NextVector2Circular(8f, 8f), (int)CalamityDusts.PurpleCosmilite);
                fire.scale = 1.3f;
                fire.velocity = Main.rand.NextVector2Circular(3f, 3f);
                fire.noGravity = true;
            }
            return false;
        }

        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = fail ? 1 : 3;
        }

        public override void KillMultiTile(int i, int j, int frameX, int frameY)
        {
            //Item.NewItem(new EntitySource_TileBreak(i, j), i * 16, j * 16, 16, 32, ModContent.ItemType<CosmicMonolithItem>());
        }

        public override void HitWire(int i, int j)
        {
            CalamityUtils.LightHitWire(Type, i, j, 2, 4);
        }

        public override bool RightClick(int i, int j)
        {
            CalamityUtils.LightHitWire(Type, i, j, 2, 4);
            SoundEngine.PlaySound(SoundID.MenuTick, new Point(i, j).ToWorldCoordinates());
            return true;
        }

        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ModContent.ItemType<DeusMonolithItem>();
        }

        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            // Since this tile does not have the hovering part on its sheet, we have to animate it ourselves
            // Therefore we register the top-left of the tile as a "special point"
            // This allows us to draw things in SpecialDraw
            if (drawData.tileFrameX % FrameWidth == 0 && drawData.tileFrameY % FrameHeight == 0)
            {
                Main.instance.TilesRenderer.AddSpecialLegacyPoint(i, j);
            }
        }

        public override void SpecialDraw(int i, int j, SpriteBatch spriteBatch)
        {
            if (Main.tile[i, j].TileFrameX < 36)
                return;
            // This is lighting-mode specific, always include this if you draw tiles manually
            Vector2 offScreen = new(Main.offScreenRange);
            if (Main.drawToScreen)
                offScreen = Vector2.Zero;

            // Take the tile, check if it actually exists
            Point p = new(i, j);
            Tile tile = Main.tile[p.X, p.Y];
            if (tile == null || !tile.HasTile)
                return;

            // Get the initial draw parameters
            Texture2D texture = AnimationTexture;

            FrameCounter++;
            if (FrameCounter > 1)
            {
                Frame++;
                if (Frame > 7)
                    Frame = 0;
                FrameCounter = 0;
            }

            Rectangle frame = new(0, Frame * 54, texture.Width, 54);

            Vector2 origin = frame.Size() / 2f;
            Vector2 worldPos = p.ToWorldCoordinates(16, 5);

            bool direction = tile.TileFrameY / FrameHeight != 0; // This is related to the alternate tile data we registered before

            SpriteEffects effects = direction ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            Vector2 drawPos = worldPos + offScreen - Main.screenPosition;

            // Draw the main texture
            spriteBatch.Draw(texture, drawPos, frame, Color.White, 0f, origin, 1f, effects, 0f);

            //Draw the periodic glow effect
            float scale = Sin(Main.GlobalTimeWrappedHourly * TwoPi / 2f) * 0.3f + 0.7f;
            Color effectColor = Color.White;
            effectColor.A = 0;
            effectColor = effectColor * 0.05f * scale;
            for (float interpolant = 0f; interpolant < 1f; interpolant += 1f / 16f)
                spriteBatch.Draw(texture, drawPos + (TwoPi * interpolant).ToRotationVector2() * 1f, frame, effectColor, 0f, origin, 1f, effects, 0f);
        }
    }
}
