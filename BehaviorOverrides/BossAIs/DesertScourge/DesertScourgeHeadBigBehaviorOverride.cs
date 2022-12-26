using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs.DesertScourge;
using CalamityMod.Particles;
using InfernumMode.OverridingSystem;
using InfernumMode.Particles;
using InfernumMode.Sounds;
using InfernumMode.Systems;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.DesertScourge
{
    public class DesertScourgeHeadBigBehaviorOverride : NPCBehaviorOverride
    {
        public enum DesertScourgeAttackType
        {
            SandSpit,
            SandRushCharge,
            SandstormParticles,
            GroundSlam,
            SummonVultures
        }

        public const float Phase2LifeRatio = 0.55f;

        public const float Phase3LifeRatio = 0.25f;

        public override int NPCOverrideType => ModContent.NPCType<DesertScourgeHead>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio,
            Phase3LifeRatio
        };
        #region AI
        public override bool PreAI(NPC npc)
        {
            npc.damage = 100;
            
            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();

            npc.alpha = Utils.Clamp(npc.alpha - 20, 0, 255);

            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float initializedFlag = ref npc.ai[2];
            ref float enrageTimer = ref npc.ai[3];

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
            enrageTimer = MathHelper.Clamp(enrageTimer + outOfBiome.ToDirectionInt(), 0f, 720f);
            bool enraged = enrageTimer > 660f;

            npc.defense = npc.defDefense;
            npc.Calamity().CurrentlyEnraged = outOfBiome;
            
            switch ((DesertScourgeAttackType)(int)attackType)
            {
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

        public static void DoBehavior_SandSpit(NPC npc, Player target, bool enraged, ref float attackTimer)
        {
            // Attempt to rush the target.
            int sandPerBurst = 7;
            int sandBurstShootRate = 80;
            float sandBurstSpeed = 11.25f;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float idealFlySpeed = MathHelper.Lerp(10f, 13.15f, 1f - lifeRatio) + npc.Distance(target.Center) * 0.012f;
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
                float flySpeed = MathHelper.Lerp(npc.velocity.Length(), idealFlySpeed, 0.1f);
                Vector2 idealVelocity = npc.SafeDirectionTo(target.Center) * flySpeed;
                npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), flyAcceleration);
                npc.velocity = npc.velocity.MoveTowards(idealVelocity, flyAcceleration * 15f);

                // Release bursts of sand periodically.
                if (Main.netMode != NetmodeID.MultiplayerClient && !npc.WithinRange(target.Center, 325f) && attackTimer % sandBurstShootRate == sandBurstShootRate - 1f)
                {
                    for (int i = 0; i < sandPerBurst; i++)
                    {
                        Vector2 sandShootVelocity = (MathHelper.TwoPi * i / sandPerBurst).ToRotationVector2() * sandBurstSpeed;
                        Vector2 spawnPosition = npc.Center + sandShootVelocity * 2.5f;
                        Utilities.NewProjectileBetter(spawnPosition, sandShootVelocity, ModContent.ProjectileType<SandBlastInfernum>(), 80, 0f);

                        for (int j = 0; j < 5; j++)
                            CreateSandParticles(npc, Color.White, sandShootVelocity, npc.Center);
                        SoundEngine.PlaySound(SoundID.Item21, spawnPosition);
                    }
                }
            }

            // Calculate rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

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
                        acceleration = MathHelper.Lerp(-0.72f, 0f, attackTimer / 30f);
                    else if (attackTimer < 70f)
                        acceleration = MathHelper.Lerp(0f, 0.38f, Utils.GetLerpValue(30f, 70f, attackTimer, true));
                    npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + acceleration, -10f, 16f);

                    // Try to stay close to the target horizontally.
                    if (MathHelper.Distance(target.Center.X, npc.Center.X) > 500f)
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
                        Tile tile = CalamityUtils.ParanoidTileRetrieval((int)(npc.Center.X / 16f), (int)(npc.Center.Y / 16f) - i);
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
                    npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, 0f, 0.175f);

                    // And set the horizontal charge speed.
                    npc.velocity.X = MathHelper.Lerp(npc.velocity.X, chargeSpeed * chargeDirection, 0.08f);

                    // Roar if first frame
                    if (attackTimer == 1)
                        SoundEngine.PlaySound(DesertScourgeHead.RoarSound, npc.Center);

                    // Release sand upward.
                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % sandCreationRate == sandCreationRate - 1f)
                    {
                        Vector2 sandShootVelocity = -Vector2.UnitY.RotatedByRandom(0.41f) * Main.rand.NextFloat(9.25f, 13.5f);
                        Utilities.NewProjectileBetter(npc.Center, sandShootVelocity, ModContent.ProjectileType<SandBlastInfernum>(), 75, 0f);
                        Utilities.NewProjectileBetter(npc.Center, -sandShootVelocity, ModContent.ProjectileType<SandBlastInfernum>(), 75, 0f);

                        for(int i = 0; i < 5; i++)
                            CreateSandParticles(npc, Color.White, sandShootVelocity, npc.Center);
                        SoundEngine.PlaySound(SoundID.Item21, npc.Center);
                    }

                    if (attackTimer > 360f || MathHelper.Distance(target.Center.X, npc.Center.X) > 1950f)
                        SelectNextAttack(npc);
                    break;
            }

            // Calculate rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public static void DoBehavior_SandstormParticles(NPC npc, Player target, bool enraged, ref float attackTimer)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            int sandParticleReleaseRate = (int)Math.Round(MathHelper.Lerp(22f, 16f, 1f - lifeRatio));
            float sandParticleSpeed = 9.5f;
            float idealFlySpeed = MathHelper.Lerp(5f, 8f, 1f - lifeRatio) + npc.Distance(target.Center) * 0.012f;
            if (enraged)
            {
                sandParticleReleaseRate /= 2;
                sandParticleSpeed *= 1.4f;
            }

            float maxChargeSpeed = idealFlySpeed * 1.54f;
            float flyAcceleration = idealFlySpeed / 710f;

            // Play the wind sound on first frame

            if (attackTimer == 1)
                SoundEngine.PlaySound(InfernumSoundRegistry.DesertScourgeSandstormWindSound with { Volume = 0.85f}, target.Center);

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
                float flySpeed = MathHelper.Lerp(npc.velocity.Length(), idealFlySpeed, 0.1f);
                Vector2 idealVelocity = npc.SafeDirectionTo(target.Center) * flySpeed;
                npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), flyAcceleration);
                npc.velocity = npc.velocity.MoveTowards(idealVelocity, flyAcceleration * 15f);
            }

            // Calculate rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            // Create the sandstorm.
            Vector2 spawnPosition = target.Center + new Vector2(Main.rand.NextBool().ToDirectionInt() * 1000f, Main.rand.NextFloat(-850f, 850f));
            Vector2 sandShootVelocity = (target.Center - spawnPosition).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.16f);
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % sandParticleReleaseRate == sandParticleReleaseRate - 1f)
            {
                
                sandShootVelocity = (sandShootVelocity * new Vector2(0.33f, 1f)).SafeNormalize(Vector2.UnitY) * sandParticleSpeed;

                for (int i = 0; i < 2; i++)
                    Utilities.NewProjectileBetter(spawnPosition + Main.rand.NextVector2Circular(120f, 120f), sandShootVelocity, ModContent.ProjectileType<SandstormBlast>(), 75, 0f);
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
            int sandBurstCount = (int)MathHelper.Lerp(20f, 32f, 1f - lifeRatio);
            float sandBurstSpeed = MathHelper.Lerp(13f, 18f, 1f - lifeRatio);
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
                    npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, -upwardFlySpeed, 0.08f);

                    // Try to stay close to the target horizontally.
                    if (MathHelper.Distance(target.Center.X, npc.Center.X) > 400f)
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
                    npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, slamSpeed, 0.08f);

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
                            Vector2 sandShootVelocity = (MathHelper.TwoPi * i / sandBurstCount).ToRotationVector2() * sandBurstSpeed * Main.rand.NextFloat(0.7f, 1f);
                            Utilities.NewProjectileBetter(npc.Center, sandShootVelocity, ModContent.ProjectileType<SandBlastInfernum>(), 75, 0f);
                            for(int j = 0; j < 2; j++)
                                CreateSandParticles(npc, Color.White, sandShootVelocity, npc.Center);
                        }

                        // Create the tornadoes.
                        for (int i = 0; i < 5; i++)
                        {
                            Vector2 tornadoVelocity = Vector2.UnitX * MathHelper.Lerp(6f, 14.75f, i / 4f);
                            Utilities.NewProjectileBetter(npc.Center, tornadoVelocity, ModContent.ProjectileType<Sandnado>(), 105, 0f);
                            Utilities.NewProjectileBetter(npc.Center, -tornadoVelocity, ModContent.ProjectileType<Sandnado>(), 105, 0f);
                        }

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
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
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
                float flySpeed = MathHelper.Lerp(npc.velocity.Length(), idealFlySpeed, 0.1f);
                Vector2 idealVelocity = npc.SafeDirectionTo(target.Center) * flySpeed;
                npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), flyAcceleration);
                npc.velocity = npc.velocity.MoveTowards(idealVelocity, flyAcceleration * 15f);
            }

            // Summon vultures.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == vultureSummonDelay)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.DesertScourgeShortRoar, npc.Center);
                for (int i = 0; i < 3; i++)
                {
                    // Prevent NPC spam if there's more than 8 vultures present.
                    if (NPC.CountNPCS(NPCID.Vulture) >= 8)
                        break;

                    Vector2 vultureSpawnPosition = target.Center + new Vector2(MathHelper.Lerp(-600f, 600f, i / 2f), -500f);
                    NPC.NewNPC(npc.GetSource_FromAI(), (int)vultureSpawnPosition.X, (int)vultureSpawnPosition.Y, NPCID.Vulture);
                }
            }

            // Calculate rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            if (attackTimer > vultureSummonDelay + attackSwitchDelay)
                SelectNextAttack(npc);
        }

        public static void SelectNextAttack(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            DesertScourgeAttackType oldAttack = (DesertScourgeAttackType)(int)npc.ai[0];
            List<DesertScourgeAttackType> potentialAttacks = new()
            {
                DesertScourgeAttackType.SandSpit,
                DesertScourgeAttackType.SandRushCharge,
                DesertScourgeAttackType.SandstormParticles,
            };

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
        #endregion
        #region Drawing
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
        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "The Scourge usually roars when it's about to whip up a sandstorm, get to high grounds!";
            yield return n => "A Hook may prove useful to quickly get out of the Scourge's mandibles!";
            yield return n =>
            {
                if (HatGirlTipsManager.ShouldUseJokeText)
                    return "I loath sand, its grainy and itchy and sticks to every part of my feet.";
                return string.Empty;
            };
            yield return n =>
            {
                if (HatGirlTipsManager.ShouldUseJokeText)
                    return "You better have dessert for me after this...";
                return string.Empty;
            };
        }
        #endregion
    }
}
