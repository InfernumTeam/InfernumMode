using CalamityMod.Events;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace InfernumMode.BehaviorOverrides.BossAIs.Prime
{
    public class PrimeHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.SkeletronPrime;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        #region Enumerations
        public enum PrimeAttackType
        {
            SpawnEffects,
            MetalBurst,
            RocketRelease,
            HoverCharge,
            EyeLaserRays,
            LightningSupercharge,
            ReleaseTeslaMines
        }

        public enum PrimeFrameType
        {
            ClosedMouth,
            OpenMouth,
            Spikes
        }
        #endregion

        #region AI
        public static bool AnyArms => NPC.AnyNPCs(NPCID.PrimeCannon) || NPC.AnyNPCs(NPCID.PrimeLaser) || NPC.AnyNPCs(NPCID.PrimeVice) || NPC.AnyNPCs(NPCID.PrimeSaw);
        public static int RemainingArms => NPC.AnyNPCs(NPCID.PrimeCannon).ToInt() + NPC.AnyNPCs(NPCID.PrimeLaser).ToInt() + NPC.AnyNPCs(NPCID.PrimeVice).ToInt() + NPC.AnyNPCs(NPCID.PrimeSaw).ToInt();
        public static bool ShouldBeInactive(int armType, float armCycleTimer)
        {
            if (RemainingArms <= 2)
                return false;

            armCycleTimer %= 1800f;
            if (armCycleTimer < 450f)
                return armType == NPCID.PrimeSaw || armType == NPCID.PrimeVice;
            if (armCycleTimer < 900f)
                return armType == NPCID.PrimeVice || armType == NPCID.PrimeCannon;
            if (armCycleTimer < 1350f)
                return armType == NPCID.PrimeCannon || armType == NPCID.PrimeLaser;
            return armType == NPCID.PrimeLaser || armType == NPCID.PrimeSaw;
        }

        public static void ArmHoverAI(NPC npc)
        {
            ref float angularVelocity = ref npc.localAI[0];

            // Have angular velocity rely on exponential momentum.
            angularVelocity = angularVelocity * 0.8f + npc.velocity.X * 0.04f * 0.2f;
            npc.rotation = npc.rotation.AngleLerp(angularVelocity, 0.15f);
        }

        public const float Phase2LifeRatio = 0.4f;

        public override bool PreAI(NPC npc)
        {
            npc.frame = new Rectangle(100000, 100000, 94, 94);
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float armCycleTimer = ref npc.ai[2];
            ref float hasRedoneSpawnAnimation = ref npc.ai[3];
            ref float frameType = ref npc.localAI[0];
            ref float hasCreatedShield = ref npc.Infernum().ExtraAI[6];

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();

            float lifeRatio = npc.life / (float)npc.lifeMax;
            Player target = Main.player[npc.target];

            // Continuously reset defense and damage.
            npc.defense = npc.defDefense;
            npc.damage = npc.defDamage + 24;

            // Don't allow damage to happen if any arms remain or the shield is still up.
            List<Projectile> shields = Utilities.AllProjectilesByID(ModContent.ProjectileType<PrimeShield>()).ToList();
            npc.dontTakeDamage = AnyArms || shields.Count > 0;

            // Create the shield.
            if (Main.netMode != NetmodeID.MultiplayerClient && hasCreatedShield == 0f)
            {
                int shield = Projectile.NewProjectile(npc.Center, Vector2.Zero, ModContent.ProjectileType<PrimeShield>(), 0, 0f, 255, npc.whoAmI);
                Main.projectile[shield].ai[0] = npc.whoAmI;
                hasCreatedShield = 1f;
            }

            if (!target.active || target.dead || Main.dayTime)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, -Vector2.UnitY * 26f, 0.08f);
                if (!npc.WithinRange(target.Center, 1560f))
                    npc.active = false;

                return false;
            }

            // Do the spawn animation again once entering the second phase.
            if (!AnyArms && hasRedoneSpawnAnimation == 0f && attackType != (int)PrimeAttackType.SpawnEffects)
            {
                attackTimer = 0f;
                attackType = (int)PrimeAttackType.SpawnEffects;
                hasRedoneSpawnAnimation = 1f;

                List<int> projectilesToDelete = new List<int>()
                {
                    ModContent.ProjectileType<MetallicSpike>(),
                    ModContent.ProjectileType<LaserBolt>(),
                    ModContent.ProjectileType<PrimeNuke>(),
                    ModContent.ProjectileType<NuclearExplosion>(),
                };

                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].active && projectilesToDelete.Contains(Main.projectile[i].type))
                        Main.projectile[i].active = false;
                }

                npc.netUpdate = true;
            }

            switch ((PrimeAttackType)(int)attackType)
            {
                case PrimeAttackType.SpawnEffects:
                    DoAttack_SpawnEffects(npc, target, attackTimer, ref frameType);
                    break;
                case PrimeAttackType.MetalBurst:
                    DoAttack_MetalBurst(npc, target, attackTimer, ref frameType);
                    break;
                case PrimeAttackType.RocketRelease:
                    DoAttack_RocketRelease(npc, target, lifeRatio, attackTimer, ref frameType);
                    break;
                case PrimeAttackType.HoverCharge:
                    DoAttack_HoverCharge(npc, target, lifeRatio, attackTimer, ref frameType);
                    break;
                case PrimeAttackType.EyeLaserRays:
                    DoAttack_EyeLaserRays(npc, target, lifeRatio, attackTimer, ref frameType);
                    break;
                case PrimeAttackType.LightningSupercharge:
                    DoAttack_LightningSupercharge(npc, target, ref attackTimer, ref frameType);
                    break;
                case PrimeAttackType.ReleaseTeslaMines:
                    DoAttack_ReleaseTeslaMines(npc, target, lifeRatio, ref attackTimer, ref frameType);
                    break;
            }

            if (npc.position.Y < 900f)
                npc.position.Y = 900f;

            armCycleTimer++;
            attackTimer++;
            return false;
        }

        #region Specific Attacks
        public static void DoAttack_SpawnEffects(NPC npc, Player target, float attackTimer, ref float frameType)
        {
            bool canHover = attackTimer < 90f;

            // Focus on the boss as it spawns.
            if (Main.LocalPlayer.WithinRange(Main.LocalPlayer.Center, 3700f))
            {
                Main.LocalPlayer.Infernum().ScreenFocusPosition = npc.Center;
                Main.LocalPlayer.Infernum().ScreenFocusInterpolant = Utils.InverseLerp(0f, 15f, attackTimer, true);
                Main.LocalPlayer.Infernum().ScreenFocusInterpolant *= Utils.InverseLerp(210f, 202f, attackTimer, true);
            }

            // Don't do damage during the spawn animation.
            npc.damage = 0;

            if (canHover)
            {
                Vector2 hoverDestination = target.Center - Vector2.UnitY * 500f;

                npc.velocity = npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(npc.Distance(hoverDestination), 32f);
                npc.rotation = npc.rotation.AngleLerp(npc.velocity.X * 0.04f, 0.1f);

                if (npc.WithinRange(target.Center, 90f))
                {
                    npc.Center = target.Center - npc.SafeDirectionTo(target.Center, Vector2.UnitY) * 90f;
                    npc.ai[1] = 89f;
                    npc.netUpdate = true;
                }

                frameType = (int)PrimeFrameType.ClosedMouth;
            }
            else
            {
                if (attackTimer >= 195f)
                    frameType = (int)PrimeFrameType.OpenMouth;

                npc.velocity *= 0.85f;
                npc.rotation = npc.rotation.AngleLerp(0f, 0.2f);
                if (attackTimer > 210f)
                {
                    Main.PlaySound(SoundID.Roar, target.Center, 0);

                    if (Main.netMode != NetmodeID.MultiplayerClient && npc.ai[3] == 0f)
                    {
                        npc.TargetClosest();
                        int arm = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCID.PrimeCannon, npc.whoAmI);
                        Main.npc[arm].ai[0] = -1f;
                        Main.npc[arm].ai[1] = npc.whoAmI;
                        Main.npc[arm].target = npc.target;
                        Main.npc[arm].netUpdate = true;

                        arm = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCID.PrimeLaser, npc.whoAmI);
                        Main.npc[arm].ai[0] = 1f;
                        Main.npc[arm].ai[1] = npc.whoAmI;
                        Main.npc[arm].target = npc.target;
                        Main.npc[arm].netUpdate = true;

                        arm = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCID.PrimeSaw, npc.whoAmI);
                        Main.npc[arm].ai[0] = 1f;
                        Main.npc[arm].ai[1] = npc.whoAmI;
                        Main.npc[arm].target = npc.target;
                        Main.npc[arm].netUpdate = true;

                        arm = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCID.PrimeVice, npc.whoAmI);
                        Main.npc[arm].ai[0] = -1f;
                        Main.npc[arm].ai[1] = npc.whoAmI;
                        Main.npc[arm].target = npc.target;
                        Main.npc[arm].netUpdate = true;
                    }

                    SelectNextAttack(npc);
                }
            }
        }

        public static void DoAttack_MetalBurst(NPC npc, Player target, float attackTimer, ref float frameType)
        {
            int shootRate = AnyArms ? 125 : 70;
            int shootCount = AnyArms ? 4 : 5;
            int spikesPerBurst = AnyArms ? 7 : 23;
            float hoverSpeed = AnyArms ? 15f : 36f;
            float wrappedTime = attackTimer % shootRate;

            if (BossRushEvent.BossRushActive)
            {
                spikesPerBurst += 10;
                hoverSpeed = MathHelper.Max(hoverSpeed, 30f) * 1.2f;
            }

            // Don't do contact damage, to prevent cheap hits.
            npc.damage = 0;

            Vector2 destination = target.Center - Vector2.UnitY * (AnyArms ? 550f : 435f);
            if (!npc.WithinRange(destination, 40f))
                npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * hoverSpeed, hoverSpeed / 65f);
            else
                npc.velocity *= 1.02f;
            npc.rotation = npc.velocity.X * 0.04f;

            bool canFire = attackTimer <= shootRate * shootCount && attackTimer > 75f;

            // Open the mouth a little bit before shooting.
            frameType = wrappedTime >= shootRate * 0.7f ? (int)PrimeFrameType.OpenMouth : (int)PrimeFrameType.ClosedMouth;

            // Only shoot projectiles if above and not extremely close to the player.
            if (wrappedTime == shootRate - 1f && npc.Center.Y < target.Center.Y - 150f && !npc.WithinRange(target.Center, 200f) && canFire)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < spikesPerBurst; i++)
                    {
                        Vector2 spikeVelocity = (MathHelper.TwoPi * i / spikesPerBurst).ToRotationVector2() * 5.5f;
                        if (AnyArms)
                            spikeVelocity *= 0.56f;
                        if (BossRushEvent.BossRushActive)
                            spikeVelocity *= 3f;

                        Utilities.NewProjectileBetter(npc.Center + spikeVelocity * 12f, spikeVelocity, ModContent.ProjectileType<MetallicSpike>(), 135, 0f);
                    }
                }
                Main.PlaySound(SoundID.Item101, target.Center);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= shootRate * shootCount + 90f)
                SelectNextAttack(npc);
        }

        public static void DoAttack_RocketRelease(NPC npc, Player target, float lifeRatio, float attackTimer, ref float frameType)
        {
            int cycleTime = 36;
            int rocketCountPerCycle = 7;
            int shootCycleCount = AnyArms ? 4 : 6;
            int rocketShootDelay = AnyArms ? 60 : 35;

            // The attack lasts longer when only the laser and cannon are around so you can focus them down.
            if (!NPC.AnyNPCs(NPCID.PrimeVice) && !NPC.AnyNPCs(NPCID.PrimeSaw) && NPC.AnyNPCs(NPCID.PrimeLaser) && NPC.AnyNPCs(NPCID.PrimeCannon))
                shootCycleCount = 6;

            float wrappedTime = attackTimer % cycleTime;

            npc.rotation = npc.velocity.X * 0.04f;

            frameType = (int)PrimeFrameType.ClosedMouth;
            if (wrappedTime > cycleTime - rocketCountPerCycle * 2f && attackTimer > rocketShootDelay)
            {
                frameType = (int)PrimeFrameType.OpenMouth;

                if (!npc.WithinRange(target.Center, 250f))
                    npc.velocity *= 0.87f;

                Main.PlaySound(SoundID.Item42, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient && wrappedTime % 3f == 2f)
                {
                    float rocketSpeed = Main.rand.NextFloat(10.5f, 12f) * (AnyArms ? 0.825f : 1f);
                    if (!AnyArms)
                        rocketSpeed += (1f - lifeRatio) * 5.6f;
                    Vector2 rocketVelocity = Main.rand.NextVector2CircularEdge(rocketSpeed, rocketSpeed);
                    if (rocketVelocity.Y < -1f)
                        rocketVelocity.Y = -1f;
                    rocketVelocity = Vector2.Lerp(rocketVelocity, npc.SafeDirectionTo(target.Center).RotatedByRandom(0.4f) * rocketVelocity.Length(), 0.6f);
                    rocketVelocity = rocketVelocity.SafeNormalize(-Vector2.UnitY) * rocketSpeed;
                    Utilities.NewProjectileBetter(npc.Center + Vector2.UnitY * 33f, rocketVelocity, ProjectileID.SaucerMissile, 150, 0f);
                }

                if (Main.netMode != NetmodeID.MultiplayerClient && wrappedTime % 12f == 11f && !AnyArms)
                {
                    Vector2 idealVelocity = npc.SafeDirectionTo(target.Center + Main.rand.NextVector2Circular(100f, 100f)) * 8f;
                    idealVelocity += Main.rand.NextVector2Circular(2f, 2f);
                    Vector2 spawnPosition = npc.Center + idealVelocity * 3f;

                    int skull = Utilities.NewProjectileBetter(spawnPosition, idealVelocity, ProjectileID.Skull, 160, 0f, Main.myPlayer, -1f, 0f);
                    Main.projectile[skull].ai[0] = -1f;
                    Main.projectile[skull].timeLeft = 300;
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= cycleTime * (shootCycleCount + 0.4f))
                SelectNextAttack(npc);
        }

        public static void DoAttack_HoverCharge(NPC npc, Player target, float lifeRatio, float attackTimer, ref float frameType)
        {
            int chargeCount = 4;
            int hoverTime = AnyArms ? 120 : 60;
            int chargeTime = AnyArms ? 72 : 45;
            float hoverSpeed = AnyArms ? 14f : 33f;
            float chargeSpeed = AnyArms ? 15f : 29f;

            // Have a bit longer of a delay for the first charge.
            if (attackTimer < hoverTime + chargeTime)
                hoverTime += 15;

            float wrappedTime = attackTimer % (hoverTime + chargeTime);

            if (BossRushEvent.BossRushActive)
            {
                hoverSpeed *= 1.3f;
                chargeSpeed *= 1.6f;
            }

            if (!AnyArms)
                chargeSpeed += (1f - lifeRatio) * 5f;

            if (wrappedTime < hoverTime - 15f)
            {
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 365f, -300f);
                npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(npc.Distance(hoverDestination), hoverSpeed)) / 8f;

                if (npc.WithinRange(target.Center, 150f))
                {
                    npc.Center = target.Center - npc.SafeDirectionTo(target.Center, Vector2.UnitY) * 150f;
                    npc.velocity = Vector2.Zero;
                }
                npc.rotation = npc.velocity.X * 0.04f;
                frameType = (int)PrimeFrameType.ClosedMouth;
            }
            else if (wrappedTime < hoverTime)
            {
                npc.velocity *= 0.94f;
                npc.rotation = npc.velocity.X * 0.04f;
                frameType = (int)PrimeFrameType.OpenMouth;
            }
            else
            {
                if (wrappedTime == hoverTime + 1f)
                {
                    npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                    npc.netUpdate = true;

                    if (!AnyArms)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int i = 0; i < 11; i++)
                            {
                                Vector2 shootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.Lerp(-0.7f, 0.7f, i / 10f)) * 8f;
                                Utilities.NewProjectileBetter(npc.Center + shootVelocity * 7f, shootVelocity, ModContent.ProjectileType<MetallicSpike>(), 135, 0f);
                            }
                        }
                        Main.PlaySound(SoundID.Item101, target.Center);
                    }

                    Main.PlaySound(SoundID.Roar, target.Center, 0);
                }

                frameType = (int)PrimeFrameType.Spikes;
                npc.rotation += npc.velocity.Length() * 0.018f;
            }

            if (attackTimer >= (hoverTime + chargeTime) * chargeCount + 20)
                SelectNextAttack(npc);
        }

        public static void DoAttack_EyeLaserRays(NPC npc, Player target, float lifeRatio, float attackTimer, ref float frameType)
        {
            int shootDelay = 95;
            ref float laserRayRotation = ref npc.Infernum().ExtraAI[0];
            ref float lineTelegraphInterpolant = ref npc.Infernum().ExtraAI[1];
            ref float angularOffset = ref npc.Infernum().ExtraAI[2];

            // Calculate the line telegraph interpolant.
            lineTelegraphInterpolant = 0f;
            if (attackTimer < shootDelay)
                lineTelegraphInterpolant = Utils.InverseLerp(0f, 0.8f, attackTimer / shootDelay, true);

            // Hover into position.
            angularOffset = MathHelper.ToRadians(39f);
            Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 320f, -270f) - npc.velocity * 4f;
            float movementSpeed = MathHelper.Lerp(33f, 4.5f, Utils.InverseLerp(shootDelay / 2, shootDelay - 5f, attackTimer, true));
            npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(npc.Distance(hoverDestination), movementSpeed)) / 8f;

            // Stay away from the target, to prevent cheap contact damage.
            if (npc.WithinRange(target.Center, 150f))
            {
                npc.Center = target.Center - npc.SafeDirectionTo(target.Center, Vector2.UnitY) * 150f;
                npc.velocity = Vector2.Zero;
            }
            npc.rotation = npc.velocity.X * 0.04f;

            // Play a telegraph sound prior to firing.
            if (attackTimer == 5f)
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/CrystylCharge"), target.Center);

            if (attackTimer == shootDelay - 35f)
                Main.PlaySound(SoundID.Roar, target.Center, 0);

            // Release the lasers from eyes.
            if (attackTimer == shootDelay)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/LaserCannon"), target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = -1; i <= 1; i += 2)
                    {
                        Vector2 beamSpawnPosition = npc.Center + new Vector2(-i * 16f, -7f);
                        Vector2 beamDirection = (target.Center - beamSpawnPosition).SafeNormalize(-Vector2.UnitY).RotatedBy(angularOffset * -i);

                        int beam = Utilities.NewProjectileBetter(beamSpawnPosition, beamDirection, ModContent.ProjectileType<PrimeEyeLaserRay>(), 230, 0f);
                        if (Main.projectile.IndexInRange(beam))
                        {
                            Main.projectile[beam].ai[0] = i * angularOffset / 120f * 0.4f;
                            Main.projectile[beam].ai[1] = npc.whoAmI;
                            Main.projectile[beam].netUpdate = true;
                        }
                    }

                    laserRayRotation = npc.AngleTo(target.Center);
                }
            }

            // Calculate frames.
            frameType = attackTimer < shootDelay - 15f ? (int)PrimeFrameType.ClosedMouth : (int)PrimeFrameType.OpenMouth;

            // Release a few rockets after creating the laser to create pressure.
            int rocketReleaseRate = lifeRatio < Phase2LifeRatio ? 11 : 18;
            if (attackTimer > shootDelay && attackTimer % rocketReleaseRate == rocketReleaseRate - 1f)
            {
                Main.PlaySound(SoundID.Item42, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float rocketAngularOffset = Utils.InverseLerp(shootDelay, 195f, attackTimer, true) * MathHelper.TwoPi;
                    Vector2 rocketVelocity = rocketAngularOffset.ToRotationVector2() * (Main.rand.NextFloat(5.5f, 6.2f) + npc.Distance(target.Center) * 0.00267f);
                    Utilities.NewProjectileBetter(npc.Center + Vector2.UnitY * 33f + rocketVelocity * 2.5f, rocketVelocity, ModContent.ProjectileType<MetallicSpike>(), 155, 0f);
                }
            }

            // Create a bullet hell outside of the laser area to prevent RoD cheese.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer > shootDelay)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 shootDirection;
                    do
                        shootDirection = Main.rand.NextVector2Unit();
                    while (shootDirection.AngleBetween(laserRayRotation.ToRotationVector2()) < angularOffset);

                    Vector2 spikeVelocity = shootDirection * Main.rand.NextFloat(11f, 20f);
                    Utilities.NewProjectileBetter(npc.Center + spikeVelocity * 5f, spikeVelocity, ModContent.ProjectileType<MetallicSpike>(), 180, 0f);
                }
            }

            if (attackTimer >= 225f)
                SelectNextAttack(npc);
        }

        public static void DoAttack_LightningSupercharge(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            int lightningCreationDelay = 35;
            ref float struckByLightningFlag = ref npc.Infernum().ExtraAI[0];
            ref float lineTelegraphInterpolant = ref npc.Infernum().ExtraAI[1];
            ref float superchargeTimer = ref npc.Infernum().ExtraAI[2];
            ref float laserSignDirection = ref npc.Infernum().ExtraAI[3];
            ref float laserOffsetAngle = ref npc.Infernum().ExtraAI[4];
            ref float laserDirection = ref npc.Infernum().ExtraAI[5];

            // Reset the line telegraph interpolant.
            lineTelegraphInterpolant = 0f;

            if (attackTimer < lightningCreationDelay)
            {
                npc.velocity *= 0.84f;
                npc.rotation = npc.velocity.X * 0.04f;
            }

            // Create a bunch of scenic lightning and decide the laser direction.
            if (attackTimer == lightningCreationDelay)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/LightningStrike"), target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 lightningSpawnPosition = npc.Center - Vector2.UnitY * 1300f + Main.rand.NextVector2Circular(30f, 30f);
                        if (lightningSpawnPosition.Y < 600f)
                            lightningSpawnPosition.Y = 600f;
                        int lightning = Utilities.NewProjectileBetter(lightningSpawnPosition, Vector2.UnitY * Main.rand.NextFloat(1.7f, 2f), ModContent.ProjectileType<LightningStrike>(), 0, 0f);
                        if (Main.projectile.IndexInRange(lightning))
                        {
                            Main.projectile[lightning].ai[0] = MathHelper.PiOver2;
                            Main.projectile[lightning].ai[1] = Main.rand.Next(100);
                        }
                    }
                }

                if (laserSignDirection == 0f)
                {
                    laserSignDirection = Main.rand.NextBool().ToDirectionInt();
                    npc.netUpdate = true;
                }
            }

            frameType = (int)PrimeFrameType.ClosedMouth;

            // Stop the attack timer if lightning has not supercharged yet. Also declare the laser direction for laser.
            if (attackTimer > lightningCreationDelay + 1f && struckByLightningFlag == 0f)
            {
                attackTimer = lightningCreationDelay + 1f;
                laserDirection = npc.AngleTo(target.Center);
            }

            else if (struckByLightningFlag == 1f)
            {
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 320f, -270f) - npc.velocity * 4f;
                float movementSpeed = MathHelper.Lerp(1f, 0.7f, Utils.InverseLerp(45f, 90f, attackTimer, true)) * npc.Distance(target.Center) * 0.0074f;
                if (movementSpeed < 4.25f)
                    movementSpeed = 0f;

                npc.velocity = (npc.velocity * 6f + npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(npc.Distance(hoverDestination), movementSpeed)) / 7f;
                npc.rotation = npc.velocity.X * 0.04f;

                if (npc.WithinRange(target.Center, 150f))
                {
                    npc.Center = target.Center - npc.SafeDirectionTo(target.Center, Vector2.UnitY) * 150f;
                    npc.velocity = Vector2.Zero;
                }

                superchargeTimer++;

                // Prepare line telegraphs.
                if (attackTimer < 165f)
                {
                    lineTelegraphInterpolant = Utils.InverseLerp(lightningCreationDelay, 165, attackTimer, true);
                    laserDirection += Utils.InverseLerp(0f, 0.6f, lineTelegraphInterpolant, true) * Utils.InverseLerp(1f, 0.7f, lineTelegraphInterpolant, true) * MathHelper.Pi / 300f;
                }

                // Roar as a telegraph.
                if (attackTimer == 130f)
                {
                    Main.PlaySound(SoundID.Roar, target.Center, 0);
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/PlagueSounds/PBGNukeWarning"), target.Center);
                }
                if (attackTimer > 95f)
                    frameType = (int)PrimeFrameType.OpenMouth;

                float shootSpeedAdditive = npc.Distance(target.Center) * 0.0084f;
                if (BossRushEvent.BossRushActive)
                    shootSpeedAdditive += 10f;

                // Fire 9 lasers outward. They intentionally avoid intersecting the player's position and do not rotate.
                // Their purpose is to act as a "border".
                if (attackTimer == 165f)
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/LaserCannon"), target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 12; i++)
                        {
                            Vector2 laserFirePosition = npc.Center - Vector2.UnitY * 16f;
                            Vector2 individualLaserDirection = (MathHelper.TwoPi * i / 12f + laserDirection).ToRotationVector2();

                            int beam = Utilities.NewProjectileBetter(laserFirePosition, individualLaserDirection, ModContent.ProjectileType<LaserRayIdle>(), 230, 0f);
                            if (Main.projectile.IndexInRange(beam))
                            {
                                Main.projectile[beam].ai[0] = 0f;
                                Main.projectile[beam].ai[1] = npc.whoAmI;
                                Main.projectile[beam].netUpdate = true;
                            }
                        }
                    }
                }

                // Use the spike frame type and make the laser move.
                if (attackTimer > 165f)
                {
                    frameType = (int)PrimeFrameType.Spikes;
                    laserOffsetAngle += Utils.InverseLerp(165f, 255f, attackTimer, true) * laserSignDirection * MathHelper.Pi / 300f;
                }

                // Release electric sparks periodically, along with missiles.
                Vector2 mouthPosition = npc.Center + Vector2.UnitY * 33f;
                if (attackTimer > 180f && attackTimer < 435f && attackTimer % 44f == 43f)
                {
                    Main.PlaySound(SoundID.Item12, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float offsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                        for (int i = 0; i < 12; i++)
                        {
                            Vector2 electricityVelocity = (target.Center - mouthPosition).SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.TwoPi * i / 12f + offsetAngle) * (shootSpeedAdditive + 9f);
                            Utilities.NewProjectileBetter(mouthPosition, electricityVelocity, ProjectileID.MartianTurretBolt, 155, 0f);
                        }
                    }
                }
                if (attackTimer > 180f && attackTimer < 435f && attackTimer % 30f == 29f)
                {
                    Main.PlaySound(SoundID.Item42, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 rocketVelocity = (target.Center - mouthPosition).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.47f) * (shootSpeedAdditive + 6.75f);
                        Utilities.NewProjectileBetter(mouthPosition, rocketVelocity, ProjectileID.SaucerMissile, 155, 0f);
                    }
                }
            }

            if (attackTimer > 435f)
                superchargeTimer = Utils.InverseLerp(465f, 435f, attackTimer, true) * 30f;

            if (attackTimer > 465f)
                SelectNextAttack(npc);
        }

        public static void DoAttack_ReleaseTeslaMines(NPC npc, Player target, float lifeRatio, ref float attackTimer, ref float frameType)
        {
            int slowdownTime = 45;
            int bombCount = (int)MathHelper.Lerp(10f, 20f, 1f - lifeRatio);

            // Choose the frame type.
            frameType = (int)PrimeFrameType.Spikes;

            // Sit in place for a moment.
            if (attackTimer < slowdownTime)
            {
                npc.velocity *= 0.9f;
                npc.rotation = npc.velocity.X * 0.04f;
            }

            // Release a bunch of tesla orb bombs around the target.
            if (attackTimer == slowdownTime)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/MechGaussRifle"), target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < bombCount; i++)
                    {
                        Vector2 bombSpawnPosition = target.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(400f, 1550f);
                        int bomb = Utilities.NewProjectileBetter(bombSpawnPosition, Vector2.Zero, ModContent.ProjectileType<TeslaBomb>(), 160, 0f);
                        if (Main.projectile.IndexInRange(bomb))
                        {
                            Main.projectile[bomb].ai[0] = Main.rand.Next(45, 70);
                            Main.projectile[bomb].netUpdate = true;
                        }
                    }
                }
            }

            if (attackTimer == slowdownTime + 75f)
                SelectNextAttack(npc);
        }

        public static void DoAttack_CarpetBombLaserCharge(NPC npc, Player target, float lifeRatio, ref float attackTimer, ref float frameType)
        {
            int chargeCount = 3;
            int hoverTime = AnyArms ? 120 : 60;
            int chargeTime = AnyArms ? 72 : 45;
            float hoverSpeed = AnyArms ? 14f : 33f;
            float chargeSpeed = AnyArms ? 15f : 24.5f;

            // Have a bit longer of a delay for the first charge.
            if (attackTimer < hoverTime + chargeTime)
                hoverTime += 20;

            float wrappedTime = attackTimer % (hoverTime + chargeTime);

            if (BossRushEvent.BossRushActive)
            {
                hoverSpeed *= 1.3f;
                chargeSpeed *= 1.6f;
            }

            if (!AnyArms)
                chargeSpeed += (1f - lifeRatio) * 10f;

            if (wrappedTime < hoverTime - 15f)
            {
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 365f, -300f);
                npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(npc.Distance(hoverDestination), hoverSpeed)) / 8f;

                if (npc.WithinRange(target.Center, 150f))
                {
                    npc.Center = target.Center - npc.SafeDirectionTo(target.Center, Vector2.UnitY) * 150f;
                    npc.velocity = Vector2.Zero;
                }
                npc.rotation = npc.velocity.X * 0.04f;
                frameType = (int)PrimeFrameType.ClosedMouth;
            }
            else if (wrappedTime < hoverTime)
            {
                npc.velocity *= 0.94f;
                npc.rotation = npc.velocity.X * 0.04f;
                frameType = (int)PrimeFrameType.OpenMouth;
            }
            else
            {
                if (wrappedTime == hoverTime + 1f)
                {
                    npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                    npc.velocity.Y -= 10f;
                    npc.netUpdate = true;

                    Main.PlaySound(SoundID.Roar, target.Center, 0);
                }

                // Release lasers upward.
                if (Main.netMode != NetmodeID.MultiplayerClient && wrappedTime % 6f == 5f)
                    Utilities.NewProjectileBetter(npc.Center, Vector2.UnitY * -16f, ModContent.ProjectileType<ScavengerLaser>(), 165, 0f);

                frameType = (int)PrimeFrameType.Spikes;
                npc.rotation += npc.velocity.Length() * 0.018f;
            }

            if (attackTimer >= (hoverTime + chargeTime) * chargeCount + 20)
                SelectNextAttack(npc);
        }
        #endregion Specific Attacks

        #region General Helper Functions
        public static void SelectNextAttack(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            PrimeAttackType currentAttack = (PrimeAttackType)(int)npc.ai[0];
            WeightedRandom<PrimeAttackType> attackSelector = new WeightedRandom<PrimeAttackType>(Main.rand);
            if (!AnyArms)
            {
                attackSelector.Add(PrimeAttackType.MetalBurst);
                attackSelector.Add(PrimeAttackType.RocketRelease);
                attackSelector.Add(PrimeAttackType.HoverCharge);
                attackSelector.Add(PrimeAttackType.EyeLaserRays);

                if (lifeRatio < Phase2LifeRatio)
                {
                    attackSelector.Add(PrimeAttackType.ReleaseTeslaMines, 1.7);
                    if (Main.rand.NextFloat() < 0.3f)
                        attackSelector.Add(PrimeAttackType.LightningSupercharge, 20D);
                }
            }
            else
            {
                attackSelector.Add(PrimeAttackType.MetalBurst);
                attackSelector.Add(PrimeAttackType.RocketRelease);
            }

            npc.velocity *= MathHelper.Lerp(1f, 0.25f, Utils.InverseLerp(14f, 30f, npc.velocity.Length()));

            do
                npc.ai[0] = (int)attackSelector.Get();
            while (npc.ai[0] == (int)currentAttack);

            npc.TargetClosest();
            npc.ai[1] = 0f;
            for (int i = 0; i < 6; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
        }
        #endregion General Helper Function

        #endregion AI

        #region Frames and Drawcode
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            NPCID.Sets.MustAlwaysDraw[npc.type] = true;

            Texture2D texture = Main.npcTexture[npc.type];
            Texture2D eyeGlowTexture = ModContent.GetTexture("InfernumMode/ExtraTextures/PrimeEyes");
            Rectangle frame = texture.Frame(1, Main.npcFrameCount[npc.type], 0, (int)npc.localAI[0]);
            Vector2 baseDrawPosition = npc.Center - Main.screenPosition;
            for (int i = 9; i >= 0; i -= 2)
            {
                Vector2 drawPosition = npc.oldPos[i] + npc.Size * 0.5f - Main.screenPosition;
                Color afterimageColor = npc.GetAlpha(lightColor);
                afterimageColor.R = (byte)(afterimageColor.R * (10 - i) / 20);
                afterimageColor.G = (byte)(afterimageColor.G * (10 - i) / 20);
                afterimageColor.B = (byte)(afterimageColor.B * (10 - i) / 20);
                afterimageColor.A = (byte)(afterimageColor.A * (10 - i) / 20);
                spriteBatch.Draw(Main.npcTexture[npc.type], drawPosition, frame, afterimageColor, npc.rotation, frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            }

            float superchargePower = Utils.InverseLerp(0f, 30f, npc.Infernum().ExtraAI[1], true);
            if (npc.ai[0] != (int)PrimeAttackType.LightningSupercharge)
                superchargePower = 0f;

            if (superchargePower > 0f)
            {
                float outwardness = superchargePower * 6f + (float)Math.Cos(Main.GlobalTime * 2f) * 0.5f;
                for (int i = 0; i < 8; i++)
                {
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 8f + Main.GlobalTime * 2.9f).ToRotationVector2() * outwardness;
                    Color drawColor = Color.Red * 0.42f;
                    drawColor.A = 0;

                    spriteBatch.Draw(texture, baseDrawPosition + drawOffset, frame, npc.GetAlpha(drawColor), npc.rotation, frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
                }
            }

            spriteBatch.Draw(texture, baseDrawPosition, frame, npc.GetAlpha(lightColor), npc.rotation, frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);

            // Draw line telegraphs for the eye attack.
            float lineTelegraphInterpolant = npc.Infernum().ExtraAI[1];
            if (npc.ai[0] == (int)PrimeAttackType.EyeLaserRays && lineTelegraphInterpolant > 0f)
            {
                spriteBatch.SetBlendState(BlendState.Additive);

                float angularOffset = npc.Infernum().ExtraAI[2];
                Texture2D line = ModContent.GetTexture("InfernumMode/ExtraTextures/BloomLine");
                Player target = Main.player[npc.target];
                Color outlineColor = Color.Lerp(Color.Red, Color.White, lineTelegraphInterpolant);
                Vector2 origin = new Vector2(line.Width / 2f, line.Height);
                Vector2 beamScale = new Vector2(lineTelegraphInterpolant * 0.5f, 2.4f);
                for (int i = -1; i <= 1; i += 2)
                {
                    Vector2 drawPosition = npc.Center + new Vector2(i * 16f, -8f).RotatedBy(npc.rotation) - Main.screenPosition;
                    Vector2 beamDirection = -(target.Center - (drawPosition + Main.screenPosition)).SafeNormalize(-Vector2.UnitY).RotatedBy(angularOffset * -i);
                    float beamRotation = beamDirection.ToRotation() - MathHelper.PiOver2;
                    spriteBatch.Draw(line, drawPosition, null, outlineColor, beamRotation, origin, beamScale, 0, 0f);
                }
                spriteBatch.ResetBlendState();
            }

            // Draw line telegraphs for the lightning attack.
            if (npc.ai[0] == (int)PrimeAttackType.LightningSupercharge && lineTelegraphInterpolant > 0f)
            {
                spriteBatch.SetBlendState(BlendState.Additive);

                float angularOffset = npc.Infernum().ExtraAI[5];

                // I don't know where this angular offset comes from, but it exists.
                // It was fixed by comparing the lines in an image editing program and performing calculations with arctangents to determine what the precise
                // discrepency is. The calculations of such are shown below:
                // p0 = (1252, 395)
                // p1 = (1543, 432)
                // p2 = (1519, 445)
                // slope of telegraph = (p1.y - p0.y) / (p1.x - p0.x) = 0.127147
                // slope of laser = (p2.y - p0.y) / (p2.x - p0.x) = 0.187266
                // d = arctan(slope of laser) - arctan(slope of telegraph) = -0.0586534
                float angularDiscrepancy = -0.0586534f;
                Texture2D line = ModContent.GetTexture("InfernumMode/ExtraTextures/BloomLine");
                Color outlineColor = Color.Lerp(Color.Red, Color.White, lineTelegraphInterpolant);
                Vector2 origin = new Vector2(line.Width / 2f, line.Height);
                Vector2 beamScale = new Vector2(lineTelegraphInterpolant * 0.5f, 2.4f);
                for (int i = 0; i < 12; i++)
                {
                    Vector2 beamDirection = (MathHelper.TwoPi * i / 12f + angularOffset - angularDiscrepancy).ToRotationVector2();
                    Vector2 drawPosition = npc.Center - Vector2.UnitY * 16f + beamDirection * 2f - Main.screenPosition;
                    float beamRotation = beamDirection.ToRotation() - MathHelper.PiOver2;
                    spriteBatch.Draw(line, drawPosition, null, outlineColor, beamRotation, origin, beamScale, 0, 0f);
                }
                spriteBatch.ResetBlendState();
            }

            spriteBatch.Draw(eyeGlowTexture, baseDrawPosition, frame, new Color(200, 200, 200, 255), npc.rotation, frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            return false;
        }
        #endregion Frames and Drawcode
    }
}