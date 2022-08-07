using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace InfernumMode.Tiles
{
    public class ProvidenceRoomDoorPedestal : ModTile
    {
        public const int Width = 4;
        public const int Height = 1;

        public override void SetStaticDefaults()
        {
            MinPick = int.MaxValue;
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileSpelunker[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x1);
            TileObjectData.newTile.Width = Width;
            TileObjectData.newTile.Height = Height;
            TileObjectData.newTile.Origin = new Point16(2, 0);
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16 };
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.addTile(Type);
            AddMapEntry(new Color(122, 66, 59));
        }

        public override bool CanExplode(int i, int j) => false;

        public override bool CanKillTile(int i, int j, ref bool blockDamaged) => false;

        public override bool CreateDust(int i, int j, ref int type)
        {
            // Fire dust.
            type = 6;
            return true;
        }

        public override void NearbyEffects(int i, int j, bool closer)
        {
            if (DownedBossSystem.downedGuardians)
                return;

            Vector2 bottom = new Vector2(i, j).ToWorldCoordinates(8f, 0f);
            int verticalOffset = 0;
            for (int k = 2; k < 200; k++)
            {
                if (WorldGen.SolidTile(i, j - k))
                {
                    verticalOffset = k * 16 + 24;
                    break;
                }
            }

            int horizontalBuffer = 32;
            Vector2 top = bottom - Vector2.UnitY * verticalOffset;
            Rectangle area = new((int)top.X - Width * 8 + horizontalBuffer / 2, (int)top.Y, Width * 16 - horizontalBuffer, verticalOffset);

            // Hurt the player if they touch the spikes.
            if (Main.LocalPlayer.Hitbox.Intersects(area))
            {
                Main.LocalPlayer.Hurt(PlayerDeathReason.ByCustomReason($"{Main.LocalPlayer.name} was somehow impaled by a pillar of crystals."), 100, 0);
                Main.LocalPlayer.AddBuff(Main.dayTime ? ModContent.BuffType<HolyFlames>() : ModContent.BuffType<Nightwither>(), 180);
            }
        }

        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            int xFrameOffset = Main.tile[i, j].TileFrameX;
            int yFrameOffset = Main.tile[i, j].TileFrameY;
            if (xFrameOffset != 0 || yFrameOffset != 0)
                return;

            Main.instance.TilesRenderer.AddSpecialLegacyPoint(i, j);
        }

        public override void SpecialDraw(int i, int j, SpriteBatch spriteBatch)
        {
            if (DownedBossSystem.downedGuardians)
                return;

            Texture2D door = ModContent.Request<Texture2D>("InfernumMode/Tiles/ProvidenceRoomDoor").Value;
            Vector2 drawOffest = (Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange));
            Vector2 drawPosition = new Vector2((float)(i * 16) - Main.screenPosition.X, (float)(j * 16) - Main.screenPosition.Y) + drawOffest;
            Color drawColour = Color.White;

            int verticalOffset = 0;
            for (int k = 2; k < 200; k++)
            {
                if (WorldGen.SolidTile(i, j - k))
                {
                    verticalOffset = k * 16 + 24;
                    break;
                }
            }

            for (int dy = verticalOffset; dy >= 0; dy -= 96)
            {
                Vector2 drawOffset = new(-12f, -dy - 48f);
                spriteBatch.Draw(door, drawPosition + drawOffset, null, drawColour, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            }
        }
    }
}
