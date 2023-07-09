using CalamityMod;
using CalamityMod.Dusts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Metadata;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Abyss
{
    public class SulphurousPlants : ModTile
    {
        public static Asset<Texture2D> Glowmask
        {
            get;
            private set;
        }

        public override void SetStaticDefaults()
        {
            Main.tileLighted[Type] = true;
            Main.tileCut[Type] = true;
            Main.tileSolid[Type] = false;
            Main.tileNoAttach[Type] = true;
            Main.tileNoFail[Type] = true;
            Main.tileLavaDeath[Type] = true;
            Main.tileWaterDeath[Type] = false;
            Main.tileFrameImportant[Type] = true;
            TileID.Sets.ReplaceTileBreakUp[Type] = true;
            TileID.Sets.SwaysInWindBasic[Type] = true;
            TileMaterials.SetForTileId(Type, TileMaterials._materialsByName["Plant"]);

            DustType = (int)CalamityDusts.SulfurousSeaAcid;

            HitSound = SoundID.Grass;

            AddMapEntry(new Color(86, 135, 98));

            if (Main.netMode != NetmodeID.Server)
                Glowmask = ModContent.Request<Texture2D>("InfernumMode/Content/Tiles/Abyss/SulphurousPlantsGlow");
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            int frameX = CalamityUtils.ParanoidTileRetrieval(i, j).TileFrameX / 18;
            bool emitLight = frameX is not 1 or 3 or 4 or 5 or 19 or 21 or 22;
            if (!emitLight)
                return;

            r = 0.5f;
            g = 0.75f;
            b = 0.25f;
        }

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
            spriteBatch.Draw(mainTexture, drawPos, new Rectangle(frameX, frameY, 18, 18), lightColor, 0f, Vector2.Zero, 1f, 0, 0f);
            spriteBatch.Draw(glowmask, drawPos, new Rectangle(frameX, frameY, 18, 18), Color.White, 0f, Vector2.Zero, 1f, 0, 0f);
            return false;
        }
    }
}
