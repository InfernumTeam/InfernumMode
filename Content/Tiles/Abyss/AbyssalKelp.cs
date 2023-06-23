using CalamityMod;
using CalamityMod.Dusts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Metadata;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Abyss
{
    public class AbyssalKelp : ModTile
    {
        public const int WindPushLifetime = 45;

        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = false;
            Main.tileLighted[Type] = true;
            Main.tileNoFail[Type] = true;
            Main.tileLavaDeath[Type] = true;
            Main.tileWaterDeath[Type] = false;
            Main.tileFrameImportant[Type] = true;
            TileID.Sets.ReplaceTileBreakUp[Type] = true;
            TileMaterials.SetForTileId(Type, TileMaterials._materialsByName["Plant"]);

            DustType = (int)CalamityDusts.SulfurousSeaAcid;

            HitSound = SoundID.Grass;

            AddMapEntry(new Color(43, 66, 18));
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            g = 0.4f;
            b = 0.5f;
        }

        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            Point p = new(i, j);
            if (Main.tile[p.X, p.Y - 1].HasTile && Main.tile[p.X, p.Y - 1].TileType == Type)
            {
                WorldGen.KillTile(p.X, p.Y - 1, false, false, false);
                if (!Main.tile[p.X, p.Y - 1].HasTile && Main.netMode != NetmodeID.SinglePlayer)
                    NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, p.X, p.Y - 1);
            }

            if (Main.tile[p.X, p.Y + 1].HasTile && Main.tile[p.X, p.Y + 1].TileType == Type)
            {
                Main.tile[p.X, p.Y + 1].Get<TileWallWireStateData>().TileFrameX = (short)(WorldGen.genRand.Next(6) * 18);
                Main.tile[p.X, p.Y + 1].Get<TileWallWireStateData>().TileFrameY = 0;
                if (Main.netMode != NetmodeID.SinglePlayer)
                    NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, p.X, p.Y + 1);
            }
        }

        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            Tile t = CalamityUtils.ParanoidTileRetrieval(i, j);
            if (t.TileFrameX == 0 && t.TileFrameY == 72)
                Main.instance.TilesRenderer.AddSpecialLegacyPoint(i, j);
        }

        public override void SpecialDraw(int i, int j, SpriteBatch spriteBatch)
        {
            int movedKelp = 0;
            Vector2 drawCenter = new(i * 16f + 8f, j * 16f + 16f + 2f);
            float windPushFactor = Lerp(0.2f, 1f, Math.Abs(Main.WindForVisuals) / 1.2f);
            float offsetAngle = -0.08f * windPushFactor;
            float windCycle = Utils.GetLerpValue(0.08f, 0.18f, Math.Abs(Main.WindForVisuals), true);
            windCycle += Sin(Main.GlobalTimeWrappedHourly * 2.3f + i * 1.1f + j * 0.81f) * 1.9f + 1.27f;

            if (!Main.SettingsEnabled_TilesSwayInWind)
                windCycle = 0f;

            float rot = 0f;
            float push = 0f;
            for (int y = j; y > 10; y--)
            {
                Tile tile = Main.tile[i, y];
                if (tile != null)
                {
                    ushort type = tile.TileType;
                    if (!tile.HasTile)
                        break;

                    if (movedKelp >= 5)
                        offsetAngle += 0.0075f * windPushFactor;
                    if (movedKelp >= 2)
                        offsetAngle += 0.0025f;
                    movedKelp++;

                    float windGridPush = Main.instance.TilesRenderer.GetWindGridPush(i, y, 60, -0.004f);
                    if (windGridPush == 0f && push != 0f)
                        rot *= -0.78f;
                    else
                        rot -= windGridPush;

                    push = windGridPush;
                    short frameX = tile.TileFrameX;
                    short frameY = tile.TileFrameY;
                    Color color = Lighting.GetColor(i, y);
                    Main.instance.TilesRenderer.GetTileDrawData(i, y, tile, type, ref frameX, ref frameY, out int width, out int num8, out int num9, out int num10, out int frameOffsetX, out int frameOffsetY, out SpriteEffects direction, out _, out _, out _);
                    Vector2 position = (Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange)) + drawCenter - Main.screenPosition;
                    float windRotation = movedKelp * -offsetAngle * windCycle + rot;

                    Texture2D tileDrawTexture = TextureAssets.Tile[Type].Value;
                    if (tileDrawTexture == null)
                        return;

                    Main.spriteBatch.Draw(tileDrawTexture, position, new Rectangle(frameX + frameOffsetX, frameY + frameOffsetY, width, num8 - num10), color, windRotation, new Vector2(width / 2, num10 - num9 + num8), 1f, direction, 0f);
                    drawCenter += (windRotation - PiOver2).ToRotationVector2() * 16f;
                }
            }
        }

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile t = CalamityUtils.ParanoidTileRetrieval(i, j);
            return t.TileFrameX == 0 && t.TileFrameY == 72;
        }
    }
}
