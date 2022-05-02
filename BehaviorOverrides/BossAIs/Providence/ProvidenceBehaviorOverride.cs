using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.NPCs;
using CalamityMod.Projectiles.Boss;
using InfernumMode.BehaviorOverrides.BossAIs.Yharon;
using InfernumMode.OverridingSystem;
using InfernumMode.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.GameContent;
using ProvidenceBoss = CalamityMod.NPCs.Providence.Providence;

namespace InfernumMode.BehaviorOverrides.BossAIs.Providence
{
    public class ProvidenceBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<ProvidenceBoss>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw | NPCOverrideContext.NPCFindFrame;

        #region Enumerations
        public enum ProvidenceAttackType
        {
            SpawnEffect,
            MoltenBlasts,

            // These two attacks should not exist near each-other pattern-wise.
            // They both fulfill niches of needing to be careful with space and together might lead to stupid situations.
            CrystalSpikes,
            HolyBombs
        }

        public enum ProvidenceFrameDrawingType
        {
            WingFlapping,
            CocoonState
        }
        #endregion

        #region AI

        public const int AuraTime = 300;
        public const int GuardianApparationTime = 600;
        public const int CocoonDefense = 620;
        public const float LifeRainbowCrystalStartRatio = 0.8f;
        public const float LifeRainbowCrystalEndRatio = 0.725f;

        public static readonly Color[] NightPalette = new Color[] { new Color(119, 232, 194), new Color(117, 201, 229), new Color(117, 93, 229) };

