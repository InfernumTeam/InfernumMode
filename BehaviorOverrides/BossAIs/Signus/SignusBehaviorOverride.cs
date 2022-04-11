using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

using SignusBoss = CalamityMod.NPCs.Signus.Signus;
using Terraria.Audio;
using Terraria.GameContent;

namespace InfernumMode.BehaviorOverrides.BossAIs.Signus
{
    public class SignusBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<SignusBoss>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        #region Enumerations
        public enum SignusAttackType
        {
            KunaiDashes,
            ScytheTeleportThrow,
            ShadowDash,
            FastHorizontalCharge,
            CosmicFlameChargeBelch,
            SummonEntities
        }
        #endregion

        #region AI

        public const float Phase2LifeRatio = 0.7f;
        public const float Phase3LifeRatio = 0.3f;

        public override bool PreAI(NPC npc)
        {
            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Immediately vanish if the target is gone.
            if (!target.active || target.dead)
            {
                npc.active = false;
                return false;
            }

            // Set the whoAmI index.
            CalamityGlobalNPC.signus = npc.whoAmI;

            npc.damage = npc.defDamage;
            npc.dontTakeDamage = false;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float attackState = ref npc.ai[1];
            ref float attackTimer = ref npc.ai[2];
            ref float fadeToBlack = ref npc.Infernum().ExtraAI[9];
            ref float attackDelay = ref npc.Infernum().ExtraAI[8];

            if (attackDelay < 70f)
            {
                attackDelay++;
                npc.Opacity = Utils.GetLerpValue(0f, 30f, attackDelay, true);
                return false;
            }

            // Regularly fade in.
            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.2f, 0f, 1f);

            switch ((SignusAttackType)(int)attackState)
            {
                case SignusAttackType.KunaiDashes:
                    DoAttack_KunaiDashes(npc, target, lifeRatio, ref attackTimer);
                    npc.ai[0] = 0f;
                    break;
                case SignusAttackType.ScytheTeleportThrow:
                    DoAttack_ScytheTeleportThrow(npc, target, lifeRatio, ref attackTimer);
                    npc.ai[0] = 0f;
                    break;
                case SignusAttackType.ShadowDash:
                    DoAttack_ShadowDash(npc, target, lifeRatio, ref attackTimer, ref fadeToBlack);
                    npc.ai[0] = 0f;
                    break;
                case SignusAttackType.FastHorizontalCharge:
                    DoAttack_FastHorizontalCharge(npc, target, lifeRatio, ref attackTimer);
                    npc.ai[0] = 0f;
                    break;
                case SignusAttackType.CosmicFlameChargeBelch:
                    DoAttack_CosmicFlameChargeBelch(npc, target, lifeRatio, ref attackTimer);
                    break;
                case SignusAttackType.SummonEntities:
                    DoAttack_SummonEntities(npc, target, lifeRatio, ref attackTimer);
                    npc.ai[0] = 3f;
                    break;
            }

