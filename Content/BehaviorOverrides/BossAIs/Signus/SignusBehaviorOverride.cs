using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.UI;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.ScreenEffects;
using InfernumMode.Content.Projectiles.Pets;
using InfernumMode.Core.GlobalInstances;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

using SignusBoss = CalamityMod.NPCs.Signus.Signus;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Signus
{
    public class SignusBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<SignusBoss>();

        #region Enumerations
        public enum SignusAttackType
        {
            Patrol,
            KunaiDashes,
            ScytheTeleportThrow,
            ShadowDash,
            FastHorizontalCharge,
            CosmicFlameChargeBombs
        }
        #endregion

        #region Loading
        public override void Load()
        {
            GlobalNPCOverrides.BossHeadSlotEvent += DisableMapIconWhenInvisible;
        }

        private void DisableMapIconWhenInvisible(NPC npc, ref int index)
        {
            // Make Signus completely invisible on the map when sufficiently faded out.
            if (npc.type == ModContent.NPCType<SignusBoss>() && npc.Opacity < 0.3f)
                index = -1;
        }
        #endregion Loading

        #region AI
        public static int KunaiDamage => 275;

        public static int ScytheDamage => 275;

        public static int ShadowSlashDamage => 300;

        public static int CosmicExplosionDamage => 350;

        public const float Phase2LifeRatio = 0.7f;

        public const float Phase3LifeRatio = 0.3f;

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio,
            Phase3LifeRatio
        };

        public override void SetDefaults(NPC npc)
        {
            // Set defaults that, if were to be changed by Calamity, would cause significant issues to the fight.
            npc.width = 150;
            npc.height = 130;
            npc.scale = 1f;
            npc.defense = 60;
        }

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
            npc.chaseable = true;
            npc.friendly = false;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float attackState = ref npc.ai[1];
            ref float attackTimer = ref npc.ai[2];
            ref float fadeToBlack = ref npc.Infernum().ExtraAI[9];
            ref float attackDelay = ref npc.Infernum().ExtraAI[8];

            if (attackDelay < 25f && attackState != (int)SignusAttackType.Patrol)
            {
                attackDelay++;
                npc.Opacity = Utils.GetLerpValue(0f, 20f, attackDelay, true);
                return false;
            }

            // Regularly fade in.
            npc.Opacity = Clamp(npc.Opacity + 0.2f, 0f, 1f);

            switch ((SignusAttackType)(int)attackState)
            {
                case SignusAttackType.Patrol:
                    DoAttack_Patrol(npc, target, ref attackTimer, ref fadeToBlack);
                    npc.ai[0] = 0f;
                    break;
                case SignusAttackType.KunaiDashes:
                    DoAttack_KunaiDashes(npc, target, lifeRatio, ref attackTimer);
                    npc.boss = true;
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
                case SignusAttackType.CosmicFlameChargeBombs:
                    DoAttack_CosmicFlameChargeBombs(npc, target, lifeRatio, ref attackTimer);
                    break;
            }

            attackTimer++;
            return false;
        }

        public static void DoAttack_Patrol(NPC npc, Player target, ref float attackTimer, ref float fadeToBlack)
        {
            int patrolTime = 1500;
            float patrolDistance = 4800f;
            bool spawnedAtGarden = npc.ai[3] == 1f;
            ref float verticalRepositionDelay = ref npc.Infernum().ExtraAI[0];
            ref float useFreddy = ref npc.Infernum().ExtraAI[10];

            if (BossRushEvent.BossRushActive)
            {
                SelectNextAttack(npc);
                return;
            }

            // Disable boss effects.
            npc.boss = false;
            npc.Calamity().ShouldCloseHPBar = true;
            BossHealthBarManager.Bars.RemoveAll(b => b.NPCIndex == npc.whoAmI);

            // Disable damage.
            npc.damage = 0;
            npc.dontTakeDamage = true;

            // Don't be targetable.
            npc.chaseable = false;
            // Why don't turrets check for don't take damage..
            npc.friendly = true;

            // Prevent hovering over Signus' name to reveal who he is.
            npc.ShowNameOnHover = false;
            npc.Opacity = Utils.GetLerpValue(10f, 120f, verticalRepositionDelay, true) * Utils.GetLerpValue(420f, 390f, verticalRepositionDelay, true) * fadeToBlack * 0.12f;
            npc.spriteDirection = Math.Sign(npc.velocity.X);

            // Teleport to the side of the target.
            if (attackTimer <= 1f)
            {
                if (!spawnedAtGarden)
                {
                    npc.Center = target.Center + Vector2.UnitX * (target.Center.X < Main.maxTilesX * 8f).ToDirectionInt() * patrolDistance * 0.5f;
                    npc.position.Y -= 120f;
                    npc.velocity = npc.SafeDirectionTo(target.Center) * patrolDistance / patrolTime;
                }

                if (Utilities.IsAprilFirst())
                    useFreddy = 1f;

                npc.netUpdate = true;
                verticalRepositionDelay = 180f;
            }

            // Bob up and down for a bit of movement variety.
            npc.velocity.Y = Sin(TwoPi * attackTimer / 300f) * 0.4f;

            verticalRepositionDelay--;
            if (verticalRepositionDelay <= 0f)
            {
                verticalRepositionDelay = spawnedAtGarden ? 390f : 420f;
                if (!npc.WithinRange(target.Center, 1050f))
                    npc.position.Y = target.Center.Y + Main.rand.NextFloatDirection() * 300f;
                npc.netUpdate = true;
            }

            fadeToBlack = Utils.GetLerpValue(patrolDistance * 0.5f, 780f, npc.Distance(target.Center), true);

            // Disappear if Signus is in the gardens and the player gets too close.
            if (spawnedAtGarden && npc.WithinRange(target.Center, 320f))
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.VassalTeleportSound, npc.Center);
                Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 12f;
                ScreenEffectSystem.SetBlurEffect(npc.Center, 1f, 45);

                npc.active = false;
            }

            if (Main.netMode != NetmodeID.Server)
            {
                int ambienceMusicID = MusicLoader.GetMusicSlot(InfernumMode.Instance, "Sounds/Music/SignusAmbience");
                Main.musicFade[MusicID.Hell] = MathF.Min(Main.musicFade[MusicID.Hell], 1f - fadeToBlack);
                Main.musicFade[MusicID.Boss1] = 0f;
                Main.musicFade[CalamityMod.CalamityMod.Instance.GetMusicFromMusicMod("Signus") ?? 0] = 0f;
                Main.musicFade[ambienceMusicID] = fadeToBlack;
            }
            fadeToBlack *= 0.9f;

            if (attackTimer >= patrolTime && !npc.WithinRange(target.Center, patrolDistance * 0.5f + 100f))
            {
                WorldSaveSystem.MetSignusAtProfanedGarden = false;
                npc.active = false;
            }
        }

        public static void DoAttack_KunaiDashes(NPC npc, Player target, float lifeRatio, ref float attackTimer)
        {
            int fadeInTime = 12;
            int riseTime = 25;
            int chargeTime = 25;
            int knifeReleaseRate = 4;
            int fadeOutTime = 25;
            int chargeCount = 3;

            if (lifeRatio < Phase2LifeRatio)
            {
                chargeTime -= 3;
                knifeReleaseRate--;
            }
            if (lifeRatio < Phase3LifeRatio)
            {
                chargeTime -= 4;
                knifeReleaseRate--;
                chargeCount--;
            }

            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[1];
            ref float shootKunaiSlower = ref npc.Infernum().ExtraAI[2];
            ref float chargeDirection = ref npc.Infernum().ExtraAI[3];

            // Become invulnerable once sufficiently invisible.
            npc.dontTakeDamage = npc.Opacity < 0.4f;

            switch ((int)attackSubstate)
            {
                // Fade in after an initial teleport.
                case 0:
                    if (attackTimer == 0f)
                    {
                        npc.Center = target.Center + (Main.rand.Next(4) * TwoPi / 4f + PiOver4).ToRotationVector2() * 350f;
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
                    if (attackTimer == riseTime - 20f)
                    {
                        chargeDirection = npc.AngleTo(target.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<ShadowDashTelegraph>(), 0, 0f, -1, 20f, chargeDirection);

                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        npc.velocity = Vector2.Zero;
                        npc.netUpdate = true;
                    }

                    if (attackTimer > riseTime)
                    {
                        attackSubstate = 2f;
                        attackTimer = 0f;
                        shootKunaiSlower = npc.WithinRange(target.Center, 400f).ToInt();
                        Vector2 chargeDestination = target.Center + chargeDirection.ToRotationVector2() * 400f;
                        npc.velocity = chargeDirection.ToRotationVector2() * npc.Distance(chargeDestination) / chargeTime;
                        npc.netUpdate = true;
                    }
                    break;

                // Perform movement during the charge.
                case 2:
                    npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
                    npc.rotation = Clamp(npc.velocity.X * 0.01f, -0.45f, 0.45f);

                    // Release redirecting kunai.
                    if (shootKunaiSlower == 1f)
                        knifeReleaseRate *= 2;
                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % knifeReleaseRate == knifeReleaseRate - 1f && attackTimer < chargeTime)
                    {
                        Vector2 knifeVelocity = -Vector2.UnitY * 10f;
                        Utilities.NewProjectileBetter(npc.Center + knifeVelocity * 6f, knifeVelocity, ModContent.ProjectileType<CosmicKunai>(), 250, 0f);
                    }

                    // Fade out after the charge has completed.
                    if (attackTimer > chargeTime)
                    {
                        npc.velocity *= 0.85f;
                        if (npc.velocity.Length() > 40f)
                            npc.velocity = npc.velocity.SafeNormalize(Vector2.Zero) * 40f;

                        npc.Opacity = 1f - Utils.GetLerpValue(chargeTime, chargeTime + fadeOutTime, attackTimer, true);
                        npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                    }

                    if (attackTimer > chargeTime + fadeOutTime)
                    {
                        chargeCounter++;
                        attackSubstate = 0f;
                        attackTimer = 0f;
                        shootKunaiSlower = 0f;
                        if (chargeCounter >= chargeCount)
                            SelectNextAttack(npc);

                        npc.netUpdate = true;
                    }
                    break;
            }
        }

        public static void DoAttack_ScytheTeleportThrow(NPC npc, Player target, float lifeRatio, ref float attackTimer)
        {
            int totalScythesToCreate = 25;
            int scytheShootDelay = 31;
            float scytheSpread = SmoothStep(1.51f, 1.67f, 1f - lifeRatio);
            int attackCycleCount = lifeRatio < Phase3LifeRatio ? 2 : 3;

            if (BossRushEvent.BossRushActive)
                totalScythesToCreate += 7;

            // Disable contact damage.
            npc.damage = 0;

            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float attackCycleCounter = ref npc.Infernum().ExtraAI[1];

            switch ((int)attackSubstate)
            {
                // Teleport near the target and fade in.
                case 0:
                    if (attackTimer == 1f)
                    {
                        npc.Center = target.Center + Main.rand.NextVector2CircularEdge(575f, 575f);
                        npc.netUpdate = true;
                    }

                    npc.Opacity = Utils.GetLerpValue(0f, 15f, attackTimer, true);
                    npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                    npc.velocity *= 0.9f;
                    npc.rotation = npc.velocity.X * 0.02f;
                    npc.damage = 0;

                    if (attackTimer >= 15f)
                    {
                        attackTimer = 0f;
                        attackSubstate++;
                        npc.netUpdate = true;
                    }
                    break;

                // Charge quickly at the target, slow down, and create a bunch of scythes.
                case 1:
                    if (attackTimer > scytheShootDelay)
                        npc.velocity *= 0.98f;
                    if (attackTimer > scytheShootDelay)
                        npc.velocity *= 0.9f;

                    npc.rotation = npc.velocity.X * 0.02f;

                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == scytheShootDelay)
                    {
                        // Create a bunch of scythes in front of Signus. The quantity of scythes and their spread is dependant on Signus' life ratio.
                        float baseShootAngle = npc.AngleTo(target.Center);
                        for (int i = 0; i < totalScythesToCreate; i++)
                        {
                            int scytheReleaseDelay = (int)Lerp(70f, 25f, i / (float)(totalScythesToCreate - 1f));
                            float scytheShootAngle = baseShootAngle + Lerp(-scytheSpread, scytheSpread, i / (float)(totalScythesToCreate - 1f));
                            Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<EldritchScythe>(), ScytheDamage, 0f, -1, scytheReleaseDelay, scytheShootAngle);
                        }

                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        npc.velocity = Vector2.Zero;
                        npc.netUpdate = true;
                    }

                    if (attackTimer > scytheShootDelay + 90f)
                    {
                        attackTimer = 0f;
                        attackSubstate = 0f;
                        attackCycleCounter++;

                        if (attackCycleCounter >= attackCycleCount)
                            SelectNextAttack(npc);

                        npc.netUpdate = true;
                    }
                    break;
            }
        }

        public static void DoAttack_ShadowDash(NPC npc, Player target, float lifeRatio, ref float attackTimer, ref float fadeToBlack)
        {
            int redirectTime = 20;
            int telegraphTime = 42;
            int blackTime = 108;
            float maxInitialSlashDistance = 350f;
            float slashMovementSpeed = 41.5f;
            int finalDelay = 130;

            if (lifeRatio < Phase2LifeRatio)
            {
                blackTime -= 10;
                maxInitialSlashDistance -= 50f;
                slashMovementSpeed += 4f;
            }
            if (lifeRatio < Phase3LifeRatio)
            {
                blackTime -= 5;
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

                    // Give a tip.
                    if (attackTimer == 1f)
                        HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.SignusShadowSlashTip");

                    // Move to the hover position and become moderately faded.
                    npc.Center = Vector2.Lerp(new Vector2(startingCenterX, startingCenterY), new Vector2(chargeHoverCenterX, chargeHoverCenterY), attackTimer / redirectTime);
                    npc.spriteDirection = (chargeHoverCenterX > npc.Center.X).ToDirectionInt();
                    npc.Opacity = Lerp(npc.Opacity, 0.5f, 0.2f);
                    npc.velocity = Vector2.Zero;

                    // Look at the player and create the telegraph line after the redirect is over.
                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= redirectTime)
                    {
                        Vector2 chargeDestination = target.Center;
                        float telegraphDirection = npc.AngleTo(chargeDestination);
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<ShadowDashTelegraph>(), 0, 0f, -1, telegraphTime, telegraphDirection);

                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        attackSubstate = 1f;
                        attackTimer = 0f;
                        npc.netUpdate = true;
                    }
                    break;

                // Cause the entire screen to melt into black, slash violently in an attempt to kill the target, and then release a bomb that explodes
                // into kunai after the black screen fade effect is over.
                case 1:
                    fadeToBlack = Utils.GetLerpValue(0f, telegraphTime, attackTimer, true) * Utils.GetLerpValue(telegraphTime + blackTime + 12f, telegraphTime + blackTime, attackTimer, true) * 0.725f;
                    npc.Opacity = 1f - fadeToBlack;

                    // Become invincible once the black screen fade is noticeably strong.
                    npc.dontTakeDamage = fadeToBlack > 0.5f;

                    // Drift towards the target very quickly.
                    if (attackTimer == 1f)
                        npc.velocity = npc.SafeDirectionTo(target.Center);

                    // Speed up after the initial charge has happened. This does not apply once the black screen fade has concluded.
                    if (attackTimer < telegraphTime + blackTime)
                    {
                        float chargeSpeed = Lerp(1f, 32f, Pow(Utils.GetLerpValue(0f, telegraphTime, attackTimer, true), 2f));
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
                        SoundEngine.PlaySound(InfernumSoundRegistry.SignusSlashSound with { Volume = 0.3f, Pitch = 0.4f, MaxInstances = 20 }, target.Center);

                        // Define a starting point if one has yet to be selected for the slashes.
                        // It attempts to start at Signus' position, but will not start too far off from the target.
                        if (slashPositionX == 0f || slashPositionY == 0f)
                        {
                            npc.Center = target.Center + target.velocity.SafeNormalize(Main.rand.NextVector2Unit()) * 1020f;
                            Vector2 startingPosition = npc.Center;

                            // Ensure that the starting position is never too far away from the target.
                            if (!target.WithinRange(startingPosition, maxInitialSlashDistance))
                                startingPosition = target.Center + (startingPosition - target.Center).SafeNormalize(Vector2.UnitY) * maxInitialSlashDistance;

                            slashPositionX = startingPosition.X;
                            slashPositionY = startingPosition.Y;
                            npc.netUpdate = true;
                        }

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 slashPosition = new(slashPositionX, slashPositionY);
                            Utilities.NewProjectileBetter(slashPosition + Main.rand.NextVector2Circular(30f, 30f), Vector2.Zero, ModContent.ProjectileType<ShadowSlash>(), ShadowSlashDamage, 0f, -1, Main.rand.NextFloat(TwoPi));

                            // Make the slashes move.
                            slashPosition = slashPosition.MoveTowards(target.Center, slashMovementSpeed);
                            slashPositionX = slashPosition.X;
                            slashPositionY = slashPosition.Y;

                            npc.netSpam = 0;
                            npc.netUpdate = true;
                        }
                    }

                    // Teleport in front of the target and create a mine between Signus and them.
                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == telegraphTime + blackTime - 1f)
                    {
                        npc.Center = target.Center + (target.Center - new Vector2(slashPositionX, slashPositionY)).SafeNormalize(Main.rand.NextVector2Unit()) * 450f;
                        if (!npc.WithinRange(target.Center, 900f))
                            npc.Center = target.Center - Vector2.UnitY * 500f;

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
                        SelectNextAttack(npc);
                    break;
            }
        }

        public static void DoAttack_FastHorizontalCharge(NPC npc, Player target, float lifeRatio, ref float attackTimer)
        {
            int chargeCount = 3;
            int hoverTime = 75;
            int chargeTime = 42;
            float chargeSpeed = 45f;
            int slowdownTime = 16;
            int kunaiCount = 11;
            float kunaiSpeed = npc.Distance(target.Center) * 0.01f + 35f;

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
                            // Don't create a kunai at the very center, to leave an opening for the player.
                            if (i == 0)
                                continue;

                            float offsetAngle = Lerp(-1.13f, 1.13f, i / (float)(kunaiCount - 1f));
                            Vector2 shootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(offsetAngle) * kunaiSpeed;
                            Vector2 spawnOffset = shootVelocity * 7f;
                            if (npc.WithinRange(target.Center, 450f))
                                spawnOffset *= 0.3f;

                            if (!target.WithinRange(npc.Center + spawnOffset, 275f))
                                Utilities.NewProjectileBetter(npc.Center + spawnOffset, shootVelocity, ModContent.ProjectileType<CosmicKunai>(), 250, 0f);
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
            npc.rotation = Clamp(npc.velocity.X * 0.01f, -0.45f, 0.45f);

            if (attackTimer > totalChargeTime * chargeCount)
                SelectNextAttack(npc);
        }

        public static void DoAttack_CosmicFlameChargeBombs(NPC npc, Player target, float lifeRatio, ref float attackTimer)
        {
            int chargeCount = 2;
            float inertia = 10f;
            float chargeSpeed = Lerp(15f, 21f, 1f - lifeRatio);
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[1];

            switch ((int)attackSubstate)
            {
                // Rise upwards a bit prior to charging.
                case 0:
                    Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 650f, -370f);
                    npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                    npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(hoverDestination) * 23f, 0.04f);
                    npc.ai[0] = 3f;
                    npc.rotation = npc.velocity.X * 0.02f;

                    // Give a tip.
                    if (attackTimer == 1f && chargeCounter <= 0f)
                        HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.SignusBombTip");

                    if (npc.WithinRange(hoverDestination, 45f) || attackTimer > 180f)
                    {
                        attackTimer = 0f;
                        attackSubstate = 1f;
                        npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                        npc.netUpdate = true;
                    }
                    break;

                // Charge at the player.
                case 1:
                    npc.ai[0] = 4f;
                    npc.rotation = npc.velocity.ToRotation();

                    if (Math.Sign(npc.velocity.X) != 0f)
                        npc.spriteDirection = -Math.Sign(npc.velocity.X);

                    if (npc.rotation < -PiOver2)
                        npc.rotation += Pi;
                    if (npc.rotation > PiOver2)
                        npc.rotation -= Pi;

                    npc.spriteDirection = Math.Sign(npc.velocity.X);

                    // Release bombs.
                    bool canReleaseBomb = attackTimer % 12f == 11f && !npc.WithinRange(target.Center, 200f);
                    if (canReleaseBomb)
                    {
                        SoundEngine.PlaySound(InfernumSoundRegistry.SignusFlameBombShootSound with { Volume = 0.45f }, npc.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            Utilities.NewProjectileBetter(npc.Center, npc.velocity * 0.8f, ModContent.ProjectileType<DarkCosmicBomb>(), 0, 0f, -1, 700f);
                    }

                    Vector2 idealFlyDirection = (target.Center - npc.Center).SafeNormalize(Vector2.UnitY);
                    if (!npc.WithinRange(target.Center, 250f))
                    {
                        npc.velocity = npc.velocity.ClampMagnitude(10f, 52.5f);
                        npc.velocity = (npc.velocity * (inertia - 1f) + idealFlyDirection * (npc.velocity.Length() + 0.15f * inertia)) / inertia;
                    }
                    else
                        npc.velocity *= 1.0135f;

                    if (attackTimer >= 300f || target.Center.Y < npc.Center.Y - 200f && attackTimer >= 90f)
                    {
                        attackTimer = 0f;
                        attackSubstate = 0f;
                        npc.velocity = npc.velocity.ClampMagnitude(0f, 24f) * 0.4f;
                        npc.netUpdate = true;
                        chargeCounter++;
                        if (chargeCounter >= chargeCount)
                            SelectNextAttack(npc);
                    }
                    break;
            }
        }

        public static void SelectNextAttack(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            for (int i = 0; i < 8; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            ref float attackState = ref npc.ai[1];
            float oldAttackState = npc.ai[1];

            WeightedRandom<SignusAttackType> attackSelector = new(Main.rand);

            attackSelector.Add(SignusAttackType.KunaiDashes);
            attackSelector.Add(SignusAttackType.ScytheTeleportThrow);
            attackSelector.Add(SignusAttackType.ShadowDash, lifeRatio < Phase2LifeRatio ? 1.6 : 1D);

            attackSelector.Add(SignusAttackType.FastHorizontalCharge);

            if (lifeRatio < Phase2LifeRatio)
                attackSelector.Add(SignusAttackType.CosmicFlameChargeBombs, 1.3);

            do
                attackState = (int)attackSelector.Get();
            while (attackState == oldAttackState);

            Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<CosmicKunai>());

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

                Rectangle frame = npc.frame;
                Rectangle? glowmaskFrame = frame;
                int frameCount = Main.npcFrameCount[npc.type];

                if (npc.ai[0] == 4f)
                {
                    NPCTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/Signus/SignusAlt2").Value;
                    glowMaskTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/Signus/SignusAlt2Glow").Value;
                    int frameY = 94 * (int)(npc.frameCounter / 12.0);
                    if (frameY >= 94 * 6)
                        frameY = 0;
                    frame = new Rectangle(0, frameY, NPCTexture.Width, NPCTexture.Height / frameCount);
                }
                else if (npc.ai[0] == 3f)
                {
                    NPCTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/Signus/SignusAlt").Value;
                    glowMaskTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/Signus/SignusAltGlow").Value;
                }
                else
                {
                    NPCTexture = TextureAssets.Npc[npc.type].Value;
                    glowMaskTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/Signus/SignusGlow").Value;
                }

                Vector2 origin = new(NPCTexture.Width / 2, NPCTexture.Height / frameCount / 2);
                float scale = npc.scale;

                // Draw things if in the patrol phase.
                if (npc.ai[1] == (int)SignusAttackType.Patrol)
                {
                    scale *= 1.5f;

                    if (npc.Infernum().ExtraAI[10] == 1)
                    {
                        glowMaskTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Signus/Freddy").Value;
                        glowmaskFrame = null;
                    }

                    // Draw an lantern backglow.
                    Texture2D lanternTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/Signus/CosmicLantern").Value;
                    Rectangle lanternFrame = lanternTexture.Frame(1, 4, 0, (int)(Main.GlobalTimeWrappedHourly * 10f) % 4);
                    float lanternBrightness = npc.Infernum().ExtraAI[9] * Utils.GetLerpValue(1100f, 850f, npc.Distance(Main.LocalPlayer.Center), true);
                    lanternBrightness += Cos(Main.GlobalTimeWrappedHourly * 2.3f) * 0.06f;

                    Vector2 lanternDrawPosition = baseDrawPosition - Main.screenPosition + new Vector2(npc.spriteDirection * 84f, -38f) * npc.scale;
                    Texture2D backglowTexture = ModContent.Request<Texture2D>("CalamityMod/Skies/XerocLight").Value;
                    ScreenOverlaysSystem.ThingsToDrawOnTopOfBlurAdditive.Add(new(backglowTexture, lanternDrawPosition, null, Color.White * lanternBrightness * 0.7f, 0f, backglowTexture.Size() * 0.5f, lanternBrightness * 1.6f, 0, 0));
                    ScreenOverlaysSystem.ThingsToDrawOnTopOfBlurAdditive.Add(new(backglowTexture, lanternDrawPosition, null, Color.Fuchsia * lanternBrightness * 0.5f, 0f, backglowTexture.Size() * 0.5f, lanternBrightness * 3f, 0, 0));
                    ScreenOverlaysSystem.ThingsToDrawOnTopOfBlur.Add(new(lanternTexture, lanternDrawPosition, lanternFrame, Color.White * lanternBrightness, 0f, lanternFrame.Size() * 0.5f, 1f, 0, 0));
                }

                float rotation = npc.rotation * canDrawAfterimages.ToDirectionInt();
                float offsetY = npc.gfxOffY;

                Vector2 drawPosition = baseDrawPosition - Main.screenPosition;
                drawPosition -= new Vector2(NPCTexture.Width, NPCTexture.Height / frameCount) * scale / 2f;
                drawPosition += origin * scale + new Vector2(0f, 4f + offsetY);
                Main.spriteBatch.Draw(NPCTexture, drawPosition, new Rectangle?(frame), npc.GetAlpha(lightColor), rotation, origin, scale, direction, 0f);

                float opacity = npc.Opacity * 4f;
                Color glowmaskColor = Color.Lerp(Color.White, Color.Fuchsia, 0.3f) * opacity;

                if (npc.ai[1] is not (int)SignusAttackType.ShadowDash and not (int)SignusAttackType.CosmicFlameChargeBombs)
                    ScreenOverlaysSystem.ThingsToDrawOnTopOfBlur.Add(new(glowMaskTexture, drawPosition, glowmaskFrame, glowmaskColor, rotation, npc.frame.Size() * 0.5f, scale, direction, 0));
            }

            Player target = Main.player[npc.target];
            drawInstance(npc.Center, true, npc.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None);

            Vector2 cloneDrawPosition = new(target.Center.X, npc.Center.Y);
            cloneDrawPosition.X += target.Center.X - npc.Center.X;
            float lifeRatio = npc.life / (float)npc.lifeMax;

            if (lifeRatio < Phase2LifeRatio)
                drawInstance(cloneDrawPosition, false, npc.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
            return false;
        }
        #endregion

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Mods.InfernumMode.PetDialog.SignusTip1";

        }
        #endregion Tips
    }
}
