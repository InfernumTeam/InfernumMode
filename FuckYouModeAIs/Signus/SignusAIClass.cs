using CalamityMod.Dusts;
using CalamityMod.Projectiles.Boss;
using InfernumMode.FuckYouModeAIs.MainAI;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Signus
{
	public class SignusAIClass
    {
        public enum DoGSignusAIState
        {
            ShadowDashes,
            CosmicKnifeDash,
            ScytheDashThrow,
            CloneKnives,
            DartPierce,
            FakeoutCharge
        }
        public enum DoGSignusDrawState
        {
            IdleFly,
            AggressiveFly,
            Charge
        }

        public const int CloneAppearTime = 150;
        public const int TotalClones = 8;

        [OverrideAppliesTo("Signus", typeof(SignusAIClass), "DoGSignusAI", EntityOverrideContext.NPCAI, true)]
        public static bool DoGSignusAI(NPC npc)
        {
            // Targeting.
            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead)
                npc.TargetClosest(true);

            float lifeRatio = npc.life / (float)npc.lifeMax;
            Player target = Main.player[npc.target];

            ref float frameState = ref npc.ai[0];
            ref float aiState = ref npc.ai[1];
            ref float attackTimer = ref npc.ai[2];
            ref float glowmaskOpacity = ref npc.localAI[0];

            frameState = (int)DoGSignusDrawState.IdleFly;

            npc.rotation = npc.rotation.AngleTowards(npc.velocity.X * 0.015f, 0.05f);
            npc.spriteDirection = (npc.velocity.X > 0).ToDirectionInt();
            glowmaskOpacity = 1f;

            switch ((DoGSignusAIState)(int)aiState)
            {
                case DoGSignusAIState.ShadowDashes:
                    if (attackTimer < 60f)
                    {
                        float flySpeed = MathHelper.Lerp(0f, 18f, (float)Math.Pow(Utils.InverseLerp(0f, 60f, attackTimer, true), 3f));
                        flySpeed *= Utils.InverseLerp(4f, 40f, npc.Distance(target.Center), true);

                        npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.05f, true) * MathHelper.Lerp(npc.velocity.Length(), flySpeed, 0.35f);
                        npc.Opacity = MathHelper.Lerp(npc.Opacity, 0.05f, 0.25f);
                    }
                    else if (attackTimer < 300f)
                    {
                        float oldSpeed = npc.velocity.Length();
                        if (attackTimer % 45f == 0f)
                            npc.velocity = npc.DirectionTo(target.Center);
                        float chargeSpeed = (float)Math.Sin(attackTimer % 45f / 45f * MathHelper.Pi) * 29f + 14f;
                        if (chargeSpeed > (lifeRatio < 0.55f ? 20f : 15.5f))
                            chargeSpeed = lifeRatio < 0.55f ? 20f : 15.5f;

                        if (npc.Distance(target.Center) < 80f)
                        {
                            npc.Opacity = MathHelper.Lerp(npc.Opacity, 0.9f, 0.45f);

                            if (attackTimer % 6f == 5f && Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Vector2 shootVelocity = npc.DirectionTo(target.Center) * 20f;
                                Utilities.NewProjectileBetter(npc.Center + shootVelocity * 1.7f, shootVelocity, ModContent.ProjectileType<SignusScythe>(), 300, 0f);
                            }
                            npc.spriteDirection = (target.Center.X - npc.Center.X > 0).ToDirectionInt();
                            chargeSpeed *= 0.1f;
                            frameState = (int)DoGSignusDrawState.AggressiveFly;
                        }
                        else
                        {
                            npc.Opacity = MathHelper.Lerp(npc.Opacity, 0.02f, 0.25f);
                        }

                        npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(oldSpeed, chargeSpeed, 0.15f);
                        float playerDistanceOrthogonality = Vector2.Dot(npc.velocity.SafeNormalize(Vector2.Zero), npc.DirectionTo(target.Center));
                        if (playerDistanceOrthogonality < 0.55f)
                        {
                            oldSpeed = npc.velocity.Length();
                            npc.velocity = Vector2.Lerp(npc.velocity, npc.DirectionTo(target.Center), 0.06f);
                            npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * oldSpeed;
                        }
                    }

                    if (attackTimer >= 320f)
                    {
                        npc.Opacity = MathHelper.Lerp(npc.Opacity, 0.75f, 0.25f);
                    }
                    if (attackTimer >= 340f)
                    {
                        aiState = (int)DoGSignusAIState.CosmicKnifeDash;
                        attackTimer = 0f;
                        npc.netUpdate = true;
                    }
                    break;
                case DoGSignusAIState.CosmicKnifeDash:
                    int dashTime = lifeRatio < 0.5f ? 90 : 60;
                    int knifeSpawnRate = 5;
                    float dashOffset = lifeRatio < 0.5f ? 1360f : 1000f;
                    if (attackTimer < 45f)
                    {
                        npc.Opacity = MathHelper.Lerp(npc.Opacity, 0.2f, 0.25f);
                        npc.velocity *= 0.96f;
                    }
                    if (attackTimer >= 45f && attackTimer <= 45f + dashTime)
                    {
                        if (attackTimer == 45f)
                        {
                            npc.Center = target.Center - new Vector2(dashOffset * 0.5f, 420f);
                            npc.velocity = Vector2.UnitX * dashOffset / dashTime;
                        }
                        if (attackTimer % knifeSpawnRate == knifeSpawnRate - 1 && Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<SignusKnife>(), 265, 0f);
                        }
                    }
                    if (attackTimer >= 45f + dashTime && attackTimer < 90f + dashTime)
                    {
                        npc.Opacity = MathHelper.Lerp(npc.Opacity, 0.75f, 0.25f);
                        npc.velocity *= 0.96f;
                    }
                    if (attackTimer == dashTime + 105f)
                    {
                        int knifeType = ModContent.ProjectileType<SignusKnife>();
                        for (int i = 0; i < Main.maxProjectiles; i++)
                        {
                            Projectile projectile = Main.projectile[i];
                            if (projectile.type != knifeType || !projectile.active)
                                continue;

                            projectile.velocity = projectile.DirectionTo(target.Center + target.velocity * 30f) * projectile.Distance(target.Center) / 30f;
                            projectile.ai[0] = 1f;
                            projectile.timeLeft = 120;
                        }
                    }
                    if (attackTimer == dashTime + 125f)
                    {
                        aiState = (int)DoGSignusAIState.ScytheDashThrow;
                        attackTimer = 0f;
                        npc.netUpdate = true;
                    }
                    break;
                case DoGSignusAIState.ScytheDashThrow:
                    int totalDashes = lifeRatio < 0.66f ? 7 : 5;
                    dashTime = 30;

                    ref float dashStartX = ref npc.Infernum().ExtraAI[0];
                    ref float dashStartY = ref npc.Infernum().ExtraAI[1];
                    ref float breakTime = ref npc.Infernum().ExtraAI[2];

                    npc.spriteDirection = (target.Center.X - npc.Center.X > 0).ToDirectionInt();
                    if (breakTime > 0)
                    {
                        npc.Opacity = MathHelper.Lerp(npc.Opacity, 0.75f, 0.35f);
                        dashStartX = npc.Center.X;
                        dashStartY = npc.Center.Y;
                        breakTime--;
                        return false;
                    }
                    float targetAngle = MathHelper.Lerp(0f, MathHelper.TwoPi, Utils.InverseLerp(0f, dashTime * totalDashes, attackTimer, true));

                    if (attackTimer % dashTime == 0)
                    {
                        dashStartX = npc.Center.X;
                        dashStartY = npc.Center.Y;

                        Vector2 baseDirection = npc.DirectionTo(target.Center + target.velocity * 16f);
                        for (int i = 0; i < 2; i++)
                        {
                            float offsetAngle = MathHelper.Lerp(-0.4f, 0.4f, i / 2f);
                            Vector2 shootVelocity = baseDirection.RotatedBy(offsetAngle) * 24f;
                            Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<SignusScythe>(), 265, 0f);
                        }
                        breakTime = 15f;
                        npc.netUpdate = true;
                    }
                    npc.Opacity = MathHelper.Lerp(npc.Opacity, 0.06f, 0.25f);
                    npc.Center = Vector2.Lerp(new Vector2(dashStartX, dashStartY), target.Center + targetAngle.ToRotationVector2() * 600f, attackTimer % dashTime / dashTime);
                    if (attackTimer >= dashTime * totalDashes)
                    {
                        dashStartX = dashStartY = breakTime = 0f;
                        aiState = lifeRatio < 0.5f ? (int)DoGSignusAIState.CloneKnives : (int)DoGSignusAIState.ShadowDashes;
                        attackTimer = 0f;
                        npc.netUpdate = true;
                    }
                    break;
                case DoGSignusAIState.CloneKnives:
                    ref float cloneIndex = ref npc.Infernum().ExtraAI[0];
                    if (attackTimer < 5)
                    {
                        npc.Opacity = MathHelper.Lerp(npc.Opacity, 1f, 0.6f);
                        npc.velocity *= 0.5f;
                    }
                    if (attackTimer == 5f)
                    {
                        npc.Center = target.Center - Vector2.UnitY * 450f;
                        cloneIndex = Main.rand.Next(TotalClones);
                    }

                    // Immediately kill all clones if hit, and only fire one scythe barrage.
                    if (attackTimer > 5f && attackTimer <= 5f + CloneAppearTime && npc.justHit)
                    {
                        attackTimer = 6f + CloneAppearTime;

                        if (npc.Distance(target.Center) > 60f && Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            float shootSpeed = Vector2.Distance(npc.Center, target.Center) / 30f;
                            shootSpeed = MathHelper.Max(6f, shootSpeed);

                            for (int i = 0; i < 6; i++)
                            {
                                float offsetAngle = MathHelper.Lerp(-0.5f, 0.5f, i / 6f);
                                Utilities.NewProjectileBetter(npc.Center, npc.DirectionTo(target.Center).RotatedBy(offsetAngle) * shootSpeed, ModContent.ProjectileType<SignusScythe>(), 270, 0f);
                            }
                        }
                    }

                    if (attackTimer == 5f + CloneAppearTime)
                    {
                        foreach (var attackPoint in GetSignusDrawPoints(npc))
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                float shootSpeed = Vector2.Distance(attackPoint, target.Center) / 30f;
                                shootSpeed = MathHelper.Max(6f, shootSpeed);

                                for (int i = 0; i < 3; i++)
                                {
                                    float offsetAngle = MathHelper.Lerp(-0.3f, 0.3f, i / 3f);
                                    Utilities.NewProjectileBetter(npc.Center, (target.Center - attackPoint).SafeNormalize(Vector2.UnitY).RotatedBy(offsetAngle) * shootSpeed, ModContent.ProjectileType<SignusScythe>(), 270, 0f);
                                }
                            }

                            if (!Main.dedServ)
                            {
                                Utils.PoofOfSmoke(attackPoint);
                                for (int i = 0; i < 25; i++)
                                {
                                    Dust magic = Dust.NewDustDirect(attackPoint, npc.width, npc.height, (int)CalamityDusts.PurpleCosmolite);
                                    magic.velocity = Main.rand.NextVector2CircularEdge(5f, 5f);
                                    magic.scale = Main.rand.NextFloat(0.95f, 1.3f);
                                    magic.noGravity = true;
                                }
                            }
                        }
                    }
                    if (attackTimer == 25f + CloneAppearTime)
                    {
                        npc.Center = target.Center - Vector2.UnitY.RotatedByRandom(0.5f) * 450f;
                    }
                    if (attackTimer >= 45f + CloneAppearTime)
                    {
                        aiState = lifeRatio < 0.275f ? (int)DoGSignusAIState.FakeoutCharge : (int)DoGSignusAIState.DartPierce;
                        attackTimer = 0f;
                        cloneIndex = 0f;
                        npc.netUpdate = true;
                    }
                    break;
                case DoGSignusAIState.DartPierce:
                    if (attackTimer <= 90f)
                    {
                        npc.spriteDirection = (target.Center.X - npc.Center.X > 0).ToDirectionInt();
                        npc.Center = Vector2.Lerp(npc.Center, target.Center - Vector2.UnitY * 320f, 0.36f);
                    }
                    if ((attackTimer == 120f || attackTimer == 220f || attackTimer == 310f) &&
                        Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float shootSpeed = Vector2.Distance(npc.Center, target.Center) / 18f;
                        shootSpeed = MathHelper.Max(25f, shootSpeed);
                        Vector2 shootVelocity = npc.DirectionTo(target.Center + target.velocity * 20f) * shootSpeed;
                        Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<SignusSplittingKnife>(), 350, 0f, Main.myPlayer);
                    }
                    if (attackTimer == 500f)
                    {
                        aiState = (int)DoGSignusAIState.ShadowDashes;
                        attackTimer = 0f;
                        npc.netUpdate = true;
                    }
                    break;
                case DoGSignusAIState.FakeoutCharge:
                    frameState = (int)DoGSignusDrawState.Charge;

                    int chargeTime = 30;
                    if (attackTimer <= 90f)
                    {
                        npc.SimpleFlyMovement(npc.DirectionTo(target.Center - Vector2.UnitY * 250f - npc.velocity) * 15f, 0.15f);
                        npc.rotation = npc.AngleTo(target.Center);
                        glowmaskOpacity = Utils.InverseLerp(0f, 45f, attackTimer, true);
                    }
                    if (attackTimer >= 92f)
                    {
                        if (attackTimer % chargeTime == 0f)
                        {
                            npc.Center = target.Center + target.velocity.SafeNormalize(Vector2.UnitX * target.direction).RotatedBy(MathHelper.Pi * 0.18f) * 800f;
                            npc.velocity = npc.DirectionTo(target.Center + target.velocity * 5f) * 26f;
                        }

                        glowmaskOpacity = 1f - Utils.InverseLerp(chargeTime - 10f, chargeTime - 1f, attackTimer % chargeTime, true);
                        npc.Opacity = glowmaskOpacity;

                        npc.spriteDirection = (target.Center.X - npc.Center.X > 0).ToDirectionInt();
                        npc.rotation = npc.velocity.ToRotation();
                        if (npc.spriteDirection == -1)
                            npc.rotation -= MathHelper.Pi;
                    }

                    ScreenObstruction.screenObstruction = MathHelper.Lerp(ScreenObstruction.screenObstruction, 0.55f, 0.2f);
                    if (attackTimer >= 90f + chargeTime * 6)
                    {
                        aiState = (int)DoGSignusAIState.ShadowDashes;
                        attackTimer = 0f;
                        npc.netUpdate = true;
                    }
                    break;
            }

            attackTimer++;
            return false;
        }

        public static List<Vector2> GetSignusDrawPoints(NPC npc)
        {
            float attackTimer = npc.ai[2];
            List<Vector2> drawPoints = new List<Vector2>();
            float offsetPerClone = MathHelper.Lerp(0f, 145f, Utils.InverseLerp(5f, 40f, attackTimer, true));
            int cloneIndex = (int)npc.Infernum().ExtraAI[0];
            int leftCloneCount = TotalClones - cloneIndex;
            int rightCloneCount = cloneIndex;

            for (int i = 0; i < leftCloneCount; i++)
                drawPoints.Add(npc.Center - Vector2.UnitX * offsetPerClone * i);
            for (int i = 0; i < rightCloneCount; i++)
                drawPoints.Add(npc.Center + Vector2.UnitX * offsetPerClone * i);

            return drawPoints;
        }

        [ExcludeBasedOnProperty("Signus", EntityOverrideContext.NPCPreDraw, typeof(FuckYouModeAIsGlobal), "IsDoGAlive")]
        [OverrideAppliesTo("Signus", typeof(SignusAIClass), "DoGSignusPreDraw", EntityOverrideContext.NPCPreDraw)]
        public static bool DoGSignusPreDraw(NPC npc, SpriteBatch spriteBatch, Color drawColor)
        {
            void drawSignus(int direction, Vector2 position)
            {
                Texture2D NPCTexture;
                Texture2D glowMaskTexture;

                SpriteEffects spriteEffects = SpriteEffects.None;
                if (direction == 1)
                    spriteEffects = SpriteEffects.FlipHorizontally;

                Rectangle frame = npc.frame;
                int frameCount = Main.npcFrameCount[npc.type];

                switch ((DoGSignusDrawState)(int)npc.ai[0])
                {
                    case DoGSignusDrawState.IdleFly:
                        NPCTexture = Main.npcTexture[npc.type];
                        glowMaskTexture = ModContent.GetTexture("CalamityMod/NPCs/Signus/SignusGlow");
                        break;
                    case DoGSignusDrawState.AggressiveFly:
                        NPCTexture = ModContent.GetTexture("CalamityMod/NPCs/Signus/SignusAlt");
                        glowMaskTexture = ModContent.GetTexture("CalamityMod/NPCs/Signus/SignusAltGlow");
                        break;
                    case DoGSignusDrawState.Charge:
                        NPCTexture = ModContent.GetTexture("CalamityMod/NPCs/Signus/SignusAlt2");
                        glowMaskTexture = ModContent.GetTexture("CalamityMod/NPCs/Signus/SignusAlt2Glow");
                        int frameY = 94 * (int)(npc.frameCounter / 12.0);
                        if (frameY >= 94 * 6)
                            frameY = 0;
                        frame = new Rectangle(0, frameY, NPCTexture.Width, NPCTexture.Height / frameCount);
                        break;
                    default: goto case DoGSignusDrawState.IdleFly;
                }

                Vector2 origin = new Vector2(NPCTexture.Width / 2, NPCTexture.Height / frameCount / 2);
                float scale = npc.scale;
                float rotation = npc.rotation;
                float offsetY = npc.gfxOffY;

                Color baseColor = drawColor;
                if (Math.Abs(npc.Center.X - position.X) > 2f)
                    baseColor *= 0.75f;

                Vector2 drawPosition = position - Main.screenPosition;
                drawPosition -= new Vector2(NPCTexture.Width, NPCTexture.Height / frameCount) * scale / 2f;
                drawPosition += origin * scale + new Vector2(0f, 4f + offsetY);
                spriteBatch.Draw(NPCTexture, drawPosition, frame, npc.GetAlpha(baseColor), rotation, origin, scale, spriteEffects, 0f);

                spriteBatch.Draw(glowMaskTexture, drawPosition, frame, Color.Lerp(Color.White, Color.Fuchsia, 0.5f) * npc.localAI[0], rotation, origin, scale, spriteEffects, 0f);
            }

            float attackTimer = npc.ai[2];
            DoGSignusAIState aiState = (DoGSignusAIState)(int)npc.ai[1];
            if (aiState == DoGSignusAIState.CloneKnives && attackTimer > 5 && attackTimer <= CloneAppearTime + 5f)
            {
                foreach (var drawPoint in GetSignusDrawPoints(npc))
                {
                    drawSignus(npc.spriteDirection, drawPoint);
                }
                return false;
            }

            drawSignus(npc.spriteDirection, npc.Center);
            return false;
        }
    }
}
