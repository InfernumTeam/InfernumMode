using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Sounds;
using InfernumMode.BehaviorOverrides.BossAIs.Yharon;
using InfernumMode.Buffs;
using InfernumMode.OverridingSystem;
using InfernumMode.Sounds;
using InfernumMode.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using ProvidenceBoss = CalamityMod.NPCs.Providence.Providence;

namespace InfernumMode.BehaviorOverrides.BossAIs.Providence
{
    public class ProvidenceBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<ProvidenceBoss>();

        public const float Phase2LifeRatio = 0.55f;

        public const float Phase3LifeRatio = 0.25f;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw | NPCOverrideContext.NPCFindFrame;

        #region Enumerations
        public enum ProvidenceAttackType
        {
            SpawnEffect,
            MoltenBlasts,

            // These three attacks should not exist near each-other pattern-wise.
            // They all fulfill niches of needing to be careful with space and together might lead to stupid situations.
            CrystalSpikes,
            HolyBombs,
            CrystalBlades,

            AcceleratingCrystalFan,
            SinusoidalCrystalFan,
            CeilingCinders,
            CrystalRainTransformation,
            CrystalBladesWithLaser,
            FireSpearCrystalCocoon,
            HolyBlasts,
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
            ref float phase2AnimationTimer = ref npc.Infernum().ExtraAI[8];

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
            npc.dontTakeDamage = false;
            npc.Calamity().DR = 0.35f;
            npc.Infernum().Arena = arenaArea;
            if (drawState == (int)ProvidenceFrameDrawingType.CocoonState)
                npc.defense = CocoonDefense;

            drawState = (int)ProvidenceFrameDrawingType.WingFlapping;

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Give the taret infinite flight time.
            target.wingTime = target.wingTimeMax;
            target.AddBuff(ModContent.BuffType<ElysianGrace>(), 10);

            // Keep the target within the arena.
            if (!WorldSaveSystem.ProvidenceArena.IsEmpty)
            {
                if (target.position.X < arenaArea.Left)
                    target.position.X = arenaArea.Left;
                if (target.position.X + target.width > arenaArea.Right)
                    target.position.X = arenaArea.Right - target.width;

                if (target.position.Y < arenaArea.Top)
                    target.position.Y = arenaArea.Top;
                if (target.position.Y + target.height > arenaArea.Bottom)
                    target.position.Y = arenaArea.Bottom - target.width;
            }

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
                npc.Opacity = 1f;
                npc.rotation = npc.rotation.AngleTowards(0f, 0.02f);
                if (deathEffectTimer == 1f && !Main.dedServ)
                    SoundEngine.PlaySound(SoundID.DD2_DefeatScene with { Volume = 1.65f }, target.Center);
                
                // Delete all fire blenders.
                Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<HolyFireBeam>());

                deathEffectTimer++;

