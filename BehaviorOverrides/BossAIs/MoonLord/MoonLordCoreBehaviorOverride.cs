using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
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
            SpawnEffects,
            PhantasmalSphereHandWaves,
            PhantasmalBoltEyeBursts
        }

        public const int ArenaWidth = 200;
        public const int ArenaHeight = 150;
        public const float BaseFlySpeedFactor = 3.5f;
        public static readonly Color OverallTint = new Color(7, 81, 81);
        public override int NPCOverrideType => NPCID.MoonLordCore;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            // Stop rain.
            CalamityMod.CalamityMod.StopRain();

            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float forcefullySwitchAttack = ref npc.Infernum().ExtraAI[0];

            // Player variable.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Reset things.
            npc.dontTakeDamage = false;

            // Life ratio.
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Start the AI and create the arena.
            if (npc.localAI[3] == 0f)
            {
                Player closest = Main.player[Player.FindClosest(npc.Center, 1, 1)];
                if (npc.Infernum().arenaRectangle == null)
                    npc.Infernum().arenaRectangle = default;

                Point closestTileCoords = closest.Center.ToTileCoordinates();
                npc.Infernum().arenaRectangle = new Rectangle((int)closest.position.X - ArenaWidth * 8, (int)closest.position.Y - ArenaHeight * 8 + 20, ArenaWidth * 16, ArenaHeight * 16);
                for (int i = closestTileCoords.X - ArenaWidth / 2; i <= closestTileCoords.X + ArenaWidth / 2; i++)
                {
                    for (int j = closestTileCoords.Y - ArenaHeight / 2; j <= closestTileCoords.Y + ArenaHeight / 2; j++)
                    {
                        // Create arena tiles.
                        if ((Math.Abs(closestTileCoords.X - i) == ArenaWidth / 2 || Math.Abs(closestTileCoords.Y - j) == ArenaHeight / 2) && !Main.tile[i, j].active())
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
                attackState = (int)MoonLordAttackState.SpawnEffects;
                npc.netUpdate = true;
            }

            switch ((MoonLordAttackState)(int)attackState)
            {
                case MoonLordAttackState.SpawnEffects:
                    DoBehavior_SpawnEffects(npc, ref attackTimer);
                    break;
                case MoonLordAttackState.PhantasmalSphereHandWaves:
                case MoonLordAttackState.PhantasmalBoltEyeBursts:
                    DoBehavior_IdleHover(npc, target, ref attackTimer);
                    break;
            }
            attackTimer++;

            // Forcefully switch attacks if the mechanism variable for it is activated.
            // This is intended to be used by the arms/head directly and not inside in-class behavior states.
            if (forcefullySwitchAttack == 1f)
            {
                SelectNextAttack(npc);
                forcefullySwitchAttack = 0f;
            }
            
            // Update other limbs if the core is supposed to sync.
            if (npc.netUpdate)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    bool isBodyPart = n.type == NPCID.MoonLordHand || n.type == NPCID.MoonLordHead;
                    if (n.active && n.ai[3] == npc.whoAmI && isBodyPart)
                    {
                        n.netSpam = npc.netSpam;
                        n.netUpdate = true;
                    }
                }
            }

            return false;
        }

        public static void DoBehavior_SpawnEffects(NPC npc, ref float attackTimer)
        {
            // Don't do damage during spawn effects.
            npc.dontTakeDamage = true;

            // Roar after a bit of time has passed.
            if (attackTimer == 30f)
                Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, 92, 1f, 0f);

            // Create light.
            if (attackTimer < 60f)
                MoonlordDeathDrama.RequestLight(attackTimer / 30f, npc.Center);

            // Create arms/head and go to the next attack state.
            if (attackTimer >= 60f)
            {
                SelectNextAttack(npc);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int[] bodyPartIndices = new int[3];
                    for (int i = 0; i < 2; i++)
                    {
                        int handIndex = NPC.NewNPC((int)npc.Center.X + i * 800 - 400, (int)npc.Center.Y - 100, NPCID.MoonLordHand, npc.whoAmI);
                        Main.npc[handIndex].ai[2] = i;
                        Main.npc[handIndex].netUpdate = true;
                        bodyPartIndices[i] = handIndex;
                    }

                    int headIndex = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y - 400, NPCID.MoonLordHead, npc.whoAmI);
                    Main.npc[headIndex].netUpdate = true;
                    bodyPartIndices[2] = headIndex;

                    // Mark the owner of the body parts.
                    for (int i = 0; i < 3; i++)
                        Main.npc[bodyPartIndices[i]].ai[3] = npc.whoAmI;

                    for (int i = 0; i < 3; i++)
                        npc.localAI[i] = bodyPartIndices[i];

                    // Reset hand AIs.
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (Main.npc[i].type == NPCID.MoonLordHand && Main.npc[i].active)
                            Main.npc[i].ai[0] = 0f;
                    }
                }
            }
            npc.netSpam = 0;
            npc.netUpdate = true;
        }

        public static void DoBehavior_IdleHover(NPC npc, Player target, ref float attackTimer)
        {
            float verticalOffset = MathHelper.Lerp(120f, 175f, (float)Math.Cos(attackTimer / 32f) * 0.5f + 0.5f);
            Vector2 hoverDestination = target.Center - Vector2.UnitY * verticalOffset;
            Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * BaseFlySpeedFactor;
            npc.SimpleFlyMovement(idealVelocity, BaseFlySpeedFactor / 20f);
            npc.velocity = npc.velocity.MoveTowards(idealVelocity, BaseFlySpeedFactor / 60f);
        }

        public static void SelectNextAttack(NPC npc)
        {
            npc.ai[1] = 0f;
            switch ((MoonLordAttackState)(int)npc.ai[0])
            {
                case MoonLordAttackState.SpawnEffects:
                    npc.ai[0] = (int)MoonLordAttackState.PhantasmalSphereHandWaves;
                    break;
                case MoonLordAttackState.PhantasmalBoltEyeBursts:
                    npc.ai[0] = (int)MoonLordAttackState.PhantasmalSphereHandWaves;
                    break;
                case MoonLordAttackState.PhantasmalSphereHandWaves:
                    npc.ai[0] = (int)MoonLordAttackState.PhantasmalBoltEyeBursts;
                    break;
            }

            npc.netUpdate = true;
        }
    }
}
