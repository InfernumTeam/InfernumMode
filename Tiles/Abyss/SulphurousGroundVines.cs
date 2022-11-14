using CalamityMod.Dusts;
using InfernumMode.WorldGeneration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Metadata;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Abyss
{
    public class SulphurousGroundVines : ModTile
    {
        public static int TileType
        {
            get;
            private set;
        }

        public static Asset<Texture2D> Glowmask
        {
            get;
            private set;
        } = null;

        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = false;
            Main.tileNoFail[Type] = true;
            Main.tileLavaDeath[Type] = true;
            Main.tileWaterDeath[Type] = false;
            Main.tileFrameImportant[Type] = true;
            TileID.Sets.ReplaceTileBreakUp[Type] = true;
            TileMaterials.SetForTileId(Type, TileMaterials._materialsByName["Plant"]);

            DustType = (int)CalamityDusts.SulfurousSeaAcid;

            HitSound = SoundID.Grass;
            
            TileType = Type;

            AddMapEntry(new Color(121, 153, 82));

            if (Main.netMode != NetmodeID.Server)
                Glowmask = ModContent.Request<Texture2D>("InfernumMode/Tiles/Abyss/SulphurousGroundVinesGlow");
        }

        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            // Provide vine rops if the player has the vine book thing.
            if (WorldGen.genRand.NextBool() && Main.player[Player.FindClosest(new Vector2(i * 16, j * 16), 16, 16)].cordage)
                Item.NewItem(new EntitySource_TileBreak(i, j), new Vector2(i * 16 + 8f, j * 16 + 8f), ItemID.VineRope);

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

        public static bool AttemptToGrowVine(Point p)
        {
            if (!Main.tile[p.X, p.Y].HasTile || Main.tile[p.X, p.Y].TileType != TileType)
                return false;

            if (Main.tile[p.X, p.Y - 1].LiquidAmount < 128 || Main.tile[p.X, p.Y - 1].LiquidType != LiquidID.Water)
                return false;

            if (!CustomAbyss.InsideOfLayer1Forest(p))
                return false;

            bool canGenerateVine = false;
            for (int y = p.Y - 9; y < p.Y; y++)
            {
                if (Main.tile[p.X, y].TopSlope)
                {
                    canGenerateVine = false;
                    break;
                }
                if (Main.tile[p.X, y].HasTile && !Main.tile[p.X, y].TopSlope)
                {
                    canGenerateVine = true;
                    break;
                }
            }
            if (canGenerateVine)
            {
                Main.tile[p.X, p.Y - 1].TileType = (ushort)TileType;
                Main.tile[p.X, p.Y - 1].TileFrameX = (short)(WorldGen.genRand.Next(6) * 18);
                Main.tile[p.X, p.Y - 1].TileFrameY = 0;
                Main.tile[p.X, p.Y - 1].Get<TileWallWireStateData>().HasTile = true;

                Main.tile[p].TileFrameX = (short)(WorldGen.genRand.Next(6) * 18);
                Main.tile[p].TileFrameY = (short)(WorldGen.genRand.Next(1, 5) * 18);
                Main.tile[p].Get<TileWallWireStateData>().Slope = SlopeType.Solid;
                Main.tile[p].Get<TileWallWireStateData>().IsHalfBlock = false;

                WorldGen.SquareTileFrame(p.X, p.Y, true);
                WorldGen.SquareTileFrame(p.X, p.Y - 1, true);
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendTileSquare(-1, p.X, p.Y, 3, TileChangeType.None);
                    NetMessage.SendTileSquare(-1, p.X, p.Y - 1, 3, TileChangeType.None);
                }
            }
            return canGenerateVine;
        }

        public override void RandomUpdate(int i, int j) => AttemptToGrowVine(new(i, j));

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile t = Main.tile[i, j];
            int frameX = t.TileFrameX;
            int frameY = t.TileFrameY;

            Texture2D mainTexture = TextureAssets.Tile[Type].Value;
            Texture2D glowmask = Glowmask.Value;
            Vector2 drawOffset = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawPos = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y) + drawOffset;
            Color lightColor = Lighting.GetColor(i, j);
            Color fruitColor = Color.Lerp(lightColor, Color.White, 0.35f);
            spriteBatch.Draw(mainTexture, drawPos, new Rectangle(frameX, frameY, 18, 18), lightColor, 0f, Vector2.Zero, 1f, 0, 0f);
            spriteBatch.Draw(glowmask, drawPos, new Rectangle(frameX, frameY, 18, 18), fruitColor, 0f, Vector2.Zero, 1f, 0, 0f);
            return false;
        }
    }
}
