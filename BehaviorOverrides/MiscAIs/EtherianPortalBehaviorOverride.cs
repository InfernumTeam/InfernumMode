using CalamityMod.NPCs;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Events;
using Terraria.ID;

namespace InfernumMode.BehaviorOverrides.MinibossAIs.MiscAIs
{
    public class EtherianPortalBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.DD2LanePortal;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            // Idly emit light if the portal has completely faded in.
            if (npc.alpha == 0)
                Lighting.AddLight(npc.Center, 0.5f, 0.1f, 0.3f);

            ref float enemySpawnTimer = ref npc.ai[0];
            ref float fadeOutTimer = ref npc.ai[2];
            ref float fadeInTimer = ref npc.localAI[0];
            ref float idlePlaySoundId = ref npc.localAI[3];
            if (npc.ai[1] == 0f)
            {
                // Play the portal opening sound at the moment of the portal's creation.
                if (fadeInTimer == 0f)
                {
                    Main.PlayTrackedSound(SoundID.DD2_EtherianPortalOpen, npc.Center);
                    idlePlaySoundId = SlotId.Invalid.ToFloat();
                }

                if (fadeInTimer > 150f && Main.GetActiveSound(SlotId.FromFloat(idlePlaySoundId)) == null)
                    idlePlaySoundId = Main.PlayTrackedSound(SoundID.DD2_EtherianPortalIdleLoop, npc.Center).ToFloat();

                if (!DD2Event.EnemySpawningIsOnHold)
                    enemySpawnTimer++;

                bool minibossShouldSpawn = OldOnesArmyMinibossChanges.GetMinibossToSummon(out int minibossID);
                if ((enemySpawnTimer >= CalamityGlobalAI.DD2EventEnemySpawnRate || minibossShouldSpawn) && DD2Event.TimeLeftBetweenWaves <= 0f)
                {
                    if (enemySpawnTimer >= CalamityGlobalAI.DD2EventEnemySpawnRate * 3 || minibossShouldSpawn)
                        enemySpawnTimer = 0f;

                    if (Main.netMode != NetmodeID.MultiplayerClient && (int)enemySpawnTimer % CalamityGlobalAI.DD2EventEnemySpawnRate == 0)
                    {
                        if (!minibossShouldSpawn)
                            DD2Event.SpawnMonsterFromGate(npc.Bottom);
                        else if (!NPC.AnyNPCs(minibossID) && minibossID != NPCID.DD2Betsy)
                        {
                            int miniboss = NPC.NewNPC((int)npc.Bottom.X, (int)npc.Bottom.Y, minibossID);
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
                npc.scale = MathHelper.Lerp(1f, 0.05f, Utils.InverseLerp(500f, 600f, fadeOutTimer, true));

                // Reset the idle play sound if it didn't get activated before for some reason.
                if (Main.GetActiveSound(SlotId.FromFloat(idlePlaySoundId)) == null)
                    idlePlaySoundId = Main.PlayTrackedSound(SoundID.DD2_EtherianPortalIdleLoop, npc.Center).ToFloat();

                ActiveSound activeSound = Main.GetActiveSound(SlotId.FromFloat(idlePlaySoundId));
                if (activeSound != null)
                    activeSound.Volume = npc.scale;

                // Kill the portal after enough time has passed.
                if (fadeOutTimer >= 550f)
                {
                    npc.dontTakeDamage = false;
                    npc.life = 0;
                    npc.checkDead();
                    npc.netUpdate = true;
                    if (activeSound != null)
                    {
                        activeSound.Stop();
                        return false;
                    }
                }
            }
            return false;
        }
    }
}
