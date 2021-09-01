using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.NPCs.SunkenSea;
using CalamityMod.Projectiles.Enemy;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using GiantClamNPC = CalamityMod.NPCs.SunkenSea.GiantClam;

namespace InfernumMode.FuckYouModeAIs.GiantClam
{
    public class GiantClamBehaviorOverride : NPCBehaviorOverride
    {
        public enum GiantClamAttackState
        {
            AttackDelayState = -1,
            HideInShellAndSummonClams = 0,
            TeleportStomp = 1,
            PearlBurst = 2,
            PearlRain = 3
        }

        public const int HitsRequiredToAnger = 5;

        public override int NPCOverrideType => ModContent.NPCType<GiantClamNPC>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame;

        public override bool PreAI(NPC npc)
        {
            ref float hitCount = ref npc.Infernum().ExtraAI[0];
            ref float hasChangedStatsAfterAngeredFlag = ref npc.Infernum().ExtraAI[1];
            ref float attackState = ref npc.Infernum().ExtraAI[2];
            ref float hidingInShellFlag = ref npc.Infernum().ExtraAI[3];
            ref float switchToAttackFrameFlag = ref npc.Infernum().ExtraAI[4];
            ref float attackSelectionDelayTimer = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[3];

            npc.TargetClosest(true);
            Player player = Main.player[npc.target];

            // Increment the hit counter when hit by something.
            if (npc.justHit && hitCount < HitsRequiredToAnger)
                hitCount++;

            npc.chaseable = hitCount > 0f;

            // Stop here if not hit enough to entail attacking.
            if (hitCount < HitsRequiredToAnger)
                return false;

            // Inflict the Clamity debuff. This angers all spawned clam minions automatically.
            if (Main.netMode != NetmodeID.Server)
            {
                if (!player.dead && player.active)
                    player.AddBuff(ModContent.BuffType<Clamity>(), 2);
            }

            if (hidingInShellFlag == 0f)
                Lighting.AddLight(npc.Center, 0f, npc.Opacity * 2.5f, npc.Opacity * 2.5f);

            // If damage and defense stats need to be changed due to a transition to the angry stat
            if (hasChangedStatsAfterAngeredFlag == 0f)
            {
                npc.defense = 10;
                npc.damage = 100;
                if (Main.hardMode)
                {
                    npc.defense = 35;
                    npc.damage = 200;
                }

                // Reset the default damage and defense values as well.
                npc.defDamage = npc.damage;
                npc.defDefense = npc.defense;

                npc.netUpdate = true;
                hasChangedStatsAfterAngeredFlag = 1f;
            }

            if (attackSelectionDelayTimer < 120f)
            {
                attackSelectionDelayTimer++;
                hidingInShellFlag = 1f;
                return false;
            }

            switch ((GiantClamAttackState)(int)attackState)
            {
                // Just select a new attack immediately. Bear in mind that this only happens once the attack selection delay is complete.
                case GiantClamAttackState.AttackDelayState:
                    WeightedRandom<GiantClamAttackState> attackStateSelector = new WeightedRandom<GiantClamAttackState>(Main.rand);

                    // Choose different attack sets depending on if the world is in hardmode.
                    if (Main.hardMode)
                    {
                        attackStateSelector.Add(GiantClamAttackState.HideInShellAndSummonClams, 0.75f);
                        attackStateSelector.Add(GiantClamAttackState.TeleportStomp, 1f);
                        attackStateSelector.Add(GiantClamAttackState.PearlBurst, 1f);
                        attackStateSelector.Add(GiantClamAttackState.PearlRain, 1f);
                    }
                    else
                    {
                        attackStateSelector.Add(GiantClamAttackState.HideInShellAndSummonClams, 0.5f);
                        attackStateSelector.Add(GiantClamAttackState.TeleportStomp, 1f);
                    }
                    attackState = (int)attackStateSelector.Get();
                    break;

                case GiantClamAttackState.HideInShellAndSummonClams:
                    DoAttack_HideInShellAndSummonClams(npc, ref attackTimer, ref hidingInShellFlag);
                    break;
                case GiantClamAttackState.TeleportStomp:
                    DoAttack_TeleportStomp(npc, player, ref switchToAttackFrameFlag, ref hidingInShellFlag);
                    break;
                case GiantClamAttackState.PearlBurst:
                    DoAttack_PearlBurst(npc, player, ref attackTimer, ref hidingInShellFlag);
                    break;
                case GiantClamAttackState.PearlRain:
                    DoAttack_PearlRain(npc, player, ref attackTimer, ref hidingInShellFlag);
                    break;
            }

            return false;
        }

