using CalamityMod;
using CalamityMod.Items.SummonItems;
using CalamityMod.NPCs;
using CalamityMod.NPCs.PrimordialWyrm;
using CalamityMod.Particles;
using CalamityMod.Sounds;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.ScreenEffects;
using InfernumMode.Content.Achievements;
using InfernumMode.Content.BehaviorOverrides.AbyssAIs;
using InfernumMode.Content.Projectiles.Generic;
using InfernumMode.Content.WorldGeneration;
using InfernumMode.Core.GlobalInstances.Players;
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
using Terraria.Graphics.Light;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class AEWHeadBehaviorOverride : NPCBehaviorOverride
    {
        public enum AEWAttackType
        {
            // Spawn animation states.
            SnatchTerminus,
            ThreateninglyHoverNearPlayer,

            // Light attacks.
            BurningGaze,
            DisintegratingBeam,
            TerminusChase,

            // Dark attacks.
            AbyssalNightmareRitual,
            ForbiddenUnleash,
            ShadowIllusions,

            // Neutral attacks.
            SplitFormCharges,
            CrystalConstriction,
            HammerheadRams,

            // Enrage attack.
            RuthlesslyMurderTarget,

            // Death animation state.
            DeathAnimation
        }

        public override int NPCOverrideType => ModContent.NPCType<PrimordialWyrmHead>();

        #region AI
        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio,
            Phase3LifeRatio,
            Phase4LifeRatio
        };

        public static Dictionary<Color, AEWAttackType[]> Phase1AttackCycles => new()
        {
            [Color.Cyan] = new AEWAttackType[]
            {
                AEWAttackType.BurningGaze,
                AEWAttackType.AbyssalNightmareRitual,
                AEWAttackType.CrystalConstriction,
                AEWAttackType.BurningGaze,
            },
            [Color.Yellow] = new AEWAttackType[]
            {
                AEWAttackType.CrystalConstriction,
                AEWAttackType.AbyssalNightmareRitual,
                AEWAttackType.BurningGaze,
                AEWAttackType.HammerheadRams,
            },
            [Color.Violet] = new AEWAttackType[]
            {
                AEWAttackType.CrystalConstriction,
                AEWAttackType.BurningGaze,
                AEWAttackType.CrystalConstriction,
                AEWAttackType.BurningGaze,
            },
        };

        public static Dictionary<Color, AEWAttackType[]> Phase2AttackCycles => new()
        {
            [Color.Cyan] = new AEWAttackType[]
            {
                AEWAttackType.BurningGaze,
                AEWAttackType.AbyssalNightmareRitual,
                AEWAttackType.BurningGaze,
                AEWAttackType.ForbiddenUnleash,
            },
            [Color.Yellow] = new AEWAttackType[]
            {
                AEWAttackType.CrystalConstriction,
                AEWAttackType.AbyssalNightmareRitual,
                AEWAttackType.BurningGaze,
                AEWAttackType.HammerheadRams,
            },
            [Color.Violet] = new AEWAttackType[]
            {
                AEWAttackType.CrystalConstriction,
                AEWAttackType.AbyssalNightmareRitual,
                AEWAttackType.ForbiddenUnleash,
                AEWAttackType.HammerheadRams,
            },
        };

        public static Dictionary<Color, AEWAttackType[]> Phase3AttackCycles => new()
        {
            [Color.Cyan] = new AEWAttackType[]
            {
                AEWAttackType.SplitFormCharges,
                AEWAttackType.DisintegratingBeam,
                AEWAttackType.ShadowIllusions,
                AEWAttackType.SplitFormCharges,
            },
            [Color.Yellow] = new AEWAttackType[]
            {
                AEWAttackType.SplitFormCharges,
                AEWAttackType.CrystalConstriction,
                AEWAttackType.DisintegratingBeam,
                AEWAttackType.AbyssalNightmareRitual,
            },
            [Color.Violet] = new AEWAttackType[]
            {
                AEWAttackType.ShadowIllusions,
                AEWAttackType.SplitFormCharges,
                AEWAttackType.CrystalConstriction,
                AEWAttackType.SplitFormCharges,
            },
        };

        public static Dictionary<Color, AEWAttackType[]> Phase4AttackCycles => new()
        {
            [Color.Wheat] = new AEWAttackType[]
            {
                AEWAttackType.TerminusChase,
                AEWAttackType.DisintegratingBeam,
                AEWAttackType.TerminusChase,
                AEWAttackType.AbyssalNightmareRitual,
                AEWAttackType.TerminusChase,
                AEWAttackType.SplitFormCharges,
                AEWAttackType.TerminusChase,
                AEWAttackType.ShadowIllusions,
            }
        };

        // Projectile damage values.
        public const int NormalShotDamage = 540;

        public const int StrongerNormalShotDamage = 560;

        public const int PowerfulShotDamage = 850;

        public const int EnragedFormSegmentCount = 45;

        public const int SegmentCount = 125;

        public const int EyeGlowOpacityIndex = 5;

        public const int LightFormInterpolantIndex = 6;

        public const int DarkFormInterpolantIndex = 7;

        public const int PreviousPatternEyeIndexIndex = 8;

        public const int PatternEyeIndexIndex = 9;

        public const int AttackCycleCounterIndex = 10;

        public const int CurrentPhaseIndex = 11;

        public const int EyeColorShiftInterpolant = 12;

        public const int DeathAnimationFadeCompletionInterpolantIndex = 13;

        public const int TerminusDrawInterpolantIndex = 14;

        public const float Phase2LifeRatio = 0.75f;

        public const float Phase3LifeRatio = 0.45f;

        public const float Phase4LifeRatio = 0.15f;

        public static Color LoreTooltipColor => new(107, 101, 180);

        public override void SetDefaults(NPC npc)
        {
            // Set defaults that, if were to be changed by Calamity, would cause significant issues to the fight.
            npc.width = 254;
            npc.height = 138;
            npc.scale = 1f;
            npc.Opacity = 0f;
            npc.defense = 100;
            npc.DR_NERD(0.4f);
        }

        public override bool PreAI(NPC npc)
        {
            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();

            Player target = Main.player[npc.target];
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float initializedFlag = ref npc.ai[2];
            ref float superEnrageTimer = ref npc.ai[3];
            ref float hammerHeadRotation = ref npc.localAI[0];
            ref float eyeGlowOpacity = ref npc.Infernum().ExtraAI[EyeGlowOpacityIndex];
            ref float lightFormInterpolant = ref npc.Infernum().ExtraAI[LightFormInterpolantIndex];
            ref float darkFormInterpolant = ref npc.Infernum().ExtraAI[DarkFormInterpolantIndex];
            ref float eyeColorShiftInterpolant = ref npc.Infernum().ExtraAI[EyeColorShiftInterpolant];
            ref float currentPhase = ref npc.Infernum().ExtraAI[CurrentPhaseIndex];

            // Create segments on the first frame of existence.
            if (Main.netMode != NetmodeID.MultiplayerClient && initializedFlag == 0f)
            {
                int segmentCount = target.chaosState ? EnragedFormSegmentCount : SegmentCount;
                CreateSegments(npc, segmentCount, ModContent.NPCType<PrimordialWyrmBody>(), ModContent.NPCType<PrimordialWyrmBodyAlt>(), ModContent.NPCType<PrimordialWyrmTail>());
                initializedFlag = 1f;
                npc.netUpdate = true;
            }

            // If there still was no valid target after the above search for one, swim away.
            float despawnDistance = attackType == (int)AEWAttackType.ShadowIllusions ? 40000f : 18000f;
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || !Main.player[npc.target].active || !Main.player[npc.target].WithinRange(npc.Center, despawnDistance))
            {
                DoBehavior_Despawn(npc);
                return false;
            }

            // WHY IS THIS A PROBLEM???
            if (!Lighting.NotRetro)
                Lighting.Mode = LightMode.Color;

            bool targetNeedsDeath = superEnrageTimer >= 1f || target.chaosState;



            // Give targets infinite flight time.
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.dead || !player.active || !npc.WithinRange(player.Center, 10000f))
                    continue;

                // Disable obnoxious water mechanics so that the player can fight the boss without interruption.
                player.breath = player.breathMax;
                player.ignoreWater = true;
                player.DoInfiniteFlightCheck(Color.DeepSkyBlue);
            }

            // Set the global whoAmI variable.
            CalamityGlobalNPC.adultEidolonWyrmHead = npc.whoAmI;

            // Reset various things every frame.
            npc.dontTakeDamage = false;
            npc.chaseable = true;
            npc.defDamage = 550;
            npc.damage = npc.defDamage;
            npc.Calamity().DR = 0.3f;

            // This is necessary to allow the boss effects buff to be applied.
            npc.Calamity().KillTime = 1;

            // Why are you despawning?
            npc.boss = true;
            npc.timeLeft = 7200;

            // Make the eye color shift happen if necessary.
            if (eyeColorShiftInterpolant < 1f)
            {
                eyeColorShiftInterpolant = Clamp(eyeColorShiftInterpolant + 0.06f, 0f, 1f);
                if (eyeColorShiftInterpolant >= 1f)
                {
                    SoundEngine.PlaySound(SoundID.Item163, target.Center);
                    SoundEngine.PlaySound(PrimordialWyrmHead.ChargeSound, target.Center);
                }
            }

            // Make the player emit a lot of light.
            Lighting.AddLight(target.Center, Vector3.One * 4f);

            switch ((AEWAttackType)attackType)
            {
                // Spawn animation states.
                case AEWAttackType.SnatchTerminus:
                    DoBehavior_SnatchTerminus(npc);
                    break;
                case AEWAttackType.ThreateninglyHoverNearPlayer:
                    DoBehavior_ThreateninglyHoverNearPlayer(npc, target, ref eyeGlowOpacity, ref attackTimer);
                    break;

                // Light attacks.
                case AEWAttackType.BurningGaze:
                    DoBehavior_BurningGaze(npc, target, currentPhase, ref attackTimer);
                    break;
                case AEWAttackType.DisintegratingBeam:
                    DoBehavior_DisintegratingBeam(npc, target, currentPhase, ref attackTimer, ref lightFormInterpolant);
                    break;
                case AEWAttackType.TerminusChase:
                    DoBehavior_TerminusChase(npc, target, ref attackTimer, ref lightFormInterpolant);
                    break;

                // Dark attacks.
                case AEWAttackType.AbyssalNightmareRitual:
                    DoBehavior_AbyssalNightmareRitual(npc, target, currentPhase, ref attackTimer, ref darkFormInterpolant);
                    break;
                case AEWAttackType.ForbiddenUnleash:
                    DoBehavior_ForbiddenUnleash(npc, target, currentPhase, ref attackTimer, ref hammerHeadRotation, ref darkFormInterpolant);
                    break;
                case AEWAttackType.ShadowIllusions:
                    DoBehavior_ShadowIllusions(npc, target, ref attackTimer, ref darkFormInterpolant);
                    break;

                // Neutral attacks.
                case AEWAttackType.SplitFormCharges:
                    DoBehavior_SplitFormCharges(npc, target, currentPhase, ref attackTimer);
                    break;
                case AEWAttackType.CrystalConstriction:
                    DoBehavior_CrystalConstriction(npc, target, currentPhase, ref attackTimer);
                    break;
                case AEWAttackType.HammerheadRams:
                    DoBehavior_HammerheadRams(npc, target, currentPhase, ref attackTimer);
                    break;

                // Enrage attack.
                case AEWAttackType.RuthlesslyMurderTarget:
                    DoBehavior_RuthlesslyMurderTarget(npc, target, ref attackTimer);
                    break;

                // This isn't an attack, how kind of you to notice.
                case AEWAttackType.DeathAnimation:
                    npc.Opacity = 1f;
                    if (npc.Infernum().ExtraAI[TerminusDrawInterpolantIndex] <= 0.5f)
                    {
                        darkFormInterpolant = Utils.GetLerpValue(15f, 90f, attackTimer, true) * 0.7f;
                        lightFormInterpolant = Clamp(lightFormInterpolant - 0.05f, 0f, 1f);
                    }
                    else
                    {
                        lightFormInterpolant = Clamp(lightFormInterpolant + 0.04f, 0f, 0.2f);
                        darkFormInterpolant = Clamp(darkFormInterpolant - 0.05f, 0f, 1f);
                    }
                    DoBehavior_DeathAnimation(npc, target, ref attackTimer);
                    break;
            }

            lightFormInterpolant *= 0.98f;
            darkFormInterpolant *= 0.98f;

            // Determine rotation based on the current velocity.
            if (npc.velocity != Vector2.Zero && npc.velocity.Length() > 0.01f)
                npc.rotation = npc.velocity.ToRotation() + PiOver2;

            // Increment the attack timer.
            attackTimer++;

            // Increment the super-enrage timer if it's activated.
            // If the player is somehow not dead after enough time has passed they're just manually killed.
            if (targetNeedsDeath)
                superEnrageTimer++;
            if (superEnrageTimer >= 1800f)
                target.KillMe(PlayerDeathReason.ByNPC(npc.whoAmI), 10000000D, 0);

            // Transform into a being of light if the super-enrage form is active.
            // This happens gradually under normal circumstances but is instantaneous if enraged with the RoD on the first frame.
            if (superEnrageTimer >= 1f)
                lightFormInterpolant = Utils.GetLerpValue(0f, 60f, superEnrageTimer, true);
            if (attackType != (int)AEWAttackType.RuthlesslyMurderTarget && targetNeedsDeath)
            {
                if (attackType == (int)AEWAttackType.ThreateninglyHoverNearPlayer && attackTimer <= 10f && target.chaosState)
                {
                    superEnrageTimer = 60f;
                    lightFormInterpolant = 1f;
                }

                SelectNextAttack(npc);
                attackType = (int)AEWAttackType.RuthlesslyMurderTarget;
                npc.netUpdate = true;
            }

            return false;
        }
        #endregion AI

        #region Specific Behaviors

        public static void DoBehavior_Despawn(NPC npc)
        {
            npc.velocity.X *= 0.985f;
            if (npc.velocity.Y < 72f)
                npc.velocity.Y += 1.3f;

            if (npc.timeLeft > 210)
                npc.timeLeft = 210;

            if (!npc.WithinRange(Main.player[npc.target].Center, 4000f))
                npc.active = false;
        }

        public static void DoBehavior_SnatchTerminus(NPC npc)
        {
            float chargeSpeed = 41f;
            List<Projectile> terminusInstances = Utilities.AllProjectilesByID(ModContent.ProjectileType<TerminusAnimationProj>()).ToList();

            // Fade in.
            npc.Opacity = Clamp(npc.Opacity + 0.08f, 0f, 1f);

            // Disable damage.
            npc.damage = 0;
            npc.dontTakeDamage = true;

            // Transition to the next attack if there are no more Terminus instances.
            if (terminusInstances.Count <= 0)
            {
                SelectNextAttack(npc);
                return;
            }

            Projectile target = terminusInstances.First();

            // Fly very, very quickly towards the Terminus.
            npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * chargeSpeed, 0.16f);

            // Delete the Terminus instance if it's being touched.
            // On the next frame the AEW will transition to the next attack, assuming there isn't another Terminus instance for some weird reason.
            if (npc.WithinRange(target.Center, 90f))
            {
                SoundEngine.PlaySound(SoundID.DD2_SkyDragonsFuryShot with { Volume = 1.3f }, target.Center);
                target.Kill();
            }
        }

        public static void DoBehavior_ThreateninglyHoverNearPlayer(NPC npc, Player target, ref float eyeGlowOpacity, ref float attackTimer)
        {
            int roarDelay = 60;
            int eyeGlowFadeinTime = 105;
            int attackTransitionDelay = 210;
            Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 450f, -360f);
            ref float hasReachedDestination = ref npc.Infernum().ExtraAI[0];

            // Disable damage.
            npc.damage = 0;
            npc.dontTakeDamage = true;

            // Attempt to hover to the top left/right of the target at first.
            if (hasReachedDestination == 0f)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(hoverDestination) * 32f, 0.084f);
                if (npc.WithinRange(hoverDestination, 96f))
                {
                    hasReachedDestination = 1f;
                    npc.netUpdate = true;
                }

                // Don't let the attack timer increment.
                attackTimer = -1f;

                return;
            }

            // Roar after a short delay.
            if (attackTimer == roarDelay)
                SoundEngine.PlaySound(InfernumSoundRegistry.AEWThreatenRoar);

            // Slow down and look at the target threateningly before attacking.
            npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * 3f, 0.071f);

            // Become opaque.
            npc.Opacity = Clamp(npc.Opacity + 0.067f, 0f, 1f);

            // Make the eye glowmask gradually fade in.
            eyeGlowOpacity = Utils.GetLerpValue(0f, eyeGlowFadeinTime, attackTimer, true);

            if (attackTimer >= roarDelay + attackTransitionDelay)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_BurningGaze(NPC npc, Player target, float currentPhase, ref float attackTimer)
        {
            int boltShootDelay = 36;
            int boltCountPerBurst = 7;
            int telegraphTime = 30;
            int boltShotCount = 7;
            float boltShootSpeed = 1.6f;

            if (currentPhase >= 1f)
            {
                boltShootDelay -= 4;
                boltCountPerBurst++;
            }
            if (currentPhase >= 2f)
            {
                boltShootDelay -= 4;
                boltCountPerBurst++;
            }
            if (currentPhase >= 3f)
            {
                boltShootDelay -= 4;
                boltCountPerBurst++;
            }

            ref float hasReachedDestination = ref npc.Infernum().ExtraAI[0];
            ref float horizontalHoverOffsetDirection = ref npc.Infernum().ExtraAI[1];
            ref float generalAttackTimer = ref npc.Infernum().ExtraAI[2];
            ref float boltShotCounter = ref npc.Infernum().ExtraAI[3];

            // Attempt to hover to the top left/right of the target at first.
            if (hasReachedDestination == 0f)
            {
                // Disable contact damage to prevent cheap hits.
                npc.damage = 0;

                if (horizontalHoverOffsetDirection == 0f)
                {
                    horizontalHoverOffsetDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                    npc.netUpdate = true;
                }

                Vector2 hoverDestination = target.Center + new Vector2(horizontalHoverOffsetDirection * 600f, -360f);

                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(hoverDestination) * 64f, 0.12f);
                if (npc.WithinRange(hoverDestination, 96f))
                {
                    hasReachedDestination = 1f;
                    npc.netUpdate = true;
                }

                // Don't let the attack timer increment.
                attackTimer = -1f;
                return;
            }

            // Slow down and look at the target threateningly.
            npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * 2f, 0.08f);
            npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.18f);

            // Create a buffer before the attack properly begins, to ensure that the player can reasonably react to it.
            generalAttackTimer++;
            if (generalAttackTimer < 90f)
                attackTimer = 0f;

            // Release eye bursts.
            if (attackTimer >= boltShootDelay)
            {
                GetEyePositions(npc, out Vector2 left, out Vector2 right);

                if (boltShotCounter < boltShotCount - 1f)
                    SoundEngine.PlaySound(CommonCalamitySounds.LaserCannonSound, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int telegraphID = ModContent.ProjectileType<AEWTelegraphLine>();
                    int boltID = ModContent.ProjectileType<DivineLightBolt>();
                    for (int i = 0; i < boltCountPerBurst; i++)
                    {
                        // Be careful when adjusting the range of the offset angle. If it isn't just right then the attack might be invalidated by just literally
                        // standing still and letting the bolts fly away.
                        float shootOffsetAngle = Lerp(-0.85f, 0.85f, i / (float)(boltCountPerBurst - 1f));
                        Vector2 leftVelocity = npc.SafeDirectionTo(left).RotatedBy(shootOffsetAngle) * boltShootSpeed;
                        Vector2 rightVelocity = npc.SafeDirectionTo(right).RotatedBy(shootOffsetAngle) * boltShootSpeed;

                        int telegraph = Utilities.NewProjectileBetter(left, leftVelocity, telegraphID, 0, 0f, -1, 0f, telegraphTime);
                        if (Main.projectile.IndexInRange(telegraph))
                            Main.projectile[telegraph].localAI[1] = 1f;

                        telegraph = Utilities.NewProjectileBetter(right, rightVelocity, telegraphID, 0, 0f, -1, 0f, telegraphTime);
                        if (Main.projectile.IndexInRange(telegraph))
                            Main.projectile[telegraph].localAI[1] = 1f;

                        Utilities.NewProjectileBetter(left, leftVelocity, boltID, NormalShotDamage, 0f);
                        Utilities.NewProjectileBetter(right, rightVelocity, boltID, NormalShotDamage, 0f);
                    }

                    boltShotCounter++;
                    hasReachedDestination = 0f;
                    horizontalHoverOffsetDirection *= -1f;
                    if (boltShotCounter >= boltShotCount)
                    {
                        Utilities.DeleteAllProjectiles(false, telegraphID, boltID);
                        SelectNextAttack(npc);
                    }

                    npc.netUpdate = true;
                }
            }
        }

        public static void DoBehavior_DisintegratingBeam(NPC npc, Player target, float currentPhase, ref float attackTimer, ref float lightFormInterpolant)
        {
            int hoverTime = 90;
            int laserShootTime = DivineLightLaserbeam.LifetimeConst;
            int perpendicularBoltShootRate = 42;
            float spinAngularVelocity = Pi / 180f;
            float perpendicularLaserSpacing = 184f;

            if (currentPhase >= 2f)
            {
                hoverTime -= 20;
                perpendicularBoltShootRate -= 3;
                perpendicularLaserSpacing -= 16f;
            }
            if (currentPhase >= 3f)
            {
                hoverTime -= 12;
                perpendicularBoltShootRate -= 4;
                perpendicularLaserSpacing -= 15f;
            }

            ref float lightOrbRadius = ref npc.Infernum().ExtraAI[0];
            ref float spinDirection = ref npc.Infernum().ExtraAI[1];

            if (attackTimer == 1f)
            {
                SoundEngine.PlaySound(SoundID.Item163, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<DivineLightOrb>(), StrongerNormalShotDamage, 0f, -1, 0f, npc.whoAmI);
                    lightOrbRadius = 3f;
                    npc.netUpdate = true;
                }
            }

            // Look at the target at first, charging power into the light orb.
            lightFormInterpolant = Utils.GetLerpValue(0f, hoverTime - 25f, attackTimer, true);

            // Release light into the orb.
            Vector2 lightOrbCenter = DivineLightOrb.GetHoverDestination(npc);
            if (Main.rand.NextFloat() < lightFormInterpolant * 0.4f)
            {
                Vector2 lightSpawnPosition = npc.Center + npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.7f) * 20f;
                Color lightColor = Color.Lerp(Color.Wheat, Color.Yellow, Main.rand.NextFloat(0.7f));
                SquishyLightParticle light = new(lightSpawnPosition, (lightOrbCenter - lightSpawnPosition) * 0.1f, 0.6f, lightColor, 35);
                GeneralParticleHandler.SpawnParticle(light);
            }

            if (attackTimer <= hoverTime)
            {
                lightOrbRadius = Lerp(lightOrbRadius, 108f, 0.04f);
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * 6f, 0.084f);
                if (!npc.WithinRange(target.Center, 450f))
                    npc.Center = npc.Center.MoveTowards(target.Center, Utils.GetLerpValue(449f, 600f, npc.Distance(target.Center), true) * 8f);

                // Cast the light laserbeam.
                if (attackTimer == hoverTime)
                {
                    Utilities.CreateShockwave(npc.Center, 2, 8, 75, false);
                    SoundEngine.PlaySound(InfernumSoundRegistry.TerminusLaserbeamSound, target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Utilities.NewProjectileBetter(lightOrbCenter, npc.velocity.SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<DivineLightLaserbeam>(), PowerfulShotDamage, 0f, -1, 0f, npc.whoAmI);
                        spinDirection = (WrapAngle(npc.AngleTo(target.Center) - npc.velocity.ToRotation()) > 0f).ToDirectionInt();
                        npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * 2f;
                        npc.netUpdate = true;
                    }
                }

                return;
            }

            // Slow spin around in an attempt to hit the player.
            npc.velocity = npc.velocity.RotatedBy(spinAngularVelocity * spinDirection);

            // Release perpendicular bolts if the player isn't too close to the laser.
            Vector2 start = npc.Center;
            Vector2 end = start + npc.velocity.SafeNormalize(Vector2.UnitY) * DivineLightLaserbeam.LaserLengthCost;
            bool canShootPerpendicularBurst = !target.WithinRange(Utils.ClosestPointOnLine(target.Center, start, end), 100f);
            if ((attackTimer - hoverTime) % perpendicularBoltShootRate == perpendicularBoltShootRate - 1f && canShootPerpendicularBurst)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceHolyBlastShootSound, target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (float dy = 0f; dy < DivineLightLaserbeam.LaserLengthCost - 108f; dy += perpendicularLaserSpacing)
                    {
                        Vector2 boltSpawnPosition = npc.Center + npc.velocity.SafeNormalize(Vector2.UnitY) * dy;
                        Vector2 boltPerpendicularVelocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(PiOver2) * 3f;

                        Utilities.NewProjectileBetter(boltSpawnPosition, -boltPerpendicularVelocity, ModContent.ProjectileType<DivineLightBolt>(), StrongerNormalShotDamage, 0f, -1, 0f, 18f);
                        Utilities.NewProjectileBetter(boltSpawnPosition, boltPerpendicularVelocity, ModContent.ProjectileType<DivineLightBolt>(), StrongerNormalShotDamage, 0f, -1, 0f, 18f);
                    }
                }
            }

            // Make the orb fade away once the laser is going away.
            if (attackTimer >= hoverTime + laserShootTime - 28f)
            {
                lightOrbRadius *= 0.94f;
                if (lightOrbRadius <= 3f)
                {
                    Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<DivineLightOrb>());
                    SelectNextAttack(npc);
                }
            }
        }

        public static void DoBehavior_TerminusChase(NPC npc, Player target, ref float attackTimer, ref float lightFormInterpolant)
        {
            int attackDelay = HorizontalRayTerminus.RedirectTime + HorizontalRayTerminus.WingGrowTime;
            int ramTime = 42;
            int redirectTime = 12;
            float chargeSpeed = 36f;
            float chargeAcceleration = 1.04f;
            float wrappedAttackTimer = (attackTimer - attackDelay) % (ramTime + redirectTime);

            lightFormInterpolant = Utils.GetLerpValue(0f, 32f, attackTimer, true) * Utils.GetLerpValue(-30f, -5f, HorizontalRayTerminus.Lifetime - attackTimer, true);

            if (attackTimer == 2f && !npc.WithinRange(target.Center, 400f))
            {
                npc.velocity = npc.velocity.SafeNormalize(target.Center) * 10f;
                npc.Center = npc.Center.MoveTowards(target.Center, 12f);
                attackTimer = 1f;
            }

            // Don't do damage if the laser hasn't been cast.
            if (!Utilities.AnyProjectiles(ModContent.ProjectileType<TerminusDeathray>()))
                npc.damage = 0;

            // Summon the Terminus on the first frame.
            if (attackTimer == 2f)
            {
                SoundEngine.PlaySound(CalamityMod.NPCs.Providence.Providence.HolyRaySound, target.Center);
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * 10f, 0.7f);

                if (Main.myPlayer == target.whoAmI)
                    Utilities.NewProjectileBetter(npc.Top, Vector2.Zero, ModContent.ProjectileType<HorizontalRayTerminus>(), 0, 0f, target.whoAmI);
            }

            // Look at the player before attacking.
            if (attackTimer < attackDelay)
            {
                npc.Calamity().DR = 0.9f;
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * 10f, 0.24f);
                return;
            }

            if (attackTimer < HorizontalRayTerminus.Lifetime - 96f)
            {
                if (wrappedAttackTimer < redirectTime)
                    npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.3f) * 0.98f;
                else if (npc.velocity.Length() < chargeSpeed)
                    npc.velocity *= chargeAcceleration;
            }
            else
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * 9f, 0.12f);

            if (attackTimer >= HorizontalRayTerminus.Lifetime - 30f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_AbyssalNightmareRitual(NPC npc, Player target, float currentPhase, ref float attackTimer, ref float darkFormInterpolant)
        {
            int fadeOutTime = 60;
            int chargeTime = 132;
            int returnToTargetTime = 50;
            int chargeCount = 2;
            float demonUpwardChargeSpeed = 19f;
            float mainWyrmUpwardChargeSpeed = 39f;
            float demonSpacing = 400f;

            if (currentPhase >= 1f)
                demonSpacing -= 40f;
            if (currentPhase >= 2f)
            {
                fadeOutTime -= 4;
                chargeTime -= 10;
            }
            if (currentPhase >= 3f)
            {
                fadeOutTime -= 5;
                chargeTime -= 5;
                demonSpacing -= 25f;
            }

            Vector2 wyrmSpawnPoint = target.Center + Vector2.UnitY * 2000f;
            ref float chargeCounter = ref npc.Infernum().ExtraAI[0];

            void createWyrms(float horizontalOffset)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (float dx = -1900f; dx < 1900f; dx += demonSpacing)
                    {
                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(telegraph =>
                        {
                            telegraph.ModProjectile<AEWTelegraphLine>().CreateSplitAEW = false;
                        });
                        Utilities.NewProjectileBetter(wyrmSpawnPoint + Vector2.UnitX * (dx + horizontalOffset), -Vector2.UnitY, ModContent.ProjectileType<AEWTelegraphLine>(), 0, 0f, -1, 1f, 50f);
                        Utilities.NewProjectileBetter(wyrmSpawnPoint + Vector2.UnitX * (dx + horizontalOffset + 85f), -Vector2.UnitY * demonUpwardChargeSpeed, ModContent.ProjectileType<AEWNightmareWyrm>(), StrongerNormalShotDamage, 0f, -1);
                    }
                }
            }

            // Disappear before attacking.
            if (attackTimer < fadeOutTime)
            {
                npc.damage = 0;
                npc.Opacity = Utils.GetLerpValue(fadeOutTime - 4f, 0f, attackTimer, true);
                npc.dontTakeDamage = true;
                darkFormInterpolant = 1f - npc.Opacity;
            }
            else
                npc.Opacity = 1f;

            // Teleport below the target and charge.
            if (attackTimer == fadeOutTime)
            {
                npc.Center = wyrmSpawnPoint;
                npc.velocity = npc.SafeDirectionTo(target.Center) * mainWyrmUpwardChargeSpeed;

                // Bring the segments to the teleport position.
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (!n.active || (n.type != ModContent.NPCType<PrimordialWyrmBody>() && n.type != ModContent.NPCType<PrimordialWyrmBodyAlt>() && n.type != ModContent.NPCType<PrimordialWyrmTail>()))
                        continue;

                    n.Center = npc.Center;
                    n.netUpdate = true;
                }

                SoundEngine.PlaySound(PrimordialWyrmHead.ChargeSound, target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(telegraph =>
                    {
                        telegraph.ModProjectile<AEWTelegraphLine>().CreateSplitAEW = false;
                    });
                    Utilities.NewProjectileBetter(npc.Center, npc.SafeDirectionTo(target.Center), ModContent.ProjectileType<AEWTelegraphLine>(), 0, 0f, -1, 1f, 30f);
                    createWyrms(0f);
                }
            }

            if (attackTimer == fadeOutTime + 67f)
                createWyrms(150f);

            // Move towards the target after charging.
            if (attackTimer >= fadeOutTime + chargeTime)
            {
                if (!npc.WithinRange(target.Center, 900f))
                    npc.Center = npc.Center.MoveTowards(target.Center, 20f);
                if (!npc.WithinRange(target.Center, 120f))
                    npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * 14f, 0.1f);
            }

            if (attackTimer >= fadeOutTime + chargeTime + returnToTargetTime)
            {
                chargeCounter++;
                attackTimer = 0f;
                if (chargeCounter >= chargeCount)
                    SelectNextAttack(npc);
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_ForbiddenUnleash(NPC npc, Player target, float currentPhase, ref float attackTimer, ref float hammerHeadRotation, ref float darkFormInterpolant)
        {
            int headOpenTime = 56;
            int attackDelay = 96;
            int soulReleaseRate = 30;
            int soulShootTime = 480;
            int attackTransitionDelay = 180;
            int soulBurstCount = 12;
            float spinAngularVelocity = Pi / 193f;

            if (currentPhase >= 2f)
            {
                soulBurstCount++;
                soulReleaseRate -= 4;
                soulShootTime -= 60;
            }
            if (currentPhase >= 3f)
            {
                soulBurstCount += 2;
                soulReleaseRate -= 4;
                soulShootTime += 60;
            }

            ref float shootCounter = ref npc.Infernum().ExtraAI[0];

            // Open the hammer head to reveal the dark portal.
            if (attackTimer <= headOpenTime)
            {
                if (attackTimer == 36f)
                    SoundEngine.PlaySound(SoundID.NPCDeath30 with { Pitch = -0.35f }, target.Center);
                if (attackTimer == 50f)
                    SoundEngine.PlaySound(SoundID.DD2_EtherianPortalOpen, target.Center);

                float headOpenInterpolant = Pow(attackTimer / headOpenTime, 7f);
                hammerHeadRotation = SmoothStep(0f, 1f, headOpenInterpolant) * Pi * 0.19f;

                // Turn into shadow.
                darkFormInterpolant = Utils.GetLerpValue(0f, headOpenTime - 15f, attackTimer, true);
            }

            // Disable contact damage.
            npc.damage = 0;

            // Move towards the target.
            if (!npc.WithinRange(target.Center, 180f))
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * 4f, 0.18f);

            // Release arcing souls and telegraphs outward.
            if (attackTimer >= attackDelay && attackTimer <= attackDelay + soulShootTime && attackTimer % soulReleaseRate == soulReleaseRate - 1f)
            {
                SoundEngine.PlaySound(SoundID.Item72, target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float directionalAngularVelocity = spinAngularVelocity * (shootCounter % 2f == 0f).ToDirectionInt();
                    Vector2 soulSpawnPosition = npc.Center + (npc.rotation - PiOver2).ToRotationVector2() * npc.scale * 36f;
                    for (int i = 0; i < soulBurstCount; i++)
                    {
                        Vector2 soulBurstVelocity = (TwoPi * i / soulBurstCount + shootCounter).ToRotationVector2() * 3f;
                        Utilities.NewProjectileBetter(soulSpawnPosition, soulBurstVelocity, ModContent.ProjectileType<AbyssalSoul>(), StrongerNormalShotDamage, 0f, -1, 0f, directionalAngularVelocity);

                        List<Vector2> telegraphPoints = new();
                        Vector2 telegraphVelocity = soulBurstVelocity;
                        Vector2 telegraphPosition = soulSpawnPosition;
                        for (int j = 0; j < 72; j++)
                        {
                            if (j % 2 == 0)
                                telegraphPoints.Add(telegraphPosition);
                            telegraphVelocity = AbyssalSoul.PerformMovementStep(telegraphVelocity, directionalAngularVelocity);
                            telegraphPosition += telegraphVelocity;
                        }

                        float hue = i / (float)(soulBurstCount - 1f);
                        if (shootCounter % 2f == 1f)
                            hue = 1f - hue;

                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(telegraph =>
                        {
                            telegraph.ModProjectile<AbyssalSoulTelegraph>().TelegraphPoints = telegraphPoints.ToArray();
                        });
                        Utilities.NewProjectileBetter(soulSpawnPosition, Vector2.Zero, ModContent.ProjectileType<AbyssalSoulTelegraph>(), 0, 0f, -1, hue);
                    }

                    shootCounter++;
                    npc.netUpdate = true;
                }
            }

            if (attackTimer >= attackDelay + soulShootTime)
            {
                hammerHeadRotation = hammerHeadRotation.AngleLerp(0f, 0.16f).AngleTowards(0f, 0.02f);
                darkFormInterpolant = Clamp(darkFormInterpolant - 0.08f, 0f, 1f);
            }

            if (attackTimer >= attackDelay + soulShootTime + attackTransitionDelay)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_ShadowIllusions(NPC npc, Player target, ref float attackTimer, ref float darkFormInterpolant)
        {
            int shadowFormChangeTime = 50;
            int illusionSpawnCount = 9;
            int illusionSpawnRate = 27;
            int telegraphDuration = (int)(illusionSpawnRate * 0.67f);
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float illusionCounter = ref npc.Infernum().ExtraAI[1];
            ref float hasPerformedRealCharge = ref npc.Infernum().ExtraAI[2];

            // The way this attack works is by keeping a counter for every charge, whether it's a real or fake one.
            // This represents which counter index is the one that will spawn the real, damaging AEW.
            ref float realCounterIndex = ref npc.Infernum().ExtraAI[3];

            switch ((int)attackSubstate)
            {
                // Turn to shadow and fade away.
                case 0:
                    darkFormInterpolant = Utils.GetLerpValue(0f, shadowFormChangeTime, attackTimer, true);

                    if (attackTimer >= shadowFormChangeTime)
                    {
                        npc.Opacity = Clamp(npc.Opacity - 0.04f, 0f, 1f);
                        if (npc.Opacity <= 0f)
                        {
                            realCounterIndex = Main.rand.Next(1, illusionSpawnCount - 2);
                            attackSubstate = 1f;
                            attackTimer = 0f;
                            npc.netUpdate = true;
                        }
                    }
                    break;

                // Cast illusions.
                case 1:
                    // Hover away from the target if not charging.
                    if (hasPerformedRealCharge == 0f)
                    {
                        npc.Center = target.Center + Vector2.UnitY * 3000f;
                        npc.damage = 0;
                        npc.dontTakeDamage = true;
                    }
                    else
                        npc.Opacity = 1f;

                    if (attackTimer % illusionSpawnRate == illusionSpawnRate - 1f)
                    {
                        SoundEngine.PlaySound(SoundID.Item165, target.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            bool createsRealAew = illusionCounter == realCounterIndex;
                            Vector2 telegraphDirection = (TwoPi * illusionCounter / illusionSpawnCount).ToRotationVector2();
                            Utilities.NewProjectileBetter(target.Center - telegraphDirection * 1500f, telegraphDirection, ModContent.ProjectileType<AEWIllusionTelegraphLine>(), 0, 0f, -1, createsRealAew.ToInt(), telegraphDuration);

                            illusionCounter++;
                            if (illusionCounter >= illusionSpawnCount)
                            {
                                attackSubstate = 2f;
                                attackTimer = 0f;
                            }

                            npc.netUpdate = true;
                        }
                    }

                    // Release spirals of lumenyl crystals that converge in on the player, to make things a bit more complex.
                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 5f == 4f)
                    {
                        Vector2 spiralDirection = (TwoPi * attackTimer / 120f).ToRotationVector2();
                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(crystal =>
                        {
                            crystal.ModProjectile<ConvergingLumenylCrystal>().ConvergenceCenter = target.Center;
                        });
                        Vector2 spiralSpawnOffset = spiralDirection * 900f;
                        Utilities.NewProjectileBetter(target.Center + spiralSpawnOffset, spiralDirection * -12f, ModContent.ProjectileType<ConvergingLumenylCrystal>(), NormalShotDamage, 0f);
                    }

                    break;

                // Fade back in and transition to the next attack.
                case 2:
                    npc.Opacity = Clamp(npc.Opacity + 0.05f, 0f, 1f);
                    darkFormInterpolant = Clamp(darkFormInterpolant - 0.02f, 0f, 1f);
                    npc.velocity = ((target.Center - npc.Center) * 0.01f).ClampMagnitude(2f, 40f);

                    if (!npc.WithinRange(target.Center, 1000f))
                        npc.Center = npc.Center.MoveTowards(target.Center, 100f);

                    if (attackTimer >= 150f)
                        SelectNextAttack(npc);

                    break;
            }
        }

        public static void DoBehavior_SplitFormCharges(NPC npc, Player target, float currentPhase, ref float attackTimer)
        {
            int swimTime = 105;
            int telegraphTime = 42;
            int chargeTime = 46;
            int chargeCount = 3;

            if (currentPhase >= 3f)
            {
                telegraphTime -= 5;
                chargeTime -= 3;
            }

            int attackCycleTime = telegraphTime + chargeTime;
            int chargeCounter = (int)(attackTimer - swimTime) / attackCycleTime;
            float attackCycleTimer = (attackTimer - swimTime) % attackCycleTime;
            bool shouldStopAttacking = chargeCounter >= chargeCount && !Utilities.AnyProjectiles(ModContent.ProjectileType<AEWSplitForm>());
            ref float verticalSwimDirection = ref npc.Infernum().ExtraAI[0];

            // Don't let the attack cycle timer increment if still swimming.
            if (attackTimer < swimTime)
                attackCycleTimer = 0f;

            // Disable homing effects.
            npc.chaseable = false;

            // Swim away from the target. If they're close to the bottom of the abyss, swim up. Otherwise, swim down.
            if (verticalSwimDirection == 0f)
            {
                verticalSwimDirection = 1f;
                if (target.Center.Y >= CustomAbyss.AbyssBottom * 16f - 2400f)
                    verticalSwimDirection = -1f;

                npc.netUpdate = true;
            }
            else if (!shouldStopAttacking)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * verticalSwimDirection * 105f, 0.1f);

                // Fade out after enough time has passed, in anticipation of the attack.
                npc.Opacity = Utils.GetLerpValue(swimTime - 1f, swimTime - 35f, attackTimer, true);
            }

            // Stay below the target once completely invisible.
            if (npc.Opacity <= 0f)
            {
                npc.Center = target.Center + Vector2.UnitY * verticalSwimDirection * 1600f;
                npc.velocity = -Vector2.UnitY * verticalSwimDirection * 23f;
            }

            // Fade back in if ready to transition to the next attack.
            if (shouldStopAttacking)
            {
                npc.Opacity = Clamp(npc.Opacity + 0.01f, 0f, 1f);
                if (npc.Opacity >= 1f)
                {
                    Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<EidolistIce>());
                    SelectNextAttack(npc);
                }
            }

            // Cast telegraph direction lines. Once they dissipate the split forms will appear and charge.
            if (attackCycleTimer == 1f && !shouldStopAttacking)
            {
                SoundEngine.PlaySound(SoundID.Item158, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int crossLineID = ModContent.ProjectileType<AEWTelegraphLine>();
                    bool firstCrossIsDark = Main.rand.NextBool();
                    float crossSpawnOffset = 2080f;
                    bool flipHorizontalDirection = target.Center.X < Main.maxTilesX * 16f - 3000f;
                    float directionX = flipHorizontalDirection.ToDirectionInt();
                    switch (chargeCounter % 2)
                    {
                        // Plus-shaped cross.
                        case 0:
                            Utilities.NewProjectileBetter(target.Center + Vector2.UnitX * directionX * crossSpawnOffset, -Vector2.UnitX * directionX, crossLineID, 0, 0f, -1, firstCrossIsDark.ToInt(), telegraphTime);
                            Utilities.NewProjectileBetter(target.Center + Vector2.UnitY * crossSpawnOffset, -Vector2.UnitY, crossLineID, 0, 0f, -1, 1f - firstCrossIsDark.ToInt(), telegraphTime);
                            break;

                        // X-shaped cross.
                        case 1:
                            Utilities.NewProjectileBetter(target.Center + new Vector2(directionX, -1f) * crossSpawnOffset * 0.707f, new(-directionX, 1f), crossLineID, 0, 0f, -1, firstCrossIsDark.ToInt(), telegraphTime);
                            Utilities.NewProjectileBetter(target.Center + new Vector2(directionX, 1f) * crossSpawnOffset * 0.707f, new(-directionX, -1f), crossLineID, 0, 0f, -1, 1f - firstCrossIsDark.ToInt(), telegraphTime);
                            break;
                    }
                }
            }
        }

        public static void DoBehavior_CrystalConstriction(NPC npc, Player target, float currentPhase, ref float attackTimer)
        {
            int spinTime = 540;
            int totalArmSpirals = 5;
            int attackDelay = 150;
            float startingSpinRadius = 1200f;
            float endingSpinRadius = 880f;
            float worldEdgeAvoidanceDistance = 600f;
            float spinRadius = Utils.Remap(attackTimer - attackDelay, 0f, spinTime - 180f, startingSpinRadius, endingSpinRadius);
            float spiralSpinRate = Pi / Utils.Remap(attackTimer - attackDelay, 0f, spinTime - 180f, 38f, 29f);
            float crystalShootSpeed = 8.5f;

            if (currentPhase >= 1f)
            {
                totalArmSpirals++;
                crystalShootSpeed += 0.5f;
            }
            if (currentPhase >= 2f)
            {
                crystalShootSpeed += 0.5f;
                spiralSpinRate *= 0.9f;
            }
            if (currentPhase >= 3f)
            {
                crystalShootSpeed += 0.75f;
            }

            ref float spinCenterX = ref npc.Infernum().ExtraAI[0];
            ref float spinCenterY = ref npc.Infernum().ExtraAI[1];
            ref float crystalShootOffsetAngle = ref npc.Infernum().ExtraAI[2];

            // Decide the spin center on the first frame.
            if (attackTimer == 1f)
            {
                spinCenterX = Clamp(target.Center.X, worldEdgeAvoidanceDistance, Main.maxTilesX * 16f - worldEdgeAvoidanceDistance);
                spinCenterY = target.Center.Y;
                while (!Collision.CanHitLine(new Vector2(spinCenterX, spinCenterY), 1, 1, new Vector2(spinCenterX, spinCenterY + 900f), 1, 1))
                    spinCenterY -= 10f;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(new(spinCenterX, spinCenterY), Vector2.Zero, ModContent.ProjectileType<CircleCenterTelegraph>(), 0, 0f);

                npc.netUpdate = true;
            }

            // Spin around the target.
            float spinSpeedInterpolant = Utils.GetLerpValue(0f, 75f, attackTimer, true);
            Vector2 hoverDestination = new Vector2(spinCenterX, spinCenterY) + (TwoPi * attackTimer / 150f).ToRotationVector2() * spinRadius;
            npc.Center = npc.Center.MoveTowards(hoverDestination, spinSpeedInterpolant * 20f);
            npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(hoverDestination) * spinSpeedInterpolant * 60f, spinSpeedInterpolant * 0.2f);

            // Disable attacks at first.
            if (attackTimer <= attackDelay)
            {
                npc.damage = 0;
                if (attackTimer == attackDelay)
                {
                    // Orient the crystals such that the player starts out squarely in the middle of a gap, for gameplay fairness purposes.
                    crystalShootOffsetAngle = npc.AngleTo(target.Center) + Pi / totalArmSpirals;
                    npc.netUpdate = true;
                }

                return;
            }

            // Release spirals of crystals inward.
            if (attackTimer % 3f == 0f && attackTimer < attackDelay + spinTime)
            {
                if (attackTimer % 24f == 0f)
                    SoundEngine.PlaySound(SoundID.Item9, target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < totalArmSpirals; i++)
                    {
                        Vector2 spiralDirection = (TwoPi * i / totalArmSpirals + crystalShootOffsetAngle).ToRotationVector2();
                        Vector2 spinCenter = new(spinCenterX, spinCenterY);
                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(crystal =>
                        {
                            crystal.ModProjectile<ConvergingLumenylCrystal>().ConvergenceCenter = spinCenter;
                        });
                        Vector2 spiralSpawnOffset = spiralDirection * (spinRadius - 72f);
                        Utilities.NewProjectileBetter(spinCenter + spiralSpawnOffset, spiralDirection * -crystalShootSpeed, ModContent.ProjectileType<ConvergingLumenylCrystal>(), NormalShotDamage, 0f);
                    }
                }
                crystalShootOffsetAngle += spiralSpinRate;
            }

            // Spawn a lot of light bolts around the player if they leave the circle.
            if (Main.netMode != NetmodeID.MultiplayerClient && !target.WithinRange(new(spinCenterX, spinCenterY), spinRadius + 180f) && attackTimer % 60f == 59f)
            {
                int telegraphID = ModContent.ProjectileType<AEWTelegraphLine>();
                int boltID = ModContent.ProjectileType<DivineLightBolt>();
                for (float dx = -1500f; dx < 1500f; dx += 30f)
                {
                    int telegraph = Utilities.NewProjectileBetter(target.Center + new Vector2(dx, -1500f), Vector2.UnitY, telegraphID, 0, 0f, -1, 0f, 30f);
                    if (Main.projectile.IndexInRange(telegraph))
                        Main.projectile[telegraph].localAI[1] = 1f;
                    telegraph = Utilities.NewProjectileBetter(target.Center + new Vector2(-1500f, dx), Vector2.UnitX, telegraphID, 0, 0f, -1, 0f, 30f);
                    if (Main.projectile.IndexInRange(telegraph))
                        Main.projectile[telegraph].localAI[1] = 1f;

                    Utilities.NewProjectileBetter(target.Center + new Vector2(dx, -1500f), Vector2.UnitY * 8f, boltID, PowerfulShotDamage, 0f);
                    Utilities.NewProjectileBetter(target.Center + new Vector2(-1500f, dx), Vector2.UnitX * 8f, boltID, PowerfulShotDamage, 0f);
                }
            }

            if (attackTimer >= attackDelay + spinTime + 45f)
            {
                Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<ConvergingLumenylCrystal>());
                SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_HammerheadRams(NPC npc, Player target, float currentPhase, ref float attackTimer)
        {
            int attackDelay = 90;
            int ramTime = 36;
            int redirectTime = 24;
            int ramCount = 8;
            int icicleCountPerBurst = 35;
            float chargeSpeed = 39f;
            float chargeAcceleration = 1.06f;

            if (currentPhase >= 1f)
            {
                chargeSpeed += 3f;
                chargeAcceleration += 0.007f;
            }
            if (currentPhase >= 2f)
            {
                ramCount++;
                icicleCountPerBurst += 4;
            }
            if (currentPhase >= 3f)
                icicleCountPerBurst += 5;

            float wrappedAttackTimer = (attackTimer - attackDelay) % (ramTime + redirectTime);
            ref float ramCounter = ref npc.Infernum().ExtraAI[0];

            // Roar on the first frame as a warning.
            if (attackTimer == 1f)
            {
                SoundEngine.PlaySound(PrimordialWyrmHead.ChargeSound, target.Center);
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * 10f, 0.7f);
            }

            // Look at the player before attacking.
            if (attackTimer < attackDelay)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * 10f, 0.24f);
                return;
            }

            // Release an even spread of ice before charging.
            if (wrappedAttackTimer == redirectTime && !npc.WithinRange(target.Center, 300f))
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.AEWIceBurst, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < icicleCountPerBurst; i++)
                    {
                        Vector2 icicleVelocity = (TwoPi * i / icicleCountPerBurst).ToRotationVector2() * 12f;
                        Utilities.NewProjectileBetter(npc.Center, icicleVelocity, ModContent.ProjectileType<EidolistIce>(), NormalShotDamage, 0f);
                    }
                }
            }

            if (wrappedAttackTimer < redirectTime)
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.2f) * 0.96f;
            else if (npc.velocity.Length() < chargeSpeed)
                npc.velocity *= chargeAcceleration;

            if (wrappedAttackTimer == redirectTime + ramTime - 1f)
            {
                ramCounter++;
                if (ramCounter >= ramCount)
                {
                    Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<EidolistIce>());
                    SelectNextAttack(npc);
                }
            }
        }

        public static void DoBehavior_RuthlesslyMurderTarget(NPC npc, Player target, ref float attackTimer)
        {
            float swimSpeed = Utils.Remap(attackTimer, 0f, 95f, 6f, attackTimer * 0.05f + 50f);

            // Scream very, very loudly on the first frame.
            if (attackTimer == 1f)
                SoundEngine.PlaySound(InfernumSoundRegistry.AEWThreatenRoar with { Volume = 3f });

            // Be fully opaque.
            npc.Opacity = 1f;

            // The target must die.
            npc.damage = 1367750;
            npc.dontTakeDamage = true;
            npc.Calamity().ShouldCloseHPBar = true;

            if (!npc.WithinRange(target.Center, 300f))
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * swimSpeed, 0.1f);
            else
                npc.velocity *= 1.1f;
        }

        public static void DoBehavior_DeathAnimation(NPC npc, Player target, ref float attackTimer)
        {
            int fadeStartTime = 30;
            int fadeEndTime = 540;
            ref float fadeCompletionInterpolant = ref npc.Infernum().ExtraAI[DeathAnimationFadeCompletionInterpolantIndex];
            ref float terminusDrawInterpolant = ref npc.Infernum().ExtraAI[TerminusDrawInterpolantIndex];

            void giveLoot()
            {
                npc.life = 0;
                npc.checkDead();
                Main.BestiaryTracker.Kills.RegisterKill(npc);
                AchievementPlayer.ExtraUpdateHandler(Main.LocalPlayer, AchievementUpdateCheck.NPCKill, npc.whoAmI);
                DownedBossSystem.downedPrimordialWyrm = true;
            }

            // Disable damage.
            npc.damage = 0;
            npc.dontTakeDamage = true;
            npc.Calamity().ShouldCloseHPBar = true;
            npc.Calamity().ProvidesProximityRage = false;

            // Disable sounds.
            npc.HitSound = npc.DeathSound = null;

            if (attackTimer == 151f)
                SoundEngine.PlaySound(InfernumSoundRegistry.AEWDeathAnimationSound, target.Center);

            // Slowly attempt to approach the target.
            if (terminusDrawInterpolant <= 0f)
            {
                if (!npc.WithinRange(target.Center, 125f))
                    npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * 6f, 0.056f);
                if (npc.velocity.Length() > 10f)
                    npc.velocity *= 0.95f;

                if (!npc.WithinRange(target.Center, 800f))
                    npc.Center = npc.Center.MoveTowards(target.Center, 50f);
            }
            else if (terminusDrawInterpolant >= 1f)
            {
                npc.velocity = Vector2.UnitY * Sin(TwoPi * attackTimer / 240f) * 2f;

                // Periodically emit shockwaves, similar to the crystal hearts in Celeste.
                if (attackTimer % 90f == 67f)
                    Utilities.CreateShockwave(npc.Center, 1, 3, 18f, false);
                if (attackTimer % 90f == 89f)
                    SoundEngine.PlaySound(InfernumSoundRegistry.TerminusPulseSound, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient && target.WithinRange(npc.Center, 60f))
                {
                    giveLoot();
                    Item.NewItem(npc.GetSource_Death(), npc.Center, ModContent.ItemType<Terminus>());
                    npc.active = false;
                }
            }
            else
                npc.velocity *= 0.9f;

            // Make the segments gradually fade away.
            fadeCompletionInterpolant = Utils.GetLerpValue(fadeStartTime, fadeEndTime, attackTimer, true);

            // Transform into the Terminus if not already defeated.
            // If already defeated simply disspear and give loot.
            if (!DownedBossSystem.downedPrimordialWyrm)
                terminusDrawInterpolant = Utils.GetLerpValue(fadeEndTime, fadeEndTime + 95f, attackTimer, true);
            else
            {
                npc.Opacity = Utils.GetLerpValue(fadeEndTime + 95f, fadeEndTime, attackTimer, true);
                if (npc.Opacity <= 0f)
                {
                    giveLoot();
                    npc.active = false;
                }
            }

            // Clear projectiles.
            Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<AbyssalSoul>(), ModContent.ProjectileType<AEWSplitForm>(), ModContent.ProjectileType<ConvergingLumenylCrystal>(),
                ModContent.ProjectileType<DivineLightBolt>(), ModContent.ProjectileType<DivineLightLaserbeam>(), ModContent.ProjectileType<HorizontalRayTerminus>(), ModContent.ProjectileType<PsychicBlast>());
        }

        #endregion Specific Behaviors

        #region AI Utility Methods
        public static void GetEyePositions(NPC npc, out Vector2 left, out Vector2 right)
        {
            float normalizedRotation = npc.rotation;
            Vector2 eyeCenterPoint = npc.Center - Vector2.UnitY.RotatedBy(normalizedRotation) * npc.width * npc.scale * 0.19f;
            left = eyeCenterPoint - normalizedRotation.ToRotationVector2() * npc.scale * npc.width * 0.09f;
            right = eyeCenterPoint + normalizedRotation.ToRotationVector2() * npc.scale * npc.width * 0.09f;
        }

        public static void CreateSegments(NPC npc, int wormLength, int bodyType1, int bodyType2, int tailType)
        {
            int previousIndex = npc.whoAmI;
            for (int i = 0; i < wormLength; i++)
            {
                int nextIndex;
                if (i < wormLength - 1)
                {
                    int bodyID = i % 2 == 0 ? bodyType1 : bodyType2;
                    nextIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, bodyID, npc.whoAmI + 1);
                }
                else
                    nextIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, tailType, npc.whoAmI + 1);

                Main.npc[nextIndex].realLife = npc.whoAmI;
                Main.npc[nextIndex].ai[2] = npc.whoAmI;
                Main.npc[nextIndex].ai[1] = previousIndex;

                if (i >= 1)
                    Main.npc[previousIndex].ai[0] = nextIndex;
                Main.npc[nextIndex].ai[3] = i;

                // Force sync the new segment into existence.
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, nextIndex, 0f, 0f, 0f, 0);

                previousIndex = nextIndex;
            }
        }

        public static Dictionary<Color, AEWAttackType[]> GetCurrentPatternSet(NPC npc)
        {
            return (int)npc.Infernum().ExtraAI[CurrentPhaseIndex] switch
            {
                1 => Phase2AttackCycles,
                2 => Phase3AttackCycles,
                3 => Phase4AttackCycles,
                _ => Phase1AttackCycles,
            };
        }

        internal static void SelectNextAttack(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;

            AEWAttackType nextAttack;
            AEWAttackType currentAttack = (AEWAttackType)npc.ai[0];
            ref float attackCycleCounter = ref npc.Infernum().ExtraAI[AttackCycleCounterIndex];
            ref float currentPhase = ref npc.Infernum().ExtraAI[CurrentPhaseIndex];
            ref float previousPatternEyeIndex = ref npc.Infernum().ExtraAI[PreviousPatternEyeIndexIndex];
            ref float patternEyeIndex = ref npc.Infernum().ExtraAI[PatternEyeIndexIndex];
            ref float eyeColorShiftInterpolant = ref npc.Infernum().ExtraAI[EyeColorShiftInterpolant];

            if (currentAttack == AEWAttackType.SnatchTerminus)
                nextAttack = AEWAttackType.ThreateninglyHoverNearPlayer;
            else
            {
                // Transition to the next phase if necessary.
                bool changedPhase = false;
                if (lifeRatio < Phase2LifeRatio && currentPhase == 0f)
                {
                    changedPhase = true;
                    currentPhase = 1f;
                }
                if (lifeRatio < Phase3LifeRatio && currentPhase == 1f)
                {
                    changedPhase = true;
                    currentPhase = 2f;
                }
                if (lifeRatio < Phase4LifeRatio && currentPhase == 2f)
                {
                    changedPhase = true;
                    currentPhase = 3f;
                }

                // Reset things if a phase change happened.
                if (changedPhase)
                {
                    patternEyeIndex = 0f;
                    attackCycleCounter = 0f;
                    eyeColorShiftInterpolant = 0f;
                }

                var patternSet = GetCurrentPatternSet(npc);
                Color eyeColor = patternSet.Keys.ElementAt((int)patternEyeIndex);
                nextAttack = patternSet[eyeColor][(int)attackCycleCounter];

                // Cycle to the next eye color pattern if done with the current one.
                attackCycleCounter++;
                if (attackCycleCounter >= patternSet[eyeColor].Length)
                {
                    previousPatternEyeIndex = patternEyeIndex;
                    patternEyeIndex = (patternEyeIndex + 1f) % patternSet.Count;
                    eyeColorShiftInterpolant = 0f;
                    attackCycleCounter = 0f;
                }
            }

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.ai[0] = (int)nextAttack;
            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }

        #endregion AI Utility Methods

        #region Draw Effects
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            npc.frame = new(0, 0, 254, 138);

            DrawSegment(npc, lightColor);
            return false;
        }

        public static void DrawSegment(NPC npc, Color lightColor)
        {
            string segmentString = "Head";
            if (npc.type == ModContent.NPCType<PrimordialWyrmBody>())
                segmentString = "Body";
            if (npc.type == ModContent.NPCType<PrimordialWyrmBodyAlt>())
                segmentString = "BodyAlt";
            if (npc.type == ModContent.NPCType<PrimordialWyrmTail>())
                segmentString = "Tail";

            if (segmentString == "Head")
            {
                DrawHead(npc, lightColor);
                npc.frame = Rectangle.Empty;
                return;
            }

            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition;

            // Reset the texture frame for drawing.
            npc.frame = texture.Frame();

            if (drawPosition.X <= -300f || drawPosition.X >= Main.screenWidth + 300f || drawPosition.Y <= -300f || drawPosition.Y >= Main.screenHeight + 300f)
            {
                npc.frame = Rectangle.Empty;
                return;
            }

            NPC head = Main.npc[npc.realLife];
            float fadeCompletionInterpolant = head.Infernum().ExtraAI[DeathAnimationFadeCompletionInterpolantIndex];
            float terminusDrawInterpolant = head.Infernum().ExtraAI[TerminusDrawInterpolantIndex];
            int segmentToFadeAway = (int)Math.Round(SegmentCount * (1f - fadeCompletionInterpolant));
            float deathAnimationOpacity = 1f;

            // Make segments fade away over time.
            if (fadeCompletionInterpolant > 0f)
            {
                // Segments that have already faded away stay invisible.
                if (npc.ai[3] > segmentToFadeAway)
                    deathAnimationOpacity = 0f;

                // Segments that are in the process of fading away do so before disappearing.
                else if (npc.ai[3] == segmentToFadeAway)
                    deathAnimationOpacity = Pow(fadeCompletionInterpolant * SegmentCount % 1f, 2f);
            }

            // Draw the segment.
            Color drawColor = npc.GetAlpha(Color.White) * deathAnimationOpacity * (1f - terminusDrawInterpolant);
            AEWShadowFormDrawSystem.LightAndDarkEffectsCache.Add(new(texture, drawPosition, npc.frame, drawColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, 0, 0));

            // Hacky way of ensuring that PostDraw doesn't do anything.
            npc.frame = Rectangle.Empty;
        }

        public static void DrawHead(NPC npc, Color lightColor)
        {
            float hammerHeadRotation = npc.localAI[0];
            float terminusDrawInterpolant = npc.Infernum().ExtraAI[TerminusDrawInterpolantIndex];

            var eyeColorList = GetCurrentPatternSet(npc).Keys;
            if (npc.Infernum().ExtraAI[PatternEyeIndexIndex] >= eyeColorList.Count)
                npc.Infernum().ExtraAI[PatternEyeIndexIndex] = 0f;
            if (npc.Infernum().ExtraAI[PreviousPatternEyeIndexIndex] >= eyeColorList.Count)
                npc.Infernum().ExtraAI[PreviousPatternEyeIndexIndex] = 0f;

            Color currentEyeColor = eyeColorList.ElementAt((int)npc.Infernum().ExtraAI[PatternEyeIndexIndex]);
            Color previousEyeColor = eyeColorList.ElementAt((int)npc.Infernum().ExtraAI[PreviousPatternEyeIndexIndex]);
            Color eyeColor = Color.Lerp(previousEyeColor, currentEyeColor, npc.Infernum().ExtraAI[EyeColorShiftInterpolant]);

            eyeColor *= npc.Opacity * npc.Infernum().ExtraAI[EyeGlowOpacityIndex] * (1f - terminusDrawInterpolant);
            Texture2D backHeadTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/AdultEidolonWyrm/AEWBackHead").Value;
            Texture2D backHeadGlowmask = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/AdultEidolonWyrm/AEWBackHeadGlow").Value;
            Texture2D hammerHeadTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/AdultEidolonWyrm/AEWHammerHeadSide").Value;
            Texture2D hammerHeadGlowmask = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/AdultEidolonWyrm/AEWHammerHeadSideGlow").Value;
            Texture2D eyesGlowmask = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/AdultEidolonWyrm/AEWEyesSide").Value;

            void drawInstance(Vector2 drawPosition, Color? colorOverride = null)
            {
                // Draw the back head.
                Main.EntitySpriteDraw(backHeadTexture, drawPosition, null, npc.GetAlpha(lightColor) * (1f - terminusDrawInterpolant), npc.rotation, backHeadTexture.Size() * 0.5f, npc.scale, 0, 0);
                AEWShadowFormDrawSystem.LightAndDarkEffectsCache.Add(new(backHeadGlowmask, drawPosition, null, npc.GetAlpha(Color.White) * (1f - terminusDrawInterpolant), npc.rotation, backHeadTexture.Size() * 0.5f, npc.scale, 0, 0));

                float moveBackInterpolant = Utils.GetLerpValue(0f, 0.3f, hammerHeadRotation, true) * 34f;
                drawPosition += (npc.rotation + PiOver2).ToRotationVector2() * npc.scale * backHeadTexture.Height * 0.5f;
                drawPosition += (npc.rotation + PiOver2).ToRotationVector2() * npc.scale * moveBackInterpolant;

                // Draw the hammer head sides.
                Vector2 leftHeadOrigin = hammerHeadTexture.Size();
                Vector2 rightHeadOrigin = hammerHeadTexture.Size() * new Vector2(0f, 1f);
                float leftHeadRotation = npc.rotation - hammerHeadRotation;
                float rightHeadRotation = npc.rotation + hammerHeadRotation;
                Color generalColor = (colorOverride ?? npc.GetAlpha(Color.White)) * (1f - terminusDrawInterpolant);

                if (terminusDrawInterpolant < 1f)
                {
                    AEWShadowFormDrawSystem.LightAndDarkEffectsCache.Add(new(hammerHeadTexture, drawPosition, null, generalColor, leftHeadRotation, leftHeadOrigin, npc.scale, 0, 0));
                    AEWShadowFormDrawSystem.LightAndDarkEffectsCache.Add(new(hammerHeadGlowmask, drawPosition, null, generalColor, leftHeadRotation, leftHeadOrigin, npc.scale, 0, 0));
                    AEWShadowFormDrawSystem.AEWEyesDrawCache.Add(new(eyesGlowmask, drawPosition - npc.velocity, null, eyeColor, leftHeadRotation, leftHeadOrigin, npc.scale, 0, 0));

                    AEWShadowFormDrawSystem.LightAndDarkEffectsCache.Add(new(hammerHeadTexture, drawPosition, null, generalColor, rightHeadRotation, rightHeadOrigin, npc.scale, SpriteEffects.FlipHorizontally, 0));
                    AEWShadowFormDrawSystem.LightAndDarkEffectsCache.Add(new(hammerHeadGlowmask, drawPosition, null, generalColor, rightHeadRotation, rightHeadOrigin, npc.scale, SpriteEffects.FlipHorizontally, 0));
                    AEWShadowFormDrawSystem.AEWEyesDrawCache.Add(new(eyesGlowmask, drawPosition - npc.velocity, null, eyeColor, rightHeadRotation, rightHeadOrigin, npc.scale, SpriteEffects.FlipHorizontally, 0));
                }

                // Draw the terminus if necessary.
                if (terminusDrawInterpolant > 0f)
                {
                    Texture2D terminusTexture = ModContent.Request<Texture2D>("CalamityMod/Items/SummonItems/Terminus").Value;
                    AEWShadowFormDrawSystem.LightAndDarkEffectsCache.Add(new(terminusTexture, npc.Center - Main.screenPosition, null, Color.White * Pow(terminusDrawInterpolant, 6f), 0f, terminusTexture.Size() * 0.5f, 1f, 0, 0));
                }
            }
            drawInstance(npc.Center - Main.screenPosition);
        }

        public static void TryToDrawAbyssalBlackHole()
        {
            int aewIndex = NPC.FindFirstNPC(ModContent.NPCType<PrimordialWyrmHead>());
            if (!Main.npc.IndexInRange(aewIndex))
                return;

            float blackHoleInterpolant = Utils.GetLerpValue(0f, 0.26f, Main.npc[aewIndex].localAI[0], true);
            DrawAbyssalBlackHole(Main.npc[aewIndex].Center - Main.screenPosition, blackHoleInterpolant, blackHoleInterpolant * 0.86f);
        }

        public static void DrawAbyssalBlackHole(Vector2 drawPosition, float opacity, float scale)
        {
            if (opacity <= 0.01f || scale <= 0.01f)
                return;

            Texture2D noiseTexture = InfernumTextureRegistry.VoronoiShapes.Value;
            Vector2 origin = noiseTexture.Size() * 0.5f;
            Main.spriteBatch.EnterShaderRegion();

            GameShaders.Misc["CalamityMod:DoGPortal"].UseOpacity(opacity);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseColor(Color.Purple);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseSecondaryColor(Color.HotPink);
            GameShaders.Misc["CalamityMod:DoGPortal"].Apply();
            Main.spriteBatch.Draw(noiseTexture, drawPosition, null, Color.White, 0f, origin, scale, 0, 0f);

            GameShaders.Misc["CalamityMod:DoGPortal"].UseOpacity(opacity * 0.7f);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseColor(Color.Cyan);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseSecondaryColor(Color.Cyan);
            GameShaders.Misc["CalamityMod:DoGPortal"].Apply();
            Main.spriteBatch.Draw(noiseTexture, drawPosition, null, Color.White, 0f, origin, scale, 0, 0f);
            Main.spriteBatch.ExitShaderRegion();
        }
        #endregion Draw Effects

        #region Death Effects
        public override bool CheckDead(NPC npc)
        {
            // Just die as usual if the wyrm is killed during the death animation. This is done so that Cheat Sheet and other butcher effects can kill it quickly.
            if (npc.ai[0] == (int)AEWAttackType.DeathAnimation)
                return true;

            SelectNextAttack(npc);
            npc.ai[0] = (int)AEWAttackType.DeathAnimation;
            npc.life = npc.lifeMax;
            npc.active = true;
            npc.netUpdate = true;
            return false;
        }
        #endregion Death Effects

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n =>
            {
                if (TipsManager.ShouldUseJokeText)
                    return "Mods.InfernumMode.PetDialog.AEWJokeTip1";
                return string.Empty;
            };
        }
        #endregion Tips
    }
}
