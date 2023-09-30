using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using InfernumMode.Assets.Sounds;
using InfernumMode.Content.Projectiles.Pets;
using InfernumMode.Content.Tiles.Misc;
using InfernumMode.Content.WorldGeneration;
using InfernumMode.Core;
using InfernumMode.Core.GlobalInstances;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.MoonLord
{
    public class MoonLordCoreBehaviorOverride : NPCBehaviorOverride
    {
        public enum MoonLordAttackState
        {
            SpawnEffects = 0,
            DeathEffects = 2,
            PhantasmalSphereHandWaves = 10,
            PhantasmalBoltEyeBursts,
            PhantasmalFlareBursts,
            PhantasmalDeathrays,
            PhantasmalSpin,
            PhantasmalRush,
            PhantasmalDance,
            PhantasmalBarrage,
            ExplodingConstellations,
            PhantasmalWrath,
            VoidAccretionDisk
        }

        public static int FireballDamage => 215;

        public static int LunarFireballDamage => 215;

        public static int PhantasmalBoltDamage => 215;

        public static int PhantasmalEyeDamage => 215;

        public static int LunarAsteroidDamage => 220;

        public static int PhantasmalSphereDamage => 220;

        public static int PhantasmalDeathrayDamage => 325;

        public static int BlackHoleDamage => 350;

        public static int PhantasmalBoltEnragedDamage => 500;

        public const int ArenaWidth = 200;

        public const int ArenaHeight = 150;

        public const float BaseFlySpeedFactor = 6f;

        public const float Phase2LifeRatio = 0.65f;

        public const float Phase3LifeRatio = 0.33333f;

        public static readonly Color OverallTint = new(7, 81, 81);

        public static bool IsEnraged
        {
            get
            {
                int moonLordIndex = NPC.FindFirstNPC(NPCID.MoonLordCore);
                if (moonLordIndex < 0)
                    return false;

                NPC moonLord = Main.npc[moonLordIndex];
                Player target = Main.player[moonLord.target];
                Rectangle arena = moonLord.Infernum().Arena;
                arena.Inflate(-8, -8);
                return !target.Hitbox.Intersects(arena);
            }
        }

        public static int CurrentActiveArms
        {
            get
            {
                int activeArms = 0;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n.type == NPCID.MoonLordHand && n.active && n.ai[0] != -2f)
                        activeArms++;
                }
                return activeArms;
            }
        }

        public static bool EyeIsActive
        {
            get
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n.type == NPCID.MoonLordHead && n.active && n.ai[0] != -2f)
                        return true;
                }
                return false;
            }
        }

        public static bool InFinalPhase
        {
            get
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n.type == NPCID.MoonLordCore && n.active && n.life < n.lifeMax * Phase3LifeRatio)
                        return true;
                }
                return false;
            }
        }

        public const int IntroSoundLength = 107;

        public override int NPCOverrideType => NPCID.MoonLordCore;

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio,
            Phase3LifeRatio
        };

        public override void Load()
        {
            GlobalNPCOverrides.OnKillEvent += GenerateProfanedTempleIfNecessary;
            GlobalNPCOverrides.StrikeNPCEvent += HandleMLBodyPhaseTriggers;
        }

        private void GenerateProfanedTempleIfNecessary(NPC npc)
        {
            // Create a profaned temple after the moon lord is killed if it doesn't exist yet, for backwards world compatibility reasons.
            if (npc.type == NPCID.MoonLordCore && !WorldSaveSystem.HasGeneratedProfanedShrine && !WeakReferenceSupport.InAnySubworld())
            {
                CalamityUtils.DisplayLocalizedText("Mods.InfernumMode.Status.PostMLTempleCreation", Color.Orange);
                ProfanedGarden.Generate(new(), new(new()));
                WorldSaveSystem.HasGeneratedProfanedShrine = true;
            }
        }

        private bool HandleMLBodyPhaseTriggers(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (npc.type is NPCID.MoonLordHand or NPCID.MoonLordHead)
                HandleBodyPartDeathTriggers(npc, modifiers.FinalDamage.Base);
            return true;
        }

        public override bool PreAI(NPC npc)
        {
            // Stop rain.
            CalamityMod.CalamityMod.StopRain();

            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float wasNotEnraged = ref npc.ai[2];
            ref float forcefullySwitchAttack = ref npc.Infernum().ExtraAI[5];
            ref float deathAttackTimer = ref npc.Infernum().ExtraAI[6];
            ref float despawnTimer = ref npc.Infernum().ExtraAI[9];
            ref float introSoundTimer = ref npc.Infernum().ExtraAI[10];

            // Player variable.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Fuck.
            if (target.HasBuff(ModContent.BuffType<Nightwither>()))
                target.ClearBuff(ModContent.BuffType<Nightwither>());

            // Play an introduction.
            if (introSoundTimer < IntroSoundLength)
            {
                if (introSoundTimer == 0f)
                    SoundEngine.PlaySound(InfernumSoundRegistry.MoonLordIntroSound, target.Center);
                introSoundTimer++;
            }

            // Reset things.
            npc.dontTakeDamage = NPC.CountNPCS(NPCID.MoonLordFreeEye) < 3;

            // Start the AI and create the arena.
            if (Main.netMode != NetmodeID.MultiplayerClient && npc.localAI[3] == 0f)
            {
                Player closest = Main.player[Player.FindClosest(npc.Center, 1, 1)];
                Point closestTileCoords = closest.Center.ToTileCoordinates();
                npc.Infernum().Arena = new Rectangle((int)closest.position.X - ArenaWidth * 8, (int)closest.position.Y - ArenaHeight * 8 + 20, ArenaWidth * 16, ArenaHeight * 16);
                for (int i = closestTileCoords.X - ArenaWidth / 2; i <= closestTileCoords.X + ArenaWidth / 2; i++)
                {
                    for (int j = closestTileCoords.Y - ArenaHeight / 2; j <= closestTileCoords.Y + ArenaHeight / 2; j++)
                    {
                        // Create arena tiles.
                        if ((Math.Abs(closestTileCoords.X - i) == ArenaWidth / 2 || Math.Abs(closestTileCoords.Y - j) == ArenaHeight / 2) && !Main.tile[i, j].HasTile)
                        {
                            Main.tile[i, j].TileType = (ushort)ModContent.TileType<MoonlordArena>();
                            Main.tile[i, j].Get<TileWallWireStateData>().HasTile = true;
                            if (Main.netMode == NetmodeID.Server)
                                NetMessage.SendTileSquare(-1, i, j, 1, TileChangeType.None);
                            else
                                WorldGen.SquareTileFrame(i, j, true);
                        }
                    }
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int[] bodyPartIndices = new int[3];
                    for (int i = 0; i < 2; i++)
                    {
                        int handIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X + i * 800 - 400, (int)npc.Center.Y - 100, NPCID.MoonLordHand, 0, 0f, 0f, i, npc.whoAmI);
                        bodyPartIndices[i] = handIndex;
                    }

                    int headIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y - 400, NPCID.MoonLordHead, 0, 0f, 0f, 0f, npc.whoAmI);
                    Main.npc[headIndex].netUpdate = true;
                    bodyPartIndices[2] = headIndex;

                    for (int i = 0; i < 3; i++)
                        npc.localAI[i] = bodyPartIndices[i];

                    // Reset hand AIs.
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (Main.npc[i].type == NPCID.MoonLordHand && Main.npc[i].active)
                        {
                            Main.npc[i].ai[0] = 0f;
                            Main.npc[i].netUpdate = true;
                        }
                    }
                }

                npc.localAI[3] = 1f;
                attackTimer = 0f;
                attackState = (int)MoonLordAttackState.SpawnEffects;
                npc.netUpdate = true;
            }

            // Define enragement status.
            npc.Calamity().CurrentlyEnraged = IsEnraged;
            if (wasNotEnraged != npc.Calamity().CurrentlyEnraged.ToInt() && IsEnraged && (MoonLordAttackState)attackState != MoonLordAttackState.DeathEffects)
            {
                var sounds = new SoundStyle[]
                {
                    SoundID.Zombie92,
                    SoundID.Zombie93,
                    SoundID.Zombie94,
                    SoundID.Zombie95,
                    SoundID.Zombie96,
                    SoundID.Zombie97
                };
                foreach (var sound in sounds)
                    SoundEngine.PlaySound(sound with { Volume = 1.2f, Pitch = 0.35f });
            }
            wasNotEnraged = npc.Calamity().CurrentlyEnraged.ToInt();

            // Despawn if the target is not present.
            if (!target.active || target.dead)
            {
                npc.velocity *= 0.9f;
                MoonlordDeathDrama.RequestLight(despawnTimer / 45f, npc.Center);
                despawnTimer++;

                attackState = -1f;
                attackTimer = 0f;
                if (despawnTimer >= 45f)
                {
                    DeleteArena();
                    npc.active = false;
                }
                return false;
            }
            despawnTimer = 0f;

            MoonLordAttackState currentAttack = (MoonLordAttackState)(int)attackState;

            switch (currentAttack)
            {
                case MoonLordAttackState.SpawnEffects:
                    DoBehavior_SpawnEffects(npc, ref attackTimer);
                    break;
                case MoonLordAttackState.DeathEffects:
                    DoBehavior_DeathEffects(npc, ref deathAttackTimer);
                    break;
                case MoonLordAttackState.VoidAccretionDisk:
                    DoBehavior_VoidAccretionDisk(npc, target, ref attackTimer);
                    break;
                default:
                    if ((currentAttack == MoonLordAttackState.PhantasmalFlareBursts ||
                        currentAttack == MoonLordAttackState.PhantasmalSphereHandWaves) && CurrentActiveArms <= 0)
                    {
                        Utilities.DeleteAllProjectiles(true, ProjectileID.PhantasmalSphere);
                        SelectNextAttack(npc);
                    }

                    DoBehavior_IdleHover(npc, target, ref attackTimer);
                    break;
            }
            attackTimer++;

            // Clear projectiles, go to the desperation attack, and do some visual effects when ready to enter the final phase.
            if (npc.Infernum().ExtraAI[8] == 0f && InFinalPhase)
            {
                SoundEngine.PlaySound(SoundID.Zombie92 with { Volume = 0.9f, Pitch = -0.48f }, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<MoonLordWave>(), 0, 0f);

                ClearAllProjectiles();
                SelectNextAttack(npc);
            }

            // Forcefully switch attacks if the mechanism variable for it is activated.
            // This is intended to be used by the arms/head directly and not inside in-class behavior states.
            if (forcefullySwitchAttack == 1f)
            {
                SelectNextAttack(npc);
                forcefullySwitchAttack = 0f;
            }

            // Update other limbs if the core is supposed to sync.
            if (npc.netUpdate)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    bool isBodyPart = n.type is NPCID.MoonLordHand or NPCID.MoonLordHead or NPCID.MoonLordFreeEye;
                    if (n.active && n.ai[3] == npc.whoAmI && isBodyPart)
                    {
                        n.netSpam = npc.netSpam;
                        n.netUpdate = true;
                    }
                }
            }

            return false;
        }

        public static void HandleBodyPartDeathTriggers(NPC npc, double realDamage)
        {
            int minLife = (int)(npc.lifeMax * 0.18) + 1;
            if (npc.life - realDamage > minLife)
                return;

            npc.life = 0;

            if (Main.netMode != NetmodeID.MultiplayerClient)
                npc.StrikeInstantKill();
            npc.checkDead();
        }

        public static void DoBehavior_SpawnEffects(NPC npc, ref float attackTimer)
        {
            // Don't take damage during spawn effects.
            npc.dontTakeDamage = true;

            // Roar after a bit of time has passed.
            if (attackTimer == 30f)
                SoundEngine.PlaySound(SoundID.Zombie92, npc.Center);

            if (attackTimer >= 125f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_DeathEffects(NPC npc, ref float attackTimer)
        {
            npc.Calamity().ShouldCloseHPBar = true;
            npc.life = 1;
            npc.velocity = Vector2.Lerp(npc.velocity, -Vector2.UnitY * 0.4f, 0.35f);
            npc.dontTakeDamage = true;

            // Create a flash before anything else.
            if (attackTimer < 60f)
            {
                if (attackTimer == 4f)
                    SoundEngine.PlaySound(SoundID.NPCDeath61, npc.Center);

                MoonlordDeathDrama.RequestLight(attackTimer / 60f, npc.Center);
            }
            else
            {
                if (attackTimer == 61f)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<MoonLordDeathAnimationHandler>(), 0, 0f, -1, npc.whoAmI);
                }

                // Create explosions periodically.
                float explosionCreationRate = Lerp(0.075f, 0.24f, Utils.GetLerpValue(75f, 300f, attackTimer, true));
                if (Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextFloat() < explosionCreationRate)
                {
                    Vector2 explosionSpawnPosition = npc.Center + Main.rand.NextVector2Circular(200f, 450f);
                    Utilities.NewProjectileBetter(explosionSpawnPosition, Vector2.Zero, ModContent.ProjectileType<MoonLordExplosion>(), 0, 0f);
                }
                MoonlordDeathDrama.RequestLight(Utils.GetLerpValue(480f, 530f, attackTimer, true) * 8f, npc.Center);
            }

            if (attackTimer >= 550f)
            {
                // Release a bunch of blood particles everywhere.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 100; i++)
                    {
                        Vector2 bloodSpawnPosition = npc.Center + Main.rand.NextVector2Circular(120f, 250f);
                        Vector2 bloodShootVelocity = -Vector2.UnitY.RotatedByRandom(1.4f) * Main.rand.NextFloat(6f, 23f);
                        Utilities.NewProjectileBetter(bloodSpawnPosition, bloodShootVelocity, ModContent.ProjectileType<MoonLordDeathBloodBlob>(), 0, 0f);
                    }
                }

                DeleteArena();
                MoonlordDeathDrama.ThrowPieces(npc.Center, npc.whoAmI);
                npc.life = 0;
                npc.NPCLoot();
                npc.active = false;
            }

            attackTimer++;
        }

        public static void DoBehavior_VoidAccretionDisk(NPC npc, Player target, ref float attackTimer)
        {
            DoBehavior_IdleHover(npc, target, ref attackTimer);

            // Create the black hole.
            if (attackTimer == 1f)
            {
                HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.MoonLordBlackHoleTip");

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<VoidBlackHole>(), BlackHoleDamage, 0f, -1, 0f, npc.whoAmI);
            }

            if (attackTimer >= 540f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_IdleHover(NPC npc, Player target, ref float attackTimer)
        {
            float verticalOffset = Lerp(0f, 45f, Cos(attackTimer / 32f) * 0.5f + 0.5f);
            Vector2 hoverDestination = target.Center - Vector2.UnitY * verticalOffset;
            Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * BaseFlySpeedFactor * 0.75f;
            npc.SimpleFlyMovement(idealVelocity, BaseFlySpeedFactor / 20f);
            npc.velocity = npc.velocity.MoveTowards(idealVelocity, BaseFlySpeedFactor / 60f);
        }

        public static void SelectNextAttack(NPC npc)
        {
            npc.ai[1] = 0f;

            int eyeCount = NPC.CountNPCS(NPCID.MoonLordFreeEye);
            float lifeRatio = npc.life / (float)npc.lifeMax;

            MoonLordAttackState[] attackCycle = new MoonLordAttackState[]
            {
                !EyeIsActive && eyeCount >= 2 ? MoonLordAttackState.PhantasmalDance :MoonLordAttackState.PhantasmalBoltEyeBursts,
                MoonLordAttackState.PhantasmalSphereHandWaves,
                eyeCount == 1 ? MoonLordAttackState.PhantasmalSpin : MoonLordAttackState.PhantasmalFlareBursts,
                !EyeIsActive && eyeCount >= 2 ? MoonLordAttackState.PhantasmalRush : MoonLordAttackState.PhantasmalDeathrays,
            };
            if (CurrentActiveArms <= 0 && npc.ai[0] != (int)MoonLordAttackState.SpawnEffects)
            {
                attackCycle = new MoonLordAttackState[]
                {
                    MoonLordAttackState.PhantasmalDance,
                    MoonLordAttackState.PhantasmalBoltEyeBursts,
                    MoonLordAttackState.PhantasmalDeathrays,
                    MoonLordAttackState.PhantasmalRush,
                };
            }
            if (eyeCount >= 3)
            {
                attackCycle = new MoonLordAttackState[]
                {
                    MoonLordAttackState.PhantasmalDance,
                    MoonLordAttackState.PhantasmalRush,
                    MoonLordAttackState.PhantasmalBarrage,
                    MoonLordAttackState.ExplodingConstellations,
                };

                if (lifeRatio < Phase2LifeRatio)
                {
                    attackCycle = new MoonLordAttackState[]
                    {
                        MoonLordAttackState.PhantasmalDance,
                        MoonLordAttackState.PhantasmalRush,
                        MoonLordAttackState.PhantasmalBarrage,
                        MoonLordAttackState.ExplodingConstellations,
                        MoonLordAttackState.PhantasmalDance,
                        MoonLordAttackState.PhantasmalWrath,
                        MoonLordAttackState.PhantasmalBarrage,
                        MoonLordAttackState.PhantasmalWrath,
                    };
                }
            }

            npc.ai[0] = (int)attackCycle[(int)npc.Infernum().ExtraAI[7] % attackCycle.Length];

            // If the third phase was just reached, use the void accretion disk attack next.
            if (npc.Infernum().ExtraAI[8] == 0f && lifeRatio < Phase3LifeRatio)
            {
                npc.ai[0] = (int)MoonLordAttackState.VoidAccretionDisk;
                npc.Infernum().ExtraAI[8] = 1f;
                npc.Infernum().ExtraAI[7] = 0f;
            }

            // Use the void accretion disk for every fourth attack when in the third phase.
            if (npc.Infernum().ExtraAI[7] % 4f == 3f && lifeRatio < Phase3LifeRatio)
                npc.ai[0] = (int)MoonLordAttackState.VoidAccretionDisk;

            npc.Infernum().ExtraAI[7]++;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                bool isBodyPart = n.type is NPCID.MoonLordHand or NPCID.MoonLordHead or NPCID.MoonLordFreeEye;
                if (n.active && n.ai[3] == npc.whoAmI && isBodyPart)
                {
                    for (int j = 0; j < 5; j++)
                        n.Infernum().ExtraAI[i] = 0f;
                }
            }

            npc.netUpdate = true;
        }

        public static void ClearAllProjectiles()
        {
            int[] projectilesToDelete = new int[]
            {
                ProjectileID.PhantasmalBolt,
                ProjectileID.PhantasmalSphere,
                ProjectileID.PhantasmalEye,
                ModContent.ProjectileType<LunarAsteroid>(),
                ModContent.ProjectileType<LunarFireball>(),
                ModContent.ProjectileType<LunarFlare>(),
                ModContent.ProjectileType<LunarFlareTelegraph>(),
                ModContent.ProjectileType<PhantasmalDeathray>(),
                ModContent.ProjectileType<PhantasmalOrb>(),
                ModContent.ProjectileType<StardustConstellation>(),
                ModContent.ProjectileType<VoidBlackHole>(),
            };
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (projectilesToDelete.Contains(Main.projectile[i].type))
                    Main.projectile[i].active = false;
            }
        }

        public static void DeleteArena()
        {
            int arenaTileID = ModContent.TileType<MoonlordArena>();
            for (int i = 0; i < Main.maxTilesX; i++)
            {
                for (int j = 0; j < Main.maxTilesY; j++)
                {
                    if (Main.tile[i, j].TileType != arenaTileID || !Main.tile[i, j].HasTile)
                        continue;

                    Main.tile[i, j].TileType = TileID.Dirt;
                    Main.tile[i, j].Get<TileWallWireStateData>().HasTile = false;
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendTileSquare(-1, i, j, 1, TileChangeType.None);
                    else
                        WorldGen.SquareTileFrame(i, j, true);
                }
            }
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            // Hideous code from vanilla. Don't mind it too much.
            Texture2D coreTexture = TextureAssets.Npc[npc.type].Value;
            Texture2D coreOutlineTexture = TextureAssets.Extra[16].Value;
            Texture2D forearmTexture = TextureAssets.Extra[14].Value;
            Texture2D bodyTexture = TextureAssets.Extra[13].Value;
            Vector2 leftHalfOrigin = new(bodyTexture.Width, 278f);
            Vector2 rightHalfOrigin = new(0f, 278f);
            Vector2 center = npc.Center;
            Point coreTileCoords = (npc.Center + new Vector2(0f, -150f)).ToTileCoordinates();
            Color color = npc.GetAlpha(Color.Lerp(Lighting.GetColor(coreTileCoords.X, coreTileCoords.Y), Color.White, 0.3f));
            for (int a = 0; a < 2; a++)
            {
                int armIndex = -1;
                bool leftArm = a == 0;
                Vector2 directionThing = new((!leftArm).ToDirectionInt(), 1f);
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].type == NPCID.MoonLordHand && Main.npc[i].ai[2] == a && Main.npc[i].ai[3] == npc.whoAmI)
                    {
                        armIndex = i;
                        break;
                    }
                }
                if (armIndex != -1)
                {
                    Vector2 shoulderPosition = center + new Vector2(220f, -60f) * directionThing;
                    Vector2 shoulderOffset = (Main.npc[armIndex].Center + new Vector2(0f, 76f) - shoulderPosition) * 0.5f;
                    float rotationalOffset = Acos(Clamp(shoulderOffset.Length() / 340f, 0f, 1f)) * -directionThing.X;
                    float forearmRotation = shoulderOffset.ToRotation() - rotationalOffset - PiOver2;
                    SpriteEffects direction = !leftArm ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                    Vector2 forearmOrigin = new(76f, 66f);
                    if (!leftArm)
                        forearmOrigin.X = forearmTexture.Width - forearmOrigin.X;

                    Main.spriteBatch.Draw(forearmTexture, shoulderPosition - Main.screenPosition, null, color, forearmRotation, forearmOrigin, 1f, direction, 0f);
                }
            }
            Main.spriteBatch.Draw(bodyTexture, center - Main.screenPosition, null, color, 0f, leftHalfOrigin, 1f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(bodyTexture, center - Main.screenPosition, null, color, 0f, rightHalfOrigin, 1f, SpriteEffects.FlipHorizontally, 0f);
            Main.spriteBatch.Draw(coreOutlineTexture, center - Main.screenPosition, null, color, 0f, new Vector2(112f, 101f), 1f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(coreTexture, center - Main.screenPosition, npc.frame, color, 0f, npc.frame.Size() / 2f, 1f, SpriteEffects.None, 0f);
            return false;
        }

        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Mods.InfernumMode.PetDialog.MoonLordTip2";
            yield return n =>
            {
                if (NPC.CountNPCS(NPCID.MoonLordFreeEye) >= 2)
                    return "Mods.InfernumMode.PetDialog.MoonLordTip1";
                return string.Empty;
            };
            yield return n =>
            {
                if (TipsManager.ShouldUseJokeText)
                    return "Mods.InfernumMode.PetDialog.MoonLordJokeTip1";
                return string.Empty;
            };
        }
    }
}
