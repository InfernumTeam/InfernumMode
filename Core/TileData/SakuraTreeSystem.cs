//using CalamityMod;
//using InfernumMode.Content.Items.Misc;
//using InfernumMode.Content.Projectiles.Generic;
//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;
//using Mono.Cecil.Cil;
//using MonoMod.Cil;
//using System;
//using System.Reflection;
//using Terraria;
//using Terraria.GameContent.Drawing;
//using Terraria.ID;
//using Terraria.ModLoader;
//using Terraria.ModLoader.IO;

// I don't know why this broke, nor do I know how to fix it. So, until then, this system will be removed.
//namespace InfernumMode.Core.TileData
//{
//    public class SakuraTreeSystem : ModSystem
//    {
//        public struct BlossomData : ITileData
//        {
//            public byte PackedData;

//            public bool HasBlossom
//            {
//                readonly get => (PackedData & 1) == 1;
//                set => PackedData = (byte)(value ? 0b1 : 0b0);
//            }
//        }

//        public override void OnModLoad()
//        {
//            IL_TileDrawing.DrawTrees += DrawBlossom;
//        }

//        private void DrawBlossom(ILContext il)
//        {
//            ILCursor cursor = new(il);

//            MethodInfo drawMethod = typeof(SpriteBatch).GetMethod("Draw", new Type[]
//            {
//                typeof(Texture2D), // Texture.
//                typeof(Vector2), // Draw position.
//                typeof(Rectangle?), // Frame.
//                typeof(Color), // Draw color.
//                typeof(float), // Rotation.
//                typeof(Vector2), // Origin.
//                typeof(float), // Scale.
//                typeof(SpriteEffects), // Direction.
//                typeof(float) // Layer depth.
//            });

//            FieldInfo specialPositionsField = typeof(TileDrawing).GetField("_specialPositions", Utilities.UniversalBindingFlags);

//            // Firstly, locate the local index of the tile coordinate.
//            int iterationLocalIndex = 0;
//            int iterationLocalIndex2 = 0;
//            cursor.GotoNext(MoveType.Before, i => i.MatchLdfld(specialPositionsField));
//            cursor.GotoNext(i => i.MatchLdloc(out iterationLocalIndex));
//            cursor.GotoNext(i => i.MatchLdloc(out iterationLocalIndex2));

//            // Go through each instance GetTreeBranchTexture where is called.
//            while (cursor.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt<TileDrawing>("GetTreeBranchTexture")))
//            {
//                // Find the local index of the local variable in which the branch texture is stored.
//                int branchTextureLocalIndex = 0;
//                cursor.GotoNext(i => i.MatchStloc(out branchTextureLocalIndex));

//                // Find the first instance of the local index being used.
//                // This is guaranteed to be in the Main.spriteBatch.Draw call.
//                cursor.GotoNext(i => i.MatchLdloc(branchTextureLocalIndex));

//                // Since the above index is guaranteed to be in a Main.spriteBatch.Draw call, get the local index of the upcoming loaded local variable.
//                // This is guaranteed to be the draw position.
//                int drawPositionLocalIndex = 0;
//                cursor.GotoNext(i => i.MatchLdloc(out drawPositionLocalIndex));

//                // Now that the draw position has been located, move the point where the draw method is done being called.
//                cursor.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt(drawMethod));

//                // Insert the cherry blossom drawing logic.
//                cursor.Emit(OpCodes.Ldarg_0);
//                cursor.Emit(OpCodes.Ldfld, specialPositionsField);
//                cursor.Emit(OpCodes.Ldloc, iterationLocalIndex);
//                cursor.Emit(OpCodes.Ldelem_Ref);
//                cursor.Emit(OpCodes.Ldloc, iterationLocalIndex2);
//                cursor.Emit(OpCodes.Ldelem_Any, typeof(Point));

//                cursor.Emit(OpCodes.Ldloc, drawPositionLocalIndex);
//                cursor.EmitDelegate(DrawCherryBlossom);
//            }
//        }

//        public static bool HasSakura(Point p)
//        {
//            bool hasSakura = false;
//            bool hasBranch = false;
//            for (int i = -25; i < 25; i++)
//            {
//                Tile checkTile = CalamityUtils.ParanoidTileRetrieval(p.X, p.Y + i);
//                if (!checkTile.HasTile || checkTile.TileType != TileID.VanityTreeSakura)
//                    continue;

//                if (checkTile.Get<BlossomData>().HasBlossom)
//                    hasSakura = true;

//                if (checkTile.TileFrameY >= 198 && (checkTile.TileFrameX is 44 or 66))
//                    hasBranch = true;
//            }

//            return hasSakura && hasBranch;
//        }

//        public static void DrawCherryBlossom(Point tileCoords, Vector2 drawPosition)
//        {
//            Tile t = CalamityUtils.ParanoidTileRetrieval(tileCoords.X, tileCoords.Y);

//            if (t.TileType != TileID.VanityTreeSakura)
//                return;

//            // Check to see if the tree has special blossom data anywhere.
//            if (!HasSakura(tileCoords))
//                return;

//            Color color = Lighting.GetColor(tileCoords);
//            float budRotation = 0.1f;
//            Texture2D budTexture = ModContent.Request<Texture2D>(ModContent.GetInstance<SakuraBud>().Texture).Value;

//            Main.spriteBatch.Draw(budTexture, drawPosition - Vector2.UnitY * 6f, null, color, budRotation, budTexture.Size() * 0.5f, 1f, 0, 0f);

//            if (!Main.gamePaused && Main.rand.NextBool(360))
//                Utilities.NewProjectileBetter(drawPosition + Main.screenPosition, Main.rand.NextVector2Circular(2f, 0.1f), ModContent.ProjectileType<CherryBlossomPetal>(), 0, 0f, Main.myPlayer);
//        }

//        public override unsafe void SaveWorldData(TagCompound tag)
//        {
//            BlossomData[] blossomData = Main.tile.GetData<BlossomData>();
//            byte[] dataCopy = new byte[blossomData.Length];

//            // Safely get blossom binary tile data from the world and save it inside of the tag.
//            fixed (BlossomData* dataPtr = blossomData)
//            {
//                byte* bytePtr = (byte*)dataPtr;
//                Span<byte> blossomDataWrapper = new(bytePtr, blossomData.Length);
//                Span<byte> dataCopyWrapper = new(dataCopy);
//                blossomDataWrapper.CopyTo(dataCopyWrapper);
//            }

//            tag["BlossomData"] = dataCopy;
//        }

//        public override unsafe void LoadWorldData(TagCompound tag)
//        {
//            BlossomData[] blossomWorldDataRef = Main.tile.GetData<BlossomData>();
//            byte[] blossomData = tag.GetByteArray("BlossomData");

//            // Safely get blossom binary tile data from the tag and apply it to the world.
//            fixed (BlossomData* dataPtr = blossomWorldDataRef)
//            {
//                byte* bytePtr = (byte*)dataPtr;
//                Span<byte> blossomDataWrapper = new(bytePtr, blossomWorldDataRef.Length);
//                Span<byte> savedDataWrapper = new(blossomData);

//                if (savedDataWrapper.Length == blossomDataWrapper.Length)
//                    savedDataWrapper.CopyTo(blossomDataWrapper);
//            }
//        }
//    }
//}
