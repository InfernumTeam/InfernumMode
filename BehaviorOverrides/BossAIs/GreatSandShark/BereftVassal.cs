using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Sounds;
using InfernumMode.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using GreatSandSharkNPC = CalamityMod.NPCs.GreatSandShark.GreatSandShark;

namespace InfernumMode.BehaviorOverrides.BossAIs.GreatSandShark
{
    public class BereftVassal : ModNPC
    {
        public enum BereftVassalAttackType
        {
            IdleState,
            SandBlobSlam,
            LongHorizontalCharges,
            SpearWaterTorrent,
            WaterWaveSlam,
            FallingWaterCastBarrges,
            SummonGreatSandShark
        }

        public Player Target => Main.player[NPC.target];

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

        public ref float AttackTimer => ref NPC.ai[1];

        public ref float SpearRotation => ref NPC.localAI[0];

        public ref float SpearOpacity => ref NPC.localAI[1];

        public ref float LineTelegraphDirection => ref NPC.localAI[2];

        public ref float LineTelegraphIntensity => ref NPC.localAI[3];

        public const float Phase2LifeRatio = 0.6f;

        public override void SetStaticDefaults()
        {
            this.HideFromBestiary();
            DisplayName.SetDefault("Bereft Vassal");
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.TrailingMode[NPC.type] = 3;
            NPCID.Sets.TrailCacheLength[NPC.type] = 10;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 5f;
            NPC.damage = 100;
            NPC.width = 30;
            NPC.height = 44;
            NPC.defense = 20;
            NPC.DR_NERD(0.2f);
            NPC.LifeMaxNERB(42700, 42700, 800000);

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
            NPC.netAlways = true;
            Music = MusicID.Boss4;

            NPC.Calamity().ShouldCloseHPBar = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(NPC.Opacity);
            writer.Write(NPC.rotation);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            NPC.Opacity = reader.ReadSingle();
            NPC.rotation = reader.ReadSingle();
        }

        public override void AI()
        {
            // Pick a target if no valid one exists.
            NPC.TargetClosestIfTargetIsInvalid();

            // Reset things every frame.
            NPC.damage = NPC.defDamage;
            NPC.dontTakeDamage = false;
            NPC.noTileCollide = false;
            NPC.noGravity = false;
            NPC.Calamity().ShouldCloseHPBar = CurrentAttack == BereftVassalAttackType.IdleState;

            // Go away if the target is dead.
            if ((!Target.active || Target.dead) && CurrentAttack != BereftVassalAttackType.IdleState)
                NPC.active = false;

            // Stay inside of the world.
            if (NPC.position.X < 150f)
                NPC.position.X = 150f;
            if (NPC.position.X > Main.maxTilesX * 16f - 150f)
                NPC.position.X = Main.maxTilesX * 16f - 150f;

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
                case BereftVassalAttackType.SummonGreatSandShark:
                    DoBehavior_SummonGreatSandShark();
                    break;
            }

            if (!HasBegunSummoningGSS && NPC.life < NPC.lifeMax * Phase2LifeRatio)
            {
                SelectNextAttack();
                CurrentAttack = BereftVassalAttackType.SummonGreatSandShark;
                HasBegunSummoningGSS = true;
            }
            
            AttackTimer++;
        }

        public void DoBehavior_IdleState()
        {
            int animationFocusTime = 32;
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
                SpearRotation = NPC.AngleTo(Target.Center) + MathHelper.PiOver4;
                NPC.netUpdate = true;
            }

            if (hasBegunAnimation == 0f)
                return;

            // Have the camera zoom in on the vassal once the animation begins.
            Target.Infernum().ScreenFocusInterpolant = Utils.GetLerpValue(2f, animationFocusTime, AttackTimer, true);
            Target.Infernum().ScreenFocusInterpolant *= Utils.GetLerpValue(0f, -animationFocusReturnTime, AttackTimer - animationFocusTime - animationTime, true);
            Target.Infernum().ScreenFocusPosition = NPC.Center;

