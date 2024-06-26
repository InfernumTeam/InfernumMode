﻿using InfernumMode.Common.UtilityMethods;
using InfernumMode.Core.OverridingSystem;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Events;
using Terraria.ID;

namespace InfernumMode.Content.BehaviorOverrides.MiscAIs
{
    public class EtherianPortalBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.DD2LanePortal;

        public override bool PreAI(NPC npc)
        {
            // Idly emit light if the portal has completely faded in.
            if (npc.alpha == 0)
                Lighting.AddLight(npc.Center, 0.5f, 0.1f, 0.3f);

            ref float enemySpawnTimer = ref npc.ai[0];
            ref float fadeOutTimer = ref npc.ai[2];
            ref float fadeInTimer = ref npc.localAI[0];
            if (npc.ai[1] == 0f)
            {
                // Play the portal opening sound at the moment of the portal's creation.
                if (fadeInTimer == 0f)
                    SoundEngine.PlaySound(SoundID.DD2_EtherianPortalOpen, npc.Center);

                if (!DD2Event.EnemySpawningIsOnHold)
                    enemySpawnTimer++;

                bool minibossShouldSpawn = OldOnesArmyMinibossChanges.GetMinibossToSummon(out int minibossID);
                if ((enemySpawnTimer >= DD2Event.LaneSpawnRate || minibossShouldSpawn) && DD2Event.TimeLeftBetweenWaves <= 0f)
                {
                    if (enemySpawnTimer >= DD2Event.LaneSpawnRate * 3 || minibossShouldSpawn)
                        enemySpawnTimer = 0f;

                    if (Main.netMode != NetmodeID.MultiplayerClient && (int)enemySpawnTimer % DD2Event.LaneSpawnRate == 0)
                    {
                        if (!minibossShouldSpawn)
                            DD2Event.SpawnMonsterFromGate(npc.Bottom);
                        else if (!NPC.AnyNPCs(minibossID) && minibossID != NPCID.DD2Betsy)
                        {
                            int miniboss = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Bottom.X, (int)npc.Bottom.Y, minibossID);
                            if (Main.npc.IndexInRange(miniboss))
                                Main.npc[miniboss].Infernum().ExtraAI[5] = 1f;
                        }

                        // If enemy spawning is on hold, add 1 to the spawn timer to ensure that the above
                        // modulo doesn't get activated every single frame due to no other change above.
                        if (DD2Event.EnemySpawningIsOnHold)
                            enemySpawnTimer++;
                    }
                    npc.netUpdate = true;
                }

                fadeInTimer++;

                // Begin to fade out of existence if the crystal is gone and the portal is done doing fade-in effects.
                if (Main.netMode != NetmodeID.MultiplayerClient && !NPC.AnyNPCs(NPCID.DD2EterniaCrystal) && fadeInTimer >= 180f)
                {
                    npc.ai[1] = 1f;
                    enemySpawnTimer = 0f;
                    npc.dontTakeDamage = true;
                    npc.netUpdate = true;
                }
            }

            // Fade-out effects.
            else if (npc.ai[1] == 1f)
            {
                fadeOutTimer++;
                npc.scale = Lerp(1f, 0.05f, Utils.GetLerpValue(500f, 600f, fadeOutTimer, true));

                // Kill the portal after enough time has passed.
                if (fadeOutTimer >= 550f)
                {
                    npc.dontTakeDamage = false;
                    npc.life = 0;
                    npc.checkDead();
                    npc.netUpdate = true;
                }
            }
            return false;
        }
    }
}
