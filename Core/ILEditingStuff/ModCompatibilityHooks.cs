using CalamityMod.Tiles.FurnitureProfaned;
using CalamityMod.Tiles.LivingFire;
using CalamityMod.Walls;
using InfernumMode.Content.Tiles.Profaned;
using InfernumMode.Core.GlobalInstances.Systems;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ModLoader;
using static InfernumMode.Core.ILEditingStuff.HookManager;

namespace InfernumMode.Core.ILEditingStuff
{
    public class DisableFargosBreakingProfanedTempleHook : IHookEdit
    {
        internal static bool DisableProfanedTempleBreakage(Func<Tile, bool> orig, Tile tile)
        {
            bool profanedTempleTile = tile.TileType == ModContent.TileType<RunicProfanedBrick>() || tile.TileType == ModContent.TileType<ProfanedSlab>() || tile.TileType == ModContent.TileType<ProfanedRock>();
            profanedTempleTile |= tile.TileType == ModContent.TileType<GuardiansPlaque>() || tile.TileType == ModContent.TileType<ProvidenceSummoner>() || tile.TileType == ModContent.TileType<ProvidenceRoomDoorPedestal>();
            profanedTempleTile |= tile.TileType == ModContent.TileType<ProfanedCandelabra>() || tile.WallType == ModContent.WallType<RunicProfanedBrickWall>() || tile.WallType == ModContent.WallType<ProfanedSlabWall>();
            profanedTempleTile |= tile.TileType == ModContent.TileType<LivingHolyFireBlockTile>() || tile.WallType == ModContent.WallType<ProfanedCrystalWall>() || tile.WallType == ModContent.WallType<ProfanedRockWall>();
            profanedTempleTile |= tile.TileType == ModContent.TileType<ProfanedCrystal>();
            if (profanedTempleTile)
                return false;

            return orig(tile);
        }

        internal static void DisableProfanedTempleBreakageIL(ILContext il)
        {
            ILCursor cursor = new(il);
            cursor.GotoNext(MoveType.After, c => c.MatchStloc(4));

            int afterXCoords = cursor.Index;
            ILLabel skipLoop = null;
            cursor.GotoNext(MoveType.After, c => c.MatchBlt(out skipLoop));

            cursor.Goto(afterXCoords);
            cursor.Emit(OpCodes.Ldloc, 3);
            cursor.Emit(OpCodes.Ldloc, 4);
            cursor.EmitDelegate<Func<int, int, bool>>((x, y) =>
            {
                return !WorldSaveSystem.ProvidenceArena.Intersects(new(x, y, 1, 1));
            });
            cursor.Emit(OpCodes.Brfalse, skipLoop);
        }

        public void Load()
        {
            if (InfernumMode.FargosMutantMod is null)
                return;

            FargosCanDestroyTile += DisableProfanedTempleBreakage;
            FargosCanDestroyTileWithInstabridge += DisableProfanedTempleBreakageIL;
            FargosCanDestroyTileWithInstabridge2 += DisableProfanedTempleBreakageIL;
        }

        public void Unload()
        {
            if (InfernumMode.FargosMutantMod is null)
                return;

            FargosCanDestroyTile -= DisableProfanedTempleBreakage;
            FargosCanDestroyTileWithInstabridge -= DisableProfanedTempleBreakageIL;
            FargosCanDestroyTileWithInstabridge2 -= DisableProfanedTempleBreakageIL;
        }
    }
}