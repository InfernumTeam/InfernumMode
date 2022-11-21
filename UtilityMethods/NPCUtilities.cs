using CalamityMod;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace InfernumMode
{
    public static partial class Utilities
    {
        public const float DefaultTargetRedecideThreshold = 4000f;
        public static void TargetClosestIfTargetIsInvalid(this NPC npc, float distanceThreshold = DefaultTargetRedecideThreshold)
        {
            bool invalidTargetIndex = npc.target is < 0 or >= 255;
            if (invalidTargetIndex)
            {
                npc.TargetClosest();
                return;
            }

            Player target = Main.player[npc.target];
            bool invalidTarget = target.dead || !target.active || target.Infernum().EelSwallowIndex >= 0;
            if (invalidTarget)
                npc.TargetClosest();

            if (distanceThreshold >= 0f && !npc.WithinRange(target.Center, distanceThreshold - target.aggro))
                npc.TargetClosest();
        }


        public static NPC CurrentlyFoughtBoss
        {
            get
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].IsABoss())
                        return Main.npc[i].realLife >= 0 ? Main.npc[Main.npc[i].realLife] : Main.npc[i];
                }
                return null;
            }
        }

        public static bool IsExoMech(NPC npc)
        {
            // Thanatos.
            if (npc.type == ModContent.NPCType<ThanatosHead>() ||
                npc.type == ModContent.NPCType<ThanatosBody1>() ||
                npc.type == ModContent.NPCType<ThanatosBody2>() ||
                npc.type == ModContent.NPCType<ThanatosTail>())
            {
                return true;
            }

            // Ares.
            if (npc.type == ModContent.NPCType<AresBody>() ||
                npc.type == ModContent.NPCType<AresLaserCannon>() ||
                npc.type == ModContent.NPCType<AresTeslaCannon>() ||
                npc.type == ModContent.NPCType<AresPlasmaFlamethrower>() ||
                npc.type == ModContent.NPCType<AresGaussNuke>() ||
                npc.type == ModContent.NPCType<AresPulseCannon>() ||
                npc.type == ModContent.NPCType<PhotonRipperNPC>())
            {
                return true;
            }

            // Artemis and Apollo.
            if (npc.type == ModContent.NPCType<Artemis>() ||
                npc.type == ModContent.NPCType<Apollo>())
            {
                return true;
            }

            return false;
        }

        public static NPC FindClosestAbyssPredator(this NPC npc, out float distanceToClosestPredator)
        {
            NPC closestPredator = null;
            distanceToClosestPredator = 9999999f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (!Main.npc[i].active || !Main.npc[i].Infernum().IsAbyssPredator)
                    continue;

                float extraDistance = (Main.npc[i].width / 2) + (Main.npc[i].height / 2);
                extraDistance *= extraDistance;

                bool canHit = Collision.CanHit(npc.Center, 1, 1, Main.npc[i].Center, 1, 1);
                if (Vector2.DistanceSquared(npc.Center, Main.npc[i].Center) < (distanceToClosestPredator + extraDistance) && canHit)
                {
                    distanceToClosestPredator = Vector2.DistanceSquared(npc.Center, Main.npc[i].Center);
                    closestPredator = Main.npc[i];
                }
            }

            // Apply a square root on the squared distance.
            distanceToClosestPredator = (float)Math.Sqrt(distanceToClosestPredator);

            return closestPredator;
        }

        public static void TargetClosestAbyssPredator(NPC searcher, bool passiveToPlayers, float preySearchDistance)
        {
            bool playerSearchFilter(Player p)
            {
                return !passiveToPlayers;
            }
            bool npcSearchFilter(NPC n)
            {
                return n.Infernum().IsAbyssPrey && n.WithinRange(searcher.Center, preySearchDistance);
            }

            NPCUtils.TargetSearchResults searchResults = NPCUtils.SearchForTarget(searcher, NPCUtils.TargetSearchFlag.All, playerSearchFilter, npcSearchFilter);
            if (searchResults.FoundTarget)
            {
                NPCUtils.TargetType value = searchResults.NearestTargetType;
                if (searchResults.FoundTank && !searchResults.NearestTankOwner.dead && !passiveToPlayers)
                    value = NPCUtils.TargetType.Player;

                searcher.target = searchResults.NearestTargetIndex;
                searcher.targetRect = searchResults.NearestTargetHitbox;
            }
        }

        public static string GetNPCNameFromID(int id)
        {
            if (id < NPCID.Count)
                return id.ToString();

            return NPCLoader.GetNPC(id).FullName;
        }

        public static int GetNPCIDFromName(string name)
        {
            if (int.TryParse(name, out int id))
                return id;

            string[] splitName = name.Split('/');
            if (ModContent.TryFind(splitName[0], splitName[1], out ModNPC modNpc))
                return modNpc.Type;

            return NPCID.None;
        }
    }
}