        public override bool PreAI(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float attackStateTimer = ref npc.ai[2];
            ref float rainbowVibrance = ref npc.Infernum().ExtraAI[5];
            ref float drawState = ref npc.localAI[0];
            ref float burnIntensity = ref npc.localAI[3];
            ref float deathEffectTimer = ref npc.Infernum().ExtraAI[6];
            ref float wasSummonedAtNight = ref npc.Infernum().ExtraAI[7];

            bool shouldDespawnAtNight = wasSummonedAtNight == 0f && !Main.dayTime && attackType != (int)ProvidenceAttackType.SpawnEffect;
            bool shouldDespawnAtDay = wasSummonedAtNight == 1f && Main.dayTime && attackType != (int)ProvidenceAttackType.SpawnEffect;
            bool shouldDespawnBecauseOfTime = shouldDespawnAtNight || shouldDespawnAtDay;

            Vector2 crystalCenter = npc.Center + new Vector2(8f, 56f);

            // Define arena variables.
            Vector2 arenaTopLeft = WorldSaveSystem.ProvidenceArena.TopLeft() * 16f + new Vector2(68f, 32f);
            Vector2 arenaBottomRight = WorldSaveSystem.ProvidenceArena.BottomRight() * 16f + new Vector2(8f, 52f);
            Vector2 arenaCenter = WorldSaveSystem.ProvidenceArena.Center() * 16f + Vector2.One * 8f;
            Rectangle arenaArea = new((int)arenaTopLeft.X, (int)arenaTopLeft.Y, (int)(arenaBottomRight.X - arenaTopLeft.X), (int)(arenaBottomRight.Y - arenaTopLeft.Y));

            // Reset various things every frame. They can be changed later as needed.
            npc.width = 600;
            npc.height = 450;
            npc.defense = 50;
            drawState = (int)ProvidenceFrameDrawingType.WingFlapping;

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Keep the target within the arena.
            if (target.position.X < arenaArea.Left)
                target.position.X = arenaArea.Left;
            if (target.position.X + target.width > arenaArea.Right)
                target.position.X = arenaArea.Right - target.width;

            if (target.position.Y < arenaArea.Top)
                target.position.Y = arenaArea.Top;
            if (target.position.Y + target.height > arenaArea.Bottom)
                target.position.Y = arenaArea.Bottom - target.width;

            // End rain.
            CalamityMod.CalamityMod.StopRain();

            // Set the global NPC index to this NPC. Used as a means of lowering the need for loops.
            CalamityGlobalNPC.holyBoss = npc.whoAmI;

            if (!target.dead && !shouldDespawnBecauseOfTime)
                npc.timeLeft = 1800;
            else
            {
                npc.velocity.Y -= 0.4f;
                if (npc.timeLeft > 90)
                    npc.timeLeft = 90;

                // Disappear if sufficiently far away from the target.
                if (!npc.WithinRange(target.Center, 1350f))
                    npc.active = false;

                return false;
            }

            // Death effects.
            if (lifeRatio < 0.04f)
            {
                if (deathEffectTimer == 1f && !Main.dedServ)
                    SoundEngine.PlaySound(SoundID.DD2_DefeatScene.WithVolume(1.65f), target.Center);

                deathEffectTimer++;

                // Delete remaining projectiles with a shockwave.
                if (deathEffectTimer == 96)
                {
                    int[] typesToDelete = new int[]
                    {
                        ModContent.ProjectileType<HolyFire2>(),
                        ModContent.ProjectileType<HolySpear>()
                    };
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        if (!Main.projectile[i].active || !typesToDelete.Contains(Main.projectile[i].type))
                            continue;
                        Main.projectile[i].Kill();
                    }
                }

                burnIntensity = Utils.GetLerpValue(0f, 45f, deathEffectTimer, true);
                npc.life = (int)MathHelper.Lerp(npc.lifeMax * 0.04f - 1f, 1f, Utils.GetLerpValue(0f, 435f, deathEffectTimer, true));
                npc.dontTakeDamage = true;
                npc.velocity *= 0.9f;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int shootRate = (int)MathHelper.Lerp(12f, 5f, Utils.GetLerpValue(0f, 250f, deathEffectTimer, true));
                    if (deathEffectTimer % shootRate == shootRate - 1 || deathEffectTimer == 92f)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            int shootType = ModContent.ProjectileType<SwirlingFire>();
                            if ((Main.rand.NextBool(150) && deathEffectTimer >= 110f) || deathEffectTimer == 92f)
                            {
                                if (deathEffectTimer >= 320f)
                                {
                                    shootType = ModContent.ProjectileType<YharonBoom>();
                                    SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Custom/ProvidenceHolyBlastShoot"), target.Center);
                                }
                                else
                                {
                                    shootType = ModContent.ProjectileType<ProvBoomDeath>();
                                    ReleaseSparkles(npc.Center, 6, 18f);
                                    SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Custom/FlareSound"), target.Center);
                                    SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Custom/ProvidenceHolyBlastImpact"), target.Center);
                                }
                            }

                            Vector2 shootVelocity = Main.rand.NextVector2CircularEdge(7f, 7f) * Main.rand.NextFloat(0.7f, 1.3f);
                            if (Vector2.Dot(shootVelocity.SafeNormalize(Vector2.Zero), npc.SafeDirectionTo(target.Center)) < 0.5f)
                                shootVelocity *= 1.7f;

                            Utilities.NewProjectileBetter(npc.Center, shootVelocity, shootType, 280, 0f, 255);
                        }
                    }
                }

                if (deathEffectTimer >= 320f && deathEffectTimer <= 360f && deathEffectTimer % 10f == 0f)
                {
                    int sparkleCount = (int)MathHelper.Lerp(10f, 30f, Main.gfxQuality);
                    int boomChance = (int)MathHelper.Lerp(8f, 3f, Main.gfxQuality);
                    if (Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool(boomChance))
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<ProvBoomDeath>(), 0, 0f);

                    ReleaseSparkles(npc.Center, sparkleCount, 18f);
                    SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Custom/FlareSound"), target.Center);
                    SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Custom/ProvidenceHolyBlastImpact"), target.Center);
                }

                if (deathEffectTimer >= 370f)
                    npc.Opacity *= 0.97f;

                if (Main.netMode != NetmodeID.MultiplayerClient && deathEffectTimer == 400f)
                {
                    ReleaseSparkles(npc.Center, 80, 22f);
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<DyingSun>(), 0, 0f, 255);
                }

                if (deathEffectTimer >= 435f)
                {
                    npc.active = false;
                    if (!target.dead)
                    {
                        npc.HitEffect();
                        npc.NPCLoot();
                    }
                    npc.netUpdate = true;
                    return false;
                }

                if (!npc.WithinRange(target.Center, 1960f))
                    target.AddBuff(ModContent.BuffType<HolyInferno>(), 2);

                return false;
            }

            // Execute attack patterns.
            switch ((ProvidenceAttackType)attackType)
            {
                case ProvidenceAttackType.SpawnEffect:
                    DoBehavior_SpawnEffects(npc, target, ref wasSummonedAtNight, ref attackTimer);
                    break;
                case ProvidenceAttackType.MoltenBlasts:
                    DoBehavior_MoltenBlasts(npc, target, lifeRatio, ref attackTimer);
                    break;
                case ProvidenceAttackType.CrystalSpikes:
                    DoBehavior_CrystalSpikes(npc, target, lifeRatio, arenaArea, ref attackTimer);
                    break;
                case ProvidenceAttackType.HolyBombs:
                    DoBehavior_HolyBombs(npc, target, lifeRatio, arenaArea, ref attackTimer);
                    break;
            }
            npc.rotation = npc.velocity.X * 0.003f;
            attackTimer++;

            return false;
        }
        public static void DoBehavior_SpawnEffects(NPC npc, Player target, ref float wasSummonedAtNight, ref float attackTimer)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 10f)
            {
                Projectile.NewProjectile(new InfernumSource(), npc.Center - Vector2.UnitY * 80f, Vector2.Zero, ModContent.ProjectileType<HolyAura>(), 0, 0f, Main.myPlayer);
                SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Custom/ProvidenceHolyRay"), npc.Center);
            }

            // Fade in.
            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.1f, 0f, 1f);

            for (int i = 0; i < 3; i++)
            {
                Color rainbowColor = Main.hslToRgb(Main.rand.NextFloat(), 0.95f, 0.5f);
                if (!Main.dayTime)
                    rainbowColor = CalamityUtils.MulticolorLerp(Main.rand.NextFloat(), NightPalette);

                Dust rainbowDust = Dust.NewDustDirect(npc.position, npc.width, npc.height, 267, 0f, 0f, 0, rainbowColor);
                rainbowDust.position = npc.Center + Main.rand.NextVector2Circular(npc.width * 2f, npc.height * 2f) + new Vector2(0f, -150f);
                rainbowDust.velocity *= Main.rand.NextFloat() * 0.8f;
                rainbowDust.noGravity = true;
                rainbowDust.fadeIn = 0.6f + Main.rand.NextFloat() * 0.7f * npc.Opacity;
                rainbowDust.velocity += Vector2.UnitY * 3f;
                rainbowDust.scale = 1.2f;

                if (rainbowDust.dustIndex != 6000)
                {
                    rainbowDust = Dust.CloneDust(rainbowDust);
                    rainbowDust.scale /= 2f;
                    rainbowDust.fadeIn *= 0.85f;
                }
            }

            // Determine if summoned at night.
            if (attackTimer == 1f)
            {
                wasSummonedAtNight = (!Main.dayTime).ToInt();
                npc.netUpdate = true;
            }

            // Create a burst of energy and push all players nearby back significantly.
            if (attackTimer >= AuraTime - 30f && attackTimer <= AuraTime - 15f && attackTimer % 3f == 2f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<YharonBoom>(), 0, 0f);

                SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Custom/ProvidenceHolyBlastShoot"), target.Center);
            }

            if (attackTimer == AuraTime - 20f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player player = Main.player[i];
                        float pushSpeed = MathHelper.Lerp(0f, 36f, Utils.GetLerpValue(2900f, 250f, npc.Distance(player.Center)));
                        player.velocity -= player.SafeDirectionTo(npc.Center) * pushSpeed;
                    }
                }
            }

            if (attackTimer >= AuraTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_MoltenBlasts(NPC npc, Player target, float lifeRatio, ref float attackTimer)
        {
            int blastShootCount = 8;
            int totalBlobsFromBlasts = 8;
            int blastShootRate = 45;
            ref float blastShootCounter = ref npc.Infernum().ExtraAI[1];

            if (blastShootCounter >= blastShootCount)
                npc.velocity *= 0.96f;
            else
                DoVanillaFlightMovement(npc, target, true, ref npc.Infernum().ExtraAI[0]);

            // Release molten blobs.
            if (attackTimer >= blastShootRate && !npc.WithinRange(target.Center, 300f))
            {
                if (blastShootCounter >= blastShootCount)
                {
                    SelectNextAttack(npc);
                    return;
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float moltenBlastSpeed = MathHelper.Lerp(14f, 20f, 1f - lifeRatio);
                    Vector2 moltenBlastSpawnPosition = npc.Center + npc.velocity * 7f;
                    Vector2 moltenBlastVelocity = npc.SafeDirectionTo(target.Center) * moltenBlastSpeed;
                    int blast = Utilities.NewProjectileBetter(moltenBlastSpawnPosition, moltenBlastVelocity, ModContent.ProjectileType<MoltenBlast>(), 225, 0f);
                    if (Main.projectile.IndexInRange(blast))
                        Main.projectile[blast].ai[0] = totalBlobsFromBlasts;

                    attackTimer = 0f;
                    blastShootCounter++;
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoBehavior_CrystalSpikes(NPC npc, Player target, float lifeRatio, Rectangle arenaArea, ref float attackTimer)
        {
            int spikeCreationDelay = 110;
            int spikeCreationRate = 50;
            int spikeCount = 3;
            float offsetPerSpike = 140f;
            ref float spikeCounter = ref npc.Infernum().ExtraAI[0];

            if (attackTimer >= spikeCreationDelay && (attackTimer - spikeCreationDelay) % spikeCreationRate == 0f)
            {
                // Upward spikes.
                if (spikeCounter % 2f == 0f)
                {
                    for (float x = arenaArea.Left; x < arenaArea.Right; x += offsetPerSpike)
                    {
                        Vector2 crystalPosition = new(x, arenaArea.Center.Y);
                        Utilities.NewProjectileBetter(crystalPosition, Vector2.UnitY * -0.01f, ModContent.ProjectileType<CrystalPillar>(), 225, 0f);
                    }
                }

                // Rightward spikes.
                else
                {
                    for (float y = arenaArea.Top + 64f; y < arenaArea.Bottom; y += offsetPerSpike)
                    {
                        Vector2 crystalPosition = new(arenaArea.Center.X, y);
                        Utilities.NewProjectileBetter(crystalPosition, Vector2.UnitX * -0.01f, ModContent.ProjectileType<CrystalPillar>(), 225, 0f);
                    }
                }

                spikeCounter++;
                npc.netUpdate = true;
            }

            if (attackTimer >= spikeCreationDelay + CrystalPillar.Lifetime * spikeCount / 4 + 8)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_HolyBombs(NPC npc, Player target, float lifeRatio, Rectangle arenaArea, ref float attackTimer)
        {
            int blastShootCount = 4;
            int boltCount = 7;
            int bombShootRate = 84;
            int explosionDelay = 220;
            float boltSpeed = 7f;
            float bombExplosionRadius = 1220f;
            ref float bombShootCounter = ref npc.Infernum().ExtraAI[1];
            ref float universalAttackTimer = ref npc.Infernum().ExtraAI[2];

            if (bombShootCounter >= blastShootCount)
                npc.velocity *= 0.96f;
            else
                DoVanillaFlightMovement(npc, target, true, ref npc.Infernum().ExtraAI[0]);

            // Release molten blobs.
            if (attackTimer >= bombShootRate && !npc.WithinRange(target.Center, 200f))
            {
                if (bombShootCounter >= blastShootCount)
                {
                    SelectNextAttack(npc);
                    return;
                }

                SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Custom/ProvidenceHolyBlastShoot"), target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // Release the holy bomb.
                    float bombSpeed = MathHelper.Lerp(14f, 20f, 1f - lifeRatio);
                    Vector2 bombSpawnPosition = npc.Center + npc.velocity * 7f;
                    Vector2 bombVelocity = npc.SafeDirectionTo(target.Center) * bombSpeed;
                    int bomb = Utilities.NewProjectileBetter(bombSpawnPosition, bombVelocity, ModContent.ProjectileType<HolyBomb>(), 0, 0f);
                    if (Main.projectile.IndexInRange(bomb))
                    {
                        Main.projectile[bomb].ai[0] = bombExplosionRadius;
                        Main.projectile[bomb].timeLeft = (int)(bombShootRate * blastShootCount - universalAttackTimer) + explosionDelay;
                    }

                    // Release molten bolts.
                    for (int i = 0; i < boltCount; i++)
                    {
                        float offsetAngle = MathHelper.Lerp(-0.59f, 0.59f, i / (float)(boltCount - 1f));
                        Vector2 boltShootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(offsetAngle) * boltSpeed;
                        Utilities.NewProjectileBetter(npc.Center, boltShootVelocity, ModContent.ProjectileType<MoltenFire>(), 220, 0f);
                    }

                    attackTimer = 0f;
                    bombShootCounter++;
                    npc.netUpdate = true;
                }
            }
            universalAttackTimer++;
        }

        public static void DoVanillaFlightMovement(NPC npc, Player target, bool stayAwayFromTarget, ref float flightPath)
        {
            // Reset the flight path direction.
            if (flightPath == 0)
            {
                flightPath = (npc.Center.X < target.Center.X).ToDirectionInt();
                npc.netUpdate = true;
            }

            float verticalDistanceFromTarget = target.position.Y - npc.Bottom.Y;
            float horizontalDistanceFromTarget = MathHelper.Distance(target.Center.X, npc.Center.X);
            float horizontalDistanceDirChangeThreshold = 800f;

            // Increase distance from target as necessary.
            if (stayAwayFromTarget)
                horizontalDistanceDirChangeThreshold += 240f;

            // Change X movement path if far enough away from target.
            if (npc.Center.X < target.Center.X && flightPath < 0 && horizontalDistanceFromTarget > horizontalDistanceDirChangeThreshold)
                flightPath = 0;
            if (npc.Center.X > target.Center.X && flightPath > 0 && horizontalDistanceFromTarget > horizontalDistanceDirChangeThreshold)
                flightPath = 0;

            // Velocity and acceleration.
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float accelerationBoost = (1f - lifeRatio) * 0.4f;
            float speedBoost = (1f - lifeRatio) * 7.5f;
            float acceleration = accelerationBoost + 1.15f;
            float maxFlySpeed = speedBoost + 17f;

            // Don't stray too far from the target.
            npc.velocity.X = MathHelper.Clamp(npc.velocity.X + flightPath * acceleration, -maxFlySpeed, maxFlySpeed);
            if (verticalDistanceFromTarget < 200f)
                npc.velocity.Y -= 0.2f;
            if (verticalDistanceFromTarget > 250f)
                npc.velocity.Y += 0.2f;

            npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y, -6f, 6f);
        }

        public static void SelectNextAttack(NPC npc)
        {
            npc.ai[2]++;
            npc.ai[1] = 0f;

            // Reset the misc ai slots.
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            switch ((int)npc.ai[2] % 3)
            {
                case 0:
                    npc.ai[0] = (int)ProvidenceAttackType.MoltenBlasts;
                    break;
                case 1:
                    npc.ai[0] = (int)ProvidenceAttackType.CrystalSpikes;
                    break;
                case 2:
                    npc.ai[0] = (int)ProvidenceAttackType.HolyBombs;
                    break;
            }

            npc.velocity = Vector2.Zero;

            // Reset the central rainbow crystal vibrance.
            npc.Infernum().ExtraAI[5] = 0f;
            npc.localAI[3] = 0f;
            npc.netUpdate = true;
        }

        public static void ReleaseSparkles(Vector2 sparkleSpawnPosition, int sparkleCount, float maxSpraySpeed)
        {
            // Prevent projectiles from spawning client-side.
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < sparkleCount; i++)
                Utilities.NewProjectileBetter(sparkleSpawnPosition, Main.rand.NextVector2Circular(maxSpraySpeed, maxSpraySpeed), ModContent.ProjectileType<YharonMajesticSparkle>(), 0, 0f);
        }
        #endregion

        #region Drawing

        public override void FindFrame(NPC npc, int frameHeight)
        {
            ref float drawState = ref npc.localAI[0];
            bool useDefenseFrames = npc.localAI[1] == 1f;
            ref float frameUsed = ref npc.localAI[2];

            if (drawState == (int)ProvidenceFrameDrawingType.CocoonState)
            {
                if (!useDefenseFrames)
                {
                    npc.frameCounter += 1.0;
                    if (npc.frameCounter > 8.0)
                    {
                        npc.frame.Y += frameHeight;
                        npc.frameCounter = 0.0;
                    }
                    if (npc.frame.Y >= frameHeight * 3)
                    {
                        npc.frame.Y = 0;
                        npc.localAI[1] = 1f;
                    }
                }
                else
                {
                    npc.frameCounter += 1.0;
                    if (npc.frameCounter > 8.0)
                    {
                        npc.frame.Y += frameHeight;
                        npc.frameCounter = 0.0;
                    }
                    if (npc.frame.Y >= frameHeight * 2)
                        npc.frame.Y = frameHeight * 2;
                }
            }
            else
            {
                if (useDefenseFrames)
                    npc.localAI[1] = 0f;

                npc.frameCounter += npc.Infernum().ExtraAI[6] > 0f ? 0.6 : 1.0;
                if (npc.frameCounter > 5.0)
                {
                    npc.frameCounter = 0.0;
                    npc.frame.Y += frameHeight;
                }
                if (npc.frame.Y >= frameHeight * 3)
                {
                    npc.frame.Y = 0;
                    frameUsed++;
                }
                if (frameUsed > 3)
                    frameUsed = 0;
            }
        }

        // Visceral rage. Debugging doesn't work for unexplained reasons due to local functions unless this external method is used.
        public static void DrawProvidenceWings(NPC npc, SpriteBatch spriteBatch, Texture2D wingTexture, float wingVibrance, Vector2 baseDrawPosition, Rectangle frame, Vector2 drawOrigin, SpriteEffects spriteEffects)
        {
            Color deathEffectColor = new(6, 6, 6, 0);
            float deathEffectInterpolant = Utils.GetLerpValue(0f, 35f, npc.Infernum().ExtraAI[6], true);

            if (Main.dayTime)
            {
                Color c = Color.Lerp(new Color(255, 120, 0, 128), deathEffectColor, deathEffectInterpolant);
                Main.spriteBatch.Draw(wingTexture, baseDrawPosition, frame, c * npc.Opacity, npc.rotation, drawOrigin, npc.scale, spriteEffects, 0f);
            }
            else
            {
                Color nightWingColor = Color.Lerp(new Color(0, 255, 191, 0), deathEffectColor, deathEffectInterpolant) * npc.Opacity;
                Main.spriteBatch.Draw(wingTexture, baseDrawPosition, frame, nightWingColor, npc.rotation, drawOrigin, npc.scale, spriteEffects, 0f);
                for (int i = 0; i < 6; i++)
                {
                    Vector2 wingOffset = (MathHelper.TwoPi * i / 6f + Main.GlobalTimeWrappedHourly * 0.72f).ToRotationVector2() * npc.Opacity * wingVibrance * 4f;
                    Main.spriteBatch.Draw(wingTexture, baseDrawPosition + wingOffset, frame, nightWingColor * 0.55f, npc.rotation, drawOrigin, npc.scale, spriteEffects, 0f);
                }
            }
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            string baseTextureString = "CalamityMod/NPCs/Providence/";
            string baseGlowTextureString = baseTextureString + "Glowmasks/";

            string getTextureString = baseTextureString + "Providence";
            string getTextureGlowString;
            string getTextureGlow2String;

            bool useDefenseFrames = npc.localAI[1] == 1f;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            ProvidenceAttackType attackType = (ProvidenceAttackType)(int)npc.ai[0];

            ref float burnIntensity = ref npc.localAI[3];

            void drawProvidenceInstance(Vector2 baseDrawPosition, int frameOffset, Color baseDrawColor)
            {
                if (npc.localAI[0] == (int)ProvidenceFrameDrawingType.CocoonState)
                {
                    if (!useDefenseFrames)
                    {
                        getTextureString = baseTextureString + "ProvidenceDefense";
                        getTextureGlowString = baseGlowTextureString + "ProvidenceDefenseGlow";
                        getTextureGlow2String = baseGlowTextureString + "ProvidenceDefenseGlow2";
                    }
                    else
                    {
                        getTextureString = baseTextureString + "ProvidenceDefenseAlt";
                        getTextureGlowString = baseGlowTextureString + "ProvidenceDefenseAltGlow";
                        getTextureGlow2String = baseGlowTextureString + "ProvidenceDefenseAltGlow2";
                    }
                }
                else
                {
                    if (npc.localAI[2] == 0f)
                    {
                        getTextureGlowString = baseGlowTextureString + "ProvidenceGlow";
                        getTextureGlow2String = baseGlowTextureString + "ProvidenceGlow2";
                    }
                    else if (npc.localAI[2] == 1f)
                    {
                        getTextureString = baseTextureString + "ProvidenceAlt";
                        getTextureGlowString = baseGlowTextureString + "ProvidenceAltGlow";
                        getTextureGlow2String = baseGlowTextureString + "ProvidenceAltGlow2";
                    }
                    else if (npc.localAI[2] == 2f)
                    {
                        getTextureString = baseTextureString + "ProvidenceAttack";
                        getTextureGlowString = baseGlowTextureString + "ProvidenceAttackGlow";
                        getTextureGlow2String = baseGlowTextureString + "ProvidenceAttackGlow2";
                    }
                    else
                    {
                        getTextureString = baseTextureString + "ProvidenceAttackAlt";
                        getTextureGlowString = baseGlowTextureString + "ProvidenceAttackAltGlow";
                        getTextureGlow2String = baseGlowTextureString + "ProvidenceAttackAltGlow2";
                    }
                }

                float wingVibrance = 1f;
                if (attackType == ProvidenceAttackType.SpawnEffect)
                    wingVibrance = npc.ai[1] / AuraTime;

                getTextureGlowString += "Night";

                Texture2D generalTexture = ModContent.Request<Texture2D>(getTextureString).Value;
                Texture2D crystalTexture = ModContent.Request<Texture2D>(getTextureGlow2String).Value;
                Texture2D wingTexture = ModContent.Request<Texture2D>(getTextureGlowString).Value;
                Texture2D fatCrystalTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Providence/ProvidenceCrystal").Value;

                SpriteEffects spriteEffects = SpriteEffects.None;
                if (npc.spriteDirection == 1)
                    spriteEffects = SpriteEffects.FlipHorizontally;

                Vector2 drawOrigin = new Vector2(TextureAssets.Npc[npc.type].Value.Width, TextureAssets.Npc[npc.type].Value.Height / Main.npcFrameCount[npc.type]) * 0.5f;

                float rainbowVibrance = npc.Infernum().ExtraAI[5];

                // Draw the crystal behind everything. It will appear if providence is herself invisible.
                applyShaderAndDoThing(() =>
                {
                    if (npc.localAI[3] > 0f)
                        return;

                    Vector2 crystalOrigin = fatCrystalTexture.Size() * 0.5f;
                    Vector2 crystalDrawPosition = npc.Center - Main.screenPosition;
                    Main.spriteBatch.Draw(fatCrystalTexture, crystalDrawPosition, null, Color.White, npc.rotation, crystalOrigin, npc.scale, spriteEffects, 0f);
                }, rainbowVibrance * 1.5f);

                int frameHeight = generalTexture.Height / 3;
                if (frameHeight <= 0)
                    frameHeight = 1;

                Rectangle frame = generalTexture.Frame(1, 3, 0, (npc.frame.Y / frameHeight + frameOffset) % 3);

                // Draw the base texture.
                baseDrawColor *= npc.Opacity;
                Main.spriteBatch.Draw(generalTexture, baseDrawPosition, frame, baseDrawColor, npc.rotation, drawOrigin, npc.scale, spriteEffects, 0f);

                // Draw the wings.
                DrawProvidenceWings(npc, spriteBatch, wingTexture, wingVibrance, baseDrawPosition, frame, drawOrigin, spriteEffects);

                // Draw the crystals. They become more and more rainbow as Providence gets closer to death.
                // This effect fades away as she burns.
                float crystalRainbowIntensity = Utils.GetLerpValue(LifeRainbowCrystalStartRatio, LifeRainbowCrystalEndRatio, lifeRatio, true);
                if (rainbowVibrance > 0.02f)
                    crystalRainbowIntensity = 0f;
                crystalRainbowIntensity *= 1f - npc.localAI[3];
                applyShaderAndDoThing(() =>
                {
                    Main.spriteBatch.Draw(crystalTexture, baseDrawPosition, frame, baseDrawColor, npc.rotation, drawOrigin, npc.scale, spriteEffects, 0f);
                }, crystalRainbowIntensity);
            }

            void applyShaderAndDoThing(Action thingToDo, float rainbowOpacity)
            {
                Main.spriteBatch.EnterShaderRegion();

                // Apply a super special shader.
                MiscShaderData gradientShader = GameShaders.Misc["Infernum:GradientWingShader"];
                gradientShader.UseImage1("Images/Misc/noise");
                gradientShader.UseOpacity(rainbowOpacity);
                gradientShader.SetShaderTexture(ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Providence/ProvidenceShaderTexture"));

                //gradientShader.Apply(null);

                thingToDo();

                Main.spriteBatch.ExitShaderRegion();
            }

            int totalProvidencesToDraw = (int)MathHelper.Lerp(1f, 30f, burnIntensity);
            Texture2D baseTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/Providence/Providence").Value;
            Vector2 textureOrigin = new(TextureAssets.Npc[npc.type].Value.Width / 2, TextureAssets.Npc[npc.type].Value.Height / Main.npcFrameCount[npc.type] / 2);
            for (int i = 0; i < totalProvidencesToDraw; i++)
            {
                float offsetAngle = MathHelper.TwoPi * i * 2f / totalProvidencesToDraw;
                float drawOffsetScalar = (float)Math.Sin(offsetAngle * 6f + Main.GlobalTimeWrappedHourly * MathHelper.Pi);
                drawOffsetScalar *= (float)Math.Pow(burnIntensity, 3f) * 36f;
                drawOffsetScalar *= MathHelper.Lerp(1f, 2f, 1f - lifeRatio);

                Vector2 drawOffset = offsetAngle.ToRotationVector2() * drawOffsetScalar;

                Vector2 drawPosition = npc.Center - Main.screenPosition;
                drawPosition -= new Vector2(baseTexture.Width, baseTexture.Height / Main.npcFrameCount[npc.type]) * npc.scale / 2f;
                drawPosition += textureOrigin * npc.scale + new Vector2(0f, 4f + npc.gfxOffY) + drawOffset;

                Color baseColor = Color.White * (MathHelper.Lerp(0.4f, 0.8f, burnIntensity) / totalProvidencesToDraw * 7f);
                baseColor.A = 0;

                baseColor = Color.Lerp(Color.White, baseColor, burnIntensity);

                drawProvidenceInstance(drawPosition, 0, baseColor);
            }

            return false;
        }
        #endregion
    }
}
