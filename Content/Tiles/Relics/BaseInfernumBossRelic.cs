using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace InfernumMode.Content.Tiles.Relics
{
    public abstract class BaseInfernumBossRelic : ModTile
    {
        public const int FrameWidth = 18 * 3;
        public const int FrameHeight = 18 * 4;
        public const int HorizontalFrames = 1;
        public const int VerticalFrames = 1;

        public Asset<Texture2D> RelicTexture;
        public Asset<Texture2D> RelicGlowTexture;
        public Asset<Texture2D> RelicBaseTexture;
        public Asset<Texture2D> RelicBaseGlowTexture;

        public abstract string RelicTextureName { get; }

        public virtual string RelicGlowTextureName => RelicTextureName + "Glow";

        public abstract int DropItemID { get; }

        public override string Texture => "InfernumMode/Content/Tiles/Relics/Pedestal";

        public static string DrawTexture => "InfernumMode/Content/Tiles/Relics/Pedestal";

        public static string RelicBaseGlowTextureName => "InfernumMode/Content/Tiles/Relics/PedestalGlow";

        public override void Load()
        {
            if (!Main.dedServ)
            {
                // Cache the extra texture displayed on the pedestal
                RelicTexture = ModContent.Request<Texture2D>(RelicTextureName);
                RelicGlowTexture = ModContent.Request<Texture2D>(RelicGlowTextureName);
                RelicBaseTexture = ModContent.Request<Texture2D>(DrawTexture);
                RelicBaseGlowTexture = ModContent.Request<Texture2D>(RelicBaseGlowTextureName);
            }
        }
        public override void Unload()
        {
            // Unload the extra texture displayed on the pedestal
            RelicTexture = null;
            RelicGlowTexture = null;
            RelicBaseTexture = null;
            RelicBaseGlowTexture = null;
        }

        public override void SetStaticDefaults()
        {
            RegisterItemDrop(DropItemID);
            Main.shine(Color.DarkRed, Type);
            Main.tileFrameImportant[Type] = true; // Any multitile requires this
            TileID.Sets.InteractibleByNPCs[Type] = true; // Town NPCs will palm their hand at this tile

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x4); // Relics are 3x4
            TileObjectData.newTile.LavaDeath = false; // Does not break when lava touches it
            TileObjectData.newTile.DrawYOffset = 2; // So the tile sinks into the ground
            TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft; // Player faces to the left
            TileObjectData.newTile.StyleHorizontal = false; // Based on how the alternate sprites are positioned on the sprite (by default, true)

            // Optional: If you decide to make your tile utilize different styles through Item.placeStyle, you need these, aswell as the code in SetDrawPositions
            // TileObjectData.newTile.StyleWrapLimitVisualOverride = 2;
            // TileObjectData.newTile.StyleMultiplier = 2;
            // TileObjectData.newTile.StyleWrapLimit = 2;
            // TileObjectData.newTile.styleLineSkipVisualOverride = 0;

            // Register an alternate tile data with flipped direction
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile); // Copy everything from above, saves us some code
            TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight; // Player faces to the right
            TileObjectData.addAlternate(1);

            // Register the tile data itself
            TileObjectData.addTile(Type);

            // Register map name and color
            // "MapObject.Relic" refers to the translation key for the vanilla "Relic" text
            AddMapEntry(new Color(41, 32, 48), Language.GetText("MapObject.Relic"));
        }

        public override bool CreateDust(int i, int j, ref int type) => false;

        public override void KillMultiTile(int i, int j, int frameX, int frameY)
        {
            //Item.NewItem(new EntitySource_TileBreak(i, j), i * 16, j * 16, 32, 32, DropItemID);
        }

        public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY)
        {
            // Only required If you decide to make your tile utilize different styles through Item.placeStyle

            // This preserves its original frameX/Y which is required for determining the correct texture floating on the pedestal, but makes it draw properly
            tileFrameX %= FrameWidth; // Clamps the frameX
            tileFrameY %= FrameHeight * 2; // Clamps the frameY (two horizontally aligned place styles, hence * 2)
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
            Texture2D texture = RelicTexture.Value;
            Texture2D glowTexture = RelicGlowTexture.Value;

            int frameY = tile.TileFrameX / FrameWidth; // Picks the frame on the sheet based on the placeStyle of the item
            Rectangle frame = texture.Frame(HorizontalFrames, VerticalFrames, 0, frameY);

            Vector2 origin = frame.Size() / 2f;
            Vector2 worldPos = p.ToWorldCoordinates(24f, 64f);

            Color color = Lighting.GetColor(p.X, p.Y);

            bool direction = tile.TileFrameY / FrameHeight != 0; // This is related to the alternate tile data we registered before

            SpriteEffects effects = direction ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            // Some math magic to make it smoothly move up and down over time
            float offset = Sin(Main.GlobalTimeWrappedHourly * TwoPi / 5f);
            Vector2 drawPos = worldPos + offScreen - Main.screenPosition + new Vector2(0f, -40f) + new Vector2(0f, offset * 4f);

            // Draw the main texture
            spriteBatch.Draw(texture, drawPos, frame, color, 0f, origin, 1f, effects, 0f);
            spriteBatch.Draw(glowTexture, drawPos, frame, Color.White, 0f, origin, 1f, effects, 0f);

            // Draw the periodic glow effect
            float scale = Sin(Main.GlobalTimeWrappedHourly * TwoPi / 2f) * 0.3f + 0.7f;
            Color effectColor = color;
            effectColor.A = 0;
            effectColor = effectColor * 0.1f * scale;
            for (float interpolant = 0f; interpolant < 1f; interpolant += 1f / 16f)
                spriteBatch.Draw(texture, drawPos + (TwoPi * interpolant).ToRotationVector2() * (6f + offset * 2f), frame, effectColor, 0f, origin, 1f, effects, 0f);
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            int xFrameOffset = Main.tile[i, j].TileFrameX;
            int yFrameOffset = Main.tile[i, j].TileFrameY;
            Texture2D glowmask = RelicBaseGlowTexture.Value;
            Vector2 drawOffest = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawPosition = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y) + drawOffest;
            Color drawColour = Color.White;
            spriteBatch.Draw(glowmask, drawPosition, new Rectangle(xFrameOffset, yFrameOffset, 18, 18), drawColour, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        }
    }
}
