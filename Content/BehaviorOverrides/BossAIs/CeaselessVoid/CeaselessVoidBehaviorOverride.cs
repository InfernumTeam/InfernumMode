using CalamityMod;
using CalamityMod.DataStructures;
using CalamityMod.Events;
using CalamityMod.Items.Weapons.Typeless;
using CalamityMod.NPCs;
using CalamityMod.NPCs.CeaselessVoid;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Boss;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Signus;
using InfernumMode.Content.Projectiles;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

using CeaselessVoidBoss = CalamityMod.NPCs.CeaselessVoid.CeaselessVoid;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class CeaselessVoidBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<CeaselessVoidBoss>();

        #region Enumerations
        public enum CeaselessVoidAttackType
        {
            ChainedUp,
            DarkEnergySwirl,

            RedirectingAcceleratingDarkEnergy,
            DiagonalMirrorBolts,

            // Old attacks. The status of each is to be determined.
            RealityRendCharge,
            ConvergingEnergyBarrages,
            SlowEnergySpirals,
            DarkEnergyBulletHell,
            BlackHoleSuck
        }
        #endregion

        #region Set Defaults

        public static List<List<VerletSimulatedSegment>> Chains
        {
            get;
            internal set;
        } = null;

        public const float Phase2LifeRatio = 0.65f;

        public const float Phase3LifeRatio = 0.3f;

        public const float DarkEnergyOffsetRadius = 1120f;

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio,
            Phase3LifeRatio
        };

        public override void SetDefaults(NPC npc)
        {
            npc.npcSlots = 36f;
            npc.width = 100;
            npc.height = 100;
            npc.defense = 0;
            npc.lifeMax = 363000;
            npc.value = Item.buyPrice(0, 35, 0, 0);

            if (ModLoader.TryGetMod("CalamityModMusic", out Mod calamityModMusic))
                npc.ModNPC.Music = MusicLoader.GetMusicSlot(calamityModMusic, "Sounds/Music/CeaselessVoid");
            else
                npc.ModNPC.Music = MusicID.Boss3;
            npc.aiStyle = -1;
            npc.ModNPC.AIType = -1;
            npc.knockBackResist = 0f;
            for (int k = 0; k < npc.buffImmune.Length; k++)
                npc.buffImmune[k] = true;

            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.boss = true;
            npc.DeathSound = SoundID.NPCDeath14;
        }
        #endregion Set Defaults

        #region AI

        public override bool PreAI(NPC npc)
        {
            // Reset DR.
            npc.Calamity().DR = 0.2f;

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Set the global whoAmI variable.
            CalamityGlobalNPC.voidBoss = npc.whoAmI;

            if (!target.active || target.dead || !npc.WithinRange(target.Center, 7200f))
            {
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * 38f, 0.08f);
                if (!npc.WithinRange(target.Center, 1450f) || target.dead)
                {
                    npc.life = 0;
                    npc.active = false;
                    npc.netUpdate = true;
                }
                return false;
            }

            npc.timeLeft = 3600;
            npc.chaseable = true;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool phase2 = lifeRatio < Phase2LifeRatio;
            bool phase3 = lifeRatio < Phase3LifeRatio;
            bool enraged = target.Center.Y < Main.worldSurface * 16f && !BossRushEvent.BossRushActive;
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float currentPhase = ref npc.ai[2];

            // Do phase transitions.
            if (currentPhase == 0f && phase2)
            {
                currentPhase = 1f;
                SelectNewAttack(npc);
                attackType = (int)CeaselessVoidAttackType.DarkEnergyBulletHell;
            }
            if (currentPhase == 1f && phase3)
            {
                Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<CelestialBarrage>(), ModContent.ProjectileType<TelegraphedCelestialBarrage>(), ModContent.ProjectileType<DarkEnergyBolt>(),
                    ModContent.ProjectileType<EnergyTelegraph>(), ModContent.ProjectileType<SpiralEnergyLaser>());
                currentPhase = 2f;
                SelectNewAttack(npc);
            }

            // This debuff is not fun.
            if (target.HasBuff(BuffID.VortexDebuff))
                target.ClearBuff(BuffID.VortexDebuff);

            // Reset things every frame. They may be adjusted in the AI methods as necessary.
            npc.damage = 0;
            npc.dontTakeDamage = enraged;
            npc.Calamity().CurrentlyEnraged = npc.dontTakeDamage;
            if (enraged)
            {
                phase2 = true;
                phase3 = true;
            }

            // Lock the camera onto the ceaseless void because it's very egotistical and cannot bear the thought of not being the center of attention.
            if (Main.LocalPlayer.WithinRange(npc.Center, 2200f) && attackType != (int)CeaselessVoidAttackType.ChainedUp)
            {
                float lookAtTargetInterpolant = Utils.GetLerpValue(420f, 2700f, ((target.Center - npc.Center) * new Vector2(1f, 1.6f)).Length(), true);
                Main.LocalPlayer.Infernum_Camera().ScreenFocusInterpolant = 1f;
                Main.LocalPlayer.Infernum_Camera().ScreenFocusPosition = Vector2.Lerp(npc.Center, target.Center, lookAtTargetInterpolant);
            }

            switch ((CeaselessVoidAttackType)(int)attackType)
            {
                case CeaselessVoidAttackType.ChainedUp:
                    DoBehavior_ChainedUp(npc, ref attackTimer);
                    break;
                case CeaselessVoidAttackType.DarkEnergySwirl:
                    DoBehavior_DarkEnergySwirl(npc, phase2, phase3, target, ref attackTimer);
                    npc.boss = true;
                    break;

                case CeaselessVoidAttackType.RedirectingAcceleratingDarkEnergy:
                    DoBehavior_RedirectingAcceleratingDarkEnergy(npc, target, ref attackTimer);
                    break;
                case CeaselessVoidAttackType.DiagonalMirrorBolts:
                    DoBehavior_DiagonalMirrorBolts(npc, target, ref attackTimer);
                    break;

                case CeaselessVoidAttackType.RealityRendCharge:
                    DoBehavior_RealityRendCharge(npc, phase2, phase3, enraged, target, ref attackTimer);
                    break;
                case CeaselessVoidAttackType.ConvergingEnergyBarrages:
                    DoBehavior_ConvergingEnergyBarrages(npc, phase2, phase3, enraged, target, ref attackTimer);
                    break;
                case CeaselessVoidAttackType.SlowEnergySpirals:
                    DoBehavior_SlowEnergySpirals(npc, phase2, phase3, enraged, target, ref attackTimer);
                    break;
                case CeaselessVoidAttackType.DarkEnergyBulletHell:
                    DoBehavior_DarkEnergyBulletHell(npc, target, ref attackTimer);
                    break;
                case CeaselessVoidAttackType.BlackHoleSuck:
                    DoBehavior_BlackHoleSuck(npc, target, ref attackTimer);
                    break;
            }

            attackTimer++;
            return false;
        }

        public static void UpdateChains(NPC npc)
        {
            // Get out of here if the chains are not initialized yet.
            if (Chains is null)
                return;

            for (int i = 0; i < Chains.Count; i++)
            {
                // Check to see if a player is moving through the chains.
                for (int j = 0; j < Main.maxPlayers; j++)
                {
                    Player p = Main.player[j];
                    if (!p.active || p.dead)
                        continue;

                    MoveChainBasedOnEntity(Chains[i], p, npc);
                }

                Vector2 chainStart = Chains[i][0].position;
                Vector2 chainEnd = Chains[i].Last().position;
                float segmentDistance = Vector2.Distance(chainStart, chainEnd) / Chains[i].Count;
                Chains[i] = VerletSimulatedSegment.SimpleSimulation(Chains[i], segmentDistance, 10, 0.6f);
            }
        }

        public static void MoveChainBasedOnEntity(List<VerletSimulatedSegment> chain, Entity e, NPC npc)
        {
            Vector2 entityVelocity = e.velocity * 0.425f;
            for (int i = 1; i < chain.Count - 1; i++)
            {
                VerletSimulatedSegment segment = chain[i];
                VerletSimulatedSegment next = chain[i + 1];

                // Check to see if the entity is between two verlet segments via line/box collision checks.
                // If they are, add the entity's velocity to the two segments relative to how close they are to each of the two.
                float _ = 0f;
                if (Collision.CheckAABBvLineCollision(e.TopLeft, e.Size, segment.position, next.position, 20f, ref _))
                {
                    // Weigh the entity's distance between the two segments.
                    // If they are close to one point that means the strength of the movement force applied to the opposite segment is weaker, and vice versa.
                    float distanceBetweenSegments = segment.position.Distance(next.position);
                    float currentMovementOffsetInterpolant = Utils.GetLerpValue(e.Distance(segment.position), distanceBetweenSegments, distanceBetweenSegments * 0.2f, true);
                    float nextMovementOffsetInterpolant = 1f - currentMovementOffsetInterpolant;

                    // Move the segments based on the weight values.
                    segment.position += entityVelocity * currentMovementOffsetInterpolant;
                    if (!next.locked)
                        next.position += entityVelocity * nextMovementOffsetInterpolant;

                    // Play some cool chain sounds.
                    if (npc.soundDelay <= 0 && entityVelocity.Length() >= 0.1f)
                    {
                        SoundEngine.PlaySound(InfernumSoundRegistry.CeaselessVoidChainSound with { Volume = 0.25f }, e.Center);
                        npc.soundDelay = 27;
                    }
                }
            }
        }

        public static void DoBehavior_ChainedUp(NPC npc, ref float attackTimer)
        {
            // Initialize Ceaseless Void's binding chains on the first frame.
            if (attackTimer <= 1f)
            {
                Chains = new();

                int segmentCount = 21;
                for (int i = 0; i < 4; i++)
                {
                    Chains.Add(new());

                    // Determine how far off the chains should go.
                    Vector2 checkDirection = (MathHelper.TwoPi * i / 4f + MathHelper.PiOver4).ToRotationVector2() * new Vector2(1f, 1.2f);
                    if (checkDirection.Y > 0f)
                        checkDirection.Y *= 0.3f;

                    Vector2 chainStart = npc.Center;
                    float[] laserScanDistances = new float[16];
                    Collision.LaserScan(chainStart, checkDirection, 16f, 5000f, laserScanDistances);
                    Vector2 chainEnd = chainStart + checkDirection.SafeNormalize(Vector2.UnitY) * (laserScanDistances.Average() + 32f);

                    for (int j = 0; j < segmentCount; j++)
                    {
                        Vector2 chainPosition = Vector2.Lerp(chainStart, chainEnd, j / (float)(segmentCount - 1f));
                        Chains[i].Add(new(chainPosition, j == 0 || j == segmentCount - 1));
                    }
                }
            }

            // Update chains.
            UpdateChains(npc);

            // Disable damage.
            npc.damage = 0;
            npc.dontTakeDamage = true;

            // Prevent hovering over the Void's name to reveal what it is.
            npc.ShowNameOnHover = false;

            // Disable boss behaviors.
            npc.boss = false;
            npc.Calamity().ShouldCloseHPBar = true;
            npc.Calamity().ProvidesProximityRage = false;
        }

        public static void DoBehavior_DarkEnergySwirl(NPC npc, bool phase2, bool phase3, Player target, ref float attackTimer)
        {
            int totalRings = 4;
            int energyCountPerRing = 7;
            int portalFireRate = 105;
            int darkEnergyID = ModContent.NPCType<DarkEnergy>();

            if (phase2)
                energyCountPerRing += 2;
            if (phase3)
            {
                energyCountPerRing++;
                totalRings++;
            }

            // Destroy the chains if they are present.
            if (Main.netMode != NetmodeID.Server && Chains is not null)
            {
                SoundEngine.PlaySound(CeaselessVoidBoss.DeathSound);
                Main.LocalPlayer.Infernum_Camera().ScreenFocusInterpolant = 16f;
                ScreenEffectSystem.SetBlurEffect(npc.Center, 0.5f, 45);

                foreach (var chain in Chains)
                {
                    Vector2[] bezierPoints = chain.Select(x => x.position).ToArray();
                    BezierCurve bezierCurve = new(bezierPoints);

                    int totalChains = (int)(Vector2.Distance(chain.First().position, chain.Last().position) / 22.4f);
                    totalChains = (int)MathHelper.Clamp(totalChains, 30f, 1200f);
                    for (int i = 0; i < totalChains - 1; i++)
                    {
                        Vector2 chainPosition = bezierCurve.Evaluate(i / (float)totalChains);
                        Vector2 chainVelocity = npc.SafeDirectionTo(chainPosition).RotatedByRandom(0.8f) * Main.rand.NextFloat(2f, 12f);

                        for (int j = 1; j <= 2; j++)
                            Gore.NewGore(npc.GetSource_FromAI(), chainPosition, chainVelocity, InfernumMode.Instance.Find<ModGore>($"CeaselessVoidChain{j}").Type, 0.8f);
                    }
                }

                Chains = null;
            }

            ref float hasCreatedDarkEnergy = ref npc.Infernum().ExtraAI[0];

            // Make the screen black to distract the player from the fact that some wacky things are going on in the background.
            if (attackTimer <= 5f)
                InfernumMode.BlackFade = 1f;

            // Initialize by creating the dark energy ring.
            if (Main.netMode != NetmodeID.MultiplayerClient && hasCreatedDarkEnergy == 0f)
            {
                for (int i = 0; i < totalRings; i++)
                {
                    float spinMovementSpeed = MathHelper.Lerp(1.45f, 3f, i / (float)(totalRings - 1f));
                    for (int j = 0; j < energyCountPerRing; j++)
                    {
                        float offsetRadius = MathHelper.Lerp(0f, 150f, CalamityUtils.Convert01To010(j / (float)(energyCountPerRing - 1f)));
                        float offsetAngle = MathHelper.TwoPi * j / energyCountPerRing;
                        NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, darkEnergyID, npc.whoAmI, offsetAngle, spinMovementSpeed, offsetRadius);
                    }
                }
                hasCreatedDarkEnergy = 1f;
                npc.netUpdate = true;
            }

            // Approach the target if they're too far away.
            float hoverSpeedInterpolant = Utils.Remap(npc.Distance(target.Center), DarkEnergyOffsetRadius + 120f, DarkEnergyOffsetRadius + 600f, 0f, 0.084f);
            if (hoverSpeedInterpolant > 0f)
                npc.Center = Vector2.Lerp(npc.Center, target.Center, hoverSpeedInterpolant);

            // Disable damage.
            npc.dontTakeDamage = true;

            // Shoot lasers if moving slowly.
            if (attackTimer % portalFireRate == portalFireRate - 1f && npc.velocity.Length() < 8f)
            {
                SoundEngine.PlaySound(SoundID.Item33, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        Vector2 laserShootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.TwoPi * i / 8f) * 9.6f;
                        Utilities.NewProjectileBetter(npc.Center, laserShootVelocity, ModContent.ProjectileType<DoGBeam>(), 0, 0f, -1, 270f / 4f);
                    }
                }
            }

            // Calculate the life ratio of all dark energy combined.
            // If it is sufficiently low then all remaining dark energy fades away and CV goes to the next attack.
            int darkEnergyTotalLife = 0;
            int darkEnergyTotalMaxLife = 0;
            List<NPC> darkEnergies = new();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == darkEnergyID)
                {
                    darkEnergyTotalLife += Main.npc[i].life;
                    darkEnergyTotalMaxLife = Main.npc[i].lifeMax;
                    darkEnergies.Add(Main.npc[i]);
                }
            }
            darkEnergyTotalMaxLife *= totalRings * energyCountPerRing;

            float darkEnergyLifeRatio = darkEnergyTotalLife / (float)darkEnergyTotalMaxLife;
            if (darkEnergyTotalMaxLife <= 0)
                darkEnergyLifeRatio = 0f;

            if (darkEnergyLifeRatio <= 0.3f)
            {
                foreach (NPC darkEnergy in darkEnergies)
                {
                    if (darkEnergy.Infernum().ExtraAI[1] == 0f)
                    {
                        darkEnergy.Infernum().ExtraAI[1] = 1f;
                        darkEnergy.netUpdate = true;
                    }
                }

                Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<DoGBeam>());
                SelectNewAttack(npc);
            }
        }

        public static void DoBehavior_RedirectingAcceleratingDarkEnergy(NPC npc, Player target, ref float attackTimer)
        {
            int energyReleaseRate = 160;
            int accelerateDelay = 102;
            int accelerationTime = 38;
            int wrappedAttackTimer = (int)attackTimer % energyReleaseRate;
            int acceleratingEnergyID = ModContent.ProjectileType<AcceleratingDarkEnergy>();
            float startingEnergySpeed = 6f;
            float idealEndingSpeed = 28f;

            // Release energy balls from above.
            if (wrappedAttackTimer == 1f)
            {
                SoundEngine.PlaySound(SoundID.Item103, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 11; i++)
                    {
                        Vector2 baseSpawnOffset = (MathHelper.TwoPi * i / 11f).ToRotationVector2() * new Vector2(1f, 1.2f) * 560f;
                        for (int j = 0; j < 4; j++)
                        {
                            Vector2 microSpawnOffset = (MathHelper.TwoPi * j / 4f).ToRotationVector2() * 40f;
                            Vector2 energyRestingPosition = Vector2.Lerp(npc.Center, target.Center, 0.3f) + baseSpawnOffset + microSpawnOffset;

                            ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(darkEnergy =>
                            {
                                darkEnergy.ModProjectile<AcceleratingDarkEnergy>().RestingPosition = energyRestingPosition;
                                darkEnergy.ModProjectile<AcceleratingDarkEnergy>().Index = i * 4 + j;
                            });
                            Utilities.NewProjectileBetter(energyRestingPosition - Vector2.UnitY * 1000f, Vector2.Zero, acceleratingEnergyID, 250, 0f);
                        }
                    }
                }
            }

            // Make energy balls accelerate.
            if (wrappedAttackTimer >= accelerateDelay)
            {
                int indexToFire = wrappedAttackTimer - accelerateDelay;
                foreach (Projectile energy in Utilities.AllProjectilesByID(acceleratingEnergyID).Where(e => e.ModProjectile<AcceleratingDarkEnergy>().Index == indexToFire && e.ai[0] == 0f))
                {
                    energy.ModProjectile<AcceleratingDarkEnergy>().Time = 0f;
                    energy.ModProjectile<AcceleratingDarkEnergy>().Acceleration = Utilities.AccelerationToReachSpeed(startingEnergySpeed, idealEndingSpeed, accelerationTime);
                    energy.ModProjectile<AcceleratingDarkEnergy>().AttackState = AcceleratingDarkEnergy.DarkEnergyAttackState.AccelerateTowardsTarget;
                    energy.velocity = energy.SafeDirectionTo(target.Center) * startingEnergySpeed;
                    energy.netUpdate = true;
                }
            }
        }

        public static void DoBehavior_DiagonalMirrorBolts(NPC npc, Player target, ref float attackTimer)
        {
            int energySuckTime = 0;
            int energyBoltReleaseRate = 1;
            int energyBoltReleaseCount = 54;
            int energyBoltShootTime = energyBoltReleaseRate * energyBoltReleaseCount;
            int energyDiagonalShootDelay = energySuckTime + energyBoltShootTime + OtherworldlyBolt.LockIntoPositionTime + OtherworldlyBolt.DisappearIntoBackgroundTime;
            int energyDiagonalBootShootRate = 3;
            bool doneShooting = attackTimer >= energyDiagonalShootDelay + energyBoltReleaseCount * energyDiagonalBootShootRate;
            float energyBoltArc = MathHelper.ToRadians(300f);

            // Play funny sounds.
            if (attackTimer == energySuckTime + 1f)
                SoundEngine.PlaySound(SoundID.Item164 with { Pitch = -0.7f }, target.Center);
            if (attackTimer == energyDiagonalShootDelay + 1f)
                SoundEngine.PlaySound(SoundID.Item163 with { Pitch = -0.7f }, target.Center);

            // Release energy bolts that fly outward.
            if (attackTimer >= energySuckTime && attackTimer <= energySuckTime + energyBoltShootTime && attackTimer % energyBoltReleaseRate == 0f)
            {
                float energyBoltShootInterpolant = Utils.GetLerpValue(energySuckTime, energySuckTime + energyBoltShootTime, attackTimer, true);
                float energyBoltShootOffsetAngle = MathHelper.Lerp(0.5f * energyBoltArc, -0.5f * energyBoltArc, energyBoltShootInterpolant);
                Vector2 energyBoltShootDirection = -Vector2.UnitY.RotatedBy(energyBoltShootOffsetAngle);
                Vector2 energySpawnPosition = npc.Center + 56f * energyBoltShootDirection;
                Color energyPuffColor = Color.Lerp(Color.Purple, Color.SkyBlue, Main.rand.NextFloat(0.66f));

                MediumMistParticle darkEnergy = new(npc.Center, energyBoltShootDirection.RotatedByRandom(0.6f) * Main.rand.NextFloat(16f), energyPuffColor, Color.DarkGray * 0.6f, 1.5f, 255f);
                GeneralParticleHandler.SpawnParticle(darkEnergy);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(energySpawnPosition, energyBoltShootDirection, ModContent.ProjectileType<OtherworldlyBolt>(), 0, 0f, -1, 0f, attackTimer - (energySuckTime + energyBoltShootTime));
            }

            // Release a rain of energy bolts.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= energyDiagonalShootDelay && attackTimer % energyDiagonalBootShootRate == energyDiagonalBootShootRate - 1f)
            {
                Vector2 energyBoltSpawnPosition = target.Center + 1250f * OtherworldlyBolt.AimDirection + new Vector2(900f * Main.rand.NextFloatDirection(), -400f);
                Utilities.NewProjectileBetter(energyBoltSpawnPosition, -5f * OtherworldlyBolt.AimDirection, ModContent.ProjectileType<OtherworldlyBolt>(), 250, 0f, -1, (int)OtherworldlyBolt.OtherwordlyBoltAttackState.AccelerateFromBelow);
            }

            if (doneShooting)
                attackTimer = -45f;
        }

        public static void DoBehavior_RealityRendCharge(NPC npc, bool phase2, bool phase3, bool enraged, Player target, ref float attackTimer)
        {
            int chargeTime = 39;
            int repositionTime = 210;
            int chargeCount = 3;
            float hoverOffset = 640f;
            float chargeDistance = hoverOffset + 800f;
            float scaleFactorDelta = 0f;
            if (phase2)
            {
                chargeTime -= 6;
                repositionTime -= 20;
                scaleFactorDelta += 0.2f;
            }
            if (phase3)
            {
                chargeTime -= 8;
                scaleFactorDelta += 0.8f;
                chargeDistance += 500f;
            }
            if (BossRushEvent.BossRushActive)
                scaleFactorDelta += 0.6f;

            if (enraged)
                scaleFactorDelta = 1.45f;

            float chargeSpeed = chargeDistance / chargeTime;
            ref float tearProjectileIndex = ref npc.Infernum().ExtraAI[0];
            ref float attackState = ref npc.Infernum().ExtraAI[1];
            ref float chargeDirection = ref npc.Infernum().ExtraAI[2];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[3];
            ref float verticalHoverOffset = ref npc.Infernum().ExtraAI[4];

            switch ((int)attackState)
            {
                // Get into position for the horizontal charge.
                case 0:
                    if (attackTimer == 1f)
                        verticalHoverOffset = Main.rand.NextFloat(-0.84f, 0.84f) * hoverOffset;

                    Vector2 hoverDestination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * hoverOffset;
                    hoverDestination.Y += verticalHoverOffset;
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 23f, 0.9f);

                    // Begin the charge if either enough time has passed or within sufficient range of the hover destination.
                    if ((attackTimer >= repositionTime || npc.WithinRange(hoverDestination, 85f)) && attackTimer >= 60f)
                    {
                        attackTimer = 0f;
                        attackState = 1f;
                        chargeDirection = npc.AngleTo(target.Center);
                        npc.velocity *= 0.372f;
                        npc.netUpdate = true;

                        // Create the reality tear.
                        SoundEngine.PlaySound(YanmeisKnife.HitSound, npc.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(tear =>
                            {
                                tear.ModProjectile<RealityTear>().ScaleFactorDelta = scaleFactorDelta;
                            });
                            tearProjectileIndex = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<RealityTear>(), 0, 0f);
                        }
                    }
                    break;

                // Do the charge.
                case 1:
                    npc.damage = npc.defDamage;
                    npc.velocity = Vector2.Lerp(npc.velocity, chargeDirection.ToRotationVector2() * chargeSpeed, 0.09f);
                    if (attackTimer >= chargeTime)
                    {
                        attackState = 0f;
                        attackTimer = 0f;
                        tearProjectileIndex = -1f;
                        chargeCounter++;
                        npc.netUpdate = true;

                        if (chargeCounter >= chargeCount)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<RealityTear>());
                                Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<CelestialBarrage>());
                            }
                            SelectNewAttack(npc);
                        }
                        else
                            npc.velocity *= 0.3f;
                    }
                    break;
            }
        }

        public static void DoBehavior_ConvergingEnergyBarrages(NPC npc, bool phase2, bool phase3, bool enraged, Player target, ref float attackTimer)
        {
            int hoverTime = 20;
            int barrageBurstCount = 4;
            int barrageTelegraphTime = 18;
            int barrageShootRate = 28;
            int barrageCount = 13;
            int attackTransitionDelay = 40;
            float maxShootOffsetAngle = 1.49f;
            float initialBarrageSpeed = 16f;
            if (phase2)
            {
                initialBarrageSpeed += 1.8f;
                barrageTelegraphTime -= 5;
                barrageShootRate -= 6;
            }
            if (phase3)
            {
                initialBarrageSpeed += 3f;
                barrageShootRate -= 6;
                barrageTelegraphTime -= 6;
                attackTransitionDelay -= 14;
            }
            if (enraged)
                initialBarrageSpeed += 7.5f;
            if (BossRushEvent.BossRushActive)
                initialBarrageSpeed += 8f;

            ref float hoverOffsetAngle = ref npc.Infernum().ExtraAI[0];
            ref float playerShootDirection = ref npc.Infernum().ExtraAI[1];
            ref float barrageBurstCounter = ref npc.Infernum().ExtraAI[2];
            if (barrageBurstCounter == 0f)
                hoverTime += 64;

            // Hover before firing.
            if (attackTimer < hoverTime)
            {
                Vector2 hoverDestination = target.Center - Vector2.UnitY.RotatedBy(hoverOffsetAngle) * 640f;
                npc.Center = Vector2.Lerp(npc.Center, hoverDestination, 0.025f);

                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 25f;
                npc.SimpleFlyMovement(idealVelocity, 1.9f);
                if (npc.WithinRange(hoverDestination, 100f))
                    npc.velocity *= 0.85f;
            }
            else
                npc.velocity *= 0.9f;

            // Prepare particle line telegraphs.
            if (attackTimer == hoverTime + barrageShootRate - barrageTelegraphTime)
            {
                SoundEngine.PlaySound(SoundID.Item8, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    playerShootDirection = npc.AngleTo(target.Center);
                    for (int i = 0; i < barrageCount; i++)
                    {
                        float offsetAngle = MathHelper.Lerp(-maxShootOffsetAngle, maxShootOffsetAngle, i / (float)(barrageCount - 1f));

                        List<Vector2> telegraphPoints = new();
                        for (int frames = 1; frames < 84; frames += 4)
                        {
                            Vector2 linePosition = TelegraphedCelestialBarrage.SimulateMotion(npc.Center, (offsetAngle + playerShootDirection).ToRotationVector2() * initialBarrageSpeed, playerShootDirection, frames);
                            telegraphPoints.Add(linePosition);
                        }

                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(telegraph =>
                        {
                            telegraph.ModProjectile<EnergyTelegraph>().TelegraphPoints = telegraphPoints.ToArray();
                        });
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<EnergyTelegraph>(), 0, 0f, -1, i / (float)barrageCount);
                    }
                    npc.velocity = Vector2.Zero;
                    npc.netUpdate = true;
                }
            }

            // Shoot.
            if (attackTimer == hoverTime + barrageShootRate)
            {
                SoundEngine.PlaySound(SoundID.Item28, npc.Center);
                for (int i = 0; i < barrageCount; i++)
                {
                    float offsetAngle = MathHelper.Lerp(-maxShootOffsetAngle, maxShootOffsetAngle, i / (float)(barrageCount - 1f));
                    Vector2 shootVelocity = (offsetAngle + playerShootDirection).ToRotationVector2() * initialBarrageSpeed;
                    Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<TelegraphedCelestialBarrage>(), 250, 0f, -1, 0f, playerShootDirection);
                }
            }

            if (attackTimer >= hoverTime + barrageShootRate + attackTransitionDelay)
            {
                attackTimer = 0f;
                hoverOffsetAngle += MathHelper.TwoPi / barrageBurstCount + Main.rand.NextFloatDirection() * 0.36f;
                barrageBurstCounter++;
                if (barrageBurstCounter >= barrageBurstCount)
                    SelectNewAttack(npc);

                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_SlowEnergySpirals(NPC npc, bool phase2, bool phase3, bool enraged, Player target, ref float attackTimer)
        {
            int shootDelay = 96;
            int burstShootRate = 26;
            int laserBurstCount = 12;
            int attackTime = 480;
            float burstShootSpeed = 11f;

            if (phase2)
                burstShootRate -= 4;
            if (phase3)
            {
                burstShootRate -= 4;
                laserBurstCount += 2;
                burstShootSpeed -= 1.6f;
            }
            if (enraged)
            {
                burstShootRate -= 8;
                laserBurstCount += 3;
                burstShootSpeed += 7.5f;
            }
            if (BossRushEvent.BossRushActive)
            {
                laserBurstCount += 2;
                burstShootSpeed += 8.4f;
            }

            // Disable contact damage.
            npc.damage = 0;

            ref float spinOffsetAngle = ref npc.Infernum().ExtraAI[1];

            // Make Ceaseless Void circle the target.
            npc.velocity *= 0.9f;
            npc.Center = npc.Center.MoveTowards(target.Center - Vector2.UnitY.RotatedBy(spinOffsetAngle) * 540f, 30f);
            spinOffsetAngle += MathHelper.ToRadians(1.8f);

            // Release lasers.
            if (attackTimer % burstShootRate == burstShootRate - 1f && attackTimer >= shootDelay && attackTimer < 400f)
            {
                SoundEngine.PlaySound(SoundID.Item28, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float shootOffsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                    for (int i = 0; i < laserBurstCount; i++)
                    {
                        for (int j = -1; j <= 1; j += 2)
                        {
                            Vector2 shootVelocity = (MathHelper.TwoPi * i / laserBurstCount + shootOffsetAngle).ToRotationVector2() * burstShootSpeed;
                            Vector2 laserSpawnPosition = npc.Center + shootVelocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2) * j * 8f;

                            ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(laser =>
                            {
                                laser.localAI[1] = j * 0.5f;
                            });
                            Utilities.NewProjectileBetter(laserSpawnPosition, shootVelocity, ModContent.ProjectileType<SpiralEnergyLaser>(), 250, 0f);
                        }
                    }
                }
            }

            if (attackTimer >= attackTime)
                SelectNewAttack(npc);
        }

        public static void DoBehavior_DarkEnergyBulletHell(NPC npc, Player target, ref float attackTimer)
        {
            int burstFireRate = 17;
            int circleFireRate = 42;
            int energyPerBurst = 12;
            int energyPerCircle = 20;
            int shootTime = 780;
            float burstBaseSpeed = 8.4f;
            if (BossRushEvent.BossRushActive)
                burstBaseSpeed *= 1.2f + npc.Distance(target.Center) * 0.00124f;

            // Slow down and use more DR.
            npc.velocity *= 0.965f;
            npc.Calamity().DR = 0.7f;

            // Make a pulse sound before firing.
            if (attackTimer == 45f)
                SoundEngine.PlaySound(SoundID.DD2_WitherBeastAuraPulse, target.Center);

            if (attackTimer >= shootTime)
                SelectNewAttack(npc);

            // Don't fire near the start/end of the attack.
            if (attackTimer < 90f || attackTimer > shootTime - 90f)
                return;

            // Create bursts.
            if (attackTimer % burstFireRate == burstFireRate - 1f)
            {
                SoundEngine.PlaySound(SoundID.Item103, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float burstAngleOffset = Main.rand.NextFloat(MathHelper.TwoPi);
                    for (int i = 0; i < energyPerBurst; i++)
                    {
                        float burstInterpolant = i / (float)(energyPerBurst - 1f);
                        float burstAngle = burstAngleOffset + burstInterpolant * (i + i * i) / 2f + 32f * i;
                        Vector2 burstVelocity = burstAngle.ToRotationVector2() * burstBaseSpeed * Main.rand.NextFloat(0.7f, 1f);
                        Utilities.NewProjectileBetter(npc.Center, burstVelocity, ModContent.ProjectileType<DarkEnergyBulletHellProj>(), 260, 0f);
                    }
                }
            }

            // Create circles of energy.
            if (attackTimer % circleFireRate == circleFireRate - 1f)
            {
                SoundEngine.PlaySound(SoundID.Item103, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < energyPerCircle; i++)
                    {
                        Vector2 burstVelocity = (MathHelper.TwoPi * i / energyPerCircle).ToRotationVector2() * burstBaseSpeed * 1.1f;
                        Utilities.NewProjectileBetter(npc.Center, burstVelocity, ModContent.ProjectileType<DarkEnergyBulletHellProj>(), 260, 0f);
                    }
                }
            }
        }

        public static void DoBehavior_BlackHoleSuck(NPC npc, Player target, ref float attackTimer)
        {
            npc.life = 1;
            npc.dontTakeDamage = true;
            npc.Calamity().ShouldCloseHPBar = true;

            int blackHoleDamage = 750;
            int soundDuration = 254;
            ref float moveTowardsTarget = ref npc.Infernum().ExtraAI[0];
            ref float hasCreatedBlackHole = ref npc.Infernum().ExtraAI[1];

            // Create the black hole on the first frame.
            if (hasCreatedBlackHole == 0f)
            {
                Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<TelegraphedCelestialBarrage>(), ModContent.ProjectileType<SpiralEnergyLaser>(), ModContent.ProjectileType<CelestialBarrage>());

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<AllConsumingBlackHole>(), blackHoleDamage, 0f);

                // Give a tip.
                HatGirl.SayThingWhileOwnerIsAlive(target, "This is the Void's last stand! Try not to get sucked in, and weave through the energy bolts!");

                hasCreatedBlackHole = 1f;
            }

            // Disable damage.
            npc.dontTakeDamage = true;

            // Redirect quickly towards the target if necessary.
            if (moveTowardsTarget == 1f)
            {
                if (npc.WithinRange(target.Center, 480f))
                {
                    npc.velocity *= 0.8f;
                    npc.damage = 0;
                    if (npc.velocity.Length() < 4f)
                    {
                        npc.velocity = Vector2.Zero;
                        moveTowardsTarget = 0f;
                        npc.netUpdate = true;
                    }
                    return;
                }

                CalamityUtils.SmoothMovement(npc, 0f, target.Center - Vector2.UnitY * 360f - npc.Center, 40f, 0.75f, true);
                return;
            }

            // Make Ceaseless Void move quickly towards the target if they go too far away.
            if (!npc.WithinRange(target.Center, 1320f))
            {
                moveTowardsTarget = 1f;
                npc.netUpdate = true;
                return;
            }

            // Slow down.
            npc.velocity *= 0.9f;

            // Play the death buildup sound.
            if (attackTimer == 560f - soundDuration)
                SoundEngine.PlaySound(CeaselessVoidBoss.BuildupSound with { Volume = 1.8f });

            if (attackTimer >= 560f)
            {
                SoundEngine.PlaySound(CeaselessVoidBoss.DeathSound with { Volume = 2f }, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(explosion =>
                    {
                        explosion.ModProjectile<CosmicExplosion>().MaxRadius = 1250f;
                    });
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<CosmicExplosion>(), 0, 0f);
                }

                npc.life = 1;
                npc.StrikeNPCNoInteraction(9999, 0f, 0, true);
                npc.HitEffect();
                npc.NPCLoot();
            }
        }

        public static void SelectNewAttack(NPC npc)
        {
            // Select a new target.
            npc.TargetClosest();

            List<CeaselessVoidAttackType> possibleAttacks = new()
            {
                CeaselessVoidAttackType.RealityRendCharge,
                CeaselessVoidAttackType.ConvergingEnergyBarrages,
                CeaselessVoidAttackType.SlowEnergySpirals
            };

            if (possibleAttacks.Count >= 2)
                possibleAttacks.Remove((CeaselessVoidAttackType)(int)npc.ai[0]);

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            npc.ai[0] = (int)Main.rand.Next(possibleAttacks);
            npc.ai[0] = (int)CeaselessVoidAttackType.DiagonalMirrorBolts;
            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }
        #endregion AI

        #region Drawing
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            // Draw chains.
            if (Chains is not null)
            {
                foreach (var chain in Chains)
                    DrawChain(chain);
            }

            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Texture2D glowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/CeaselessVoid/CeaselessVoidGlow").Value;
            Texture2D voidTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/CeaselessVoid/CeaselessVoidVoidStuff").Value;
            Main.spriteBatch.Draw(texture, npc.Center - Main.screenPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, 0, 0f);
            Main.spriteBatch.Draw(glowmask, npc.Center - Main.screenPosition, npc.frame, npc.GetAlpha(Color.White), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, 0, 0f);

            Main.spriteBatch.EnterShaderRegion();

            DrawData drawData = new(voidTexture, npc.Center - Main.screenPosition, npc.frame, npc.GetAlpha(Color.White), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, 0, 0);
            InfernumEffectsRegistry.RealityTear2Shader.SetShaderTexture(InfernumTextureRegistry.Stars);
            InfernumEffectsRegistry.RealityTear2Shader.Apply(drawData);
            drawData.Draw(Main.spriteBatch);
            Main.spriteBatch.ExitShaderRegion();

            if (npc.ai[0] == (int)CeaselessVoidAttackType.ChainedUp)
                DrawSeal(npc);
            return false;
        }

        public static void DrawSeal(NPC npc)
        {
            float scale = MathHelper.Lerp(0.15f, 0.16f, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.5f) * 0.5f + 0.5f) * 1.4f;
            float noiseScale = MathHelper.Lerp(0.4f, 0.8f, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.3f) * 0.5f + 0.5f);

            Effect shieldEffect = Filters.Scene["CalamityMod:RoverDriveShield"].GetShader().Shader;
            shieldEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.15f);
            shieldEffect.Parameters["blowUpPower"].SetValue(2.5f);
            shieldEffect.Parameters["blowUpSize"].SetValue(0.5f);
            shieldEffect.Parameters["noiseScale"].SetValue(noiseScale);

            // Prepare the forcefield opacity.
            float baseShieldOpacity = 0.9f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2f);
            shieldEffect.Parameters["shieldOpacity"].SetValue(baseShieldOpacity * (npc.Opacity * 0.9f + 0.1f) * 0.8f);
            shieldEffect.Parameters["shieldEdgeBlendStrenght"].SetValue(4f);

            Color edgeColor = Color.Lerp(Color.Purple, Color.Black, 0.65f);
            Color shieldColor = Color.DarkBlue;

            // Prepare the forcefield colors.
            shieldEffect.Parameters["shieldColor"].SetValue(shieldColor.ToVector3());
            shieldEffect.Parameters["shieldEdgeColor"].SetValue(edgeColor.ToVector3());

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, shieldEffect, Main.GameViewMatrix.TransformationMatrix);

            // Draw the forcefield. This doesn't happen if the lighting behind the vassal is too low, to ensure that it doesn't draw if underground or in a darkly lit area.
            Texture2D noise = InfernumTextureRegistry.WavyNoise.Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            if (shieldColor.ToVector4().Length() > 0.02f)
                Main.spriteBatch.Draw(noise, drawPosition, null, Color.White * npc.Opacity, 0, noise.Size() / 2f, scale * 2f, 0, 0);

            Main.spriteBatch.ExitShaderRegion();
        }

        public static void DrawChain(List<VerletSimulatedSegment> chain)
        {
            Texture2D chainTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/CeaselessVoid/CeaselessVoidChain").Value;

            // Collect chain draw positions.
            Vector2[] bezierPoints = chain.Select(x => x.position).ToArray();
            BezierCurve bezierCurve = new(bezierPoints);

            float chainScale = 0.8f;
            int totalChains = (int)(Vector2.Distance(chain.First().position, chain.Last().position) / chainTexture.Height / chainScale);
            totalChains = (int)MathHelper.Clamp(totalChains, 30f, 1200f);
            for (int i = 0; i < totalChains - 1; i++)
            {
                Vector2 drawPosition = bezierCurve.Evaluate(i / (float)totalChains);
                float completionRatio = i / (float)totalChains + 1f / totalChains;
                float angle = (bezierCurve.Evaluate(completionRatio) - drawPosition).ToRotation() - MathHelper.PiOver2;

                float brightnessInterpolant1 = MathF.Pow(MathF.Sin(12f * MathHelper.Pi * completionRatio - Main.GlobalTimeWrappedHourly * 9f) * 0.5f + 0.5f, 4f);
                float brightnessInterpolant2 = MathF.Pow(MathF.Sin(15f * MathHelper.Pi * completionRatio - Main.GlobalTimeWrappedHourly * 13.5f) * 0.5f + 0.5f, 4.4f);
                Color baseChainColor = Lighting.GetColor((int)drawPosition.X / 16, (int)drawPosition.Y / 16) * 2f;
                Main.EntitySpriteDraw(chainTexture, drawPosition - Main.screenPosition, null, baseChainColor, angle, chainTexture.Size() * 0.5f, chainScale, SpriteEffects.None, 0);
            }
        }
        #endregion Drawing

        #region Death Effects
        public override bool CheckDead(NPC npc)
        {
            // Just die as usual if the Ceaseless Void is killed during the death animation. This is done so that Cheat Sheet and other butcher effects can kill it quickly.
            if (npc.Infernum().ExtraAI[6] >= 1f)
                return true;

            npc.active = true;
            npc.dontTakeDamage = true;
            npc.Infernum().ExtraAI[6] = 1f;
            npc.life = 1;

            SelectNewAttack(npc);
            npc.ai[0] = (int)CeaselessVoidAttackType.BlackHoleSuck;

            npc.netUpdate = true;
            return false;
        }
        #endregion Death Effects

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Try not to move too much at the start of the battle. Finding a good spot and staying near it helps a lot!";
            yield return n => "Most of the Void's attacks require fast maneuvering to evade. Be sure to pay attention to any projectiles on-screen!";
        }
        #endregion Tips
    }
}
