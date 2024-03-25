using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Utilities;

namespace InfernumMode
{
    public static partial class Utilities
    {
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
            distanceToClosestPredator = Sqrt(distanceToClosestPredator);

            return closestPredator;
        }

        public static void TargetClosestAbyssPredator(NPC searcher, bool passiveToPlayers, float preySearchDistance, float playerSearchDistance)
        {
            bool playerSearchFilter(Player p)
            {
                return !passiveToPlayers && p.WithinRange(searcher.Center, playerSearchDistance);
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

        public static void SpawnSchoolOfFish(NPC npc, int MinSchoolSize, int MaxSchoolSize)
        {
            // Larger schools are made rarer by this exponent by effectively "squashing" randomness.
            float fishInterpolant = Pow(Main.rand.NextFloat(), 4f);
            int fishCount = (int)Lerp(MinSchoolSize, MaxSchoolSize, fishInterpolant);

            for (int i = 0; i < fishCount; i++)
                NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, npc.type, npc.whoAmI, 1f);

            npc.ai[0] = 1f;
            npc.netUpdate = true;
        }

        public static void TurnAroundBehavior(NPC npc, Vector2 ahead, bool aboutToLeaveWorld)
        {
            float distanceToTileOnLeft = LumUtils.DistanceToTileCollisionHit(npc.Center, npc.velocity.RotatedBy(-PiOver2)) ?? 999f;
            float distanceToTileOnRight = LumUtils.DistanceToTileCollisionHit(npc.Center, npc.velocity.RotatedBy(PiOver2)) ?? 999f;
            float turnDirection = distanceToTileOnLeft > distanceToTileOnRight ? -1f : 1f;
            Vector2 idealVelocity = npc.velocity.RotatedBy(PiOver2 * turnDirection);
            if (aboutToLeaveWorld)
                idealVelocity = ahead.X >= Main.maxTilesX * 16f - 700f ? -Vector2.UnitX * 4f : Vector2.UnitX * 4f;

            npc.velocity = npc.velocity.MoveTowards(idealVelocity, 0.15f);
            npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.15f);
        }
    }
}
