using CalamityMod.NPCs;
using CalamityMod.NPCs.Ravager;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Ravager
{
    public class RavagerClawLeftBehaviorOverride : NPCBehaviorOverride
    {
        public enum RavagerClawAttackState
        {
            StickToBody,
            Punch
        }

        public override int NPCOverrideType => ModContent.NPCType<RavagerClawLeft>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc) => DoClawAI(npc, true);

        public static bool DoClawAI(NPC npc, bool leftClaw)
        {
            // Do targeting.
            npc.TargetClosest();
            Player target = Main.player[npc.target];

            // Die if the main body does not exist anymore.
            if (CalamityGlobalNPC.scavenger < 0 || !Main.npc[CalamityGlobalNPC.scavenger].active)
            {
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            NPC ravagerBody = Main.npc[CalamityGlobalNPC.scavenger];

            float reelbackSpeed = 24f;
            float punchSpeed = 21.5f;
            Vector2 stickPosition = ravagerBody.Center + new Vector2(-120f * leftClaw.ToDirectionInt(), 50f);

            ref float attackState = ref npc.ai[0];
            ref float punchTimer = ref npc.ai[1];

            // Prevent typical despawning.
            if (npc.timeLeft < 1800)
                npc.timeLeft = 1800;

            // Fade in.
            if (npc.alpha > 0)
            {
                npc.alpha = Utils.Clamp(npc.alpha - 10, 0, 255);
                punchTimer = -90f;
            }

            npc.spriteDirection = leftClaw.ToDirectionInt();

            switch ((RavagerClawAttackState)(int)attackState)
            {
                case RavagerClawAttackState.StickToBody:
                    npc.noTileCollide = true;

                    if (npc.WithinRange(stickPosition, reelbackSpeed + 12f))
                    {
                        npc.rotation = 0f;
                        npc.velocity = Vector2.Zero;
                        npc.Center = stickPosition;

                        punchTimer += 8f;

                        // If the target is to the correct side and this claw is ready, prepare a punch.
                        if (punchTimer >= 60f)
                        {
                            npc.TargetClosest(true);

                            bool canPunch;
                            if (leftClaw)
                                canPunch = npc.Center.X + 100f > target.Center.X;
                            else
                                canPunch = npc.Center.X - 100f < target.Center.X;

                            if (canPunch)
                            {
                                punchTimer = 0f;
                                npc.ai[0] = 1f;
                                npc.noTileCollide = true;
                                npc.collideX = false;
                                npc.collideY = false;
                                npc.velocity = npc.SafeDirectionTo(target.Center) * punchSpeed;
                                npc.rotation = npc.velocity.ToRotation();
                                npc.netUpdate = true;
                                return false;
                            }
                            punchTimer = 0f;
                            return false;
                        }
                    }
                    else
                    {
                        npc.velocity = npc.SafeDirectionTo(stickPosition) * reelbackSpeed;
                        npc.rotation = (npc.Center - stickPosition).ToRotation();
                    }
                    break;
                case RavagerClawAttackState.Punch:
                    // Check if tile collision is still necesssary.
                    if (Math.Abs(npc.velocity.X) > Math.Abs(npc.velocity.Y))
                    {
                        if (npc.velocity.X > 0f && npc.Center.X > target.Center.X)
                            npc.noTileCollide = false;

                        if (npc.velocity.X < 0f && npc.Center.X < target.Center.X)
                            npc.noTileCollide = false;
                    }
                    else
                    {
                        if (npc.velocity.Y > 0f && npc.Center.Y > target.Center.Y)
                            npc.noTileCollide = false;

                        if (npc.velocity.Y < 0f && npc.Center.Y < target.Center.Y)
                            npc.noTileCollide = false;
                    }
                    if (!npc.WithinRange(stickPosition, 700f) || npc.collideX || npc.collideY || npc.justHit)
                    {
                        npc.noTileCollide = true;
                        attackState = (int)RavagerClawAttackState.StickToBody;
                    }
                    break;
            }

            return false;
        }
    }
}
