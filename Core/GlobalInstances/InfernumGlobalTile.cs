using CalamityMod;
using CalamityMod.Tiles.Abyss;
using InfernumMode.Content.Achievements;
using InfernumMode.Content.Subworlds;
using InfernumMode.Content.Tiles.Abyss;
using InfernumMode.Content.Tiles.Colosseum;
using InfernumMode.Content.Tiles.Misc;
using InfernumMode.Content.Tiles.Profaned;
using InfernumMode.Content.Tiles.Wishes;
using InfernumMode.Core.GlobalInstances.Players;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework.Graphics;
using SubworldLibrary;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using InfernumMode.Content.Achievements.DevWishes;

namespace InfernumMode.Core.GlobalInstances
{
    public class InfernumGlobalTile : GlobalTile
    {
        public static int LumenylCrystalID
        {
            get;
            private set;
        }

        public override void Load()
        {
            LumenylCrystalID = ModContent.TileType<LumenylCrystals>();
            On_WorldGen.ShakeTree += ShakeTree;
        }

        private void ShakeTree(On_WorldGen.orig_ShakeTree orig, int i, int j)
        {
            orig(i, j);
            if (Main.netMode == NetmodeID.Server)
                return;

            if (Main.LocalPlayer.GetModPlayer<AchievementPlayer>().achievements.Any(achievement => achievement is SakuraWish && achievement.DoneCompletionEffects))
                return;

            Tile tile = CalamityUtils.ParanoidTileRetrieval(i, j);
            if (tile.HasTile && tile.TileType == TileID.VanityTreeSakura)
            {
                AchievementPlayer.ExtraUpdateHandler(Main.LocalPlayer, AchievementUpdateCheck.Sakura);
            }
        }

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

            bool wofBlock = type is TileID.CrimtaneBrick or TileID.DemoniteBrick;
            if ((WorldSaveSystem.ProvidenceArena.Intersects(new(i, j, 1, 1)) && !wofBlock) || SubworldSystem.IsActive<LostColosseum>())
                return false;

            if (CalamityUtils.ParanoidTileRetrieval(i, j - 1).TileType == ModContent.TileType<AbyssalKelp>())
                WorldGen.KillTile(i, j - 1);

            return base.CanKillTile(i, j, type, ref blockDamaged);
        }

        public override bool CanReplace(int i, int j, int type, int tileTypeBeingPlaced)
        {
            if (WorldSaveSystem.ProvidenceArena.Intersects(new(i, j, 1, 1)) || SubworldSystem.IsActive<LostColosseum>())
                return false;

            return base.CanPlace(i, j, type);
        }

        public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            // Trigger achievement checks.
            if (Main.netMode != NetmodeID.Server)
                AchievementPlayer.ExtraUpdateHandler(Main.LocalPlayer, AchievementUpdateCheck.TileBreak, type);
        }

        public override bool PreDraw(int i, int j, int type, SpriteBatch spriteBatch)
        {
            if (type == LumenylCrystalID)
                return false;

            return base.PreDraw(i, j, type, spriteBatch);
        }

        public override void NearbyEffects(int i, int j, int type, bool closer)
        {
            bool tombstonesShouldSpontaneouslyCombust = WorldSaveSystem.ProvidenceArena.Intersects(new(i, j, 16, 16)) || SubworldSystem.IsActive<LostColosseum>();
            if (tombstonesShouldSpontaneouslyCombust && type is TileID.Tombstones)
                WorldGen.KillTile(i, j);
        }
    }
}
