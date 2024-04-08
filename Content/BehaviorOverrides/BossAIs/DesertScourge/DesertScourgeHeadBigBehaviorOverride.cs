using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs.DesertScourge;
using CalamityMod.Particles;
using InfernumMode.Assets.BossTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Particles;
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
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.DesertScourge
{
    public class DesertScourgeHeadBigBehaviorOverride : NPCBehaviorOverride
    {
        public enum DesertScourgeAttackType
        {
            SpawnAnimation,
            SandSpit,
            SandRushCharge,
            SandstormParticles,
            GroundSlam,
            SummonVultures
        }

        public static int SandBlastDamage => 75;

        public static int SandnadoDamage => 90;

        public const int HideMapIconIndex = 5;

        public const float Phase2LifeRatio = 0.55f;

        public const float Phase3LifeRatio = 0.25f;

        public override int NPCOverrideType => ModContent.NPCType<DesertScourgeHead>();

        public override float[] PhaseLifeRatioThresholds =>
        [
            Phase2LifeRatio,
            Phase3LifeRatio
        ];

        #region Loading
        public override void Load()
        {
            GlobalNPCOverrides.BossHeadSlotEvent += DisableMapIconDuringSpawnAnimation;
        }

        private void DisableMapIconDuringSpawnAnimation(NPC npc, ref int index)
        {
            if (npc.type == ModContent.NPCType<DesertScourgeHead>() && npc.Infernum().ExtraAI[HideMapIconIndex] >= 1f)
                index = -1;
        }
        #endregion Loading

        #region AI
        public override void SetDefaults(NPC npc)
        {
            // Set defaults that, if were to be changed by Calamity, would cause significant issues to the fight.
            npc.width = 32;
            npc.height = 80;
            npc.scale = 1f;
            npc.Opacity = 1f;
            npc.defense = 4;
            npc.alpha = 255;

        }

        public override void BossHeadSlot(NPC npc, ref int index)
        {
            index = ModContent.GetModBossHeadSlot(BossTextureRegistry.DesertScourgeMapIcon);
        }

        public override bool PreAI(NPC npc)
        {
            // Reset the contact damage.
            npc.damage = 100;

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();

            // Fade in.
            npc.alpha = Utils.Clamp(npc.alpha - 20, 0, 255);

            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float initializedFlag = ref npc.ai[2];
            ref float enrageTimer = ref npc.ai[3];
            ref float hideMapIcon = ref npc.Infernum().ExtraAI[HideMapIconIndex];

            if (Main.netMode != NetmodeID.MultiplayerClient && initializedFlag == 0f)
            {
                CreateSegments(npc, 28, ModContent.NPCType<DesertScourgeBody>(), ModContent.NPCType<DesertScourgeTail>());
                initializedFlag = 1f;
                npc.netUpdate = true;
            }

            // If there still was no valid target, dig away.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead ||
                !Main.player[npc.target].active || !npc.WithinRange(Main.player[npc.target].Center, 5600f))
            {
                DoBehavior_Despawn(npc);
                return false;
            }

            Player target = Main.player[npc.target];
            bool outOfBiome = !target.ZoneDesert && !BossRushEvent.BossRushActive;
            enrageTimer = Clamp(enrageTimer + outOfBiome.ToDirectionInt(), 0f, 420f);
            bool enraged = enrageTimer > 360f;

            npc.defense = npc.defDefense;
            npc.Calamity().CurrentlyEnraged = outOfBiome;

            switch ((DesertScourgeAttackType)(int)attackType)
            {
                case DesertScourgeAttackType.SpawnAnimation:
                    DoBehavior_SpawnAnimation(npc, target, ref attackTimer, ref hideMapIcon);
                    break;
                case DesertScourgeAttackType.SandSpit:
                    DoBehavior_SandSpit(npc, target, enraged, ref attackTimer);
                    break;
                case DesertScourgeAttackType.SandRushCharge:
                    DoBehavior_SandRushCharge(npc, target, enraged, ref attackTimer);
                    break;
                case DesertScourgeAttackType.SandstormParticles:
                    DoBehavior_SandstormParticles(npc, target, enraged, ref attackTimer);
                    break;
                case DesertScourgeAttackType.GroundSlam:
                    DoBehavior_GroundSlam(npc, target, enraged, ref attackTimer);
                    break;
                case DesertScourgeAttackType.SummonVultures:
                    DoBehavior_SummonVultures(npc, target, ref attackTimer);
                    break;
            }
            attackTimer++;

            return false;
        }

        public static void DoBehavior_SpawnAnimation(NPC npc, Player target, ref float attackTimer, ref float hideMapIcon)
        {
            int groundShakeTime = 270;
            int riseUpTime = 300;
            int hoverTime = 120;
            ref float hasReachedSurface = ref npc.Infernum().ExtraAI[0];

            // Make the ground shake and the ground create rising sand particles on the ground at first.
            if (attackTimer <= groundShakeTime)
            {
                // Play a rumble sound.
                if (attackTimer == 1f)
                    SoundEngine.PlaySound(InfernumSoundRegistry.LeviathanRumbleSound);

                float groundShakeInterpolant = attackTimer / groundShakeTime;

                for (int i = 0; i < 3; i++)
                {
                    if (Main.rand.NextFloat() >= groundShakeInterpolant + 0.2f)
                        continue;

                    Vector2 particleSpawnPosition = Utilities.GetGroundPositionFrom(target.Center + new Vector2(Main.rand.NextFloatDirection() * 1200f, -560f));
                    bool sandBelow = Framing.GetTileSafely((int)(particleSpawnPosition.X / 16f), (int)(particleSpawnPosition.Y / 16f)).TileType == TileID.Sand;
                    if (sandBelow)
                        Dust.NewDustPerfect(particleSpawnPosition + new Vector2(Main.rand.NextFloatDirection() * 8f, -8f), 32, Main.rand.NextVector2Circular(1.5f, 1.5f) - Vector2.UnitY * 1.5f);
                }

                // Create screen shake effects.
                target.Infernum_Camera().CurrentScreenShakePower = Pow(groundShakeInterpolant, 1.81f) * 10f;

                // Stick below the target.
                npc.velocity = Vector2.UnitY * -9f;
                npc.Center = target.Center + Vector2.UnitY * 1020f;
            }

            // Emerge from the sand.
            else if (attackTimer <= groundShakeTime + riseUpTime)
            {
                if (attackTimer == groundShakeTime + 1f)
                {
                    ScreenEffectSystem.SetBlurEffect(npc.Center, 2f, 45);
                    SoundEngine.PlaySound(InfernumSoundRegistry.AquaticScourgeAppearSound with { Pitch = -0.7f }, target.Center);
                }

                float horizontalDestination = target.Center.X + (target.Center.X < npc.Center.X).ToDirectionInt() * 250f;

                npc.velocity.X = Lerp(npc.velocity.X, npc.SafeDirectionTo(new(horizontalDestination, target.Center.Y)).X * 15f, 0.075f);
                npc.velocity.Y = Clamp(npc.velocity.Y - 0.6f, -25f, 10f);

                // Check if the scourge has reached the surface. If it has, create some particle effects and go to the next substate.
                bool inTiles = WorldGen.SolidTile(Framing.GetTileSafely((int)(npc.Center.X / 16f), (int)(npc.Center.Y / 16f)));
                if (hasReachedSurface == 0f && !inTiles && npc.Center.Y <= target.Bottom.Y + 80f)
                {
                    hasReachedSurface = 1f;
                    attackTimer = groundShakeTime + riseUpTime;
                    npc.velocity *= new Vector2(0.2f, 0.7f);
                    npc.netUpdate = true;

                    for (int i = 0; i < 54; i++)
                    {
                        Color sandColor = Color.Lerp(Color.SaddleBrown, Color.SandyBrown, Main.rand.NextFloat(0.7f)) * 0.5f;
                        SmallSmokeParticle sand = new(npc.Center + Main.rand.NextVector2Circular(64f, 64f), Main.rand.NextVector2Circular(10f, 16f) - Vector2.UnitY * 19f, sandColor, Color.Tan, Main.rand.NextFloat(0.7f, 1f), 255f, Main.rand.NextFloatDirection() * 0.015f);
                        GeneralParticleHandler.SpawnParticle(sand);
                    }
                    for (int i = 0; i < 32; i++)
                    {
                        Vector2 particleSpawnPosition = Utilities.GetGroundPositionFrom(target.Center + new Vector2(Main.rand.NextFloatDirection() * 1200f, -560f));
                        bool sandBelow = Framing.GetTileSafely((int)(particleSpawnPosition.X / 16f), (int)(particleSpawnPosition.Y / 16f)).TileType == TileID.Sand;
                        if (sandBelow)
                        {
                            Color sandColor = Color.Lerp(Color.SaddleBrown, Color.SandyBrown, Main.rand.NextFloat(0.7f)) * 0.4f;
                            SmallSmokeParticle sand = new(particleSpawnPosition + Main.rand.NextVector2Circular(10f, 10f), Main.rand.NextVector2Circular(4f, 8f) - Vector2.UnitY * 9f, sandColor, Color.Tan, Main.rand.NextFloat(0.32f, 0.67f), 255f, Main.rand.NextFloatDirection() * 0.015f);
                            GeneralParticleHandler.SpawnParticle(sand);
                        }
                    }
                }
            }

            // Hover to the top left/right of the target after emerging from the sand.
            else
            {
                Vector2 idealVelocity = npc.SafeDirectionTo(target.Center) * Utils.Remap(npc.Distance(target.Center), 180f, 60f, 12f, 3f);
                if (attackTimer < groundShakeTime + riseUpTime + hoverTime - 32f)
                    npc.velocity = npc.velocity.MoveTowards(idealVelocity, 0.18f).RotateTowards(idealVelocity.ToRotation(), Pi / 92f);
                else
                    npc.velocity *= 1.018f;

                // Roar before the attacks begin.
                if (attackTimer == groundShakeTime + riseUpTime + hoverTime - 32f)
                {
                    ScreenEffectSystem.SetBlurEffect(npc.Center, 2f, 45);
                    SoundEngine.PlaySound(DesertScourgeHead.RoarSound, target.Center);
                    npc.velocity = npc.SafeDirectionTo(target.Center) * 8f;
                }

                if (attackTimer >= groundShakeTime + riseUpTime + hoverTime)
                {
                    hideMapIcon = 0f;
                    SelectNextAttack(npc);
                    return;
                }
            }

            hideMapIcon = 1f - hasReachedSurface;

            // Disable damage.
            npc.damage = 0;

            // Disable the boss HP bar.
            npc.Calamity().ShouldCloseHPBar = true;

            // Calculate rotation.
            npc.rotation = npc.velocity.ToRotation() + PiOver2;
        }

        public static void DoBehavior_SandSpit(NPC npc, Player target, bool enraged, ref float attackTimer)
        {
            // Attempt to rush the target.
            int sandPerBurst = 7;
            int sandBurstShootRate = 80;
            float sandBurstSpeed = 11.25f;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float idealFlySpeed = Lerp(10f, 13.15f, 1f - lifeRatio) + npc.Distance(target.Center) * 0.012f;
            if (enraged)
            {
                sandPerBurst += 5;
                sandBurstShootRate -= 42;
                sandBurstSpeed *= 1.3f;
                idealFlySpeed *= 1.4f;
            }

            float maxChargeSpeed = idealFlySpeed * 1.54f;
            float flyAcceleration = idealFlySpeed / 560f;

            // Accelerate if close to the target.
            if (npc.WithinRange(target.Center, 280f))
            {
                if (npc.velocity.Length() < 3f)
                    npc.velocity = Vector2.UnitY * 3f;

                if (npc.velocity.Length() < maxChargeSpeed)
                    npc.velocity *= 1f + flyAcceleration * 0.64f;
            }

            // Otherwise fly towards them and release bursts of sand.
            else
            {
                float flySpeed = Lerp(npc.velocity.Length(), idealFlySpeed, 0.1f);
                Vector2 idealVelocity = npc.SafeDirectionTo(target.Center) * flySpeed;
                npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), flyAcceleration);
                npc.velocity = npc.velocity.MoveTowards(idealVelocity, flyAcceleration * 15f);

                // Release bursts of sand periodically.
                if (Main.netMode != NetmodeID.MultiplayerClient && !npc.WithinRange(target.Center, 325f) && attackTimer % sandBurstShootRate == sandBurstShootRate - 1f)
                {
                    for (int i = 0; i < sandPerBurst; i++)
                    {
                        Vector2 sandShootVelocity = (TwoPi * i / sandPerBurst).ToRotationVector2() * sandBurstSpeed;
                        Vector2 spawnPosition = npc.Center + sandShootVelocity * 2.5f;
                        Utilities.NewProjectileBetter(spawnPosition, sandShootVelocity, ModContent.ProjectileType<SandBlastInfernum>(), SandBlastDamage, 0f);

                        for (int j = 0; j < 5; j++)
                            CreateSandParticles(npc, Color.White, sandShootVelocity, npc.Center);
                        SoundEngine.PlaySound(SoundID.Item21, spawnPosition);
                    }
                }
            }

            // Calculate rotation.
            npc.rotation = npc.velocity.ToRotation() + PiOver2;

            if (attackTimer > 360f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_SandRushCharge(NPC npc, Player target, bool enraged, ref float attackTimer)
        {
            int inGroundDepthDefinition = 3;
            int sandCreationRate = 8;
            float chargeSpeed = 19f;
            if (enraged)
                sandCreationRate -= 3;

            ref float attackState = ref npc.Infernum().ExtraAI[0];
            ref float chargeDirection = ref npc.Infernum().ExtraAI[1];

            switch ((int)attackState)
            {
                // Slam into the ground.
                // If no ground is reached after a certain amount of falling, just go to the next attack.
                case 0:
                    if (attackTimer > 300f)
                        SelectNextAttack(npc);

                    float acceleration = 0.3f;
                    if (attackTimer < 30f)
                        acceleration = Lerp(-0.72f, 0f, attackTimer / 30f);
                    else if (attackTimer < 70f)
                        acceleration = Lerp(0f, 0.38f, Utils.GetLerpValue(30f, 70f, attackTimer, true));
                    npc.velocity.Y = Clamp(npc.velocity.Y + acceleration, -10f, 16f);

                    // Try to stay close to the target horizontally.
                    if (Distance(target.Center.X, npc.Center.X) > 500f)
                    {
                        float idealHorizontalSpeed = Math.Sign(target.Center.X - npc.Center.X) * 16f;
                        npc.velocity.X = (npc.velocity.X * 24f + idealHorizontalSpeed) / 25f;
                    }

                    // Check to see if DS is underground, with inGroundDepthDefinition total active tiles above it.
                    // If any of those tiles is active then it is considered not deep enough.
                    // If DS is above the target, it is automatically designated as unable to begin the charge.
                    bool inGround = npc.Center.Y > target.Center.Y;
                    for (int i = 0; i < inGroundDepthDefinition; i++)
                    {
                        Tile tile = Framing.GetTileSafely((int)(npc.Center.X / 16f), (int)(npc.Center.Y / 16f) - i);
                        if (!tile.HasTile)
                        {
                            inGround = false;
                            break;
                        }
                    }

                    // Prepare for the charge.
                    if (inGround && attackTimer > 50f)
                    {
                        chargeDirection = Math.Sign(target.Center.X - npc.Center.X);
                        attackState = 1f;
                        attackTimer = 0f;
                        npc.netUpdate = true;
                    }
                    break;

                // Charge underground.
                case 1:
                    // Constantly approach 0 vertical movement via linear interpolation.
                    npc.velocity.Y = Lerp(npc.velocity.Y, 0f, 0.175f);

                    // And set the horizontal charge speed.
                    npc.velocity.X = Lerp(npc.velocity.X, chargeSpeed * chargeDirection, 0.08f);

                    // Roar if first frame
                    if (attackTimer == 1)
                        SoundEngine.PlaySound(DesertScourgeHead.RoarSound, npc.Center);

                    // Release sand upward.
                    if (attackTimer % sandCreationRate == sandCreationRate - 1f)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 sandShootVelocity = -Vector2.UnitY.RotatedByRandom(0.22f) * Main.rand.NextFloat(8f, 11f);
                            Utilities.NewProjectileBetter(npc.Center, sandShootVelocity, ModContent.ProjectileType<SandBlastInfernum>(), SandBlastDamage, 0f);
                            Utilities.NewProjectileBetter(npc.Center, -sandShootVelocity, ModContent.ProjectileType<SandBlastInfernum>(), SandBlastDamage, 0f);

                            for (int i = 0; i < 5; i++)
                                CreateSandParticles(npc, Color.White, sandShootVelocity, npc.Center);
                        }

                        // Emit strong dust bursts upward.
                        for (int i = 0; i < 80; i++)
                        {
                            if (Distance(target.Center.X, npc.Center.X) >= 900f)
                                break;

                            Vector2 air = Utilities.GetGroundPositionFrom(npc.Center, new Searches.Up(9000)) - Vector2.UnitY * 32f;
                            Dust sand = Dust.NewDustPerfect(air, 32);
                            sand.velocity = -Vector2.UnitY.RotatedByRandom(0.22f) * (i * 0.85f + 4f);
                            sand.scale = Main.rand.NextFloat(0.8f, 1f) + i * 0.024f;
                            sand.fadeIn = -1f;
                            sand.noGravity = true;
                        }

                        SoundEngine.PlaySound(SoundID.Item21, npc.Center);
                    }

                    if (attackTimer > 360f || Distance(target.Center.X, npc.Center.X) > 1950f)
                        SelectNextAttack(npc);
                    break;
            }

            // Calculate rotation.
            npc.rotation = npc.velocity.ToRotation() + PiOver2;
        }

        public static void DoBehavior_SandstormParticles(NPC npc, Player target, bool enraged, ref float attackTimer)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            int sandParticleReleaseRate = (int)Math.Round(Lerp(19f, 14f, 1f - lifeRatio));
            float sandParticleSpeed = 9.5f;
            float idealFlySpeed = Lerp(4f, 7f, 1f - lifeRatio) + npc.Distance(target.Center) * 0.011f;
            if (enraged)
            {
                sandParticleReleaseRate /= 2;
                sandParticleSpeed *= 1.4f;
            }

            float maxChargeSpeed = idealFlySpeed * 1.54f;
            float flyAcceleration = idealFlySpeed / 710f;

            // Play the wind sound on first frame

            if (attackTimer == 1)
                SoundEngine.PlaySound(InfernumSoundRegistry.DesertScourgeSandstormWindSound with { Volume = 0.85f }, target.Center);

            // Accelerate if close to the target.
            if (npc.WithinRange(target.Center, 250f))
            {
                if (npc.velocity.Length() < 3f)
                    npc.velocity = Vector2.UnitY * 3f;

                if (npc.velocity.Length() < maxChargeSpeed)
                    npc.velocity *= 1f + flyAcceleration * 0.64f;
            }

            // Otherwise fly towards them and release bursts of sand.
            else
            {
                float flySpeed = Lerp(npc.velocity.Length(), idealFlySpeed, 0.1f);
                Vector2 idealVelocity = npc.SafeDirectionTo(target.Center) * flySpeed;
                npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), flyAcceleration);
                npc.velocity = npc.velocity.MoveTowards(idealVelocity, flyAcceleration * 15f);
            }

            // Calculate rotation.
            npc.rotation = npc.velocity.ToRotation() + PiOver2;

            // Create the sandstorm.
            Vector2 spawnPosition = target.Center + new Vector2(Main.rand.NextBool().ToDirectionInt() * 1000f, Main.rand.NextFloat(-1020f, 850f));
            Vector2 sandShootVelocity = (target.Center - spawnPosition).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.16f);
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % sandParticleReleaseRate == sandParticleReleaseRate - 1f)
            {
                sandShootVelocity = (sandShootVelocity * new Vector2(0.33f, 1f)).SafeNormalize(Vector2.UnitY) * sandParticleSpeed;

                for (int i = 0; i < 2; i++)
                    Utilities.NewProjectileBetter(spawnPosition + Main.rand.NextVector2Circular(120f, 120f), sandShootVelocity, ModContent.ProjectileType<SandstormBlast>(), SandBlastDamage, 0f);
            }
            Vector2 sandPosition = target.Center + new Vector2(Main.rand.NextBool().ToDirectionInt() * 1000f, Main.rand.NextFloat(-850f, 850f));
            Vector2 sandVelocity = new Vector2(target.Center.X - sandPosition.X, 0).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.16f);
            for (int j = 0; j < 4; j++)
                CreateSandParticles(npc, Color.White * 0.75f, sandVelocity * 40, sandPosition, 60, Main.rand.NextFloat(1.2f, 1.4f));

            if (attackTimer > 480f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_GroundSlam(NPC npc, Player target, bool enraged, ref float attackTimer)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            int totalSlams = 2;
            float upwardFlySpeed = 16f;
            float slamSpeed = 21f;
            int sandBurstCount = (int)Lerp(20f, 32f, 1f - lifeRatio);
            float sandBurstSpeed = Lerp(13f, 18f, 1f - lifeRatio);
            if (enraged)
            {
                sandBurstCount += 10;
                sandBurstSpeed *= 1.6f;
            }

            ref float attackState = ref npc.Infernum().ExtraAI[0];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[1];

            switch ((int)attackState)
            {
                // Fly upward for a time.
                case 0:
                    // Fly upward.
                    npc.velocity.Y = Lerp(npc.velocity.Y, -upwardFlySpeed, 0.08f);

                    // Try to stay close to the target horizontally.
                    if (Distance(target.Center.X, npc.Center.X) > 400f)
                    {
                        float idealHorizontalSpeed = npc.SafeDirectionTo(target.Center).X * 12.5f;
                        npc.velocity.X = (npc.velocity.X * 14f + idealHorizontalSpeed) / 15f;
                    }

                    // Roar as a telegraph.
                    if (attackTimer == 85f)
                        SoundEngine.PlaySound(DesertScourgeHead.RoarSound, target.Center);

                    // Slam downward.
                    if (attackTimer > 110f)
                    {
                        npc.velocity.Y *= 0.6f;
                        attackState = 1f;
                        attackTimer = 0f;
                        npc.netUpdate = true;
                    }
                    break;

                // Slam into the ground.
                case 1:
                    // Fly downward.
                    npc.velocity.Y = Lerp(npc.velocity.Y, slamSpeed, 0.08f);

                    // Slow down horizontally if moving fast enough.
                    if (Math.Abs(npc.velocity.X) > 6.66f)
                        npc.velocity.X *= 0.975f;

                    // Create a bunch of sandstorms that traverse the landscape and some sand blasts that expand outward.
                    if (Main.netMode != NetmodeID.MultiplayerClient && Collision.SolidCollision(npc.position, npc.width, npc.height) && attackTimer < 240f)
                    {
                        attackTimer = 240f;
                        npc.velocity.Y *= 0.5f;
                        // Play impact sound.
                        SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode, npc.Center);
                        // Create the sand burst.
                        for (int i = 0; i < sandBurstCount; i++)
                        {
                            Vector2 sandShootVelocity = (TwoPi * i / sandBurstCount).ToRotationVector2() * sandBurstSpeed * Main.rand.NextFloat(0.7f, 1f);
                            Utilities.NewProjectileBetter(npc.Center, sandShootVelocity, ModContent.ProjectileType<SandBlastInfernum>(), SandBlastDamage, 0f);
                            for (int j = 0; j < 2; j++)
                                CreateSandParticles(npc, Color.White, sandShootVelocity, npc.Center);
                        }

                        // Create the tornadoes.
                        Vector2 tornadoVelocity = Vector2.UnitX * 4f;
                        Utilities.NewProjectileBetter(npc.Center, tornadoVelocity, ModContent.ProjectileType<Sandnado>(), SandnadoDamage, 0f);
                        Utilities.NewProjectileBetter(npc.Center, -tornadoVelocity, ModContent.ProjectileType<Sandnado>(), SandnadoDamage, 0f);

                        npc.netUpdate = true;
                    }

                    if (attackTimer > 260f)
                    {
                        chargeCounter++;
                        if (chargeCounter >= totalSlams)
                            SelectNextAttack(npc);
                        else
                        {
                            attackState = 0f;
                            attackTimer = 0f;
                        }
                    }
                    break;
            }

            // Calculate rotation.
            npc.rotation = npc.velocity.ToRotation() + PiOver2;
        }

        public static void DoBehavior_SummonVultures(NPC npc, Player target, ref float attackTimer)
        {
            int vultureSummonDelay = 45;
            int attackSwitchDelay = 30;
            float idealFlySpeed = npc.Distance(target.Center) * 0.012f + 7f;
            float maxChargeSpeed = idealFlySpeed * 1.54f;
            float flyAcceleration = idealFlySpeed / 710f;

            // Accelerate if close to the target.
            if (npc.WithinRange(target.Center, 250f))
            {
                if (npc.velocity.Length() < 3f)
                    npc.velocity = Vector2.UnitY * 3f;

                if (npc.velocity.Length() < maxChargeSpeed)
                    npc.velocity *= 1f + flyAcceleration * 0.64f;
            }

            // Otherwise fly towards them and release bursts of sand.
            else
            {
                float flySpeed = Lerp(npc.velocity.Length(), idealFlySpeed, 0.1f);
                Vector2 idealVelocity = npc.SafeDirectionTo(target.Center) * flySpeed;
                npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), flyAcceleration);
                npc.velocity = npc.velocity.MoveTowards(idealVelocity, flyAcceleration * 15f);
            }

            // Summon vultures.
            if (attackTimer == vultureSummonDelay)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.DesertScourgeShortRoar, npc.Center);
                HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.DSFinalPhaseTip");
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        // Prevent NPC spam if there's more than 8 vultures present.
                        if (NPC.CountNPCS(NPCID.Vulture) >= 8)
                            break;

                        Vector2 vultureSpawnPosition = target.Center + new Vector2(Lerp(-600f, 600f, i / 2f), -500f);
                        NPC.NewNPC(npc.GetSource_FromAI(), (int)vultureSpawnPosition.X, (int)vultureSpawnPosition.Y, NPCID.Vulture);
                    }
                }
            }

            // Calculate rotation.
            npc.rotation = npc.velocity.ToRotation() + PiOver2;

            if (attackTimer > vultureSummonDelay + attackSwitchDelay)
                SelectNextAttack(npc);
        }

        public static void SelectNextAttack(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            DesertScourgeAttackType oldAttack = (DesertScourgeAttackType)(int)npc.ai[0];
            List<DesertScourgeAttackType> potentialAttacks =
            [
                DesertScourgeAttackType.SandSpit,
                DesertScourgeAttackType.SandRushCharge,
                DesertScourgeAttackType.SandstormParticles,
            ];

            if (lifeRatio < Phase2LifeRatio)
                potentialAttacks.Add(DesertScourgeAttackType.GroundSlam);
            if (lifeRatio < Phase3LifeRatio)
            {
                potentialAttacks.Add(DesertScourgeAttackType.SummonVultures);
                potentialAttacks.Add(DesertScourgeAttackType.SummonVultures);
            }

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            do
                npc.ai[0] = (int)Main.rand.Next(potentialAttacks);
            while ((int)oldAttack == (int)npc.ai[0] && potentialAttacks.Count >= 2);

            if (oldAttack == DesertScourgeAttackType.SpawnAnimation)
                npc.ai[0] = (int)DesertScourgeAttackType.SandSpit;

            npc.TargetClosest();
            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }

        public static void DoBehavior_Despawn(NPC npc)
        {
            npc.velocity.X *= 0.985f;
            if (npc.velocity.Y < 26f)
                npc.velocity.Y += 0.4f;

            if (npc.timeLeft > 200)
                npc.timeLeft = 200;

            // If someone has instant respawn and complains about the despawn not working I will break their spine :)
            if (!npc.WithinRange(Main.player[npc.target].Center, 1500f))
                npc.active = false;
        }

        public static void CreateSegments(NPC npc, int wormLength, int bodyType, int tailType)
        {
            int previousIndex = npc.whoAmI;
            for (int i = 0; i < wormLength; i++)
            {
                int nextIndex;
                if (i < wormLength - 1)
                    nextIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, bodyType, npc.whoAmI);
                else
                    nextIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, tailType, npc.whoAmI);

                Main.npc[nextIndex].realLife = npc.whoAmI;
                Main.npc[nextIndex].ai[2] = npc.whoAmI;
                Main.npc[nextIndex].ai[1] = previousIndex;

                if (i > 0)
                    Main.npc[previousIndex].ai[0] = nextIndex;

                // Force sync the new segment into existence.
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, nextIndex, 0f, 0f, 0f, 0);

                previousIndex = nextIndex;
            }
        }

        public static void CreateSandParticles(NPC npc, Color color, Vector2? velocity = default, Vector2? spawnPosition = null, int lifeTime = 60, float? scale = null)
        {
            // Allow use of custom velocity for specific movement.
            spawnPosition ??= npc.Center + Main.rand.NextVector2Circular(70, 70) + npc.velocity * 2f;
            velocity ??= -npc.velocity.SafeNormalize(Vector2.UnitX * npc.spriteDirection) * Main.rand.NextFloat(6f, 8.75f);
            scale ??= Main.rand.NextFloat(0.85f, 1.1f);

            Particle sand = new DesertScourgeSandstormParticle(spawnPosition.Value, velocity.Value, color, scale.Value, lifeTime);
            GeneralParticleHandler.SpawnParticle(sand);
        }
        #endregion

        #region Drawing
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.Draw(BossTextureRegistry.DesertScourgeHead.Value, npc.Center - Main.screenPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            return false;
        }
        #endregion

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Mods.InfernumMode.PetDialog.DSTip1";
            yield return n => "Mods.InfernumMode.PetDialog.DSTip2";

            yield return n =>
            {
                if (TipsManager.ShouldUseJokeText)
                    return "Mods.InfernumMode.PetDialog.DSJokeTip1";
                return string.Empty;
            };
        }
        #endregion
    }
}
