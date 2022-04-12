using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Events;
using CalamityMod.NPCs.AstrumDeus;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumDeus
{
    public class AstrumDeusHeadBehaviorOverride : NPCBehaviorOverride
    {
        public enum DeusAttackType
        {
            AstralBombs,
            StellarCrash,
            CelestialLights,
            WarpCharge,
            StarWeave,
            InfectedPlasmaVomit,
            ConstellationSpawn,
            FusionBurst
        }

        public const float Phase2LifeThreshold = 0.55f;
        public const float Phase3LifeThreshold = 0.2f;

        public override int NPCOverrideType => ModContent.NPCType<AstrumDeusHeadSpectral>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public override bool PreAI(NPC npc)
        {
            // Emit a pale white light idly.
            Lighting.AddLight(npc.Center, 0.3f, 0.3f, 0.3f);

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();

            // Reset damage. Do none by default if somewhat transparent.
            npc.damage = npc.alpha > 40 ? 0 : npc.defDamage;

            // If none was found or it was too far away, despawn.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || Main.dayTime || !Main.player[npc.target].active || !npc.WithinRange(Main.player[npc.target].Center, 7800f))
            {
                DoBehavior_Despawn(npc);
                return false;
            }

            // Set the whoAmI index.

            // Create a beacon if none exists.
            List<Projectile> beacons = Utilities.AllProjectilesByID(ModContent.ProjectileType<DeusRitualDrama>()).ToList();
            if (beacons.Count == 0)
            {
                Projectile.NewProjectile(npc.Center, Vector2.Zero, ModContent.ProjectileType<DeusRitualDrama>(), 0, 0f);
                beacons = Utilities.AllProjectilesByID(ModContent.ProjectileType<DeusRitualDrama>()).ToList();
            }

            Player target = Main.player[npc.target];

            float lifeRatio = npc.life / (float)npc.lifeMax;
            float beaconAngerFactor = Utils.InverseLerp(3600f, 5600f, MathHelper.Distance(beacons.First().Center.X, target.Center.X), true);
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float hasCreatedSegments = ref npc.localAI[0];
            ref float releasingParticlesFlag = ref npc.localAI[1];

            // Save the beacon anger factor in a variable.
            npc.Infernum().ExtraAI[6] = beaconAngerFactor;
            npc.Calamity().CurrentlyEnraged = beaconAngerFactor > 0.7f;

            // Create segments and initialize on the first frame.
            if (Main.netMode != NetmodeID.MultiplayerClient && hasCreatedSegments == 0f)
            {
                CreateSegments(npc, 54, ModContent.NPCType<AstrumDeusBodySpectral>(), ModContent.NPCType<AstrumDeusTailSpectral>());
                attackType = (int)DeusAttackType.AstralBombs;
                hasCreatedSegments = 1f;
                npc.netUpdate = true;
            }

            // Clamp position into the world.
            npc.position.X = MathHelper.Clamp(npc.position.X, 700f, Main.maxTilesX * 16f - 700f);
            npc.position.Y = MathHelper.Clamp(npc.position.Y, 600f, Main.maxTilesY * 16f - 600f);

            // Quickly fade in.
            npc.alpha = Utils.Clamp(npc.alpha - 16, 0, 255);

            switch ((DeusAttackType)(int)attackType)
            {
                case DeusAttackType.AstralBombs:
                    DoBehavior_AstralBombs(npc, target, beaconAngerFactor, lifeRatio, attackTimer);
                    break;
                case DeusAttackType.StellarCrash:
                    DoBehavior_StellarCrash(npc, target, beaconAngerFactor, lifeRatio, ref attackTimer);
                    break;
                case DeusAttackType.CelestialLights:
                    DoBehavior_CelestialLights(npc, target, beaconAngerFactor, lifeRatio, attackTimer);
                    break;
                case DeusAttackType.WarpCharge:
                    DoBehavior_WarpCharge(npc, target, beaconAngerFactor, lifeRatio, attackTimer, ref releasingParticlesFlag);
                    break;
                case DeusAttackType.StarWeave:
                    DoBehavior_StarWeave(npc, target, beaconAngerFactor, ref attackTimer);
                    break;
                case DeusAttackType.InfectedPlasmaVomit:
                    DoBehavior_InfectedPlasmaVomit(npc, target, beaconAngerFactor, ref attackTimer);
                    break;
                case DeusAttackType.ConstellationSpawn:
                    DoBehavior_ConstellationSpawn(npc, target, beaconAngerFactor, ref attackTimer);
                    break;
                case DeusAttackType.FusionBurst:
                    DoBehavior_FusionBurst(npc, target, beaconAngerFactor, ref attackTimer);
                    break;
            }
            attackTimer++;
            return false;
        }

        #region Custom Behaviors

        public static void DoBehavior_Despawn(NPC npc)
        {
            // Ascend into the sky and disappear.
            npc.velocity = Vector2.Lerp(npc.velocity, -Vector2.UnitY * 26f, 0.08f);
            npc.velocity.X *= 0.975f;
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            // Cap the despawn timer so that the boss can swiftly disappear.
            npc.timeLeft = Utils.Clamp(npc.timeLeft - 1, 0, 120);

            if (npc.timeLeft <= 0)
            {
                npc.life = 0;
                npc.active = false;
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_AstralBombs(NPC npc, Player target, float beaconAngerFactor, float lifeRatio, float attackTimer)
        {
            int shootRate = (int)MathHelper.Lerp(9f, 5f, 1f - lifeRatio);
            int totalBombsToShoot = lifeRatio < Phase2LifeThreshold ? 38 : 45;
            float flySpeed = MathHelper.Lerp(12f, 16f, 1f - lifeRatio) + beaconAngerFactor * 10f;
            float flyAcceleration = MathHelper.Lerp(0.028f, 0.034f, 1f - lifeRatio) + beaconAngerFactor * 0.036f;
            if (BossRushEvent.BossRushActive)
            {
                flySpeed *= 2f;
                flyAcceleration *= 1.7f;
                totalBombsToShoot -= 14;
            }

            int shootTime = shootRate * totalBombsToShoot;
            int attackSwitchDelay = lifeRatio < Phase2LifeThreshold ? 45 : 105;

            // Initialize movement if it is too low.
            if (npc.velocity == Vector2.Zero || npc.velocity.Length() < 5f)
                npc.velocity = npc.velocity.SafeNormalize(-Vector2.UnitY) * 5.25f;

            // Drift towards the player. Contact damage is possible, but should be of little threat.
            if (!npc.WithinRange(target.Center, 625f))
            {
                float newSpeed = MathHelper.Lerp(npc.velocity.Length(), flySpeed, 0.075f);
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), flyAcceleration, true) * newSpeed;
            }

            // Release astral bomb mines from the sky.
            // They become faster/closer at specific life thresholds.
            if (attackTimer % shootRate == 0 && attackTimer < shootTime)
            {
                SoundEngine.PlaySound(SoundID.Item11, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float bombShootOffset = lifeRatio < Phase2LifeThreshold ? 850f : 1050f;
                    Vector2 bombShootPosition = target.Center - Vector2.UnitY.RotatedByRandom(MathHelper.PiOver2) * bombShootOffset;
                    Vector2 bombShootVelocity = (target.Center - bombShootPosition).SafeNormalize(Vector2.UnitY) * 27f;
                    if (lifeRatio < Phase2LifeThreshold)
                        bombShootVelocity *= 1.4f;

                    Utilities.NewProjectileBetter(bombShootPosition, bombShootVelocity, ModContent.ProjectileType<DeusMine2>(), 155, 0f);
                }
            }

            // Adjust rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            if (attackTimer > shootTime + attackSwitchDelay)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_StellarCrash(NPC npc, Player target, float beaconAngerFactor, float lifeRatio, ref float attackTimer)
        {
            int totalCrashes = lifeRatio < Phase2LifeThreshold ? 2 : 3;
            int crashRiseTime = lifeRatio < Phase2LifeThreshold ? 315 : 375;
            int crashChargeTime = lifeRatio < Phase2LifeThreshold ? 100 : 95;
            float crashSpeed = MathHelper.Lerp(41f, 56f, 1f - lifeRatio) + beaconAngerFactor * 20f;
            if (BossRushEvent.BossRushActive)
            {
                crashChargeTime -= 8;
                crashSpeed *= 1.4f;
            }

            float wrappedTime = attackTimer % (crashRiseTime + crashChargeTime);

            // Rise upward and release redirecting astral bombs.
            if (wrappedTime < crashRiseTime)
            {
                Vector2 riseDestination = target.Center + new Vector2(70f, MathHelper.Lerp(-1250f, -965f, 1f - lifeRatio));
                if (!npc.WithinRange(riseDestination, 95f))
                {
                    npc.Center = npc.Center.MoveTowards(riseDestination, 12f);
                    npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(riseDestination), 0.035f);
                    npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(riseDestination) * (33.5f + beaconAngerFactor * 13f), 0.6f);
                }
                else
                    attackTimer += crashRiseTime - wrappedTime;

                if (npc.WithinRange(target.Center, 285f))
                    npc.velocity = npc.velocity.RotatedBy(MathHelper.Pi * 0.025f) * 0.95f;
            }

            // Attempt to crash into the target from above after releasing flames as a dive-bomb.
            else
            {
                if (wrappedTime - crashRiseTime < 16)
                {
                    npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(target.Center + target.velocity * 20f) * crashSpeed, crashSpeed * 0.11f);
                    if (Main.netMode != NetmodeID.MultiplayerClient && wrappedTime % 3f == 0f)
                    {
                        Vector2 shootVelocity = Vector2.Lerp(npc.velocity.SafeNormalize(-Vector2.UnitY), npc.SafeDirectionTo(target.Center), 0.95f) * Main.rand.NextFloat(12f, 14f);
                        shootVelocity = shootVelocity.RotatedByRandom(0.34f);
                        Utilities.NewProjectileBetter(npc.Center + shootVelocity * 2f, shootVelocity, ModContent.ProjectileType<LargeAstralComet>(), 155, 0f);
                    }
                }

                if (wrappedTime == crashRiseTime + 8f)
                    SoundEngine.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/AstrumDeusSplit"), target.Center);
            }

            // Adjust rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            if (attackTimer > totalCrashes * (crashRiseTime + crashChargeTime) + 40)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_CelestialLights(NPC npc, Player target, float beaconAngerFactor, float lifeRatio, float attackTimer)
        {
            float flySpeed = MathHelper.Lerp(12.5f, 17f, 1f - lifeRatio);
            float flyAcceleration = MathHelper.Lerp(0.03f, 0.038f, 1f - lifeRatio);
            if (BossRushEvent.BossRushActive)
            {
                flySpeed *= 2f;
                flyAcceleration *= 1.75f;
            }

            int shootRate = lifeRatio < Phase2LifeThreshold ? 2 : 4;
            shootRate = (int)MathHelper.Lerp(shootRate, 1f, beaconAngerFactor);

            ref float starCounter = ref npc.Infernum().ExtraAI[0];

            // Initialize movement if it is too low.
            if (npc.velocity == Vector2.Zero || npc.velocity.Length() < 5f)
                npc.velocity = npc.velocity.SafeNormalize(-Vector2.UnitY) * 5.25f;

            // Drift towards the player. Contact damage is possible, but should be of little threat.
            if (!npc.WithinRange(target.Center, 865f))
            {
                float newSpeed = MathHelper.Lerp(npc.velocity.Length(), flySpeed, 0.08f);
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), flyAcceleration, true) * newSpeed;
            }

            if (attackTimer > 60f && attackTimer % shootRate == shootRate - 1f && starCounter < 30f)
            {
                int bodyType = ModContent.NPCType<AstrumDeusBodySpectral>();
                int shootIndex = (int)(starCounter * 2f);
                Vector2 starSpawnPosition = Vector2.Zero;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].ai[2] != shootIndex || Main.npc[i].type != bodyType || !Main.npc[i].active)
                        continue;

                    starSpawnPosition = Main.npc[i].Center;
                    break;
                }

                SoundEngine.PlaySound(SoundID.Item9, starSpawnPosition);
                if (Main.netMode != NetmodeID.MultiplayerClient && starSpawnPosition != Vector2.Zero)
                {
                    Vector2 starVelocity = -Vector2.UnitY.RotatedByRandom(0.37f) * Main.rand.NextFloat(5f, 6f);
                    starVelocity *= MathHelper.Lerp(1f, 1.8f, beaconAngerFactor);
                    int star = Utilities.NewProjectileBetter(starSpawnPosition, starVelocity, ModContent.ProjectileType<AstralStar>(), 160, 0f);
                    if (Main.projectile.IndexInRange(star))
                        Main.projectile[star].ai[1] = beaconAngerFactor;
                }

                starCounter++;
            }

            if (attackTimer >= 60f + shootRate * 30f + 380f)
                SelectNextAttack(npc);

            // Adjust rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public static void DoBehavior_WarpCharge(NPC npc, Player target, float beaconAngerFactor, float lifeRatio, float attackTimer, ref float releasingParticlesFlag)
        {
            int fadeInTime = lifeRatio < Phase2LifeThreshold ? 42 : 60;
            int chargeTime = lifeRatio < Phase2LifeThreshold ? 38 : 45;
            int fadeOutTime = fadeInTime / 2 + 10;
            int chargeCount = lifeRatio < Phase2LifeThreshold ? 3 : 4;
            float teleportOutwardness = MathHelper.Lerp(1420f, 1095f, 1f - lifeRatio) - beaconAngerFactor * 240f;
            float chargeSpeed = MathHelper.Lerp(33.5f, 45f, 1f - lifeRatio) + beaconAngerFactor * 15f;
            if (BossRushEvent.BossRushActive)
            {
                chargeTime -= 4;
                chargeSpeed *= 1.425f;
            }

            float wrappedTimer = attackTimer % (fadeInTime + chargeTime + fadeOutTime);

            if (wrappedTimer < fadeInTime)
            {
                // Drift towards the player. Contact damage is possible, but should be of little threat.
                if (!npc.WithinRange(target.Center, 375f) && wrappedTimer < 45f)
                {
                    float newSpeed = MathHelper.Lerp(npc.velocity.Length(), 16f, 0.085f);
                    npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.032f, true) * newSpeed;
                }
                else if (wrappedTimer >= 45f)
                {
                    float maxSpeed = BossRushEvent.BossRushActive ? 33f : 26f;
                    if (npc.velocity.Length() < maxSpeed)
                        npc.velocity *= 1.015f;
                }

                npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.13f, 0f, 1f);
                if (wrappedTimer == fadeInTime - 1f)
                {
                    Vector2 teleportOffsetDirection = -target.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.456f);

                    SoundEngine.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/AstrumDeusSplit"), target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        npc.Center = target.Center + teleportOffsetDirection * teleportOutwardness;
                        npc.velocity = npc.SafeDirectionTo(target.Center + target.velocity * 20f) * chargeSpeed;
                        npc.netUpdate = true;

                        for (int i = 0; i < 5; i++)
                        {
                            Vector2 fireVelocity = (MathHelper.TwoPi * i / 5f).ToRotationVector2() * 7f;
                            if (BossRushEvent.BossRushActive)
                                fireVelocity *= 2.3f;

                            Utilities.NewProjectileBetter(npc.Center, fireVelocity, ModContent.ProjectileType<AstralFlame2>(), 155, 0f);
                        }

                        BringAllSegmentsToNPCPosition(npc);
                    }
                }
            }
            else if (wrappedTimer < fadeInTime + chargeTime)
                npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.15f, 0f, 1f);
            else if (wrappedTimer < fadeInTime + chargeTime + fadeOutTime)
            {
                npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.06f, 0f, 1f);

                if (!npc.WithinRange(target.Center, 200f))
                    npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.05f);

                // Teleport near the player again at the end of the cycle.
                if (wrappedTimer == fadeInTime + chargeTime + fadeOutTime)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        npc.Center = target.Center + Main.rand.NextVector2CircularEdge(teleportOutwardness, teleportOutwardness) * 1.75f;
                        npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                        npc.netUpdate = true;

                        BringAllSegmentsToNPCPosition(npc);
                    }
                }
            }

            // Release particles when disappearing.
            releasingParticlesFlag = (npc.Opacity < 0.5f && npc.Opacity > 0f).ToInt();

            if (attackTimer >= chargeCount * (fadeInTime + chargeTime + fadeOutTime) + 3)
                SelectNextAttack(npc);

            // Adjust rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public static void DoBehavior_StarWeave(NPC npc, Player target, float beaconAngerFactor, ref float attackTimer)
        {
            // Apply the extreme gravity debuff.
            if (Main.netMode != NetmodeID.Server)
                target.AddBuff(ModContent.BuffType<ExtremeGrav>(), 25);

            float spinSpeed = 34f;
            ref float cantKeepSpinningFlag = ref npc.Infernum().ExtraAI[0];

            // Check if any segments are too close to the target.
            bool tooClose = npc.WithinRange(target.Center, 650f);
            int bodyType = ModContent.NPCType<AstrumDeusBodySpectral>();
            if (!tooClose)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].type != bodyType || !Main.npc[i].active || !Main.npc[i].WithinRange(target.Center, 300f))
                        continue;

                    tooClose = true;
                    break;
                }
            }

            // Move away from the target if it's too far away if the star has not been created yet.
            // This delays the star creation.
            if (attackTimer < 60f && tooClose)
            {
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(target.Center) * -30f, 3f);
                attackTimer = 30f;
            }

            // Move near the target if it's too far away if the star has not been created yet.
            // This delays the star creation.
            if (attackTimer < 60f && !npc.WithinRange(target.Center, 1550f))
            {
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(target.Center) * 23f, 1.5f);
                attackTimer = 30f;
            }

            // Otherwise approach the ideal spin speed.
            else if (npc.velocity.Length() < spinSpeed && attackTimer < 90f)
                npc.velocity = npc.velocity.SafeNormalize(-Vector2.UnitY) * MathHelper.Lerp(npc.velocity.Length(), spinSpeed, 0.075f);

            // Spin if the boss is allowed to do so.
            if (cantKeepSpinningFlag == 0f)
                npc.velocity = npc.velocity.RotatedBy(MathHelper.TwoPi / 125f);

            // Summon a small star near the center point at which the boss is currently spinning.
            // The star will grow based on energy generated by body segments.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 60f)
            {
                Vector2 starSpawnPosition = npc.Center + npc.velocity.RotatedBy(MathHelper.PiOver2) * 135f / MathHelper.TwoPi;
                int star = Utilities.NewProjectileBetter(starSpawnPosition, Vector2.Zero, ModContent.ProjectileType<GiantAstralStar>(), 250, 0f);
                if (Main.projectile.IndexInRange(star))
                    Main.projectile[star].localAI[0] = beaconAngerFactor;
            }

            List<Projectile> stars = Utilities.AllProjectilesByID(ModContent.ProjectileType<GiantAstralStar>()).ToList();

            // Keep the attack timer in stasis while the star is being formed.
            // This also allows the spin to begin.
            if (attackTimer > 60f && stars.Count > 0 && stars.First().scale < 7f)
            {
                cantKeepSpinningFlag = 0f;
                attackTimer = 60f;
            }

            // If an opening is found to hit the target and the star is fully charged, fly towards the target and send the star towards them too.
            bool aimedTowardsPlayer = npc.velocity.AngleBetween(npc.SafeDirectionTo(target.Center)) < MathHelper.Pi * 0.18f;
            if (aimedTowardsPlayer && stars.Count > 0 && stars.First().velocity == Vector2.Zero && attackTimer > 90f)
            {
                stars.First().velocity = stars.First().SafeDirectionTo(target.Center) * MathHelper.Lerp(4f, 8f, beaconAngerFactor);
                if (BossRushEvent.BossRushActive)
                    stars.First().velocity *= 2.5f;

                stars.First().netUpdate = true;

                cantKeepSpinningFlag = 1f;
                npc.velocity = npc.SafeDirectionTo(target.Center) * MathHelper.Lerp(24f, 38f, beaconAngerFactor);
                if (BossRushEvent.BossRushActive)
                    npc.velocity *= 1.4f;

                npc.netUpdate = true;
            }

            // Adjust rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            if (attackTimer > 90f && !npc.WithinRange(target.Center, 270f))
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(target.Center) * (15f + beaconAngerFactor * 5f), 0.7f);

            if (attackTimer > 270f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_InfectedPlasmaVomit(NPC npc, Player target, float beaconAngerFactor, ref float attackTimer)
        {
            int shootRate = (int)MathHelper.Lerp(32f, 12f, beaconAngerFactor);
            ref float shootTimer = ref npc.Infernum().ExtraAI[0];

            // Drift towards the player.
            if (!npc.WithinRange(target.Center, 530f))
            {
                float newSpeed = MathHelper.Lerp(npc.velocity.Length(), BossRushEvent.BossRushActive ? 31f : 17f, 0.085f);
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.036f, true) * newSpeed;
            }

            // Release bursts of plasma.
            if (shootTimer >= shootRate)
            {
                SoundEngine.PlaySound(SoundID.Item74, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 plasmaVelocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.56f) * Main.rand.NextFloat(14f, 18f);
                        if (BossRushEvent.BossRushActive)
                            plasmaVelocity *= 1.8f;

                        Utilities.NewProjectileBetter(npc.Center + plasmaVelocity * 2f, plasmaVelocity, ModContent.ProjectileType<InfectiousPlasma>(), 160, 0f);
                    }
                    shootTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            // Adjust rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            if (attackTimer >= 270f)
                SelectNextAttack(npc);

            shootTimer++;
        }

        public static void DoBehavior_ConstellationSpawn(NPC npc, Player target, float beaconAngerFactor, ref float attackTimer)
        {
            int totalStarsToCreate = 16;
            ref float shootCounter = ref npc.Infernum().ExtraAI[0];
            ref float shootTimer = ref npc.Infernum().ExtraAI[1];

            if (attackTimer >= 120f)
                shootTimer++;
            if (attackTimer > 120f && shootCounter < totalStarsToCreate)
                attackTimer = 120f;

            // Drift towards the player.
            if (!npc.WithinRange(target.Center, 680f))
            {
                float newSpeed = MathHelper.Lerp(npc.velocity.Length(), 15f, 0.085f);
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.05f, true) * newSpeed;
            }

            if (shootTimer > Main.rand.Next(10, 18) - beaconAngerFactor * 6f && shootCounter < totalStarsToCreate)
            {
                SoundEngine.PlaySound(SoundID.Item8, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 spawnPosition = npc.Center - Vector2.UnitY.RotatedByRandom(MathHelper.PiOver2) * Main.rand.NextFloat(20f, 96f);
                    int star = Utilities.NewProjectileBetter(spawnPosition, Vector2.Zero, ModContent.ProjectileType<ConstellationStar>(), 0, 0f);
                    if (Main.projectile.IndexInRange(star))
                        Main.projectile[star].ai[0] = shootCounter;

                    shootCounter++;
                }
                shootTimer = 0f;
                npc.netUpdate = true;
            }

            if (attackTimer == 175f)
            {
                SoundEngine.PlaySound(SoundID.Item72, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int starType = ModContent.ProjectileType<ConstellationStar>();
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        if (Main.projectile[i].type != starType || !Main.projectile[i].active)
                            continue;

                        Main.projectile[i].timeLeft = Main.rand.Next(70, 140);
                        Main.projectile[i].netUpdate = true;
                    }
                }
            }

            if (attackTimer > 320f)
                SelectNextAttack(npc);

            // Adjust rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public static void DoBehavior_FusionBurst(NPC npc, Player target, float beaconAngerFactor, ref float attackTimer)
        {
            int fireDelay = 270;
            int totalBursts = 3;
            bool cannotFireAnymore = attackTimer >= fireDelay + (AstralPlasmaBeam.Lifetime + 40f) * totalBursts;
            bool doneCharging = attackTimer > fireDelay - 75f;
            bool rayHasBeenReleased = attackTimer > fireDelay;
            float idealGeneralMoveSpeed = doneCharging ? 6f : 15f;
            float idealGeneralMoveAcceleration = doneCharging ? 0.013f : 0.03f;
            ref float burstShootTimer = ref npc.Infernum().ExtraAI[0];

            // Drift towards the player.
            float rotateThreshold = MathHelper.Pi * 0.76f;
            if (rayHasBeenReleased && (attackTimer - fireDelay) % (AstralPlasmaBeam.Lifetime + 40f) > AstralPlasmaBeam.Lifetime)
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center) - 0.2f, 0.027f, true) * idealGeneralMoveSpeed;

            if (!npc.WithinRange(target.Center, 475f) && npc.velocity.AngleBetween(npc.SafeDirectionTo(target.Center)) < rotateThreshold)
            {
                float newSpeed = MathHelper.Lerp(npc.velocity.Length(), idealGeneralMoveSpeed, 0.085f);
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center) - 0.025f, idealGeneralMoveAcceleration, true) * newSpeed;
            }

            // Get close to the target if they're far off prior to firing so that the player can know what the boss is doing.
            // This delays the firing.
            if (!npc.WithinRange(target.Center, 1000f) && attackTimer > fireDelay - 150f && attackTimer < fireDelay - 60f)
            {
                npc.Center = npc.Center.MoveTowards(target.Center, 56f);
                npc.velocity = npc.SafeDirectionTo(target.Center) * npc.velocity.Length();
                attackTimer = fireDelay - 80f;
            }

            // Create plasma particles near the mouth to indicate that something is happening.
            if (attackTimer < fireDelay)
            {
                int particleCount = (int)MathHelper.SmoothStep(2f, 6f, Utils.InverseLerp(15f, fireDelay - 45f, attackTimer, true));
                Vector2 mouthPosition = npc.Center + (npc.rotation - MathHelper.PiOver2).ToRotationVector2() * 30f;
                for (int i = 0; i < particleCount; i++)
                {
                    Vector2 drawOffset = Main.rand.NextVector2CircularEdge(24f, 24f) * Main.rand.NextFloat(0.8f, 1.2f) + npc.velocity;
                    Dust plasma = Dust.NewDustPerfect(mouthPosition + drawOffset, 267);
                    plasma.color = Color.Lerp(Main.rand.NextBool() ? Color.Cyan : Color.OrangeRed, Color.White, Main.rand.NextFloat(0.5f, 0.8f));
                    plasma.scale = Main.rand.NextFloat(1.1f, 1.4f);
                    plasma.velocity = (mouthPosition + npc.velocity - plasma.position) * 0.08f + npc.velocity * 2f;
                    plasma.noGravity = true;
                }
            }

            // Cause the screen to shake prior to firing in anticipatiom.
            target.Infernum().CurrentScreenShakePower = Utils.InverseLerp(fireDelay - 105f, fireDelay - 15f, attackTimer, true) * Utils.InverseLerp(fireDelay + 12f, fireDelay, attackTimer, true) * 4f;

            // Play an acoustic indicator prior to firing as a charge.
            if (attackTimer == fireDelay - 120f)
                SoundEngine.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/CrystylCharge"), target.Center);

            // Release a plasma beam periodically.
            if (!cannotFireAnymore && attackTimer >= fireDelay && (attackTimer - fireDelay) % (AstralPlasmaBeam.Lifetime + 40f) == 0)
            {
                SoundEngine.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PlasmaGrenadeExplosion"), target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int plasmaBeam = Utilities.NewProjectileBetter(npc.Center, Vector2.UnitY, ModContent.ProjectileType<AstralPlasmaBeam>(), 270, 0f);
                    if (Main.projectile.IndexInRange(plasmaBeam))
                        Main.projectile[plasmaBeam].ai[1] = npc.whoAmI;
                }
            }

            // Periodically release bursts of astral rays outward from the mouth while rays are firing.
            float shootSpeed = MathHelper.Lerp(15f, 27f, beaconAngerFactor);
            int laserCount = (int)MathHelper.Lerp(12f, 20f, beaconAngerFactor);
            int laserBurstShootRate = (int)MathHelper.Lerp(50f, 24f, beaconAngerFactor);
            if (!cannotFireAnymore && rayHasBeenReleased)
                burstShootTimer++;

            if (burstShootTimer >= laserBurstShootRate)
            {
                SoundEngine.PlaySound(SoundID.Item12, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < laserCount; i++)
                    {
                        Vector2 shootVelocity = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * shootSpeed;
                        Utilities.NewProjectileBetter(npc.Center + shootVelocity * 3f, shootVelocity, ModContent.ProjectileType<AstralShot2>(), 160, 0f);
                    }
                    burstShootTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            // Adjust rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            if (attackTimer >= fireDelay + (AstralPlasmaBeam.Lifetime + 40f) * totalBursts + 125f)
                SelectNextAttack(npc);
        }
        #endregion Custom Behaviors

        #region Misc AI Operations
        public static void SelectNextAttack(NPC npc)
        {
            // Select a new target.
            npc.TargetClosest();

            float lifeRatio = npc.life / (float)npc.lifeMax;
            DeusAttackType oldAttackState = (DeusAttackType)(int)npc.ai[0];
            DeusAttackType secondLastAttackState = (DeusAttackType)(int)npc.ai[3];
            DeusAttackType newAttackState;
            WeightedRandom<DeusAttackType> attackSelector = new();
            attackSelector.Add(DeusAttackType.AstralBombs, lifeRatio < Phase2LifeThreshold ? 0.5f : 0.8f);
            attackSelector.Add(DeusAttackType.StellarCrash, lifeRatio < Phase2LifeThreshold ? 0.7f : 1f);
            attackSelector.Add(DeusAttackType.CelestialLights, lifeRatio < Phase2LifeThreshold ? 0.8f : 1.15f);
            attackSelector.Add(DeusAttackType.WarpCharge, lifeRatio < Phase2LifeThreshold ? 0.8f : 1f);
            if (lifeRatio < Phase2LifeThreshold)
            {
                attackSelector.Add(DeusAttackType.StarWeave, 1.225f);
                attackSelector.Add(DeusAttackType.InfectedPlasmaVomit, 1.1f);
                attackSelector.Add(DeusAttackType.ConstellationSpawn, 1.15f);
            }
            if (lifeRatio < Phase3LifeThreshold)
                attackSelector.Add(DeusAttackType.FusionBurst, 4.1f);

            do
                newAttackState = attackSelector.Get();
            while (newAttackState == oldAttackState || newAttackState == secondLastAttackState);

            npc.ai[0] = (int)newAttackState;
            npc.ai[1] = 0f;
            npc.ai[3] = (int)oldAttackState;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
        }

        public static void CreateSegments(NPC npc, int wormLength, int bodyType, int tailType)
        {
            int previousIndex = npc.whoAmI;
            for (int i = 0; i < wormLength; i++)
            {
                int nextIndex;
                if (i < wormLength - 1)
                    nextIndex = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, bodyType, npc.whoAmI);
                else
                    nextIndex = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, tailType, npc.whoAmI);

                Main.npc[nextIndex].realLife = npc.whoAmI;
                Main.npc[nextIndex].ai[2] = i;
                Main.npc[nextIndex].ai[1] = npc.whoAmI;
                Main.npc[nextIndex].ai[0] = previousIndex;
                if (i < wormLength - 1)
                    Main.npc[nextIndex].localAI[3] = i % 2;

                // Force sync the new segment into existence.
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, nextIndex, 0f, 0f, 0f, 0);

                previousIndex = nextIndex;
            }
        }

        public static void BringAllSegmentsToNPCPosition(NPC npc)
        {
            int segmentCount = 0;
            int bodyType = ModContent.NPCType<AstrumDeusBodySpectral>();
            int tailType = ModContent.NPCType<AstrumDeusTailSpectral>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && (Main.npc[i].type == bodyType || Main.npc[i].type == tailType))
                {
                    Main.npc[i].Center = npc.Center - (npc.rotation - MathHelper.PiOver2).ToRotationVector2() * segmentCount;
                    Main.npc[i].netUpdate = true;
                    segmentCount++;
                }
            }
        }

        #endregion Misc AI Operations
    }
}