            // Spin the spear.
            int animationTimer = (int)(AttackTimer - animationFocusTime);
            if (animationTimer < 0)
                return;

            if (animationTimer <= spearSpinTime)
            {
                if (animationTimer == 6)
                    SoundEngine.PlaySound(CommonCalamitySounds.MeatySlashSound, NPC.Center);

                SpearOpacity = Utils.GetLerpValue(0f, 16f, animationTimer, true);
                SpearRotation += MathHelper.Pi / spearSpinTime * 10f;
            }

            // Look towards the target before leaping at them.
            else if (animationTimer <= spearSpinTime + spearStrikeTime)
            {
                // Look at the target and aim the spear at them.
                SpearRotation = SpearRotation.AngleLerp(NPC.AngleTo(Target.Center) + MathHelper.PiOver4, 0.2f);
                NPC.spriteDirection = (Target.Center.X < NPC.Center.X).ToDirectionInt();

                // Jump into the air.
                if (animationTimer == spearSpinTime + spearStrikeTime - 10)
                {
                    SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode, NPC.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Utilities.NewProjectileBetter(NPC.Bottom, Vector2.UnitX * NPC.spriteDirection * 8f, ProjectileID.DD2OgreSmash, 160, 0f);
                        NPC.velocity = new Vector2((Target.Center.X > NPC.Center.X).ToDirectionInt() * 19f, -23f);
                        NPC.netUpdate = true;
                    }
                }