        public static void PrepareForNextAttack(NPC npc)
        {
            // Reset the attack selection delay timer.
            npc.ai[0] = 0f;

            // The attack timer.
            npc.ai[3] = 0f;

            // And all the optional attack-specific values.
            for (int i = 5; i < 10; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            // And set the attack state to the new one.
            npc.Infernum().ExtraAI[2] = (int)GiantClamAttackState.AttackDelayState;
            npc.netUpdate = true;
        }

        public static void DoAttack_HideInShellAndSummonClams(NPC npc, ref float attackTimer, ref float hidingInShellFlag)
        {
            attackTimer++;
            hidingInShellFlag = 1f;
            npc.defense = 9999;

            // Spawn a bunch of clams and prepare go to the next attack after enough time has passed.
            if (attackTimer >= 90f)
            {
                npc.ai[0] = 0f;
                attackTimer = 0f;
                hidingInShellFlag = 0f;

                PrepareForNextAttack(npc);

                npc.defense = npc.defDefense;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.NewNPC((int)(npc.Center.X + 5), (int)npc.Center.Y, ModContent.NPCType<Clam>(), 0, 0f, 0f, 0f, 0f, 255);
                    NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<Clam>(), 0, 0f, 0f, 0f, 0f, 255);
                    NPC.NewNPC((int)(npc.Center.X - 5), (int)npc.Center.Y, ModContent.NPCType<Clam>(), 0, 0f, 0f, 0f, 0f, 255);
                }
            }
        }

        public static void DoAttack_TeleportStomp(NPC npc, Player target, ref float switchToAttackFrameFlag, ref float hidingInShellFlag)
        {
            float fallAcceleration = 0.8f;
            ref float attackSubstate = ref npc.Infernum().ExtraAI[5];

            // Select a new target immediately and go to the next substate.
            if (attackSubstate == 0f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.TargetClosest(true);
                    attackSubstate = 1f;
                    npc.netUpdate = true;
                }
            }

            // Fade out. This causes the npc to become invincible, untargetable, and do 0 damage.
            // The fadeout is faster in hardmode than not.
            else if (attackSubstate == 1f)
            {
                npc.damage = 0;
                npc.chaseable = false;
                npc.dontTakeDamage = true;
                npc.alpha += Main.hardMode ? 8 : 5;

                // Disable gravity and tile collision. This only happens when the npc is in a stuck spot and poses no issues.
                // It will be used in future substates.
                npc.noGravity = true;
                npc.noTileCollide = true;

                // Open the shell.
                hidingInShellFlag = 0f;

                // Once completely faded out, teleport above the target and go to the next attack substate.
                if (npc.alpha >= 255)
                {
                    npc.alpha = 255;
                    npc.position.X = target.position.X - 15f;
                    npc.position.Y = target.position.Y + target.gfxOffY - 300f;
                    attackSubstate = 2f;
                    npc.netUpdate = true;
                }
            }

            // Hover in place and reappear.
            // The reappearance is fastter in hardmode than not.
            else if (attackSubstate == 2f)
            {
                // Emit cyan dust idly.
                if (Main.rand.NextBool(2))
                {
                    Dust blueElectricity = Dust.NewDustDirect(npc.position, npc.width, npc.height, 226, 0f, 0f, 200, default, 1.5f);
                    blueElectricity.noGravity = true;
                    blueElectricity.velocity *= 0.75f;
                    blueElectricity.fadeIn = 1.3f;
                    blueElectricity.position = npc.Center - Main.rand.NextVector2CircularEdge(75f, 75f) * Main.rand.NextFloat(0.8f, 1.2f);
                    blueElectricity.velocity = (npc.Center - blueElectricity.position) * 0.085f;
                }

                npc.alpha -= Main.hardMode ? 7 : 4;
                if (npc.alpha <= 0)
                {
                    // Make damage higher than usual before falling, to make it more impactful than typical damage.
                    npc.damage = Main.hardMode ? 250 : 135;

                    npc.chaseable = true;
                    npc.dontTakeDamage = false;
                    npc.alpha = 0;
                    attackSubstate = 3f;
                    npc.netUpdate = true;
                }
            }

