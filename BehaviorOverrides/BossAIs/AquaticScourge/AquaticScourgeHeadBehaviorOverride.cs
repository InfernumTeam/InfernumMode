using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs.AquaticScourge;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Projectiles.Enemy;
using InfernumMode.Miscellaneous;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria.World.Generation;

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
            JustCharges
        }

        public override int NPCOverrideType => ModContent.NPCType<AquaticScourgeHead>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        #region AI

        public override bool PreAI(NPC npc)
        {
            // Select a new target if an old one was lost.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();

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
                DoAttack_Despawn(npc);
                return false;
            }

            Player target = Main.player[npc.target];
            float speedFactor = 1f;
            float enrageFactor = 1f - lifeRatio;
            if (lifeRatio < 0.33f)
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
                Mod calamityModMusic = ModLoader.GetMod("CalamityModMusic");
                if (calamityModMusic != null)
                    npc.modNPC.music = calamityModMusic.GetSoundSlot(SoundType.Music, "Sounds/Music/AquaticScourge");
                else
                    npc.modNPC.music = MusicID.Boss2;
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
                                DoAttack_BelchAcid(npc, target, attackTimer, enrageFactor);
                                break;
                            case AquaticScourgeAttackType.SpitTeeth:
                                DoAttack_SpitTeeth(npc, target, attackTimer, enrageFactor);
                                break;
                            case AquaticScourgeAttackType.ReleaseCircleOfSand:
                                DoAttack_ReleaseCircleOfSand(npc, target, attackTimer, enrageFactor);
                                break;
                            case AquaticScourgeAttackType.BelchParasites:
                                DoAttack_BelchParasites(npc, attackTimer, ref speedFactor);
                                break;
                            case AquaticScourgeAttackType.BubbleSummon:
                                DoAttack_BubbleSummon(npc, target, attackTimer, enrageFactor, ref speedFactor);
                                break;
                            case AquaticScourgeAttackType.CallForSeekers:
                                DoAttack_CallForSeekers(npc, target, attackTimer, ref speedFactor);
                                break;
                            case AquaticScourgeAttackType.JustCharges:
                                speedFactor = 1.2f;
                                if (attackTimer > 300f)
                                    GotoNextAttack(npc);
                                break;
                        }
                        attackTimer++;
                    }

                    DoMovement_GeneralMovement(npc, target, speedFactor);
                }
            }

            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
            generalTimer++;

            return false;
        }

        #region Specific Behaviors

        public static void DoAttack_Despawn(NPC npc)
        {
            npc.velocity.X *= 0.985f;
            if (npc.velocity.Y < 24f)
                npc.velocity.Y += 0.32f;

            if (npc.timeLeft > 210)
                npc.timeLeft = 210;
        }

        public static void DoAttack_BelchAcid(NPC npc, Player target, float attackTimer, float enrageFactor)
        {
            int shootRate = 55;
            int shootCount = 4;

            if (!npc.WithinRange(target.Center, 380f) && attackTimer % shootRate == shootRate - 1f)
            {
                Main.PlaySound(SoundID.DD2_SkyDragonsFuryShot, npc.Center);
                Main.PlaySound(SoundID.NPCDeath13, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 11; i++)
                    {
                        Vector2 acidVelocity = Vector2.Lerp((npc.rotation - MathHelper.PiOver2).ToRotationVector2(), npc.SafeDirectionTo(target.Center), 0.5f);
                        acidVelocity = acidVelocity.RotatedByRandom(0.35f) * Main.rand.NextFloat(11f, 15f) * (1f + enrageFactor * 0.2f);
                        Utilities.NewProjectileBetter(npc.Center + acidVelocity * 3f, acidVelocity, ModContent.ProjectileType<OldDukeSummonDrop>(), 110, 0f);
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 cloudVelocity = Main.rand.NextVector2Unit() * (Main.rand.NextFloat(7f, 9f) + enrageFactor * 2f);
                        Utilities.NewProjectileBetter(npc.Center + cloudVelocity * 3f, cloudVelocity, ModContent.ProjectileType<SulphurousPoisonCloud>(), 115, 0f);
                    }
                }
            }

            if (attackTimer >= shootRate * shootCount)
                GotoNextAttack(npc);
        }

        public static void DoAttack_SpitTeeth(NPC npc, Player target, float attackTimer, float enrageFactor)
        {
            int shootRate = 65;
            int shootCount = 4;

            if (!npc.WithinRange(target.Center, 250f) && attackTimer % shootRate == shootRate - 1f)
            {
                Main.PlaySound(SoundID.NPCDeath13, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 toothVelocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.Lerp(-0.48f, 0.48f, i / 2f)) * 7.2f;
                        toothVelocity *= 1f + (float)Math.Sin(MathHelper.Pi * i / 3f) * 0.2f + enrageFactor * 0.135f;
                        Utilities.NewProjectileBetter(npc.Center + toothVelocity * 3f, toothVelocity, ModContent.ProjectileType<SlowerSandTooth>(), 115, 0f);
                    }

                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 cloudVelocity;
                        do
                            cloudVelocity = Main.rand.NextVector2Unit() * (Main.rand.NextFloat(6f, 8f) + enrageFactor * 2f);
                        while (cloudVelocity.AngleBetween(npc.SafeDirectionTo(target.Center)) > MathHelper.Pi * 0.67f);
                        Utilities.NewProjectileBetter(npc.Center + cloudVelocity * 3f, cloudVelocity, ModContent.ProjectileType<SandPoisonCloud>(), 115, 0f);
                    }
                }
            }

            if (attackTimer >= shootRate * shootCount)
                GotoNextAttack(npc);
        }

        public static void DoAttack_ReleaseCircleOfSand(NPC npc, Player target, float attackTimer, float enrageFactor)
        {
            int shootRate = 45;
            int shootCount = 5;

            if (!npc.WithinRange(target.Center, 345f) && attackTimer % shootRate == shootRate - 1f)
            {
                Main.PlaySound(SoundID.NPCDeath13, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 24; i++)
                    {
                        Vector2 sandVelocity = (MathHelper.TwoPi * i / 24f).ToRotationVector2() * 6f;
                        Utilities.NewProjectileBetter(npc.Center + sandVelocity * 3f, sandVelocity, ModContent.ProjectileType<SandBlast>(), 110, 0f);
                    }

                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 cloudVelocity;
                        do
                            cloudVelocity = Main.rand.NextVector2Unit() * (Main.rand.NextFloat(6f, 8f) + enrageFactor * 2f);
                        while (cloudVelocity.AngleBetween(npc.SafeDirectionTo(target.Center)) > MathHelper.Pi * 0.67f);
                        Utilities.NewProjectileBetter(npc.Center + cloudVelocity * 3f, cloudVelocity, ModContent.ProjectileType<SandPoisonCloud>(), 115, 0f);
                    }
                }
            }

            if (attackTimer >= shootRate * shootCount)
                GotoNextAttack(npc);
        }

        public static void DoAttack_BelchParasites(NPC npc, float attackTimer, ref float speedFactor)
        {
            speedFactor *= MathHelper.Lerp(1f, 0.25f, Utils.InverseLerp(10f, 90f, attackTimer, true));
            if (attackTimer == 135f)
            {
                Main.PlaySound(SoundID.NPCDeath13, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 parasiteVelocity = (npc.rotation - MathHelper.PiOver2).ToRotationVector2().RotatedByRandom(0.37f) * Main.rand.NextFloat(8.5f, 12f);
                        int parasite = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<AquaticParasite2>());
                        if (Main.npc.IndexInRange(parasite))
                        {
                            Main.npc[parasite].velocity = parasiteVelocity;
                            Main.npc[parasite].netUpdate = true;
                        }
                    }
                }
            }

            if (attackTimer >= 205f)
                GotoNextAttack(npc);
        }

        public static void DoAttack_BubbleSummon(NPC npc, Player target, float attackTimer, float enrageFactor, ref float speedFactor)
        {
            speedFactor *= MathHelper.Lerp(1f, 0.667f, Utils.InverseLerp(10f, 70f, attackTimer, true));
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 8f == 7f)
            {
                List<Vector2> potentialSpawnPoints = new List<Vector2>();
                for (int i = -120; i < 120; i++)
                {
                    Point waterPosition = new Point((int)(target.Center.X / 16 + i), (int)target.Center.Y / 16 - 50);
                    if (WorldUtils.Find(waterPosition, Searches.Chain(new Searches.Down(8000), new CustomTileConditions.IsWater()), out Point updatedWaterPosition))
                        potentialSpawnPoints.Add(updatedWaterPosition.ToWorldCoordinates());
                }

                Vector2 spawnPoint;
                do
                    spawnPoint = Main.rand.Next(potentialSpawnPoints);
                while (MathHelper.Distance(spawnPoint.X, target.Center.X) < 180f);
                Utilities.NewProjectileBetter(Main.rand.Next(potentialSpawnPoints), -Vector2.UnitY * (8.4f + enrageFactor * 1.8f), ModContent.ProjectileType<SulphuricAcidBubble>(), 110, 0f);
            }

            if (attackTimer >= 385f)
                GotoNextAttack(npc);
        }
        
        public static void DoAttack_CallForSeekers(NPC npc, Player target, float attackTimer, ref float speedFactor)
        {
            int summonRate = 90;
            int summonCount = 3;

            speedFactor *= MathHelper.Lerp(1f, 0.5f, Utils.InverseLerp(10f, 50f, attackTimer, true));

            if (attackTimer == 75f)
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/MaulerRoar"), npc.Center);

            if (attackTimer % summonRate == summonRate - 1f)
            {
                List<Vector2> potentialSpawnPoints = new List<Vector2>();
                for (int i = -120; i < 120; i++)
                {
                    Point waterPosition = new Point((int)(target.Center.X / 16 + i), (int)target.Center.Y / 16);
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
                    NPC.NewNPC((int)spawnPoint.X, (int)spawnPoint.Y, ModContent.NPCType<AquaticSeekerHead2>());
                }
            }

            if (attackTimer >= summonRate * summonCount)
                GotoNextAttack(npc);
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

            float movementSpeed = MathHelper.Lerp(5f, 8.5f, Utils.InverseLerp(300f, 750f, npc.Distance(flyDestination), true));
            npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(flyDestination), movementSpeed / 300f, true) * movementSpeed;
        }

        public static void DoMovement_AggressiveSnakeMovement(NPC npc, Player target, float generalTimer, float speedFactor)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float idealRotation = npc.AngleTo(target.Center);
            float acceleration = MathHelper.Lerp(0.023f, 0.0285f, Utils.InverseLerp(1f, 0.15f, lifeRatio, true));
            float movementSpeed = MathHelper.Lerp(10.5f, 14f, Utils.InverseLerp(1f, 0.15f, lifeRatio, true));
            movementSpeed += MathHelper.Lerp(0f, 15f, Utils.InverseLerp(420f, 3000f, npc.Distance(target.Center), true));
            idealRotation += (float)Math.Sin(generalTimer / 43f) * Utils.InverseLerp(360f, 425f, npc.Distance(target.Center), true) * 0.74f;
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
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/DesertScourgeRoar"), npc.Center);
                    roarSoundCountdown = 45f;
                }
                npc.velocity *= 1.031f;
            }
        }

        public static void DoMovement_GeneralMovement(NPC npc, Player target, float speedFactor)
        {
            AquaticScourgeAttackType attackType = (AquaticScourgeAttackType)(int)npc.ai[2];
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float idealRotation = npc.AngleTo(target.Center);
            float acceleration = MathHelper.Lerp(0.019f, 0.0265f, Utils.InverseLerp(1f, 0.15f, lifeRatio, true));
            float movementSpeed = MathHelper.Lerp(12.25f, 14.25f, Utils.InverseLerp(1f, 0.15f, lifeRatio, true));
            movementSpeed += MathHelper.Lerp(0f, 15f, Utils.InverseLerp(420f, 3000f, npc.Distance(target.Center), true));
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
                    nextIndex = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, bodyType, npc.whoAmI + 1);
                else
                    nextIndex = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, tailType, npc.whoAmI + 1);

                Main.npc[nextIndex].realLife = npc.whoAmI;
                Main.npc[nextIndex].ai[2] = npc.whoAmI;
                Main.npc[nextIndex].ai[1] = previousIndex;
                Main.npc[previousIndex].ai[0] = nextIndex;

                // Force sync the new segment into existence.
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, nextIndex, 0f, 0f, 0f, 0);

                previousIndex = nextIndex;
            }
        }

        internal static void GotoNextAttack(NPC npc)
        {
            npc.alpha = 0;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            AquaticScourgeAttackType currentAttack = (AquaticScourgeAttackType)(int)npc.ai[2];
            AquaticScourgeAttackType nextAttack;

            WeightedRandom<AquaticScourgeAttackType> attackSelector = new WeightedRandom<AquaticScourgeAttackType>();

            if (lifeRatio > 0.15f)
            {
                attackSelector.Add(AquaticScourgeAttackType.BelchAcid, 1f);
                attackSelector.Add(AquaticScourgeAttackType.SpitTeeth, 1.1f);
                attackSelector.Add(AquaticScourgeAttackType.ReleaseCircleOfSand, 0.9);
                attackSelector.Add(AquaticScourgeAttackType.BelchParasites, lifeRatio < 0.7f && NPC.CountNPCS(ModContent.NPCType<AquaticParasite2>()) < 6 ? 1.2f : 0f);
                attackSelector.Add(AquaticScourgeAttackType.BubbleSummon, lifeRatio < 0.5f ? 1.4f : 0f);
                attackSelector.Add(AquaticScourgeAttackType.CallForSeekers, lifeRatio < 0.35f && NPC.CountNPCS(ModContent.NPCType<AquaticSeekerHead2>()) < 4 ? 1.6f : 0f);

                do
                    nextAttack = attackSelector.Get();
                while (nextAttack == currentAttack);
            }
            else
                nextAttack = AquaticScourgeAttackType.JustCharges;

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

        #endregion AI
    }
}
