using CalamityMod;
using CalamityMod.Items.Potions;
using CalamityMod.Skies;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares;
using InfernumMode.GlobalInstances;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.GameContent;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Athena
{
    [AutoloadBossHead]
    public class AthenaNPC : ModNPC
    {
        public class AthenaTurret
        {
            private int frame;
            public int Frame
            {
                get => frame;
                set => frame = Utils.Clamp(value, 0, IsSmall ? 2 : 3);
            }
            public bool IsSmall;
        }

        public enum AthenaAttackType
        {
            CircleOfLightning,
            ExowlHologramSwarm,
            AimedPulseLasers,
            DashingIllusions,
            ElectricCharge,
            IllusionRocketCharge
        }

        public enum AthenaTurretFrameType
        {
            Blinking,
            OpenMainTurret,
            CloseAllTurrets,
            OpenAllTurrets
        }

        public PrimitiveTrail FlameTrail = null;

        public PrimitiveTrail LightRayDrawer = null;

        public AthenaTurret[] Turrets = new AthenaTurret[5];

        public Player Target => Main.player[NPC.target];

        public AthenaAttackType AttackState
        {
            get => (AthenaAttackType)(int)NPC.ai[0];
            set => NPC.ai[0] = (int)value;
        }

        public bool HasInitialized
        {
            get => NPC.ai[2] == 1f;
            set => NPC.ai[2] = value.ToInt();
        }

        public AthenaTurretFrameType TurretFrameState
        {
            get => (AthenaTurretFrameType)(int)NPC.localAI[0];
            set => NPC.localAI[0] = (int)value;
        }

        public bool HasSummonedComplementMech
        {
            get => NPC.Infernum().ExtraAI[ExoMechManagement.HasSummonedComplementMechIndex] == 1f;
            set => NPC.Infernum().ExtraAI[ExoMechManagement.HasSummonedComplementMechIndex] = value.ToInt();
        }

        public bool WasInitialSummon
        {
            get => NPC.Infernum().ExtraAI[ExoMechManagement.WasNotInitialSummonIndex] == 0f;
            set => NPC.Infernum().ExtraAI[ExoMechManagement.WasNotInitialSummonIndex] = (!value).ToInt();
        }

        public ref float AttackTimer => ref NPC.ai[1];

        public ref float MinionRedCrystalGlow => ref NPC.localAI[1];

        public ref float TelegraphInterpolant => ref NPC.localAI[2];

        public ref float TelegraphRotation => ref NPC.localAI[3];

        public ref float ComplementMechIndex => ref NPC.Infernum().ExtraAI[ExoMechManagement.ComplementMechIndexIndex];

        public ref float FinalMechIndex => ref NPC.Infernum().ExtraAI[ExoMechManagement.FinalMechIndexIndex];

        public static Vector2 UniversalVerticalTurretOffset => Vector2.UnitY * -22f;

        public static Vector2[] TurretOffsets => new Vector2[]
        {
            UniversalVerticalTurretOffset + new Vector2(-66f, -6f),
            UniversalVerticalTurretOffset + new Vector2(-36f, -2f),
            UniversalVerticalTurretOffset,
            UniversalVerticalTurretOffset + new Vector2(36f, -2f),
            UniversalVerticalTurretOffset + new Vector2(66f, -6f),
        };

        public Vector2 MainTurretCenter => GetTurretPosition(2);

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("XM-04 Athena");
            NPCID.Sets.TrailingMode[NPC.type] = 3;
            NPCID.Sets.TrailCacheLength[NPC.type] = 10;
        }

        public override void SetDefaults()
        {
            AthenaSetDefaults(NPC);
            NPC.boss = true;
        }
        
        public static void AthenaSetDefaults(NPC npc)
        {
            npc.npcSlots = 5f;
            npc.damage = 450;
            npc.width = 230;
            npc.height = 170;
            npc.defense = 100;
            npc.DR_NERD(0.35f);
            npc.LifeMaxNERB(1300000, 1300000, 1300000);
            double HPBoost = CalamityConfig.Instance.BossHealthBoost * 0.01;
            npc.lifeMax += (int)(npc.lifeMax * HPBoost);
            npc.aiStyle = -1;
            npc.ModNPC.AIType = -1;
            npc.knockBackResist = 0f;
            npc.value = Item.buyPrice(3, 33, 0, 0);
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.HitSound = SoundID.NPCHit4;
            npc.DeathSound = SoundID.NPCDeath14;
            npc.netAlways = true;
            npc.Calamity().canBreakPlayerDefense = true;
            npc.Calamity().VulnerableToSickness = false;
            npc.Calamity().VulnerableToElectricity = true;
        }

        #region Syncing

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(TelegraphRotation);
            writer.Write(NPC.Opacity);
            writer.Write(Turrets.Length);
            BitsByte[] turretSizes = new BitsByte[Turrets.Length / 8 + 1];
            for (int i = 0; i < Turrets.Length; i++)
                turretSizes[i / 8][i % 8] = Turrets[i].IsSmall;

            for (int i = 0; i < turretSizes.Length; i++)
                writer.Write(turretSizes[i]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            TelegraphRotation = reader.ReadSingle();
            NPC.Opacity = reader.ReadSingle();
            int turretCount = reader.ReadInt32();
            int turretSizeCount = turretCount / 8 + 1;
            Turrets = new AthenaTurret[turretCount];

            for (int i = 0; i < turretSizeCount; i++)
            {
                BitsByte turretSizes = reader.ReadByte();
                for (int j = 0; j < 8; j++)
                {
                    if (i * 8 + j >= turretCount)
                        break;

                    Turrets[i * 8 + j].IsSmall = turretSizes[j];
                }
            }
        }

        #endregion Syncing

        #region AI and Behaviors
        public void InitializeTurrets()
        {
            Turrets = new AthenaTurret[5];
            for (int i = 0; i < Turrets.Length; i++)
            {
                Turrets[i] = new AthenaTurret()
                {
                    IsSmall = i != 2
                };
            }
        }

        public override void AI()
        {
            float lifeRatio = NPC.life / (float)NPC.lifeMax;
            NPC initialMech = ExoMechManagement.FindInitialMech();
            NPC complementMech = ComplementMechIndex >= 0 && Main.npc[(int)ComplementMechIndex].active && Utilities.IsExoMech(Main.npc[(int)ComplementMechIndex]) ? Main.npc[(int)ComplementMechIndex] : null;
            NPC finalMech = ExoMechManagement.FindFinalMech();

            // Handle initializations.
            if (!HasInitialized)
            {
                InitializeTurrets();
                FinalMechIndex = -1f;
                ComplementMechIndex = -1f;
                HasInitialized = true;
                NPC.netUpdate = true;
            }

            // Summon the complement mech and reset things once ready.
            if (!HasSummonedComplementMech && lifeRatio < ExoMechManagement.Phase4LifeRatio)
            {
                Exowl.MakeAllExowlsExplode();
                ExoMechManagement.SummonComplementMech(NPC);
                HasSummonedComplementMech = true;
                AttackTimer = 0f;
                SelectNextAttack();
                NPC.netUpdate = true;
            }

            // Summon the final mech once ready.
            if (WasInitialSummon && FinalMechIndex == -1f && complementMech != null && complementMech.life / (float)complementMech?.lifeMax < ExoMechManagement.ComplementMechInvincibilityThreshold)
            {
                Exowl.MakeAllExowlsExplode();
                ExoMechManagement.SummonFinalMech(NPC);
                NPC.netUpdate = true;
            }

            // Search for a target.
            NPC.TargetClosestIfTargetIsInvalid();

            // Set the global whoAmI index.
            GlobalNPCOverrides.Athena = NPC.whoAmI;

            // Become invincible if the complement mech is at high enough health or if in the middle of a death animation.
            NPC.dontTakeDamage = ExoMechAIUtilities.PerformingDeathAnimation(NPC);
            if (ComplementMechIndex >= 0 && Main.npc[(int)ComplementMechIndex].active && Main.npc[(int)ComplementMechIndex].life > Main.npc[(int)ComplementMechIndex].lifeMax * ExoMechManagement.ComplementMechInvincibilityThreshold)
                NPC.dontTakeDamage = true;

            // Become invincible and disappear if necessary.
            if (ExoMechAIUtilities.ShouldExoMechVanish(NPC))
            {
                NPC.damage = 0;
                NPC.Opacity = MathHelper.Clamp(NPC.Opacity - 0.08f, 0f, 1f);
                if (NPC.Opacity <= 0f)
                    NPC.Center = Target.Center - Vector2.UnitY * 1600f;

                AttackTimer = 0f;
                AttackState = AthenaAttackType.AimedPulseLasers;
                NPC.Calamity().ShouldCloseHPBar = true;
                NPC.dontTakeDamage = true;
            }
            else
                NPC.Opacity = MathHelper.Clamp(NPC.Opacity + 0.08f, 0f, 1f);

            // Reset things.
            NPC.damage = NPC.defDamage;
            MinionRedCrystalGlow = 0f;
            TelegraphInterpolant = 0f;
            TurretFrameState = AthenaTurretFrameType.CloseAllTurrets;

            // Handle attacks.
            switch (AttackState)
            {
                case AthenaAttackType.CircleOfLightning:
                    DoBehavior_CircleOfLightning();
                    break;
                case AthenaAttackType.ExowlHologramSwarm:
                    DoBehavior_ExowlHologramSwarm();
                    break;
                case AthenaAttackType.AimedPulseLasers:
                    DoBehavior_AimedPulseLasers();
                    break;
                case AthenaAttackType.DashingIllusions:
                    DoBehavior_DashingIllusions();
                    break;
                case AthenaAttackType.ElectricCharge:
                    DoBehavior_ElectricCharge();
                    break;
                case AthenaAttackType.IllusionRocketCharge:
                    DoBehavior_IllusionRocketCharge();
                    break;
            }

            if (ExoMechComboAttackContent.UseTwinsAthenaComboAttack(NPC, 1f, ref AttackTimer, ref NPC.localAI[0]))
                SelectNextAttack();

            AttackTimer++;
        }

        public void DoBehavior_CircleOfLightning()
        {
            int teleportFadeTime = 8;
            int teleportTime = teleportFadeTime * 2;
            int circleSummonDelay = 36;
            int telegraphTime = 42;
            int shootDelay = 38;
            int lightningShootRate = 4;
            int lightningShootTime = 170;

            TurretFrameState = AthenaTurretFrameType.CloseAllTurrets;
            if (AttackTimer >= teleportTime + circleSummonDelay)
                TurretFrameState = AthenaTurretFrameType.Blinking;
            if (AttackTimer >= teleportTime + circleSummonDelay + telegraphTime)
                TurretFrameState = AthenaTurretFrameType.OpenMainTurret;

            // Fade out.
            if (AttackTimer < teleportFadeTime)
            {
                NPC.velocity *= 0.5f;
                NPC.Opacity = MathHelper.Clamp(NPC.Opacity - 0.15f, 0f, 1f);
            }
            
            // Teleport.
            if (AttackTimer == teleportFadeTime)
            {
                SoundEngine.PlaySound(SoundID.Item103, NPC.Center);
                NPC.velocity = Vector2.Zero;
                NPC.Center = Target.Center + (MathHelper.TwoPi * Main.rand.Next(4) / 4f).ToRotationVector2() * 450f;
                SoundEngine.PlaySound(SoundID.Item104, NPC.Center);
                NPC.netUpdate = true;
            }

            // Fade back in.
            if (AttackTimer >= teleportFadeTime && AttackTimer < teleportTime)
                NPC.Opacity = MathHelper.Clamp(NPC.Opacity + 0.15f, 0f, 1f);

            // Summon a circle of minions that spin in place and act as a barrier.
            // The player can technically teleport out of the circle, but doing so prevents seeing the boss.
            if (AttackTimer == teleportTime + circleSummonDelay)
            {
                SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Custom/ThunderStrike"), NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Utilities.NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<ExowlCircleSummonBoom>(), 0, 0f);

                    List<int> circle = new();
                    for (int i = 0; i < 15; i++)
                    {
                        int exowl = NPC.NewNPC(new InfernumSource(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<Exowl>(), NPC.whoAmI);
                        if (Main.npc.IndexInRange(exowl))
                        {
                            Main.npc[exowl].ModNPC<Exowl>().CircleCenter = NPC.Center;
                            Main.npc[exowl].ModNPC<Exowl>().CircleRadius = 1100f;
                            Main.npc[exowl].ModNPC<Exowl>().CircleOffsetAngle = MathHelper.TwoPi * i / 15f;
                            Main.npc[exowl].netUpdate = true;
                            circle.Add(exowl);
                        }
                    }

                    // Attach every member of the circle.
                    for (int i = 0; i < circle.Count; i++)
                        Main.npc[circle[i]].ModNPC<Exowl>().NPCToAttachTo = circle[(i + 1) % circle.Count];
                }
                NPC.netUpdate = true;
            }

            // Determine telegraph variables.
            if (AttackTimer >= teleportTime + circleSummonDelay && AttackTimer < teleportTime + circleSummonDelay + telegraphTime)
                TelegraphRotation = TelegraphRotation.AngleLerp(NPC.AngleTo(Target.Center + Target.velocity * 15f), 0.3f);

            if (AttackTimer < teleportTime + circleSummonDelay + teleportTime + shootDelay)
                TelegraphInterpolant = Utils.GetLerpValue(0f, telegraphTime + shootDelay, AttackTimer - (teleportTime + circleSummonDelay), true);

            // Release the lightning.
            else if (AttackTimer % lightningShootRate == 0f)
            {
                if (AttackTimer % (lightningShootRate * 2f) == 0f)
                    SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Item/TeslaCannonFire"), MainTurretCenter);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 lightningShootVelocity = TelegraphRotation.ToRotationVector2() * 8.4f;
                    int lightning = Utilities.NewProjectileBetter(MainTurretCenter - lightningShootVelocity * 12f, lightningShootVelocity, ModContent.ProjectileType<TerateslaLightningBlast>(), 530, 0f);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Main.projectile[lightning].ai[0] = TelegraphRotation;
                        Main.projectile[lightning].ai[1] = Main.rand.Next(100);
                    }

                    TelegraphRotation += MathHelper.TwoPi / lightningShootTime * lightningShootRate * 1.75f;
                    NPC.netUpdate = true;
                }
            }

            MinionRedCrystalGlow = Utils.GetLerpValue(0f, 60f, AttackTimer - (teleportTime + circleSummonDelay), true);

            if (AttackTimer >= teleportTime + circleSummonDelay + teleportTime + shootDelay + lightningShootTime)
            {
                SelectNextAttack();
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].type == ModContent.NPCType<Exowl>())
                        Main.npc[i].active = false;
                }
            }
        }

        public void DoBehavior_ExowlHologramSwarm()
        {
            int teleportFadeTime = 8;
            int teleportTime = teleportFadeTime * 2;
            int hologramCreationTime = 150;
            int holographFadeoutTime = 50;
            int hologramAttackTime = 360;
            ref float hologramInterpolant = ref NPC.Infernum().ExtraAI[0];
            ref float illusionCount = ref NPC.Infernum().ExtraAI[1];
            ref float hologramSpan = ref NPC.Infernum().ExtraAI[2];
            ref float exowlIllusionFadeInterpolant = ref NPC.Infernum().ExtraAI[3];
            ref float hologramRayDissipation = ref NPC.Infernum().ExtraAI[4];

            // Initialize the minion illusion count.
            if (illusionCount == 0f)
                illusionCount = 13f;

            // Fade out.
            if (AttackTimer < teleportFadeTime)
            {
                NPC.velocity *= 0.5f;
                NPC.Opacity = MathHelper.Clamp(NPC.Opacity - 0.15f, 0f, 1f);
            }

            // Teleport.
            if (AttackTimer == teleportFadeTime)
            {
                SoundEngine.PlaySound(SoundID.Item103, NPC.Center);
                NPC.velocity = Vector2.Zero;
                NPC.Center = Target.Center + new Vector2(Main.rand.NextBool().ToDirectionInt() * 510f, -240f);
                while (Collision.SolidCollision(NPC.position - Vector2.One * 200f, NPC.width + 400, NPC.height + 400))
                    NPC.position.Y -= 120f;

                SoundEngine.PlaySound(SoundID.Item104, NPC.Center);
                SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Custom/CodebreakerBeam"), NPC.Center);
                NPC.netUpdate = true;
            }

            // Fade back in.
            if (AttackTimer >= teleportFadeTime && AttackTimer < teleportTime)
                NPC.Opacity = MathHelper.Clamp(NPC.Opacity + 0.15f, 0f, 1f);

            // Calculate the hologram interpolant, span, and fade interpolant.
            hologramInterpolant = Utils.GetLerpValue(0f, 36f, AttackTimer - teleportTime, true);
            hologramRayDissipation = Utils.GetLerpValue(hologramCreationTime, hologramCreationTime - 12f, AttackTimer - teleportTime, true);
            hologramSpan = MathHelper.Lerp(8f, 450f, (float)Math.Pow(hologramInterpolant, 1.73) * hologramRayDissipation);
            exowlIllusionFadeInterpolant = Utils.GetLerpValue(0f, holographFadeoutTime, AttackTimer - teleportTime - hologramCreationTime, true);
            if (AttackTimer > teleportTime + hologramCreationTime)
            {
                Vector2 idealVelocity = NPC.SafeDirectionTo(Target.Center + (MathHelper.TwoPi * AttackTimer / 150f).ToRotationVector2() * 500f) * 16f;
                NPC.SimpleFlyMovement(idealVelocity, idealVelocity.Length() / 20f);
                hologramInterpolant = 0f;
            }

            // Transform the holograms into true exowls.
            if (AttackTimer == teleportTime + hologramCreationTime)
            {
                SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Item/LargeWeaponFire"), NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int realExowlIndex = Main.rand.Next((int)illusionCount);
                    for (int i = 0; i < illusionCount; i++)
                    {
                        Vector2 hologramPosition = GetHologramPosition(i, illusionCount, hologramSpan, hologramInterpolant);
                        int exowl = NPC.NewNPC(new InfernumSource(), (int)hologramPosition.X, (int)hologramPosition.Y, ModContent.NPCType<Exowl>(), NPC.whoAmI);
                        if (Main.npc.IndexInRange(exowl))
                        {
                            Main.npc[exowl].ModNPC<Exowl>().UseConfusionEffect = true;
                            Main.npc[exowl].ModNPC<Exowl>().IsIllusion = i != realExowlIndex;
                        }
                    }
                    NPC.netUpdate = true;
                }
            }

            if (AttackTimer == teleportTime + hologramCreationTime + hologramAttackTime)
            {
                Exowl.MakeAllExowlsExplode();
                NPC.netUpdate = true;
            }

            if (AttackTimer >= teleportTime + hologramCreationTime + hologramAttackTime + 45f)
                SelectNextAttack();
        }

        public void DoBehavior_AimedPulseLasers()
        {
            int teleportFadeTime = 10;
            int teleportTime = teleportFadeTime * 2;
            int telegraphTime = PulseBeamTelegraph.Lifetime;
            int laserShootTime = PulseBeamStart.LifetimeConst;
            int pulseLaserReleaseRate = 7;
            int laserCount = 4;
            float predictionFactor = 16f;
            ref float pulseLaserDirection = ref NPC.Infernum().ExtraAI[0];
            ref float pulseLaserShootCounter = ref NPC.Infernum().ExtraAI[1];

            // Fade out.
            if (AttackTimer < teleportFadeTime)
            {
                NPC.velocity *= 0.5f;
                NPC.Opacity = MathHelper.Clamp(NPC.Opacity - 0.15f, 0f, 1f);
            }

            // Teleport.
            if (AttackTimer == teleportFadeTime)
            {
                SoundEngine.PlaySound(SoundID.Item103, NPC.Center);
                NPC.velocity = Vector2.Zero;
                NPC.Center = Target.Center - Target.velocity.SafeNormalize(Main.rand.NextVector2Unit()) * 400f;
                SoundEngine.PlaySound(SoundID.Item104, NPC.Center);
                SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Custom/CodebreakerBeam"), NPC.Center);
                NPC.netUpdate = true;
            }

            // Fade back in.
            if (AttackTimer >= teleportFadeTime && AttackTimer < teleportTime)
                NPC.Opacity = MathHelper.Clamp(NPC.Opacity + 0.15f, 0f, 1f);

            // Fire laser telegraph.
            if (AttackTimer == teleportTime)
            {
                // Play a laser sound.
                if (Main.LocalPlayer.active && !Main.LocalPlayer.dead && NPC.WithinRange(Main.LocalPlayer.Center, 3200f))
                    SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Custom/THanosLaser"), Main.LocalPlayer.Center);

                // Create a bunch of lightning bolts in the sky.
                ExoMechsSky.CreateLightningBolt(12);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    pulseLaserDirection = NPC.AngleTo(Target.Center);

                    int type = ModContent.ProjectileType<PulseBeamTelegraph>();
                    for (int i = 0; i < laserCount; i++)
                    {
                        Vector2 aimDirection = (pulseLaserDirection + MathHelper.TwoPi * i / laserCount).ToRotationVector2();
                        for (int b = 0; b < 9; b++)
                        {
                            int beam = Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, aimDirection, type, 0, 0f, 255, NPC.whoAmI);

                            // Determine the initial offset angle of telegraph. It will be smoothened to give a "stretch" effect.
                            if (Main.projectile.IndexInRange(beam))
                            {
                                float squishedRatio = (float)Math.Pow((float)Math.Sin(MathHelper.Pi * b / 9f), 2D);
                                float smoothenedRatio = MathHelper.SmoothStep(0f, 1f, squishedRatio);
                                Main.projectile[beam].ai[0] = NPC.whoAmI;
                                Main.projectile[beam].ai[1] = MathHelper.Lerp(-0.74f, 0.74f, smoothenedRatio);
                            }
                        }
                        int beam2 = Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, aimDirection, type, 0, 0f, 255, NPC.whoAmI);
                        if (Main.projectile.IndexInRange(beam2))
                            Main.projectile[beam2].ai[0] = NPC.whoAmI;
                    }
                }
            }

            // Fire the laserbeam.
            if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer == teleportTime + telegraphTime)
            {
                for (int i = 0; i < laserCount; i++)
                {
                    Vector2 laserbeamDirection = (pulseLaserDirection + MathHelper.TwoPi * i / laserCount).ToRotationVector2();
                    int laserbeam = Utilities.NewProjectileBetter(NPC.Center, laserbeamDirection, ModContent.ProjectileType<PulseBeamStart>(), 950, 0f);
                    if (Main.projectile.IndexInRange(laserbeam))
                        Main.projectile[laserbeam].ai[1] = NPC.whoAmI;
                }
            }
            
            // Slowly fly around with the laser and release smaller telegraphed lasers.
            if (AttackTimer >= teleportTime + telegraphTime && AttackTimer < teleportTime + telegraphTime + laserShootTime)
            {
                // Open turrets.
                TurretFrameState = AthenaTurretFrameType.OpenAllTurrets;

                Vector2 hoverDestination = Target.Center + new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 425f, -200f);
                if (!NPC.WithinRange(hoverDestination, 60f))
                    NPC.SimpleFlyMovement(NPC.SafeDirectionTo(hoverDestination) * 6f, 0.1f);

                // Release pulse lasers.
                if (AttackTimer % pulseLaserReleaseRate == pulseLaserReleaseRate - 1f)
                {
                    SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, $"Sounds/Item/LaserCannon"), NPC.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        // Calculate the turret index.
                        // If it lands on a big turret, use the next one.
                        int turretIndex = (int)(pulseLaserShootCounter % TurretOffsets.Length);
                        if (!Turrets[turretIndex].IsSmall)
                            turretIndex++;

                        int type = ModContent.ProjectileType<PulseLaser>();
                        Vector2 projectileDestination = Target.Center + Target.velocity * predictionFactor;
                        int laser = Utilities.NewProjectileBetter(NPC.Center, NPC.SafeDirectionTo(projectileDestination) * 8f, type, 500, 0f, Main.myPlayer);
                        if (Main.projectile.IndexInRange(laser))
                        {
                            Main.projectile[laser].owner = NPC.target;
                            Main.projectile[laser].ModProjectile<PulseLaser>().InitialDestination = projectileDestination;
                            Main.projectile[laser].ModProjectile<PulseLaser>().TurretOffsetIndex = turretIndex;
                            Main.projectile[laser].ai[1] = NPC.whoAmI;
                            Main.projectile[laser].netUpdate = true;
                        }

                        pulseLaserShootCounter++;
                        NPC.netUpdate = true;
                    }
                }
            }

            if (AttackTimer >= teleportTime + telegraphTime + laserShootTime + 45f)
                SelectNextAttack();
        }

        public void DoBehavior_DashingIllusions()
        {
            int attackDelay = 35;
            int chargeDelay = 75;
            int chargeTime = 36;
            int illusionCount = 7;
            int chargeCount = 8;
            float chargeSpeed = 39f;
            float predictivenessFactor = 0f;
            ref float chargeCounter = ref NPC.Infernum().ExtraAI[0];

            // Always have all turrets open.
            // This allows the player to distinguish between the real and fake versions of the boss.
            TurretFrameState = AthenaTurretFrameType.OpenMainTurret;

            // Do teleportation effects.
            if (AttackTimer <= attackDelay)
            {
                NPC.Opacity = Utils.GetLerpValue(attackDelay - 1f, 0f, AttackTimer, true);
                if (AttackTimer == attackDelay)
                {
                    NPC.Center = Target.Center - NPC.SafeDirectionTo(Target.Center, -Vector2.UnitY) * 425f;
                    NPC.netUpdate = true;
                    NPC.Opacity = 1f;
                }
            }

            // Summon the illusions.
            if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer == attackDelay + 1f)
            {
                for (int i = 1; i < 1 + illusionCount; i++)
                {
                    int illusion = NPC.NewNPC(new InfernumSource(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<AthenaIllusion>());
                    Main.npc[illusion].ai[1] = MathHelper.TwoPi * i / (float)(illusionCount + 1f);
                }
            }

            // Charge at the target and release tesla orbs.
            if (AttackTimer == attackDelay + chargeDelay)
            {
                SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Item/LargeWeaponFire"), NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 shootVelocity = NPC.SafeDirectionTo(Target.Center) * 8f;
                    Utilities.NewProjectileBetter(NPC.Center, shootVelocity, ModContent.ProjectileType<AresTeslaOrb>(), 500, 0f);
                }

                NPC.velocity = NPC.SafeDirectionTo(Target.Center + Target.velocity * predictivenessFactor) * chargeSpeed;
                NPC.netUpdate = true;
            }

            // Slow down after the charge should concluded and fade away.
            if (AttackTimer > attackDelay + chargeDelay + chargeTime)
            {
                NPC.velocity *= 0.97f;
                NPC.Opacity = MathHelper.Clamp(NPC.Opacity - 0.15f, 0f, 1f);

                // Charge again once completely faded away.
                if (NPC.Opacity <= 0f)
                {
                    chargeCounter++;

                    if (chargeCounter >= chargeCount)
                        SelectNextAttack();
                    else
                        AttackTimer = attackDelay + chargeDelay - 4f;
                    NPC.Opacity = 1f;
                    NPC.netUpdate = true;
                }
            }
        }

        public void DoBehavior_ElectricCharge()
        {
            int waitTime = 8;
            int chargeTime = 45;
            int totalCharges = 6;
            int sparkCount = 36;
            float chargeSpeed = 42.5f;
            float predictivenessFactor = 6f;
            Vector2 hoverDestination = Target.Center + new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 540f, -300f);

            ref float attackSubstate = ref NPC.Infernum().ExtraAI[0];
            ref float attackDelay = ref NPC.Infernum().ExtraAI[1];
            ref float chargeCounter = ref NPC.Infernum().ExtraAI[2];

            switch ((int)attackSubstate)
            {
                // Hover into position.
                case 0:
                    NPC.damage = 0;

                    // Hover to the top left/right of the target.
                    ExoMechAIUtilities.DoSnapHoverMovement(NPC, hoverDestination, 50f, 92f);

                    // Once sufficiently close, go to the next attack substate.
                    if (NPC.WithinRange(hoverDestination, 50f))
                    {
                        NPC.velocity = Vector2.Zero;
                        attackSubstate = 1f;
                        AttackTimer = 0f;
                        NPC.netUpdate = true;
                    }
                    break;

                // Wait in place for a short period of time.
                case 1:
                    // Charge and release sparks.
                    if (AttackTimer >= waitTime && attackDelay >= 45f)
                    {
                        // Create lightning bolts in the sky.
                        int lightningBoltCount = ExoMechManagement.CurrentTwinsPhase >= 6 ? 55 : 30;
                        if (Main.netMode != NetmodeID.Server)
                            ExoMechsSky.CreateLightningBolt(lightningBoltCount, true);

                        SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Item/ELRFire"), NPC.Center);

                        NPC.velocity = NPC.SafeDirectionTo(Target.Center + Target.velocity * predictivenessFactor) * chargeSpeed;

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            float offsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                            for (int i = 0; i < sparkCount; i++)
                            {
                                Vector2 sparkShootVelocity = (MathHelper.TwoPi * i / sparkCount + offsetAngle).ToRotationVector2() * 16f;
                                Utilities.NewProjectileBetter(NPC.Center + sparkShootVelocity * 10f, sparkShootVelocity, ModContent.ProjectileType<TeslaSpark>(), 530, 0f);
                            }
                        }

                        attackSubstate = 2f;
                        AttackTimer = 0f;
                        NPC.netUpdate = true;
                    }
                    break;

                // Release fire.
                case 2:
                    NPC.damage = NPC.defDamage;

                    if (AttackTimer >= chargeTime)
                    {
                        AttackTimer = 0f;
                        attackSubstate = 0f;
                        chargeCounter++;
                        NPC.netUpdate = true;

                        if (chargeCounter >= totalCharges)
                            SelectNextAttack();
                    }
                    break;
            }
            attackDelay++;
        }

        public void DoBehavior_IllusionRocketCharge()
        {
            int chargeCount = 6;
            int redirectTime = 25;
            int chargeTime = 36;
            int attackTransitionDelay = 8;
            int rocketReleaseRate = 8;
            float rocketShootSpeed = 16f;
            float chargeSpeed = 58f;
            float hoverSpeed = 25f;
            ref float chargeDirection = ref NPC.Infernum().ExtraAI[0];
            ref float chargeCounter = ref NPC.Infernum().ExtraAI[1];

            if (chargeCounter == 0f)
                redirectTime += 32;

            // Initialize the charge direction.
            if (AttackTimer == 1f)
            {
                chargeDirection = (Target.Center.X > NPC.Center.X).ToDirectionInt();
                NPC.netUpdate = true;
            }

            // Hover into position before charging.
            if (AttackTimer <= redirectTime)
            {
                Vector2 hoverDestination = Target.Center + Vector2.UnitX * chargeDirection * -420f;
                NPC.Center = NPC.Center.MoveTowards(hoverDestination, 12.5f);
                NPC.SimpleFlyMovement(NPC.SafeDirectionTo(hoverDestination) * hoverSpeed, hoverSpeed * 0.16f);
                if (AttackTimer == redirectTime)
                    NPC.velocity *= 0.3f;
            }
            else if (AttackTimer <= redirectTime + chargeTime)
            {
                // Charge and release rockets.
                NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.UnitX * chargeDirection * chargeSpeed, 0.15f);
                if (AttackTimer == redirectTime + chargeTime)
                    NPC.velocity *= 0.7f;

                // Fire rockets. This does not happen if close to the target.
                if (AttackTimer % rocketReleaseRate == rocketReleaseRate - 1f && !NPC.WithinRange(Target.Center, 350f))
                {
                    SoundEngine.PlaySound(SoundID.Item36, NPC.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 rocketShootVelocity = (Target.Center - MainTurretCenter).SafeNormalize(-Vector2.UnitY) * rocketShootSpeed;
                        Utilities.NewProjectileBetter(MainTurretCenter, rocketShootVelocity, ModContent.ProjectileType<AthenaRocket>(), 500, 0f);
                    }
                }

                // Do damage and become temporarily invulnerable. This is done to prevent dash-cheese.
                NPC.damage = NPC.defDamage;
                NPC.dontTakeDamage = true;
            }
            else
                NPC.velocity *= 0.92f;

            if (AttackTimer >= redirectTime + chargeTime + attackTransitionDelay)
            {
                AttackTimer = 0f;
                chargeCounter++;
                if (chargeCounter >= chargeCount)
                    SelectNextAttack();
                NPC.netUpdate = true;
            }
        }

        public void SelectNextAttack()
        {
            for (int i = 0; i < 5; i++)
                NPC.Infernum().ExtraAI[i] = 0f;

            switch (AttackState)
            {
                case AthenaAttackType.CircleOfLightning:
                    AttackState = AthenaAttackType.ExowlHologramSwarm;
                    break;
                case AthenaAttackType.ExowlHologramSwarm:
                    AttackState = AthenaAttackType.AimedPulseLasers;
                    break;
                case AthenaAttackType.AimedPulseLasers:
                    AttackState = AthenaAttackType.DashingIllusions;
                    break;
                case AthenaAttackType.DashingIllusions:
                    AttackState = AthenaAttackType.ElectricCharge;
                    break;
                case AthenaAttackType.ElectricCharge:
                    AttackState = AthenaAttackType.IllusionRocketCharge;
                    break;
                case AthenaAttackType.IllusionRocketCharge:
                    AttackState = AthenaAttackType.CircleOfLightning;
                    break;
            }

            if (ExoMechComboAttackContent.ShouldSelectComboAttack(NPC, out ExoMechComboAttackContent.ExoMechComboAttackType newAttack))
                NPC.ai[0] = (int)newAttack;

            AttackTimer = 0f;
            NPC.netUpdate = true;
        }

        #endregion AI and Behaviors

        #region Frames and Drawcode

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter++;
            for (int i = 0; i < Turrets.Length; i++)
            {
                int frame = Turrets[i].Frame;
                int maxFrame = Turrets[i].IsSmall ? 3 : 4;

                switch (TurretFrameState)
                {
                    case AthenaTurretFrameType.Blinking:
                        float frameInterpolant = (float)Math.Sin(NPC.frameCounter * 0.13f + i * 1.02f) * 0.5f + 0.5f;
                        frame = (int)(frameInterpolant * maxFrame * 0.99f);
                        break;
                    case AthenaTurretFrameType.OpenMainTurret:
                        if (NPC.frameCounter % 6 == 5)
                        {
                            if (Turrets[i].IsSmall)
                                frame--;
                            else
                                frame++;
                        }
                        break;
                    case AthenaTurretFrameType.CloseAllTurrets:
                        if (NPC.frameCounter % 6 == 5)
                            frame--;
                        break;
                    case AthenaTurretFrameType.OpenAllTurrets:
                        if (NPC.frameCounter % 6 == 5)
                            frame++;
                        break;
                }

                Turrets[i].Frame = frame;
            }
        }

        public float FlameTrailPulse => (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f + NPC.whoAmI * 111.5856f) * 0.5f + 0.5f;

        // Update these in the illusion NPC's file if this needs changing for some reason.
        // Static methods doesn't easily work in this context, unfortunately.
        public float FlameTrailWidthFunction(float completionRatio)
        {
            float maxWidth = MathHelper.Lerp(15f, 80f, FlameTrailPulse);
            return MathHelper.SmoothStep(maxWidth, 8f, completionRatio) * NPC.Opacity;
        }

        public Color FlameTrailColorFunction(float completionRatio)
        {
            float trailOpacity = Utils.GetLerpValue(0.8f, 0.27f, completionRatio, true) * Utils.GetLerpValue(0f, 0.067f, completionRatio, true);
            trailOpacity *= MathHelper.Lerp(1f, 0.27f, 1f - FlameTrailPulse) * NPC.Opacity;
            Color startingColor = Color.Lerp(Color.White, Color.Cyan, 0.27f);
            Color middleColor = Color.Lerp(Color.Orange, Color.Blue, 0.74f);
            Color endColor = Color.DarkCyan;
            Color color = CalamityUtils.MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * trailOpacity;
            color.A /= 8;
            return color * NPC.Opacity;
        }

        public float RayWidthFunction(float completionRatio)
        {
            float widthOffset = (float)Math.Cos(completionRatio * 73f - Main.GlobalTimeWrappedHourly * 8f) * 
                Utils.GetLerpValue(0f, 0.1f, completionRatio, true) * 
                Utils.GetLerpValue(1f, 0.9f, completionRatio, true);
            return MathHelper.Lerp(2f, NPC.Infernum().ExtraAI[2] * 0.7f, completionRatio) + widthOffset;
        }

        public static Color RayColorFunction(float completionRatio)
        {
            return Color.Cyan * Utils.GetLerpValue(0.8f, 0.5f, completionRatio, true) * 0.6f;
        }

        public void DrawLightRay(float initialRayRotation, float rayBrightness, Vector2 rayStartingPoint)
        {
            if (LightRayDrawer is null)
                LightRayDrawer = new PrimitiveTrail(RayWidthFunction, RayColorFunction, PrimitiveTrail.RigidPointRetreivalFunction);
            Vector2 currentRayDirection = initialRayRotation.ToRotationVector2();

            float length = rayBrightness * NPC.Infernum().ExtraAI[4] * 400f;
            List<Vector2> points = new();
            for (int i = 0; i <= 12; i++)
                points.Add(Vector2.Lerp(rayStartingPoint, rayStartingPoint + initialRayRotation.ToRotationVector2() * length, i / 12f));

            LightRayDrawer.Draw(points, -Main.screenPosition, 47);
        }

        public static void DrawExowlHologram(Vector2 drawPosition, int exowlFrame, float hologramInterpolant)
        {
            float hologramOpacity = (float)Math.Pow(hologramInterpolant, 0.45);
            Texture2D exowlTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Draedon/Athena/Exowl").Value;
            Rectangle frame = exowlTexture.Frame(1, 3, 0, exowlFrame);

            DrawData fuckYou = new(exowlTexture, drawPosition, frame, Color.White * hologramOpacity, 0f, frame.Size() * 0.5f, 1f, 0, 0);
            GameShaders.Misc["Infernum:Hologram"].UseOpacity(hologramInterpolant);
            GameShaders.Misc["Infernum:Hologram"].UseColor(Color.Cyan);
            GameShaders.Misc["Infernum:Hologram"].UseSecondaryColor(Color.Gold);
            GameShaders.Misc["Infernum:Hologram"].SetShaderTexture(ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/HologramTexture"));
            GameShaders.Misc["Infernum:Hologram"].Apply(fuckYou);
            fuckYou.Draw(Main.spriteBatch);
        }

        public Vector2 GetHologramPosition(int index, float illusionCount, float hologramSpan, float hologramInterpolant)
        {
            float completionRatio = index / (illusionCount - 1f);
            float hologramHorizontalOffset = MathHelper.Lerp(-0.5f, 0.5f, completionRatio) * hologramSpan;
            float hologramVerticalOffset = Utils.GetLerpValue(0f, 0.5f, hologramInterpolant, true) * 200f + CalamityUtils.Convert01To010(completionRatio) * 40f;
            return NPC.Top + new Vector2(hologramHorizontalOffset, -hologramVerticalOffset);
        }

        public static void DrawBaseNPC(NPC npc, Vector2 screenPos, Color drawColor, float flameTrailPulse, PrimitiveTrail flameTrail)
        {
            // Drift towards a brighter color.
            drawColor = Color.Lerp(drawColor, Color.White, 0.45f);

            // Prepare the flame trail shader with its map texture.
            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/ScarletDevilStreak"));

            // Draw a flame trail on the thrusters.
            for (int direction = -1; direction <= 1; direction++)
            {
                Vector2 baseDrawOffset = new Vector2(0f, direction == 0f ? -6f : -14f).RotatedBy(npc.rotation);
                baseDrawOffset += new Vector2(direction * 64f, 0f).RotatedBy(npc.rotation);

                float backFlameLength = direction == 0f ? 340f : 250f;
                backFlameLength *= MathHelper.Lerp(0.7f, 1f, 1f - flameTrailPulse);

                Vector2 drawStart = npc.Center + baseDrawOffset;
                Vector2 drawEnd = drawStart - (npc.rotation - MathHelper.PiOver2).ToRotationVector2() * backFlameLength;
                Vector2[] drawPositions = new Vector2[]
                {
                    drawStart,
                    drawEnd
                };

                for (int i = 0; i < 6; i++)
                {
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 6f;
                    flameTrail.Draw(drawPositions, drawOffset - screenPos, 70);
                }
            }

            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Texture2D glowmask = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Draedon/Athena/AthenaNPC_Glowmask").Value;
            Vector2 drawPosition = npc.Center - screenPos;
            Vector2 origin = npc.frame.Size() * 0.5f;
            Main.spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(drawColor), npc.rotation, origin, npc.scale, 0, 0f);
            Main.spriteBatch.Draw(glowmask, drawPosition, npc.frame, npc.GetAlpha(Color.White), npc.rotation, origin, npc.scale, 0, 0f);
        }

        public Vector2 GetTurretPosition(int i) => NPC.Center + TurretOffsets[i].RotatedBy(NPC.rotation) + Vector2.UnitY * 46f;

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Declare the trail drawers if they have yet to be defined.
            if (FlameTrail is null)
                FlameTrail = new PrimitiveTrail(FlameTrailWidthFunction, FlameTrailColorFunction, null, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);

            if (AttackState == AthenaAttackType.IllusionRocketCharge || (int)AttackState == (int)ExoMechComboAttackContent.ExoMechComboAttackType.TwinsAthena_ThermoplasmaDance)
            {
                Texture2D texture = TextureAssets.Npc[NPC.type].Value;
                for (int i = -3; i <= 8; i++)
                {
                    if (i == 0)
                        continue;

                    Color duplicateColor = Color.White;
                    Vector2 baseDrawPosition = NPC.Center - screenPos;
                    Vector2 drawPosition = baseDrawPosition;

                    // Create lagging afterimages.
                    if (i > 3)
                    {
                        float lagBehindFactor = Utils.GetLerpValue(30f, 70f, AttackTimer, true);
                        if (lagBehindFactor == 0f)
                            continue;

                        drawPosition = baseDrawPosition + NPC.velocity * -3f * (i - 4f) * lagBehindFactor;
                        duplicateColor *= 1f - (i - 3f) / 4f;
                    }

                    // Create cool afterimages while charging at the target.
                    else
                    {
                        float hue = (i + 5f) / 10f;
                        float drawOffsetFactor = 60f;
                        Vector3 offsetInformation = Vector3.Transform(Vector3.Forward,
                            Matrix.CreateRotationX((Main.GlobalTimeWrappedHourly - 0.3f + i * 0.1f) * 0.7f * MathHelper.TwoPi) *
                            Matrix.CreateRotationY((Main.GlobalTimeWrappedHourly - 0.8f + i * 0.3f) * 0.7f * MathHelper.TwoPi) *
                            Matrix.CreateRotationZ((Main.GlobalTimeWrappedHourly + i * 0.5f) * 0.1f * MathHelper.TwoPi));
                        drawOffsetFactor += Utils.GetLerpValue(-1f, 1f, offsetInformation.Z, true) * 70f;
                        Vector2 drawOffset = new Vector2(offsetInformation.X, offsetInformation.Y) * drawOffsetFactor;
                        drawOffset = drawOffset.RotatedBy(MathHelper.TwoPi * AttackTimer / 180f);

                        if ((int)AttackState == (int)ExoMechComboAttackContent.ExoMechComboAttackType.TwinsAthena_ThermoplasmaDance)
                            drawOffset *= Utils.GetLerpValue(16f, 25f, NPC.velocity.Length(), true);

                        float luminanceInterpolant = Utils.GetLerpValue(90f, 0f, AttackTimer, true);
                        duplicateColor = Main.hslToRgb(hue, 1f, MathHelper.Lerp(0.5f, 1f, luminanceInterpolant)) * NPC.Opacity * 0.8f;
                        duplicateColor.A /= 3;
                        drawPosition += drawOffset;
                    }

                    // Draw the base texture.
                    spriteBatch.Draw(texture, drawPosition, NPC.frame, duplicateColor, NPC.rotation, NPC.frame.Size() * 0.5f, NPC.scale, 0, 0f);
                }
            }

            DrawBaseNPC(NPC, screenPos, drawColor, FlameTrailPulse, FlameTrail);

            for (int i = 0; i < Turrets.Length; i++)
            {
                if (Turrets[i] is null)
                    break;

                int totalFrames = 4;
                Texture2D turretTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Draedon/Athena/AthenaTurretLarge").Value;
                if (Turrets[i].IsSmall)
                {
                    totalFrames = 3;
                    turretTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Draedon/Athena/AthenaTurretSmall").Value;
                }

                // totalFrames is used instead of totalFrames - 1f in division to allow for some of the original color to still go through.
                // Even as a full crystal, I'd prefer the texture not be completely fullbright.
                Color turretColor = Color.Lerp(drawColor, Color.White, Turrets[i].Frame / (float)totalFrames);
                Rectangle turretFrame = turretTexture.Frame(1, totalFrames, 0, Turrets[i].Frame);
                Vector2 drawPosition = NPC.Center - screenPos;
                Vector2 turretOrigin = turretFrame.Size() * 0.5f;
                Vector2 turretDrawPosition = GetTurretPosition(i) - NPC.Center + drawPosition;
                Main.spriteBatch.Draw(turretTexture, turretDrawPosition, turretFrame, NPC.GetAlpha(turretColor), 0f, turretOrigin, NPC.scale, 0, 0f);
            }

            // Draw a line telegraph as necessary
            if (TelegraphInterpolant > 0f)
            {
                Main.spriteBatch.SetBlendState(BlendState.Additive);

                Texture2D telegraphTexture = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/BloomLine").Value;
                float telegraphScaleFactor = TelegraphInterpolant * 1.2f;

                for (float offsetAngle = 0f; offsetAngle < 0.2f; offsetAngle += 0.03f)
                {
                    Vector2 telegraphStart = MainTurretCenter + (TelegraphRotation + offsetAngle).ToRotationVector2() * 20f - screenPos;
                    Vector2 telegraphOrigin = new Vector2(0.5f, 0f) * telegraphTexture.Size();
                    Vector2 telegraphScale = new(telegraphScaleFactor, 3f);
                    Color telegraphColor = new Color(74, 255, 204) * (float)Math.Pow(TelegraphInterpolant, 0.79) * ((0.2f - offsetAngle) / 0.2f) * 1.6f;
                    Main.spriteBatch.Draw(telegraphTexture, telegraphStart, null, telegraphColor, TelegraphRotation + offsetAngle - MathHelper.PiOver2, telegraphOrigin, telegraphScale, 0, 0f);
                }

                Main.spriteBatch.ResetBlendState();
            }

            // Draw holograms.
            if (AttackState == AthenaAttackType.ExowlHologramSwarm)
            {
                float hologramInterpolant = NPC.Infernum().ExtraAI[0];
                float illusionCount = NPC.Infernum().ExtraAI[1];
                float hologramSpan = NPC.Infernum().ExtraAI[2];

                Main.spriteBatch.EnterShaderRegion();

                float rayBrightness = Utils.GetLerpValue(0f, 0.45f, hologramInterpolant, true);
                DrawLightRay(-MathHelper.PiOver2, rayBrightness, MainTurretCenter);
                Main.spriteBatch.EnterShaderRegion();

                for (int i = 0; i < illusionCount; i++)
                {
                    int illusionFrame = (int)(Main.GlobalTimeWrappedHourly * 6f + i) % 3;
                    float completionRatio = i / (illusionCount - 1f);
                    Vector2 hologramDrawPosition = GetHologramPosition(i, illusionCount, hologramSpan, hologramInterpolant) - screenPos;
                    DrawExowlHologram(hologramDrawPosition, illusionFrame, hologramInterpolant);
                }
                Main.spriteBatch.ExitShaderRegion();
            }

            return false;
        }

        #endregion Frames and Drawcode

        #region Misc Things

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ModContent.ItemType<OmegaHealingPotion>();
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            for (int k = 0; k < 3; k++)
                Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, 107, 0f, 0f, 100, new Color(0, 255, 255), 1f);
        }

        public override bool CheckActive() => false;

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.lifeMax = (int)(NPC.lifeMax * 0.5f * bossLifeScale);
            NPC.damage = (int)(NPC.damage * 0.8f);
        }
        #endregion Misc Things
    }
}
