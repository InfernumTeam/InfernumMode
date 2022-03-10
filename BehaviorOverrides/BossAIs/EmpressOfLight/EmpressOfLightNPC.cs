using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.EmpressOfLight
{
    [AutoloadBossHead]
    public class EmpressOfLightNPC : ModNPC
    {
        #region Fields, Properties, and Enumerations
        public enum EmpressOfLightAttackType
        {
            SpawnAnimation,
            PrismaticBoltCircle,
            MesmerizingMagic,
            HorizontalCharge,
            EnterSecondPhase,
            LanceOctagon,
            RainbowWispForm
        }

        public EmpressOfLightAttackType AttackType
        {
            get => (EmpressOfLightAttackType)npc.ai[0];
            set => npc.ai[0] = (int)value;
        }

        public bool InPhase2 => CurrentPhase >= 1f && (AttackType != EmpressOfLightAttackType.EnterSecondPhase || AttackTimer >= SecondPhaseFadeoutTime);

        public bool ReadyToUseScreenShader => InPhase2;

        public bool Enraged => ShouldBeEnraged;

        public Player Target => Main.player[npc.target];

        public ref float AttackTimer => ref npc.ai[1];

        public ref float CurrentPhase => ref npc.ai[2];

        public ref float WingFrameCounter => ref npc.localAI[0];

        public ref float LeftArmFrame => ref npc.localAI[1];

        public ref float RightArmFrame => ref npc.localAI[2];

        public ref float ScreenShaderStrength => ref npc.localAI[3];

        public static bool ShouldBeEnraged => Main.dayTime;

        public const int SecondPhaseFadeoutTime = 90;

        public const int SecondPhaseFadeBackInTime = 90;

        public const float Phase2LifeRatio = 0.7f;

        #endregion Fields, Properties, and Enumerations

        #region Set Defaults

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Empress Of Light");
            Main.npcFrameCount[npc.type] = 2;
            NPCID.Sets.TrailingMode[npc.type] = 3;
            NPCID.Sets.TrailCacheLength[npc.type] = 12;
        }

        public override void SetDefaults()
        {
            npc.noGravity = true;
            npc.width = 100;
            npc.height = 100;
            npc.damage = 80;
            npc.defense = 50;
            npc.lifeMax = 70000;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.knockBackResist = 0f;
            npc.value = 250000f;
            npc.noTileCollide = true;
            npc.boss = true;
            npc.Opacity = 0f;
            npc.dontTakeDamage = true;
            npc.boss = true;
            music = MusicID.Boss3;
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.lifeMax = (int)(npc.lifeMax * 0.8f * bossLifeScale);
            npc.damage = (int)(npc.damage * 0.8f);
        }

        #endregion Set Defaults

        #region AI and Behaviors

        public override void AI()
        {
            npc.TargetClosestIfTargetIsInvalid();

            npc.damage = 0;
            npc.spriteDirection = 1;
            LeftArmFrame = 0f;
            RightArmFrame = 0f;
            npc.dontTakeDamage = false;

            // Enter new phases.
            float lifeRatio = npc.life / (float)npc.lifeMax;
            if (CurrentPhase == 0f && lifeRatio < Phase2LifeRatio)
            {
                SelectNextAttack();
                AttackType = EmpressOfLightAttackType.EnterSecondPhase;
                CurrentPhase = 1f;
            }

            switch (AttackType)
            {
                case EmpressOfLightAttackType.SpawnAnimation:
                    DoBehavior_SpawnAnimation();
                    break;
                case EmpressOfLightAttackType.PrismaticBoltCircle:
                    DoBehavior_PrismaticBoltCircle();
                    break;
                case EmpressOfLightAttackType.MesmerizingMagic:
                    DoBehavior_MesmerizingMagic();
                    break;
                case EmpressOfLightAttackType.HorizontalCharge:
                    DoBehavior_HorizontalCharge();
                    break;
                case EmpressOfLightAttackType.EnterSecondPhase:
                    DoBehavior_EnterSecondPhase();
                    break;
                case EmpressOfLightAttackType.LanceOctagon:
                    DoBehavior_LanceOctagon();
                    break;
                case EmpressOfLightAttackType.RainbowWispForm:
                    DoBehavior_RainbowWispForm();
                    break;
            }

            // Manage the screen shader.
            if (Main.netMode != NetmodeID.Server)
            {
                ScreenShaderStrength = 1f;
                if (AttackType == EmpressOfLightAttackType.EnterSecondPhase)
                    ScreenShaderStrength = Utils.InverseLerp(SecondPhaseFadeoutTime, SecondPhaseFadeoutTime + SecondPhaseFadeBackInTime, AttackTimer, true);

                Filters.Scene["InfernumMode:EmpressOfLight"].GetShader().UseImage("Images/Misc/Noise");
                Filters.Scene["InfernumMode:EmpressOfLight"].GetShader().UseImage(ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/EmpressOfLight/EmpressOfLightWingsTexture"), 1);
                Filters.Scene["InfernumMode:EmpressOfLight"].GetShader().UseImage("Images/Misc/Perlin", 2);
                Filters.Scene["InfernumMode:EmpressOfLight"].GetShader().UseIntensity(ScreenShaderStrength);
            }

            WingFrameCounter++;
            AttackTimer++;
        }

        public void DoBehavior_SpawnAnimation()
        {
            int spawnAnimationTime = EmpressAurora.Lifetime - 30;

            // Fade in.
            npc.Opacity = MathHelper.Clamp((float)Math.Sqrt(AttackTimer / spawnAnimationTime), 0f, 1f);

            // Disable damage.
            npc.dontTakeDamage = true;

            // Summon auroras and descend.
            if (AttackTimer == 1f)
            {
                npc.velocity = Vector2.UnitY * 5f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 auroraSpawnPosition = npc.Center - Vector2.UnitY * 80f;
                    Utilities.NewProjectileBetter(auroraSpawnPosition, Vector2.Zero, ModContent.ProjectileType<EmpressAurora>(), 0, 0f);
                }

                npc.netUpdate = true;
            }

            npc.velocity *= 0.95f;

            // Scream short after being summoned.
            if (AttackTimer == 10f)
                Main.PlaySound(InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/EmpressOfLightSummon"), npc.Center);

            // Hold hands together for the first part of the animation.
            if (AttackTimer < spawnAnimationTime * 0.67f)
            {
                LeftArmFrame = 5f;
                RightArmFrame = 5f;
            }

            // Cast shimmers.
            if (AttackTimer > 10f && AttackTimer < spawnAnimationTime - 10f)
            {
                for (int i = 0; i < 6; i++)
                {
                    float dustPersistence = MathHelper.Lerp(1.3f, 0.7f, npc.Opacity) * Utils.InverseLerp(0f, spawnAnimationTime - 60f, AttackTimer, true);
                    Color newColor = Main.hslToRgb((AttackTimer / spawnAnimationTime + Main.rand.NextFloat(0.1f)) % 1f, 1f, 0.5f);
                    Dust rainbowMagic = Dust.NewDustDirect(npc.position, npc.width, npc.height, 267, 0f, 0f, 0, newColor, 1f);
                    rainbowMagic.position = npc.Center + Main.rand.NextVector2Circular(npc.width * 12f, npc.height * 12f) + new Vector2(0f, -150f);
                    rainbowMagic.velocity *= Main.rand.NextFloat(0.8f);
                    rainbowMagic.noGravity = true;
                    rainbowMagic.fadeIn = 0.7f + Main.rand.NextFloat(dustPersistence * 0.7f);
                    rainbowMagic.velocity += Vector2.UnitY * 3f;
                    rainbowMagic.scale = 0.6f;

                    rainbowMagic = Dust.CloneDust(rainbowMagic);
                    rainbowMagic.scale /= 2f;
                    rainbowMagic.fadeIn *= 0.85f;
                }
            }

            if (AttackTimer >= spawnAnimationTime)
                SelectNextAttack();
        }

        public void DoBehavior_PrismaticBoltCircle()
        {
            int boltReleaseDelay = 90;
            int boltReleaseTime = 74;
            int boltReleaseRate = 2;
            int attackSwitchDelay = 180;
            Vector2 handOffset = new Vector2(-55f, -30f);

            // Hover to the top left/right of the target.
            Vector2 hoverDestination = Target.Center + new Vector2((Target.Center.X < npc.Center.X).ToDirectionInt() * 400f, -250f);
            Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 13.5f;
            if (!npc.WithinRange(hoverDestination, 40f))
                npc.SimpleFlyMovement(idealVelocity, 0.75f);

            // Play a magic sound.
            if (AttackTimer == boltReleaseDelay)
                Main.PlaySound(InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/EmpressOfLightBoltCast"), npc.Center);

            // Fade out and teleport to the opposite side of the target halfway through the attack.
            if (AttackTimer >= boltReleaseDelay + boltReleaseTime / 2 - 10 && AttackTimer <= boltReleaseDelay + boltReleaseTime / 2)
            {
                npc.Opacity = Utils.InverseLerp(0f, -10f, AttackTimer - (boltReleaseDelay + boltReleaseTime / 2), true);
                if (npc.Opacity <= 0f)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<EmpressExplosion>(), 0, 0f);

                    float horizontalDistanceFromTarget = Target.Center.X - npc.Center.X;
                    npc.Opacity = 1f;
                    npc.position.X += horizontalDistanceFromTarget * 2f;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<EmpressExplosion>(), 0, 0f);

                    npc.netUpdate = true;
                }
            }

            // Release bolts.
            if (AttackTimer >= boltReleaseDelay && AttackTimer < boltReleaseDelay + boltReleaseTime)
            {
                LeftArmFrame = 3f;
                if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer % boltReleaseRate == 0f)
                {
                    float castCompletionInterpolant = Utils.InverseLerp(boltReleaseDelay, boltReleaseDelay + boltReleaseTime, AttackTimer, true);
                    Vector2 boltVelocity = -Vector2.UnitY.RotatedBy(MathHelper.TwoPi * castCompletionInterpolant) * 10.5f;
                    int bolt = Utilities.NewProjectileBetter(npc.Center + handOffset, boltVelocity, ModContent.ProjectileType<PrismaticBolt>(), 175, 0f);

                    if (Main.projectile.IndexInRange(bolt))
                    {
                        Main.projectile[bolt].ai[0] = npc.target;
                        Main.projectile[bolt].ai[1] = castCompletionInterpolant;
                    }
                }
            }

            if (AttackTimer >= boltReleaseDelay + boltReleaseTime + attackSwitchDelay)
                SelectNextAttack();
        }

        public void DoBehavior_MesmerizingMagic()
        {
            int shootRate = 75;
            int shootCount = 6;
            float wrappedAttackTimer = AttackTimer % shootRate;
            float slowdownFactor = Utils.InverseLerp(shootRate - 8f, shootRate - 24f, wrappedAttackTimer, true);
            float boltShootSpeed = 23f;
            ref float telegraphRotation = ref npc.Infernum().ExtraAI[0];
            ref float telegraphInterpolant = ref npc.Infernum().ExtraAI[1];
            ref float boltCount = ref npc.Infernum().ExtraAI[2];
            ref float totalHandsToShootFrom = ref npc.Infernum().ExtraAI[3];
            ref float shootCounter = ref npc.Infernum().ExtraAI[4];

            // Initialize things.
            if (totalHandsToShootFrom == 0f)
            {
                boltCount = 25f;
                totalHandsToShootFrom = 1f;
                if (InPhase2)
                {
                    boltCount = 18f;
                    totalHandsToShootFrom = 2f;
                }

                npc.netUpdate = true;
            }

            // Calculate the telegraph interpolant.
            telegraphInterpolant = Utils.InverseLerp(24f, shootRate - 18f, wrappedAttackTimer);

            // Hover to the top left/right of the target.
            Vector2 hoverDestination = Target.Center + new Vector2((Target.Center.X < npc.Center.X).ToDirectionInt() * 120f, -300f);
            Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 13.5f;
            if (!npc.WithinRange(hoverDestination, 40f))
                npc.SimpleFlyMovement(idealVelocity * slowdownFactor, slowdownFactor * 0.75f);
            else
                npc.velocity *= 0.93f;

            // Determinine the initial rotation of the telegraphs.
            if (wrappedAttackTimer == 4f)
            {
                telegraphRotation = Main.rand.NextFloat(MathHelper.TwoPi);
                npc.netUpdate = true;
            }

            // Rotate the telegraphs.
            telegraphRotation += CalamityUtils.Convert01To010(telegraphInterpolant) * MathHelper.Pi / 75f;

            // Release magic on hands and eventually create bolts.
            int magicDustCount = (int)Math.Round(MathHelper.Lerp(1f, 5f, telegraphInterpolant));
            for (int i = 0; i < 2; i++)
            {
                if (i >= totalHandsToShootFrom)
                    break;

                int handDirection = (i == 0).ToDirectionInt();
                Vector2 handOffset = new Vector2(55f, -30f);
                Vector2 handPosition = npc.Center + handOffset * new Vector2(handDirection, 1f);

                // Create magic dust.
                for (int j = 0; j < magicDustCount; j++)
                {
                    float magicHue = (AttackTimer / 45f + Main.rand.NextFloat(0.2f)) % 1f;
                    Dust magic = Dust.NewDustPerfect(handPosition, 267);
                    magic.color = Main.hslToRgb(magicHue, 1f, 0.5f);
                    magic.velocity = -Vector2.UnitY.RotatedByRandom(0.6f) * Main.rand.NextFloat(1f, 4f);
                    magic.scale *= 0.9f;
                    magic.noGravity = true;
                }

                // Raise hands.
                if (i == 0)
                    RightArmFrame = 3;
                else
                    LeftArmFrame = 3;

                // Release bolts outward and create hand explosions.
                if (wrappedAttackTimer == shootRate - 1f)
                {
                    if (i == 0)
                    {
                        var sound = Main.PlaySound(SoundID.DD2_PhantomPhoenixShot, npc.Center);
                        if (sound != null)
                            sound.Volume = MathHelper.Clamp(sound.Volume * 2.6f, 0f, 1f);
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int j = 0; j < boltCount; j++)
                        {
                            Vector2 boltShootVelocity = (MathHelper.TwoPi * j / boltCount + telegraphRotation).ToRotationVector2() * boltShootSpeed;
                            int bolt = Utilities.NewProjectileBetter(handPosition, boltShootVelocity, ModContent.ProjectileType<AcceleratingPrismaticBolt>(), 175, 0f);
                            if (Main.projectile.IndexInRange(bolt))
                                Main.projectile[bolt].ai[1] = j / boltCount;
                        }
                        Utilities.NewProjectileBetter(handPosition, Vector2.Zero, ModContent.ProjectileType<EmpressExplosion>(), 0, 0f);

                        if (i == 0)
                        {
                            shootCounter++;
                            if (shootCounter >= shootCount)
                                SelectNextAttack();
                        }

                        npc.netUpdate = true;
                    }
                }
            }
        }

        public void DoBehavior_HorizontalCharge()
        {
            int chargeCount = 6;
            int redirectTime = 40;
            int chargeTime = 45;
            int attackTransitionDelay = 8;
            float chargeSpeed = 56f;
            ref float chargeDirection = ref npc.Infernum().ExtraAI[0];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[1];

            // Initialize the charge direction.
            if (AttackTimer == 1f)
            {
                chargeDirection = (Target.Center.X > npc.Center.X).ToDirectionInt();
                npc.netUpdate = true;
            }

            // Hover into position before charging.
            if (AttackTimer <= redirectTime)
            {
                // Scream prior to charging.
                if (AttackTimer == redirectTime / 2)
                    Main.PlaySound(InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/EmpressOfLightDash"), npc.Center);

                Vector2 hoverDestination = Target.Center + Vector2.UnitX * chargeDirection * -500f;
                npc.Center = npc.Center.MoveTowards(hoverDestination, 6f);
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 25f, 1.5f);
                if (AttackTimer == redirectTime)
                    npc.velocity *= 0.3f;
            }
            else if (AttackTimer <= redirectTime + chargeTime)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitX * chargeDirection * chargeSpeed, 0.15f);
                if (AttackTimer == redirectTime + chargeTime)
                    npc.velocity *= 0.7f;

                // Do damage and become temporarily invulnerable. This is done to prevent dash-cheese.
                npc.damage = npc.defDamage;
                npc.dontTakeDamage = true;
            }
            else
                npc.velocity *= 0.92f;

            if (AttackTimer >= redirectTime + chargeTime + attackTransitionDelay)
            {
                AttackTimer = 0f;
                chargeCounter++;
                if (chargeCounter >= chargeCount)
                    SelectNextAttack();
                npc.netUpdate = true;
            }
        }

        public void DoBehavior_EnterSecondPhase()
        {
            int reappearTime = 35;

            // Don't take damage when transitioning.
            npc.dontTakeDamage = true;

            // Slow down.
            npc.velocity *= 0.8f;

            // Scream before fading out.
            if (AttackTimer == 10f)
                Main.PlaySound(InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/EmpressOfLightSummon"), npc.Center);

            // Fade out.
            if (AttackTimer <= SecondPhaseFadeoutTime)
                npc.Opacity = MathHelper.Lerp(1f, 0f, AttackTimer / SecondPhaseFadeoutTime);

            // Fade back in and teleport above the target.
            else if (AttackTimer <= SecondPhaseFadeoutTime + reappearTime)
            {
                if (AttackTimer == SecondPhaseFadeoutTime + 1f)
                {
                    npc.Center = Target.Center - Vector2.UnitY * 100f;
                    npc.netUpdate = true;
                }

                npc.Opacity = Utils.InverseLerp(0f, reappearTime, AttackTimer - SecondPhaseFadeoutTime, true);
            }

            if (AttackTimer >= SecondPhaseFadeoutTime + SecondPhaseFadeBackInTime)
                SelectNextAttack();
        }

        public void DoBehavior_LanceOctagon()
        {
            int lanceCount = 10;
            int lanceCreationRate = 50;
            int lanceBarrageCount = 8;
            float lanceSpawnOffset = 1000f;
            float targetCircleOffset = 350f;
            ref float lanceBarrageCounter = ref npc.Infernum().ExtraAI[0];

            // Hover above the target.
            Vector2 hoverDestination = Target.Center - Vector2.UnitY * 310f;
            Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 7f;
            if (!npc.WithinRange(hoverDestination, 40f))
                npc.SimpleFlyMovement(idealVelocity, 0.75f);
            else
                npc.velocity *= 0.96f;

            // Release lance telegraphs at the target. They will fire short afterward.
            if (AttackTimer % lanceCreationRate == lanceCreationRate - 1f)
            {
                Main.PlayTrackedSound(Utilities.GetTrackableSound("Sounds/Custom/EmpressOfLightLances"), npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 lanceOffset = (MathHelper.TwoPi * lanceBarrageCounter / lanceBarrageCount).ToRotationVector2() * lanceSpawnOffset;
                    Vector2 offsetOrthogonalDirection = lanceOffset.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2);

                    for (int i = 0; i < lanceCount; i++)
                    {
                        Vector2 orthogonalOffset = offsetOrthogonalDirection * MathHelper.Lerp(-1f, 1f, i / (float)(lanceCount - 1f));
                        Vector2 lanceDestination = Target.Center + orthogonalOffset.RotatedBy(MathHelper.PiOver2) * targetCircleOffset;
                        Vector2 lanceSpawnPosition = Target.Center + lanceOffset + orthogonalOffset * lanceSpawnOffset * 0.8f;

                        int lance = Utilities.NewProjectileBetter(lanceSpawnPosition, Vector2.Zero, ModContent.ProjectileType<EtherealLance>(), 180, 0f);
                        if (Main.projectile.IndexInRange(lance))
                        {
                            Main.projectile[lance].ai[0] = (lanceDestination - lanceSpawnPosition).ToRotation();
                            Main.projectile[lance].ai[1] = i / (float)lanceCount;
                        }

                        lanceSpawnPosition = Target.Center + lanceOffset - orthogonalOffset * lanceSpawnOffset * 0.8f;

                        lance = Utilities.NewProjectileBetter(lanceSpawnPosition, Vector2.Zero, ModContent.ProjectileType<EtherealLance>(), 180, 0f);
                        if (Main.projectile.IndexInRange(lance))
                        {
                            Main.projectile[lance].ai[0] = (lanceDestination - lanceSpawnPosition).ToRotation();
                            Main.projectile[lance].ai[1] = i / (float)lanceCount;
                        }
                    }
                    lanceBarrageCounter++;
                    if (lanceBarrageCounter >= lanceBarrageCount)
                        SelectNextAttack();

                    npc.netUpdate = true;
                }
            }
        }

        public void DoBehavior_RainbowWispForm()
        {
            int wispFormEnterTime = 60;
            int spinTime = 210;
            int prismCreationRate = 42;
            float spinSpeed = 36f;
            float spinOffset = 460f;
            ref float wispColorInterpolant = ref npc.Infernum().ExtraAI[0];

            // Hover above the target and enter the wisp form.
            if (AttackTimer <= wispFormEnterTime)
            {
                Vector2 hoverDestination = Target.Center - Vector2.UnitY * 350f;
                npc.velocity = Vector2.Zero.MoveTowards(hoverDestination - npc.Center, 16f);
                wispColorInterpolant = AttackTimer / wispFormEnterTime;
            }

            else
            {
                Vector2 hoverDestination = Target.Center - Vector2.UnitY.RotatedBy(MathHelper.TwoPi / spinTime * 2f * AttackTimer) * spinOffset;
                npc.velocity = Vector2.Zero.MoveTowards(hoverDestination - npc.Center, spinSpeed);

                if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer % prismCreationRate == prismCreationRate - 1f)
                {
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<EmpressExplosion>(), 0, 0f);
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<EmpressPrism>(), 0, 0f);
                }
            }
        }

        public void SelectNextAttack()
        {
            switch (AttackType)
            {
                case EmpressOfLightAttackType.SpawnAnimation:
                case EmpressOfLightAttackType.EnterSecondPhase:
                    npc.dontTakeDamage = false;
                    AttackType = EmpressOfLightAttackType.PrismaticBoltCircle;
                    break;
                case EmpressOfLightAttackType.PrismaticBoltCircle:
                    AttackType = EmpressOfLightAttackType.MesmerizingMagic;
                    break;
                case EmpressOfLightAttackType.MesmerizingMagic:
                    AttackType = EmpressOfLightAttackType.HorizontalCharge;
                    break;
                case EmpressOfLightAttackType.HorizontalCharge:
                    AttackType = InPhase2 ? EmpressOfLightAttackType.LanceOctagon : EmpressOfLightAttackType.PrismaticBoltCircle;
                    break;
                case EmpressOfLightAttackType.LanceOctagon:
                    AttackType = EmpressOfLightAttackType.RainbowWispForm;
                    break;
                case EmpressOfLightAttackType.RainbowWispForm:
                    AttackType = EmpressOfLightAttackType.PrismaticBoltCircle;
                    break;
            }
            AttackType = EmpressOfLightAttackType.RainbowWispForm;

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            AttackTimer = 0f;
            npc.netUpdate = true;
        }

        #endregion AI and Behaviors

        #region Drawing and Frames

        public override Color? GetAlpha(Color drawColor)
        {
            return Color.Lerp(drawColor, Color.White, 0.4f) * npc.Opacity;
        }

        public void PrepareShader()
        {
            Main.graphics.GraphicsDevice.Textures[1] = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/EmpressOfLight/EmpressOfLightWingsTexture");
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            Texture2D texture = Main.npcTexture[npc.type];
            Vector2 baseDrawPosition = npc.Center - Main.screenPosition;
            Color baseColor = npc.GetAlpha(drawColor);
            Texture2D wingOutlineTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/EmpressOfLight/EmpressOfLightWingsOutline");
            Texture2D leftArmTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/EmpressOfLight/EmpressOfLightLeftArm");
            Texture2D rightArmTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/EmpressOfLight/EmpressOfLightRightArm");
            Texture2D wingTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/EmpressOfLight/EmpressOfLightWings");
            Texture2D tentacleTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/EmpressOfLight/EmpressOfLightTentacles");
            Texture2D dressGlowmaskTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/EmpressOfLight/EmpressOfLightGlowmask");

            Rectangle tentacleFrame = tentacleTexture.Frame(1, 8, 0, (int)(WingFrameCounter / 5f) % 8);
            Rectangle wingFrame = wingOutlineTexture.Frame(1, 11, 0, (int)(WingFrameCounter / 5f) % 11);
            Rectangle leftArmFrame = leftArmTexture.Frame(1, 7, 0, (int)LeftArmFrame);
            Rectangle rightArmFrame = rightArmTexture.Frame(1, 7, 0, (int)RightArmFrame);
            Vector2 origin = leftArmFrame.Size() / 2f;
            Vector2 origin2 = rightArmFrame.Size() / 2f;
            SpriteEffects direction = npc.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            int leftArmDrawOrder = 0;
            int rightArmDrawOrder = 0;
            if (LeftArmFrame == 5)
                leftArmDrawOrder = 1;

            if (RightArmFrame == 5)
                rightArmDrawOrder = 1;

            float baseColorOpacity = 1f;
            int laggingAfterimageCount = 0;
            int baseDuplicateCount = 0;
            float afterimageOffsetFactor = 0f;
            float opacity = 0f;
            float duplicateFade = 0f;

            // Define variables for the horizontal charge state.
            if (AttackType == EmpressOfLightAttackType.HorizontalCharge)
            {
                afterimageOffsetFactor = Utils.InverseLerp(0f, 30f, AttackTimer, true) * Utils.InverseLerp(90f, 30f, AttackTimer, true);
                opacity = Utils.InverseLerp(0f, 30f, AttackTimer, true) * Utils.InverseLerp(90f, 70f, AttackTimer, true);
                duplicateFade = Utils.InverseLerp(0f, 15f, AttackTimer, true) * Utils.InverseLerp(45f, 30f, AttackTimer, true);
                baseColor = Color.Lerp(baseColor, Color.White, afterimageOffsetFactor);
                baseColorOpacity *= 1f - duplicateFade;
                laggingAfterimageCount = 4;
                baseDuplicateCount = 3;
            }

            if (AttackType == EmpressOfLightAttackType.EnterSecondPhase)
            {
                afterimageOffsetFactor = Utils.InverseLerp(30f, SecondPhaseFadeoutTime, AttackTimer, true) * 
                    Utils.InverseLerp(SecondPhaseFadeBackInTime, 0f, AttackTimer - SecondPhaseFadeoutTime, true);
                opacity = Utils.InverseLerp(0f, 60f, AttackTimer, true) * 
                    Utils.InverseLerp(SecondPhaseFadeBackInTime, SecondPhaseFadeBackInTime - 60f, AttackTimer - SecondPhaseFadeoutTime, true);
                duplicateFade = Utils.InverseLerp(0f, 60f, AttackTimer, true) * 
                    Utils.InverseLerp(SecondPhaseFadeBackInTime, SecondPhaseFadeBackInTime - 60f, AttackTimer - SecondPhaseFadeoutTime, true);
                baseColor = Color.Lerp(baseColor, Color.White, afterimageOffsetFactor);
                baseColorOpacity *= 1f - duplicateFade;
                baseDuplicateCount = 4;
            }

            if (AttackType == EmpressOfLightAttackType.RainbowWispForm)
            {
                baseColorOpacity = afterimageOffsetFactor = opacity = npc.Infernum().ExtraAI[0];
                baseColor = Color.Lerp(baseColor, Color.White, baseColorOpacity);
                baseDuplicateCount = 2;
                laggingAfterimageCount = 4;
            }

            if (baseDuplicateCount + laggingAfterimageCount > 0)
            {
                for (int i = -baseDuplicateCount; i <= baseDuplicateCount + laggingAfterimageCount; i++)
                {
                    if (i == 0)
                        continue;

                    Color duplicateColor = Color.White;
                    Vector2 drawPosition = baseDrawPosition;

                    // Create cool afterimages while charging at the target.
                    if (AttackType == EmpressOfLightAttackType.HorizontalCharge)
                    {
                        float hue = (i + 5f) / 10f;
                        float drawOffsetFactor = 80f;
                        Vector3 offsetInformation = Vector3.Transform(Vector3.Forward, 
                            Matrix.CreateRotationX((Main.GlobalTime - 0.3f + i * 0.1f) * 0.7f * MathHelper.TwoPi) * 
                            Matrix.CreateRotationY((Main.GlobalTime - 0.8f + i * 0.3f) * 0.7f * MathHelper.TwoPi) * 
                            Matrix.CreateRotationZ((Main.GlobalTime + i * 0.5f) * 0.1f * MathHelper.TwoPi));
                        drawOffsetFactor += Utils.InverseLerp(-1f, 1f, offsetInformation.Z, true) * 150f;
                        Vector2 drawOffset = new Vector2(offsetInformation.X, offsetInformation.Y) * drawOffsetFactor * afterimageOffsetFactor;
                        drawOffset = drawOffset.RotatedBy(MathHelper.TwoPi * AttackTimer / 180f);

                        float luminanceInterpolant = Utils.InverseLerp(90f, 0f, AttackTimer, true);
                        duplicateColor = Main.hslToRgb(hue, 1f, MathHelper.Lerp(0.5f, 1f, luminanceInterpolant)) * opacity * 0.8f;
                        duplicateColor.A /= 3;
                        drawPosition += drawOffset;
                    }

                    // Handle the wisp form afterimages.
                    if (AttackType == EmpressOfLightAttackType.RainbowWispForm)
                    {
                        float hue = (i + 5f) / 10f % 1f;
                        float drawOffsetFactor = 80f;
                        Vector3 offsetInformation = Vector3.Transform(Vector3.Forward,
                            Matrix.CreateRotationX((Main.GlobalTime * 1.3f - 0.4f + i * 0.16f) * 0.7f * MathHelper.TwoPi) *
                            Matrix.CreateRotationY((Main.GlobalTime * 1.3f - 0.7f + i * 0.32f) * 0.7f * MathHelper.TwoPi) *
                            Matrix.CreateRotationZ((Main.GlobalTime * 1.3f + 0.3f + i * 0.6f) * 0.1f * MathHelper.TwoPi));
                        drawOffsetFactor += Utils.InverseLerp(-1f, 1f, offsetInformation.Z, true) * 30f;
                        Vector2 drawOffset = new Vector2(offsetInformation.X, offsetInformation.Y) * drawOffsetFactor * afterimageOffsetFactor;
                        drawOffset = drawOffset.RotatedBy(MathHelper.TwoPi * AttackTimer / 180f);

                        duplicateColor = Main.hslToRgb(hue, 1f, 0.6f) * opacity;
                        if (i > baseDuplicateCount)
                            duplicateColor = Main.hslToRgb((i - baseDuplicateCount - 1f) / laggingAfterimageCount % 1f, 1f, 0.5f) * opacity;

                        duplicateColor.A /= 12;
                        drawPosition += drawOffset;
                    }

                    // Do the transition visuals for phase 2.
                    if (AttackType == EmpressOfLightAttackType.EnterSecondPhase)
                    {
                        // Fade in.
                        if (AttackTimer >= SecondPhaseFadeoutTime)
                        {
                            int offsetIndex = i;
                            if (offsetIndex < 0)
                                offsetIndex++;

                            Vector2 circularOffset = ((offsetIndex + 0.5f) * MathHelper.PiOver4 + Main.GlobalTime * MathHelper.Pi * 1.333f).ToRotationVector2();
                            drawPosition += circularOffset * afterimageOffsetFactor * new Vector2(600f, 150f);
                        }

                        // Fade out and create afterimages that dissipate.
                        else
                            drawPosition += Vector2.UnitX * i * afterimageOffsetFactor * 200f;

                        duplicateColor = Color.White * opacity * baseColorOpacity * 0.8f;
                        duplicateColor.A /= 3;
                    }

                    // Create lagging afterimages.
                    if (i > baseDuplicateCount)
                    {
                        float lagBehindFactor = Utils.InverseLerp(30f, 70f, AttackTimer, true);
                        if (lagBehindFactor == 0f)
                            continue;

                        drawPosition = baseDrawPosition + npc.velocity * -3f * (i - baseDuplicateCount - 1f) * lagBehindFactor;
                        duplicateColor *= 1f - duplicateFade;
                    }

                    // Draw wings.
                    spriteBatch.Draw(wingOutlineTexture, drawPosition, wingFrame, duplicateColor, npc.rotation, wingFrame.Size() / 2f, npc.scale * 2f, direction, 0f);
                    spriteBatch.Draw(wingTexture, drawPosition, wingFrame, duplicateColor, npc.rotation, wingFrame.Size() / 2f, npc.scale * 2f, direction, 0f);

                    // Draw tentacles in phase 2.
                    if (InPhase2)
                        spriteBatch.Draw(tentacleTexture, drawPosition, tentacleFrame, duplicateColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);

                    // Draw the base texture.
                    spriteBatch.Draw(texture, drawPosition, npc.frame, duplicateColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);

                    // Draw hands.
                    for (int j = 0; j < 2; j++)
                    {
                        if (j == leftArmDrawOrder)
                            spriteBatch.Draw(leftArmTexture, drawPosition, leftArmFrame, duplicateColor, npc.rotation, origin, npc.scale, direction, 0f);

                        if (j == rightArmDrawOrder)
                            spriteBatch.Draw(rightArmTexture, drawPosition, rightArmFrame, duplicateColor, npc.rotation, origin2, npc.scale, direction, 0f);
                    }
                }
            }

            baseColor *= baseColorOpacity;
            void DrawInstance(Vector2 drawPosition, Color color, Color? tentacleDressColorOverride = null)
            {
                // Draw wings. This involves usage of a shader to give the wing texture.
                spriteBatch.Draw(wingOutlineTexture, drawPosition, wingFrame, color, npc.rotation, wingFrame.Size() / 2f, npc.scale * 2f, direction, 0f);

                spriteBatch.EnterShaderRegion();

                DrawData wingData = new DrawData(wingTexture, drawPosition, wingFrame, color, npc.rotation, wingFrame.Size() / 2f, npc.scale * 2f, direction, 0);
                PrepareShader();
                GameShaders.Misc["Infernum:EmpressOfLightWings"].Apply(wingData);
                wingData.Draw(spriteBatch);
                spriteBatch.ExitShaderRegion();

                float pulse = (float)Math.Sin(Main.GlobalTime * MathHelper.Pi) * 0.5f + 0.5f;
                Color tentacleDressColor = Main.hslToRgb((pulse * 0.08f + 0.6f) % 1f, 1f, 0.5f);
                tentacleDressColor.A = 0;
                tentacleDressColor *= 0.6f;
                if (Enraged)
                {
                    tentacleDressColor = Main.OurFavoriteColor;
                    tentacleDressColor.A = 0;
                    tentacleDressColor *= 0.3f;
                }
                tentacleDressColor = tentacleDressColorOverride ?? tentacleDressColor;
                tentacleDressColor *= baseColorOpacity * npc.Opacity;

                // Draw tentacles.
                if (InPhase2)
                {
                    spriteBatch.Draw(tentacleTexture, drawPosition, tentacleFrame, color, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 drawOffset = npc.rotation.ToRotationVector2().RotatedBy(MathHelper.TwoPi * i / 4f + MathHelper.PiOver4) * MathHelper.Lerp(2f, 8f, pulse);
                        spriteBatch.Draw(tentacleTexture, drawPosition + drawOffset, tentacleFrame, tentacleDressColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);
                    }
                }

                // Draw the base texture.
                spriteBatch.Draw(texture, drawPosition, npc.frame, color, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);

                // Draw the dress.
                if (InPhase2)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 drawOffset = npc.rotation.ToRotationVector2().RotatedBy(MathHelper.TwoPi * i / 4f + MathHelper.PiOver4) * MathHelper.Lerp(2f, 8f, pulse);
                        spriteBatch.Draw(dressGlowmaskTexture, drawPosition + drawOffset, null, tentacleDressColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);
                    }
                }

                // Draw arms.
                for (int k = 0; k < 2; k++)
                {
                    if (k == leftArmDrawOrder)
                        spriteBatch.Draw(leftArmTexture, drawPosition, leftArmFrame, color, npc.rotation, origin, npc.scale, direction, 0f);

                    if (k == rightArmDrawOrder)
                        spriteBatch.Draw(rightArmTexture, drawPosition, rightArmFrame, color, npc.rotation, origin2, npc.scale, direction, 0f);
                }
            }

            if (AttackType == EmpressOfLightAttackType.RainbowWispForm)
            {
                float wispInterpolant = npc.Infernum().ExtraAI[0];
                for (int i = 0; i < 10; i++)
                {
                    Color wispColor = Main.hslToRgb((Main.GlobalTime * 0.6f + 0.2f + i / 10f) % 1f, 1f, 0.5f) * baseColorOpacity * 0.2f;
                    wispColor = Color.Lerp(baseColor, wispColor, wispInterpolant);
                    wispColor.A /= 6;

                    Vector2 drawOffset = (MathHelper.TwoPi * i / 10f + Main.GlobalTime * 3f).ToRotationVector2() * wispInterpolant * 20f;
                    DrawInstance(baseDrawPosition + drawOffset, wispColor, wispColor);
                }

                Color baseInstanceColor = baseColor;
                baseInstanceColor.A /= 5;
                DrawInstance(baseDrawPosition, baseInstanceColor);
            }
            else
                DrawInstance(baseDrawPosition, baseColor);

            // Draw telegraphs.
            if (AttackType == EmpressOfLightAttackType.MesmerizingMagic)
            {
                float telegraphRotation = npc.Infernum().ExtraAI[0];
                float telegraphInterpolant = npc.Infernum().ExtraAI[1];
                float boltCount = npc.Infernum().ExtraAI[2];
                float totalHandsToShootFrom = npc.Infernum().ExtraAI[3];

                if (telegraphInterpolant <= 0f)
                    return false;

                for (int i = 0; i < 2; i++)
                {
                    if (i >= totalHandsToShootFrom)
                        break;

                    int handDirection = (i == 0).ToDirectionInt();
                    float telegraphWidth = MathHelper.Lerp(0.5f, 4f, telegraphInterpolant);
                    Vector2 handOffset = new Vector2(55f, -30f);
                    Vector2 handPosition = npc.Center + handOffset * new Vector2(handDirection, 1f);

                    for (int j = 0; j < boltCount; j++)
                    {
                        Color telegraphColor = Main.hslToRgb(j / (float)boltCount, 1f, 0.5f) * (float)Math.Sqrt(telegraphInterpolant) * 0.6f;
                        Vector2 telegraphDirection = (MathHelper.TwoPi * j / boltCount + telegraphRotation).ToRotationVector2();
                        Vector2 start = handPosition;
                        Vector2 end = start + telegraphDirection * 4500f;
                        spriteBatch.DrawLineBetter(start, end, telegraphColor, telegraphWidth);
                    }
                }
            }
            return false;
        }

        public override void FindFrame(int frameHeight)
        {
            npc.frame.Y = InPhase2.ToInt() * frameHeight;
        }

        #endregion Drawing and Frames

        #region Hit Effects and Loot

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }

        public override bool CheckActive() => false;

        #endregion Hit Effects and Loot
    }
}
