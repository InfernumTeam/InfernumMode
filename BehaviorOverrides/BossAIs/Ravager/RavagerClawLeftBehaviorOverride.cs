using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Ravager;
using InfernumMode.Dusts;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Ravager
{
    public class RavagerClawLeftBehaviorOverride : NPCBehaviorOverride
    {
        public enum RavagerClawAttackState
        {
            StickToBody,
            Punch,
            Hover,
            AccelerationPunch,
            SlamIntoGround
        }

        public override int NPCOverrideType => ModContent.NPCType<RavagerClawLeft>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public override bool PreAI(NPC npc) => DoClawAI(npc, true);

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor) => DrawClaw(npc, spriteBatch, lightColor, true);

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
            float punchSpeed = 23.5f;
            Vector2 stickPosition = ravagerBody.Center + new Vector2(-120f * leftClaw.ToDirectionInt(), 50f);

            if (BossRushEvent.BossRushActive)
            {
                punchSpeed *= 1.45f;
                reelbackSpeed *= 1.7f;
            }

            bool shouldSlamIntoGround = ravagerBody.Infernum().ExtraAI[7] == 1f;
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
            npc.damage = npc.defDamage - 15;
            if (attackState < 2 && free)
                attackState = (int)RavagerClawAttackState.Hover;

            // Don't attack if the Ravager isn't ready to do so yet.
            if (!free)
                npc.dontTakeDamage = false;

            // Stay attach to Ravager when it isn't ready to attack yet.
            if (ravagerBody.Infernum().ExtraAI[5] < RavagerBodyBehaviorOverride.AttackDelay)
            {
                npc.damage = 0;
                npc.dontTakeDamage = true;
                attackState = (int)RavagerClawAttackState.StickToBody;
                punchTimer = 0f;
            }

            else if (npc.dontTakeDamage)
                npc.life = 1;

            // Stay attached to the Ravager unless it explicitly states that arms can punch in the form of an ExtraAI value.
            else if (ravagerBody.Infernum().ExtraAI[6] == 0f)
            {
                npc.damage = 0;
                attackState = (int)RavagerClawAttackState.StickToBody;
                punchTimer = 0f;
            }

            // Slam into the ground if necessary.
            if (shouldSlamIntoGround)
            {
                npc.damage = 0;
                attackState = (int)RavagerClawAttackState.SlamIntoGround;
                punchTimer = 0f;
            }

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

                    float reelbackDistance = BossRushEvent.BossRushActive ? 1450f : 700f;
                    if (!npc.WithinRange(stickPosition, reelbackDistance) || npc.collideX || npc.collideY || npc.justHit)
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

                    // Emit magic as a telegraph to signal that a punch will happen soon.
                    if (punchTimer >= 165f)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            Dust darkMagicFire = Dust.NewDustPerfect(npc.Center + npc.rotation.ToRotationVector2() * 28f, ModContent.DustType<RavagerMagicDust>());
                            darkMagicFire.velocity = (npc.rotation + Main.rand.NextFloat(-0.56f, 0.56f)).ToRotationVector2() * Main.rand.NextFloat(2f, 4f);
                            darkMagicFire.position += Main.rand.NextVector2Circular(3f, 3f);
                            darkMagicFire.scale = 1.45f;
                            darkMagicFire.noGravity = true;
                        }
                    }

                    if (punchTimer >= 240f)
                    {
                        punchTimer = 0f;
                        attackState = (int)RavagerClawAttackState.AccelerationPunch;
                        npc.velocity = npc.SafeDirectionTo(target.Center) * punchSpeed / 1.67f;
                        npc.netUpdate = true;
                    }

                    punchTimer++;
                    break;
                case RavagerClawAttackState.AccelerationPunch:
                    npc.damage = npc.defDamage + 30;

                    // Emit dust.
                    if (punchTimer % 12f == 11f)
                    {
                        for (int i = 0; i < 18; i++)
                        {
                            Vector2 ringOffset = Vector2.UnitX * -npc.width / 2f - Vector2.UnitY.RotatedBy(MathHelper.TwoPi * i / 18f) * new Vector2(8f, 16f);
                            ringOffset = ringOffset.RotatedBy(npc.rotation);
                            Dust darkMagicFire = Dust.NewDustDirect(npc.Center, 0, 0, ModContent.DustType<RavagerMagicDust>(), 0f, 0f, 160, default, 1f);
                            darkMagicFire.scale = 1.35f;
                            darkMagicFire.fadeIn = 1.4f;
                            darkMagicFire.noGravity = true;
                            darkMagicFire.position = npc.Center + ringOffset;
                            darkMagicFire.velocity = npc.velocity * 0.1f;
                            darkMagicFire.velocity = Vector2.Normalize(npc.Center - npc.velocity * 3f - darkMagicFire.position) * 1.25f;
                        }
                    }
                    if (Main.rand.NextBool(4))
                    {
                        Vector2 spawnOffset = -Vector2.UnitX.RotatedBy(npc.velocity.ToRotation() + Main.rand.NextFloatDirection() * MathHelper.Pi / 16f) * npc.width * 0.5f;
                        Dust smoke = Dust.NewDustDirect(npc.position, npc.width, npc.height, 31, 0f, 0f, 100, default, 1f);
                        smoke.position = npc.Center + spawnOffset;
                        smoke.velocity *= 0.1f;
                        smoke.fadeIn = 0.9f;
                    }
                    if (Main.rand.NextBool(32))
                    {
                        Vector2 spawnOffset = -Vector2.UnitX.RotatedBy(npc.velocity.ToRotation() + Main.rand.NextFloatDirection() * MathHelper.Pi / 8f) * npc.width * 0.5f;
                        Dust smoke = Dust.NewDustDirect(npc.position, npc.width, npc.height, 31, 0f, 0f, 155, default, 0.8f);
                        smoke.velocity *= 0.3f;
                        smoke.position = npc.Center + spawnOffset;
                        if (Main.rand.NextBool(2))
                            smoke.fadeIn = 1.4f;
                    }
                    if (Main.rand.NextBool(2))
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            Vector2 spawnOffset = -Vector2.UnitX.RotatedBy(npc.velocity.ToRotation() + Main.rand.NextFloatDirection() * MathHelper.PiOver4) * npc.width * 0.5f;
                            Dust darkMagicFire = Dust.NewDustDirect(npc.position, npc.width, npc.height, ModContent.DustType<RavagerMagicDust>(), 0f, 0f, 0, default, 1.45f);
                            darkMagicFire.velocity *= 0.3f;
                            darkMagicFire.position = npc.Center + spawnOffset;
                            darkMagicFire.noGravity = true;
                            if (Main.rand.NextBool(2))
                                darkMagicFire.fadeIn = 1.75f;
                        }
                    }

                    if (punchTimer >= 45f)
                    {
                        punchTimer = 0f;
                        attackState = (int)RavagerClawAttackState.Hover;
                        npc.velocity *= 0.5f;
                        npc.netUpdate = true;
                    }
                    npc.velocity *= 1.014f;
                    npc.rotation = npc.velocity.ToRotation();

                    punchTimer++;
                    break;
                case RavagerClawAttackState.SlamIntoGround:
                    npc.velocity = ((npc.Center - ravagerBody.Center) * new Vector2(0.12f, 1f)).SafeNormalize(Vector2.UnitY) * 48f;
                    npc.rotation = npc.velocity.ToRotation();
                    if (Collision.SolidCollision(npc.TopLeft, npc.width, npc.height))
                        npc.velocity = Vector2.Zero;
                    break;
            }

            // Inherit HP from the NPC that has the actual HP pool if applicable.
            if (npc.realLife >= 0 && (Main.npc[npc.realLife].type == ModContent.NPCType<RavagerClawLeft>() || Main.npc[npc.realLife].type == ModContent.NPCType<RavagerClawRight>()))
            {
                if (!Main.npc[npc.realLife].active)
                    npc.active = false;
                npc.life = Main.npc[npc.realLife].life;
                npc.lifeMax = Main.npc[npc.realLife].lifeMax;
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