            // Fall down quickly.
            else if (attackSubstate == 3f)
            {
                npc.velocity.Y += fallAcceleration;
                switchToAttackFrameFlag = 1f;

                // When sufficiently below the target, 
                if (npc.Center.Y > target.position.Y + target.gfxOffY - 15f)
                {
                    npc.noTileCollide = false;
                    attackSubstate = 4f;
                    npc.netUpdate = true;
                }
            }
            else if (attackSubstate == 4f)
            {
                hidingInShellFlag = 1f;

                // If y movement is 0 (usually indicative of hitting a block), go to the next attack.
                if (npc.velocity.Y == 0f)
                {
                    // Reset damage and apply gravity/tile collision again.
                    npc.damage = npc.defDamage;
                    npc.netUpdate = true;
                    npc.noGravity = false;

                    // And create visual/acoustic effects to support the fact that a collision happened.
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/ClamImpact"), (int)npc.position.X, (int)npc.position.Y);
                    for (int stompDustArea = (int)npc.position.X - 30; stompDustArea < (int)npc.position.X + npc.width + 60; stompDustArea += 30)
                    {
                        for (int stompDustAmount = 0; stompDustAmount < 5; stompDustAmount++)
                        {
                            Dust stompDust = Dust.NewDustDirect(new Vector2(npc.position.X - 30f, npc.position.Y + npc.height), npc.width + 30, 4, 33, 0f, 0f, 100, default, 1.5f);
                            stompDust.velocity *= 0.2f;
                        }

                        Gore stompSmokeGore = Gore.NewGoreDirect(new Vector2(stompDustArea - 30f, npc.position.Y + npc.height - 12f), default, Main.rand.Next(61, 64), 1f);
                        stompSmokeGore.velocity *= 0.4f;
                    }

                    PrepareForNextAttack(npc);
                }

                // Continue falling.
                npc.velocity.Y += fallAcceleration;
            }
        }

        public static void DoAttack_PearlBurst(NPC npc, Player target, ref float attackTimer, ref float hidingInShellFlag)
        {
            attackTimer++;

            // Exit the shell after a bit of time.
            hidingInShellFlag = attackTimer > 45f ? 0f : 1f;
            npc.defense = hidingInShellFlag == 0f ? npc.defDefense : 9999;

            // Sit in place for a bit before releasing a spread of pearls outward and immediately going to the next attack.
            if (attackTimer >= 90f)
            {
                // Play a fire sound.
                Main.PlaySound(SoundID.Item67, npc.position);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int projectileType = ModContent.ProjectileType<PearlBurst>();
                    float shootSpeed = 5f;
                    float startAngle = Main.rand.NextFloat(MathHelper.TwoPi);

                    // Fire one pearl directly at the target.
                    Utilities.NewProjectileBetter(npc.Center, npc.SafeDirectionTo(target.Center) * shootSpeed, projectileType, 112, 0f, Main.myPlayer, 0f, 0f);

                    for (int i = 0; i < 8; i++)
                    {
                        // The shot spacing from this is determined in such a way that it's pseudo-random in nature.
                        Vector2 pearlShootVelocity = (startAngle + (i + i * i) / 2f + 32f * i).ToRotationVector2() * shootSpeed * 0.6f;
                        Utilities.NewProjectileBetter(npc.Center, pearlShootVelocity, projectileType, 112, 0f, Main.myPlayer, 0f, 0f);
                    }
                }

                PrepareForNextAttack(npc);
            }
        }

        public static void DoAttack_PearlRain(NPC npc, Player target, ref float attackTimer, ref float hidingInShellFlag)
        {
            attackTimer++;

            // Exit the shell after a bit of time.
            hidingInShellFlag = attackTimer > 45f ? 0f : 1f;
            npc.defense = hidingInShellFlag == 0f ? npc.defDefense : 9999;

            // Sit in place for a bit before releasing a spread of pearls outward and immediately going to the next attack.
            if (attackTimer >= 90f)
            {
                // Play a fire sound.
                Main.PlaySound(SoundID.Item67, npc.position);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Main.PlaySound(SoundID.Item68, npc.position);
                    for (float horizontalSpawnOffset = -750f; horizontalSpawnOffset < 750f; horizontalSpawnOffset += 150f)
                    {
                        Vector2 spawnPosition = target.Center + new Vector2(horizontalSpawnOffset, -750f);
                        Vector2 pearlShootVelocity = Vector2.UnitY * 8f;
                        Utilities.NewProjectileBetter(spawnPosition, pearlShootVelocity, ModContent.ProjectileType<PearlRain>(), 112, 0f, Main.myPlayer, 0f, 0f);
                    }
                }

                PrepareForNextAttack(npc);
            }
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            ref float hitCount = ref npc.Infernum().ExtraAI[0];
            ref float hidingInShellFlag = ref npc.Infernum().ExtraAI[3];
            ref float switchToAttackFrameFlag = ref npc.Infernum().ExtraAI[4];

            // Cyle through frames as normal.
            npc.frameCounter++;
            if (npc.frameCounter > (switchToAttackFrameFlag == 1f ? 2f : 5f))
            {
                npc.frameCounter = 0D;
                npc.frame.Y += frameHeight;
            }

            // If not angered or hiding in the shell use the 12th frame at all times.
            if (hitCount < HitsRequiredToAnger || hidingInShellFlag == 1f)
                npc.frame.Y = frameHeight * 11;

            // Otherwise, if attack frames are needed, cycle through frames 4-11 specifically.
            else if (switchToAttackFrameFlag == 1f)
            {
                if (npc.frame.Y < frameHeight * 3)
                    npc.frame.Y = frameHeight * 3;

                if (npc.frame.Y > frameHeight * 10)
                {
                    hidingInShellFlag = 1f;
                    switchToAttackFrameFlag = 0f;
                }
            }

            // Otherwise, simply cycle through frames 1-3.
            else if (npc.frame.Y > frameHeight * 3)
                npc.frame.Y = 0;
        }
    }
}
