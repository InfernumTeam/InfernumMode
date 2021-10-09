using CalamityMod.NPCs;
using CalamityMod.NPCs.StormWeaver;
using InfernumMode.BehaviorOverrides.BossAIs.AquaticScourge;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

using SignusBoss = CalamityMod.NPCs.Signus.Signus;

namespace InfernumMode.BehaviorOverrides.BossAIs.Signus
{
    public class SignusBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<SignusBoss>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        #region Enumerations
        public enum SignusAttackType
        {
            KunaiDashes,
            ScytheTeleportThrow,
            ShadowDash
        }
        #endregion

        #region AI

        public const float Phase2LifeRatio = 0.7f;

        public override bool PreAI(NPC npc)
        {
            // Do targeting.
            npc.TargetClosest();
            Player target = Main.player[npc.target];

            // Immediately vanish if the target is gone.
            if (!target.active || target.dead)
            {
                npc.active = false;
                return false;
            }

            // Set the whoAmI index.
            CalamityGlobalNPC.signus = npc.whoAmI;

            // Regularly fade in.
            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.2f, 0f, 1f);
            npc.damage = npc.defDamage;
            npc.dontTakeDamage = false;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float attackState = ref npc.ai[1];
            ref float attackTimer = ref npc.ai[2];
            ref float fadeToBlack = ref npc.ai[3];

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
                    break;
            }

            attackTimer++;
            return false;
        }

        public static void DoAttack_KunaiDashes(NPC npc, Player target, float lifeRatio, ref float attackTimer)
        {
            int fadeInTime = 12;
            int riseTime = 25;
            int chargeTime = lifeRatio < Phase2LifeRatio ? 25 : 32;
            int knifeReleaseRate = lifeRatio < Phase2LifeRatio ? 4 : 6;
            int fadeOutTime = 25;
            int chargeCount = 3;
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
                    npc.Opacity = Utils.InverseLerp(fadeInTime / 2f, fadeInTime, attackTimer, true);
                    if (attackTimer > fadeInTime)
                    {
                        attackSubstate = 1f;
                        attackTimer = 0f;
                        npc.netUpdate = true;
                    }
                    break;

                // Rise upward prior to charging.
                case 1:
                    float riseSpeed = (1f - Utils.InverseLerp(0f, riseTime, attackTimer - 6f, true)) * 15f;
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

                        npc.Opacity = 1f - Utils.InverseLerp(chargeTime, chargeTime + fadeOutTime, attackTimer, true);
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
            int attackCycleCount = 2;
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
                        float baseShootAngle = npc.AngleTo(target.Center);
                        for (int i = 0; i < totalScythesToCreate; i++)
                        {
                            int scythe = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<EldritchScythe>(), 250, 0f);
                            if (Main.projectile.IndexInRange(scythe))
                            {
                                Main.projectile[scythe].ai[0] = (int)MathHelper.Lerp(50f, 5f, i / (float)(totalScythesToCreate - 1f));
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
            int blackTime = 80;
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float chargeHoverCenterX = ref npc.Infernum().ExtraAI[1];
            ref float chargeHoverCenterY = ref npc.Infernum().ExtraAI[2];
            ref float startingCenterX = ref npc.Infernum().ExtraAI[3];
            ref float startingCenterY = ref npc.Infernum().ExtraAI[4];
            ref float chargeDestinationX = ref npc.Infernum().ExtraAI[5];
            ref float chargeDestinationY = ref npc.Infernum().ExtraAI[6];

            switch ((int)attackSubstate)
            {
                // Line up and create a telegraph. This will be very brief.
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

                    // Move to the hover position and become slightly faded.
                    npc.Center = Vector2.Lerp(new Vector2(startingCenterX, startingCenterY), new Vector2(chargeHoverCenterX, chargeHoverCenterY), attackTimer / redirectTime);
                    npc.spriteDirection = (chargeHoverCenterX > npc.Center.X).ToDirectionInt();
                    npc.Opacity = MathHelper.Lerp(npc.Opacity, 0.5f, 0.2f);
                    npc.velocity = Vector2.Zero;

                    // Look at the player and create the telegraph.
                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= redirectTime)
                    {
                        Vector2 chargeDestination = target.Center;
                        chargeDestinationX = chargeDestination.X;
                        chargeDestinationY = chargeDestination.Y;
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

                // Cause the entire screen to melt into black.
                case 1:
                    npc.ai[3] = Utils.InverseLerp(0f, telegraphTime, attackTimer, true) * Utils.InverseLerp(telegraphTime + blackTime + 12f, telegraphTime + blackTime, attackTimer, true);
                    npc.dontTakeDamage = npc.ai[3] > 0.7f;

                    // Drift towards the target very quickly.
                    if (attackTimer == 1f)
                        npc.velocity = npc.SafeDirectionTo(new Vector2(chargeDestinationX, chargeHoverCenterY));

                    if (attackTimer < telegraphTime + blackTime)
                    {
                        float chargeSpeed = MathHelper.Lerp(1f, 32f, (float)Math.Pow(Utils.InverseLerp(0f, telegraphTime, attackTimer, true), 2D));
                        npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * chargeSpeed;
                    }
                    else
                        npc.velocity *= 0.97f;

                    if (attackTimer > telegraphTime)
                        npc.damage = 0;

                    float initialPositionAngleToTarget = (target.Center - new Vector2(startingCenterX, startingCenterY)).ToRotation();
                    float initialPositionAngleToChargeArea = new Vector2(chargeDestinationX - startingCenterX, chargeDestinationY - startingCenterY).ToRotation();

                    // Create various slashes. These are only visual.
                    if (attackTimer > telegraphTime && attackTimer < telegraphTime + blackTime - 3f && attackTimer % 3f == 2f)
                    {
                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/LightningStrike"), target.Center);

                        Vector2 slashPosition = target.Center + MathHelper.WrapAngle(initialPositionAngleToTarget - initialPositionAngleToChargeArea).ToRotationVector2() * 250f;
                        slashPosition += Main.rand.NextVector2Circular(100f, 100f);
                        slashPosition.X -= 450f;
                        int slash = Utilities.NewProjectileBetter(slashPosition, Vector2.Zero, ModContent.ProjectileType<ShadowSlash>(), 0, 0f);
                        if (Main.projectile.IndexInRange(slash))
                            Main.projectile[slash].ai[0] = Main.rand.NextFloat(MathHelper.TwoPi);
                    }

                    // Teleport in front of the target and create a mine between Signus and the target.
                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == telegraphTime + blackTime - 1f)
                    {
                        npc.Center = target.Center + MathHelper.WrapAngle(initialPositionAngleToTarget - initialPositionAngleToChargeArea).ToRotationVector2() * 350f;
                        npc.velocity = npc.SafeDirectionTo(target.Center) * -12f;
                        npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                        npc.netUpdate = true;

                        Vector2 bombPosition = (target.Center + npc.Center) * 0.5f;
                        Utilities.NewProjectileBetter(bombPosition, Vector2.Zero, ModContent.ProjectileType<CosmicMine>(), 0, 0f);
                    }

                    // Determine rotation based on velocity.
                    npc.rotation = npc.velocity.X * 0.02f;

                    // Transition to the next attack after a small delay.
                    if (attackTimer == telegraphTime + blackTime + 75f)
                        SelectNewAttack(npc);
                    break;
            }
        }

        public static void SelectNewAttack(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 8; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            ref float attackState = ref npc.ai[1];
            float oldAttackState = npc.ai[1];
            WeightedRandom<float> newStatePicker = new WeightedRandom<float>(Main.rand);
            newStatePicker.Add((int)SignusAttackType.KunaiDashes);
            newStatePicker.Add((int)SignusAttackType.ScytheTeleportThrow);
            newStatePicker.Add((int)SignusAttackType.ShadowDash);

            do
                attackState = newStatePicker.Get();
            while (attackState == oldAttackState);

            npc.ai[1] = (int)attackState;
            npc.ai[2] = 0f;
            npc.netUpdate = true;
        }
        #endregion AI
    }
}