            attackTimer++;
            return false;
        }

        public static void DoAttack_KunaiDashes(NPC npc, Player target, float lifeRatio, ref float attackTimer)
        {
            int fadeInTime = 12;
            int riseTime = 25;
            int chargeTime = 32;
            int knifeReleaseRate = 6;
            int fadeOutTime = 25;
            int chargeCount = 3;

            if (lifeRatio < Phase2LifeRatio)
            {
                chargeTime -= 7;
                knifeReleaseRate -= 2;
            }
            if (lifeRatio < Phase3LifeRatio)
            {
                chargeTime -= 4;
                knifeReleaseRate--;
                chargeCount--;
            }

            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[1];

            // Become invulnerable once sufficiently invisible.
            npc.dontTakeDamage = npc.Opacity < 0.4f;

            switch ((int)attackSubstate)
            {
                // Fade in after an initial teleport.
                case 0:
                    if (attackTimer == 0f)
                    {
                        npc.Center = target.Center + (Main.rand.Next(4) * MathHelper.TwoPi / 4f + MathHelper.PiOver4).ToRotationVector2() * 350f;
                        npc.netUpdate = true;
                    }

                    // And fade in.
                    npc.Opacity = Utils.GetLerpValue(fadeInTime / 2f, fadeInTime, attackTimer, true);
                    if (attackTimer > fadeInTime)
                    {
                        attackSubstate = 1f;
                        attackTimer = 0f;
                        npc.netUpdate = true;
                    }
                    break;

                // Rise upward prior to charging.
                case 1:
                    float riseSpeed = (1f - Utils.GetLerpValue(0f, riseTime, attackTimer - 6f, true)) * 15f;
                    npc.velocity = Vector2.Lerp(npc.velocity, -Vector2.UnitY * riseSpeed, 0.15f);
                    npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                    npc.rotation = npc.velocity.X * 0.02f;

                    // Select a location to teleport near the target.
                    if (attackTimer == riseTime - 10f)
                    {
                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        npc.velocity = Vector2.Zero;
                        npc.netUpdate = true;
                    }

                    if (attackTimer > riseTime)
                    {
                        attackSubstate = 2f;
                        attackTimer = 0f;
                        Vector2 chargeDestination = target.Center + npc.SafeDirectionTo(target.Center) * 400f;
                        npc.velocity = npc.SafeDirectionTo(chargeDestination) * npc.Distance(chargeDestination) / chargeTime;
                        npc.netUpdate = true;
                    }
                    break;

                // Perform movement during the charge.
                case 2:
                    npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
                    npc.rotation = MathHelper.Clamp(npc.velocity.X * 0.01f, -0.45f, 0.45f);

                    // Release redirecting kunai.
                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % knifeReleaseRate == knifeReleaseRate - 1f && attackTimer < chargeTime)
                    {
                        Vector2 knifeVelocity = -Vector2.UnitY * 10f;
                        Utilities.NewProjectileBetter(npc.Center + knifeVelocity * 6f, knifeVelocity, ModContent.ProjectileType<CosmicKunai>(), 250, 0f);
                    }

                    // Fade out after the charge has completed.
                    if (attackTimer > chargeTime)
                    {
                        npc.velocity *= 0.85f;
                        if (npc.velocity.Length() > 50f)
                            npc.velocity = npc.velocity.SafeNormalize(Vector2.Zero) * 50f;

                        npc.Opacity = 1f - Utils.GetLerpValue(chargeTime, chargeTime + fadeOutTime, attackTimer, true);
                        npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                    }

                    if (attackTimer > chargeTime + fadeOutTime)
                    {
                        chargeCounter++;
                        attackSubstate = 0f;
                        attackTimer = 0f;

                        if (chargeCounter >= chargeCount)
                            SelectNewAttack(npc);

                        npc.netUpdate = true;
                    }
                    break;
            }
        }

        public static void DoAttack_ScytheTeleportThrow(NPC npc, Player target, float lifeRatio, ref float attackTimer)
        {
            int totalScythesToCreate = (int)MathHelper.Lerp(13f, 24f, 1f - lifeRatio);
            int chargeSlowdownDelay = (int)MathHelper.Lerp(32f, 48f, 1f - lifeRatio);
            int slowdownTime = 25;
            float scytheSpread = MathHelper.SmoothStep(0.95f, 1.34f, 1f - lifeRatio);
            int attackCycleCount = lifeRatio < Phase3LifeRatio ? 1 : 2;

            if (BossRushEvent.BossRushActive)
                totalScythesToCreate += 7;

            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float attackCycleCounter = ref npc.Infernum().ExtraAI[1];

            switch ((int)attackSubstate)
            {
                // Attempt to hover over the target.
                case 0:
                    Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 375f, -200f);
                    Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 20.5f;
                    npc.velocity = (npc.velocity * 24f + idealVelocity) / 25f;
                    npc.velocity = npc.velocity.MoveTowards(idealVelocity, 0.6f);
                    npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                    npc.rotation = npc.velocity.X * 0.02f;

                    if (attackTimer > 55f || npc.WithinRange(hoverDestination, 90f))
                    {
                        attackTimer = 0f;
                        attackSubstate++;
                        npc.velocity = npc.SafeDirectionTo(target.Center) * 27.5f;
                        npc.netUpdate = true;
                    }
                    break;

                // Charge quickly at the target, slow down, and create a bunch of scythes.
                case 1:
                    if (attackTimer < chargeSlowdownDelay + slowdownTime + 30f)
                        npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
                    if (attackTimer > chargeSlowdownDelay)
                        npc.velocity *= 0.98f;
                    if (attackTimer > chargeSlowdownDelay + slowdownTime)
                        npc.velocity *= 0.9f;

                    npc.rotation = npc.velocity.X * 0.02f;

                    if (attackTimer == chargeSlowdownDelay + slowdownTime + 30f)
                    {
                        // Create a bunch of scythes in front of Signus. The quantity of scythes and their spread is dependant on Signus' life ratio.
                        float baseShootAngle = npc.AngleTo(target.Center);
                        for (int i = 0; i < totalScythesToCreate; i++)
                        {
                            int scythe = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<EldritchScythe>(), 250, 0f);
                            if (Main.projectile.IndexInRange(scythe))
                            {
                                Main.projectile[scythe].ai[0] = (int)MathHelper.Lerp(50f, 10f, i / (float)(totalScythesToCreate - 1f));
                                Main.projectile[scythe].ai[1] = baseShootAngle + MathHelper.Lerp(-scytheSpread, scytheSpread, i / (float)(totalScythesToCreate - 1f));
                            }
                        }

                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        npc.velocity = Vector2.Zero;
                        npc.netUpdate = true;
                    }

                    if (attackTimer > chargeSlowdownDelay + slowdownTime + 85f)
                    {
                        attackTimer = 0f;
                        attackSubstate = 0f;
                        attackCycleCounter++;

                        if (attackCycleCounter >= attackCycleCount)
                            SelectNewAttack(npc);

                        npc.netUpdate = true;
                    }
                    break;
            }
        }

        public static void DoAttack_ShadowDash(NPC npc, Player target, float lifeRatio, ref float attackTimer, ref float fadeToBlack)
        {
            int redirectTime = 20;
            int telegraphTime = 30;
            int blackTime = 85;
            float maxInitialSlashDistance = 350f;
            float slashMovementSpeed = 40f;
            int finalDelay = 130;

            if (lifeRatio < Phase2LifeRatio)
            {
                blackTime += 10;
                maxInitialSlashDistance -= 50f;
                slashMovementSpeed += 4f;
            }
            if (lifeRatio < Phase3LifeRatio)
            {
                blackTime += 5;
                maxInitialSlashDistance -= 15f;
                finalDelay -= 15;
            }

            if (BossRushEvent.BossRushActive)
                slashMovementSpeed += 12f;

            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float chargeHoverCenterX = ref npc.Infernum().ExtraAI[1];
            ref float chargeHoverCenterY = ref npc.Infernum().ExtraAI[2];
            ref float startingCenterX = ref npc.Infernum().ExtraAI[3];
            ref float startingCenterY = ref npc.Infernum().ExtraAI[4];
            ref float slashPositionX = ref npc.Infernum().ExtraAI[5];
            ref float slashPositionY = ref npc.Infernum().ExtraAI[6];

            switch ((int)attackSubstate)
            {
                // Line up and create a telegraph. This is brief.
                case 0:
                    if (chargeHoverCenterX == 0f || chargeHoverCenterY == 0f)
                    {
                        Vector2 hoverDestination = target.Center + Vector2.UnitX.RotatedByRandom(0.62f) * (target.Center.X < npc.Center.X).ToDirectionInt() * 450f;
                        chargeHoverCenterX = hoverDestination.X;
                        chargeHoverCenterY = hoverDestination.Y;
                        startingCenterX = npc.Center.X;
                        startingCenterY = npc.Center.Y;
                        npc.netUpdate = true;
                        return;
                    }

                    // Move to the hover position and become moderately faded.
                    npc.Center = Vector2.Lerp(new Vector2(startingCenterX, startingCenterY), new Vector2(chargeHoverCenterX, chargeHoverCenterY), attackTimer / redirectTime);
                    npc.spriteDirection = (chargeHoverCenterX > npc.Center.X).ToDirectionInt();
                    npc.Opacity = MathHelper.Lerp(npc.Opacity, 0.5f, 0.2f);
                    npc.velocity = Vector2.Zero;

                    // Look at the player and create the telegraph line after the redirect is over.
                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= redirectTime)
                    {
                        Vector2 chargeDestination = target.Center;
                        float telegraphDirection = npc.AngleTo(chargeDestination);
                        int telegraph = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<ShadowDashTelegraph>(), 0, 0f);
                        if (Main.projectile.IndexInRange(telegraph))
                        {
                            Main.projectile[telegraph].ai[0] = telegraphTime;
                            Main.projectile[telegraph].ai[1] = telegraphDirection;
                        }

                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        attackSubstate = 1f;
                        attackTimer = 0f;
                        npc.netUpdate = true;
                    }
                    break;

                // Cause the entire screen to melt into black, slash violently in an attempt to kill the target, and then release a bomb that explodes
                // into kunai after the black screen fade effect is over.
                case 1:
                    fadeToBlack = Utils.GetLerpValue(0f, telegraphTime, attackTimer, true) * Utils.GetLerpValue(telegraphTime + blackTime + 12f, telegraphTime + blackTime, attackTimer, true);

                    // Become invincible once the black screen fade is noticeably strong.
                    npc.dontTakeDamage = fadeToBlack > 0.7f;

                    // Drift towards the target very quickly.
                    if (attackTimer == 1f)
                        npc.velocity = npc.SafeDirectionTo(target.Center);

                    // Speed up after the initial charge has happened. This does not apply once the black screen fade has concluded.
                    if (attackTimer < telegraphTime + blackTime)
                    {
                        float chargeSpeed = MathHelper.Lerp(1f, 32f, (float)Math.Pow(Utils.GetLerpValue(0f, telegraphTime, attackTimer, true), 2D));
                        npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * chargeSpeed;
                    }

                    // Slow down if the black screen fade effect is over.
                    else
                        npc.velocity *= 0.97f;

                    // Don't do damage after the telegraph is gone.
                    if (attackTimer > telegraphTime)
                        npc.damage = 0;

                    // Create various slashes that attempt to approach the target.
                    if (attackTimer > telegraphTime && attackTimer < telegraphTime + blackTime - 3f && attackTimer % 3f == 2f)
                    {
                        // Play a sound.
                        SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Custom/LightningStrike"), target.Center);

                        // Define a starting point if one has yet to be selected for the slashes.
                        // It attempts to start at Signus' position, but will not start too far off from the target.
                        if (slashPositionX == 0f || slashPositionY == 0f)
                        {
                            Vector2 startingPosition = npc.Center;

                            // Ensure that the starting position is never too far away from the target.
                            if (!target.WithinRange(startingPosition, maxInitialSlashDistance))
                                startingPosition = target.Center + (startingPosition - target.Center).SafeNormalize(Vector2.UnitY) * maxInitialSlashDistance;

                            slashPositionX = startingPosition.X;
                            slashPositionY = startingPosition.Y;
                            npc.netUpdate = true;
                        }

                        Vector2 slashPosition = new(slashPositionX, slashPositionY);
                        int slash = Utilities.NewProjectileBetter(slashPosition + Main.rand.NextVector2Circular(30f, 30f), Vector2.Zero, ModContent.ProjectileType<ShadowSlash>(), 250, 0f);
                        if (Main.projectile.IndexInRange(slash))
                            Main.projectile[slash].ai[0] = Main.rand.NextFloat(MathHelper.TwoPi);

                        // Make the slashes move.
                        slashPosition = slashPosition.MoveTowards(target.Center, slashMovementSpeed);
                        slashPositionX = slashPosition.X;
                        slashPositionY = slashPosition.Y;
                    }

                    // Teleport in front of the target and create a mine between Signus and them.
                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == telegraphTime + blackTime - 1f)
                    {
                        npc.Center = target.Center + (target.Center - new Vector2(slashPositionX, slashPositionY)).SafeNormalize(Main.rand.NextVector2Unit()) * 450f;

                        // Retain a little bit of movement to add to the atmosphere. This is quickly slowed down in above code.
                        npc.velocity = npc.SafeDirectionTo(target.Center) * -18f;
                        npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                        npc.netUpdate = true;

                        Vector2 bombPosition = (target.Center + npc.Center) * 0.5f;
                        Utilities.NewProjectileBetter(bombPosition, Vector2.Zero, ModContent.ProjectileType<CosmicMine>(), 0, 0f);
                    }

                    // Determine rotation based on horizontal movement.
                    npc.rotation = npc.velocity.X * 0.02f;

                    // Transition to the next attack after a small delay.
                    if (attackTimer == telegraphTime + blackTime + finalDelay)
                        SelectNewAttack(npc);
                    break;
            }
        }

        public static void DoAttack_FastHorizontalCharge(NPC npc, Player target, float lifeRatio, ref float attackTimer)
        {
            int chargeCount = 3;
            int hoverTime = 75;
            int chargeTime = 45;
            float chargeSpeed = 45f;
            int slowdownTime = 20;
            int kunaiCount = 11;

            if (lifeRatio < Phase2LifeRatio)
            {
                hoverTime -= 8;
                chargeTime -= 3;
                chargeSpeed += 7.5f;
            }
            if (lifeRatio < Phase3LifeRatio)
            {
                chargeCount--;
                chargeTime -= 3;
                chargeSpeed += 3f;
                slowdownTime -= 4;
            }

            if (BossRushEvent.BossRushActive)
                kunaiCount = 19;

            int totalChargeTime = hoverTime + chargeTime + slowdownTime;
            float wrappedAttackTimer = attackTimer % totalChargeTime;

            ref float chargeDirection = ref npc.Infernum().ExtraAI[0];
            ref float attackSubstate = ref npc.Infernum().ExtraAI[1];

            // Determine a new charge direction as necessary on the first frame.
            if (wrappedAttackTimer == 1f)
            {
                chargeDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                npc.netUpdate = true;
            }

            // Briefly attempt to hover next to the target.
            if (wrappedAttackTimer <= hoverTime)
            {
                Vector2 hoverDestination = target.Center + new Vector2(chargeDirection * -560f, 0f);
                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 22f;
                idealVelocity = idealVelocity.ClampMagnitude(0f, npc.Distance(hoverDestination));
                npc.velocity = npc.velocity.MoveTowards(idealVelocity, 5f);

                // Look at the target.
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

                // Charge early if close enough to the destination.
                if (npc.WithinRange(hoverDestination, 20f))
                {
                    npc.Center = hoverDestination;
                    attackTimer += hoverTime - wrappedAttackTimer;
                    wrappedAttackTimer = attackTimer % totalChargeTime;
                    npc.netUpdate = true;
                }

                // Slow down drastically on the frame the hovering should end.
                // Also shoot kunai.
                if (wrappedAttackTimer == hoverTime)
                {
                    npc.velocity *= 0.25f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < kunaiCount; i++)
                        {
                            float offsetAngle = MathHelper.Lerp(-1.13f, 1.13f, i / (float)(kunaiCount - 1f));
                            Vector2 shootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(offsetAngle) * 35f;
                            Utilities.NewProjectileBetter(npc.Center + shootVelocity, shootVelocity, ModContent.ProjectileType<CosmicKunai>(), 250, 0f);
                        }
                    }
                }
            }

            // Lerp to horizontal movement.
            // This is similar to the charges the Empress of Light uses.
            else if (wrappedAttackTimer <= hoverTime + chargeTime)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitX * chargeDirection * chargeSpeed, 0.1f);
                if (wrappedAttackTimer == hoverTime + chargeTime)
                    npc.velocity *= 0.7f;
            }

            // Slow down after the charge should end.
            else
                npc.velocity *= 0.84f;

            // Determine rotation.
            npc.rotation = MathHelper.Clamp(npc.velocity.X * 0.01f, -0.45f, 0.45f);

            if (attackTimer > totalChargeTime * chargeCount)
                SelectNewAttack(npc);
        }

        public static void DoAttack_CosmicFlameChargeBelch(NPC npc, Player target, float lifeRatio, ref float attackTimer)
        {
            float chargeSpeed = MathHelper.Lerp(25f, 31f, 1f - lifeRatio);
            int spinDelay = 35;
            float spinSpeed = MathHelper.TwoPi / MathHelper.Lerp(100f, 50f, 1f - lifeRatio);
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];

            switch ((int)attackSubstate)
            {
                // Rise upwards a bit prior to charging.
                case 0:
                    Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 500f, -370f);
                    npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(hoverDestination) * 23f, 0.04f);
                    npc.ai[0] = 0f;

                    if (npc.WithinRange(hoverDestination, 25f) || attackTimer > 120f)
                    {
                        attackTimer = 0f;
                        attackSubstate = 1f;
                        npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                        npc.netUpdate = true;
                    }
                    break;

                // Do a spin charge and belch cosmic flames from the mouth.
                case 1:
                    // Start to rotate during the charge after enough time has passed.
                    if (attackTimer > spinDelay)
                        npc.velocity = npc.velocity.RotatedBy(spinSpeed);
                    npc.ai[0] = 4f;
                    npc.rotation = npc.velocity.ToRotation();

                    // Stop spinning once Signus is within the target's line of sight and enough time has passed.
                    bool inLineOfSightOfTarget = npc.velocity.AngleBetween(npc.SafeDirectionTo(target.Center)) < 0.26f;
                    if (attackTimer > spinDelay + 100f || (inLineOfSightOfTarget && attackTimer > spinDelay + 50f))
                    {
                        attackTimer = 0f;
                        attackSubstate = 2f;
                        npc.netUpdate = true;
                    }
                    break;

                // Charge at the target and release fireballs.
                case 2:
                    if (attackTimer % 8f == 7f && !npc.WithinRange(target.Center, 300f) && attackTimer < 30f)
                    {
                        SoundEngine.PlaySound(SoundID.Item73, npc.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                float offsetAngle = MathHelper.Lerp(-0.6f, 0.6f, i / 2f);
                                Vector2 shootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(offsetAngle) * Main.rand.NextFloat(8f, 12f);
                                Utilities.NewProjectileBetter(npc.Center + shootVelocity * 3f, shootVelocity, ModContent.ProjectileType<CosmicFlame>(), 260, 0f);
                            }
                        }
                    }

                    if (attackTimer > 70f)
                        SelectNewAttack(npc);
                    break;
            }
        }

        public static void DoAttack_SummonEntities(NPC npc, Player target, float lifeRatio, ref float attackTimer)
        {
            int totalEntitiesToSummon = (int)MathHelper.SmoothStep(4f, 8f, 1f - lifeRatio);
            int entitySummonRate = (int)MathHelper.Lerp(10f, 15f, 1f - lifeRatio);
            ref float entitySummonCounter = ref npc.Infernum().ExtraAI[0];

            // Slow down at first and appear above the target.
            if (attackTimer < 90f)
            {
                npc.velocity *= 0.95f;
                npc.rotation = npc.velocity.X * 0.02f;
                npc.Opacity = Utils.GetLerpValue(0f, 35f, attackTimer, true);
                if (attackTimer == 1f)
                    npc.Center = target.Center - Vector2.UnitY * 350f;
                return;
            }

            // Look at the target.
            npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

            // And create entities.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % entitySummonRate == entitySummonRate - 1f)
            {
                Vector2 entitySpawnPosition = npc.Center + Main.rand.NextVector2Circular(250f, 250f);
                NPC.NewNPC((int)entitySpawnPosition.X, (int)entitySpawnPosition.Y, ModContent.NPCType<UnworldlyEntity>(), npc.whoAmI);

                entitySummonCounter++;
                npc.netUpdate = true;
            }

            if (entitySummonCounter > totalEntitiesToSummon)
                SelectNewAttack(npc);
        }

        public static void SelectNewAttack(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            for (int i = 0; i < 8; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            ref float attackState = ref npc.ai[1];
            float oldAttackState = npc.ai[1];

            WeightedRandom<SignusAttackType> newStatePicker = new(Main.rand);
            newStatePicker.Add(SignusAttackType.KunaiDashes);
            newStatePicker.Add(SignusAttackType.ScytheTeleportThrow);
            if (!NPC.AnyNPCs(ModContent.NPCType<UnworldlyEntity>()))
                newStatePicker.Add(SignusAttackType.ShadowDash, lifeRatio < Phase2LifeRatio ? 1.6 : 1D);
            newStatePicker.Add(SignusAttackType.FastHorizontalCharge);

            if (lifeRatio < Phase2LifeRatio)
            {
                newStatePicker.Add(SignusAttackType.CosmicFlameChargeBelch, 1.85);
                newStatePicker.Add(SignusAttackType.SummonEntities, 1.85);
            }

            do
                attackState = (int)newStatePicker.Get();
            while (attackState == oldAttackState);

            npc.TargetClosest();
            npc.ai[2] = 0f;
            npc.netUpdate = true;
        }
        #endregion AI

        #region Drawing
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            void drawInstance(Vector2 baseDrawPosition, bool canDrawAfterimages, SpriteEffects direction)
            {
                Texture2D NPCTexture;
                Texture2D glowMaskTexture;

                int afterimageCount = 5;
                Rectangle frame = npc.frame;
                int frameCount = Main.npcFrameCount[npc.type];

                if (npc.ai[0] == 4f)
                {
                    NPCTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/Signus/SignusAlt2").Value;
                    glowMaskTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/Signus/SignusAlt2Glow").Value;
                    afterimageCount = 10;
                    int frameY = 94 * (int)(npc.frameCounter / 12.0);
                    if (frameY >= 94 * 6)
                        frameY = 0;
                    frame = new Rectangle(0, frameY, NPCTexture.Width, NPCTexture.Height / frameCount);
                }
                else if (npc.ai[0] == 3f)
                {
                    NPCTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/Signus/SignusAlt").Value;
                    glowMaskTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/Signus/SignusAltGlow").Value;
                    afterimageCount = 7;
                }
                else
                {
                    NPCTexture = TextureAssets.Npc[npc.type].Value;
                    glowMaskTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/Signus/SignusGlow").Value;
                }

                Vector2 origin = new(NPCTexture.Width / 2, NPCTexture.Height / frameCount / 2);
                float scale = npc.scale;
                float rotation = npc.rotation * canDrawAfterimages.ToDirectionInt();
                float offsetY = npc.gfxOffY;

                if (canDrawAfterimages && CalamityConfig.Instance.Afterimages)
                {
                    for (int i = 1; i < afterimageCount; i += 2)
                    {
                        Color afterimageColor = lightColor;
                        afterimageColor = Color.Lerp(afterimageColor, Color.White, 0.5f);
                        afterimageColor = npc.GetAlpha(afterimageColor);
                        afterimageColor *= (afterimageCount - i) / 15f;
                        Vector2 afterimageDrawPosition = npc.oldPos[i] + new Vector2(npc.width, npc.height) / 2f - Main.screenPosition;
                        afterimageDrawPosition -= new Vector2(NPCTexture.Width, NPCTexture.Height / frameCount) * scale / 2f;
                        afterimageDrawPosition += origin * scale + new Vector2(0f, 4f + offsetY);
                        spriteBatch.Draw(NPCTexture, afterimageDrawPosition, new Rectangle?(frame), afterimageColor, rotation, origin, scale, direction, 0f);
                    }
                }

                Vector2 drawPosition = baseDrawPosition - Main.screenPosition;
                drawPosition -= new Vector2(NPCTexture.Width, NPCTexture.Height / frameCount) * scale / 2f;
                drawPosition += origin * scale + new Vector2(0f, 4f + offsetY);
                spriteBatch.Draw(NPCTexture, drawPosition, new Rectangle?(frame), npc.GetAlpha(lightColor), rotation, origin, scale, direction, 0f);

                Color glowmaskColor = Color.Lerp(Color.White, Color.Fuchsia, 0.5f);

                if (canDrawAfterimages && CalamityConfig.Instance.Afterimages)
                {
                    for (int i = 1; i < afterimageCount; i++)
                    {
                        Color afterimageColor = glowmaskColor;
                        afterimageColor = Color.Lerp(afterimageColor, Color.White, 0.5f);
                        afterimageColor = npc.GetAlpha(afterimageColor);
                        afterimageColor *= (afterimageCount - i) / 15f;
                        Vector2 afterimageDrawPosition = npc.oldPos[i] + new Vector2(npc.width, npc.height) / 2f - Main.screenPosition;
                        afterimageDrawPosition -= new Vector2(glowMaskTexture.Width, glowMaskTexture.Height / frameCount) * scale / 2f;
                        afterimageDrawPosition += origin * scale + new Vector2(0f, 4f + offsetY);
                        spriteBatch.Draw(glowMaskTexture, afterimageDrawPosition, new Rectangle?(frame), afterimageColor, rotation, origin, scale, direction, 0f);
                    }
                }

                spriteBatch.Draw(glowMaskTexture, drawPosition, new Rectangle?(frame), glowmaskColor, rotation, origin, scale, direction, 0f);
            }

            Player target = Main.player[npc.target];
            drawInstance(npc.Center, true, npc.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None);

            Vector2 cloneDrawPosition = new(target.Center.X, npc.Center.Y);
            cloneDrawPosition.X += target.Center.X - npc.Center.X;
            float lifeRatio = npc.life / (float)npc.lifeMax;

            if (lifeRatio < Phase2LifeRatio)
                drawInstance(cloneDrawPosition, false, npc.Center.X > target.Center.X ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
            return false;
        }
        #endregion
    }
}
