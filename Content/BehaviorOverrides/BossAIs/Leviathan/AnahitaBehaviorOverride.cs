using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Leviathan;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Sounds;
using InfernumMode.Content.Projectiles.Pets;
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

using LeviathanNPC = CalamityMod.NPCs.Leviathan.Leviathan;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Leviathan
{
    public class AnahitaBehaviorOverride : NPCBehaviorOverride
    {
        public enum AnahitaAttackType
        {
            // Alone attacks.
            FloatTowardsPlayer,
            CreateWaterIllusions,
            PlaySinusoidalSong,
            IceMistBarrages,
            ChargeAndCreateWaterCircle,

            // Alone and enraged attacks.
            AtlantisCharge
        }

        public const float OceanDistanceLeniancy = 9000f;

        public override int NPCOverrideType => ModContent.NPCType<Anahita>();

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            ComboAttackManager.LeviathanSummonLifeRatio
        };

        public override bool PreAI(NPC npc)
        {
            // Stay within the world you stupid fucking fish I swear to god.
            npc.position.X = MathHelper.Clamp(npc.position.X, 360f, Main.maxTilesX * 16f - 360f);

            // Define afterimage variables.
            NPCID.Sets.TrailingMode[npc.type] = 1;
            NPCID.Sets.TrailCacheLength[npc.type] = 5;

            // Select a target and reset damage and invulnerability.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];
            npc.direction = (target.Center.X > npc.Center.X).ToDirectionInt();
            npc.damage = npc.defDamage;
            npc.dontTakeDamage = false;

            // Send natural despawns into the sun.
            npc.timeLeft = 3600;

            // Despawn.
            if (target.dead || !target.active)
            {
                npc.TargetClosest();
                if (target.dead || !target.active)
                {
                    npc.active = false;
                    return false;
                }
            }

            // Set the whoAmI variable.
            CalamityGlobalNPC.siren = npc.whoAmI;

            // Inherit attributes from the leader.
            ComboAttackManager.InheritAttributesFromLeader(npc);

            float lifeRatio = npc.life / (float)npc.lifeMax;
            Vector2 headPosition = npc.Center + new Vector2(npc.spriteDirection * 16f, -42f);
            ref float attackTimer = ref npc.ai[1];
            ref float hasSummonedLeviathan = ref npc.ai[3];
            ref float frameState = ref npc.localAI[0];
            ref float horizontalAfterimageInterpolant = ref npc.localAI[1];

            // Reset things.
            bool shouldGoAway = ComboAttackManager.FightState == LeviAnahitaFightState.LeviathanAlone;
            bool enraged = ComboAttackManager.FightState == LeviAnahitaFightState.AloneEnraged;
            if (enraged)
                HatGirl.SayThingWhileOwnerIsAlive(target, "Home stretch! Do the same as you did before!");

            frameState = 0f;
            horizontalAfterimageInterpolant = 0f;
            npc.damage = 0;
            npc.dontTakeDamage = shouldGoAway;

            // Don't take damage if the target leaves the ocean.
            bool outOfOcean = target.position.X > OceanDistanceLeniancy && target.position.X < Main.maxTilesX * 16 - OceanDistanceLeniancy && !BossRushEvent.BossRushActive;
            if (outOfOcean)
            {
                npc.dontTakeDamage = true;
                npc.Calamity().CurrentlyEnraged = true;
            }

            // Summon the Leviathan once ready.
            bool canSummonLeviathan = !NPC.AnyNPCs(ModContent.NPCType<LeviathanNPC>()) && !Utilities.AnyProjectiles(ModContent.ProjectileType<LeviathanSpawner>());
            if (lifeRatio < ComboAttackManager.LeviathanSummonLifeRatio && canSummonLeviathan && hasSummonedLeviathan == 0f)
            {
                npc.ai[0] = 0f;
                attackTimer = 0f;
                DoBehavior_SummonLeviathan(npc, ref hasSummonedLeviathan);
                return false;
            }

            // Fade in and out as necessary.
            npc.alpha = Utils.Clamp(npc.alpha + shouldGoAway.ToDirectionInt() * 14, 0, 255);

            if (shouldGoAway)
            {
                npc.ai[0] = 0f;
                npc.Center = Vector2.Lerp(npc.Center, target.Center - Vector2.UnitY * 800f, 0.1f);
                npc.velocity = Vector2.Zero;
                npc.Calamity().ShouldCloseHPBar = true;
                attackTimer = 0f;
                return false;
            }

            switch ((int)npc.ai[0])
            {
                case (int)AnahitaAttackType.FloatTowardsPlayer:
                    DoBehavior_FloatTowardsPlayer(npc, target, ref attackTimer);
                    break;
                case (int)AnahitaAttackType.CreateWaterIllusions:
                    DoBehavior_CreateWaterIllusions(npc, target, enraged, ref attackTimer, ref horizontalAfterimageInterpolant);
                    break;
                case (int)AnahitaAttackType.PlaySinusoidalSong:
                    DoBehavior_PlaySinusoidalSong(npc, target, enraged, headPosition, ref attackTimer);
                    break;
                case (int)AnahitaAttackType.IceMistBarrages:
                    DoBehavior_IceMistBarrages(npc, target, enraged, headPosition, ref attackTimer);
                    break;
                case (int)AnahitaAttackType.ChargeAndCreateWaterCircle:
                    DoBehavior_ChargeAndCreateWaterCircle(npc, target, enraged, ref attackTimer);
                    break;
                case (int)AnahitaAttackType.AtlantisCharge:
                    DoBehavior_AtlantisCharge(npc, target, ref attackTimer);
                    break;
            }
            ComboAttackManager.DoComboAttacks(npc, target, ref attackTimer);

            attackTimer++;
            return false;
        }

        public static void DoBehavior_SummonLeviathan(NPC npc, ref float hasSummonedLeviathan)
        {
            if (hasSummonedLeviathan == 1f)
                return;

            // Force Anahita to use charging frames.
            npc.localAI[0] = 1f;

            npc.rotation = npc.velocity.X * 0.014f;

            // Descend back into the ocean.
            npc.direction = (npc.Center.X < Main.maxTilesX * 8f).ToDirectionInt();
            if (npc.alpha <= 0)
            {
                float moveDirection = 1f;
                if (Math.Abs(npc.Center.X - Main.maxTilesX * 16f) > Math.Abs(npc.Center.X))
                    moveDirection = -1f;
                npc.velocity.X = moveDirection * 6f;
                npc.spriteDirection = (int)-moveDirection;
            }
            npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + 0.2f, 6f, 16f);

            float idealRotation = npc.velocity.ToRotation();
            if (npc.spriteDirection == 1)
                idealRotation += MathHelper.Pi;

            npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.08f);

            if (Collision.WetCollision(npc.position, npc.width, npc.height) || npc.position.Y > Main.worldSurface * 16D || BossRushEvent.BossRushActive)
            {
                int oldAlpha = npc.alpha;
                npc.alpha = Utils.Clamp(npc.alpha + 9, 0, 255);
                if (Main.netMode != NetmodeID.MultiplayerClient && npc.alpha >= 255 && oldAlpha < 255)
                {
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<LeviathanSpawner>(), 0, 0f);
                    hasSummonedLeviathan = 1f;
                }

                // Set the whoAmI variable.
                CalamityGlobalNPC.siren = npc.whoAmI;

                npc.velocity *= 0.9f;
            }
            npc.dontTakeDamage = true;
        }

        public static void DoBehavior_FloatTowardsPlayer(NPC npc, Player target, ref float attackTimer)
        {
            int hoverTime = 15;

            npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
            npc.rotation = npc.velocity.X * 0.02f;

            DoDefaultMovement(npc, target.Center - Vector2.UnitY * 400f, Vector2.One * 7f, 0.14f);
            if (attackTimer >= hoverTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_CreateWaterIllusions(NPC npc, Player target, bool enraged, ref float attackTimer, ref float horizontalAfterimageInterpolant)
        {
            int fadeOutTime = 40;
            int shootRate = 33;
            int shootTime = AnahitaWaterIllusion.Lifetime;
            int illusionCount = 6;
            float waterBoltShootSpeed = 15.6f;
            if (enraged)
            {
                shootRate -= 6;
                illusionCount += 2;
                waterBoltShootSpeed -= 1.1f;
            }

            float slowdownInterpolant = Utils.GetLerpValue(-42f, 0f, attackTimer - fadeOutTime - shootTime, true);
            ref float hoverDirection = ref npc.Infernum().ExtraAI[0];

            // Define rotation.
            npc.rotation = npc.velocity.X * 0.02f;

            // Play a wacky sound after descending.
            if (attackTimer == 1f)
                SoundEngine.PlaySound(SoundID.Item165, target.Center);

            // Fade out before teleporting above the target and creating water illusions.
            if (attackTimer <= fadeOutTime)
            {
                horizontalAfterimageInterpolant = Utils.GetLerpValue(0f, fadeOutTime * 0.8f, attackTimer, true);
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * 16f, 0.1f);
                npc.dontTakeDamage = horizontalAfterimageInterpolant > 0.4f;

                // Teleport and create the illusions.
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == fadeOutTime)
                {
                    npc.Center = target.Center + new Vector2(Main.rand.NextFloatDirection() * 700f, -750f);
                    npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                    npc.velocity = Vector2.Zero;
                    npc.netUpdate = true;
                    for (int i = 0; i < illusionCount; i++)
                    {
                        float offsetAngle = MathHelper.TwoPi * i / illusionCount;
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<AnahitaWaterIllusion>(), 0, 0f, -1, 0f, offsetAngle);
                    }
                }
                return;
            }

            // Hover to the top left/right of the target.
            if (hoverDirection == 0f)
            {
                hoverDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                npc.netUpdate = true;
            }

            if (attackTimer == fadeOutTime + shootTime / 2)
            {
                hoverDirection *= -1f;
                npc.netUpdate = true;
            }

            npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
            Vector2 hoverDestination = target.Center + new Vector2(hoverDirection * 540f, -220f);
            npc.Center = npc.Center.MoveTowards(hoverDestination, 10f);
            DoDefaultMovement(npc, hoverDestination, Vector2.One * slowdownInterpolant * 16f, 0.2f);

            // Shoot bolts of water at the target.
            float adjustedAttackTimer = attackTimer - fadeOutTime;
            if (adjustedAttackTimer >= 90f && adjustedAttackTimer % shootRate == shootRate - 1f && !npc.WithinRange(target.Center, 250f))
            {
                SoundEngine.PlaySound(SoundID.Item28, npc.Center);
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    return;

                List<Entity> entitiesToShoot = new() { npc };
                entitiesToShoot.AddRange(Utilities.AllProjectilesByID(ModContent.ProjectileType<AnahitaWaterIllusion>()));

                // Yes, npc.spriteDirection is used here. Entities do not have a sprite direction defined, but all illusions
                // inherit their sprite direction from Anahita herself, so it is safe to do this.
                foreach (Entity entity in entitiesToShoot)
                {
                    int waterBoltCount = entity == npc ? 3 : 1;
                    Vector2 shootPosition = entity.Center + new Vector2(npc.spriteDirection * 16f, -42f);
                    for (int i = 0; i < waterBoltCount; i++)
                    {
                        Vector2 waterBoltShootVelocity = (target.Center - shootPosition).SafeNormalize(Vector2.UnitX * npc.spriteDirection) * waterBoltShootSpeed;
                        if (waterBoltCount > 1)
                        {
                            float shootOffsetAngle = MathHelper.Lerp(-0.4f, 0.4f, i / (float)(waterBoltCount - 1f));
                            waterBoltShootVelocity = waterBoltShootVelocity.RotatedBy(shootOffsetAngle);
                        }

                        Utilities.NewProjectileBetter(shootPosition, waterBoltShootVelocity, ModContent.ProjectileType<WaterBolt>(), 175, 0f);
                    }
                }
            }

            if (attackTimer >= fadeOutTime + shootTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_PlaySinusoidalSong(NPC npc, Player target, bool enraged, Vector2 headPosition, ref float attackTimer)
        {
            int shootDelay = 72;
            int shootTime = 250;
            int shootRate = 10;
            float bobAmplitude = 325f;
            float bobPeriod = 30f;
            float songShootSpeed = 13f;
            if (enraged)
            {
                shootRate -= 2;
                bobAmplitude -= 32f;
                bobPeriod -= 9f;
                songShootSpeed += 2f;
            }

            bool ableToShoot = attackTimer >= shootDelay && attackTimer < shootDelay + shootTime;

            // Hover to the side of the target and bob up and down.
            Vector2 hoverDestination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * 600f;
            if (attackTimer >= shootDelay)
                hoverDestination.Y += (float)Math.Sin((attackTimer - shootDelay) * MathHelper.TwoPi / bobPeriod) * bobAmplitude;
            npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
            DoDefaultMovement(npc, hoverDestination, new Vector2(19.6f, 12.5f), 0.8f);

            // Shoot clefs of sound.
            if (ableToShoot && attackTimer % shootRate == shootRate - 1f && !npc.WithinRange(target.Center, 270f))
            {
                Main.musicPitch = Main.rand.NextFloatDirection() * 0.25f;
                SoundEngine.PlaySound(SoundID.Item26, target.position);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 songShootVelocity = Vector2.UnitX * npc.spriteDirection * songShootSpeed;
                    Utilities.NewProjectileBetter(headPosition, songShootVelocity, ModContent.ProjectileType<HeavenlyLullaby>(), 175, 0f);
                }
            }

            if (attackTimer >= shootDelay + shootTime + 75f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_IceMistBarrages(NPC npc, Player target, bool enraged, Vector2 headPosition, ref float attackTimer)
        {
            int intialTeleportDelay = 90;
            int teleportChargeTime = 60;
            int teleportCount = 5;
            int mistReleaseRate = 10;
            float mistMaxSpeed = 8.5f;
            float horizontalTeleportOffset = 360f;
            float verticalTeleportOffset = 540f;
            if (enraged)
            {
                teleportCount--;
                teleportChargeTime -= 12;
                mistReleaseRate -= 3;
                mistMaxSpeed += 2.4f;
            }

            Vector2 initialTeleportOffset = target.Center - Vector2.UnitY * 350f;

            ref float teleportCounter = ref npc.Infernum().ExtraAI[0];
            ref float teleportTimer = ref npc.Infernum().ExtraAI[1];

            // Drift towards the top of the target.
            if (attackTimer < intialTeleportDelay)
            {
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(initialTeleportOffset) * 8f, 0.35f);
                npc.rotation = npc.velocity.X * 0.02f;
                npc.Opacity = Utils.GetLerpValue(intialTeleportDelay - 1f, intialTeleportDelay - 24f, attackTimer, true);
            }
            else
            {
                teleportTimer++;
                npc.Opacity = Utils.GetLerpValue(teleportChargeTime - 1f, teleportChargeTime - 24f, teleportTimer, true);

                // Determine direction.
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

                // Periodically release mist.
                if (attackTimer % mistReleaseRate == mistReleaseRate - 1f)
                {
                    SoundEngine.PlaySound(CommonCalamitySounds.LouderPhantomPhoenix, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 icicleShootVelocity = (target.Center - headPosition).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.6f, 1f) * mistMaxSpeed;
                        Utilities.NewProjectileBetter(headPosition + icicleShootVelocity * 4f, icicleShootVelocity, ModContent.ProjectileType<FrostMist>(), 175, 0f);
                    }
                }
            }

            Vector2 teleportOffset = new((teleportCounter % 2f == 0f).ToDirectionInt() * horizontalTeleportOffset, verticalTeleportOffset);
            if (Math.Sign(target.velocity.X) == (teleportCounter % 2f == 0f).ToDirectionInt())
                teleportOffset.X += target.velocity.X * 50f;

            // Reset opacity and teleport after the delay is finished.
            if (attackTimer == intialTeleportDelay || teleportTimer >= teleportChargeTime)
            {
                teleportTimer = 0f;
                teleportCounter++;

                npc.Opacity = 1f;
                npc.Center = target.Center + teleportOffset;
                npc.velocity = Vector2.UnitY * -verticalTeleportOffset * 2f / teleportChargeTime;
                npc.netUpdate = true;

                if (teleportCounter >= teleportCount)
                    SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_ChargeAndCreateWaterCircle(NPC npc, Player target, bool enraged, ref float attackTimer)
        {
            int hoverTime = 36;
            int waterSpearCount = 30;
            int ringCount = 2;
            float waterSpearShootSpeed = 11f;
            float chargeSpeed = 24.5f;
            if (enraged)
            {
                waterSpearCount += 6;
                ringCount++;
                waterSpearShootSpeed += 1.5f;
                chargeSpeed += 4f;
            }

            // Use charging frames and do damage.
            npc.localAI[0] = 1f;
            npc.damage = npc.defDamage;

            // Hover before charging.
            if (attackTimer < hoverTime)
            {
                npc.spriteDirection = -1;
                float idealRotation = npc.AngleTo(target.Center);
                if (npc.Center.X > target.Center.X)
                {
                    npc.spriteDirection = 1;
                    idealRotation += MathHelper.Pi;
                }

                npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.12f);

                Vector2 destination = target.Center;
                destination.X -= Math.Sign(target.Center.X - npc.Center.X) * 400f;
                destination.Y -= 300f;
                destination -= npc.velocity;
                npc.Center = Vector2.Lerp(npc.Center, new Vector2(destination.X, npc.Center.Y), 0.01f);
                npc.Center = Vector2.Lerp(npc.Center, new Vector2(npc.Center.X, destination.Y), 0.03f);
                npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * 22f, 1.85f);
            }

            // Charge.
            if (attackTimer == hoverTime)
            {
                npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                npc.spriteDirection = -1;
                npc.rotation = npc.velocity.ToRotation();
                if (npc.Center.X > target.Center.X)
                {
                    npc.spriteDirection = 1;
                    npc.rotation += MathHelper.Pi;
                }
            }

            // Check to see if water or tiles have been hit. If they have, go to the next attack state and create a bunch of water spears.
            bool edgeOfWorld = npc.Center.X < 540f || npc.Center.Y >= Main.maxTilesX * 16f - 540f;
            bool createSpears = Collision.SolidCollision(npc.TopLeft, npc.width, npc.height) || Collision.WetCollision(npc.TopLeft, npc.width, npc.height) || attackTimer >= hoverTime + 180f || edgeOfWorld;
            if (attackTimer > hoverTime && createSpears)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < waterSpearCount; i++)
                    {
                        for (int j = 0; j < ringCount; j++)
                        {
                            Vector2 waterSpearVelocity = (MathHelper.TwoPi * i / waterSpearCount + j * 0.33f).ToRotationVector2() * waterSpearShootSpeed;
                            waterSpearVelocity *= MathHelper.Lerp(1f, 0.32f, j / (float)(ringCount - 1f));
                            Utilities.NewProjectileBetter(npc.Center, waterSpearVelocity, ModContent.ProjectileType<WaterBolt>(), 180, 0f);
                        }
                    }
                }
                SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_AtlantisCharge(NPC npc, Player target, ref float attackTimer)
        {
            // Use charging frames.
            npc.localAI[0] = 1f;

            int hoverTime = 35;
            int chargeTime = 36;
            float chargeSpeed = 29f;
            float totalCharges = 3f;
            ref float atlantisCooldown = ref npc.Infernum().ExtraAI[0];

            if (attackTimer == 5f)
                SoundEngine.PlaySound(SoundID.DD2_PhantomPhoenixShot, target.Center);
            int wrappedAttackTimer = (int)(attackTimer % (hoverTime + chargeTime));

            // Hover before charging.
            if (wrappedAttackTimer < hoverTime)
            {
                npc.spriteDirection = -1;
                float idealRotation = npc.AngleTo(target.Center);
                if (npc.Center.X > target.Center.X)
                {
                    npc.spriteDirection = 1;
                    idealRotation += MathHelper.Pi;
                }

                npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.12f);

                Vector2 destination = target.Center;
                destination.X -= Math.Sign(target.Center.X - npc.Center.X) * 400f;
                destination.Y -= 300f;
                destination -= npc.velocity;
                npc.Center = Vector2.Lerp(npc.Center, new Vector2(destination.X, npc.Center.Y), 0.01f);
                npc.Center = Vector2.Lerp(npc.Center, new Vector2(npc.Center.X, destination.Y), 0.03f);
                npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * 22f, 1.85f);
            }

            // Charge.
            if (wrappedAttackTimer == hoverTime)
            {
                npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                npc.spriteDirection = -1;
                npc.rotation = npc.velocity.ToRotation();
                if (npc.Center.X > target.Center.X)
                {
                    npc.spriteDirection = 1;
                    npc.rotation += MathHelper.Pi;
                }
            }

            // Use Atlantis after charging.
            if (wrappedAttackTimer > hoverTime)
            {
                // Do a bit more damage than usual when charging.
                npc.damage = (int)(npc.defDamage * 1.667);

                // Release idle dust.
                if (!Main.dedServ)
                {
                    int dustCount = 7;
                    for (int i = 0; i < dustCount; i++)
                    {
                        Vector2 dustSpawnOffset = (Vector2.Normalize(npc.velocity) * new Vector2((npc.width + 50) / 2f, npc.height) * 0.75f).RotatedBy((i - (dustCount / 2 - 1)) * MathHelper.Pi / dustCount);
                        Vector2 dustVelocity = Main.rand.NextFloat(-MathHelper.PiOver2, MathHelper.PiOver2).ToRotationVector2() * Main.rand.NextFloat(6f, 16f);
                        dustSpawnOffset += dustVelocity * 0.5f;

                        Dust water = Dust.NewDustDirect(npc.Center + dustSpawnOffset, 0, 0, DustID.DungeonWater, dustVelocity.X, dustVelocity.Y, 100, default, 1.4f);
                        water.velocity /= 4f;
                        water.velocity -= npc.velocity;
                        water.noGravity = true;
                        water.noLight = true;
                    }
                }

                Vector2 currentDirection = (npc.position - npc.oldPos[1]).SafeNormalize(Vector2.Zero);
                Vector2 spearDirection = currentDirection.RotatedBy(npc.direction * MathHelper.Pi * -0.08f);

                npc.velocity *= 1.003f;
                npc.rotation = currentDirection.ToRotation();
                if (npc.spriteDirection == 1)
                    npc.rotation += MathHelper.Pi;

                // Poke the target with Atlantis if close to them and pointing towards them.
                bool aimingAtPlayer = currentDirection.AngleBetween(npc.SafeDirectionTo(target.Center)) < MathHelper.ToRadians(54f);
                bool closeToPlayer = npc.WithinRange(target.Center, 180f);
                if (aimingAtPlayer && closeToPlayer && atlantisCooldown <= 0f)
                {
                    SoundEngine.PlaySound(SoundID.DD2_KoboldIgnite, target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (float offset = 0f; offset < 110f; offset += 10f)
                            Utilities.NewProjectileBetter(npc.Center + spearDirection * (15f + offset), spearDirection * (20f + offset * 0.4f), ModContent.ProjectileType<AtlantisSpear>(), 200, 0f);
                    }
                    atlantisCooldown = 30f;
                }

                if (atlantisCooldown > 0)
                    atlantisCooldown--;
            }

            if (attackTimer >= (hoverTime + chargeTime) * totalCharges)
            {
                npc.ai[0] = 0f;
                npc.rotation = 0f;
                SelectNextAttack(npc);
            }
        }

        public static void DoDefaultMovement(NPC npc, Vector2 destination, Vector2 maxVelocity, float acceleration)
        {
            if (BossRushEvent.BossRushActive)
            {
                maxVelocity *= 2.4f;
                acceleration *= 2.7f;
            }

            if (npc.Center.Y > destination.Y + 50f)
            {
                if (npc.velocity.Y > 0f)
                    npc.velocity.Y *= 0.99f;
                npc.velocity.Y -= acceleration;
                if (npc.velocity.Y > maxVelocity.Y)
                    npc.velocity.Y = maxVelocity.Y;
            }
            else if (npc.Center.Y < destination.Y - 50f)
            {
                if (npc.velocity.Y < 0f)
                    npc.velocity.Y *= 0.99f;
                npc.velocity.Y += acceleration;
                if (npc.velocity.Y < -maxVelocity.Y)
                    npc.velocity.Y = -maxVelocity.Y;
            }

            if (npc.Center.X > destination.X + 100f)
            {
                if (npc.velocity.X > 0f)
                    npc.velocity.X *= 0.99f;
                npc.velocity.X -= acceleration;
                if (npc.velocity.X > maxVelocity.X)
                    npc.velocity.X = maxVelocity.X;
            }

            if (npc.Center.X < destination.X - 100f)
            {
                if (npc.velocity.X < 0f)
                    npc.velocity.X *= 0.99f;
                npc.velocity.X += acceleration;
                if (npc.velocity.X < -maxVelocity.X)
                    npc.velocity.X = -maxVelocity.X;
            }
        }

        public static void SelectNextAttack(NPC npc)
        {
            npc.ai[2]++;

            bool enraged = ComboAttackManager.FightState == LeviAnahitaFightState.AloneEnraged;
            AnahitaAttackType[] patternToUse = new AnahitaAttackType[]
            {
                AnahitaAttackType.FloatTowardsPlayer,
                AnahitaAttackType.CreateWaterIllusions,
                AnahitaAttackType.FloatTowardsPlayer,
                AnahitaAttackType.IceMistBarrages,
                AnahitaAttackType.FloatTowardsPlayer,
                AnahitaAttackType.PlaySinusoidalSong,
                enraged ? AnahitaAttackType.AtlantisCharge : AnahitaAttackType.FloatTowardsPlayer,
                AnahitaAttackType.FloatTowardsPlayer,
                AnahitaAttackType.ChargeAndCreateWaterCircle,
            };
            AnahitaAttackType nextAttackType = patternToUse[(int)(npc.ai[2] % patternToUse.Length)];

            // Go to the next AI state.
            npc.ai[0] = (int)nextAttackType;
            ComboAttackManager.SelectNextAttackSpecific(npc);

            // Reset the attack timer.
            npc.ai[1] = 0f;

            // Reset the misc ai slots.
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            int timeBetweenFrames = 8;
            npc.frameCounter++;
            if (npc.frameCounter > timeBetweenFrames * Main.npcFrameCount[npc.type])
                npc.frameCounter = 0;

            npc.frame.Y = frameHeight * (int)(npc.frameCounter / timeBetweenFrames);
            if (npc.frame.Y >= frameHeight * Main.npcFrameCount[npc.type])
                npc.frame.Y = 0;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            float horizontalAfterimageInterpolant = npc.localAI[1];
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            switch ((int)npc.localAI[0])
            {
                case 0:
                    texture = TextureAssets.Npc[npc.type].Value;
                    break;
                case 1:
                    texture = ModContent.Request<Texture2D>("CalamityMod/NPCs/Leviathan/AnahitaStabbing").Value;
                    break;
            }

            bool charging = npc.localAI[0] == 1f;
            int height = texture.Height / Main.npcFrameCount[npc.type];
            int width = texture.Width;
            Vector2 baseDrawPosition = npc.Center - Main.screenPosition;
            Color baseColor = npc.GetAlpha(lightColor) * (1f - horizontalAfterimageInterpolant);
            SpriteEffects direction = charging ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            if (npc.spriteDirection == -1)
                direction = charging ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Vector2 origin = new Vector2(width, height) * 0.5f;

            // Draw horizontal afterimages.
            if (horizontalAfterimageInterpolant > 0f)
            {
                for (int i = -3; i <= 3; i++)
                {
                    if (i == 0)
                        continue;

                    Vector2 drawOffset = Vector2.UnitX * i * horizontalAfterimageInterpolant * 100f;
                    Color afterimageColor = npc.GetAlpha(Color.Cyan with { A = 97 }) * (1f - horizontalAfterimageInterpolant);
                    Main.spriteBatch.Draw(texture, baseDrawPosition + drawOffset, npc.frame, afterimageColor, npc.rotation, origin, npc.scale, direction, 0f);
                }
            }

            Main.spriteBatch.Draw(texture, baseDrawPosition, npc.frame, baseColor, npc.rotation, origin, npc.scale, direction, 0f);
            return false;
        }

        public override IEnumerable<Func<NPC, string>> GetTips(bool hatGirl)
        {
            yield return n =>
            {
                if (hatGirl)
                    return "You can weave through the clefs if you manipulate her movement well!";
                return string.Empty;
            };
            yield return n =>
            {
                if (hatGirl && NPC.AnyNPCs(ModContent.NPCType<LeviathanNPC>()))
                    return "The meteors all split in the same way; use this to your advantage!";
                return string.Empty;
            };
        }
    }
}