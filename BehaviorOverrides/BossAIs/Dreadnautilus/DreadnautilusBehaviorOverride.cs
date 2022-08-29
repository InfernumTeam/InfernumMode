using CalamityMod;
using CalamityMod.Particles;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Dreadnautilus
{
    public class DreadnautilusBehaviorOverride : NPCBehaviorOverride
    {
        public enum DreadnautilusAttackState
        {
            InitialSummonDelay,
            BloodSpitToothBalls,
            EyeGleamEyeFishSummon,
            RandomBloodBurstSpread,
            EquallySpreadBloodBolts,
            HorizontalCharge,
            SanguineBatSwarm,
            SquidGames
        }

        public override int NPCOverrideType => NPCID.BloodNautilus;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCSetDefaults | NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public const float Phase2LifeRatio = 0.55f;

        public const float Phase3LifeRatio = 0.25f;

        public override void SetDefaults(NPC npc)
        {
            NPCID.Sets.TrailCacheLength[npc.type] = 8;
            NPCID.Sets.TrailingMode[npc.type] = 1;

            npc.boss = true;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.npcSlots = 15f;
            npc.damage = 130;
            npc.width = 100;
            npc.height = 100;
            npc.defense = 20;
            npc.DR_NERD(0.25f);
            npc.LifeMaxNERB(33000, 33000);
            npc.lifeMax /= 2;
            npc.aiStyle = -1;
            npc.knockBackResist = 0f;
            npc.value = Item.buyPrice(0, 10, 0, 0);
            npc.rarity = 1;
            for (int k = 0; k < npc.buffImmune.Length; k++)
                npc.buffImmune[k] = true;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.timeLeft = NPC.activeTime * 30;
            npc.Calamity().canBreakPlayerDefense = true;
        }

        public override bool PreAI(NPC npc)
        {
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            npc.ai[3] = 1f;
            npc.dontTakeDamage = false;

            bool phase2 = npc.life < npc.lifeMax * Phase2LifeRatio;
            bool phase3 = npc.life < npc.lifeMax * Phase3LifeRatio;
            ref float attackTimer = ref npc.ai[1];
            ref float eyeGleamInterpolant = ref npc.ai[2];

            switch ((DreadnautilusAttackState)npc.ai[0])
            {
                case DreadnautilusAttackState.InitialSummonDelay:
                    DoBehavior_InitialSummonDelay(npc, target, ref attackTimer);
                    break;
                case DreadnautilusAttackState.BloodSpitToothBalls:
                    DoBehavior_BloodSpitToothBalls(npc, target, phase2, phase3, ref attackTimer);
                    break;
                case DreadnautilusAttackState.EyeGleamEyeFishSummon:
                    DoBehavior_EyeGleamEyeFishSummon(npc, target, phase2, phase3, ref attackTimer, ref eyeGleamInterpolant);
                    break;
                case DreadnautilusAttackState.RandomBloodBurstSpread:
                    DoBehavior_RandomBloodBurstSpread(npc, target, phase2, phase3, ref attackTimer);
                    break;
                case DreadnautilusAttackState.EquallySpreadBloodBolts:
                    DoBehavior_EquallySpreadBloodBolts(npc, target, phase2, phase3, ref attackTimer);
                    break;
                case DreadnautilusAttackState.HorizontalCharge:
                    DoBehavior_HorizontalCharge(npc, target, phase3, ref attackTimer);
                    break;
                case DreadnautilusAttackState.SanguineBatSwarm:
                    DoBehavior_SanguineBatSwarm(npc, target, phase3, ref attackTimer, ref eyeGleamInterpolant);
                    break;
                case DreadnautilusAttackState.SquidGames:
                    DoBehavior_SquidGames(npc, target, phase3, ref attackTimer);
                    break;
            }
            attackTimer++;
            return false;
        }

        public static void DoBehavior_InitialSummonDelay(NPC npc, Player target, ref float attackTimer)
        {
            npc.velocity *= 0.98f;
            int direction = -Math.Sign(target.Center.X - npc.Center.X);
            if (direction != 0)
                npc.spriteDirection = direction;

            // Create a ring of blood particles on the first frame.
            if (attackTimer == 1f)
            {
                for (int i = 0; i < 36; i++)
                {
                    Vector2 velocityDirection = (npc.velocity.SafeNormalize(Vector2.UnitY) * new Vector2(npc.width / 2f, (float)npc.height) * 0.75f * 0.5f).RotatedBy(MathHelper.TwoPi * i / 36f);
                    Dust blood = Dust.NewDustDirect(npc.Center, 0, 0, 5, 0f, 0f, 100, default, 1.4f);
                    blood.velocity = velocityDirection.SafeNormalize(Vector2.UnitY) * 3f;
                    blood.noGravity = true;
                }
            }

            // Rise into the air and handle fade shortly after appearing.
            if (attackTimer > 5f)
            {
                npc.velocity.Y = -2.5f;
                npc.alpha = Utils.Clamp(npc.alpha - 10, 0, 150);
                if (Collision.SolidCollision(npc.position, npc.width, npc.height))
                    npc.alpha = Utils.Clamp(npc.alpha + 15, 0, 150);
            }

            // Transition to the first attack after a short period of time has passed.
            if (attackTimer >= 50f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_BloodSpitToothBalls(NPC npc, Player target, bool phase2, bool phase3, ref float attackTimer)
        {
            int shootCycleTime = 80;
            int shootPrepareTime = 30;
            int shotCount = 3;

            if (phase2)
            {
                shootCycleTime += 15;
                shotCount++;
            }
            if (phase3)
                shotCount++;

            float wrappedTime = attackTimer % shootCycleTime;
            bool preparingToShoot = wrappedTime > shootCycleTime - shootPrepareTime;

            // Turn with 100% sharpness when not preparing to shoot.
            // If about to shoot however, have the sharpness fall off until eventually there is no more aiming.
            float angleTurnSharpness = 1f - Utils.GetLerpValue(shootCycleTime - 25f, shootCycleTime - 3f, wrappedTime, true);

            // Have the mouth face towards the target.
            npc.direction = (npc.Center.X < target.Center.X).ToDirectionInt();
            float idealRotation = npc.AngleTo(target.Center) - MathHelper.Pi * npc.spriteDirection * 0.15f;
            if (npc.spriteDirection == -1)
                idealRotation += MathHelper.Pi;

            if (npc.spriteDirection != npc.direction)
            {
                npc.spriteDirection = npc.direction;
                npc.rotation = -npc.rotation;
                idealRotation = -idealRotation;
            }
            if (angleTurnSharpness > 0f)
                npc.rotation = npc.rotation.AngleLerp(idealRotation, angleTurnSharpness);

            // Have the movement fall off quickly when preparing to shoot.
            if (preparingToShoot)
            {
                npc.velocity *= 0.97f;
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.Zero, 0.04f);
                if (npc.velocity.Length() < 0.03f)
                    npc.velocity = Vector2.Zero;

                // Release a burst of spit balls right before the end of the cycle.
                if (wrappedTime == shootCycleTime - 1f)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        npc.BloodNautilus_GetMouthPositionAndRotation(out Vector2 shootPosition, out Vector2 shootDirection);
                        for (int i = 0; i < 2; i++)
                        {
                            Vector2 shootVelocity = shootDirection.RotatedByRandom(0.33f) * Main.rand.NextFloat(11.5f, 15f);
                            Utilities.NewProjectileBetter(shootPosition, shootVelocity, ModContent.ProjectileType<GoreSpitBall>(), 120, 0f, npc.target);
                        }

                        // Rebound backward.
                        float aimAwayFromTargetInterpolant = Utils.GetLerpValue(250f, 185f, npc.Distance(target.Center), true);
                        float reboundSpeed = Utils.Remap(npc.Distance(target.Center), 500f, 100f, 5f, 16f);
                        Vector2 reboundDirection = Vector2.Lerp(shootDirection, npc.SafeDirectionTo(target.Center), aimAwayFromTargetInterpolant).SafeNormalize(Vector2.UnitY);
                        npc.velocity -= reboundDirection * reboundSpeed;

                        // And sync the NPC, to catch potential accumulating desyncs.
                        npc.netUpdate = true;
                    }

                    // Play a split sound.
                    SoundEngine.PlaySound(SoundID.Item17, npc.Center);
                }
            }

            // Move towards the closest side to the target. Also have a slight upward offset at said destination.
            // This movement becomes extremely fast if notably far from the destination.
            else
            {
                Vector2 hoverDestination = target.Center + new Vector2((npc.Center.X > target.Center.X).ToDirectionInt() * 325f, -60f);
                float distanceToDestination = npc.Distance(hoverDestination);
                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(distanceToDestination, 10f);
                npc.SimpleFlyMovement(Vector2.Lerp(idealVelocity, (hoverDestination - npc.Center) * 0.15f, Utils.GetLerpValue(180f, 540f, distanceToDestination, true)), 0.4f);
            }

            if (attackTimer >= shotCount * shootCycleTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_EyeGleamEyeFishSummon(NPC npc, Player target, bool phase2, bool phase3, ref float attackTimer, ref float eyeGleamInterpolant)
        {
            int slowdownTime = 50;
            int summonTime = 65;
            int phaseTransitionDelay = 125;
            int fishSummonCount = 1;
            int maxFishAtOnce = 3;

            if (phase2)
                summonTime -= 15;
            if (phase3)
                fishSummonCount++;

            // Slow down, look at the target, and create the gleam effect.
            npc.direction = (npc.Center.X < target.Center.X).ToDirectionInt();
            float idealRotation = npc.AngleTo(target.Center) - MathHelper.Pi * npc.spriteDirection * 0.15f;
            if (npc.spriteDirection == -1)
                idealRotation += MathHelper.Pi;

            if (npc.spriteDirection != npc.direction)
            {
                npc.spriteDirection = npc.direction;
                npc.rotation = -npc.rotation;
                idealRotation = -idealRotation;
            }
            npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.035f);

            if (attackTimer < slowdownTime)
            {
                eyeGleamInterpolant = Utils.GetLerpValue(16f, slowdownTime - 1f, attackTimer, true);
                npc.velocity *= 0.975f;
            }

            // Increase the intensity of the gleam and summon wandering eye fish from the sky.
            else if (attackTimer < slowdownTime + summonTime)
            {
                npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, -6f, 0.02f);
                float gleamInterpolant = Utils.GetLerpValue(0f, summonTime, attackTimer - slowdownTime, true);
                eyeGleamInterpolant = 1f + CalamityUtils.Convert01To010(gleamInterpolant) * 0.6f;

                // Stop the attack early if hit in time.
                if (npc.justHit)
                {
                    eyeGleamInterpolant = 0f;
                    SoundEngine.PlaySound(SoundID.NPCHit35 with { Volume = 1.75f, Pitch = -0.85f }, npc.Center);

                    attackTimer = slowdownTime + summonTime + phaseTransitionDelay - 45f;
                    npc.velocity = npc.SafeDirectionTo(target.Center) * -27f;
                    npc.netUpdate = true;

                    // Create a lot of blood as an indicator.
                    Vector2 bloodSpawnPosition = target.Center + Main.rand.NextVector2Circular(target.width, target.height) * 0.04f;
                    Vector2 splatterDirection = (target.Center - bloodSpawnPosition).SafeNormalize(Vector2.UnitY);
                    SoundEngine.PlaySound(SoundID.NPCHit18, npc.Center);
                    for (int i = 0; i < 21; i++)
                    {
                        int bloodLifetime = Main.rand.Next(22, 36);
                        float bloodScale = Main.rand.NextFloat(0.6f, 0.8f);
                        Color bloodColor = Color.Lerp(Color.Red, Color.DarkRed, Main.rand.NextFloat());
                        bloodColor = Color.Lerp(bloodColor, new Color(51, 22, 94), Main.rand.NextFloat(0.65f));

                        if (Main.rand.NextBool(20))
                            bloodScale *= 2f;

                        Vector2 bloodVelocity = splatterDirection.RotatedByRandom(0.81f) * Main.rand.NextFloat(11f, 23f);
                        bloodVelocity.Y -= 12f;
                        BloodParticle blood = new(bloodSpawnPosition, bloodVelocity, bloodLifetime, bloodScale, bloodColor);
                        GeneralParticleHandler.SpawnParticle(blood);
                    }
                    for (int i = 0; i < 10; i++)
                    {
                        float bloodScale = Main.rand.NextFloat(0.2f, 0.33f);
                        Color bloodColor = Color.Lerp(Color.Red, Color.DarkRed, Main.rand.NextFloat(0.5f, 1f));
                        Vector2 bloodVelocity = splatterDirection.RotatedByRandom(0.9f) * Main.rand.NextFloat(9f, 14.5f);
                        BloodParticle2 blood = new(bloodSpawnPosition, bloodVelocity, 20, bloodScale, bloodColor);
                        GeneralParticleHandler.SpawnParticle(blood);
                    }

                    return;
                }

                // Summon wandering eye fish.
                if (attackTimer == slowdownTime + summonTime - 8)
                {
                    SoundEngine.PlaySound(SoundID.Item122, npc.Center);
                    SoundEngine.PlaySound(SoundID.Item170, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient && NPC.CountNPCS(NPCID.EyeballFlyingFish) + fishSummonCount <= maxFishAtOnce)
                    {
                        for (int i = 0; i < fishSummonCount; i++)
                        {
                            for (int j = 0; j < 500; j++)
                            {
                                float horizontalOffset = Main.rand.NextFloat(360f, 800f) * Main.rand.NextBool().ToDirectionInt();
                                float verticalOffset = Main.rand.NextFloat(-550f, -420f);
                                Vector2 potentialSpawnPosition = target.Center + new Vector2(horizontalOffset, verticalOffset);
                                if (!Collision.SolidCollision(potentialSpawnPosition - Vector2.One * 50f, 100, 100) && Collision.CanHit(potentialSpawnPosition, 1, 1, target.Center, 1, 1))
                                {
                                    NPC.NewNPC(npc.GetSource_FromAI(), (int)potentialSpawnPosition.X, (int)potentialSpawnPosition.Y, NPCID.EyeballFlyingFish, npc.whoAmI);
                                    break;
                                }
                            }
                        }
                        npc.velocity = Vector2.Zero;
                        npc.netUpdate = true;
                    }
                }
            }

            // Make the gleam fade away again and eventually transition to the next attack.
            else if (attackTimer < slowdownTime + summonTime + phaseTransitionDelay)
            {
                eyeGleamInterpolant = MathHelper.Clamp(eyeGleamInterpolant - 0.04f, 0f, 1f);
                npc.velocity *= 0.95f;
            }
            else
            {
                eyeGleamInterpolant = 0f;
                SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_RandomBloodBurstSpread(NPC npc, Player target, bool phase2, bool phase3, ref float attackTimer)
        {
            int shootCount = 4;
            float attackDelay = 90f;
            float shootTime = 90f;

            if (phase2)
                shootCount++;

            float wrappedAttackTimer = (attackTimer - attackDelay) % (shootTime / shootCount);

            // Look at the target.
            npc.direction = (npc.Center.X < target.Center.X).ToDirectionInt();
            float idealRotation = npc.AngleTo(target.Center) - MathHelper.Pi * npc.spriteDirection * 0.15f;
            if (npc.spriteDirection == -1)
                idealRotation += MathHelper.Pi;

            if (npc.spriteDirection != npc.direction)
            {
                npc.spriteDirection = npc.direction;
                npc.rotation = -npc.rotation;
                idealRotation = -idealRotation;
            }
            npc.rotation = npc.rotation.AngleLerp(idealRotation, 0.2f);

            if (attackTimer < attackDelay)
            {
                npc.velocity *= 0.95f;
                npc.BloodNautilus_GetMouthPositionAndRotation(out Vector2 mouthPosition, out Vector2 mouthDirection);
                if (!Main.rand.NextBool(4))
                {
                    Dust blood = Dust.NewDustDirect(mouthPosition + mouthDirection * 60f - new Vector2(60f), 120, 120, 16, 0f, 0f, 150, Color.Transparent, 0.6f);
                    blood.fadeIn = 1f;
                    blood.velocity = blood.position.DirectionTo(mouthPosition + Main.rand.NextVector2Circular(15f, 15f)) * (blood.velocity.Length() + 3f);
                    blood.noGravity = true;
                    blood = Dust.NewDustDirect(mouthPosition + mouthDirection * 100f - new Vector2(80f), 160, 160, 16, 0f, 0f, 100, Color.Transparent, 0.9f);
                    blood.fadeIn = 1.5f;
                    blood.velocity = blood.position.DirectionTo(mouthPosition + Main.rand.NextVector2Circular(15f, 15f)) * (blood.velocity.Length() + 5f);
                    blood.noGravity = true;
                }
            }
            else if (attackTimer < attackDelay + shootTime)
            {
                npc.velocity *= 0.9f;
                npc.BloodNautilus_GetMouthPositionAndRotation(out Vector2 mouthPosition, out Vector2 mouthDirection);
                if (wrappedAttackTimer < shootTime / shootCount * 0.8f)
                {
                    for (int k = 0; k < 5; k++)
                    {
                        Dust blood = Dust.NewDustDirect(mouthPosition + mouthDirection * 50f - new Vector2(15f), 30, 30, 5, 0f, 0f, 0, Color.Transparent, 1.5f);
                        blood.velocity = blood.position.DirectionFrom(mouthPosition + Main.rand.NextVector2Circular(5f, 5f)) * blood.velocity.Length();
                        blood.position -= mouthDirection * 60f;
                        blood = Dust.NewDustDirect(mouthPosition + mouthDirection * 90f - new Vector2(20f), 40, 40, 5, 0f, 0f, 100, Color.Transparent, 1.5f);
                        blood.velocity = blood.position.DirectionFrom(mouthPosition + Main.rand.NextVector2Circular(10f, 10f)) * (blood.velocity.Length() + 5f);
                        blood.position -= mouthDirection * 100f;
                    }
                }

                // Create blood projectiles.
                if ((int)wrappedAttackTimer == 0)
                {
                    npc.velocity -= mouthDirection * 8f;
                    for (int l = 0; l < 20; l++)
                    {
                        Dust blood = Dust.NewDustDirect(mouthPosition + mouthDirection * 60f - new Vector2(15f), 30, 30, 5, 0f, 0f, 0, Color.Transparent, 1.5f);
                        blood.velocity = blood.position.DirectionFrom(mouthPosition + Main.rand.NextVector2Circular(5f, 5f)) * blood.velocity.Length();
                        blood.position -= mouthDirection * 60f;
                        blood = Dust.NewDustDirect(mouthPosition + mouthDirection * 100f - new Vector2(20f), 40, 40, 5, 0f, 0f, 100, Color.Transparent, 1.5f);
                        blood.velocity = blood.position.DirectionFrom(mouthPosition + Main.rand.NextVector2Circular(10f, 10f)) * (blood.velocity.Length() + 5f);
                        blood.position -= mouthDirection * 100f;
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int bloodCount = Main.rand.Next(6, 13);
                        for (int i = 0; i < bloodCount; i++)
                        {
                            Vector2 bloodVelocity = mouthDirection * 10f + Main.rand.NextVector2Square(-4f, 4f);
                            Utilities.NewProjectileBetter(mouthPosition - mouthDirection * 5f, bloodVelocity, ModContent.ProjectileType<BloodShot2>(), 120, 0f, Main.myPlayer);
                        }
                    }
                }
            }

            if (attackTimer >= attackDelay + shootTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_EquallySpreadBloodBolts(NPC npc, Player target, bool phase2, bool phase3, ref float attackTimer)
        {
            int shootDelay = 75;
            int slowdownTime = 30;
            int shootTime = 120;
            int totalBoltsToShoot = 15;
            float shootSpeed = 2.7f;

            if (phase2)
            {
                shootDelay -= 15;
                shootSpeed *= 1.15f;
            }
            if (phase3)
            {
                totalBoltsToShoot += 3;
                shootSpeed *= 1.25f;
            }

            int shootRate = shootTime / totalBoltsToShoot;
            ref float shootDirection = ref npc.Infernum().ExtraAI[0];

            // Look at the target.
            npc.direction = (npc.Center.X < target.Center.X).ToDirectionInt();
            float idealRotation = npc.AngleTo(target.Center) - MathHelper.Pi * npc.spriteDirection * 0.15f;
            if (npc.spriteDirection == -1)
                idealRotation += MathHelper.Pi;

            if (npc.spriteDirection != npc.direction)
            {
                npc.spriteDirection = npc.direction;
                npc.rotation = -npc.rotation;
                idealRotation = -idealRotation;
            }
            npc.rotation = npc.rotation.AngleLerp(idealRotation, 0.12f);

            // Hover into position prior to firing.
            if (attackTimer < shootDelay)
            {
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 530f, -50f);
                if (!npc.WithinRange(hoverDestination, 50f))
                {
                    Vector2 desiredVelocity = npc.SafeDirectionTo(hoverDestination) * 15f;
                    npc.SimpleFlyMovement(desiredVelocity, 0.4f);
                }
            }

            // Slow down for a moment.
            else if (attackTimer < shootDelay + slowdownTime)
                npc.velocity = (npc.velocity * 0.97f).MoveTowards(Vector2.Zero, 0.15f);

            // Release accelerating blood bolts in an even spread.
            else if ((attackTimer - shootDelay - slowdownTime) % shootRate == shootRate - 1f)
            {
                SoundEngine.PlaySound(SoundID.Item171, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.BloodNautilus_GetMouthPositionAndRotation(out Vector2 mouthPosition, out Vector2 mouthDirection);

                    if (shootDirection == 0f || shootDirection.ToRotationVector2().AngleBetween(npc.SafeDirectionTo(target.Center)) > 1.03f)
                        shootDirection = mouthDirection.ToRotation();

                    float offsetAngle = Utils.Remap(attackTimer - shootDelay - slowdownTime, 0f, shootTime, -0.91f, 0.91f);
                    Vector2 shootVelocity = (shootDirection + offsetAngle).ToRotationVector2() * shootSpeed;
                    Utilities.NewProjectileBetter(mouthPosition, shootVelocity, ModContent.ProjectileType<BloodBolt>(), 120, 0f);

                    npc.netUpdate = true;
                }
            }

            else if (attackTimer >= shootDelay + slowdownTime + shootTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_HorizontalCharge(NPC npc, Player target, bool phase3, ref float attackTimer)
        {
            int chargeCount = 3;
            int redirectTime = 40;
            int chargeTime = 40;
            int attackTransitionDelay = 8;
            int backSpikeCount = 6;
            float chargeSpeed = 38f;
            float hoverSpeed = 25f;

            if (phase3)
            {
                chargeTime -= 6;
                backSpikeCount += 2;
            }

            ref float chargeDirection = ref npc.Infernum().ExtraAI[0];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[1];
            npc.BloodNautilus_GetMouthPositionAndRotation(out Vector2 mouthPosition, out Vector2 mouthDirection);

            if (chargeCounter == 0f)
                redirectTime += 45;

            // Initialize the charge direction.
            if (attackTimer == 1f)
            {
                chargeDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                npc.netUpdate = true;
            }

            // Hover into position before charging.
            if (attackTimer <= redirectTime)
            {
                // Make a sound prior to charging.
                if (attackTimer == redirectTime / 2)
                    SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown, npc.Center);

                Vector2 hoverDestination = target.Center + Vector2.UnitX * chargeDirection * -420f;
                npc.Center = npc.Center.MoveTowards(hoverDestination, 2f);
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, hoverSpeed * 0.05f);

                // Slow down drastically prior to charge and release an arc of homing spikes away from the target.
                if (attackTimer == redirectTime)
                {
                    SoundEngine.PlaySound(SoundID.Item17, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < backSpikeCount; i++)
                        {
                            Vector2 backSpikeShootVelocity = -mouthDirection.RotatedBy(MathHelper.Lerp(-1.5f, 1.5f, i / (float)(backSpikeCount - 1f))) * 7f;
                            Utilities.NewProjectileBetter(npc.Center + backSpikeShootVelocity * 8f, backSpikeShootVelocity, ModContent.ProjectileType<GoreSpike>(), 125, 0f);
                        }
                    }

                    npc.velocity *= 0.3f;
                    npc.netUpdate = true;
                }

                // Determine the current rotation and sprite direction.
                npc.direction = (npc.Center.X < target.Center.X).ToDirectionInt();
                float idealRotation = npc.AngleTo(target.Center) - MathHelper.Pi * npc.spriteDirection * 0.15f;
                if (npc.spriteDirection == -1)
                    idealRotation += MathHelper.Pi;

                if (npc.spriteDirection != npc.direction)
                {
                    npc.spriteDirection = npc.direction;
                    npc.rotation = -npc.rotation;
                    idealRotation = -idealRotation;
                }
                npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.12f);
            }
            else if (attackTimer <= redirectTime + chargeTime)
            {
                npc.ai[3] = 0f;
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitX * chargeDirection * chargeSpeed, 0.15f);
                npc.rotation += npc.spriteDirection * 0.2f;
                if (attackTimer == redirectTime + chargeTime)
                    npc.velocity *= 0.7f;

                // Emit blood from the mouth as a means of creating a spiral.
                Dust blood = Dust.NewDustPerfect(mouthPosition, 267);
                blood.velocity = mouthDirection * 8f + Main.rand.NextVector2Circular(1.2f, 1.2f);
                blood.noGravity = true;
                blood.scale = 1.5f;
                blood.color = Color.Red;

                // Do damage and become temporarily invulnerable. This is done to prevent dash-cheese.
                npc.damage = npc.defDamage + 40;
                npc.dontTakeDamage = true;
            }
            else
                npc.velocity *= 0.92f;

            if (attackTimer >= redirectTime + chargeTime + attackTransitionDelay)
            {
                attackTimer = 0f;
                chargeCounter++;
                if (chargeCounter >= chargeCount)
                    SelectNextAttack(npc);
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_SanguineBatSwarm(NPC npc, Player target, bool phase3, ref float attackTimer, ref float eyeGleamInterpolant)
        {
            int slowdownTime = 30;
            int summonTime = 90;
            int totalBatsToSummon = 15;
            int batAttackTime = SanguineBat.Lifetime;
            int attackTransitionDelay = 90;
            int bloodBurstReleaseRate = 75;
            int bloodPerBurst = 4;

            if (phase3)
            {
                totalBatsToSummon += 3;
                bloodPerBurst += 3;
            }

            int batSummonRate = summonTime / totalBatsToSummon;
            npc.BloodNautilus_GetMouthPositionAndRotation(out Vector2 mouthPosition, out _);

            // Look at the target.
            npc.direction = (npc.Center.X < target.Center.X).ToDirectionInt();
            float idealRotation = npc.AngleTo(target.Center) - MathHelper.Pi * npc.spriteDirection * 0.15f;
            if (npc.spriteDirection == -1)
                idealRotation += MathHelper.Pi;

            if (npc.spriteDirection != npc.direction)
            {
                npc.spriteDirection = npc.direction;
                npc.rotation = -npc.rotation;
                idealRotation = -idealRotation;
            }
            npc.rotation = npc.rotation.AngleLerp(idealRotation, 0.12f);

            // Slow down at first.
            if (attackTimer < slowdownTime)
                npc.velocity = (npc.velocity * 0.97f).MoveTowards(Vector2.Zero, 0.25f);

            // Summon bats in the sky.
            else if (attackTimer < slowdownTime + summonTime)
            {
                npc.velocity = Vector2.Zero;

                eyeGleamInterpolant = CalamityUtils.Convert01To010(Utils.GetLerpValue(0f, summonTime - 5f, attackTimer - slowdownTime, true));
                if (attackTimer % batSummonRate == batSummonRate - 1f)
                {
                    SoundEngine.PlaySound(SoundID.Item122, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int batLifetime = batAttackTime + (slowdownTime + summonTime - (int)attackTimer);
                        Vector2 batSpawnPosition = target.Center + new Vector2(Main.rand.NextFloatDirection() * 600f, -1000f);
                        int bat = Utilities.NewProjectileBetter(batSpawnPosition, Vector2.UnitY * -6f, ModContent.ProjectileType<SanguineBat>(), 130, 0f);
                        if (Main.projectile.IndexInRange(bat))
                        {
                            Main.projectile[bat].ai[1] = batLifetime;
                            Main.projectile[bat].netUpdate = true;
                        }
                    }
                }
            }

            // Hover near the target after summoning the bats.
            else if (attackTimer < slowdownTime + summonTime + batAttackTime)
            {
                float speedFactor = Utils.GetLerpValue(bloodBurstReleaseRate * 0.65f, bloodBurstReleaseRate * 0.9f, attackTimer % bloodBurstReleaseRate, true);
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 320f, -100f) - npc.velocity;
                if (!npc.WithinRange(hoverDestination, 50f))
                {
                    Vector2 desiredVelocity = npc.SafeDirectionTo(hoverDestination) * speedFactor * 11f;
                    npc.SimpleFlyMovement(desiredVelocity, 0.225f);
                }

                // Release bursts of blood periodically.
                if (attackTimer % bloodBurstReleaseRate == bloodBurstReleaseRate - 1f)
                {
                    SoundEngine.PlaySound(SoundID.Item171, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float offsetAngle = Main.rand.NextBool().ToInt() * MathHelper.Pi / bloodPerBurst;
                        for (int i = 0; i < bloodPerBurst; i++)
                        {
                            Vector2 bloodShootVelocity = (MathHelper.TwoPi * i / bloodPerBurst + offsetAngle).ToRotationVector2() * 8f;
                            Utilities.NewProjectileBetter(mouthPosition, bloodShootVelocity, ModContent.ProjectileType<BloodShot2>(), 125, 0f);
                        }

                        npc.netUpdate = true;
                    }
                }
            }

            if (attackTimer >= slowdownTime + summonTime + batAttackTime + attackTransitionDelay)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_SquidGames(NPC npc, Player target, bool phase3, ref float attackTimer)
        {
            int summonDelay = 90;
            int squidSummonCount = 1;

            if (phase3)
                squidSummonCount++;

            // Look at the target.
            npc.direction = (npc.Center.X < target.Center.X).ToDirectionInt();
            float idealRotation = npc.AngleTo(target.Center) - MathHelper.Pi * npc.spriteDirection * 0.15f;
            if (npc.spriteDirection == -1)
                idealRotation += MathHelper.Pi;

            if (npc.spriteDirection != npc.direction)
            {
                npc.spriteDirection = npc.direction;
                npc.rotation = -npc.rotation;
                idealRotation = -idealRotation;
            }
            npc.rotation = npc.rotation.AngleLerp(idealRotation, 0.12f);

            if (attackTimer < summonDelay)
                npc.velocity *= 0.95f;

            // Have a squid burst out of the Dreadnautilus, leaving blood behind and damaging it.
            if (attackTimer == summonDelay)
            {
                npc.BloodNautilus_GetMouthPositionAndRotation(out _, out Vector2 mouthDirection);

                int damage = (int)(npc.lifeMax * 0.01f);
                for (int i = 0; i < squidSummonCount; i++)
                {
                    if (NPC.CountNPCS(NPCID.BloodSquid) >= squidSummonCount + 1)
                        break;

                    Vector2 splatterDirection = -mouthDirection.RotatedByRandom(0.78f);
                    Vector2 bloodSpawnPosition = npc.Center + splatterDirection * 60f;
                    SoundEngine.PlaySound(SoundID.NPCHit18, npc.Center);
                    for (int j = 0; j < 21; j++)
                    {
                        int bloodLifetime = Main.rand.Next(22, 36);
                        float bloodScale = Main.rand.NextFloat(0.6f, 0.8f);
                        Color bloodColor = Color.Lerp(Color.Red, Color.DarkRed, Main.rand.NextFloat());
                        bloodColor = Color.Lerp(bloodColor, new Color(51, 22, 94), Main.rand.NextFloat(0.65f));

                        if (Main.rand.NextBool(20))
                            bloodScale *= 2f;

                        Vector2 bloodVelocity = splatterDirection.RotatedByRandom(0.81f) * Main.rand.NextFloat(11f, 23f);
                        bloodVelocity.Y -= 12f;
                        BloodParticle blood = new(bloodSpawnPosition, bloodVelocity, bloodLifetime, bloodScale, bloodColor);
                        GeneralParticleHandler.SpawnParticle(blood);
                    }
                    for (int j = 0; j < 10; j++)
                    {
                        float bloodScale = Main.rand.NextFloat(0.2f, 0.33f);
                        Color bloodColor = Color.Lerp(Color.Red, Color.DarkRed, Main.rand.NextFloat(0.5f, 1f));
                        Vector2 bloodVelocity = splatterDirection.RotatedByRandom(0.9f) * Main.rand.NextFloat(9f, 14.5f);
                        BloodParticle2 blood = new(bloodSpawnPosition, bloodVelocity, 20, bloodScale, bloodColor);
                        GeneralParticleHandler.SpawnParticle(blood);
                    }

                    npc.StrikeNPC(damage, 0f, 0);

                    if (Main.netMode != NetmodeID.MultiplayerClient && npc.life > 0)
                    {
                        NPC.NewNPC(npc.GetSource_FromAI(), (int)bloodSpawnPosition.X, (int)bloodSpawnPosition.Y, NPCID.BloodSquid, npc.whoAmI);
                        npc.velocity = -splatterDirection * 7f;
                        npc.netUpdate = true;
                    }
                }
            }

            if (attackTimer >= summonDelay + 45f)
                SelectNextAttack(npc);
        }

        public static void SelectNextAttack(NPC npc)
        {
            bool phase2 = npc.life < npc.lifeMax * Phase2LifeRatio;
            DreadnautilusAttackState oldAttack = (DreadnautilusAttackState)npc.ai[0];
            switch (oldAttack)
            {
                case DreadnautilusAttackState.InitialSummonDelay:
                    npc.ai[0] = (int)DreadnautilusAttackState.BloodSpitToothBalls;
                    break;
                case DreadnautilusAttackState.BloodSpitToothBalls:
                    npc.ai[0] = (int)DreadnautilusAttackState.EyeGleamEyeFishSummon;
                    break;
                case DreadnautilusAttackState.EyeGleamEyeFishSummon:
                    npc.ai[0] = (int)DreadnautilusAttackState.RandomBloodBurstSpread;
                    break;
                case DreadnautilusAttackState.RandomBloodBurstSpread:
                    npc.ai[0] = (int)DreadnautilusAttackState.EquallySpreadBloodBolts;
                    break;
                case DreadnautilusAttackState.EquallySpreadBloodBolts:
                    npc.ai[0] = phase2 ? (int)DreadnautilusAttackState.HorizontalCharge : (int)DreadnautilusAttackState.BloodSpitToothBalls;
                    break;
                case DreadnautilusAttackState.HorizontalCharge:
                    npc.ai[0] = (int)DreadnautilusAttackState.SquidGames;
                    break;
                case DreadnautilusAttackState.SquidGames:
                    npc.ai[0] = (int)DreadnautilusAttackState.SanguineBatSwarm;
                    break;
                case DreadnautilusAttackState.SanguineBatSwarm:
                    npc.ai[0] = (int)DreadnautilusAttackState.BloodSpitToothBalls;
                    break;
            }

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            float eyeGleamInterpolant = npc.ai[2];
            float backglowFade = Utils.Remap(eyeGleamInterpolant - 1f, 0f, 0.6f, 0f, 1f);
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            SpriteEffects direction = npc.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            // Create a backglow as necessary.
            if (backglowFade > 0f)
            {
                float drawOffsetFactor = backglowFade * 7.5f + (float)Math.Cos(Main.GlobalTimeWrappedHourly * 5f) * 3f;
                if (drawOffsetFactor < 0f)
                    drawOffsetFactor = 0f;
                Color backglowColor = Color.Red * (float)Math.Pow(backglowFade, 0.55f);

                for (int i = 0; i < 6; i++)
                {
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * drawOffsetFactor;
                    spriteBatch.Draw(texture, drawPosition + drawOffset, npc.frame, npc.GetAlpha(backglowColor), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);

                    if (npc.ai[3] != 0f)
                        spriteBatch.Draw(TextureAssets.Extra[129].Value, drawPosition + drawOffset, npc.frame, npc.GetAlpha(backglowColor), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);
                }
            }

            spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);
            if (npc.ai[3] != 0f)
                spriteBatch.Draw(TextureAssets.Extra[129].Value, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);

            // Render a gleam above the eye as necessary.
            if (eyeGleamInterpolant > 0f)
            {
                spriteBatch.SetBlendState(BlendState.Additive);

                Texture2D gleamTexture = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/EmpressStar").Value;
                Color gleamColor = new Color(0.93f, 0.03f, 0.11f) * eyeGleamInterpolant * npc.Opacity;
                float eyeOffsetRotation = npc.rotation + MathHelper.Pi * npc.spriteDirection * 0.15f;
                if (npc.spriteDirection == -1)
                    eyeOffsetRotation += MathHelper.Pi;
                Vector2 eyePosition = drawPosition + eyeOffsetRotation.ToRotationVector2() * new Vector2(14f, 18f);

                // Calculate the rotation and scale of each piece of the gleam.
                float[] eyeRotations = new[] 
                {
                    -Main.GlobalTimeWrappedHourly * 3.74f,
                    Main.GlobalTimeWrappedHourly * 3.74f,
                    MathHelper.PiOver2
                };
                float[] eyeScales = new[]
                {
                    npc.Opacity * eyeGleamInterpolant * 1.65f,
                    npc.Opacity * eyeGleamInterpolant* 1.65f,
                    npc.Opacity * eyeGleamInterpolant * Utils.Remap((float)Math.Cos(Main.GlobalTimeWrappedHourly * 9.3f), -1f, 1f, 2f, 2.7f),
                };

                for (int i = 0; i < eyeRotations.Length; i++)
                    spriteBatch.Draw(gleamTexture, eyePosition, null, gleamColor, eyeRotations[i], gleamTexture.Size() * 0.5f, eyeScales[i] * 0.4f, 0, 0f);

                spriteBatch.ResetBlendState();
            }

            return false;
        }
    }
}
