using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs.Abyss;
using CalamityMod.NPCs.AcidRain;
using Microsoft.Xna.Framework;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class NuclearTerrorSpawnSystem : ModSystem
    {
        public static bool WaitingForNuclearTerrorSpawn
        {
            get;
            set;
        }

        public static int TotalNuclearTerrorsKilledInEvent
        {
            get;
            set;
        }

        public override void PreUpdateWorld()
        {
            // Don't spawn anything if Infernum is disabled, Acid Rain is not happening, or if not in Tier 3.
            if (!InfernumMode.CanUseCustomAIs || !AcidRainEvent.AcidRainEventIsOngoing || !DownedBossSystem.downedPolterghast)
            {
                TotalNuclearTerrorsKilledInEvent = 0;
                return;
            }

            if (!WaitingForNuclearTerrorSpawn)
            {
                CheckIfTerrorSpawnIsNecessary();
                return;
            }

            // Don't spawn more than one Nuclear Terror.
            if (NPC.AnyNPCs(ModContent.NPCType<NuclearTerror>()))
                return;

            // If there is no valid player, don't try to spawn one.
            if (!Main.player.Any(p => p.active && !p.dead && p.Calamity().ZoneSulphur))
                return;

            Player playerToSpawnTerrorOn = Main.player.FirstOrDefault(p => p.active && !p.dead && p.Calamity().ZoneSulphur);
            int nuclearTerror = NPC.NewNPC(new EntitySource_WorldEvent(), (int)playerToSpawnTerrorOn.Center.X, (int)playerToSpawnTerrorOn.Center.Y + 900, ModContent.NPCType<NuclearTerror>(), 1);
            Main.npc[nuclearTerror].netUpdate = true;
        }

        public static void CheckIfTerrorSpawnIsNecessary()
        {
            if (1f - AcidRainEvent.AcidRainCompletionRatio >= 0.5f && TotalNuclearTerrorsKilledInEvent <= 0)
                WaitingForNuclearTerrorSpawn = true;
            if (1f - AcidRainEvent.AcidRainCompletionRatio >= 0.95f && TotalNuclearTerrorsKilledInEvent <= 1)
                WaitingForNuclearTerrorSpawn = true;
        }

        public static void AttemptToSpawnMiniboss(Player player)
        {
            // Spawn a miniboss a preset distance away from the target.
            int minibossID = Main.rand.NextFromList(ModContent.NPCType<ReaperShark>(), ModContent.NPCType<EidolonWyrmHead>());
            Vector2 minibossSpawnPosition = player.Center + Main.rand.NextVector2CircularEdge(1080f, 1080f);

            int miniboss = NPC.NewNPC(new EntitySource_WorldEvent(), (int)minibossSpawnPosition.X, (int)minibossSpawnPosition.Y, minibossID, 1);
            if (Main.npc.IndexInRange(miniboss))
                Main.npc[miniboss].netUpdate = true;
        }
    }
}
