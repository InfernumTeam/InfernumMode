using CalamityMod;
using CalamityMod.InverseKinematics;
using CalamityMod.NPCs;
using CalamityMod.Particles;
using CalamityMod.Sounds;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.DataStructures;
using InfernumMode.Common.Graphics.Primitives;
using InfernumMode.Common.Graphics.ScreenEffects;
using InfernumMode.Core.GlobalInstances.Players;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares.AresBodyBehaviorOverride;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.DraedonBehaviorOverride;
using CalamityModClass = CalamityMod.CalamityMod;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class AresEnergyKatana : ModNPC
    {
        private bool katanaIsInUse;

        public LimbCollection Limbs = new(new CyclicCoordinateDescentUpdateRule(0.27f, Pi * 0.75f), 140f, 154f);

        public AresCannonChargeParticleSet EnergyDrawer = new(-1, 15, 40f, Color.Red);

        public ThanatosSmokeParticleSet SmokeDrawer = new(-1, 3, 0f, 16f, 1.5f);

        public Vector2 SlashStart
        {
            get;
            set;
        }

        public PrimitiveTrailCopy SlashDrawer
        {
            get;
            set;
        }

        public bool KatanaIsInUse
        {
            get => katanaIsInUse;
            set
            {
                if (value && !katanaIsInUse)
                    SoundEngine.PlaySound(CommonCalamitySounds.ExoLaserShootSound, NPC.Center);

                katanaIsInUse = value;
            }
        }

        public List<Vector2> SlashControlPoints
        {
            get
            {
                Vector2 slashStart = SlashStart;
                Vector2 aimDirection = ((float)Limbs.Limbs[1].Rotation).ToRotationVector2();
                Vector2 slashEnd = NPC.Center + aimDirection * NPC.scale * 160f;
                Vector2 slashMiddle1 = Vector2.Lerp(slashStart, slashEnd, 0.25f);
                Vector2 slashMiddle2 = Vector2.Lerp(slashStart, slashEnd, 0.5f);
                Vector2 slashMiddle3 = Vector2.Lerp(slashStart, slashEnd, 0.75f);

                return new List<Vector2>()
                {
                    slashEnd,
                    slashMiddle3 + aimDirection * 30f,
                    slashMiddle2,
                    slashMiddle1 - aimDirection * 30f,
                    slashStart,
                };
            }
        }

        public Rectangle ActualHitbox => Utils.CenteredRectangle(NPC.Center, Vector2.One * NPC.scale * 142f);

        public Player Target => Main.player[NPC.target];

        public ref float ArmOffsetDirection => ref NPC.ai[2];

        public ref float CurrentDirection => ref NPC.ai[3];

        public ref float SlashTrailFadeOut => ref NPC.localAI[0];

        public static int DownwardCrossSlicesAnticipationTime
        {
            get
            {
                if (ExoMechManagement.CurrentAresPhase >= 6)
                    return 50;

                if (ExoMechManagement.CurrentAresPhase >= 5)
                    return 70;

                return 84;
            }
        }

        public static int DownwardCrossSlicesSliceTime => 20;

        public static int DownwardCrossSlicesHoldInPlaceTime
        {
            get
            {
                if (ExoMechManagement.CurrentAresPhase >= 6)
                    return 21;

                if (ExoMechManagement.CurrentAresPhase >= 5)
                    return 26;

                return 32;
            }
        }

        public static int ThreeDimensionalSlicesAnticipationTime
        {
            get
            {
                if (ExoMechManagement.CurrentAresPhase >= 6)
                    return 50;

                return 72;
            }
        }

        public static int ThreeDimensionalSlicesSliceTime => 20;

        public static NPC Ares => AresCannonBehaviorOverride.Ares;

        public static float AttackTimer => Ares.ai[1];

        public static Vector2 ActiveHitboxSize => new(450f);

        public override void SetStaticDefaults()
        {
            this.HideFromBestiary();
            NPCID.Sets.TrailingMode[NPC.type] = 3;
            NPCID.Sets.TrailCacheLength[NPC.type] = 50;

            InfernumPlayer.PostUpdateEvent += (InfernumPlayer player) =>
            {
                Referenced<int> cooldown = player.GetRefValue<int>("HitSoundCountdown");
                cooldown.Value--;
            };

            InfernumPlayer.OnHitByNPCEvent += (InfernumPlayer player, NPC npc, Player.HurtInfo hurtInfo) =>
            {
                if (npc.type != ModContent.NPCType<AresEnergyKatana>() || hurtInfo.Damage <= 0)
                    return;

                Referenced<int> cooldown = player.GetRefValue<int>("HitSoundCountdown");

                // Play hit souds if the countdown has passed.
                if (cooldown.Value <= 0)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.AquaticScourgeGoreSound with { Volume = 3f }, player.Player.Center);
                    cooldown.Value = 30;
                }

                for (int i = 0; i < 15; i++)
                {
                    int bloodLifetime = Main.rand.Next(22, 36);
                    float bloodScale = Main.rand.NextFloat(0.6f, 0.8f);
                    Color bloodColor = Color.Lerp(Color.Red, Color.DarkRed, Main.rand.NextFloat());
                    bloodColor = Color.Lerp(bloodColor, new Color(51, 22, 94), Main.rand.NextFloat(0.65f));

                    if (Main.rand.NextBool(20))
                        bloodScale *= 2f;

                    Vector2 bloodVelocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.81f) * Main.rand.NextFloat(11f, 30f);
                    bloodVelocity.Y -= 12f;
                    BloodParticle blood = new(player.Player.Center, bloodVelocity, bloodLifetime, bloodScale, bloodColor);
                    GeneralParticleHandler.SpawnParticle(blood);
                }
                for (int i = 0; i < 25; i++)
                {
                    float bloodScale = Main.rand.NextFloat(0.2f, 0.33f);
                    Color bloodColor = Color.Lerp(Color.Red, Color.DarkRed, Main.rand.NextFloat(0.5f, 1f));
                    Vector2 bloodVelocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.9f) * Main.rand.NextFloat(9f, 20.5f);
                    BloodParticle2 blood = new(player.Player.Center, bloodVelocity, 20, bloodScale, bloodColor);
                    GeneralParticleHandler.SpawnParticle(blood);
                }
            };
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 5f;
            NPC.damage = AresEnergyKatanaContactDamage / 2;
            NPC.Size = Vector2.One * 60f;
            NPC.defense = 80;
            NPC.DR_NERD(0.35f);
            NPC.LifeMaxNERB(1250000, 1495000, 500000);
            double HPBoost = CalamityConfig.Instance.BossHealthBoost * 0.01;
            NPC.lifeMax += (int)(NPC.lifeMax * HPBoost);
            NPC.aiStyle = -1;
            AIType = -1;
            NPC.Opacity = 0f;
            NPC.knockBackResist = 0f;
            NPC.canGhostHeal = false;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.HitSound = null;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.netAlways = true;
            NPC.boss = true;
            NPC.hide = true;
            NPC.Calamity().canBreakPlayerDefense = true;
            Music = (InfernumMode.CalamityMod as CalamityModClass).GetMusicFromMusicMod("ExoMechs") ?? MusicID.Boss3;
        }

        public override void AI()
        {
            // Die if Ares is not present.
            if (CalamityGlobalNPC.draedonExoMechPrime == -1)
            {
                NPC.life = 0;
                NPC.active = false;
                return;
            }

            // Update the energy drawers.
            EnergyDrawer.Update();
            SmokeDrawer.Update();

            // Update limbs.
            UpdateLimbs();

            // Close the HP bar.
            NPC.boss = false;
            NPC.Calamity().ShouldCloseHPBar = true;

            // Inherit a bunch of attributes such as opacity from the body.
            ExoMechAIUtilities.HaveArmsInheritAresBodyAttributes(NPC);

            // Ensure this does not take damage in the desperation attack.
            NPC.dontTakeDamage = false;
            if (Ares.ai[0] == (int)AresBodyAttackType.PrecisionBlasts)
                NPC.dontTakeDamage = true;

            bool currentlyDisabled = ArmIsDisabled(NPC);

            // Inherit a bunch of attributes such as opacity from the body.
            ExoMechAIUtilities.HaveArmsInheritAresBodyAttributes(NPC);

            AresCannonBehaviorOverride.UpdateParticleDrawers(SmokeDrawer, EnergyDrawer, 0f, 100f);

            // Check to see if this arm should be used for special things in a combo attack.
            if (AresCannonBehaviorOverride.IsInUseByComboAttack(NPC))
            {
                NPC.Size = ActiveHitboxSize;
                return;
            }

            // Hover in place below Ares if disabled.
            if (currentlyDisabled)
            {
                PerformDisabledHoverMovement();
                return;
            }

            // Unlike projectiles, NPCs have no Colliding hook to use for general-purpose collision logic.
            // As such, a roundabout hack is required, where the hurt box is so large that it triggers for everything, but a CanHitPlayer check culls invalid hits.
            NPC.Size = ActiveHitboxSize;

            switch ((AresBodyAttackType)Ares.ai[0])
            {
                case AresBodyAttackType.EnergyBladeSlices:
                    DoBehavior_EnergyBladeSlices();
                    break;
                case AresBodyAttackType.DownwardCrossSlices:
                    DoBehavior_DownwardCrossSlices();
                    break;
                case AresBodyAttackType.ThreeDimensionalSuperslashes:
                    DoBehavior_ThreeDimensionalSuperslashes();
                    break;
            }
        }

        public void UpdateLimbs()
        {
            Vector2 connectPosition = Ares.Center + new Vector2(ArmOffsetDirection * 70f, -108f).RotatedBy(Ares.rotation * -Ares.spriteDirection);
            Vector2 endPosition = NPC.Center;

            for (int i = 0; i < 12; i++)
            {
                float lockedRotation;
                if (ArmOffsetDirection == 1f)
                    lockedRotation = 0.23f;
                else
                    lockedRotation = Pi - 0.23f;
                Limbs[0].Rotation = Clamp((float)Limbs[0].Rotation, lockedRotation - 0.45f, lockedRotation + 0.45f);

                Limbs.Update(connectPosition, endPosition);
            }
        }

        public void DoBehavior_EnergyBladeSlices()
        {
            int anticipationTime = 54;
            int sliceTime = 16;
            int hoverTime = 8;
            float slashShootSpeed = 4f;

            if (ExoMechManagement.CurrentAresPhase >= 3)
                slashShootSpeed += 0.5f;
            if (ExoMechManagement.CurrentAresPhase >= 5)
                anticipationTime -= 6;
            if (ExoMechManagement.CurrentAresPhase >= 6)
                anticipationTime -= 5;

            float wrappedAttackTimer = (AttackTimer + (int)ArmOffsetDirection * anticipationTime / 3) % (anticipationTime + sliceTime + hoverTime);
            float flySpeedBoost = Ares.velocity.Length() * 0.51f;

            // Anticipate the slash.
            if (wrappedAttackTimer <= anticipationTime)
            {
                SlashTrailFadeOut = 1f;
                float minHoverSpeed = Utils.Remap(wrappedAttackTimer, 7f, anticipationTime * 0.5f, 2f, 42f);
                Vector2 startingOffset = new(ArmOffsetDirection * 470f, 0f);
                Vector2 endingOffset = new(ArmOffsetDirection * 172f, -175f);
                Vector2 hoverOffset = Vector2.Lerp(startingOffset, endingOffset, Utils.GetLerpValue(0f, anticipationTime, wrappedAttackTimer, true));
                ExoMechAIUtilities.DoSnapHoverMovement(NPC, Ares.Center + hoverOffset.SafeNormalize(Vector2.Zero) * 450f, flySpeedBoost + minHoverSpeed, 115f);
            }

            // Do the slash.
            else if (wrappedAttackTimer <= anticipationTime + sliceTime)
            {
                SlashTrailFadeOut = 0f;
                Vector2 startingOffset = new(ArmOffsetDirection * 172f, -175f);
                Vector2 endingOffset = new(ArmOffsetDirection * -260f, 400f);
                Vector2 hoverOffset = Vector2.Lerp(startingOffset, endingOffset, Utils.GetLerpValue(anticipationTime, anticipationTime + sliceTime, wrappedAttackTimer, true));
                ExoMechAIUtilities.DoSnapHoverMovement(NPC, Ares.Center + hoverOffset.SafeNormalize(Vector2.Zero) * 400f, flySpeedBoost + 49f, 115f);
            }

            // Drift for a short time after the slash.
            else
            {
                NPC.velocity.X *= 0.6f;
                NPC.velocity.Y *= 0.1f;
                SlashTrailFadeOut = Clamp(SlashTrailFadeOut + 0.5f, 0f, 1f);
            }

            // Prepare the slash.
            if (wrappedAttackTimer == anticipationTime)
            {
                // Reset the position cache, so that the trail can be drawn with a fresh set of points.
                NPC.oldPos = new Vector2[NPC.oldPos.Length];

                // Calculate the starting position of the slash. This is used for determining the orientation of the trail.
                SlashStart = NPC.Center + ((float)Limbs.Limbs[1].Rotation).ToRotationVector2() * NPC.scale * 160f;
                NPC.netUpdate = true;

                // Play a slice sound.
                SoundEngine.PlaySound(InfernumSoundRegistry.AresSlashSound, NPC.Center);
            }

            // Create an energy slash.
            if (wrappedAttackTimer == anticipationTime + sliceTime / 2 + 4)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 energySlashVelocity = Vector2.Lerp(((float)Limbs[1].Rotation).ToRotationVector2(), NPC.SafeDirectionTo(Target.Center), 0.6f) * slashShootSpeed;

                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(slash =>
                    {
                        slash.ModProjectile<AresEnergySlash>().ControlPoints = SlashControlPoints.ToArray();
                    });
                    Utilities.NewProjectileBetter(NPC.Center, energySlashVelocity, ModContent.ProjectileType<AresEnergySlash>(), AresEnergySlashDamage, 0f);
                }
            }

            // Rotate based on the direction of the arm.
            NPC.rotation = (float)Limbs[1].Rotation;
            NPC.spriteDirection = (int)ArmOffsetDirection;
            if (ArmOffsetDirection == 1)
                NPC.rotation += Pi;

            // Ensure that the katanas are drawn.
            KatanaIsInUse = true;
        }

        public void DoBehavior_DownwardCrossSlices()
        {
            int anticipationTime = DownwardCrossSlicesAnticipationTime;
            int sliceTime = DownwardCrossSlicesSliceTime;
            int holdInPlaceTime = DownwardCrossSlicesHoldInPlaceTime;
            float wrappedAttackTimer = AttackTimer % (anticipationTime + sliceTime + holdInPlaceTime);
            float flySpeedBoost = Ares.velocity.Length() * 1.1f;

            // Anticipate the slash.
            if (wrappedAttackTimer <= anticipationTime)
            {
                SlashTrailFadeOut = 1f;
                float minHoverSpeed = Utils.Remap(wrappedAttackTimer, 7f, anticipationTime * 0.5f, 2f, 42f);
                Vector2 startingOffset = new(ArmOffsetDirection * 470f, 0f);
                Vector2 endingOffset = new(ArmOffsetDirection * 140f, -192f);
                Vector2 hoverOffset = Vector2.Lerp(startingOffset, endingOffset, Utils.GetLerpValue(0f, anticipationTime, wrappedAttackTimer, true));
                ExoMechAIUtilities.DoSnapHoverMovement(NPC, Ares.Center + hoverOffset.SafeNormalize(Vector2.Zero) * 450f, flySpeedBoost + minHoverSpeed, 115f);
            }

            // Do the slash.
            else if (wrappedAttackTimer <= anticipationTime + sliceTime)
            {
                SlashTrailFadeOut = 0f;
                Vector2 startingOffset = new(ArmOffsetDirection * 140f, -192f);
                Vector2 endingOffset = new(ArmOffsetDirection * -260f, 450f);
                Vector2 hoverOffset = Vector2.Lerp(startingOffset, endingOffset, Utils.GetLerpValue(anticipationTime, anticipationTime + sliceTime, wrappedAttackTimer, true));
                ExoMechAIUtilities.DoSnapHoverMovement(NPC, Ares.Center + hoverOffset.SafeNormalize(Vector2.Zero) * 400f, flySpeedBoost + 67f, 115f);
            }

            // Hold the blades in place.
            else
            {
                NPC.velocity *= 0.27f;
                SlashTrailFadeOut = Clamp(SlashTrailFadeOut + 0.25f, 0f, 1f);
            }

            // Prepare the slash.
            if (wrappedAttackTimer == anticipationTime)
            {
                // Reset the position cache, so that the trail can be drawn with a fresh set of points.
                NPC.oldPos = new Vector2[NPC.oldPos.Length];
                Target.Infernum_Camera().CurrentScreenShakePower = 6f;
                ScreenEffectSystem.SetFlashEffect(Target.Center, 2f, 30);

                // Calculate the starting position of the slash. This is used for determining the orientation of the trail.
                SlashStart = NPC.Center + ((float)Limbs.Limbs[1].Rotation).ToRotationVector2() * NPC.scale * 160f;
                NPC.netUpdate = true;

                // Play a slice sound.
                SoundEngine.PlaySound(InfernumSoundRegistry.AresSlashSound, NPC.Center);
            }

            // Rotate based on the direction of the arm.
            if (wrappedAttackTimer <= anticipationTime + sliceTime)
            {
                NPC.rotation = (float)Limbs[1].Rotation;
                NPC.spriteDirection = (int)ArmOffsetDirection;
                if (ArmOffsetDirection == 1)
                    NPC.rotation += Pi;
            }

            // Ensure that the katanas are drawn.
            KatanaIsInUse = true;
        }

        public void DoBehavior_ThreeDimensionalSuperslashes()
        {
            int anticipationTime = ThreeDimensionalSlicesAnticipationTime;
            int sliceTime = ThreeDimensionalSlicesSliceTime;
            float wrappedAttackTimer = AttackTimer % (anticipationTime + sliceTime);
            float flySpeedBoost = Ares.position.Distance(Ares.oldPosition);

            // Anticipate the slash.
            if (wrappedAttackTimer <= anticipationTime)
            {
                SlashTrailFadeOut = 1f;
                float minHoverSpeed = Utils.Remap(wrappedAttackTimer, 7f, anticipationTime * 0.5f, 9f, 66f);
                Vector2 startingOffset = new(ArmOffsetDirection * 470f, 0f);
                Vector2 endingOffset = new(ArmOffsetDirection * 172f, -175f);
                Vector2 hoverOffset = Vector2.Lerp(startingOffset, endingOffset, Utils.GetLerpValue(0f, anticipationTime, wrappedAttackTimer, true));
                ExoMechAIUtilities.DoSnapHoverMovement(NPC, Ares.Center + hoverOffset.SafeNormalize(Vector2.Zero) * Ares.scale * 880f, flySpeedBoost + minHoverSpeed, 115f);
            }

            // Do the slash.
            else if (wrappedAttackTimer <= anticipationTime + sliceTime)
            {
                SlashTrailFadeOut = 0f;
                Vector2 startingOffset = new(ArmOffsetDirection * 172f, -175f);
                Vector2 endingOffset = new(ArmOffsetDirection * -260f, 400f);
                Vector2 hoverOffset = Vector2.Lerp(startingOffset, endingOffset, Utils.GetLerpValue(anticipationTime, anticipationTime + sliceTime, wrappedAttackTimer, true));
                ExoMechAIUtilities.DoSnapHoverMovement(NPC, Ares.Center + hoverOffset.SafeNormalize(Vector2.Zero) * Ares.scale * 400f, flySpeedBoost + 67f, 115f);
            }

            // Prepare the slash.
            if (wrappedAttackTimer == anticipationTime)
            {
                // Reset the position cache, so that the trail can be drawn with a fresh set of points.
                NPC.oldPos = new Vector2[NPC.oldPos.Length];

                // Calculate the starting position of the slash. This is used for determining the orientation of the trail.
                SlashStart = NPC.Center + ((float)Limbs.Limbs[1].Rotation).ToRotationVector2() * NPC.scale * 160f;
                NPC.netUpdate = true;

                // Play a slice sound.
                SoundEngine.PlaySound(InfernumSoundRegistry.AresSlashSound, NPC.Center);
            }

            // Rotate based on the direction of the arm.
            NPC.rotation = (float)Limbs[1].Rotation;
            NPC.spriteDirection = (int)ArmOffsetDirection;
            if (ArmOffsetDirection == 1)
                NPC.rotation += Pi;

            // Ensure that the katanas are drawn.
            KatanaIsInUse = true;
        }

        public Vector2 PerformDisabledHoverMovement()
        {
            // The katana should by default not be in use.
            KatanaIsInUse = false;

            // Reset the hit/hurtbox.
            NPC.Size = Vector2.One * 60f;

            ExoMechAIUtilities.PerformAresArmDirectioning(NPC, Ares, Target, Vector2.UnitY, true, false, ref CurrentDirection);

            Vector2 hoverOffset = new(ArmOffsetDirection * 470f, 0f);
            Vector2 hoverDestination = Ares.Center + hoverOffset * Ares.scale;
            ExoMechAIUtilities.DoSnapHoverMovement(NPC, hoverDestination, 64f, 115f);

            return hoverOffset;
        }

        public override void DrawBehind(int index)
        {
            Main.instance.DrawCacheNPCProjectiles.Add(index);
        }

        public override void ModifyHoverBoundingBox(ref Rectangle boundingBox)
        {
            boundingBox = ActualHitbox;
        }

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) => false;

        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            return projectile.Colliding(projectile.Hitbox, ActualHitbox) ? null : false;
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            // Use the boss cooldown slot.
            cooldownSlot = ImmunityCooldownID.Bosses;

            // Don't do damage if Ares is in the background.
            if (Ares.ai[2] >= 0.25f)
                return false;

            // If the player is colliding with the katana, they take damage.
            float _ = 0f;
            Vector2 katanaStart = NPC.Center - NPC.rotation.ToRotationVector2() * ArmOffsetDirection * NPC.scale * 14f;
            Vector2 katanaEnd = NPC.Center - NPC.rotation.ToRotationVector2() * ArmOffsetDirection * NPC.scale * 264f;
            bool playerIsCollidingWithKatana = Collision.CheckAABBvLineCollision(target.TopLeft, target.Hitbox.Size(), katanaStart, katanaEnd, NPC.scale * 50f, ref _);
            if (KatanaIsInUse && playerIsCollidingWithKatana)
                return true;

            return false;
        }

        public override bool CanHitNPC(NPC target)/* tModPorter Suggestion: Return true instead of null */ => false;

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.soundDelay == 1)
            {
                NPC.soundDelay = 3;
                SoundEngine.PlaySound(CommonCalamitySounds.ExoHitSound, NPC.Center);
            }

            for (int k = 0; k < 3; k++)
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.TerraBlade, 0f, 0f, 100, new Color(0, 255, 255), 1f);

            if (NPC.life <= 0)
            {
                for (int i = 0; i < 2; i++)
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.TerraBlade, 0f, 0f, 100, new Color(0, 255, 255), 1.5f);

                for (int i = 0; i < 20; i++)
                {
                    Dust exoEnergy = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.TerraBlade, 0f, 0f, 0, new Color(0, 255, 255), 2.5f);
                    exoEnergy.noGravity = true;
                    exoEnergy.velocity *= 3f;

                    exoEnergy = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.TerraBlade, 0f, 0f, 100, new Color(0, 255, 255), 1.5f);
                    exoEnergy.velocity *= 2f;
                    exoEnergy.noGravity = true;
                }

                if (Main.netMode != NetmodeID.Server)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("AresPulseCannon1").Type, NPC.scale);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, InfernumMode.CalamityMod.Find<ModGore>("AresHandBase1").Type, NPC.scale);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, InfernumMode.CalamityMod.Find<ModGore>("AresHandBase2").Type, NPC.scale);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, InfernumMode.CalamityMod.Find<ModGore>("AresHandBase3").Type, NPC.scale);
                }
            }
        }

        public float SlashWidthFunction(float completionRatio) => NPC.scale * 100f;

        public Color SlashColorFunction(float completionRatio) => Color.White * Utils.GetLerpValue(0.9f, 0.4f, completionRatio, true) * (1f - SlashTrailFadeOut) * NPC.Opacity * NPC.scale;

        public void DrawSlash()
        {
            PrepareSlashShader();
            SlashDrawer.Draw(SlashControlPoints, -Main.screenPosition, 20, (float)Limbs.Limbs[1].Rotation + PiOver2);
        }

        public static void PrepareSlashShader()
        {
            var slashShader = GameShaders.Misc["CalamityMod:ExobladeSlash"];
            slashShader.SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/VoronoiShapes"));
            slashShader.UseColor(new Color(237, 148, 54));
            slashShader.UseSecondaryColor(new Color(104, 24, 38));
            slashShader.Shader.Parameters["fireColor"].SetValue(Color.Wheat.ToVector3());
            slashShader.Shader.Parameters["flipped"].SetValue(false);
            slashShader.Apply();
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Draw the cannon.
            string glowmaskTexturePath = "InfernumMode/Content/BehaviorOverrides/BossAIs/Draedon/Ares/AresEnergyKatanaGlow";
            AresCannonBehaviorOverride.DrawCannon(NPC, glowmaskTexturePath, Color.Red, drawColor, NPC.Center - Main.screenPosition, EnergyDrawer, SmokeDrawer);

            if (KatanaIsInUse)
            {
                // Prepare the slash drawer.
                var slashShader = GameShaders.Misc["CalamityMod:ExobladeSlash"];
                SlashDrawer ??= new PrimitiveTrailCopy(SlashWidthFunction, SlashColorFunction, null, true, slashShader);

                // Draw the zany slash effect.
                Main.spriteBatch.EnterShaderRegion();

                for (int i = 0; i < 6; i++)
                    DrawSlash();
                Main.spriteBatch.ExitShaderRegion();

                // Draw the energy katana.
                int bladeFrameNumber = (int)((Main.GlobalTimeWrappedHourly * 16f + NPC.whoAmI * 7.13f) % 9f);
                Texture2D bladeTexture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/DraedonsArsenal/PhaseslayerBlade").Value;
                Rectangle bladeFrame = bladeTexture.Frame(3, 7, bladeFrameNumber / 7, bladeFrameNumber % 7);
                Vector2 bladeOrigin = bladeFrame.Size() * new Vector2(0.5f, 1f);
                Vector2 bladeDrawPosition = NPC.Center - Main.screenPosition - NPC.rotation.ToRotationVector2() * ArmOffsetDirection * NPC.scale * 14f;
                Vector2 bladeScale = Vector2.One * NPC.scale;
                float squish = NPC.position.Distance(NPC.oldPosition) * 0.006f;
                bladeScale.X -= squish;

                Main.EntitySpriteDraw(bladeTexture, bladeDrawPosition, bladeFrame, NPC.GetAlpha(Color.White), NPC.rotation - ArmOffsetDirection * PiOver2, bladeOrigin, bladeScale, 0, 0);
            }

            return false;
        }

        public override bool CheckActive() => false;
    }
}
