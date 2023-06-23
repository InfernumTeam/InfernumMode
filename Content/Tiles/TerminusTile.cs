using CalamityMod;
using CalamityMod.NPCs.AdultEidolonWyrm;
using InfernumMode.Content.Projectiles.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace InfernumMode.Content.Tiles
{
    public class TerminusTile : ModTile
    {
        public const int Width = 3;

        public const int Height = 2;

        public static bool TerminusIsNotAttached => NPC.AnyNPCs(ModContent.NPCType<AdultEidolonWyrmHead>()) || Utilities.AnyProjectiles(ModContent.ProjectileType<TerminusAnimationProj>()) || DownedBossSystem.downedAdultEidolonWyrm || !InfernumMode.CanUseCustomAIs;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileSpelunker[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
            TileObjectData.newTile.Width = Width;
            TileObjectData.newTile.Height = Height;
            TileObjectData.newTile.Origin = new Point16(1, 1);
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16 };
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.addTile(Type);
            AddMapEntry(new Color(53, 52, 81));
        }

        // This tile should not be destroyed by any natural means.
        // If someone somehow destroys it (such as with those Fargos autoplatforms), tough luck for them. They can cheat in the Searune.
        public override bool CanExplode(int i, int j) => false;

        public override bool CanKillTile(int i, int j, ref bool blockDamaged) => false;

        // This hook name is slightly misleading, as it applies to both tile breakage AND pickaxe hits, regardless of whether they actually succeed or not.
        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            if (TerminusIsNotAttached)
                return;

            Tile t = CalamityUtils.ParanoidTileRetrieval(i, j);
            Vector2 terminusSpawnPosition = new Point(i, j).ToWorldCoordinates() - new Vector2(t.TileFrameX, t.TileFrameY) + Vector2.UnitX * 32f;
            Utilities.NewProjectileBetter(terminusSpawnPosition, Vector2.Zero, ModContent.ProjectileType<TerminusAnimationProj>(), 0, 0f);
        }

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile t = CalamityUtils.ParanoidTileRetrieval(i, j);

            // Due to tile layering fuckery the Terminus should only be drawn by the final subtile.
            // Doing it earlier could result in incoming tiles having weird layering artifacts, which would be undesirable.
            if (t.TileFrameX != 0 || t.TileFrameY != 0)
                return true;

            // Don't draw the Terminus if the Terminus animation is happening or the AEW fight is ongoing.
            if (TerminusIsNotAttached)
                return true;

            // Draw the Terminus at an angle below the rest of the rubble.
            float terminusRotation = PiOver2 - 0.43f;
            Texture2D terminusTexture = ModContent.Request<Texture2D>("CalamityMod/Items/SummonItems/Terminus").Value;
            Vector2 drawPosition = new Vector2(i * 16f, j * 16f) - Main.screenPosition + new Vector2(40f, -24f);
            if (!Main.drawToScreen)
                drawPosition += Vector2.One * Main.offScreenRange;

            spriteBatch.Draw(terminusTexture, drawPosition, null, Color.White, terminusRotation, Vector2.One, 1f, 0, 0f);
            return false;
        }
    }
}
