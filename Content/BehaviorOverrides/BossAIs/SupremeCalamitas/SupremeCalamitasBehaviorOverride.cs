using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Events;
using CalamityMod.Items.Mounts;
using CalamityMod.NPCs;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Tiles;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.AttemptRecording;
using InfernumMode.Common.Graphics.Fluids;
using InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas.Symbols;
using InfernumMode.Content.Credits;
using InfernumMode.Content.Projectiles.Pets;
using InfernumMode.Core;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

using SCalBoss = CalamityMod.NPCs.SupremeCalamitas.SupremeCalamitas;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public partial class SupremeCalamitasBehaviorOverride : NPCBehaviorOverride
    {
        public enum SCalAttackType
        {
            HorizontalDarkSoulRelease,
            CondemnationFanBurst,
            ExplosiveCharges,
            HellblastBarrage,
            BecomeBerserk,
            SummonSuicideBomberDemons,
            BrimstoneJewelBeam,
            DarkMagicBombWalls,
            FireLaserSpin,
            SummonSepulcher,
            SummonBrothers,
            SummonSeekers,
            PhaseTransition,
            DesperationPhase,

            // Shadow demon attacks.
            SummonShadowDemon = 50,
            ShadowDemon_ReleaseExplodingShadowBlasts,
            ShadowDemon_ShadowGigablastsAndCharges,
        }

        public enum SCalFrameType
        {
            UpwardDraft,
            FasterUpwardDraft,
            MagicCircle,
            BlastCast,
            BlastPunchCast,
            OutwardHandCast,
            PunchHandCast,
            Count
        }

        public FluidFieldInfernum FireDrawer
        {
            get;
            set;
        }

        public static NPC SCal
        {
            get
            {
                if (CalamityGlobalNPC.SCal == -1)
                    return null;

                return Main.npc[CalamityGlobalNPC.SCal];
            }
        }

        public static float ShieldOpacity
        {
            get
            {
                if (SCal is null)
                    return 0f;
                return SCal.ModNPC<SCalBoss>().shieldOpacity;
            }
            set
            {
                if (SCal is null)
                    return;

                SCal.ModNPC<SCalBoss>().shieldOpacity = value;
            }
        }

        public static float ShieldRotation
        {
            get
            {
                if (SCal is null)
                    return 0f;

                return SCal.ModNPC<SCalBoss>().shieldRotation;
            }
            set
            {
                if (SCal is null)
                    return;

                SCal.ModNPC<SCalBoss>().shieldRotation = value;
            }
        }

        public static float ForcefieldScale
        {
            get
            {
                if (SCal is null)
                    return 0f;

                return SCal.ModNPC<SCalBoss>().forcefieldScale;
            }
            set
            {
                if (SCal is null)
                    return;

                SCal.ModNPC<SCalBoss>().forcefieldScale = value;
            }
        }

        public static bool Enraged
        {
            get
            {
                if (SCal is null)
                    return false;

                return !Main.player[SCal.target].Hitbox.Intersects(SCal.Infernum().Arena);
            }
        }

        public static int BrimstoneDartDamage => 540;

        public static int BrimstoneHellblastDamage => 540;

        public static int BrothersProjectileDamage => 540;

        public static int CondemnationArrowDamage => 540;

        public static int DarkMagicFlameDamage => 540;

        public static int SepulcherBoneDamage => 540;

        public static int ShadowBoltDamage => 540;

        public static int GigablastDamage => 600;

        public static int LostSoulDamage => 600;

        public static int ShadowBlastDamage => 600;

        public static int DemonicExplosionDamage => 650;

        public static int SepulcherSoulBombDamage => 650;

        public static int SuicideBomberDemonDamage => 650;

        public static int FlameOverloadBeamDamage => 900;

        public static int FlamePillarDamage => 900;

        public static int RancorLaserbeamDamage => 900;

        public static bool ShadowDemonCanAttack => SCal?.ai[0] >= 50f;

        public static SCalAttackType[] Phase1AttackCycle => new SCalAttackType[]
        {
            SCalAttackType.HorizontalDarkSoulRelease,
            SCalAttackType.CondemnationFanBurst,
            SCalAttackType.ExplosiveCharges,
            SCalAttackType.SummonSuicideBomberDemons,
            SCalAttackType.HorizontalDarkSoulRelease,
            SCalAttackType.CondemnationFanBurst,
            SCalAttackType.ExplosiveCharges,
            SCalAttackType.SummonSuicideBomberDemons,
        };

        public static SCalAttackType[] Phase2AttackCycle => new SCalAttackType[]
        {
            SCalAttackType.CondemnationFanBurst,
            SCalAttackType.ShadowDemon_ReleaseExplodingShadowBlasts,
            SCalAttackType.HellblastBarrage,
            SCalAttackType.BrimstoneJewelBeam,
            SCalAttackType.CondemnationFanBurst,
            SCalAttackType.ExplosiveCharges,
            SCalAttackType.ShadowDemon_ReleaseExplodingShadowBlasts,
            SCalAttackType.HorizontalDarkSoulRelease,
            SCalAttackType.HellblastBarrage,
            SCalAttackType.SummonSuicideBomberDemons,
            SCalAttackType.ShadowDemon_ReleaseExplodingShadowBlasts,
            SCalAttackType.BrimstoneJewelBeam,
            SCalAttackType.ExplosiveCharges,
            SCalAttackType.HellblastBarrage,
            SCalAttackType.CondemnationFanBurst,
            SCalAttackType.ShadowDemon_ReleaseExplodingShadowBlasts,
            SCalAttackType.HorizontalDarkSoulRelease,
            SCalAttackType.HellblastBarrage,
            SCalAttackType.BrimstoneJewelBeam,
            SCalAttackType.SummonSuicideBomberDemons,
        };

        public static SCalAttackType[] Phase3AttackCycle => new SCalAttackType[]
        {
            SCalAttackType.ShadowDemon_ShadowGigablastsAndCharges,
            SCalAttackType.FireLaserSpin,
            SCalAttackType.DarkMagicBombWalls,
            SCalAttackType.CondemnationFanBurst,
            SCalAttackType.BecomeBerserk,
            SCalAttackType.BrimstoneJewelBeam,
            SCalAttackType.ShadowDemon_ShadowGigablastsAndCharges,
            SCalAttackType.DarkMagicBombWalls,
            SCalAttackType.ExplosiveCharges,
            SCalAttackType.BecomeBerserk,
            SCalAttackType.CondemnationFanBurst,
            SCalAttackType.ShadowDemon_ShadowGigablastsAndCharges,
            SCalAttackType.BrimstoneJewelBeam,
            SCalAttackType.FireLaserSpin,
            SCalAttackType.ExplosiveCharges
        };

        public const float Phase2LifeRatio = 0.75f;

        public const float Phase3LifeRatio = 0.45f;

        public const float Phase4LifeRatio = 0.25f;

        public override int NPCOverrideType => ModContent.NPCType<SCalBoss>();

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio,
            Phase3LifeRatio,
            Phase4LifeRatio
        };

        #region AI
        public override void SetDefaults(NPC npc)
        {
            // Set defaults that, if were to be changed by Calamity, would cause significant issues to the fight.
            npc.width = 44;
            npc.height = 44;
            npc.scale = 1f;
            npc.defense = 100;
            npc.DR_NERD(0.25f);
        }

        public static Vector2 CalculateHandPosition()
        {
            if (SCal is null)
                return Vector2.Zero;

            return SCal.Center + new Vector2(SCal.spriteDirection * -18f, 2f);
        }

        public override bool PreAI(NPC npc)
        {
            // Do targeting.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            Vector2 handPosition = CalculateHandPosition();
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float attackDelay = ref npc.ai[2];
            ref float berserkPhaseInterpolant = ref npc.ai[3];
            ref float frameChangeSpeed = ref npc.localAI[1];
            ref float frameType = ref npc.localAI[2];
            ref float currentPhase = ref npc.Infernum().ExtraAI[6];
            ref float switchToDesperationPhase = ref npc.Infernum().ExtraAI[7];

            // Set the whoAmI variable.
            CalamityGlobalNPC.SCal = npc.whoAmI;

            // Become angry.
            npc.Calamity().CurrentlyEnraged = Enraged;

            // Handle initializations.
            if (npc.localAI[0] == 0f)
            {
                // Define the arena. This is finely picked to be the same as in death mode.
                Vector2 arenaArea = new(125f, 125f);
                npc.Infernum().Arena = Utils.CenteredRectangle(npc.Center, arenaArea * 16f);
                ushort arenaTileType = (ushort)ModContent.TileType<ArenaTile>();

                Point arenaCenter = npc.Infernum().Arena.Center().ToTileCoordinates();
                arenaCenter.X += 3;
                arenaCenter.Y += 4;
                int width = npc.Infernum().Arena.Width / 2 / 16 + 1;
                int height = npc.Infernum().Arena.Height / 2 / 16 + 1;
                for (int x = arenaCenter.X - width; x <= arenaCenter.X + width; x++)
                {
                    for (int y = arenaCenter.Y - height; y <= arenaCenter.Y + height; y++)
                    {
                        if (!WorldGen.InWorld(x, y, 2))
                            continue;

                        if ((x == arenaCenter.X - width || x == arenaCenter.X + width || y == arenaCenter.Y - height || y == arenaCenter.Y + height) && !Main.tile[x, y].HasTile)
                        {
                            Main.tile[x, y].TileType = arenaTileType;
                            Main.tile[x, y].Get<TileWallWireStateData>().HasTile = true;
                        }
                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendTileSquare(-1, x, y, 1, TileChangeType.None);
                        }
                        else
                        {
                            WorldGen.SquareTileFrame(x, y, true);
                        }
                    }
                }

                npc.Infernum().Arena.X += 48;
                npc.Infernum().Arena.Y += 64;
                npc.ModNPC<SCalBoss>().initialRitualPosition = npc.Center + Vector2.UnitY * 24f;
                attackDelay = 270f;
                attackType = (int)SCalAttackType.SummonSepulcher;
                npc.localAI[0] = 2f;
                ShieldOpacity = 0f;
                npc.netUpdate = true;
            }

            // Handle music.
            HandleMusicVariables(npc);

            // Reset things every frame.
            npc.localAI[3] = 0f;
            npc.damage = 0;
            npc.dontTakeDamage = NPC.AnyNPCs(ModContent.NPCType<SoulSeekerSupreme>()) || Enraged;
            npc.Infernum().ExtraAI[8] = 1f;
            npc.Calamity().DR = 0.4f;
            npc.ModNPC<SCalBoss>().safeBox = npc.Infernum().Arena;

            // Disable the exo box.
            if (target.mount?.Type == ModContent.MountType<DraedonGamerChairMount>())
                target.mount.Dismount(target);

            // Redfine the hitbox size.
            Vector2 hitboxSize = new(ForcefieldScale * 216f / 1.4142f);
            hitboxSize = Vector2.Max(hitboxSize, new Vector2(42, 44));
            if (npc.Size != hitboxSize)
                npc.Size = hitboxSize;

            // Vanish if the target is gone.
            if (!target.active || target.dead)
            {
                npc.Opacity = Clamp(npc.Opacity - 0.1f, 0f, 1f);

                for (int i = 0; i < 2; i++)
                {
                    Dust fire = Dust.NewDustPerfect(npc.Center, (int)CalamityDusts.Brimstone);
                    fire.position += Main.rand.NextVector2Circular(36f, 36f);
                    fire.velocity = Main.rand.NextVector2Circular(8f, 8f);
                    fire.noGravity = true;
                    fire.scale *= Main.rand.NextFloat(1f, 1.2f);
                }

                if (npc.Opacity <= 0f)
                    npc.active = false;
                return false;
            }

            // Don't attack if a delay is in place.
            if (attackDelay > 0f)
            {
                npc.dontTakeDamage = true;
                attackDelay--;
                if (attackDelay == 42f)
                    SoundEngine.PlaySound(SCalBoss.SpawnSound, target.Center);

                return false;
            }

            // Randomly create symbols far from the target. This is affected by the graphics config.
            if (Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool(12) && !InfernumConfig.Instance.ReducedGraphicsConfig)
            {
                Vector2 arenaEdge = npc.Infernum().Arena.Center.ToVector2();
                Vector2 symbolSpawnPosition = arenaEdge + Main.rand.NextVector2Unit() * npc.Infernum().Arena.Size() * Main.rand.NextFloat(1.4f, 1.96f) * 0.5f;
                Utilities.NewProjectileBetter(symbolSpawnPosition, Vector2.Zero, ModContent.ProjectileType<SCalSymbol>(), 0, 0f);
            }

            // Enter new phases.
            float lifeRatio = npc.life / (float)npc.lifeMax;
            if (lifeRatio < Phase2LifeRatio && currentPhase == 0f)
            {
                attackTimer = 0f;
                attackType = (int)SCalAttackType.PhaseTransition;
                currentPhase++;
                npc.netUpdate = true;
            }

            if (lifeRatio < Phase3LifeRatio && currentPhase == 1f)
            {
                attackTimer = 0f;
                attackType = (int)SCalAttackType.PhaseTransition;
                currentPhase++;
                npc.netUpdate = true;
            }

            if (lifeRatio < Phase4LifeRatio && currentPhase == 2f)
            {
                attackTimer = 0f;
                attackType = (int)SCalAttackType.PhaseTransition;
                currentPhase++;
                npc.netUpdate = true;
            }

            if (switchToDesperationPhase == 1f && currentPhase == 3f)
            {
                attackTimer = 0f;
                switchToDesperationPhase = 0f;
                attackType = (int)SCalAttackType.PhaseTransition;
                currentPhase++;
                npc.dontTakeDamage = true;
                npc.netUpdate = true;
            }

            bool inBerserkPhase = berserkPhaseInterpolant > 0f;
            switch ((SCalAttackType)attackType)
            {
                case SCalAttackType.HorizontalDarkSoulRelease:
                    DoBehavior_HorizontalDarkSoulRelease(npc, target, (int)currentPhase, handPosition, inBerserkPhase, ref frameType, ref frameChangeSpeed, ref attackTimer);
                    break;
                case SCalAttackType.CondemnationFanBurst:
                    DoBehavior_CondemnationFanBurst(npc, target, (int)currentPhase, handPosition, inBerserkPhase, ref frameType, ref frameChangeSpeed, ref attackTimer);
                    break;
                case SCalAttackType.ExplosiveCharges:
                    DoBehavior_ExplosiveCharges(npc, target, (int)currentPhase, inBerserkPhase, ref frameType, ref frameChangeSpeed, ref attackTimer);
                    break;
                case SCalAttackType.HellblastBarrage:
                    DoBehavior_HellblastBarrage(npc, target, (int)currentPhase, inBerserkPhase, ref frameType, ref frameChangeSpeed, ref attackTimer);
                    break;
                case SCalAttackType.BecomeBerserk:
                    DoBehavior_BecomeBerserk(npc, target, ref frameType, ref frameChangeSpeed, ref attackTimer);
                    break;
                case SCalAttackType.SummonSuicideBomberDemons:
                    DoBehavior_SummonSuicideBomberDemons(npc, target, (int)currentPhase, ref frameType, ref frameChangeSpeed, ref attackTimer);
                    break;
                case SCalAttackType.BrimstoneJewelBeam:
                    DoBehavior_BrimstoneJewelBeam(npc, target, (int)currentPhase, ref frameType, ref frameChangeSpeed, ref attackTimer);
                    break;
                case SCalAttackType.DarkMagicBombWalls:
                    DoBehavior_DarkMagicBombWalls(npc, target, (int)currentPhase, ref frameType, ref frameChangeSpeed, ref attackTimer);
                    break;
                case SCalAttackType.FireLaserSpin:
                    DoBehavior_FireLaserSpin(npc, target, (int)currentPhase, ref frameType, ref frameChangeSpeed, ref attackTimer);
                    break;
                case SCalAttackType.SummonShadowDemon:
                    DoBehavior_SummonShadowDemon(npc, target, ref frameType, ref frameChangeSpeed, ref attackTimer);
                    break;
                case SCalAttackType.SummonSepulcher:
                    DoBehavior_SummonSepulcher(npc, target, ref frameType, ref frameChangeSpeed, ref attackTimer);
                    break;
                case SCalAttackType.SummonBrothers:
                    DoBehavior_SummonBrothers(npc, target, ref frameType, ref frameChangeSpeed, ref attackTimer);
                    break;
                case SCalAttackType.SummonSeekers:
                    DoBehavior_SummonSeekers(npc, target, handPosition, ref frameType, ref frameChangeSpeed, ref attackTimer);
                    break;
                case SCalAttackType.PhaseTransition:
                    DoBehavior_PhaseTransition(npc, target, (int)currentPhase, ref frameType, ref frameChangeSpeed, ref attackTimer);
                    break;
                case SCalAttackType.DesperationPhase:
                    DoBehavior_DesperationPhase(npc, target, ref frameType, ref frameChangeSpeed, ref attackTimer);
                    break;
                default:
                    frameType = (int)SCalFrameType.MagicCircle;
                    frameChangeSpeed = 0.2f;

                    // Hover to the side of the target.
                    npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                    Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 500f, -350f);
                    if (!npc.WithinRange(hoverDestination, 150f))
                        npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 32f, 1.5f);
                    break;
            }

            attackTimer++;

            return false;
        }

        public static void HandleMusicVariables(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;

            CalamityGlobalNPC.SCalGrief = -1;
            CalamityGlobalNPC.SCalLament = -1;
            CalamityGlobalNPC.SCalEpiphany = -1;
            CalamityGlobalNPC.SCalAcceptance = -1;

            if (lifeRatio <= Phase4LifeRatio)
                CalamityGlobalNPC.SCalEpiphany = npc.whoAmI;
            else if (lifeRatio <= Phase3LifeRatio)
                CalamityGlobalNPC.SCalLament = npc.whoAmI;
            else
                CalamityGlobalNPC.SCalGrief = npc.whoAmI;
        }

        public static void DoBehavior_HorizontalDarkSoulRelease(NPC npc, Player target, int currentPhase, Vector2 handPosition, bool inBerserkPhase, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)
        {
            int boltBurstReleaseCount = 2;
            int shootDelay = 60;
            int shootTime = 180;
            int shootRate = 7;
            float soulShootSpeed = 8.5f;

            if (currentPhase >= 1)
                shootRate -= 2;
            if (inBerserkPhase)
                shootRate = 4;

            if (Enraged)
            {
                shootRate = 3;
                soulShootSpeed += 13.5f;
            }

            ref float boltBurstCounter = ref npc.Infernum().ExtraAI[0];

            // Use the punch casting animation.
            frameChangeSpeed = 0.27f;
            frameType = (int)SCalFrameType.PunchHandCast;

            // Reset animation values.
            ForcefieldScale = 1f;
            ShieldOpacity = 0f;
            ShieldRotation = 0f;

            // Hover to the side of the target.
            npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();

            float hoverAcceleration = 0.9f;
            Vector2 hoverDestination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * 820f;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 32f, hoverAcceleration);

            // Give a tip.
            if (attackTimer == shootDelay)
                HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.SCalSkullTip");

            if (attackTimer >= shootDelay)
            {
                // Release energy particles at the hand position.
                Dust brimstoneMagic = Dust.NewDustPerfect(handPosition, 264);
                brimstoneMagic.velocity = Vector2.UnitY.RotatedByRandom(0.14f) * Main.rand.NextFloat(-3.5f, -3f) + npc.velocity;
                brimstoneMagic.scale = Main.rand.NextFloat(1.25f, 1.35f);
                brimstoneMagic.noGravity = true;
                brimstoneMagic.noLight = true;

                // Fire the souls.
                if ((attackTimer - shootDelay) % shootRate == shootRate - 1f)
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath52, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int shootCounter = (int)((attackTimer - shootDelay) / shootRate);
                        float offsetAngle = Lerp(-0.67f, 0.67f, shootCounter % 3f / 2f) + Main.rand.NextFloatDirection() * 0.25f;
                        Vector2 soulVelocity = (Vector2.UnitX * (target.Center.X > npc.Center.X).ToDirectionInt()).RotatedBy(offsetAngle) * soulShootSpeed * new Vector2(0.67f, 1f);
                        soulVelocity.Y += target.velocity.Y;

                        Utilities.NewProjectileBetter(handPosition, soulVelocity, ModContent.ProjectileType<RedirectingDarkSoul>(), LostSoulDamage, 0f);
                    }
                }

                if (attackTimer >= shootDelay + shootTime)
                {
                    attackTimer = 0f;
                    boltBurstCounter++;

                    if (boltBurstCounter >= boltBurstReleaseCount)
                        SelectNextAttack(npc);
                }
            }
        }

        public static void DoBehavior_CondemnationFanBurst(NPC npc, Player target, int currentPhase, Vector2 handPosition, bool inBerserkPhase, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)
        {
            int chargeupTime = 120;
            int condemnationSpinTime = 48;
            int condemnationChargePuffRate = 15;
            int fanShootTime = 52;
            int shootRate = 2;
            int shootCount = 2;
            bool dartConvergence = false;
            float shootSpeed = 11.25f;
            float angularVariance = 2.94f;

            if (currentPhase >= 1)
            {
                shootSpeed += 3.5f;
                dartConvergence = true;
                condemnationSpinTime += 36;
                chargeupTime += 35;
                angularVariance -= 0.22f;
            }

            if (inBerserkPhase)
            {
                condemnationSpinTime -= 12;
                condemnationChargePuffRate = 12;
                shootSpeed = 15f;
                angularVariance = 3.03f;
                dartConvergence = true;
            }

            if (currentPhase >= 3)
                shootSpeed += 4.75f;

            if (Enraged)
            {
                shootRate = 1;
                shootSpeed += 17.5f;
            }

            float fanAngularOffsetInterpolant = Utils.GetLerpValue(chargeupTime - 45f, chargeupTime - 8f, attackTimer, true);
            float fanCompletionInterpolant = Utils.GetLerpValue(0f, fanShootTime, attackTimer - chargeupTime, true);
            float hoverSpeedFactor = Utils.GetLerpValue(chargeupTime * 0.75f, 0f, attackTimer, true) * 0.65f + 0.35f;
            ref float condemnationIndex = ref npc.Infernum().ExtraAI[0];
            ref float fanDirection = ref npc.Infernum().ExtraAI[1];
            ref float playerAimLockonDirection = ref npc.Infernum().ExtraAI[2];
            ref float shootCounter = ref npc.Infernum().ExtraAI[3];

            // Define the projectile as a convenient reference type variable, for easy manipulation of its attributes.
            Projectile condemnationRef = Main.projectile[(int)condemnationIndex];
            if (condemnationRef.type != ModContent.ProjectileType<CondemnationProj>())
                condemnationRef = null;

            // Use the hands out casting animation.
            frameChangeSpeed = 0.27f;
            frameType = (int)SCalFrameType.BlastCast;

            // Reset animation values.
            ForcefieldScale = 1f;
            ShieldOpacity = 0f;
            ShieldRotation = 0f;

            // Hover to the side of the target.
            npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
            Vector2 hoverDestination = npc.Infernum().Arena.Center.ToVector2();
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeedFactor * 32f, hoverSpeedFactor * 1.2f);

            // Create Condemnation on the first frame and decide which direction the fan will go in.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 1f)
            {
                condemnationIndex = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<CondemnationProj>(), 0, 0f);
                fanDirection = Main.rand.NextBool().ToDirectionInt();
                npc.netUpdate = true;
            }

            // Spin condemnation around before aiming it at the target.
            float spinRotation = WrapAngle(Pi * attackTimer / condemnationSpinTime * 6f);
            float aimAtTargetRotation = (target.Center - handPosition + target.velocity * 10f).ToRotation() + Main.rand.NextFloatDirection() * 0.07f;
            if (playerAimLockonDirection != 0f)
                aimAtTargetRotation = playerAimLockonDirection;

            // Define the lock-on direction.
            if (playerAimLockonDirection == 0f && fanCompletionInterpolant > 0f)
            {
                playerAimLockonDirection = aimAtTargetRotation;
                npc.netUpdate = true;
            }

            // Make the aim direction move upward before firing, in anticipation of the fan.
            aimAtTargetRotation -= angularVariance * fanDirection * fanAngularOffsetInterpolant * Lerp(-0.5f, 0.5f, fanCompletionInterpolant);

            // Adjust Condemnation's rotation.
            float condemnationSpinInterpolant = Utils.GetLerpValue(condemnationSpinTime + 10f, condemnationSpinTime, attackTimer, true);
            if (condemnationRef != null)
                condemnationRef.rotation = aimAtTargetRotation.AngleLerp(spinRotation, condemnationSpinInterpolant);

            // Create puffs of energy at the tip of Condemnation after the spin completes.
            if (condemnationRef != null && attackTimer >= condemnationSpinTime && attackTimer < chargeupTime &&
                attackTimer % condemnationChargePuffRate == condemnationChargePuffRate - 1f)
            {
                // Play a sound for additional notification that an arrow has been loaded.
                SoundEngine.PlaySound(SoundID.Item108 with
                {
                    Volume = 0.3f
                });
                Vector2 condemnationTip = condemnationRef.ModProjectile<CondemnationProj>().TipPosition;
                for (int i = 0; i < 36; i++)
                {
                    Dust chargeMagic = Dust.NewDustPerfect(condemnationTip, 267);
                    chargeMagic.velocity = (TwoPi * i / 36f).ToRotationVector2() * 5f + npc.velocity;
                    chargeMagic.scale = Main.rand.NextFloat(1f, 1.5f);
                    chargeMagic.color = Color.Violet;
                    chargeMagic.noGravity = true;
                }
            }

            // Spawn darts around the player that slowly converge inward in later phases, to incentivize not sitting in the same place.
            // These darts spawn behind the player if they are moving, to prevent telefrags, and adjust their speed so that they reach their destination
            // just before the attack concludes.
            if (dartConvergence && attackTimer == chargeupTime)
            {
                SoundEngine.PlaySound(SCalBoss.BrimstoneShotSound, target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 dartSpawnOffset = (TwoPi * i / 6f).ToRotationVector2() * 650f - target.velocity * 15f;
                        Vector2 dartShootVelocity = -dartSpawnOffset.SafeNormalize(Vector2.UnitY) * dartSpawnOffset.Length() / fanShootTime * 0.4f;
                        Utilities.NewProjectileBetter(target.Center + dartSpawnOffset, dartShootVelocity, ModContent.ProjectileType<BrimstoneBarrage>(), BrimstoneDartDamage, 0f);

                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(telegraph =>
                        {
                            telegraph.ModProjectile<DemonicTelegraphLine>().DontMakeProjectile = true;
                            telegraph.ModProjectile<DemonicTelegraphLine>().BombRadius = 0f;
                        });
                        Utilities.NewProjectileBetter(target.Center, dartShootVelocity * 0.001f, ModContent.ProjectileType<DemonicTelegraphLine>(), 0, 0f, -1, 0f, fanShootTime);
                    }
                    npc.netUpdate = true;
                }
            }

            // Release arrows from condemnation's tip once ready to fire.
            if (condemnationRef != null && fanCompletionInterpolant > 0f && attackTimer % shootRate == shootRate - 1f)
            {
                SoundEngine.PlaySound(SoundID.Item73, handPosition);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 shootVelocity = condemnationRef.rotation.ToRotationVector2() * shootSpeed;
                    Utilities.NewProjectileBetter(condemnationRef.ModProjectile<CondemnationProj>().TipPosition, shootVelocity, ModContent.ProjectileType<CondemnationArrowSCal>(), CondemnationArrowDamage, 0f);
                }
            }

            // Decide when to transition to the next attack.
            if (fanCompletionInterpolant >= 1f)
            {
                playerAimLockonDirection = 0f;
                shootCounter++;

                if (shootCounter >= shootCount)
                {
                    Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<CondemnationProj>());
                    SelectNextAttack(npc);
                }
                else
                    attackTimer = condemnationSpinTime;

                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_ExplosiveCharges(NPC npc, Player target, int currentPhase, bool inBerserkPhase, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)
        {
            int chargeDelay = 132;
            int chargeTime = 45;
            int chargeCount = 6;
            int explosionDelay = 120;
            float chargeSpeed = 43f;
            float bombShootSpeed = 20f;
            float bombExplosionRadius = 1020f;
            ref float chargeCounter = ref npc.Infernum().ExtraAI[0];

            if (currentPhase >= 1)
            {
                chargeTime -= 4;
                chargeSpeed += 4.5f;
                bombShootSpeed *= 0.75f;
            }

            if (currentPhase >= 3)
            {
                explosionDelay -= 18;
                chargeSpeed += 3.5f;
            }

            if (inBerserkPhase)
            {
                chargeCount--;
                explosionDelay -= 45;
                chargeSpeed += 9.5f;
            }

            if (Enraged)
            {
                explosionDelay = 40;
                chargeSpeed += 20f;
                bombExplosionRadius += 1250f;
            }

            // Use the updraft animation.
            frameChangeSpeed = 0.2f;
            frameType = (int)SCalFrameType.UpwardDraft;

            // Do damage.
            npc.damage = npc.defDamage;

            // Hover near the target and have the shield laugh at the target before charging.
            if (attackTimer < chargeDelay)
            {
                ShieldOpacity = Clamp(ShieldOpacity + 0.1f, 0f, 1f);
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 500f, -270f) - npc.velocity;

                npc.Center = npc.Center.MoveTowards(hoverDestination, 10f);
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 28f, 1.1f);

                // Aim the shield and use laughing frames.
                float idealRotation = npc.AngleTo(target.Center);
                ShieldRotation = ShieldRotation.AngleLerp(idealRotation, 0.125f);
                ShieldRotation = ShieldRotation.AngleTowards(idealRotation, 0.18f);
                npc.localAI[3] = 1f;
            }

            // Give a tip.
            if (attackTimer == chargeDelay)
                HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.SCalFireChargeTip");

            // Charge rapid-fire.
            if (attackTimer >= chargeDelay)
            {
                if ((attackTimer - chargeDelay) % chargeTime == 0f)
                {
                    npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                    npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                    ShieldRotation = npc.AngleTo(target.Center);

                    SoundEngine.PlaySound(SCalBoss.DashSound, npc.Center);

                    // Release a bomb and gigablast at the target.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int bombLifetime = (int)(chargeDelay + chargeTime * chargeCount - attackTimer) + explosionDelay;
                        Vector2 bombShootVelocity = npc.SafeDirectionTo(target.Center) * bombShootSpeed;

                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(bomb =>
                        {
                            bomb.timeLeft = bombLifetime;
                        });
                        Utilities.NewProjectileBetter(npc.Center, bombShootVelocity, ModContent.ProjectileType<DemonicBomb>(), GigablastDamage, 0f, -1, bombExplosionRadius);

                        if (chargeCounter % 3f == 2f)
                            Utilities.NewProjectileBetter(npc.Center, bombShootVelocity.RotatedByRandom(0.4f) * 0.5f, ModContent.ProjectileType<InfernumBrimstoneGigablast>(), GigablastDamage, 0f);

                        chargeCounter++;
                        npc.netUpdate = true;
                    }
                }

                // Slow down a bit after charging.
                else
                {
                    npc.velocity *= 0.9835f;

                    // Creation motion blur particles.
                    if (Main.rand.NextBool(4) && npc.velocity.Length() > 8.5f)
                    {
                        Vector2 energySpawnPosition = npc.Center + Main.rand.NextVector2Circular(32f, 32f) + npc.velocity * 3.5f;
                        Vector2 energyVelocity = -npc.velocity.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(6f, 10f);
                        Particle energyLeak = new SquishyLightParticle(energySpawnPosition, energyVelocity, Main.rand.NextFloat(0.55f, 0.9f), Color.OrangeRed, 30, 3.4f, 4.5f, hueShift: Main.rand.NextFloat(0.002f, 0.011f));
                        GeneralParticleHandler.SpawnParticle(energyLeak);
                    }
                }
            }

            if (attackTimer >= chargeDelay + chargeTime * chargeCount)
            {
                ShieldRotation = 0f;
                ShieldOpacity = 0f;
                SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_HellblastBarrage(NPC npc, Player target, int currentPhase, bool inBerserkPhase, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)
        {
            int shootDelay = 105;
            int hellblastShootRate = 14;
            int verticalBobPeriod = 56;
            int shootTime = 360;
            int endOfAttackShootBlockTime = 90;
            int dartBurstPeriod = 5;
            int dartCount = 7;
            float dartSpread = 0.45f;
            float dartSpeed = 8.4f;
            float horizontalOffset = 700f;
            float verticalBobAmplitude = 330f;
            float hoverSpeedFactor = Utils.Remap(attackTimer, 0f, shootDelay * 0.65f, 0.36f, 1f);
            if (currentPhase >= 1)
            {
                hellblastShootRate -= 2;
                verticalBobAmplitude += 20f;
                dartSpeed += 1.36f;
            }

            if (currentPhase >= 3)
            {
                verticalBobAmplitude += 50f;
                dartSpeed += 2.5f;
            }

            if (inBerserkPhase)
            {
                hellblastShootRate -= 3;
                verticalBobAmplitude += 50f;
                dartSpeed += 2.6f;
            }

            if (Enraged)
            {
                dartSpread = 1.13f;
                dartCount += 30;
                dartSpeed += 13.5f;
            }

            if (NPC.AnyNPCs(ModContent.NPCType<SoulSeekerSupreme>()))
                horizontalOffset += 120f;

            bool hasBegunFiring = attackTimer >= shootDelay;
            Vector2 handPosition = CalculateHandPosition();
            ref float shootCounter = ref npc.Infernum().ExtraAI[0];

            // Hover to the side of the target. Once she begins firing, SCal bobs up and down.
            npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
            Vector2 hoverDestination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * horizontalOffset;
            if (hasBegunFiring)
                hoverDestination.Y += Sin((attackTimer - shootDelay) * Pi / verticalBobPeriod) * verticalBobAmplitude;

            Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * hoverSpeedFactor * MathF.Min(npc.Distance(hoverDestination), 32f);
            npc.SimpleFlyMovement(idealVelocity, hoverSpeedFactor * 2.25f);
            npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.1f);

            // Use the magic cast animation when firing and a magic circle prior to that, as a charge-up effect.
            frameChangeSpeed = 0.2f;
            frameType = (int)(hasBegunFiring ? SCalFrameType.OutwardHandCast : SCalFrameType.MagicCircle);

            // Create an explosion effect prior to firing.
            if (attackTimer == shootDelay)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.CalamitousEnergyBurstSound, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(explosion =>
                    {
                        explosion.ModProjectile<DemonicExplosion>().MaxRadius = 300f;
                    });
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<DemonicExplosion>(), 0, 0f);
                }
            }

            // Release a burst of magic dust along with a brimstone hellblast skull once firing should happen.
            if (hasBegunFiring && attackTimer % hellblastShootRate == hellblastShootRate - 1f && attackTimer < shootDelay + shootTime)
            {
                SoundEngine.PlaySound(SCalBoss.HellblastSound, npc.Center);

                for (int i = 0; i < 25; i++)
                {
                    Dust brimstoneMagic = Dust.NewDustPerfect(handPosition, 264);
                    brimstoneMagic.velocity = npc.SafeDirectionTo(target.Center).RotatedByRandom(0.31f) * Main.rand.NextFloat(3f, 8f) + npc.velocity;
                    brimstoneMagic.scale = Main.rand.NextFloat(1.25f, 1.35f);
                    brimstoneMagic.noGravity = true;
                    brimstoneMagic.color = Color.OrangeRed;
                    brimstoneMagic.fadeIn = 1.5f;
                    brimstoneMagic.noLight = true;
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 projectileVelocity = (npc.SafeDirectionTo(target.Center) * new Vector2(1f, 0.1f)).SafeNormalize(Vector2.UnitY) * 15f;
                    Vector2 hellblastSpawnPosition = npc.Center + projectileVelocity * 0.4f;
                    int projectileType = ModContent.ProjectileType<BrimstoneHellblast>();
                    Utilities.NewProjectileBetter(hellblastSpawnPosition, projectileVelocity, projectileType, BrimstoneHellblastDamage, 0f, Main.myPlayer);

                    // Release a burst of darts after a certain number of hellblasts have been fired.
                    if (shootCounter % dartBurstPeriod == dartBurstPeriod - 1f)
                    {
                        for (int i = 0; i < dartCount; i++)
                        {
                            float dartOffsetAngle = Lerp(-dartSpread, dartSpread, i / (float)(dartCount - 1f));
                            Vector2 dartVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(dartOffsetAngle) * dartSpeed;
                            Utilities.NewProjectileBetter(npc.Center, dartVelocity, ModContent.ProjectileType<BrimstoneBarrage>(), BrimstoneDartDamage, 0f, Main.myPlayer);
                        }
                    }

                    shootCounter++;
                }
            }

            if (attackTimer >= shootDelay + shootTime + endOfAttackShootBlockTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_DarkMagicBombWalls(NPC npc, Player target, int currentPhase, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)
        {
            int chargeupTime = HeresyProjSCal.ChargeupTime;
            int cindersPerBurst = 3;
            int shootRate = 20;
            int shootTime = 330;
            int endOfAttackShootBlockTime = 90;
            int bombReleasePeriod = 3;
            int totalBombsToShootPerBurst = 13;
            int telegraphReleaseRate = 7;
            float totalBombOffset = 1800f;
            float shootSpeed = 2.7f;
            float bombExplosionRadius = 1080f;

            if (currentPhase >= 3)
            {
                cindersPerBurst += 2;
                shootRate -= 4;
                totalBombOffset += 300f;
            }

            if (Enraged)
            {
                cindersPerBurst = 15;
                shootRate = 5;
            }

            int bombShootDelay = shootRate * bombReleasePeriod;
            int telegraphTime = totalBombsToShootPerBurst * telegraphReleaseRate;
            int bombShootTime = telegraphTime + 16;
            float wrappedBombShootTimer = (attackTimer - chargeupTime) % (bombShootDelay + bombShootTime);
            bool hasBegunFiring = attackTimer >= chargeupTime;
            Vector2 handPosition = CalculateHandPosition();
            ref float shootCounter = ref npc.Infernum().ExtraAI[0];
            ref float bombFireOffsetAngle = ref npc.Infernum().ExtraAI[1];
            ref float bombFirePositionX = ref npc.Infernum().ExtraAI[2];
            ref float bombFirePositionY = ref npc.Infernum().ExtraAI[3];

            // Hover to the side of the target.
            npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
            Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 600f, -350f);
            if (!npc.WithinRange(hoverDestination, 150f))
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 32f, 1.5f);

            // Create Heresy on the first frame.
            if (attackTimer == 1f)
            {
                SoundEngine.PlaySound(SoundID.DD2_EtherianPortalDryadTouch, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<HeresyProjSCal>(), 0, 0f);
            }

            // Use the updraft animation when firing and a magic circle prior to that, as a charge-up effect.
            frameChangeSpeed = 0.2f;
            frameType = (int)(hasBegunFiring ? SCalFrameType.UpwardDraft : SCalFrameType.MagicCircle);

            // Create an explosion effect prior to firing.
            if (attackTimer == chargeupTime)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.CalamitousEnergyBurstSound, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(explosion =>
                    {
                        explosion.ModProjectile<DemonicExplosion>().MaxRadius = 300f;
                    });
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<DemonicExplosion>(), 0, 0f);
                }
            }

            // Release bursts of cinders.
            if (hasBegunFiring && attackTimer % shootRate == shootRate - 1f && attackTimer < chargeupTime + shootTime)
            {
                SoundEngine.PlaySound(SCalBoss.HellblastSound, npc.Center);
                float cinderSpawnOffsetAngle = Main.rand.NextFloat(TwoPi);
                for (int i = 0; i < cindersPerBurst; i++)
                {
                    Vector2 shootOffset = (TwoPi * i / cindersPerBurst + cinderSpawnOffsetAngle).ToRotationVector2() * 1000f;
                    Vector2 cinderShootVelocity = shootOffset.SafeNormalize(Vector2.UnitY) * -shootSpeed;

                    for (int j = 0; j < 150; j++)
                    {
                        Vector2 dustSpawnPosition = Vector2.Lerp(handPosition, target.Center + shootOffset, j / 149f);
                        Dust fire = Dust.NewDustPerfect(dustSpawnPosition, 267);
                        fire.velocity = Vector2.Zero;
                        fire.scale = 1.1f;
                        fire.alpha = 128;
                        fire.color = Color.Red;
                        fire.noGravity = true;
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(target.Center + shootOffset, cinderShootVelocity, ModContent.ProjectileType<AcceleratingDarkMagicFlame>(), DarkMagicFlameDamage, 0f);
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    shootCounter++;
                    npc.netUpdate = true;
                    npc.netSpam = 0;
                }
            }

            // Release bombs from the side, starting with telegraph lines.
            if (hasBegunFiring && wrappedBombShootTimer >= bombShootDelay)
            {
                // Initialize the bomb firing angle.
                if (wrappedBombShootTimer == bombShootDelay)
                {
                    do
                        bombFireOffsetAngle = TwoPi * Main.rand.NextFloat(8) / 8f;
                    while (bombFireOffsetAngle.ToRotationVector2().AngleBetween(target.velocity) < 0.91f);
                    bombFirePositionX = target.Center.X + Cos(bombFireOffsetAngle) * 1150f;
                    bombFirePositionY = target.Center.Y + Sin(bombFireOffsetAngle) * 1150f;
                    npc.netUpdate = true;
                }

                // Create telegraph lines.
                if (wrappedBombShootTimer <= bombShootDelay + telegraphTime && wrappedBombShootTimer % telegraphReleaseRate == telegraphReleaseRate - 1f)
                {
                    SoundEngine.PlaySound(SCalBoss.BrimstoneShotSound with
                    {
                        Volume = 0.67f
                    }, target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float bombFireOffset = Lerp(-totalBombOffset, totalBombOffset, Utils.GetLerpValue(0f, telegraphTime, wrappedBombShootTimer - bombShootDelay)) * 0.5f;
                        Vector2 bombShootPosition = new Vector2(bombFirePositionX, bombFirePositionY) + (bombFireOffsetAngle + PiOver2).ToRotationVector2() * bombFireOffset;
                        Vector2 telegraphDirection = bombFireOffsetAngle.ToRotationVector2() * -0.001f;

                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(telegraph =>
                        {
                            telegraph.ModProjectile<DemonicTelegraphLine>().BombRadius = bombExplosionRadius;
                        });
                        Utilities.NewProjectileBetter(bombShootPosition, telegraphDirection, ModContent.ProjectileType<DemonicTelegraphLine>(), 0, 0f, -1, 0f, 45f);
                    }
                }
            }

            if (attackTimer >= chargeupTime + shootTime + endOfAttackShootBlockTime)
            {
                Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<HeresyProjSCal>());
                SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_FireLaserSpin(NPC npc, Player target, int currentPhase, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)
        {
            int hoverTime = 120;
            int orbCastDelay = 45;
            int orbGrowDelay = 25;
            int orbGrowTime = 45;
            int orbAttackTime = 320;
            int gigablastShootRate = 75;
            float smallOrbSize = 12f;
            float bigOrbSize = 525f;
            float gigablastSpeed = 10.5f;
            Vector2 handPosition = CalculateHandPosition();
            Vector2 orbSummonSpawnPosition = npc.Center + Vector2.UnitY * 8f;

            if (currentPhase >= 3)
            {
                gigablastShootRate -= 18;
                gigablastSpeed += 1.25f;
            }

            if (Enraged)
            {
                gigablastShootRate = 18;
                gigablastSpeed += 25f;
            }

            ref float orbSize = ref npc.Infernum().ExtraAI[0];
            ref float fadeAwayInterpolant = ref npc.Infernum().ExtraAI[1];

            // Hover in place at first before slowing down.
            Vector2 hoverDestination = npc.Infernum().Arena.Center.ToVector2();
            if (attackTimer < hoverTime && !npc.WithinRange(hoverDestination, 100f))
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(hoverDestination) * 15f, 0.1f);
            else
                npc.velocity *= 0.92f;

            // Update the frame change speed and base type.
            frameChangeSpeed = 0.2f;
            frameType = (int)SCalFrameType.UpwardDraft;

            // Initialize the orb size.
            if (attackTimer >= hoverTime && attackTimer <= hoverTime + orbCastDelay)
                orbSize = smallOrbSize;

            // Create the orb.
            if (attackTimer == hoverTime + orbCastDelay)
            {
                // Give a tip.
                HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.SCalFireSpinTip");

                SoundEngine.PlaySound(SoundID.Item163, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Utilities.NewProjectileBetter(orbSummonSpawnPosition, Vector2.Zero, ModContent.ProjectileType<BrimstoneFlameOrb>(), 0, 0f, -1, 0f, npc.whoAmI);
                    npc.netUpdate = true;
                }
            }

            // Rise upward.
            if (attackTimer == hoverTime + orbCastDelay + orbGrowDelay + 10f)
            {
                CreditManager.StartRecordingFootageForCredits(ScreenCapturer.RecordingBoss.SCal);
                npc.velocity = -Vector2.UnitY * 35f;
            }

            // Make the orb grow.
            if (attackTimer >= hoverTime + orbCastDelay + orbGrowDelay)
                orbSize = SmoothStep(smallOrbSize, bigOrbSize, Utils.GetLerpValue(0f, orbGrowTime, attackTimer - (hoverTime + orbCastDelay + orbGrowDelay), true));

            // Eventually make the light orb fade away.
            fadeAwayInterpolant = Utils.GetLerpValue(0f, 60f, attackTimer - (hoverTime + orbCastDelay + orbGrowDelay + orbGrowTime + BrimstoneFlameOrb.LaserReleaseDelay + orbAttackTime), true);

            // Release gigablasts.
            if (attackTimer >= hoverTime + orbCastDelay + orbGrowDelay + orbGrowTime)
            {
                frameChangeSpeed = 0.135f;
                frameType = (int)SCalFrameType.BlastPunchCast;
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                if (attackTimer % gigablastShootRate == gigablastShootRate - 1f)
                {
                    SoundEngine.PlaySound(SCalBoss.BrimstoneBigShotSound, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 shootVelocity = (target.Center - handPosition).SafeNormalize(Vector2.UnitY) * gigablastSpeed;
                        Utilities.NewProjectileBetter(handPosition, shootVelocity, ModContent.ProjectileType<InfernumBrimstoneGigablast>(), 500, 0f);
                    }
                }
            }

            if (attackTimer >= hoverTime + orbCastDelay + orbGrowDelay + orbGrowTime + BrimstoneFlameOrb.LaserReleaseDelay + orbAttackTime + 120f)
            {
                Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<BrimstoneFlameOrb>(), ModContent.ProjectileType<InfernumBrimstoneGigablast>());
                SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_BecomeBerserk(NPC npc, Player target, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)
        {
            int transitionTime = 95;

            // Slow down and use the magic circle frame effect.
            frameChangeSpeed = 0.2f;
            frameType = (int)SCalFrameType.MagicCircle;
            npc.velocity *= 0.95f;

            // Create mild screen-shake effects.
            float playerDistanceInterpolant = Utils.GetLerpValue(2400f, 1250f, npc.Distance(Main.LocalPlayer.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = playerDistanceInterpolant * attackTimer / transitionTime * 20f;
            npc.ai[3] = attackTimer / transitionTime;

            if (attackTimer >= transitionTime)
            {
                Vector2 teleportPosition = target.Center - Vector2.UnitY * 450f;
                Dust.QuickDustLine(npc.Center, teleportPosition, 300f, Color.Red);
                npc.Center = teleportPosition;

                SoundEngine.PlaySound(InfernumSoundRegistry.CalThunderStrikeSound, npc.Center);
                SoundEngine.PlaySound(SCalBoss.SpawnSound, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(explosion =>
                    {
                        explosion.ModProjectile<DemonicExplosion>().MaxRadius = 500f;
                    });
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<DemonicExplosion>(), 0, 0f);

                    // The SelectNextAttack call fires a netUpdate, hence why one is not present here.
                    npc.ai[3] = 1f;
                }
                SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_SummonSuicideBomberDemons(NPC npc, Player target, int currentPhase, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)
        {
            int demonSummonRate = 6;
            int demonSummonCount = 6;
            int dartShootRate = 32;
            int dartCount = 5;
            float dartSpeed = 7.5f;
            if (currentPhase >= 1)
            {
                dartCount += 2;
                dartShootRate -= 5;
                demonSummonCount += 2;
            }

            if (Enraged)
            {
                dartCount = 17;
                dartShootRate = 9;
                dartSpeed = 30f;
            }

            int castTime = demonSummonRate * demonSummonCount + SuicideBomberRitual.Lifetime + 45;
            Vector2 handPosition = CalculateHandPosition();
            bool doneAttacking = attackTimer >= castTime + SuicideBomberDemonHostile.AttackDuration;
            ref float demonCircleCounter = ref npc.Infernum().ExtraAI[0];
            ref float dartShootCounter = ref npc.Infernum().ExtraAI[1];
            ref float hoverOffsetDirection = ref npc.Infernum().ExtraAI[2];

            // Define the frame change speed.
            frameChangeSpeed = 0.2f;

            if (NPC.AnyNPCs(ModContent.NPCType<SoulSeekerSupreme>()))
            {
                SelectNextAttack(npc);
                return;
            }

            // Cast a bunch of magic circles.
            if (attackTimer < castTime)
            {
                // Slow down and use the magic circle frame effect.
                frameType = (int)SCalFrameType.MagicCircle;
                npc.velocity *= 0.925f;

                // Create some magic at the position of SCal's hands.
                Dust darkMagic = Dust.NewDustPerfect(handPosition, 267);
                darkMagic.color = Color.Lerp(Color.Red, Color.Violet, Main.rand.NextFloat(0.81f));
                darkMagic.noGravity = true;

                if (demonCircleCounter < demonSummonCount && attackTimer % demonSummonRate == demonSummonRate - 1f)
                {
                    Vector2 circleSpawnPosition = handPosition + (TwoPi * demonCircleCounter / demonSummonCount).ToRotationVector2() * 225f;

                    // Create the ritual circle.
                    Dust.QuickDustLine(handPosition, circleSpawnPosition, 45f, Color.Red);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Utilities.NewProjectileBetter(circleSpawnPosition, Vector2.Zero, ModContent.ProjectileType<SuicideBomberRitual>(), 0, 0f);
                        demonCircleCounter++;
                        npc.netUpdate = true;
                    }
                }
                return;
            }

            // Attack the player while the suicide bombers chase them.
            if (!doneAttacking)
                frameType = (int)SCalFrameType.OutwardHandCast;
            if (attackTimer % dartShootRate == dartShootRate - 1f && !doneAttacking && !npc.WithinRange(target.Center, 320f))
            {
                SoundEngine.PlaySound(SCalBoss.BrimstoneShotSound, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < dartCount; i++)
                    {
                        float dartOffsetAngle = Lerp(-0.45f, 0.45f, i / (float)(dartCount - 1f));
                        Vector2 dartVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(dartOffsetAngle) * dartSpeed;
                        Utilities.NewProjectileBetter(npc.Center, dartVelocity, ModContent.ProjectileType<BrimstoneBarrage>(), BrimstoneDartDamage, 0f, Main.myPlayer);
                    }
                    dartShootCounter++;
                    npc.netUpdate = true;
                }

                // Switch directions.
                if (dartShootCounter % 6f == 5f)
                {
                    hoverOffsetDirection *= -1f;
                    SoundEngine.PlaySound(InfernumSoundRegistry.CalThunderStrikeSound, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        npc.velocity *= 0.3f;
                        npc.Center = target.Center + new Vector2(hoverOffsetDirection * 600f, -300f);

                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(explosion =>
                        {
                            explosion.ModProjectile<DemonicExplosion>().MaxRadius = 300f;
                        });
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<DemonicExplosion>(), 0, 0f);
                    }
                }
            }

            // Initialize the hover offset.
            if (hoverOffsetDirection == 0f)
                hoverOffsetDirection = (target.Center.X < npc.Center.X).ToDirectionInt();

            // Hover to the side of the target. Once she begins firing, SCal bobs up and down.
            npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
            Vector2 hoverDestination = target.Center + new Vector2(hoverOffsetDirection * 600f, -300f);
            if (!npc.WithinRange(hoverDestination, 100f))
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 32f, 1.5f);

            if (attackTimer >= castTime + SuicideBomberDemonHostile.AttackDuration + 90f)
            {
                Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<SuicideBomberDemonHostile>());
                SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_BrimstoneJewelBeam(NPC npc, Player target, int currentPhase, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)
        {
            int jewelChargeupTime = BrimstoneJewelProj.ChargeupTime;
            int laserbeamLifetime = BrimstoneLaserbeam.Lifetime;
            int dartReleaseRate = 8;
            int bombReleaseRate = 60;
            int ritualCreationRate = 85;
            float dartShootSpeed = 16f;
            float bombExplosionRadius = 1100f;
            float spinArc = TwoPi * 1.25f;
            Vector2 handPosition = CalculateHandPosition();

            if (currentPhase >= 2)
            {
                dartReleaseRate -= 2;
                bombReleaseRate -= 10;
                spinArc *= 1.3f;
            }

            if (Enraged)
            {
                dartReleaseRate = 3;
                dartShootSpeed = 38f;
                bombExplosionRadius += 3000f;
            }

            ref float brimstoneJewelIndex = ref npc.Infernum().ExtraAI[0];
            ref float spinDirection = ref npc.Infernum().ExtraAI[1];
            ref float laserDirection = ref npc.Infernum().ExtraAI[2];

            // Define the projectile as a convenient reference type variable, for easy manipulation of its attributes.
            Projectile jewelRef = Main.projectile[(int)brimstoneJewelIndex];
            if (jewelRef.type != ModContent.ProjectileType<BrimstoneJewelProj>())
                jewelRef = null;

            // Use the hands out casting animation.
            frameChangeSpeed = 0.25f;
            frameType = (int)SCalFrameType.BlastCast;

            // Move towards the center of the arena.
            Vector2 arenaCenter = npc.Infernum().Arena.Center.ToVector2();
            if (!npc.WithinRange(arenaCenter, 400f))
                npc.Center = Vector2.Lerp(npc.Center, arenaCenter, 0.025f).MoveTowards(arenaCenter, 6f);

            // Create the jewel on the first frame.
            if (attackTimer == 1f)
            {
                // Create some chargeup dust and play a charge sound.
                SoundEngine.PlaySound(SoundID.DD2_DarkMageHealImpact, target.Center);
                for (int i = 0; i < 15; i++)
                {
                    Dust magic = Dust.NewDustPerfect(handPosition, 267);
                    magic.color = Color.Lerp(Color.Red, Color.Purple, Main.rand.NextFloat());
                    magic.velocity = Main.rand.NextVector2Circular(5f, 5f);
                    magic.scale = Main.rand.NextFloat(1f, 1.25f);
                    magic.noGravity = true;
                }

                // Clear old entities.
                ClearAllEntities();

                // Teleport to the center of the arena.
                npc.Center = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * 600f;
                npc.velocity = Vector2.Zero;
                SoundEngine.PlaySound(InfernumSoundRegistry.CalThunderStrikeSound, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(explosion =>
                    {
                        explosion.ModProjectile<DemonicExplosion>().MaxRadius = 300f;
                    });
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<DemonicExplosion>(), 0, 0f);
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                    brimstoneJewelIndex = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<BrimstoneJewelProj>(), 0, 0f);
                    npc.netUpdate = true;
                }
            }

            // Adjust the jewel's rotation and create particles.
            if (attackTimer < jewelChargeupTime && jewelRef != null)
            {
                float angularTurnSpeed = Utils.Remap(attackTimer, 0f, jewelChargeupTime * 0.67f, Pi / 45f, Pi / 355f);
                jewelRef.rotation = jewelRef.rotation.AngleTowards(jewelRef.AngleTo(target.Center), angularTurnSpeed);

                float fireParticleScale = Main.rand.NextFloat(1f, 1.25f);
                Color fireColor = Color.Lerp(Color.Red, Color.Violet, Main.rand.NextFloat());
                Vector2 fireParticleSpawnPosition = handPosition + Main.rand.NextVector2Unit() * Main.rand.NextFloat(40f, 200f);
                Vector2 fireParticleVelocity = (handPosition - fireParticleSpawnPosition) * 0.03f;
                SquishyLightParticle chargeFire = new(fireParticleSpawnPosition, fireParticleVelocity, fireParticleScale, fireColor, 50);
                GeneralParticleHandler.SpawnParticle(chargeFire);
            }

            // Create the laserbeam.
            if (jewelRef != null && attackTimer == jewelChargeupTime)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.CalamitousEnergyBurstSound with
                {
                    Pitch = 0.2f
                }, npc.Center);

                // Store the jewel's rotation.
                laserDirection = jewelRef.rotation;

                Vector2 aimDirection = (jewelRef.rotation + PiOver2).ToRotationVector2();
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center, aimDirection, ModContent.ProjectileType<BrimstoneLaserbeam>(), RancorLaserbeamDamage, 0f);
            }

            // Make the laserbeam spin after it's created.
            // Also release bursts of bombs and darts rapid-fire.
            else if (jewelRef != null && attackTimer > jewelChargeupTime)
            {
                // Initialize the spin direction.
                if (spinDirection == 0f)
                {
                    spinDirection = (WrapAngle(jewelRef.AngleTo(target.Center) - jewelRef.rotation) > 0f).ToDirectionInt();
                    npc.netUpdate = true;
                }

                jewelRef.rotation += spinArc / laserbeamLifetime * spinDirection;
                npc.spriteDirection = (Math.Cos(jewelRef.rotation) < 0f).ToDirectionInt();

                // Store the jewel's rotation.
                laserDirection = jewelRef.rotation;

                // Release darts.
                if (attackTimer % dartReleaseRate == dartReleaseRate - 1f)
                {
                    SoundEngine.PlaySound(SCalBoss.BrimstoneShotSound, target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 dartVelocity = npc.SafeDirectionTo(target.Center) * dartShootSpeed;
                        Utilities.NewProjectileBetter(npc.Center, dartVelocity, ModContent.ProjectileType<BrimstoneBarrage>(), 500, 0f, Main.myPlayer);
                    }
                }

                // Release bombs.
                if (attackTimer % bombReleaseRate == bombReleaseRate - 1f)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 bombShootVelocity = npc.SafeDirectionTo(target.Center + target.velocity * 35f) * dartShootSpeed * 0.7f;

                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(bomb =>
                        {
                            bomb.timeLeft = 120;
                        });
                        Utilities.NewProjectileBetter(npc.Center, bombShootVelocity, ModContent.ProjectileType<DemonicBomb>(), 0, 0f, -1, bombExplosionRadius);
                    }
                }

                // Summon rituals.
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % ritualCreationRate == ritualCreationRate - 1f)
                    Utilities.NewProjectileBetter(target.Center - target.velocity * 5f, Vector2.Zero, ModContent.ProjectileType<SuicideBomberRitual>(), 0, 0f);
            }

            if (attackTimer >= jewelChargeupTime + laserbeamLifetime || Main.netMode != NetmodeID.SinglePlayer)
            {
                Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<BrimstoneJewelProj>(), ModContent.ProjectileType<BrimstoneLaserbeam>());
                SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_SummonShadowDemon(NPC npc, Player target, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)
        {
            int fadeInTime = 120;
            int blackScreenTime = 50;
            int attackTransitionDelay = 100;
            int fadeOutTime = 30;

            // Darken the screen.
            InfernumMode.BlackFade = Utils.GetLerpValue(0f, fadeInTime, attackTimer, true) * Utils.GetLerpValue(fadeOutTime, 0f, attackTimer - fadeInTime - blackScreenTime, true);

            // Slow down and look at the target.
            npc.velocity *= 0.95f;
            if (npc.velocity.Length() < 8f)
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();

            // Disable contact damage.
            npc.damage = 0;
            npc.dontTakeDamage = true;

            // Use the magic circle animation, as a charge-up effect.
            frameChangeSpeed = 0.2f;
            frameType = (int)SCalFrameType.MagicCircle;

            // Play the demon's spawning sound.
            if (attackTimer == 1)
                SoundEngine.PlaySound(InfernumSoundRegistry.ShadowHydraSpawn, target.Center);

            // Summon the demon.
            if (attackTimer == fadeInTime + blackScreenTime)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.ShadowHydraCharge, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y - 850, ModContent.NPCType<ShadowDemon>(), npc.whoAmI);
            }

            if (attackTimer >= fadeInTime - blackScreenTime + attackTransitionDelay)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_SummonBrothers(NPC npc, Player target, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)
        {
            // Switch from the Grief section of Stained, Brutal Calamity to the Lament section.
            npc.ModNPC.Music = MusicID.Boss3;

            int screenShakeTime = 135;

            // Slow down and look at the target.
            npc.velocity *= 0.95f;
            if (npc.velocity.Length() < 8f)
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();

            // Disable contact damage.
            npc.damage = 0;
            npc.dontTakeDamage = true;

            // Use the magic circle animation, as a charge-up effect.
            frameChangeSpeed = 0.2f;
            frameType = (int)SCalFrameType.MagicCircle;

            // Shake the screen.
            float screenShakeDistanceFade = Utils.GetLerpValue(npc.Distance(target.Center), 2600f, 1375f, true);
            float screenShakeFactor = Utils.Remap(attackTimer, 25f, screenShakeTime, 2f, 12.5f) * screenShakeDistanceFade;
            if (attackTimer >= screenShakeTime)
                screenShakeFactor = 0f;

            target.Calamity().GeneralScreenShakePower = screenShakeFactor;

            // Play a summoning sound
            if (attackTimer == 1f)
                SoundEngine.PlaySound(InfernumSoundRegistry.SCalBrothersSpawnSound with
                {
                    Volume = 1.5f
                });

            // Create the portals.
            if (attackTimer == screenShakeTime - 50f)
            {
                SoundEngine.PlaySound(SoundID.Item103, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Utilities.NewProjectileBetter(npc.Center - Vector2.UnitX * 600f, Vector2.Zero, ModContent.ProjectileType<SupremeCalamitasBrotherPortal>(), 0, 0f, -1, ModContent.NPCType<SupremeCataclysm>());
                    Utilities.NewProjectileBetter(npc.Center + Vector2.UnitX * 600f, Vector2.Zero, ModContent.ProjectileType<SupremeCalamitasBrotherPortal>(), 0, 0f, -1, ModContent.NPCType<SupremeCatastrophe>());

                    npc.netUpdate = true;
                }
            }

            if (attackTimer >= screenShakeTime + SupremeCalamitasBrotherPortal.Lifetime && !NPC.AnyNPCs(ModContent.NPCType<SupremeCataclysm>()))
                SelectNextAttack(npc);
        }

        public static void DoBehavior_SummonSeekers(NPC npc, Player target, Vector2 handPosition, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)
        {
            int chargeupTime = 75;
            int vigilanceSpinTime = 36;
            int seekerSummonCount = 12;
            int seekerSummonRate = 5;
            int seekerSummonTime = seekerSummonRate * seekerSummonCount;
            float fanCompletionInterpolant = Utils.GetLerpValue(0f, seekerSummonTime, attackTimer - chargeupTime, true);
            ref float vigilanceIndex = ref npc.Infernum().ExtraAI[0];
            ref float seekersCanFire = ref npc.Infernum().ExtraAI[1];

            // Define the projectile as a convenient reference type variable, for easy manipulation of its attributes.
            Projectile vigilanceRef = Main.projectile[(int)vigilanceIndex];
            if (vigilanceRef.type != ModContent.ProjectileType<VigilanceProj>())
            {
                var staffs = Utilities.AllProjectilesByID(ModContent.ProjectileType<VigilanceProj>());
                if (staffs.Any())
                    vigilanceRef = staffs.First();
            }

            // Despawn leftover projectiles.
            if (attackTimer == 1f)
                ClearAllEntities();

            // Use the hands out casting animation.
            frameChangeSpeed = 0.27f;
            frameType = (int)SCalFrameType.BlastCast;

            // Reset animation values.
            ForcefieldScale = 1f;
            ShieldOpacity = 0f;
            ShieldRotation = 0f;

            // Disable damage.
            npc.damage = 0;
            npc.dontTakeDamage = true;

            // Slow down.
            npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
            npc.velocity *= 0.98f;

            // Create vigilance on the first frame and decide which direction the fan will go in.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 1f)
            {
                vigilanceIndex = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<VigilanceProj>(), 0, 0f);
                npc.netUpdate = true;
            }

            // Spin vigilance around before aiming it upward.
            float spinRotation = WrapAngle(Pi * attackTimer / vigilanceSpinTime * 10f);

            // Adjust vigilance's rotation.
            float vigilanceSpinInterpolant = Utils.GetLerpValue(vigilanceSpinTime + 10f, vigilanceSpinTime, attackTimer, true);
            if (vigilanceRef != null)
                vigilanceRef.rotation = (-PiOver2).AngleLerp(spinRotation, vigilanceSpinInterpolant) + TwoPi * fanCompletionInterpolant - PiOver4 + Pi;

            // Release bursts of energy from Vigilance's tip and summon a seeker.
            if (vigilanceRef != null && fanCompletionInterpolant > 0f && fanCompletionInterpolant < 1f && attackTimer % seekerSummonRate == 0f)
            {
                SoundEngine.PlaySound(SoundID.Item73, handPosition);

                bool outerSeeker = fanCompletionInterpolant > 0.5f;
                Vector2 seekerSpawnOffset = (Pi * fanCompletionInterpolant * 4f).ToRotationVector2() * (outerSeeker ? 1000f : 500f);
                Vector2 seekerSpawnPosition = npc.Center + seekerSpawnOffset;

                Dust.QuickDustLine(vigilanceRef.ModProjectile<VigilanceProj>().TipPosition, seekerSpawnPosition, 40f, Color.Red);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float seekerOffsetAngle = ToDegrees(seekerSpawnOffset.ToRotation() + Pi);
                    NPC.NewNPC(npc.GetSource_FromAI(), (int)seekerSpawnPosition.X, (int)seekerSpawnPosition.Y, ModContent.NPCType<SoulSeekerSupreme>(), npc.whoAmI, seekerOffsetAngle, 0f, outerSeeker.ToInt(), seekerOffsetAngle);
                }
            }

            // Allow seekers to fire once they've all been summoned.
            seekersCanFire = (fanCompletionInterpolant >= 1f).ToInt();
            if (fanCompletionInterpolant >= 1f && vigilanceRef.type == ModContent.ProjectileType<VigilanceProj>())
                vigilanceRef?.Kill();

            // Transition to the next attack once most of the seekers are dead.
            if (fanCompletionInterpolant >= 1f && NPC.CountNPCS(ModContent.NPCType<SoulSeekerSupreme>()) <= 3)
            {
                Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<VigilanceProj>());
                SelectNextAttack(npc);
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_SummonSepulcher(NPC npc, Player target, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)
        {
            int heartCount = 13;
            int animationDelay = 60;
            int heartSpinAnimationTime = 150;
            int focusTime = 92;
            int laserFadeoutTime = 90;
            int heartID = ModContent.ProjectileType<RitualBrimstoneHeart>();
            float maxHeartRadius = 135f;
            if (WorldSaveSystem.HasSepulcherAnimationBeenPlayed)
            {
                animationDelay = 30;
                heartSpinAnimationTime = 60;
                focusTime = 38;
            }

            ref float heartSpinAngle = ref npc.Infernum().ExtraAI[0];

            // Slow down and look at the target.
            npc.velocity *= 0.95f;
            if (npc.velocity.Length() < 8f)
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();

            // Disable contact damage.
            npc.damage = 0;
            npc.dontTakeDamage = true;

            // Use the magic circle animation, as a charge-up effect.
            frameChangeSpeed = 0.2f;
            frameType = (int)SCalFrameType.MagicCircle;

            if (attackTimer < animationDelay)
                return;

            float adjustedAttackTimer = attackTimer - animationDelay;
            float heartFadeout = Utils.GetLerpValue(laserFadeoutTime, 45f, adjustedAttackTimer - heartSpinAnimationTime - focusTime, true);
            float heartRadius = Utils.Remap(adjustedAttackTimer, 0f, animationDelay, 2f, maxHeartRadius) * heartFadeout;

            // Create the hearts.
            if (adjustedAttackTimer == 1f)
            {
                for (int i = 0; i < heartCount; i++)
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, heartID, 0, 0f, -1, i / (float)(heartCount - 1f));
                npc.netUpdate = true;
            }

            // Make the hearts spin.
            float focusInterpolant = Utils.GetLerpValue(0f, focusTime, adjustedAttackTimer - heartSpinAnimationTime, true);
            if (focusInterpolant <= 0f)
                heartSpinAngle += Utils.Remap(adjustedAttackTimer, 0f, heartSpinAnimationTime, 0.001f, Pi / 27f) * Utils.GetLerpValue(0f, -36f, adjustedAttackTimer - heartSpinAnimationTime);

            List<Projectile> brimstoneHearts = Main.projectile.Take(Main.maxProjectiles).Where(n => n.active && n.type == heartID).ToList();
            foreach (Projectile heart in brimstoneHearts)
            {
                float heartOffsetAngle = WrapAngle(TwoPi * heart.ai[0] + heartSpinAngle);
                if (heartOffsetAngle < 0f)
                    heartOffsetAngle += TwoPi;

                float focusAngle = -PiOver2 + Lerp(-0.87f, 0.87f, Sin(heart.ai[0] * TwoPi + attackTimer / 19f) * 0.5f + 0.5f);

                // Have hearts hover a fixed distance away from SCal.
                heart.Center = npc.Center + heartOffsetAngle.AngleLerp(focusAngle, focusInterpolant).ToRotationVector2() * heartRadius;
                heart.ai[1] = focusInterpolant;
                heart.Infernum().ExtraAI[0] = heartFadeout;
            }

            // Summon Sepulcher.
            if (adjustedAttackTimer == heartSpinAnimationTime + focusTime)
            {
                SoundEngine.PlaySound(SCalBoss.SepulcherSummonSound, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y - 2100, ModContent.NPCType<SepulcherHead>(), 1);
                    npc.netUpdate = true;
                }
            }

            if (adjustedAttackTimer >= heartSpinAnimationTime + focusTime + laserFadeoutTime + 50f)
            {
                Utilities.DeleteAllProjectiles(false, heartID);

                if (!NPC.AnyNPCs(ModContent.NPCType<SepulcherHead>()))
                {
                    for (int i = 0; i < 2; i++)
                        Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<BrimstoneBarrage>(), ModContent.ProjectileType<DemonicBomb>(), ModContent.ProjectileType<SepulcherBone>());
                    if (Main.netMode != NetmodeID.MultiplayerClient && !WorldSaveSystem.HasSepulcherAnimationBeenPlayed)
                    {
                        WorldSaveSystem.HasSepulcherAnimationBeenPlayed = true;
                        CalamityNetcode.SyncWorld();
                    }
                    SelectNextAttack(npc);
                }
            }
        }

        public static void DoBehavior_PhaseTransition(NPC npc, Player target, int currentPhase, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)
        {
            // Teleport above the player and delete all hostile projectiles on the first frame.
            if (attackTimer == 1f)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.CalThunderStrikeSound, npc.Center);
                SoundEngine.PlaySound(SCalBoss.SpawnSound, npc.Center);

                for (int i = 0; i < 4; i++)
                    ClearAllEntities();

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(explosion =>
                    {
                        explosion.ModProjectile<DemonicExplosion>().MaxRadius = 600f;
                    });
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<DemonicExplosion>(), 0, 0f);

                    npc.Infernum().ExtraAI[5] = 0f;
                    npc.Center = target.Center - Vector2.UnitY * 450f;
                    npc.velocity = Vector2.Zero;
                    npc.netUpdate = true;
                }
            }

            // Use the magic circle animation, as a charge-up effect.
            frameChangeSpeed = 0.2f;
            frameType = (int)SCalFrameType.MagicCircle;

            // Disable contact damage.
            npc.damage = 0;
            npc.dontTakeDamage = true;

            // Make the shield go away.
            ShieldOpacity = 0f;

            // Emit fire dust.
            for (int i = 0; i < 5; i++)
            {
                Dust brimstoneFire = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Square(-24f, 24f), (int)CalamityDusts.Brimstone);
                brimstoneFire.velocity = Vector2.UnitY * -Main.rand.NextFloat(2.75f, 4.25f);
                brimstoneFire.noGravity = true;
            }

            if (attackTimer >= 105f)
            {
                SelectNextAttack(npc);

                // Summon the Shadow Demon when entering the second phase.
                if (currentPhase == 1)
                    npc.ai[0] = (int)SCalAttackType.SummonShadowDemon;

                // Summon brothers when entering the third phase.
                if (currentPhase == 2)
                    npc.ai[0] = (int)SCalAttackType.SummonBrothers;

                // Summon seekers when entering the fourth phase.
                if (currentPhase == 3)
                {
                    npc.ModNPC.Music = MusicID.LunarBoss;
                    npc.ai[0] = (int)SCalAttackType.SummonSeekers;
                }

                // Transition to the desperation attack when entering the final phase.
                // Also get rid of the shadow hydra.
                if (currentPhase == 4f)
                {
                    int shadowHydraID = ModContent.NPCType<ShadowDemon>();
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (Main.npc[i].active && Main.npc[i].type == shadowHydraID)
                        {
                            Main.npc[i].active = false;
                            Main.npc[i].netUpdate = true;
                        }
                    }

                    npc.ai[0] = (int)SCalAttackType.DesperationPhase;
                }
            }
        }

        public static void DoBehavior_DesperationPhase(NPC npc, Player target, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)
        {
            int flamePillarReleaseRate = 16;
            int flamePillarSoulReleaseRate = 18;
            int flamePillarBarrageDuration = 720;
            float flamePillarHorizontalStep = 300f;

            int baseTeleportDelay = 38;
            int sitTime = 780;

            int bombBulletHellDuration = 636;
            int baseBombReleaseRate = 120;
            int dartReleaseRate = 56;
            int dartsPerBurst = 7;
            float dartSpread = 0.79f;
            float dartShootSpeed = 8.4f;
            float bombExplosionRadius = 1050f;

            int gigablastBulletHellDuration = 1050;
            int baseGigaReleaseRate = 75;
            int dartBulletHellReleaseRate = 60;
            float dartBulletHellWallArea = 420f;
            float dartBulletHellGapArea = 42f;

            ref float flamePillarHorizontalOffset = ref npc.Infernum().ExtraAI[0];
            ref float teleportCountdown = ref npc.Infernum().ExtraAI[1];
            ref float superfastTeleportCounter = ref npc.Infernum().ExtraAI[2];
            ref float bombReleaseCountdown = ref npc.Infernum().ExtraAI[3];
            ref float attackState = ref npc.Infernum().ExtraAI[4];
            ref float bombShootCounter = ref npc.Infernum().ExtraAI[7];
            ref float dartTelegraphDirection = ref npc.Infernum().ExtraAI[9];
            ref float dartBulletHellCounter = ref npc.Infernum().ExtraAI[10];
            ref float gigablastReleaseCountdown = ref npc.Infernum().ExtraAI[11];
            ref float gigablastShootCounter = ref npc.Infernum().ExtraAI[12];

            // Cope seethe mald and die.
            if (Enraged)
            {
                dartReleaseRate = 11;
                dartsPerBurst = 17;
                dartShootSpeed = 40f;

                // Kill the player if they leave the arena during the gigablast bullet hell.
                if (attackState == 2f)
                    target.KillMe(PlayerDeathReason.ByNPC(npc.whoAmI), 1000000, 1);
            }

            // Disable contact damage.
            npc.damage = 0;
            npc.dontTakeDamage = true;

            // Do frame stuff.
            frameChangeSpeed = 0.2f;

            // Look at the target.
            npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();

            // Become berserk.
            npc.ai[3] = 1f;

            // Release a bunch of flame pillars all throughout the arena, along with redirecting souls.
            if (attackState == 0f)
            {
                frameType = (int)SCalFrameType.MagicCircle;

                // Hover to the side of the target.
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 500f, -350f);
                if (!npc.WithinRange(hoverDestination, 150f))
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 32f, 1.5f);

                // Give a tip.
                if (attackTimer == 1f)
                    HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.SCalDesperationTip");

                if (attackTimer < flamePillarBarrageDuration - 60f)
                {
                    // Create the telegraphs. They will create the pillars once ready to explode.
                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % flamePillarReleaseRate == flamePillarReleaseRate - 1f)
                    {
                        flamePillarHorizontalOffset = (flamePillarHorizontalOffset + flamePillarHorizontalStep) % npc.Infernum().Arena.Width;
                        Vector2 flamePillarSpawnPosition = npc.Infernum().Arena.BottomLeft() + new Vector2(flamePillarHorizontalOffset, -10f);
                        Utilities.NewProjectileBetter(flamePillarSpawnPosition, Vector2.Zero, ModContent.ProjectileType<BrimstoneFlamePillarTelegraph>(), 0, 0f, -1, 60f);
                    }

                    // Release souls.
                    if (attackTimer % flamePillarSoulReleaseRate == flamePillarSoulReleaseRate - 1f)
                    {
                        SoundEngine.PlaySound(SoundID.NPCDeath52, npc.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 shootVelocity = -Vector2.UnitY.RotatedByRandom(0.73f) * Main.rand.NextFloat(8f, 17f);
                            Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<RedirectingLostSoulProj>(), LostSoulDamage, 0f, -1, 0f, 0.6f);
                        }
                    }
                }

                if (attackTimer >= flamePillarBarrageDuration)
                {
                    // Delete homing souls.
                    Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<RedirectingLostSoulProj>());

                    SoundEngine.PlaySound(InfernumSoundRegistry.CalThunderStrikeSound, npc.Center);

                    npc.Center = target.Center - Vector2.UnitY * 400f;
                    npc.velocity = Vector2.Zero;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(explosion =>
                        {
                            explosion.ModProjectile<DemonicExplosion>().MaxRadius = 700f;
                        });
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<DemonicExplosion>(), 0, 0f);
                    }

                    attackState = 1f;
                    attackTimer = 0f;
                    flamePillarHorizontalOffset = 0f;
                    npc.netUpdate = true;
                }
            }

            // Perform a bomb-based bullet hell attack.
            if (attackState == 1f)
            {
                frameType = (int)SCalFrameType.PunchHandCast;

                // Teleport to the center of the arena.
                if (attackTimer == 1f)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.CalThunderStrikeSound, npc.Center);

                    npc.Center = npc.Infernum().Arena.Center.ToVector2();

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(explosion =>
                        {
                            explosion.ModProjectile<DemonicExplosion>().MaxRadius = 1200f;
                        });
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<DemonicExplosion>(), 0, 0f);
                    }

                    npc.netUpdate = true;
                }

                // Initialize the bomb countdown timer.
                if (bombReleaseCountdown <= 0f)
                {
                    bombReleaseCountdown = baseBombReleaseRate;
                    npc.netUpdate = true;
                }

                // Give the target infinite flight time.
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player player = Main.player[i];
                    if (player.dead || !player.active || !npc.WithinRange(player.Center, 10000f))
                        continue;

                    player.DoInfiniteFlightCheck(Color.Red);
                }

                // Make the bomb countdown effect happen.
                if (bombReleaseCountdown >= 0f)
                {
                    bombReleaseCountdown--;

                    // Release bombs in four cardinal directions, alternating between a plus and X shape.
                    if (bombReleaseCountdown <= 0f)
                    {
                        bombShootCounter++;

                        bombReleaseCountdown = baseBombReleaseRate - bombShootCounter * 6f;
                        if (bombReleaseCountdown <= 56f)
                            bombReleaseCountdown = 56f;

                        SoundEngine.PlaySound(SCalBoss.BrimstoneBigShotSound, npc.Center);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                Vector2 bombShootVelocity = (TwoPi * i / 4f).ToRotationVector2() * 16f;
                                if (bombShootCounter % 2f == 1f)
                                    bombShootVelocity = bombShootVelocity.RotatedBy(PiOver4);

                                ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(bomb =>
                                {
                                    bomb.timeLeft = (int)(npc.Infernum().ExtraAI[3] * 0.95f);
                                });
                                Utilities.NewProjectileBetter(npc.Center, bombShootVelocity, ModContent.ProjectileType<DemonicBomb>(), 0, 0f, -1, bombExplosionRadius);
                            }
                        }
                    }
                }

                // Periodically release bursts of darts at the target.
                float dartTimer = attackTimer % dartReleaseRate;
                if (dartTimer == dartReleaseRate - 1f)
                {
                    SoundEngine.PlaySound(SCalBoss.BrimstoneShotSound, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < dartsPerBurst; i++)
                        {
                            float shootOffsetAngle = Lerp(-dartSpread, dartSpread, i / (float)(dartsPerBurst - 1f));
                            Vector2 dartShootVelocity = (dartTelegraphDirection + shootOffsetAngle).ToRotationVector2() * dartShootSpeed;
                            Utilities.NewProjectileBetter(npc.Center + dartShootVelocity * 2f, dartShootVelocity, ModContent.ProjectileType<BrimstoneBarrage>(), BrimstoneDartDamage, 0f);

                            dartShootVelocity *= -1f;
                            Utilities.NewProjectileBetter(npc.Center + dartShootVelocity * 2f, dartShootVelocity, ModContent.ProjectileType<BrimstoneBarrage>(), BrimstoneDartDamage, 0f);
                        }
                    }
                }

                // Create a telegraph before releasing the darts.
                if (Main.netMode != NetmodeID.MultiplayerClient && dartTimer == (int)(dartReleaseRate * 0.5f))
                {
                    for (int i = 0; i < dartsPerBurst; i++)
                    {
                        float shootOffsetAngle = Lerp(-dartSpread, dartSpread, i / (float)(dartsPerBurst - 1f));
                        Vector2 telegraphDirection = npc.SafeDirectionTo(target.Center).RotatedBy(shootOffsetAngle);

                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(telegraph =>
                        {
                            telegraph.ModProjectile<DemonicTelegraphLine>().DontMakeProjectile = true;
                        });
                        Utilities.NewProjectileBetter(npc.Center, telegraphDirection * 0.01f, ModContent.ProjectileType<DemonicTelegraphLine>(), 0, 0f, -1, 0f, (int)(dartReleaseRate * 0.5f));
                    }

                    dartTelegraphDirection = npc.AngleTo(target.Center);
                    npc.netUpdate = true;
                }

                if (attackTimer >= bombBulletHellDuration)
                {
                    // Delete any stray darts or explosions.
                    Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<DemonicBomb>(), ModContent.ProjectileType<DemonicExplosion>(), ModContent.ProjectileType<BrimstoneBarrage>());

                    attackState = 2f;
                    bombShootCounter = 0f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            // Release dart bullet hells from above.
            if (attackState == 2f)
            {
                // Initialize the gigablast countdown timer.
                if (gigablastReleaseCountdown <= 0f)
                {
                    gigablastReleaseCountdown = baseGigaReleaseRate;
                    npc.netUpdate = true;
                }

                // Give the target infinite flight time.
                // Give the target infinite flight time.
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player player = Main.player[i];
                    if (player.dead || !player.active || !npc.WithinRange(player.Center, 10000f))
                        continue;

                    player.DoInfiniteFlightCheck(Color.Red);
                }

                // Release walls of darts from the top of the arena.
                int brimstoneDartID = ModContent.ProjectileType<BrimstoneBarrage>();
                if (attackTimer % dartBulletHellReleaseRate == dartBulletHellReleaseRate - 1f)
                {
                    for (float x = npc.Infernum().Arena.Left; x < npc.Infernum().Arena.Right; x += dartBulletHellWallArea * 2f)
                    {
                        if (Main.netMode == NetmodeID.MultiplayerClient)
                            break;

                        for (float dx = 0f; dx < dartBulletHellWallArea; dx += dartBulletHellGapArea)
                        {
                            Vector2 dartSpawnPosition = new(x + dx + Main.rand.NextFloatDirection() * 8f, npc.Infernum().Arena.Top + Main.rand.NextFloatDirection() * 12f);
                            if (dartSpawnPosition.X >= npc.Infernum().Arena.Right)
                                continue;

                            if (dartBulletHellCounter % 2f == 1f)
                                dartSpawnPosition.X += dartBulletHellWallArea;

                            int dart = Utilities.NewProjectileBetter(dartSpawnPosition, Vector2.UnitY * 5f, brimstoneDartID, BrimstoneDartDamage, 0f);
                            if (Main.projectile.IndexInRange(dart))
                                Main.projectile[dart].timeLeft -= 210;
                        }
                    }

                    dartBulletHellCounter++;
                    npc.netUpdate = true;
                }

                // Cap the speed of all darts.
                foreach (Projectile dart in Utilities.AllProjectilesByID(brimstoneDartID))
                    dart.velocity = dart.velocity.ClampMagnitude(0f, 7.6f);

                // Make the bomb countdown effect happen.
                if (gigablastReleaseCountdown >= 0f)
                {
                    gigablastReleaseCountdown--;

                    // Release bombs in four cardinal directions, alternating between a plus and X shape.
                    if (gigablastReleaseCountdown <= 0f)
                    {
                        gigablastShootCounter++;

                        gigablastReleaseCountdown = baseGigaReleaseRate - gigablastShootCounter * 4f;
                        if (gigablastReleaseCountdown <= 34f)
                            gigablastReleaseCountdown = 34f;

                        SoundEngine.PlaySound(SCalBoss.BrimstoneBigShotSound, npc.Center);
                        Utilities.NewProjectileBetter(npc.Center, npc.SafeDirectionTo(target.Center) * 9.4f, ModContent.ProjectileType<InfernumBrimstoneGigablast>(), GigablastDamage, 0f, -1, 0f, 13f);
                    }
                }

                if (attackTimer >= gigablastBulletHellDuration)
                {
                    // Make all stray darts fade away.
                    foreach (Projectile dart in Utilities.AllProjectilesByID(brimstoneDartID))
                        dart.timeLeft = 50;

                    // Delete stray gigablasts.
                    Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<InfernumBrimstoneGigablast>());

                    attackState = 3f;
                    teleportCountdown = baseTeleportDelay;
                    bombShootCounter = 0f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            // Teleport around in rapid succession before descending.
            if (attackState == 3f)
            {
                frameType = (int)SCalFrameType.MagicCircle;

                // Fade out.
                npc.Opacity = Utils.GetLerpValue(3f, baseTeleportDelay, teleportCountdown, true);
                npc.Infernum().ExtraAI[8] = npc.Opacity;

                if (attackTimer >= teleportCountdown)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.CalThunderStrikeSound, npc.Center);

                    // Create some screenshake.
                    target.Infernum_Camera().CurrentScreenShakePower = 6f;

                    npc.Center = target.Center - Main.rand.NextVector2Unit() * Main.rand.NextFloat(180f, 455f);
                    npc.velocity = Vector2.Zero;

                    // Create increasingly large explosion effects until the entire screen is covered.
                    float explosionSize = Utils.Remap(teleportCountdown, baseTeleportDelay, 4f, 350f, 1500f);
                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(explosion =>
                    {
                        explosion.ModProjectile<DemonicExplosion>().MaxRadius = explosionSize;
                    });
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<DemonicExplosion>(), 0, 0f);

                    teleportCountdown -= 2f;
                    attackTimer = 0f;
                    if (teleportCountdown <= 4f)
                    {
                        teleportCountdown = 4f;
                        superfastTeleportCounter++;
                    }

                    if (superfastTeleportCounter >= 20f)
                    {
                        superfastTeleportCounter = 0f;
                        teleportCountdown = baseTeleportDelay;
                        attackState = 4f;
                    }

                    npc.netUpdate = true;
                }
            }

            // Teleport above the player and descend.
            if (attackState == 4f)
            {
                // Switch from the Epiphany section of Stained, Brutal Calamity to the Acceptance section.
                npc.ModNPC.Music = MusicID.Eerie;
                CalamityGlobalNPC.SCalAcceptance = npc.whoAmI;

                // Start the acceptance timer for after SCal disappears.
                if (SCalAcceptanceMusicSystem.ThemeTimer <= 0)
                    SCalAcceptanceMusicSystem.ThemeTimer = 1;

                frameType = (int)SCalFrameType.UpwardDraft;

                ForcefieldScale = Utils.GetLerpValue(45f, 0f, attackTimer, true);
                npc.Opacity = 1f;
                npc.gfxOffY = (1f - ForcefieldScale) * 6f;
                npc.ai[3] = 0f;
                npc.noTileCollide = false;

                if (attackTimer == 1f)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.CalThunderStrikeSound, npc.Center);

                    npc.Center = target.Center - Vector2.UnitY * 300f;
                    npc.velocity = Vector2.Zero;
                    npc.netUpdate = true;
                }

                // Congratulate the player.
                if (attackTimer == sitTime - 120f)
                    CalamityUtils.DisplayLocalizedText("Mods.InfernumMode.Status.SCalCongratulations", Color.Orange);

                if (attackTimer >= sitTime)
                {
                    // Begin the credits if draedon is dead.
                    if (DownedBossSystem.downedExoMechs)
                        CreditManager.BeginCredits();

                    npc.active = false;
                    npc.netUpdate = true;
                    npc.NPCLoot();
                }

                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * 6f, 0.018f);
            }
        }

        public static void SelectNextAttack(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            // Reset the berserk phase.
            int currentPhase = (int)npc.Infernum().ExtraAI[6];
            SCalAttackType[] attackCycle = Phase1AttackCycle;
            if (currentPhase == 1)
                attackCycle = Phase2AttackCycle;
            if (currentPhase >= 2)
                attackCycle = Phase3AttackCycle;

            // Delete any old demons.
            Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<SuicideBomberRitual>(), ModContent.ProjectileType<SuicideBomberDemonHostile>());

            // Pick a random attack to become berserk in if the previous attack was a berserk phase starter.
            if (npc.ai[0] == (int)SCalAttackType.BecomeBerserk)
            {
                npc.ai[0] = (int)Utils.SelectRandom(Main.rand,
                    SCalAttackType.CondemnationFanBurst,
                    SCalAttackType.ExplosiveCharges,
                    SCalAttackType.HellblastBarrage);
            }
            else
            {
                npc.ai[3] = 0f;
                npc.ai[0] = (int)attackCycle[(int)npc.Infernum().ExtraAI[5] % attackCycle.Length];
                npc.Infernum().ExtraAI[5]++;
            }

            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }

        public static void ClearAllEntities()
        {
            for (int i = 0; i < 3; i++)
            {
                Utilities.DeleteAllProjectiles(false,
                    ModContent.ProjectileType<AcceleratingDarkMagicFlame>(),
                    ModContent.ProjectileType<BrimstoneDemonSummonExplosion>(),
                    ModContent.ProjectileType<BrimstoneFlameOrb>(),
                    ModContent.ProjectileType<BrimstoneJewelProj>(),
                    ModContent.ProjectileType<BrimstoneLaserbeam>(),
                    ModContent.ProjectileType<CatastropheSlash>(),
                    ModContent.ProjectileType<CondemnationArrowSCal>(),
                    ModContent.ProjectileType<CondemnationProj>(),
                    ModContent.ProjectileType<DemonicBomb>(),
                    ModContent.ProjectileType<DemonicTelegraphLine>(),
                    ModContent.ProjectileType<InfernumBrimstoneGigablast>(),
                    ModContent.ProjectileType<FlameOverloadBeam>(),
                    ModContent.ProjectileType<HeresyProjSCal>(),
                    ModContent.ProjectileType<LostSoulProj>(),
                    ModContent.ProjectileType<RedirectingDarkSoul>(),
                    ModContent.ProjectileType<RedirectingHellfireSCal>(),
                    ModContent.ProjectileType<RedirectingLostSoulProj>(),
                    ModContent.ProjectileType<SepulcherBone>(),
                    ModContent.ProjectileType<SuicideBomberDemonHostile>(),
                    ModContent.ProjectileType<SuicideBomberRitual>());
            }
        }
        #endregion AI

        #region Frames and Drawcode
        public override void FindFrame(NPC npc, int frameHeight)
        {
            SCalFrameType frameType = (SCalFrameType)(int)npc.localAI[2];
            npc.frameCounter += npc.localAI[1];
            npc.frameCounter %= 6;
            npc.frame.Y = (int)npc.frameCounter + (int)frameType * 6;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            if (!InfernumConfig.Instance.ReducedGraphicsConfig)
            {
                // Create the fire drawer.
                FireDrawer ??= new FluidFieldInfernum(375, 375, new(0.0001f, 2.9f, 0.99f, 1.69f, 0.08f, 0.9f, 0.984f));
                FireDrawer.MovementUpdateSteps = 3;

                // Emit and draw fire behind everything else in phase 4.
                FireDrawer.ShouldUpdate = true;
                if (ForcefieldScale > 0f && npc.life < npc.lifeMax * Phase4LifeRatio)
                {
                    int instanceCount = 11;
                    float interpolant = Main.GlobalTimeWrappedHourly % 2f;
                    for (int i = 0; i < instanceCount; i++)
                    {
                        var color = CalamityUtils.MulticolorLerp(interpolant, Color.Red, Color.Orange, Color.OrangeRed);
                        float speedFactor = Main.rand.NextFloat(2f, 10f) * FireDrawer.Width * ForcefieldScale * 0.95f / (i * 0.04f + 1f);
                        Vector2 angularSpawnOffset = (TwoPi * i / instanceCount + Main.GlobalTimeWrappedHourly * 5f).ToRotationVector2();
                        angularSpawnOffset = Vector2.Lerp(angularSpawnOffset, -Vector2.UnitY, 0.3f) * 2.96f;

                        FireDrawer.CreateSource(new Vector2(FireDrawer.Width / 2 + angularSpawnOffset.X, FireDrawer.Height / 2 + angularSpawnOffset.Y).ToPoint(), Vector2.One * 2f, angularSpawnOffset.RotatedByRandom(0.3f) * speedFactor, color, ForcefieldScale);
                    }

                    FireDrawer.Draw(npc.Center - Main.screenPosition);
                }
            }

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            float berserkPhaseInterpolant = npc.ai[3];
            Texture2D energyChargeupEffect = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/SupremeCalamitas/PowerEffect").Value;
            Texture2D texture = DownedBossSystem.downedCalamitas && !BossRushEvent.BossRushActive ? TextureAssets.Npc[npc.type].Value : ModContent.Request<Texture2D>("CalamityMod/NPCs/SupremeCalamitas/SupremeCalamitasHooded").Value;

            // Draw a chargeup effect behind SCal if berserk.
            if (berserkPhaseInterpolant > 0f)
            {
                Color chargeupColor = Color.White * berserkPhaseInterpolant * npc.Opacity;
                Vector2 chargeupDrawPosition = npc.Bottom - Main.screenPosition + Vector2.UnitY * 20f;
                Rectangle chargeupFrame = energyChargeupEffect.Frame(1, 5, 0, (int)(Main.GlobalTimeWrappedHourly * 15.6f) % 5);
                Main.spriteBatch.Draw(energyChargeupEffect, chargeupDrawPosition, chargeupFrame, chargeupColor, npc.rotation, chargeupFrame.Size() * new Vector2(0.5f, 1f), npc.scale * 1.4f, 0, 0f);
            }

            Vector2 origin = new(texture.Width / 2f, texture.Height / Main.npcFrameCount[npc.type] / 2f);

            Rectangle frame = texture.Frame(2, Main.npcFrameCount[npc.type], npc.frame.Y / Main.npcFrameCount[npc.type], npc.frame.Y % Main.npcFrameCount[npc.type]);

            if (CalamityConfig.Instance.Afterimages)
            {
                int afterimageAmount = 7;
                for (int i = 1; i < afterimageAmount; i += 2)
                {
                    Color afterimageColor = npc.GetAlpha(Color.Lerp(lightColor, Color.White, 0.5f));
                    afterimageColor = npc.GetAlpha(afterimageColor);
                    afterimageColor *= (afterimageAmount - i) / (afterimageAmount * 2f);
                    Vector2 afterimagePosition = npc.oldPos[i] + new Vector2(npc.width, npc.height) / 2f - Main.screenPosition;
                    afterimagePosition -= new Vector2(texture.Width / 2f, texture.Height / Main.npcFrameCount[npc.type]) * npc.scale / 2f;
                    afterimagePosition += origin * npc.scale + new Vector2(0f, npc.gfxOffY);
                    Main.spriteBatch.Draw(texture, afterimagePosition, frame, afterimageColor, npc.rotation, origin, npc.scale, spriteEffects, 0f);
                }
            }

            bool inPhase2 = npc.ai[0] >= 3f && npc.life > npc.lifeMax * 0.01 || berserkPhaseInterpolant > 0f;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            drawPosition -= new Vector2(texture.Width / 2f, texture.Height / Main.npcFrameCount[npc.type]) * npc.scale / 2f;
            drawPosition += origin * npc.scale + new Vector2(0f, npc.gfxOffY);

            if (inPhase2)
            {
                // Make the sprite jitter with rage in phase 2. This does not happen in rematches since it would make little sense logically.
                if (!DownedBossSystem.downedCalamitas)
                    drawPosition += Main.rand.NextVector2Circular(0.8f, 2f);

                // And gain a flaming aura.
                Color auraColor = npc.GetAlpha(Color.Red) * 0.4f;
                for (int i = 0; i < 7; i++)
                {
                    Vector2 rotationalDrawOffset = (TwoPi * i / 7f + Main.GlobalTimeWrappedHourly * 4f).ToRotationVector2();
                    rotationalDrawOffset *= Lerp(3f, 4.25f, Cos(Main.GlobalTimeWrappedHourly * 4f) * 0.5f + 0.5f);
                    Main.spriteBatch.Draw(texture, drawPosition + rotationalDrawOffset, frame, auraColor, npc.rotation, origin, npc.scale * 1.1f, spriteEffects, 0f);
                }
            }
            Main.spriteBatch.Draw(texture, drawPosition, frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, spriteEffects, 0f);

            // Draw special effects in SCal's berserk phase.
            if (berserkPhaseInterpolant > 0f)
            {
                float eyePulse = Main.GlobalTimeWrappedHourly * 0.84f % 1f;
                Texture2D eyeGleam = InfernumTextureRegistry.Gleam.Value;
                Vector2 eyePosition = npc.Center + new Vector2(npc.spriteDirection * -4f, -14f);
                Vector2 horizontalGleamScaleSmall = new Vector2(berserkPhaseInterpolant * 3f, 1f) * 0.36f;
                Vector2 verticalGleamScaleSmall = new Vector2(1f, berserkPhaseInterpolant * 2f) * 0.36f;
                Vector2 horizontalGleamScaleBig = horizontalGleamScaleSmall * (1f + eyePulse * 2f);
                Vector2 verticalGleamScaleBig = verticalGleamScaleSmall * (1f + eyePulse * 2f);
                Color eyeGleamColorSmall = Color.Violet * berserkPhaseInterpolant;
                eyeGleamColorSmall.A = 0;
                Color eyeGleamColorBig = eyeGleamColorSmall * (1f - eyePulse);

                // Draw a pulsating red eye.
                Main.spriteBatch.Draw(eyeGleam, eyePosition - Main.screenPosition, null, eyeGleamColorSmall, 0f, eyeGleam.Size() * 0.5f, horizontalGleamScaleSmall, 0, 0f);
                Main.spriteBatch.Draw(eyeGleam, eyePosition - Main.screenPosition, null, eyeGleamColorSmall, 0f, eyeGleam.Size() * 0.5f, verticalGleamScaleSmall, 0, 0f);
                Main.spriteBatch.Draw(eyeGleam, eyePosition - Main.screenPosition, null, eyeGleamColorBig, 0f, eyeGleam.Size() * 0.5f, horizontalGleamScaleBig, 0, 0f);
                Main.spriteBatch.Draw(eyeGleam, eyePosition - Main.screenPosition, null, eyeGleamColorBig, 0f, eyeGleam.Size() * 0.5f, verticalGleamScaleBig, 0, 0f);
            }

            DrawForcefield(npc);
            DrawShield(npc);
            return false;
        }

        public static void DrawForcefield(NPC npc)
        {
            Main.spriteBatch.EnterShaderRegion();

            float intensity = 0.25f;

            // Shield intensity is always high during invincibility.
            if (npc.dontTakeDamage)
                intensity = 0.75f + Math.Abs(Cos(Main.GlobalTimeWrappedHourly * 1.7f)) * 0.1f;

            // Make the forcefield weaker in the second phase as a means of showing desparation.
            if (npc.ai[0] >= 3f)
                intensity *= 0.6f;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            float flickerPower = 0f;
            if (lifeRatio < 0.6f)
                flickerPower += 0.1f;
            if (lifeRatio < 0.3f)
                flickerPower += 0.15f;
            if (lifeRatio < 0.1f)
                flickerPower += 0.2f;
            if (lifeRatio < 0.05f)
                flickerPower += 0.1f;
            float opacity = Lerp(1f, MathF.Max(1f - flickerPower, 0.64f), Pow(Cos(Main.GlobalTimeWrappedHourly * Lerp(3f, 5f, flickerPower)), 26f));

            // During/prior to a charge the forcefield is always darker than usual and thus its intensity is also higher.
            if (!npc.dontTakeDamage && ShieldOpacity > 0f)
                intensity = 1.1f;

            // Dampen the opacity and intensity slightly, to allow SCal to be more easily visible inside of the forcefield.
            intensity *= 0.75f;
            opacity *= npc.Infernum().ExtraAI[8] * 0.75f;

            Texture2D forcefieldTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/SupremeCalamitas/ForcefieldTexture").Value;
            GameShaders.Misc["CalamityMod:SupremeShield"].UseImage1("Images/Misc/Perlin");

            Color forcefieldColor = Color.DarkViolet;
            Color secondaryForcefieldColor = Color.Red * 1.4f;

            if (!npc.dontTakeDamage && ShieldOpacity > 0f)
            {
                forcefieldColor *= 0.25f;
                secondaryForcefieldColor = Color.Lerp(secondaryForcefieldColor, Color.Black, 0.7f);
            }

            forcefieldColor *= opacity;
            secondaryForcefieldColor *= opacity;

            GameShaders.Misc["CalamityMod:SupremeShield"].UseSecondaryColor(secondaryForcefieldColor);
            GameShaders.Misc["CalamityMod:SupremeShield"].UseColor(forcefieldColor);
            GameShaders.Misc["CalamityMod:SupremeShield"].UseSaturation(intensity);
            GameShaders.Misc["CalamityMod:SupremeShield"].UseOpacity(opacity);
            GameShaders.Misc["CalamityMod:SupremeShield"].Apply();

            Main.spriteBatch.Draw(forcefieldTexture, npc.Center - Main.screenPosition, null, Color.White * opacity, 0f, forcefieldTexture.Size() * 0.5f, ForcefieldScale * 3f, SpriteEffects.None, 0f);

            Main.spriteBatch.ExitShaderRegion();
        }

        public static void DrawShield(NPC npc)
        {
            float jawRotation = ShieldRotation;
            float jawRotationOffset = 0f;
            bool shouldUseShieldLaughAnimation = npc.localAI[3] != 0f;

            // Have an agape mouth when charging.
            if (npc.ai[1] == 2f)
                jawRotationOffset -= 0.71f;

            // And a laugh right before the charge.
            else if (shouldUseShieldLaughAnimation)
                jawRotationOffset += Lerp(0.04f, -0.82f, Sin(Main.GlobalTimeWrappedHourly * 17.2f) * 0.5f + 0.5f);

            Color shieldColor = Color.White * ShieldOpacity;
            Texture2D shieldSkullTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/SupremeCalamitas/SupremeShieldTop").Value;
            Texture2D shieldJawTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/SupremeCalamitas/SupremeShieldBottom").Value;
            Vector2 drawPosition = npc.Center + ShieldRotation.ToRotationVector2() * 24f - Main.screenPosition;
            Vector2 jawDrawPosition = drawPosition;
            SpriteEffects direction = Math.Cos(ShieldRotation) > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically;
            if (direction == SpriteEffects.FlipVertically)
                jawDrawPosition += (ShieldRotation - PiOver2).ToRotationVector2() * 42f;
            else
            {
                jawDrawPosition += (ShieldRotation + PiOver2).ToRotationVector2() * 42f;
                jawRotationOffset *= -1f;
            }

            Main.spriteBatch.Draw(shieldJawTexture, jawDrawPosition, null, shieldColor, jawRotation + jawRotationOffset, shieldJawTexture.Size() * 0.5f, 1f, direction, 0f);
            Main.spriteBatch.Draw(shieldSkullTexture, drawPosition, null, shieldColor, ShieldRotation, shieldSkullTexture.Size() * 0.5f, 1f, direction, 0f);
        }
        #endregion Frames and Drawcode

        #region Death Effects
        public override bool CheckDead(NPC npc)
        {
            // Just die as usual if Supreme Calamitas is killed during the death animation. This is done so that Cheat Sheet and other butcher effects can kill her quickly.
            if (npc.Infernum().ExtraAI[7] >= 1f)
                return true;

            npc.active = true;
            npc.dontTakeDamage = true;
            npc.life = 1;
            npc.Infernum().ExtraAI[7] = 1f;
            npc.netUpdate = true;

            return false;
        }
        #endregion Death Effects

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Mods.InfernumMode.PetDialog.SCalTip1";
            yield return n =>
            {
                if (TipsManager.ShouldUseJokeText)
                    return "Mods.InfernumMode.PetDialog.SCalJokeTip1";
                return string.Empty;
            };
        }
        #endregion Tips
    }
}
