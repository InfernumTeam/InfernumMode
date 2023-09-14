using CalamityMod.Items.Pets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace InfernumMode.Content.Tiles.Abyss
{
    public class StrangeOrbTile : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLighted[Type] = true;

            // Prepare the bottom variant.
            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.addTile(Type);

            AddMapEntry(new Color(70, 206, 160));

            base.SetStaticDefaults();
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            r = 0.5f;
            g = 0.36f;
            b = 0.46f;
        }

        public override bool CanExplode(int i, int j) => false;

        public override bool CanKillTile(int i, int j, ref bool blockDamaged) => NPC.downedBoss3;

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile t = Main.tile[i, j];
            int frameX = t.TileFrameX;
            int frameY = t.TileFrameY;

            Texture2D mainTexture = TextureAssets.Tile[Type].Value;
            Vector2 drawOffset = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawPos = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y) + drawOffset;
            Color lightColor = Lighting.GetColor(i, j);
            spriteBatch.Draw(mainTexture, drawPos, new Rectangle(frameX, frameY, 18, 18), lightColor, 0f, Vector2.Zero, 1f, 0, 0f);

            // Draw some vague anahita apparitions if the player isn't too close.
            if (frameX == 18 && frameY == 18)
            {
                Vector2 center = new(i * 16f, j * 16f);
                Player closestPlayer = Main.player[Player.FindClosest(center, 1, 1)];
                float playerDistance = Vector2.Distance(center, closestPlayer.Center);
                float spiritOpacity = Utils.GetLerpValue(60f, 150f, playerDistance, true);

                if (spiritOpacity <= 0f)
                    return false;

                Texture2D spiritTexture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Pets/OceanSpirit").Value;
                for (int k = 0; k < 6; k++)
                {
                    int spiritFrameY = (int)Lerp(0f, 8f, Sin(Main.GlobalTimeWrappedHourly * 4f + k * 0.7f) * 0.5f + 0.5f);
                    float offsetInterpolant = Sin(PiOver2 * k / 6f + Main.GlobalTimeWrappedHourly * 0.74f) * 0.5f + 0.5f;
                    float rotationalOffset = Sin(TwoPi * k / 6f + Main.GlobalTimeWrappedHourly * 0.85f) * 0.8f;
                    float offset = Lerp(84f, 136f, offsetInterpolant) * spiritOpacity;
                    Vector2 drawPosition = drawPos - Vector2.UnitY.RotatedBy(rotationalOffset) * offset;
                    SpriteEffects direction = drawPosition.X < drawPos.X ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                    Rectangle frame = spiritTexture.Frame(1, 17, 0, spiritFrameY);

                    spriteBatch.Draw(spiritTexture, drawPosition, frame, Color.White * spiritOpacity * 0.55f, rotationalOffset * -0.5f, frame.Size() * 0.5f, 1f, direction, 0f);
                }
            }

            return false;
        }
    }
}
