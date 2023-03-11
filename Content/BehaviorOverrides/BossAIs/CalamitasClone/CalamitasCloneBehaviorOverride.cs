using CalamityMod;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.NPCs;
using CalamityMod.NPCs.CalClone;
using CalamityMod.Particles;
using CalamityMod.Particles.Metaballs;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Projectiles.Environment;
using CalamityMod.UI.CalamitasEnchants;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using CalamitasCloneBoss = CalamityMod.NPCs.CalClone.CalamitasClone;
using SCalBoss = CalamityMod.NPCs.SupremeCalamitas.SupremeCalamitas;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasClone
{
    public class CalamitasCloneBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<CalamitasCloneBoss>();

        #region Enumerations
        public enum CloneAttackType
        {
            SpawnAnimation,

            WandFireballs,
            SoulSeekerResurrection,
            ShadowTeleports,
            DarkOverheadFireball,
            ConvergingBookEnergy, // Nerd emoji.
            FireburstDashes,

            BrothersPhase,
        }
        #endregion

        #region AI

        public const float Phase2LifeRatio = 0.55f;

        public const float Phase3LifeRatio = 0.25f;

        public const int ArmRotationIndex = 5;

        public const int HexTypeIndex = 6;

        public const int HexType2Index = 7;

        public const int HasEnteredPhase2Index = 8;

        public static Primitive3DStrip HexStripDrawer
        {
            get;
            set;
        }

        public static float HexFadeInInterpolant
        {
            get;
            set;
        } = 1f;

        public static Color HexColor
        {
            get;
            set;
        }

        public static List<string> Hexes => new()
        {
            "Zeal",
            "Accentuation",
            "Catharsis",
            "Weakness",
            "Indignation"
        };

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio,
            Phase3LifeRatio
        };

        public override bool PreAI(NPC npc)
        {
            // FUCK YOU FUCK YOU FUCK YOU FUCK YOU FUCK YOU FUCK YOU FUCK
            if (npc.scale != 1f)
            {
                npc.width = 52;
                npc.height = 52;
                npc.scale = 1f;
            }

            // Do targeting.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Set the whoAmI variable globally.
            CalamityGlobalNPC.calamitas = npc.whoAmI;

            // Handle despawn behaviors.
            if (!target.active || target.dead || !npc.WithinRange(target.Center, 7200f))
            {
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * -28f, 0.08f);
                if (!npc.WithinRange(target.Center, 1450f))
                {
                    npc.life = 0;
                    npc.active = false;
                    npc.netUpdate = true;
                }
                return false;
            }

            int catharsisSoulReleaseRate = 90;
            bool anyBrothers = NPC.AnyNPCs(ModContent.NPCType<Cataclysm>()) || NPC.AnyNPCs(ModContent.NPCType<Catastrophe>());
            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float generalTimer = ref npc.ai[2];
            ref float hexApplicationPauseDelay = ref npc.ai[3];
            ref float backgroundEffectIntensity = ref npc.localAI[1];
            ref float blackFormInterpolant = ref npc.localAI[2];
            ref float eyeGleamInterpolant = ref npc.localAI[3];
            ref float armRotation = ref npc.Infernum().ExtraAI[ArmRotationIndex];
            ref float hexType = ref npc.Infernum().ExtraAI[HexTypeIndex];
            ref float hexType2 = ref npc.Infernum().ExtraAI[HexType2Index];
            ref float hasEnteredPhase2 = ref npc.Infernum().ExtraAI[HasEnteredPhase2Index];

            // Apply hexes to the target.
            string hexName = Hexes[(int)hexType];
            string hex2Name = Hexes[(int)hexType2];

            if (!anyBrothers && attackType != (int)CloneAttackType.BrothersPhase)
            {
                target.Infernum_CalCloneHex().ActivateHex(hexName);
                if (lifeRatio < Phase2LifeRatio)
                    target.Infernum_CalCloneHex().ActivateHex(hex2Name);
            }

            // Use a custom hitsound.
            npc.HitSound = SoundID.NPCHit49 with { Pitch = -0.56f };
            // Reset things every frame.
            npc.defDamage = 0;
            npc.damage = npc.defDamage;
            npc.dontTakeDamage = false;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.gfxOffY = 10f;

            if (hexApplicationPauseDelay >= 1f)
            {
                armRotation = armRotation.AngleLerp(MathHelper.Pi, 0.09f).AngleTowards(MathHelper.Pi, 0.045f);
                hexApplicationPauseDelay--;

                // Create magic on CalClone's hand.
                Vector2 armStart = npc.Center + new Vector2(npc.spriteDirection * 9.6f, -2f);
                Vector2 armEnd = armStart + (armRotation + MathHelper.PiOver2).ToRotationVector2() * npc.scale * 8f;
                Dust magic = Dust.NewDustPerfect(armEnd + Main.rand.NextVector2Circular(3f, 3f), 267);
                magic.color = CalamityUtils.MulticolorLerp(Main.rand.NextFloat(), Color.MediumPurple, Color.Red, Color.Orange, Color.Red);
                magic.noGravity = true;
                magic.velocity = -Vector2.UnitY.RotatedByRandom(0.22f) * Main.rand.NextFloat(0.4f, 18f);
                magic.scale = Main.rand.NextFloat(1f, 1.3f);

                // Create fire on the player.
                Color fireMistColor = Color.Lerp(Color.Red, Color.Yellow, Main.rand.NextFloat(0.25f, 0.85f));
                var mist = new MediumMistParticle(target.Center + Main.rand.NextVector2Circular(24f, 24f), Main.rand.NextVector2Circular(4.5f, 4.5f), fireMistColor, Color.Gray, Main.rand.NextFloat(0.6f, 1.3f), 198 - Main.rand.Next(50), 0.02f);
                GeneralParticleHandler.SpawnParticle(mist);

                return false;
            }

            // Give the target a soul seeker if they're in need of one because of their current hex.
            if (Main.netMode != NetmodeID.MultiplayerClient && target.Infernum_CalCloneHex().HexIsActive("Indignation") && target.ownedProjectileCounts[ModContent.ProjectileType<HauntingSoulSeeker>()] <= 0)
                Utilities.NewProjectileBetter(target.Center - Vector2.UnitY * 640f, Vector2.Zero, ModContent.ProjectileType<HauntingSoulSeeker>(), 0, 0f, npc.target);

            // Have the target release redirecting souls from out of them if they have the catharsis hex.
            if (generalTimer % catharsisSoulReleaseRate == 0f && target.Infernum_CalCloneHex().HexIsActive("Catharsis"))
            {
                SoundEngine.PlaySound(SoundID.NPCDeath39 with { Pitch = -0.8f, Volume = 0.15f }, target.Center);

                Vector2 soulShootDirection = (-target.velocity).RotatedByRandom(0.45f).SafeNormalize(Main.rand.NextVector2Unit());
                for (int i = 0; i < 8; i++)
                {
                    Color fireMistColor = Color.Lerp(Color.Red, Color.Yellow, Main.rand.NextFloat(0.25f, 0.85f));
                    var mist = new MediumMistParticle(target.Center + Main.rand.NextVector2Circular(24f, 24f), Main.rand.NextVector2Circular(4.5f, 4.5f) + soulShootDirection * 8f, fireMistColor, Color.Gray, Main.rand.NextFloat(0.6f, 1.3f), 192 - Main.rand.Next(50), 0.02f);
                    GeneralParticleHandler.SpawnParticle(mist);
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(target.Center, soulShootDirection * 12f, ModContent.ProjectileType<CatharsisSoul>(), 155, 0f);
            }

            if (hasEnteredPhase2 == 0f && lifeRatio < Phase2LifeRatio)
            {
                SelectNextAttack(npc);
                hasEnteredPhase2 = 1f;
                attackType = (int)CloneAttackType.BrothersPhase;
            }
            
            switch ((CloneAttackType)(int)attackType)
            {
                case CloneAttackType.SpawnAnimation:
                    DoBehavior_SpawnAnimation(npc, target, ref attackTimer, ref backgroundEffectIntensity, ref blackFormInterpolant, ref eyeGleamInterpolant, ref armRotation);
                    break;
                case CloneAttackType.WandFireballs:
                    DoBehavior_WandFireballs(npc, target, ref attackTimer, ref armRotation);
                    break;
                case CloneAttackType.SoulSeekerResurrection:
                    DoBehavior_SoulSeekerResurrection(npc, target, ref attackTimer, ref armRotation);
                    break;
                case CloneAttackType.ShadowTeleports:
                    DoBehavior_ShadowTeleports(npc, target, ref attackTimer, ref armRotation, ref blackFormInterpolant);
                    break;
                case CloneAttackType.DarkOverheadFireball:
                    DoBehavior_DarkOverheadFireball(npc, target, ref attackTimer, ref armRotation);
                    break;
                case CloneAttackType.ConvergingBookEnergy:
                    DoBehavior_ConvergingBookEnergy(npc, target, ref attackTimer, ref armRotation);
                    break;
                case CloneAttackType.FireburstDashes:
                    DoBehavior_FireburstDashes(npc, target, ref attackTimer, ref armRotation);
                    break;

                case CloneAttackType.BrothersPhase:
                    DoBehavior_BrothersPhase(npc, target, anyBrothers, ref attackTimer, ref armRotation);
                    break;
            }

            // Disable the base Calamity screen shader and background.
            if (Main.netMode != NetmodeID.Server)
                Filters.Scene["CalamityMod:CalamitasRun3"].Deactivate();

            attackTimer++;
            generalTimer++;
            return false;
        }

        public static void DoBehavior_SpawnAnimation(NPC npc, Player target, ref float attackTimer, ref float backgroundEffectIntensity, ref float blackFormInterpolant, ref float eyeGleamInterpolant, ref float armRotation)
        {
            int blackFadeoutTime = 30;
            int blackFadeinTime = 6;
            int maximumDarknessTime = 50;
            int eyeGleamTime = 44;

            // Calculate the black fade intensity. This is used to give an illusion that CalClone emerged from the shadows.
            InfernumMode.BlackFade = Utils.GetLerpValue(0f, blackFadeoutTime, attackTimer, true) * Utils.GetLerpValue(blackFadeoutTime + blackFadeinTime + maximumDarknessTime, blackFadeoutTime + maximumDarknessTime, attackTimer, true);

            // Respond the gravity and natural tile collision for the duration of the attack.
            npc.noGravity = false;
            npc.noTileCollide = false;

            // Don't exist yet if the fade effects are ongoing.
            if (attackTimer < blackFadeoutTime)
            {
                blackFormInterpolant = 1f;
                npc.Opacity = 0f;
                npc.dontTakeDamage = true;
                npc.ShowNameOnHover = false;
                npc.Center = target.Center + Vector2.UnitX * target.direction * 450f;
                while (Collision.SolidCollision(npc.TopLeft, npc.width, npc.height))
                    npc.position.Y -= 2f;
                npc.Calamity().ShouldCloseHPBar = true;
                npc.Calamity().ProvidesProximityRage = false;

                armRotation = 0f;
            }

            // Appear once they're done.
            else if (InfernumMode.BlackFade < 1f)
            {
                blackFormInterpolant = MathHelper.Clamp(blackFormInterpolant - 0.018f, 0f, 1f);
                npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.06f, 0f, 1f);
                npc.ShowNameOnHover = true;

                // Do an eye gleam effect.
                float gleamAnimationCompletion = Utils.GetLerpValue(blackFadeoutTime + blackFadeinTime + maximumDarknessTime, blackFadeoutTime + blackFadeinTime + maximumDarknessTime + eyeGleamTime, attackTimer, true);
                eyeGleamInterpolant = CalamityUtils.Convert01To010(gleamAnimationCompletion);
                if (attackTimer == blackFadeoutTime + blackFadeinTime + maximumDarknessTime)
                {
                    bool feelingLikeABigShot = Main.rand.NextBool(100) || Utilities.IsAprilFirst();
                    SoundEngine.PlaySound(feelingLikeABigShot ? InfernumSoundRegistry.GolemSpamtonSound : HeavenlyGale.LightningStrikeSound, target.Center);
                }

                // Look at the target.
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();

                backgroundEffectIntensity = MathHelper.Clamp(backgroundEffectIntensity + 0.011f, 0f, 1f);

                // Fly into the air and transition to the first attack after the background is fully dark.
                if (backgroundEffectIntensity >= 1f)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.VassalJumpSound with { Pitch = -0.4f, Volume = 1.6f }, target.Center);
                    SoundEngine.PlaySound(SCalBoss.SpawnSound with { Pitch = -0.12f, Volume = 0.7f }, target.Center);

                    npc.velocity.Y -= 23f;
                    Collision.HitTiles(npc.TopLeft, Vector2.UnitY * -12f, npc.width, npc.height + 100);
                    SelectNextAttack(npc);
                }
            }

            // Perform animation effects.
            npc.frameCounter += 0.2f;
        }

        public static void DoBehavior_WandFireballs(NPC npc, Player target, ref float attackTimer, ref float armRotation)
        {
            int wandChargeUpTime = 45;
            int wandAimDelay = 32;
            int wandAimTime = 30;
            int wandWaveTime = 45;
            int wandCycleTime = wandAimTime + wandWaveTime;
            int totalWandCycles = 2;
            int flameReleaseRate = 3;
            int wandAttackCycle = (int)(attackTimer - wandChargeUpTime - wandAimDelay) % wandCycleTime;

            int wandReelBackTime = 40;

            float fireShootSpeed = npc.Distance(target.Center) * 0.027f + 16.75f;
            Vector2 armStart = npc.Center + new Vector2(npc.spriteDirection * 9.6f, -2f);
            Vector2 wandEnd = armStart + (armRotation + MathHelper.Pi - MathHelper.PiOver2).ToRotationVector2() * npc.scale * 45f;
            wandEnd += (armRotation + MathHelper.Pi).ToRotationVector2() * npc.scale * npc.spriteDirection * -8f;
            ref float wandGlowInterpolant = ref npc.Infernum().ExtraAI[0];
            ref float throwingWand = ref npc.Infernum().ExtraAI[1];
            ref float wandWasThrown = ref npc.Infernum().ExtraAI[2];

            // Aim the wand at the sky.
            if (attackTimer < wandChargeUpTime && throwingWand == 0f)
            {
                armRotation = armRotation.AngleLerp(MathHelper.Pi, 0.06f).AngleTowards(MathHelper.Pi, 0.016f);
                npc.velocity *= 0.93f;
            }

            // Release lightning at the wand.
            if (attackTimer == wandChargeUpTime && throwingWand == 0f)
            {
                SoundEngine.PlaySound(HeavenlyGale.LightningStrikeSound, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 7; i++)
                    {
                        Vector2 lightningSpawnPosition = npc.Center - Vector2.UnitY.RotatedByRandom(0.51f) * Main.rand.NextFloat(900f, 1000f);
                        Vector2 lightningVelocity = (wandEnd - lightningSpawnPosition).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(6.4f, 6.7f);

                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(lightning =>
                        {
                            lightning.ModProjectile<BrimstoneLightning>().Destination = wandEnd;
                        });
                        Utilities.NewProjectileBetter(lightningSpawnPosition, lightningVelocity, ModContent.ProjectileType<BrimstoneLightning>(), 0, 0f, -1, lightningVelocity.ToRotation(), Main.rand.Next(100));
                    }

                    npc.velocity = Vector2.Zero;
                    npc.netUpdate = true;
                }
            }

            // Aim the wand at the target and hover near them.
            if (attackTimer >= wandChargeUpTime + wandAimDelay && throwingWand == 0f)
            {
                float idealRotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;

                // Wave the wand and release flame projectiles.
                if (wandAttackCycle >= wandAimTime)
                {
                    // Shoot fire.
                    if (wandAttackCycle % flameReleaseRate == 0f)
                    {
                        SoundEngine.PlaySound(SoundID.Item73, wandEnd);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            if (npc.velocity.Length() < 10f)
                                npc.velocity -= npc.SafeDirectionTo(target.Center) * 1.3f;
                            Vector2 fireShootVelocity = Vector2.Lerp(npc.SafeDirectionTo(wandEnd), npc.SafeDirectionTo(target.Center), 0.24f) * fireShootSpeed;
                            Utilities.NewProjectileBetter(wandEnd, fireShootVelocity, ModContent.ProjectileType<DarkMagicFlame>(), 155, 0f);
                        }

                        // Do funny screen effects.
                        Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 4f;
                    }

                    float aimCompletion = Utils.GetLerpValue(0f, wandWaveTime, wandAttackCycle - wandAimTime, true);
                    float aimAngularOffset = MathF.Sin(3f * MathHelper.Pi * aimCompletion) * 1.09f;
                    idealRotation += aimAngularOffset;
                }
                else
                    npc.Center = Vector2.Lerp(npc.Center, target.Center, 0.024f);

                armRotation = armRotation.AngleLerp(idealRotation, 0.08f).AngleTowards(idealRotation, 0.017f);

                // Fly near the target.
                Vector2 idealVelocity = npc.SafeDirectionTo(target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * 400f) * 16f;
                npc.SimpleFlyMovement(idealVelocity, 0.22f);

                // Emit cinders at the end of the wand.
                Dust cinder = Dust.NewDustPerfect(wandEnd, Main.rand.NextBool() ? 169 : 60, -Vector2.UnitY.RotatedByRandom(0.56f) * Main.rand.NextFloat(2f));
                cinder.scale *= 1.4f;
                cinder.color = Color.Lerp(Color.White, Color.Orange, Main.rand.NextFloat());
                cinder.noLight = true;
                cinder.noLightEmittence = true;
                cinder.noGravity = true;
            }

            // After the cycles have completed, move the arm back in anticipation before throwing it.
            if (throwingWand == 1f)
            {
                npc.velocity *= 0.94f;

                // Move the arm back.
                if (attackTimer >= wandReelBackTime)
                {
                    float idealRotation = MathHelper.Pi - 2.44f * npc.spriteDirection;
                    if (MathHelper.Distance(MathHelper.WrapAngle(idealRotation), MathHelper.WrapAngle(armRotation)) > 0.2f)
                        armRotation -= npc.spriteDirection * 0.18f;
                }
                else
                {
                    float idealRotation = (-MathHelper.PiOver2 - 0.72f) * npc.spriteDirection;
                    armRotation = armRotation.AngleLerp(idealRotation, 0.075f).AngleTowards(idealRotation, 0.019f);
                }

                // Throw the wand.
                if (attackTimer == wandReelBackTime)
                {
                    SoundEngine.PlaySound(SoundID.Item117 with { Pitch = 0.3f }, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Utilities.NewProjectileBetter(armStart, (target.Center - armStart).SafeNormalize(Vector2.UnitY) * 18f, ModContent.ProjectileType<CharredWand>(), 0, 0f);
                        wandWasThrown = 1f;
                        npc.netUpdate = true;
                    }
                }

                if (attackTimer >= wandReelBackTime + 132f)
                    SelectNextAttack(npc);
            }

            // Look at the target.
            npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();

            // Perform animation effects.
            npc.frameCounter += 0.2f;

            if (attackTimer >= wandChargeUpTime + wandAimDelay + wandCycleTime * totalWandCycles && throwingWand == 0f)
            {
                throwingWand = 1f;
                attackTimer = 0f;
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_SoulSeekerResurrection(NPC npc, Player target, ref float attackTimer, ref float armRotation)
        {
            int redirectTime = 45;
            int seekerSummonTime = 30;
            int seekerShootTime = 240;
            int laserTelegraphTime = 120;
            int laserShootTime = EntropyBeam.Lifetime;
            Vector2 armStart = npc.Center + new Vector2(npc.spriteDirection * 9.6f, -2f);
            Vector2 staffEnd = armStart + (armRotation + MathHelper.Pi - MathHelper.PiOver2).ToRotationVector2() * npc.scale * 66f;
            ref float totalSummonedSoulSeekers = ref npc.Infernum().ExtraAI[0];
            ref float telegraphInterpolant = ref npc.Infernum().ExtraAI[1];
            ref float beamDirection = ref npc.Infernum().ExtraAI[2];

            // Hover to the side of the target.
            if (attackTimer <= redirectTime)
            {
                Vector2 hoverDestination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * 400f;
                npc.Center = Vector2.Lerp(npc.Center, hoverDestination, 0.03f).MoveTowards(hoverDestination, 2.4f);
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 18f, 0.32f);

                if (Collision.SolidCollision(npc.TopLeft, npc.width, npc.height + 800))
                    npc.position.Y -= 10f;
            }

            // Aim Entropy's Vigil downwards and use it to raise soul seekers from the dead.
            else if (attackTimer <= redirectTime + seekerSummonTime)
            {
                npc.velocity *= 0.9f;

                float idealRotation = Utils.Remap(attackTimer - redirectTime, 0f, seekerSummonTime, -0.54f, 0.54f);
                armRotation = armRotation.AngleLerp(idealRotation, 0.2f).AngleTowards(idealRotation, 0.03f);
                if (attackTimer % 5f == 4f)
                {
                    SoundEngine.PlaySound(SoundID.Item74, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(staffEnd, npc.SafeDirectionTo(staffEnd), ModContent.ProjectileType<SoulSeekerResurrectionBeam>(), 0, 0f);
                }
            }

            // Hover near the target.
            else
            {
                if (attackTimer <= redirectTime + seekerSummonTime + seekerShootTime)
                {
                    Vector2 hoverDestination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * 300f;
                    hoverDestination.Y -= 60f;
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 18f, 0.32f);

                    float idealRotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;
                    armRotation = armRotation.AngleTowards(idealRotation, 0.05f);
                }

                // Make all seekers go away.
                if (attackTimer == redirectTime + seekerSummonTime + seekerShootTime)
                {
                    SoundEngine.PlaySound(CalamitasEnchantUI.EXSound with { Pitch = -0.5f }, target.Center);

                    // Teleport above the player and make all seekers leave.
                    npc.Center = target.Center - Vector2.UnitY * 350f;
                    npc.velocity = Vector2.Zero;
                    armRotation = MathHelper.Pi;

                    armStart = npc.Center + new Vector2(npc.spriteDirection * 9.6f, -2f);
                    staffEnd = armStart + (armRotation + MathHelper.Pi - MathHelper.PiOver2).ToRotationVector2() * npc.scale * 66f;
                    for (int i = 0; i < 35; i++)
                    {
                        Color fireColor = Main.rand.NextBool() ? Color.Yellow : Color.Red;
                        CloudParticle fireCloud = new(staffEnd, (MathHelper.TwoPi * i / 35f).ToRotationVector2() * 20f, fireColor, Color.DarkGray, 50, Main.rand.NextFloat(2.6f, 3.4f));
                        GeneralParticleHandler.SpawnParticle(fireCloud);
                    }

                    ScreenEffectSystem.SetBlurEffect(staffEnd, 0.7f, 45);
                    target.Infernum_Camera().CurrentScreenShakePower = 10f;

                    Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<DarkMagicFlame>());

                    int seekerID = ModContent.NPCType<SoulSeeker>();
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC n = Main.npc[i];

                        if (n.active && n.type == seekerID)
                        {
                            n.Infernum().ExtraAI[0] = 1f;
                            n.netUpdate = true;
                        }
                    }
                }

                // Aim the staff at the target in anticipation of the laser.
                if (attackTimer <= redirectTime + seekerSummonTime + seekerShootTime + laserTelegraphTime)
                {
                    // Play a charge telegraph sound.
                    if (attackTimer == redirectTime + seekerSummonTime + seekerShootTime)
                        SoundEngine.PlaySound(InfernumSoundRegistry.EntropyRayChargeSound, target.Center);

                    float telegraphCompletion = Utils.GetLerpValue(0f, laserTelegraphTime, attackTimer - redirectTime - seekerSummonTime - seekerShootTime, true);
                    telegraphInterpolant = Utils.GetLerpValue(0f, 0.67f, telegraphCompletion, true) * Utils.GetLerpValue(1f, 0.84f, telegraphCompletion, true);

                    float idealRotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;
                    armRotation = armRotation.AngleLerp(idealRotation, 0.04f).AngleTowards(idealRotation, 0.01f);
                }

                // Fire the laser.
                if (attackTimer == redirectTime + seekerSummonTime + seekerShootTime + laserTelegraphTime)
                {
                    ScreenEffectSystem.SetBlurEffect(staffEnd, 0.7f, 45);
                    target.Infernum_Camera().CurrentScreenShakePower = 10f;
                    SoundEngine.PlaySound(InfernumSoundRegistry.EntropyRayFireSound, target.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Utilities.NewProjectileBetter(npc.Center, armRotation.ToRotationVector2(), ModContent.ProjectileType<EntropyBeam>(), 240, 0f);
                        beamDirection = (MathHelper.WrapAngle(npc.AngleTo(target.Center) - armRotation - MathHelper.PiOver2) > 0f).ToDirectionInt();
                        npc.netUpdate = true;
                    }
                }

                // Spin the laser after it appears.
                if (attackTimer >= redirectTime + seekerSummonTime + seekerShootTime + laserTelegraphTime)
                    armRotation += beamDirection * 0.023f;

                if (attackTimer >= redirectTime + seekerSummonTime + seekerShootTime + laserTelegraphTime + laserShootTime)
                    SelectNextAttack(npc);
            }

            // Look at the target.
            if (attackTimer < redirectTime + seekerSummonTime + seekerShootTime + laserTelegraphTime)
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();

            // Perform animation effects.
            npc.frameCounter += 0.2f;
        }

        public static void DoBehavior_ShadowTeleports(NPC npc, Player target, ref float attackTimer, ref float armRotation, ref float blackFormInterpolant)
        {
            int jitterTime = 45;
            int disappearTime = 20;
            int sitTime = 14;
            int fadeOutTime = 25;
            int teleportCount = 6;
            int wrappedAttackTimer = (int)(attackTimer - jitterTime) % (disappearTime + sitTime + fadeOutTime);
            ref float teleportOffsetAngle = ref npc.Infernum().ExtraAI[0];
            ref float teleportCounter = ref npc.Infernum().ExtraAI[1];

            armRotation = 0f;

            // Jitter in place and become transluscent.
            if (attackTimer <= jitterTime)
            {
                float jitterInterpolant = Utils.GetLerpValue(0f, jitterTime, attackTimer, true);
                npc.Center += Main.rand.NextVector2Circular(3f, 3f) * jitterInterpolant;
                npc.Opacity = MathHelper.Lerp(1f, 0.5f, jitterInterpolant);
            }

            if (attackTimer >= jitterTime)
            {
                // Dissipate into shadow particles.
                if (wrappedAttackTimer == 0f)
                {
                    if (attackTimer == jitterTime)
                        teleportOffsetAngle = MathHelper.TwoPi * Main.rand.Next(4) / 4f;
                    else
                    {
                        teleportOffsetAngle += MathHelper.TwoPi / teleportCount;
                        teleportCounter++;
                    }

                    npc.velocity = Vector2.Zero;
                    npc.Opacity = 0f;
                    npc.dontTakeDamage = true;
                    npc.netUpdate = true;
                    if (teleportCounter >= teleportCount - 1f)
                    {
                        npc.Opacity = 1f;
                        SelectNextAttack(npc);
                        return;
                    }

                    SoundEngine.PlaySound(InfernumSoundRegistry.SizzleSound, target.Center);

                    if (Main.netMode != NetmodeID.Server)
                    {
                        var cloneTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/CalamitasClone/CalamitasCloneSingleFrame", AssetRequestMode.ImmediateLoad).Value;
                        cloneTexture.CreateMetaballsFromTexture(ref FusableParticleManager.GetParticleSetByType<ShadowDemonParticleSet>().Particles, npc.Center, npc.rotation, npc.scale, 28f, 10);
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 12; i++)
                            Utilities.NewProjectileBetter(npc.Center, Main.rand.NextVector2Unit() * Main.rand.NextFloat(9f, 14f), ModContent.ProjectileType<ShadowBlob>(), 0, 0f);
                    }
                }

                // Hover above the target.
                if (wrappedAttackTimer <= disappearTime)
                {
                    npc.Center = target.Center + teleportOffsetAngle.ToRotationVector2() * 396f;
                    npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();

                    // Create shadow particles shortly before fully appearing.
                    if (wrappedAttackTimer >= disappearTime - 15f)
                    {
                        npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.07f, 0f, 1f);
                        npc.dontTakeDamage = true;
                        for (int i = 0; i < (1f - npc.Opacity) * 24f; i++)
                        {
                            Color shadowMistColor = Color.Lerp(Color.MediumPurple, Color.DarkBlue, Main.rand.NextFloat(0.66f));
                            var mist = new MediumMistParticle(npc.Center + npc.Opacity * Main.rand.NextVector2Circular(90f, 90f), Main.rand.NextVector2Circular(5f, 5f), shadowMistColor, Color.DarkGray, Main.rand.NextFloat(0.55f, 0.7f), 172f, Main.rand.NextFloatDirection() * 0.012f);
                            GeneralParticleHandler.SpawnParticle(mist);
                        }
                    }
                    else
                        npc.Opacity = 0f;
                }

                if (wrappedAttackTimer == disappearTime)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.CalCloneTeleportSound, target.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 7; i++)
                        {
                            float shootOffsetAngle = MathHelper.Lerp(-0.63f, 0.63f, i / 6f);
                            Vector2 shootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(shootOffsetAngle) * 12f;
                            Utilities.NewProjectileBetter(npc.Center + shootVelocity, shootVelocity, ModContent.ProjectileType<DarkMagicFlame>(), 155, 0f);
                        }
                    }
                }
            }

            blackFormInterpolant = MathF.Pow(1f - npc.Opacity, 0.2f) * 3f;
        }

        public static void DoBehavior_DarkOverheadFireball(NPC npc, Player target, ref float attackTimer, ref float armRotation)
        {
            int redirectTime = 45;
            int fireOrbReleaseDelay = 15;
            int boltReleaseDelay = 60;
            int boltCircleReleaseRate = 25;
            int boltShootCycleTime = 90;
            int wrappedAttackTimer = (int)(attackTimer - redirectTime - fireOrbReleaseDelay - boltReleaseDelay) % boltShootCycleTime;
            int fireShootTime = 240;
            var fireOrbs = Utilities.AllProjectilesByID(ModContent.ProjectileType<LargeDarkFireOrb>());
            bool readyToBlowUpFireOrb = attackTimer >= redirectTime + fireOrbReleaseDelay + boltReleaseDelay + fireShootTime;
            bool canShootFire = attackTimer >= redirectTime + fireOrbReleaseDelay + boltReleaseDelay && !readyToBlowUpFireOrb;
            Vector2 armStart = npc.Center + new Vector2(npc.spriteDirection * 9.6f, -2f);
            Vector2 armEnd = armStart + (armRotation + MathHelper.PiOver2).ToRotationVector2() * npc.scale * 8f;
            ref float fireShootCounter = ref npc.Infernum().ExtraAI[0];
            ref float isSlammingFireballDown = ref npc.Infernum().ExtraAI[1];
            ref float fireballHasExploded = ref npc.Infernum().ExtraAI[2];

            if (fireballHasExploded == 0f)
            {
                // Hover above the target at first.
                if (attackTimer < redirectTime)
                {
                    Vector2 hoverDestination = target.Center - Vector2.UnitY * 200f;
                    Vector2 idealVelocity = (hoverDestination - npc.Center) * 0.06f;
                    npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.12f);

                    // Look in the direction of the player if not extremely close to them horizontally.
                    if (MathHelper.Distance(target.Center.X, npc.Center.X) >= 50f)
                        npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                }

                // Afterwards have CalClone raise her arm up towards the fire orb and slow down.
                else
                {
                    if (npc.velocity.Length() > 0.001f)
                        npc.velocity = npc.velocity.ClampMagnitude(0f, 9f) * 0.8f;
                    armRotation = armRotation.AngleLerp(MathHelper.Pi, 0.05f).AngleTowards(MathHelper.Pi, 0.015f);

                    if (!readyToBlowUpFireOrb)
                    {
                        Dust magic = Dust.NewDustPerfect(armEnd + Main.rand.NextVector2Circular(3f, 3f), 267);
                        magic.color = CalamityUtils.MulticolorLerp(Main.rand.NextFloat(), Color.MediumPurple, Color.Red, Color.Orange, Color.Red);
                        magic.noGravity = true;
                        magic.velocity = -Vector2.UnitY.RotatedByRandom(0.22f) * Main.rand.NextFloat(0.4f, 18f);
                        magic.scale = Main.rand.NextFloat(1f, 1.3f);
                    }
                }

                // Prepare the fire orb.
                if (attackTimer == redirectTime + fireOrbReleaseDelay)
                {
                    SoundEngine.PlaySound(SoundID.Item163 with { Pitch = 0.08f }, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 fireOrbSpawnPosition = npc.Top - Vector2.UnitY * (LargeDarkFireOrb.MaxFireOrbRadius - 75f);
                        Utilities.NewProjectileBetter(fireOrbSpawnPosition, Vector2.Zero, ModContent.ProjectileType<LargeDarkFireOrb>(), 0, 0f);
                    }
                }

                // Release fire from the orb.
                if (canShootFire && wrappedAttackTimer <= boltShootCycleTime - 30f && wrappedAttackTimer % boltCircleReleaseRate == boltCircleReleaseRate - 1f)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.SizzleSound, target.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient && fireOrbs.Any())
                    {
                        int fireShootCount = (int)Utils.Remap(npc.Distance(target.Center), 950f, 3000f, 24f, 48f);
                        if (fireShootCount % 2 != 0)
                            fireShootCount++;
                        if (target.Infernum_CalCloneHex().HexIsActive("Accentuation"))
                            fireShootCount -= 8;

                        float fireShootSpeed = Utils.Remap(npc.Distance(target.Center), 800f, 3000f, 8.5f, 70f);
                        Vector2 fireOrbCenter = fireOrbs.First().Center;
                        for (int i = 0; i < fireShootCount; i++)
                        {
                            Vector2 fireShootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.TwoPi * (i + (fireShootCounter % 2f == 0f ? 0.5f : 0f)) / fireShootCount) * fireShootSpeed;
                            Utilities.NewProjectileBetter(fireOrbCenter + fireShootVelocity * 5f, fireShootVelocity, ModContent.ProjectileType<DarkMagicFlame>(), 155, 0f);
                        }

                        fireShootCounter++;
                        npc.netUpdate = true;
                    }
                }

                // Blow up the fire orb.
                if (readyToBlowUpFireOrb && fireOrbs.Any())
                {
                    Projectile fireOrb = fireOrbs.First();
                    float idealHoverDestinationY = npc.Center.Y - LargeDarkFireOrb.MaxFireOrbRadius - 120f;
                    if (fireOrb.Center.Y >= idealHoverDestinationY + 10f && fireOrb.velocity.Length() < 2f && fireOrb.timeLeft >= 40)
                        fireOrb.Center = new Vector2(fireOrb.Center.X, MathHelper.Lerp(fireOrb.Center.Y, idealHoverDestinationY, 0.12f));

                    // Make the orb slam down.
                    else if (isSlammingFireballDown == 0f)
                    {
                        fireOrb.velocity = Vector2.UnitY * 8f;
                        fireOrb.netUpdate = true;
                        isSlammingFireballDown = 1f;
                        npc.netUpdate = true;
                    }
                }
            }

            // Create meteors from above.
            else
            {
                if (attackTimer == 1f)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceLavaEruptionSmallSound, target.Center);
                    ScreenEffectSystem.SetFlashEffect(target.Center - Vector2.UnitY * 500f, 4f, 35);
                    target.Infernum_Camera().CurrentScreenShakePower = 10f;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (float dx = -1300f; dx < 1300f; dx += 150f)
                        {
                            Vector2 meteorSpawnPosition = target.Center + new Vector2(dx - 60f, Math.Abs(dx) * -0.35f - 600f);
                            Vector2 meteorShootVelocity = Vector2.UnitY * 15f;
                            Utilities.NewProjectileBetter(meteorSpawnPosition, meteorShootVelocity, ModContent.ProjectileType<BrimstoneMeteor>(), 160, 0f, -1, 0f, target.Bottom.Y);
                        }
                    }
                }

                if (attackTimer >= 90f)
                    SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_ConvergingBookEnergy(NPC npc, Player target, ref float attackTimer, ref float armRotation)
        {
            int bookAppearTime = 30;
            int sparkSpiralCount = 7;
            int sparkReleaseRate = 10;
            int sparkReleaseTime = 240;
            int bookExplodeDelay = 180;
            int dartShootCount = 24;
            float sparkShootSpeed = 4.5f;
            float dartShootSpeed = 4.5f;
            float dartAngularVelocity = MathHelper.ToRadians(0.6f);
            Vector2 bookCenter = npc.Center - Vector2.UnitX * npc.scale * npc.spriteDirection * 12f;
            ref float bookAppearInterpolant = ref npc.Infernum().ExtraAI[0];
            ref float sparkShootOffsetAngle = ref npc.Infernum().ExtraAI[1];
            ref float bookJitterInterpolant = ref npc.Infernum().ExtraAI[2];
            ref float bookHasExploded = ref npc.Infernum().ExtraAI[3];

            if (bookHasExploded == 0f)
            {
                // Hover to the side of the target and create the book.
                if (attackTimer <= bookAppearTime)
                {
                    if (attackTimer == 1f)
                        SoundEngine.PlaySound(SoundID.DD2_EtherianPortalDryadTouch, target.Center);
                    bookAppearInterpolant = Utils.GetLerpValue(0f, bookAppearTime, attackTimer, true);
                    armRotation = armRotation.AngleLerp(MathHelper.PiOver2 * npc.spriteDirection, 0.37f * bookAppearInterpolant);

                    Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 350f, -120f);
                    Vector2 idealVelocity = (hoverDestination - npc.Center) * 0.067f;
                    npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.13f);

                    // Look in the direction of the player if not extremely close to them horizontally.
                    if (MathHelper.Distance(target.Center.X, npc.Center.X) >= 50f)
                        npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                }
                else
                {
                    // Emit particles off the book.
                    Color magicColor = CalamityUtils.MulticolorLerp(Main.rand.NextFloat(), Color.MediumPurple, Color.Red, Color.Orange, Color.Red);
                    GlowyLightParticle magic = new(bookCenter + Main.rand.NextVector2Circular(8f, 2f), -Vector2.UnitY.RotatedByRandom(0.54f) * Main.rand.NextFloat(0.4f, 3f), magicColor, 20, Main.rand.NextFloat(0.09f, 0.15f), 0.87f, false);
                    GeneralParticleHandler.SpawnParticle(magic);

                    npc.velocity *= 0.7f;
                    npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();

                    // Don't get stuck in blocks.
                    if (Collision.SolidCollision(npc.TopLeft, npc.width, npc.height + 300))
                    {
                        npc.Center = npc.Center.MoveTowards(target.Center, 4f);
                        npc.position.Y -= 10f;
                    }
                }
            }
            else
            {
                Vector2 hoverDestination = Vector2.Lerp(npc.Center, target.Center, 0.5f);
                Vector2 idealVelocity = (hoverDestination - npc.Center) * 0.067f;
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.04f);
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();

                if (attackTimer >= 90f)
                    SelectNextAttack(npc);
            }

            // Emit energy spirals.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= bookAppearTime && attackTimer <= bookAppearTime + sparkReleaseTime && attackTimer % sparkReleaseRate == sparkReleaseRate - 1f && bookHasExploded == 0f)
            {
                for (int i = 0; i < sparkSpiralCount; i++)
                {
                    Vector2 sparkSpawnPosition = npc.Center + (MathHelper.TwoPi * i / sparkSpiralCount + sparkShootOffsetAngle).ToRotationVector2() * 1800f;
                    Vector2 sparkVelocity = (npc.Center - sparkSpawnPosition).SafeNormalize(Vector2.UnitY) * sparkShootSpeed;
                    Utilities.NewProjectileBetter(sparkSpawnPosition, sparkVelocity, ModContent.ProjectileType<ConvergingShadowSpark>(), 160, 0f);
                }
                sparkShootOffsetAngle += MathHelper.ToRadians(12f);
            }

            // Make the book jitter before exploding.
            if (bookHasExploded == 0f)
                bookJitterInterpolant = Utils.GetLerpValue(bookAppearTime + sparkReleaseTime, bookAppearTime + sparkReleaseTime + bookExplodeDelay, attackTimer, true);

            // Make the book explode.
            if (bookJitterInterpolant >= 1f && bookHasExploded == 0f)
            {
                // Do funny screen stuff.
                Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 12f;
                ScreenEffectSystem.SetFlashEffect(npc.Center, 2f, 45);

                SoundEngine.PlaySound(SCalBoss.BrimstoneBigShotSound, npc.Center);
                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceLavaEruptionSmallSound, npc.Center);

                // Create explosion particles.
                if (Main.netMode != NetmodeID.Server)
                {
                    for (int i = 0; i < 35; i++)
                    {
                        Color explosionFireColor = Color.Lerp(Color.Orange, Color.Red, Main.rand.NextFloat(0.7f));
                        SmallSmokeParticle explosion = new(bookCenter + Main.rand.NextVector2Circular(64f, 64f), Main.rand.NextVector2Circular(10f, 10f) - Vector2.UnitY * 11f, explosionFireColor, Color.DarkGray, Main.rand.NextFloat(1f, 1.15f), 255f, Main.rand.NextFloatDirection() * 0.015f);
                        GeneralParticleHandler.SpawnParticle(explosion);
                    }

                    for (int i = 0; i < 13; i++)
                    {
                        Gore page = Gore.NewGorePerfect(npc.GetSource_FromAI(), bookCenter + Main.rand.NextVector2Circular(30f, 30f), Main.rand.NextVector2Circular(7.5f, 7.5f) - Vector2.UnitY * 11f, 1007);
                        page.timeLeft = 60;
                        page.alpha = 50;
                    }
                }

                // Explode into a barrage of arcing darts.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < dartShootCount; i++)
                    {
                        Vector2 dartVelocity = (MathHelper.TwoPi * i / dartShootCount).ToRotationVector2() * dartShootSpeed;
                        Utilities.NewProjectileBetter(bookCenter, dartVelocity * 0.5f, ModContent.ProjectileType<ArcingBrimstoneDart>(), 160, 0f, -1, -dartAngularVelocity, 0f);
                        Utilities.NewProjectileBetter(bookCenter, dartVelocity, ModContent.ProjectileType<ArcingBrimstoneDart>(), 160, 0f, -1, dartAngularVelocity, 0f);
                    }
                }

                attackTimer = 0f;
                bookHasExploded = 1f;
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_FireburstDashes(NPC npc, Player target, ref float attackTimer, ref float armRotation)
        {
            int hoverTime = 30;
            int chargeTime = 26;
            int chargeCount = 4;
            int flameShootCount = 19;
            int chargeSlowdowntime = 12;
            int wrappedAttackTimer = (int)attackTimer % (hoverTime + chargeTime + chargeSlowdowntime);
            float baseChargeSpeed = 12f;
            float flameShootSpeed = 8f;
            float chargeAcceleration = 1.75f;
            ref float shieldScale = ref npc.Infernum().ExtraAI[0];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[1];

            // Hover to the side of the target in anticipation of the charge.
            if (wrappedAttackTimer <= hoverTime)
            {
                Vector2 hoverDestination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * 400f;
                Vector2 idealVelocity = (hoverDestination - npc.Center) * 0.07f;
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.1f);
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();

                armRotation = armRotation.AngleTowards(npc.AngleTo(target.Center) - MathHelper.PiOver2, 0.25f);

                // Make the shield appear.
                shieldScale = MathHelper.Clamp(shieldScale + 0.04f, 0f, 1f);
            }
            else if (npc.velocity.Length() >= 10f)
                npc.damage = 155;

            // Charge at the target.
            if (wrappedAttackTimer == hoverTime + 1f)
            {
                SoundEngine.PlaySound(SCalBoss.DashSound, npc.Center);
                npc.velocity = npc.SafeDirectionTo(target.Center) * baseChargeSpeed;
            }
            
            // Accelerate after charging.
            if (wrappedAttackTimer >= hoverTime + 1f && wrappedAttackTimer <= hoverTime + chargeTime)
                npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * (npc.velocity.Length() + chargeAcceleration);

            // Slow down in anticipation of the next charge.
            if (wrappedAttackTimer >= hoverTime + chargeTime)
            {
                // Release a burst of flames in all directions.
                if (wrappedAttackTimer == hoverTime + chargeTime + 11f)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.SizzleSound, target.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        chargeCounter++;
                        if (chargeCounter >= chargeCount)
                        {
                            npc.velocity *= 0.5f;
                            SelectNextAttack(npc);
                            return;
                        }

                        for (int i = 0; i < flameShootCount; i++)
                        {
                            Vector2 flameShootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.TwoPi * i / flameShootCount) * flameShootSpeed;
                            Utilities.NewProjectileBetter(npc.Center + flameShootVelocity * 2f, flameShootVelocity, ModContent.ProjectileType<DarkMagicFlame>(), 160, 0f);
                        }
                        npc.netUpdate = true;
                    }
                }

                npc.velocity *= 0.85f;
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                armRotation = armRotation.AngleTowards(npc.AngleTo(target.Center) - MathHelper.PiOver2, 0.25f);
                if (chargeCounter >= chargeCount - 1f)
                    shieldScale *= 0.85f;
            }
        }

        public static void DoBehavior_BrothersPhase(NPC npc, Player target, bool anyBrothers, ref float attackTimer, ref float armRotation)
        {
            int rumbleTime = 240;

            // Aim the arm downward.
            armRotation = armRotation.AngleTowards(0f, 0.02f);

            // Clear away projectiles from old attacks at first.
            if (attackTimer <= 2f)
            {
                if (attackTimer <= 1f)
                    SoundEngine.PlaySound(InfernumSoundRegistry.SCalBrothersSpawnSound);

                int[] projectilesToDelete = new int[]
                {
                    ModContent.ProjectileType<ArcingBrimstoneDart>(),
                    ModContent.ProjectileType<BrimstoneMeteor>(),
                    ModContent.ProjectileType<CatharsisSoul>(),
                    ModContent.ProjectileType<CharredWand>(),
                    ModContent.ProjectileType<ConvergingShadowSpark>(),
                    ModContent.ProjectileType<DarkMagicFlame>()
                };
                Utilities.DeleteAllProjectiles(false, projectilesToDelete);
            }

            // Fall to the ground and stop taking damage in anticipation of the summoning.
            if (attackTimer <= rumbleTime)
            {
                npc.noGravity = false;
                npc.noTileCollide = false;
                npc.dontTakeDamage = true;
                npc.velocity.X *= 0.75f;
                npc.Opacity = 1f;
                target.Infernum_Camera().CurrentScreenShakePower = attackTimer / rumbleTime * 6f;
            }

            if (attackTimer == rumbleTime - 5f)
                Utilities.DisplayText($"Destroy {(target.Male ? "him" : "her")}, my brothers.", Color.Orange);

            // Have CalClone teleport away and summon the brothers.
            if (attackTimer == rumbleTime)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.CalCloneTeleportSound);

                // Have Calclone explode into shadow blobs.
                if (Main.netMode != NetmodeID.Server)
                {
                    var cloneTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/CalamitasClone/CalamitasCloneSingleFrame", AssetRequestMode.ImmediateLoad).Value;
                    cloneTexture.CreateMetaballsFromTexture(ref FusableParticleManager.GetParticleSetByType<ShadowDemonParticleSet>().Particles, npc.Center, npc.rotation, npc.scale, 18f, 10);
                }
                
                // Summon Catatrophe and Cataclysm.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int cataclysm = NPC.NewNPC(npc.GetSource_FromAI(), (int)target.Center.X - 1000, (int)target.Center.Y - 1000, ModContent.NPCType<Cataclysm>());
                    CalamityUtils.BossAwakenMessage(cataclysm);

                    int catastrophe = NPC.NewNPC(npc.GetSource_FromAI(), (int)target.Center.X + 1000, (int)target.Center.Y - 1000, ModContent.NPCType<Catastrophe>());
                    CalamityUtils.BossAwakenMessage(catastrophe);
                   
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, cataclysm);
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, catastrophe);

                    for (int i = 0; i < 50; i++)
                    {
                        float seekerAngle = MathHelper.TwoPi * i / 50f;
                        NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<SoulSeeker2>(), npc.whoAmI, seekerAngle);
                    }
                }
            }

            if (attackTimer >= rumbleTime)
            {
                // Move the ring towards the target.
                npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center - Vector2.UnitY * 350f) * 4.5f, 0.2f);

                npc.ShowNameOnHover = false;
                npc.dontTakeDamage = true;
                npc.Opacity = 0f;
            }

            if (attackTimer >= rumbleTime + 5f && !anyBrothers)
            {
                npc.Opacity = 1f;
                npc.Center = target.Center - Vector2.UnitY * 560f;
                npc.velocity = Vector2.Zero;
                npc.noGravity = true;
                SelectNextAttack(npc);
            }
        }

        public static void SelectNextAttack(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool phase2 = lifeRatio < Phase2LifeRatio;
            CloneAttackType currentAttack = (CloneAttackType)npc.ai[0];
            CloneAttackType nextAttack = CloneAttackType.DarkOverheadFireball;
            switch (currentAttack)
            {
                case CloneAttackType.SpawnAnimation:
                case CloneAttackType.BrothersPhase:
                    nextAttack = CloneAttackType.WandFireballs;
                    break;
                case CloneAttackType.WandFireballs:
                    nextAttack = CloneAttackType.SoulSeekerResurrection;
                    break;
                case CloneAttackType.SoulSeekerResurrection:
                    nextAttack = CloneAttackType.ShadowTeleports;
                    break;
                case CloneAttackType.ShadowTeleports:
                    nextAttack = CloneAttackType.DarkOverheadFireball;
                    break;
                case CloneAttackType.DarkOverheadFireball:
                    nextAttack = CloneAttackType.ConvergingBookEnergy;
                    break;
                case CloneAttackType.ConvergingBookEnergy:
                    nextAttack = phase2 ? CloneAttackType.FireburstDashes : CloneAttackType.WandFireballs;
                    break;
                case CloneAttackType.FireburstDashes:
                    nextAttack = CloneAttackType.WandFireballs;
                    break;
            }

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            // Reroll hex indices.
            ref float hexType = ref npc.Infernum().ExtraAI[HexTypeIndex];
            ref float hexType2 = ref npc.Infernum().ExtraAI[HexType2Index];
            float oldHexType = hexType;
            float oldHexType2 = hexType2;
            do
                hexType = Main.rand.Next(5);
            while (hexType == oldHexType || hexType == oldHexType2);
            do
                hexType2 = Main.rand.Next(5);
            while (hexType2 == oldHexType || hexType2 == oldHexType2 || hexType2 == hexType);

            // Ensure that homing and acceleration hexes do not combine, since that would be stupid.
            string hexName = Hexes[(int)hexType];
            string hex2Name = Hexes[(int)hexType2];
            if ((hexName == "Zeal" && hex2Name == "Accentuation") || (hexName == "Accentuation" && hex2Name == "Zeal"))
                hexType2 = 2f;

            SoundEngine.PlaySound(CalamitasEnchantUI.EnchSound, npc.Center);

            npc.ai[0] = (int)nextAttack;
            npc.ai[1] = 0f;
            npc.ai[3] = 36f;
            npc.netUpdate = true;
        }
        #endregion AI

        #region Frames and Drawcode

        public override void FindFrame(NPC npc, int frameHeight)
        {
            // Redefine the frame size to be in line with the SCal sheet.
            npc.frame.Width = 52;
            npc.frame.Height = 52;

            npc.frameCounter += npc.localAI[0];
            int frameOffset = (int)npc.frameCounter % 6;
            int frame = frameOffset;

            npc.frame.X = npc.frame.Width * (frame / 21);
            npc.frame.Y = npc.frame.Height * (frame % 21);
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            SpriteEffects direction = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                direction = SpriteEffects.FlipHorizontally;

            int afterimageCount = 8;
            Texture2D texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/CalamitasClone/CalamitasClone").Value;
            Texture2D armTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/CalamitasClone/CalamitasCloneArm").Value;
            Vector2 origin = npc.frame.Size() * 0.5f;

            // Incorporate the black shadow form effects.
            lightColor = Color.Lerp(lightColor, Color.Black, MathF.Pow(npc.localAI[2], 0.33f));
            float shadowBackglowOffset = 25f * MathF.Pow(npc.localAI[2], 2.8f) * npc.scale;
            float eyeGleamInterpolant = npc.localAI[3];

            // Draw afterimages.
            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < afterimageCount; i += 2)
                {
                    Color afterimageColor = lightColor * ((afterimageCount - i) / 15f) * npc.Opacity;
                    Vector2 afterimageDrawPosition = Vector2.Lerp(npc.oldPos[i] + npc.Size * 0.5f, npc.Center, 0.55f);
                    afterimageDrawPosition += -Main.screenPosition + Vector2.UnitY * npc.gfxOffY;
                    Main.spriteBatch.Draw(texture, afterimageDrawPosition, npc.frame, afterimageColor, npc.rotation, origin, npc.scale, direction, 0f);
                }
            }

            // Draw a shadow backglow.
            Vector2 drawPosition = npc.Center - Main.screenPosition + Vector2.UnitY * npc.gfxOffY;
            Vector2 armDrawPosition = drawPosition + new Vector2(npc.spriteDirection * 9.6f, -2f);
            if (shadowBackglowOffset > 0f)
            {
                for (int i = 0; i < 10; i++)
                {
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 10f).ToRotationVector2() * shadowBackglowOffset;
                    Main.spriteBatch.Draw(texture, drawPosition + drawOffset, npc.frame, lightColor with { A = 0 } * (1f - npc.localAI[2]), npc.rotation, origin, npc.scale, direction, 0f);
                }
            }

            Color shadowColor = CalamityUtils.ColorSwap(Color.Purple, Color.Blue, 10f);
            lightColor = Color.Lerp(lightColor, shadowColor, 0.7f);
            lightColor = Color.Lerp(lightColor, Color.Black, 0.32f);
            lightColor.A = 232;

            // Draw the body and arm.
            float armRotation = npc.Infernum().ExtraAI[ArmRotationIndex];

            // Draw a backglow.
            for (int i = 0; i < 5; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 5f).ToRotationVector2() * 4f;
                Color backglowColor = Color.Purple with { A = 0 };
                Main.spriteBatch.Draw(texture, drawPosition + drawOffset, npc.frame, backglowColor * npc.Opacity * 0.45f, npc.rotation, origin, npc.scale, direction, 0f);
            }

            // Draw the body and arms.
            Main.spriteBatch.Draw(texture, drawPosition, npc.frame, lightColor * npc.Opacity, npc.rotation, origin, npc.scale, direction, 0f);
            Main.spriteBatch.Draw(armTexture, armDrawPosition, null, lightColor * npc.Opacity, armRotation, armTexture.Size() * new Vector2(0.4f, 0.1f), npc.scale, direction, 0f);

            // Draw the wand if it's being used.
            if (npc.ai[0] == (int)CloneAttackType.WandFireballs && npc.Infernum().ExtraAI[2] == 0f)
            {
                float wandBrightness = npc.Infernum().ExtraAI[0];
                float wandRotation = armRotation + MathHelper.Pi - MathHelper.PiOver4;
                Texture2D wandTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/CalamitasClone/CharredWand").Value;
                Vector2 wandDrawPosition = armDrawPosition + (armRotation + MathHelper.Pi - MathHelper.PiOver2).ToRotationVector2() * npc.scale * 12f;

                if (wandBrightness > 0f)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        Color wandMagicBackglowColor = Color.HotPink with { A = 0 } * wandBrightness * npc.Opacity * 0.6f;
                        Vector2 drawOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * wandBrightness * 3f;
                        Main.spriteBatch.Draw(wandTexture, wandDrawPosition + drawOffset, null, wandMagicBackglowColor, wandRotation, wandTexture.Size() * Vector2.UnitY, npc.scale * 0.7f, 0, 0f);
                    }
                }
                Main.spriteBatch.Draw(wandTexture, wandDrawPosition, null, Color.LightGray * npc.Opacity, wandRotation, wandTexture.Size() * Vector2.UnitY, npc.scale * 0.7f, 0, 0f);
            }

            // Draw the staff if it's being used.
            if (npc.ai[0] == (int)CloneAttackType.SoulSeekerResurrection)
            {
                float staffRotation = armRotation + MathHelper.Pi - MathHelper.PiOver4;
                float telegraphInterpolant = npc.Infernum().ExtraAI[1];
                Texture2D staffTexture = ModContent.Request<Texture2D>("CalamityMod/Items/Weapons/Summon/EntropysVigil").Value;
                Vector2 staffDrawPosition = armDrawPosition + (armRotation + MathHelper.Pi - MathHelper.PiOver2).ToRotationVector2() * npc.scale * 12f;
                Vector2 staffEnd = armDrawPosition + (armRotation + MathHelper.Pi - MathHelper.PiOver2).ToRotationVector2() * npc.scale * 48f;

                BloomLineDrawInfo lineInfo = new()
                {
                    LineRotation = -armRotation - MathHelper.PiOver2,
                    WidthFactor = 0.004f + MathF.Pow(telegraphInterpolant, 4f) * (MathF.Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.001f + 0.001f),
                    BloomIntensity = MathHelper.Lerp(0.3f, 0.4f, telegraphInterpolant),
                    Scale = Vector2.One * telegraphInterpolant * MathHelper.Clamp(npc.Distance(Main.player[npc.target].Center) * 3f, 10f, 3600f),
                    MainColor = Color.Lerp(Color.HotPink, Color.Red, telegraphInterpolant * 0.9f + 0.1f),
                    DarkerColor = Color.Orange,
                    Opacity = MathF.Sqrt(telegraphInterpolant),
                    BloomOpacity = 0.5f,
                    LightStrength = 5f
                };
                Utilities.DrawBloomLineTelegraph(staffEnd, lineInfo);

                Main.spriteBatch.Draw(staffTexture, staffDrawPosition, null, Color.White * npc.Opacity, staffRotation, staffTexture.Size() * Vector2.UnitY, npc.scale * 0.85f, 0, 0f);
            }

            // Draw the book if it's being used.
            if (npc.ai[0] == (int)CloneAttackType.ConvergingBookEnergy && npc.Infernum().ExtraAI[3] == 0f)
            {
                Color bookColor = Color.Lerp(Color.HotPink with { A = 0 }, Color.White, MathF.Sqrt(npc.Infernum().ExtraAI[0])) * npc.Infernum().ExtraAI[0];
                Vector2 bookDrawPosition = npc.Center - Vector2.UnitX * npc.spriteDirection * npc.scale * 6f - Main.screenPosition + Main.rand.NextVector2Unit() * npc.Infernum().ExtraAI[2] * 3f;
                bookDrawPosition.Y += 6f;

                Texture2D bookTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/CalamitasClone/LashesOfChaosHeld").Value;
                Rectangle bookFrame = bookTexture.Frame(1, 8, 0, (int)(Main.GlobalTimeWrappedHourly * 15f) % 8);
                Main.spriteBatch.Draw(bookTexture, bookDrawPosition, bookFrame, bookColor * npc.Opacity, 0f, bookFrame.Size() * 0.5f, npc.scale * 0.8f, direction, 0f);
            }

            // Draw the shield if it's being used.
            if (npc.ai[0] == (int)CloneAttackType.FireburstDashes)
            {
                float shieldScale = npc.Infernum().ExtraAI[0];
                float shieldRotation = armRotation - MathHelper.PiOver2;
                Texture2D shieldTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/CalamitasClone/CalamitasShield").Value;
                Vector2 shieldOrigin = shieldTexture.Size() * new Vector2(0f, 0.5f);
                if (npc.spriteDirection == -1)
                {
                    shieldOrigin.X = shieldTexture.Width - shieldOrigin.X;
                    shieldRotation += MathHelper.Pi;
                }

                Vector2 shieldDrawPosition = armDrawPosition + (armRotation + MathHelper.PiOver2).ToRotationVector2() * 30f;
                Main.spriteBatch.Draw(shieldTexture, shieldDrawPosition, null, Color.White * npc.Opacity, shieldRotation, shieldTexture.Size() * 0.5f, npc.scale * shieldScale * 0.8f, direction, 0f);
            }

            // Draw the eye gleam.
            if (eyeGleamInterpolant > 0f)
            {
                float eyePulse = Main.GlobalTimeWrappedHourly * 0.84f % 1f;
                Texture2D eyeGleam = InfernumTextureRegistry.Gleam.Value;
                Vector2 eyePosition = npc.Center + new Vector2(npc.spriteDirection * -4f, -6f);
                Vector2 horizontalGleamScaleSmall = new Vector2(eyeGleamInterpolant * 3f, 1f) * 0.55f;
                Vector2 verticalGleamScaleSmall = new Vector2(1f, eyeGleamInterpolant * 2f) * 0.55f;
                Vector2 horizontalGleamScaleBig = horizontalGleamScaleSmall * (1f + eyePulse * 2f);
                Vector2 verticalGleamScaleBig = verticalGleamScaleSmall * (1f + eyePulse * 2f);
                Color eyeGleamColorSmall = Color.Violet * eyeGleamInterpolant;
                eyeGleamColorSmall.A = 0;
                Color eyeGleamColorBig = eyeGleamColorSmall * (1f - eyePulse);

                // Draw a pulsating red eye.
                Main.spriteBatch.Draw(eyeGleam, eyePosition - Main.screenPosition, null, eyeGleamColorSmall, 0f, eyeGleam.Size() * 0.5f, horizontalGleamScaleSmall, 0, 0f);
                Main.spriteBatch.Draw(eyeGleam, eyePosition - Main.screenPosition, null, eyeGleamColorSmall, 0f, eyeGleam.Size() * 0.5f, verticalGleamScaleSmall, 0, 0f);
                Main.spriteBatch.Draw(eyeGleam, eyePosition - Main.screenPosition, null, eyeGleamColorBig, 0f, eyeGleam.Size() * 0.5f, horizontalGleamScaleBig, 0, 0f);
                Main.spriteBatch.Draw(eyeGleam, eyePosition - Main.screenPosition, null, eyeGleamColorBig, 0f, eyeGleam.Size() * 0.5f, verticalGleamScaleBig, 0, 0f);
            }

            return false;
        }

        public static float HexHeightFunction(float _) => HexFadeInInterpolant * 10f + 0.01f;

        public static Color HexColorFunction(float _) => HexColor * HexFadeInInterpolant * 0.8f;

        public static void DrawHexOnTarget(Player target, Color hexColor, float verticalOffset, float fadeInInterpolant)
        {
            HexFadeInInterpolant = fadeInInterpolant;
            HexColor = hexColor;

            Vector2 left = target.Center + new Vector2(-40f, verticalOffset) - Main.screenPosition;
            Vector2 right = target.Center + new Vector2(40f, verticalOffset) - Main.screenPosition;
            HexStripDrawer ??= new(HexHeightFunction, HexColorFunction);
            HexStripDrawer.UseBandTexture(ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/CalamitasClone/ZealHexSymbols"));

            Main.instance.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            HexStripDrawer.Draw(left, right, 0.15f, 2f, Main.GlobalTimeWrappedHourly * 2f);
        }
        #endregion Frames and Drawcode
    }
}
