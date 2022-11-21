using CalamityMod;
using CalamityMod.NPCs.Abyss;
using CalamityMod.NPCs.NormalNPCs;
using InfernumMode.Subworlds;
using InfernumMode.Systems;
using SubworldLibrary;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.GlobalInstances
{
    public partial class GlobalNPCOverrides : GlobalNPC
    {
        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            if (player.Infernum().ZoneProfaned || SubworldSystem.IsActive<LostColosseum>())
            {
                spawnRate *= 40000;
                maxSpawns = 0;
            }

            if (player.Calamity().ZoneAbyssLayer4 && WorldSaveSystem.InPostAEWUpdateWorld)
            {
                spawnRate *= 40000;
                maxSpawns = 0;
            }
        }

        public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
        {
            // Don't mess with abyss spawns in worlds without a reworked abyss.
            if (!WorldSaveSystem.InPostAEWUpdateWorld)
                return;

            // Don't spawn anything naturally in layer 4. The miniboss spawns will be handled manually. The only exception to this is bobbit worms.
            if (spawnInfo.Player.Calamity().ZoneAbyssLayer4)
                pool = pool.Where(p => p.Key == ModContent.NPCType<BobbitWormHead>()).ToDictionary(kv => kv.Key, kv => kv.Value);

            // Clear abyss miniboss spawns from the pool. They are always spawned manually, sincetraditional enemy spawns have a
            // tendency to be limited to spawning on sold ground.
            pool.Remove(ModContent.NPCType<ColossalSquid>());
            pool.Remove(ModContent.NPCType<Eidolist>());
            pool.Remove(ModContent.NPCType<EidolonWyrmHead>());
            pool.Remove(ModContent.NPCType<ReaperShark>());
        }
    }
}