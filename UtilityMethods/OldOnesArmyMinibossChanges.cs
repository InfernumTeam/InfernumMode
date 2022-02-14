using Microsoft.Xna.Framework;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.Utilities;

namespace InfernumMode
{
    public static class OldOnesArmyMinibossChanges
    {
        private static readonly MethodInfo oldOnesArmyStatusMethod = typeof(DD2Event).GetMethod("GetInvasionStatus", Utilities.UniversalBindingFlags);

        public static void GetOldOnesArmyStatus(out int currentWave, out int requiredKillCount, out int currentKillCount, bool currentlyInCheckProgress = false)
        {
            currentWave = requiredKillCount = currentKillCount = 0;
            object[] parameters = new object[] { currentWave, requiredKillCount, currentKillCount, currentlyInCheckProgress };
            oldOnesArmyStatusMethod.Invoke(null, parameters);
            currentWave = (int)parameters[0];
            requiredKillCount = (int)parameters[1];
            currentKillCount = (int)parameters[2];
        }

        public static bool GetMinibossToSummon(out int minibossID)
        {
            minibossID = -1;
            GetOldOnesArmyStatus(out int currentWave, out int requiredKillCount, out int currentKillCount);

            int currentTier = 1;
            if (DD2Event.ReadyForTier2)
                currentTier = 2;
            if (DD2Event.ReadyForTier3)
                currentTier = 3;

            float waveCompletion = MathHelper.Clamp(currentKillCount / (float)requiredKillCount, 0f, 1f);
            bool atEndOfWave = waveCompletion >= 0.9f;
            if (currentWave == 5 && currentTier == 1 && atEndOfWave)
                minibossID = NPCID.DD2DarkMageT1;
            if (currentWave == 7 && currentTier == 2 && atEndOfWave)
                minibossID = NPCID.DD2OgreT2;
            if (currentWave == 5 && currentTier == 3 && atEndOfWave)
                minibossID = NPCID.DD2DarkMageT3;
            if (currentWave == 6 && currentTier == 3 && atEndOfWave)
                minibossID = NPCID.DD2OgreT3;
            if (currentWave == 7 && currentTier == 3)
                minibossID = NPCID.DD2Betsy;

            return minibossID != -1;
        }

        public static void ClearPickoffOOAEnemies()
        {
            int[] pickOffNPCs = new int[]
            {
                NPCID.DD2GoblinT1,
                NPCID.DD2GoblinT2,
                NPCID.DD2GoblinT3,
                NPCID.DD2GoblinBomberT1,
                NPCID.DD2GoblinBomberT2,
                NPCID.DD2GoblinBomberT3,
                NPCID.DD2WyvernT1,
                NPCID.DD2WyvernT2,
                NPCID.DD2WyvernT3,
                NPCID.DD2JavelinstT1,
                NPCID.DD2JavelinstT2,
                NPCID.DD2JavelinstT3,
                NPCID.DD2WitherBeastT2,
                NPCID.DD2WitherBeastT3,
                NPCID.DD2KoboldWalkerT2,
                NPCID.DD2KoboldWalkerT3,
                NPCID.DD2KoboldFlyerT2,
                NPCID.DD2KoboldFlyerT3,
                NPCID.DD2LightningBugT3,
                NPCID.DD2DrakinT2,
                NPCID.DD2DrakinT3,
            };
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && pickOffNPCs.Contains(Main.npc[i].type))
                {
                    if (Main.npc[i].Opacity > 0.8f)
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            Dust magic = Dust.NewDustDirect(Main.npc[i].position, Main.npc[i].width, Main.npc[i].height, 27);
                            magic.velocity.Y -= 3f;
                            magic.velocity *= Main.rand.NextFloat(1f, 1.25f);
                            magic.alpha = 128;
                            magic.noGravity = true;
                        }
                    }
                    Main.npc[i].active = false;
                }
            }
        }

        public static void TargetClosestMiniboss(NPC searcher, bool faceTarget = true, bool prioritizeCrystal = false)
        {
            if (!DD2Event.Ongoing)
                prioritizeCrystal = false;

            NPCUtils.TargetSearchFlag targetFlags = NPCUtils.TargetSearchFlag.All;

            // If a player exists and is nearby, only attack players.
            float playerSearchDistance = prioritizeCrystal ? 160f : 1600f;
            if (Main.player[Player.FindClosest(searcher.Center, 1, 1)].WithinRange(searcher.Center, playerSearchDistance))
                targetFlags = NPCUtils.TargetSearchFlag.Players;

            var playerFilter = NPCUtils.SearchFilters.OnlyPlayersInCertainDistance(searcher.Center, playerSearchDistance);
            var npcFilter = new NPCUtils.SearchFilter<NPC>(NPCUtils.SearchFilters.OnlyCrystal);

            NPCUtils.TargetSearchResults searchResults = NPCUtils.SearchForTarget(searcher, targetFlags, playerFilter, npcFilter);
            if (searchResults.FoundTarget)
            {
                searcher.target = searchResults.NearestTargetIndex;
                searcher.targetRect = searchResults.NearestTargetHitbox;
                if (searcher.ShouldFaceTarget(ref searchResults, null) && faceTarget)
                {
                    searcher.FaceTarget();
                }
            }
        }
    }
}
