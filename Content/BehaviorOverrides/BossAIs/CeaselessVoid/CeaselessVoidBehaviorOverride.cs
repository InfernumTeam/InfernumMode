using System;
using System.Collections.Generic;
using System.Linq;
using CalamityMod;
using CalamityMod.DataStructures;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.CeaselessVoid;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Boss;
using CalamityMod.UI;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Common.Graphics.ScreenEffects;
using InfernumMode.Content.Projectiles.Pets;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
            // Phase 1 startup.
            ChainedUp,
            DarkEnergySwirl,

            // Phase 1 attacks.
            RedirectingAcceleratingDarkEnergy,
            DiagonalMirrorBolts,
            CircularVortexSpawn,
            SpinningDarkEnergy,
            AreaDenialVortexTears,

            // Phase 2 transition.
            ShellCrackTransition,
            DarkEnergyTorrent,

            // Phase 2 attacks.
            EnergySuck,

            // Phase 3 transition.
            ChainBreakTransition,

            // Phase 3 attacks.
            JevilDarkEnergyBursts,
            MirroredCharges,
            ConvergingEnergyBarrages,

            // Death animation attack.
            DeathAnimation
        }
        #endregion

        #region Set Defaults

        public static List<List<VerletSimulatedSegment>> Chains
        {
            get;
            internal set;
        }

        public static CeaselessVoidAttackType[] Phase1AttackCycle =>
        [
            CeaselessVoidAttackType.RedirectingAcceleratingDarkEnergy,
            CeaselessVoidAttackType.DiagonalMirrorBolts,
            CeaselessVoidAttackType.CircularVortexSpawn,
            CeaselessVoidAttackType.SpinningDarkEnergy,
            CeaselessVoidAttackType.AreaDenialVortexTears,
            CeaselessVoidAttackType.DiagonalMirrorBolts,
            CeaselessVoidAttackType.CircularVortexSpawn,
            CeaselessVoidAttackType.SpinningDarkEnergy
        ];

        public static CeaselessVoidAttackType[] Phase2AttackCycle =>
        [
            CeaselessVoidAttackType.RedirectingAcceleratingDarkEnergy,
            CeaselessVoidAttackType.DiagonalMirrorBolts,
            CeaselessVoidAttackType.EnergySuck,
            CeaselessVoidAttackType.CircularVortexSpawn,
            CeaselessVoidAttackType.SpinningDarkEnergy,
            CeaselessVoidAttackType.EnergySuck,
            CeaselessVoidAttackType.AreaDenialVortexTears,
            CeaselessVoidAttackType.DiagonalMirrorBolts,
            CeaselessVoidAttackType.EnergySuck,
            CeaselessVoidAttackType.CircularVortexSpawn,
            CeaselessVoidAttackType.SpinningDarkEnergy,
            CeaselessVoidAttackType.EnergySuck
        ];

        public static CeaselessVoidAttackType[] Phase3AttackCycle =>
        [
            CeaselessVoidAttackType.JevilDarkEnergyBursts,
            CeaselessVoidAttackType.ConvergingEnergyBarrages,
            CeaselessVoidAttackType.MirroredCharges,
            CeaselessVoidAttackType.ConvergingEnergyBarrages
        ];

        public static readonly Color InfiniteFlightTextColor = Color.Lerp(Color.LightPink, Color.Black, 0.35f);

        public override float[] PhaseLifeRatioThresholds =>
        [
            Phase2LifeRatio,
            Phase3LifeRatio
        ];

        public const int PhaseCycleIndexIndex = 5;

        public override void SetDefaults(NPC npc)
        {
            npc.npcSlots = 36f;
            npc.width = 100;
            npc.height = 100;
            npc.defense = 0;
            npc.lifeMax = 636000;
            npc.value = Item.buyPrice(0, 35, 0, 0);

            if (ModLoader.TryGetMod("CalamityModMusic", out Mod calamityModMusic))
                npc.ModNPC.Music = MusicLoader.GetMusicSlot(calamityModMusic, "Sounds/Music/CeaselessVoid");
            else
                npc.ModNPC.Music = MusicID.Boss3;

            npc.lifeMax /= 2;
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

        public const float Phase2LifeRatio = 0.66667f;

        public const float Phase3LifeRatio = 0.15f;

        public const float DarkEnergyOffsetRadius = 1200f;

        public static int DarkEnergyDamage => 275;

        public static int RubbleDamage => 275;

        public static int OtherworldlyBoltDamage => 275;

        public static int EnergyPulseDamage => 275;

        public static int VortexTearDamage => 300;

        public static int DarkEnergyTorrentDamage => 350;

        public override bool PreAI(NPC npc)
        {
            // Reset DR.
            npc.Calamity().DR = 0.4f;

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

            // Prevent natural despawning.
            npc.timeLeft = 3600;
            npc.chaseable = true;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool phase2 = lifeRatio < Phase2LifeRatio;
            bool phase3 = lifeRatio < Phase3LifeRatio;
            bool enraged = target.Center.Y < Main.worldSurface * 16f && !BossRushEvent.BossRushActive;
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float currentPhase = ref npc.ai[2];
            ref float voidIsCracked = ref npc.localAI[0];
            ref float teleportEffectInterpolant = ref npc.localAI[1];
            ref float phaseCycleIndex = ref npc.Infernum().ExtraAI[PhaseCycleIndexIndex];

            // Do phase transitions.
            if (currentPhase == 0f && phase2)
            {
                currentPhase = 1f;
                SelectNewAttack(npc);
                ClearEntities();
                phaseCycleIndex = 0f;
                attackType = (int)CeaselessVoidAttackType.ShellCrackTransition;
            }
            if (currentPhase == 1f && phase3)
            {
                currentPhase = 2f;
                SelectNewAttack(npc);
                ClearEntities();
                phaseCycleIndex = 0f;
                attackType = (int)CeaselessVoidAttackType.ChainBreakTransition;
            }

            // This debuff is not fun.
            if (target.HasBuff(BuffID.VortexDebuff))
                target.ClearBuff(BuffID.VortexDebuff);

            // Reset things every frame. They may be adjusted in the AI methods as necessary.
            npc.damage = 0;
            npc.dontTakeDamage = enraged;
            npc.Calamity().CurrentlyEnraged = npc.dontTakeDamage;

            // Lock the camera onto the Ceaseless Void because it's very egotistical and cannot bear the thought of not being the center of attention.
            if (Main.LocalPlayer.WithinRange(npc.Center, 2200f) && attackType != (int)CeaselessVoidAttackType.ChainedUp && !phase3)
            {
                float lookAtTargetInterpolant = Utils.GetLerpValue(420f, 2700f, ((target.Center - npc.Center) * new Vector2(1f, 1.8f)).Length(), true);
                Main.LocalPlayer.Infernum_Camera().ScreenFocusInterpolant = 1f;
                Main.LocalPlayer.Infernum_Camera().ScreenFocusPosition = Vector2.Lerp(npc.Center, Main.LocalPlayer.Center, lookAtTargetInterpolant);
            }

            if (!phase3 && Chains is not null && npc.ai[3] == 0f && !BossRushEvent.BossRushActive)
                npc.Center = Chains[0][0].position;

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
                    DoBehavior_RedirectingAcceleratingDarkEnergy(npc, target, phase2, ref attackTimer);
                    break;
                case CeaselessVoidAttackType.DiagonalMirrorBolts:
                    DoBehavior_DiagonalMirrorBolts(npc, target, phase2, ref attackTimer);
                    break;
                case CeaselessVoidAttackType.CircularVortexSpawn:
                    DoBehavior_CircularVortexSpawn(npc, target, phase2, ref attackTimer);
                    break;
                case CeaselessVoidAttackType.SpinningDarkEnergy:
                    DoBehavior_SpinningDarkEnergy(npc, target, phase2, ref attackTimer);
                    break;
                case CeaselessVoidAttackType.AreaDenialVortexTears:
                    DoBehavior_AreaDenialVortexTears(npc, target, phase2, ref attackTimer);
                    break;

                case CeaselessVoidAttackType.ShellCrackTransition:
                    DoBehavior_ShellCrack(npc, target, ref attackTimer, ref voidIsCracked);
                    break;
                case CeaselessVoidAttackType.DarkEnergyTorrent:
                    DoBehavior_DarkEnergyTorrent(npc, target, ref attackTimer);
                    break;

                case CeaselessVoidAttackType.EnergySuck:
                    DoBehavior_EnergySuck(npc, target, ref attackTimer);
                    break;

                case CeaselessVoidAttackType.ChainBreakTransition:
                    DoBehavior_ChainBreakTransition(npc, target, ref attackTimer);
                    break;

                case CeaselessVoidAttackType.JevilDarkEnergyBursts:
                    DoBehavior_JevilDarkEnergyBursts(npc, target, ref attackTimer, ref teleportEffectInterpolant);
                    break;
                case CeaselessVoidAttackType.MirroredCharges:
                    DoBehavior_MirroredCharges(npc, target, ref attackTimer, ref teleportEffectInterpolant);
                    break;
                case CeaselessVoidAttackType.ConvergingEnergyBarrages:
                    DoBehavior_ConvergingEnergyBarrages(npc, target, ref attackTimer);
                    break;
                case CeaselessVoidAttackType.DeathAnimation:
                    DoBehavior_DeathAnimation(npc, target, ref attackTimer);
                    break;
            }

            // Update chains.
            if (Chains is not null)
                UpdateChains(npc);

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

                for (int j = 0; j < Main.maxProjectiles; j++)
                {
                    Projectile proj = Main.projectile[j];

                    if (!proj.active || proj.hostile)
                        continue;

                    MoveChainBasedOnEntity(Chains[i], proj, npc);
                }

                Vector2 chainStart = Chains[i][0].position;
                Vector2 chainEnd = Chains[i].Last().position;
                float segmentDistance = Vector2.Distance(chainStart, chainEnd) / Chains[i].Count;
                Chains[i] = VerletSimulatedSegment.SimpleSimulation(Chains[i], segmentDistance, 10, 0.6f);
            }
        }

        public static void DestroyChains(NPC npc)
        {
            if (Main.netMode == NetmodeID.Server || Chains is null)
                return;

            // Create impact effects.
            SoundEngine.PlaySound(CeaselessVoidBoss.DeathSound);
            Main.LocalPlayer.Infernum_Camera().ScreenFocusInterpolant = 16f;
            ScreenEffectSystem.SetBlurEffect(npc.Center, 0.5f, 45);

            foreach (var chain in Chains)
            {
                if (chain is null)
                    continue;

                Vector2[] bezierPoints = chain.Select(x => x.position).ToArray();
                BezierCurve bezierCurve = new(bezierPoints);

                int totalChains = (int)(Vector2.Distance(chain.First().position, chain.Last().position) / 22.4f);
                totalChains = (int)Clamp(totalChains, 30f, 1200f);

                // Generate gores.
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

        public static void TeleportToPosition(NPC npc, Vector2 teleportPosition)
        {
            // Teleport to the position.
            npc.Center = teleportPosition;
            npc.netUpdate = true;

            // Play the teleport sound.
            SoundEngine.PlaySound(InfernumSoundRegistry.CeaselessVoidTeleportSound with { Volume = 0.6f, Pitch = -0.25f }, npc.Center);

            // Create a puff of dark energy at the teleport position.
            for (int i = 0; i < 32; i++)
            {
                Color darkEnergyColor = Main.rand.NextBool() ? Color.HotPink : Color.Purple;
                CloudParticle darkEnergyCloud = new(npc.Center, (TwoPi * i / 32f).ToRotationVector2() * 8f, darkEnergyColor, Color.DarkBlue * 0.75f, 25, Main.rand.NextFloat(2.5f, 3.2f));
                GeneralParticleHandler.SpawnParticle(darkEnergyCloud);
            }
        }

        public static void CreateEnergySuckParticles(NPC npc, Vector2 generalOffset, float minOffset = 240f, float maxOffset = 630f, float scale = 0.8f)
        {
            int lightLifetime = Main.rand.Next(20, 24);
            float squishFactor = 2f;
            Vector2 lightSpawnPosition = npc.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(minOffset, maxOffset) + generalOffset;
            Vector2 lightVelocity = (npc.Center - lightSpawnPosition) / lightLifetime * 1.1f;
            Color lightColor = Color.Lerp(Color.MediumPurple, Color.DarkBlue, Main.rand.NextFloat(0.5f));
            if (Main.rand.NextBool())
                lightColor = Color.Lerp(Color.Purple, Color.Black, 0.6f);

            SquishyLightParticle light = new(lightSpawnPosition, lightVelocity, scale, lightColor, lightLifetime, 1f, squishFactor, squishFactor * 4f);
            GeneralParticleHandler.SpawnParticle(light);
        }

        public static void MoveChainBasedOnEntity(List<VerletSimulatedSegment> chain, Entity e, NPC npc)
        {
            // Cap the velocity to ensure it doesn't make the chains go flying.
            Vector2 entityVelocity = (e.velocity * 0.425f).ClampMagnitude(0f, 5f);

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
                    float distanceToChains = e.Distance(segment.position);
                    float currentMovementOffsetInterpolant = Utils.GetLerpValue(distanceToChains, distanceBetweenSegments, distanceBetweenSegments * 0.2f, true);
                    float nextMovementOffsetInterpolant = 1f - currentMovementOffsetInterpolant;

                    // Move the segments based on the weight values.
                    segment.position += entityVelocity * currentMovementOffsetInterpolant;
                    if (!next.locked)
                        next.position += entityVelocity * nextMovementOffsetInterpolant;

                    // Play some cool chain sounds.
                    if (npc.soundDelay <= 0 && entityVelocity.Length() >= 0.1f)
                    {
                        SoundEngine.PlaySound(InfernumSoundRegistry.CeaselessVoidChainSound with { Volume = 0.25f, PitchVariance = 0.05f }, e.Center);
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
                Chains = [];

                int segmentCount = 21;
                for (int i = 0; i < 4; i++)
                {
                    Chains.Add([]);

                    // Determine how far off the chains should go.
                    Vector2 checkDirection = (TwoPi * i / 4f + PiOver4).ToRotationVector2() * new Vector2(1f, 1.2f);
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

            // Disable damage.
            npc.damage = 0;
            npc.dontTakeDamage = true;

            // Prevent hovering over the Void's name to reveal what it is.
            npc.ShowNameOnHover = false;

            // Disable boss behaviors.
            npc.boss = false;
            npc.Calamity().ShouldCloseHPBar = true;
            npc.Calamity().ProvidesProximityRage = false;
            BossHealthBarManager.Bars.RemoveAll(b => b.NPCIndex == npc.whoAmI);

            if (BossRushEvent.BossRushActive)
            {
                SelectNewAttack(npc);
                npc.ai[0] = (int)CeaselessVoidAttackType.DarkEnergySwirl;
                npc.Center = WorldSaveSystem.ForbiddenArchiveCenter.ToWorldCoordinates() + Vector2.UnitY * 1332f;
            }
        }

        public static void DoBehavior_DarkEnergySwirl(NPC npc, bool phase2, bool phase3, Player target, ref float attackTimer)
        {
            int totalRings = 5;
            int energyCountPerRing = 11;
            int darkEnergyID = ModContent.NPCType<DarkEnergy>();
            float energyOrbAcceleration = 1.017f;

            if (phase2)
                energyCountPerRing += 2;
            if (phase3)
            {
                energyCountPerRing++;
                totalRings++;
            }

            ref float hasCreatedDarkEnergy = ref npc.Infernum().ExtraAI[0];
            ref float darkEnergyShootTimer = ref npc.Infernum().ExtraAI[1];

            // Make the screen black to distract the player from the fact that some wacky things are going on in the background.
            if (attackTimer <= 5f)
                InfernumMode.BlackFade = 1f;

            // Give a tip.
            if (attackTimer == 10f)
                HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.CVEnergyBurstTip3");

            // Grant the targets infinite flight time during the portal tear charge up attack, so that they don't run out and take an unfair hit.
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.dead || !player.active || !npc.WithinRange(player.Center, 10000f))
                    continue;

                player.DoInfiniteFlightCheck(InfiniteFlightTextColor);
            }

            // Initialize by creating the dark energy ring.
            if (Main.netMode != NetmodeID.MultiplayerClient && hasCreatedDarkEnergy == 0f)
            {
                for (int i = 0; i < totalRings; i++)
                {
                    float spinMovementSpeed = Lerp(7f, 1f, i / (float)(totalRings - 1f));
                    float offsetRadius = Lerp(180f, DarkEnergyOffsetRadius, LumUtils.Convert01To010(i / (float)(energyCountPerRing - 1f)));
                    for (int j = 0; j < energyCountPerRing; j++)
                    {
                        float offsetAngle = TwoPi * j / energyCountPerRing;
                        NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, darkEnergyID, npc.whoAmI, offsetAngle, spinMovementSpeed, offsetRadius);
                    }
                }
                hasCreatedDarkEnergy = 1f;
                npc.netUpdate = true;
            }

            // Disable damage.
            npc.dontTakeDamage = true;

            // Calculate the life ratio of all dark energy combined.
            // If it is sufficiently low then all remaining dark energy fades away and CV goes to the next attack.
            int darkEnergyTotalLife = 0;
            int darkEnergyTotalMaxLife = 0;
            List<NPC> darkEnergies = [];
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

            // Shoot accelerating dark energy.
            darkEnergyShootTimer++;
            int darkEnergyShootRate = (int)Lerp(45f, 27f, 1f - darkEnergyLifeRatio);
            if (darkEnergyShootTimer >= darkEnergyShootRate && attackTimer >= 180f)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.CeaselessVoidSwirlSound, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 energyShootVelocity = npc.SafeDirectionTo(target.Center + target.velocity * 15f).RotatedBy(TwoPi * i / 4f) * 14.75f;
                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(darkEnergy =>
                        {
                            darkEnergy.ModProjectile<AcceleratingDarkEnergy>().Time = -8f;
                        });
                        Utilities.NewProjectileBetter(npc.Center, energyShootVelocity, ModContent.ProjectileType<AcceleratingDarkEnergy>(), DarkEnergyDamage, 0f, -1, (int)AcceleratingDarkEnergy.DarkEnergyAttackState.AccelerateTowardsTarget, energyOrbAcceleration);
                    }
                    darkEnergyShootTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            if (darkEnergyLifeRatio <= 0.5f)
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

        public static void DoBehavior_RedirectingAcceleratingDarkEnergy(NPC npc, Player target, bool phase2, ref float attackTimer)
        {
            int accelerateDelay = 102;
            int accelerationTime = 33;
            int acceleratingEnergyID = ModContent.ProjectileType<AcceleratingDarkEnergy>();
            float startingEnergySpeed = 8f;
            float idealEndingSpeed = 33f;

            if (phase2)
            {
                startingEnergySpeed += 4f;
                idealEndingSpeed += 4f;
            }

            // Release energy balls from above.
            if (attackTimer == 1f)
            {
                // Give a tip.
                HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.CVEnergyBurstTip2");

                SoundEngine.PlaySound(SoundID.Item103, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 9; i++)
                    {
                        Vector2 baseSpawnOffset = (TwoPi * i / 9f).ToRotationVector2() * new Vector2(1f, 1.2f) * 560f;
                        for (int j = 0; j < 4; j++)
                        {
                            Vector2 microSpawnOffset = (TwoPi * j / 4f).ToRotationVector2() * 40f;
                            Vector2 energyRestingPosition = Vector2.Lerp(npc.Center, target.Center, 0.3f) + baseSpawnOffset;

                            ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(darkEnergy =>
                            {
                                darkEnergy.ModProjectile<AcceleratingDarkEnergy>().RestingPosition = energyRestingPosition + microSpawnOffset;
                                darkEnergy.ModProjectile<AcceleratingDarkEnergy>().CenterPoint = energyRestingPosition;
                                darkEnergy.ModProjectile<AcceleratingDarkEnergy>().Index = i * 4 + j;
                            });
                            Utilities.NewProjectileBetter(energyRestingPosition - Vector2.UnitY * 1000f, Vector2.Zero, acceleratingEnergyID, DarkEnergyDamage, 0f);
                        }
                    }
                }
            }

            // Make energy balls accelerate.
            if (attackTimer >= accelerateDelay)
            {
                if (attackTimer == accelerateDelay)
                    SoundEngine.PlaySound(InfernumSoundRegistry.CeaselessVoidSwirlSound, target.Center);

                int indexToFire = (int)attackTimer - accelerateDelay;
                foreach (Projectile energy in Utilities.AllProjectilesByID(acceleratingEnergyID).Where(e => e.ModProjectile<AcceleratingDarkEnergy>().Index == indexToFire && e.ai[0] == 0f))
                {
                    energy.ModProjectile<AcceleratingDarkEnergy>().Time = 0f;
                    energy.ModProjectile<AcceleratingDarkEnergy>().Acceleration = Utilities.AccelerationToReachSpeed(startingEnergySpeed, idealEndingSpeed, accelerationTime);
                    energy.ModProjectile<AcceleratingDarkEnergy>().AttackState = AcceleratingDarkEnergy.DarkEnergyAttackState.AccelerateTowardsTarget;
                    energy.velocity = energy.SafeDirectionTo(target.Center) * startingEnergySpeed;
                    energy.netUpdate = true;
                }
            }

            if (attackTimer >= 164f)
                SelectNewAttack(npc);
        }

        public static void DoBehavior_DiagonalMirrorBolts(NPC npc, Player target, bool phase2, ref float attackTimer)
        {
            int energySuckTime = 0;
            int energyBoltReleaseRate = 1;
            int energyBoltReleaseCount = 54;
            int energyBoltShootTime = energyBoltReleaseRate * energyBoltReleaseCount;
            int energyDiagonalShootDelay = energySuckTime + energyBoltShootTime + OtherworldlyBolt.LockIntoPositionTime + OtherworldlyBolt.DisappearIntoBackgroundTime;
            int energyDiagonalBootShootRate = 3;
            bool doneShooting = attackTimer >= energyDiagonalShootDelay + energyBoltReleaseCount * energyDiagonalBootShootRate;
            float energyShootSpeed = phase2 ? 14.5f : 9f;
            float energyBoltArc = ToRadians(300f);

            // Play funny sounds.
            if (attackTimer == energySuckTime + 1f)
                SoundEngine.PlaySound(SoundID.Item164 with { Pitch = -0.7f }, target.Center);
            if (attackTimer == energyDiagonalShootDelay + 1f)
                SoundEngine.PlaySound(SoundID.Item163 with { Pitch = -0.7f }, target.Center);

            // Release energy bolts that fly outward.
            if (attackTimer >= energySuckTime && attackTimer <= energySuckTime + energyBoltShootTime && attackTimer % energyBoltReleaseRate == 0f)
            {
                float energyBoltShootInterpolant = Utils.GetLerpValue(energySuckTime, energySuckTime + energyBoltShootTime, attackTimer, true);
                float energyBoltShootOffsetAngle = Lerp(0.5f * energyBoltArc, -0.5f * energyBoltArc, energyBoltShootInterpolant);
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
                for (int i = 0; i < 2; i++)
                {
                    Vector2 energyBoltSpawnPosition = target.Center + 1250f * OtherworldlyBolt.AimDirection + new Vector2(1960f * Main.rand.NextFloatDirection(), -400f);
                    Utilities.NewProjectileBetter(energyBoltSpawnPosition, OtherworldlyBolt.AimDirection * -energyShootSpeed, ModContent.ProjectileType<OtherworldlyBolt>(), OtherworldlyBoltDamage, 0f, -1, (int)OtherworldlyBolt.OtherwordlyBoltAttackState.AccelerateFromBelow);
                }
            }

            if (doneShooting)
                SelectNewAttack(npc);
        }

        public static void DoBehavior_CircularVortexSpawn(NPC npc, Player target, bool phase2, ref float attackTimer)
        {
            int vortexCount = 35;
            int chargeUpDelay = 150;
            int chargeUpTime = 90;
            int burstWaitTime = 132;
            int energyBoltCountMainRing = 39;

            if (phase2)
            {
                chargeUpDelay -= 15;
                burstWaitTime -= 36;
            }

            bool playShootSound = npc.Infernum().ExtraAI[0] == 1f;
            ref float ringBulletCount = ref npc.Infernum().ExtraAI[1];
            ref float ringBulletAngularOffset = ref npc.Infernum().ExtraAI[2];

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 1f)
            {
                float spinOffsetAngle = Main.rand.NextFloat(TwoPi);
                for (int i = 0; i < vortexCount; i++)
                {
                    Vector2 vortexSpawnPosition = npc.Center + (TwoPi * i / vortexCount + spinOffsetAngle).ToRotationVector2() * 1350f;
                    Vector2 aimDestination = npc.Center + (TwoPi * i / vortexCount + spinOffsetAngle + PiOver2).ToRotationVector2() * 136f;
                    Vector2 aimDirection = (aimDestination - vortexSpawnPosition).SafeNormalize(Vector2.UnitY);
                    Utilities.NewProjectileBetter(vortexSpawnPosition, aimDirection, ModContent.ProjectileType<CeaselessVortex>(), 0, 0f);
                }

                ringBulletCount = energyBoltCountMainRing;
                npc.netUpdate = true;
            }

            // Grant the target infinite flight time during the portal tear charge up attack, so that they don't run out and take an unfair hit.
            if (attackTimer <= chargeUpDelay)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player player = Main.player[i];
                    if (player.dead || !player.active || !npc.WithinRange(player.Center, 10000f))
                        continue;

                    player.DoInfiniteFlightCheck(InfiniteFlightTextColor);
                }
            }

            // Play a shoot sound if ready.
            if (playShootSound)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.CeaselessVoidStrikeSound, target.Center);
                npc.Infernum().ExtraAI[0] = 0f;

                ScreenEffectSystem.SetBlurEffect(npc.Center, 0.4f, 10);
                target.Infernum_Camera().CurrentScreenShakePower = 8f;
            }

            // Create convergence particles.
            if (attackTimer >= chargeUpDelay && attackTimer <= chargeUpDelay + chargeUpTime)
            {
                CreateEnergySuckParticles(npc, Vector2.Zero);

                // Create pulse rungs and bloom periodically.
                if (attackTimer % 15f == 0f)
                {
                    Color energyColor = Color.Lerp(Color.MediumPurple, Color.DarkBlue, Main.rand.NextFloat(0.5f));
                    PulseRing ring = new(npc.Center, Vector2.Zero, energyColor, 3.6f, 0f, 60);
                    GeneralParticleHandler.SpawnParticle(ring);

                    StrongBloom bloom = new(npc.Center, Vector2.Zero, energyColor, 1f, 15);
                    GeneralParticleHandler.SpawnParticle(bloom);
                }

                // Create energy sparks at the center of Ceaseless Void.
                CritSpark spark = new(npc.Center, Main.rand.NextVector2Circular(8f, 8f), Color.LightCyan, Color.Cyan, 5f, 6, 0.01f, 7.5f);
                GeneralParticleHandler.SpawnParticle(spark);
            }

            // Play a convergence sound.
            if (attackTimer == chargeUpDelay)
                SoundEngine.PlaySound(InfernumSoundRegistry.EntropyRayChargeSound with { Pitch = 0.3f }, target.Center);

            // Release accelerating bolts outward.
            if (attackTimer == chargeUpDelay + chargeUpTime)
            {
                // Create impact effects.
                ScreenEffectSystem.SetBlurEffect(npc.Center, 0.4f, 24);
                target.Infernum_Camera().CurrentScreenShakePower = 12f;
                Utilities.CreateShockwave(npc.Center, 2, 8, 75f, true);
                SoundEngine.PlaySound(InfernumSoundRegistry.CeaselessVoidStrikeSound with { Pitch = 0.4f }, target.Center);
            }

            if (attackTimer >= chargeUpDelay + chargeUpTime && attackTimer % 9f == 0f && ringBulletCount >= 9f)
            {
                // Create bloom and pulse rings while firing.
                PulseRing ring = new(npc.Center, Vector2.Zero, Color.MediumPurple * 0.5f, 0f, 16f, 20);
                GeneralParticleHandler.SpawnParticle(ring);

                StrongBloom bloom = new(npc.Center, Vector2.Zero, Color.Lerp(Color.Purple, Color.DarkBlue, 0.6f), 4f, 15);
                GeneralParticleHandler.SpawnParticle(bloom);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 energyBoltSpawnPosition = npc.Center;
                    for (int i = 0; i < ringBulletCount; i++)
                    {
                        Vector2 energyBoltVelocity = (TwoPi * i / ringBulletCount + ringBulletAngularOffset).ToRotationVector2() * 0.02f;
                        Utilities.NewProjectileBetter(energyBoltSpawnPosition, energyBoltVelocity, ModContent.ProjectileType<OtherworldlyBolt>(), OtherworldlyBoltDamage, 0f, -1, (int)OtherworldlyBolt.OtherwordlyBoltAttackState.AccelerateFromBelow);
                    }

                    ringBulletAngularOffset += ToRadians(11f);
                    ringBulletCount -= 6f;
                }
            }

            if (attackTimer >= chargeUpDelay + chargeUpTime + burstWaitTime)
                SelectNewAttack(npc);
        }

        public static void DoBehavior_SpinningDarkEnergy(NPC npc, Player target, bool phase2, ref float attackTimer)
        {
            int energyReleaseRate = 128;
            int accelerateDelay = 54;
            int wrappedAttackTimer = (int)attackTimer % energyReleaseRate;
            int acceleratingEnergyID = ModContent.ProjectileType<AcceleratingDarkEnergy>();
            float acceleration = 1.0227f;

            if (phase2)
                acceleration += 0.004f;

            // Release energy balls from the Ceaseless Void's center.
            if (wrappedAttackTimer == 1f)
            {
                if (attackTimer >= energyReleaseRate * 2f)
                {
                    SelectNewAttack(npc);
                    return;
                }

                SoundEngine.PlaySound(SoundID.Item104, target.Center);

                // Create bloom and pulse rings while firing.
                PulseRing ring = new(npc.Center, Vector2.Zero, Color.MediumPurple * 0.5f, 0f, 8f, 20);
                GeneralParticleHandler.SpawnParticle(ring);

                StrongBloom bloom = new(npc.Center, Vector2.Zero, Color.Lerp(Color.Purple, Color.DarkBlue, 0.6f), 4f, 35);
                GeneralParticleHandler.SpawnParticle(bloom);

                // Create bursts of energy outward.
                for (int i = 0; i < 80; i++)
                {
                    Vector2 energyVelocity = -Vector2.UnitY.RotatedByRandom(0.47f) * Main.rand.NextFloat(2f, 53f);
                    Color energyColor = Color.Lerp(Color.MediumPurple, Color.Blue, Main.rand.NextFloat(0.6f));
                    MediumMistParticle darkEnergy = new(npc.Center, energyVelocity, energyColor, Color.DarkGray * 0.6f, 1.5f, 255f);
                    GeneralParticleHandler.SpawnParticle(darkEnergy);
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 7; i++)
                    {
                        Vector2 baseSpawnOffset = new Vector2(Lerp(-775f, 775f, i / 6f), -200f - LumUtils.Convert01To010(i / 6f) * 100f) + Main.rand.NextVector2Circular(30f, 30f);
                        for (int j = 0; j < 8; j++)
                        {
                            Vector2 microSpawnOffset = (TwoPi * j / 8f).ToRotationVector2() * 66f;
                            if (i % 2 == 0)
                                microSpawnOffset = microSpawnOffset.RotatedBy(Pi / 6f);

                            Vector2 energyRestingPosition = Vector2.Lerp(npc.Center, target.Center, 0.125f) + baseSpawnOffset;

                            ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(darkEnergy =>
                            {
                                darkEnergy.ModProjectile<AcceleratingDarkEnergy>().RestingPosition = energyRestingPosition + microSpawnOffset;
                                darkEnergy.ModProjectile<AcceleratingDarkEnergy>().CenterPoint = energyRestingPosition;
                                darkEnergy.ModProjectile<AcceleratingDarkEnergy>().Index = i * 4 + j;
                            });
                            Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, acceleratingEnergyID, DarkEnergyDamage, 0f);
                        }
                    }
                }
            }

            // Make energy balls accelerate.
            if (wrappedAttackTimer == accelerateDelay)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.CeaselessVoidSwirlSound, target.Center);
                foreach (Projectile energy in Utilities.AllProjectilesByID(acceleratingEnergyID))
                {
                    energy.ModProjectile<AcceleratingDarkEnergy>().Time = 0f;
                    energy.ModProjectile<AcceleratingDarkEnergy>().Acceleration = acceleration;
                    energy.ModProjectile<AcceleratingDarkEnergy>().AttackState = AcceleratingDarkEnergy.DarkEnergyAttackState.SpinInPlace;
                    energy.netUpdate = true;
                }
            }

            if (wrappedAttackTimer == accelerateDelay + AcceleratingDarkEnergy.SpinTime)
            {
                target.Infernum_Camera().CurrentScreenShakePower = 10f;
                SoundEngine.PlaySound(InfernumSoundRegistry.CeaselessVoidStrikeSound with { Pitch = -0.6f }, target.Center);
            }
        }

        public static void DoBehavior_AreaDenialVortexTears(NPC npc, Player target, bool phase2, ref float attackTimer)
        {
            int vortexSpawnDelay = 60;
            int vortexSpawnRate = 24;
            int vortexSpawnCount = 11;

            if (phase2)
            {
                vortexSpawnRate -= 8;
                vortexSpawnCount++;
            }

            ref float vortexSpawnCounter = ref npc.Infernum().ExtraAI[0];
            ref float waiting = ref npc.Infernum().ExtraAI[1];

            if (waiting == 1f)
            {
                if (attackTimer >= 150f)
                {
                    SelectNewAttack(npc);
                    ClearEntities();
                }
                return;
            }

            // Wait before creating vortices.
            if (attackTimer < vortexSpawnDelay)
                return;

            // Give a tip before the torrent is fired.
            if (attackTimer == vortexSpawnDelay + 30f)
                HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.CVPortalTip");

            // Periodically release vortices that strike at the target.
            float attackCompletion = Utils.GetLerpValue(vortexSpawnDelay, vortexSpawnDelay + vortexSpawnCount * vortexSpawnRate, attackTimer, true);
            if ((attackTimer - vortexSpawnDelay) % vortexSpawnRate == 0f)
            {
                if (attackCompletion >= 1f)
                {
                    attackTimer = 0f;
                    waiting = 1f;
                    npc.netUpdate = true;
                    return;
                }

                float vortexSpawnOffsetAngle = TwoPi * attackCompletion;
                SoundEngine.PlaySound(SoundID.Item104, target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 vortexSpawnPosition = Vector2.Lerp(npc.Center, target.Center, 0.45f) - Vector2.UnitY.RotatedBy(vortexSpawnOffsetAngle) * 600f;
                    Vector2 vortexAimDirection = (target.Center - vortexSpawnPosition).SafeNormalize(Vector2.UnitY);
                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(vortex =>
                    {
                        vortex.MaxUpdates = 4;
                        vortex.ModProjectile<CeaselessVortex>().AimDirectlyAtTarget = true;
                    });
                    Utilities.NewProjectileBetter(vortexSpawnPosition, vortexAimDirection, ModContent.ProjectileType<CeaselessVortex>(), 0, 0f);
                }
            }
        }

        public static void DoBehavior_ShellCrack(NPC npc, Player target, ref float attackTimer, ref float voidIsCracked)
        {
            int chargeUpTime = 88;
            int whiteningTime = 35;
            int whiteningWaitTime = 36;
            int whiteningFadeOutTime = 12;

            // Disable damage during this attack.
            npc.dontTakeDamage = true;

            // Charge up energy before performing whitening.
            if (attackTimer <= chargeUpTime)
            {
                target.Infernum_Camera().CurrentScreenShakePower = attackTimer / chargeUpTime * 3f;

                // Create a slice effect through the void right before the screen whitening happens.
                if (attackTimer == chargeUpTime - CeaselessVoidLineTelegraph.Lifetime)
                    SoundEngine.PlaySound(InfernumSoundRegistry.CeaselessVoidStrikeSound with { Volume = 1.8f }, target.Center);

                float sliceInterpolant = Utils.GetLerpValue(chargeUpTime - CeaselessVoidLineTelegraph.Lifetime, chargeUpTime - CeaselessVoidLineTelegraph.Lifetime + 6f, attackTimer, true);
                if (Main.netMode != NetmodeID.MultiplayerClient && sliceInterpolant > 0f && sliceInterpolant < 1f)
                {
                    Vector2 lineDirection = (ToRadians(30f) + Pi * sliceInterpolant).ToRotationVector2();
                    Utilities.NewProjectileBetter(npc.Center, lineDirection, ModContent.ProjectileType<CeaselessVoidLineTelegraph>(), 0, 0f);
                }

                return;
            }

            // Make the whitening effect draw the Ceaseless Void.
            float whiteningFadeIn = Utils.GetLerpValue(chargeUpTime, chargeUpTime + whiteningTime, attackTimer, true);
            float whiteningFadeOut = Utils.GetLerpValue(chargeUpTime + whiteningTime + whiteningWaitTime + whiteningFadeOutTime, chargeUpTime + whiteningTime + whiteningWaitTime, attackTimer, true);
            CeaselessVoidWhiteningEffect.WhiteningInterpolant = whiteningFadeIn * whiteningFadeOut;
            CeaselessVoidWhiteningEffect.DrawStatus = CeaselessVoidWhiteningEffect.OutlineDrawStatus.DrawCeaselessVoid;

            // Break the metal.
            if (attackTimer == chargeUpTime + whiteningTime)
            {
                target.Infernum_Camera().CurrentScreenShakePower = 24f;

                SoundEngine.PlaySound(InfernumSoundRegistry.CeaselessVoidMetalBreakSound);
                voidIsCracked = 1f;

                if (Main.netMode != NetmodeID.Server)
                {
                    for (int i = 0; i < 15; i++)
                        Gore.NewGore(npc.GetSource_FromAI(), npc.Center + Main.rand.NextVector2Circular(85f, 85f), Main.rand.NextVector2Circular(10f, 10f), InfernumMode.Instance.Find<ModGore>("CeaselessVoidFragment").Type, 0.8f);
                }
            }

            if (whiteningFadeOut <= 0f)
                SelectNewAttack(npc);
        }

        public static void DoBehavior_DarkEnergyTorrent(NPC npc, Player target, ref float attackTimer)
        {
            int chargeUpTime = 180;
            int spiralShootTime = 180;
            int attackTransitionDelay = 105;
            int spiralReleaseRate = 7;
            int spiralArmsCount = 6;
            float spiralAcceleration = 1.0245f;

            // Disable damage during this attack.
            npc.dontTakeDamage = true;

            // Play sounds at sections of the attack.
            if (attackTimer == 1f)
                SoundEngine.PlaySound(BossRushEvent.TerminusDeactivationSound with { Pitch = -0.45f });
            if (attackTimer == chargeUpTime)
                SoundEngine.PlaySound(InfernumSoundRegistry.CeaselessVoidEnergyTorrentSound);

            // Perform charge-up effects.
            if (attackTimer < chargeUpTime)
            {
                // Create light streaks that converge inward.
                if (attackTimer <= chargeUpTime - 45f)
                    CreateEnergySuckParticles(npc, Vector2.Zero, 240f, 776f, 0.3f);

                // Create a pulsating energy orb.
                float energyOrbChargeInterpolant = Utils.GetLerpValue(30f, chargeUpTime - 30f, attackTimer, true);
                if (energyOrbChargeInterpolant > 0f && attackTimer <= chargeUpTime - 30f)
                {
                    float energyOrbPulse = Sin(TwoPi * attackTimer / 8f) * 0.3f;
                    float energyOrbScaleFadeIn = Utils.GetLerpValue(0f, 0.56f, energyOrbChargeInterpolant, true);
                    float energyOrbScaleFadeOut = Utils.GetLerpValue(1f, 0.94f, energyOrbChargeInterpolant, true);
                    float energyOrbScale = energyOrbPulse + energyOrbScaleFadeIn * energyOrbScaleFadeOut * 2.5f;

                    for (float d = 0.5f; d < 1f; d += 0.2f)
                    {
                        Color energyOrbColor = Color.Lerp(Color.DeepPink, Color.DarkBlue, Main.rand.NextFloat(0.8f)) * 0.7f;
                        StrongBloom energyOrb = new(npc.Center, Vector2.Zero, energyOrbColor, energyOrbScale * d, 3);
                        GeneralParticleHandler.SpawnParticle(energyOrb);
                    }
                }

                // Create a pulse particle before firing.
                if (attackTimer == chargeUpTime - 20f)
                {
                    target.Infernum_Camera().CurrentScreenShakePower = 24f;
                    Utilities.CreateShockwave(npc.Center, 12, 5, 64f, false);
                    ScreenEffectSystem.SetBlurEffect(npc.Center, 0.5f, 20);
                }

                return;
            }

            // Give a tip before the torrent is fired.
            if (attackTimer == chargeUpTime + 1f)
                HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.CVEnergyBurstTip");

            // Release a spiral of dark energy.
            if (attackTimer >= chargeUpTime && attackTimer < chargeUpTime + spiralShootTime)
            {
                // Periodically emit energy sparks.
                if (attackTimer % 40f == 39f)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        Vector2 sparkVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 16f);
                        Color sparkColor = Color.Lerp(Color.Cyan, Color.IndianRed, Main.rand.NextFloat(0.6f));
                        GeneralParticleHandler.SpawnParticle(new SparkParticle(npc.Center, sparkVelocity, false, 45, 2f, sparkColor));

                        sparkVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 23f);
                        Color arcColor = Color.Lerp(Color.Cyan, Color.HotPink, Main.rand.NextFloat(0.1f, 0.65f));
                        GeneralParticleHandler.SpawnParticle(new ElectricArc(npc.Center, sparkVelocity, arcColor, 0.84f, 27));
                    }
                }

                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % spiralReleaseRate == 0f)
                {
                    for (int i = 0; i < spiralArmsCount; i++)
                    {
                        float spiralOffsetAngle = TwoPi * i / spiralArmsCount;
                        float timeShootOffsetAngle = (attackTimer - chargeUpTime) * ToRadians(3f);
                        Vector2 spiralShootVelocity = (spiralOffsetAngle + timeShootOffsetAngle).ToRotationVector2() * 7f;

                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(darkEnergy =>
                        {
                            darkEnergy.ModProjectile<AcceleratingDarkEnergy>().Time = 30f;
                        });
                        Utilities.NewProjectileBetter(npc.Center, spiralShootVelocity, ModContent.ProjectileType<AcceleratingDarkEnergy>(), DarkEnergyTorrentDamage, 0f, -1, (int)AcceleratingDarkEnergy.DarkEnergyAttackState.AccelerateTowardsTarget, spiralAcceleration);
                    }
                }
            }

            if (attackTimer >= chargeUpTime + spiralShootTime + attackTransitionDelay)
                SelectNewAttack(npc);
        }

        public static void DoBehavior_EnergySuck(NPC npc, Player target, ref float attackTimer)
        {
            int attackDelay = 90;
            int suckTime = 270;
            int attackTransitionDelay = 120;
            int darkEnergyCircleCount = 5;
            int rubbleReleaseRate = 4;
            float suckDistance = 2750f;
            float burstAcceleration = 1.023f;

            // Grant the target infinite flight time so that they don't run out and take an unfair hit.
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.dead || !player.active || !npc.WithinRange(player.Center, 10000f))
                    continue;

                player.DoInfiniteFlightCheck(InfiniteFlightTextColor);
            }

            // Create a dark energy circle on the first frame.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 1f)
            {
                for (int i = 0; i < darkEnergyCircleCount; i++)
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<SpinningDarkEnergy>(), DarkEnergyDamage, 0f, -1, 0f, TwoPi * i / darkEnergyCircleCount);
            }

            // Do contact damage so that the player is punished for being sucked in.
            npc.damage = npc.defDamage;

            // Calculate the relative intensity of the suck effect.
            float suckPowerInterpolant = Utils.GetLerpValue(attackDelay + 30f, attackDelay + suckTime * 0.35f, attackTimer, true);
            float suckAcceleration = 0f;
            if (attackTimer >= attackDelay + suckTime)
                suckPowerInterpolant = 0f;

            // Make the screen shake at first.
            if (attackTimer >= attackDelay && attackTimer <= attackDelay + suckTime)
                target.Infernum_Camera().CurrentScreenShakePower = Utils.GetLerpValue(attackDelay + suckTime - 90f, attackDelay + suckTime, attackTimer, true) * 12f;

            if (attackTimer == attackDelay + suckTime)
            {
                target.Infernum_Camera().CurrentScreenShakePower = 18f;
                ScreenEffectSystem.SetBlurEffect(npc.Center, 0.6f, 25);

                SoundEngine.PlaySound(InfernumSoundRegistry.CeaselessVoidStrikeSound with { Volume = 2f, Pitch = -0.5f });
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 27; i++)
                    {
                        Vector2 spiralShootVelocity = (TwoPi * i / 27f).ToRotationVector2() * Main.rand.NextFloat(7f, 9.5f) + Main.rand.NextVector2Circular(0.5f, 0.5f);
                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(darkEnergy =>
                        {
                            darkEnergy.ModProjectile<AcceleratingDarkEnergy>().Time = 30f;
                        });
                        Utilities.NewProjectileBetter(npc.Center, spiralShootVelocity, ModContent.ProjectileType<AcceleratingDarkEnergy>(), DarkEnergyDamage, 0f, -1, (int)AcceleratingDarkEnergy.DarkEnergyAttackState.AccelerateTowardsTarget, burstAcceleration);
                    }
                }
                Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<SpinningDarkEnergy>(), ModContent.ProjectileType<ConvergingDungeonRubble>());
            }

            // Play a suck sound.
            if (attackTimer == attackDelay)
                SoundEngine.PlaySound(CeaselessVoidBoss.BuildupSound);

            // Create various energy particles.
            if (suckPowerInterpolant > 0f)
            {
                suckAcceleration = Lerp(0.16f, 0.35f, suckPowerInterpolant);
                Vector2 energySuckOffset = Vector2.Zero;
                CreateEnergySuckParticles(npc, energySuckOffset, 240f, 960f, 0.5f / (energySuckOffset.Length() * 0.0012f + 1f));

                // Create pulse rungs and bloom periodically.
                if (attackTimer % 15f == 0f)
                {
                    SoundEngine.PlaySound(SoundID.Item104 with { Pitch = 0.4f, Volume = 0.8f }, npc.Center);

                    Color lightColor = Color.Lerp(Color.MediumPurple, Color.DarkBlue, Main.rand.NextFloat(0f, 0.5f));
                    if (Main.rand.NextBool())
                        lightColor = Color.Lerp(Color.Purple, Color.Black, 0.6f);

                    PulseRing ring = new(npc.Center, Vector2.Zero, lightColor, 4f, 0f, 60);
                    GeneralParticleHandler.SpawnParticle(ring);

                    StrongBloom bloom = new(npc.Center, Vector2.Zero, lightColor, 1f, 15);
                    GeneralParticleHandler.SpawnParticle(bloom);
                }
            }

            // Release rubble around the arena.
            if (Main.netMode != NetmodeID.MultiplayerClient && suckPowerInterpolant > 0f && attackTimer % rubbleReleaseRate == 0f && attackTimer < attackDelay + suckTime)
            {
                float rubbleShootSpeed = 9f;
                Vector2 rubbleSpawnPosition = npc.Center + Main.rand.NextVector2Unit() * (npc.Distance(target.Center) + Main.rand.NextFloat(250f, 700f));
                Vector2 rubbleVelocity = (npc.Center - rubbleSpawnPosition).SafeNormalize(Vector2.UnitY) * rubbleShootSpeed;
                while (target.WithinRange(rubbleSpawnPosition, 750f))
                    rubbleSpawnPosition -= rubbleVelocity;

                Utilities.NewProjectileBetter(rubbleSpawnPosition, rubbleVelocity, ModContent.ProjectileType<ConvergingDungeonRubble>(), RubbleDamage, 0f, -1, 0f, 1f);
            }

            // Suck the player in towards the Ceaseless Void.
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];

                for (int j = 0; j < 10; j++)
                {
                    if (p.grappling[j] != -1)
                    {
                        Main.projectile[p.grappling[j]].Kill();
                        p.grappling[j] = -1;
                    }
                }

                float distance = p.Distance(npc.Center);
                if (distance < suckDistance && p.grappling[0] == -1)
                {
                    p.velocity.X += (p.Center.X < npc.Center.X).ToDirectionInt() * suckAcceleration;

                    if (Math.Abs(p.velocity.Y) >= 0.2f)
                        p.velocity.Y += (p.Center.Y < npc.Center.Y).ToDirectionInt() * suckAcceleration * 0.5f;
                }
            }

            if (attackTimer >= attackDelay + suckTime + attackTransitionDelay)
                SelectNewAttack(npc);
        }

        public static void DoBehavior_ChainBreakTransition(NPC npc, Player target, ref float attackTimer)
        {
            int chargeUpTime = 184;
            int whiteningTime = 35;
            int whiteningWaitTime = 36;
            int whiteningFadeOutTime = 6;
            float whiteningFadeIn = Utils.GetLerpValue(chargeUpTime, chargeUpTime + whiteningTime, attackTimer, true);
            float whiteningFadeOut = Utils.GetLerpValue(chargeUpTime + whiteningTime + whiteningWaitTime + whiteningFadeOutTime, chargeUpTime + whiteningTime + whiteningWaitTime, attackTimer, true);
            float whiteningInterpolant = whiteningFadeIn * whiteningFadeOut;

            // Disable damage.
            npc.dontTakeDamage = true;

            // Play a buildup sound prior to the whitening effect.
            if (attackTimer == 1f)
            {
                // Give a tip.
                ClearEntities();
                HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.CVEnergyChainsTip");
                SoundEngine.PlaySound(CeaselessVoidBoss.BuildupSound);
            }

            // Enable the distortion filter if it isnt active and the player's config permits it.
            if (Main.netMode != NetmodeID.Server && !InfernumEffectsRegistry.ScreenDistortionScreenShader.IsActive() && Main.UseHeatDistortion)
            {
                float distortionInterpolant = (1f - whiteningInterpolant) * whiteningFadeOut * Utils.GetLerpValue(0f, 45f, attackTimer, true);

                Filters.Scene.Activate("InfernumMode:ScreenDistortion", Main.LocalPlayer.Center);
                InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().UseImage("Images/Extra_193");
                InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().Shader.Parameters["distortionAmount"].SetValue(distortionInterpolant * 25f);
                InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().Shader.Parameters["wiggleSpeed"].SetValue(2f);
            }

            // Charge up energy before performing whitening.
            if (attackTimer <= chargeUpTime)
            {
                target.Infernum_Camera().CurrentScreenShakePower = attackTimer / chargeUpTime * 8f;
                CreateEnergySuckParticles(npc, Vector2.Zero, 240f, 1120f, 0.4f);

                // Create pulse rings and bloom periodically.
                if (attackTimer % 10f == 9f)
                {
                    Color energyColor = Color.Lerp(Color.MediumPurple, Color.DarkBlue, Main.rand.NextFloat(0.5f));
                    PulseRing ring = new(npc.Center, Vector2.Zero, energyColor, 3.6f, 0f, 60);
                    GeneralParticleHandler.SpawnParticle(ring);

                    StrongBloom bloom = new(npc.Center, Vector2.Zero, energyColor, 1f, 15);
                    GeneralParticleHandler.SpawnParticle(bloom);
                }

                return;
            }

            // Make the whitening effect draw the Ceaseless Void's chains.
            CeaselessVoidWhiteningEffect.WhiteningInterpolant = whiteningInterpolant;
            CeaselessVoidWhiteningEffect.DrawStatus = CeaselessVoidWhiteningEffect.OutlineDrawStatus.DrawChains;

            // Break the chains.
            if (attackTimer == chargeUpTime + whiteningTime)
            {
                target.Infernum_Camera().CurrentScreenShakePower = 25f;

                SoundEngine.PlaySound(CeaselessVoidBoss.DeathSound);
                DestroyChains(npc);
            }

            if (whiteningFadeOut <= 0f)
                SelectNewAttack(npc);
        }

        public static void DoBehavior_JevilDarkEnergyBursts(NPC npc, Player target, ref float attackTimer, ref float teleportEffectInterpolant)
        {
            int teleportAnimationTime = 37;
            int darkBurstCount = 3;
            int shootCount = 9;
            float animationAttackTimer = attackTimer % teleportAnimationTime;
            float spiralAcceleration = 1.02f;
            float teleportOffset = 560f;
            float teleportAnimationCompletionFactor = 1.8f;
            ref float shootCounter = ref npc.Infernum().ExtraAI[0];

            // Decide the teleport effect interpolant.
            teleportEffectInterpolant = animationAttackTimer / teleportAnimationTime * teleportAnimationCompletionFactor;

            if (animationAttackTimer == (int)(teleportAnimationTime * 0.5f / teleportAnimationCompletionFactor))
            {
                // Teleport next to the target.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 teleportOffsetDirection = -target.velocity.SafeNormalize(Main.rand.NextVector2Unit()).RotatedByRandom(Pi - 0.6f);
                    TeleportToPosition(npc, target.Center + teleportOffsetDirection * teleportOffset);

                    shootCounter++;
                    npc.netUpdate = true;

                    if (shootCounter >= shootCount)
                    {
                        SelectNewAttack(npc);
                        return;
                    }

                    for (int i = 0; i < darkBurstCount; i++)
                    {
                        float shootOffsetAngle = Lerp(-0.5f, 0.5f, i / (float)(darkBurstCount - 1f));
                        Vector2 spiralShootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(shootOffsetAngle) * 9f + Main.rand.NextVector2Circular(1.4f, 1.4f);

                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(darkEnergy =>
                        {
                            darkEnergy.ModProjectile<AcceleratingDarkEnergy>().Time = 30f;
                        });
                        Utilities.NewProjectileBetter(npc.Center, spiralShootVelocity, ModContent.ProjectileType<AcceleratingDarkEnergy>(), DarkEnergyDamage, 0f, -1, (int)AcceleratingDarkEnergy.DarkEnergyAttackState.AccelerateTowardsTarget, spiralAcceleration);
                    }
                }

                SoundEngine.PlaySound(InfernumSoundRegistry.CeaselessVoidSwirlSound, npc.Center);
            }
        }

        public static void DoBehavior_MirroredCharges(NPC npc, Player target, ref float attackTimer, ref float teleportEffectInterpolant)
        {
            int teleportAnimationTime = 40;
            int ringBulletCount = 18;
            int chargeCount = 6;
            int attackStartDelay = 60;
            float teleportAnimationCompletionFactor = 2.4f;
            float teleportOffset = 540f;
            float startingSpeed = teleportOffset / 172f;
            float arcingBoltAngularVelocity = ToRadians(0.7f) * Main.rand.NextFromList(-1f, 1f);
            ref float shootCounter = ref npc.Infernum().ExtraAI[0];
            ref float teleportCenterX = ref npc.Infernum().ExtraAI[1];
            ref float teleportCenterY = ref npc.Infernum().ExtraAI[2];
            ref float acceleration = ref npc.Infernum().ExtraAI[3];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[4];

            float animationAttackTimer = (attackTimer - attackStartDelay) % teleportAnimationTime;

            // Do contact damage.
            npc.damage = npc.defDamage;

            // Initialize acceleration. The underlying calculus necessary to decide this value can be a bit complex, and as such it is only done once for
            // performance reasons. This calculates the acceleration the Ceaseless Void must move at every frame to ensure that it travels an exact distance in a given
            // amount of time from a specific starting speed.
            if (acceleration <= 0f)
            {
                double offsetFromIdealTravelDistance(double x)
                {
                    double distance = 0D;
                    for (int i = 1; i <= teleportAnimationTime; i++)
                        distance += Math.Pow(x, i) * startingSpeed;
                    return distance - teleportOffset;
                }
                acceleration = (float)Utilities.IterativelySearchForRoot(offsetFromIdealTravelDistance, 1D, 13);
                npc.netUpdate = true;
            }

            if (attackTimer <= attackStartDelay)
            {
                teleportEffectInterpolant = 0f;
                npc.velocity = Vector2.Zero;
                return;
            }

            // Decide the teleport effect interpolant.
            teleportEffectInterpolant = animationAttackTimer / teleportAnimationTime * teleportAnimationCompletionFactor;

            // Teleport on top of the player before the split charges happen.
            if (animationAttackTimer == (int)(teleportAnimationTime * 0.5f / teleportAnimationCompletionFactor))
            {
                // Do funny screen stuff.
                Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 6f;
                ScreenEffectSystem.SetFlashEffect(npc.Center, 1.25f, 24);

                Vector2 impactPoint = new(teleportCenterX, teleportCenterY);

                // Release energy sparks at the impact point.
                for (int i = 0; i < 25; i++)
                {
                    Vector2 sparkVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 16f);
                    Color sparkColor = Color.Lerp(Color.Cyan, Color.IndianRed, Main.rand.NextFloat(0.6f));
                    GeneralParticleHandler.SpawnParticle(new SparkParticle(impactPoint, sparkVelocity, false, 45, 2f, sparkColor));

                    sparkVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 23f);
                    Color arcColor = Color.Lerp(Color.Cyan, Color.HotPink, Main.rand.NextFloat(0.1f, 0.65f));
                    GeneralParticleHandler.SpawnParticle(new ElectricArc(impactPoint, sparkVelocity, arcColor, 0.84f, 27));
                }

                if (chargeCounter >= chargeCount)
                {
                    npc.velocity = Vector2.Zero;
                    TeleportToPosition(npc, target.Center - Vector2.UnitY * 350f);
                    SelectNewAttack(npc);
                    return;
                }

                SoundEngine.PlaySound(InfernumSoundRegistry.CeaselessVoidStrikeSound with { Volume = 0.55f }, impactPoint);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < ringBulletCount; i++)
                    {
                        Vector2 energyBoltVelocity = (TwoPi * i / ringBulletCount).ToRotationVector2() * 4.5f;

                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(bolt =>
                        {
                            bolt.ModProjectile<OtherworldlyBolt>().ArcAngularVelocity = arcingBoltAngularVelocity;
                        });
                        Utilities.NewProjectileBetter(impactPoint, energyBoltVelocity, ModContent.ProjectileType<OtherworldlyBolt>(), OtherworldlyBoltDamage, 0f, -1, (int)OtherworldlyBolt.OtherwordlyBoltAttackState.ArcAndAccelerate);
                    }
                }

                // Teleport at an offset perpendicular to the player's current velocity.
                teleportCenterX = target.Center.X + target.velocity.X * 12f;
                teleportCenterY = target.Center.Y + target.velocity.Y * 12f;
                TeleportToPosition(npc, new Vector2(teleportCenterX, teleportCenterY) - target.velocity.RotatedBy(PiOver2).SafeNormalize(Main.rand.NextVector2Unit()) * teleportOffset);
                npc.velocity = npc.SafeDirectionTo(new(teleportCenterX, teleportCenterY)) * startingSpeed;
                chargeCounter++;
            }

            // Accelerate.
            npc.velocity *= acceleration;
        }

        public static void DoBehavior_ConvergingEnergyBarrages(NPC npc, Player target, ref float attackTimer)
        {
            int hoverTime = 30;
            int barrageBurstCount = 4;
            int barrageTelegraphTime = 25;
            int barrageShootRate = 20;
            int barrageCount = 17;
            int attackTransitionDelay = 25;
            float maxShootOffsetAngle = 1.49f;
            float initialBarrageSpeed = 20.5f;

            ref float hoverOffsetAngle = ref npc.Infernum().ExtraAI[0];
            ref float playerShootDirection = ref npc.Infernum().ExtraAI[1];
            ref float barrageBurstCounter = ref npc.Infernum().ExtraAI[2];
            if (barrageBurstCounter == 0f)
                hoverTime += 64;

            // Hover before firing.
            if (attackTimer < hoverTime + barrageShootRate - barrageTelegraphTime)
            {
                Vector2 hoverDestination = target.Center - Vector2.UnitY.RotatedBy(hoverOffsetAngle) * 640f;
                npc.Center = Vector2.Lerp(npc.Center, hoverDestination, 0.025f);

                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 25f;
                npc.SimpleFlyMovement(idealVelocity, 1.9f);
                if (npc.WithinRange(hoverDestination, 100f))
                    npc.velocity *= 0.85f;
            }
            else
                npc.velocity *= 0.8f;

            // Prepare particle line telegraphs.
            if (attackTimer == hoverTime + barrageShootRate - barrageTelegraphTime)
            {
                SoundEngine.PlaySound(SoundID.Item28, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    playerShootDirection = npc.AngleTo(target.Center);
                    for (int i = 0; i < barrageCount; i++)
                    {
                        float offsetAngle = Lerp(-maxShootOffsetAngle, maxShootOffsetAngle, i / (float)(barrageCount - 1f));

                        List<Vector2> telegraphPoints = [];
                        for (int frames = 1; frames < 84; frames += 12)
                        {
                            Vector2 linePosition = TelegraphedOtherwordlyBolt.SimulateMotion(npc.Center, (offsetAngle + playerShootDirection).ToRotationVector2() * initialBarrageSpeed, playerShootDirection, frames);
                            telegraphPoints.Add(linePosition);
                        }

                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(telegraph =>
                        {
                            telegraph.ModProjectile<EnergyTelegraph>().TelegraphPoints = [.. telegraphPoints];
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
                // Create a puff of dark energy.
                for (int i = 0; i < 16; i++)
                {
                    Color darkEnergyColor = Main.rand.NextBool() ? Color.HotPink : Color.Purple;
                    Vector2 darkEnergyVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(Lerp(-0.88f, 0.88f, i / 16f)) * 14f + Main.rand.NextVector2Circular(2f, 2f);
                    CloudParticle darkEnergyCloud = new(npc.Center, darkEnergyVelocity, darkEnergyColor, Color.DarkBlue * 0.75f, 25, Main.rand.NextFloat(2.5f, 3.2f));
                    GeneralParticleHandler.SpawnParticle(darkEnergyCloud);
                }

                SoundEngine.PlaySound(InfernumSoundRegistry.CeaselessVoidStrikeSound, npc.Center);
                for (int i = 0; i < barrageCount; i++)
                {
                    float offsetAngle = Lerp(-maxShootOffsetAngle, maxShootOffsetAngle, i / (float)(barrageCount - 1f));
                    Vector2 shootVelocity = (offsetAngle + playerShootDirection).ToRotationVector2() * initialBarrageSpeed;
                    Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<TelegraphedOtherwordlyBolt>(), OtherworldlyBoltDamage, 0f, -1, 0f, playerShootDirection);
                }
            }

            if (attackTimer >= hoverTime + barrageShootRate + attackTransitionDelay)
            {
                attackTimer = 0f;
                hoverOffsetAngle += TwoPi / barrageBurstCount + Main.rand.NextFloatDirection() * 0.36f;
                barrageBurstCounter++;
                if (barrageBurstCounter >= barrageBurstCount)
                    SelectNewAttack(npc);

                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_DeathAnimation(NPC npc, Player target, ref float attackTimer)
        {
            int jitterTime = 90;
            int outburstTime = 480;
            int waitDelay = 45;
            int spiralReleaseRate = 5;
            int spiralArmsCount = 7;
            float spiralAcceleration = 1.017f;
            ref float hasExploded = ref npc.Infernum().ExtraAI[0];
            ref float portalScale = ref npc.Infernum().ExtraAI[1];

            // Completely stop in place.
            npc.velocity = Vector2.Zero;

            // Close the HP bar.
            npc.Calamity().ShouldCloseHPBar = true;

            // Teleport above the player on the first frame. If there's tiles above, teleport below them instead.
            if (attackTimer <= 1f)
            {
                Vector2 teleportPosition = target.Center - Vector2.UnitY * 300f;
                if (Collision.SolidCollision(teleportPosition - Vector2.One * 150f, 300, 300))
                    teleportPosition = target.Center + Vector2.UnitY * 300f;

                TeleportToPosition(npc, teleportPosition);
                SoundEngine.PlaySound(InfernumSoundRegistry.CeaselessVoidSwirlSound);
            }

            // Jitter in place and shake the screen at first.
            if (attackTimer <= jitterTime)
            {
                float jitterSpeed = attackTimer / jitterTime * 12f;
                npc.Center += Main.rand.NextVector2CircularEdge(jitterSpeed, jitterSpeed);
                target.Infernum_Camera().CurrentScreenShakePower = jitterSpeed * 0.8f;

                // Charge energy.
                CreateEnergySuckParticles(npc, Vector2.Zero);

                // Create pulse rings and bloom periodically.
                if (attackTimer % 9f == 8f)
                {
                    Color energyColor = Color.Lerp(Color.MediumPurple, Color.DarkBlue, Main.rand.NextFloat(0.5f));
                    PulseRing ring = new(npc.Center, Vector2.Zero, energyColor, 3.6f, 0f, 60);
                    GeneralParticleHandler.SpawnParticle(ring);

                    StrongBloom bloom = new(npc.Center, Vector2.Zero, energyColor, 1f, 15);
                    GeneralParticleHandler.SpawnParticle(bloom);
                }
            }

            // Prepere a shockwave and destroy the metal shell.
            if (attackTimer == jitterTime)
            {
                // Perform screen effects.
                target.Infernum_Camera().CurrentScreenShakePower = 16f;
                ScreenEffectSystem.SetBlurEffect(npc.Center, 0.3f, 25);

                // Play impactful sounds.
                SoundEngine.PlaySound(InfernumSoundRegistry.CeaselessVoidEnergyTorrentSound with { Volume = 1.5f });
                SoundEngine.PlaySound(InfernumSoundRegistry.CeaselessVoidMetalBreakSound with { Volume = 1.5f });

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 12; i++)
                    {
                        int variant = i % 3;
                        if (i >= 1 && variant == 0)
                            variant = 2;

                        Vector2 shellSpawnPosition = npc.Center + Main.rand.NextVector2Circular(50f, 50f);
                        Vector2 shellShootVelocity = (TwoPi * i / 12f).ToRotationVector2() * 15f + Main.rand.NextVector2Circular(4f, 4f);
                        Utilities.NewProjectileBetter(shellSpawnPosition, shellShootVelocity, ModContent.ProjectileType<CeaselessVoidShell>(), 0, 0f, -1, variant);
                    }
                    hasExploded = 1f;
                    npc.netUpdate = true;
                }
            }

            float energyOrbChargeInterpolant = Utils.GetLerpValue(jitterTime, jitterTime + outburstTime - 30f, attackTimer, true);
            if (attackTimer >= jitterTime && energyOrbChargeInterpolant < 1f)
            {
                // Periodically emit energy sparks and circles.
                if (attackTimer % 32f == 31f)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.CeaselessVoidStrikeSound);

                    for (int i = 0; i < 20; i++)
                    {
                        Vector2 sparkVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 16f);
                        Color sparkColor = Color.Lerp(Color.Cyan, Color.IndianRed, Main.rand.NextFloat(0.6f));
                        GeneralParticleHandler.SpawnParticle(new SparkParticle(npc.Center, sparkVelocity, false, 45, 2f, sparkColor));

                        sparkVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 23f);
                        Color arcColor = Color.Lerp(Color.Cyan, Color.HotPink, Main.rand.NextFloat(0.1f, 0.65f));
                        GeneralParticleHandler.SpawnParticle(new ElectricArc(npc.Center, sparkVelocity, arcColor, 0.84f, 27));
                    }

                    for (float s = 7f; s < 12f; s += 1.5f)
                    {
                        Color energyColor = Color.Lerp(Color.MediumPurple, Color.DarkBlue, Main.rand.NextFloat(0.5f));
                        PulseRing ring = new(npc.Center, Vector2.Zero, energyColor, 0f, s, 30);
                        GeneralParticleHandler.SpawnParticle(ring);
                    }
                }

                // Release very powerful spirals of dark energy outward.
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % spiralReleaseRate == 0f && energyOrbChargeInterpolant < 1f)
                {
                    for (int i = 0; i < spiralArmsCount; i++)
                    {
                        float spiralOffsetAngle = TwoPi * i / spiralArmsCount;
                        float timeShootOffsetAngle = Cos(TwoPi * (attackTimer - jitterTime) / 90f) * ToRadians(54f);
                        Vector2 spiralShootVelocity = (spiralOffsetAngle + timeShootOffsetAngle).ToRotationVector2() * 7.5f;

                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(darkEnergy =>
                        {
                            darkEnergy.ModProjectile<AcceleratingDarkEnergy>().Time = 30f;
                            darkEnergy.ModProjectile<AcceleratingDarkEnergy>().NeverCollideWithTiles = true;
                        });
                        Utilities.NewProjectileBetter(npc.Center, spiralShootVelocity, ModContent.ProjectileType<AcceleratingDarkEnergy>(), DarkEnergyTorrentDamage, 0f, -1, (int)AcceleratingDarkEnergy.DarkEnergyAttackState.AccelerateTowardsTarget, spiralAcceleration);
                    }
                }
            }

            // Create a pulsating energy orb.
            if (energyOrbChargeInterpolant > 0f)
            {
                // Calculate the portal scale.
                portalScale = Utils.GetLerpValue(0f, 0.25f, energyOrbChargeInterpolant, true);

                float energyOrbPulse = Sin(TwoPi * attackTimer / 7f) * 0.5f;
                float energyOrbScaleFadeIn = Utils.GetLerpValue(0f, 0.56f, energyOrbChargeInterpolant, true);
                float energyOrbScale = energyOrbPulse + energyOrbScaleFadeIn * 6f;

                for (float d = 0.5f; d < 1f; d += 0.2f)
                {
                    Color energyOrbColor = Color.Lerp(Color.DeepPink, Color.White, Main.rand.NextFloat(0.8f));
                    energyOrbColor = Color.Lerp(energyOrbColor, Color.DeepSkyBlue, 0.25f);
                    StrongBloom energyOrb = new(npc.Center, Vector2.Zero, energyOrbColor * 0.8f, energyOrbScale * d, 3);
                    GeneralParticleHandler.SpawnParticle(energyOrb);
                }
            }

            // Whiten the screen during the wait delay.
            float whiteningInterpolant = Utils.GetLerpValue(jitterTime + outburstTime, jitterTime + outburstTime + waitDelay - 30f, attackTimer, true);
            CeaselessVoidWhiteningEffect.WhiteningInterpolant = Pow(whiteningInterpolant, 2f);
            CeaselessVoidWhiteningEffect.DrawStatus = CeaselessVoidWhiteningEffect.OutlineDrawStatus.DrawChains;

            // Die.
            if (attackTimer >= jitterTime + outburstTime + waitDelay)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.CeaselessVoidMetalBreakSound with { Pitch = -0.6f });
                npc.active = false;
                npc.HitEffect();
                npc.NPCLoot();

                CeaselessVoidArchivesSpawnSystem.WaitingForPlayersToLeaveArchives = true;
            }

            // Disable damage.
            npc.dontTakeDamage = true;
        }

        public static void ClearEntities()
        {
            for (int i = 0; i < 3; i++)
            {
                Utilities.DeleteAllProjectiles(false,
                [
                    ModContent.ProjectileType<AcceleratingDarkEnergy>(),
                    ModContent.ProjectileType<CeaselessEnergyPulse>(),
                    ModContent.ProjectileType<CeaselessVoidLineTelegraph>(),
                    ModContent.ProjectileType<CeaselessVortex>(),
                    ModContent.ProjectileType<CeaselessVortexTear>(),
                    ModContent.ProjectileType<ConvergingDungeonRubble>(),
                    ModContent.ProjectileType<OtherworldlyBolt>(),
                    ModContent.ProjectileType<SpinningDarkEnergy>(),
                    ModContent.ProjectileType<TelegraphedOtherwordlyBolt>()
                ]);
            }
        }

        public static void SelectNewAttack(NPC npc)
        {
            // Select a new target.
            npc.TargetClosest();

            CeaselessVoidAttackType previousAttack = (CeaselessVoidAttackType)npc.ai[0];
            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool inPhase2 = lifeRatio < Phase2LifeRatio;
            bool inPhase3 = lifeRatio < Phase3LifeRatio;
            ref float phaseCycleIndex = ref npc.Infernum().ExtraAI[PhaseCycleIndexIndex];

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            if (inPhase3)
                npc.ai[0] = (int)Phase3AttackCycle[(int)phaseCycleIndex % Phase3AttackCycle.Length];
            else if (inPhase2)
                npc.ai[0] = (int)Phase2AttackCycle[(int)phaseCycleIndex % Phase2AttackCycle.Length];
            else
                npc.ai[0] = (int)Phase1AttackCycle[(int)phaseCycleIndex % Phase1AttackCycle.Length];

            if (previousAttack == CeaselessVoidAttackType.ShellCrackTransition)
                npc.ai[0] = (int)CeaselessVoidAttackType.DarkEnergyTorrent;
            else
                phaseCycleIndex++;
            npc.localAI[1] = 0f;
            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }
        #endregion AI

        #region Drawing

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            // Draw chains.
            DrawChains(Color.White);

            // Draw the Ceaseless Void.
            if (npc.ai[0] == (int)CeaselessVoidAttackType.MirroredCharges)
            {
                Vector2 teleportCenter = new Vector2(npc.Infernum().ExtraAI[1], npc.Infernum().ExtraAI[2]) - Main.screenPosition;
                Vector2 left = npc.Center - Main.screenPosition;
                Vector2 right = teleportCenter + (teleportCenter - left);
                float distanceFromCenter = teleportCenter.Distance(left);
                float specialColorInterpolant = Utils.GetLerpValue(40f, 150f, distanceFromCenter, true);
                DrawInstance(npc, left, lightColor, Color.Lerp(Color.White, Color.Cyan with { A = 0 } * 0.5f, specialColorInterpolant));
                DrawInstance(npc, right, lightColor, Color.Lerp(Color.White, Color.HotPink with { A = 0 } * 0.5f, specialColorInterpolant));
            }
            else if (npc.ai[0] == (int)CeaselessVoidAttackType.DeathAnimation)
            {
                bool hasExploded = npc.Infernum().ExtraAI[0] == 1f;
                float baseScale = npc.Infernum().ExtraAI[1];

                // Draw the portal if the Ceaseless Void has exploded.
                if (hasExploded)
                {
                    spriteBatch.EnterShaderRegion();

                    DrawBlackHole(npc, baseScale * 184f);
                    for (float s = 0f; s < 1f; s += 0.5f)
                        DrawPortal(npc, baseScale * s);
                    spriteBatch.ExitShaderRegion();
                }
                else
                    DrawInstance(npc, npc.Center - Main.screenPosition, lightColor, Color.White);
            }
            else
                DrawInstance(npc, npc.Center - Main.screenPosition, lightColor, Color.White);

            return false;
        }

        public static void DrawPortal(NPC npc, float portalScale)
        {
            var portalShader = InfernumEffectsRegistry.CeaselessVoidPortalShader;
            Texture2D noiseTexture = InfernumTextureRegistry.Void.Value;
            Vector2 origin = noiseTexture.Size() * 0.5f;
            Vector2 drawPosition = npc.Center - Main.screenPosition;

            portalShader.UseOpacity(npc.Opacity);
            portalShader.UseColor(Color.Black);
            portalShader.UseSecondaryColor(Color.Lerp(Color.HotPink, Color.DarkBlue, 0.58f));
            portalShader.Apply();
            Main.spriteBatch.Draw(noiseTexture, drawPosition, null, Color.White, 0f, origin, npc.scale * portalScale * 6f, 0, 0f);
        }

        public static void DrawBlackHole(NPC npc, float radius)
        {
            var tear = InfernumEffectsRegistry.RealityTearVertexShader;
            tear.SetTexture(InfernumTextureRegistry.Stars, 1);
            tear.TrySetParameter("useOutline", false);

            PrimitiveRenderer.RenderCircle(npc.Center, new(_ => radius, _ => Color.White, Shader: InfernumEffectsRegistry.RealityTearVertexShader));
        }

        public static void DrawInstance(NPC npc, Vector2 drawPosition, Color lightColor, Color colorFactor)
        {
            // Calculate scale values for the teleport effect.
            float teleportEffectInterpolant = npc.localAI[1];
            float stretchX = 1f;
            float stretchY = 1f;
            float opacity = 1f;
            if (teleportEffectInterpolant < 0.5f)
            {
                float localStretchInterpolant = Utils.GetLerpValue(0f, 0.5f, teleportEffectInterpolant, true);
                stretchX = Lerp(1f, 0.4f, Pow(localStretchInterpolant, 2f));
                stretchY = Lerp(1f, 0f, Pow(localStretchInterpolant, 0.5f));

                opacity = Pow(1f - localStretchInterpolant, 3f);
            }
            else if (teleportEffectInterpolant < 0.8f)
            {
                float localStretchInterpolant = Utils.GetLerpValue(0.5f, 0.8f, teleportEffectInterpolant, true);
                stretchX = localStretchInterpolant;
                stretchY = Sqrt(localStretchInterpolant);

                opacity = Pow(localStretchInterpolant, 2f);
            }

            Vector2 scale = new Vector2(stretchX, stretchY) * npc.scale;
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Texture2D glowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/CeaselessVoid/CeaselessVoidGlow").Value;
            Texture2D voidTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/CeaselessVoid/CeaselessVoidVoidStuff").Value;
            Main.spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(lightColor).MultiplyRGBA(colorFactor) * opacity, npc.rotation, npc.frame.Size() * 0.5f, scale, 0, 0f);
            Main.spriteBatch.Draw(glowmask, drawPosition, npc.frame, npc.GetAlpha(Color.White).MultiplyRGBA(colorFactor) * opacity, npc.rotation, npc.frame.Size() * 0.5f, scale, 0, 0f);

            Main.spriteBatch.EnterShaderRegion();

            DrawData drawData = new(voidTexture, drawPosition, npc.frame, npc.GetAlpha(Color.White) * opacity, npc.rotation, npc.frame.Size() * 0.5f, scale, 0, 0);
            InfernumEffectsRegistry.RealityTear2Shader.SetShaderTexture(InfernumTextureRegistry.Stars);
            InfernumEffectsRegistry.RealityTear2Shader.Apply(drawData);
            drawData.Draw(Main.spriteBatch);

            Main.spriteBatch.EnterShaderRegion();

            // Draw the shell.
            bool voidIsCracked = npc.localAI[0] == 1f;
            if (voidIsCracked)
            {
                Texture2D metalTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/CeaselessVoid/CeaselessMetalShell").Value;
                Texture2D maskTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/CeaselessVoid/CeaselessMetalShellMaskWhite").Value;
                drawData = new(maskTexture, drawPosition, maskTexture.Frame(), npc.GetAlpha(Color.White).MultiplyRGBA(colorFactor) * (1f - CeaselessVoidWhiteningEffect.WhiteningInterpolant) * opacity, npc.rotation, maskTexture.Size() * 0.5f, scale, 0, 0);
                InfernumEffectsRegistry.RealityTear2Shader.SetShaderTexture(InfernumTextureRegistry.Stars);
                InfernumEffectsRegistry.RealityTear2Shader.Apply(drawData);
                drawData.Draw(Main.spriteBatch);

                Main.spriteBatch.EnterShaderRegion();

                // Apply the crack effect if necessary.
                InfernumEffectsRegistry.CeaselessVoidCrackShader.UseShaderSpecificData(new(npc.frame.X, npc.frame.Y, npc.frame.Width, npc.frame.Height));
                InfernumEffectsRegistry.CeaselessVoidCrackShader.UseImage1("Images/Misc/Perlin");
                InfernumEffectsRegistry.CeaselessVoidCrackShader.Shader.Parameters["sheetSize"].SetValue(metalTexture.Size());
                InfernumEffectsRegistry.CeaselessVoidCrackShader.Apply();

                Main.spriteBatch.Draw(metalTexture, drawPosition, npc.frame, npc.GetAlpha(Color.White).MultiplyRGBA(colorFactor) * (1f - CeaselessVoidWhiteningEffect.WhiteningInterpolant) * opacity, npc.rotation, npc.frame.Size() * 0.5f, scale, 0, 0f);
                Main.spriteBatch.ExitShaderRegion();
            }

            if (npc.ai[0] == (int)CeaselessVoidAttackType.ChainedUp)
                DrawSeal(npc);
        }

        public static void DrawSeal(NPC npc)
        {
            float scale = Lerp(0.15f, 0.16f, Sin(Main.GlobalTimeWrappedHourly * 0.5f) * 0.5f + 0.5f) * 1.4f;
            float noiseScale = Lerp(0.4f, 0.8f, Sin(Main.GlobalTimeWrappedHourly * 0.3f) * 0.5f + 0.5f);

            Effect shieldEffect = Filters.Scene["CalamityMod:RoverDriveShield"].GetShader().Shader;
            shieldEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.15f);
            shieldEffect.Parameters["blowUpPower"].SetValue(2.5f);
            shieldEffect.Parameters["blowUpSize"].SetValue(0.5f);
            shieldEffect.Parameters["noiseScale"].SetValue(noiseScale);

            // Prepare the forcefield opacity.
            float baseShieldOpacity = 0.9f + 0.1f * Sin(Main.GlobalTimeWrappedHourly * 2f);
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

        public static void DrawChain(List<VerletSimulatedSegment> chain, Color colorFactor)
        {
            Texture2D chainTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/CeaselessVoid/CeaselessVoidChain").Value;

            // Collect chain draw positions.
            Vector2[] bezierPoints = chain.Select(x => x.position).ToArray();
            BezierCurve bezierCurve = new(bezierPoints);

            float chainScale = 0.8f;
            int totalChains = (int)(Vector2.Distance(chain.First().position, chain.Last().position) / chainTexture.Height / chainScale);
            totalChains = (int)Clamp(totalChains, 30f, 1200f);
            for (int i = 0; i < totalChains - 1; i++)
            {
                Vector2 drawPosition = bezierCurve.Evaluate(i / (float)totalChains);
                float completionRatio = i / (float)totalChains + 1f / totalChains;
                float angle = (bezierCurve.Evaluate(completionRatio) - drawPosition).ToRotation() - PiOver2;
                Color baseChainColor = Lighting.GetColor((int)drawPosition.X / 16, (int)drawPosition.Y / 16) * 2f;
                Main.EntitySpriteDraw(chainTexture, drawPosition - Main.screenPosition, null, baseChainColor.MultiplyRGBA(colorFactor), angle, chainTexture.Size() * 0.5f, chainScale, SpriteEffects.None, 0);
            }
        }

        public static void DrawChains(Color colorFactor)
        {
            if (Chains is not null)
            {
                foreach (var chain in Chains)
                    DrawChain(chain, colorFactor);
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
            ClearEntities();
            npc.ai[0] = (int)CeaselessVoidAttackType.DeathAnimation;

            npc.netUpdate = true;
            return false;
        }
        #endregion Death Effects

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Mods.InfernumMode.PetDialog.CVTip1";
            yield return n => "Mods.InfernumMode.PetDialog.CVTip2";
        }
        #endregion Tips
    }
}