                // Delete remaining projectiles with a shockwave.
                if (deathEffectTimer == 96)
                {
                    int[] typesToDelete = new int[]
                    {
                        ModContent.ProjectileType<AcceleratingCrystalShard>(),
                        ModContent.ProjectileType<BouncingCrystalBlade>(),
                        ModContent.ProjectileType<CrystalPillar>(),
                        ModContent.ProjectileType<FallingCrystalShard>(),
                        ModContent.ProjectileType<HolySunExplosion>(),
                        ModContent.ProjectileType<MoltenFire>(),
                        ModContent.ProjectileType<ProfanedSpear>(),
                        ModContent.ProjectileType<HolyBlast>(),
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
                                    SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceHolyBlastShootSound, target.Center);
                                }
                                else
                                {
                                    shootType = ModContent.ProjectileType<ProvBoomDeath>();
                                    ReleaseSparkles(npc.Center, 6, 18f);
                                    SoundEngine.PlaySound(CommonCalamitySounds.FlareSound, target.Center);
                                    SoundEngine.PlaySound(HolyBlast.ImpactSound, target.Center);
                                }
                            }

                            Vector2 shootVelocity = Main.rand.NextVector2CircularEdge(7f, 7f) * Main.rand.NextFloat(0.7f, 1.3f);
                            if (Vector2.Dot(shootVelocity.SafeNormalize(Vector2.Zero), npc.SafeDirectionTo(target.Center)) < 0.5f)
                                shootVelocity *= 1.7f;

                            Utilities.NewProjectileBetter(npc.Center, shootVelocity, shootType, 0, 0f, 255);
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
                    SoundEngine.PlaySound(CommonCalamitySounds.FlareSound, target.Center);
                    SoundEngine.PlaySound(HolyBlast.ImpactSound, target.Center);
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

                return false;
            }

            bool inPhase2 = lifeRatio < Phase2LifeRatio;
            bool inPhase3 = lifeRatio < Phase3LifeRatio;
            if (inPhase2 && phase2AnimationTimer < 180f)
            {
                if (phase2AnimationTimer == 0f)
                {
                    npc.ai[2] = -1f;
                    SelectNextAttack(npc);
                }

                npc.Opacity = 1f;
                DoBehavior_EnterPhase2(npc, phase2AnimationTimer);
                phase2AnimationTimer++;
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
                    DoBehavior_CrystalSpikes(npc, inPhase2, inPhase3, arenaArea, ref attackTimer);
                    break;
                case ProvidenceAttackType.HolyBombs:
                    DoBehavior_HolyBombs(npc, target, lifeRatio, inPhase2, inPhase3, ref attackTimer);
                    break;
                case ProvidenceAttackType.AcceleratingCrystalFan:
                    DoBehavior_AcceleratingCrystalFan(npc, target, false, inPhase2, inPhase3, crystalCenter, ref attackTimer);
                    break;
                case ProvidenceAttackType.SinusoidalCrystalFan:
                    DoBehavior_AcceleratingCrystalFan(npc, target, true, inPhase2, inPhase3, crystalCenter, ref attackTimer);
                    break;
                case ProvidenceAttackType.CeilingCinders:
                    DoBehavior_CeilingCinders(npc, target, inPhase2, inPhase3, arenaArea, ref attackTimer);
                    break;
                case ProvidenceAttackType.CrystalRainTransformation:
                    DoBehavior_CrystalRainTransformation(npc, target, lifeRatio, inPhase2, inPhase3, ref attackTimer);
                    break;
                case ProvidenceAttackType.CrystalBlades:
                    DoBehavior_CrystalBlades(npc, lifeRatio, ref attackTimer);
                    break;
                case ProvidenceAttackType.CrystalBladesWithLaser:
                    DoBehavior_CrystalBladesWithLaser(npc, target, lifeRatio, ref attackTimer);
                    break;
                case ProvidenceAttackType.FireSpearCrystalCocoon:
                    DoBehavior_FireSpearCrystalCocoon(npc, target, crystalCenter, arenaCenter, ref drawState, ref attackTimer);
                    break;
                case ProvidenceAttackType.HolyBlasts:
                    DoBehavior_HolyBlasts(npc, target, lifeRatio, ref attackTimer);
                    break;
            }
            npc.rotation = npc.velocity.X * 0.003f;
            attackTimer++;

            return false;
        }

        public static void DoBehavior_EnterPhase2(NPC npc, float phase2AnimationTimer)
        {
            // Slow down.
            npc.velocity *= 0.94f;

            // Create spikes throughout the arena at first. This will activate soon afterwards.
            if (phase2AnimationTimer == 1f)
            {
                float startY = Utilities.GetGroundPositionFrom(Main.player[npc.target].Center).Y - 50f;
                for (float i = -3650f; i < 3650f; i += 32f)
                {
                    Vector2 top = Utilities.GetGroundPositionFrom(new(Main.player[npc.target].Center.X + i, startY), new Searches.Up(9001)).Floor();
                    Vector2 bottom = Utilities.GetGroundPositionFrom(new(Main.player[npc.target].Center.X + i, startY)).Floor();

                    int topSpike = Utilities.NewProjectileBetter(top, Vector2.Zero, ModContent.ProjectileType<GroundCrystalSpike>(), 350, 0f);
                    if (Main.projectile.IndexInRange(topSpike))
                    {
                        Main.projectile[topSpike].ModProjectile<GroundCrystalSpike>().SpikeDirection = MathHelper.PiOver2;
                        Main.projectile[topSpike].netUpdate = true;
                    }

                    if (!Collision.SolidCollision(bottom - new Vector2(1f, 10f), 20, 2))
                    {
                        int bottomSpike = Utilities.NewProjectileBetter(bottom, Vector2.Zero, ModContent.ProjectileType<GroundCrystalSpike>(), 350, 0f);
                        if (Main.projectile.IndexInRange(bottomSpike))
                        {
                            Main.projectile[bottomSpike].ModProjectile<GroundCrystalSpike>().SpikeDirection = -MathHelper.PiOver2;
                            Main.projectile[bottomSpike].netUpdate = true;
                        }
                    }
                }
            }

            // Create a rumble effect.
            if (phase2AnimationTimer > 30f && phase2AnimationTimer < 75f && Main.LocalPlayer.WithinRange(npc.Center, 5000f))
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = 10f;

            // Make all spike traps release their spears.
            if (phase2AnimationTimer == 75f)
            {
                SoundEngine.PlaySound(CommonCalamitySounds.MeatySlashSound);
                foreach (Projectile spear in Utilities.AllProjectilesByID(ModContent.ProjectileType<GroundCrystalSpike>()))
                {
                    spear.ModProjectile<GroundCrystalSpike>().SpikesShouldExtendOutward = true;
                    spear.netUpdate = true;
                }
                npc.ai[2] = 7f;
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_SpawnEffects(NPC npc, Player target, ref float wasSummonedAtNight, ref float attackTimer)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 10f)
            {
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center - Vector2.UnitY * 80f, Vector2.Zero, ModContent.ProjectileType<HolyAura>(), 0, 0f, Main.myPlayer);
                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceHolyRaySound, npc.Center);
            }

            // Fade in.
            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.1f, 0f, 1f);

            // Disable damage.
            npc.dontTakeDamage = true;

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

                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceHolyBlastShootSound, target.Center);
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
            int blastShootCount = 12;
            int totalBlobsFromBlasts = 8;
            int blastShootRate = 27;
            float moltenBlastSpeed = MathHelper.Lerp(14f, 20f, 1f - lifeRatio);

            if (!Main.dayTime)
            {
                blastShootCount += 4;
                totalBlobsFromBlasts += 4;
                blastShootRate -= 13;
            }

            ref float blastShootCounter = ref npc.Infernum().ExtraAI[1];

            if (blastShootCounter >= blastShootCount)
                npc.velocity *= 0.96f;
            else
                DoVanillaFlightMovement(npc, target, true, ref npc.Infernum().ExtraAI[0]);

            // Release molten blobs.
            if (attackTimer >= blastShootRate && !npc.WithinRange(target.Center, 350f))
            {
                if (blastShootCounter >= blastShootCount)
                {
                    SelectNextAttack(npc);
                    return;
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int blastDamage = Main.dayTime ? 225 : 350;
                    Vector2 moltenBlastSpawnPosition = npc.Center + npc.velocity * 7f;
                    Vector2 moltenBlastVelocity = npc.SafeDirectionTo(target.Center) * moltenBlastSpeed;
                    int blast = Utilities.NewProjectileBetter(moltenBlastSpawnPosition, moltenBlastVelocity, ModContent.ProjectileType<MoltenBlast>(), blastDamage, 0f);
                    if (Main.projectile.IndexInRange(blast))
                        Main.projectile[blast].ai[0] = totalBlobsFromBlasts;

                    attackTimer = 0f;
                    blastShootCounter++;
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoBehavior_CrystalSpikes(NPC npc, bool inPhase2, bool inPhase3, Rectangle arenaArea, ref float attackTimer)
        {
            SelectNextAttack(npc);
            int spikeCreationDelay = 110;
            int spikeCreationRate = 45;
            int spikeCount = 3;
            float offsetPerSpike = 150f;

            if (inPhase2)
            {
                spikeCreationRate -= 8;
                spikeCount++;
            }

            if (inPhase3)
            {
                spikeCreationRate -= 8;
                offsetPerSpike -= 15f;
            }

            if (!Main.dayTime)
            {
                spikeCreationRate -= 7;
                spikeCount = 3;
                offsetPerSpike -= 25f;
            }

            ref float spikeCounter = ref npc.Infernum().ExtraAI[0];

            // Gain extra DR.
            npc.Calamity().DR = 0.8f;

            // Delete homing fire.
            Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<HolyFire2>());

            // Release spikes.
            if (attackTimer >= spikeCreationDelay && (attackTimer - spikeCreationDelay) % spikeCreationRate == 0f)
            {
                int spikeDamage = Main.dayTime ? 225 : 350;

                // Upward spikes.
                if (spikeCounter % 2f == 0f)
                {
                    float xOffset = Main.rand.NextFloat(-35f, 35f);
                    for (float x = arenaArea.Left; x < arenaArea.Right; x += offsetPerSpike)
                    {
                        if (MathHelper.Distance(npc.Center.X, x) > 4200f)
                            continue;

                        Vector2 crystalPosition = new(x + xOffset, arenaArea.Center.Y);
                        Utilities.NewProjectileBetter(crystalPosition, Vector2.UnitY * -0.01f, ModContent.ProjectileType<CrystalPillar>(), spikeDamage, 0f);
                    }
                }

                // Rightward spikes.
                else
                {
                    float yOffset = Main.rand.NextFloat(-35f, 35f);
                    for (float y = arenaArea.Top + 100f; y < arenaArea.Bottom - 80f; y += offsetPerSpike)
                    {
                        Vector2 crystalPosition = new(npc.Center.X, y + yOffset);
                        Utilities.NewProjectileBetter(crystalPosition, Vector2.UnitX * -0.01f, ModContent.ProjectileType<CrystalPillar>(), spikeDamage, 0f);
                    }
                }

                spikeCounter++;
                npc.netUpdate = true;
            }

            if (attackTimer >= spikeCreationDelay + CrystalPillar.Lifetime * spikeCount / 4 + 8)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_HolyBombs(NPC npc, Player target, float lifeRatio, bool inPhase2, bool inPhase3, ref float attackTimer)
        {
            int blastShootCount = 6;
            int boltCount = 11;
            int bombShootRate = 84;
            int explosionDelay = 120;
            float boltSpeed = 10f;
            float bombExplosionRadius = 1500f;

            if (inPhase2)
            {
                // Make explosions have a slight variance in terms of when they explode, instead of all at once.
                explosionDelay += Main.rand.Next(90);
                boltCount += 2;
                boltSpeed += 3f;
                bombExplosionRadius += 150f;
            }

            if (inPhase3)
            {
                blastShootCount++;
                bombExplosionRadius += 150f;
            }

            if (!Main.dayTime)
                bombExplosionRadius += 225f;

            ref float bombShootCounter = ref npc.Infernum().ExtraAI[1];
            ref float universalAttackTimer = ref npc.Infernum().ExtraAI[2];

            if (bombShootCounter >= blastShootCount)
                npc.velocity *= 0.96f;
            else
                DoVanillaFlightMovement(npc, target, false, ref npc.Infernum().ExtraAI[0]);

            // Release molten blobs.
            int explosionCountdown = (int)(bombShootRate * blastShootCount - universalAttackTimer) + explosionDelay;
            if (attackTimer >= bombShootRate && !npc.WithinRange(target.Center, 200f))
            {
                if (explosionCountdown >= 160f)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceHolyBlastShootSound, target.Center);
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
                            Main.projectile[bomb].timeLeft = explosionCountdown;
                        }

                        // Release molten bolts.
                        int fireBoltDamage = Main.dayTime ? 220 : 335;
                        for (int i = 0; i < boltCount; i++)
                        {
                            float offsetAngle = MathHelper.Lerp(-0.59f, 0.59f, i / (float)(boltCount - 1f));
                            Vector2 boltShootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(offsetAngle) * boltSpeed;
                            Utilities.NewProjectileBetter(npc.Center, boltShootVelocity, ModContent.ProjectileType<MoltenFire>(), fireBoltDamage, 0f);
                        }

                        attackTimer = 0f;
                        bombShootCounter++;
                        npc.netUpdate = true;
                    }
                }
            }

            if (explosionCountdown <= 0)
                SelectNextAttack(npc);

            universalAttackTimer++;
        }

        public static void DoBehavior_AcceleratingCrystalFan(NPC npc, Player target, bool useSinusoidalFan, bool inPhase2, bool inPhase3, Vector2 crystalCenter, ref float attackTimer)
        {
            int crystalFireDelay = 70;
            int crystalReleaseRate = 3;
            int crystalReleaseCount = 16;
            int crystalFanCount = 3;
            float maxFanOffsetAngle = 1.09f;
            float crystalSpeed = 9.6f;

            if (inPhase2)
            {
                maxFanOffsetAngle += 0.23f;
                crystalSpeed += 1.2f;
            }

            if (inPhase3)
            {
                crystalReleaseRate--;
                crystalFanCount--;
            }

            if (!Main.dayTime)
            {
                crystalReleaseRate--;
                maxFanOffsetAngle += 0.3f;
                crystalSpeed += 4f;
            }

            // Use less wide fan if using a sinusoidal pattern.
            if (useSinusoidalFan)
                maxFanOffsetAngle *= 0.6f;

            ref float initialDirection = ref npc.Infernum().ExtraAI[1];
            ref float crystalFanCounter = ref npc.Infernum().ExtraAI[2];

            // Make the crystals appear more quickly if using the sinusoidal variant.
            if (useSinusoidalFan)
            {
                crystalReleaseRate = 2;
                crystalReleaseCount += 8;
            }

            // Delay the attack if the target is far away.
            if (attackTimer == crystalFireDelay && !npc.WithinRange(target.Center, 650f))
            {
                npc.Center = npc.Center.MoveTowards(target.Center, 10f);
                attackTimer = crystalFireDelay - 1f;
            }

            // Release a fan of crystals.
            if (attackTimer >= crystalFireDelay)
            {
                // Slow down.
                npc.velocity *= 0.92f;

                // Recede away from the target if they're close.
                if (npc.WithinRange(target.Center, 360f))
                    npc.Center -= npc.SafeDirectionTo(target.Center, -Vector2.UnitY) * 4f;

                // Decide an initial direction angle and play a sound to accomodate the crystals.
                if (attackTimer == crystalFireDelay)
                {
                    SoundEngine.PlaySound(SoundID.Item164, npc.Center);
                    initialDirection = (target.Center - crystalCenter).ToRotation();
                    npc.netUpdate = true;
                }

                // Cast dust outward that projects the radial area where the crystals will fire.
                Vector2 leftDirection = (initialDirection - maxFanOffsetAngle).ToRotationVector2();
                Vector2 rightDirection = (initialDirection + maxFanOffsetAngle).ToRotationVector2();
                for (int i = 0; i < 4; i++)
                {
                    Vector2 fireSpawnPosition = npc.Center + leftDirection * Main.rand.NextFloat(1250f);
                    Dust fire = Dust.NewDustPerfect(fireSpawnPosition, Main.dayTime ? 222 : 221);
                    fire.scale = 1.5f;
                    fire.fadeIn = 0.4f;
                    fire.velocity = leftDirection * Main.rand.NextFloat(8f);
                    fire.noGravity = true;

                    fireSpawnPosition = npc.Center + rightDirection * Main.rand.NextFloat(1250f);
                    fire = Dust.NewDustPerfect(fireSpawnPosition, Main.dayTime ? 222 : 221);
                    fire.scale = 1.5f;
                    fire.fadeIn = 0.4f;
                    fire.velocity = rightDirection * Main.rand.NextFloat(8f);
                    fire.noGravity = true;
                }

                // Recreate crystals.
                if (attackTimer % crystalReleaseRate == crystalReleaseRate - 1f)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int crystalShardDamage = Main.dayTime ? 225 : 350;
                        float fanInterpolant = Utils.GetLerpValue(0f, crystalReleaseRate * crystalReleaseCount, attackTimer - crystalFireDelay, true);
                        float offsetAngle = MathHelper.Lerp(-maxFanOffsetAngle, maxFanOffsetAngle, fanInterpolant);
                        if (useSinusoidalFan)
                            offsetAngle = (float)Math.Sin(MathHelper.Pi * 3f * fanInterpolant) * maxFanOffsetAngle;

                        Vector2 shootVelocity = (initialDirection + offsetAngle).ToRotationVector2() * crystalSpeed;
                        Utilities.NewProjectileBetter(crystalCenter, shootVelocity, ModContent.ProjectileType<AcceleratingCrystalShard>(), crystalShardDamage, 0f);
                        int telegraph = Utilities.NewProjectileBetter(crystalCenter, shootVelocity, ModContent.ProjectileType<CrystalTelegraphLine>(), 0, 0f);
                        if (Main.projectile.IndexInRange(telegraph))
                            Main.projectile[telegraph].ai[1] = 30f;
                    }
                }

                if (attackTimer >= crystalFireDelay + crystalReleaseRate * crystalReleaseCount)
                {
                    attackTimer = 0f;
                    crystalFanCounter++;

                    if (crystalFanCounter >= crystalFanCount)
                        SelectNextAttack(npc);

                    npc.netUpdate = true;
                }
            }

            // Fly around.
            else
                DoVanillaFlightMovement(npc, target, false, ref npc.Infernum().ExtraAI[0]);
        }

        public static void DoBehavior_CeilingCinders(NPC npc, Player target, bool inPhase2, bool inPhase3, Rectangle arenaArea, ref float attackTimer)
        {
            int cinderCreationDelay = 135;
            int circularCinderCount = 15;
            int attackSwitchDelay = 180;
            float offsetPerCinder = 105f;
            float circularCinderSpeed = 7f;

            if (inPhase2)
            {
                circularCinderCount += 7;
                offsetPerCinder -= 10f;
                circularCinderSpeed += 1.25f;
            }

            if (inPhase3)
            {
                circularCinderCount += 3;
                circularCinderSpeed += 1.25f;
            }

            ref float horizontalOffset = ref npc.Infernum().ExtraAI[0];

            // Initialize the horizontal offset of the cinders.
            if (horizontalOffset == 0f)
            {
                horizontalOffset = Main.rand.NextFloatDirection() * 50f;
                npc.netUpdate = true;
            }

            // Fly to the side of the target.
            Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 400f, -260f);
            if (!npc.WithinRange(hoverDestination, 100f))
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 25f, 0.84f);

            // Create fire particles at the top of the arena as a telegraph.
            if (attackTimer < cinderCreationDelay)
            {
                for (int i = 0; i < 20; i++)
                {
                    float fireScale = Main.rand.NextFloat(0.67f, 1f);
                    Color fireColor = Color.Lerp(Color.Orange, Color.Yellow, 0.6f);
                    Vector2 fireSpawnPosition = new(Main.rand.NextFloat(arenaArea.Left + 20f, arenaArea.Right - 20f), arenaArea.Top + Main.rand.NextFloat(15f));
                    MediumMistParticle fire = new(fireSpawnPosition, Main.rand.NextVector2Circular(5f, 5f), fireColor, Color.White, fireScale, 255f);
                    GeneralParticleHandler.SpawnParticle(fire);
                }
            }

            // Create cinders that fall downward and come from Providence's center.
            else if (attackTimer == cinderCreationDelay)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceHolyBlastShootSound, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int cinderDamage = Main.dayTime ? 225 : 350;
                    for (float x = arenaArea.Left; x < arenaArea.Right; x += offsetPerCinder)
                    {
                        Vector2 cinderSpawnPosition = new(x + offsetPerCinder, arenaArea.Top);
                        Utilities.NewProjectileBetter(cinderSpawnPosition, Vector2.UnitY * 2f, ModContent.ProjectileType<HolyCinder>(), cinderDamage, 0f);
                    }

                    for (int i = 0; i < circularCinderCount; i++)
                    {
                        Vector2 cinderShootVelocity = (MathHelper.TwoPi * i / circularCinderCount).ToRotationVector2() * circularCinderSpeed;
                        int cinder = Utilities.NewProjectileBetter(npc.Center, cinderShootVelocity, ModContent.ProjectileType<HolyCinder>(), cinderDamage, 0f);
                        if (Main.projectile.IndexInRange(cinder))
                            Main.projectile[cinder].ai[0] = 45f;
                    }
                }
            }

            else if (attackTimer == cinderCreationDelay + 30f)
                SoundEngine.PlaySound(HolyBlast.ImpactSound, target.Center);

            else if (attackTimer >= cinderCreationDelay + attackSwitchDelay)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_CrystalRainTransformation(NPC npc, Player target, float lifeRatio, bool inPhase2, bool inPhase3, ref float attackTimer)
        {
            int shootDelay = 90;
            int totalCrystalBursts = (int)MathHelper.Lerp(15f, 24f, 1f - lifeRatio);
            int crystalBurstShootRate = (int)MathHelper.Lerp(36f, 24f, 1f - lifeRatio);
            int totalCrystalsPerBurst = 24;
            int transitionDelay = 120;

            if (inPhase2)
            {
                crystalBurstShootRate += 5;
                totalCrystalsPerBurst -= 2;
            }

            if (inPhase3)
                totalCrystalsPerBurst++;
            
            if (!Main.dayTime)
            {
                crystalBurstShootRate -= 6;
                totalCrystalsPerBurst += 7;
            }
            
            ref float burstTimer = ref npc.Infernum().ExtraAI[2];
            ref float burstCounter = ref npc.Infernum().ExtraAI[3];

            Vector2 destination;
            if (target.gravDir == -1f)
                destination = target.Top + Vector2.UnitY * 400f;
            else
                destination = target.Bottom - Vector2.UnitY * 400f;

            // Fade into rainbow crystal form at first.
            if (attackTimer < shootDelay)
                npc.Opacity = 1f - attackTimer / shootDelay;
            else
            {
                // Adjust the hitbox to align with the crystal.
                npc.width = 42;
                npc.height = 100;

                burstTimer++;
                npc.Opacity = 0f;

                // If movement results in the crystal being close to the player, don't shoot at all.
                // This is done to prevent cheap shots.
                bool veryCloseToPlayer = npc.WithinRange(target.Center, 180f);
                if (!veryCloseToPlayer && burstCounter < totalCrystalBursts && burstTimer >= crystalBurstShootRate)
                {
                    SoundEngine.PlaySound(SoundID.Item109, target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int crystalShardDamage = Main.dayTime ? 250 : 400;
                        float xSpeedOffset = target.velocity.X + Main.rand.NextFloat(-5f, 5f);
                        Vector2 shootPosition = npc.Center - Vector2.UnitY * 36f;
                        for (int i = 0; i < totalCrystalsPerBurst; i++)
                        {
                            Vector2 shootVelocity = new(MathHelper.Lerp(-20f, 20f, i / (float)totalCrystalsPerBurst) + xSpeedOffset, -5.75f);
                            shootVelocity.X += Main.rand.NextFloatDirection() * 0.6f;
                            Utilities.NewProjectileBetter(shootPosition, shootVelocity, ModContent.ProjectileType<FallingCrystalShard>(), crystalShardDamage, 0f);
                        }
                        burstTimer = 0f;
                        burstCounter++;
                        npc.netUpdate = true;
                    }
                }
                npc.velocity = Vector2.Zero;
                npc.Center = Vector2.Lerp(npc.Center, destination, 0.35f);
            }

            if (burstCounter >= totalCrystalBursts)
            {
                npc.Opacity = 1f;
                npc.Center = target.Center - Vector2.UnitY * 800f;

                if (burstTimer >= transitionDelay)
                {
                    // Explode violently into a burst of flames before reverting back to normal.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int explosion = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<HolySunExplosion>(), 0, 0f);
                        if (Main.projectile.IndexInRange(explosion))
                        {
                            Main.projectile[explosion].MaxUpdates = 2;
                            Main.projectile[explosion].ModProjectile<HolySunExplosion>().MaxRadius = 600f;
                        }
                    }

                    SelectNextAttack(npc);
                }
            }
        }

        public static void DoBehavior_CrystalBlades(NPC npc, float lifeRatio, ref float attackTimer)
        {
            int crystalCount = (int)MathHelper.Lerp(12f, 22f, 1f - lifeRatio);
            int bladeReleaseDelay = 90;

            // Slow down.
            npc.velocity *= 0.925f;

            // Create the blades.
            if (attackTimer == bladeReleaseDelay)
            {
                SoundEngine.PlaySound(CommonCalamitySounds.MeatySlashSound, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int bladeDamage = Main.dayTime ? 225 : 350;
                    float offsetAngle = Main.rand.NextBool() ? MathHelper.Pi / crystalCount : 0f;
                    for (int i = 0; i < crystalCount; i++)
                    {
                        Vector2 bladeVelocity = (MathHelper.TwoPi * i / crystalCount + offsetAngle).ToRotationVector2() * 10f;
                        Utilities.NewProjectileBetter(npc.Center + bladeVelocity, bladeVelocity, ModContent.ProjectileType<BouncingCrystalBlade>(), bladeDamage, 0f);
                    }
                }
                SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_CrystalBladesWithLaser(NPC npc, Player target, float lifeRatio, ref float attackTimer)
        {
            int laserShootDelay = 180;
            int bladeRelaseRate = 45;
            int laserShootTime = HolyFireBeam.Lifetime;
            float bladeSpeed = 9.6f;
            float maxLaserAngularVelocity = MathHelper.ToRadians(0.72f + (1f - lifeRatio) * 0.16f);
            
            if (!Main.dayTime)
            {
                bladeRelaseRate -= 10;
                bladeSpeed += 3f;
            }
            
            ref float laserOffsetAngle = ref npc.Infernum().ExtraAI[0];
            ref float telegraphOpacity = ref npc.Infernum().ExtraAI[1];
            ref float laserCount = ref npc.Infernum().ExtraAI[2];

            npc.velocity *= 0.9f;

            // Gain extra DR.
            npc.Calamity().DR = 0.8f;

            // Move towards the center of the arena when not firing the lasers.
            if (attackTimer < laserShootDelay - 30f)
                npc.Center = npc.Center.MoveTowards(target.Center, 7.5f);

            // Initialize the laser offset angle on the first frame.
            if (attackTimer == 1f)
            {
                laserCount = 13f;
                if (!Main.dayTime)
                    laserCount = 17f;

                laserOffsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                npc.netUpdate = true;
            }

            // Make the telegraphs fade in quickly.
            if (attackTimer < laserShootDelay)
                telegraphOpacity = MathHelper.Clamp(telegraphOpacity + 0.0325f, 0f, 1f);

            // Make the telegraphs disappear and and make the lasers move.
            else
            {
                float laserAngularVelocity = Utils.GetLerpValue(0f, 60f, attackTimer - laserShootDelay, true) * maxLaserAngularVelocity;
                laserOffsetAngle += laserAngularVelocity;
                telegraphOpacity = 0f;

                // Release crystal blades.
                if (attackTimer % bladeRelaseRate == bladeRelaseRate - 1f)
                {
                    SoundEngine.PlaySound(CommonCalamitySounds.MeatySlashSound, target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int bladeDamage = Main.dayTime ? 225 : 350;
                        Vector2 bladeVelocity = npc.SafeDirectionTo(target.Center) * bladeSpeed;
                        Utilities.NewProjectileBetter(npc.Center, bladeVelocity, ModContent.ProjectileType<BouncingCrystalBlade>(), bladeDamage, 0f);
                    }
                }
            }

            // Cast fire beams.
            if (attackTimer == laserShootDelay)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceHolyRaySound, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int beamDamage = Main.dayTime ? 375 : 600;
                    for (int i = 0; i < laserCount; i++)
                    {
                        float offsetAngleInterpolant = i / laserCount;
                        int fireBeam = Utilities.NewProjectileBetter(npc.Center, Vector2.UnitY, ModContent.ProjectileType<HolyFireBeam>(), beamDamage, 0f);
                        if (Main.projectile.IndexInRange(fireBeam))
                            Main.projectile[fireBeam].ai[1] = offsetAngleInterpolant;
                    }
                }
            }

            if (attackTimer >= laserShootDelay + laserShootTime + 20f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_FireSpearCrystalCocoon(NPC npc, Player target, Vector2 crystalCenter, Vector2 arenaCenter, ref float drawState, ref float attackTimer)
        {
            int shootDelay = 120;
            int crystalShootRate = 4;
            int spearReleaseRate = 10;
            int spearsPerBurst = 9;
            int spearBurstReleaseRate = 50;
            int attackTime = 300;
            int attackTransitionDelay = 120;
            float spearShootSpeed = 8.4f;
            float crystalShootSpeed = 5f;

            // Enter the cocoon state.
            drawState = (int)ProvidenceFrameDrawingType.CocoonState;

            // Slow down prior to attacking and drift towards the arena center.
            if (attackTimer < shootDelay)
            {
                if (attackTimer < shootDelay - 60f)
                    npc.Center = npc.Center.MoveTowards(arenaCenter, 3f);
                npc.velocity *= 0.9f;

                float fireParticleScale = Main.rand.NextFloat(1f, 1.25f);
                Color fireColor = Color.Lerp(Color.Yellow, Color.Orange, Main.rand.NextFloat());
                Vector2 fireParticleSpawnPosition = npc.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(60f, 600f);
                Vector2 fireParticleVelocity = (npc.Center - fireParticleSpawnPosition) * 0.03f;
                SquishyLightParticle chargeFire = new(fireParticleSpawnPosition, fireParticleVelocity, fireParticleScale, fireColor, 50);
                GeneralParticleHandler.SpawnParticle(chargeFire);
            }

            else if (attackTimer <= shootDelay + attackTime)
            {
                // Create an explosion to accomodate the charge effect.
                if (attackTimer == shootDelay)
                {
                    SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int explosion = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<HolySunExplosion>(), 0, 0f);
                        if (Main.projectile.IndexInRange(explosion))
                        {
                            Main.projectile[explosion].MaxUpdates = 2;
                            Main.projectile[explosion].ModProjectile<HolySunExplosion>().MaxRadius = 540f;
                        }
                    }
                }

                // Release a spiral of crystals.
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % crystalShootRate == crystalShootRate - 1f)
                {
                    int crystalShardDamage = Main.dayTime ? 225 : 375;
                    Vector2 spiralVelocity = ((attackTimer - shootDelay) * MathHelper.TwoPi / 105f).ToRotationVector2() * crystalShootSpeed;
                    Utilities.NewProjectileBetter(crystalCenter, spiralVelocity, ModContent.ProjectileType<AcceleratingCrystalShard>(), crystalShardDamage, 0f);
                    int telegraph = Utilities.NewProjectileBetter(crystalCenter, spiralVelocity.SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<CrystalTelegraphLine>(), 0, 0f);
                    if (Main.projectile.IndexInRange(telegraph))
                        Main.projectile[telegraph].ai[1] = 30f;
                }

                // Release bursts of spears.
                int spearDamage = Main.dayTime ? 225 : 375;
                if (attackTimer % spearBurstReleaseRate == spearBurstReleaseRate - 1f)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceHolyBlastShootSound, target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float offsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                        for (int i = 0; i < spearsPerBurst; i++)
                        {
                            Vector2 spearVelocity = (MathHelper.TwoPi * i / spearsPerBurst + offsetAngle).ToRotationVector2() * spearShootSpeed;
                            Utilities.NewProjectileBetter(crystalCenter, spearVelocity, ModContent.ProjectileType<ProfanedSpear>(), spearDamage, 0f);
                        }
                    }
                }

                // Release spears at the target directly.
                if (attackTimer % spearReleaseRate == spearReleaseRate - 1f)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceHolyBlastShootSound, target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 spearVelocity = (target.Center - crystalCenter).SafeNormalize(Vector2.UnitY) * spearShootSpeed * 1.75f;
                        Utilities.NewProjectileBetter(crystalCenter, spearVelocity, ModContent.ProjectileType<ProfanedSpear>(), spearDamage, 0f);
                    }
                }
            }

            if (attackTimer >= shootDelay + attackTime + attackTransitionDelay)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_HolyBlasts(NPC npc, Player target, float lifeRatio, ref float attackTimer)
        {
            int blastShootCount = 8;
            int blastShootRate = 40;
            int boltCount = 9;
            float boltSpeed = 10f;
            float holyBlastSpeed = MathHelper.Lerp(14f, 21f, 1f - lifeRatio);

            if (!Main.dayTime)
            {
                blastShootCount += 5;
                blastShootRate -= 15;
                boltCount += 5;
                holyBlastSpeed += 3f;
                boltSpeed += 4f;
            }

            ref float blastShootCounter = ref npc.Infernum().ExtraAI[1];

            if (blastShootCounter >= blastShootCount)
                npc.velocity *= 0.96f;
            else
                DoVanillaFlightMovement(npc, target, true, ref npc.Infernum().ExtraAI[0]);

            // Release holy blasts.
            if (attackTimer >= blastShootRate && !npc.WithinRange(target.Center, 400f))
            {
                if (blastShootCounter >= blastShootCount)
                {
                    SelectNextAttack(npc);
                    return;
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int fireDamage = Main.dayTime ? 235 : 375;
                    Vector2 holyBlastSpawnPosition = npc.Center + npc.velocity * 7f;
                    Vector2 holyBlastVelocity = npc.SafeDirectionTo(target.Center) * holyBlastSpeed;
                    Utilities.NewProjectileBetter(holyBlastSpawnPosition, holyBlastVelocity, ModContent.ProjectileType<HolyBlast>(), fireDamage, 0f);

                    // Release molten bolts.
                    for (int i = 0; i < boltCount; i++)
                    {
                        float offsetAngle = MathHelper.Lerp(-0.59f, 0.59f, i / (float)(boltCount - 1f));
                        Vector2 boltShootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(offsetAngle) * boltSpeed;
                        Utilities.NewProjectileBetter(npc.Center, boltShootVelocity, ModContent.ProjectileType<MoltenFire>(), fireDamage, 0f);
                    }

                    attackTimer = 0f;
                    blastShootCounter++;
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoVanillaFlightMovement(NPC npc, Player target, bool stayAwayFromTarget, ref float flightPath, float speedFactor = 1f)
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
            float acceleration = (accelerationBoost + 1.15f) * speedFactor;
            float maxFlySpeed = (speedBoost + 17f) * speedFactor;
            
            // Fly faster at night.
            if (!Main.dayTime)
            {
                maxFlySpeed *= 1.35f;
                acceleration *= 1.35f;
            }

            // Fly faster at night.
            if (!Main.dayTime)
            {
                maxFlySpeed *= 1.35f;
                acceleration *= 1.35f;
            }

            // Don't stray too far from the target.
            npc.velocity.X = MathHelper.Clamp(npc.velocity.X + flightPath * acceleration, -maxFlySpeed, maxFlySpeed);
            if (verticalDistanceFromTarget < 150f)
                npc.velocity.Y -= 0.2f;
            if (verticalDistanceFromTarget > 220f)
                npc.velocity.Y += 0.4f;

            npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y, -6f, 6f);
        }

        public static void SelectNextAttack(NPC npc)
        {
            npc.ai[2]++;
            npc.ai[1] = 0f;

            // Reset the misc ai slots.
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool inPhase2 = lifeRatio < Phase2LifeRatio;

            int attackCount = 10;
            if (inPhase2)
                attackCount += 3;

            if (npc.ai[0] == (int)ProvidenceAttackType.SpawnEffect)
                npc.ai[2] = 0f;

            switch ((int)npc.ai[2] % attackCount)
            {
                case 0:
                    npc.ai[0] = inPhase2 ? (int)ProvidenceAttackType.HolyBlasts : (int)ProvidenceAttackType.MoltenBlasts;
                    break;
                case 1:
                    npc.ai[0] = (int)ProvidenceAttackType.CeilingCinders;
                    break;
                case 2:
                    npc.ai[0] = (int)ProvidenceAttackType.SinusoidalCrystalFan;
                    break;
                case 3:
                    npc.ai[0] = (int)ProvidenceAttackType.HolyBombs;
                    break;
                case 4:
                    npc.ai[0] = (int)ProvidenceAttackType.AcceleratingCrystalFan;
                    break;
                case 5:
                    npc.ai[0] = (int)ProvidenceAttackType.HolyBombs;
                    break;
                case 6:
                    npc.ai[0] = (int)ProvidenceAttackType.CeilingCinders;
                    break;
                case 7:
                    npc.ai[0] = (int)ProvidenceAttackType.CrystalRainTransformation;
                    break;
                case 8:
                    npc.ai[0] = inPhase2 ? (int)ProvidenceAttackType.CrystalBladesWithLaser : (int)ProvidenceAttackType.CrystalBlades;
                    break;
                case 9:
                    npc.ai[0] = (int)ProvidenceAttackType.AcceleratingCrystalFan;
                    break;
                case 10:
                    npc.ai[0] = (int)ProvidenceAttackType.FireSpearCrystalCocoon;
                    break;
                case 11:
                    npc.ai[0] = (int)ProvidenceAttackType.HolyBombs;
                    break;
                case 12:
                    npc.ai[0] = (int)ProvidenceAttackType.CeilingCinders;
                    break;
            }

            int platformID = ModContent.NPCType<ProvArenaPlatform>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == platformID)
                {
                    for (int j = 0; j < 16; j++)
                    {
                        Dust light = Dust.NewDustDirect(Main.npc[i].position, Main.npc[i].width, Main.npc[i].height, 267);
                        light.color = Main.hslToRgb(Main.rand.NextFloat(), 1f, 0.9f);
                        light.scale = 1.56f;
                        light.noGravity = true;
                    }
                    Main.npc[i].active = false;
                }
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

            Vector2 crystalCenter = npc.Center + new Vector2(8f, 56f);
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

                Vector2 drawOrigin = npc.frame.Size() * 0.5f;

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
                DrawProvidenceWings(npc, Main.spriteBatch, wingTexture, wingVibrance, baseDrawPosition, frame, drawOrigin, spriteEffects);

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
            Vector2 textureOrigin = npc.frame.Size() * 0.5f;
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

            // Draw the telegraph.
            if (lifeRatio > 0.04f && attackType == ProvidenceAttackType.CrystalBladesWithLaser)
            {
                float telegraphOffsetAngle = npc.Infernum().ExtraAI[0];
                float telegraphOpacity = npc.Infernum().ExtraAI[1];
                int laserCount = (int)npc.Infernum().ExtraAI[2];

                if (telegraphOpacity <= 0f)
                    return false;

                Color telegraphColor = (Main.dayTime ? Color.Yellow : Color.Cyan) * telegraphOpacity;
                telegraphColor.A = 127;
                for (int i = 0; i < laserCount; i++)
                {
                    float telegraphRotation = MathHelper.TwoPi * i / laserCount + telegraphOffsetAngle;
                    Vector2 telegraphDirection = telegraphRotation.ToRotationVector2();
                    Vector2 start = crystalCenter;
                    Vector2 end = start + telegraphDirection * 5000f;
                    Main.spriteBatch.DrawLineBetter(start, end, telegraphColor, telegraphOpacity * 4f);
                }
            }

            return false;
        }
        #endregion
    }
}
