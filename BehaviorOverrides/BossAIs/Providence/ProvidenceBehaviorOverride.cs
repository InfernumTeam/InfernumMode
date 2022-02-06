using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Dusts;
using CalamityMod.NPCs;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Projectiles.Ranged;
using InfernumMode.BehaviorOverrides.BossAIs.Yharon;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

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
            Starburst,
            BootlegRadianceSpears,
            CrystalRainbowDeathray,
            BurningAir,
            SolarMeteorShower,
            CrystalRain,
            CrystalFlames,
            AttackerGuardians
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

            bool inRainbowCrystalState = lifeRatio < LifeRainbowCrystalEndRatio;
            bool phase2 = lifeRatio < 0.45f;
            bool shouldDespawnAtNight = wasSummonedAtNight == 0f && !Main.dayTime && attackType != (int)ProvidenceAttackType.SpawnEffect;
            bool shouldDespawnAtDay = wasSummonedAtNight == 1f && Main.dayTime && attackType != (int)ProvidenceAttackType.SpawnEffect;
            bool shouldDespawnBecauseOfTime = shouldDespawnAtNight || shouldDespawnAtDay;

            Vector2 crystalCenter = npc.Center + new Vector2(8f, 56f);

            // Reset various things every frame. They can be changed later as needed.
            npc.width = 600;
            npc.height = 450;
            npc.defense = 50;
            drawState = (int)ProvidenceFrameDrawingType.WingFlapping;

            npc.TargetClosest();
            Player target = Main.player[npc.target];

            // End rain.
            CalamityMod.CalamityMod.StopRain();

            // Set the global NPC index to this NPC. Used as a means of lowering the need for loops.
            CalamityGlobalNPC.holyBoss = npc.whoAmI;

            attackTimer++;

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
                    Main.PlaySound(SoundID.DD2_DefeatScene.WithVolume(1.65f), target.Center);

                deathEffectTimer++;

                // Delete laser-beams.
                if (deathEffectTimer == 3)
                {
                    int laserType = ModContent.ProjectileType<PrismRay>();
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        if (!Main.projectile[i].active || Main.projectile[i].type != laserType)
                            continue;
                        Main.projectile[i].Kill();
                    }
                }

                // Delete remaining projectiles with a shockwave.
                if (deathEffectTimer == 96)
                {
                    int[] typesToDelete = new int[]
                    {
                        ModContent.ProjectileType<AttackerApparation>(),
                        ModContent.ProjectileType<HolyFire2>(),
                        ModContent.ProjectileType<CrystalPillar>(),
                        ModContent.ProjectileType<HolySnipeSpear>(),
                        ModContent.ProjectileType<HolySpear>()
                    };
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        if (!Main.projectile[i].active || !typesToDelete.Contains(Main.projectile[i].type))
                            continue;
                        Main.projectile[i].Kill();
                    }
                }

                burnIntensity = Utils.InverseLerp(0f, 45f, deathEffectTimer, true);
                npc.life = (int)MathHelper.Lerp(npc.lifeMax * 0.04f - 1f, 1f, Utils.InverseLerp(0f, 435f, deathEffectTimer, true));
                npc.dontTakeDamage = true;
                npc.velocity *= 0.9f;

                int totalStarsPerBurst = (int)MathHelper.Lerp(8, 24, Utils.InverseLerp(45f, 300f, deathEffectTimer, true));

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (deathEffectTimer % 40f == 39f)
                    {
                        for (int i = 0; i < totalStarsPerBurst; i++)
                        {
                            Vector2 shootVelocity = (MathHelper.TwoPi * i / totalStarsPerBurst).ToRotationVector2().RotatedByRandom(0.1f) * Main.rand.NextFloat(1.5f, 3.2f);
                            int star = Utilities.NewProjectileBetter(crystalCenter, shootVelocity, ModContent.ProjectileType<GreatStar>(), 280, 0f);
                            Main.projectile[star].Size /= 1.3f;
                            Main.projectile[star].scale /= 1.3f;
                            Main.projectile[star].ai[1] = 1f;
                            Main.projectile[star].netUpdate = true;
                        }
                    }

                    int shootRate = (int)MathHelper.Lerp(12f, 5f, Utils.InverseLerp(0f, 250f, deathEffectTimer, true));
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
                                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ProvidenceHolyBlastShoot"), target.Center);
                                }
                                else
                                {
                                    shootType = ModContent.ProjectileType<ProvBoomDeath>();
                                    ReleaseSparkles(npc.Center, 6, 18f);
                                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/FlareSound"), target.Center);
                                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ProvidenceHolyBlastImpact"), target.Center);
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
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/FlareSound"), target.Center);
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ProvidenceHolyBlastImpact"), target.Center);
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

            switch ((ProvidenceAttackType)(int)attackType)
            {
                case ProvidenceAttackType.SpawnEffect:
                    DoBehavior_SpawnEffects(npc, target, phase2, inRainbowCrystalState, ref wasSummonedAtNight, ref attackTimer);
                    break;
                case ProvidenceAttackType.Starburst:
                    DoBehavior_Starburst(npc, target, crystalCenter, lifeRatio, phase2, inRainbowCrystalState, ref drawState, ref attackTimer);
                    break;
                case ProvidenceAttackType.BootlegRadianceSpears:
                    DoBehavior_BootlegRadianceSpears(npc, target, crystalCenter, lifeRatio, phase2, inRainbowCrystalState, ref attackTimer);
                    break;
                case ProvidenceAttackType.CrystalRainbowDeathray:
                    DoBehavior_CrystalRainbowDeathray(npc, target, lifeRatio, phase2, inRainbowCrystalState, ref rainbowVibrance, ref attackTimer);
                    break;
                case ProvidenceAttackType.BurningAir:
                    DoBehavior_BurningAir(npc, target, lifeRatio, phase2, inRainbowCrystalState, ref attackTimer);
                    break;
                case ProvidenceAttackType.SolarMeteorShower:
                    DoBehavior_SolarMeteor(npc, target, phase2, inRainbowCrystalState, ref attackTimer);
                    break;
                case ProvidenceAttackType.CrystalRain:
                    DoBehavior_CrystalRain(npc, target, lifeRatio, phase2, inRainbowCrystalState, ref rainbowVibrance, ref attackTimer);
                    break;
                case ProvidenceAttackType.CrystalFlames:
                    DoBehavior_CrystalFlames(npc, target, crystalCenter, lifeRatio, phase2, inRainbowCrystalState, ref drawState, ref attackTimer);
                    break;
                case ProvidenceAttackType.AttackerGuardians:
                    DoBehavior_AttackerGuardians(npc, phase2, inRainbowCrystalState, ref drawState, ref attackTimer);
                    break;
            }
            return false;
        }

        public static void DoBehavior_SpawnEffects(NPC npc, Player target, bool phase2, bool inRainbowCrystalState, ref float wasSummonedAtNight, ref float attackTimer)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 10f)
            {
                Projectile.NewProjectile(npc.Center - Vector2.UnitY * 80f, Vector2.Zero, ModContent.ProjectileType<HolyAura>(), 0, 0f, Main.myPlayer);
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ProvidenceHolyRay"), npc.Center);
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

                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ProvidenceHolyBlastShoot"), target.Center);
            }

            if (attackTimer == AuraTime - 20f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player player = Main.player[i];
                        float pushSpeed = MathHelper.Lerp(0f, 36f, Utils.InverseLerp(2900f, 250f, npc.Distance(player.Center)));
                        player.velocity -= player.SafeDirectionTo(npc.Center) * pushSpeed;
                    }
                }
            }

            if (attackTimer >= AuraTime)
                SelectNextAttack(npc, phase2, inRainbowCrystalState);
        }

        public static void DoBehavior_Starburst(NPC npc, Player target, Vector2 crystalCenter, float lifeRatio, bool phase2, bool inRainbowCrystalState, ref float drawState, ref float attackTimer)
        {
            int totalStars = 10;
            int starShootTime = 12;
            int beamWaitTime = 60;
            int totalStarsPerBurst = (int)MathHelper.Lerp(7f, 16f, 1f - lifeRatio);
            float burstSpeed = 4f;

            if (!Main.dayTime)
            {
                totalStarsPerBurst += 5;
                burstSpeed += 3f;
            }

            npc.velocity *= 0.9f;
            npc.defense = (int)MathHelper.Lerp(50, CocoonDefense, Utils.InverseLerp(0f, 40f, attackTimer, true));
            drawState = (int)ProvidenceFrameDrawingType.CocoonState;
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % starShootTime == starShootTime - 1)
            {
                if (Main.rand.NextBool(2))
                    Utilities.NewProjectileBetter(crystalCenter, npc.SafeDirectionTo(target.Center) * 12f, ModContent.ProjectileType<GreatStar>(), 280, 0f);
                for (int i = 0; i < totalStarsPerBurst; i++)
                {
                    Vector2 shootVelocity = (MathHelper.TwoPi * i / totalStarsPerBurst).ToRotationVector2() * burstSpeed;
                    int star = Utilities.NewProjectileBetter(crystalCenter, shootVelocity, ModContent.ProjectileType<GreatStar>(), 280, 0f);
                    Main.projectile[star].Size /= 1.3f;
                    Main.projectile[star].scale /= 1.3f;
                    Main.projectile[star].ai[1] = 1f;
                    Main.projectile[star].netUpdate = true;
                }
            }

            if (attackTimer >= totalStars * starShootTime + beamWaitTime)
                SelectNextAttack(npc, phase2, inRainbowCrystalState);
        }

        public static void DoBehavior_BootlegRadianceSpears(NPC npc, Player target, Vector2 crystalCenter, float lifeRatio, bool phase2, bool inRainbowCrystalState, ref float attackTimer)
        {
            int totalGroundSpears = 10;
            int totalSkySpearWaves = 5;
            int groundSpearSpawnRate = 30;
            int skySpearSpawnRate = 90;
            int skySpearCountPerWave = 15;
            int starBurstRate = -1;
            float skySpearSpacing = 125f;
            float groundSpearSpacing = 160f;

            if (phase2)
            {
                starBurstRate = 40;
                skySpearSpacing = 160f;
                groundSpearSpacing = 250f;
            }

            if (!Main.dayTime)
            {
                starBurstRate = 30;
                skySpearSpawnRate -= 32;
            }

            ref float currentSpearOffset = ref npc.Infernum().ExtraAI[1];
            ref float skySpearWaveCount = ref npc.Infernum().ExtraAI[2];
            ref float groundSpearCount = ref npc.Infernum().ExtraAI[3];
            ref float phaseSwitchDelay = ref npc.Infernum().ExtraAI[4];

            npc.velocity *= 0.965f;
            if (attackTimer == 1)
            {
                currentSpearOffset = totalGroundSpears * groundSpearSpacing / -2;
                npc.netUpdate = true;
            }

            // Summon a ground pillar.
            if (groundSpearCount < totalGroundSpears && attackTimer % groundSpearSpawnRate == groundSpearSpawnRate - 1)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 spawnPosition = target.Bottom + new Vector2(currentSpearOffset + target.velocity.X * 45f, 4f);
                    if (Math.Abs(spawnPosition.X - target.Center.X) > 200f)
                        Utilities.NewProjectileBetter(spawnPosition, Vector2.Zero, ModContent.ProjectileType<CrystalPillar>(), 300, 0f);
                }

                currentSpearOffset += groundSpearSpacing;
                groundSpearCount++;
            }

            // Release a burst of stars from the crystal position in the second phase.
            // The other attacks are made more lenient as a result.
            if (attackTimer % starBurstRate == starBurstRate - 1)
            {
                for (int i = 0; i < 11; i++)
                {
                    Vector2 shootVelocity = (MathHelper.TwoPi * i / 11f).ToRotationVector2() * 2.3f;
                    int star = Utilities.NewProjectileBetter(crystalCenter, shootVelocity, ModContent.ProjectileType<GreatStar>(), 280, 0f);
                    Main.projectile[star].Size /= 1.3f;
                    Main.projectile[star].scale /= 1.3f;
                    Main.projectile[star].ai[1] = 1f;
                    Main.projectile[star].netUpdate = true;
                }
            }

            // Summon spears from the sky, ala Radiance.
            if (skySpearWaveCount < totalSkySpearWaves && attackTimer % skySpearSpawnRate == skySpearSpawnRate - 1)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float maxOffset = skySpearSpacing * skySpearCountPerWave * 0.5f;
                    float generalOffset = Main.rand.NextFloat(skySpearSpacing);
                    float spearFallSpeed = MathHelper.Lerp(28.5f, 35f, 1f - lifeRatio);
                    for (int i = 0; i < skySpearCountPerWave; i++)
                    {
                        float spearXOffset = MathHelper.Lerp(-maxOffset, maxOffset, i / (float)skySpearCountPerWave) + generalOffset;
                        Vector2 spawnPosition = target.Top + new Vector2(spearXOffset, -535f + target.velocity.Y * 28f);
                        Utilities.NewProjectileBetter(spawnPosition, Vector2.UnitY * spearFallSpeed, ModContent.ProjectileType<HolySpear>(), 280, 0f);
                    }

                    Vector2 sideSpearSpawnPosition = target.Center + Vector2.UnitX * 800f;
                    Vector2 sideSpearVelocity = Vector2.UnitX * -33f;
                    Utilities.NewProjectileBetter(sideSpearSpawnPosition, sideSpearVelocity, ModContent.ProjectileType<ProfanedSpear>(), 280, 0f);

                    sideSpearSpawnPosition = target.Center + Vector2.UnitX * -800f;
                    sideSpearVelocity = Vector2.UnitX * 33f;
                    Utilities.NewProjectileBetter(sideSpearSpawnPosition, sideSpearVelocity, ModContent.ProjectileType<ProfanedSpear>(), 280, 0f);
                }
                skySpearWaveCount++;
            }

            if (groundSpearCount >= totalGroundSpears && skySpearWaveCount >= totalSkySpearWaves)
                phaseSwitchDelay++;

            if (phaseSwitchDelay >= 75)
                SelectNextAttack(npc, phase2, inRainbowCrystalState);
        }

        public static void DoBehavior_CrystalRainbowDeathray(NPC npc, Player target, float lifeRatio, bool phase2, bool inRainbowCrystalState, ref float rainbowVibrance, ref float attackTimer)
        {
            int redirectTime = 45;
            int fadeoutTime = 30;
            int chargeUpTime = 90;
            int postChargeDelay = 45;

            // Adjust the hitbox to align with the crystal.
            if (attackTimer >= redirectTime + fadeoutTime)
            {
                npc.width = 42;
                npc.height = 100;
            }

            // Fly above the player.
            if (attackTimer < redirectTime)
            {
                npc.velocity = Vector2.Zero;
                npc.Center = Vector2.Lerp(npc.Center, target.Center - Vector2.UnitY * 435f, 0.35f);
            }

            // Fade into just a crystal.
            else if (attackTimer <= redirectTime + fadeoutTime)
            {
                npc.Opacity = 1f - Utils.InverseLerp(redirectTime, redirectTime + fadeoutTime, attackTimer, true);
                if (attackTimer == redirectTime + fadeoutTime)
                {
                    npc.Center = target.Center + new Vector2(Main.rand.NextBool().ToDirectionInt() * 400f, -435f);
                    ReleaseSparkles(npc.Center, 80, 16f);
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ProvidenceHolyBlastShoot"), npc.Center);
                    npc.netUpdate = true;
                }
            }

            // Charge up energy.
            else if (attackTimer <= redirectTime + fadeoutTime + chargeUpTime)
            {
                float generalScale = MathHelper.Lerp(0.9f, 1.4f, Utils.InverseLerp(redirectTime + fadeoutTime, redirectTime + fadeoutTime + chargeUpTime, attackTimer, true));
                CreateChargeDust(npc.Center, MathHelper.Max(npc.width, npc.height) * 1.3f, generalScale);
                rainbowVibrance = MathHelper.Clamp(Utils.InverseLerp(redirectTime + fadeoutTime, redirectTime + fadeoutTime + chargeUpTime, attackTimer, true) + 0.4f, 0.4f, 1f);
            }

            // Push the player back and release a massive rainbow death-laser.
            else if (attackTimer == redirectTime + fadeoutTime + chargeUpTime + postChargeDelay)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<YharonBoom>(), 0, 0f);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float maxPushbackSpeed = MathHelper.Lerp(10f, 19f, 1f - lifeRatio);
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player player = Main.player[i];
                        float pushSpeed = MathHelper.Lerp(0f, maxPushbackSpeed, Utils.InverseLerp(2200f, 180f, npc.Distance(player.Center)));
                        player.velocity -= player.SafeDirectionTo(npc.Center) * pushSpeed;
                    }

                    // Release a prism death laser.
                    float laserSpeed = MathHelper.TwoPi * 1.35f / PrismRay.LaserLifetime;
                    Vector2 laserUnitDirection = npc.SafeDirectionTo(target.Center);
                    int laserXDirection = -(laserUnitDirection.X > 0).ToDirectionInt();
                    laserSpeed *= laserXDirection;

                    laserUnitDirection = laserUnitDirection.RotatedBy(-laserXDirection * MathHelper.Pi / 2.4f);

                    int laser = Utilities.NewProjectileBetter(npc.Center, laserUnitDirection, ModContent.ProjectileType<PrismRay>(), 720, 0f, 255);
                    Main.projectile[laser].ai[0] = laserSpeed;
                    Main.projectile[laser].ai[1] = Main.rand.NextFloat();
                }

                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ProvidenceHolyBlastShoot"), target.Center);
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ProvidenceHolyRay"), npc.Center);
            }

            // Idly release sparkles over time.
            if (attackTimer >= redirectTime + fadeoutTime + chargeUpTime + postChargeDelay &&
                attackTimer < redirectTime + fadeoutTime + chargeUpTime + postChargeDelay + PrismRay.LaserLifetime)
            {
                ReleaseSparkles(npc.Center, 7, 11.6f);
            }

            // Fade back in.
            if (attackTimer >= redirectTime + fadeoutTime + chargeUpTime + postChargeDelay + PrismRay.LaserLifetime &&
                attackTimer <= redirectTime + fadeoutTime + chargeUpTime + postChargeDelay + PrismRay.LaserLifetime + 30)
            {
                npc.Opacity = MathHelper.Lerp(npc.Opacity, 1f, 0.26f);
            }

            if (attackTimer >= redirectTime + fadeoutTime + chargeUpTime + postChargeDelay + PrismRay.LaserLifetime + 30)
                SelectNextAttack(npc, phase2, inRainbowCrystalState);
        }

        public static void DoBehavior_BurningAir(NPC npc, Player target, float lifeRatio, bool phase2, bool inRainbowCrystalState, ref float attackTimer)
        {
            int shootDelay = 60;
            int swirlingFireTime = 240;
            int swirlingBulletsToSpawn = 60;
            float maxFlySpeed = 22f;
            if (!Main.dayTime)
            {
                swirlingFireTime -= 40;
                swirlingBulletsToSpawn += 20;
            }

            int swirlingBulletSpawnRate = swirlingFireTime / swirlingBulletsToSpawn;

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= shootDelay && attackTimer <= swirlingFireTime &&
                attackTimer % swirlingBulletSpawnRate == swirlingBulletSpawnRate - 1)
            {
                Vector2 shootVelocity = Main.rand.NextVector2Circular(40f, 40f);
                Vector2 spawnPosition = npc.Center + shootVelocity * 40f;
                int fire = Utilities.NewProjectileBetter(spawnPosition, shootVelocity, ModContent.ProjectileType<SwirlingFire>(), 280, 0f, Main.myPlayer);
                Main.projectile[fire].ai[0] = Main.rand.NextFloat(0.014f, 0.035f) * Main.rand.NextBool().ToDirectionInt();

                // Make the swirling flames more volatile the less HP providence has.
                Main.projectile[fire].ai[0] *= MathHelper.Lerp(1f, 1.6f, 1f - lifeRatio);
            }

            // Air is burning text + Flight debuff.
            if (attackTimer == swirlingFireTime / 2)
            {
                CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.ProfanedBossText", Color.Orange);
                Main.PlaySound(SoundID.Item20, Main.LocalPlayer.position);
                Main.LocalPlayer.AddBuff(ModContent.BuffType<ExtremeGravity>(), 1200, true);
            }

            Vector2 destination = target.Center - Vector2.UnitY.RotatedBy((float)Math.Sin(attackTimer) * MathHelper.Pi / 3f) * 400f;
            npc.velocity.X += Math.Sign(destination.X - npc.Center.X) * 0.5f;
            npc.velocity.Y += Math.Sign(destination.Y - npc.Center.Y) * 0.5f;
            npc.velocity = Vector2.Clamp(npc.velocity, new Vector2(-maxFlySpeed), new Vector2(maxFlySpeed));

            if (attackTimer >= swirlingFireTime + 90f)
                SelectNextAttack(npc, phase2, inRainbowCrystalState);
        }

        public static void DoBehavior_SolarMeteor(NPC npc, Player target, bool phase2, bool inRainbowCrystalState, ref float attackTimer)
        {
            int teleportDelay = 45;
            int totalMeteorShowers = 4;
            int meteorShowerFireRate = 40;
            int totalMeteorsPerBurst = 2;

            if (!Main.dayTime)
            {
                totalMeteorShowers += 2;
                meteorShowerFireRate += 6;
                totalMeteorsPerBurst++;
            }

            ref float completedMeteorShowers = ref npc.Infernum().ExtraAI[1];

            if (attackTimer == teleportDelay)
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ProvidenceHolyBlastShoot"), npc.Center);

            if (attackTimer >= teleportDelay)
                npc.Center = Vector2.Lerp(npc.Center, target.Center - Vector2.UnitY * 440f, 0.28f);

            // Release a bunch of holy blast meteors from above Providence 
            if (Main.netMode != NetmodeID.MultiplayerClient &&
                attackTimer % meteorShowerFireRate == meteorShowerFireRate - 1 &&
                completedMeteorShowers < totalMeteorShowers)
            {
                for (int i = 0; i < totalMeteorsPerBurst; i++)
                {
                    Vector2 spawnPosition = npc.Top + new Vector2(MathHelper.Lerp(-1100f, 1100f, i / (float)(totalMeteorsPerBurst - 1f)) + target.velocity.X * 60f, -450f);
                    Vector2 showerDirection = (target.Center - spawnPosition + target.velocity * 100f).SafeNormalize(Vector2.UnitY);

                    Utilities.NewProjectileBetter(spawnPosition, showerDirection * 16f, ModContent.ProjectileType<HolyBlast>(), 280, 0f, Main.myPlayer);
                }
                completedMeteorShowers++;
                npc.netUpdate = true;
            }

            if (completedMeteorShowers >= totalMeteorShowers)
                SelectNextAttack(npc, phase2, inRainbowCrystalState);
        }

        public static void DoBehavior_CrystalRain(NPC npc, Player target, float lifeRatio, bool phase2, bool inRainbowCrystalState, ref float rainbowVibrance, ref float attackTimer)
        {
            int shootDelay = 90;
            int totalCrystalBursts = (int)MathHelper.Lerp(15f, 24f, 1f - lifeRatio);
            int crystalBurstShootRate = (int)MathHelper.Lerp(24f, 12f, 1f - lifeRatio);
            int totalCrystalsPerBurst = 20;
            int transitionDelay = 120;

            if (!Main.dayTime)
            {
                crystalBurstShootRate -= 4;
                totalCrystalsPerBurst += 7;
            }

            ref float burstTimer = ref npc.Infernum().ExtraAI[2];
            ref float burstCounter = ref npc.Infernum().ExtraAI[3];

            Vector2 destination;
            if (target.gravDir == -1f)
                destination = target.Top + Vector2.UnitY * 420f;
            else
                destination = target.Bottom - Vector2.UnitY * 420f;

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

                if (!veryCloseToPlayer &&
                    burstCounter < totalCrystalBursts &&
                    burstTimer >= crystalBurstShootRate)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float xSpeedOffset = target.velocity.X + Main.rand.NextFloat(-5f, 5f);
                        Vector2 shootPosition = npc.Center - Vector2.UnitY * 36f;
                        for (int i = 0; i < totalCrystalsPerBurst; i++)
                        {
                            float hue = (i / (float)totalCrystalsPerBurst + Main.GlobalTime) % 1f;
                            Vector2 shootVelocity = new Vector2(MathHelper.Lerp(-20f, 20f, i / (float)totalCrystalsPerBurst) + xSpeedOffset, -4f);
                            shootVelocity.X += Main.rand.NextFloatDirection() * 0.6f;
                            int crystal = Utilities.NewProjectileBetter(shootPosition, shootVelocity, ModContent.ProjectileType<RainbowCrystal>(), 280, 0f);
                            Main.projectile[crystal].ai[0] = hue;
                            Main.projectile[crystal].ai[1] = phase2.ToInt();
                        }
                        burstTimer = 0f;
                        burstCounter++;
                        npc.netUpdate = true;
                    }
                    Main.PlaySound(SoundID.Item109, target.Center);
                }
                npc.velocity = Vector2.Zero;
                npc.Center = Vector2.Lerp(npc.Center, destination, 0.35f);
            }

            rainbowVibrance = 1f - npc.Opacity;
            if (burstCounter >= totalCrystalBursts)
            {
                npc.Opacity = 1f;
                rainbowVibrance = 0f;

                npc.Center = target.Center - Vector2.UnitY * 800f;

                if (burstTimer >= transitionDelay)
                {
                    // Explode violently into a burst of flames before reverting back to normal.
                    if (!Main.dedServ)
                    {
                        for (int i = 0; i < 450; i++)
                        {
                            Dust holyFire = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(560f, 560f), (int)CalamityDusts.ProfanedFire);
                            holyFire.scale = Main.rand.NextFloat(3f, 6f);
                            holyFire.velocity = Main.rand.NextVector2Circular(16f, 16f);
                            holyFire.noGravity = true;
                        }
                    }

                    SelectNextAttack(npc, phase2, inRainbowCrystalState);
                }
            }
        }

        public static void DoBehavior_CrystalFlames(NPC npc, Player target, Vector2 crystalCenter, float lifeRatio, bool phase2, bool inRainbowCrystalState, ref float drawState, ref float attackTimer)
        {
            int shootDelay = 150;
            int crystalShootRate = 4;
            int flameShootRate = 60;

            if (lifeRatio < 0.5f)
            {
                crystalShootRate = 3;
                flameShootRate = 45;
            }

            if (!Main.dayTime)
            {
                crystalShootRate = 2;
                flameShootRate = 32;
            }

            int totalCrystalsToShoot = 80;
            ref float shootTimer = ref npc.Infernum().ExtraAI[1];
            ref float crystalsShot = ref npc.Infernum().ExtraAI[2];

            npc.defense = (int)MathHelper.Lerp(50, CocoonDefense, Utils.InverseLerp(0f, 40f, attackTimer, true));
            drawState = (int)ProvidenceFrameDrawingType.CocoonState;

            if (crystalsShot < totalCrystalsToShoot && shootTimer % crystalShootRate == crystalShootRate - 1f && shootTimer >= shootDelay)
            {
                crystalsShot++;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float farAwayEnrageInterpolant = Utils.InverseLerp(500f, 1900f, npc.Distance(target.Center), true);
                    float crystalShootSpeed = MathHelper.Lerp(5f, 31f, farAwayEnrageInterpolant);
                    Vector2 shootPosition = crystalCenter;
                    Vector2 shootVelocity = ((attackTimer - shootDelay) * MathHelper.TwoPi / 120f).ToRotationVector2() * crystalShootSpeed;

                    // Approach a velocity that points directly at the player with a bit of variance the farther away they ary.
                    Vector2 offsetPointVelocity = npc.SafeDirectionTo(target.Center) * crystalShootSpeed + Main.rand.NextVector2Circular(crystalShootSpeed, crystalShootSpeed) * 0.3f;
                    shootVelocity = Vector2.Lerp(shootVelocity, offsetPointVelocity, farAwayEnrageInterpolant);

                    Utilities.NewProjectileBetter(shootPosition, shootVelocity, ModContent.ProjectileType<RainbowCrystal2>(), 280, 0f, Main.myPlayer);
                    Utilities.NewProjectileBetter(shootPosition, -shootVelocity, ModContent.ProjectileType<RainbowCrystal2>(), 280, 0f, Main.myPlayer);
                }

                npc.netUpdate = true;
            }

            if (shootTimer >= shootDelay && attackTimer % flameShootRate == flameShootRate - 1f)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ProvidenceHolyBlastShoot"), target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float angularOffset = Main.rand.NextFloat(MathHelper.TwoPi);
                    for (int i = 0; i < 8; i++)
                    {
                        Vector2 shootVelocity = (MathHelper.TwoPi * i / 8f + angularOffset).ToRotationVector2() * 35f;
                        Utilities.NewProjectileBetter(crystalCenter, shootVelocity, ModContent.ProjectileType<HolySpear3>(), 280, 0f);
                    }

                    for (int i = 0; i < 24; i++)
                    {
                        Vector2 shootVelocity = (MathHelper.TwoPi * i / 24f + angularOffset).ToRotationVector2() * 6.5f;
                        int star = Utilities.NewProjectileBetter(crystalCenter, shootVelocity, ModContent.ProjectileType<GreatStar>(), 280, 0f);
                        Main.projectile[star].Size /= 1.3f;
                        Main.projectile[star].scale /= 1.3f;
                        Main.projectile[star].ai[1] = 1f;
                        Main.projectile[star].netUpdate = true;
                    }
                }
            }

            shootTimer++;
            if (crystalsShot >= totalCrystalsToShoot)
                SelectNextAttack(npc, phase2, inRainbowCrystalState);
        }

        public static void DoBehavior_AttackerGuardians(NPC npc, bool phase2, bool inRainbowCrystalState, ref float drawState, ref float attackTimer)
        {
            npc.defense = (int)MathHelper.Lerp(50, CocoonDefense, Utils.InverseLerp(0f, 30f, attackTimer, true));
            drawState = (int)ProvidenceFrameDrawingType.CocoonState;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if ((int)attackTimer == 1 || (int)attackTimer == 10)
                {
                    int directionalBias = attackTimer <= 1f ? 1 : -1;
                    int guardian = Utilities.NewProjectileBetter(npc.Center, Vector2.UnitY * -8f, ModContent.ProjectileType<AttackerApparation>(), 250, 0f, Main.myPlayer);
                    (Main.projectile[guardian].modProjectile as AttackerApparation).Time = 10 - attackTimer;
                    Main.projectile[guardian].localAI[1] = directionalBias;

                    if ((int)attackTimer == 10)
                    {
                        (Main.projectile[guardian].modProjectile as AttackerApparation).OtherGuardianIndex =
                            Utilities.AllProjectilesByID(ModContent.ProjectileType<AttackerApparation>()).First().identity;
                    }
                }
            }

            if (attackTimer >= GuardianApparationTime + 60f)
                SelectNextAttack(npc, phase2, inRainbowCrystalState);
        }

        public static void SelectNextAttack(NPC npc, bool phase2, bool inRainbowCrystalState)
        {
            npc.ai[2]++;
            npc.ai[1] = 0f;
            switch ((int)npc.ai[2] % 15)
            {
                case 0:
                    npc.ai[0] = (int)ProvidenceAttackType.Starburst;
                    break;
                case 1:
                    npc.ai[0] = (int)ProvidenceAttackType.SolarMeteorShower;
                    break;
                case 2:
                    npc.ai[0] = (int)ProvidenceAttackType.BurningAir;
                    break;
                case 3:
                    npc.ai[0] = phase2 && !Main.dayTime ? (int)ProvidenceAttackType.AttackerGuardians : (int)ProvidenceAttackType.BootlegRadianceSpears;
                    break;
                case 4:
                    npc.ai[0] = (int)ProvidenceAttackType.Starburst;
                    break;
                case 5:
                    npc.ai[0] = inRainbowCrystalState ? (int)ProvidenceAttackType.CrystalRainbowDeathray : (int)ProvidenceAttackType.Starburst;
                    break;
                case 6:
                    npc.ai[0] = inRainbowCrystalState ? (int)ProvidenceAttackType.CrystalRain : (int)ProvidenceAttackType.BurningAir;
                    break;
                case 7:
                    npc.ai[0] = inRainbowCrystalState ? (int)ProvidenceAttackType.CrystalFlames : (int)ProvidenceAttackType.Starburst;
                    break;
                case 8:
                    npc.ai[0] = phase2 ? (int)ProvidenceAttackType.BootlegRadianceSpears : (int)ProvidenceAttackType.SolarMeteorShower;
                    break;
                case 9:
                    npc.ai[0] = (int)ProvidenceAttackType.Starburst;
                    break;
                case 10:
                    npc.ai[0] = inRainbowCrystalState ? (int)ProvidenceAttackType.CrystalRain : (int)ProvidenceAttackType.BurningAir;
                    break;
                case 11:
                    npc.ai[0] = phase2 ? (int)ProvidenceAttackType.CrystalFlames : (int)ProvidenceAttackType.BootlegRadianceSpears;
                    break;
                case 12:
                    npc.ai[0] = phase2 && !Main.dayTime ? (int)ProvidenceAttackType.AttackerGuardians : (int)ProvidenceAttackType.Starburst;
                    break;
                case 13:
                    npc.ai[0] = inRainbowCrystalState ? (int)ProvidenceAttackType.CrystalRainbowDeathray : (int)ProvidenceAttackType.BurningAir;
                    break;
                case 14:
                    npc.ai[0] = inRainbowCrystalState ? (int)ProvidenceAttackType.CrystalRain : (int)ProvidenceAttackType.SolarMeteorShower;
                    break;
            }

            // Reset the misc ai slots.
            for (int i = 0; i < 5; i++)
            {
                npc.Infernum().ExtraAI[i] = 0f;
            }

            npc.velocity = Vector2.Zero;

            // And the central rainbow crystal vibrance.
            npc.Infernum().ExtraAI[5] = 0f;
            npc.localAI[3] = 0f;
        }

        public static void ReleaseSparkles(Vector2 sparkleSpawnPosition, int sparkleCount, float maxSpraySpeed)
        {
            // Prevent projectiles from spawning client-side.
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < sparkleCount; i++)
                Utilities.NewProjectileBetter(sparkleSpawnPosition, Main.rand.NextVector2Circular(maxSpraySpeed, maxSpraySpeed), ModContent.ProjectileType<YharonMajesticSparkle>(), 0, 0f);
        }

        public static void CreateChargeDust(Vector2 chargingPoint, float maxOffsetRadius, float generalScale)
        {
            // Prevent dust visuals from spawning server-side.
            if (Main.dedServ)
                return;

            for (int i = 0; i < 6; i++)
            {
                Vector2 spawnPosition = chargingPoint + Main.rand.NextVector2Unit() * Main.rand.NextFloat(0.6f, 1f) * maxOffsetRadius;
                Vector2 chargeDustVelocity = (chargingPoint - spawnPosition) / 7f;

                Dust rainbowChargeDust = Dust.NewDustPerfect(spawnPosition, 261);
                rainbowChargeDust.scale = generalScale * Main.rand.NextFloat(0.75f, 1.25f);
                rainbowChargeDust.velocity = chargeDustVelocity;
                rainbowChargeDust.color = Main.hslToRgb(Main.rand.NextFloat(), 0.9f, 0.55f);
                rainbowChargeDust.noGravity = true;
            }
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
            if (Main.dayTime)
                spriteBatch.Draw(wingTexture, baseDrawPosition, frame, new Color(255, 120, 0, 128) * npc.Opacity, npc.rotation, drawOrigin, npc.scale, spriteEffects, 0f);
            else
            {
                Color nightWingColor = new Color(0, 255, 191, 0) * npc.Opacity;
                spriteBatch.Draw(wingTexture, baseDrawPosition, frame, nightWingColor, npc.rotation, drawOrigin, npc.scale, spriteEffects, 0f);
                for (int i = 0; i < 6; i++)
                {
                    Vector2 wingOffset = (MathHelper.TwoPi * i / 6f + Main.GlobalTime * 0.72f).ToRotationVector2() * npc.Opacity * wingVibrance * 4f;
                    spriteBatch.Draw(wingTexture, baseDrawPosition + wingOffset, frame, nightWingColor * 0.55f, npc.rotation, drawOrigin, npc.scale, spriteEffects, 0f);
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

                Texture2D generalTexture = ModContent.GetTexture(getTextureString);
                Texture2D crystalTexture = ModContent.GetTexture(getTextureGlow2String);
                Texture2D wingTexture = ModContent.GetTexture(getTextureGlowString);
                Texture2D fatCrystalTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Providence/ProvidenceCrystal");

                SpriteEffects spriteEffects = SpriteEffects.None;
                if (npc.spriteDirection == 1)
                    spriteEffects = SpriteEffects.FlipHorizontally;

                Vector2 drawOrigin = new Vector2(Main.npcTexture[npc.type].Width, Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type]) * 0.5f;

                float rainbowVibrance = npc.Infernum().ExtraAI[5];

                // Draw the crystal behind everything. It will appear if providence is herself invisible.
                applyShaderAndDoThing(() =>
                {
                    if (npc.localAI[3] > 0f)
                        return;

                    Vector2 crystalOrigin = fatCrystalTexture.Size() * 0.5f;
                    Vector2 crystalDrawPosition = npc.Center - Main.screenPosition;
                    spriteBatch.Draw(fatCrystalTexture, crystalDrawPosition, null, Color.White, npc.rotation, crystalOrigin, npc.scale, spriteEffects, 0f);
                }, rainbowVibrance * 1.5f);

                int frameHeight = generalTexture.Height / 3;
                Rectangle frame = generalTexture.Frame(1, 3, 0, (npc.frame.Y / frameHeight + frameOffset) % 3);

                // Draw the base texture.
                baseDrawColor *= npc.Opacity;
                spriteBatch.Draw(generalTexture, baseDrawPosition, frame, baseDrawColor, npc.rotation, drawOrigin, npc.scale, spriteEffects, 0f);

                // Draw the wings.
                DrawProvidenceWings(npc, spriteBatch, wingTexture, wingVibrance, baseDrawPosition, frame, drawOrigin, spriteEffects);

                // Draw the crystals. They become more and more rainbow as Providence gets closer to death.
                // This effect fades away as she burns.
                float crystalRainbowIntensity = Utils.InverseLerp(LifeRainbowCrystalStartRatio, LifeRainbowCrystalEndRatio, lifeRatio, true);
                if (rainbowVibrance > 0.02f)
                    crystalRainbowIntensity = 0f;
                crystalRainbowIntensity *= 1f - npc.localAI[3];
                applyShaderAndDoThing(() =>
                {
                    spriteBatch.Draw(crystalTexture, baseDrawPosition, frame, baseDrawColor, npc.rotation, drawOrigin, npc.scale, spriteEffects, 0f);
                }, crystalRainbowIntensity);
            }

            void applyShaderAndDoThing(Action thingToDo, float rainbowOpacity)
            {
                spriteBatch.EnterShaderRegion();

                // Apply a super special shader.
                MiscShaderData gradientShader = GameShaders.Misc["Infernum:GradientWingShader"];
                gradientShader.UseImage("Images/Misc/Noise");
                gradientShader.UseOpacity(rainbowOpacity);
                gradientShader.SetShaderTexture(ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Providence/ProvidenceShaderTexture"));

                gradientShader.Apply(null);

                thingToDo();

                spriteBatch.ExitShaderRegion();
            }

            int totalProvidencesToDraw = (int)MathHelper.Lerp(1f, 30f, burnIntensity);
            Texture2D baseTexture = ModContent.GetTexture("CalamityMod/NPCs/Providence/Providence");
            Vector2 textureOrigin = new Vector2(Main.npcTexture[npc.type].Width / 2, Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type] / 2);
            for (int i = 0; i < totalProvidencesToDraw; i++)
            {
                float offsetAngle = MathHelper.TwoPi * i * 2f / totalProvidencesToDraw;
                float drawOffsetScalar = (float)Math.Sin(offsetAngle * 6f + Main.GlobalTime * MathHelper.Pi);
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
