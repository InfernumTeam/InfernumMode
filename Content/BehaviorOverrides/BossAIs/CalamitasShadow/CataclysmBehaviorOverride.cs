using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.CalClone;
using CalamityMod.Particles;
using CalamityMod.Sounds;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.ScreenEffects;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityMod.CalamityUtils;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasShadow.CalamitasShadowBehaviorOverride;
using CalCloneNPC = CalamityMod.NPCs.CalClone.CalamitasClone;
using SCalNPC = CalamityMod.NPCs.SupremeCalamitas.SupremeCalamitas;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasShadow
{
    public class CataclysmBehaviorOverride : NPCBehaviorOverride
    {
        public enum SCalBrotherAttackType
        {
            HorizontalCharges,
            FireAndSwordSlashes,
            BladeUppercutAndDashes
        }

        public static AresCannonChargeParticleSet CataclysmEnergyDrawer
        {
            get;
            set;
        } = new(-1, 12, 85f, Color.Red);

        public static AresCannonChargeParticleSet CatastropheEnergyDrawer
        {
            get;
            set;
        } = new(-1, 12, 85f, Color.DeepSkyBlue);

        public override int? NPCTypeToDeferToForTips => ModContent.NPCType<CalCloneNPC>();

        public override int NPCOverrideType => ModContent.NPCType<Cataclysm>();

        #region AI
        public override bool PreAI(NPC npc)
        {
            DoAI(npc);
            return false;
        }

        public static void DoAI(NPC npc)
        {
            int cataclysmIndex = NPC.FindFirstNPC(ModContent.NPCType<Cataclysm>());
            int catastropheIndex = NPC.FindFirstNPC(ModContent.NPCType<Catastrophe>());
            bool isCataclysm = npc.type == ModContent.NPCType<Cataclysm>();
            bool isCatastrophe = npc.type == ModContent.NPCType<Catastrophe>();
            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float attackSpecificTimer = ref npc.Infernum().ExtraAI[5];

            // Reset hit sounds.
            npc.HitSound = SoundID.DD2_OgreRoar;
            npc.DeathSound = null;

            // Die if the either brother is missing.
            if (cataclysmIndex == -1 || catastropheIndex == -1 || !NPC.AnyNPCs(ModContent.NPCType<CalCloneNPC>()))
            {
                npc.life = 0;
                npc.HitEffect();
                npc.netUpdate = true;
                return;
            }

            // No.
            if (npc.scale != 1f)
            {
                npc.scale = 1f;
                npc.Size /= npc.scale;
            }

            if (isCatastrophe)
            {
                // Shamelessly steal variables from Cataclysm.
                NPC cataclysm = Main.npc[cataclysmIndex];

                // Sync if Catastrophe changed attack states or there's a noticeable discrepancy between attack timers.
                if (attackState != cataclysm.ai[0] || Distance(attackTimer, cataclysm.ai[1]) > 20f)
                    npc.netUpdate = true;

                npc.ai = cataclysm.ai;
                npc.target = cataclysm.target;
                npc.life = cataclysm.life;
                npc.lifeMax = cataclysm.lifeMax;
                npc.realLife = cataclysm.whoAmI;
                attackState = ref cataclysm.ai[0];
                attackTimer = ref cataclysm.ai[1];

                CatastropheEnergyDrawer.Update();
                CatastropheEnergyDrawer.ParticleSpawnRate = int.MaxValue;

                CalamityGlobalNPC.catastrophe = npc.whoAmI;

                // Use a fallback target if Cataclysm doesn't have one at the moment. This will not care about large distances.
                npc.TargetClosestIfTargetIsInvalid(1000000f);
            }

            // Have Cataclysm increment the attack timer and handle targeting.
            else if (isCataclysm)
            {
                CalamityGlobalNPC.cataclysm = npc.whoAmI;
                npc.TargetClosestIfTargetIsInvalid();
                attackTimer++;

                CataclysmEnergyDrawer.Update();
                CataclysmEnergyDrawer.ParticleSpawnRate = int.MaxValue;
            }

            Player target = Main.player[npc.target];

            // Reset hitbox widths.
            npc.width = 90;

            // Perform attacks.
            npc.Opacity = Clamp(npc.Opacity + 0.05f, 0f, 1f);
            npc.damage = npc.defDamage;
            switch ((SCalBrotherAttackType)attackState)
            {
                case SCalBrotherAttackType.HorizontalCharges:
                    DoBehavior_HorizontalCharges(npc, target, isCataclysm, ref attackTimer);
                    break;
                case SCalBrotherAttackType.FireAndSwordSlashes:
                    DoBehavior_FireAndSwordSlashes(npc, target, isCataclysm, ref attackTimer);
                    break;
                case SCalBrotherAttackType.BladeUppercutAndDashes:
                    DoBehavior_BladeUppercutAndDashes(npc, target, isCataclysm);
                    break;
            }
        }

        public static void UpdateEnergyDrawer(float attackTimer, float chargeDelay, AresCannonChargeParticleSet energyDrawer)
        {
            // Decide the state of the particle drawers.
            if (attackTimer > chargeDelay * 0.45f)
            {
                float chargeCompletion = Clamp(attackTimer / chargeDelay, 0f, 1f);
                energyDrawer.ParticleSpawnRate = 2;
                energyDrawer.SpawnAreaCompactness = 100f;
                energyDrawer.chargeProgress = chargeCompletion;

                if (attackTimer % 15f == 14f && chargeCompletion < 1f)
                    energyDrawer.AddPulse(chargeCompletion * 6f);
            }
        }

        public static void DoBehavior_HorizontalCharges(NPC npc, Player target, bool isCataclysm, ref float attackTimer)
        {
            int chargeTime = 19;
            int chargeSlowdownTime = 8;
            int chargeCount = 3;
            float baseChargeSpeed = 9.75f;
            float chargeAcceleration = 2.5f;

            if (BossRushEvent.BossRushActive)
            {
                baseChargeSpeed *= 1.4f;
                chargeCount--;
            }

            if (CalamityGlobalNPC.cataclysm == -1)
                CalamityGlobalNPC.cataclysm = NPC.FindFirstNPC(ModContent.NPCType<Cataclysm>());
            if (CalamityGlobalNPC.catastrophe == -1)
                CalamityGlobalNPC.catastrophe = NPC.FindFirstNPC(ModContent.NPCType<Catastrophe>());

            ref float attackState = ref Main.npc[CalamityGlobalNPC.cataclysm].Infernum().ExtraAI[0];
            ref float chargeCounter = ref Main.npc[CalamityGlobalNPC.cataclysm].Infernum().ExtraAI[1];
            ref float resetDesperationThing = ref Main.npc[CalamityGlobalNPC.cataclysm].Infernum().ExtraAI[2];
            ref float catastropheArmRotation = ref Main.npc[CalamityGlobalNPC.catastrophe].localAI[0];
            float horizontalChargeOffset = isCataclysm.ToDirectionInt() * (chargeCounter % 2f == 0f).ToDirectionInt() * 480f;

            if (resetDesperationThing == 0f)
            {
                attackState = 0f;
                chargeCounter = 0f;
                attackTimer = 0f;
                resetDesperationThing = 1f;
                npc.netUpdate = true;
            }

            switch ((int)attackState)
            {
                // Hover into position.
                case 0:
                    int chargeDelay = chargeCounter <= 0f ? 88 : 45;
                    Vector2 hoverDestination = target.Center + Vector2.UnitX * horizontalChargeOffset;
                    Vector2 idealVelocity = ((hoverDestination - npc.Center) * 0.15f).ClampMagnitude(4f, 50f);
                    npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.25f);
                    npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                    npc.rotation = 0f;
                    npc.damage = 0;

                    // Make Catastrophe anticipate with his blade.
                    catastropheArmRotation = Utils.Remap(attackTimer, 0f, 24f, 0f, -1.8f);

                    UpdateEnergyDrawer(attackTimer, chargeDelay, isCataclysm ? CataclysmEnergyDrawer : CatastropheEnergyDrawer);
                    if (((attackTimer > 210f || npc.WithinRange(hoverDestination, 80f)) && attackTimer > chargeDelay) || attackState == 1f)
                    {
                        npc.velocity *= 0.3f;
                        attackTimer = 0f;
                        attackState = 1f;
                    }
                    break;

                // Do the charge.
                case 1:
                    npc.rotation = npc.velocity.X * 0.01f;

                    // Make Catastrophe swing his blade.
                    catastropheArmRotation = Utils.Remap(attackTimer, 0f, 9f, -1.8f, 1.1f) * Utils.GetLerpValue(chargeTime + chargeSlowdownTime, chargeTime, attackTimer, true);

                    if (attackTimer == 1f)
                    {
                        SoundEngine.PlaySound(CommonCalamitySounds.MeatySlashSound, npc.Center);
                        npc.velocity = npc.SafeDirectionTo(target.Center, -Vector2.UnitY) * baseChargeSpeed;
                    }

                    // Accelerate during the charge.
                    if (attackTimer <= chargeTime)
                        npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * (npc.velocity.Length() + chargeAcceleration);

                    // Slow down after the charge has ended and look at the target.
                    if (attackTimer > chargeTime)
                    {
                        npc.velocity = npc.velocity.MoveTowards(Vector2.Zero, 0.25f) * 0.8f;
                        npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                    }

                    // Go to the next attack once done slowing down.
                    if (attackTimer > chargeTime + chargeSlowdownTime)
                    {
                        if (isCataclysm)
                            chargeCounter++;

                        if (isCataclysm && chargeCounter >= chargeCount)
                            SelectNextAttack(npc);

                        attackTimer = 0f;
                        attackState = 0f;
                        npc.netUpdate = true;
                    }
                    break;
            }
        }

        public static void DoBehavior_FireAndSwordSlashes(NPC npc, Player target, bool isCataclysm, ref float attackTimer)
        {
            // Define attack values when the other brother is alive.
            int attackShiftDelay = 0;
            int hoverTime = 45;
            int fireReleaseRate = 48;
            float fireShootSpeed = 11f;
            float slashShootSpeed = 14f;
            ref float catastropheArmRotation = ref Main.npc[CalamityGlobalNPC.catastrophe].localAI[0];

            if (BossRushEvent.BossRushActive)
            {
                fireShootSpeed *= 1.4f;
                slashShootSpeed *= 1.5f;
            }

            int attackCycleTime = hoverTime + 360;
            int attackTime = attackCycleTime - 120;
            float wrappedTimer = attackTimer % attackCycleTime;

            // Disable contact damage.
            npc.damage = 0;

            if (attackTimer >= attackTime + attackShiftDelay)
            {
                Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<BrimstoneSlash>(), ModContent.ProjectileType<DarkMagicFlame>());
                SelectNextAttack(npc);
            }

            // Slow down and do nothing prior to the attack ending.
            if (attackTimer >= attackTime)
            {
                npc.velocity *= 0.92f;
                npc.rotation *= 0.92f;
                catastropheArmRotation = catastropheArmRotation.AngleTowards(0f, 0.06f);
                return;
            }

            if (wrappedTimer < hoverTime)
            {
                // Slow down right before firing.
                if (wrappedTimer > hoverTime * 0.5f)
                    npc.velocity *= 0.9f;

                // Otherwise, do typical hover behavior, towards the upper right of the target.
                else
                {
                    Vector2 idealVelocity = ((target.Center + new Vector2(isCataclysm.ToDirectionInt() * 400f, -255f) - npc.Center) * 0.15f).ClampMagnitude(4f, 50f);
                    npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.25f);
                    npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                    npc.rotation = Clamp(npc.velocity.X * 0.026f, -0.2f, 0.2f);
                }
            }
            else
            {
                // Cease all movement when firing.
                npc.velocity = Vector2.Zero;

                // Rapidly approach a 0 rotation.
                npc.rotation = npc.rotation.AngleLerp(0f, 0.1f).AngleTowards(0f, 0.15f);

                // Make catastrophe swing his blade.
                CurveSegment anticipation = new(EasingType.PolyOut, 0f, 0f, -2.4f, 4);
                CurveSegment slash = new(EasingType.PolyInOut, 0.34f, anticipation.EndingHeight, 5.8f, 3);
                CurveSegment recovery = new(EasingType.PolyIn, 0.8f, slash.EndingHeight, -slash.EndingHeight, 8);
                float swingCompletion = Utils.GetLerpValue(0f, fireReleaseRate, wrappedTimer % fireReleaseRate, true);
                catastropheArmRotation = PiecewiseAnimation(swingCompletion, anticipation, slash, recovery);

                if (wrappedTimer % fireReleaseRate == fireReleaseRate - 42f)
                {
                    // Play a firing sound.
                    SoundEngine.PlaySound(CommonCalamitySounds.MeatySlashSound with { Volume = 0.6f }, npc.Center);
                    SoundEngine.PlaySound(SCalNPC.BrimstoneShotSound, npc.Center);

                    // And shoot the projectile serverside.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        if (isCataclysm)
                        {
                            Vector2 fireSpawnPosition = npc.Center - Vector2.UnitY.RotatedBy(npc.rotation * npc.spriteDirection) * 64f;
                            for (int i = 0; i < 5; i++)
                            {
                                float shootOffsetAngle = Lerp(-0.6f, 0.6f, i / 4f);
                                Vector2 fireShootVelocity = (target.Center - fireSpawnPosition).SafeNormalize(Vector2.UnitY).RotatedBy(shootOffsetAngle) * fireShootSpeed;
                                Utilities.NewProjectileBetter(fireSpawnPosition, fireShootVelocity, ModContent.ProjectileType<DarkMagicFlame>(), DarkMagicFlameDamage, 0f);
                            }

                            for (int i = 0; i < 15; i++)
                            {
                                Color fireMistColor = Color.Lerp(Color.Red, Color.Yellow, Main.rand.NextFloat(0.25f, 0.85f));
                                var mist = new MediumMistParticle(fireSpawnPosition + Main.rand.NextVector2Circular(24f, 24f), Main.rand.NextVector2Circular(4.5f, 4.5f) - Vector2.UnitY * 10f, fireMistColor, Color.Gray, Main.rand.NextFloat(0.6f, 1.3f), 192 - Main.rand.Next(50), 0.02f);
                                GeneralParticleHandler.SpawnParticle(mist);
                            }
                        }
                        else
                        {
                            Vector2 slashSpawnPosition = npc.Center - Vector2.UnitX * npc.spriteDirection * 30f;
                            for (int i = 0; i < 3; i++)
                            {
                                float shootOffsetAngle = Lerp(-0.46f, 0.46f, i / 2f);
                                Vector2 slashShootVelocity = (target.Center - slashSpawnPosition).SafeNormalize(Vector2.UnitY).RotatedBy(shootOffsetAngle) * slashShootSpeed;
                                Utilities.NewProjectileBetter(slashSpawnPosition, slashShootVelocity, ModContent.ProjectileType<BrimstoneSlash>(), BrimstoneSlashDamage, 0f);
                            }

                            for (int i = 0; i < 25; i++)
                            {
                                Color fireMistColor = Color.Lerp(Color.Cyan, Color.Orange, Main.rand.NextBool() ? 0.2f : 0.9f);
                                var mist = new MediumMistParticle(slashSpawnPosition + Main.rand.NextVector2Circular(24f, 24f), Main.rand.NextVector2Circular(3.5f, 3.5f) + Vector2.UnitX * -npc.spriteDirection * 16f, fireMistColor, Color.Gray, Main.rand.NextFloat(0.6f, 1.3f), 192 - Main.rand.Next(50), 0.02f);
                                GeneralParticleHandler.SpawnParticle(mist);
                            }
                        }
                    }
                }
            }
        }

        public static void DoBehavior_BladeUppercutAndDashes(NPC npc, Player target, bool isCataclysm)
        {
            ref float localAttackTimer = ref npc.Infernum().ExtraAI[0];

            // Catastrophe does undercut charges from below.
            if (!isCataclysm)
            {
                int attackDelay = 48;
                int uppercutTime = 35;
                int attackRepeatDelay = 38;
                int wrappedAttackTimer = (int)localAttackTimer % (attackDelay + uppercutTime + attackRepeatDelay);
                float baseUppercutSpeed = 12f;
                float uppercutAcceleration = 1.04f;
                ref float armRotation = ref npc.localAI[0];

                // Fly into position.
                if (wrappedAttackTimer <= attackDelay)
                {
                    // Look at the target at first.
                    if (wrappedAttackTimer <= 2f)
                        npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();

                    Vector2 hoverDestination = target.Center + Vector2.UnitY * 425f;
                    if (wrappedAttackTimer >= attackDelay - 4f)
                        npc.Center = Vector2.Lerp(npc.Center, hoverDestination, 0.3f);

                    float flyInterpolant = Utils.Remap(wrappedAttackTimer, 0f, attackDelay, 0.03f, 0.27f);
                    Vector2 idealVelocity = ((hoverDestination - npc.Center) * flyInterpolant * 2f).ClampMagnitude(3f, 80f);
                    npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, flyInterpolant);

                    // Swing the arm back in anticipation.
                    float idealArmRotation = Utils.Remap(wrappedAttackTimer, 0f, attackDelay, 0.1f, -1.76f);
                    armRotation = armRotation.AngleLerp(idealArmRotation, 0.27f).AngleTowards(idealArmRotation, 0.04f);

                    // Disable contact damage.
                    npc.damage = 0;
                }

                // Fly upward.
                if (wrappedAttackTimer == attackDelay)
                {
                    SoundEngine.PlaySound(CommonCalamitySounds.MeatySlashSound with { Volume = 0.7f }, npc.Center);
                    npc.velocity = Vector2.UnitY * -baseUppercutSpeed;
                    npc.netUpdate = true;
                }

                // Accelerate and aim the sword upward.
                if (wrappedAttackTimer > attackDelay && wrappedAttackTimer <= attackDelay + uppercutTime)
                {
                    npc.velocity.Y -= uppercutAcceleration;

                    if (Distance(WrapAngle(armRotation), Pi) > 0.3f)
                        armRotation += 0.29f;

                    // Creation motion blur particles.
                    if (Main.rand.NextBool(4) && npc.velocity.Length() > 8.5f)
                    {
                        Vector2 energySpawnPosition = npc.Center + Main.rand.NextVector2Circular(56f, 56f) + npc.velocity * 3.5f;
                        Vector2 energyVelocity = -npc.velocity.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(6f, 10f);
                        Particle energyLeak = new SquishyLightParticle(energySpawnPosition, energyVelocity, Main.rand.NextFloat(0.55f, 0.9f), Color.OrangeRed, 30, 3.4f, 4.5f, hueShift: 0.002f);
                        GeneralParticleHandler.SpawnParticle(energyLeak);
                    }

                    // Release falling brimstone bombs after the uppercut is over.
                    if (wrappedAttackTimer == attackDelay + uppercutTime && !npc.WithinRange(target.Center, 300f))
                    {
                        SoundEngine.PlaySound(InfernumSoundRegistry.SizzleSound with { Pitch = -0.5f }, target.Center);
                        target.Infernum_Camera().CurrentScreenShakePower = 5f;
                        ScreenEffectSystem.SetFlashEffect(npc.Center, 1f, 20);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            npc.velocity *= 0.3f;
                            npc.netUpdate = true;

                            for (int i = 0; i < 7; i++)
                            {
                                float offsetAngle = Lerp(-1.09f, 1.09f, i / 6f) + Main.rand.NextFloatDirection() * 0.04f;
                                Vector2 shootVelocity = -Vector2.UnitY.RotatedBy(offsetAngle) * Main.rand.NextFloat(10f, 13.6f);
                                shootVelocity += Main.rand.NextVector2Circular(1.5f, 1.5f);
                                Utilities.NewProjectileBetter(npc.Top, shootVelocity, ModContent.ProjectileType<BrimstoneBomb>(), BrimstoneBombDamage, 0f);
                            }
                        }
                    }
                }

                // Reposition after the uppercut is done.
                if (wrappedAttackTimer >= attackDelay + uppercutTime)
                {
                    Vector2 hoverDestination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * 436f;
                    Vector2 idealVelocity = ((hoverDestination - npc.Center) * 0.2f).ClampMagnitude(3f, 50f);
                    npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.12f);
                    npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();

                    // Disable contact damage.
                    npc.damage = 0;

                    armRotation = armRotation.AngleTowards(0f, 0.256f);
                }

                npc.rotation = npc.velocity.X * 0.01f;
            }
            else
            {
                int hoverTime = 48;
                int chargeTime = 45;
                int chargeSlowdowntime = 24;
                int wrappedAttackTimer = (int)localAttackTimer % (hoverTime + chargeTime + chargeSlowdowntime);
                int chargeCount = 3;
                float baseChargeSpeed = 5f;
                float chargeAcceleration = 0.82f;

                // Hover to the side of the target in anticipation of the charge.
                if (wrappedAttackTimer <= hoverTime)
                {
                    Vector2 hoverDestination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * 400f;
                    Vector2 idealVelocity = (hoverDestination - npc.Center) * 0.07f;
                    npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.1f);
                    npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                    npc.damage = 0;
                }

                // Charge at the target.
                if (wrappedAttackTimer == hoverTime + 1f)
                {
                    SoundEngine.PlaySound(SoundID.Item73, npc.Center);
                    npc.velocity = npc.SafeDirectionTo(target.Center) * baseChargeSpeed;
                }

                // Accelerate after charging.
                if (wrappedAttackTimer >= hoverTime + 1f && wrappedAttackTimer <= hoverTime + chargeTime)
                    npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * (npc.velocity.Length() + chargeAcceleration);

                // Slow down in anticipation of the next charge.
                if (wrappedAttackTimer >= hoverTime + chargeTime)
                {
                    npc.damage = 0;
                    npc.velocity *= 0.92f;
                    npc.Center = Vector2.Lerp(npc.Center, target.Center, 0.02f);
                    npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                }

                if (localAttackTimer >= (hoverTime + chargeTime + chargeSlowdowntime) * chargeCount)
                {
                    SelectNextAttack(npc);
                    Main.npc[CalamityGlobalNPC.cataclysm].Infernum().ExtraAI[0] = 0f;
                    Main.npc[CalamityGlobalNPC.cataclysm].Infernum().ExtraAI[1] = 0f;
                }
            }
            localAttackTimer++;
        }

        public static void SelectNextAttack(NPC npc)
        {
            // Catastrophe does not have control over when attack switches happen.
            bool isCatastrophe = npc.type == ModContent.NPCType<Catastrophe>();
            if (isCatastrophe)
                return;

            // The 6 instead of 5 is intentional in the loop below. The fifth index is reserved for the attack specific timer, which should be cleared alongside everything else.
            if (npc.ai[0] == (int)SCalBrotherAttackType.HorizontalCharges)
                npc.ai[0] = (int)SCalBrotherAttackType.FireAndSwordSlashes;
            else if (npc.ai[0] == (int)SCalBrotherAttackType.FireAndSwordSlashes)
                npc.ai[0] = (int)SCalBrotherAttackType.BladeUppercutAndDashes;
            else if (npc.ai[0] == (int)SCalBrotherAttackType.BladeUppercutAndDashes)
                npc.ai[0] = (int)SCalBrotherAttackType.HorizontalCharges;

            npc.ai[1] = 0f;
            for (int i = 0; i < 6; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            npc.netUpdate = true;
        }

        #endregion AI

        #region Frames and Drawcode
        public override void FindFrame(NPC npc, int frameHeight)
        {
            ref float currentFrame = ref npc.localAI[1];
            npc.frameCounter += 0.15;
            if (npc.frameCounter >= 1D)
            {
                currentFrame = (currentFrame + 1f) % 6f;
                npc.frameCounter = 0D;
            }

            npc.frame.Width = 128;
            npc.frame.Height = 208;
            npc.frame.X = 0;
            npc.frame.Y = (int)currentFrame * npc.frame.Height;
        }

        public static bool DrawBrother(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            bool isCatastrophe = npc.type == ModContent.NPCType<Catastrophe>();
            Texture2D texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/CalamitasShadow/Cataclysm").Value;
            if (isCatastrophe)
                texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/CalamitasShadow/Catastrophe").Value;
            Vector2 origin = npc.frame.Size() * 0.5f;
            int afterimageCount = 9;

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < afterimageCount; i += 2)
                {
                    Color afterimageColor = npc.GetAlpha(Color.Lerp(lightColor, Color.White, 0.5f)) * ((afterimageCount - i) / 15f);
                    Vector2 drawPosition = Vector2.Lerp(npc.oldPos[i] + npc.Size * 0.5f, npc.Center, 0.75f) - Main.screenPosition;
                    spriteBatch.Draw(texture, drawPosition, npc.frame, afterimageColor, npc.rotation, origin, npc.scale, spriteEffects, 0f);
                }
            }

            Vector2 mainDrawPosition = npc.Center - Main.screenPosition;

            // Draw backglow afterimages.
            if (npc.localAI[2] > 0f)
            {
                for (int i = 0; i < 8; i++)
                {
                    Vector2 drawOffset = (TwoPi * i / 8f).ToRotationVector2() * npc.localAI[2] * npc.scale * 5f;
                    Color backglowColor = Color.Red * npc.Opacity * npc.localAI[2];
                    backglowColor.A = 0;
                    spriteBatch.Draw(texture, mainDrawPosition + drawOffset, npc.frame, backglowColor, npc.rotation, origin, npc.scale, spriteEffects, 0f);
                }
            }
            spriteBatch.Draw(texture, mainDrawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, spriteEffects, 0f);

            // Draw catastrophe's arm.
            if (isCatastrophe)
            {
                texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/CalamitasShadow/CatastropheGlowmask").Value;
                spriteBatch.Draw(texture, mainDrawPosition, npc.frame, npc.GetAlpha(Color.White), npc.rotation, origin, npc.scale, spriteEffects, 0f);
                float armRotation = npc.localAI[0] * npc.spriteDirection - PiOver4;
                float armSquishFactor = 1f - Utils.GetLerpValue(0.3f, 0.9f, npc.localAI[0], true) * Utils.GetLerpValue(Pi - 0.3f, Pi - 0.9f, npc.localAI[0], true) * 0.33f;
                Texture2D armTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/CalamitasShadow/CatastropheArm").Value;
                Vector2 armTextureDrawPosition = mainDrawPosition + new Vector2(npc.spriteDirection * -12f, Cos(armRotation) * -8f - 40f).RotatedBy(npc.rotation) * npc.scale;
                Vector2 armOrigin = armTexture.Size() * 0f;
                Vector2 armScale = new Vector2(armSquishFactor, 1f + (1f - armSquishFactor) * 1.45f) * npc.scale;
                if (npc.spriteDirection == -1)
                {
                    armOrigin.X = armTexture.Width - armOrigin.X;
                    armTextureDrawPosition.X -= armTexture.Width * npc.scale * 0.5f;
                }
                else
                {
                    armRotation += PiOver2;
                    armTextureDrawPosition.X += armTexture.Width * npc.scale * 0.5f;
                }

                spriteBatch.Draw(armTexture, armTextureDrawPosition, null, npc.GetAlpha(lightColor), armRotation, armOrigin, armScale, spriteEffects, 0f);

                Main.spriteBatch.SetBlendState(BlendState.Additive);
                CatastropheEnergyDrawer.DrawBloom(npc.Center);
                CatastropheEnergyDrawer.DrawPulses(npc.Center);
                CatastropheEnergyDrawer.DrawSet(npc.Center);
                Main.spriteBatch.ResetBlendState();
            }
            else
            {
                texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/CalamitasShadow/CataclysmGlowmask").Value;
                spriteBatch.Draw(texture, mainDrawPosition, npc.frame, npc.GetAlpha(Color.White), npc.rotation, origin, npc.scale, spriteEffects, 0f);

                Main.spriteBatch.SetBlendState(BlendState.Additive);
                CataclysmEnergyDrawer.DrawBloom(npc.Center);
                CataclysmEnergyDrawer.DrawPulses(npc.Center);
                CataclysmEnergyDrawer.DrawSet(npc.Center);
                Main.spriteBatch.ResetBlendState();
            }

            return false;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor) => DrawBrother(npc, spriteBatch, lightColor);
        #endregion Frames and Drawcode
    }
}