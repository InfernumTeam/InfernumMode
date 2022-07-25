using CalamityMod;
using CalamityMod.Events;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace InfernumMode.BehaviorOverrides.BossAIs.StormWeaver
{
    public static class StormWeaverHeadBehaviorOverride
    {
        #region Enumerations
        public enum StormWeaverAttackType
        {
            NormalMove,
            SparkBurst,
            LightningCharge,
            StaticChargeup,
            IceStorm,
            StormWeave,
        }
        #endregion

        #region AI

        public static bool PreAI(NPC npc)
        {
            // Do targeting.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Reset the hit sound.
            npc.HitSound = SoundID.NPCHit13;

            // Fade in.
            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.2f, 0f, 1f);

            // Start rain.
            if (npc.localAI[0] == 0f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    CalamityUtils.StartRain(true);
                    Main.cloudBGActive = 1f;
                    Main.numCloudsTemp = 160;
                    Main.numClouds = Main.numCloudsTemp;
                    Main.windSpeedTarget = 1.56f;
                    Main.windSpeedCurrent = Main.windSpeedTarget;
                    Main.maxRaining = 0.9f;
                }

                npc.localAI[0] = 1f;
            }

            float fadeToBlue = 0f;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float attackState = ref npc.ai[1];
            ref float attackTimer = ref npc.ai[2];

            switch ((StormWeaverAttackType)(int)attackState)
            {
                case StormWeaverAttackType.NormalMove:
                    DoAttack_NormalMove(npc, target, attackTimer);
                    break;
                case StormWeaverAttackType.SparkBurst:
                    DoAttack_SparkBurst(npc, target, lifeRatio, attackTimer);
                    break;
                case StormWeaverAttackType.LightningCharge:
                    DoAttack_LightningCharge(npc, target, lifeRatio, ref attackTimer, ref fadeToBlue);
                    break;
                case StormWeaverAttackType.StaticChargeup:
                    DoAttack_StaticChargeup(npc, target, ref attackTimer, ref fadeToBlue);
                    break;
                case StormWeaverAttackType.IceStorm:
                    DoAttack_IceStorm(npc, target, ref attackTimer);
                    break;
                case StormWeaverAttackType.StormWeave:
                    DoAttack_StormWeave(npc, target, ref attackTimer);
                    break;
            }

            Main.rainTime = 480;

            // Determine rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            // Determine the blue fade.
            npc.Calamity().newAI[0] = MathHelper.Lerp(280f, 400f, MathHelper.Clamp(fadeToBlue, 0f, 1f));

            attackTimer++;
            return false;
        }

        public static void DoAttack_NormalMove(NPC npc, Player target, float attackTimer)
        {
            float turnSpeed = (!npc.WithinRange(target.Center, 220f)).ToInt() * 0.039f;
            float moveSpeed = npc.velocity.Length();

            if (npc.WithinRange(target.Center, 285f))
                moveSpeed *= 1.015f;
            else if (npc.velocity.Length() > 19f + attackTimer / 36f)
                moveSpeed *= 0.98f;

            moveSpeed = MathHelper.Clamp(moveSpeed, 15f, 29f) * (BossRushEvent.BossRushActive ? 1.45f : 1f);

            npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), turnSpeed, true) * moveSpeed;

            if (attackTimer >= 300f)
                SelectNewAttack(npc);
        }

        public static void DoAttack_SparkBurst(NPC npc, Player target, float lifeRatio, float attackTimer)
        {
            float turnSpeed = (!npc.WithinRange(target.Center, 220f)).ToInt() * 0.054f;
            float moveSpeed = npc.velocity.Length();

            if (npc.WithinRange(target.Center, 285f))
                moveSpeed *= 1.01f;
            else if (npc.velocity.Length() > 13f)
                moveSpeed *= 0.98f;

            moveSpeed = MathHelper.Clamp(moveSpeed, 13f, 25f) * (BossRushEvent.BossRushActive ? 1.45f : 1f);

            npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), turnSpeed, true) * moveSpeed;

            if (attackTimer % 40f == 39f && !npc.WithinRange(target.Center, 210f))
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
                    float shootSpeed = MathHelper.Lerp(8f, 12.5f, 1f - lifeRatio);
                    if (BossRushEvent.BossRushActive)
                        shootSpeed *= 1.5f;

                    for (int i = 0; i < 9; i++)
                    {
                        float offsetAngle = MathHelper.Lerp(-0.51f, 0.51f, i / 8f);
                        Vector2 sparkVelocity = npc.SafeDirectionTo(target.Center, -Vector2.UnitY).RotatedBy(offsetAngle) * shootSpeed;
                        Utilities.NewProjectileBetter(npc.Center + sparkVelocity * 3f, sparkVelocity, ModContent.ProjectileType<WeaverSpark>(), 245, 0f);
                    }
                }
            }

            if (attackTimer >= 450f)
                SelectNewAttack(npc);
        }

        public static void DoAttack_LightningCharge(NPC npc, Player target, float lifeRatio, ref float attackTimer, ref float fadeToBlue)
        {
            int hoverRedirectTime = 240;
            Vector2 hoverOffset = new Vector2((target.Center.X < npc.Center.X).ToDirectionInt(), (target.Center.Y < npc.Center.Y).ToDirectionInt()) * 485f;
            Vector2 hoverDestination = target.Center + hoverOffset;
            int chargeRedirectTime = 25;
            int chargeTime = 15;
            int chargeSlowdownTime = 10;
            int chargeCount = 4;
            ref float idealChargeVelocityX = ref npc.Infernum().ExtraAI[0];
            ref float idealChargeVelocityY = ref npc.Infernum().ExtraAI[1];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[2];

            // Attempt to get into position for a charge.
            if (attackTimer < hoverRedirectTime)
            {
                float idealHoverSpeed = MathHelper.Lerp(33.5f, 50f, attackTimer / hoverRedirectTime);
                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * MathHelper.Lerp(npc.velocity.Length(), idealHoverSpeed, 0.08f);
                npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), 0.064f, true) * idealVelocity.Length();

                // Stop hovering if close to the hover destination
                if (npc.WithinRange(hoverDestination, 40f))
                {
                    attackTimer = hoverRedirectTime;
                    if (npc.velocity.Length() > 24f)
                        npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * 24f;

                    npc.netUpdate = true;
                }
            }

            // Determine a charge velocity to adjust to.
            if (attackTimer == hoverRedirectTime)
            {
                Vector2 idealChargeVelocity = npc.SafeDirectionTo(target.Center + target.velocity * 20f) * MathHelper.Lerp(37f, 45f, 1f - lifeRatio);
                if (BossRushEvent.BossRushActive)
                    idealChargeVelocity *= 1.2f;
                idealChargeVelocityX = idealChargeVelocity.X;
                idealChargeVelocityY = idealChargeVelocity.Y;
                npc.netUpdate = true;
            }

            // Move into the charge.
            if (attackTimer > hoverRedirectTime && attackTimer <= hoverRedirectTime + chargeRedirectTime)
            {
                Vector2 idealChargeVelocity = new(idealChargeVelocityX, idealChargeVelocityY);
                npc.velocity = npc.velocity.RotateTowards(idealChargeVelocity.ToRotation(), 0.08f, true) * MathHelper.Lerp(npc.velocity.Length(), idealChargeVelocity.Length(), 0.15f);
                npc.velocity = npc.velocity.MoveTowards(idealChargeVelocity, 5f);
            }

            // Release lightning from behind the worm once the charge has begun.
            if (attackTimer == hoverRedirectTime + chargeRedirectTime / 2)
            {
                SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 lightningSpawnPosition = npc.Center - npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.92f) * 150f;
                        Vector2 lightningVelocity = (target.Center - lightningSpawnPosition).SafeNormalize(Vector2.UnitY) * 6.5f;
                        int arc = Utilities.NewProjectileBetter(lightningSpawnPosition, lightningVelocity, ProjectileID.CultistBossLightningOrbArc, 255, 0f);
                        if (Main.projectile.IndexInRange(arc))
                        {
                            Main.projectile[arc].ai[0] = lightningVelocity.ToRotation();
                            Main.projectile[arc].ai[1] = Main.rand.Next(100);
                            Main.projectile[arc].tileCollide = false;
                        }
                    }
                }
            }

            // Slow down after charging.
            if (attackTimer > hoverRedirectTime + chargeRedirectTime + chargeTime)
                npc.velocity *= 0.8f;

            // Calculate fade to blue.
            fadeToBlue = Utils.GetLerpValue(hoverRedirectTime, hoverRedirectTime + chargeRedirectTime, attackTimer, true) *
                Utils.GetLerpValue(hoverRedirectTime + chargeRedirectTime + chargeTime + chargeSlowdownTime, hoverRedirectTime + chargeRedirectTime + chargeTime, attackTimer, true);

            // Prepare the next charge. If all charges are done, go to the next attack.
            if (attackTimer > hoverRedirectTime + chargeRedirectTime + chargeTime + chargeSlowdownTime)
            {
                chargeCounter++;
                idealChargeVelocityX = 0f;
                idealChargeVelocityY = 0f;
                attackTimer = 0f;
                if (chargeCounter >= chargeCount)
                    SelectNewAttack(npc);

                npc.netUpdate = true;
            }
        }

        public static void DoAttack_StaticChargeup(NPC npc, Player target, ref float attackTimer, ref float fadeToBlue)
        {
            int initialAttackWaitDelay = 10;
            float attackStartDistanceThreshold = 490f;
            int spinDelay = 30;
            int totalSpins = 3;
            int spinTime = 270;
            float sparkShootSpeed = 5.6f;
            int sparkShootRate = 8;
            float spinSpeed = 18f;
            if (BossRushEvent.BossRushActive)
            {
                sparkShootRate = 5;
                sparkShootSpeed *= 1.8f;
            }

            float angularSpinVelocity = MathHelper.TwoPi * totalSpins / spinTime;

            // Determine fade to blue.
            fadeToBlue = Utils.GetLerpValue(spinDelay, spinDelay + initialAttackWaitDelay, attackTimer, true) *
                Utils.GetLerpValue(spinDelay + initialAttackWaitDelay + spinTime, spinDelay + initialAttackWaitDelay + spinTime - 30f, attackTimer, true);

            // Attempt to move towards the target if far away from them.
            if (!npc.WithinRange(target.Center, attackStartDistanceThreshold) && attackTimer < initialAttackWaitDelay)
            {
                float idealMoventSpeed = (npc.Distance(target.Center) - attackStartDistanceThreshold) / 70f + 15f;
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

            // Do the spin, along with associated attacks from it.
            if (attackTimer > spinDelay + initialAttackWaitDelay)
            {
                npc.velocity = npc.velocity.RotatedBy(angularSpinVelocity);
                Vector2 spinCenter = npc.Center + npc.velocity.RotatedBy(MathHelper.PiOver2) * spinTime / totalSpins / MathHelper.TwoPi;

                // Frequently release sparks.
                if (attackTimer % sparkShootRate == sparkShootRate - 1f)
                {
                    SoundEngine.PlaySound(SoundID.DD2_LightningAuraZap, spinCenter);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int projectileCount = attackTimer % 24f == 23f ? 9 : 1;

                        for (int i = 0; i < projectileCount; i++)
                        {
                            float angularImprecision = Utils.GetLerpValue(720f, 350f, npc.Distance(target.Center), true);
                            float predictivenessFactor = (float)Math.Pow(1f - angularImprecision, 2D);
                            float angularOffset = 0f;
                            if (projectileCount > 1)
                                angularOffset = MathHelper.Lerp(-0.87f, 0.87f, i / (float)(projectileCount - 1f));

                            Vector2 predictiveOffset = target.velocity * predictivenessFactor * 15f;
                            Vector2 shootVelocity = (target.Center - spinCenter + predictiveOffset).SafeNormalize(Vector2.UnitY).RotatedBy(angularOffset).RotatedByRandom(angularImprecision * 0.59f) * sparkShootSpeed;
                            Utilities.NewProjectileBetter(spinCenter, shootVelocity, ModContent.ProjectileType<WeaverSpark>(), 255, 0f);
                        }
                    }
                }

                // As well as some electric orbs that explode.
                if (attackTimer % 90f == 89f)
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

            if (attackTimer >= spinDelay + initialAttackWaitDelay + spinTime)
                SelectNewAttack(npc);
        }

        public static void DoAttack_IceStorm(NPC npc, Player target, ref float attackTimer)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            int shootCount = 3;
            int shotSpacing = (int)MathHelper.Lerp(175f, 145f, 1f - lifeRatio);
            int delayBeforeFiring = 60;
            int shootRate = delayBeforeFiring + 54;

            // Lazily move towards the target.
            if (!npc.WithinRange(target.Center, 200f))
            {
                Vector2 idealVelocity = npc.SafeDirectionTo(target.Center) * 13f;
                npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), 0.036f, true) * idealVelocity.Length();
            }

            if (attackTimer % shootRate == 1f)
            {
                // Play a sound on the player getting frost waves rained on them, as a telegraph.
                SoundEngine.PlaySound(SoundID.Item120, target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float shootOffsetAngle = Main.rand.NextFloatDirection() * MathHelper.PiOver4;
                    for (int dx = -1750; dx < 1750; dx += shotSpacing)
                    {
                        Vector2 spawnOffset = -Vector2.UnitY.RotatedBy(shootOffsetAngle) * 1600f + shootOffsetAngle.ToRotationVector2() * dx;
                        Vector2 maxShootVelocity = Vector2.UnitY.RotatedBy(shootOffsetAngle) * 9f;
                        int telegraph = Utilities.NewProjectileBetter(target.Center + spawnOffset, maxShootVelocity * 0.5f, ModContent.ProjectileType<StormWeaverFrostWaveTelegraph>(), 0, 0f);
                        if (Main.projectile.IndexInRange(telegraph))
                            Main.projectile[telegraph].ai[1] = maxShootVelocity.Length();
                        int wave = Utilities.NewProjectileBetter(target.Center + spawnOffset, maxShootVelocity * 0.1f, ProjectileID.FrostWave, 260, 0f);
                        if (Main.projectile.IndexInRange(wave))
                        {
                            Main.projectile[wave].ai[0] = -delayBeforeFiring;
                            Main.projectile[wave].ai[1] = maxShootVelocity.Length();
                        }
                    }
                }
            }

            if (attackTimer >= shootRate * shootCount)
                SelectNewAttack(npc);
        }

        public static void DoAttack_StormWeave(NPC npc, Player target, ref float attackTimer)
        {
            int hoverRedirectTime = 240;
            int chargeTime = 150;
            float cloudCoverArea = 4600f;
            Vector2 hoverOffset = new((target.Center.X < npc.Center.X).ToDirectionInt() * cloudCoverArea * 0.5f, -1350f);
            Vector2 hoverDestination = target.Center + hoverOffset;
            if (hoverDestination.Y < 300f)
                hoverDestination.Y = 300f;

            float chargeSpeed = cloudCoverArea / chargeTime;
            int chargeSlowdownTime = 90;
            ref float chargeDirection = ref npc.Infernum().ExtraAI[0];

            // Attempt to get into position for a charge.
            if (attackTimer < hoverRedirectTime)
            {
                float idealHoverSpeed = MathHelper.Lerp(26.5f, 45f, attackTimer / hoverRedirectTime);
                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * MathHelper.Lerp(npc.velocity.Length(), idealHoverSpeed, 0.08f);
                npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), 0.074f, true) * idealVelocity.Length();
                chargeDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

                // Stop hovering if close to the hover destination
                if (npc.WithinRange(hoverDestination, 40f))
                {
                    attackTimer = hoverRedirectTime;
                    npc.netUpdate = true;
                }
            }

            // Begin charging horizontally, releasing storm clouds while doing so.
            if (attackTimer > hoverRedirectTime && attackTimer < hoverRedirectTime + chargeTime)
            {
                Vector2 chargeVelocity = Vector2.UnitX * chargeSpeed * chargeDirection;
                npc.velocity = Vector2.Lerp(npc.velocity, chargeVelocity, 0.15f).MoveTowards(chargeVelocity, 2f);

                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 4f == 3f)
                {
                    Vector2 cloudSpawnPosition = npc.Center + Main.rand.NextVector2Circular(8f, 8f);
                    Vector2 cloudVelocity = Main.rand.NextVector2Circular(5f, 5f);
                    Utilities.NewProjectileBetter(cloudSpawnPosition, cloudVelocity, ModContent.ProjectileType<StormWeaveCloud>(), 0, 0f);
                }
            }

            if (attackTimer > hoverRedirectTime + chargeTime + chargeSlowdownTime)
                SelectNewAttack(npc);
        }

        public static void SelectNewAttack(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            for (int i = 0; i < 4; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            ref float attackState = ref npc.ai[1];
            float oldAttackState = npc.ai[1];
            WeightedRandom<float> newStatePicker = new(Main.rand);
            newStatePicker.Add((int)StormWeaverAttackType.NormalMove, 1.5);
            newStatePicker.Add((int)StormWeaverAttackType.LightningCharge);
            newStatePicker.Add((int)StormWeaverAttackType.StaticChargeup);
            newStatePicker.Add((int)StormWeaverAttackType.IceStorm);

            if (lifeRatio < 0.4f)
                newStatePicker.Add((int)StormWeaverAttackType.StormWeave, 5D);

            do
                attackState = newStatePicker.Get();
            while (attackState == oldAttackState);

            npc.TargetClosest();
            npc.ai[1] = (int)attackState;
            npc.ai[2] = 0f;
            npc.netUpdate = true;
        }
        #endregion AI
    }
}
