using CalamityMod;
using CalamityMod.NPCs.Abyss;
using CalamityMod.NPCs.AdultEidolonWyrm;
using InfernumMode.Projectiles;
using InfernumMode.WorldGeneration;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace InfernumMode.Systems
{
    public class AbyssMinibossSpawnSystem : ModSystem
    {
        public static bool MajorAbyssEnemyExists
        {
            get
            {
                if (NPC.AnyNPCs(ModContent.NPCType<AdultEidolonWyrmHead>()) || Utilities.AnyProjectiles(ModContent.ProjectileType<TerminusAnimationProj>()))
                    return true;
                if (NPC.AnyNPCs(ModContent.NPCType<EidolonWyrmHead>()))
                    return true;
                if (NPC.AnyNPCs(ModContent.NPCType<ReaperShark>()))
                    return true;

                return false;
            }
        }

        public const int MinibossSpawnRate = 1380;
        
        public override void PreUpdateWorld()
        {
            // Don't mess with abyss spawns in worlds without a reworked abyss.
            if (!WorldSaveSystem.InPostAEWUpdateWorld || MajorAbyssEnemyExists)
                return;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p.dead || !p.active)
                    continue;

                if (!p.Calamity().ZoneAbyssLayer4 || p.Center.Y < CustomAbyss.Layer4Top * 16f + 1000f)
                    continue;

                if (!Main.rand.NextBool(MinibossSpawnRate))
                    continue;

                AttemptToSpawnMiniboss(p);
                break;
            }
        }

        public static void AttemptToSpawnMiniboss(Player player)
        {
            // Spawn a miniboss a preset distance away from the target.
            int minibossID = ModContent.NPCType<ReaperShark>();
            Vector2 minibossSpawnPosition = player.Center + Main.rand.NextVector2CircularEdge(1080f, 1080f);
            NPC.NewNPC(new EntitySource_WorldEvent(), (int)minibossSpawnPosition.X, (int)minibossSpawnPosition.Y, minibossID, 1);
        }
    }
}