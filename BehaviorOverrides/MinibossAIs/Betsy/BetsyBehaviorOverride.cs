using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.MinibossAIs.Betsy
{
    public class BetsyBehaviorOverride : NPCBehaviorOverride
    {
        public enum BetsyAttackType
        {
            Charges,
            FlameBreath,
            ExplodingWyvernSummoning,
            SpinCharge,
            MeteorVomit,
        }

        public override int NPCOverrideType => NPCID.DD2Betsy;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame | NPCOverrideContext.NPCPreDraw;

        public override bool PreAI(NPC npc) => DoAI(npc);

        public static bool DoAI(NPC npc)
        {
            // Select a target.
            OldOnesArmyMinibossChanges.TargetClosestMiniboss(npc);
            NPCAimedTarget target = npc.GetTargetData();

            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float attackDelay = ref npc.ai[2];
            ref float currentFrame = ref npc.localAI[0];
            ref float wingArmFrameCounter = ref npc.localAI[1];

            // Clear pickoff enemies.
            OldOnesArmyMinibossChanges.ClearPickoffOOAEnemies();

            if (attackDelay < 90f)
            {
                attackDelay++;
                wingArmFrameCounter++;
                npc.velocity = -Vector2.UnitY * 6.5f;
                return false;
            }

            switch ((BetsyAttackType)(int)attackState)
            {
                case BetsyAttackType.Charges:
                    DoBehavior_Charges(npc, target, ref attackTimer, ref wingArmFrameCounter);
                    break;
                case BetsyAttackType.FlameBreath:
                    DoBehavior_FlameBreath(npc, target, ref attackTimer, ref currentFrame, ref wingArmFrameCounter);
                    break;
                case BetsyAttackType.ExplodingWyvernSummoning:
                    DoBehavior_ExplodingWyvernSummoning(npc, target, ref attackTimer, ref currentFrame, ref wingArmFrameCounter);
                    break;
                case BetsyAttackType.SpinCharge:
                    DoBehavior_SpinCharge(npc, target, ref attackTimer, ref wingArmFrameCounter);
                    break;
                case BetsyAttackType.MeteorVomit:
                    DoBehavior_MeteorVomit(npc, target, ref attackTimer, ref currentFrame, ref wingArmFrameCounter);
                    break;
            }

            attackTimer++;
            return false;
        }

        public static void DoBehavior_Charges(NPC npc, NPCAimedTarget target, ref float attackTimer, ref float wingArmFrameCounter)
        {
            int hoverRedirectTime = 22;
            int chargeTime = 32;
            int slowdownTime = 12;
            int chargeCount = 4;
            float chargeSpeed = 33f;
            ref float chargeCounter = ref npc.Infernum().ExtraAI[0];

            float idealRotation = npc.AngleTo(target.Center);
            if (npc.spriteDirection == -1)
                idealRotation += MathHelper.Pi;

            if (attackTimer < hoverRedirectTime)
            {
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                Vector2 destination = target.Center + new Vector2(npc.spriteDirection * -500f, -200f);
                npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * 27f, 1.6f);
                npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.04f);
            }

            if (attackTimer == hoverRedirectTime)
            {
                npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();

                idealRotation = npc.AngleTo(target.Center);
                if (npc.spriteDirection == -1)
                    idealRotation += MathHelper.Pi;
                npc.rotation = idealRotation;

                Main.PlaySound(SoundID.DD2_BetsyWindAttack, target.Center);
            }

            // Flap wings faster depending on fly speed.
            wingArmFrameCounter += MathHelper.Max(npc.velocity.Length() * 0.03f, 0.35f);
            if (attackTimer > hoverRedirectTime + chargeTime)
                npc.velocity *= 0.97f;

            if (attackTimer >= hoverRedirectTime + chargeTime + slowdownTime)
            {
                attackTimer = 0f;
                chargeCounter++;
                if (chargeCounter >= chargeCount)
                    SelectNextAttack(npc);

                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_MeteorVomit(NPC npc, NPCAimedTarget target, ref float attackTimer, ref float currentFrame, ref float wingArmFrameCounter)
        {
            int hoverRedirectTime = 40;
            int chargeTime = 60;
            float hoverSpeed = 31f;
            float horizontalFlySpeed = 9f;

            // Move into position.
            if (attackTimer < hoverRedirectTime)
            {
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 660f, -320f);
                Vector2 velocity = npc.SafeDirectionTo(hoverDestination) * hoverSpeed;
                if (npc.WithinRange(hoverDestination, hoverSpeed * 1.1f))
                {
                    attackTimer = hoverRedirectTime - 1f;
                    npc.Center = hoverDestination;
                    npc.netUpdate = true;
                }
                else
                    npc.position += velocity;

                float idealRotation = npc.AngleTo(target.Center);
                if (npc.spriteDirection == -1)
                    idealRotation += MathHelper.Pi;
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.04f);

                wingArmFrameCounter += 0.9f;
                currentFrame = MathHelper.Lerp(5f, 8f, attackTimer / hoverRedirectTime);
            }

            // Do the charge.
            if (attackTimer == hoverRedirectTime)
            {
                npc.velocity = Vector2.UnitX * (target.Center.X > npc.Center.X).ToDirectionInt() * horizontalFlySpeed;
                npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
            }

            // Update movement and visual effects while releasing meteors.
            if (attackTimer >= hoverRedirectTime)
            {
                if (Math.Abs(target.Center.X - npc.Center.X) > 550f && Math.Abs(npc.velocity.X) < 27f)
                    npc.velocity.X += Math.Sign(npc.velocity.X) * 0.5f;

                npc.rotation = npc.rotation.AngleLerp(0f, 0.05f).AngleTowards(0f, 0.125f);
                wingArmFrameCounter += 1.25f;
                currentFrame = MathHelper.Lerp(currentFrame, 10f, 0.15f);

                if (attackTimer % 6f == 5f)
                {
                    Main.PlayTrackedSound(SoundID.DD2_BetsyFireballShot, npc.Center);

                    Vector2 mouthPosition = npc.Center + new Vector2(npc.spriteDirection * 140f, 20f).RotatedBy(npc.rotation);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 meteorShootVelocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.TwoPi * npc.spriteDirection / 16f) * 33f;
                        meteorShootVelocity += Main.rand.NextVector2Circular(4f, 4f);
                        Utilities.NewProjectileBetter(mouthPosition, meteorShootVelocity, ModContent.ProjectileType<MoltenMeteor>(), 180, 0f);
                    }
                }
            }

            if (attackTimer >= hoverRedirectTime + chargeTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_ExplodingWyvernSummoning(NPC npc, NPCAimedTarget target, ref float attackTimer, ref float currentFrame, ref float wingArmFrameCounter)
        {
            int summonTime = 145;

            npc.velocity *= 0.95f;
            if (attackTimer < summonTime)
                currentFrame = MathHelper.Lerp(0f, 4f, MathHelper.Clamp(attackTimer / summonTime * 3f, 0f, 1f));
            wingArmFrameCounter += 0.85f;

            if (attackTimer == summonTime / 2)
            {
                Main.PlaySound(SoundID.DD2_BetsySummon, target.Center);
                Main.PlaySound(SoundID.DD2_BetsyScream, target.Center);
            }
            if (attackTimer >= summonTime / 2 && attackTimer % 4f == 3f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 summonPosition = target.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(240f, 600f);
                Vector2 wyvernVelocity = -Vector2.UnitY * Main.rand.NextFloat(1f, 6.5f);
                Utilities.NewProjectileBetter(summonPosition, wyvernVelocity, ModContent.ProjectileType<ExplodingWyvern>(), 0, 0f);
            }

            if (attackTimer >= summonTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_SpinCharge(NPC npc, NPCAimedTarget target, ref float attackTimer, ref float wingArmFrameCounter)
        {
            float upwardFlyTime = 70f;
            float horizontalMovementDelay = 45f;
            float totalSpins = 1f;
            float spinTime = 45f;
            float chargeSpeed = 38.5f;
            ref float hasChargedYet01Flag = ref npc.Infernum().ExtraAI[0];

            // Slow down quickly during the delay.
            if (attackTimer < 45f)
                npc.velocity *= 0.97f;

            // Approach a position to the top left/right of the target.
            if (attackTimer > 45f && attackTimer < upwardFlyTime)
            {
                ref float xAimOffset = ref npc.Infernum().ExtraAI[4];
                if (xAimOffset == 0f)
                    xAimOffset = 820f * Math.Sign((npc.Center - target.Center).X);

                Vector2 destination = target.Center + new Vector2(xAimOffset, -450f);

                if (npc.WithinRange(destination, 16f))
                    npc.Center = destination;
                else
                    npc.velocity = npc.SafeDirectionTo(destination) * 16f;

                npc.spriteDirection = (npc.velocity.X > 0).ToDirectionInt();
                npc.rotation = npc.rotation.AngleLerp(0f, 0.2f);

                if (npc.WithinRange(destination, 42f))
                    attackTimer = upwardFlyTime - 1f;
            }

            // Charge after either the hover position is reached or enough time has passed.
            if (attackTimer == upwardFlyTime)
            {
                int direction = Math.Sign(npc.SafeDirectionTo(target.Center).X);
                npc.velocity = Vector2.UnitX * direction * 35f;
                npc.spriteDirection = (npc.velocity.X > 0).ToDirectionInt();
            }

            // Spin and charge at the target once the spin happens to point very closely at the target.
            if (attackTimer > upwardFlyTime + horizontalMovementDelay && (attackTimer - upwardFlyTime - horizontalMovementDelay) % 90f <= spinTime)
            {
                // Reset the attack timer if an opening for a charge is found, and charge towards the player.
                if (hasChargedYet01Flag == 0f)
                {
                    // Spin 2 win.
                    npc.velocity = npc.velocity.RotatedBy(MathHelper.TwoPi / spinTime);

                    bool aimedTowardsPlayer = npc.velocity.AngleBetween(npc.SafeDirectionTo(target.Center)) < MathHelper.ToRadians(18f);
                    if (attackTimer >= upwardFlyTime + horizontalMovementDelay + 75f * (totalSpins - 1) && aimedTowardsPlayer)
                    {
                        npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                        attackTimer = upwardFlyTime + horizontalMovementDelay + 75f * (totalSpins - 1);
                        hasChargedYet01Flag = 1f;
                    }
                }
                npc.spriteDirection = (npc.velocity.X > 0).ToDirectionInt();
                npc.rotation = npc.velocity.ToRotation() + (npc.spriteDirection == -1).ToInt() * MathHelper.Pi;
            }

            wingArmFrameCounter += 1.05f;
            if (attackTimer >= upwardFlyTime + horizontalMovementDelay + totalSpins * 55f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_FlameBreath(NPC npc, NPCAimedTarget target, ref float attackTimer, ref float currentFrame, ref float wingArmFrameCounter)
        {
            int hoverRedirectTime = 50;
            int chargeTime = 75;
            float hoverSpeed = 33f;
            float horizontalFlySpeed = 11f;

            // Move into position.
            if (attackTimer < hoverRedirectTime)
            {
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 560f, -130f);
                Vector2 velocity = npc.SafeDirectionTo(hoverDestination) * hoverSpeed;
                if (npc.WithinRange(hoverDestination, hoverSpeed * 1.1f))
                {
                    attackTimer = hoverRedirectTime - 1f;
                    npc.Center = hoverDestination;
                    npc.netUpdate = true;
                }
                else
                    npc.position += velocity;

                float idealRotation = npc.AngleTo(target.Center);
                if (npc.spriteDirection == -1)
                    idealRotation += MathHelper.Pi;
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.04f);

                wingArmFrameCounter += 0.9f;
                currentFrame = MathHelper.Lerp(5f, 8f, attackTimer / hoverRedirectTime);
            }

            // Do the charge.
            if (attackTimer == hoverRedirectTime)
            {
                Main.PlayTrackedSound(SoundID.DD2_BetsyFlameBreath, npc.Center);
                npc.velocity = Vector2.UnitX * (target.Center.X > npc.Center.X).ToDirectionInt() * horizontalFlySpeed;
                npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
                Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ProjectileID.DD2BetsyFlameBreath, 190, 0f, -1, 0f, npc.whoAmI);
            }

            // Update movement and visual effects while releasing meteors.
            if (attackTimer >= hoverRedirectTime)
            {
                if (Math.Abs(target.Center.X - npc.Center.X) > 550f && Math.Abs(npc.velocity.X) < 27f)
                    npc.velocity.X += Math.Sign(npc.velocity.X) * 0.5f;

                npc.rotation = npc.rotation.AngleLerp(0f, 0.05f).AngleTowards(0f, 0.125f);
                wingArmFrameCounter += 1.25f;
                currentFrame = MathHelper.Lerp(currentFrame, 10f, 0.15f);

                if (attackTimer % 10f == 9f)
                {
                    Main.PlayTrackedSound(SoundID.DD2_BetsyFireballShot, npc.Center);

                    Vector2 mouthPosition = npc.Center + new Vector2(npc.spriteDirection * 140f, 20f).RotatedBy(npc.rotation);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 meteorShootVelocity = -Vector2.UnitY.RotatedByRandom(0.4f) * Main.rand.NextFloat(19f, 22f);
                        Utilities.NewProjectileBetter(mouthPosition, meteorShootVelocity, ProjectileID.DD2BetsyFireball, 180, 0f);
                    }
                }
            }

            if (attackTimer >= hoverRedirectTime + chargeTime)
                SelectNextAttack(npc);
        }

        public static void SelectNextAttack(NPC npc)
        {
            npc.localAI[0] = 0f;
            switch ((BetsyAttackType)(int)npc.ai[0])
            {
                case BetsyAttackType.Charges:
                    npc.ai[0] = (int)BetsyAttackType.FlameBreath;
                    break;
                case BetsyAttackType.FlameBreath:
                    npc.ai[0] = (int)BetsyAttackType.ExplodingWyvernSummoning;
                    break;
                case BetsyAttackType.ExplodingWyvernSummoning:
                    npc.ai[0] = (int)BetsyAttackType.SpinCharge;
                    break;
                case BetsyAttackType.SpinCharge:
                    npc.ai[0] = (int)BetsyAttackType.MeteorVomit;
                    break;
                case BetsyAttackType.MeteorVomit:
                    npc.ai[0] = (int)BetsyAttackType.Charges;
                    break;
            }

            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frame.Y = (int)Math.Round(npc.localAI[0]) * frameHeight;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D npcTexture = Main.npcTexture[npc.type];
            Texture2D wingsTexture = Main.extraTexture[81];
            Texture2D armsTexture = Main.extraTexture[82];
            SpriteEffects direction = (npc.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally);

            int wingArmFrame = (int)(npc.localAI[1] / 4f) % 9;
            Vector2 drawPosition = npc.Center - Main.screenPosition;

            Vector2 npcOrigin = Vector2.Lerp(new Vector2(171f, 44f), new Vector2(230f, 52f), 0.5f) + new Vector2(-50f, 30f);
            Vector2 wingsDrawOffset = new Vector2(171f, 44f) - npcOrigin;
            Vector2 armsDrawOffset = new Vector2(230f, 52f) - npcOrigin;
            if (direction.HasFlag(SpriteEffects.FlipHorizontally))
                armsDrawOffset.X *= -1f;

            Rectangle armFrame = armsTexture.Frame(2, 5, wingArmFrame / 5, wingArmFrame % 5);
            Vector2 armsOrigin = new Vector2(16f, 176f);
            if (direction.HasFlag(SpriteEffects.FlipHorizontally))
                armsOrigin.X = armFrame.Width - armsOrigin.X;
            if (direction.HasFlag(SpriteEffects.FlipHorizontally))
                npcOrigin.X = npc.frame.Width - npcOrigin.X;

            if (direction.HasFlag(SpriteEffects.FlipHorizontally))
                wingsDrawOffset.X *= -1f;

            Rectangle wingsFrame = wingsTexture.Frame(2, 5, wingArmFrame / 5, wingArmFrame % 5);
            Vector2 wingsOrigin = new Vector2(215f, 170f);
            if (direction.HasFlag(SpriteEffects.FlipHorizontally))
                wingsOrigin.X = wingsFrame.Width - wingsOrigin.X;

            Color color = npc.GetAlpha(lightColor);
            for (int i = npc.oldPos.Length - 1; i > 0; i -= 3)
            {
                Vector2 afterimageDrawPosition = npc.oldPos[i] + npc.Size / 2f - Main.screenPosition;
                float oldRotation = npc.oldRot[i];

                Color afterimageColor = color * (1f - i / 10f) * 0.35f;
                afterimageColor.A /= 2;

                spriteBatch.Draw(armsTexture, afterimageDrawPosition + armsDrawOffset.RotatedBy(oldRotation), armFrame, afterimageColor, oldRotation, armsOrigin, 1f, direction, 0f);
                spriteBatch.Draw(npcTexture, afterimageDrawPosition, npc.frame, afterimageColor, oldRotation, npcOrigin, 1f, direction, 0f);
                spriteBatch.Draw(wingsTexture, afterimageDrawPosition + wingsDrawOffset.RotatedBy(oldRotation), wingsFrame, afterimageColor, oldRotation, wingsOrigin, 1f, direction, 0f);
            }
            spriteBatch.Draw(armsTexture, drawPosition + armsDrawOffset.RotatedBy(npc.rotation), armFrame, color, npc.rotation, armsOrigin, 1f, direction, 0f);
            spriteBatch.Draw(npcTexture, drawPosition, npc.frame, color, npc.rotation, npcOrigin, 1f, direction, 0f);
            spriteBatch.Draw(wingsTexture, drawPosition + wingsDrawOffset.RotatedBy(npc.rotation), wingsFrame, color, npc.rotation, wingsOrigin, 1f, direction, 0f);
            return false;
        }
    }
}
