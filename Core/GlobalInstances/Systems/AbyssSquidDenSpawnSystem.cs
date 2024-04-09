using CalamityMod;
using CalamityMod.NPCs.Abyss;
using InfernumMode.Content.WorldGeneration;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class AbyssSquidDenSpawnSystem : ModSystem
    {
        public const int GiantSquidSpawnRate = 180;

        public const int ColossalSquidRespawnRate = 1500;

        public override void PreUpdateWorld()
        {
            // Don't mess with abyss spawns in worlds without a reworked abyss.
            if (!WorldSaveSystem.InPostAEWUpdateWorld)
                return;

            if (Main.rand.NextBool(ColossalSquidRespawnRate))
                RespawnColossalSquid();

            int squidID = ModContent.NPCType<GiantSquid>();
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p.dead || !p.active)
                    continue;

                if (!p.Calamity().ZoneAbyss)
                    continue;

                if (!Main.rand.NextBool(GiantSquidSpawnRate) || NPC.CountNPCS(squidID) >= 6)
                    continue;

                AttemptToSpawnSquid(p, squidID);
                break;
            }
        }

        public override void OnWorldLoad() => RespawnColossalSquid();

        public static void AttemptToSpawnSquid(Player player, int squidID)
        {
            int colossalSquidIndex = NPC.FindFirstNPC(ModContent.NPCType<ColossalSquid>());
            if (colossalSquidIndex >= 0 && Main.npc[colossalSquidIndex].Infernum().ExtraAI[5] == 1f)
                return;

            // Spawn a squid in the squid den.
            Vector2 squidSpawnPosition;
            do
            {
                squidSpawnPosition = WorldSaveSystem.SquidDenCenter.ToWorldCoordinates() + Main.rand.NextVector2Circular(CustomAbyss.Layer3SquidDenOuterRadius, CustomAbyss.Layer3SquidDenOuterRadius) * 14f;
            }
            while (Collision.SolidCollision(squidSpawnPosition - Vector2.One * 150f, 300, 300));

            if (player.WithinRange(squidSpawnPosition, 660f))
                return;

            NPC.NewNPC(new EntitySource_WorldEvent(), (int)squidSpawnPosition.X, (int)squidSpawnPosition.Y, squidID, 1);
        }

        public static void RespawnColossalSquid()
        {
            if (!WorldSaveSystem.InPostAEWUpdateWorld || Main.netMode == NetmodeID.MultiplayerClient || !InfernumMode.CanUseCustomAIs || NPC.AnyNPCs(ModContent.NPCType<ColossalSquid>()))
                return;

            Vector2 squidSpawnPosition = Utilities.GetGroundPositionFrom(WorldSaveSystem.SquidDenCenter.ToWorldCoordinates());

            // Don't spawn the squid if a player is really close to where it'd be.
            Player closestPlayer = Main.player[Player.FindClosest(squidSpawnPosition, 1, 1)];
            if (closestPlayer.WithinRange(squidSpawnPosition, 450f))
                return;

            int squidIndex = NPC.NewNPC(new EntitySource_WorldEvent(), (int)squidSpawnPosition.X, (int)squidSpawnPosition.Y, ModContent.NPCType<ColossalSquid>(), 1);
            if (Main.npc.IndexInRange(squidIndex))
                Main.npc[squidIndex].Bottom = squidSpawnPosition;
        }
    }
}
