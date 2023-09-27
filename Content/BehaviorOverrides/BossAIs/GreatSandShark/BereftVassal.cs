using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Items.Placeables.Furniture.DevPaintings;
using CalamityMod.Items.SummonItems;
using CalamityMod.Particles;
using InfernumMode.Assets.Sounds;
using InfernumMode.Content.Achievements;
using InfernumMode.Content.BossBars;
using InfernumMode.Content.Items.Accessories;
using InfernumMode.Content.Items.BossBags;
using InfernumMode.Content.Items.LoreItems;
using InfernumMode.Content.Items.Misc;
using InfernumMode.Content.Items.Placeables;
using InfernumMode.Content.Items.Weapons.Magic;
using InfernumMode.Content.Items.Weapons.Melee;
using InfernumMode.Content.Items.Weapons.Ranged;
using InfernumMode.Content.Items.Weapons.Rogue;
using InfernumMode.Content.Subworlds;
using InfernumMode.Core.CrossCompatibility;
using InfernumMode.Core.GlobalInstances.Players;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SubworldLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.GreatSandShark.BereftVassalComboAttackManager;
using GreatSandSharkNPC = CalamityMod.NPCs.GreatSandShark.GreatSandShark;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.GreatSandShark
{
    [AutoloadBossHead]
    public class BereftVassal : ModNPC, IBossChecklistHandler
    {
        public enum BereftVassalAttackType
        {
            IdleState,
            SandBlobSlam,
            LongHorizontalCharges,
            SpearWaterTorrent,
            WaterWaveSlam,
            FallingWaterCastBarrges,
            SandnadoPressureCharges,
            HypersonicWaterSlashes,
            SummonGreatSandShark,
            TransitionToFinalPhase,
            RetreatAnimation
        }

        public enum BereftVassalFrameType
        {
            Idle,
            BlowHorn,
            Jump,
            Kneel
        }

        public float AngerInterpolant;

        public float LineTelegraphDirection;

        public float ElectricShieldOpacity;

        public ThanatosSmokeParticleSet SmokeDrawer = new(-1, 3, 0f, 16f, 1.5f);

        public Player Target => Main.player[NPC.target];

        public bool TargetIsOutsideOfColosseum => Target.Center.X < 18400f && SubworldSystem.IsActive<LostColosseum>();

        public BereftVassalAttackType CurrentAttack
        {
            get => (BereftVassalAttackType)NPC.ai[0];
            set => NPC.ai[0] = (int)value;
        }

        public bool HasBegunSummoningGSS
        {
            get => NPC.Infernum().ExtraAI[5] == 1f;
            set => NPC.Infernum().ExtraAI[5] = value.ToInt();
        }

        public BereftVassalFrameType FrameType
        {
            get => (BereftVassalFrameType)NPC.localAI[3];
            set => NPC.localAI[3] = (int)value;
        }

        public bool Enraged => FightState == BereftVassalFightState.EnragedBereftVassal && CurrentAttack != BereftVassalAttackType.RetreatAnimation && CurrentAttack != BereftVassalAttackType.TransitionToFinalPhase;

        public ref float AttackTimer => ref NPC.ai[1];

        public ref float SpearRotation => ref NPC.localAI[0];

        public ref float SpearOpacity => ref NPC.localAI[1];

        public ref float CurrentFrame => ref NPC.localAI[2];

        public ref float LineTelegraphIntensity => ref NPC.localAI[3];

        public static BereftVassalAttackType[] Phase1AttackCycle => new BereftVassalAttackType[]
        {
            BereftVassalAttackType.SandBlobSlam,
            BereftVassalAttackType.SpearWaterTorrent,
            BereftVassalAttackType.WaterWaveSlam,
            BereftVassalAttackType.SandBlobSlam,
            BereftVassalAttackType.FallingWaterCastBarrges,
            BereftVassalAttackType.WaterWaveSlam,
        };

        public static BereftVassalAttackType[] Phase2AttackCycle => new BereftVassalAttackType[]
        {
            BereftVassalAttackType.SandBlobSlam,
            BereftVassalAttackType.SandnadoPressureCharges,
            BereftVassalAttackType.HypersonicWaterSlashes,
            BereftVassalAttackType.LongHorizontalCharges,
            BereftVassalAttackType.SpearWaterTorrent,
            BereftVassalAttackType.HypersonicWaterSlashes,
            BereftVassalAttackType.WaterWaveSlam,
            BereftVassalAttackType.SandnadoPressureCharges,
            BereftVassalAttackType.SandBlobSlam,
            BereftVassalAttackType.FallingWaterCastBarrges,
            BereftVassalAttackType.WaterWaveSlam,
        };

        public const float Phase2LifeRatio = 0.6f;

        // Boss Checklist things.
        public string BossTitle => "Bereft Vassal";

        // A little bit after Astrum Deus.
        public float ProgressionValue => 17.75f;

        public List<int> CollectibleItems => new()
        {
            ModContent.ItemType<BereftVassalTrophy>(),
            ModContent.ItemType<KnowledgeBereftVassal>(),
            ModContent.ItemType<WaterglassToken>(),
            ModContent.ItemType<ThankYouPainting>(),
        };

        public int? SpawnItem => ModContent.ItemType<SandstormsCore>();

        public string SpawnRequirement => $"Use a [i:{SpawnItem.Value}] at the pedestal in the heart of the desert.";

        public string DespawnMessage => CalamityUtils.ColorMessage("Argus returns to quiet solitude at the center of the Colosseum.", new(28, 175, 189));

        public bool AvailabilityCondition => NPC.downedAncientCultist;

        public bool DefeatCondition => WorldSaveSystem.DownedBereftVassal;

        public string HeadIconPath => "InfernumMode/Content/BehaviorOverrides/BossAIs/GreatSandShark/BereftVassal_Head_Boss";

        public List<int> ExtraNPCIDs => new()
        {
            ModContent.NPCType<GreatSandSharkNPC>()
        };

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Argus, the Bereft Vassal");
            Main.npcFrameCount[Type] = 4;
            NPCID.Sets.TrailingMode[NPC.type] = 3;
            NPCID.Sets.TrailCacheLength[NPC.type] = 10;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 5f;
            NPC.damage = 100;
            NPC.width = 30;
            NPC.height = 44;
            NPC.defense = 12;
            NPC.LifeMaxNERB(124000, 124000, 800000);

            // Fuck arbitrary Expert boosts.
            NPC.lifeMax /= 2;

            double HPBoost = CalamityConfig.Instance.BossHealthBoost * 0.01;
            NPC.lifeMax += (int)(NPC.lifeMax * HPBoost);
            NPC.aiStyle = -1;
            AIType = -1;
            NPC.knockBackResist = 0f;
            NPC.canGhostHeal = false;
            NPC.boss = true;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.HitSound = null;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.value = Item.buyPrice(6, 25, 0, 0) / 5;
            NPC.netAlways = true;
            NPC.BossBar = ModContent.GetInstance<BereftVassalBossBar>();

            NPC.Calamity().ShouldCloseHPBar = true;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Desert,
                new FlavorTextBestiaryInfoElement("A vigilant guardian, once wandering without a purpose. Having learned that his king lives on, it'd seem that he has started to regain his will to live. He looks forward to fighting you again.")
            });
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(NPC.Opacity);
            writer.Write(NPC.rotation);
            writer.Write(LineTelegraphDirection);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            NPC.Opacity = reader.ReadSingle();
            NPC.rotation = reader.ReadSingle();
            LineTelegraphDirection = reader.ReadSingle();
        }

        public override void AI()
        {
            // Pick a target if no valid one exists.
            NPC.TargetClosestIfTargetIsInvalid();

            // Reset things every frame.
            bool sandSharkExists = NPC.AnyNPCs(ModContent.NPCType<GreatSandSharkNPC>());
            NPC.damage = NPC.defDamage;
            NPC.dontTakeDamage = false;
            NPC.noTileCollide = false;
            NPC.noGravity = false;
            NPC.chaseable = !sandSharkExists;
            NPC.Calamity().DR = sandSharkExists ? 0.999999f : 0f;
            NPC.Calamity().ShouldCloseHPBar = CurrentAttack == BereftVassalAttackType.IdleState || sandSharkExists;

            // Teleport above the target if stuck and alone.
            if (!sandSharkExists && NPC.Center.Y >= Main.maxTilesY * 16f - 500f)
            {
                NPC.Center = Target.Center - Vector2.UnitY * 700f;
                NPC.netUpdate = true;
            }

            // Disable DoTs because they're apparently overpowered enough to do damage while supposedly invulnerable.
            if (sandSharkExists)
                NPC.lifeRegen = 1000000;

            // Ensure that the player receives the boss effects buff.
            NPC.Calamity().KillTime = 1800;

            ElectricShieldOpacity = Clamp(ElectricShieldOpacity + (NPC.Calamity().DR > 0.99f).ToDirectionInt() * 0.015f, 0f, 1f);

            // Go away if the target is dead or left the Colosseum.
            if ((!Target.active || Target.dead || TargetIsOutsideOfColosseum) && CurrentAttack != BereftVassalAttackType.IdleState)
            {
                for (int i = 0; i < 8; i++)
                {
                    Vector2 firePosition = NPC.Center + Main.rand.NextVector2Circular(50f, 50f);
                    float fireScale = Main.rand.NextFloat(1f, 1.32f);
                    float fireRotationSpeed = Main.rand.NextFloat(-0.05f, 0.05f);

                    var particle = new HeavySmokeParticle(firePosition, Vector2.Zero, Color.Cyan, 50, fireScale, 1, fireRotationSpeed, true, 0f, true);
                    GeneralParticleHandler.SpawnParticle(particle);
                }

                NPC.active = false;
                LostColosseum.HasBereftVassalAppeared = false;
            }

            // Change the sunset based on fight progression if inside of the subworld.
            if (SubworldSystem.IsActive<LostColosseum>())
                LostColosseum.SunsetInterpolant = 1f - NPC.life / (float)NPC.lifeMax;

            // Constantly give the target Weak Pertrification.
            if (Main.netMode != NetmodeID.Server && CurrentAttack != BereftVassalAttackType.IdleState)
            {
                if (!Target.dead && Target.active)
                    Target.AddBuff(ModContent.BuffType<WeakPetrification>(), 15);
            }

            // Stay inside of the world.
            NPC.Center = Vector2.Clamp(NPC.Center, Vector2.One * 150f, Vector2.One * new Vector2(Main.maxTilesX * 16f - 150f, Main.maxTilesY * 16f - 150f));

            // Reset frames.
            NPC.frameCounter++;
            FrameType = BereftVassalFrameType.Idle;
            CurrentFrame = (int)(NPC.frameCounter / 5f % Main.npcFrameCount[Type]);

            // Make the anger interpolant decrease over time.
            AngerInterpolant = Clamp(AngerInterpolant - 0.05f, 0f, 1f);

            // Do not despawn, you bastard.
            NPC.timeLeft = 7200;

            switch (CurrentAttack)
            {
                case BereftVassalAttackType.IdleState:
                    DoBehavior_IdleState();
                    break;
                case BereftVassalAttackType.SandBlobSlam:
                    DoBehavior_SandBlobSlam();
                    break;
                case BereftVassalAttackType.LongHorizontalCharges:
                    DoBehavior_LongHorizontalCharges();
                    break;
                case BereftVassalAttackType.SpearWaterTorrent:
                    DoBehavior_SpearWaterTorrent();
                    break;
                case BereftVassalAttackType.WaterWaveSlam:
                    DoBehavior_WaterWaveSlam();
                    break;
                case BereftVassalAttackType.FallingWaterCastBarrges:
                    DoBehavior_FallingWaterCastBarrges();
                    break;
                case BereftVassalAttackType.SandnadoPressureCharges:
                    DoBehavior_SandnadoPressureCharges();
                    break;
                case BereftVassalAttackType.HypersonicWaterSlashes:
                    DoBehavior_HypersonicWaterSlashes();
                    break;
                case BereftVassalAttackType.SummonGreatSandShark:
                    DoBehavior_SummonGreatSandShark();
                    break;
                case BereftVassalAttackType.TransitionToFinalPhase:
                    DoBehavior_TransitionToFinalPhase();
                    break;
                case BereftVassalAttackType.RetreatAnimation:
                    DoBehavior_RetreatAnimation();
                    break;
            }

            if (Main.netMode != NetmodeID.SinglePlayer && CurrentAttack != BereftVassalAttackType.IdleState)
            {
                Main.hideUI = false;
                Main.blockInput = false;
            }

            // Become pissed once the Great Sand Shark is dead.
            if (FightState == BereftVassalFightState.EnragedBereftVassal && CurrentAttack != BereftVassalAttackType.SummonGreatSandShark && NPC.ai[2] == 0f)
            {
                SelectNextAttack();
                CurrentAttack = BereftVassalAttackType.TransitionToFinalPhase;
                NPC.ai[2] = 1f;
            }

            // Create a sandstorm when in the last phase.
            if (Enraged)
                CreateSandstormParticle(Target.Center.X < NPC.Center.X);

            // Update the smoke drawer.
            SmokeDrawer.ParticleSpawnRate = AngerInterpolant > 0.4f ? 3 : int.MaxValue;
            SmokeDrawer.BaseMoveRotation = PiOver2 + Main.rand.NextFloatDirection() * 0.05f;
            SmokeDrawer.Update();

            if (!HasBegunSummoningGSS && NPC.life < NPC.lifeMax * Phase2LifeRatio && CurrentAttack != BereftVassalAttackType.RetreatAnimation)
            {
                SelectNextAttack();
                CurrentAttack = BereftVassalAttackType.SummonGreatSandShark;
                HasBegunSummoningGSS = true;
            }
            DoComboAttacksIfNecessary(NPC, Target, ref AttackTimer);
            if (NPC.ai[0] >= 100f && FightState != BereftVassalFightState.BereftVassalAndGSS)
                SelectNextAttack();

            AttackTimer++;
        }

        public void DoBehavior_IdleState()
        {
            int animationFocusTime = 54;
            int spearSpinTime = 67;
            int spearStrikeTime = 45;
            int animationFocusReturnTime = 14;
            int animationTime = spearSpinTime + spearStrikeTime + animationFocusReturnTime;
            ref float hasBegunAnimation = ref NPC.Infernum().ExtraAI[0];


            // Disable damage entirely.
            NPC.dontTakeDamage = true;
            NPC.damage = 0;

            // Look away from the target if not performing an animation.
            if (hasBegunAnimation == 0f)
            {
                AttackTimer = 0f;
                NPC.spriteDirection = (Target.Center.X > NPC.Center.X).ToDirectionInt();
            }

            // Begin the animation once the player is sufficiently close to the vassal.
            if (NPC.WithinRange(Target.Center, 700f) && hasBegunAnimation == 0f)
            {
                hasBegunAnimation = 1f;
                SpearRotation = NPC.AngleTo(Target.Center) + PiOver4;
                BlockerSystem.Start(true, true, () =>
                {
                    int index = NPC.FindFirstNPC(Type);
                    if (Main.npc.IndexInRange(index))
                    {
                        NPC vassal = Main.npc[index];
                        if ((BereftVassalAttackType)vassal.ai[0] == BereftVassalAttackType.IdleState)
                            return true;
                    }
                    return false;
                });
                NPC.netUpdate = true;
            }

            // Using kneeling frames.
            FrameType = BereftVassalFrameType.Kneel;
            CurrentFrame = 0f;

            // Get rid of any and all adrenaline.
            Target.Calamity().adrenaline = 0f;

            if (hasBegunAnimation == 0f)
                return;

            // Have the camera zoom in on the vassal once the animation begins.
            Target.Infernum_Camera().ScreenFocusInterpolant = Utils.GetLerpValue(2f, animationFocusTime, AttackTimer, true);
            Target.Infernum_Camera().ScreenFocusInterpolant *= Utils.GetLerpValue(0f, -animationFocusReturnTime, AttackTimer - animationFocusTime - animationTime, true);
            Target.Infernum_Camera().ScreenFocusPosition = NPC.Center;

            // Spin the spear.
            int animationTimer = (int)(AttackTimer - animationFocusTime);
            if (animationTimer < 0)
            {
                CurrentFrame = Utils.Remap(AttackTimer, 0f, animationFocusTime, 0f, 4f);
                return;
            }

            // Use idle frames when done kneeling.
            FrameType = BereftVassalFrameType.Idle;

            if (animationTimer <= spearSpinTime)
            {
                if (animationTimer == 6)
                    SoundEngine.PlaySound(InfernumSoundRegistry.MyrindaelSpinSound, NPC.Center);

                SpearOpacity = Utils.GetLerpValue(0f, 16f, animationTimer, true);
                SpearRotation += Pi / spearSpinTime * 10f;
            }

            // Look towards the target before leaping at them.
            else if (animationTimer <= spearSpinTime + spearStrikeTime)
            {
                // Look at the target and aim the spear at them.
                SpearRotation = SpearRotation.AngleLerp(NPC.AngleTo(Target.Center) + PiOver4, 0.2f);
                NPC.spriteDirection = (Target.Center.X < NPC.Center.X).ToDirectionInt();

                // Jump into the air.
                if (animationTimer == spearSpinTime + spearStrikeTime - 10)
                {
                    SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode, NPC.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Utilities.NewProjectileBetter(NPC.Bottom, Vector2.UnitX * NPC.spriteDirection * 8f, ProjectileID.DD2OgreSmash, 0, 0f);
                        NPC.velocity = new Vector2((Target.Center.X > NPC.Center.X).ToDirectionInt() * 19f, -23f);
                        NPC.netUpdate = true;
                    }
                }

                // Change music.
                SceneEffectPriority = SceneEffectPriority.BossHigh;
                Music = MusicID.OldOnesArmy;
                if (ModLoader.TryGetMod("InfernumModeMusic", out Mod musicMod))
                    Music = MusicLoader.GetMusicSlot(musicMod, "Sounds/Music/BereftVassal");
            }

            if (AttackTimer >= animationTime + animationFocusReturnTime)
                SelectNextAttack();
        }

        public void DoBehavior_SandBlobSlam()
        {
            int chargeCount = 2;
            int repositionInterpolationTime = 22;
            int sandBlobCount = 30;
            int sandBlobCount2 = 0;
            int slamDelay = 36;
            int attackTransitionDelay = 60;
            float slamSpeed = 30f;
            float sandBlobAngularArea = 0.74f;
            float sandBlobSpeed = 22f;
            float maxFlySpeed = 22f;

            if (Enraged)
            {
                chargeCount++;
                repositionInterpolationTime -= 7;
                sandBlobCount += 5;
                sandBlobCount2 += 9;
                slamDelay -= 14;
                attackTransitionDelay -= 27;
                slamSpeed += 4f;
                maxFlySpeed += 9f;
            }

            ref float attackSubstate = ref NPC.Infernum().ExtraAI[0];
            ref float startingTargetPositionY = ref NPC.Infernum().ExtraAI[1];
            ref float frameTimer = ref NPC.Infernum().ExtraAI[2];
            ref float chargeCounter = ref NPC.Infernum().ExtraAI[3];

            // Disable gravity and tile collision universally during this attack.
            // All of these things are applied manually.
            NPC.noTileCollide = true;
            NPC.noGravity = true;

            // Use jump frames.
            FrameType = BereftVassalFrameType.Jump;

            // Hover into position, above the target.
            if (attackSubstate == 0f)
            {
                frameTimer++;
                CurrentFrame = (int)(frameTimer / 4f * 8.5f);

                // If the attack goes on for longer than expected the vassal will interpolant towards the destination faster and faster until it's eventually reached.
                float flySpeedInterpolant = Utils.GetLerpValue(0f, repositionInterpolationTime, AttackTimer, true);
                float positionIncrement = Lerp(0.32f, 7f, flySpeedInterpolant) + (AttackTimer - repositionInterpolationTime) * 0.26f;
                float flySpeed = Lerp(2f, maxFlySpeed, flySpeedInterpolant);
                Vector2 hoverDestination = Target.Center - Vector2.UnitY * 275f;

                // Apply manual movement calculations before determining the ideal velocity, to ensure that there is not a one-frame buffer between what the velocity thinks the current position is
                // versus what it actually is.
                NPC.Center = NPC.Center.MoveTowards(hoverDestination, positionIncrement);
                if (!NPC.WithinRange(hoverDestination, 1000f))
                    NPC.Center = Vector2.Lerp(NPC.Center, hoverDestination, 0.05f);

                // Perform movement calculations.
                Vector2 idealVelocity = NPC.SafeDirectionTo(hoverDestination) * flySpeed;
                NPC.velocity = Vector2.Lerp(NPC.velocity, idealVelocity, 0.065f);

                // Transition to the hover state once sufficiently close to the hover destination and the reposition interpolant has been maxed out.
                if (NPC.WithinRange(hoverDestination, 60f) && flySpeedInterpolant >= 1f)
                {
                    AttackTimer = 0f;
                    attackSubstate = 1f;
                    NPC.Center = hoverDestination;
                    NPC.velocity = Vector2.Zero;
                    NPC.netUpdate = true;
                    return;
                }

                // Look at the target.
                if (Distance(Target.Center.X, NPC.Center.X) > 25f)
                    NPC.spriteDirection = (Target.Center.X < NPC.Center.X).ToDirectionInt();

                // Disable contact damag when rising upward.
                NPC.damage = 0;
                return;
            }

            // Hover in place for a short period of time.
            if (attackSubstate == 1f)
            {
                // Spin the spear such that it points downward.
                SpearRotation = SpearRotation.AngleLerp(Pi - PiOver4, 0.12f);
                SpearOpacity = Lerp(SpearOpacity, 1f, 0.1f);

                // Disable contact damag when hovering in place.
                NPC.damage = 0;

                // Decide frames.
                frameTimer = 0f;
                CurrentFrame = 8f;

                // Slam downward after enough time has passed and cache the target's current Y position for later.
                if (AttackTimer >= slamDelay)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.VassalSlashSound, NPC.Center);

                    attackSubstate = 2f;
                    startingTargetPositionY = Target.Center.Y;
                    AttackTimer = 0f;
                    NPC.velocity = Vector2.UnitY * slamSpeed * 0.3f;
                    NPC.netUpdate = true;
                }
                return;
            }

            // Slam downward.
            if (attackSubstate == 2f)
            {
                // Perform acceleration by rapidly interpolating towards terminal velocity.
                NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.UnitY * slamSpeed, 0.12f);

                // Decide frames.
                CurrentFrame = 9f;
                frameTimer = 0f;

                CreateMotionStreakParticles();

                // Check for collision. This does not apply if current above the target's bottom.
                bool hasHitGround = Collision.SolidCollision(NPC.BottomRight - Vector2.UnitY * 4f, NPC.width, 6, true);
                bool ignoreTiles = NPC.Bottom.Y < startingTargetPositionY;
                bool pretendFakeCollisionHappened = NPC.Bottom.Y >= Target.Bottom.Y + 900f;
                if (hasHitGround && !ignoreTiles || pretendFakeCollisionHappened)
                {
                    // Perform ground hit effects once a collision is registered. This involves releasing sand rubble into the air and creating a damaging ground area of effect.
                    SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode, NPC.Bottom);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 sandSpawnPosition = NPC.Center + new Vector2(Main.rand.NextFloatDirection() * 6f, Main.rand.NextFloat(12f));
                        for (int i = 0; i < sandBlobCount; i++)
                        {
                            float sandVelocityOffsetAngle = Lerp(-sandBlobAngularArea, sandBlobAngularArea, i / (float)(sandBlobCount - 1f));

                            // Add a small amount of variance to the sane velocity, to make it require a bit of dynamic reaction.
                            sandVelocityOffsetAngle += Main.rand.NextFloatDirection() * 0.04f;

                            Vector2 sandVelocity = -Vector2.UnitY.RotatedBy(sandVelocityOffsetAngle) * sandBlobSpeed;

                            ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(blob =>
                            {
                                blob.ModProjectile<SandBlob>().StartingYPosition = Target.Bottom.Y;
                            });
                            Utilities.NewProjectileBetter(sandSpawnPosition, sandVelocity, ModContent.ProjectileType<SandBlob>(), SandBlobDamage, 0f);
                        }

                        // Release second spread of sand that goes higher up if necessary.
                        if (sandBlobCount2 >= 1)
                        {
                            for (int i = 0; i < sandBlobCount2; i++)
                            {
                                float sandVelocityOffsetAngle = Lerp(-sandBlobAngularArea, sandBlobAngularArea, i / (float)(sandBlobCount2 - 1f));

                                // Add a small amount of variance to the sane velocity, to make it require a bit of dynamic reaction.
                                sandVelocityOffsetAngle += Main.rand.NextFloatDirection() * 0.04f;
                                Vector2 sandVelocity = -Vector2.UnitY.RotatedBy(sandVelocityOffsetAngle) * sandBlobSpeed * 1.18f;

                                ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(blob =>
                                {
                                    blob.ModProjectile<SandBlob>().StartingYPosition = Target.Bottom.Y;
                                });
                                Utilities.NewProjectileBetter(sandSpawnPosition, sandVelocity, ModContent.ProjectileType<SandBlob>(), SandBlobDamage, 0f);
                            }
                        }

                        Utilities.NewProjectileBetter(NPC.Bottom, Vector2.UnitX * NPC.spriteDirection * 8f, ProjectileID.DD2OgreSmash, 0, 0f);
                    }

                    AttackTimer = pretendFakeCollisionHappened ? attackTransitionDelay - 36f : 0f;
                    attackSubstate = 3f;
                    NPC.velocity.Y = 0f;
                    while (Collision.SolidCollision(NPC.BottomRight - Vector2.UnitY * 4f, NPC.width, 6, true) && !pretendFakeCollisionHappened)
                        NPC.position.Y--;

                    NPC.netUpdate = true;
                }
                return;
            }

            // Return gravity and tile collision back to normal.
            NPC.noGravity = false;
            NPC.noTileCollide = false;

            // Pick the spear up and aim it at the target before picking the next attack.
            float spearAimInterpolant = Utils.GetLerpValue(27f, 54f, AttackTimer, true);
            SpearRotation = (Pi - PiOver4).AngleLerp(NPC.AngleTo(Target.Center) + PiOver4, spearAimInterpolant);

            // Look at the target.
            if (Distance(Target.Center.X, NPC.Center.X) > 25f && spearAimInterpolant >= 0.3f)
                NPC.spriteDirection = (Target.Center.X < NPC.Center.X).ToDirectionInt();

            // Decide frames.
            frameTimer++;
            CurrentFrame = Lerp(10f, 15f, frameTimer / 24f);

            // Once enough time has passed, transition to the next attack.
            if (AttackTimer >= attackTransitionDelay)
            {
                chargeCounter++;
                attackSubstate = 0f;
                AttackTimer = 0f;
                if (chargeCounter >= chargeCount)
                    SelectNextAttack();
                NPC.netUpdate = true;
            }
        }

        public void DoBehavior_LongHorizontalCharges()
        {
            int chargeCount = 4;
            int chargeTime = 30;
            int sandBlobCount = 5;
            int attackDelayAfterTeleport = 12;
            int chargeAnticipationTime = 20;
            float teleportHoverOffset = 440f;
            float teleportChargeSpeed = 50f;
            float sandBlobSpeed = 16f;
            bool canReleaseSandBlobs = true;

            if (Enraged)
            {
                chargeTime -= 6;
                sandBlobCount += 2;
                attackDelayAfterTeleport -= 4;
                chargeAnticipationTime -= 4;
                teleportChargeSpeed += 6.7f;
            }

            // Slow down if doing a combo attack.
            if (FightState == BereftVassalFightState.BereftVassalAndGSS)
            {
                chargeAnticipationTime += 9;
                chargeTime += 6;
                canReleaseSandBlobs = false;
            }

            ref float chargeCounter = ref NPC.Infernum().ExtraAI[0];
            int chargeDirection = (chargeCounter % 2f == 0f).ToDirectionInt();

            // Teleport to the side of the target in a flash. First, the charge happens from the left side. Then, it happens on the right.
            // Any successive charges alternate between the two.
            if (AttackTimer == 1f)
            {
                Vector2 teleportPosition = Target.Center - Vector2.UnitX * chargeDirection * teleportHoverOffset;
                TeleportToPosition(teleportPosition);

                if (chargeCounter >= chargeCount)
                {
                    SelectNextAttack();
                    return;
                }
            }

            // Fade in after the teleport.
            NPC.Opacity = Utils.GetLerpValue(0f, attackDelayAfterTeleport, AttackTimer, true);

            // Cast the telegraph line.
            float telegraphInterpolant = Utils.GetLerpValue(0f, chargeAnticipationTime, AttackTimer - attackDelayAfterTeleport, true);
            LineTelegraphDirection = chargeDirection == 1 ? 0f : Pi;
            LineTelegraphIntensity = telegraphInterpolant;
            SpearOpacity = 1f;
            SpearRotation = LineTelegraphDirection + PiOver4;

            // Disable gravity and tile collision.
            NPC.noGravity = true;
            NPC.noTileCollide = true;

            // Charge at the target.
            if (AttackTimer >= chargeAnticipationTime + attackDelayAfterTeleport)
            {
                // Create a slash sound and create sand blobs once the charge begins.
                if (AttackTimer == chargeAnticipationTime + attackDelayAfterTeleport)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.VassalSlashSound, NPC.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient && !NPC.WithinRange(Target.Center, 336f) && canReleaseSandBlobs)
                    {
                        for (int i = 0; i < sandBlobCount; i++)
                        {
                            float sandVelocityOffsetAngle = Lerp(0.04f, 0.79f, i / (float)(sandBlobCount - 1f)) * chargeDirection;

                            // Add a small amount of variance to the sane velocity, to make it require a bit of dynamic reaction.
                            sandVelocityOffsetAngle += Main.rand.NextFloatDirection() * 0.11f;

                            Vector2 sandVelocity = -Vector2.UnitY.RotatedBy(sandVelocityOffsetAngle) * sandBlobSpeed;
                            Vector2 sandSpawnPosition = NPC.Center + new Vector2(Main.rand.NextFloatDirection() * 6f, Main.rand.NextFloat(12f));

                            ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(blob =>
                            {
                                blob.ModProjectile<SandBlob>().StartingYPosition = Target.Bottom.Y;
                            });
                            Utilities.NewProjectileBetter(sandSpawnPosition, sandVelocity, ModContent.ProjectileType<SandBlob>(), SandBlobDamage, 0f);
                        }
                    }
                }

                CreateMotionStreakParticles();
                NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.UnitX * chargeDirection * teleportChargeSpeed, 0.2f);
                NPC.spriteDirection = -Math.Sign(NPC.velocity.X);

                // Handle end triggers once the charge is over.
                if (AttackTimer >= chargeAnticipationTime + attackDelayAfterTeleport + chargeTime)
                {
                    AttackTimer = 0f;
                    chargeCounter++;

                    NPC.netUpdate = true;
                }
            }

            // Look at the target if not charging.
            else
                NPC.spriteDirection = (Target.Center.X < NPC.Center.X).ToDirectionInt();
        }

        public void DoBehavior_SpearWaterTorrent()
        {
            int jumpDelay = 16;
            int waveCount = 7;
            int spearSpinTime = 75;
            int waterSpinTime = WaterTorrentBeam.Lifetime;
            float waterSpinArc = Pi * 0.28f;
            float recoilSpeed = 9f;
            float waveArc = ToRadians(70f);
            float waveSpeed = 4.6f;

            if (Enraged)
            {
                waveCount += 2;
                spearSpinTime -= 14;
                waterSpinArc *= 1.2f;
            }

            ref float aimDirection = ref NPC.Infernum().ExtraAI[0];

            // Disable tile collision and gravity.
            if (AttackTimer <= spearSpinTime + waterSpinTime)
            {
                NPC.noGravity = AttackTimer >= jumpDelay;
                NPC.noTileCollide = AttackTimer >= jumpDelay;
            }

            // Disable contact damage.
            NPC.damage = 0;

            // Wait until on ground for the attack to progress.
            if (AttackTimer <= 1f && NPC.velocity.Y != 0f)
                AttackTimer = 0f;

            // Jump away from the player shortly after reaching ground.
            if (AttackTimer == jumpDelay)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.VassalJumpSound, NPC.Bottom);
                SoundEngine.PlaySound(InfernumSoundRegistry.MyrindaelSpinSound, NPC.Center);

                NPC.spriteDirection = (Target.Center.X < NPC.Center.X).ToDirectionInt();
                NPC.velocity = new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 19f, -23f);
                NPC.netUpdate = true;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(NPC.Bottom, Vector2.UnitX * NPC.spriteDirection * 5f, ProjectileID.DD2OgreSmash, 0, 0f);
            }

            // Slow down to a halt and linger in the air.
            if (AttackTimer >= jumpDelay && AttackTimer <= spearSpinTime + waterSpinTime)
            {
                NPC.velocity.X *= 0.93f;
                NPC.velocity.Y *= 0.95f;
            }

            // Spin the spear.
            if (AttackTimer < spearSpinTime)
            {
                NPC.spriteDirection = (Target.Center.X < NPC.Center.X).ToDirectionInt();
                SpearOpacity = Utils.GetLerpValue(0f, 10f, AttackTimer, true);
                SpearRotation = WrapAngle(NPC.AngleTo(Target.Center) + Pi * AttackTimer / spearSpinTime * 6f) + PiOver4 - Pi * 0.12f;
            }

            // Prepare the line telegraph.
            LineTelegraphDirection = NPC.AngleTo(Target.Center) - Pi * 0.12f;
            LineTelegraphIntensity = Utils.GetLerpValue(0f, spearSpinTime, AttackTimer, true);

            // Release the water beam, some waves, and recoil backward somewhat.
            if (AttackTimer == spearSpinTime)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.VassalWaterBeamSound with { Volume = 1.5f }, NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // Apply recoil effects.
                    NPC.velocity -= (SpearRotation - PiOver4).ToRotationVector2() * recoilSpeed;
                    Utilities.NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<WaterTorrentBeam>(), WaterBeamDamage, 0f, -1, 0f, NPC.whoAmI);

                    // Release an even spread of waves.
                    for (int i = 0; i < waveCount; i++)
                    {
                        float waveShootOffsetAngle = Lerp(-waveArc, waveArc, i / (float)(waveCount - 1f));
                        Vector2 waveVelocity = NPC.SafeDirectionTo(Target.Center).RotatedBy(waveShootOffsetAngle) * waveSpeed;
                        Utilities.NewProjectileBetter(NPC.Center, waveVelocity, ModContent.ProjectileType<TorrentWave>(), WaterTorrentDamage, 0f);
                    }

                    aimDirection = (WrapAngle(NPC.AngleTo(Target.Center) - SpearRotation + PiOver4) > 0f).ToDirectionInt();

                    NPC.netUpdate = true;
                }
            }

            // Spin the water.
            if (AttackTimer >= spearSpinTime)
            {
                SpearRotation += waterSpinArc / spearSpinTime * aimDirection;
                SpearOpacity = Utils.GetLerpValue(40f, 0f, AttackTimer - spearSpinTime - waterSpinTime, true);
            }

            if (AttackTimer >= spearSpinTime + waterSpinTime + 50)
                SelectNextAttack();
        }

        public void DoBehavior_WaterWaveSlam()
        {
            int jumpCount = 3;
            int jumpDelay = 12;
            int hoverTime = 40;
            int attackTransitionDelay = 45;
            int chargeTime = 40;
            int smallWaveCount = 13;
            int waveBurstCount = 2;
            float jumpSpeed = NPC.Distance(Target.Center) * 0.017f + 35f;
            float waveSpeed = 15f;
            float waveShootSpeed = 4.5f;

            if (Enraged)
            {
                jumpCount++;
                jumpDelay -= 4;
                attackTransitionDelay -= 10;
                waveBurstCount++;
            }

            ref float jumpDirection = ref NPC.Infernum().ExtraAI[0];
            ref float jumpCounter = ref NPC.Infernum().ExtraAI[1];
            ref float startingTargetPositionY = ref NPC.Infernum().ExtraAI[2];
            ref float hasPassedTargetYPosition = ref NPC.Infernum().ExtraAI[3];
            ref float fallSpeed = ref NPC.Infernum().ExtraAI[4];

            // Disable tile collision and gravity.
            NPC.noGravity = AttackTimer >= jumpDelay;
            NPC.noTileCollide = AttackTimer >= jumpDelay;

            // Why.
            NPC.Opacity = 1f;

            // Wait until on ground for the attack to progress.
            if (AttackTimer <= 1f && NPC.velocity.Y != 0f)
            {
                if (!Collision.SolidCollision(NPC.TopLeft, NPC.width, NPC.height + 16))
                {
                    fallSpeed = Clamp(fallSpeed + 1.1f, 0f, 12f);
                    NPC.position.Y += fallSpeed;
                }
                AttackTimer = 0f;
            }

            // Disable contact damage while rising upward.
            if (AttackTimer < jumpDelay + hoverTime)
                NPC.damage = 0;

            // Jump away from the target shortly after reaching ground.
            if (AttackTimer == jumpDelay)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.VassalJumpSound, NPC.Bottom);

                NPC.spriteDirection = (Target.Center.X < NPC.Center.X).ToDirectionInt();
                NPC.velocity = new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 20f, -20f);
                NPC.netUpdate = true;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(NPC.Bottom, Vector2.UnitX * NPC.spriteDirection * 5f, ProjectileID.DD2OgreSmash, 0, 0f);
            }

            // Slow down and hover in place after jumping.
            // Also aim at the target.
            if (AttackTimer >= jumpDelay && AttackTimer < jumpDelay + hoverTime)
            {
                // Rapidly decelerate.
                NPC.velocity.X *= 0.9f;
                NPC.velocity.Y += 0.6f;

                // Enable tile collision if above the target.
                NPC.noTileCollide = NPC.Bottom.Y >= Target.Center.Y;

                // Decide where to jump to and completely cease horizontal movement once sufficiently slow.
                if (Math.Abs(NPC.velocity.X) < 1.2f)
                {
                    jumpDirection = NPC.AngleTo(Target.Center);
                    NPC.velocity.X = 0f;
                    NPC.netUpdate = true;
                }

                // Aim the spear at the target.
                float idealSpearRotation = NPC.AngleTo(Target.Center) + PiOver4;
                SpearRotation = SpearRotation.AngleTowards(idealSpearRotation, 0.2f).AngleLerp(idealSpearRotation, 0.06f);

                // Aim feet-first at the target once all horizontal movement has stopped.
                if (NPC.velocity.X == 0f)
                {
                    float idealRotation = jumpDirection + Pi;
                    if (NPC.spriteDirection == -1)
                        idealRotation += Pi;
                    NPC.rotation = NPC.rotation.AngleLerp(idealRotation, 0.05f).AngleTowards(idealRotation, 0.056f);
                    SpearRotation = jumpDirection + PiOver4;
                }
            }

            // Charge at the target.
            if (AttackTimer == jumpDelay + hoverTime)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.VassalSlashSound, NPC.Center);

                startingTargetPositionY = Target.Center.Y;
                NPC.velocity = jumpDirection.ToRotationVector2() * jumpSpeed;
                NPC.netUpdate = true;
            }

            // Handle post-charge collision effects.
            if (AttackTimer >= jumpDelay + hoverTime && AttackTimer < jumpDelay + hoverTime + chargeTime)
            {
                if (Distance(NPC.Center.Y, startingTargetPositionY + Math.Sign(NPC.velocity.Y) * Target.height * 0.5f) < 50f)
                {
                    hasPassedTargetYPosition = 1f;
                    NPC.netUpdate = true;
                }

                CreateMotionStreakParticles();

                // Check for collision. This does not apply if current above the target's bottom.
                bool hasHitGround = Collision.SolidCollision(NPC.TopLeft, NPC.width, NPC.height, true);
                bool ignoreTiles = hasPassedTargetYPosition == 0f || AttackTimer < jumpDelay + hoverTime + 4f;

                // Make the attack go by faster if far from the target.
                if (!NPC.WithinRange(Target.Center, 670f))
                    AttackTimer += 2f;

                if (hasHitGround && !ignoreTiles)
                {
                    // Perform ground hit effects once a collision is registered. This involves waves of water and creating a damaging ground area of effect.
                    SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode, NPC.Bottom);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        // Create the ground slam effect.
                        Utilities.NewProjectileBetter(NPC.Bottom, Vector2.UnitX * NPC.spriteDirection * 8f, ProjectileID.DD2OgreSmash, 0, 0f);

                        // Create the wave effect.
                        for (int i = -1; i <= 1; i += 2)
                        {
                            int wave = Utilities.NewProjectileBetter(NPC.Center, Vector2.UnitX * i * waveSpeed, ModContent.ProjectileType<GroundSlamWave>(), WaterTorrentDamage, 0f);

                            NPC.Bottom = Utilities.GetGroundPositionFrom(NPC.Bottom);
                            Main.projectile[wave].Bottom = NPC.Bottom;
                        }

                        // Create a burst of smaller waves.
                        float offsetAngle = Main.rand.NextFloat(TwoPi);
                        for (int i = 0; i < waveBurstCount; i++)
                        {
                            for (int j = 0; j < smallWaveCount; j++)
                            {
                                Vector2 sparkShootVelocity = (TwoPi * (j + (i % 2f == 1f ? 0.5f : 0f)) / smallWaveCount + offsetAngle).ToRotationVector2() * waveShootSpeed;
                                Utilities.NewProjectileBetter(NPC.Center + sparkShootVelocity * 4f, sparkShootVelocity, ModContent.ProjectileType<TorrentWave>(), WaveDamage, 0f);
                            }
                            waveShootSpeed *= 0.65f;
                        }
                    }

                    AttackTimer = jumpDelay + hoverTime + chargeTime;
                    SpearRotation = Pi - PiOver4;
                    NPC.rotation = 0f;
                    while (Collision.SolidCollision(NPC.TopLeft - Vector2.UnitY * 2f, NPC.width, NPC.height + 4, true))
                        NPC.position -= NPC.velocity.SafeNormalize(Vector2.UnitX * NPC.spriteDirection);

                    // Create a bunch of impact sparks.
                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 sparkSpawnPosition = NPC.Center + NPC.velocity.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloatDirection() * 180f;
                        SparkParticle spark = new(sparkSpawnPosition, -NPC.velocity.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(8f, 19f), false, 45, 1.45f, Color.Yellow);
                        GeneralParticleHandler.SpawnParticle(spark);
                    }

                    NPC.velocity *= new Vector2(0.1f, 0f);

                    NPC.netUpdate = true;
                }
            }

            // Have the spear dissipate after ground collision, as an indicator that it's disappearing to create the wave.
            if (AttackTimer >= jumpDelay + hoverTime + chargeTime)
            {
                NPC.velocity.X *= 0.9f;
                NPC.rotation = 0f;
                NPC.noGravity = false;
                NPC.noTileCollide = false;

                SpearOpacity = Utils.GetLerpValue(40f, 0f, AttackTimer - jumpDelay - hoverTime - chargeTime, true);

                if (AttackTimer >= jumpDelay + hoverTime + chargeTime + attackTransitionDelay)
                {
                    AttackTimer = 0f;
                    jumpCounter++;
                    hasPassedTargetYPosition = 0f;
                    if (jumpCounter >= jumpCount)
                        SelectNextAttack();
                }
            }
            else
                SpearOpacity = Utils.GetLerpValue(0f, 36f, AttackTimer, true);
        }

        public void DoBehavior_FallingWaterCastBarrges()
        {
            int shootDelay = 45;
            int shootRate = 11;
            int shootTime = 85;
            int waveCount = 5;
            int waveReleaseRate = 30;
            int attackTransitionDelay = 40;
            float waveArc = ToRadians(70f);
            float waveSpeed = 4.6f;

            if (Enraged)
            {
                shootRate -= 2;
                waveCount += 2;
                waveArc *= 1.3f;
                waveSpeed *= 1.2f;
            }

            ref float fallSpeed = ref NPC.Infernum().ExtraAI[0];

            // Wait until on ground for the attack to progress.
            if (AttackTimer <= 1f && NPC.velocity.Y != 0f)
            {
                if (!Collision.SolidCollision(NPC.TopLeft, NPC.width, NPC.height + 16))
                {
                    fallSpeed = Clamp(fallSpeed + 1.1f, 0f, 12f);
                    NPC.position.Y += fallSpeed;
                }
                AttackTimer = 0f;
            }

            // Bring the spear out and aim it upward.
            SpearOpacity = Utils.GetLerpValue(2f, shootDelay - 20f, AttackTimer, true);
            if (AttackTimer < shootDelay)
            {
                SpearRotation = NPC.AngleTo(Target.Center).AngleLerp(-PiOver4, 0.8f);

                // Create water particles at the end of the spear.
                Vector2 spearEnd = NPC.Center + (SpearRotation - PiOver4).ToRotationVector2() * 32f;
                if (AttackTimer % 12f == 11f)
                {
                    Color pulseColor = Main.rand.NextBool() ? Main.rand.NextBool() ? Color.SkyBlue : Color.LightSkyBlue : Main.rand.NextBool() ? Color.LightBlue : Color.DeepSkyBlue;
                    var pulse = new DirectionalPulseRing(spearEnd, Vector2.Zero, pulseColor, Vector2.One * 1.35f, SpearRotation - PiOver4, 0.05f, 0.42f, 30);
                    GeneralParticleHandler.SpawnParticle(pulse);

                    int numDust = 18;
                    for (int i = 0; i < numDust; i++)
                    {
                        Vector2 ringVelocity = (TwoPi * i / numDust).ToRotationVector2().RotatedBy(SpearRotation + PiOver4) * 5f;
                        Dust ringDust = Dust.NewDustPerfect(spearEnd, 211, ringVelocity, 100, default, 1.25f);
                        ringDust.noGravity = true;
                    }
                }

                for (int i = 0; i < 2; i++)
                {
                    Gore bubble = Gore.NewGorePerfect(NPC.GetSource_FromAI(), spearEnd, Main.rand.NextVector2Circular(0.8f, 0.8f), 411);
                    bubble.timeLeft = Main.rand.Next(8, 14);
                    bubble.scale = Main.rand.NextFloat(0.6f, 1f) * 1.2f;
                    bubble.type = Main.rand.NextBool(3) ? 412 : 411;
                }
            }

            // Look at the target.
            NPC.spriteDirection = (Target.Center.X < NPC.Center.X).ToDirectionInt();

            // Start shooting water spears.
            if (AttackTimer >= shootDelay && AttackTimer < shootDelay + shootTime)
            {
                // Prevent slow slide drifting.
                NPC.velocity.X *= 0.9f;

                if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer % waveReleaseRate == waveReleaseRate - 1f && !NPC.WithinRange(Target.Center, 300f))
                {
                    // Release an even spread of waves.
                    for (int i = 0; i < waveCount; i++)
                    {
                        float waveShootOffsetAngle = Lerp(-waveArc, waveArc, i / (float)(waveCount - 1f));
                        Vector2 waveVelocity = NPC.SafeDirectionTo(Target.Center).RotatedBy(waveShootOffsetAngle) * waveSpeed;
                        Utilities.NewProjectileBetter(NPC.Center, waveVelocity, ModContent.ProjectileType<TorrentWave>(), WaveDamage, 0f);
                    }
                }

                // Release spears into the air.
                if (AttackTimer % shootRate == shootRate - 1f)
                {
                    SoundEngine.PlaySound(SoundID.AbigailAttack, NPC.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float shootInterpolant = Utils.Remap(AttackTimer - shootDelay, 0f, shootTime - 8f, 0.04f, 0.8f);
                        Vector2 shootPosition = NPC.Center + (SpearRotation - PiOver4).ToRotationVector2() * 12f;
                        Vector2 shootDestination = Target.Center + Vector2.UnitX * (Target.Center.X > NPC.Center.X).ToDirectionInt() * 540f;
                        shootDestination.X = Lerp(shootDestination.X, NPC.Center.X, shootInterpolant);

                        float horizontalDistance = Vector2.Distance(shootPosition, shootDestination);
                        float idealShootSpeed = Sqrt(horizontalDistance * WaterSpear.Gravity);
                        float spearShootSpeed = Clamp(idealShootSpeed, 10f, 29f);
                        Vector2 spearShootVelocity = Utilities.GetProjectilePhysicsFiringVelocity(shootPosition, shootDestination, WaterSpear.Gravity, spearShootSpeed, out _);
                        spearShootVelocity.Y -= 4.5f;

                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(spear =>
                        {
                            spear.ModProjectile<WaterSpear>().StartingYPosition = Target.Bottom.Y;
                        });
                        Utilities.NewProjectileBetter(shootPosition, spearShootVelocity, ModContent.ProjectileType<WaterSpear>(), WaterSpearDamage, 0f);
                    }
                }
            }

            if (AttackTimer >= shootDelay + shootTime + attackTransitionDelay)
                SelectNextAttack();
        }

        public void DoBehavior_SandnadoPressureCharges()
        {
            int chargeDelay = 37;
            int chargeTime = 36;
            int chargeDisappearTime = 9;
            int chargeCycleTime = chargeDelay + chargeTime + chargeDisappearTime;
            int tornadoSummonDelay = 88;
            int tornadoConvergeTime = PressureSandnado.Lifetime;
            float tornadoMinHorizontalOffset = 100f;
            float tornadoMaxHorizontalOffset = 600f;
            float tornadoSpeed = (tornadoMaxHorizontalOffset - tornadoMinHorizontalOffset) / tornadoConvergeTime;
            float chargeSpeed = 44f;

            ref float chargeCounter = ref NPC.Infernum().ExtraAI[0];
            ref float hasHitGround = ref NPC.Infernum().ExtraAI[1];
            bool chargingVertically = chargeCounter % 2f == 0f;

            // Create a rumble effect and summon tornadoes.
            if (AttackTimer <= tornadoSummonDelay)
            {
                // Be completely invisible.
                SpearOpacity = 0f;
                NPC.Opacity = 0f;
                NPC.damage = 0;
                NPC.dontTakeDamage = true;

                Target.Calamity().GeneralScreenShakePower = AttackTimer / tornadoSummonDelay * 5f;

                // Play a wind sound and summon tornadoes.
                if (AttackTimer == tornadoSummonDelay)
                {
                    SoundEngine.PlaySound(SoundID.DD2_BetsyWindAttack, Target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 left = Utilities.GetGroundPositionFrom(Target.Center - Vector2.UnitX * tornadoMaxHorizontalOffset) + Vector2.UnitY * 40f;
                        Vector2 right = Utilities.GetGroundPositionFrom(Target.Center + Vector2.UnitX * tornadoMaxHorizontalOffset) + Vector2.UnitY * 40f;
                        Utilities.NewProjectileBetter(left, Vector2.UnitX * tornadoSpeed, ModContent.ProjectileType<PressureSandnado>(), PressureSandnadoDamage, 0f);
                        Utilities.NewProjectileBetter(right, Vector2.UnitX * -tornadoSpeed, ModContent.ProjectileType<PressureSandnado>(), PressureSandnadoDamage, 0f);
                    }
                }
                return;
            }

            float chargeTimer = (AttackTimer - tornadoSummonDelay) % chargeCycleTime;

            // Teleport as necessary before attacking.
            if (chargeTimer == 1f)
            {
                bool attackWillEnd = AttackTimer >= tornadoSummonDelay + PressureSandnado.Lifetime;
                Vector2 teleportDestination = Target.Center - Vector2.UnitY * 350f;
                if (!chargingVertically && !attackWillEnd)
                {
                    float horizontalOffset = Main.rand.NextBool().ToDirectionInt() * 450f;
                    teleportDestination = Target.Center + Vector2.UnitX * horizontalOffset;
                    teleportDestination.Y -= 20f;
                    if (Collision.SolidCollision(teleportDestination - NPC.Size * 0.5f, NPC.width, NPC.height) || Main.rand.NextBool())
                        teleportDestination.X -= horizontalOffset * 2f;
                }
                else if (chargingVertically)
                    teleportDestination.X += Main.rand.NextFloatDirection() * 50f;

                TeleportToPosition(teleportDestination);

                if (attackWillEnd)
                {
                    SelectNextAttack();
                    return;
                }

                LineTelegraphDirection = NPC.AngleTo(Target.Center);
                NPC.spriteDirection = (Target.Center.X < NPC.Center.X).ToDirectionInt();
                NPC.velocity = Vector2.Zero;
                chargeCounter++;
                hasHitGround = 0f;
            }

            if (chargeTimer < chargeDelay)
            {
                NPC.noGravity = true;
                NPC.velocity = Vector2.Zero;
            }

            // Handle fade effects.
            float fadeIn = Utils.GetLerpValue(0f, 12f, chargeTimer, true);
            float fadeOut = Utils.GetLerpValue(chargeCycleTime, chargeCycleTime - chargeDisappearTime, chargeTimer, true);
            NPC.Opacity = fadeIn * fadeOut;
            NPC.dontTakeDamage = NPC.Opacity < 0.75f;
            NPC.rotation = 0f;

            // Adjust the telegraph intensity.
            LineTelegraphIntensity = Utils.GetLerpValue(1f, chargeDelay, chargeTimer, true);
            SpearOpacity = LineTelegraphIntensity;
            SpearRotation = LineTelegraphDirection + PiOver4;

            // Disable contact damage before charging.
            if (chargeTimer < chargeDelay || chargeTimer >= chargeDelay + chargeTime)
            {
                NPC.damage = 0;
                NPC.velocity.X = 0f;
            }
            else if (NPC.velocity.Length() > 8f)
                CreateMotionStreakParticles();

            // Release spears upwards before firing.
            if (Main.netMode != NetmodeID.MultiplayerClient && chargeTimer == chargeDelay)
            {
                for (int i = 0; i < 7; i++)
                {
                    Vector2 spearShootVelocity = -Vector2.UnitY.RotatedBy(Lerp(-0.5f, 0.5f, i / 6f)) * 13f;

                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(spear =>
                    {
                        spear.ModProjectile<WaterSpear>().StartingYPosition = Target.Bottom.Y;
                    });
                    Utilities.NewProjectileBetter(NPC.Top, spearShootVelocity, ModContent.ProjectileType<WaterSpear>(), WaterSpearDamage, 0f);
                }
            }

            if (chargeTimer >= chargeDelay)
            {
                if (NPC.velocity.Y == 0f && hasHitGround == 0f && chargeTimer >= chargeDelay + 5f && !chargingVertically)
                {
                    SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode, NPC.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Utilities.NewProjectileBetter(NPC.Bottom, Vector2.UnitX * NPC.spriteDirection * 8f, ProjectileID.DD2OgreSmash, 0, 0f);

                        hasHitGround = 1f;
                        NPC.velocity = Vector2.Zero;
                        NPC.netUpdate = true;
                    }
                }
            }

            // Adjust velocity after the telegraph.
            if (chargeTimer >= chargeDelay && chargeTimer < chargeDelay + chargeTime && hasHitGround == 0f)
            {
                NPC.velocity = Vector2.Lerp(NPC.velocity, LineTelegraphDirection.ToRotationVector2() * chargeSpeed, 0.18f);
                NPC.noGravity = true;
            }

            // Leap into the air after the charge is over.
            if (NPC.velocity.Y > -15f && chargeTimer == chargeDelay + chargeTime)
            {
                NPC.velocity.Y = -15f;
                NPC.netUpdate = true;
            }

            if (chargeTimer >= chargeDelay + chargeTime)
                NPC.noGravity = true;
        }

        public void DoBehavior_HypersonicWaterSlashes()
        {
            int fadeOutTime = 30;
            int chargeTelegraphTime = 21;
            int chargeTime = 30;
            int chargeFadeoutTime = 9;
            int chargeCount = 7;
            float hoverOffset = 400f;
            float fadeOutRiseSpeed = 0.38f;
            float hoverSpeed = 28f;
            float chargeSpeed = 34f;
            float movementAngularVelocity = TwoPi / 76f;
            ref float chargeCounter = ref NPC.Infernum().ExtraAI[0];
            ref float hasPerformedRiseAnimation = ref NPC.Infernum().ExtraAI[1];
            ref float hoverOffsetDirection = ref NPC.Infernum().ExtraAI[2];
            ref float tearProjectileIndex = ref NPC.Infernum().ExtraAI[3];

            // Disable tile collision and gravity.
            NPC.noGravity = true;
            NPC.noTileCollide = true;

            if (AttackTimer <= fadeOutTime && hasPerformedRiseAnimation == 0f)
            {
                // Disable damage when fading out.
                NPC.dontTakeDamage = true;
                NPC.damage = 0;

                // Initialize the tear index.
                tearProjectileIndex = -1f;

                // Fade out.
                NPC.Opacity = Utils.GetLerpValue(25f, 0f, AttackTimer, true);

                // Rise upwards and fade out.
                if (NPC.velocity.Y > 0f)
                    NPC.velocity.Y = 0f;

                // Determine jump frames.
                FrameType = BereftVassalFrameType.Jump;
                CurrentFrame = (int)(AttackTimer / fadeOutTime * 8.5f);

                NPC.velocity.Y -= fadeOutRiseSpeed;

                // Teleport above the target.
                if (AttackTimer >= fadeOutTime)
                {
                    hasPerformedRiseAnimation = 1f;
                    hoverOffsetDirection = Target.AngleTo(Target.Center);
                    TeleportToPosition(Target.Center - Vector2.UnitY * hoverOffset);
                    AttackTimer = 0f;
                    NPC.netUpdate = true;
                }
                return;
            }

            if (chargeCounter == 0f)
                chargeTelegraphTime += 36;

            // Hover near the target before charging.
            if (AttackTimer <= chargeTelegraphTime)
            {
                // Make the spear fade in.
                NPC.spriteDirection = (Target.Center.X < NPC.Center.X).ToDirectionInt();
                SpearOpacity = AttackTimer / chargeTelegraphTime;
                SpearRotation = NPC.AngleTo(Target.Center) + PiOver4;

                // Look at the target.
                NPC.rotation = -NPC.AngleTo(Target.Center) * 0.18f;

                // Disable damage.
                NPC.damage = 0;

                // Be opaque.
                NPC.Opacity = 1f;

                // Hover to the sides of the target.
                Vector2 hoverDestination = Target.Center - hoverOffsetDirection.ToRotationVector2() * hoverOffset;
                Vector2 idealVelocity = NPC.SafeDirectionTo(hoverDestination) * hoverSpeed;
                NPC.SimpleFlyMovement(idealVelocity, hoverSpeed * 0.04f);
                NPC.velocity = NPC.velocity.MoveTowards(idealVelocity, 1.1f);

                // Charge at the target.
                if (AttackTimer == chargeTelegraphTime)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.VassalSlashSound, NPC.Center);
                    NPC.velocity = NPC.SafeDirectionTo(Target.Center) * chargeSpeed;
                    NPC.netUpdate = true;

                    // Create the water tear.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        tearProjectileIndex = Utilities.NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<WaterSlice>(), WaterSliceDamage, 0f);
                }

                return;
            }

            // Arc after charging for long enough. This always attempts to arc away from the player.
            if (AttackTimer >= chargeTelegraphTime + chargeTime / 2)
            {
                Vector2 left = NPC.velocity.RotatedBy(-movementAngularVelocity);
                Vector2 right = NPC.velocity.RotatedBy(movementAngularVelocity);
                Vector2 directionToTarget = NPC.SafeDirectionTo(Target.Center);
                if (left.AngleBetween(directionToTarget) > right.AngleBetween(directionToTarget))
                    NPC.velocity = left;
                else
                    NPC.velocity = right;
                NPC.rotation = -NPC.velocity.ToRotation() * 0.18f;
            }

            // Fade away and make the tear detach.
            if (AttackTimer >= chargeTelegraphTime + chargeTime)
            {
                tearProjectileIndex = -1f;
                NPC.Opacity = Clamp(NPC.Opacity - 1f / chargeFadeoutTime, 0f, 1f);
                NPC.damage = 0;
                NPC.dontTakeDamage = true;
            }
            else
                CreateMotionStreakParticles();

            // Prepare for the next teleport.
            if (AttackTimer >= chargeTelegraphTime + chargeTime + chargeFadeoutTime)
            {
                NPC.Opacity = 1f;
                NPC.rotation = 0f;

                hoverOffsetDirection = Target.velocity.SafeNormalize(Main.rand.NextVector2Unit()).ToRotation() + Pi;
                Vector2 teleportDestination = Target.Center - hoverOffsetDirection.ToRotationVector2() * hoverOffset;

                chargeCounter++;
                if (chargeCounter >= chargeCount)
                {
                    // Neutralize all water slices and make them fade away.
                    foreach (Projectile slice in Utilities.AllProjectilesByID(ModContent.ProjectileType<WaterSlice>()))
                    {
                        slice.damage = 0;
                        slice.timeLeft = slice.MaxUpdates * 23;
                        slice.netUpdate = true;
                    }

                    teleportDestination = Target.Center - Vector2.UnitY * 350f;
                    SelectNextAttack();
                }

                TeleportToPosition(teleportDestination, false);
                AttackTimer = 0f;
                NPC.netUpdate = true;
            }
        }

        public void DoBehavior_SummonGreatSandShark()
        {
            int jumpHoverTime = 35;
            int animationFocusTime = 15;
            int hornSoundTime = 212;
            int gssSummonDelay = 233;
            int animationFocusReturnTime = 12;
            int attackTransitionDelay = 75;
            ref float fallSpeed = ref NPC.Infernum().ExtraAI[0];

            // Make the spear disappear.
            SpearOpacity = Clamp(SpearOpacity - 0.1f, 0f, 1f);
            SpearRotation = 0f;

            // Get rid of old telegraphs.
            LineTelegraphIntensity = 0f;

            // Disable damage.
            NPC.damage = 0;
            NPC.dontTakeDamage = true;

            // Disable tile collision and gravity when blowing the horn.
            bool blowingHorn = AttackTimer >= jumpHoverTime && AttackTimer <= jumpHoverTime + hornSoundTime;
            NPC.noTileCollide = blowingHorn;
            NPC.noGravity = blowingHorn;

            // Wait until on ground for the attack to progress.
            if (AttackTimer <= 1f && NPC.velocity.Y != 0f)
            {
                if (!Collision.SolidCollision(NPC.TopLeft, NPC.width, NPC.height + 16))
                {
                    fallSpeed = Clamp(fallSpeed + 0.7f, 0f, 13.6f);
                    NPC.position.Y += fallSpeed;
                }
                AttackTimer = 0f;
            }

            // Jump upward and look at the target once on the ground.
            if (AttackTimer == 2f)
            {
                // Disable controls and UI for the target.
                if (Main.myPlayer == NPC.target)
                {
                    BlockerSystem.Start(true, true, () =>
                    {
                        int index = NPC.FindFirstNPC(Type);
                        if (Main.npc.IndexInRange(index))
                        {
                            NPC vassal = Main.npc[index];
                            if (vassal.ai[1] >= jumpHoverTime + hornSoundTime + gssSummonDelay - 54f)
                                return false;
                        }
                        return true;
                    });
                }

                // Delete all old projectiles.
                Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<GroundSlamWave>(), ModContent.ProjectileType<SandBlob>(), ModContent.ProjectileType<TorrentWave>(), ModContent.ProjectileType<WaterSpear>(), ModContent.ProjectileType<WaterTorrentBeam>());

                NPC.spriteDirection = (Target.Center.X < NPC.Center.X).ToDirectionInt();
                NPC.velocity = new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 20f, -20f);
                NPC.netUpdate = true;
            }

            // Handle post-jump behaviors.
            if (AttackTimer >= 2f && AttackTimer <= jumpHoverTime)
            {
                // Rapidly decelerate and spin.
                NPC.velocity.X *= 0.9f;
                NPC.velocity.Y += 0.7f;
                NPC.rotation = TwoPi * AttackTimer / jumpHoverTime;
            }

            // Calculate frames.
            if (AttackTimer >= jumpHoverTime - 30f)
            {
                CurrentFrame = (int)(Utils.GetLerpValue(jumpHoverTime - 30f, jumpHoverTime, AttackTimer, true) * Utils.GetLerpValue(-54f, -84f, AttackTimer - jumpHoverTime - hornSoundTime - gssSummonDelay, true) * 7f);
                if (CurrentFrame >= 1f)
                    FrameType = BereftVassalFrameType.BlowHorn;
            }

            // Blow the horn.
            if (AttackTimer == jumpHoverTime)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.VassalHornSound with { Volume = 2f });
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(NPC.Center + Vector2.UnitX * NPC.spriteDirection * 20f, Vector2.Zero, ModContent.ProjectileType<BereftVassalBigBoom>(), 0, 0f);
            }

            // Slow down after blowing the horn.
            if (AttackTimer >= jumpHoverTime)
                NPC.velocity *= 0.87f;

            // Play the great sand shark summon sound.
            if (AttackTimer == jumpHoverTime + hornSoundTime)
                SoundEngine.PlaySound(InfernumSoundRegistry.GreatSandSharkSpawnSound with { Volume = 1.2f });

            // Summon the great sand shark.
            if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer == jumpHoverTime + hornSoundTime + gssSummonDelay)
            {
                int shark = NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y + 300, ModContent.NPCType<GreatSandSharkNPC>(), NPC.whoAmI);
                if (Main.npc.IndexInRange(shark))
                    Main.npc[shark].velocity = -Vector2.UnitY * 23f;
            }

            // Give the player their controls back before the great sand shark is spawned.
            if (Main.myPlayer == NPC.target && AttackTimer >= jumpHoverTime + hornSoundTime + gssSummonDelay - 54f)
            {
                Main.hideUI = false;
                Main.blockInput = false;
            }

            // Have the camera zoom in on the vassal once the animation begins.
            float screenShakeInterpolant = Utils.GetLerpValue(0f, 60f, AttackTimer - jumpHoverTime - hornSoundTime, true) * Utils.GetLerpValue(-2f, -22f, AttackTimer - jumpHoverTime - hornSoundTime - gssSummonDelay, true);
            Target.Infernum_Camera().ScreenFocusInterpolant = Utils.GetLerpValue(2f, animationFocusTime, AttackTimer, true);
            Target.Infernum_Camera().ScreenFocusInterpolant *= Utils.GetLerpValue(-54f, -54f - animationFocusReturnTime, AttackTimer - jumpHoverTime - hornSoundTime - gssSummonDelay, true);
            Target.Infernum_Camera().CurrentScreenShakePower = screenShakeInterpolant * 6f;
            Target.Infernum_Camera().ScreenFocusPosition = NPC.Center;

            // Create sand particles from the left.
            for (int i = 0; i < 6; i++)
            {
                if (Main.rand.NextFloat() < screenShakeInterpolant)
                    CreateSandstormParticle(false);
            }

            if (AttackTimer == jumpHoverTime + hornSoundTime + gssSummonDelay + attackTransitionDelay)
                SelectNextAttack();
        }

        public void DoBehavior_TransitionToFinalPhase()
        {
            int mournTime = 75;
            int mournTransitionTime = 30;
            int angerTime = 164;

            // Be fully opaque.
            NPC.Opacity = Utils.GetLerpValue(-8f, -20f, AttackTimer - mournTime - mournTransitionTime - angerTime, true);
            NPC.rotation = 0f;
            SpearOpacity = 0f;
            SpearRotation = 0f;

            // Reset the DoT timer.
            NPC.lifeRegen = 0;

            // Teleport to the surface on the first frame, and look away from the target.
            if (AttackTimer == 1f)
            {
                if (!NPC.WithinRange(Target.Center, 900f))
                    NPC.Center = Target.Center + Vector2.UnitX * Target.direction * 120f;

                NPC.position.Y = 300f;
                NPC.spriteDirection = (Target.Center.X > NPC.Center.X).ToDirectionInt();
                TeleportToPosition(Utilities.GetGroundPositionFrom(NPC.Center) - Vector2.UnitY * (NPC.height * 0.5f + 8f));
            }

            // Play an rage sound when mourning has finished.
            if (AttackTimer == mournTime + mournTransitionTime)
                SoundEngine.PlaySound(InfernumSoundRegistry.VassalAngerSound, NPC.Center);

            // Jitter in rage after mourning.
            AngerInterpolant = Utils.GetLerpValue(mournTime, mournTime + mournTransitionTime + 32f, AttackTimer, true);
            if (AttackTimer >= mournTime)
                NPC.position.X += Main.rand.NextFloatDirection() * AngerInterpolant * 1.2f;

            // Use kneeling frames, as though the vassal is mourning the death of the sand shark.
            if (AttackTimer <= mournTime + mournTransitionTime)
            {
                // Disable damage when kneeling.
                NPC.dontTakeDamage = true;
                NPC.damage = 0;

                FrameType = BereftVassalFrameType.Kneel;
                CurrentFrame = (int)Utils.Remap(AttackTimer, mournTime, mournTime + mournTransitionTime, 0f, 4f);
            }

            if (AttackTimer >= mournTime + mournTransitionTime + angerTime)
            {
                NPC.Infernum().ExtraAI[6] = 0f;
                SelectNextAttack();
            }
        }

        public void DoBehavior_RetreatAnimation()
        {
            int groundSitTime = 190;
            int riseUpTime = 32;
            int waitTime = 45;
            ref float timeSinceHitGround = ref NPC.Infernum().ExtraAI[0];

            // Disable damage.
            NPC.dontTakeDamage = true;
            NPC.damage = 0;

            // Close the boss bar.
            NPC.Calamity().ShouldCloseHPBar = true;

            // Put away the sharp objects.
            SpearOpacity = 0f;
            SpearRotation = 0f;

            // No music.
            Music = 0;

            // Reset opacity and rotation.
            if (timeSinceHitGround <= 0f)
            {
                NPC.Opacity = 1f;
                NPC.rotation = 0f;
            }

            // Teleport above ground on the first frame.
            if (AttackTimer == 1f)
            {
                NPC.velocity = Vector2.UnitY * 5f;

                Vector2 baseTeleportPosition = new(Target.Center.X + Target.direction * Main.rand.NextFloat(50f, 150f), 300f);
                if (Distance(Target.Center.X, NPC.Center.X) < 500f)
                    baseTeleportPosition.X = NPC.Center.X;
                if (NPC.collideY)
                    baseTeleportPosition.Y = NPC.Center.Y;

                Vector2 teleportPosition = Utilities.GetGroundPositionFrom(baseTeleportPosition) - Vector2.UnitY * (NPC.collideY ? 32f : 250f);
                TeleportToPosition(teleportPosition, false);

                NPC.spriteDirection = (Target.Center.X < NPC.Center.X).ToDirectionInt();
            }

            // Create ground hit effects.
            if (AttackTimer >= 2f && NPC.collideY && timeSinceHitGround == 0f)
            {
                SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode, NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Utilities.NewProjectileBetter(NPC.Bottom, Vector2.UnitX * NPC.spriteDirection * 8f, ProjectileID.DD2OgreSmash, 0, 0f);
                    timeSinceHitGround = 1f;
                    NPC.netUpdate = true;
                }
            }

            // Use kneeling frames after hitting frames.
            if (timeSinceHitGround >= 1f)
            {
                if (timeSinceHitGround < groundSitTime + riseUpTime + waitTime)
                {
                    FrameType = BereftVassalFrameType.Kneel;
                    CurrentFrame = Utils.GetLerpValue(groundSitTime, groundSitTime + riseUpTime, timeSinceHitGround, true) * 4.5f;
                }

                // Fly away and drop loot.
                else
                {
                    NPC.noGravity = true;
                    NPC.noTileCollide = true;

                    // Jump into the air and accidentally drop some loot.
                    if (timeSinceHitGround == groundSitTime + riseUpTime + waitTime + 1f)
                    {
                        SoundEngine.PlaySound(InfernumSoundRegistry.VassalJumpSound, NPC.Center);

                        NPC.boss = false;
                        NPC.NPCLoot();
                        NPC.spriteDirection = (Target.Center.X > NPC.Center.X).ToDirectionInt();
                        NPC.velocity = new(NPC.spriteDirection * -15f, -17f);
                        NPC.rotation = NPC.velocity.ToRotation() * NPC.spriteDirection * 0.2f;

                        NPC.netUpdate = true;
                    }

                    // Fade away.
                    NPC.Opacity -= 0.08f;
                    if (NPC.Opacity <= 0f)
                    {
                        LostColosseum.HasBereftVassalBeenDefeated = true;
                        Main.BestiaryTracker.Kills.RegisterKill(NPC);
                        AchievementPlayer.ExtraUpdateHandler(Main.LocalPlayer, AchievementUpdateCheck.NPCKill, NPC.whoAmI);
                        NPC.active = false;
                    }
                }

                timeSinceHitGround++;
            }
        }

        public void SelectNextAttack()
        {
            var attackCycle = NPC.life < NPC.lifeMax * Phase2LifeRatio ? Phase2AttackCycle : Phase1AttackCycle;
            CurrentAttack = attackCycle[(int)NPC.Infernum().ExtraAI[6] % attackCycle.Length];

            for (int i = 0; i < 5; i++)
                NPC.Infernum().ExtraAI[i] = 0f;

            NPC.Infernum().ExtraAI[6]++;
            NPC.Opacity = 1f;
            NPC.rotation = 0f;
            AttackTimer = 0f;
            NPC.netUpdate = true;
        }

        public void CreateMotionStreakParticles()
        {
            // Release anime-like streak particle effects at the side of the vassal to indicate motion.
            if (Main.rand.NextBool(2))
            {
                Vector2 energySpawnPosition = NPC.Center + Main.rand.NextVector2Circular(30f, 20f) + NPC.velocity * 2f;
                Vector2 energyVelocity = -NPC.velocity.SafeNormalize(Vector2.UnitX * NPC.spriteDirection) * Main.rand.NextFloat(6f, 8.75f);
                Particle energyLeak = new SquishyLightParticle(energySpawnPosition, energyVelocity, Main.rand.NextFloat(0.55f, 0.9f), Color.Yellow, 30, 3.4f, 4.5f);
                GeneralParticleHandler.SpawnParticle(energyLeak);
            }
        }

        public void CreateSandstormParticle(bool fromRight)
        {
            Vector2 position = new(Main.rand.NextFloat(-150f, 350f), Main.rand.NextFloat(-50f, 0f));
            if (Main.rand.NextBool(3))
                position.X = Main.rand.Next(500) - 500;
            if (fromRight)
                position.X = Main.screenWidth - position.X;

            // Mostly from vanilla. Spawns sandstorm particles from the side.
            position.Y = Main.rand.NextFloat(0.1f, 0.9f) * Main.screenHeight;
            position += Main.screenPosition + Target.velocity;
            int tileCoordX = (int)position.X / 16;
            int tileCoordY = (int)position.Y / 16;
            if (WorldGen.InWorld(tileCoordX, tileCoordY) && Main.tile[tileCoordX, tileCoordY].WallType == WallID.None)
            {
                Dust dust = Dust.NewDustDirect(position, 10, 10, DustID.Sandstorm, 0f, 0f, 0, default, 1f);
                dust.velocity.Y = Main.rand.NextFloat(0.7f, 0.77f) * dust.scale;
                dust.velocity.X = Main.rand.NextFloat(1f, 40f) * (fromRight ? -1f : 1f);
                if (Enraged)
                    dust.velocity.X = Clamp(Math.Abs(dust.velocity.X) * 4f, 4f, 40f) * Math.Sign(dust.velocity.X);

                dust.velocity *= 1.08f;
                dust.color = Color.Orange;
                dust.fadeIn = 2.6f;
                dust.scale = Main.rand.NextFloat(1.5f, 2.18f);
            }
        }

        public void TeleportToPosition(Vector2 position, bool createBoomAtStart = true)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && !NPC.WithinRange(position, 900f) && createBoomAtStart)
                Utilities.NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<BereftVassalTeleportBoom>(), 0, 0f);

            SoundEngine.PlaySound(InfernumSoundRegistry.VassalTeleportSound, NPC.Center);

            ElectricShieldOpacity = 0f;
            NPC.Center = position;
            NPC.velocity = Vector2.Zero;
            NPC.Opacity = 0f;
            NPC.netUpdate = true;

            if (Main.netMode != NetmodeID.MultiplayerClient)
                Utilities.NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<BereftVassalTeleportBoom>(), 0, 0f);
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.soundDelay <= 0)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.VassalHitSound with { Volume = 1.5f }, NPC.Center);
                NPC.soundDelay = 9;
            }
        }

        public override bool CheckDead()
        {
            if (CurrentAttack != BereftVassalAttackType.RetreatAnimation)
            {
                // Delete all old projectiles.
                Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<GroundSlamWave>(), ModContent.ProjectileType<PressureSandnado>(), ModContent.ProjectileType<SandBlob>(),
                    ModContent.ProjectileType<TorrentWave>(), ModContent.ProjectileType<WaterSlice>(), ModContent.ProjectileType<WaterSpear>(), ModContent.ProjectileType<WaterTorrentBeam>());

                SelectNextAttack();
                CurrentAttack = BereftVassalAttackType.RetreatAnimation;
                NPC.life = 1;
                NPC.dontTakeDamage = true;
                NPC.active = true;
                NPC.netUpdate = true;
            }
            return false;
        }

        public override void BossLoot(ref string name, ref int potionType) => potionType = ItemID.GreaterHealingPotion;

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            // Expert+ bag.
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<BereftVassalBossBag>()));

            LeadingConditionRule normalOnly = npcLoot.DefineNormalOnlyDropSet();

            // Weapons.
            int[] weapons = new int[]
            {
                ModContent.ItemType<AridBattlecry>(),
                ModContent.ItemType<Myrindael>(),
                ModContent.ItemType<TheGlassmaker>(),
                ModContent.ItemType<WanderersShell>()
            };

            normalOnly.Add(ModContent.ItemType<CherishedSealocket>());
            normalOnly.Add(ModContent.ItemType<WaterglassToken>());
            normalOnly.Add(DropHelper.CalamityStyle(DropHelper.NormalWeaponDropRateFraction, weapons));

            // Trophy (always directly from boss, never in bag).
            npcLoot.Add(ModContent.ItemType<BereftVassalTrophy>(), 10);

            // Lore.
            npcLoot.AddConditionalPerPlayer(() => !WorldSaveSystem.DownedBereftVassal, ModContent.ItemType<KnowledgeBereftVassal>(), desc: DropHelper.FirstKillText);
        }

        public override void OnKill()
        {
            // Mark the bereft vassal as defeated.
            WorldSaveSystem.DownedBereftVassal = true;
            CalamityNetcode.SyncWorld();
        }

        public float PrimitiveWidthFunction(float completionRatio) => Lerp(0.2f, 12f, LineTelegraphIntensity) * Utils.GetLerpValue(1f, 0.72f, LineTelegraphIntensity, true);

        public Color PrimitiveTrailColor(float completionRatio)
        {
            float opacity = Utils.GetLerpValue(0f, 0.4f, LineTelegraphIntensity, true) * Utils.GetLerpValue(1f, 0.8f, LineTelegraphIntensity, true);
            Color startingColor = CurrentAttack == BereftVassalAttackType.SpearWaterTorrent ? Color.MediumBlue : Color.Orange;
            Color c = Color.Lerp(startingColor, Color.White, LineTelegraphIntensity) * opacity * (1f - completionRatio) * 0.32f;
            return c * Utils.GetLerpValue(0.01f, 0.06f, completionRatio, true);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Initialize the telegraph drawer.
            NPC.Infernum().OptionalPrimitiveDrawer ??= new(PrimitiveWidthFunction, PrimitiveTrailColor, null, true, GameShaders.Misc["CalamityMod:SideStreakTrail"]);

            int frameCount = 4;
            float verticalOffset = 2f;
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;

            // Define things for the bestiary.
            if (NPC.IsABestiaryIconDummy)
            {
                NPC.spriteDirection = 1;
                CurrentFrame = (int)(Main.GlobalTimeWrappedHourly * 9.1f) % frameCount;
            }

            switch (FrameType)
            {
                case BereftVassalFrameType.BlowHorn:
                    frameCount = 7;
                    texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/GreatSandShark/BereftVassalHorn").Value;
                    break;
                case BereftVassalFrameType.Jump:
                    frameCount = 16;
                    verticalOffset = -4f;
                    texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/GreatSandShark/BereftVassalJump").Value;
                    break;
                case BereftVassalFrameType.Kneel:
                    frameCount = 5;
                    verticalOffset = -4f;
                    texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/GreatSandShark/BereftVassalKneel").Value;
                    break;
            }

            NPC.frame = texture.Frame(1, frameCount, 0, Utils.Clamp((int)CurrentFrame, 0, frameCount - 1));

            Vector2 drawPosition = NPC.Center - screenPos + Vector2.UnitY * verticalOffset;
            Vector2 spearDrawPosition = drawPosition + Vector2.UnitY * 8f;

            // Draw the downward telegraph trail as needed.
            if (LineTelegraphIntensity > 0f)
            {
                Vector2[] telegraphPoints = new Vector2[3]
                {
                    spearDrawPosition,
                    spearDrawPosition + LineTelegraphDirection.ToRotationVector2() * 2000f,
                    spearDrawPosition + LineTelegraphDirection.ToRotationVector2() * 4000f
                };
                GameShaders.Misc["CalamityMod:SideStreakTrail"].UseImage1("Images/Misc/Perlin");
                NPC.Infernum().OptionalPrimitiveDrawer.Draw(telegraphPoints, Vector2.Zero, 51);
            }

            Texture2D spearTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/GreatSandShark/BereftVassalSpear").Value;
            SpriteEffects direction = NPC.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            // Draw anger smoke.
            SmokeDrawer.DrawSet(NPC.Center);

            Main.EntitySpriteDraw(texture, drawPosition, NPC.frame, NPC.GetAlpha(drawColor), NPC.rotation, NPC.frame.Size() * 0.5f, NPC.scale, direction, 0);

            // Draw a red overlay if angry.
            if (AngerInterpolant > 0f)
            {
                Color angerColor = Color.Lerp(drawColor, new(1f, 0.2f, 0.04f, 1f), AngerInterpolant) * NPC.Opacity * AngerInterpolant * 0.7f;
                Main.EntitySpriteDraw(texture, drawPosition, NPC.frame, angerColor, NPC.rotation, NPC.frame.Size() * 0.5f, NPC.scale, direction, 0);
            }

            // Draw the spear.
            BereftVassalSpear.DrawSpearInstance(spearDrawPosition, Color.White * NPC.Opacity, SpearOpacity, SpearRotation, NPC.scale * 0.8f, false);

            // Draw the electric shield if it's present.
            if (ElectricShieldOpacity > 0f && !NPC.IsABestiaryIconDummy)
                DrawElectricShield(ElectricShieldOpacity, NPC.Center + Vector2.UnitY * NPC.gfxOffY - Main.screenPosition, Lighting.Brightness((int)(NPC.Center.X / 16f), (int)(NPC.Center.Y / 16f)));
            return false;
        }

        public static void DrawElectricShield(float opacity, Vector2 drawPosition, float colorBrightness, float scaleFactor = 1f)
        {
            float scale = Lerp(0.15f, 0.18f, Sin(Main.GlobalTimeWrappedHourly * 0.5f) * 0.5f + 0.5f) * scaleFactor;
            float noiseScale = Lerp(0.4f, 0.8f, Sin(Main.GlobalTimeWrappedHourly * 0.3f) * 0.5f + 0.5f);

            Effect shieldEffect = Terraria.Graphics.Effects.Filters.Scene["CalamityMod:RoverDriveShield"].GetShader().Shader;
            shieldEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.24f);
            shieldEffect.Parameters["blowUpPower"].SetValue(2.5f);
            shieldEffect.Parameters["blowUpSize"].SetValue(0.5f);
            shieldEffect.Parameters["noiseScale"].SetValue(noiseScale);

            // Prepare the forcefield opacity.
            float baseShieldOpacity = 0.9f + 0.1f * Sin(Main.GlobalTimeWrappedHourly * 2f);
            shieldEffect.Parameters["shieldOpacity"].SetValue(baseShieldOpacity * (opacity * 0.9f + 0.1f));
            shieldEffect.Parameters["shieldEdgeBlendStrenght"].SetValue(4f);

            Color edgeColor = new(226, 179, 97);
            Color shieldColor = Color.Cyan;

            // Prepare the forcefield colors.
            shieldEffect.Parameters["shieldColor"].SetValue(shieldColor.ToVector3());
            shieldEffect.Parameters["shieldEdgeColor"].SetValue(edgeColor.ToVector3());

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, shieldEffect, Main.GameViewMatrix.TransformationMatrix);

            // Draw the forcefield. This doesn't happen if the lighting behind the vassal is too low, to ensure that it doesn't draw if underground or in a darkly lit area.
            Texture2D noise = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/EternityStreak").Value;
            if (shieldColor.ToVector4().Length() > 0.02f)
                Main.spriteBatch.Draw(noise, drawPosition, null, Color.White * opacity * colorBrightness, 0, noise.Size() / 2f, scale * 2f, 0, 0);

            Main.spriteBatch.ExitShaderRegion();
        }

        public override bool CheckActive() => false;
    }
}
