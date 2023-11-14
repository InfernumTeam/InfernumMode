using CalamityMod;
using CalamityMod.Events;
using InfernumMode.Content.Projectiles.Pets;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Plantera
{
    public class PlanteraBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.Plantera;

        public const float Phase2LifeRatio = 0.8f;

        public const float Phase3LifeRatio = 0.35f;

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio,
            Phase3LifeRatio
        };

        #region Enumerations
        internal enum PlanteraAttackState
        {
            UnripeFakeout,
            RedBlossom,
            PetalBurst,
            PoisonousGasRelease,
            TentacleSnap,
            NettleBorders,
            RoseGrowth,
            Charge,
        }
        #endregion

        #region AI

        public static int GoreSpawnCountdownTime => 20;

        public static int Phase2TransitionDuration => 180;

        public static int PetalDamage => 160;

        public static int SporeGasDamage => 165;

        public static int NettlevineArenaSeparatorDamage => 215;

        public override bool PreAI(NPC npc)
        {
            NPC.plantBoss = npc.whoAmI;

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();

            // Reset damage.
            npc.damage = 0;

            // If none was found or it was too far away, despawn.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead ||
                !Main.player[npc.target].active || !npc.WithinRange(Main.player[npc.target].Center, 3400f))
            {
                DoDespawnEffects(npc);
                return false;
            }

            Player target = Main.player[npc.target];

            int hookCount = 3;
            bool enraged = target.Center.Y < Main.worldSurface * 16f && !BossRushEvent.BossRushActive;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool inPhase3 = lifeRatio < Phase3LifeRatio;
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float currentPhase = ref npc.ai[2];
            ref float phase2TransitionCountdown = ref npc.Infernum().ExtraAI[6];
            ref float hasCreatedHooksFlag = ref npc.localAI[0];
            ref float bulbHueInterpolant = ref npc.localAI[1];

            // Determine if should be invincible.
            npc.dontTakeDamage = enraged;
            npc.Calamity().CurrentlyEnraged = npc.dontTakeDamage;

            // Summon weird leg tentacle hook things.
            if (Main.netMode != NetmodeID.MultiplayerClient && hasCreatedHooksFlag == 0f)
            {
                for (int i = 0; i < hookCount; i++)
                    NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y + 4, NPCID.PlanterasHook, npc.whoAmI);

                hasCreatedHooksFlag = 1f;
            }

            // Used by hooks.
            npc.ai[3] = 1.25f;

            // Perform the transition to Phase 2. This involves the usage of camera effects and the removal of Plantera's bulb.
            if (currentPhase == 0f && lifeRatio < Phase2LifeRatio)
            {
                phase2TransitionCountdown = Phase2TransitionDuration;
                currentPhase++;

                SelectNextAttack(npc);
                DeleteHostileThings();

                npc.netUpdate = true;
            }
            if (currentPhase == 1f && lifeRatio < Phase3LifeRatio)
            {
                currentPhase++;
                npc.netUpdate = true;
            }

            if (phase2TransitionCountdown > 0f)
            {
                DoPhase2Transition(npc, target, phase2TransitionCountdown);
                phase2TransitionCountdown--;
                if (phase2TransitionCountdown <= 0f)
                    HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.PlanteraFinalPhaseTip");
                return false;
            }

            // Disable extra damage from the poisoned debuff. The attacks themselves hit hard enough.
            if (target.HasBuff(BuffID.Poisoned))
                target.ClearBuff(BuffID.Poisoned);

            switch ((PlanteraAttackState)(int)attackType)
            {
                // The constitutes the first phase.
                case PlanteraAttackState.UnripeFakeout:
                    npc.damage = npc.defDamage;
                    DoAttack_UnripeFakeout(npc, target, enraged, ref attackTimer);
                    break;
                case PlanteraAttackState.RedBlossom:
                    npc.damage = npc.defDamage;
                    DoAttack_RedBlossom(npc, target, inPhase3, enraged, ref attackTimer);
                    break;
                case PlanteraAttackState.PetalBurst:
                    npc.damage = npc.defDamage;
                    DoAttack_PetalBurst(npc, target, inPhase3, enraged, ref attackTimer);
                    break;
                case PlanteraAttackState.PoisonousGasRelease:
                    DoAttack_PoisonousGasRelease(npc, target, enraged, ref attackTimer);
                    break;
                case PlanteraAttackState.TentacleSnap:
                    npc.damage = npc.defDamage;
                    DoAttack_TentacleSnap(npc, target, inPhase3, ref attackTimer);
                    break;
                case PlanteraAttackState.NettleBorders:
                    DoAttack_NettleBorders(npc, target, inPhase3, ref attackTimer);
                    break;
                case PlanteraAttackState.RoseGrowth:
                    DoAttack_RoseGrowth(npc, target, inPhase3, ref attackTimer);
                    break;
                case PlanteraAttackState.Charge:
                    DoAttack_Charge(npc, target, lifeRatio, enraged, ref attackTimer);
                    break;
            }
            attackTimer++;
            return false;
        }

        #region Specific Attacks
        public static void DoDespawnEffects(NPC npc)
        {
            // Even if the player is dead it is still a valid index.
            float newSpeed = npc.velocity.Length() + 0.05f;
            Player oldTarget = Main.player[npc.target];

            npc.velocity = npc.SafeDirectionTo(oldTarget.Center) * -newSpeed;

            if (npc.timeLeft > 60)
                npc.timeLeft = 60;

            if (!npc.WithinRange(oldTarget.Center, 4000f))
            {
                npc.life = 0;
                npc.active = false;
            }
        }

        public static void DoAttack_UnripeFakeout(NPC npc, Player target, bool enraged, ref float attackTimer)
        {
            float hoverSpeed = 3.25f;
            int seedFireRate = enraged ? 12 : 32;
            float seedShootSpeed = 12f;
            if (BossRushEvent.BossRushActive)
            {
                hoverSpeed *= 4f;
                seedFireRate = 15;
                seedShootSpeed *= 2.4f;
            }

            npc.rotation = npc.AngleTo(target.Center) + PiOver2;

            if (!npc.WithinRange(target.Center, 85f))
                npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center) * hoverSpeed, hoverSpeed / 32.5f);
            else
                npc.velocity *= 0.9f;

            // Cause flowers to appear on blocks, walls, and near hooks.
            // They will explode into bursts of petals after some time.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 270f == 269f)
            {
                List<Vector2> flowerSpawnPositions = new();

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].type != NPCID.PlanterasHook || !Main.npc[i].active)
                        continue;

                    flowerSpawnPositions.Add(Main.npc[i].Center);
                }

                for (int tries = 0; tries < 10000; tries++)
                {
                    Vector2 potentialSpawnPosition = target.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(480f, 1350f);
                    Point potentialTilePosition = potentialSpawnPosition.ToTileCoordinates();
                    Vector2 ceneteredSpawnPosition = potentialTilePosition.ToWorldCoordinates();
                    Tile tile = CalamityUtils.ParanoidTileRetrieval(potentialTilePosition.X, potentialTilePosition.Y);

                    // If a tile is an active wall with no tile in fron of it register it as a place to spawn a flower.
                    if (tile.WallType > 0 && !WorldGen.SolidTile(tile) || BossRushEvent.BossRushActive)
                        flowerSpawnPositions.Add(ceneteredSpawnPosition);

                    // If a tile is a jungle grass mud tile and is active but not actuated register it as a place to spawn a flower.
                    if (tile.TileType == TileID.JungleGrass && tile.HasUnactuatedTile)
                        flowerSpawnPositions.Add(ceneteredSpawnPosition);

                    // Stop attempting to spawn more flowers once enough have been decided.
                    if (flowerSpawnPositions.Count > 4)
                        break;
                }

                // Create the flowers.
                foreach (Vector2 flowerSpawnPosition in flowerSpawnPositions)
                    Utilities.NewProjectileBetter(flowerSpawnPosition, Vector2.Zero, ModContent.ProjectileType<ExplodingFlower>(), 0, 0f);
            }

            // Release seeds.
            if (attackTimer % seedFireRate == seedFireRate - 1f)
            {
                Vector2 shootVelocity = npc.SafeDirectionTo(target.Center) * seedShootSpeed;
                Vector2 spawnPosition = npc.Center + shootVelocity.SafeNormalize(Vector2.Zero) * 68f;
                Utilities.NewProjectileBetter(spawnPosition, shootVelocity, ProjectileID.PoisonSeedPlantera, PetalDamage, 0f);
            }
        }

        public static void DoAttack_RedBlossom(NPC npc, Player target, bool inPhase3, bool enraged, ref float attackTimer)
        {
            npc.rotation = npc.AngleTo(target.Center) + PiOver2;
            float hoverSpeed = 4f;
            int seedFireRate = enraged ? 10 : 16;
            float seedShootSpeed = 15f;
            if (BossRushEvent.BossRushActive)
            {
                hoverSpeed *= 3.5f;
                seedFireRate = 11;
                seedShootSpeed *= 1.8f;
            }

            if (!npc.WithinRange(target.Center, 85f))
                npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center) * hoverSpeed, hoverSpeed / 27f);
            else
                npc.velocity *= 0.9f;

            // Cause flowers to appear on blocks, walls, and near hooks.
            // They will explode into bursts of petals after some time.
            int blossomRate = inPhase3 ? 200 : 180;
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % blossomRate == blossomRate - 1f)
            {
                int petalCount = inPhase3 ? 10 : 8;

                for (int i = 0; i < petalCount; i++)
                {
                    Vector2 spawnPosition = target.Center + (TwoPi * i / petalCount).ToRotationVector2() * 500f;
                    spawnPosition += Main.rand.NextVector2Circular(150f, 150f);
                    Vector2 centeredSpawnPosition = spawnPosition.ToTileCoordinates().ToWorldCoordinates();
                    Utilities.NewProjectileBetter(centeredSpawnPosition, Vector2.Zero, ModContent.ProjectileType<ExplodingFlower>(), 0, 0f);
                }
            }

            // Release seeds.
            if (inPhase3)
                seedFireRate -= 2;
            if (attackTimer % seedFireRate == seedFireRate - 1f)
            {
                for (int i = 0; i < 3; i++)
                {
                    float shootOffsetAngle = Lerp(-0.48f, 0.48f, i / 2f);
                    Vector2 shootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(shootOffsetAngle) * seedShootSpeed;
                    Vector2 spawnPosition = npc.Center + shootVelocity.SafeNormalize(Vector2.Zero) * 68f;
                    Utilities.NewProjectileBetter(spawnPosition, shootVelocity, ProjectileID.PoisonSeedPlantera, PetalDamage, 0f);
                }
            }

            if (attackTimer > 360f)
                SelectNextAttack(npc);
        }

        public static void DoAttack_PetalBurst(NPC npc, Player target, bool inPhase3, bool enraged, ref float attackTimer)
        {
            npc.rotation = npc.AngleTo(target.Center) + PiOver2;

            if (!npc.WithinRange(target.Center, 85f))
            {
                float hoverSpeed = enraged ? 9f : 4f;
                if (BossRushEvent.BossRushActive)
                    hoverSpeed = 15.5f;

                npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center) * hoverSpeed, hoverSpeed / 50f);
            }
            else
                npc.velocity *= 0.9f;

            float petalShootSpeed = 10.5f;
            ref float petalReleaseCountdown = ref npc.Infernum().ExtraAI[0];
            ref float petalReleaseDelay = ref npc.Infernum().ExtraAI[1];
            ref float petalCount = ref npc.Infernum().ExtraAI[2];

            if (BossRushEvent.BossRushActive)
                petalShootSpeed *= 1.85f;

            if (attackTimer == 1f)
            {
                petalReleaseDelay = inPhase3 ? 12f : 16f;
                petalCount = 1f;
            }

            petalReleaseCountdown++;

            if (petalReleaseCountdown > petalReleaseDelay && petalReleaseDelay > 0f)
            {
                SoundEngine.PlaySound(SoundID.Item17, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < (int)petalCount; i++)
                    {
                        float rotateOffset = Lerp(-0.33f, 0.33f, i / (petalCount - 1f));
                        Vector2 petalShootVelocity = npc.SafeDirectionTo(target.Center, -Vector2.UnitY).RotatedBy(rotateOffset) * petalShootSpeed;
                        Vector2 spawnPosition = npc.Center + npc.SafeDirectionTo(target.Center) * 68f;
                        Utilities.NewProjectileBetter(spawnPosition, petalShootVelocity, ModContent.ProjectileType<Petal>(), PetalDamage, 0f);
                    }
                }

                // Make the next petal burst take longer to happen.
                petalReleaseDelay = (int)(petalReleaseDelay * 1.4f);
                petalReleaseCountdown = 0f;
                petalCount++;
            }

            // Go to the next attack after a burst of 8 petals has been shot.
            if (petalCount >= 6f)
                SelectNextAttack(npc);
        }

        public static void DoAttack_PoisonousGasRelease(NPC npc, Player target, bool enraged, ref float attackTimer)
        {
            npc.rotation = npc.AngleTo(target.Center) + PiOver2;

            if (!npc.WithinRange(target.Center, 85f))
                npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center) * 5.7f, 0.15f);
            else
                npc.velocity *= 0.9f;

            int gasReleaseRate = enraged ? 50 : 90;

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 75f)
            {
                for (int i = 0; i < 19; i++)
                {
                    Vector2 spawnPosition = target.Center + (TwoPi * i / 24f).ToRotationVector2() * 720f;
                    Vector2 gasSporeVelocity = (target.Center - spawnPosition).SafeNormalize(Vector2.Zero) * 5f;
                    Utilities.NewProjectileBetter(spawnPosition, gasSporeVelocity, ModContent.ProjectileType<SporeGas>(), SporeGasDamage, 0f);
                }
            }

            if (attackTimer == gasReleaseRate)
                HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.PlanteraPlanteraTip");

            // Periodically release gas.
            if (attackTimer % gasReleaseRate == gasReleaseRate - 1f)
            {
                SoundEngine.PlaySound(SoundID.DD2_FlameburstTowerShot, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 spawnPosition = npc.Center + npc.SafeDirectionTo(target.Center) * 32f;
                    for (int i = 0; i < 42; i++)
                    {
                        Vector2 gasSporeVelocity;
                        do
                            gasSporeVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(7f, 41f);
                        while (gasSporeVelocity.AngleBetween(npc.SafeDirectionTo(target.Center)) < 0.23f);

                        if (enraged)
                            gasSporeVelocity *= 1.25f;
                        if (BossRushEvent.BossRushActive)
                            gasSporeVelocity *= 1.5f;

                        Utilities.NewProjectileBetter(spawnPosition, gasSporeVelocity, ModContent.ProjectileType<SporeGas>(), SporeGasDamage, 0f);
                    }
                }
            }

            if (attackTimer >= 250f)
                SelectNextAttack(npc);
        }

        public static void DoAttack_TentacleSnap(NPC npc, Player target, bool inPhase3, ref float attackTimer)
        {
            npc.rotation = npc.AngleTo(target.Center) + PiOver2;

            if (!npc.WithinRange(target.Center, 85f))
                npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center) * 1.35f, 0.1f);
            else
                npc.velocity *= 0.9f;

            int tentacleSpawnDelay = inPhase3 ? 45 : 60;
            int tentacleSummonTime = 45;
            bool canCreateTentacles = attackTimer >= tentacleSpawnDelay && attackTimer < tentacleSpawnDelay + tentacleSummonTime;
            float tentacleAngle = Utils.GetLerpValue(tentacleSpawnDelay, tentacleSpawnDelay + tentacleSummonTime, attackTimer, true) * TwoPi;
            ref float freeAreaAngle = ref npc.Infernum().ExtraAI[0];
            ref float freeAreaAngle2 = ref npc.Infernum().ExtraAI[1];
            ref float snapCount = ref npc.Infernum().ExtraAI[2];

            if (attackTimer == 1f)
            {
                int tries = 0;

                // Ignore blocked directions if possible, to prevent the player from getting unfairly hit.
                do
                {
                    if (freeAreaAngle == 0f)
                        freeAreaAngle = npc.AngleTo(target.Center) + Main.rand.NextFloatDirection() * 1.21f;
                    else
                        freeAreaAngle = (freeAreaAngle + Main.rand.NextFloat(2.28f)) % TwoPi;

                    if (freeAreaAngle < 0f)
                        freeAreaAngle += TwoPi;
                    tries++;
                }
                while (!Collision.CanHit(npc.Center, 1, 1, npc.Center + freeAreaAngle.ToRotationVector2() * 500f, 1, 1) && tries < 100);

                do
                {
                    if (freeAreaAngle2 == 0f)
                        freeAreaAngle2 = Main.rand.NextFloat(TwoPi);
                    else
                        freeAreaAngle2 = (freeAreaAngle2 + Main.rand.NextFloat(2.28f)) % TwoPi;

                    if (freeAreaAngle2 < 0f)
                        freeAreaAngle2 += TwoPi;
                    tries++;
                }
                while (!Collision.CanHit(npc.Center, 1, 1, npc.Center + freeAreaAngle2.ToRotationVector2() * 500f, 1, 1) && freeAreaAngle2.ToRotationVector2().AngleBetween(freeAreaAngle.ToRotationVector2()) < 2.26f && tries < 100);

                npc.netUpdate = true;
            }

            else if (canCreateTentacles)
            {
                // Time is relative to when the tentacle was created and as such is synchronized.
                float time = attackTimer - (tentacleSpawnDelay + tentacleSummonTime) - 85f;
                if (inPhase3)
                    time += 30f;

                if (Main.netMode != NetmodeID.MultiplayerClient && Math.Abs(tentacleAngle - freeAreaAngle) > Pi * 0.14f)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        float angularStep = TwoPi * i / tentacleSummonTime / 2f;
                        NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, NPCID.PlanterasTentacle, npc.whoAmI, tentacleAngle + angularStep, 148f, time);
                    }
                }

                if (Main.netMode != NetmodeID.MultiplayerClient && Math.Abs(tentacleAngle - freeAreaAngle2) > Pi * 0.16f && inPhase3)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        float angularStep = TwoPi * i / tentacleSummonTime / 2f;
                        NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<PlanteraPinkTentacle>(), npc.whoAmI, tentacleAngle + angularStep + 0.01f, 112f, time);
                    }
                }
            }

            if (attackTimer < tentacleSpawnDelay + tentacleSummonTime + 115f && attackTimer > 25f && tentacleAngle > freeAreaAngle)
            {
                Vector2 dustSpawnOffset = (freeAreaAngle + Main.rand.NextFloatDirection() * 0.14f).ToRotationVector2() * Main.rand.NextFloat(140f);
                Dust telegraphPuff = Dust.NewDustPerfect(npc.Center + dustSpawnOffset, 267);
                telegraphPuff.velocity = dustSpawnOffset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(4f);
                telegraphPuff.color = Color.Lime;
                telegraphPuff.scale = 1.2f;
                telegraphPuff.noGravity = true;
            }
            if (attackTimer < tentacleSpawnDelay + tentacleSummonTime + 265f && attackTimer > 25f && tentacleAngle > freeAreaAngle2 && inPhase3)
            {
                Vector2 dustSpawnOffset = (freeAreaAngle2 + Main.rand.NextFloatDirection() * 0.14f).ToRotationVector2() * Main.rand.NextFloat(90f);
                Dust telegraphPuff = Dust.NewDustPerfect(npc.Center + dustSpawnOffset, 267);
                telegraphPuff.velocity = dustSpawnOffset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(4f);
                telegraphPuff.color = Color.HotPink;
                telegraphPuff.scale = 1.2f;
                telegraphPuff.noGravity = true;
            }
            if (attackTimer == tentacleSpawnDelay + tentacleSummonTime)
                SoundEngine.PlaySound(SoundID.Item73, target.Center);

            if (attackTimer > tentacleSpawnDelay + tentacleSummonTime + 45f && !NPC.AnyNPCs(NPCID.PlanterasTentacle) && !NPC.AnyNPCs(ModContent.NPCType<PlanteraPinkTentacle>()))
            {
                attackTimer = 0f;
                snapCount++;
                if (snapCount >= 2f)
                    SelectNextAttack(npc);
            }
        }

        public static void DoAttack_NettleBorders(NPC npc, Player target, bool inPhase3, ref float attackTimer)
        {
            npc.rotation = npc.AngleTo(target.Center) + PiOver2;

            // Slow down prior to firing the bursts.
            float idealSpeed = Utils.GetLerpValue(120f, 70f, attackTimer, true) * 6f;
            if (!npc.WithinRange(target.Center, 85f) && idealSpeed > 0f)
                npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center) * idealSpeed, 0.15f);
            else
                npc.velocity *= 0.9f;

            // Release a burst of nettle vines. These do not do damage for a moment and linger, splitting the arena
            // into sections.
            if (attackTimer >= 135f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int vineCount = inPhase3 ? 6 : 4;
                    for (int i = 0; i < vineCount; i++)
                    {
                        Vector2 thornVelocity = (TwoPi * i / vineCount).ToRotationVector2() * 12f;
                        if (BossRushEvent.BossRushActive)
                            thornVelocity *= 1.5f;
                        Utilities.NewProjectileBetter(npc.Center, thornVelocity, ModContent.ProjectileType<NettlevineArenaSeparator>(), NettlevineArenaSeparatorDamage, 0f);
                    }
                }
                SelectNextAttack(npc);
            }
        }

        public static void DoAttack_RoseGrowth(NPC npc, Player target, bool inPhase3, ref float attackTimer)
        {
            npc.rotation = npc.AngleTo(target.Center) + PiOver2;

            if (!npc.WithinRange(target.Center, 85f))
                npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center) * 3f, 0.15f);
            else
                npc.velocity *= 0.9f;

            // Cause flowers to appear on blocks and walls.
            // They will explode into bursts of petals after some time.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 40f)
            {
                List<Vector2> flowerSpawnPositions = new();

                for (int tries = 0; tries < 10000; tries++)
                {
                    Vector2 spawnOffset = Vector2.One * Main.rand.NextFloat(480f, 1550f);
                    if (spawnOffset.Y > -150f)
                        spawnOffset.Y = 150f;
                    Vector2 potentialSpawnPosition = target.Center + Main.rand.NextVector2Unit() * spawnOffset;
                    Point potentialTilePosition = potentialSpawnPosition.ToTileCoordinates();
                    Vector2 ceneteredSpawnPosition = potentialTilePosition.ToWorldCoordinates();
                    Tile tile = CalamityUtils.ParanoidTileRetrieval(potentialTilePosition.X, potentialTilePosition.Y);

                    // If a tile is an active wall with no tile in fron of it register it as a place to spawn a flower.
                    if (tile.WallType > 0 && !WorldGen.SolidTile(tile) || BossRushEvent.BossRushActive)
                        flowerSpawnPositions.Add(ceneteredSpawnPosition);

                    // If a tile is a jungle grass mud tile and is active but not actuated register it as a place to spawn a flower.
                    if (tile.TileType == TileID.JungleGrass && tile.HasUnactuatedTile)
                        flowerSpawnPositions.Add(ceneteredSpawnPosition);

                    // Stop attempting to spawn more flowers once enough have been decided.
                    if (flowerSpawnPositions.Count > (inPhase3 ? 14 : 10))
                        break;
                }

                // Create the flowers.
                foreach (Vector2 flowerSpawnPosition in flowerSpawnPositions)
                    Utilities.NewProjectileBetter(flowerSpawnPosition, Vector2.Zero, ModContent.ProjectileType<ExplodingFlower>(), 0, 0f);
            }

            if (attackTimer > 70f)
                SelectNextAttack(npc);
        }

        public static void DoAttack_Charge(NPC npc, Player target, float lifeRatio, bool enraged, ref float attackTimer)
        {
            npc.damage = 170;

            int chargeSlowdownDelay = 30;
            int chargeTime = 30;
            int chargeTimer = (int)(attackTimer - 60) % (chargeTime + chargeSlowdownDelay);
            int chargeCount = 5;
            float chargeSpeed = (enraged ? 25f : 18.5f) + (1f - lifeRatio) * 3.2f;
            ref float chargeCounter = ref npc.Infernum().ExtraAI[0];

            if (BossRushEvent.BossRushActive)
                chargeSpeed *= 1.6f;

            if (attackTimer < 60f)
            {
                if (!npc.WithinRange(target.Center, 85f))
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center) * 3f, 0.15f);
                else
                    npc.velocity *= 0.9f;
                npc.rotation = npc.AngleTo(target.Center) + PiOver2;
            }

            // Slow down and try to look at the target.
            else if (chargeTimer < chargeSlowdownDelay)
            {
                float idealRotation = npc.AngleTo(target.Center) + PiOver2;
                npc.rotation = npc.rotation.AngleLerp(idealRotation, 0.1f).AngleTowards(idealRotation, 0.2f);
                npc.velocity *= 0.93f;

                if (chargeCounter > chargeCount)
                    SelectNextAttack(npc);
            }

            // Do the charge.
            else if (chargeTimer == chargeSlowdownDelay)
            {
                npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                npc.rotation = npc.velocity.ToRotation() + PiOver2;
                chargeCounter++;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        float shootOffsetAngle = Lerp(-0.45f, 0.45f, i / 4f);
                        Vector2 spawnPosition = npc.Center + npc.SafeDirectionTo(target.Center) * 32f;
                        Vector2 petalShootVelocity = npc.SafeDirectionTo(target.Center, -Vector2.UnitY).RotatedBy(shootOffsetAngle) * (chargeSpeed * 0.67f);

                        Utilities.NewProjectileBetter(spawnPosition, petalShootVelocity, ModContent.ProjectileType<Petal>(), PetalDamage, 0f);
                    }
                }

                SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown, npc.Center);

                npc.netUpdate = true;
            }

            // Accelerate after charging.
            else
                npc.velocity *= 1.008f;

            npc.ai[3] = 2.6f;
        }

        public static void DoPhase2Transition(NPC npc, Player target, float transitionCountdown)
        {
            npc.velocity *= 0.95f;
            npc.rotation = npc.AngleTo(target.Center) + PiOver2;

            // Focus on the boss as it transforms.
            if (npc.WithinRange(Main.LocalPlayer.Center, 2850f))
            {
                Main.LocalPlayer.Infernum_Camera().ScreenFocusPosition = npc.Center;
                Main.LocalPlayer.Infernum_Camera().ScreenFocusInterpolant = Utils.GetLerpValue(0f, 15f, transitionCountdown, true);
                Main.LocalPlayer.Infernum_Camera().ScreenFocusInterpolant *= Utils.GetLerpValue(Phase2TransitionDuration, Phase2TransitionDuration - 8f, transitionCountdown, true);
            }

            // Roar right and turn into a trap plant thing before transitioning back to attacking.
            if (Main.netMode != NetmodeID.Server && transitionCountdown == GoreSpawnCountdownTime)
            {
                Vector2 goreVelocity = (npc.rotation - PiOver2).ToRotationVector2().RotatedByRandom(0.54f) * Main.rand.NextFloat(10f, 16f);
                for (int i = 378; i <= 380; i++)
                    Gore.NewGore(npc.GetSource_FromAI(), new Vector2(npc.position.X + Main.rand.Next(npc.width), npc.position.Y + Main.rand.Next(npc.height)) + goreVelocity * 3f, goreVelocity, i, npc.scale);

                SoundEngine.PlaySound(SoundID.Roar, npc.Center);
            }
        }
        #endregion Specific Attacks

        #region AI Utility Methods

        public static void SelectNextAttack(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float phase2TransitionCountdown = npc.Infernum().ExtraAI[6];

            PlanteraAttackState oldAttackType = (PlanteraAttackState)(int)npc.ai[0];
            PlanteraAttackState newAttackType = oldAttackType;
            switch (oldAttackType)
            {
                case PlanteraAttackState.UnripeFakeout:
                    newAttackType = PlanteraAttackState.RedBlossom;
                    break;
                case PlanteraAttackState.RedBlossom:
                    newAttackType = PlanteraAttackState.PetalBurst;
                    break;
                case PlanteraAttackState.PetalBurst:
                    newAttackType = lifeRatio < Phase3LifeRatio ? PlanteraAttackState.RoseGrowth : PlanteraAttackState.RedBlossom;
                    break;
                case PlanteraAttackState.RoseGrowth:
                    newAttackType = PlanteraAttackState.PoisonousGasRelease;
                    break;
                case PlanteraAttackState.PoisonousGasRelease:
                    newAttackType = PlanteraAttackState.TentacleSnap;
                    break;
                case PlanteraAttackState.TentacleSnap:
                    newAttackType = PlanteraAttackState.NettleBorders;
                    break;
                case PlanteraAttackState.NettleBorders:
                    newAttackType = PlanteraAttackState.Charge;
                    break;
                case PlanteraAttackState.Charge:
                    newAttackType = lifeRatio < Phase3LifeRatio ? PlanteraAttackState.RedBlossom : PlanteraAttackState.PoisonousGasRelease;
                    break;
            }

            // Ensure that Plantera starts phase 2 off with the poisonous gas release attack.
            if (phase2TransitionCountdown > 0f)
                newAttackType = PlanteraAttackState.PoisonousGasRelease;

            npc.ai[0] = (int)newAttackType;
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
        }

        public static void DeleteHostileThings()
        {
            List<int> projectilesToDelete = new()
            {
                ProjectileID.PoisonSeedPlantera,
                ModContent.ProjectileType<ExplodingFlower>(),
                ModContent.ProjectileType<Petal>(),
                ModContent.ProjectileType<BouncingPetal>(),
            };

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (!projectilesToDelete.Contains(Main.projectile[i].type) || !Main.projectile[i].active)
                    continue;

                Main.projectile[i].active = false;
                Main.projectile[i].netUpdate = true;
            }
        }
        #endregion AI Utility Methods

        #endregion AI

        #region Drawing

        public override void FindFrame(NPC npc, int frameHeight)
        {
            float phase2TransitionCountdown = npc.Infernum().ExtraAI[6];

            npc.frameCounter += 1D;
            if (npc.frameCounter > 6D)
            {
                npc.frameCounter = 0D;
                npc.frame.Y += frameHeight;
            }

            bool inPhase2 = npc.life < npc.lifeMax * Phase2LifeRatio;
            if (phase2TransitionCountdown > GoreSpawnCountdownTime)
                inPhase2 = false;

            if (!inPhase2)
            {
                if (npc.frame.Y >= frameHeight * 4)
                    npc.frame.Y = 0;
            }
            else
            {
                if (npc.frame.Y <= frameHeight * 4)
                    npc.frame.Y = frameHeight * 4;

                if (npc.frame.Y >= frameHeight * Main.npcFrameCount[npc.type])
                    npc.frame.Y = frameHeight * 4;
            }
        }
        #endregion

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Mods.InfernumMode.PetDialog.PlanteraTip1";
            yield return n =>
            {
                if (TipsManager.ShouldUseJokeText)
                    return "Mods.InfernumMode.PetDialog.PlanteraJokeTip1";
                return string.Empty;
            };
            yield return n =>
            {
                if (TipsManager.ShouldUseJokeText)
                    return "Mods.InfernumMode.PetDialog.PlanteraJokeTip2";
                return string.Empty;
            };
        }
        #endregion Tips
    }
}
