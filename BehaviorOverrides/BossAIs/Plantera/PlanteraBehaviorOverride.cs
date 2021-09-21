using CalamityMod;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Plantera
{
    public class PlanteraBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.Plantera;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw | NPCOverrideContext.NPCFindFrame;

        public const float Phase2LifeRatio = 0.9f;
        public const float Phase3LifeRatio = 0.5f;

        #region Enumerations
        internal enum PlanteraAttackState
        {
            UnripeFakeout,
            RedBlossom,
            PetalBurst,
            PoisonousGasRelease,
            TentacleSnap,
            SporeGrowth
        }
        #endregion

        #region AI

        public override bool PreAI(NPC npc)
        {
            NPC.plantBoss = npc.whoAmI;

            // Select a new target if an old one was lost.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();

            // Reset damage.
            npc.damage = npc.defDamage;

            // If none was found or it was too far away, despawn.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead ||
                !Main.player[npc.target].active || !npc.WithinRange(Main.player[npc.target].Center, 3400f))
            {
                DoDespawnEffects(npc);
                return false;
            }

            Player target = Main.player[npc.target];

            npc.dontTakeDamage = !target.ZoneCrimson && !target.ZoneCorrupt;

            int hookCount = 3;
            bool enraged = target.Center.Y < Main.worldSurface * 16f;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float phaseTransitionCounter = ref npc.ai[2];
            ref float phase2TransitionTimer = ref npc.Infernum().ExtraAI[6];
            ref float phase3TransitionTimer = ref npc.Infernum().ExtraAI[7];
            ref float hasCreatedHooksFlag = ref npc.localAI[0];
            ref float bulbHueInterpolant = ref npc.localAI[1];

            // Determine if should be invincible.
            npc.dontTakeDamage = enraged;

            // Summon weird leg tentacle hook things.
            if (Main.netMode != NetmodeID.MultiplayerClient && hasCreatedHooksFlag == 0f)
            {
                for (int i = 0; i < hookCount; i++)
                    NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y + 4, NPCID.PlanterasHook, npc.whoAmI);

                hasCreatedHooksFlag = 1f;
            }

            // Used by hooks.
            npc.ai[3] = 1.25f;

            // Handle phase transitions.
            if (phaseTransitionCounter == 0f && lifeRatio < Phase2LifeRatio)
            {
                phase2TransitionTimer = 180f;
                phaseTransitionCounter++;

                GotoNextAttackState(npc);
                DeleteHostileThings();

                npc.netUpdate = true;
            }
            if (phaseTransitionCounter == 1f && lifeRatio < Phase3LifeRatio)
            {
                phase3TransitionTimer = 180f;
                phaseTransitionCounter++;

                GotoNextAttackState(npc);
                DeleteHostileThings();

                npc.netUpdate = true;
            }

            if (phase2TransitionTimer > 0f)
            {
                DoPhase2Transition(npc, target, phase2TransitionTimer, ref bulbHueInterpolant);
                phase2TransitionTimer--;
                return false;
            }
            if (phase3TransitionTimer > 0f)
            {
                DoPhase3Transition(npc, target, phase3TransitionTimer);
                phase3TransitionTimer--;
                return false;
            }

            switch ((PlanteraAttackState)(int)attackType)
            {
                // The constitutes the first phase.
                case PlanteraAttackState.UnripeFakeout:
                    DoAttack_UnripeFakeout(npc, target, enraged, ref attackTimer);
                    break;
                case PlanteraAttackState.RedBlossom:
                    DoAttack_RedBlossom(npc, target, enraged, ref attackTimer);
                    break;
                case PlanteraAttackState.PetalBurst:
                    DoAttack_PetalBurst(npc, target, enraged, ref attackTimer);
                    break;
                case PlanteraAttackState.PoisonousGasRelease:
                    DoAttack_PoisonousGasRelease(npc, target, enraged, ref attackTimer);
                    break;
                case PlanteraAttackState.TentacleSnap:
                    DoAttack_TentacleSnap(npc, target, ref attackTimer);
                    break;
                case PlanteraAttackState.SporeGrowth:
                    DoAttack_SporeGrowth(npc, target, ref attackTimer);
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
            npc.damage = 0;

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
            npc.damage = 0;
            npc.rotation = npc.AngleFrom(target.Center) - MathHelper.PiOver2;

            if (!npc.WithinRange(target.Center, 85f))
                npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center) * 3.25f, 0.1f);
            else
                npc.velocity *= 0.9f;

            // Cause flowers to appear on blocks, walls, and near hooks.
            // They will explode into bursts of petals after some time.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 270f == 269f)
            {
                List<Vector2> flowerSpawnPositions = new List<Vector2>();

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
                    if (tile.wall > 0 && !WorldGen.SolidTile(tile))
                        flowerSpawnPositions.Add(ceneteredSpawnPosition);

                    // If a tile is a jungle grass mud tile and is active but not actuated register it as a place to spawn a flower.
                    if (tile.type == TileID.JungleGrass && tile.nactive())
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
            int seedFireRate = enraged ? 12 : 32;
            if (attackTimer % seedFireRate == seedFireRate - 1f)
            {
                Vector2 shootVelocity = npc.SafeDirectionTo(target.Center) * 12f;
                Vector2 spawnPosition = npc.Center + shootVelocity.SafeNormalize(Vector2.Zero) * 68f;
                Utilities.NewProjectileBetter(spawnPosition, shootVelocity, ProjectileID.PoisonSeedPlantera, 155, 0f);
            }
        }

        public static void DoAttack_RedBlossom(NPC npc, Player target, bool enraged, ref float attackTimer)
        {
            npc.damage = 0;
            npc.rotation = npc.AngleFrom(target.Center) - MathHelper.PiOver2;

            if (!npc.WithinRange(target.Center, 85f))
                npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center) * 4f, 0.15f);
            else
                npc.velocity *= 0.9f;

            // Cause flowers to appear on blocks, walls, and near hooks.
            // They will explode into bursts of petals after some time.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 180f == 179f)
            {
                List<Vector2> flowerSpawnPositions = new List<Vector2>();

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
                    if (tile.wall > 0 && !WorldGen.SolidTile(tile))
                        flowerSpawnPositions.Add(ceneteredSpawnPosition);

                    // If a tile is a jungle grass mud tile and is active but not actuated register it as a place to spawn a flower.
                    if (tile.type == TileID.JungleGrass && tile.nactive())
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
            int seedFireRate = enraged ? 12 : 20;
            if (attackTimer % seedFireRate == seedFireRate - 1f)
            {
                Vector2 shootVelocity = npc.SafeDirectionTo(target.Center) * 15f;
                Vector2 spawnPosition = npc.Center + shootVelocity.SafeNormalize(Vector2.Zero) * 68f;
                Utilities.NewProjectileBetter(spawnPosition, shootVelocity, ProjectileID.PoisonSeedPlantera, 155, 0f);
            }

            if (attackTimer > 600f)
                GotoNextAttackState(npc);
        }

        public static void DoAttack_PetalBurst(NPC npc, Player target, bool enraged, ref float attackTimer)
        {
            npc.damage = enraged ? 235 : 0;
            npc.rotation = npc.AngleFrom(target.Center) - MathHelper.PiOver2;

            if (!npc.WithinRange(target.Center, 85f))
            {
                float hoverSpeed = enraged ? 9f : 4f;
                npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center) * hoverSpeed, 0.15f);
            }
            else
                npc.velocity *= 0.9f;

            ref float petalReleaseCountdown = ref npc.Infernum().ExtraAI[0];
            ref float petalReleaseDelay = ref npc.Infernum().ExtraAI[1];
            ref float petalCount = ref npc.Infernum().ExtraAI[2];

            if (attackTimer == 1f)
            {
                petalReleaseDelay = 10f;
                petalCount = 1f;
            }

            petalReleaseCountdown++;

            if (petalReleaseCountdown > petalReleaseDelay && petalReleaseDelay > 0f)
            {
                Main.PlaySound(SoundID.Item17, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < (int)petalCount; i++)
                    {
                        float rotateOffset = MathHelper.Lerp(-0.47f, 0.47f, i / (petalCount - 1f));
                        Vector2 petalShootVelocity = npc.SafeDirectionTo(target.Center, -Vector2.UnitY).RotatedBy(rotateOffset) * 9.25f;
                        Vector2 spawnPosition = npc.Center + npc.SafeDirectionTo(target.Center) * 68f;
                        Utilities.NewProjectileBetter(spawnPosition, petalShootVelocity, ModContent.ProjectileType<BouncingPetal>(), 155, 0f);
                    }
                }

                // Make the next petal burst take longer to happen.
                petalReleaseDelay = (int)(petalReleaseDelay * 1.325f);
                petalReleaseCountdown = 0f;
                petalCount++;
            }

            // Go to the next attack after a burst of 8 petals has been shot.
            if (petalCount >= 8f)
                GotoNextAttackState(npc);
        }

        public static void DoAttack_PoisonousGasRelease(NPC npc, Player target, bool enraged, ref float attackTimer)
        {
            npc.damage = 0;
            npc.rotation = npc.AngleFrom(target.Center) - MathHelper.PiOver2;

            if (!npc.WithinRange(target.Center, 85f))
                npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center) * 5.7f, 0.15f);
            else
                npc.velocity *= 0.9f;

            int gasReleaseRate = enraged ? 60 : 110;
            float gasSpeed = enraged ? 7f : 3.6f;

            // Periodically release gas.
            if (attackTimer % gasReleaseRate == gasReleaseRate - 1f)
            {
                Main.PlaySound(SoundID.DD2_FlameburstTowerShot, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 spawnPosition = npc.Center + npc.SafeDirectionTo(target.Center) * 32f;
                    for (int i = 0; i < 6; i++)
                    {
                        float offsetAngle = MathHelper.Lerp(-0.78f, 0.78f, i / 5f);
                        Vector2 gasSporeVelocity = npc.SafeDirectionTo(target.Center, -Vector2.UnitY).RotatedBy(offsetAngle) * gasSpeed;
                        Utilities.NewProjectileBetter(spawnPosition, gasSporeVelocity, ModContent.ProjectileType<SporeGasPlantera>(), 160, 0f);
                    }
                }
            }

            if (attackTimer >= 385f)
                GotoNextAttackState(npc);
        }

        public static void DoAttack_TentacleSnap(NPC npc, Player target, ref float attackTimer)
        {
            npc.damage = 0;
            npc.rotation = npc.AngleFrom(target.Center) - MathHelper.PiOver2;
            npc.velocity *= 0.93f;

            int tentacleSpawnDelay = 60;
            int tentacleSummonTime = 45;
            bool canCreateTentacles = attackTimer >= tentacleSpawnDelay && attackTimer < tentacleSpawnDelay + tentacleSummonTime;
            ref float freeAreaAngle = ref npc.Infernum().ExtraAI[0];
            ref float snapCount = ref npc.Infernum().ExtraAI[1];

            if (attackTimer == 1f)
			{
                int tries = 0;

                // Ignore blocked directions if possible, to prevent the player from getting unfairly hit.
                do
                {
                    if (freeAreaAngle == 0f)
                        freeAreaAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                    else
                        freeAreaAngle = (freeAreaAngle + Main.rand.NextFloat(2.28f)) % MathHelper.TwoPi;
                    tries++;
                }
                while (!Collision.CanHit(npc.Center, 1, 1, npc.Center + freeAreaAngle.ToRotationVector2() * 200f, 1, 1) && tries < 100);

                npc.netUpdate = true;
            }

            if (canCreateTentacles)
			{
                float tentacleAngle = Utils.InverseLerp(tentacleSpawnDelay, tentacleSpawnDelay + tentacleSummonTime, attackTimer, true) * MathHelper.TwoPi;
                if (Main.netMode != NetmodeID.MultiplayerClient && Math.Abs(tentacleAngle - freeAreaAngle) > MathHelper.Pi * 0.16f)
                {
                    // Time is relative to when the tentacle was created and as such is synchronized.
                    float time = attackTimer - (tentacleSpawnDelay + tentacleSummonTime) - 40f;
                    NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCID.PlanterasTentacle, npc.whoAmI, tentacleAngle, 128f, time);
                }
			}

            if (attackTimer == tentacleSpawnDelay + 45f)
                Main.PlaySound(SoundID.Item73, target.Center);

            if (attackTimer > tentacleSpawnDelay + tentacleSummonTime + 45f && !NPC.AnyNPCs(NPCID.PlanterasTentacle))
			{
                attackTimer = 0f;
                snapCount++;
                if (snapCount >= 3f)
                    GotoNextAttackState(npc);
			}
        }

        public static void DoAttack_SporeGrowth(NPC npc, Player target, ref float attackTimer)
        {
            npc.damage = 0;
            npc.rotation = npc.AngleFrom(target.Center) - MathHelper.PiOver2;

            if (!npc.WithinRange(target.Center, 85f))
                npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center) * 3f, 0.15f);
            else
                npc.velocity *= 0.9f;

            // Cause flowers to appear on blocks and walls.
            // They will explode into bursts of petals after some time.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 125f)
            {
                List<Vector2> flowerSpawnPositions = new List<Vector2>();

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
                    if (tile.wall > 0 && !WorldGen.SolidTile(tile))
                        flowerSpawnPositions.Add(ceneteredSpawnPosition);

                    // If a tile is a jungle grass mud tile and is active but not actuated register it as a place to spawn a flower.
                    if (tile.type == TileID.JungleGrass && tile.nactive())
                        flowerSpawnPositions.Add(ceneteredSpawnPosition);

                    // Stop attempting to spawn more flowers once enough have been decided.
                    if (flowerSpawnPositions.Count > 13)
                        break;
                }

                // Create the flowers.
                foreach (Vector2 flowerSpawnPosition in flowerSpawnPositions)
                    Utilities.NewProjectileBetter(flowerSpawnPosition, Vector2.Zero, ModContent.ProjectileType<SporeFlower>(), 0, 0f);
            }

            if (attackTimer > 240f)
                GotoNextAttackState(npc);
        }

        public static void DoPhase2Transition(NPC npc, Player target, float transitionCountdown, ref float bulbHueInterpolant)
        {
            npc.velocity *= 0.95f;
            npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;
            bulbHueInterpolant = Utils.InverseLerp(105f, 30f, transitionCountdown, true);

            // Focus on the boss as it transforms.
            if (Main.LocalPlayer.WithinRange(Main.LocalPlayer.Center, 2850f))
            {
                Main.LocalPlayer.Infernum().ScreenFocusPosition = npc.Center;
                Main.LocalPlayer.Infernum().ScreenFocusInterpolant = Utils.InverseLerp(0f, 15f, transitionCountdown, true);
                Main.LocalPlayer.Infernum().ScreenFocusInterpolant *= Utils.InverseLerp(180f, 172f, transitionCountdown, true);
            }

            // Roar right before transitioning back to attacking.
            if (transitionCountdown == 20f)
                Main.PlaySound(SoundID.Roar, (int)npc.Center.X, (int)npc.Center.Y, 0, 1f, 0f);
        }

        public static void DoPhase3Transition(NPC npc, Player target, float transitionCountdown)
        {
            npc.velocity *= 0.95f;
            npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;

            // Focus on the boss as it transforms.
            if (Main.LocalPlayer.WithinRange(Main.LocalPlayer.Center, 2850f))
            {
                Main.LocalPlayer.Infernum().ScreenFocusPosition = npc.Center;
                Main.LocalPlayer.Infernum().ScreenFocusInterpolant = Utils.InverseLerp(0f, 15f, transitionCountdown, true);
                Main.LocalPlayer.Infernum().ScreenFocusInterpolant *= Utils.InverseLerp(180f, 172f, transitionCountdown, true);
            }

            // Roar right and turn into a trap plant thing before transitioning back to attacking.
            if (transitionCountdown == 20f)
            {
                Vector2 goreVelocity = (npc.rotation - MathHelper.PiOver2).ToRotationVector2().RotatedByRandom(0.54f) * Main.rand.NextFloat(10f, 16f);
                for (int i = 378; i <= 380; i++)
                    Gore.NewGore(new Vector2(npc.position.X + Main.rand.Next(npc.width), npc.position.Y + Main.rand.Next(npc.height)), goreVelocity, i, npc.scale);

                Main.PlaySound(SoundID.Roar, (int)npc.Center.X, (int)npc.Center.Y, 0, 1f, 0f);
            }
        }
        #endregion Specific Attacks

        #region AI Utility Methods

        public static void GotoNextAttackState(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float phase3TransitionTimer = npc.Infernum().ExtraAI[7];

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
                    newAttackType = PlanteraAttackState.RedBlossom;
                    break;
                case PlanteraAttackState.PoisonousGasRelease:
                    newAttackType = PlanteraAttackState.TentacleSnap;
                    break;
                case PlanteraAttackState.TentacleSnap:
                    newAttackType = PlanteraAttackState.SporeGrowth;
                    break;
                case PlanteraAttackState.SporeGrowth:
                    newAttackType = PlanteraAttackState.PoisonousGasRelease;
                    break;
            }

            if (phase3TransitionTimer > 0f)
                newAttackType = PlanteraAttackState.PoisonousGasRelease;

            npc.ai[0] = (int)newAttackType;
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
        }

        public static void DeleteHostileThings()
        {
            List<int> projectilesToDelete = new List<int>()
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
            float phase3TransitionTimer = npc.Infernum().ExtraAI[7];

            npc.frameCounter += 1D;
            if (npc.frameCounter > 6D)
            {
                npc.frameCounter = 0D;
                npc.frame.Y += frameHeight;
            }

            bool inPhase3 = npc.life < npc.lifeMax * Phase3LifeRatio;
            if (phase3TransitionTimer > 20f)
                inPhase3 = false;

            if (!inPhase3)
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

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Plantera/PlanteraTexture");
            Texture2D bulbTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Plantera/PlanteraBulbTexture");
            Color bulbColor = npc.GetAlpha(Color.Lerp(new Color(143, 215, 29), new Color(225, 104, 206), npc.localAI[1]).MultiplyRGB(lightColor));
            Color baseColor = npc.GetAlpha(lightColor);
            Vector2 drawPosition = npc.Center - Main.screenPosition + Vector2.UnitY * npc.gfxOffY;

            spriteBatch.Draw(texture, drawPosition, npc.frame, baseColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(bulbTexture, drawPosition, npc.frame, bulbColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            return false;
        }
        #endregion
    }
}
