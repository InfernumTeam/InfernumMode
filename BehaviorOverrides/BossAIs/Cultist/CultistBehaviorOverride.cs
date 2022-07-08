using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Events;
using CalamityMod.Sounds;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Cultist
{
    public class CultistBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.CultistBoss;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw | NPCOverrideContext.NPCFindFrame;

        public enum CultistFrameState
        {
            AbsorbEffect,
            Hover,
            RaiseArmsUp,
            HoldArmsOut,
            Laugh,
        }

        public enum CultistAIState
        {
            SpawnEffects,
            FireballBarrage,
            LightningHover,
            ConjureLightBlasts,
            Ritual,
            IceStorm,
            AncientDoom
        }

        public const float BorderWidth = 3472f;
        public const float Phase2LifeRatio = 0.65f;
        public const float Phase3LifeRatio = 0.25f;
        public const float TransitionAnimationTime = 90f;

        #region AI

        public override bool PreAI(NPC npc)
        {
            CultistAIState attackState = (CultistAIState)(int)npc.ai[0];

            if (!Main.player.IndexInRange(npc.target) || Main.player[npc.target].dead || !Main.player[npc.target].active)
            {
                npc.TargetClosest();
                if (!Main.player.IndexInRange(npc.target) || Main.player[npc.target].dead || !Main.player[npc.target].active)
                {
                    DoDespawnEffect(npc);
                    return false;
                }
            }

            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Universally disable contact damage.
            npc.damage = 0;

            ref float attackTimer = ref npc.ai[1];
            ref float phaseState = ref npc.ai[2];
            ref float transitionTimer = ref npc.ai[3];
            ref float frameType = ref npc.localAI[0];
            ref float deathTimer = ref npc.Infernum().ExtraAI[7];
            ref float initialXPosition = ref npc.Infernum().ExtraAI[8];
            ref float borderDustCounter = ref npc.Infernum().ExtraAI[9];

            bool shouldBeInPhase2 = npc.life < npc.lifeMax * Phase2LifeRatio;
            bool inPhase2 = phaseState == 2f;
            bool dying = npc.Infernum().ExtraAI[6] == 1f;

            if (initialXPosition == 0f)
            {
                initialXPosition = npc.Center.X;
                npc.netUpdate = true;
            }

            if (dying)
            {
                DoDyingEffects(npc, ref deathTimer);
                npc.dontTakeDamage = true;
                frameType = (int)CultistFrameState.Laugh;
                return false;
            }

            float left = initialXPosition - BorderWidth / 2f + 30f;
            float right = initialXPosition + BorderWidth / 2f - 30f;

            // Restrict the player's position.
            target.Center = Vector2.Clamp(target.Center, new Vector2(left + target.width * 0.5f, -100f), new Vector2(right - target.width * 0.5f, Main.maxTilesY * 16f + 100f));
            if (target.Center.X < left + 160f)
            {
                Dust magic = Dust.NewDustPerfect(new Vector2(left - 12f, target.Center.Y), 261);
                magic.velocity = Main.rand.NextVector2Circular(10f, 5f);
                magic.velocity.X = Math.Abs(magic.velocity.X);
                magic.color = Color.Lerp(Color.Blue, Color.MediumSeaGreen, Main.rand.NextFloat(0.25f, 1f));
                magic.scale = 1.1f;
                magic.fadeIn = 1.4f;
                magic.noGravity = true;
            }
            if (target.Center.X > right - 160f)
            {
                Dust magic = Dust.NewDustPerfect(new Vector2(right + 12f, target.Center.Y), 261);
                magic.velocity = Main.rand.NextVector2Circular(10f, 5f);
                magic.velocity.X = -Math.Abs(magic.velocity.X);
                magic.color = Color.Lerp(Color.Blue, Color.MediumSeaGreen, Main.rand.NextFloat(0.25f, 1f));
                magic.scale = 1.1f;
                magic.fadeIn = 1.4f;
                magic.noGravity = true;
            }

            // Create an eye effect, sans-style.
            if ((phaseState == 1f && transitionTimer >= TransitionAnimationTime + 8f) || inPhase2)
                DoEyeEffect(npc);

            if (shouldBeInPhase2 && !inPhase2)
            {
                npc.dontTakeDamage = true;
                TransitionToSecondPhase(npc, target, ref frameType, ref transitionTimer, ref phaseState);
                transitionTimer++;
                return false;
            }

            npc.dontTakeDamage = false;
            switch (attackState)
            {
                case CultistAIState.SpawnEffects:
                    DoAttack_SpawnEffects(npc, target, ref frameType, ref attackTimer);
                    break;
                case CultistAIState.FireballBarrage:
                    DoAttack_FireballBarrage(npc, target, ref frameType, ref attackTimer, inPhase2);
                    break;
                case CultistAIState.LightningHover:
                    DoAttack_LightningHover(npc, target, ref frameType, ref attackTimer, inPhase2);
                    break;
                case CultistAIState.ConjureLightBlasts:
                    DoAttack_ConjureLightBlasts(npc, target, ref frameType, ref attackTimer, inPhase2);
                    break;
                case CultistAIState.Ritual:
                    DoAttack_PerformRitual(npc, target, ref frameType, ref attackTimer, inPhase2);
                    break;
                case CultistAIState.IceStorm:
                    DoAttack_IceStorm(npc, target, ref frameType, ref attackTimer);
                    break;
                case CultistAIState.AncientDoom:
                    DoAttack_AncientDoom(npc, target, ref frameType, ref attackTimer);
                    break;
            }
            attackTimer++;

            return false;
        }

        public static void DoEyeEffect(NPC npc)
        {
            Vector2 eyePosition = npc.Top + new Vector2(npc.spriteDirection == -1f ? -8f : 6f, 12f);

            Dust eyeDust = Dust.NewDustPerfect(eyePosition, 264);
            eyeDust.color = Color.CornflowerBlue;
            eyeDust.velocity = -Vector2.UnitY.RotatedBy(MathHelper.Clamp(npc.velocity.X * -0.04f, -1f, 1f)) * 2.6f;
            eyeDust.velocity = eyeDust.velocity.RotatedByRandom(0.12f);
            eyeDust.velocity += npc.velocity;
            eyeDust.scale = Main.rand.NextFloat(1.4f, 1.48f);
            eyeDust.noGravity = true;
        }

        public static void ClearAwayEntities()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // Clear any clones or other things that might remain from other attacks.
            int[] projectilesToClearAway = new int[]
            {
                ModContent.ProjectileType<Ritual>(),
                ModContent.ProjectileType<CultistFireBeamTelegraph>(),
                ModContent.ProjectileType<FireBeam>(),
                ModContent.ProjectileType<AncientDoom>(),
                ModContent.ProjectileType<DoomBeam>(),
            };
            int[] npcsToClearAway = new int[]
            {
                NPCID.CultistBossClone,
                NPCID.CultistDragonHead,
                NPCID.CultistDragonBody1,
                NPCID.CultistDragonBody2,
                NPCID.CultistDragonBody3,
                NPCID.CultistDragonBody4,
                NPCID.CultistDragonTail,
                NPCID.AncientCultistSquidhead,
            };

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (projectilesToClearAway.Contains(Main.projectile[i].type) && Main.projectile[i].active)
                    Main.projectile[i].Kill();
            }

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (npcsToClearAway.Contains(Main.npc[i].type) && Main.npc[i].active)
                {
                    Main.npc[i].active = false;
                    Main.npc[i].netUpdate = true;
                }
            }
        }

        public static void DoDespawnEffect(NPC npc)
        {
            npc.velocity = Vector2.Zero;
            npc.dontTakeDamage = true;
            if (npc.timeLeft > 25)
                npc.timeLeft = 25;

            npc.alpha = Utils.Clamp(npc.alpha + 40, 0, 255);
            if (npc.alpha >= 255)
            {
                npc.active = false;
                npc.netUpdate = true;
            }
        }

        public static void DoDyingEffects(NPC npc, ref float deathTimer)
        {
            npc.velocity = Vector2.Zero;

            if (deathTimer > 300f)
            {
                npc.NPCLoot();
                npc.active = false;
                npc.netUpdate = true;

                // Create a rumble effect to go with the summoning of the pillars.
                Main.LocalPlayer.Infernum().CurrentScreenShakePower = 15f;
                return;
            }

            // Focus on the boss as it spawns.
            if (Main.LocalPlayer.WithinRange(Main.LocalPlayer.Center, 4000f))
            {
                Main.LocalPlayer.Infernum().ScreenFocusPosition = npc.Center;
                Main.LocalPlayer.Infernum().ScreenFocusInterpolant = Utils.GetLerpValue(0f, 15f, deathTimer, true);
                Main.LocalPlayer.Infernum().ScreenFocusInterpolant *= Utils.GetLerpValue(300f, 292f, deathTimer, true);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool(36) && deathTimer >= 75f && deathTimer < 210f)
                Utilities.NewProjectileBetter(npc.Center, Main.rand.NextVector2Unit(), ModContent.ProjectileType<LightBeam>(), 0, 0f);

            if (deathTimer > 100f)
            {
                Dust magic = Dust.NewDustDirect(npc.position, npc.width, npc.height, 223);
                magic.velocity = -Vector2.UnitY.RotatedByRandom(0.29f) * Main.rand.NextFloat(2.8f, 3.5f);
                magic.scale = Main.rand.NextFloat(1.2f, 1.3f);
                magic.fadeIn = 0.7f;
                magic.noGravity = true;
                magic.noLight = true;
            }

            int variant = 0;
            bool canMakeExplosion = false;
            switch ((int)deathTimer)
            {
                case 180:
                    variant = 0;
                    canMakeExplosion = true;
                    break;
                case 190:
                    variant = 1;
                    canMakeExplosion = true;
                    break;
                case 200:
                    variant = 2;
                    canMakeExplosion = true;
                    break;
                case 210:
                    variant = 3;
                    canMakeExplosion = true;
                    break;
            }

            // Create explosions with pillar colors.
            if (canMakeExplosion)
            {
                SoundEngine.PlaySound(CommonCalamitySounds.FlareSound, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int explosion = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<DeathExplosion>(), 0, 0f);
                    Main.projectile[explosion].localAI[1] = variant;
                }
            }

            deathTimer++;
        }

        public static void TransitionToSecondPhase(NPC npc, Player target, ref float frameType, ref float transitionTimer, ref float phaseState)
        {
            npc.velocity *= 0.95f;

            // Fade out effects.
            if (phaseState == 0f)
            {
                // Create a laugh sound effect.
                if (transitionTimer == 15f)
                    SoundEngine.PlaySound(SoundID.Zombie105, npc.Center);

                // Fade away.
                npc.Opacity = Utils.GetLerpValue(35f, 15f, transitionTimer, true);

                if (Main.netMode != NetmodeID.MultiplayerClient && transitionTimer >= 35f)
                {
                    ClearAwayEntities();

                    npc.Center = target.Center - Vector2.UnitY * 305f;
                    transitionTimer = 0f;
                    phaseState = 1f;
                    npc.netUpdate = true;
                }

                frameType = (int)CultistFrameState.Laugh;
            }

            if (phaseState == 1f)
            {
                npc.Opacity = Utils.GetLerpValue(0f, 8f, transitionTimer, true);

                // Create a laugh sound effect.
                if (transitionTimer == TransitionAnimationTime + 5f)
                    SoundEngine.PlaySound(SoundID.Zombie105, npc.Center);

                if (phaseState >= TransitionAnimationTime)
                    frameType = (int)CultistFrameState.Laugh;
                else
                    frameType = (int)CultistFrameState.Hover;

                // Transition to the second phase.
                if (transitionTimer >= TransitionAnimationTime + 25f)
                {
                    // Reset the ongoing attack to light blast usage.
                    npc.ai[0] = (int)CultistAIState.ConjureLightBlasts;
                    npc.ai[1] = 0f;

                    transitionTimer = 0f;
                    phaseState = 2f;
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoAttack_SpawnEffects(NPC npc, Player target, ref float frameType, ref float attackTimer)
        {
            int absorbEffectTime = 24;
            int hoverTime = 240;

            if (attackTimer < absorbEffectTime + hoverTime)
            {
                if (attackTimer < absorbEffectTime)
                    frameType = (int)CultistFrameState.AbsorbEffect;
                else
                    frameType = (int)CultistFrameState.Hover;

                // Fade in.
                npc.alpha = Utils.Clamp(npc.alpha - 5, 0, 255);
            }
            else
            {
                // Create a laugh sound effect.
                if (attackTimer == absorbEffectTime + hoverTime + 15f)
                    SoundEngine.PlaySound(SoundID.Zombie105, npc.Center);
                if (attackTimer > absorbEffectTime + hoverTime + 20f)
                {
                    // Fade out.
                    npc.alpha = Utils.Clamp(npc.alpha + 21, 0, 255);

                    // And create a bunch of magic at the hitbox when disappearing.
                    if (npc.Opacity < 0.5f)
                    {
                        int totalDust = (int)MathHelper.Lerp(1f, 4f, Utils.GetLerpValue(0.5f, 0.1f, npc.Opacity, true));
                        for (int i = 0; i < totalDust; i++)
                        {
                            Dust magic = Dust.NewDustDirect(npc.position, npc.width, npc.height, 264);
                            magic.color = Color.Lerp(Color.LightPink, Color.Magenta, Main.rand.NextFloat());
                            magic.velocity = -Vector2.UnitY.RotatedByRandom(0.4f) * Main.rand.NextFloat(2.8f, 3.5f);
                            magic.scale = Main.rand.NextFloat(1.2f, 1.3f);
                            magic.fadeIn = 1.1f;
                            magic.noGravity = true;
                            magic.noLight = true;
                        }
                    }

                    Vector2[] armPositions = new Vector2[]
                    {
                        npc.Center + new Vector2(npc.spriteDirection == -1 ? 10f : -6f, 4f),
                        npc.Center + new Vector2(npc.spriteDirection == -1 ? 6f : -10f, 4f),
                    };

                    // Do a magic effect from the arms.
                    foreach (Vector2 armPosition in armPositions)
                    {
                        Dust magic = Dust.NewDustPerfect(armPosition, 267);
                        magic.velocity = -Vector2.UnitY.RotatedByRandom(0.14f) * Main.rand.NextFloat(2.5f, 3.25f);
                        magic.color = Color.Lerp(Color.Purple, Color.DarkBlue, Main.rand.NextFloat()) * npc.Opacity;
                        magic.scale = Main.rand.NextFloat(1.05f, 1.25f);
                        magic.noGravity = true;
                    }

                    // Start attacking.
                    if (npc.alpha >= 255)
                    {
                        Vector2 teleportPosition = target.Center - Vector2.UnitY * 350f;
                        CreateTeleportTelegraph(npc.Center, teleportPosition, 250);
                        npc.Center = teleportPosition;
                        SelectNextAttack(npc);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            npc.Center = teleportPosition;
                            npc.netUpdate = true;
                        }
                    }
                }

                frameType = (int)CultistFrameState.Laugh;
            }

            npc.dontTakeDamage = true;
        }

        public static void DoAttack_FireballBarrage(NPC npc, Player target, ref float frameType, ref float attackTimer, bool phase2)
        {
            int fireballShootRate = phase2 ? 11 : 7;
            int fireballCount = phase2 ? 30 : 32;
            int attackLength = 105 + fireballShootRate * fireballCount;
            if (phase2)
                attackLength += 390;

            bool canShootFireballs = attackTimer >= 105f && attackTimer < 105f + fireballShootRate * fireballCount;

            ref float aimRotation = ref npc.Infernum().ExtraAI[0];

            npc.velocity *= 0.96f;

            if (attackTimer == 10f && !npc.WithinRange(target.Center, 720f))
            {
                Vector2 teleportPosition = target.Center - Vector2.UnitY * 300f;
                CreateTeleportTelegraph(npc.Center, teleportPosition, 250);
                npc.Center = teleportPosition;
                npc.netUpdate = true;
            }

            if (attackTimer < 105f)
            {
                frameType = (int)CultistFrameState.Hover;
                npc.spriteDirection = (npc.Center.X < target.Center.X).ToDirectionInt();
            }

            // Shoot fireballs.
            else if (canShootFireballs)
            {
                // Fire a barrage of fireballs from the hands and create some from the sky in Phase 1.
                if (!phase2)
                {
                    int skyFireballShootRate = fireballShootRate * 2;
                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % fireballShootRate == fireballShootRate - 1f)
                    {
                        Vector2 fireballSpawnPosition = npc.Center + new Vector2(npc.spriteDirection * 24f, 6f);
                        if (aimRotation == 0f)
                            aimRotation = (target.Center - fireballSpawnPosition + target.velocity * 30f).ToRotation();
                        else
                            aimRotation = aimRotation.AngleTowards(npc.AngleTo(target.Center), 0.18f);

                        float shootSpeed = Main.rand.NextFloat(12f, 14f) + npc.Distance(target.Center) * 0.011f;
                        shootSpeed *= Utils.Remap(attackTimer, 105f, 182f, 0.35f, 1f);

                        Vector2 fireballShootVelocity = aimRotation.ToRotationVector2() * shootSpeed;
                        fireballShootVelocity = fireballShootVelocity.RotatedByRandom(MathHelper.Pi * 0.1f);
                        if (BossRushEvent.BossRushActive)
                            fireballShootVelocity *= 1.5f;

                        Utilities.NewProjectileBetter(fireballSpawnPosition, fireballShootVelocity, ProjectileID.CultistBossFireBall, 195, 0f);
                    }
                    
                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % skyFireballShootRate == skyFireballShootRate - 1f)
                    {
                        Vector2 fireballSpawnPosition = target.Center - new Vector2(Main.rand.NextFloatDirection() * 600f, -850f - target.velocity.Y * 20f);
                        Utilities.NewProjectileBetter(fireballSpawnPosition, Vector2.UnitY * 7.75f, ProjectileID.CultistBossFireBall, 195, 0f);
                    }

                    frameType = (int)CultistFrameState.HoldArmsOut;
                }

                // In Phase 2 however, conjure fireballs that appear from the sides of the target.
                else
                {
                    // Also make cinders appear around the target to actually give a sense that things are warming up.
                    for (int i = 0; i < 4; i++)
                    {
                        if (!Main.rand.NextBool(7))
                            continue;

                        Dust fire = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Square(-1200f, 1200f), 6);
                        fire.velocity = -Vector2.UnitY.RotatedByRandom(0.35f) * Main.rand.NextFloat(2.5f, 4f);
                        fire.scale *= Main.rand.NextFloat(1.5f, 2f);
                        fire.fadeIn = Main.rand.NextFloat(0.4f, 0.75f);
                        fire.noGravity = true;
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % fireballShootRate == fireballShootRate - 1f)
                    {
                        Vector2 fireballSpawnPosition = target.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(1035f, 1185f);
                        int telegraph = Utilities.NewProjectileBetter(fireballSpawnPosition, Vector2.Zero, ModContent.ProjectileType<FireballLineTelegraph>(), 0, 0f);
                        if (Main.projectile.IndexInRange(telegraph))
                            Main.projectile[telegraph].ModProjectile<FireballLineTelegraph>().Destination = target.Center + target.velocity * 28f;
                    }
                    frameType = (int)CultistFrameState.RaiseArmsUp;
                }
            }

            // Shoot a powerful fire beam in phase 2.
            else if (phase2 && attackTimer >= 105 + fireballShootRate * fireballCount)
            {
                float adjustedTime = attackTimer - (105 + fireballShootRate * fireballCount);
                frameType = (int)CultistFrameState.RaiseArmsUp;
                if (adjustedTime is > 80f and < 140f)
                    frameType = (int)CultistFrameState.Laugh;

                if (adjustedTime == 10f && !npc.WithinRange(target.Center, 720f))
                {
                    Vector2 teleportPosition = target.Center - Vector2.UnitY * 325f;
                    CreateTeleportTelegraph(npc.Center, teleportPosition, 250);
                    npc.Center = teleportPosition;
                    npc.spriteDirection = (npc.Center.X < target.Center.X).ToDirectionInt();
                    npc.netUpdate = true;
                }

                Vector2 beamShootPosition = npc.Top - Vector2.UnitY * 4f;

                // Create charge-up dust.
                if (adjustedTime < 80f)
                {
                    Vector2 dustSpawnPosition = beamShootPosition + Main.rand.NextVector2CircularEdge(56f, 56f);
                    Dust fire = Dust.NewDustPerfect(dustSpawnPosition, 222);
                    fire.color = Color.Orange;
                    fire.velocity = (beamShootPosition - fire.position) * 0.08f;
                    fire.scale = 1.125f;
                    fire.noGravity = true;
                }

                // Make burst dust and make a chanting sound.
                if (adjustedTime == 90f)
                {
                    npc.TargetClosest();
                    npc.spriteDirection = (npc.Center.X < target.Center.X).ToDirectionInt();

                    for (int i = 0; i < 40; i++)
                    {
                        Dust fire = Dust.NewDustPerfect(beamShootPosition, ModContent.DustType<FinalFlame>());
                        fire.velocity = (MathHelper.TwoPi * i / 40f).ToRotationVector2() * 5f;
                        fire.scale = 1.5f;
                        fire.noGravity = true;
                    }
                    SoundEngine.PlaySound(SoundID.Zombie90, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 aimDirection = (target.Center - beamShootPosition).SafeNormalize(-Vector2.UnitY);
                        Utilities.NewProjectileBetter(beamShootPosition, aimDirection, ModContent.ProjectileType<CultistFireBeamTelegraph>(), 0, 0f);
                    }
                }
            }

            if (attackTimer >= attackLength)
                SelectNextAttack(npc);
        }

        public static void DoAttack_LightningHover(NPC npc, Player target, ref float frameType, ref float attackTimer, bool phase2)
        {
            int lightningBurstCount = phase2 ? 2 : 3;
            int hoverTime = phase2 ? 54 : 65;
            int summonLightningTime = phase2 ? 40 : 50;
            int lightningBurstTime = (hoverTime + summonLightningTime) * lightningBurstCount;
            int attackLength = lightningBurstTime + 20;

            int nebulaTelegraphTime = 50;
            int nebulaShootTime = 50;
            int lightningCount = 18;
            int nebulaLightningCycleCount = 2;
            int nebulaLightningShootTime = (nebulaTelegraphTime + nebulaShootTime + 10) * nebulaLightningCycleCount;
            Vector2 lightningSpawnPosition = npc.Center + Vector2.UnitX * npc.spriteDirection * 20f;
            if (phase2)
                attackLength += (nebulaTelegraphTime + nebulaShootTime + 10) * nebulaLightningCycleCount - 2;
            ref float nebulaLightningDirection = ref npc.Infernum().ExtraAI[0];
            ref float telegraphSummonCounter = ref npc.Infernum().ExtraAI[1];

            // Play a chant sound and create pink dust prior to releasing red lightning.
            if (phase2 && attackTimer >= attackLength - nebulaLightningShootTime - 35f && attackTimer < attackLength - nebulaLightningShootTime + 5f)
            {
                // Release hand electric dust.
                for (int j = 0; j < 2; j++)
                {
                    Dust electricity = Dust.NewDustPerfect(lightningSpawnPosition, 264);
                    electricity.velocity = Vector2.UnitX.RotatedByRandom(0.2f) * npc.spriteDirection * 2.6f;
                    electricity.scale = Main.rand.NextFloat(1.3f, 1.425f);
                    electricity.fadeIn = 0.9f;
                    electricity.color = Color.Red;
                    electricity.noLight = true;
                    electricity.noGravity = true;
                }
            }

            if (phase2 && attackTimer == attackLength - nebulaLightningShootTime + 5f)
                SoundEngine.PlaySound(SoundID.Zombie91, npc.Center);

            // Hover and fly above the player.
            if (attackTimer % (hoverTime + summonLightningTime) < hoverTime && attackTimer < lightningBurstTime + 20)
            {
                Vector2 destination = target.Center - Vector2.UnitY * 375f;
                Vector2 idealVelocity = npc.SafeDirectionTo(destination) * MathHelper.Max(10f, npc.Distance(destination) * 0.05f);

                if (!npc.WithinRange(destination, 185f))
                    npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.15f);
                else
                    npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(destination), 0.045f);

                if (MathHelper.Distance(destination.X, npc.Center.X) > 24f)
                    npc.spriteDirection = Math.Sign(destination.X - npc.Center.X);

                frameType = (int)CultistFrameState.Hover;
            }
             
            if (attackTimer < lightningBurstTime)
            {
                npc.velocity *= 0.94f;

                Vector2[] handPositions = new Vector2[]
                {
                    npc.Top + new Vector2(-12f, 6f),
                    npc.Top + new Vector2(12f, 6f),
                };

                float adjustedTime = attackTimer % (hoverTime + summonLightningTime) - hoverTime;

                // Teleport if necessary.
                bool tooFarFromPlayer = !npc.WithinRange(target.Center, 520f) || MathHelper.Distance(target.Center.Y, npc.Center.Y) > 335f;
                if (adjustedTime == 5f && tooFarFromPlayer)
                {
                    Vector2 teleportPosition = target.Center - Vector2.UnitY * 245f;
                    CreateTeleportTelegraph(npc.Center, teleportPosition, 350);
                    npc.velocity = Vector2.Zero;
                    npc.Center = teleportPosition;
                    npc.spriteDirection = (npc.Center.X < target.Center.X).ToDirectionInt();
                    npc.netUpdate = true;
                }

                // Create electric sparks on cultist's hands.
                if (adjustedTime < 25f)
                {
                    foreach (Vector2 handPosition in handPositions)
                    {
                        Dust electricity = Dust.NewDustPerfect(handPosition, 229);
                        electricity.velocity = -Vector2.UnitY.RotatedByRandom(0.21f) * Main.rand.NextFloat(2.4f, 4f);
                        electricity.scale = Main.rand.NextFloat(0.75f, 0.85f);
                        electricity.noGravity = true;
                    }
                }

                // Create a burst of sparks and summon orbs.
                if (adjustedTime is 30f or 36f)
                {
                    npc.velocity = Vector2.Zero;
                    for (int i = 0; i < 2; i++)
                    {
                        for (int j = 0; j < (phase2 ? 2 : 1); j++)
                        {
                            Vector2 orbSummonPosition = npc.Center - Vector2.UnitY * 450f;
                            orbSummonPosition.X -= (i == 0).ToDirectionInt() * (350f + j * 100f);

                            if (adjustedTime == 30f)
                            {
                                // Release a line of electricity towards the orb.
                                for (int k = 0; k < 200; k++)
                                {
                                    Vector2 dustPosition = Vector2.Lerp(handPositions[i], orbSummonPosition, k / 200f);
                                    Dust electricity = Dust.NewDustPerfect(dustPosition, 229);
                                    electricity.velocity = Main.rand.NextVector2Circular(0.15f, 0.15f);
                                    electricity.scale = Main.rand.NextFloat(1f, 1.2f);
                                    electricity.noGravity = true;
                                }
                            }

                            else if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                int lightningCircleCount = phase2 ? 4 : 1;
                                for (int k = 0; k < lightningCircleCount; k++)
                                {
                                    Vector2 predictivenessOffset = target.velocity * new Vector2(40f, 20f);
                                    if (phase2)
                                        predictivenessOffset *= 0.9f;

                                    Vector2 lightningVelocity = (target.Center - orbSummonPosition + predictivenessOffset).SafeNormalize(Vector2.UnitY) * 8.5f;
                                    lightningVelocity = lightningVelocity.RotatedBy(MathHelper.TwoPi * k / lightningCircleCount);
                                    if (!phase2)
                                        lightningVelocity *= 1.15f;
                                    if (BossRushEvent.BossRushActive)
                                        lightningVelocity *= 1.3f;

                                    int lightning = Utilities.NewProjectileBetter(orbSummonPosition, lightningVelocity, ProjectileID.CultistBossLightningOrbArc, 200, 0f);
                                    Main.projectile[lightning].ai[0] = lightningVelocity.ToRotation();
                                    Main.projectile[lightning].ai[1] = Main.rand.Next(100);
                                    Main.projectile[lightning].tileCollide = false;
                                }
                            }
                        }
                    }

                    SoundEngine.PlaySound(SoundID.Item72, target.Center);
                    npc.netUpdate = true;
                }

                frameType = (int)CultistFrameState.RaiseArmsUp;
            }

            // Release a torrent of telegraphed nebula lightning in phase 2.
            else if (phase2)
            {
                // Hold hands out.
                frameType = (int)CultistFrameState.HoldArmsOut;

                npc.velocity *= 0.95f;

                float wrappedAttackTimer = (attackTimer - lightningBurstTime - 10f) % (nebulaTelegraphTime + nebulaShootTime);

                // Create telegraph lines.
                if (wrappedAttackTimer < nebulaTelegraphTime)
                {
                    npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                    if (wrappedAttackTimer == 1f && telegraphSummonCounter < nebulaLightningCycleCount)
                    {
                        // Play a firing sound.
                        SoundEngine.PlaySound(SoundID.Item72, target.Center);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            nebulaLightningDirection = npc.AngleTo(target.Center);
                            telegraphSummonCounter++;
                            for (int i = 0; i < lightningCount; i++)
                            {
                                Vector2 telegraphDirection = (MathHelper.TwoPi * i / lightningCount + nebulaLightningDirection).ToRotationVector2();
                                int line = Utilities.NewProjectileBetter(npc.Center, telegraphDirection, ModContent.ProjectileType<NebulaTelegraphLine>(), 0, 0f);
                                if (Main.projectile.IndexInRange(line))
                                {
                                    Main.projectile[line].ai[1] = nebulaTelegraphTime - 1f;
                                    Main.projectile[line].localAI[0] = MathHelper.Lerp(1f, 0.35f, i / (float)(lightningCount - 1f));
                                }
                            }
                            npc.netUpdate = true;
                        }
                    }
                }

                // Release the nebula lightning.
                else if (wrappedAttackTimer % 3f == 0f && wrappedAttackTimer < nebulaTelegraphTime + nebulaShootTime)
                {
                    float shootInterpolant = Utils.GetLerpValue(nebulaTelegraphTime, nebulaTelegraphTime + nebulaShootTime, wrappedAttackTimer, true);
                    Vector2 lightningVelocity = (MathHelper.TwoPi * shootInterpolant + nebulaLightningDirection).ToRotationVector2() * 1.87f;
                    lightningSpawnPosition -= lightningVelocity * 40f;

                    npc.spriteDirection = (lightningVelocity.X > 0f).ToDirectionInt();
                    SoundEngine.PlaySound(SoundID.Item72, target.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int lightning = Utilities.NewProjectileBetter(lightningSpawnPosition, lightningVelocity, ModContent.ProjectileType<PinkLightning>(), 225, 0f);
                        if (Main.projectile.IndexInRange(lightning))
                        {
                            Main.projectile[lightning].ai[0] = Main.projectile[lightning].velocity.ToRotation();
                            Main.projectile[lightning].ai[1] = Main.rand.Next(100);
                        }
                    }
                }
            }

            if (attackTimer >= attackLength)
            {
                Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<FireballLineTelegraph>(), ModContent.ProjectileType<PinkLightning>());
                SelectNextAttack(npc);
            }
        }

        public static void DoAttack_ConjureLightBlasts(NPC npc, Player target, ref float frameType, ref float attackTimer, bool phase2)
        {
            int shootDelay = 20;
            int lightBurstCount = phase2 ? 24 : 14;
            int lightBurstShootRate = phase2 ? 2 : 3;
            int sideLightSummonRate = phase2 ? 18 : 25;
            int lightBurstAttackDelay = phase2 ? 185 : 225;
            int attackLength = shootDelay + lightBurstCount * lightBurstShootRate + lightBurstAttackDelay;
            if (phase2)
                attackLength += 205;

            bool waitingBeforeFiring = attackTimer >= shootDelay + lightBurstCount * lightBurstShootRate &&
                attackTimer < shootDelay + lightBurstCount * lightBurstShootRate + lightBurstAttackDelay;
            bool performingPhase2Attack = attackTimer >= shootDelay + lightBurstCount * lightBurstShootRate + lightBurstAttackDelay;

            if (attackTimer <= 15f)
                frameType = (int)CultistFrameState.Hover;

            ref float shotCounter = ref npc.Infernum().ExtraAI[0];

            // Teleport above the player.
            if (attackTimer == 15f)
            {
                Vector2 teleportPosition = target.Center - Vector2.UnitY * 270f;
                CreateTeleportTelegraph(npc.Center, teleportPosition, 250);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.velocity = Vector2.Zero;
                    npc.Center = teleportPosition;
                    npc.spriteDirection = Main.rand.NextBool(2).ToDirectionInt();
                    npc.netUpdate = true;
                }
            }

            // Release a burst of lights everywhere.
            if (performingPhase2Attack)
            {
                float adjustedTime = attackTimer - (shootDelay + lightBurstCount * lightBurstShootRate + lightBurstAttackDelay);

                // Absorb a bunch of magic.
                if (adjustedTime < 75f)
                {
                    Vector2 dustSpawnPosition = npc.Center + Main.rand.NextVector2CircularEdge(85f, 85f);
                    Dust light = Dust.NewDustPerfect(dustSpawnPosition, 264);
                    light.color = Color.Orange;
                    light.velocity = (npc.Center - light.position) * 0.08f;
                    light.scale = 1.4f;
                    light.fadeIn = 0.3f;
                    light.noGravity = true;
                }
                frameType = (int)CultistFrameState.RaiseArmsUp;

                // Create a flash of light at the cultist's position and release a bunch of light.
                if (adjustedTime == 75f && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.Center = target.Center - Vector2.UnitY * 300f;
                    npc.netUpdate = true;

                    CreateTeleportTelegraph(npc.Center, npc.Center, 0);
                }

                if (adjustedTime > 80f && adjustedTime < 180f && adjustedTime % 5f == 4f)
                {
                    Vector2 lightSpawnPosition = target.Center + target.velocity * 15f + Main.rand.NextVector2Circular(920f, 920f) * (BossRushEvent.BossRushActive ? 1.45f : 1f);
                    lightSpawnPosition += target.velocity * Main.rand.NextFloat(5f, 32f);
                    CreateTeleportTelegraph(npc.Center, lightSpawnPosition, 150, true, 1);
                    int light = Utilities.NewProjectileBetter(lightSpawnPosition, Vector2.Zero, ModContent.ProjectileType<LightBurst>(), 195, 0f);
                    if (Main.projectile.IndexInRange(light))
                        Main.projectile[light].ai[0] = 215f - adjustedTime + Main.rand.Next(20);
                }
            }

            // Release a burst of light.
            else if (attackTimer > 15f && !waitingBeforeFiring)
            {
                Vector2 handPosition = npc.Center + new Vector2(npc.spriteDirection * 20f, 6f);

                // Release light from the hand.
                for (int i = 0; i < 2; i++)
                {
                    Dust lightMagic = Dust.NewDustPerfect(handPosition + Main.rand.NextVector2Circular(4f, 4f), 264);
                    lightMagic.scale = Main.rand.NextFloat(1.1f, 1.275f);
                    lightMagic.fadeIn = 0.45f;
                    lightMagic.velocity = -Vector2.UnitY.RotatedByRandom(0.28f) * Main.rand.NextFloat(2.8f, 4.2f);
                    lightMagic.color = Color.LightBlue;
                    lightMagic.noLight = true;
                    lightMagic.noGravity = true;
                }

                if (attackTimer > shootDelay)
                {
                    if (attackTimer % lightBurstShootRate == lightBurstShootRate - 1f)
                    {
                        // Release a burst of light from the hand.
                        for (int i = 0; i < 16; i++)
                        {
                            Dust lightMagic = Dust.NewDustPerfect(handPosition, 264);
                            lightMagic.scale = 0.85f;
                            lightMagic.fadeIn = 0.35f;
                            lightMagic.velocity = (MathHelper.TwoPi * i / 16f).ToRotationVector2() * 2.7f;
                            lightMagic.velocity.Y -= 1.8f;
                            lightMagic.color = Color.LightBlue;
                            lightMagic.noLight = true;
                            lightMagic.noGravity = true;
                        }

                        // Create the light patterns.
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            npc.TargetClosest();

                            Vector2 shootVelocity = Vector2.UnitX.RotatedByRandom(0.51f) * npc.spriteDirection * 10f;
                            if (phase2)
                                shootVelocity = -Vector2.UnitY.RotatedBy(MathHelper.TwoPi * shotCounter / lightBurstCount) * 12f;
                            if (BossRushEvent.BossRushActive)
                                shootVelocity *= 1.7f;

                            Point lightSpawnPosition = (handPosition + shootVelocity.SafeNormalize(Vector2.UnitX * npc.spriteDirection) * 10f).ToPoint();
                            int ancientLight = NPC.NewNPC(npc.GetSource_FromAI(), lightSpawnPosition.X, lightSpawnPosition.Y, NPCID.AncientLight, 0, phase2.ToInt());
                            if (Main.npc.IndexInRange(ancientLight))
                            {
                                Main.npc[ancientLight].velocity = shootVelocity;
                                Main.npc[ancientLight].target = npc.target;
                            }

                            shotCounter++;
                            npc.netUpdate = true;
                        }
                    }
                }
                frameType = (int)CultistFrameState.HoldArmsOut;
            }

            if (attackTimer > shootDelay)
            {
                // Create light behind the player that follows them, to make the attack more interesting.
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % sideLightSummonRate == sideLightSummonRate - 1f)
                {
                    Point lightSpawnPosition = (target.Center - target.velocity.RotatedByRandom(0.5f).SafeNormalize(Main.rand.NextVector2Unit()) * 850f).ToPoint();
                    int ancientLight = NPC.NewNPC(npc.GetSource_FromAI(), lightSpawnPosition.X, lightSpawnPosition.Y, NPCID.AncientLight, 0, 2f);
                    if (Main.npc.IndexInRange(ancientLight))
                    {
                        Main.npc[ancientLight].velocity = (target.Center - lightSpawnPosition.ToVector2()).SafeNormalize(Vector2.UnitY) * 13f;
                        Main.npc[ancientLight].target = npc.target;
                    }
                }
            }

            if (attackTimer >= attackLength)
                SelectNextAttack(npc);
        }

        public static void DoAttack_PerformRitual(NPC npc, Player target, ref float frameType, ref float attackTimer, bool phase2)
        {
            int cloneCount = phase2 ? 11 : 7;
            int waitDelay = 30 + Ritual.GetWaitTime(phase2);
            ref float fadeCountdown = ref npc.Infernum().ExtraAI[0];
            ref float ritualIndex = ref npc.Infernum().ExtraAI[1];

            void createRitualZap(List<int> cultists = null)
            {
                if (cultists is null)
                {
                    cultists = new List<int>();
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if ((Main.npc[i].type == NPCID.CultistBoss || Main.npc[i].type == NPCID.CultistBossClone) && Main.npc[i].active)
                            cultists.Add(i);
                    }
                }

                foreach (int cultist in cultists)
                    CreateTeleportTelegraph(Main.projectile[(int)npc.Infernum().ExtraAI[1]].Center, Main.npc[cultist].Center, 45, false);
            }

            // Play a chant sound before fading out.
            if (attackTimer == 15f)
                SoundEngine.PlaySound(SoundID.Zombie90, npc.Center);
            if (attackTimer <= 30f)
            {
                npc.Opacity = Utils.GetLerpValue(30f, 15f, attackTimer, true);
                frameType = (int)CultistFrameState.Laugh;
            }

            // Holds arms out during the ritual.
            if (attackTimer == 29f)
                frameType = (int)CultistFrameState.HoldArmsOut;

            // Fade in after summoning a ritual.
            if (fadeCountdown > 0f)
            {
                npc.Opacity = Utils.GetLerpValue(18f, 0f, fadeCountdown, true);
                fadeCountdown--;
            }

            // Attempt to begin a ritual.
            if (attackTimer == 30f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                List<int> cultists = new();

                // Ensure that the ritual is not started outside of the arena border and not in tiles.
                float leftEdgeOfBorder = npc.Infernum().ExtraAI[8] - BorderWidth * 0.5f + 300f;
                float rightEdgeOfBorder = npc.Infernum().ExtraAI[8] + BorderWidth * 0.5f - 300f;

                int ritualDecisionTries = 0;
                Vector2 ritualCenter;
                do
                {
                    ritualCenter = target.Center + Main.rand.NextVector2CircularEdge(ritualDecisionTries * 0.25f + 360f, ritualDecisionTries * 0.25f + 360f);
                    ritualCenter.X = MathHelper.Clamp(ritualCenter.X, leftEdgeOfBorder, rightEdgeOfBorder);

                    if (!Collision.SolidCollision(ritualCenter - Vector2.One * 180f, 360, 360))
                        break;
                }
                while (ritualDecisionTries < 1000);

                for (int i = 0; i < cloneCount; i++)
                {
                    int clone = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, NPCID.CultistBossClone, npc.whoAmI);
                    if (Main.npc.IndexInRange(clone) && clone < Main.maxNPCs)
                    {
                        Main.npc[clone].Infernum().ExtraAI[0] = npc.whoAmI;
                        cultists.Add(clone);
                    }
                }

                // Insert the true cultist into the ring at a random position.
                cultists.Insert(Main.rand.Next(cultists.Count), npc.whoAmI);

                // If for some reason only the real cultist is present at the ritual, go to a different attack immediately.
                if (cultists.Count <= 1)
                {
                    SelectNextAttack(npc);
                    return;
                }

                // Create the actual ritual.
                ritualIndex = Projectile.NewProjectile(npc.GetSource_FromAI(), ritualCenter, Vector2.Zero, ModContent.ProjectileType<Ritual>(), 0, 0f, Main.myPlayer, 0f, npc.whoAmI);

                // Prepare to fade back in.
                fadeCountdown = 18f;

                // Bring all cultists to the ritual and do some fancy shit.
                for (int i = 0; i < cultists.Count; i++)
                {
                    NPC cultist = Main.npc[cultists[i]];
                    cultist.Center = ritualCenter + (MathHelper.TwoPi * i / cultists.Count).ToRotationVector2() * 180f;
                    cultist.spriteDirection = (cultist.Center.X < ritualCenter.X).ToDirectionInt();
                    cultist.netUpdate = true;
                }
                createRitualZap(cultists);
            }

            if (attackTimer > 30f && attackTimer < waitDelay - 25f && attackTimer % 65f == 64f)
                createRitualZap();

            // Don't take damage until a bit after the ritual has started.
            npc.dontTakeDamage = attackTimer <= 90f;

            // Cancel the ritual if hit before it's complete.
            if (npc.justHit && attackTimer < waitDelay)
            {
                attackTimer = waitDelay + 1f;
                npc.netUpdate = true;
            }

            // Laugh, cause clones to fade away, and summon things to fuck with the player if they failed the ritual.
            if (attackTimer == waitDelay)
            {
                // Create a laugh sound effect.
                SoundEngine.PlaySound(SoundID.Zombie105, target.Center);

                frameType = (int)CultistFrameState.Laugh;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Point ritualCenter = (Main.projectile[(int)ritualIndex].Center + Main.projectile[(int)ritualIndex].SafeDirectionTo(target.Center) * 20f).ToPoint();
                    if (phase2)
                        NPC.NewNPC(npc.GetSource_FromAI(), ritualCenter.X, ritualCenter.Y, NPCID.CultistDragonHead, 1);
                    else
                    {
                        for (int i = 0; i < 2; i++)
                            NPC.NewNPC(npc.GetSource_FromAI(), ritualCenter.X, ritualCenter.Y, NPCID.AncientCultistSquidhead, 0, i);
                    }
                }
            }

            // Teleport above the player if too far away or after the ritual ends.
            if (attackTimer == waitDelay + 1f || (attackTimer > waitDelay + 1f && attackTimer % 45f == 44f && !npc.WithinRange(target.Center, 900f)))
            {
                Vector2 targetPosition = target.Center - Vector2.UnitY * 300f;
                CreateTeleportTelegraph(npc.Center, targetPosition, 200);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.spriteDirection = target.direction;
                    npc.Center = targetPosition;
                    npc.netUpdate = true;
                }
            }

            if (attackTimer > waitDelay + 45f)
            {
                frameType = (int)CultistFrameState.Hover;
                if (!NPC.AnyNPCs(NPCID.CultistDragonHead) && !NPC.AnyNPCs(NPCID.CultistDragonBody1) && !NPC.AnyNPCs(NPCID.CultistDragonTail))
                    SelectNextAttack(npc);
                else
                    npc.dontTakeDamage = true;
            }
        }

        public static void DoAttack_IceStorm(NPC npc, Player target, ref float frameType, ref float attackTimer)
        {
            // Release snow particles before shooting.
            if (attackTimer < 35f)
            {
                for (int i = 0; i < 5; i++)
                {
                    Vector2 dustSpawnPosition = npc.Top + Vector2.UnitY * 28f + Main.rand.NextVector2CircularEdge(50f, 50f);
                    Dust snow = Dust.NewDustPerfect(dustSpawnPosition, 221);
                    snow.velocity = -Vector2.UnitY * Main.rand.NextFloat(2f, 4f);
                    snow.scale = Main.rand.NextFloat(1.05f, 1.35f);
                    snow.fadeIn = 0.4f;
                    snow.noGravity = true;
                }
                frameType = (int)CultistFrameState.RaiseArmsUp;
            }

            if (attackTimer is 50f or 195f or 310f)
            {
                Vector2 teleportPosition = target.Center - Vector2.UnitY * 300f;
                CreateTeleportTelegraph(npc.Center, teleportPosition, 200);

                // Play an ice sound.
                SoundEngine.PlaySound(SoundID.Item120, target.position);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 iceMassSpawnPosition = npc.Top - Vector2.UnitY * 20f;
                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 shootVelocity = (target.Center - iceMassSpawnPosition).SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.TwoPi * i / 5f) * 3.2f;
                        Utilities.NewProjectileBetter(iceMassSpawnPosition, shootVelocity, ModContent.ProjectileType<IceMass>(), 190, 0f);
                    }

                    npc.Center = teleportPosition;
                    npc.netUpdate = true;
                }
            }

            if (attackTimer >= 480f)
                SelectNextAttack(npc);
        }

        public static void DoAttack_AncientDoom(NPC npc, Player target, ref float frameType, ref float attackTimer)
        {
            float attackPower = Utils.GetLerpValue(0.2f, 0.035f, npc.life / (float)npc.lifeMax, true);

            int burstCount = 2;
            int burstShootRate = (int)MathHelper.Lerp(270f, 215f, attackPower);
            ref float burstShootCounter = ref npc.Infernum().ExtraAI[0];
            ref float cycleIndex = ref npc.Infernum().ExtraAI[1];

            if (attackTimer < 30f)
                npc.Opacity = Utils.GetLerpValue(25f, 0f, attackTimer, true);
            else
                npc.Opacity = 1f;

            // Teleport and raise arms.
            if (attackTimer == 30f)
            {
                Vector2 teleportPosition = target.Center - Vector2.UnitY * 300f;
                CreateTeleportTelegraph(npc.Center, teleportPosition, 200);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.Center = teleportPosition;
                    npc.netUpdate = true;
                }

                burstShootCounter = 145f;
                frameType = (int)CultistFrameState.RaiseArmsUp;
                npc.netUpdate = true;
            }

            // Summon ancient doom NPCs and release a circle of projectiles to weave through.
            burstShootCounter++;
            if (burstShootCounter >= burstShootRate)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int doom = Utilities.NewProjectileBetter(npc.Top - Vector2.UnitY * 26f, Vector2.Zero, ModContent.ProjectileType<AncientDoom>(), 0, 0f);
                    if (Main.projectile.IndexInRange(doom))
                        Main.projectile[doom].localAI[1] = cycleIndex++ % 2;
                }

                burstShootCounter = 0f;
                npc.netUpdate = true;
            }

            if (attackTimer >= burstShootRate * (burstCount + 0.95f))
                SelectNextAttack(npc);
        }

        public static void CreateTeleportTelegraph(Vector2 start, Vector2 end, int dustCount, bool canCreateDust = true, int extraUpdates = 0)
        {
            if (canCreateDust)
            {
                for (int i = 0; i < 40; i++)
                {
                    Dust magic = Dust.NewDustPerfect(start + Main.rand.NextVector2Circular(50f, 50f), 264);
                    magic.velocity = -Vector2.UnitY * Main.rand.NextFloat(2f, 4f);
                    magic.color = Color.Blue;
                    magic.scale = 1.3f;
                    magic.fadeIn = 0.5f;
                    magic.noGravity = true;
                    magic.noLight = true;

                    magic = Dust.CloneDust(magic);
                    magic.position = end + Main.rand.NextVector2Circular(50f, 50f);
                }
            }

            for (int i = 0; i < dustCount; i++)
            {
                Vector2 dustDrawPosition = Vector2.Lerp(start, end, i / (float)dustCount);

                Dust magic = Dust.NewDustPerfect(dustDrawPosition, 267);
                magic.velocity = -Vector2.UnitY * Main.rand.NextFloat(0.2f, 0.235f);
                magic.color = Color.LightCyan;
                magic.color.A = 0;
                magic.scale = 0.8f;
                magic.fadeIn = 1.4f;
                magic.noGravity = true;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 6; i++)
            {
                int laser = Projectile.NewProjectile(new EntitySource_WorldEvent(), start, Vector2.Zero, ModContent.ProjectileType<TeleportTelegraph>(), 0, 0f);
                Main.projectile[laser].ai[0] = (!canCreateDust).ToInt();
                Main.projectile[laser].timeLeft -= i * 2;

                if (extraUpdates > 0)
                    Main.projectile[laser].extraUpdates = extraUpdates;

                laser = Projectile.NewProjectile(new EntitySource_WorldEvent(), end, Vector2.Zero, ModContent.ProjectileType<TeleportTelegraph>(), 0, 0f);
                Main.projectile[laser].ai[0] = (!canCreateDust).ToInt();
                Main.projectile[laser].timeLeft -= i * 2;

                if (extraUpdates > 0)
                    Main.projectile[laser].extraUpdates = extraUpdates;
            }
        }

        public static void SelectNextAttack(NPC npc)
        {
            npc.alpha = 0;
            npc.TargetClosest();
            bool phase2 = npc.life < npc.lifeMax * Phase2LifeRatio;
            bool phase3 = npc.life < npc.lifeMax * Phase3LifeRatio;
            CultistAIState oldAttackState = (CultistAIState)(int)npc.ai[0];
            CultistAIState newAttackState = CultistAIState.FireballBarrage;

            switch (oldAttackState)
            {
                case CultistAIState.SpawnEffects:
                    newAttackState = CultistAIState.FireballBarrage;
                    break;

                case CultistAIState.FireballBarrage:
                    newAttackState = CultistAIState.LightningHover;
                    break;
                case CultistAIState.LightningHover:
                    newAttackState = CultistAIState.ConjureLightBlasts;
                    break;
                case CultistAIState.ConjureLightBlasts:
                    newAttackState = CultistAIState.Ritual;
                    break;
                case CultistAIState.Ritual:
                    newAttackState = phase2 ? CultistAIState.IceStorm : CultistAIState.FireballBarrage;
                    break;
                case CultistAIState.IceStorm:
                    newAttackState = phase3 ? CultistAIState.AncientDoom : CultistAIState.FireballBarrage;
                    break;
                case CultistAIState.AncientDoom:
                    newAttackState = CultistAIState.FireballBarrage;
                    break;
            }

            npc.ai[0] = (int)newAttackState;
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
        }

        #endregion AI

        #region Drawing and Frames

        public override void FindFrame(NPC npc, int frameHeight)
        {
            int frameCount = Main.npcFrameCount[npc.type];
            switch ((CultistFrameState)(int)npc.localAI[0])
            {
                case CultistFrameState.AbsorbEffect:
                    npc.frame.Y = (int)(npc.frameCounter / 5) * frameHeight;
                    if (npc.frameCounter >= 18)
                        npc.frameCounter = 18;
                    break;

                case CultistFrameState.Hover:
                    npc.frame.Y = (int)(4 + npc.frameCounter / 5) * frameHeight;
                    if (npc.frameCounter >= 14)
                        npc.frameCounter = 0;
                    break;

                case CultistFrameState.RaiseArmsUp:
                    npc.frame.Y = (int)(frameCount - 9 + npc.frameCounter / 5) * frameHeight;
                    if (npc.frameCounter >= 14)
                        npc.frameCounter = 0;
                    break;

                case CultistFrameState.HoldArmsOut:
                    npc.frame.Y = (int)(frameCount - 6 + npc.frameCounter / 5) * frameHeight;
                    if (npc.frameCounter >= 14)
                        npc.frameCounter = 0;
                    break;

                case CultistFrameState.Laugh:
                    npc.frame.Y = (int)(frameCount - 3 + npc.frameCounter / 5) * frameHeight;
                    if (npc.frameCounter >= 14)
                        npc.frameCounter = 0;
                    break;
            }

            npc.frameCounter++;
        }

        public static void ExtraDrawcode(NPC npc, SpriteBatch spriteBatch)
        {
            float frameState = npc.ai[2];
            float transitionTimer = npc.ai[3];
            Texture2D cultistTexture = TextureAssets.Npc[npc.type].Value;

            if (frameState == 1f)
            {
                // Create a circle of illusions that fade in and collapse on the cultist.
                for (int i = 0; i < 8; i++)
                {
                    float colorInterpolant = (Main.GlobalTimeWrappedHourly * 0.53f + i / 8f) % 1f;
                    Color solarColor = new(255, 93, 30);
                    Color nebulaColor = new(232, 76, 183);
                    Color vortexColor = new(6, 229, 156);
                    Color stardustColor = new(0, 170, 221);
                    Color illusionColor = CalamityUtils.MulticolorLerp(colorInterpolant, solarColor, nebulaColor, vortexColor, stardustColor);
                    float illusionOpacity = Utils.GetLerpValue(0f, 32f, transitionTimer, true) *
                        Utils.GetLerpValue(TransitionAnimationTime - 4f, TransitionAnimationTime - 32f, transitionTimer, true) * npc.Opacity * 0.6f;
                    illusionColor.A = (byte)MathHelper.Lerp(125f, 0f, 1f - illusionOpacity);

                    Vector2 drawOffset = (MathHelper.TwoPi * i / 8f + MathHelper.TwoPi * 2f / TransitionAnimationTime).ToRotationVector2();
                    drawOffset = drawOffset.RotatedBy(MathHelper.TwoPi * transitionTimer / TransitionAnimationTime);
                    drawOffset *= MathHelper.Lerp(0f, 200f, Utils.GetLerpValue(TransitionAnimationTime - 10f, 0f, transitionTimer, true));
                    Vector2 drawPosition = npc.Center + drawOffset - Main.screenPosition;
                    SpriteEffects direction = (drawOffset.X < 0f) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

                    Main.spriteBatch.Draw(cultistTexture, drawPosition, npc.frame, illusionColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);
                }
            }

            float glowOpacity = 0f;
            if (frameState == 2f)
                glowOpacity = 1f;
            else if (frameState == 1f)
                glowOpacity = Utils.GetLerpValue(TransitionAnimationTime, TransitionAnimationTime + 15f, transitionTimer, true);

            // Create an afterimage glow in phase 2.
            for (int i = 0; i < 8; i++)
            {
                Color glowColor = Color.Cyan * glowOpacity * 0.45f;
                glowColor.A = 0;

                Vector2 drawPosition = npc.Center - Main.screenPosition;
                Vector2 drawOffset = (MathHelper.TwoPi * i / 8f + Main.GlobalTimeWrappedHourly * 4f).ToRotationVector2();
                drawOffset *= MathHelper.Lerp(4f, 5f, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 1.4f) * 0.5f + 0.5f);
                drawPosition += drawOffset;
                SpriteEffects direction = npc.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

                Main.spriteBatch.Draw(cultistTexture, drawPosition, npc.frame, glowColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);
            }
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            NPCID.Sets.MustAlwaysDraw[npc.type] = true;

            // Draw borders.
            bool dying = npc.Infernum().ExtraAI[6] == 1f;
            Texture2D borderTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Cultist/Border").Value;
            float initialXPosition = npc.Infernum().ExtraAI[8];
            float left = initialXPosition - BorderWidth * 0.5f;
            float right = initialXPosition + BorderWidth * 0.5f;
            float leftBorderOpacity = Utils.GetLerpValue(left + 850f, left + 300f, Main.LocalPlayer.Center.X, true);
            float rightBorderOpacity = Utils.GetLerpValue(right - 850f, right - 300f, Main.LocalPlayer.Center.X, true);

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            if (leftBorderOpacity > 0f && !dying)
            {
                Vector2 baseDrawPosition = new Vector2(left, Main.LocalPlayer.Center.Y) - Main.screenPosition;
                float borderOutwardness = Utils.GetLerpValue(0f, 0.9f, leftBorderOpacity, true) * MathHelper.Lerp(400f, 455f, (float)Math.Cos(Main.GlobalTimeWrappedHourly * 4.4f) * 0.5f + 0.5f);
                Color borderColor = Color.Lerp(Color.Transparent, Color.DeepSkyBlue, leftBorderOpacity);

                for (int i = 0; i < 80; i++)
                {
                    float fade = 1f - Math.Abs(i - 40f) / 40f;
                    Vector2 drawPosition = baseDrawPosition + Vector2.UnitY * (i - 40f) / 40f * borderOutwardness;
                    Main.spriteBatch.Draw(borderTexture, drawPosition, null, Color.Lerp(borderColor, Color.Purple, 1f - fade) * fade, 0f, borderTexture.Size() * 0.5f, new Vector2(0.33f, 1f), SpriteEffects.None, 0f);
                }
                Main.spriteBatch.Draw(borderTexture, baseDrawPosition, null, Color.Lerp(borderColor, Color.Purple, 0.5f), 0f, borderTexture.Size() * 0.5f, new Vector2(0.33f, 1f), SpriteEffects.None, 0f);
            }

            if (rightBorderOpacity > 0f && !dying)
            {
                Vector2 baseDrawPosition = new Vector2(right, Main.LocalPlayer.Center.Y) - Main.screenPosition;
                float borderOutwardness = Utils.GetLerpValue(0f, 0.9f, rightBorderOpacity, true) * MathHelper.Lerp(400f, 455f, (float)Math.Cos(Main.GlobalTimeWrappedHourly * 4.4f) * 0.5f + 0.5f);
                Color borderColor = Color.Lerp(Color.Transparent, Color.DeepSkyBlue, rightBorderOpacity);

                for (int i = 0; i < 80; i++)
                {
                    float fade = 1f - Math.Abs(i - 40f) / 40f;
                    Vector2 drawPosition = baseDrawPosition + Vector2.UnitY * (i - 40f) / 40f * borderOutwardness;
                    Main.spriteBatch.Draw(borderTexture, drawPosition, null, Color.Lerp(borderColor, Color.Purple, 1f - fade) * fade, 0f, borderTexture.Size() * 0.5f, new Vector2(0.33f, 1f), SpriteEffects.FlipHorizontally, 0f);
                }
                Main.spriteBatch.Draw(borderTexture, baseDrawPosition, null, Color.Lerp(borderColor, Color.Purple, 0.5f), 0f, borderTexture.Size() * 0.5f, new Vector2(0.33f, 1f), SpriteEffects.FlipHorizontally, 0f);
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            float deathTimer = npc.Infernum().ExtraAI[7];
            if (!dying)
                ExtraDrawcode(npc, spriteBatch);
            else if (deathTimer > 120f)
            {
                Main.spriteBatch.EnterShaderRegion();
                GameShaders.Misc["Infernum:CultistDeath"].UseOpacity((1f - Utils.GetLerpValue(120f, 305f, deathTimer, true)) * 0.8f);
                GameShaders.Misc["Infernum:CultistDeath"].UseImage1("Images/Misc/Perlin");
                GameShaders.Misc["Infernum:CultistDeath"].Apply();
            }

            bool performingRitual = npc.ai[0] == (int)CultistAIState.Ritual && npc.ai[1] >= 30f && !npc.dontTakeDamage;
            Texture2D baseTexture = TextureAssets.Npc[npc.type].Value;
            Rectangle frame = npc.frame;
            if (performingRitual)
            {
                baseTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Cultist/CultistLaughFrames").Value;
                frame = baseTexture.Frame(1, 3, 0, (int)(npc.frameCounter / 14f % 1f * 3f));
            }

            SpriteEffects direction = npc.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Main.spriteBatch.Draw(baseTexture, npc.Center - Main.screenPosition, frame, npc.GetAlpha(lightColor), npc.rotation, frame.Size() * 0.5f, npc.scale, direction, 0f);

            if (deathTimer > 120f)
                Main.spriteBatch.ExitShaderRegion();
            return false;
        }

        #endregion Drawing and Frames
    }
}
