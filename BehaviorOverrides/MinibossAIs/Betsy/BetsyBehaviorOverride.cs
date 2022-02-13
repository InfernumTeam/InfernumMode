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
            MeteorVomit
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
            ref float currentFrame = ref npc.localAI[0];
            ref float wingArmFrameCounter = ref npc.localAI[1];

            // Clear pickoff enemies.
            OldOnesArmyMinibossChanges.ClearPickoffOOAEnemies();

            switch ((BetsyAttackType)(int)attackState)
            {
                case BetsyAttackType.Charges:
                    DoBehavior_Charges(npc, target, ref attackTimer, ref wingArmFrameCounter);
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
            if (npc.spriteDirection == 1)
                idealRotation += MathHelper.Pi;

            if (attackTimer < hoverRedirectTime)
            {
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                Vector2 destination = target.Center + new Vector2(npc.spriteDirection * 500f, -200f);
                npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * 27f, 1.6f);
                npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.04f);
            }

            if (attackTimer == hoverRedirectTime)
            {
                npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                npc.spriteDirection = (npc.velocity.X < 0f).ToDirectionInt();

                idealRotation = npc.AngleTo(target.Center);
                if (npc.spriteDirection == 1)
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
                {
                    chargeCounter = 0f;
                    npc.ai[0] = (int)BetsyAttackType.MeteorVomit;
                }
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
                if (npc.spriteDirection == 1)
                    idealRotation += MathHelper.Pi;
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.04f);

                wingArmFrameCounter += 0.9f;
                currentFrame = MathHelper.Lerp(5f, 8f, attackTimer / hoverRedirectTime);
            }

            // Do the charge.
            if (attackTimer == hoverRedirectTime)
            {
                npc.velocity = Vector2.UnitX * (target.Center.X > npc.Center.X).ToDirectionInt() * horizontalFlySpeed;
                npc.spriteDirection = (npc.velocity.X < 0f).ToDirectionInt();
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
                    Vector2 mouthPosition = npc.Center + new Vector2(npc.spriteDirection * -140f, 20f).RotatedBy(npc.rotation);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 meteorShootVelocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.TwoPi * npc.spriteDirection / -16f) * 33f;
                        meteorShootVelocity += Main.rand.NextVector2Circular(4f, 4f);
                        Utilities.NewProjectileBetter(mouthPosition, meteorShootVelocity, ModContent.ProjectileType<MoltenMeteor>(), 180, 0f);
                    }
                }
            }

            if (attackTimer >= hoverRedirectTime + chargeTime)
            {
                currentFrame = 0f;
                npc.ai[0] = (int)BetsyAttackType.Charges;
                npc.netUpdate = true;
            }
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
            SpriteEffects direction = (npc.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally) ^ SpriteEffects.FlipHorizontally;

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
