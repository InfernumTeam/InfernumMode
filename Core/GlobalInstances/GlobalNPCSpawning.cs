using CalamityMod;
using CalamityMod.NPCs.Abyss;
using CalamityMod.NPCs.AquaticScourge;
using CalamityMod.NPCs.NormalNPCs;
using InfernumMode.Content.Subworlds;
using InfernumMode.Core.GlobalInstances.Systems;
using SubworldLibrary;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.GlobalInstances
{
    public partial class GlobalNPCOverrides : GlobalNPC
    {
        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            if (player.Infernum_Biome().ZoneProfaned || SubworldSystem.IsActive<LostColosseum>())
            {
                spawnRate *= 40000;
                maxSpawns = 0;
            }
        }

        public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
        {
            // Disable natural Aquatic Scourge spawns.
            if (InfernumMode.CanUseCustomAIs)
                pool.Remove(ModContent.NPCType<AquaticScourgeHead>());

            // Don't mess with abyss spawns in worlds without a reworked abyss.
            if (!WorldSaveSystem.InPostAEWUpdateWorld)
                return;

            // Don't spawn anything naturally in layer 4. The miniboss spawns will be handled manually.
            if (spawnInfo.Player.Calamity().ZoneAbyssLayer4)
            {
                pool.Clear();

                if (!AbyssMinibossSpawnSystem.MajorAbyssEnemyExists)
                {
                    pool[ModContent.NPCType<Bloatfish>()] = 0.1f;
                    pool[ModContent.NPCType<BobbitWormHead>()] = 0.08f;
                }
            }

            // Clear abyss miniboss spawns from the pool. They are always spawned manually, since traditional enemy spawns have a
            // tendency to be limited to spawning on solid ground.
            pool.Remove(ModContent.NPCType<ColossalSquid>());
            pool.Remove(ModContent.NPCType<Eidolist>());
            pool.Remove(ModContent.NPCType<EidolonWyrmHead>());
            pool.Remove(ModContent.NPCType<ReaperShark>());

            // Clear Devilfish and Toxic Minnows from the pool, so that they can be moved to a different location.
            pool.Remove(ModContent.NPCType<DevilFish>());
            pool.Remove(ModContent.NPCType<DevilFishAlt>());
            pool.Remove(ModContent.NPCType<ToxicMinnow>());
            if (spawnInfo.Player.Infernum_Biome().InLayer3HadalZone && spawnInfo.Water)
                pool[ModContent.NPCType<DevilFish>()] = 0.12f;
            if (spawnInfo.Player.Calamity().ZoneAbyssLayer1 && spawnInfo.Water)
                pool[ModContent.NPCType<ToxicMinnow>()] = 0.1f;
        }
    }
}
