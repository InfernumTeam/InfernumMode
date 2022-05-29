using CalamityMod;
using CalamityMod.Items.Potions;
using CalamityMod.Items.TreasureBags;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.Particles;
using CalamityMod.Skies;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares;
using InfernumMode.GlobalInstances;
using InfernumMode.Particles;
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

        public Player Target => Main.player[npc.target];

        public AthenaAttackType AttackState
        {
            get => (AthenaAttackType)(int)npc.ai[0];
            set => npc.ai[0] = (int)value;
        }

        public bool HasInitialized
        {
            get => npc.ai[2] == 1f;
            set => npc.ai[2] = value.ToInt();
        }

        public AthenaTurretFrameType TurretFrameState
        {
            get => (AthenaTurretFrameType)(int)npc.localAI[0];
            set => npc.localAI[0] = (int)value;
        }

        public bool HasSummonedComplementMech
        {
            get => npc.Infernum().ExtraAI[ExoMechManagement.HasSummonedComplementMechIndex] == 1f;
            set => npc.Infernum().ExtraAI[ExoMechManagement.HasSummonedComplementMechIndex] = value.ToInt();
        }

        public bool WasInitialSummon
        {
            get => npc.Infernum().ExtraAI[ExoMechManagement.WasNotInitialSummonIndex] == 0f;
            set => npc.Infernum().ExtraAI[ExoMechManagement.WasNotInitialSummonIndex] = (!value).ToInt();
        }

        public ref float AttackTimer => ref npc.ai[1];

        public ref float MinionRedCrystalGlow => ref npc.localAI[1];

        public ref float TelegraphInterpolant => ref npc.localAI[2];

        public ref float TelegraphRotation => ref npc.localAI[3];

        public ref float ComplementMechIndex => ref npc.Infernum().ExtraAI[ExoMechManagement.ComplementMechIndexIndex];

        public ref float FinalMechIndex => ref npc.Infernum().ExtraAI[ExoMechManagement.FinalMechIndexIndex];

        public ref float FinalPhaseAnimationTime => ref npc.Infernum().ExtraAI[ExoMechManagement.FinalPhaseTimerIndex];

        public ref float DeathAnimationTimer => ref npc.Infernum().ExtraAI[ExoMechManagement.DeathAnimationTimerIndex];

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
            NPCID.Sets.TrailingMode[npc.type] = 3;
            NPCID.Sets.TrailCacheLength[npc.type] = 10;
        }

        public override void SetDefaults()
        {
            AthenaSetDefaults(npc);
            npc.boss = true;
            npc.modNPC.bossBag = ModContent.ItemType<DraedonTreasureBag>();
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
            npc.modNPC.aiType = -1;
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
            writer.Write(npc.Opacity);
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
            npc.Opacity = reader.ReadSingle();
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
            bool performingDeathAnimation = ExoMechAIUtilities.PerformingDeathAnimation(npc);
            float lifeRatio = npc.life / (float)npc.lifeMax;
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
                npc.netUpdate = true;
            }

            // Summon the complement mech and reset things once ready.
            if (!HasSummonedComplementMech && lifeRatio < ExoMechManagement.Phase4LifeRatio)
            {
                Exowl.MakeAllExowlsExplode();
                ExoMechManagement.SummonComplementMech(npc);
                HasSummonedComplementMech = true;
                AttackTimer = 0f;
                SelectNextAttack();
                npc.netUpdate = true;
            }

            // Summon the final mech once ready.
            if (WasInitialSummon && FinalMechIndex == -1f && complementMech != null && complementMech.life / (float)complementMech?.lifeMax < ExoMechManagement.ComplementMechInvincibilityThreshold)
            {
                Exowl.MakeAllExowlsExplode();
                ExoMechManagement.SummonFinalMech(npc);
                npc.netUpdate = true;
            }

            // Search for a target.
            npc.TargetClosestIfTargetIsInvalid();

            // Set the global whoAmI index.
            GlobalNPCOverrides.Athena = npc.whoAmI;

            // Become invincible if the complement mech is at high enough health or if in the middle of a death animation.
            npc.dontTakeDamage = performingDeathAnimation;
            if (ComplementMechIndex >= 0 && Main.npc[(int)ComplementMechIndex].active && Main.npc[(int)ComplementMechIndex].life > Main.npc[(int)ComplementMechIndex].lifeMax * ExoMechManagement.ComplementMechInvincibilityThreshold)
                npc.dontTakeDamage = true;

            // Become invincible and disappear if necessary.
            if (ExoMechAIUtilities.ShouldExoMechVanish(npc))
            {
                npc.damage = 0;
                npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.08f, 0f, 1f);
                if (npc.Opacity <= 0f)
                    npc.Center = Target.Center - Vector2.UnitY * 1600f;

                AttackTimer = 0f;
                AttackState = AthenaAttackType.AimedPulseLasers;
                npc.Calamity().ShouldCloseHPBar = true;
                npc.dontTakeDamage = true;
            }
            else
                npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.08f, 0f, 1f);

            // Reset things.
            npc.damage = npc.defDamage;
            MinionRedCrystalGlow = 0f;
            TelegraphInterpolant = 0f;
            TurretFrameState = AthenaTurretFrameType.CloseAllTurrets;

            // Despawn if the target is gone.
            if (!Target.active || Target.dead)
            {
                npc.TargetClosest(false);
                if (!Target.active || Target.dead)
                    npc.active = false;
            }

            // Handle the final phase transition.
            if (FinalPhaseAnimationTime < ExoMechManagement.FinalPhaseTransitionTime && ExoMechManagement.CurrentAthenaPhase >= 6 && !ExoMechManagement.ExoMechIsPerformingDeathAnimation)
            {
                AttackState = AthenaAttackType.AimedPulseLasers;
                FinalPhaseAnimationTime++;
                npc.dontTakeDamage = true;
                DoBehavior_DoFinalPhaseTransition();
                return;
            }

            // Use combo attacks as necessary.
            if (ExoMechManagement.TotalMechs >= 2 && (int)AttackState < 100)
            {
                AttackTimer = 0f;

                if (initialMech.whoAmI == npc.whoAmI)
                    SelectNextAttack();

                AttackState = (AthenaAttackType)(int)initialMech.ai[0];
                npc.netUpdate = true;
            }

            // Reset the attack type if it was a combo attack but the respective mech is no longer present.
            if (((finalMech != null && finalMech.Opacity > 0f) || ExoMechManagement.CurrentAresPhase >= 6) && (int)AttackState >= 100f)
            {
                AttackTimer = 0f;
                AttackState = AthenaAttackType.AimedPulseLasers;
                npc.netUpdate = true;
            }

            // Handle attacks.
            if (!performingDeathAnimation)
            {
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
            }
            else
                DoBehavior_DeathAnimation();

            if (ExoMechComboAttackContent.UseTwinsAthenaComboAttack(npc, 1f, ref AttackTimer, ref npc.localAI[0]))
                SelectNextAttack();

            AttackTimer++;
        }

        public void DoBehavior_DoFinalPhaseTransition()
        {
            npc.velocity *= 0.925f;
            npc.rotation = 0f;

            // Determine frames.
            TurretFrameState = AthenaTurretFrameType.Blinking;

            // Play the transition sound at the start.
            if (FinalPhaseAnimationTime == 3f)
                Main.PlaySound(InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ExoMechFinalPhaseChargeup"), Target.Center);

            // Clear away all lasers and laser telegraphs.
            if (FinalPhaseAnimationTime == 3f)
            {
                // Destroy all lasers and telegraphs.
                Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<PulseBeamStart>(), ModContent.ProjectileType<PulseBeamTelegraph>());
            }
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
            float totalArcFactor = 1.56f;
            ref float aimDirection = ref npc.Infernum().ExtraAI[0];

            if (ExoMechManagement.CurrentAthenaPhase >= 2)
                totalArcFactor += 0.06f;
            if (ExoMechManagement.CurrentAthenaPhase >= 3)
                totalArcFactor += 0.06f;
            if (ExoMechManagement.CurrentAthenaPhase >= 5)
            {
                totalArcFactor += 0.12f;
                lightningShootRate++;
            }
            if (ExoMechManagement.CurrentAthenaPhase >= 6)
                totalArcFactor += 0.15f;

            TurretFrameState = AthenaTurretFrameType.CloseAllTurrets;
            if (AttackTimer >= teleportTime + circleSummonDelay)
                TurretFrameState = AthenaTurretFrameType.Blinking;
            if (AttackTimer >= teleportTime + circleSummonDelay + telegraphTime)
                TurretFrameState = AthenaTurretFrameType.OpenMainTurret;

            // Fade out.
            if (AttackTimer < teleportFadeTime)
            {
                npc.velocity *= 0.5f;
                npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.15f, 0f, 1f);
            }

            // Teleport.
            if (AttackTimer == teleportFadeTime)
            {
                Main.PlaySound(SoundID.Item103, npc.Center);
                npc.velocity = Vector2.Zero;
                npc.Center = Target.Center + (MathHelper.TwoPi * Main.rand.Next(4) / 4f).ToRotationVector2() * 450f;
                while (Collision.SolidCollision(npc.position - Vector2.One * 200f, npc.width + 400, npc.height + 400))
                    npc.position.Y -= 120f;

                Main.PlaySound(SoundID.Item104, npc.Center);
                npc.netUpdate = true;
            }

            // Fade back in.
            if (AttackTimer >= teleportFadeTime && AttackTimer < teleportTime)
                npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.15f, 0f, 1f);

            // Summon a circle of minions that spin in place and act as a barrier.
            // The player can technically teleport out of the circle, but doing so prevents seeing the boss.
            if (AttackTimer == teleportTime + circleSummonDelay)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ThunderStrike"), npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<ExowlCircleSummonBoom>(), 0, 0f);

                    List<int> circle = new List<int>();
                    for (int i = 0; i < 15; i++)
                    {
                        int exowl = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<Exowl>(), npc.whoAmI);
                        if (Main.npc.IndexInRange(exowl))
                        {
                            Main.npc[exowl].ModNPC<Exowl>().CircleCenter = npc.Center;
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
                npc.netUpdate = true;
            }

            // Determine telegraph variables.
            if (AttackTimer >= teleportTime + circleSummonDelay && AttackTimer < teleportTime + circleSummonDelay + telegraphTime)
                TelegraphRotation = TelegraphRotation.AngleLerp(npc.AngleTo(Target.Center + Target.velocity * 15f), 0.125f);

            if (AttackTimer < teleportTime + circleSummonDelay + teleportTime + shootDelay)
                TelegraphInterpolant = Utils.InverseLerp(0f, telegraphTime + shootDelay, AttackTimer - (teleportTime + circleSummonDelay), true);

            // Release the lightning.
            else if (AttackTimer % lightningShootRate == 0f)
            {
                if (aimDirection == 0f)
                    aimDirection = (MathHelper.WrapAngle(npc.AngleTo(Target.Center) - TelegraphRotation) > 0f).ToDirectionInt();

                if (AttackTimer % (lightningShootRate * 2f) == 0f)
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/TeslaCannonFire"), MainTurretCenter);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 lightningShootVelocity = TelegraphRotation.ToRotationVector2() * 8.4f;
                    int lightning = Utilities.NewProjectileBetter(MainTurretCenter - lightningShootVelocity * 12f, lightningShootVelocity, ModContent.ProjectileType<TerateslaLightningBlast>(), 530, 0f);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Main.projectile[lightning].ai[0] = TelegraphRotation;
                        Main.projectile[lightning].ai[1] = Main.rand.Next(100);
                    }

                    TelegraphRotation += MathHelper.TwoPi / lightningShootTime * lightningShootRate * aimDirection * totalArcFactor;
                    npc.netUpdate = true;
                }
            }

            MinionRedCrystalGlow = Utils.InverseLerp(0f, 60f, AttackTimer - (teleportTime + circleSummonDelay), true);

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
            int intendedIllusionCount = 13;
            ref float hologramInterpolant = ref npc.Infernum().ExtraAI[0];
            ref float illusionCount = ref npc.Infernum().ExtraAI[1];
            ref float hologramSpan = ref npc.Infernum().ExtraAI[2];
            ref float exowlIllusionFadeInterpolant = ref npc.Infernum().ExtraAI[3];
            ref float hologramRayDissipation = ref npc.Infernum().ExtraAI[4];

            if (ExoMechManagement.CurrentAthenaPhase >= 2)
            {
                hologramCreationTime -= 25;
                hologramAttackTime += 30;
            }
            if (ExoMechManagement.CurrentAthenaPhase >= 3)
            {
                hologramCreationTime -= 10;
                intendedIllusionCount++;
            }
            if (ExoMechManagement.CurrentAthenaPhase >= 5)
            {
                hologramCreationTime -= 24;
                hologramAttackTime += 30;
                intendedIllusionCount += 6;
            }
            if (ExoMechManagement.CurrentAthenaPhase >= 6)
            {
                hologramCreationTime -= 24;
                hologramAttackTime += 45;
                intendedIllusionCount += 4;
            }

            // Initialize the minion illusion count.
            if (illusionCount == 0f)
            {
                illusionCount = intendedIllusionCount;
                npc.netUpdate = true;
            }

            // Fade out.
            if (AttackTimer < teleportFadeTime)
            {
                npc.velocity *= 0.5f;
                npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.15f, 0f, 1f);
            }

            // Teleport.
            if (AttackTimer == teleportFadeTime)
            {
                Main.PlaySound(SoundID.Item103, npc.Center);
                npc.velocity = Vector2.Zero;
                npc.Center = Target.Center + new Vector2(Main.rand.NextBool().ToDirectionInt() * 510f, -240f);
                while (Collision.SolidCollision(npc.position - Vector2.One * 200f, npc.width + 400, npc.height + 400))
                    npc.position.Y -= 120f;

                Main.PlaySound(SoundID.Item104, npc.Center);
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/CodebreakerBeam"), npc.Center);
                npc.netUpdate = true;
            }

            // Fade back in.
            if (AttackTimer >= teleportFadeTime && AttackTimer < teleportTime)
                npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.15f, 0f, 1f);

            // Calculate the hologram interpolant, span, and fade interpolant.
            hologramInterpolant = Utils.InverseLerp(0f, 36f, AttackTimer - teleportTime, true);
            hologramRayDissipation = Utils.InverseLerp(hologramCreationTime, hologramCreationTime - 12f, AttackTimer - teleportTime, true);
            hologramSpan = MathHelper.Lerp(8f, 450f, (float)Math.Pow(hologramInterpolant, 1.73) * hologramRayDissipation);
            exowlIllusionFadeInterpolant = Utils.InverseLerp(0f, holographFadeoutTime, AttackTimer - teleportTime - hologramCreationTime, true);
            if (AttackTimer > teleportTime + hologramCreationTime)
            {
                Vector2 idealVelocity = npc.SafeDirectionTo(Target.Center + (MathHelper.TwoPi * AttackTimer / 150f).ToRotationVector2() * 500f) * 16f;
                npc.SimpleFlyMovement(idealVelocity, idealVelocity.Length() / 20f);
                hologramInterpolant = 0f;
            }

            // Transform the holograms into true exowls.
            if (AttackTimer == teleportTime + hologramCreationTime)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/LargeWeaponFire"), npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int realExowlIndex = Main.rand.Next((int)illusionCount);
                    for (int i = 0; i < illusionCount; i++)
                    {
                        Vector2 hologramPosition = GetHologramPosition(i, illusionCount, hologramSpan, hologramInterpolant);
                        int exowl = NPC.NewNPC((int)hologramPosition.X, (int)hologramPosition.Y, ModContent.NPCType<Exowl>(), npc.whoAmI);
                        if (Main.npc.IndexInRange(exowl))
                        {
                            Main.npc[exowl].ModNPC<Exowl>().UseConfusionEffect = true;
                            Main.npc[exowl].ModNPC<Exowl>().IsIllusion = i != realExowlIndex;
                        }
                    }
                    npc.netUpdate = true;
                }
            }

            if (AttackTimer == teleportTime + hologramCreationTime + hologramAttackTime)
            {
                Exowl.MakeAllExowlsExplode();
                npc.netUpdate = true;
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
            int laserbeamCount = 4;
            float predictionFactor = 16f;
            ref float pulseLaserDirection = ref npc.Infernum().ExtraAI[0];
            ref float pulseLaserShootCounter = ref npc.Infernum().ExtraAI[1];

            if (ExoMechManagement.CurrentAthenaPhase >= 2)
                laserbeamCount++;
            if (ExoMechManagement.CurrentAthenaPhase >= 3)
                laserbeamCount++;
            if (ExoMechManagement.CurrentAthenaPhase >= 5)
            {
                laserbeamCount++;
                pulseLaserReleaseRate--;
            }
            if (ExoMechManagement.CurrentAthenaPhase >= 6)
                laserbeamCount += 2;

            // Fade out.
            if (AttackTimer < teleportFadeTime)
            {
                npc.velocity *= 0.5f;
                npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.15f, 0f, 1f);
            }

            // Teleport.
            if (AttackTimer == teleportFadeTime)
            {
                Main.PlaySound(SoundID.Item103, npc.Center);
                npc.velocity = Vector2.Zero;
                npc.Center = Target.Center - Target.velocity.SafeNormalize(Main.rand.NextVector2Unit()) * 400f;
                Main.PlaySound(SoundID.Item104, npc.Center);
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/CodebreakerBeam"), npc.Center);
                npc.netUpdate = true;
            }

            // Fade back in.
            if (AttackTimer >= teleportFadeTime && AttackTimer < teleportTime)
                npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.15f, 0f, 1f);

            // Fire laser telegraph.
            if (AttackTimer == teleportTime)
            {
                // Play a laser sound.
                if (Main.LocalPlayer.active && !Main.LocalPlayer.dead && npc.WithinRange(Main.LocalPlayer.Center, 3200f))
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/THanosLaser"), Main.LocalPlayer.Center);

                // Create a bunch of lightning bolts in the sky.
                ExoMechsSky.CreateLightningBolt(12);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    pulseLaserDirection = npc.AngleTo(Target.Center);

                    int type = ModContent.ProjectileType<PulseBeamTelegraph>();
                    for (int i = 0; i < laserbeamCount; i++)
                    {
                        Vector2 aimDirection = (pulseLaserDirection + MathHelper.TwoPi * i / laserbeamCount).ToRotationVector2();
                        for (int b = 0; b < 9; b++)
                        {
                            int beam = Projectile.NewProjectile(npc.Center, aimDirection, type, 0, 0f, 255, npc.whoAmI);

                            // Determine the initial offset angle of telegraph. It will be smoothened to give a "stretch" effect.
                            if (Main.projectile.IndexInRange(beam))
                            {
                                float squishedRatio = (float)Math.Pow((float)Math.Sin(MathHelper.Pi * b / 9f), 2D);
                                float smoothenedRatio = MathHelper.SmoothStep(0f, 1f, squishedRatio);
                                Main.projectile[beam].ai[0] = npc.whoAmI;
                                Main.projectile[beam].ai[1] = MathHelper.Lerp(-0.74f, 0.74f, smoothenedRatio);
                            }
                        }
                        int beam2 = Projectile.NewProjectile(npc.Center, aimDirection, type, 0, 0f, 255, npc.whoAmI);
                        if (Main.projectile.IndexInRange(beam2))
                            Main.projectile[beam2].ai[0] = npc.whoAmI;
                    }
                }
            }

            // Fire the laserbeams.
            if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer == teleportTime + telegraphTime)
            {
                for (int i = 0; i < laserbeamCount; i++)
                {
                    Vector2 laserbeamDirection = (pulseLaserDirection + MathHelper.TwoPi * i / laserbeamCount).ToRotationVector2();
                    int laserbeam = Utilities.NewProjectileBetter(npc.Center, laserbeamDirection, ModContent.ProjectileType<PulseBeamStart>(), 950, 0f);
                    if (Main.projectile.IndexInRange(laserbeam))
                        Main.projectile[laserbeam].ai[1] = npc.whoAmI;
                }
            }

            // Slowly fly around with the laser and release smaller telegraphed lasers.
            if (AttackTimer >= teleportTime + telegraphTime && AttackTimer < teleportTime + telegraphTime + laserShootTime)
            {
                // Open turrets.
                TurretFrameState = AthenaTurretFrameType.OpenAllTurrets;

                Vector2 hoverDestination = Target.Center + new Vector2((Target.Center.X < npc.Center.X).ToDirectionInt() * 425f, -200f);
                if (!npc.WithinRange(hoverDestination, 60f))
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 6f, 0.1f);

                // Release pulse lasers.
                if (AttackTimer % pulseLaserReleaseRate == pulseLaserReleaseRate - 1f)
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/LaserCannon"), npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        // Calculate the turret index.
                        // If it lands on a big turret, use the next one.
                        int turretIndex = (int)(pulseLaserShootCounter % TurretOffsets.Length);
                        if (!Turrets[turretIndex].IsSmall)
                            turretIndex++;

                        int type = ModContent.ProjectileType<PulseLaser>();
                        Vector2 projectileDestination = Target.Center + Target.velocity * predictionFactor;
                        int laser = Utilities.NewProjectileBetter(npc.Center, npc.SafeDirectionTo(projectileDestination) * 8f, type, 500, 0f, Main.myPlayer);
                        if (Main.projectile.IndexInRange(laser))
                        {
                            Main.projectile[laser].owner = npc.target;
                            Main.projectile[laser].ModProjectile<PulseLaser>().InitialDestination = projectileDestination;
                            Main.projectile[laser].ModProjectile<PulseLaser>().TurretOffsetIndex = turretIndex;
                            Main.projectile[laser].ai[1] = npc.whoAmI;
                            Main.projectile[laser].netUpdate = true;
                        }

                        pulseLaserShootCounter++;
                        npc.netUpdate = true;
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
            int chargeTime = 38;
            int illusionCount = 7;
            int chargeCount = 8;
            float chargeSpeed = 37.5f;
            float predictivenessFactor = 0f;
            ref float chargeCounter = ref npc.Infernum().ExtraAI[0];

            if (ExoMechManagement.CurrentAthenaPhase >= 2)
                chargeSpeed += 2.5f;
            if (ExoMechManagement.CurrentAthenaPhase >= 3)
                chargeSpeed += 2.5f;
            if (ExoMechManagement.CurrentAthenaPhase >= 5)
            {
                illusionCount += 2;
                chargeCount++;
            }
            if (ExoMechManagement.CurrentAthenaPhase >= 6)
            {
                illusionCount += 3;
                chargeTime -= 3;
                chargeSpeed += 4.5f;
            }

            // Always have all turrets open.
            // This allows the player to distinguish between the real and fake versions of the boss.
            TurretFrameState = AthenaTurretFrameType.OpenMainTurret;

            // Do teleportation effects.
            if (AttackTimer <= attackDelay)
            {
                npc.Opacity = Utils.InverseLerp(attackDelay - 1f, 0f, AttackTimer, true);
                if (AttackTimer == attackDelay)
                {
                    npc.Center = Target.Center - npc.SafeDirectionTo(Target.Center, -Vector2.UnitY) * 425f;
                    npc.netUpdate = true;
                    npc.Opacity = 1f;
                }
            }

            // Summon the illusions.
            if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer == attackDelay + 1f)
            {
                for (int i = 1; i < 1 + illusionCount; i++)
                {
                    int illusion = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<AthenaIllusion>());
                    Main.npc[illusion].ai[1] = MathHelper.TwoPi * i / (float)(illusionCount + 1f);
                }
            }

            // Charge at the target and release tesla orbs.
            if (AttackTimer == attackDelay + chargeDelay)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/LargeWeaponFire"), npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 shootVelocity = npc.SafeDirectionTo(Target.Center) * 8f;
                    Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<AresTeslaOrb>(), 500, 0f);
                }

                npc.velocity = npc.SafeDirectionTo(Target.Center + Target.velocity * predictivenessFactor) * chargeSpeed;
                npc.netUpdate = true;
            }

            // Slow down after the charge should concluded and fade away.
            if (AttackTimer > attackDelay + chargeDelay + chargeTime)
            {
                npc.velocity *= 0.97f;
                npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.15f, 0f, 1f);

                // Charge again once completely faded away.
                if (npc.Opacity <= 0f)
                {
                    chargeCounter++;

                    if (chargeCounter >= chargeCount)
                        SelectNextAttack();
                    else
                        AttackTimer = attackDelay + chargeDelay - 4f;
                    npc.Opacity = 1f;
                    npc.netUpdate = true;
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
            Vector2 hoverDestination = Target.Center + new Vector2((Target.Center.X < npc.Center.X).ToDirectionInt() * 540f, -300f);
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float attackDelay = ref npc.Infernum().ExtraAI[1];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[2];

            if (ExoMechManagement.CurrentAthenaPhase >= 2)
                totalCharges++;
            if (ExoMechManagement.CurrentAthenaPhase >= 3)
            {
                sparkCount += 5;
                chargeTime += 3;
                chargeSpeed += 3f;
            }
            if (ExoMechManagement.CurrentAthenaPhase >= 5)
            {
                predictivenessFactor = 0f;
                chargeSpeed += 5f;
            }
            if (ExoMechManagement.CurrentAthenaPhase >= 6)
            {
                chargeTime -= 6;
                sparkCount += 10;
            }

            switch ((int)attackSubstate)
            {
                // Hover into position.
                case 0:
                    npc.damage = 0;

                    // Hover to the top left/right of the target.
                    ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 50f, 92f);

                    // Once sufficiently close, go to the next attack substate.
                    if (npc.WithinRange(hoverDestination, 50f))
                    {
                        npc.velocity = Vector2.Zero;
                        attackSubstate = 1f;
                        AttackTimer = 0f;
                        npc.netUpdate = true;
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

                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/ELRFire"), npc.Center);

                        npc.velocity = npc.SafeDirectionTo(Target.Center + Target.velocity * predictivenessFactor) * chargeSpeed;

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            float offsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                            for (int i = 0; i < sparkCount; i++)
                            {
                                Vector2 sparkShootVelocity = (MathHelper.TwoPi * i / sparkCount + offsetAngle).ToRotationVector2() * 16f;
                                Utilities.NewProjectileBetter(npc.Center + sparkShootVelocity * 10f, sparkShootVelocity, ModContent.ProjectileType<TeslaSpark>(), 530, 0f);
                            }
                        }

                        attackSubstate = 2f;
                        AttackTimer = 0f;
                        npc.netUpdate = true;
                    }
                    break;

                // Release fire.
                case 2:
                    npc.damage = npc.defDamage;

                    if (AttackTimer >= chargeTime)
                    {
                        AttackTimer = 0f;
                        attackSubstate = 0f;
                        chargeCounter++;
                        npc.netUpdate = true;

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
            ref float chargeDirection = ref npc.Infernum().ExtraAI[0];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[1];

            if (ExoMechManagement.CurrentAthenaPhase >= 2)
                rocketReleaseRate--;
            if (ExoMechManagement.CurrentAthenaPhase >= 3)
            {
                chargeSpeed += 3f;
                rocketShootSpeed += 2f;
            }
            if (ExoMechManagement.CurrentAthenaPhase >= 5)
            {
                chargeTime -= 4;
                rocketReleaseRate--;
            }
            if (ExoMechManagement.CurrentAthenaPhase >= 2)
            {
                chargeCount += 2;
                chargeSpeed += 3f;
            }

            if (chargeCounter == 0f)
                redirectTime += 32;

            // Initialize the charge direction.
            if (AttackTimer == 1f)
            {
                chargeDirection = (Target.Center.X > npc.Center.X).ToDirectionInt();
                npc.netUpdate = true;
            }

            // Hover into position before charging.
            if (AttackTimer <= redirectTime)
            {
                Vector2 hoverDestination = Target.Center + Vector2.UnitX * chargeDirection * -420f;
                npc.Center = npc.Center.MoveTowards(hoverDestination, 12.5f);
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, hoverSpeed * 0.16f);

                // Slow down and summon an Exowl before charging.
                if (AttackTimer == redirectTime)
                {
                    npc.velocity *= 0.3f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int exowl = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<Exowl>(), npc.whoAmI);
                        if (Main.npc.IndexInRange(exowl))
                        {
                            Main.npc[exowl].ModNPC<Exowl>().UseConfusionEffect = true;
                            Main.npc[exowl].ModNPC<Exowl>().IsIllusion = false;
                        }
                    }
                }
            }
            else if (AttackTimer <= redirectTime + chargeTime)
            {
                // Charge and release rockets.
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitX * chargeDirection * chargeSpeed, 0.15f);
                if (AttackTimer == redirectTime + chargeTime)
                    npc.velocity *= 0.7f;

                // Fire rockets. This does not happen if close to the target.
                if (AttackTimer % rocketReleaseRate == rocketReleaseRate - 1f && !npc.WithinRange(Target.Center, 350f))
                {
                    Main.PlaySound(SoundID.Item36, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 rocketShootVelocity = (Target.Center - MainTurretCenter).SafeNormalize(-Vector2.UnitY) * rocketShootSpeed;
                        Utilities.NewProjectileBetter(MainTurretCenter, rocketShootVelocity, ModContent.ProjectileType<AthenaRocket>(), 500, 0f);
                    }
                }

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
                {
                    Exowl.MakeAllExowlsExplode();
                    SelectNextAttack();
                }
                npc.netUpdate = true;
            }
        }

        public void DoBehavior_DeathAnimation()
        {
            int implosionRingLifetime = 180;
            int pulseRingCreationRate = 32;
            int explosionTime = 240;
            float implosionRingScale = 1.5f;
            float explosionRingScale = 4f;
            Vector2 coreCenter = MainTurretCenter;

            // Slow down dramatically.
            npc.velocity *= 0.9f;

            // Use close to the minimum HP.
            npc.life = 50000;

            // Disable damage.
            npc.damage = 0;
            npc.dontTakeDamage = true;

            // Close the boss HP bar.
            npc.Calamity().ShouldCloseHPBar = true;

            // Create the implosion ring on the first frame.
            if (DeathAnimationTimer == 1f)
            {
                GeneralParticleHandler.SpawnParticle(new ElectricExplosionRing(coreCenter, Vector2.Zero, CalamityUtils.ExoPalette, implosionRingScale, implosionRingLifetime));
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/AresEnraged"), npc.Center);
            }

            // Create particles that fly outward every frame.
            if (DeathAnimationTimer > 25f && DeathAnimationTimer < implosionRingLifetime - 30f)
            {
                float particleScale = Main.rand.NextFloat(0.1f, 0.15f);
                Vector2 particleVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(10f, 32f);
                Color particleColor = CalamityUtils.MulticolorLerp(Main.rand.NextFloat(), CalamityUtils.ExoPalette);

                for (int j = 0; j < 4; j++)
                    GeneralParticleHandler.SpawnParticle(new StrongBloom(coreCenter, particleVelocity, particleColor, particleScale, 80));

                for (int i = 0; i < 2; i++)
                {
                    particleScale = Main.rand.NextFloat(1.5f, 2f);
                    particleVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(4.5f, 10f);
                    GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(coreCenter, particleVelocity, particleScale, Color.Cyan, 75, 1f, 12f));
                }
            }

            // Periodically create pulse rings.
            if (DeathAnimationTimer > 10f && DeathAnimationTimer < implosionRingLifetime - 30f && DeathAnimationTimer % pulseRingCreationRate == pulseRingCreationRate - 1f)
            {
                float finalScale = MathHelper.Lerp(3f, 5f, Utils.InverseLerp(25f, 160f, DeathAnimationTimer, true));
                Color pulseColor = CalamityUtils.MulticolorLerp(Main.rand.NextFloat(), CalamityUtils.ExoPalette);

                for (int i = 0; i < 3; i++)
                    GeneralParticleHandler.SpawnParticle(new PulseRing(coreCenter, Vector2.Zero, pulseColor, 0.2f, finalScale, pulseRingCreationRate));
            }

            // Create an explosion.
            if (DeathAnimationTimer == implosionRingLifetime)
            {
                GeneralParticleHandler.SpawnParticle(new ElectricExplosionRing(coreCenter, Vector2.Zero, CalamityUtils.ExoPalette, explosionRingScale, explosionTime));
                var sound = Main.PlaySound(InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/WyrmElectricCharge"), npc.Center);
                if (sound != null)
                    CalamityUtils.SafeVolumeChange(ref sound, 1.75f);
            }

            DeathAnimationTimer++;

            // Fade away as the explosion progresses.
            float opacityFadeInterpolant = Utils.InverseLerp(implosionRingLifetime + explosionTime * 0.75f, implosionRingLifetime, DeathAnimationTimer, true);
            npc.Opacity = (float)Math.Pow(opacityFadeInterpolant, 6.1);

            if (DeathAnimationTimer == (int)(implosionRingLifetime + explosionTime * 0.5f))
            {
                npc.life = 0;
                npc.HitEffect();
                npc.StrikeNPC(10, 0f, 1);
                npc.checkDead();
            }
        }

        public void SelectNextAttack()
        {
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

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

            if (ExoMechComboAttackContent.ShouldSelectComboAttack(npc, out ExoMechComboAttackContent.ExoMechComboAttackType newAttack))
                npc.ai[0] = (int)newAttack;

            AttackTimer = 0f;
            npc.netUpdate = true;
        }

        #endregion AI and Behaviors

        #region Frames and Drawcode

        public override void FindFrame(int frameHeight)
        {
            npc.frameCounter++;
            for (int i = 0; i < Turrets.Length; i++)
            {
                int frame = Turrets[i].Frame;
                int maxFrame = Turrets[i].IsSmall ? 3 : 4;

                switch (TurretFrameState)
                {
                    case AthenaTurretFrameType.Blinking:
                        float frameInterpolant = (float)Math.Sin(npc.frameCounter * 0.13f + i * 1.02f) * 0.5f + 0.5f;
                        frame = (int)(frameInterpolant * maxFrame * 0.99f);
                        break;
                    case AthenaTurretFrameType.OpenMainTurret:
                        if (npc.frameCounter % 6 == 5)
                        {
                            if (Turrets[i].IsSmall)
                                frame--;
                            else
                                frame++;
                        }
                        break;
                    case AthenaTurretFrameType.CloseAllTurrets:
                        if (npc.frameCounter % 6 == 5)
                            frame--;
                        break;
                    case AthenaTurretFrameType.OpenAllTurrets:
                        if (npc.frameCounter % 6 == 5)
                            frame++;
                        break;
                }

                Turrets[i].Frame = frame;
            }
        }

        public float FlameTrailPulse => (float)Math.Sin(Main.GlobalTime * 6f + npc.whoAmI * 111.5856f) * 0.5f + 0.5f;

        // Update these in the illusion NPC's file if this needs changing for some reason.
        // Static methods doesn't easily work in this context, unfortunately.
        public float FlameTrailWidthFunction(float completionRatio)
        {
            float maxWidth = MathHelper.Lerp(15f, 80f, FlameTrailPulse);
            return MathHelper.SmoothStep(maxWidth, 8f, completionRatio) * npc.Opacity;
        }

        public Color FlameTrailColorFunction(float completionRatio)
        {
            float trailOpacity = Utils.InverseLerp(0.8f, 0.27f, completionRatio, true) * Utils.InverseLerp(0f, 0.067f, completionRatio, true);
            trailOpacity *= MathHelper.Lerp(1f, 0.27f, 1f - FlameTrailPulse) * npc.Opacity;
            Color startingColor = Color.Lerp(Color.White, Color.Cyan, 0.27f);
            Color middleColor = Color.Lerp(Color.Orange, Color.Blue, 0.74f);
            Color endColor = Color.DarkCyan;
            Color color = CalamityUtils.MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * trailOpacity;
            color.A /= 8;
            return color * npc.Opacity;
        }

        public float RayWidthFunction(float completionRatio)
        {
            float widthOffset = (float)Math.Cos(completionRatio * 73f - Main.GlobalTime * 8f) * 
                Utils.InverseLerp(0f, 0.1f, completionRatio, true) * 
                Utils.InverseLerp(1f, 0.9f, completionRatio, true);
            return MathHelper.Lerp(2f, npc.Infernum().ExtraAI[2] * 0.7f, completionRatio) + widthOffset;
        }

        public static Color RayColorFunction(float completionRatio)
        {
            return Color.Cyan * Utils.InverseLerp(0.8f, 0.5f, completionRatio, true) * 0.6f;
        }

        public void DrawLightRay(float initialRayRotation, float rayBrightness, Vector2 rayStartingPoint)
        {
            if (LightRayDrawer is null)
                LightRayDrawer = new PrimitiveTrail(RayWidthFunction, RayColorFunction, PrimitiveTrail.RigidPointRetreivalFunction);
            Vector2 currentRayDirection = initialRayRotation.ToRotationVector2();

            float length = rayBrightness * npc.Infernum().ExtraAI[4] * 400f;
            List<Vector2> points = new List<Vector2>();
            for (int i = 0; i <= 12; i++)
                points.Add(Vector2.Lerp(rayStartingPoint, rayStartingPoint + initialRayRotation.ToRotationVector2() * length, i / 12f));

            LightRayDrawer.Draw(points, -Main.screenPosition, 47);
        }

        public static void DrawExowlHologram(Vector2 drawPosition, int exowlFrame, float hologramInterpolant)
        {
            float hologramOpacity = (float)Math.Pow(hologramInterpolant, 0.45);
            Texture2D exowlTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Draedon/Athena/Exowl");
            Rectangle frame = exowlTexture.Frame(1, 3, 0, exowlFrame);

            DrawData fuckYou = new DrawData(exowlTexture, drawPosition, frame, Color.White * hologramOpacity, 0f, frame.Size() * 0.5f, 1f, 0, 0);
            GameShaders.Misc["Infernum:Hologram"].UseOpacity(hologramInterpolant);
            GameShaders.Misc["Infernum:Hologram"].UseColor(Color.Cyan);
            GameShaders.Misc["Infernum:Hologram"].UseSecondaryColor(Color.Gold);
            GameShaders.Misc["Infernum:Hologram"].SetShaderTexture(ModContent.GetTexture("InfernumMode/ExtraTextures/HologramTexture"));
            GameShaders.Misc["Infernum:Hologram"].Apply(fuckYou);
            fuckYou.Draw(Main.spriteBatch);
        }

        public Vector2 GetHologramPosition(int index, float illusionCount, float hologramSpan, float hologramInterpolant)
        {
            float completionRatio = index / (illusionCount - 1f);
            float hologramHorizontalOffset = MathHelper.Lerp(-0.5f, 0.5f, completionRatio) * hologramSpan;
            float hologramVerticalOffset = Utils.InverseLerp(0f, 0.5f, hologramInterpolant, true) * 200f + CalamityUtils.Convert01To010(completionRatio) * 40f;
            return npc.Top + new Vector2(hologramHorizontalOffset, -hologramVerticalOffset);
        }

        public static void DrawBaseNPC(NPC npc, Vector2 screenPos, Color drawColor, float flameTrailPulse, PrimitiveTrail flameTrail)
        {
            // Drift towards a brighter color.
            drawColor = Color.Lerp(drawColor, Color.White, 0.45f);

            // Prepare the flame trail shader with its map texture.
            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(ModContent.GetTexture("CalamityMod/ExtraTextures/ScarletDevilStreak"));

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

            Texture2D texture = Main.npcTexture[npc.type];
            Texture2D glowmask = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Draedon/Athena/AthenaNPC_Glowmask");
            Vector2 drawPosition = npc.Center - screenPos;
            Vector2 origin = npc.frame.Size() * 0.5f;
            Main.spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(drawColor), npc.rotation, origin, npc.scale, 0, 0f);
            Main.spriteBatch.Draw(glowmask, drawPosition, npc.frame, npc.GetAlpha(Color.White), npc.rotation, origin, npc.scale, 0, 0f);
        }

        public Vector2 GetTurretPosition(int i) => npc.Center + TurretOffsets[i].RotatedBy(npc.rotation) + Vector2.UnitY * 46f;

        public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            // Declare the trail drawers if they have yet to be defined.
            if (FlameTrail is null)
                FlameTrail = new PrimitiveTrail(FlameTrailWidthFunction, FlameTrailColorFunction, null, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);

            Texture2D texture = Main.npcTexture[npc.type];
            float finalPhaseGlowInterpolant = Utils.InverseLerp(0f, ExoMechManagement.FinalPhaseTransitionTime * 0.75f, FinalPhaseAnimationTime, true);
            if (finalPhaseGlowInterpolant > 0f)
            {
                float backAfterimageOffset = finalPhaseGlowInterpolant * 10f;
                for (int i = 0; i < 8; i++)
                {
                    Color color = Main.hslToRgb((i / 8f + Main.GlobalTime * 0.6f) % 1f, 1f, 0.56f);
                    color.A = 0;
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 8f + Main.GlobalTime * 0.8f).ToRotationVector2() * backAfterimageOffset;
                    Vector2 drawPosition = npc.Center - Main.screenPosition + drawOffset;
                    Main.spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(color), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
                }
            }

            if (AttackState == AthenaAttackType.IllusionRocketCharge || (int)AttackState == (int)ExoMechComboAttackContent.ExoMechComboAttackType.TwinsAthena_ThermoplasmaDance)
            {
                for (int i = -3; i <= 8; i++)
                {
                    if (i == 0)
                        continue;

                    Color duplicateColor = Color.White;
                    Vector2 baseDrawPosition = npc.Center - Main.screenPosition;
                    Vector2 drawPosition = baseDrawPosition;

                    // Create lagging afterimages.
                    if (i > 3)
                    {
                        float lagBehindFactor = Utils.InverseLerp(30f, 70f, AttackTimer, true);
                        if (lagBehindFactor == 0f)
                            continue;

                        drawPosition = baseDrawPosition + npc.velocity * -3f * (i - 4f) * lagBehindFactor;
                        duplicateColor *= 1f - (i - 3f) / 4f;
                    }

                    // Create cool afterimages while charging at the target.
                    else
                    {
                        float hue = (i + 5f) / 10f;
                        float drawOffsetFactor = 60f;
                        Vector3 offsetInformation = Vector3.Transform(Vector3.Forward,
                            Matrix.CreateRotationX((Main.GlobalTime - 0.3f + i * 0.1f) * 0.7f * MathHelper.TwoPi) *
                            Matrix.CreateRotationY((Main.GlobalTime - 0.8f + i * 0.3f) * 0.7f * MathHelper.TwoPi) *
                            Matrix.CreateRotationZ((Main.GlobalTime + i * 0.5f) * 0.1f * MathHelper.TwoPi));
                        drawOffsetFactor += Utils.InverseLerp(-1f, 1f, offsetInformation.Z, true) * 70f;
                        Vector2 drawOffset = new Vector2(offsetInformation.X, offsetInformation.Y) * drawOffsetFactor;
                        drawOffset = drawOffset.RotatedBy(MathHelper.TwoPi * AttackTimer / 180f);

                        if ((int)AttackState == (int)ExoMechComboAttackContent.ExoMechComboAttackType.TwinsAthena_ThermoplasmaDance)
                            drawOffset *= Utils.InverseLerp(16f, 25f, npc.velocity.Length(), true);

                        float luminanceInterpolant = Utils.InverseLerp(90f, 0f, AttackTimer, true);
                        duplicateColor = Main.hslToRgb(hue, 1f, MathHelper.Lerp(0.5f, 1f, luminanceInterpolant)) * npc.Opacity * 0.8f;
                        duplicateColor.A /= 3;
                        drawPosition += drawOffset;
                    }

                    // Draw the base texture.
                    spriteBatch.Draw(texture, drawPosition, npc.frame, duplicateColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, 0, 0f);
                }
            }

            DrawBaseNPC(npc, Main.screenPosition, drawColor, FlameTrailPulse, FlameTrail);

            for (int i = 0; i < Turrets.Length; i++)
            {
                if (Turrets[i] is null)
                    break;

                int totalFrames = 4;
                Texture2D turretTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Draedon/Athena/AthenaTurretLarge");
                if (Turrets[i].IsSmall)
                {
                    totalFrames = 3;
                    turretTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Draedon/Athena/AthenaTurretSmall");
                }

                // totalFrames is used instead of totalFrames - 1f in division to allow for some of the original color to still go through.
                // Even as a full crystal, I'd prefer the texture not be completely fullbright.
                Color turretColor = Color.Lerp(drawColor, Color.White, Turrets[i].Frame / (float)totalFrames);
                Rectangle turretFrame = turretTexture.Frame(1, totalFrames, 0, Turrets[i].Frame);
                Vector2 drawPosition = npc.Center - Main.screenPosition;
                Vector2 turretOrigin = turretFrame.Size() * 0.5f;
                Vector2 turretDrawPosition = GetTurretPosition(i) - npc.Center + drawPosition;
                Main.spriteBatch.Draw(turretTexture, turretDrawPosition, turretFrame, npc.GetAlpha(turretColor), 0f, turretOrigin, npc.scale, 0, 0f);
            }

            // Draw a line telegraph as necessary
            if (TelegraphInterpolant > 0f)
            {
                Main.spriteBatch.SetBlendState(BlendState.Additive);

                Texture2D telegraphTexture = ModContent.GetTexture("InfernumMode/ExtraTextures/BloomLine");
                float telegraphScaleFactor = TelegraphInterpolant * 1.2f;

                for (float offsetAngle = 0f; offsetAngle < 0.2f; offsetAngle += 0.03f)
                {
                    Vector2 telegraphStart = MainTurretCenter + (TelegraphRotation + offsetAngle).ToRotationVector2() * 20f - Main.screenPosition;
                    Vector2 telegraphOrigin = new Vector2(0.5f, 0f) * telegraphTexture.Size();
                    Vector2 telegraphScale = new Vector2(telegraphScaleFactor, 3f);
                    Color telegraphColor = new Color(74, 255, 204) * (float)Math.Pow(TelegraphInterpolant, 0.79) * ((0.2f - offsetAngle) / 0.2f) * 1.6f;
                    Main.spriteBatch.Draw(telegraphTexture, telegraphStart, null, telegraphColor, TelegraphRotation + offsetAngle - MathHelper.PiOver2, telegraphOrigin, telegraphScale, 0, 0f);
                }

                Main.spriteBatch.ResetBlendState();
            }

            // Draw holograms.
            if (AttackState == AthenaAttackType.ExowlHologramSwarm)
            {
                float hologramInterpolant = npc.Infernum().ExtraAI[0];
                float illusionCount = npc.Infernum().ExtraAI[1];
                float hologramSpan = npc.Infernum().ExtraAI[2];

                Main.spriteBatch.EnterShaderRegion();

                float rayBrightness = Utils.InverseLerp(0f, 0.45f, hologramInterpolant, true);
                DrawLightRay(-MathHelper.PiOver2, rayBrightness, MainTurretCenter);
                Main.spriteBatch.EnterShaderRegion();

                for (int i = 0; i < illusionCount; i++)
                {
                    int illusionFrame = (int)(Main.GlobalTime * 6f + i) % 3;
                    float completionRatio = i / (illusionCount - 1f);
                    Vector2 hologramDrawPosition = GetHologramPosition(i, illusionCount, hologramSpan, hologramInterpolant) - Main.screenPosition;
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

        public override void NPCLoot()
        {
            AresBody.DropExoMechLoot(npc, (int)AresBody.MechType.Thanatos);
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            for (int k = 0; k < 3; k++)
                Dust.NewDust(new Vector2(npc.position.X, npc.position.Y), npc.width, npc.height, 107, 0f, 0f, 100, new Color(0, 255, 255), 1f);
        }

        public override bool CheckActive() => false;

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.lifeMax = (int)(npc.lifeMax * 0.5f * bossLifeScale);
            npc.damage = (int)(npc.damage * 0.8f);
        }
        #endregion Misc Things
    }
}