                // Change music.
                SceneEffectPriority = SceneEffectPriority.BossHigh;
                Music = MusicID.Boss3;
                if (ModLoader.TryGetMod("InfernumModeMusic", out Mod musicMod))
                    Music = MusicLoader.GetMusicSlot(musicMod, "Sounds/Music/Boss3");
            }

            // Disable controls and UI for the target.
            if (Main.myPlayer == NPC.target)
            {
                Main.hideUI = true;
                Main.blockInput = true;
            }

            if (AttackTimer >= animationTime + animationFocusReturnTime)
            {
                if (Main.myPlayer == NPC.target)
                {
                    Main.hideUI = false;
                    Main.blockInput = false;
                }

                SelectNextAttack();
            }
        }

        public void DoBehavior_SandBlobSlam()
        {
            int repositionInterpolationTime = 32;
            int sandBlobCount = 13;
            int slamDelay = 36;
            int attackTransitionDelay = 96;
            float slamSpeed = 28f;
            float sandBlobAngularArea = 1.27f;
            float sandBlobSpeed = 18f;
            ref float attackSubstate = ref NPC.Infernum().ExtraAI[0];
            ref float startingTargetPositionY = ref NPC.Infernum().ExtraAI[1];

            // Disable gravity and tile collision universally during this attack.
            // All of these things are applied manually.
            NPC.noTileCollide = true;
            NPC.noGravity = true;

            // Hover into position, above the target.
            if (attackSubstate == 0f)
            {
                // If the attack goes on for longer than expected the vassal will interpolant towards the destination faster and faster until it's eventually reached.
                float flySpeedInterpolant = Utils.GetLerpValue(0f, repositionInterpolationTime, AttackTimer, true);
                float positionIncrement = MathHelper.Lerp(0.32f, 6.4f, flySpeedInterpolant) + (AttackTimer - repositionInterpolationTime) * 0.18f;
                float flySpeed = MathHelper.Lerp(2f, 22f, flySpeedInterpolant);
                Vector2 hoverDestination = Target.Center - Vector2.UnitY * 275f;

                // Apply manual movement calculations before determining the ideal velocity, to ensure that there is not a one-frame buffer between what the velocity thinks the current position is
                // versus what it actually is.
                NPC.Center = NPC.Center.MoveTowards(hoverDestination, positionIncrement);

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
                if (MathHelper.Distance(Target.Center.X, NPC.Center.X) > 25f)
                    NPC.spriteDirection = (Target.Center.X < NPC.Center.X).ToDirectionInt();

                // Disable contact damag when rising upward.
                NPC.damage = 0;
                return;
            }
            
            // Hover in place for a short period of time.
            if (attackSubstate == 1f)
            {
                // Spin the spear such that it points downward.
                SpearRotation = SpearRotation.AngleLerp(MathHelper.Pi - MathHelper.PiOver4, 0.12f);
                SpearOpacity = MathHelper.Lerp(SpearOpacity, 1f, 0.1f);

                // Disable contact damag when hovering in place.
                NPC.damage = 0;

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

                CreateMotionStreakParticles();

                // Check for collision. This does not apply if current above the target's bottom.
                bool hasHitGround = Collision.SolidCollision(NPC.BottomRight - Vector2.UnitY * 4f, NPC.width, 6, true);
                bool ignoreTiles = NPC.Bottom.Y < startingTargetPositionY;
                bool pretendFakeCollisionHappened = NPC.Bottom.Y >= Target.Bottom.Y + 600f;
                if ((hasHitGround && !ignoreTiles) || pretendFakeCollisionHappened)
                {
                    // Perform ground hit effects once a collision is registered. This involves releasing sand rubble into the air and creating a damaging ground area of effect.
                    SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode, NPC.Bottom);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < sandBlobCount; i++)
                        {
                            float sandVelocityOffsetAngle = MathHelper.Lerp(-sandBlobAngularArea, sandBlobAngularArea, i / (float)(sandBlobCount - 1f));

                            // Add a small amount of variance to the sane velocity, to make it require a bit of dynamic reaction.
                            sandVelocityOffsetAngle += Main.rand.NextFloatDirection() * 0.11f;

                            Vector2 sandVelocity = -Vector2.UnitY.RotatedBy(sandVelocityOffsetAngle) * sandBlobSpeed;
                            Vector2 sandSpawnPosition = NPC.Center + new Vector2(Main.rand.NextFloatDirection() * 6f, Main.rand.NextFloat(12f));
                            int blobIndex = Utilities.NewProjectileBetter(sandSpawnPosition, sandVelocity, ModContent.ProjectileType<SandBlob>(), 160, 0f);
                            if (Main.projectile.IndexInRange(blobIndex))
                                Main.projectile[blobIndex].ModProjectile<SandBlob>().StartingYPosition = Target.Bottom.Y;
                        }

                        Utilities.NewProjectileBetter(NPC.Bottom, Vector2.UnitX * NPC.spriteDirection * 8f, ProjectileID.DD2OgreSmash, 160, 0f);
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
            SpearRotation = (MathHelper.Pi - MathHelper.PiOver4).AngleLerp(NPC.AngleTo(Target.Center) + MathHelper.PiOver4, spearAimInterpolant);

            // Look at the target.
            if (MathHelper.Distance(Target.Center.X, NPC.Center.X) > 25f && spearAimInterpolant >= 0.3f)
                NPC.spriteDirection = (Target.Center.X < NPC.Center.X).ToDirectionInt();

            // TODO -- Maybe kneel for a bit before standing back up. This will require talking with Peng.

            // Once enough time has passed, transition to the next attack.
            if (AttackTimer >= attackTransitionDelay)
                SelectNextAttack();
        }

        public void DoBehavior_LongHorizontalCharges()
        {
            int chargeCount = 4;
            int chargeTime = 30;
            int sandBlobCount = 4;
            int attackDelayAfterTeleport = 12;
            int chargeAnticipationTime = 20;
            float teleportHoverOffset = 440f;
            float teleportChargeSpeed = 50f;
            float sandBlobSpeed = 14f;
            ref float chargeCounter = ref NPC.Infernum().ExtraAI[0];
            int chargeDirection = (chargeCounter % 2f == 0f).ToDirectionInt();

            // Teleport to the side of the target in a flash. First, the charge happens from the left side. Then, it happens on the right.
            // Any successive charges alternate between the two.
            if (AttackTimer == 1f)
            {
                Vector2 teleportPosition = Target.Center - Vector2.UnitX * chargeDirection * teleportHoverOffset;
                if (Main.netMode != NetmodeID.MultiplayerClient && !NPC.WithinRange(teleportPosition, 900f))
                    Utilities.NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<BereftVassalTeleportBoom>(), 0, 0f);
                
                NPC.Center = teleportPosition;
                NPC.velocity = Vector2.Zero;
                NPC.Opacity = 0f;
                NPC.netUpdate = true;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<BereftVassalTeleportBoom>(), 0, 0f);

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
            LineTelegraphDirection = chargeDirection == 1 ? 0f : MathHelper.Pi;
            LineTelegraphIntensity = telegraphInterpolant;
            SpearOpacity = 1f;
            SpearRotation = LineTelegraphDirection + MathHelper.PiOver4;

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
                    if (Main.netMode != NetmodeID.MultiplayerClient && !NPC.WithinRange(Target.Center, 336f))
                    {
                        for (int i = 0; i < sandBlobCount; i++)
                        {
                            float sandVelocityOffsetAngle = MathHelper.Lerp(0.04f, 0.79f, i / (float)(sandBlobCount - 1f)) * chargeDirection;

                            // Add a small amount of variance to the sane velocity, to make it require a bit of dynamic reaction.
                            sandVelocityOffsetAngle += Main.rand.NextFloatDirection() * 0.11f;

                            Vector2 sandVelocity = -Vector2.UnitY.RotatedBy(sandVelocityOffsetAngle) * sandBlobSpeed;
                            Vector2 sandSpawnPosition = NPC.Center + new Vector2(Main.rand.NextFloatDirection() * 6f, Main.rand.NextFloat(12f));
                            int blobIndex = Utilities.NewProjectileBetter(sandSpawnPosition, sandVelocity, ModContent.ProjectileType<SandBlob>(), 160, 0f);
                            if (Main.projectile.IndexInRange(blobIndex))
                                Main.projectile[blobIndex].ModProjectile<SandBlob>().StartingYPosition = Target.Bottom.Y;
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
            int spearSpinTime = 68;
            int waterSpinTime = WaterTorrentBeam.Lifetime;
            float waterSpinArc = MathHelper.Pi * 0.33f;
            float recoilSpeed = 9f;
            float waveArc = MathHelper.ToRadians(70f);
            float waveSpeed = 4.6f;
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
                SoundEngine.PlaySound(CommonCalamitySounds.MeatySlashSound, NPC.Center);

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
                SpearRotation = MathHelper.WrapAngle(NPC.AngleTo(Target.Center) + MathHelper.Pi * AttackTimer / spearSpinTime * 6f) + MathHelper.PiOver4 - MathHelper.Pi * 0.12f;
            }

            // Prepare the line telegraph.
            LineTelegraphDirection = NPC.AngleTo(Target.Center) - MathHelper.Pi * 0.12f;
            LineTelegraphIntensity = Utils.GetLerpValue(0f, spearSpinTime, AttackTimer, true);

            // Release the water beam, some waves, and recoil backward somewhat.
            if (AttackTimer == spearSpinTime)
            {
                SoundEngine.PlaySound(SoundID.DD2_KoboldIgnite with { Volume = 1.5f, Pitch = -0.3f }, NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // Apply recoil effects.
                    NPC.velocity -= (SpearRotation - MathHelper.PiOver4).ToRotationVector2() * recoilSpeed;

                    int waterBeam = Utilities.NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<WaterTorrentBeam>(), 225, 0f);
                    if (Main.projectile.IndexInRange(waterBeam))
                        Main.projectile[waterBeam].ai[1] = NPC.whoAmI;

                    // Release an even spread of waves.
                    for (int i = 0; i < waveCount; i++)
                    {
                        float waveShootOffsetAngle = MathHelper.Lerp(-waveArc, waveArc, i / (float)(waveCount - 1f));
                        Vector2 waveVelocity = NPC.SafeDirectionTo(Target.Center).RotatedBy(waveShootOffsetAngle) * waveSpeed;
                        Utilities.NewProjectileBetter(NPC.Center, waveVelocity, ModContent.ProjectileType<TorrentWave>(), 160, 0f);
                    }
                    
                    aimDirection = (MathHelper.WrapAngle(NPC.AngleTo(Target.Center) - SpearRotation + MathHelper.PiOver4) > 0f).ToDirectionInt();

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
            int jumpDelay = 20;
            int hoverTime = 36;
            int attackTransitionDelay = 45;
            int chargeTime = 54;
            int smallWaveCount = 13;
            float jumpSpeed = 35f;
            float waveSpeed = 15f;
            ref float jumpDirection = ref NPC.Infernum().ExtraAI[0];
            ref float jumpCounter = ref NPC.Infernum().ExtraAI[1];
            ref float startingTargetPositionY = ref NPC.Infernum().ExtraAI[2];
            ref float hasPassedTargetYPosition = ref NPC.Infernum().ExtraAI[3];
            ref float fallSpeed = ref NPC.Infernum().ExtraAI[4];

            // Disable tile collision and gravity.
            NPC.noGravity = AttackTimer >= jumpDelay;
            NPC.noTileCollide = AttackTimer >= jumpDelay;

            // Wait until on ground for the attack to progress.
            if (AttackTimer <= 1f && NPC.velocity.Y != 0f)
            {
                if (!Collision.SolidCollision(NPC.TopLeft, NPC.width, NPC.height + 16))
                {
                    fallSpeed = MathHelper.Clamp(fallSpeed + 1.1f, 0f, 12f);
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
                NPC.velocity.Y += 0.7f;

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
                float idealSpearRotation = NPC.AngleTo(Target.Center) + MathHelper.PiOver4;
                SpearRotation = SpearRotation.AngleTowards(idealSpearRotation, 0.2f).AngleLerp(idealSpearRotation, 0.06f);

                // Aim feet-first at the target once all horizontal movement has stopped.
                if (NPC.velocity.X == 0f)
                {
                    float idealRotation = jumpDirection + MathHelper.Pi;
                    if (NPC.spriteDirection == -1)
                        idealRotation += MathHelper.Pi;
                    NPC.rotation = NPC.rotation.AngleLerp(idealRotation, 0.05f).AngleTowards(idealRotation, 0.056f);
                    SpearRotation = jumpDirection + MathHelper.PiOver4;
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
                if (MathHelper.Distance(NPC.Center.Y, startingTargetPositionY + Math.Sign(NPC.velocity.Y) * Target.height * 0.5f) < 50f)
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
                        Utilities.NewProjectileBetter(NPC.Bottom, Vector2.UnitX * NPC.spriteDirection * 8f, ProjectileID.DD2OgreSmash, 160, 0f);

                        // Create the wave effect.
                        for (int i = -1; i <= 1; i += 2)
                        {
                            int wave = Utilities.NewProjectileBetter(NPC.Center, Vector2.UnitX * i * waveSpeed, ModContent.ProjectileType<GroundSlamWave>(), 200, 0f);
                            Main.projectile[wave].Bottom = NPC.Bottom;
                        }

                        // Create a burst of smaller waves.
                        float offsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                        for (int i = 0; i < smallWaveCount; i++)
                        {
                            Vector2 sparkShootVelocity = (MathHelper.TwoPi * i / smallWaveCount + offsetAngle).ToRotationVector2() * 4.5f;
                            Utilities.NewProjectileBetter(NPC.Center + sparkShootVelocity * 4f, sparkShootVelocity, ModContent.ProjectileType<TorrentWave>(), 160, 0f);

                            sparkShootVelocity = (MathHelper.TwoPi * (i + 0.5f) / smallWaveCount + offsetAngle).ToRotationVector2() * 3f;
                            Utilities.NewProjectileBetter(NPC.Center + sparkShootVelocity * 4f, sparkShootVelocity, ModContent.ProjectileType<TorrentWave>(), 160, 0f);
                        }
                    }

                    AttackTimer = jumpDelay + hoverTime + chargeTime;
                    SpearRotation = MathHelper.Pi - MathHelper.PiOver4;
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
            float waveArc = MathHelper.ToRadians(70f);
            float waveSpeed = 4.6f;
            ref float fallSpeed = ref NPC.Infernum().ExtraAI[0];

            // Wait until on ground for the attack to progress.
            if (AttackTimer <= 1f && NPC.velocity.Y != 0f)
            {
                if (!Collision.SolidCollision(NPC.TopLeft, NPC.width, NPC.height + 16))
                {
                    fallSpeed = MathHelper.Clamp(fallSpeed + 1.1f, 0f, 12f);
                    NPC.position.Y += fallSpeed;
                }
                AttackTimer = 0f;
            }

            // Bring the spear out and aim it upward.
            SpearOpacity = Utils.GetLerpValue(2f, shootDelay - 20f, AttackTimer, true);
            if (AttackTimer < shootDelay)
            {
                SpearRotation = NPC.AngleTo(Target.Center).AngleLerp(-MathHelper.PiOver4, 0.8f);

                // Create water particles at the end of the spear.
                Vector2 spearEnd = NPC.Center + (SpearRotation - MathHelper.PiOver4).ToRotationVector2() * 32f;
                if (AttackTimer % 12f == 11f)
                {
                    Color pulseColor = Main.rand.NextBool() ? (Main.rand.NextBool() ? Color.SkyBlue : Color.LightSkyBlue) : (Main.rand.NextBool() ? Color.LightBlue : Color.DeepSkyBlue);
                    var pulse = new DirectionalPulseRing(spearEnd, Vector2.Zero, pulseColor, Vector2.One * 1.35f, SpearRotation - MathHelper.PiOver4, 0.05f, 0.42f, 30);
                    GeneralParticleHandler.SpawnParticle(pulse);

                    int numDust = 18;
                    for (int i = 0; i < numDust; i++)
                    {
                        Vector2 ringVelocity = (MathHelper.TwoPi * i / numDust).ToRotationVector2().RotatedBy(SpearRotation + MathHelper.PiOver4) * 5f;
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

                if (AttackTimer % waveReleaseRate == waveReleaseRate - 1f && !NPC.WithinRange(Target.Center, 300f))
                {
                    // Release an even spread of waves.
                    for (int i = 0; i < waveCount; i++)
                    {
                        float waveShootOffsetAngle = MathHelper.Lerp(-waveArc, waveArc, i / (float)(waveCount - 1f));
                        Vector2 waveVelocity = NPC.SafeDirectionTo(Target.Center).RotatedBy(waveShootOffsetAngle) * waveSpeed;
                        Utilities.NewProjectileBetter(NPC.Center, waveVelocity, ModContent.ProjectileType<TorrentWave>(), 160, 0f);
                    }
                }

                // Release spears into the air.
                if (AttackTimer % shootRate == shootRate - 1f)
                {
                    SoundEngine.PlaySound(SoundID.AbigailAttack, NPC.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float shootInterpolant = Utils.Remap(AttackTimer - shootDelay, 0f, shootTime - 8f, 0.04f, 0.8f);
                        Vector2 shootPosition = NPC.Center + (SpearRotation - MathHelper.PiOver4).ToRotationVector2() * 12f;
                        Vector2 shootDestination = Target.Center + Vector2.UnitX * (Target.Center.X > NPC.Center.X).ToDirectionInt() * 250f;
                        shootDestination.X = MathHelper.Lerp(shootDestination.X, NPC.Center.X, shootInterpolant);

                        float horizontalDistance = Vector2.Distance(shootPosition, shootDestination);
                        float idealShootSpeed = (float)Math.Sqrt(horizontalDistance * WaterSpear.Gravity);
                        float spearShootSpeed = MathHelper.Clamp(idealShootSpeed, 10f, 29f);
                        Vector2 spearShootVelocity = Utilities.GetProjectilePhysicsFiringVelocity(shootPosition, shootDestination, WaterSpear.Gravity, spearShootSpeed, out _);
                        spearShootVelocity.Y -= 4.5f;

                        int spearIndex = Utilities.NewProjectileBetter(shootPosition, spearShootVelocity, ModContent.ProjectileType<WaterSpear>(), 160, 0f);
                        if (Main.projectile.IndexInRange(spearIndex))
                            Main.projectile[spearIndex].ModProjectile<WaterSpear>().StartingYPosition = Target.Bottom.Y;
                    }
                }
            }

            if (AttackTimer >= shootDelay + shootTime + attackTransitionDelay)
                SelectNextAttack();
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
            SpearOpacity = MathHelper.Clamp(SpearOpacity - 0.1f, 0f, 1f);
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
                    fallSpeed = MathHelper.Clamp(fallSpeed + 0.7f, 0f, 13.6f);
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
                    Main.hideUI = true;
                    Main.blockInput = true;
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
                NPC.rotation = MathHelper.TwoPi * AttackTimer / jumpHoverTime;
            }

            // Blow the horn.
            if (AttackTimer == jumpHoverTime)
                SoundEngine.PlaySound(InfernumSoundRegistry.VassalHornSound with { Volume = 1.6f });

            // Slow down after blowing the horn.
            if (AttackTimer >= jumpHoverTime)
                NPC.velocity *= 0.87f;

            // Play the great sand shark summon sound.
            if (AttackTimer == jumpHoverTime + hornSoundTime)
                SoundEngine.PlaySound(InfernumSoundRegistry.GreatSandSharkSpawnSound with { Volume = 1.2f });

            // Summon the great sand shark.
            if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer == jumpHoverTime + hornSoundTime + gssSummonDelay)
            {
                int shark = NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y + 500, ModContent.NPCType<GreatSandSharkNPC>(), NPC.whoAmI);
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
            Target.Infernum().ScreenFocusInterpolant = Utils.GetLerpValue(2f, animationFocusTime, AttackTimer, true);
            Target.Infernum().ScreenFocusInterpolant *= Utils.GetLerpValue(-54f, -54f - animationFocusReturnTime, AttackTimer - jumpHoverTime - hornSoundTime - gssSummonDelay, true);
            Target.Infernum().CurrentScreenShakePower = screenShakeInterpolant * 6f;
            Target.Infernum().ScreenFocusPosition = NPC.Center;

            // Create sand particles from below.
            for (int i = 0; i < 6; i++)
            {
                if (Main.rand.NextFloat() < screenShakeInterpolant)
                {
                    Vector2 position = new(Main.rand.NextFloat(-150f, 350f), Main.rand.NextFloat(-50f, 0f));
                    if (Main.rand.NextBool(3))
                        position.X = Main.rand.Next(500) - 500;

                    position.Y = Main.rand.NextFloat(0.1f, 0.9f) * Main.screenHeight;
                    position += Main.screenPosition + Target.velocity;
                    int tileCoordX = (int)position.X / 16;
                    int tileCoordY = (int)position.Y / 16;
                    if (WorldGen.InWorld(tileCoordX, tileCoordY) && Main.tile[tileCoordX, tileCoordY].WallType == WallID.None)
                    {
                        Dust dust = Dust.NewDustDirect(position, 10, 10, 268, 0f, 0f, 0, default, 1f);
                        dust.velocity.Y = Main.rand.NextFloat(0.7f, 0.77f) * dust.scale;
                        dust.velocity.X = Main.rand.NextFloat(1f, 40f);
                        dust.velocity *= 1.08f;
                        dust.color = Color.Orange;
                        dust.fadeIn = 2.6f;
                        dust.scale = Main.rand.NextFloat(1.5f, 2.18f);
                    }
                }
            }

            if (AttackTimer == jumpHoverTime + hornSoundTime + gssSummonDelay + attackTransitionDelay)
                SelectNextAttack();
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

        public void SelectNextAttack()
        {
            switch (CurrentAttack)
            {
                case BereftVassalAttackType.SandBlobSlam:
                    CurrentAttack = BereftVassalAttackType.LongHorizontalCharges;
                    break;
                case BereftVassalAttackType.LongHorizontalCharges:
                    CurrentAttack = BereftVassalAttackType.SpearWaterTorrent;
                    break;
                case BereftVassalAttackType.SpearWaterTorrent:
                    CurrentAttack = BereftVassalAttackType.WaterWaveSlam;
                    break;
                case BereftVassalAttackType.WaterWaveSlam:
                    CurrentAttack = BereftVassalAttackType.FallingWaterCastBarrges;
                    break;
                case BereftVassalAttackType.FallingWaterCastBarrges:
                case BereftVassalAttackType.IdleState:
                case BereftVassalAttackType.SummonGreatSandShark:
                    CurrentAttack = BereftVassalAttackType.SandBlobSlam;
                    break;
            }
            for (int i = 0; i < 5; i++)
                NPC.Infernum().ExtraAI[i] = 0f;

            NPC.Opacity = 1f;
            AttackTimer = 0f;
            NPC.netUpdate = true;
        }

        public override void FindFrame(int frameHeight)
        {
            int frame = 0;
            if (NPC.velocity.Y == 0f)
                frame = 0;

            NPC.frame.Y = frameHeight * frame;
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            if (NPC.soundDelay <= 0)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.VassalHitSound with { Volume = 1.5f }, NPC.Center);
                NPC.soundDelay = 9;
            }
        }

        public float PrimitiveWidthFunction(float completionRatio) => MathHelper.Lerp(0.2f, 12f, LineTelegraphIntensity) * Utils.GetLerpValue(1f, 0.72f, LineTelegraphIntensity, true);

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

            // Draw the downward telegraph trail as needed.
            Vector2 drawPosition = NPC.Center - screenPos;
            Vector2 spearDrawPosition = drawPosition + Vector2.UnitY * 8f;
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

            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D spearTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/GreatSandShark/BereftVassalSpear").Value;
            SpriteEffects direction = NPC.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Main.EntitySpriteDraw(texture, drawPosition, NPC.frame, NPC.GetAlpha(drawColor), NPC.rotation, NPC.frame.Size() * 0.5f, NPC.scale, direction, 0);

            if (SpearOpacity < 0.95f)
            {
                Color spearAfterimageColor = new Color(0.23f, 0.93f, 0.96f, 0f) * SpearOpacity * NPC.Opacity;
                for (int i = 0; i < 12; i++)
                {
                    Vector2 spearOffset = (MathHelper.TwoPi * i / 12f + Main.GlobalTimeWrappedHourly * 2.2f).ToRotationVector2() * (1f - SpearOpacity) * 12f;
                    Main.EntitySpriteDraw(spearTexture, spearDrawPosition + spearOffset, null, spearAfterimageColor, SpearRotation, spearTexture.Size() * 0.5f, NPC.scale * 0.8f, 0, 0);
                }
            }
            Main.EntitySpriteDraw(spearTexture, spearDrawPosition, null, NPC.GetAlpha(drawColor) * SpearOpacity, SpearRotation, spearTexture.Size() * 0.5f, NPC.scale * 0.8f, 0, 0);
            return false;
        }

        public override bool CheckActive() => false;
    }
}
