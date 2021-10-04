using CalamityMod;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using PlaguebringerBoss = CalamityMod.NPCs.PlaguebringerGoliath.PlaguebringerGoliath;

namespace InfernumMode.BehaviorOverrides.BossAIs.PlaguebringerGoliath
{
	public class PlaguebringerGoliathBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<PlaguebringerBoss>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw | NPCOverrideContext.NPCFindFrame;

        #region Enumerations
        // Make 8 attacks.
        public enum PBGAttackType
        {
            Charge,
            MissileLaunch,
            PlagueVomit,
            CarpetBombing,
            ExplodingPlagueChargers,
            DroneSummoning,
            PlagueSwarm,
            BombConstructors,
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
                    DoBehavior_Charge(npc, target, enrageFactor, ref frameType);
                    break;
                case PBGAttackType.MissileLaunch:
                    DoBehavior_MissileLaunch(npc, target, ref attackTimer, enrageFactor, ref frameType);
                    break;
                case PBGAttackType.PlagueVomit:
                    DoBehavior_PlagueVomit(npc, target, ref attackTimer, enrageFactor, ref frameType);
                    break;
                case PBGAttackType.CarpetBombing:
                    DoBehavior_CarpetBombing(npc, target, enrageFactor, ref frameType);
                    break;
            }

