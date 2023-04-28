using CalamityMod;
using CalamityMod.Tiles.Abyss;
using InfernumMode.Content.Achievements;
using InfernumMode.Content.Items;
using InfernumMode.Content.Subworlds;
using InfernumMode.Content.Tiles;
using InfernumMode.Content.Tiles.Abyss;
using InfernumMode.Core.GlobalInstances.Players;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.TileData;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SubworldLibrary;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances
{
    public class InfernumGlobalTile : GlobalTile
    {
        public static bool ShouldNotBreakDueToAboveTile(int x, int y)
        {
            int[] invincibleTiles = new int[]
            {
                ModContent.TileType<BrimstoneRose>(),
                ModContent.TileType<ColosseumPortal>(),
                ModContent.TileType<EggSwordShrine>(),
                ModContent.TileType<IronBootsSkeleton>(),
                ModContent.TileType<ProvidenceSummoner>(),
                ModContent.TileType<ProvidenceRoomDoorPedestal>(),
                ModContent.TileType<StrangeOrbTile>(),
            };

            Tile checkTile = CalamityUtils.ParanoidTileRetrieval(x, y);
            Tile aboveTile = CalamityUtils.ParanoidTileRetrieval(x, y - 1);

            // Prevent tiles below invincible tiles from being destroyed. This is like chests in vanilla.
            return aboveTile.HasTile && checkTile.TileType != aboveTile.TileType && invincibleTiles.Contains(aboveTile.TileType);
        }

        public override bool CanExplode(int i, int j, int type)
        {
            if (ShouldNotBreakDueToAboveTile(i, j))
                return false;

            if (WorldSaveSystem.ProvidenceArena.Intersects(new(i, j, 1, 1)) || SubworldSystem.IsActive<LostColosseum>())
                return false;

            return base.CanExplode(i, j, type);
        }

        public override bool CanKillTile(int i, int j, int type, ref bool blockDamaged)
        {
            if (ShouldNotBreakDueToAboveTile(i, j))
                return false;

            if (WorldSaveSystem.ProvidenceArena.Intersects(new(i, j, 1, 1)) || SubworldSystem.IsActive<LostColosseum>())
                return false;

            if (CalamityUtils.ParanoidTileRetrieval(i, j - 1).TileType == ModContent.TileType<AbyssalKelp>())
                WorldGen.KillTile(i, j - 1);

            return base.CanKillTile(i, j, type, ref blockDamaged);
        }

        public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            // Trigger achievement checks.
            if (Main.netMode != NetmodeID.Server)
                AchievementPlayer.ExtraUpdateHandler(Main.LocalPlayer, AchievementUpdateCheck.TileBreak, type);

            if (type == TileID.VanityTreeSakura && SakuraTreeSystem.HasSakura(new(i, j)))
                AchievementPlayer.ExtraUpdateHandler(Main.LocalPlayer, AchievementUpdateCheck.Sakura);
        }

        public override bool PreDraw(int i, int j, int type, SpriteBatch spriteBatch)
        {
            if (type == ModContent.TileType<LumenylCrystals>())
                return false;

            return base.PreDraw(i, j, type, spriteBatch);
        }

        public override void NearbyEffects(int i, int j, int type, bool closer)
        {
            bool tombstonesShouldSpontaneouslyCombust = WorldSaveSystem.ProvidenceArena.Intersects(new(i, j, 16, 16)) || SubworldSystem.IsActive<LostColosseum>();
            if (tombstonesShouldSpontaneouslyCombust && type is TileID.Tombstones)
                WorldGen.KillTile(i, j);
        }

        public override void RandomUpdate(int i, int j, int type)
        {
            if (type == TileID.VanityTreeSakura && Main.rand.NextBool(100))
                Main.tile[i, j].Get<SakuraTreeSystem.BlossomData>().HasBlossom = true;
        }
    }
}
