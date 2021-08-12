using CalamityMod.NPCs;
using CalamityMod.NPCs.Ravager;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
            Punch,
            Hover,
            AccelerationPunch
        }

        public override int NPCOverrideType => ModContent.NPCType<RavagerClawLeft>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public override bool PreAI(NPC npc) => DoClawAI(npc, true);

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor) => RavagerClawLeftBehaviorOverride.DrawClaw(npc, spriteBatch, lightColor, true);

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

            bool free = npc.Infernum().ExtraAI[0] == 1f;
            float reelbackSpeed = ravagerBody.velocity.Length() + 24f;
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
            npc.damage = npc.defDamage;
            if (attackState < 2 && free)
                attackState = (int)RavagerClawAttackState.Hover;

            switch ((RavagerClawAttackState)(int)attackState)
            {
                case RavagerClawAttackState.StickToBody:
                    npc.noTileCollide = true;

                    if (npc.WithinRange(stickPosition, reelbackSpeed + 12f))
                    {
                        npc.rotation = leftClaw.ToInt() * MathHelper.Pi;
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
                case RavagerClawAttackState.Hover:
                    npc.damage = 0;
                    npc.noTileCollide = true;

                    Vector2 hoverDestination = target.Center + Vector2.UnitX * leftClaw.ToDirectionInt() * -575f;
                    if (!npc.WithinRange(hoverDestination, 50f))
                        npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 20f, 0.9f);
                    else
                        npc.velocity *= 0.965f;
                    npc.rotation = npc.AngleTo(target.Center);

                    if (punchTimer >= 240f)
                    {
                        punchTimer = 0f;
                        attackState = (int)RavagerClawAttackState.AccelerationPunch;
                        npc.velocity = npc.SafeDirectionTo(target.Center) * 17f;
                        npc.netUpdate = true;
                    }

                    punchTimer++;
                    break;
                case RavagerClawAttackState.AccelerationPunch:
                    if (punchTimer >= 45f)
                    {
                        punchTimer = 0f;
                        attackState = (int)RavagerClawAttackState.Hover;
                        npc.velocity *= 0.5f;
                        npc.netUpdate = true;
                    }
                    npc.velocity *= 1.01f;
                    npc.rotation = npc.velocity.ToRotation();

                    punchTimer++;
                    break;
            }

            return false;
        }

        public static bool DrawClaw(NPC npc, SpriteBatch spriteBatch, Color lightColor, bool leftclaw)
        {
            NPC ravagerBody = Main.npc[CalamityGlobalNPC.scavenger];
            Texture2D chainTexture = ModContent.GetTexture("CalamityMod/NPCs/Ravager/RavagerChain");
            Texture2D npcTexture = Main.npcTexture[npc.type];
            Vector2 drawStart = ravagerBody.Center + new Vector2(-92f * leftclaw.ToDirectionInt(), 46f);
            Vector2 drawPosition = drawStart;
            float chainRotation = npc.AngleFrom(drawStart) - MathHelper.PiOver2;
            while (npc.Infernum().ExtraAI[0] == 0f)
            {
                if (npc.WithinRange(drawPosition, 14f))
                    break;

                drawPosition += (npc.Center - drawStart).SafeNormalize(Vector2.Zero) * 14f; 
                Color color = npc.GetAlpha(Lighting.GetColor((int)drawPosition.X / 16, (int)(drawPosition.Y / 16f)));
                Vector2 screenDrawPosition = drawPosition - Main.screenPosition;
                spriteBatch.Draw(chainTexture, screenDrawPosition, null, color, chainRotation, chainTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            }

            Vector2 clawDrawPosition = npc.Center - Main.screenPosition;
            SpriteEffects direction = npc.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            spriteBatch.Draw(npcTexture, clawDrawPosition, null, npc.GetAlpha(lightColor), npc.rotation, npcTexture.Size() * 0.5f, npc.scale, direction, 0f);
            return false;
        }
    }
}
