using CalamityMod;
using CalamityMod.NPCs.Abyss;
using CalamityMod.NPCs.AcidRain;
using CalamityMod.NPCs.AquaticScourge;
using CalamityMod.NPCs.NormalNPCs;
using CalamityMod.NPCs.Signus;
using CalamityMod.NPCs.StormWeaver;
using InfernumMode.Content.Cutscenes;
using InfernumMode.Content.Subworlds;
using InfernumMode.Core.GlobalInstances.Systems;
using SubworldLibrary;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances
{
    public partial class GlobalNPCOverrides : GlobalNPC
    {
        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            if (player.Infernum_Biome().ZoneProfaned || SubworldSystem.IsActive<LostColosseum>() || CutsceneManager.ActiveCutscene != null)
            {
                spawnRate *= 40000;
                maxSpawns = 0;
            }

            // Make enemies much rarer in the blossom garden.
            if (player.WithinRange(WorldSaveSystem.BlossomGardenCenter.ToWorldCoordinates(), 3200f))
            {
                spawnRate *= 3;
                maxSpawns /= 3;
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

            // The sentinels occasionally spawn naturally in their respective biomes if they haven't been defeated yet.
            bool sentinelsCanSpawn = Main.hardMode && InfernumMode.CanUseCustomAIs && !spawnInfo.Player.Infernum_Biome().ZoneProfaned;
            if (sentinelsCanSpawn && !DownedBossSystem.downedSignus && spawnInfo.Player.ZoneUnderworldHeight && !WorldSaveSystem.MetSignusAtProfanedGarden && !NPC.AnyNPCs(ModContent.NPCType<Signus>()))
                pool[ModContent.NPCType<Signus>()] = 0.0032f;
            if (sentinelsCanSpawn && !DownedBossSystem.downedStormWeaver && spawnInfo.Player.ZoneSkyHeight && !NPC.AnyNPCs(ModContent.NPCType<StormWeaverHead>()))
                pool[ModContent.NPCType<StormWeaverHead>()] = 0.00876f;

            // Clear abyss miniboss spawns from the pool. They are always spawned manually, since traditional enemy spawns have a
            // tendency to be limited to spawning on solid ground.
            pool.Remove(ModContent.NPCType<ColossalSquid>());
            pool.Remove(ModContent.NPCType<Eidolist>());
            pool.Remove(ModContent.NPCType<EidolonWyrmHead>());
            pool.Remove(ModContent.NPCType<ReaperShark>());

            // Clear Nuclear Terror spawns from the pool. They are always spawned manually in Tier 3 twice at 50% and 95% of the event's completion.
            pool.Remove(ModContent.NPCType<NuclearTerror>());

            // Clear Devilfish and Toxic Minnows from the pool, so that they can be moved to a different location.
            pool.Remove(ModContent.NPCType<DevilFish>());
            pool.Remove(ModContent.NPCType<DevilFishAlt>());
            pool.Remove(ModContent.NPCType<ToxicMinnow>());
            if (spawnInfo.Player.Infernum_Biome().InLayer3HadalZone && spawnInfo.Water)
                pool[ModContent.NPCType<DevilFish>()] = 0.12f;
            if (spawnInfo.Player.Calamity().ZoneAbyssLayer1 && spawnInfo.Water)
                pool[ModContent.NPCType<ToxicMinnow>()] = 0.1f;

            // Clear the pool if a nuclear terror is present.
            if (NPC.AnyNPCs(ModContent.NPCType<NuclearTerror>()))
                pool.Clear();
        }
    }
}
