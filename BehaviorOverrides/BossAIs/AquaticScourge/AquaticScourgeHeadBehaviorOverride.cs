using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs.AcidRain;
using CalamityMod.NPCs.AquaticScourge;
using CalamityMod.NPCs.DesertScourge;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Projectiles.Enemy;
using InfernumMode.Miscellaneous;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria.WorldBuilding;

namespace InfernumMode.BehaviorOverrides.BossAIs.AquaticScourge
{
    public class AquaticScourgeHeadBehaviorOverride : NPCBehaviorOverride
    {
        public enum AquaticScourgeAttackType
        {
            BelchAcid,
            SpitTeeth,
            ReleaseCircleOfSand,
            BelchParasites,
            BubbleSummon,
            CallForSeekers,
            ProjectileSpin,
            JustCharges
        }

        public override int NPCOverrideType => ModContent.NPCType<AquaticScourgeHead>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio,
            Phase3LifeRatio,
            Phase4LifeRatio,
            Phase5LifeRatio
        };

        public const float Phase2LifeRatio = 0.7f;

        public const float Phase3LifeRatio = 0.5f;

        public const float Phase4LifeRatio = 0.35f;

        public const float Phase5LifeRatio = 0.15f;

        public override bool PreAI(NPC npc)
        {
            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();

            npc.alpha = Utils.Clamp(npc.alpha - 20, 0, 255);

            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float generalTimer = ref npc.ai[1];
            ref float attackType = ref npc.ai[2];
            ref float attackTimer = ref npc.ai[3];
            ref float attackDelay = ref npc.Infernum().ExtraAI[5];
            ref float initializedFlag = ref npc.Infernum().ExtraAI[6];
            ref float angeredYet = ref npc.Infernum().ExtraAI[1];

            if (Main.netMode != NetmodeID.MultiplayerClient && initializedFlag == 0f)
            {
                CreateSegments(npc, 32, ModContent.NPCType<AquaticScourgeBody>(), ModContent.NPCType<AquaticScourgeTail>());
                initializedFlag = 1f;
                npc.netUpdate = true;
            }

            // Determine hostility.
            if (npc.justHit || lifeRatio < 0.995f || BossRushEvent.BossRushActive)
            {
                if (npc.damage == 0)
                    npc.timeLeft *= 20;

                CalamityMod.CalamityMod.bossKillTimes.TryGetValue(npc.type, out int revKillTime);
                npc.Calamity().KillTime = revKillTime;

                angeredYet = 1f;
                npc.damage = npc.defDamage;
                npc.boss = true;
                npc.netUpdate = true;
            }
            else
                npc.damage = 0;

            npc.chaseable = angeredYet == 1f;
            npc.Calamity().newAI[0] = angeredYet;

            // If there still was no valid target, swim away.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
            {
                DoBehavior_Despawn(npc);
                return false;
            }

            Player target = Main.player[npc.target];
            float speedFactor = 1f;
            float enrageFactor = 1f - lifeRatio;
            if (lifeRatio < Phase4LifeRatio)
                speedFactor *= 1.15f;

            // Enrage if the target leaves the ocean.
            if (target.position.Y < 800f || target.position.Y > Main.worldSurface * 16.0 || (target.position.X > 6400f && target.position.X < (Main.maxTilesX * 16 - 6400)))
            {
                if (!BossRushEvent.BossRushActive)
                {
                    npc.Calamity().CurrentlyEnraged = angeredYet == 1f;
                    enrageFactor = 1.8f;
                }
            }

            if (BossRushEvent.BossRushActive)
                enrageFactor *= 3f;

            // Swim slowly around the "target" when not angry.
            if (angeredYet == 0f)
                DoMovement_IdleHoverMovement(npc, target);
            else
            {
                if (ModLoader.TryGetMod("CalamityModMusic", out Mod calamityModMusic))
                    npc.ModNPC.Music = MusicLoader.GetMusicSlot(calamityModMusic, "Sounds/Music/AquaticScourge");
                else
                    npc.ModNPC.Music = MusicID.Boss2;
                float wrappedTime = generalTimer % 1040f;
                if (wrappedTime < 360f)
                {
                    DoMovement_AggressiveSnakeMovement(npc, target, generalTimer, speedFactor);
                    if (attackDelay > 50f)
                        attackDelay = 50f;
                }
                else
                {
                    // Decrement the attack timer as needed.
                    if (attackDelay > 0f)
                    {
                        attackTimer = 0f;
                        attackDelay--;
                    }
                    else
                    {
                        switch ((AquaticScourgeAttackType)(int)attackType)
                        {
                            case AquaticScourgeAttackType.BelchAcid:
                                DoBehavior_BelchAcid(npc, target, attackTimer, enrageFactor);
                                break;
                            case AquaticScourgeAttackType.SpitTeeth:
                                DoBehavior_SpitTeeth(npc, target, attackTimer, enrageFactor);
                                break;
                            case AquaticScourgeAttackType.ReleaseCircleOfSand:
                                DoBehavior_ReleaseCircleOfSand(npc, target, attackTimer, enrageFactor);
                                break;
                            case AquaticScourgeAttackType.BelchParasites:
                                DoBehavior_BelchParasites(npc, attackTimer, ref speedFactor);
                                break;
                            case AquaticScourgeAttackType.BubbleSummon:
                                DoBehavior_BubbleSummon(npc, target, attackTimer, enrageFactor, ref speedFactor);
                                break;
                            case AquaticScourgeAttackType.CallForSeekers:
                                DoBehavior_CallForSeekers(npc, target, attackTimer, ref speedFactor);
                                break;
                            case AquaticScourgeAttackType.ProjectileSpin:
                                DoBehavior_ProjectileSpin(npc, attackTimer, ref speedFactor);
                                break;
                            case AquaticScourgeAttackType.JustCharges:
                                speedFactor = 1.5f;
                                if (attackTimer > 300f)
                                    SelectNextAttack(npc);
                                break;
                        }
                        attackTimer++;
                    }

                    DoMovement_GeneralMovement(npc, target, speedFactor * 1.25f);
                }
            }

            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
            generalTimer++;

            return false;
        }

