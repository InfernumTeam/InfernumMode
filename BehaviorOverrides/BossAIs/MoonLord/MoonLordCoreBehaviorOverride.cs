using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class MoonLordCoreBehaviorOverride : NPCBehaviorOverride
    {
        public enum MoonLordAttackState
        {
            Teleport = -2,
            Initializations,
            InvulnerableFlyToTarget,
            VulnerableFlyToTarget,
            DeathEffects,
            Despawn
        }

        public const int ArenaWidth = 200;
        public const int ArenaHeight = 150;
        public const int ArenaHorizontalStandSpace = 70;
        public const int ArenaStandSpaceHeight = 19;
        public static readonly Color OverallTint = new Color(7, 81, 81);
        public override int NPCOverrideType => NPCID.MoonLordCore;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            // Stop rain.
            CalamityMod.CalamityMod.StopRain();

            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];

            // Player variable.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Reset invulnerability.
            npc.dontTakeDamage = NPC.CountNPCS(NPCID.MoonLordFreeEye) >= 3;

            // Life Ratio
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Start the AI up and create the arena.
            if (npc.localAI[3] == 0f)
            {
                npc.netUpdate = true;

                Player closest = Main.player[Player.FindClosest(npc.Center, 1, 1)];
                if (npc.Infernum().arenaRectangle == null)
                    npc.Infernum().arenaRectangle = default;

                Point closestTileCoords = closest.Center.ToTileCoordinates();
                npc.Infernum().arenaRectangle = new Rectangle((int)closest.position.X - ArenaWidth * 8, (int)closest.position.Y - ArenaHeight * 8 + 20, ArenaWidth * 16, ArenaHeight * 16);
                for (int i = closestTileCoords.X - ArenaWidth / 2; i <= closestTileCoords.X + ArenaWidth / 2; i++)
                {
                    for (int j = closestTileCoords.Y - ArenaHeight / 2; j <= closestTileCoords.Y + ArenaHeight / 2; j++)
                    {
                        int relativeX = i - closestTileCoords.X + ArenaWidth / 2;
                        int relativeY = j - closestTileCoords.Y + ArenaHeight / 2;
                        bool withinArenaStand = relativeX > ArenaHorizontalStandSpace && relativeX < ArenaWidth - ArenaHorizontalStandSpace &&
                                                relativeY > ArenaHeight - ArenaStandSpaceHeight;

                        // Create arena tiles.
                        if ((Math.Abs(closestTileCoords.X - i) == ArenaWidth / 2 || Math.Abs(closestTileCoords.Y - j) == ArenaHeight / 2 || withinArenaStand) && !Main.tile[i, j].active())
                        {
                            Main.tile[i, j].type = (ushort)ModContent.TileType<Tiles.MoonlordArena>();
                            Main.tile[i, j].active(true);
                            if (Main.netMode == NetmodeID.Server)
                                NetMessage.SendTileSquare(-1, i, j, 1, TileChangeType.None);
                            else
                                WorldGen.SquareTileFrame(i, j, true);
                        }
                    }
                }
                npc.localAI[3] = 1f;
                attackState = (int)MoonLordCoreAttackState.Initializations;
                npc.netUpdate = true;
            }
            return false;
        }
    }
}
