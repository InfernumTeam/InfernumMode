using System.Linq;
using CalamityMod;
using InfernumMode.Content.Achievements.DevWishes;
using InfernumMode.Content.Projectiles.Rogue;
using InfernumMode.Core.GlobalInstances.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class StormMaidensRetributionSpawnSystem : ModSystem
    {
        public static bool SpearCanBeSummoned
        {
            get
            {
                if (!Main.raining)
                    return false;

                if (!DownedBossSystem.downedCalamitas || !DownedBossSystem.downedExoMechs)
                    return false;

                if (Utilities.AnyProjectiles(ModContent.ProjectileType<StormMaidensRetributionWorldProj>()))
                    return false;

                return true;
            }
        }

        public override void PostUpdateWorld()
        {
            if (!Main.rand.NextBool(1800) || !SpearCanBeSummoned)
                return;

            Vector2 spawnCenter = new Vector2(Main.spawnTileX, Main.spawnTileY) * 16f;
            Player closestPlayer = Main.player[Player.FindClosest(spawnCenter, 1, 1)];
            if (closestPlayer.GetModPlayer<AchievementPlayer>().AchievementInstances.First(a => a.DisplayName.Value == Utilities.GetLocalization($"Achievements.Wishes.{nameof(StormMaidenWish)}.DisplayName").Value).DoneCompletionEffects)
                return;

            // Spawn the spear from above if a player is close enough to spawn.
            if (closestPlayer.WithinRange(spawnCenter, 1800f))
                Utilities.NewProjectileBetter(closestPlayer.Center + new Vector2(Main.rand.NextFromList(-1f, 1f) * Main.rand.NextFloat(150f, 500f), -1200f), Vector2.UnitY * 4f, ModContent.ProjectileType<StormMaidensRetributionWorldProj>(), 0, 0f);
        }
    }
}