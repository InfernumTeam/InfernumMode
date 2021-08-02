using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Leviathan;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using PlaguebringerBoss = CalamityMod.NPCs.PlaguebringerGoliath.PlaguebringerGoliath;

namespace InfernumMode.FuckYouModeAIs.Leviathan
{
	public class PlaguebringerGoliathBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<PlaguebringerBoss>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw | NPCOverrideContext.NPCFindFrame;

        #region Enumerations
        public enum PBGAttackType
        {
            Charge
        }

        public enum PBGFrameType
        {
            Fly,
            Charge
        }
        #endregion Enumerations

        #region AI
        public override bool PreAI(NPC npc)
        {
            npc.damage = npc.defDamage;
            npc.dontTakeDamage = false;

            npc.TargetClosest();

            Player target = Main.player[npc.target];
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float enrageFactor = 1f - lifeRatio;
            if (target.Center.Y < Main.worldSurface * 16f)
                enrageFactor = 1.5f;

            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float frameType = ref npc.localAI[0];

            // Continuously reset things.
            npc.damage = 0;
            frameType = (int)PBGFrameType.Fly;

            switch ((PBGAttackType)(int)attackType)
            {
                case PBGAttackType.Charge:
                    DoBehavior_Charge(npc, target, attackTimer, enrageFactor, ref frameType);
                    break;
            }

            attackTimer++;
            return false;
        }

        #region Specific Behaviors
        public static void DoBehavior_Charge(NPC npc, Player target, float attackTimer, float enrageFactor, ref float frameType)
        {
            int maxChargeCount = (int)Math.Ceiling(3f + enrageFactor * 1.3f);
            int chargeTime = (int)(75f - enrageFactor * 21f);
            bool canDoDiagonalCharges = enrageFactor > 0.3f;
            float chargeSpeed = enrageFactor * 4f + 27.5f;

            ref float chargeCount = ref npc.Infernum().ExtraAI[0];
            ref float hoverOffsetY = ref npc.Infernum().ExtraAI[1];
            ref float chargeTimer = ref npc.Infernum().ExtraAI[2];
            ref float chargeState = ref npc.Infernum().ExtraAI[3];
            ref float hoverTimer = ref npc.Infernum().ExtraAI[4];
            Vector2 hoverOffset = new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 540f, hoverOffsetY);

            // Do initializations.
            if (Main.netMode != NetmodeID.MultiplayerClient && chargeState == 0f)
            {
                hoverOffsetY = canDoDiagonalCharges ? -360f : 0f;
                if (chargeCount % 2 == 1)
                    hoverOffsetY = 0f;
                chargeState = 1f;
                npc.netUpdate = true;
            }

            // Hover until reaching the destination.
            if (chargeState == 1f)
            {
                Vector2 hoverDestination = target.Center + hoverOffset;
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

                if (npc.WithinRange(hoverDestination, 85f))
                {
                    npc.velocity *= 0.96f;

                    // Do the charge.
                    if (npc.WithinRange(hoverDestination, 65f) && hoverTimer > 45f)
                    {
                        hoverTimer = 0f;
                        chargeState = 2f;
                        npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        npc.netUpdate = true;
                    }
                }
                else
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 27f, 0.75f);
                npc.rotation = npc.velocity.X * 0.0125f;
                hoverTimer++;
            }

            // Charge behavior.
            if (chargeState == 2f)
            {
                frameType = (int)PBGFrameType.Charge;

                npc.damage = npc.defDamage;
                npc.rotation = npc.velocity.ToRotation();
                if (npc.spriteDirection == -1)
                    npc.rotation += MathHelper.Pi;

                chargeTimer++;
                
                // Slow down before transitioning back to hovering.
                if (chargeTimer > chargeTime - 15f)
                    npc.velocity *= 0.97f;

                if (chargeTimer >= chargeTime)
                {
                    chargeCount++;
                    hoverOffsetY = 0f;
                    chargeTimer = 0f;
                    chargeState = 0f;
                    npc.netUpdate = true;
                }
            }
        }
        #endregion Specific Behaviors

        #endregion AI

        #region Frames and Drawcode

        public override void FindFrame(NPC npc, int frameHeight)
        {
            bool charging = npc.localAI[0] == (int)PBGFrameType.Charge;
            int width = !charging ? (532 / 2) : (644 / 2);
            int height = !charging ? (768 / 3) : (636 / 3);
            npc.frameCounter += charging ? 1.8f : 1f;

            if (npc.frameCounter > 4.0)
            {
                npc.frame.Y += height;
                npc.frameCounter = 0.0;
            }
            if (npc.frame.Y >= height * 3)
            {
                npc.frame.Y = 0;
                npc.frame.X = npc.frame.X == 0 ? width : 0;
            }
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            bool charging = npc.localAI[0] == (int)PBGFrameType.Charge;
            ref float previousFrameType = ref npc.localAI[1];
            Texture2D texture = Main.npcTexture[npc.type];
            Texture2D glowTexture = ModContent.GetTexture("CalamityMod/NPCs/PlaguebringerGoliath/PlaguebringerGoliathGlow");
            if (charging)
            {
                texture = ModContent.GetTexture("CalamityMod/NPCs/PlaguebringerGoliath/PlaguebringerGoliathChargeTex");
                glowTexture = ModContent.GetTexture("CalamityMod/NPCs/PlaguebringerGoliath/PlaguebringerGoliathChargeTexGlow");
            }

            // Reset frames when frame types change.
            if (previousFrameType != npc.localAI[0])
            {
                npc.frame.X = 0;
                npc.frame.Y = 0;
                previousFrameType = npc.localAI[0];
            }

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            int frameCount = 3;
            int afterimageCount = 10;
            Rectangle frame = new Rectangle(npc.frame.X, npc.frame.Y, texture.Width / 2, texture.Height / frameCount);
            Vector2 origin = frame.Size() / 2f;
            Vector2 drawOffset = Vector2.UnitX * (charging ? 175f : 125f);

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < afterimageCount; i += 2)
                {
                    Color color38 = npc.GetAlpha(Color.Lerp(lightColor, Color.White, 0.5f)) * ((afterimageCount - i) / 15f);
                    Vector2 drawPosition = npc.oldPos[i] + npc.Size * 0.5f - Main.screenPosition;
                    drawPosition -= new Vector2(texture.Width, texture.Height / frameCount) * npc.scale / 2f;
                    drawPosition += origin * npc.scale + drawOffset;
                    spriteBatch.Draw(texture, drawPosition, frame, color38, npc.rotation, origin, npc.scale, spriteEffects, 0f);
                }
            }

            Vector2 baseDrawPosition = npc.Center - Main.screenPosition;
            spriteBatch.Draw(texture, baseDrawPosition, frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, spriteEffects, 0f);
            spriteBatch.Draw(glowTexture, baseDrawPosition, frame, npc.GetAlpha(Color.White), npc.rotation, origin, npc.scale, spriteEffects, 0f);
            return false;
        }
        #endregion Frames and Drawcode
    }
}