        #region Specific Behaviors

        public static void DoBehavior_Despawn(NPC npc)
        {
            npc.velocity.X *= 0.985f;
            if (npc.velocity.Y < 24f)
                npc.velocity.Y += 0.32f;

            if (npc.timeLeft > 210)
                npc.timeLeft = 210;
        }

        public static void DoBehavior_BelchAcid(NPC npc, Player target, float attackTimer, float enrageFactor)
        {
            int shootRate = 55;
            int shootCount = 4;

            if (!npc.WithinRange(target.Center, 380f) && attackTimer % shootRate == shootRate - 1f)
            {
                SoundEngine.PlaySound(SoundID.DD2_SkyDragonsFuryShot, npc.Center);
                SoundEngine.PlaySound(SoundID.NPCDeath13, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 11; i++)
                    {
                        Vector2 acidVelocity = Vector2.Lerp((npc.rotation - MathHelper.PiOver2).ToRotationVector2(), npc.SafeDirectionTo(target.Center), 0.5f);
                        acidVelocity = acidVelocity.RotatedByRandom(0.35f) * Main.rand.NextFloat(11f, 15f) * (1f + enrageFactor * 0.2f);
                        Utilities.NewProjectileBetter(npc.Center + acidVelocity * 3f, acidVelocity, ModContent.ProjectileType<OldDukeSummonDrop>(), 125, 0f);
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 cloudVelocity = Main.rand.NextVector2Unit() * (Main.rand.NextFloat(7f, 9f) + enrageFactor * 2f);
                        Utilities.NewProjectileBetter(npc.Center + cloudVelocity * 3f, cloudVelocity, ModContent.ProjectileType<SulphurousPoisonCloud>(), 125, 0f);
                    }
                }
            }

