using CalamityMod;
using CalamityMod.NPCs.Abyss;
using CalamityMod.NPCs.PrimordialWyrm;
using InfernumMode.Content.Projectiles.Generic;
using InfernumMode.Content.WorldGeneration;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class AbyssMinibossSpawnSystem : ModSystem
    {
        public static bool MajorAbyssEnemyExists
        {
            get
            {
                if (NPC.AnyNPCs(ModContent.NPCType<PrimordialWyrmHead>()) || Utilities.AnyProjectiles(ModContent.ProjectileType<TerminusAnimationProj>()))
                    return true;
                if (NPC.AnyNPCs(ModContent.NPCType<EidolonWyrmHead>()))
                    return true;
                if (NPC.AnyNPCs(ModContent.NPCType<ReaperShark>()))
                    return true;

                return false;
            }
        }

        public const int MinibossSpawnRate = 2400;
        private static bool CycleThroughMiniboss;

        public override void PreUpdateWorld()
        {
            // Don't mess with abyss spawns in worlds without a reworked abyss.
            if (!WorldSaveSystem.InPostAEWUpdateWorld || MajorAbyssEnemyExists || !InfernumMode.CanUseCustomAIs)
                return;

            foreach (Player p in Main.ActivePlayers)
            {
                if (p == null || p.dead || p.ghost)
                    continue;

                if (!p.Calamity().ZoneAbyssLayer4 || p.Center.Y < CustomAbyss.Layer4Top * 16f + 1000f)
                    continue;

                if (!Main.rand.NextBool(MinibossSpawnRate) || typeof(NPC).GetField("maxSpawns", LumUtils.UniversalBindingFlags)?.GetValue(null) as int? == 0)
                    continue;

                AttemptToSpawnMiniboss(p);
                break;
            }
        }

        public static void AttemptToSpawnMiniboss(Player player)
        {
            // Spawn a miniboss a preset distance away from the target.
            int minibossID = CycleThroughMiniboss ? ModContent.NPCType<ReaperShark>() : ModContent.NPCType<EidolonWyrmHead>();
            CycleThroughMiniboss = !CycleThroughMiniboss;
            Vector2 minibossSpawnPosition = player.Center + Main.rand.NextVector2CircularEdge(1080f, 1080f);

            int miniboss = NPC.NewNPC(new EntitySource_WorldEvent(), (int)minibossSpawnPosition.X, (int)minibossSpawnPosition.Y, minibossID, 1);
            if (Main.npc.IndexInRange(miniboss))
                Main.npc[miniboss].netUpdate = true;
        }
    }
}
