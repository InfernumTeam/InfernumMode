using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares.AresBodyBehaviorOverride;
using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.ThanatosHeadBehaviorOverride;
using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.ExoMechManagement;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
    public static partial class ExoMechComboAttackContent
    {
        public static Dictionary<ExoMechComboAttackType, int[]> AffectedAresArms => new Dictionary<ExoMechComboAttackType, int[]>()
        {
            [ExoMechComboAttackType.ThanatosAres_ExplosionCircle] = new int[] { ModContent.NPCType<AresTeslaCannon>(), 
                                                                                ModContent.NPCType<AresPlasmaFlamethrower>() },
            [ExoMechComboAttackType.ThanatosAres_NuclearHell] = new int[] { ModContent.NPCType<AresLaserCannon>() },
            [ExoMechComboAttackType.ThanatosAres_LaserBarrage] = new int[] { ModContent.NPCType<AresLaserCannon>(),
                                                                             ModContent.NPCType<AresTeslaCannon>() },
            [ExoMechComboAttackType.ThanatosAres_ElectropulseBursts] = new int[] { ModContent.NPCType<AresTeslaCannon>(),
                                                                             ModContent.NPCType<AresPulseCannon>() },
        };

        public static bool ArmCurrentlyBeingUsed(NPC npc)
        {
            // Return false Ares is not present.
            if (CalamityGlobalNPC.draedonExoMechPrime == -1)
                return false;

            // Return false if the arm is disabled.
            if (ArmIsDisabled(npc))
                return false;

            // Locate Ares' body as an NPC.
            NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];
            if (AffectedAresArms.TryGetValue((ExoMechComboAttackType)aresBody.ai[0], out int[] activeArms))
                return activeArms.Contains(npc.type);
            return false;
        }

        public static bool UseThanatosAresComboAttack(NPC npc, ref float attackTimer, ref float frame)
        {
            NPC initialMech = FindInitialMech();
            if (initialMech is null)
                return false;

            Player target = Main.player[initialMech.target];
            switch ((ExoMechComboAttackType)initialMech.ai[0])
            {
                case ExoMechComboAttackType.ThanatosAres_ExplosionCircle:
                    return DoBehavior_ThanatosAres_ExplosionCircle(npc, target, ref attackTimer, ref frame);
                case ExoMechComboAttackType.ThanatosAres_NuclearHell:
                    return DoBehavior_ThanatosAres_NuclearHell(npc, target, ref attackTimer, ref frame);
                case ExoMechComboAttackType.ThanatosAres_LaserBarrage:
                    return DoBehavior_ThanatosAres_LaserBarrage(npc, target, ref attackTimer, ref frame);
                case ExoMechComboAttackType.ThanatosAres_ElectropulseBursts:
                    return DoBehavior_ThanatosAres_ElectropulseBursts(npc, target, ref attackTimer, ref frame);
            }
            return false;
        }

        public static bool DoBehavior_ThanatosAres_ExplosionCircle(NPC npc, Player target, ref float attackTimer, ref float frame)
        {
            int attackDelay = 210;
            int attackTime = 550;
            int plasmaBurstShootRate = 85;
            int totalPlasmaPerBurst = 6;
            float plasmaBurstMaxSpread = 0.74f;
            float plasmaShootSpeed = 15f;
            int lightningShootRate = 160;
            int totalLightningShotsPerBurst = 3;
            int lightningBurstTime = 18;

            // Thanatos spins around the target with its head always open.
            if (npc.type == ModContent.NPCType<ThanatosHead>())
            {
                Vector2 spinDestination = target.Center + (attackTimer * MathHelper.TwoPi / 150f).ToRotationVector2() * 2000f;
                npc.velocity = npc.SafeDirectionTo(spinDestination) * MathHelper.Min(npc.Distance(spinDestination), 34f);
                npc.Center = npc.Center.MoveTowards(spinDestination, target.velocity.Length() * 1.2f + 35f);
                if (npc.WithinRange(spinDestination, 40f))
                    npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                else
                    npc.rotation = npc.rotation.AngleTowards((attackTimer + 8f) * MathHelper.TwoPi / 150f + MathHelper.PiOver2, 0.25f);

                // Decide frames.
                frame = (int)ThanatosFrameType.Open;
            }

            // Ares' body hovers above the player, slowly moving back and forth horizontally.
            if (npc.type == ModContent.NPCType<AresBody>())
            {
                Vector2 hoverDestination = target.Center - Vector2.UnitY * 425f;
                if (attackTimer > attackDelay)
                    hoverDestination.X += (float)Math.Sin((attackTimer - attackDelay) * MathHelper.TwoPi / 180f) * 90f;

                // Decide frames.
                frame = (int)AresBodyFrameType.Normal;

                DoHoverMovement(npc, hoverDestination, 24f, 75f);
            }

            // Ares' plasma arm releases bursts of plasma that slow down and explode.
            // If hit by lightning the plasma explodes early.
            if (npc.type == ModContent.NPCType<AresPlasmaFlamethrower>())
            {
                // Choose a direction and rotation.
                // Rotation is relative to predictiveness.
                float aimPredictiveness = 15f;
                Vector2 aimDirection = npc.SafeDirectionTo(target.Center + target.velocity * aimPredictiveness);
                Vector2 endOfCannon = npc.Center + aimDirection.SafeNormalize(Vector2.Zero) * 66f + Vector2.UnitY * 16f;
                float idealRotation = aimDirection.ToRotation();

                if (npc.spriteDirection == 1)
                    idealRotation += MathHelper.Pi;
                if (idealRotation < 0f)
                    idealRotation += MathHelper.TwoPi;
                if (idealRotation > MathHelper.TwoPi)
                    idealRotation -= MathHelper.TwoPi;
                npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.065f);

                int direction = Math.Sign(target.Center.X - npc.Center.X);
                if (direction != 0)
                {
                    npc.direction = direction;

                    if (npc.spriteDirection != -npc.direction)
                        npc.rotation += MathHelper.Pi;

                    npc.spriteDirection = -npc.direction;
                }

                // Release dust at the end of the cannon as a telegraph.
                if (attackTimer >= attackDelay && attackTimer % plasmaBurstShootRate > plasmaBurstShootRate * 0.7f)
                {
                    Vector2 dustSpawnPosition = endOfCannon + Main.rand.NextVector2Circular(45f, 45f);
                    Dust plasma = Dust.NewDustPerfect(dustSpawnPosition, 107);
                    plasma.velocity = (endOfCannon - plasma.position) * 0.04f;
                    plasma.scale = 1.25f;
                    plasma.noGravity = true;
                }

                // Periodically release bursts of plasma bombs.
                if (attackTimer >= attackDelay && attackTimer % plasmaBurstShootRate == plasmaBurstShootRate - 1f)
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PlasmaCasterFire"), npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < totalPlasmaPerBurst; i++)
                        {
                            Vector2 plasmaShootVelocity = npc.SafeDirectionTo(target.Center).RotatedByRandom(plasmaBurstMaxSpread) * Main.rand.NextFloat(0.85f, 1f) * plasmaShootSpeed;
                            Utilities.NewProjectileBetter(endOfCannon, plasmaShootVelocity, ModContent.ProjectileType<PlasmaBomb>(), 530, 0f);
                        }
                    }
                }
            }

            // Ares' tesla cannon releases streams of lightning rapid-fire.
            if (npc.type == ModContent.NPCType<AresTeslaCannon>())
            {
                // Choose a direction and rotation.
                // Rotation is relative to predictiveness.
                float aimPredictiveness = 25f;
                Vector2 aimDirection = npc.SafeDirectionTo(target.Center + target.velocity * aimPredictiveness);
                Vector2 endOfCannon = npc.Center + aimDirection.SafeNormalize(Vector2.Zero) * 84f + Vector2.UnitY * 8f;
                float idealRotation = aimDirection.ToRotation();

                if (npc.spriteDirection == 1)
                    idealRotation += MathHelper.Pi;
                if (idealRotation < 0f)
                    idealRotation += MathHelper.TwoPi;
                if (idealRotation > MathHelper.TwoPi)
                    idealRotation -= MathHelper.TwoPi;
                npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.065f);

                int direction = Math.Sign(target.Center.X - npc.Center.X);
                if (direction != 0)
                {
                    npc.direction = direction;

                    if (npc.spriteDirection != -npc.direction)
                        npc.rotation += MathHelper.Pi;

                    npc.spriteDirection = -npc.direction;
                }

                // Release dust at the end of the cannon as a telegraph.
                if (attackTimer >= attackDelay && attackTimer % lightningShootRate > lightningShootRate * 0.6f)
                {
                    Vector2 dustSpawnPosition = endOfCannon + Main.rand.NextVector2Circular(45f, 45f);
                    Dust electricity = Dust.NewDustPerfect(dustSpawnPosition, 229);
                    electricity.velocity = (endOfCannon - electricity.position) * 0.04f;
                    electricity.scale = 1.25f;
                    electricity.noGravity = true;
                }

                // Release lightning rapidfire.
                int timeBetweenLightningBurst = lightningBurstTime / totalLightningShotsPerBurst;
                bool canFireLightning = attackTimer % lightningShootRate >= lightningShootRate - lightningBurstTime && attackTimer % timeBetweenLightningBurst == timeBetweenLightningBurst - 1f;
                if (attackTimer >= attackDelay && canFireLightning)
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/TeslaCannonFire"), npc.Center);

                    // Release sparks everywhere.
                    for (int i = 0; i < 50; i++)
                    {
                        float sparkPower = Main.rand.NextFloat();
                        Dust spark = Dust.NewDustPerfect(endOfCannon + Main.rand.NextVector2Circular(25f, 25f), 261);
                        spark.velocity = (spark.position - endOfCannon) * MathHelper.Lerp(0.08f, 0.35f, sparkPower);
                        spark.scale = MathHelper.Lerp(1f, 1.5f, sparkPower);
                        spark.fadeIn = sparkPower;
                        spark.noGravity = true;
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 lightningShootVelocity = npc.SafeDirectionTo(endOfCannon) * 8f;
                        int lightning = Utilities.NewProjectileBetter(endOfCannon, lightningShootVelocity, ModContent.ProjectileType<TerateslaLightningBlast>(), 800, 0f);
                        Utilities.NewProjectileBetter(endOfCannon + lightningShootVelocity * 15f, Vector2.Zero, ModContent.ProjectileType<TeslaExplosion>(), 0, 0f);
                        if (Main.projectile.IndexInRange(lightning))
                        {
                            Main.projectile[lightning].ai[0] = lightningShootVelocity.ToRotation();
                            Main.projectile[lightning].ai[1] = Main.rand.Next(100);
                        }
                    }
                }
            }

            return attackTimer > attackDelay + attackTime;
        }

        public static bool DoBehavior_ThanatosAres_NuclearHell(NPC npc, Player target, ref float attackTimer, ref float frame)
        {
            int attackDelay = 120;
            int attackTime = 480;
            int thanatosNukeShootRate = 100;
            int laserBurstShootRate = 64;
            float laserBurstShootSpeed = 12f;

            // Thanatos periodically releases nukes from the mouth. Its head is always open.
            if (npc.type == ModContent.NPCType<ThanatosHead>())
            {
                // Decide frames.
                frame = (int)ThanatosFrameType.Open;

                DoProjectileShootInterceptionMovement(npc, target, 1.3f);
                if (attackTimer > attackDelay && attackTimer % thanatosNukeShootRate == thanatosNukeShootRate - 1f)
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/LargeWeaponFire"), npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 nukeShootVelocity = npc.velocity.SafeNormalize(Vector2.UnitY) * 19f;
                        Utilities.NewProjectileBetter(npc.Center, nukeShootVelocity, ModContent.ProjectileType<ThanatosNuke>(), 0, 0f, npc.target);

                        npc.netUpdate = true;
                    }
                }
            }
            
            // Ares hovers above the target.
            if (npc.type == ModContent.NPCType<AresBody>())
            {
                Vector2 hoverDestination = target.Center - Vector2.UnitY * 450f;

                // Decide frames.
                frame = (int)AresBodyFrameType.Normal;

                DoHoverMovement(npc, hoverDestination, 24f, 75f);
            }

            // Ares's laser cannon releases streams of 3 lasers at the target periodically.
            if (npc.type == ModContent.NPCType<AresLaserCannon>())
            {
                // Choose a direction and rotation.
                // Rotation is relative to predictiveness.
                float aimPredictiveness = 15f;
                Vector2 aimDirection = npc.SafeDirectionTo(target.Center + target.velocity * aimPredictiveness);
                Vector2 endOfCannon = npc.Center + aimDirection.SafeNormalize(Vector2.Zero) * 74f + Vector2.UnitY * 8f;
                float idealRotation = aimDirection.ToRotation();

                if (npc.spriteDirection == 1)
                    idealRotation += MathHelper.Pi;
                if (idealRotation < 0f)
                    idealRotation += MathHelper.TwoPi;
                if (idealRotation > MathHelper.TwoPi)
                    idealRotation -= MathHelper.TwoPi;
                npc.ai[3] = idealRotation;
                npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.065f);

                int direction = Math.Sign(target.Center.X - npc.Center.X);
                if (direction != 0)
                {
                    npc.direction = direction;

                    if (npc.spriteDirection != -npc.direction)
                        npc.rotation += MathHelper.Pi;

                    npc.spriteDirection = -npc.direction;
                }

                // Release dust at the end of the cannon as a telegraph.
                if (attackTimer >= attackDelay && attackTimer % laserBurstShootRate > laserBurstShootRate * 0.7f)
                {
                    Vector2 dustSpawnPosition = endOfCannon + Main.rand.NextVector2Circular(45f, 45f);
                    Dust laser = Dust.NewDustPerfect(dustSpawnPosition, 182);
                    laser.velocity = (endOfCannon - laser.position) * 0.04f;
                    laser.scale = 1.25f;
                    laser.noGravity = true;
                }

                // Periodically release bursts of lasers.
                if (attackTimer >= attackDelay && attackTimer % laserBurstShootRate == laserBurstShootRate - 1f)
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/LaserCannon"), npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 7; i++)
                        {
                            float laserShootSpread = MathHelper.Lerp(-0.44f, 0.44f, i / 6f) + Main.rand.NextFloatDirection() * 0.04f;
                            Vector2 laserShootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(laserShootSpread) * laserBurstShootSpeed;
                            int laser = Utilities.NewProjectileBetter(endOfCannon, laserShootVelocity, ModContent.ProjectileType<CannonLaser>(), 530, 0f);
                            if (Main.projectile.IndexInRange(laser))
                                Main.projectile[laser].ai[1] = npc.whoAmI;
                        }
                    }
                }
            }

            return attackTimer > attackDelay + attackTime;
        }

        public static bool DoBehavior_ThanatosAres_LaserBarrage(NPC npc, Player target, ref float attackTimer, ref float frame)
        {
            int attackDelay = 180;
            int attackTime = 580;
            int aresLaserBurstShootRate = 60;
            int lightningShootRate = 200;
            int lightningBurstTime = 60;
            int totalLightningShotsPerBurst = 8;
            float aresLaserBurstShootSpeed = 10f;
            bool teslaArmIsDisabled = CalamityGlobalNPC.draedonExoMechPrime >= 0 && Main.npc[CalamityGlobalNPC.draedonExoMechPrime].Infernum().ExtraAI[15] == 1f;

            if (teslaArmIsDisabled)
            {
                aresLaserBurstShootRate -= 9;
                aresLaserBurstShootSpeed += 3.5f;
            }

            // Thanatos attempts to intercept the player's movement while releasing barrages of lasers at them.
            if (npc.type == ModContent.NPCType<ThanatosHead>())
            {
                // Decide frames.
                frame = (int)ThanatosFrameType.Closed;

                int segmentShootDelay = 115;
                ref float totalSegmentsToFire = ref npc.Infernum().ExtraAI[0];
                ref float segmentFireTime = ref npc.Infernum().ExtraAI[1];
                ref float segmentFireCountdown = ref npc.Infernum().ExtraAI[2];

                // Do movement.
                DoProjectileShootInterceptionMovement(npc, target, 1.55f);

                // Select segment shoot attributes.
                if (attackTimer > attackDelay && attackTimer % segmentShootDelay == segmentShootDelay - 1f)
                {
                    totalSegmentsToFire = 16f;
                    segmentFireTime = 90f;

                    segmentFireCountdown = segmentFireTime;
                    npc.netUpdate = true;
                }

                if (segmentFireCountdown > 0f)
                    segmentFireCountdown--;
            }

            // Ares hovers above the target.
            if (npc.type == ModContent.NPCType<AresBody>())
            {
                Vector2 hoverDestination = target.Center - Vector2.UnitY * 475f;

                // Decide frames.
                frame = (int)AresBodyFrameType.Normal;

                DoHoverMovement(npc, hoverDestination, 24f, 75f);
            }

            // Ares' laser arm periodically releases bursts of lasers.
            if (npc.type == ModContent.NPCType<AresLaserCannon>())
            {
                // Choose a direction and rotation.
                // Rotation is relative to predictiveness.
                float aimPredictiveness = 25f;
                Vector2 aimDirection = npc.SafeDirectionTo(target.Center + target.velocity * aimPredictiveness);
                Vector2 endOfCannon = npc.Center + aimDirection.SafeNormalize(Vector2.Zero) * 74f + Vector2.UnitY * 8f;
                float idealRotation = aimDirection.ToRotation();

                if (npc.spriteDirection == 1)
                    idealRotation += MathHelper.Pi;
                if (idealRotation < 0f)
                    idealRotation += MathHelper.TwoPi;
                if (idealRotation > MathHelper.TwoPi)
                    idealRotation -= MathHelper.TwoPi;
                npc.ai[3] = idealRotation;
                npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.065f);

                int direction = Math.Sign(target.Center.X - npc.Center.X);
                if (direction != 0)
                {
                    npc.direction = direction;

                    if (npc.spriteDirection != -npc.direction)
                        npc.rotation += MathHelper.Pi;

                    npc.spriteDirection = -npc.direction;
                }

                // Release dust at the end of the cannon as a telegraph.
                if (attackTimer >= attackDelay && attackTimer % aresLaserBurstShootRate > aresLaserBurstShootRate * 0.7f)
                {
                    Vector2 dustSpawnPosition = endOfCannon + Main.rand.NextVector2Circular(45f, 45f);
                    Dust laser = Dust.NewDustPerfect(dustSpawnPosition, 182);
                    laser.velocity = (endOfCannon - laser.position) * 0.04f;
                    laser.scale = 1.25f;
                    laser.noGravity = true;
                }

                // Periodically release bursts of lasers.
                if (attackTimer >= attackDelay && attackTimer % aresLaserBurstShootRate == aresLaserBurstShootRate - 1f)
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/LaserCannon"), npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 9; i++)
                        {
                            float laserShootSpread = MathHelper.Lerp(-0.65f, 0.65f, i / 8f) + Main.rand.NextFloatDirection() * 0.04f;
                            Vector2 laserShootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(laserShootSpread) * aresLaserBurstShootSpeed;
                            int laser = Utilities.NewProjectileBetter(endOfCannon, laserShootVelocity, ModContent.ProjectileType<CannonLaser>(), 530, 0f);
                            if (Main.projectile.IndexInRange(laser))
                                Main.projectile[laser].ai[1] = npc.whoAmI;
                        }
                    }
                }
            }

            // Ares' tesla cannon releases streams of lightning in a circular spread from time to time.
            if (npc.type == ModContent.NPCType<AresTeslaCannon>())
            {
                // Choose a direction and rotation.
                Vector2 aimDirection = npc.SafeDirectionTo(target.Center);
                Vector2 endOfCannon = npc.Center + aimDirection.SafeNormalize(Vector2.Zero) * 84f + Vector2.UnitY * 8f;
                float lightningShootAngle = Utils.InverseLerp(lightningShootRate - lightningBurstTime, lightningShootRate, attackTimer % lightningShootRate, true) * MathHelper.TwoPi;
                float idealRotation = aimDirection.ToRotation();
                if (lightningShootAngle > 0f)
                    idealRotation = lightningShootAngle;

                if (npc.spriteDirection == 1)
                    idealRotation += MathHelper.Pi;
                if (idealRotation < 0f)
                    idealRotation += MathHelper.TwoPi;
                if (idealRotation > MathHelper.TwoPi)
                    idealRotation -= MathHelper.TwoPi;
                npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.065f);

                int direction = Math.Sign(target.Center.X - npc.Center.X);
                if (direction != 0)
                {
                    npc.direction = direction;

                    if (npc.spriteDirection != -npc.direction)
                        npc.rotation += MathHelper.Pi;

                    npc.spriteDirection = -npc.direction;
                }

                // Release dust at the end of the cannon and play a sound as a telegraph.
                if (attackTimer >= attackDelay && attackTimer % lightningShootRate == (int)(lightningShootRate * 0.45f))
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/LightningStrike"), target.Center);

                if (attackTimer >= attackDelay && attackTimer % lightningShootRate > lightningShootRate * 0.6f)
                {
                    Vector2 dustSpawnPosition = endOfCannon + Main.rand.NextVector2Circular(45f, 45f);
                    Dust electricity = Dust.NewDustPerfect(dustSpawnPosition, 229);
                    electricity.velocity = (endOfCannon - electricity.position) * 0.04f;
                    electricity.scale = 1.25f;
                    electricity.noGravity = true;
                }

                // Release lightning rapidfire.
                int timeBetweenLightningBurst = lightningBurstTime / totalLightningShotsPerBurst;
                bool canFireLightning = attackTimer % lightningShootRate >= lightningShootRate - lightningBurstTime && attackTimer % timeBetweenLightningBurst == timeBetweenLightningBurst - 1f;
                if (attackTimer >= attackDelay && canFireLightning)
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/TeslaCannonFire"), npc.Center);

                    // Release sparks everywhere.
                    for (int i = 0; i < 50; i++)
                    {
                        float sparkPower = Main.rand.NextFloat();
                        Dust spark = Dust.NewDustPerfect(endOfCannon + Main.rand.NextVector2Circular(25f, 25f), 261);
                        spark.velocity = (spark.position - endOfCannon) * MathHelper.Lerp(0.08f, 0.35f, sparkPower);
                        spark.scale = MathHelper.Lerp(1f, 1.5f, sparkPower);
                        spark.fadeIn = sparkPower;
                        spark.noGravity = true;
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 lightningShootVelocity = lightningShootAngle.ToRotationVector2() * 9f;
                        int lightning = Utilities.NewProjectileBetter(endOfCannon, lightningShootVelocity, ModContent.ProjectileType<TerateslaLightningBlast>(), 800, 0f);
                        Utilities.NewProjectileBetter(endOfCannon + lightningShootVelocity * 15f, Vector2.Zero, ModContent.ProjectileType<TeslaExplosion>(), 0, 0f);
                        if (Main.projectile.IndexInRange(lightning))
                        {
                            Main.projectile[lightning].ai[0] = lightningShootVelocity.ToRotation();
                            Main.projectile[lightning].ai[1] = Main.rand.Next(100);
                        }
                    }
                }
            }

            return attackTimer > attackDelay + attackTime;
        }

        public static bool DoBehavior_ThanatosAres_ElectropulseBursts(NPC npc, Player target, ref float attackTimer, ref float frame)
        {
            int attackDelay = 180;
            int attackTime = 560;
            int lightningShootRate = 200;
            int lightningBurstTime = 60;
            int totalLightningShotsPerBurst = 6;
            int aresPulseBlastConjureRate = 90;
            int aresPulseBlastShootDelay = 55;

            // Thanatos attempts to intercept the player's movement while releasing barrages of lasers at them.
            if (npc.type == ModContent.NPCType<ThanatosHead>())
            {
                // Decide frames.
                frame = (int)ThanatosFrameType.Closed;

                int segmentShootDelay = 190;
                ref float totalSegmentsToFire = ref npc.Infernum().ExtraAI[0];
                ref float segmentFireTime = ref npc.Infernum().ExtraAI[1];
                ref float segmentFireCountdown = ref npc.Infernum().ExtraAI[2];

                // Do movement.
                DoProjectileShootInterceptionMovement(npc, target, 1.55f);

                // Select segment shoot attributes.
                if (attackTimer > attackDelay && attackTimer % segmentShootDelay == segmentShootDelay - 1f)
                {
                    totalSegmentsToFire = 40f;
                    segmentFireTime = 90f;

                    segmentFireCountdown = segmentFireTime;
                    npc.netUpdate = true;
                }

                if (segmentFireCountdown > 0f)
                    segmentFireCountdown--;
            }

            // Ares hovers above the target.
            if (npc.type == ModContent.NPCType<AresBody>())
            {
                Vector2 hoverDestination = target.Center - Vector2.UnitY * 400f;

                // Decide frames.
                frame = (int)AresBodyFrameType.Normal;

                DoHoverMovement(npc, hoverDestination, 24f, 75f);
            }

            // Ares' tesla cannon releases streams of lightning in a circular spread from time to time.
            if (npc.type == ModContent.NPCType<AresTeslaCannon>())
            {
                // Choose a direction and rotation.
                Vector2 aimDirection = npc.SafeDirectionTo(target.Center);
                Vector2 endOfCannon = npc.Center + aimDirection.SafeNormalize(Vector2.Zero) * 84f + Vector2.UnitY * 8f;
                float lightningShootAngle = Utils.InverseLerp(lightningShootRate - lightningBurstTime, lightningShootRate, attackTimer % lightningShootRate, true) * MathHelper.TwoPi;
                float idealRotation = aimDirection.ToRotation();
                if (lightningShootAngle > 0f)
                    idealRotation = lightningShootAngle;

                if (npc.spriteDirection == 1)
                    idealRotation += MathHelper.Pi;
                if (idealRotation < 0f)
                    idealRotation += MathHelper.TwoPi;
                if (idealRotation > MathHelper.TwoPi)
                    idealRotation -= MathHelper.TwoPi;
                npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.065f);

                int direction = Math.Sign(target.Center.X - npc.Center.X);
                if (direction != 0)
                {
                    npc.direction = direction;

                    if (npc.spriteDirection != -npc.direction)
                        npc.rotation += MathHelper.Pi;

                    npc.spriteDirection = -npc.direction;
                }

                // Release dust at the end of the cannon and play a sound as a telegraph.
                if (attackTimer >= attackDelay && attackTimer % lightningShootRate == (int)(lightningShootRate * 0.45f))
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/LightningStrike"), target.Center);

                if (attackTimer >= attackDelay && attackTimer % lightningShootRate > lightningShootRate * 0.6f)
                {
                    Vector2 dustSpawnPosition = endOfCannon + Main.rand.NextVector2Circular(45f, 45f);
                    Dust electricity = Dust.NewDustPerfect(dustSpawnPosition, 229);
                    electricity.velocity = (endOfCannon - electricity.position) * 0.04f;
                    electricity.scale = 1.25f;
                    electricity.noGravity = true;
                }

                // Release lightning rapidfire.
                int timeBetweenLightningBurst = lightningBurstTime / totalLightningShotsPerBurst;
                bool canFireLightning = attackTimer % lightningShootRate >= lightningShootRate - lightningBurstTime && attackTimer % timeBetweenLightningBurst == timeBetweenLightningBurst - 1f;
                if (attackTimer >= attackDelay && canFireLightning)
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/TeslaCannonFire"), npc.Center);

                    // Release sparks everywhere.
                    for (int i = 0; i < 50; i++)
                    {
                        float sparkPower = Main.rand.NextFloat();
                        Dust spark = Dust.NewDustPerfect(endOfCannon + Main.rand.NextVector2Circular(25f, 25f), 261);
                        spark.velocity = (spark.position - endOfCannon) * MathHelper.Lerp(0.08f, 0.35f, sparkPower);
                        spark.scale = MathHelper.Lerp(1f, 1.5f, sparkPower);
                        spark.fadeIn = sparkPower;
                        spark.noGravity = true;
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 lightningShootVelocity = lightningShootAngle.ToRotationVector2() * 9f;
                        int lightning = Utilities.NewProjectileBetter(endOfCannon, lightningShootVelocity, ModContent.ProjectileType<TerateslaLightningBlast>(), 800, 0f);
                        Utilities.NewProjectileBetter(endOfCannon + lightningShootVelocity * 15f, Vector2.Zero, ModContent.ProjectileType<TeslaExplosion>(), 0, 0f);
                        if (Main.projectile.IndexInRange(lightning))
                        {
                            Main.projectile[lightning].ai[0] = lightningShootVelocity.ToRotation();
                            Main.projectile[lightning].ai[1] = Main.rand.Next(100);
                        }
                    }
                }
            }

            // Ares' pulse cannon conjures up energy streams and fires unstable pulse bursts.
            if (npc.type == ModContent.NPCType<AresPulseCannon>())
            {
                ref float pulseDestinationOffsetDirection = ref npc.Infernum().ExtraAI[0];

                // Choose a direction and rotation.
                // Rotation is relative to predictiveness.
                float aimPredictiveness = 15f;
                Vector2 aimDirection = npc.SafeDirectionTo(target.Center + target.velocity * aimPredictiveness);
                Vector2 endOfCannon = npc.Center + aimDirection.SafeNormalize(Vector2.Zero) * 74f + Vector2.UnitY * 8f;
                float idealRotation = aimDirection.ToRotation();

                if (npc.spriteDirection == 1)
                    idealRotation += MathHelper.Pi;
                if (idealRotation < 0f)
                    idealRotation += MathHelper.TwoPi;
                if (idealRotation > MathHelper.TwoPi)
                    idealRotation -= MathHelper.TwoPi;
                npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.065f);

                int direction = Math.Sign(target.Center.X - npc.Center.X);
                if (direction != 0)
                {
                    npc.direction = direction;

                    if (npc.spriteDirection != -npc.direction)
                        npc.rotation += MathHelper.Pi;

                    npc.spriteDirection = -npc.direction;
                }

                // Release a pulse blast.
                Vector2 blastArcSpawnStart = endOfCannon;
                blastArcSpawnStart += npc.SafeDirectionTo(endOfCannon).RotatedBy(MathHelper.PiOver2 * npc.spriteDirection) * 20f;
                blastArcSpawnStart -= npc.SafeDirectionTo(endOfCannon) * 42f;
                Vector2 pulseBlastSpawnPosition = blastArcSpawnStart + npc.SafeDirectionTo(endOfCannon) * 125f;
                if (attackTimer > attackDelay && attackTimer % aresPulseBlastConjureRate == 1f)
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/MechGaussRifle"), npc.Center);

                    int nodeCount = 36;
                    Vector2[] dustSpawnPositions = new Vector2[nodeCount + 1];

                    // Initialize the first spawn position.
                    dustSpawnPositions[nodeCount] = pulseBlastSpawnPosition;

                    for (int j = 1; j < dustSpawnPositions.Length; j++)
                    {
                        float completionRatio = j / (float)dustSpawnPositions.Length;
                        dustSpawnPositions[j] = Vector2.Lerp(blastArcSpawnStart, pulseBlastSpawnPosition, completionRatio);
                        if (j != dustSpawnPositions.Length - 1f)
                        {
                            float offsetFactor = Main.rand.NextFloat(-12f, 12f);
                            dustSpawnPositions[j] += (blastArcSpawnStart - pulseBlastSpawnPosition).SafeNormalize(Vector2.Zero).RotatedByRandom(MathHelper.PiOver2) * offsetFactor;
                        }

                        // Spawns dust between the determined draw postions.
                        Vector2 prevPos = j == 1 ? blastArcSpawnStart : dustSpawnPositions[j - 1];
                        for (float k = 0; k < 1; k += 0.035f)
						{
                            Dust pulseLightning = Dust.NewDustPerfect(Vector2.Lerp(prevPos, dustSpawnPositions[j], k), 267, Vector2.Zero, 0, Color.Violet, 1.35f);
                            pulseLightning.noGravity = true;
                        }
                    }

                    // Spawn the blast.
                    // It will hover in place at first before being fired.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            int blast = Utilities.NewProjectileBetter(pulseBlastSpawnPosition, Vector2.Zero, ModContent.ProjectileType<AresUnstablePulseBlast>(), 650, 0f);
                            if (Main.projectile.IndexInRange(blast))
                                Main.projectile[blast].ai[0] = MathHelper.Lerp(-0.46f, 0.46f, i / 2f);
                        }
                    }
                }

                // Hold the blast in position.
                if (attackTimer > attackDelay && attackTimer % aresPulseBlastConjureRate < aresPulseBlastShootDelay)
                    pulseDestinationOffsetDirection = npc.AngleTo(pulseBlastSpawnPosition);

                // Fire all blasts.
                if (attackTimer > attackDelay && attackTimer % aresPulseBlastConjureRate == aresPulseBlastShootDelay)
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PulseRifleFire"), npc.Center);
                    foreach (Projectile blast in Utilities.AllProjectilesByID(ModContent.ProjectileType<AresUnstablePulseBlast>()))
                    {
                        blast.velocity = blast.SafeDirectionTo(target.Center).RotatedBy(blast.ai[0]) * 6f;
                        blast.netUpdate = true;
                    }
                }
            }

            // Make all blasts disappear.
            if (attackTimer == attackDelay + attackTime - 10f)
            {
                foreach (Projectile blast in Utilities.AllProjectilesByID(ModContent.ProjectileType<AresUnstablePulseBlast>()))
                {
                    blast.ai[1] = 1f;
                    blast.netUpdate = true;
                }
            }

            return attackTimer > attackDelay + attackTime;
        }
    }
}
