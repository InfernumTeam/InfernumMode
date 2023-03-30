using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.CeaselessVoid;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class CeaselessVoidArchivesSpawnSystem : ModSystem
    {
        public static bool WaitingForPlayersToLeaveArchives
        {
            get;
            set;
        }

        public override void PreUpdateWorld()
        {
            Vector2 voidSpawnPosition = WorldSaveSystem.ForbiddenArchiveCenter.ToWorldCoordinates() + Vector2.UnitY * 1332f;
            if (WaitingForPlayersToLeaveArchives && !Main.player[Player.FindClosest(voidSpawnPosition, 1, 1)].WithinRange(voidSpawnPosition, 2700f))
                WaitingForPlayersToLeaveArchives = false;

            if (!InfernumMode.CanUseCustomAIs || WorldSaveSystem.ForbiddenArchiveCenter == Point.Zero || WaitingForPlayersToLeaveArchives)
                return;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (!p.dead && p.active && !CalamityUtils.AnyBossNPCS() && p.ZoneDungeon && p.WithinRange(voidSpawnPosition, 2000f) && CalamityGlobalNPC.voidBoss == -1)
                {
                    int ceaselessVoid = NPC.NewNPC(new EntitySource_WorldEvent(), (int)voidSpawnPosition.X, (int)voidSpawnPosition.Y, ModContent.NPCType<CeaselessVoid>(), 0, 0f, 0f, 0f, 1f);
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, ceaselessVoid);
                    break;
                }
            }
        }
    }
}