            if (attackTimer >= shootRate * shootCount)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_SpitTeeth(NPC npc, Player target, float attackTimer, float enrageFactor)
        {
            int shootRate = 65;
            int shootCount = 4;

            if (!npc.WithinRange(target.Center, 250f) && attackTimer % shootRate == shootRate - 1f)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath13, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 toothVelocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.Lerp(-0.48f, 0.48f, i / 2f)) * 7.2f;
                        toothVelocity *= 1f + (float)Math.Sin(MathHelper.Pi * i / 3f) * 0.2f + enrageFactor * 0.135f;
                        Utilities.NewProjectileBetter(npc.Center + toothVelocity * 3f, toothVelocity, ModContent.ProjectileType<SlowerSandTooth>(), 125, 0f);
                    }

                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 cloudVelocity;
                        do
                            cloudVelocity = Main.rand.NextVector2Unit() * (Main.rand.NextFloat(6f, 8f) + enrageFactor * 2f);
                        while (cloudVelocity.AngleBetween(npc.SafeDirectionTo(target.Center)) > MathHelper.Pi * 0.67f);
                        Utilities.NewProjectileBetter(npc.Center + cloudVelocity * 3f, cloudVelocity, ModContent.ProjectileType<SandPoisonCloud>(), 125, 0f);
                    }
                }
            }

            if (attackTimer >= shootRate * shootCount)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_ReleaseCircleOfSand(NPC npc, Player target, float attackTimer, float enrageFactor)
        {
            int shootRate = 45;
            int shootCount = 5;

            if (!npc.WithinRange(target.Center, 345f) && attackTimer % shootRate == shootRate - 1f)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath13, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 24; i++)
                    {
                        Vector2 sandVelocity = (MathHelper.TwoPi * i / 24f).ToRotationVector2() * 6f;
                        Utilities.NewProjectileBetter(npc.Center + sandVelocity * 3f, sandVelocity, ModContent.ProjectileType<SandBlast>(), 125, 0f);
                    }

                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 cloudVelocity;
                        do
                            cloudVelocity = Main.rand.NextVector2Unit() * (Main.rand.NextFloat(6f, 8f) + enrageFactor * 2f);
                        while (cloudVelocity.AngleBetween(npc.SafeDirectionTo(target.Center)) > MathHelper.Pi * 0.67f);
                        Utilities.NewProjectileBetter(npc.Center + cloudVelocity * 3f, cloudVelocity, ModContent.ProjectileType<SandPoisonCloud>(), 125, 0f);
                    }
                }
            }

            if (attackTimer >= shootRate * shootCount)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_BelchParasites(NPC npc, float attackTimer, ref float speedFactor)
        {
            speedFactor *= MathHelper.Lerp(1f, 0.25f, Utils.GetLerpValue(10f, 90f, attackTimer, true));
            if (attackTimer == 135f)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath13, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 parasiteVelocity = (npc.rotation - MathHelper.PiOver2).ToRotationVector2().RotatedByRandom(0.37f) * Main.rand.NextFloat(8.5f, 12f);
                        int parasite = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<AquaticParasite2>());
                        if (Main.npc.IndexInRange(parasite))
                        {
                            Main.npc[parasite].velocity = parasiteVelocity;
                            Main.npc[parasite].netUpdate = true;
                        }
                    }
                }
            }

            if (attackTimer >= 205f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_BubbleSummon(NPC npc, Player target, float attackTimer, float enrageFactor, ref float speedFactor)
        {
            speedFactor *= MathHelper.Lerp(1f, 0.667f, Utils.GetLerpValue(10f, 70f, attackTimer, true));
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 8f == 7f)
            {
                List<Vector2> potentialSpawnPoints = new();
                for (int i = -120; i < 120; i++)
                {
                    Point waterPosition = new((int)(target.Center.X / 16 + i), (int)target.Center.Y / 16 - 50);
                    if (WorldUtils.Find(waterPosition, Searches.Chain(new Searches.Down(8000), new CustomTileConditions.IsWater()), out Point updatedWaterPosition))
                        potentialSpawnPoints.Add(updatedWaterPosition.ToWorldCoordinates());
                }

                Vector2 spawnPoint;
                do
                    spawnPoint = Main.rand.Next(potentialSpawnPoints);
                while (MathHelper.Distance(spawnPoint.X, target.Center.X) < 180f);
                Utilities.NewProjectileBetter(Main.rand.Next(potentialSpawnPoints), -Vector2.UnitY * (8.4f + enrageFactor * 1.8f), ModContent.ProjectileType<SulphuricAcidBubble>(), 125, 0f);
            }

            if (attackTimer >= 385f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_CallForSeekers(NPC npc, Player target, float attackTimer, ref float speedFactor)
        {
            int summonRate = 90;
            int summonCount = 3;

            speedFactor *= MathHelper.Lerp(1f, 0.5f, Utils.GetLerpValue(10f, 50f, attackTimer, true));

            if (attackTimer == 75f)
                SoundEngine.PlaySound(Mauler.RoarSound, npc.Center);

            if (attackTimer % summonRate == summonRate - 1f)
            {
                List<Vector2> potentialSpawnPoints = new();
                for (int i = -120; i < 120; i++)
                {
                    Point waterPosition = new((int)(target.Center.X / 16 + i), (int)target.Center.Y / 16);
                    if (WorldUtils.Find(waterPosition, Searches.Chain(new Searches.Down(8000), new CustomTileConditions.IsWater()), out Point updatedWaterPosition))
                        potentialSpawnPoints.Add(updatedWaterPosition.ToWorldCoordinates(8f, 36f));
                }

                if (potentialSpawnPoints.Count == 0)
                {
                    NPC bodySegment = Main.npc[(int)npc.ai[0]];
                    for (int i = 0; i < 45; i++)
                    {
                        if (i > 5)
                            potentialSpawnPoints.Add(bodySegment.Center);
                        bodySegment = Main.npc[(int)bodySegment.ai[0]];
                    }
                }

                if (Main.netMode != NetmodeID.MultiplayerClient && potentialSpawnPoints.Count > 0)
                {
                    Vector2 spawnPoint = Main.rand.Next(potentialSpawnPoints);
                    NPC.NewNPC(npc.GetSource_FromAI(), (int)spawnPoint.X, (int)spawnPoint.Y, ModContent.NPCType<AquaticSeekerHead2>());
                }
            }

            if (attackTimer >= summonRate * summonCount)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_ProjectileSpin(NPC npc, float attackTimer, ref float speedFactor)
        {
            int redirectTime = 75;
            int spinTime = 150;
            int sandToothReleaseRate = 20;
            int cloudReleaseRate = 48;
            float spinSpeed = 23f;
            float spinArc = MathHelper.TwoPi / spinTime;

            // Disable contact damage.
            npc.damage = 0;

            speedFactor = Utils.GetLerpValue(redirectTime, 0f, attackTimer, true);
            if (attackTimer < redirectTime)
                return;

            // Spin in place.
            npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * spinSpeed;
            npc.velocity = npc.velocity.RotatedBy(spinArc);

            // Release sand teeth.
            if (attackTimer % sandToothReleaseRate == sandToothReleaseRate - 1f)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath13, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center, npc.velocity * 0.45f, ModContent.ProjectileType<SlowerSandTooth>(), 125, 0f);
            }

            // Release sand poison clouds.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % cloudReleaseRate == cloudReleaseRate - 1f)
            {
                for (int i = 0; i < 20; i++)
                {
                    Vector2 cloudVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(5f, 14.5f);
                    Utilities.NewProjectileBetter(npc.Center, cloudVelocity, ModContent.ProjectileType<SandPoisonCloud>(), 125, 0f);
                }
            }

            if (attackTimer >= redirectTime + spinTime)
                SelectNextAttack(npc);
        }

        public static void DoMovement_IdleHoverMovement(NPC npc, Player target)
        {
            if (npc.WithinRange(target.Center, 160f) && npc.velocity != Vector2.Zero)
                return;

            Vector2 flyDestination = target.Center;

            // Don't fly too high in the air.
            if (WorldUtils.Find(flyDestination.ToTileCoordinates(), Searches.Chain(new Searches.Down(10000), new CustomTileConditions.IsWaterOrSolid()), out Point result))
            {
                Vector2 worldCoordinatesResult = result.ToWorldCoordinates();
                if (worldCoordinatesResult.Y > flyDestination.Y + 50f)
                    flyDestination.Y = worldCoordinatesResult.Y + 25f;
            }

            float movementSpeed = MathHelper.Lerp(5f, 8.5f, Utils.GetLerpValue(300f, 750f, npc.Distance(flyDestination), true));
            npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(flyDestination), movementSpeed / 300f, true) * movementSpeed;
        }

        public static void DoMovement_AggressiveSnakeMovement(NPC npc, Player target, float generalTimer, float speedFactor)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float idealRotation = npc.AngleTo(target.Center);
            float acceleration = MathHelper.Lerp(0.023f, 0.0285f, Utils.GetLerpValue(1f, Phase5LifeRatio, lifeRatio, true));
            float movementSpeed = MathHelper.Lerp(10.5f, 14f, Utils.GetLerpValue(1f, Phase5LifeRatio, lifeRatio, true));
            movementSpeed += MathHelper.Lerp(0f, 15f, Utils.GetLerpValue(420f, 3000f, npc.Distance(target.Center), true));
            idealRotation += (float)Math.Sin(generalTimer / 43f) * Utils.GetLerpValue(360f, 425f, npc.Distance(target.Center), true) * 0.74f;
            movementSpeed *= speedFactor;

            ref float roarSoundCountdown = ref npc.localAI[0];

            if (!npc.WithinRange(target.Center, 220f))
            {
                float newSpeed = MathHelper.Lerp(npc.velocity.Length(), movementSpeed, acceleration * 2.5f);
                npc.velocity = npc.velocity.RotateTowards(idealRotation, acceleration, true);
                npc.velocity = Vector2.Lerp(npc.velocity * newSpeed, npc.SafeDirectionTo(target.Center) * newSpeed, 0.03f);

                // Decrement the sound countdown as necessary.
                if (roarSoundCountdown > 0f)
                    roarSoundCountdown--;
            }
            else if (npc.velocity.Length() < 23.5f)
            {
                if (roarSoundCountdown <= 0f)
                {
                    SoundEngine.PlaySound(DesertScourgeHead.RoarSound, npc.Center);
                    roarSoundCountdown = 45f;
                }
                npc.velocity *= 1.031f;
            }
        }

        public static void DoMovement_GeneralMovement(NPC npc, Player target, float speedFactor)
        {
            if (speedFactor == 0f)
                return;

            AquaticScourgeAttackType attackType = (AquaticScourgeAttackType)(int)npc.ai[2];
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float idealRotation = npc.AngleTo(target.Center);
            float acceleration = MathHelper.Lerp(0.019f, 0.0265f, Utils.GetLerpValue(1f, Phase5LifeRatio, lifeRatio, true));
            float movementSpeed = MathHelper.Lerp(12.25f, 14.25f, Utils.GetLerpValue(1f, Phase5LifeRatio, lifeRatio, true));
            movementSpeed += MathHelper.Lerp(0f, 15f, Utils.GetLerpValue(420f, 3000f, npc.Distance(target.Center), true));
            movementSpeed *= speedFactor * (BossRushEvent.BossRushActive ? 2.1f : 1f);
            acceleration *= BossRushEvent.BossRushActive ? 2f : 1f;

            if (attackType == AquaticScourgeAttackType.JustCharges)
                acceleration *= (speedFactor - 1f) * 0.5f + 1f;

            if (!npc.WithinRange(target.Center, attackType == AquaticScourgeAttackType.JustCharges ? 180f : 320f))
            {
                float newSpeed = MathHelper.Lerp(npc.velocity.Length(), movementSpeed, acceleration * 3.2f);
                npc.velocity = npc.velocity.RotateTowards(idealRotation, acceleration, true);
                npc.velocity = Vector2.Lerp(npc.velocity * newSpeed, npc.SafeDirectionTo(target.Center) * newSpeed, 0.015f);
            }
        }

        #endregion Specific Behaviors

        #region AI Utility Methods
        public static void CreateSegments(NPC npc, int wormLength, int bodyType, int tailType)
        {
            int previousIndex = npc.whoAmI;
            for (int i = 0; i < wormLength; i++)
            {
                int nextIndex;
                if (i < wormLength - 1)
                    nextIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, bodyType, npc.whoAmI + 1);
                else
                    nextIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, tailType, npc.whoAmI + 1);

                Main.npc[nextIndex].realLife = npc.whoAmI;
                Main.npc[nextIndex].ai[2] = npc.whoAmI;
                Main.npc[nextIndex].ai[1] = previousIndex;
                Main.npc[previousIndex].ai[0] = nextIndex;

                // Force sync the new segment into existence.
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, nextIndex, 0f, 0f, 0f, 0);

                previousIndex = nextIndex;
            }
        }

        internal static void SelectNextAttack(NPC npc)
        {
            npc.alpha = 0;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            AquaticScourgeAttackType currentAttack = (AquaticScourgeAttackType)(int)npc.ai[2];
            AquaticScourgeAttackType nextAttack;

            WeightedRandom<AquaticScourgeAttackType> attackSelector = new();

            if (lifeRatio > Phase5LifeRatio)
            {
                attackSelector.Add(AquaticScourgeAttackType.BelchAcid, 1f);
                attackSelector.Add(AquaticScourgeAttackType.SpitTeeth, 1.1f);
                attackSelector.Add(AquaticScourgeAttackType.ReleaseCircleOfSand, 0.9);
                attackSelector.Add(AquaticScourgeAttackType.BelchParasites, lifeRatio < Phase2LifeRatio && NPC.CountNPCS(ModContent.NPCType<AquaticParasite2>()) < 6 ? 1.2f : 0f);
                attackSelector.Add(AquaticScourgeAttackType.BubbleSummon, lifeRatio < Phase3LifeRatio ? 1.4f : 0f);
                attackSelector.Add(AquaticScourgeAttackType.ProjectileSpin, lifeRatio < Phase3LifeRatio ? 1.4f : 0f);
                attackSelector.Add(AquaticScourgeAttackType.CallForSeekers, lifeRatio < Phase4LifeRatio && NPC.CountNPCS(ModContent.NPCType<AquaticSeekerHead2>()) < 4 ? 1.6f : 0f);

                do
                    nextAttack = attackSelector.Get();
                while (nextAttack == currentAttack);
            }
            else
                nextAttack = AquaticScourgeAttackType.JustCharges;

            // Get a new target.
            npc.TargetClosest();

            npc.ai[2] = (int)nextAttack;
            npc.ai[3] = 0f;

            // Set an 2 second delay up after the attack.
            npc.Infernum().ExtraAI[5] = 120f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.noTileCollide = true;
            npc.netUpdate = true;
        }

        #endregion AI Utility Methods
    }
}
