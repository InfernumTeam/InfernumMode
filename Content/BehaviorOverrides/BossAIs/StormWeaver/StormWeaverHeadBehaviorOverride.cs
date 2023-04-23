using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.StormWeaver;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Boss;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Content.BehaviorOverrides.BossAIs.AquaticScourge;
using InfernumMode.Content.Projectiles.Pets;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.StormWeaver
{
    public class StormWeaverHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<StormWeaverHead>();

        #region Enumerations
        public enum StormWeaverAttackType
        {
            NormalMove,
            SparkBurst,
            StaticChargeup,
            IceStorm,
            FakeoutCharge,
            FogSneakAttackCharges,
            AimedLightningBolts,
            BerdlyWindGusts
        }
        #endregion

        #region AI

        public const int FogInterpolantIndex = 6;

        public const float MaxLightningBrightness = 0.55f;

        public const float Phase2LifeRatio = 0.5f;

        public override bool PreAI(NPC npc)
        {
            // Do targeting.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Reset the hit sound.
            npc.HitSound = SoundID.NPCHit13;

            // Disable natural drawing so that the render target can handle it.
            npc.hide = true;

            float fadeToBlue = 0f;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float attackState = ref npc.ai[1];
            ref float attackTimer = ref npc.ai[2];
            ref float electricityFormInterpolant = ref npc.ai[3];
            ref float fogInterpolant = ref npc.Infernum().ExtraAI[FogInterpolantIndex];
            ref float lightningSkyBrightness = ref npc.ModNPC<StormWeaverHead>().lightning;

            lightningSkyBrightness = MathHelper.Clamp(lightningSkyBrightness - 0.025f, 0f, 1f);

            if (lifeRatio < 0.1f)
                CalamityMod.CalamityMod.StopRain();
            else if (!Main.raining || Main.maxRaining < 0.7f)
            {
                CalamityUtils.StartRain(false, true);
                Main.cloudBGActive = 1f;
                Main.numCloudsTemp = 160;
                Main.numClouds = Main.numCloudsTemp;
                Main.windSpeedCurrent = 1.04f;
                Main.windSpeedTarget = Main.windSpeedCurrent;
                Main.maxRaining = 0.96f;
            }

            // Lol. Lmao.
            if (target.HasBuff(BuffID.Chilled))
                target.ClearBuff(BuffID.Chilled);
            if (target.HasBuff(BuffID.Frozen))
                target.ClearBuff(BuffID.Frozen);
            if (target.HasBuff(BuffID.Electrified))
                target.ClearBuff(BuffID.Electrified);

            // Update the hit and death sounds to account for the fact that there is no more phase 1.
            npc.HitSound = SoundID.NPCHit13;
            npc.DeathSound = SoundID.NPCDeath13;

            // Create segments.
            if (npc.localAI[0] == 0f)
            {
                AquaticScourgeHeadBehaviorOverride.CreateSegments(npc, 25, ModContent.NPCType<StormWeaverBody>(), ModContent.NPCType<StormWeaverTail>());
                npc.Opacity = 1f;
                npc.localAI[0] = 1f;
            }

            // Reset DR.
            npc.Calamity().DR = 0.1f;

            switch ((StormWeaverAttackType)(int)attackState)
            {
                case StormWeaverAttackType.NormalMove:
                    DoBehavior_NormalMove(npc, target, attackTimer);
                    break;
                case StormWeaverAttackType.SparkBurst:
                    DoBehavior_SparkBurst(npc, target, lifeRatio, attackTimer);
                    break;
                case StormWeaverAttackType.StaticChargeup:
                    DoBehavior_StaticChargeup(npc, target, ref attackTimer, ref fadeToBlue);
                    break;
                case StormWeaverAttackType.IceStorm:
                    DoBehavior_IceStorm(npc, target, ref lightningSkyBrightness, ref attackTimer);
                    break;
                case StormWeaverAttackType.FakeoutCharge:
                    DoBehavior_FakeoutCharge(npc, target, ref lightningSkyBrightness, ref attackTimer);
                    break;
                case StormWeaverAttackType.FogSneakAttackCharges:
                    DoBehavior_FogSneakAttackCharges(npc, target, ref lightningSkyBrightness, ref attackTimer, ref fogInterpolant, ref electricityFormInterpolant);
                    break;

                case StormWeaverAttackType.AimedLightningBolts:
                    float _ = 0f;
                    DoBehavior_AimedLightningBolts(npc, target, ref attackTimer, ref lightningSkyBrightness, ref _, ref electricityFormInterpolant);
                    break;
                case StormWeaverAttackType.BerdlyWindGusts:
                    DoBehavior_BerdlyWindGusts(npc, target, ref attackTimer);
                    break;
            }

            Main.rainTime = 480;

            // Determine rotation.
            npc.rotation = (npc.position - npc.oldPosition).ToRotation() + MathHelper.PiOver2;

            // Determine the blue fade.
            npc.Calamity().newAI[0] = MathHelper.Lerp(280f, 400f, MathHelper.Clamp(fadeToBlue, 0f, 1f));

            attackTimer++;
            return false;
        }

        public static void DoBehavior_NormalMove(NPC npc, Player target, float attackTimer)
        {
            float turnSpeed = (!npc.WithinRange(target.Center, 220f)).ToInt() * 0.039f;
            float moveSpeed = npc.velocity.Length();

            if (npc.WithinRange(target.Center, 285f))
                moveSpeed *= 1.015f;
            else if (npc.velocity.Length() > 25f + attackTimer / 36f)
                moveSpeed *= 0.98f;

            moveSpeed = MathHelper.Clamp(moveSpeed, 21f, 32.5f) * (BossRushEvent.BossRushActive ? 1.45f : 1f);

            npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), turnSpeed, true) * moveSpeed;

            if (attackTimer >= 1f)
                SelectNewAttack(npc);
        }

        public static void DoBehavior_SparkBurst(NPC npc, Player target, float lifeRatio, float attackTimer)
        {
            float turnSpeed = (!npc.WithinRange(target.Center, 220f)).ToInt() * 0.054f;
            float moveSpeed = npc.velocity.Length();

            if (npc.WithinRange(target.Center, 285f))
                moveSpeed *= 1.01f;
            else if (npc.velocity.Length() > 13f)
                moveSpeed *= 0.98f;

            moveSpeed = MathHelper.Clamp(moveSpeed, 15.4f, 26f) * (BossRushEvent.BossRushActive ? 1.45f : 1f);

            npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), turnSpeed, true) * moveSpeed;

            if (attackTimer % 27f == 26f && !npc.WithinRange(target.Center, 210f))
            {
                // Create some mouth dust.
                for (int i = 0; i < 20; i++)
                {
                    Dust electricity = Dust.NewDustPerfect(npc.Center + npc.velocity.SafeNormalize(Vector2.Zero) * 30f, 229);
                    electricity.velocity = Main.rand.NextVector2Circular(5f, 5f) + npc.velocity;
                    electricity.scale = 1.9f;
                    electricity.noGravity = true;
                }

                SoundEngine.PlaySound(SoundID.Item94, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float shootSpeed = MathHelper.Lerp(7f, 11.5f, 1f - lifeRatio);
                    if (BossRushEvent.BossRushActive)
                        shootSpeed *= 1.5f;

                    for (int i = 0; i < 11; i++)
                    {
                        float offsetAngle = MathHelper.Lerp(-0.51f, 0.51f, i / 10f);
                        Vector2 sparkVelocity = npc.SafeDirectionTo(target.Center, -Vector2.UnitY).RotatedBy(offsetAngle) * shootSpeed;
                        Utilities.NewProjectileBetter(npc.Center + sparkVelocity * 3f, sparkVelocity, ModContent.ProjectileType<WeaverSpark>(), 245, 0f);
                    }
                }
            }

            if (attackTimer >= 300f)
                SelectNewAttack(npc);
        }

        public static void DoBehavior_StaticChargeup(NPC npc, Player target, ref float attackTimer, ref float fadeToBlue)
        {
            int initialAttackWaitDelay = 10;
            float attackStartDistanceThreshold = 490f;
            int spinDelay = 30;
            int totalSpins = 3;
            int spinTime = 270;
            int sparkShootRate = 8;
            int orbShootRate = 75;
            int attackTransitionDelay = 65;
            float sparkShootSpeed = 5.6f;
            float spinSpeed = 18f;
            if (BossRushEvent.BossRushActive)
            {
                sparkShootRate = 5;
                orbShootRate -= 12;
                sparkShootSpeed *= 1.8f;
            }

            // Reset DR.
            npc.Calamity().DR = 0.55f;

            float angularSpinVelocity = MathHelper.TwoPi * totalSpins / spinTime;
            ref float sparkShootCounter = ref npc.Infernum().ExtraAI[0];

            // Determine fade to blue.
            fadeToBlue = Utils.GetLerpValue(spinDelay, spinDelay + initialAttackWaitDelay, attackTimer, true) *
                Utils.GetLerpValue(spinDelay + initialAttackWaitDelay + spinTime, spinDelay + initialAttackWaitDelay + spinTime - 30f, attackTimer, true);

            // Attempt to move towards the target if far away from them.
            if (!npc.WithinRange(target.Center, attackStartDistanceThreshold) && attackTimer < initialAttackWaitDelay)
            {
                float idealMoventSpeed = (npc.Distance(target.Center) - attackStartDistanceThreshold) / 45f + 31f;
                npc.velocity = (npc.velocity * 39f + npc.SafeDirectionTo(target.Center) * idealMoventSpeed) / 40f;

                attackTimer = 0f;
            }

            // Attempt to get closer to the ideal spin speed once ready.
            if (attackTimer >= initialAttackWaitDelay && attackTimer < spinDelay + initialAttackWaitDelay && !npc.WithinRange(target.Center, 150f))
                npc.velocity = (npc.velocity * 14f + npc.SafeDirectionTo(target.Center) * spinSpeed) / 15f;

            // Prepare the spin movement and direction.
            if (attackTimer == spinDelay + initialAttackWaitDelay)
            {
                npc.velocity = npc.SafeDirectionTo(target.Center, Vector2.UnitY) * spinSpeed;
                npc.netUpdate = true;
            }

            // Do the spin, and create the sparks from the point at which the weaver is spinning.
            if (attackTimer > spinDelay + initialAttackWaitDelay && attackTimer <= spinDelay + initialAttackWaitDelay + spinTime)
            {
                npc.velocity = npc.velocity.RotatedBy(angularSpinVelocity);
                Vector2 spinCenter = npc.Center + npc.velocity.RotatedBy(MathHelper.PiOver2) * spinTime / totalSpins / MathHelper.TwoPi;

                // Frequently release sparks.
                if (attackTimer % sparkShootRate == sparkShootRate - 1f)
                {
                    SoundEngine.PlaySound(SoundID.DD2_LightningAuraZap, spinCenter);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int projectileCount = sparkShootCounter % 3f == 2f ? 9 : 1;
                        for (int i = 0; i < projectileCount; i++)
                        {
                            float angularImprecision = Utils.GetLerpValue(720f, 350f, npc.Distance(target.Center), true);
                            float predictivenessFactor = (float)Math.Pow(1f - angularImprecision, 2D);
                            float angularOffset = 0f;
                            if (projectileCount > 1)
                                angularOffset = MathHelper.Lerp(-0.87f, 0.87f, i / (float)(projectileCount - 1f));

                            Vector2 predictiveOffset = target.velocity * predictivenessFactor * 15f;
                            Vector2 shootVelocity = (target.Center - spinCenter + predictiveOffset).SafeNormalize(Vector2.UnitY).RotatedBy(angularOffset).RotatedByRandom(angularImprecision * 0.59f) * sparkShootSpeed;
                            Utilities.NewProjectileBetter(spinCenter, shootVelocity, ModContent.ProjectileType<WeaverSpark>(), 280, 0f);
                        }
                        sparkShootCounter++;
                    }
                }

                // Release some electric orbs that explode.
                if (attackTimer % orbShootRate == orbShootRate - 1f)
                {
                    Vector2 orbSpawnPosition = spinCenter + Main.rand.NextVector2Unit() * Main.rand.NextFloat(50f, 220f);
                    Vector2 orbShootVelocity = (target.Center - orbSpawnPosition).SafeNormalize(Vector2.UnitY) * 5f;

                    // Play a sound and create some electric dust.
                    for (int i = 0; i < 16; i++)
                    {
                        Dust electricity = Dust.NewDustPerfect(orbSpawnPosition + Main.rand.NextVector2Circular(45f, 45f), 264);
                        electricity.color = Color.Cyan;
                        electricity.velocity = (electricity.position - orbShootVelocity).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(5f, 12f);
                        electricity.noGravity = true;
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(orbSpawnPosition, orbShootVelocity, ModContent.ProjectileType<ElectricOrb>(), 255, 0f);
                }
            }

            if (attackTimer >= spinDelay + initialAttackWaitDelay + spinTime + attackTransitionDelay)
                SelectNewAttack(npc);
        }

        public static void DoBehavior_IceStorm(NPC npc, Player target, ref float lightningSkyBrightness, ref float attackTimer)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            int shootCount = 4;
            int shotSpacing = (int)MathHelper.Lerp(175f, 145f, 1f - lifeRatio);
            int delayBeforeFiring = 60;
            int shootRate = delayBeforeFiring + 54;

            // Circle the target.
            Vector2 flyDestination = target.Center - Vector2.UnitY.RotatedBy(attackTimer / 30f) * 630f;
            npc.Center = npc.Center.MoveTowards(flyDestination, 22f);

            Vector2 idealVelocity = npc.SafeDirectionTo(flyDestination) * 33f;
            npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), 0.036f, true) * idealVelocity.Length();
            npc.velocity = npc.velocity.MoveTowards(idealVelocity, 4f);

            // Disable contact damage.
            npc.damage = 0;

            if (attackTimer % shootRate == 1f)
            {
                // Play a sound on the player getting frost waves rained on them, as a telegraph.
                SoundEngine.PlaySound(SoundID.Item120, target.Center);
                SoundEngine.PlaySound(InfernumSoundRegistry.CalThunderStrikeSound, target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float shootOffsetAngle = Main.rand.NextFloatDirection() * MathHelper.PiOver4;
                    for (float dx = -1750; dx < 1750; dx += shotSpacing + Main.rand.NextFloatDirection() * 60f)
                    {
                        Vector2 spawnOffset = -Vector2.UnitY.RotatedBy(shootOffsetAngle) * 1600f + shootOffsetAngle.ToRotationVector2() * dx;
                        Vector2 maxShootVelocity = Vector2.UnitY.RotatedBy(shootOffsetAngle) * 9f;
                        Utilities.NewProjectileBetter(target.Center + spawnOffset, maxShootVelocity * 0.5f, ModContent.ProjectileType<StormWeaverFrostWaveTelegraph>(), 0, 0f, -1, 0f, maxShootVelocity.Length());
                        Utilities.NewProjectileBetter(target.Center + spawnOffset, maxShootVelocity * 0.15f, ProjectileID.FrostWave, 260, 0f, -1, -delayBeforeFiring, maxShootVelocity.Length());
                    }
                }
                lightningSkyBrightness = MaxLightningBrightness;
            }

            if (attackTimer >= shootRate * shootCount)
                SelectNewAttack(npc);
        }

        public static void DoBehavior_FakeoutCharge(NPC npc, Player target, ref float lightningSkyBrightness, ref float attackTimer)
        {
            ref float substate = ref npc.Infernum().ExtraAI[0];
            ref float centerX = ref npc.Infernum().ExtraAI[1];
            ref float centerY = ref npc.Infernum().ExtraAI[2];

            float circleTime = 150f;
            float skyBrightenTime = circleTime - 25f;
            float initialChargeSpeed = 30f;
            float initialChargeStopDistance = 470f;
            float secondaryChargeSpeed = 35f;
            float secondaryChargeLength = 25f;
            float afterChargeWait = 25f;
            switch (substate)
            {
                case 0:
                    // Move around the player at distance.
                    Vector2 hoverDestination = target.Center + (attackTimer / 25f).ToRotationVector2() * 1400;
                    if (npc.velocity.Length() < 2f)
                        npc.velocity = Vector2.UnitY * -2.4f;

                    float flySpeed = MathHelper.Lerp(27 * 0.5f, 27, Utils.GetLerpValue(50f, 270f, npc.Distance(hoverDestination), true));
                    flySpeed *= Utils.GetLerpValue(0f, 50f, npc.Distance(hoverDestination), true);
                    npc.velocity = npc.velocity * 0.85f + npc.SafeDirectionTo(hoverDestination) * flySpeed * 0.15f;
                    npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(hoverDestination) * flySpeed, 4f);

                    if (attackTimer >= skyBrightenTime)
                        lightningSkyBrightness = MathHelper.Lerp(lightningSkyBrightness, MaxLightningBrightness, 0.25f);

                    if (attackTimer >= circleTime)
                    {
                        attackTimer = 0;
                        substate++;
                    }
                    break;
                case 1:
                    npc.velocity = npc.SafeDirectionTo(target.Center) * initialChargeSpeed;
                    SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, target.Center);

                    centerX = target.Center.X;
                    centerY = target.Center.Y;
                    attackTimer = 0;
                    substate++;
                    break;
                case 2:
                    if (npc.WithinRange(new(centerX, centerY), initialChargeStopDistance) || attackTimer > 90f)
                    {
                        npc.velocity *= 0.95f;
                        if (npc.velocity.Length() < 5.5f)
                        {
                            attackTimer = 0;
                            substate++;
                        }
                    }
                    break;
                case 3:
                    npc.velocity = npc.SafeDirectionTo(target.Center) * secondaryChargeSpeed;
                    SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, target.Center);
                    SoundEngine.PlaySound(InfernumSoundRegistry.CalThunderStrikeSound, target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            Vector2 lightningSpawnPosition = npc.Center - npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.92f) * 150f;
                            Vector2 lightningVelocity = (target.Center - lightningSpawnPosition).SafeNormalize(Vector2.UnitY) * 6.5f;
                            int arc = Utilities.NewProjectileBetter(lightningSpawnPosition, lightningVelocity, ProjectileID.CultistBossLightningOrbArc, 255, 0f, -1, lightningVelocity.ToRotation(), Main.rand.Next(100));
                            if (Main.projectile.IndexInRange(arc))
                                Main.projectile[arc].tileCollide = false;
                        }

                        NPC tail = Main.npc[NPC.FindFirstNPC(ModContent.NPCType<StormWeaverTail>())];
                        for (int i = 0; i < 4; i++)
                        {
                            float shootOffsetAngle = MathHelper.Lerp(-0.37f, 0.37f, i / 3f);
                            Vector2 sparkVelocity = tail.SafeDirectionTo(target.Center).RotatedBy(shootOffsetAngle) * 6.7f;
                            Utilities.NewProjectileBetter(tail.Center, sparkVelocity, ModContent.ProjectileType<WeaverSpark>(), 255, 0f);
                        }
                    }
                    attackTimer = 0;
                    substate++;
                    break;
                case 4:
                    if (attackTimer >= afterChargeWait + secondaryChargeLength)
                        SelectNewAttack(npc);
                    break;
            }
        }

        public static void DoBehavior_FogSneakAttackCharges(NPC npc, Player target, ref float lightningSkyBrightness, ref float attackTimer, ref float fogInterpolant, ref float electricityFormInterpolant)
        {
            int chargeCount = 4;
            int telegraphTime = 40;
            int chargeTime = 40;
            int sparkCount = 14;
            float chargeSpeed = 22f;
            float chargeAcceleration = 1.037f;
            float sparkSpeed = 11f;
            ref float chargeCounter = ref npc.Infernum().ExtraAI[0];
            ref float telegraphHoverOffsetDirection = ref npc.Infernum().ExtraAI[1];

            if (chargeCounter <= 0f)
                telegraphTime += 40;

            // Make the fog appear.
            fogInterpolant = MathHelper.Clamp(fogInterpolant + 0.02f, 0f, 1f);

            float telegraphHoverOffset = MathHelper.Lerp(500f, 400f, chargeCounter / chargeCount);
            Vector2 teleportPosition = target.Center + telegraphHoverOffsetDirection.ToRotationVector2() * telegraphHoverOffset;
            if (attackTimer <= telegraphTime)
            {
                // Rapidly fade out.
                npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.12f, 0f, 1f);

                electricityFormInterpolant = 1f - npc.Opacity;

                // Slow down.
                npc.velocity *= 0.9f;

                // Initialize the telegraph offset direction.
                if (attackTimer == 1f)
                {
                    telegraphHoverOffsetDirection = Main.rand.NextFloat(MathHelper.TwoPi);
                    npc.netUpdate = true;
                }

                if (attackTimer % 10f == 0f)
                {
                    SoundEngine.PlaySound(AresTeslaCannon.TeslaOrbShootSound, teleportPosition);

                    CreateSparks(teleportPosition);
                    for (int i = 0; i < 16; i++)
                    {
                        Color fireColor = Main.rand.NextBool() ? Color.Yellow : Color.Cyan;
                        CloudParticle fireCloud = new(teleportPosition, (MathHelper.TwoPi * i / 16f).ToRotationVector2() * 6f, fireColor, Color.DarkGray, 20, Main.rand.NextFloat(2.5f, 3.2f));
                        GeneralParticleHandler.SpawnParticle(fireCloud);
                    }
                }
            }

            // Do the charge teleport.
            else if (attackTimer == telegraphTime + 1f)
            {
                npc.Opacity = 1f;
                npc.Center = teleportPosition;
                lightningSkyBrightness = 0.5f;
                SoundEngine.PlaySound(InfernumSoundRegistry.CalThunderStrikeSound with { PitchVariance = 0.15f, Volume = 1.6f }, target.Center);
                SoundEngine.PlaySound(InfernumSoundRegistry.StormWeaverElectricDischargeSound with { Volume = 0.67f }, target.Center);

                // Create screen flash effect.
                ScreenEffectSystem.SetFlashEffect(npc.Center, 0.8f, 25);

                // Bring all segments to the weaver's position for the teleport.
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].realLife == npc.whoAmI)
                    {
                        Main.npc[i].Infernum().ExtraAI[0] = 36f;
                        Main.npc[i].Center = npc.Center;
                        Main.npc[i].netUpdate = true;
                    }
                }

                // Release sparks.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < sparkCount; i++)
                    {
                        Vector2 sparkVelocity = (MathHelper.TwoPi * i / sparkCount).ToRotationVector2() * sparkSpeed;
                        Utilities.NewProjectileBetter(npc.Center, sparkVelocity, ModContent.ProjectileType<WeaverSpark>(), 250, 0f);
                    }
                }

                npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
            }

            // Handle post charge behaviors.
            else
            {
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.025f) * chargeAcceleration;
                if (attackTimer >= telegraphTime + chargeTime)
                {
                    chargeCounter++;
                    if (chargeCounter >= chargeCount)
                        SelectNewAttack(npc);
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoBehavior_AimedLightningBolts(NPC npc, Player target, ref float attackTimer, ref float lightningSkyBrightness, ref float weatherStrength, ref float electricityFormInterpolant)
        {
            int shootCount = 3;
            int redirectTime = 35;
            int arcRedirectTime = 18;
            ref float shootCounter = ref npc.Infernum().ExtraAI[0];

            // Have the weaver orient itself near the player at first, and become wreathed in lightning.
            if (attackTimer <= redirectTime)
            {
                Vector2 hoverDestination = target.Center - Vector2.UnitY * 300f;
                float flySpeed = MathHelper.Lerp(46f, 7f, attackTimer / redirectTime);
                float movementInterpolant = Utils.Remap(attackTimer, 0f, redirectTime - 20f, 0.2f, 0.04f);
                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * flySpeed;
                if (npc.WithinRange(hoverDestination, 270f))
                    idealVelocity = npc.velocity.ClampMagnitude(15f, 45f);

                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, movementInterpolant).RotateTowards(idealVelocity.ToRotation(), 0.05f);
                electricityFormInterpolant = MathHelper.Clamp(electricityFormInterpolant + 0.02f, 0f, 1f);
                return;
            }

            // Slow down and arc around towards the player while the segments emit electricity in anticipation of the attac.
            if (attackTimer <= redirectTime + arcRedirectTime)
            {
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), MathHelper.Pi / 189f) * 0.96f;
                weatherStrength = MathHelper.Clamp(weatherStrength + 0.07f, 0f, 0.8f);
                return;
            }

            // Create a lightning flash in the background.
            if (attackTimer == redirectTime + arcRedirectTime + 1f)
            {
                lightningSkyBrightness = 0.4f;
                SoundEngine.PlaySound(InfernumSoundRegistry.CalThunderStrikeSound with { PitchVariance = 0.15f }, target.Center);
                SoundEngine.PlaySound(InfernumSoundRegistry.StormWeaverElectricDischargeSound with { Volume = 1.6f }, target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float sparkSpeed = npc.Distance(target.Center) * 0.0197f + 7f;
                    float aimAtTargetInterpolant = Utils.Remap(npc.Distance(target.Center), 720f, 1400f, 0.1f, 0.85f);
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC n = Main.npc[i];
                        if (n.realLife != npc.whoAmI || n.WithinRange(target.Center, 380f))
                            continue;

                        Vector2 sparkVelocity = n.rotation.ToRotationVector2().RotatedByRandom(0.53f) * Main.rand.NextFromList(-1f, 1f) * sparkSpeed * Main.rand.NextFloat(0.85f, 1.15f);

                        // Proportionally make the sparks aim at the target.
                        sparkVelocity = Vector2.Lerp(sparkVelocity, n.SafeDirectionTo(target.Center) * sparkVelocity.Length(), aimAtTargetInterpolant).SafeNormalize(Vector2.UnitY) * sparkSpeed;

                        Utilities.NewProjectileBetter(n.Center + Main.rand.NextVector2Circular(8f, 8f), sparkVelocity, ModContent.ProjectileType<HomingWeaverSpark>(), 250, 0f);

                        // Create sparks.
                        CreateSparks(n.Center);
                    }
                    electricityFormInterpolant = 0f;
                    npc.netUpdate = true;

                    Utilities.CreateShockwave(npc.Center, 2, 8, 75, false);
                }

                // Create a screen blur effect.
                ScreenEffectSystem.SetBlurEffect(npc.Center, 1f, 25);
            }

            if (attackTimer >= redirectTime + arcRedirectTime + 60f)
            {
                attackTimer = 0f;
                shootCounter++;
                if (shootCounter >= shootCount)
                {
                    electricityFormInterpolant = 0f;
                    SelectNewAttack(npc);
                }
            }
        }

        public static void DoBehavior_BerdlyWindGusts(NPC npc, Player target, ref float attackTimer)
        {
            int windGustTime = 360;
            int windGustReleaseRate = 45;
            int gustsPerBurst = 5;
            ref float centerPointX = ref npc.Infernum().ExtraAI[0];
            ref float centerPointY = ref npc.Infernum().ExtraAI[1];
            ref float windBurstCounter = ref npc.Infernum().ExtraAI[2];

            // Idly emit wind particles.
            Vector2 windSpawnPosition = target.Center + new Vector2(Main.windSpeedCurrent.DirectionalSign() * -1250f, Main.rand.NextFloatDirection() * 900f);
            Vector2 windVelocity = new Vector2(1f, 0.25f) * Main.windSpeedCurrent.DirectionalSign() * Main.rand.NextFloat(0.1f, 1.8f) * 70f;
            Particle wind = new SnowyIceParticle(windSpawnPosition, windVelocity, Color.White with { A = 0 } * 0.6f, Main.rand.NextFloat(0.7f, 1.1f), 60);
            GeneralParticleHandler.SpawnParticle(wind);

            // Change the wind speeds periodically.
            if (attackTimer % 90f == 0f)
            {
                Main.windSpeedTarget = Main.rand.NextFloat(-3f, 3f);
                Main.windSpeedCurrent = MathHelper.Lerp(Main.windSpeedCurrent, Main.windSpeedTarget, 0.85f);
                SoundEngine.PlaySound(InfernumSoundRegistry.StormWeaverWindSound);

                // Create a gust of wind particles in the new direction.
                for (int i = 0; i < 85; i++)
                {
                    windSpawnPosition = target.Center + new Vector2(Main.windSpeedCurrent.DirectionalSign() * -1250f, Main.rand.NextFloatDirection() * 900f);
                    windVelocity = new Vector2(1f, 0.25f) * Main.windSpeedCurrent.DirectionalSign() * Main.rand.NextFloat(0.1f, 1.8f) * 90f;
                    wind = new SnowyIceParticle(windSpawnPosition, windVelocity, Color.White with { A = 0 } * 0.6f, Main.rand.NextFloat(1f, 2f), 60);
                    GeneralParticleHandler.SpawnParticle(wind);
                }
            }

            // Spin around the player while releasing wind gusts.
            if (attackTimer <= windGustTime)
            {
                if (centerPointX == 0f || centerPointY == 0f)
                {
                    centerPointX = target.Center.X;
                    centerPointY = target.Center.Y;
                    npc.netUpdate = true;
                }
                centerPointX = MathHelper.Lerp(centerPointX, target.Center.X, 0.024f);
                centerPointY = MathHelper.Lerp(centerPointY, target.Center.Y, 0.024f);

                Vector2 spinDestination = new Vector2(centerPointX, centerPointY) + (attackTimer * MathHelper.TwoPi / 60f).ToRotationVector2() * 900f;
                npc.velocity = npc.SafeDirectionTo(spinDestination) * MathHelper.Min(npc.Distance(spinDestination), 34f);
                npc.Center = npc.Center.MoveTowards(spinDestination, target.velocity.Length() * 1.2f + 35f);

                // Grant the target infinite flight time, so that they don't run out in the middle of a flight and get screwed by losing the ability to dodge the wind.
                target.wingTime = target.wingTimeMax;

                if (attackTimer % windGustReleaseRate == 0f)
                {
                    float spinDirection = (windBurstCounter % 2f == 0f).ToDirectionInt();
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < gustsPerBurst; i++)
                        {
                            ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(wind =>
                            {
                                wind.ModProjectile<WindGust>().SpinDirection = spinDirection;
                                wind.ModProjectile<WindGust>().SpinCenter = target.Center;
                            });
                            Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<WindGust>(), 250, 0f, -1, MathHelper.TwoPi * i / gustsPerBurst);
                        }
                        windBurstCounter++;
                        npc.netUpdate = true;
                    }
                }

                // Yeah, no. We're not having any of this.
                if (npc.Calamity().dashImmunityTime[target.whoAmI] >= 1)
                {
                    npc.Calamity().dashImmunityTime[target.whoAmI] = 0;
                    target.immuneTime = 0;
                }
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].realLife == npc.whoAmI)
                        Main.npc[i].Calamity().dashImmunityTime[target.whoAmI] = 0;
                }

                return;
            }

            SelectNewAttack(npc);
        }

        public static void CreateSparks(Vector2 sparkSpawnPosition)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector2 sparkVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(5f, 11f);
                Color sparkColor = Color.Lerp(Color.White, Color.Cyan, Main.rand.NextFloat(0.4f, 1f));
                GeneralParticleHandler.SpawnParticle(new SparkParticle(sparkSpawnPosition, sparkVelocity, false, 60, 2f, sparkColor));

                sparkVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 10f);
                Color arcColor = Color.Lerp(Color.Cyan, Color.Wheat, Main.rand.NextFloat(0.2f, 0.67f));
                GeneralParticleHandler.SpawnParticle(new ElectricArc(sparkSpawnPosition, sparkVelocity, arcColor, 0.84f, 30));
            }
        }

        public static void SelectNewAttack(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            bool phase2 = lifeRatio < Phase2LifeRatio;
            ref float attackState = ref npc.ai[1];
            ref float attackCycleIndex = ref npc.Infernum().ExtraAI[5];

            attackCycleIndex++;
            switch ((int)attackCycleIndex % 7)
            {
                case 0:
                    attackState = (int)StormWeaverAttackType.NormalMove;
                    break;
                case 1:
                    attackState = (int)StormWeaverAttackType.AimedLightningBolts;
                    break;
                case 2:
                    attackState = (int)(phase2 ? StormWeaverAttackType.BerdlyWindGusts : StormWeaverAttackType.IceStorm);
                    break;
                case 3:
                    attackState = (int)StormWeaverAttackType.FakeoutCharge;
                    break;
                case 4:
                    attackState = (int)(phase2 ? StormWeaverAttackType.FogSneakAttackCharges : StormWeaverAttackType.NormalMove);
                    break;
                case 5:
                    attackState = (int)StormWeaverAttackType.StaticChargeup;
                    break;
                case 6:
                    attackState = (int)(phase2 ? StormWeaverAttackType.BerdlyWindGusts : StormWeaverAttackType.FakeoutCharge);
                    break;
            }

            npc.TargetClosest();
            npc.ai[1] = (int)attackState;
            npc.ai[2] = 0f;
            npc.netUpdate = true;
        }
        #endregion AI
    }
}
