using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs.Bumblebirb;
using CalamityMod.Projectiles.Boss;
using InfernumMode.BehaviorOverrides.BossAIs.Twins;
using InfernumMode.OverridingSystem;
using InfernumMode.Projectiles;
using InfernumMode.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Dragonfolly
{
    public class DragonfollyBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<Bumblefuck>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw | NPCOverrideContext.NPCFindFrame;

        #region Attacks and Frame Enumeration
        public enum DragonfollyAttackType
        {
            SpawnEffects,
            FeatherSpreadRelease,
            OrdinaryCharge,
            FakeoutCharge,
            ThunderCharge,
            SummonSwarmers,
            NormalLightningAura,
            PlasmaBursts,
            ElectricOverload,
            RuffleFeathers,
            ExplodingEnergyOrbs,
            LightningSupercharge
        }

        public enum DragonfollyFrameDrawingType
        {
            FlapWings,
            Screm
        }

        public static DragonfollyAttackType[] Phase1AttackCycle => new DragonfollyAttackType[]
        {
            DragonfollyAttackType.FeatherSpreadRelease,
            DragonfollyAttackType.OrdinaryCharge,

            DragonfollyAttackType.PlasmaBursts,
            DragonfollyAttackType.FakeoutCharge,

            DragonfollyAttackType.FeatherSpreadRelease,
            DragonfollyAttackType.OrdinaryCharge,

            DragonfollyAttackType.NormalLightningAura,
            DragonfollyAttackType.ThunderCharge,
        };

        public static DragonfollyAttackType[] Phase2AttackCycle => new DragonfollyAttackType[]
        {
            DragonfollyAttackType.PlasmaBursts,
            DragonfollyAttackType.FakeoutCharge,

            DragonfollyAttackType.FeatherSpreadRelease,
            DragonfollyAttackType.OrdinaryCharge,

            DragonfollyAttackType.ExplodingEnergyOrbs,
            DragonfollyAttackType.ThunderCharge,

            DragonfollyAttackType.NormalLightningAura,
            DragonfollyAttackType.FakeoutCharge,

            DragonfollyAttackType.FeatherSpreadRelease,
            DragonfollyAttackType.ThunderCharge,

            DragonfollyAttackType.RuffleFeathers,
            DragonfollyAttackType.OrdinaryCharge,
        };

        public static DragonfollyAttackType[] Phase3AttackCycle => new DragonfollyAttackType[]
        {
            DragonfollyAttackType.PlasmaBursts,
            DragonfollyAttackType.FakeoutCharge,

            DragonfollyAttackType.LightningSupercharge,
            DragonfollyAttackType.OrdinaryCharge,

            DragonfollyAttackType.PlasmaBursts,
            DragonfollyAttackType.FakeoutCharge,

            DragonfollyAttackType.RuffleFeathers,
            DragonfollyAttackType.ThunderCharge,

            DragonfollyAttackType.ExplodingEnergyOrbs,
            DragonfollyAttackType.OrdinaryCharge,

            DragonfollyAttackType.ElectricOverload,
            DragonfollyAttackType.FakeoutCharge,

            DragonfollyAttackType.FeatherSpreadRelease,
            DragonfollyAttackType.ThunderCharge,

            DragonfollyAttackType.RuffleFeathers,
            DragonfollyAttackType.OrdinaryCharge,
        };

        #endregion

        #region AI

        public const int TransitionTime = ScreamTime + 15;

        public const float Phase2LifeRatio = 0.75f;

        public const float Phase3LifeRatio = 0.3333f;

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio,
            Phase3LifeRatio
        };

        public override bool PreAI(NPC npc)
        {
            DetermineTarget(npc, out bool despawning);
            if (despawning)
                return false;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool phase2 = lifeRatio < Phase2LifeRatio;
            bool phase3 = lifeRatio < Phase3LifeRatio;
            Player target = Main.player[npc.target];
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float chargeType = ref npc.ai[2];
            ref float lastChargeType = ref npc.ai[3];
            ref float frameType = ref npc.localAI[0];
            ref float flapRate = ref npc.localAI[1];
            ref float fadeToRed = ref npc.localAI[2];
            ref float previousPhase = ref npc.Infernum().ExtraAI[5];
            ref float phaseTransitionCountdown = ref npc.Infernum().ExtraAI[6];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[7];
            ref float backgroundFadeToRed = ref npc.Infernum().ExtraAI[8];
            ref float phase2TransitionCountdown = ref npc.Infernum().ExtraAI[9];

            // Go to the next phases.
            if (previousPhase == 0f && phase2)
            {
                phaseTransitionCountdown = TransitionTime;
                phase2TransitionCountdown = 600f;
                previousPhase = 1f;
                npc.ai[3] = 0f;

                // Piss off any remaining swarmers.
                int swarmerType = ModContent.NPCType<Bumblefuck2>();
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (!Main.npc[i].active || Main.npc[i].type != swarmerType)
                        continue;

                    Main.npc[i].ai[0] = 3f;
                    Main.npc[i].ai[1] = 0f;
                    Main.npc[i].ai[2] = 0f;
                    Main.npc[i].ai[3] = 0f;
                    Main.npc[i].netUpdate = true;
                }

                npc.netUpdate = true;
            }
            if (previousPhase == 1f && phase3)
            {
                chargeCounter = 0f;
                phaseTransitionCountdown = TransitionTime;
                previousPhase = 2f;
                npc.ai[3] = 0f;

                // Piss off any remaining swarmers.
                int swarmerType = ModContent.NPCType<Bumblefuck2>();
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (!Main.npc[i].active || Main.npc[i].type != swarmerType)
                        continue;

                    Main.npc[i].ai[0] = 3f;
                    Main.npc[i].ai[1] = 0f;
                    Main.npc[i].ai[2] = 0f;
                    Main.npc[i].ai[3] = 2f;
                    Main.npc[i].netUpdate = true;
                }

                npc.netUpdate = true;
            }

            if (phase2TransitionCountdown > 0)
            {
                npc.Calamity().DR = MathHelper.SmoothStep(0.1f, 0.35f, phase2TransitionCountdown / 600f);
                npc.defense = (int)MathHelper.SmoothStep(npc.defDefense, 100f, phase2TransitionCountdown / 600f);
                if (phase2)
                    npc.damage = (int)MathHelper.SmoothStep(npc.defDamage * 1.1f, npc.defDamage * 1.4f, phase2TransitionCountdown / 600f);
            }

            if (phaseTransitionCountdown > 0)
            {
                npc.Opacity = MathHelper.Lerp(npc.Opacity, 1f, 0.185f);
                npc.velocity *= 0.95f;
                npc.rotation *= 0.95f;
                frameType = (int)DragonfollyFrameDrawingType.Screm;
                phaseTransitionCountdown--;
                return false;
            }

            npc.damage = npc.defDamage;

            switch ((DragonfollyAttackType)(int)attackType)
            {
                case DragonfollyAttackType.SpawnEffects:
                    DoAttack_SpawnEffects(npc, target, attackTimer, ref fadeToRed, ref frameType, ref flapRate);
                    break;
                case DragonfollyAttackType.OrdinaryCharge:
                case DragonfollyAttackType.FakeoutCharge:
                case DragonfollyAttackType.ThunderCharge:
                    DoAttack_Charge(npc, target, (DragonfollyAttackType)(int)attackType, phase2, phase3, ref fadeToRed, ref attackTimer, ref frameType, ref flapRate);
                    break;

                // Currently unused to attack overlap problems.
                case DragonfollyAttackType.SummonSwarmers:
                    DoAttack_SummonSwarmers(npc, target, phase2, phase3, ref attackTimer, ref frameType, ref flapRate);
                    break;

                case DragonfollyAttackType.NormalLightningAura:
                    DoAttack_CreateNormalLightningAura(npc, target, ref attackTimer, ref frameType, ref flapRate);
                    break;
                case DragonfollyAttackType.FeatherSpreadRelease:
                    DoAttack_FeatherSpreadRelease(npc, target, ref attackTimer, ref frameType, ref flapRate);
                    break;
                case DragonfollyAttackType.PlasmaBursts:
                    DoAttack_ReleasePlasmaBursts(npc, target, ref attackTimer, ref fadeToRed, ref frameType, ref flapRate);
                    break;
                case DragonfollyAttackType.ElectricOverload:
                    DoAttack_ElectricOverload(npc, target, ref attackTimer, ref frameType, ref flapRate);
                    break;
                case DragonfollyAttackType.RuffleFeathers:
                    DoAttack_RuffleFeathers(npc, target, phase3, ref attackTimer, ref frameType, ref flapRate);
                    break;
                case DragonfollyAttackType.ExplodingEnergyOrbs:
                    DoAttack_ExplodingEnergyOrbs(npc, target, phase3, ref attackTimer, ref frameType, ref flapRate);
                    break;
                case DragonfollyAttackType.LightningSupercharge:
                    DoAttack_LightningSupercharge(npc, target, ref attackTimer, ref fadeToRed, ref frameType, ref flapRate);
                    break;
            }

            // Cause the background red to wane when not doing an electric overload.
            if ((DragonfollyAttackType)(int)attackType != DragonfollyAttackType.ElectricOverload)
            {
                backgroundFadeToRed *= 0.98f;
                backgroundFadeToRed = MathHelper.Clamp(backgroundFadeToRed - 0.025f, 0f, 1f);
            }

            attackTimer++;
            return false;
        }

        public static void DetermineTarget(NPC npc, out bool despawning)
        {
            despawning = false;

            // Aquire a new target if the current one is dead or inactive.
            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
            {
                npc.TargetClosest(false);

                // If no possible target was found, fly away.
                if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                {
                    npc.velocity.Y -= 0.5f;
                    npc.rotation = npc.rotation.AngleLerp(0f, 0.25f);
                    if (npc.timeLeft > 90)
                        npc.timeLeft = 90;
                    despawning = true;
                }
                npc.netUpdate = true;
            }
            else
                npc.timeLeft = 7200;
        }

        public static void SelectNextAttack(NPC npc)
        {
            npc.alpha = 0;

            int attackCycleIndex = (int)npc.ai[3];
            DragonfollyAttackType newAttackState = Phase1AttackCycle[attackCycleIndex % Phase1AttackCycle.Length];
            if (npc.life < npc.lifeMax * Phase2LifeRatio)
                newAttackState = Phase2AttackCycle[attackCycleIndex % Phase2AttackCycle.Length];
            if (npc.life < npc.lifeMax * Phase3LifeRatio)
                newAttackState = Phase3AttackCycle[attackCycleIndex % Phase3AttackCycle.Length];

            npc.TargetClosest();
            npc.ai[0] = (int)newAttackState;
            npc.ai[1] = 0f;
            npc.ai[3]++;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.noTileCollide = true;
            npc.netUpdate = true;
        }

        #region Specific Attacks
        public static void DoAttack_SpawnEffects(NPC npc, Player target, float attackTimer, ref float fadeToRed, ref float frameType, ref float flapRate)
        {
            int chargeDelay = 30;
            if (attackTimer <= 1f)
                npc.Opacity = 0f;

            frameType = (int)DragonfollyFrameDrawingType.FlapWings;
            flapRate = (int)Utils.Clamp(8f - npc.velocity.Length() * 0.125f, 4f, 8f);

            if (attackTimer <= 45f)
            {
                npc.Opacity = Utils.GetLerpValue(25f, 45f, attackTimer, true);
                npc.Center = Vector2.SmoothStep(npc.Center, target.Center - Vector2.UnitY * 1350f, (float)Math.Pow(attackTimer / 45f, 3D));
                npc.spriteDirection = (npc.Center.X - target.Center.X < 0).ToDirectionInt();
                flapRate = 7;
            }

            // Release a bunch of feathers that aim towards the player.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 45f)
            {
                for (float offsetAngle = -1.21f; offsetAngle <= 1.21f; offsetAngle += 0.1f)
                {
                    Vector2 spawnPosition = target.Center - Vector2.UnitY.RotatedBy(offsetAngle) * 800f;
                    Vector2 shootDirection = target.DirectionFrom(spawnPosition) * 0.001f;
                    Utilities.NewProjectileBetter(spawnPosition, shootDirection, ModContent.ProjectileType<RedLightningSnipeFeather>(), 300, 0f);
                }
            }
            if (attackTimer >= 150f)
            {
                // Teleport to a side of the player.
                if (attackTimer == 150f)
                {
                    npc.Center = target.Center + Vector2.UnitX * (npc.Center.X > target.Center.X).ToDirectionInt() * 1750f;
                    npc.netUpdate = true;
                }

                // Charge.
                if (attackTimer == 150f + chargeDelay)
                {
                    npc.velocity = npc.SafeDirectionTo(target.Center) * 33f;
                    npc.netUpdate = true;
                }

                // And do specific things after charging.
                if (attackTimer >= 150f + chargeDelay)
                {
                    npc.rotation = (npc.rotation * 7f + npc.velocity.X * 0.01f) / 8f;

                    // Fade to red and back depending on how much time is left in the charge.
                    fadeToRed = MathHelper.Lerp(fadeToRed, attackTimer >= 205f + chargeDelay ? 0f : 1f, 0.15f);

                    // Release lightning clouds from time to time while charging.
                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 6f == 5f)
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<LightningCloud>(), 0, 0f);
                }
                if (attackTimer >= 230f + chargeDelay)
                {
                    npc.velocity *= 0.96f;
                    if (attackTimer >= 305f + chargeDelay)
                        SelectNextAttack(npc);
                }

                npc.alpha = Utils.Clamp(npc.alpha - 25, 0, 255);
            }

            if (Math.Abs(npc.velocity.X) > 0.8f)
                npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
            npc.noTileCollide = true;
        }
        public static void DoAttack_Charge(NPC npc, Player target, DragonfollyAttackType chargeType, bool phase2, bool phase3, ref float fadeToRed, ref float attackTimer, ref float frameType, ref float flapRate)
        {
            npc.noTileCollide = true;

            float horizontalOffset = 550f;
            switch (chargeType)
            {
                case DragonfollyAttackType.FakeoutCharge:
                    horizontalOffset = 650f;
                    break;
                case DragonfollyAttackType.ThunderCharge:
                    horizontalOffset = 1050f;
                    break;
            }

            // Delete plasma orbs during thunder charges.
            if (chargeType == DragonfollyAttackType.ThunderCharge)
            {
                int plasmaOrbType = ModContent.NPCType<RedPlasmaEnergy>();
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].type != plasmaOrbType || !Main.npc[i].active)
                        continue;

                    Main.npc[i].life = 0;
                    Main.npc[i].active = false;
                    Main.npc[i].HitEffect();
                    Main.npc[i].netUpdate = true;
                }
            }

            ref float chargeState = ref npc.Infernum().ExtraAI[0];
            ref float accumulatedSpeed = ref npc.Infernum().ExtraAI[1];
            ref float chargeDirection = ref npc.Infernum().ExtraAI[2];
            ref float hasDoneFakeoutFlag = ref npc.Infernum().ExtraAI[3];

            // Phase 2-3 exclusive.
            int totalRedirects = phase3 ? 2 : 1;
            ref float redirectCounter = ref npc.Infernum().ExtraAI[4];

            // Line up for charge.
            if (chargeState == 0f)
            {
                npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();

                float verticalOffsetLeniance = 65f;
                float flySpeed = 18.5f + accumulatedSpeed;
                float flyInertia = 4f;
                if (BossRushEvent.BossRushActive)
                    flySpeed += 7f;

                Vector2 destination = target.Center - Vector2.UnitX * Math.Sign(target.Center.X - npc.Center.X) * horizontalOffset;

                // Fly towards the destination beside the player.
                npc.velocity = (npc.velocity * (flyInertia - 1f) + npc.SafeDirectionTo(destination) * flySpeed) / flyInertia;

                // If within a good approximation of the player's position, prepare charging.
                if (Math.Abs(npc.Center.X - target.Center.X) > horizontalOffset - 50f && Math.Abs(npc.Center.Y - target.Center.Y) < verticalOffsetLeniance)
                {
                    chargeState = 1f;
                    accumulatedSpeed = 0f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }

                // Become more and more fast the more time has passed.
                // (Why does this comment sound funny to me?)
                accumulatedSpeed += 0.035f;
            }

            // Prepare for the charge.
            else if (chargeState == 1f)
            {
                int chargeDelay = chargeType == DragonfollyAttackType.ThunderCharge ? 45 : 20;
                if (chargeType == DragonfollyAttackType.OrdinaryCharge && phase2 && redirectCounter > 0f)
                    chargeDelay = 6;

                float flySpeed = BossRushEvent.BossRushActive ? 40f : 32f;
                float flyInertia = 8f;
                Vector2 chargeVelocity = npc.SafeDirectionTo(target.Center) * flySpeed;
                npc.velocity = (npc.velocity * (flyInertia - 1f) + chargeVelocity * (chargeType == DragonfollyAttackType.ThunderCharge ? 0.67f : 1f)) / flyInertia;
                npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();

                if (attackTimer >= chargeDelay)
                {
                    attackTimer = 0f;
                    chargeState = 2f;
                    npc.velocity = chargeVelocity;
                    npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
                    chargeDirection = npc.spriteDirection;
                    npc.netUpdate = true;

                    // Make a diving sound.
                    SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown, npc.Center);

                    // Release some feathers into the air.
                    for (int i = 0; i < Main.rand.Next(4, 8 + 1); i++)
                    {
                        Vector2 featherVelocity = Main.rand.NextVector2Circular(12f, 3f);
                        featherVelocity.Y = -Math.Abs(featherVelocity.Y);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + Main.rand.NextVector2CircularEdge(50f, 50f), featherVelocity, ModContent.ProjectileType<FollyFeather>(), 0, 0f);
                    }

                    // If in phase 2 and doing a lightning attack, release an aura from the mouth that goes towards the player.
                    if (chargeType == DragonfollyAttackType.ThunderCharge && phase2)
                    {
                        SoundEngine.PlaySound(SoundID.DD2_BetsyScream with { Pitch = 0.25f }, npc.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 mouthPosition = npc.Center + Vector2.UnitX * npc.direction * (float)Math.Cos(npc.rotation) * (npc.width * 0.5f + 36f);
                            if (phase3)
                            {
                                Vector2 baseShootVelocity = npc.SafeDirectionTo(mouthPosition) * 7f;
                                Projectile.NewProjectile(npc.GetSource_FromAI(), mouthPosition, baseShootVelocity.RotatedBy(-0.36f), ModContent.ProjectileType<BirbThunderAuraFlare>(), 0, 0f, Main.myPlayer, 2f, npc.target + 1);
                                Projectile.NewProjectile(npc.GetSource_FromAI(), mouthPosition, baseShootVelocity.RotatedBy(0.36f), ModContent.ProjectileType<BirbThunderAuraFlare>(), 0, 0f, Main.myPlayer, 2f, npc.target + 1);
                            }
                            else
                                Projectile.NewProjectile(npc.GetSource_FromAI(), mouthPosition, Vector2.Zero, ModContent.ProjectileType<BirbThunderAuraFlare>(), 0, 0f, Main.myPlayer, 2f, npc.target + 1);
                        }
                    }
                }
            }

            // Do the actual charge.
            else if (chargeState == 2f)
            {
                float horizontalSpeed = MathHelper.Lerp(31f, 43.5f, 1f - npc.life / (float)npc.lifeMax);

                // Fly faster than usual after a fakeout.
                if (hasDoneFakeoutFlag == 1f && chargeType == DragonfollyAttackType.FakeoutCharge)
                    horizontalSpeed += 9f;

                accumulatedSpeed += phase3 ? 0.08f : 0.04f;
                npc.velocity.X = chargeDirection * (horizontalSpeed + accumulatedSpeed);

                float offsetRemoval = chargeType == DragonfollyAttackType.ThunderCharge ? -80f : 210f;
                bool farEnoughAwayFromPlayer = chargeDirection > 0f && npc.Center.X > target.Center.X + (horizontalOffset - offsetRemoval);
                farEnoughAwayFromPlayer |= chargeDirection < 0f && npc.Center.X < target.Center.X - (horizontalOffset - offsetRemoval);

                if (farEnoughAwayFromPlayer)
                {
                    if (redirectCounter < totalRedirects && chargeType == DragonfollyAttackType.OrdinaryCharge)
                    {
                        chargeState = 1f;
                        attackTimer = 0f;
                        redirectCounter++;
                        npc.netUpdate = true;
                    }

                    npc.velocity *= chargeType != DragonfollyAttackType.ThunderCharge ? 0.3f : 0.6f;
                    SelectNextAttack(npc);
                }

                // Release lightning clouds from time to time while charging if doing a lightning charge.
                int cloudSpawnRate = (int)MathHelper.Lerp(8f, 4f, 1f - npc.life / (float)npc.lifeMax);
                float cloudOffsetAngle = npc.velocity.X * 0.01f;
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % cloudSpawnRate == cloudSpawnRate - 1f && chargeType == DragonfollyAttackType.ThunderCharge)
                {
                    Vector2 cloudSpawnPosition = npc.Center - npc.velocity * 2f;
                    int cloud = Projectile.NewProjectile(npc.GetSource_FromAI(), cloudSpawnPosition, Vector2.Zero, ModContent.ProjectileType<LightningCloud>(), 0, 0f);
                    if (Main.projectile.IndexInRange(cloud))
                        Main.projectile[cloud].ModProjectile<LightningCloud>().AngularOffset = cloudOffsetAngle;
                }

                if (hasDoneFakeoutFlag == 0f && chargeType == DragonfollyAttackType.FakeoutCharge)
                {
                    // Fade out for the fake out.
                    if (npc.alpha < 255)
                    {
                        // Turn red as a telegraph for a short moment.
                        fadeToRed = (float)Math.Sin(Utils.GetLerpValue(0f, 18f, attackTimer, true) * MathHelper.Pi);

                        npc.alpha = Utils.Clamp(npc.alpha + 10, 0, 255);
                        npc.damage = 0;
                    }

                    // After completely fading out, teleport to the other side of the player,
                    else
                    {
                        fadeToRed = 0f;
                        chargeState = 1f;
                        attackTimer = 0f;
                        hasDoneFakeoutFlag = 1f;
                        npc.Center = target.Center + Vector2.UnitX * horizontalOffset * (target.Center.X < npc.Center.X).ToDirectionInt();

                        // Charge diagonally in phase 2.
                        if (phase2)
                            npc.position.Y -= 475f;

                        npc.netUpdate = true;
                    }
                }

                // Rapidly fade in and slow down a bit if doing the fakeout charge.
                if (hasDoneFakeoutFlag == 1f && chargeType == DragonfollyAttackType.FakeoutCharge)
                {
                    npc.alpha = Utils.Clamp(npc.alpha - 45, 0, 255);
                    npc.velocity *= 0.985f;
                }
            }

            frameType = (int)DragonfollyFrameDrawingType.FlapWings;
            flapRate = (int)Utils.Clamp(8f - npc.velocity.Length() * 0.125f, 4f, 8f);
            npc.rotation = (npc.rotation * 7f + npc.velocity.X * 0.01f) / 8f;
        }

        public static void DoAttack_SummonSwarmers(NPC npc, Player target, bool phase2, bool phase3, ref float attackTimer, ref float frameType, ref float flapRate)
        {
            npc.rotation = npc.rotation.AngleLerp(0f, 0.125f);
            npc.rotation = npc.rotation.AngleTowards(0f, 0.125f);
            npc.noTileCollide = true;

            int maxSwarmersAtOnce = phase2 ? 3 : 2;
            ref float attackState = ref npc.Infernum().ExtraAI[0];
            ref float originalSwamerCount = ref npc.Infernum().ExtraAI[1];
            if (originalSwamerCount == 0f)
            {
                originalSwamerCount = NPC.CountNPCS(ModContent.NPCType<Bumblefuck2>());

                // Don't bother doing this attack if the swarmer count is already at the limit.
                if (originalSwamerCount >= maxSwarmersAtOnce)
                    SelectNextAttack(npc);

                npc.netUpdate = true;
            }

            // Fly near the target.
            if (attackState == 0f)
            {
                frameType = (int)DragonfollyFrameDrawingType.FlapWings;
                flapRate = 5f;

                npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center - Vector2.UnitY * 200f, -Vector2.UnitY) * 21f, 0.15f);
                npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();

                // If somewhat close to the target or enough time has passed, begin summoning swarmers.
                if (npc.WithinRange(target.Center, 600f) || attackTimer >= 180f)
                {
                    attackState = 1f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            // Scream and summon swarmers.
            else if (attackState == 1f)
            {
                frameType = (int)DragonfollyFrameDrawingType.Screm;

                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.Zero, 0.04f);
                if (npc.velocity.Length() < 0.8f)
                    npc.velocity = Vector2.Zero;

                // Create swarmers around the dragonfolly.
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == ScreamSoundDelay)
                {
                    int remainingPossibleSummons = maxSwarmersAtOnce - (int)originalSwamerCount;
                    int totalSwarmersToSummon = 2;
                    if (phase2 && remainingPossibleSummons >= 2 && Main.rand.NextBool(2))
                        totalSwarmersToSummon = 3;

                    for (int i = 0; i < totalSwarmersToSummon; i++)
                    {
                        Vector2 potentialSpawnPosition = npc.Center + Main.rand.NextVector2Unit() * new Vector2(100f, 85f) * Main.rand.NextFloat(0.6f, 1f);

                        // Ensure that the spawn position is not near the target, to prevent potentially unfair hits.
                        if (!target.WithinRange(potentialSpawnPosition, 160f))
                        {
                            int swarmer = NPC.NewNPC(npc.GetSource_FromAI(), (int)potentialSpawnPosition.X, (int)potentialSpawnPosition.Y, ModContent.NPCType<Bumblefuck2>(), npc.whoAmI);
                            Main.npc[swarmer].ai[3] = phase2.ToInt() + phase3.ToInt();
                            Main.npc[swarmer].velocity = Vector2.UnitY * -12f;
                        }
                    }
                }

                if (attackTimer > ScreamTime + 8f)
                {
                    frameType = (int)DragonfollyFrameDrawingType.FlapWings;
                    flapRate = 5f;
                }

                if (attackTimer >= ScreamTime + 25f)
                    SelectNextAttack(npc);
            }
        }

        public static void DoAttack_CreateNormalLightningAura(NPC npc, Player target, ref float attackTimer, ref float frameType, ref float flapRate)
        {
            npc.velocity *= 0.96f;
            npc.rotation *= 0.95f;

            int shootDelay = 75;

            if (attackTimer >= shootDelay - ScreamSoundDelay)
                frameType = (int)DragonfollyFrameDrawingType.Screm;

            // Terminate the attack early if an aura or flare already exists.
            if (attackTimer < shootDelay)
            {
                if (Utilities.AnyProjectiles(ModContent.ProjectileType<BirbAuraFlare>()) || Utilities.AnyProjectiles(ModContent.ProjectileType<BirbAura>()))
                    SelectNextAttack(npc);
                npc.spriteDirection = (npc.SafeDirectionTo(target.Center).X > 0f).ToDirectionInt();
            }

            if (attackTimer == shootDelay)
            {
                SoundEngine.PlaySound(SoundID.DD2_BetsyFireballShot, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 mouthPosition = npc.Center + Vector2.UnitX * npc.direction * (float)Math.Cos(npc.rotation) * (npc.width * 0.5f + 36f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), mouthPosition, Vector2.Zero, ModContent.ProjectileType<BirbAuraFlare>(), 0, 0f, Main.myPlayer, 2f, npc.target + 1);
                }
            }

            if (attackTimer > shootDelay + 12f)
            {
                frameType = (int)DragonfollyFrameDrawingType.FlapWings;
                flapRate = 5f;
            }

            if (attackTimer == shootDelay + 35f)
                SelectNextAttack(npc);
        }

        public static void DoAttack_FeatherSpreadRelease(NPC npc, Player target, ref float attackTimer, ref float frameType, ref float flapRate)
        {
            int totalWaves = (int)MathHelper.Lerp(3f, 7.5f, 1f - npc.life / (float)npc.lifeMax);
            int flyTime = 35;
            int waveDelay = 32;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float waveCounter = ref npc.Infernum().ExtraAI[0];
            ref float screamTimer = ref npc.localAI[3];

            if (attackTimer < flyTime)
            {
                float flyInertia = 9f;
                float flySpeed = 26f;

                if (!npc.WithinRange(target.Center, 400f))
                    npc.velocity = (npc.velocity * (flyInertia - 1f) + npc.SafeDirectionTo(target.Center) * flySpeed) / flyInertia;
            }
            npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();

            frameType = (int)DragonfollyFrameDrawingType.FlapWings;
            flapRate = 6f;

            if (attackTimer >= flyTime)
            {
                if (attackTimer <= flyTime + 65f)
                {
                    if (screamTimer < 30f)
                        screamTimer = 30f;
                    if (waveCounter > 0f)
                        screamTimer++;
                    frameType = (int)DragonfollyFrameDrawingType.Screm;
                }

                if (waveCounter > 0f)
                    attackTimer++;
            }

            if (attackTimer == flyTime + waveDelay)
            {
                // Release a burst of feathers into the air.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int featherType = ModContent.ProjectileType<RedLightningRedirectingFeather>();
                    int totalFeathers = (int)MathHelper.Lerp(8, 26, 1f - lifeRatio);
                    for (int i = 0; i < totalFeathers; i++)
                    {
                        Vector2 shootVelocity = Vector2.UnitY.RotatedBy(MathHelper.TwoPi * i / totalFeathers) * -8f;
                        Utilities.NewProjectileBetter(npc.Center + shootVelocity * 9f, shootVelocity, featherType, 240, 0f);
                    }
                }

                // As well as a burst of dust.
                if (!Main.dedServ)
                {
                    for (float speed = 3f; speed <= 10f; speed += 1.2f)
                    {
                        float lifePersistance = Main.rand.NextFloat(1.5f, 2f);
                        for (int i = 0; i < 60; i++)
                        {
                            Dust energy = Dust.NewDustPerfect(npc.Center, 267);
                            energy.velocity = (MathHelper.TwoPi * i / 60f).ToRotationVector2() * speed;
                            energy.noGravity = true;
                            energy.color = Main.hslToRgb(Main.rand.NextFloat(0f, 0.08f), 0.85f, 0.6f);
                            energy.fadeIn = lifePersistance;
                            energy.scale = 1.56f;
                        }
                    }
                }
            }

            if (attackTimer >= flyTime)
                npc.velocity *= 0.975f;
            npc.rotation *= 0.96f;

            if (attackTimer >= flyTime + waveDelay + 90f)
            {
                if (waveCounter >= totalWaves - 1f)
                    SelectNextAttack(npc);
                else
                {
                    npc.velocity = npc.SafeDirectionTo(target.Center) * 34f;
                    attackTimer = flyTime;
                    waveCounter++;
                    npc.netUpdate = true;

                    // Make a diving sound.
                    SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown, npc.Center);
                }
            }
        }

        public static void DoAttack_ReleasePlasmaBursts(NPC npc, Player target, ref float attackTimer, ref float fadeToRed, ref float frameType, ref float flapRate)
        {
            if (NPC.CountNPCS(ModContent.NPCType<RedPlasmaEnergy>()) >= 3)
                SelectNextAttack(npc);
            ref float chargeTime = ref npc.Infernum().ExtraAI[0];

            frameType = (int)DragonfollyFrameDrawingType.Screm;

            for (int delay = 0; delay < 60; delay += 20)
            {
                Vector2 mouthPosition = npc.Center + Vector2.UnitX * npc.direction * (float)Math.Cos(npc.rotation) * (npc.width * 0.5f + 27f);
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == ScreamTime - 30f + delay)
                {
                    int plasmaBall = NPC.NewNPC(npc.GetSource_FromAI(), (int)mouthPosition.X, (int)mouthPosition.Y, ModContent.NPCType<RedPlasmaEnergy>());
                    if (Main.npc.IndexInRange(plasmaBall))
                        Main.npc[plasmaBall].velocity = Vector2.UnitX.RotatedByRandom(0.4f) * npc.direction * 7f;
                }
            }

            npc.rotation *= 0.975f;
            npc.velocity *= 0.975f;
            if (attackTimer >= ScreamTime + 45f)
                SelectNextAttack(npc);
        }

        public static void DoAttack_ElectricOverload(NPC npc, Player target, ref float attackTimer, ref float frameType, ref float flapRate)
        {
            int cloudReleaseRate = 10;
            int sparkReleaseRate = 20;
            float horizontalOffset = 750f;
            ref float chargeState = ref npc.Infernum().ExtraAI[0];
            ref float accumulatedSpeed = ref npc.Infernum().ExtraAI[1];
            ref float backgroundFadeToRed = ref npc.Infernum().ExtraAI[8];

            // Line up for charge.
            if (chargeState == 0f)
            {
                float verticalOffsetLeniance = 75f;
                float flySpeed = 18.5f + accumulatedSpeed;
                float flyInertia = 4f;
                Vector2 destination = target.Center - Vector2.UnitX * Math.Sign(target.Center.X - npc.Center.X) * horizontalOffset;

                // Fly towards the destination beside the player.
                npc.velocity = (npc.velocity * (flyInertia - 1f) + npc.SafeDirectionTo(destination) * flySpeed) / flyInertia;
                npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();

                // If within a good approximation of the player's position, scream loudly.
                if (Math.Abs(npc.Center.X - target.Center.X) > horizontalOffset && Math.Abs(npc.Center.Y - target.Center.Y) < verticalOffsetLeniance)
                {
                    chargeState = 1f;
                    accumulatedSpeed = 0f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }

                // Become more and more fast the more time has passed.
                // (Why does this comment sound funny to me?)
                accumulatedSpeed += 0.055f;

                frameType = (int)DragonfollyFrameDrawingType.FlapWings;
                flapRate = 6f;
            }

            // Scream and create a red shockwave/background.
            else if (chargeState == 1f)
            {
                npc.rotation *= 0.96f;
                npc.velocity *= 0.98f;
                backgroundFadeToRed = MathHelper.Lerp(backgroundFadeToRed, 1f, 0.1f);
                if (attackTimer < ScreamTime + 30f)
                {
                    frameType = (int)DragonfollyFrameDrawingType.Screm;
                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == ScreamTime + 10f)
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<TwinsEnergyExplosion>(), 0, 0f);

                    HatGirl.SayThingWhileOwnerIsAlive(target, "Static bolts seem to be flying towards you! Be wary of them, and don't get trapped by the lightning telegraphs!");
                }
                else
                {
                    frameType = (int)DragonfollyFrameDrawingType.FlapWings;
                    flapRate = 6f;
                }

                // Reel back.
                if (attackTimer == ScreamTime + 75f)
                {
                    npc.velocity = npc.SafeDirectionTo(target.Center) * -8f;
                    npc.velocity.X *= 0.3f;
                    chargeState = 2f;
                    accumulatedSpeed = 0f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            // Prepare for the charge.
            else if (chargeState == 2f)
            {
                backgroundFadeToRed = 1f;

                float flySpeed = 32.5f;
                float flyInertia = 26f;
                Vector2 chargeVelocity = npc.SafeDirectionTo(target.Center) * flySpeed;
                npc.velocity = (npc.velocity * (flyInertia - 1f) + chargeVelocity * 0.8f) / flyInertia;
                npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
                npc.rotation = (npc.rotation * 7f + npc.velocity.X * 0.01f) / 8f;

                if (attackTimer >= 15f)
                {
                    npc.velocity = chargeVelocity;
                    chargeState = 3f;
                    accumulatedSpeed = 0f;
                    attackTimer = 0f;
                    npc.netUpdate = true;

                    // Make a diving sound.
                    SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown, npc.Center);
                }
            }

            // Do the charge and release lightning everywhere.
            else if (chargeState == 3f)
            {
                npc.velocity *= 0.99f;
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % cloudReleaseRate == cloudReleaseRate - 1f)
                {
                    Vector2 spawnPosition = target.Center + Vector2.UnitX * Main.rand.NextFloat(60f, 500f) * Main.rand.NextBool().ToDirectionInt();
                    int cloud = Utilities.NewProjectileBetter(spawnPosition, Vector2.Zero, ModContent.ProjectileType<LightningCloud2>(), 0, 0f);
                    if (Main.projectile.IndexInRange(cloud))
                    {
                        Main.projectile[cloud].timeLeft = 10 + (170 - (int)attackTimer);
                        Main.projectile[cloud].netUpdate = true;
                    }
                }

                // Send sparks towards the target periodically.
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % sparkReleaseRate == sparkReleaseRate - 1f)
                {
                    Vector2 spawnOffset = -target.velocity.SafeNormalize(Main.rand.NextVector2Unit()) * 775f;
                    Vector2 sparkVelocity = -spawnOffset.SafeNormalize(Vector2.UnitY) * 14f;
                    Utilities.NewProjectileBetter(target.Center + spawnOffset, sparkVelocity, ModContent.ProjectileType<RedSpark>(), 240, 0f);
                }

                npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
                npc.rotation = (npc.rotation * 7f + npc.velocity.X * 0.01f) / 8f;
                if (attackTimer >= 60f)
                {
                    float flyInertia = 6f;
                    float flySpeed = 25f;
                    if (!npc.WithinRange(target.Center, 315f))
                        npc.velocity = (npc.velocity * (flyInertia - 1f) + npc.SafeDirectionTo(target.Center) * flySpeed) / flyInertia;
                }

                if (attackTimer >= 180f)
                {
                    SoundEngine.PlaySound(HolyBlast.ImpactSound, target.Center);
                    SelectNextAttack(npc);
                }
            }
        }

        public static void DoAttack_RuffleFeathers(NPC npc, Player target, bool phase3, ref float attackTimer, ref float frameType, ref float flapRate)
        {
            npc.rotation = npc.rotation.AngleLerp(0f, 0.125f);
            npc.rotation = npc.rotation.AngleTowards(0f, 0.125f);
            npc.noTileCollide = true;

            int featherReleaseRate = phase3 ? 4 : 7;
            ref float attackState = ref npc.Infernum().ExtraAI[0];

            // Fly near the target.
            if (attackState == 0f)
            {
                frameType = (int)DragonfollyFrameDrawingType.FlapWings;
                flapRate = 5f;

                npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center - Vector2.UnitY * 200f, -Vector2.UnitY) * 29f, 0.45f);
                npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();

                // If somewhat close to the target or enough time has passed, transition to the feather creating state.
                if (npc.WithinRange(target.Center, 420f) || attackTimer >= 360f)
                {
                    attackState = 1f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            // Create a bunch of feathers.
            else if (attackState == 1f)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.Zero, 0.04f);
                if (npc.velocity.Length() < 0.8f)
                    npc.velocity = Vector2.Zero;

                // Create feathers in the air.
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer < ScreamSoundDelay && attackTimer % featherReleaseRate == featherReleaseRate - 1f)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 featherVelocity = Main.rand.NextVector2Circular(9.6f, 4.5f);
                        featherVelocity.Y = -Math.Abs(featherVelocity.Y);
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + Main.rand.NextVector2CircularEdge(50f, 50f), featherVelocity, ModContent.ProjectileType<BigFollyFeather>(), 0, 0f);
                    }
                }

                frameType = (int)DragonfollyFrameDrawingType.FlapWings;
                flapRate = 3f;

                if (attackTimer >= ScreamTime + 25f)
                    SelectNextAttack(npc);
            }
        }

        public static void DoAttack_ExplodingEnergyOrbs(NPC npc, Player target, bool phase3, ref float attackTimer, ref float frameType, ref float flapRate)
        {
            int chargeDelay = 20;
            int energyOrbReleaseRate = 16;
            int chargeTime = 48;
            int chargeCount = 2;
            float chargeSpeed = 39.5f;
            float horizontalOffset = 600f;

            if (phase3)
            {
                chargeDelay -= 5;
                energyOrbReleaseRate -= 7;
                chargeTime -= 8;
                chargeCount++;
            }

            Vector2 hoverDestination = target.Center - Vector2.UnitX * Math.Sign(target.Center.X - npc.Center.X) * horizontalOffset;
            ref float attackState = ref npc.Infernum().ExtraAI[0];
            ref float chargeDirection = ref npc.Infernum().ExtraAI[1];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[2];

            // Define frames.
            frameType = (int)DragonfollyFrameDrawingType.FlapWings;
            flapRate = 5f;

            // Hover to the side of the target in anticipation of the charge.
            if (attackState == 0f)
            {
                npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 27f, 0.6f);

                // Prepare for the charge if sufficiently close to the hover destination or if enough natural time has elapsed.
                if ((attackTimer >= 45f && npc.WithinRange(hoverDestination, 200f)) || attackTimer >= 270f)
                {
                    chargeDirection = npc.AngleTo(target.Center);
                    attackState = 1f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            // Reel back in anticipation of the charge.
            if (attackState == 1f)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * -8f, 0.1f);
                if (attackTimer >= chargeDelay)
                {
                    attackState = 2f;
                    attackTimer = 0f;
                    npc.velocity = chargeDirection.ToRotationVector2() * chargeSpeed;
                    SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown, npc.Center);
                    npc.netUpdate = true;
                }
            }

            // Charge and release energy orbs.
            if (attackState == 2f)
            {
                npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();

                // Create the orbs from the mouth.
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % energyOrbReleaseRate == energyOrbReleaseRate - 1f)
                {
                    Vector2 mouthPosition = npc.Center + Vector2.UnitX * npc.direction * (float)Math.Cos(npc.rotation) * (npc.width * 0.5f + 36f);
                    Vector2 baseShootVelocity = (target.Center - mouthPosition).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.56f) * 11f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), mouthPosition, baseShootVelocity.RotatedBy(-0.36f), ModContent.ProjectileType<ExplodingEnergyOrb>(), 0, 0f);
                }

                if (attackTimer >= chargeTime)
                {
                    chargeCounter++;

                    if (chargeCounter >= chargeCount)
                        SelectNextAttack(npc);
                    else
                    {
                        attackState = 0f;
                        attackTimer = 0f;
                        npc.velocity *= 0.3f;
                        npc.netUpdate = true;
                    }
                }
            }

            npc.rotation = npc.velocity.X * 0.01f;
        }

        public static void DoAttack_LightningSupercharge(NPC npc, Player target, ref float attackTimer, ref float fadeToRed, ref float frameType, ref float flapRate)
        {
            int chargeDelay = 20;
            int lightningSpawnerReleaseRate = 13;
            int featherReleaseRate = 3;
            int chargeTime = 48;
            float chargeSpeed = 39.5f;
            float horizontalOffset = 600f;
            Vector2 hoverDestination = target.Center - Vector2.UnitX * Math.Sign(target.Center.X - npc.Center.X) * horizontalOffset;
            ref float attackState = ref npc.Infernum().ExtraAI[0];
            ref float hasCreatedTelegraph = ref npc.Infernum().ExtraAI[1];
            ref float chargeDirection = ref npc.Infernum().ExtraAI[2];

            // Define frames.
            frameType = (int)DragonfollyFrameDrawingType.FlapWings;
            flapRate = 5f;

            // Hover to the side of the target in anticipation of the charge.
            if (attackState == 0f)
            {
                npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 27f, 0.6f);

                // Prepare for the charge if sufficiently close to the hover destination or if enough natural time has elapsed.
                if ((attackTimer >= 45f && npc.WithinRange(hoverDestination, 200f)) || attackTimer >= 270f)
                {
                    attackState = 1f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            // Reel back in anticipation of the charge.
            if (attackState == 1f)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * -8f, 0.1f);
                if (Main.netMode != NetmodeID.MultiplayerClient && hasCreatedTelegraph == 0f)
                {
                    chargeDirection = npc.AngleTo(target.Center);

                    int telegraph = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<LightningSuperchargeTelegraph>(), 0, 0f);
                    if (Main.projectile.IndexInRange(telegraph))
                    {
                        Main.projectile[telegraph].ModProjectile<LightningSuperchargeTelegraph>().ChargePositions = new[]
                        {
                            npc.Center,
                            npc.Center + chargeDirection.ToRotationVector2() * 1200f
                        };
                        Main.projectile[telegraph].netUpdate = true;
                    }
                    hasCreatedTelegraph = 1f;
                    npc.netUpdate = true;
                }

                fadeToRed = attackTimer / chargeDelay;
                if (attackTimer >= chargeDelay)
                {
                    attackState = 2f;
                    attackTimer = 0f;
                    npc.velocity = chargeDirection.ToRotationVector2() * chargeSpeed;
                    SoundEngine.PlaySound(InfernumSoundRegistry.CalThunderStrikeSound, npc.Center);
                    SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown, npc.Center);
                    npc.netUpdate = true;
                }
            }

            // Charge and release a lot of feathers, along with some lightning spawners.
            if (attackState == 2f)
            {
                npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();

                // Create feathers in the air.
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % featherReleaseRate == featherReleaseRate - 1f)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 featherVelocity = Main.rand.NextVector2Circular(9.6f, 4.5f);
                        featherVelocity.Y = -Math.Abs(featherVelocity.Y);
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + Main.rand.NextVector2CircularEdge(50f, 50f), featherVelocity, ModContent.ProjectileType<BigFollyFeather>(), 0, 0f);
                    }
                }
                
                // Create the bolts from the mouth.
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % lightningSpawnerReleaseRate == lightningSpawnerReleaseRate - 1f)
                {
                    Vector2 mouthPosition = npc.Center + Vector2.UnitX * npc.direction * (float)Math.Cos(npc.rotation) * (npc.width * 0.5f + 36f);
                    Vector2 baseShootVelocity = npc.SafeDirectionTo(mouthPosition) * 10f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), mouthPosition, baseShootVelocity.RotatedBy(-0.36f), ModContent.ProjectileType<BirbThunderAuraFlare>(), 0, 0f, Main.myPlayer, 2f, npc.target + 1);
                }

                fadeToRed = Utils.GetLerpValue(chargeTime, chargeTime - 10f, attackTimer, true);
                if (attackTimer >= chargeTime)
                    SelectNextAttack(npc);
            }

            npc.rotation = npc.velocity.X * 0.01f;
        }

        #endregion

        #endregion AI

        #region Frames and Drawcode

        public const int ScreamTime = 60;
        public const int ScreamSoundDelay = ScreamTime - 20;

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frameCounter++;
            ref float frameType = ref npc.localAI[0];
            ref float flapRate = ref npc.localAI[1];
            ref float scremTimer = ref npc.localAI[3];

            switch ((DragonfollyFrameDrawingType)(int)frameType)
            {
                case DragonfollyFrameDrawingType.FlapWings:
                    if (npc.frameCounter >= flapRate)
                    {
                        npc.frameCounter = 0D;
                        npc.frame.Y += frameHeight;
                    }
                    if (npc.frame.Y >= frameHeight * 5)
                        npc.frame.Y = 0;
                    scremTimer = 0f;
                    break;
                case DragonfollyFrameDrawingType.Screm:
                    scremTimer++;
                    if (npc.frameCounter >= 5f)
                    {
                        npc.frameCounter = 0D;
                        npc.frame.Y += frameHeight;
                    }
                    if (npc.frame.Y >= frameHeight * 5)
                        npc.frame.Y = 0;

                    if (scremTimer >= ScreamSoundDelay - 15f)
                    {
                        npc.frame.Y = frameHeight * 5;
                        if (scremTimer == ScreamSoundDelay)
                            SoundEngine.PlaySound(SoundID.DD2_BetsyScream with { Pitch = 0.25f }, npc.Center);
                    }
                    break;
            }
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            float fadeToRed = npc.localAI[2];
            float phaseTransitionCountdown = npc.Infernum().ExtraAI[6];
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            int drawInstances = (int)MathHelper.Lerp(1f, 4f, fadeToRed);
            Color drawColor = Color.Lerp(lightColor, Color.Red * 0.9f, fadeToRed);
            drawColor *= MathHelper.Lerp(1f, 0.4f, fadeToRed);
            if (fadeToRed > 0.4f)
            {
                npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.1f, 0f, 1f);
                drawColor.A = 0;
            }

            Vector2 origin = npc.frame.Size() * 0.5f;

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            void drawInstance(Vector2 baseDrawPosition, float scale, float opacity)
            {
                if (phaseTransitionCountdown > 0f)
                {
                    float outwardnessFactor = 1f - (float)Math.Cos(phaseTransitionCountdown * MathHelper.TwoPi / TransitionTime);
                    outwardnessFactor /= 3f;
                    for (int i = 0; i < 6; i++)
                    {
                        Color afterimageColor = npc.GetAlpha(Color.Lerp(lightColor, Color.Red, 0.5f));
                        afterimageColor *= 1f - outwardnessFactor;

                        Vector2 drawPosition = npc.Center + (i / 6f * MathHelper.TwoPi + npc.rotation).ToRotationVector2() * outwardnessFactor * 42f - Main.screenPosition;
                        Main.spriteBatch.Draw(texture, drawPosition, npc.frame, afterimageColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, spriteEffects, 0f);
                    }
                }

                for (int i = 0; i < drawInstances; i++)
                {
                    Vector2 drawPosition = baseDrawPosition - Main.screenPosition + Vector2.UnitY * npc.gfxOffY;
                    if (fadeToRed > 0.4f)
                        drawPosition += (MathHelper.TwoPi * i / drawInstances + Main.GlobalTimeWrappedHourly * 5f).ToRotationVector2() * 5f;
                    Main.spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(drawColor) * opacity, npc.rotation, origin, scale, spriteEffects, 0f);
                }
            }

            drawInstance(npc.Center, npc.scale, 1f);
            return false;
        }
        #endregion

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "The fight against the Dragonfolly is very chaotic and fast paced. Good mobility and reaction time help a lot!";
            yield return n => "Those large red lightning pillars can be negated by flying below them!";
        }
        #endregion Tips
    }
}