            attackTimer++;
            return false;
        }

        #region Specific Behaviors
        public static void DoBehavior_Charge(NPC npc, Player target, float enrageFactor, ref float frameType)
        {
            int maxChargeCount = (int)Math.Ceiling(5f + enrageFactor * 1.3f);
            int chargeTime = (int)(48f - enrageFactor * 15f);
            bool canDoDiagonalCharges = enrageFactor > 0.3f;
            float chargeSpeed = enrageFactor * 4f + 30f;

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

                if (npc.WithinRange(hoverDestination, 255f))
                {
                    npc.velocity *= 0.935f;

                    // Do the charge.
                    if (npc.WithinRange(hoverDestination, 175f) && hoverTimer > 18f)
                    {
                        hoverTimer = 0f;
                        chargeState = 2f;
                        npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        npc.netUpdate = true;

                        Main.PlaySound(SoundID.Roar, target.Center, 0);
                    }
                }
                else
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 19f, 3f);
                npc.Center = npc.Center.MoveTowards(hoverDestination, 12f);
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

                    if (chargeCount > maxChargeCount)
                        GotoNextAttackState(npc);
                }
            }
        }

        public static void DoBehavior_MissileLaunch(NPC npc, Player target, ref float attackTimer, float enrageFactor, ref float frameType)
		{
            int attackCycleCount = 2;
            int missileShootRate = (int)(16f - enrageFactor * 6f);
            float missileShootSpeed = enrageFactor * 5f + 12f;
            ref float attackState = ref npc.Infernum().ExtraAI[0];
            ref float bombingCount = ref npc.Infernum().ExtraAI[1];
            ref float missileShootTimer = ref npc.Infernum().ExtraAI[2];
            ref float attackCycleCounter = ref npc.Infernum().ExtraAI[3];

            frameType = (int)PBGFrameType.Fly;
            switch ((int)attackState)
			{
                // Attempt to hover near the target.
                case 0:
                    Vector2 hoverOffset = new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 420f, -360f);
                    Vector2 hoverDestination = target.Center + hoverOffset;
                    npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 21f, 1f);
                    npc.Center = npc.Center.MoveTowards(hoverDestination, 4f);

                    // Make the attack go by way quicker once in position.
                    if (npc.WithinRange(hoverDestination, 35f))
                        attackTimer += 4f;

                    missileShootTimer = 0f;
                    if (attackTimer >= 180f)
					{
                        attackState++;
                        attackTimer = 0f;
					}
                    break;

                // Slow down and release a bunch of missiles.
                case 1:
                    npc.velocity = npc.velocity.MoveTowards(Vector2.Zero, 0.5f) * 0.95f;

                    missileShootTimer++;
                    if (missileShootTimer >= missileShootRate)
					{
                        Main.PlaySound(SoundID.Item11, npc.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
						{
                            Vector2 abdomenPosition = npc.Center + Vector2.UnitY.RotatedBy(npc.rotation) * new Vector2(npc.spriteDirection, 1f) * 108f;
                            Vector2 shootDirection = (abdomenPosition - npc.Center).SafeNormalize(Vector2.UnitY);
                            shootDirection = shootDirection.RotateTowards(npc.SafeDirectionTo(target.Center).ToRotation(), Main.rand.NextFloat(0.74f, 1.04f));
                            Vector2 shootVelocity = shootDirection.RotatedByRandom(0.31f) * missileShootSpeed;
                            Utilities.NewProjectileBetter(abdomenPosition, shootVelocity, ModContent.ProjectileType<RedirectingPlagueMissile>(), 160, 0f);
                        }
                        missileShootTimer = 0f;
                        npc.netUpdate = true;
                    }

                    if (attackTimer >= 120f)
					{
                        attackCycleCounter++;
                        if (attackCycleCounter >= attackCycleCount)
                            GotoNextAttackState(npc);
						else
						{
                            attackTimer = 0f;
                            attackState = 0f;
						}
                        npc.netUpdate = true;
					}
                    break;
            }

            // Determine rotation.
            npc.rotation = npc.velocity.X * 0.0125f;
        }

        public static void DoBehavior_PlagueVomit(NPC npc, Player target, ref float attackTimer, float enrageFactor, ref float frameType)
        {
            int attackCycleCount = 2;
            int vomitShootRate = (int)(55f - enrageFactor * 25f);
            float vomitShootSpeed = 11f;
            ref float attackState = ref npc.Infernum().ExtraAI[0];
            ref float bombingCount = ref npc.Infernum().ExtraAI[1];
            ref float vomitShootTimer = ref npc.Infernum().ExtraAI[2];
            ref float attackCycleCounter = ref npc.Infernum().ExtraAI[3];

            frameType = (int)PBGFrameType.Fly;
            switch ((int)attackState)
            {
                // Attempt to hover near the target.
                case 0:
                    Vector2 hoverOffset = new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 420f, -360f);
                    Vector2 hoverDestination = target.Center + hoverOffset;
                    npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 21f, 1f);
                    npc.Center = npc.Center.MoveTowards(hoverDestination, 4f);

                    // Make the attack go by way quicker once in position.
                    if (npc.WithinRange(hoverDestination, 35f))
                        attackTimer += 4f;

                    vomitShootTimer = 0f;
                    if (attackTimer >= 120f)
                    {
                        attackState++;
                        attackTimer = 0f;
                    }
                    break;

                // Slow down and release a bunch of vomits.
                case 1:
                    npc.velocity = npc.velocity.MoveTowards(Vector2.Zero, 0.5f) * 0.95f;

                    vomitShootTimer++;
                    if (vomitShootTimer >= vomitShootRate)
                    {
                        Main.PlaySound(SoundID.Item11, npc.Center);

                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 mouthPosition = npc.Center;
                            mouthPosition += Vector2.UnitY.RotatedBy(npc.rotation) * 18f;
                            mouthPosition -= Vector2.UnitX.RotatedBy(npc.rotation) * npc.spriteDirection * -68f;
                            Vector2 shootVelocity = (target.Center - mouthPosition).SafeNormalize(Vector2.UnitY) * vomitShootSpeed;
                            Utilities.NewProjectileBetter(mouthPosition, shootVelocity, ModContent.ProjectileType<PlagueVomit>(), 160, 0f);
                        }
                        vomitShootTimer = 0f;
                        npc.netUpdate = true;
                    }

                    if (attackTimer >= 180f)
                    {
                        attackCycleCounter++;
                        if (attackCycleCounter >= attackCycleCount)
                            GotoNextAttackState(npc);
                        else
                        {
                            attackTimer = 0f;
                            attackState = 0f;
                        }
                        npc.netUpdate = true;
                    }
                    break;
            }

            // Determine rotation.
            npc.rotation = npc.velocity.X * 0.0125f;
        }

        public static void DoBehavior_CarpetBombing(NPC npc, Player target, float enrageFactor, ref float frameType)
        {
            int maxChargeCount = (int)Math.Ceiling(2f + enrageFactor * 1.1f);
            int chargeTime = (int)(68f - enrageFactor * 31f);
            float chargeSpeed = enrageFactor * 2.75f + 27.5f;

            ref float chargeCount = ref npc.Infernum().ExtraAI[0];
            ref float chargeTimer = ref npc.Infernum().ExtraAI[1];
            ref float chargeState = ref npc.Infernum().ExtraAI[2];
            ref float hoverTimer = ref npc.Infernum().ExtraAI[3];
            Vector2 hoverOffset = new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 740f, -325f);

            // Do initializations.
            if (Main.netMode != NetmodeID.MultiplayerClient && chargeState == 0f)
            {
                chargeState = 1f;
                npc.netUpdate = true;
            }

            // Hover until reaching the destination.
            if (chargeState == 1f)
            {
                Vector2 hoverDestination = target.Center + hoverOffset;
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

                if (npc.WithinRange(hoverDestination, 195f))
                {
                    npc.velocity *= 0.95f;

                    // Do the charge.
                    if (npc.WithinRange(hoverDestination, 135f) && hoverTimer > 30f)
                    {
                        hoverTimer = 0f;
                        chargeState = 2f;
                        npc.velocity = Vector2.UnitX * npc.SafeDirectionTo(target.Center, Vector2.UnitX).X * chargeSpeed;
                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        npc.netUpdate = true;

                        Main.PlaySound(SoundID.Roar, target.Center, 0);
                    }
                }
                else
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 19f, 0.95f);
                npc.Center = npc.Center.MoveTowards(hoverDestination, 3f);
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

				// Otherwise, release missiles.
				else if (Main.netMode != NetmodeID.MultiplayerClient && chargeTimer % 6f == 5f)
				{
                    Vector2 missileShootVelocity = new Vector2(npc.velocity.X * 0.6f, 12f);
                    Utilities.NewProjectileBetter(npc.Center + missileShootVelocity * 2f, missileShootVelocity, ModContent.ProjectileType<PlagueMissile>(), 160, 0f);
				}

                if (chargeTimer >= chargeTime)
                {
                    chargeCount++;
                    chargeTimer = 0f;
                    chargeState = 0f;
                    npc.netUpdate = true;

                    if (chargeCount > maxChargeCount)
                        GotoNextAttackState(npc);
                }
            }
        }

        public static void GotoNextAttackState(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            PBGAttackType currentAttackState = (PBGAttackType)(int)npc.ai[0];
            PBGAttackType newAttackState = PBGAttackType.Charge;
            switch (currentAttackState)
            {
                case PBGAttackType.Charge:
                    newAttackState = PBGAttackType.MissileLaunch;
                    break;
                case PBGAttackType.MissileLaunch:
                    newAttackState = PBGAttackType.CarpetBombing;
                    break;
                case PBGAttackType.CarpetBombing:
                    newAttackState = PBGAttackType.PlagueVomit;
                    break;
                case PBGAttackType.PlagueVomit:
                    newAttackState = PBGAttackType.Charge;
                    break;
            }

            npc.ai[0] = (int)newAttackState;
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
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