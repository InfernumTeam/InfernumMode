using CalamityMod.Items.Accessories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace InfernumMode.Content.Tiles.Abyss
{
    public class IronBootsSkeleton : ModTile
    {
        public static Asset<Texture2D> Glowmask
        {
            get;
            private set;
        } = null;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;

            // Prepare the bottom variant.
            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
            TileObjectData.addTile(Type);

            DustType = 1;

            AddMapEntry(new Color(168, 188, 192));

            if (Main.netMode != NetmodeID.Server)
                Glowmask = ModContent.Request<Texture2D>("InfernumMode/Content/Tiles/Abyss/IronBootsSkeletonGlow");

            base.SetStaticDefaults();
        }

        public override bool CanExplode(int i, int j) => false;

        public override bool CanKillTile(int i, int j, ref bool blockDamaged) => NPC.downedBoss3;

        public override void KillMultiTile(int i, int j, int frameX, int frameY)
        {
            Item.NewItem(new EntitySource_TileBreak(i, j), i * 16, j * 16, 32, 32, ModContent.ItemType<IronBoots>());
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
