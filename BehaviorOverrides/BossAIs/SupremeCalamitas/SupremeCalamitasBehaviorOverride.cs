using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.NPCs;
using CalamityMod.Tiles;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using SCalBoss = CalamityMod.NPCs.SupremeCalamitas.SupremeCalamitas;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class SupremeCalamitasBehaviorOverride : NPCBehaviorOverride
    {
        public enum SCalAttackType
        {
            HorizontalDarkSoulRelease,
            CondemnationFanBurst,
            ExplosiveCharges,
            DarkMagicCircleBarrage
        }

        public enum SCalFrameType
        {
            UpwardDraft,
            FasterUpwardDraft,
            Casting,
            BlastCast,
            BlastPunchCast,
            OutwardHandCast,
            PunchHandCast,
            Count
        }

        // TODO -- Manually handle drawcode so that the shield can be drawn as intended without horrible IL edits and reflection.
        private static readonly FieldInfo shieldOpacityField = typeof(SCalBoss).GetField("shieldOpacity", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo shieldRotationField = typeof(SCalBoss).GetField("shieldRotation", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo forcefieldScaleField = typeof(SCalBoss).GetField("forcefieldScale", BindingFlags.NonPublic | BindingFlags.Instance);

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

                return (float)shieldOpacityField.GetValue(SCal.modNPC);
            }
            set
            {
                if (SCal is null)
                    return;

                shieldOpacityField.SetValue(SCal.modNPC, value);
            }
        }

        public static float ShieldRotation
        {
            get
            {
                if (SCal is null)
                    return 0f;

                return (float)shieldRotationField.GetValue(SCal.modNPC);
            }
            set
            {
                if (SCal is null)
                    return;

                shieldRotationField.SetValue(SCal.modNPC, value);
            }
        }

        public static float ForcefieldScale
        {
            get
            {
                if (SCal is null)
                    return 0f;

                return (float)forcefieldScaleField.GetValue(SCal.modNPC);
            }
            set
            {
                if (SCal is null)
                    return;

                forcefieldScaleField.SetValue(SCal.modNPC, value);
            }
        }

        public override int NPCOverrideType => ModContent.NPCType<SCalBoss>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame;

        #region AI

        public static Vector2 CalculateHandPosition()
        {
            if (SCal is null)
                return Vector2.Zero;

            return SCal.Center + new Vector2(SCal.spriteDirection * -18f, 2f);
        }

        public override bool PreAI(NPC npc)
        {
            // Do targeting.
            npc.TargetClosest();
            Player target = Main.player[npc.target];

            Vector2 handPosition = CalculateHandPosition();
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float attackDelay = ref npc.ai[2];
            ref float frameChangeSpeed = ref npc.localAI[1];
            ref float frameType = ref npc.localAI[2];

            // Set the whoAmI variable.
            CalamityGlobalNPC.SCal = npc.whoAmI;

            // Handle initializations.
            if (npc.localAI[0] == 0f)
            {
                // Define the arena.
                Vector2 arenaArea = new Vector2(140f, 140f);
                npc.Infernum().arenaRectangle = Utils.CenteredRectangle(npc.Center, arenaArea * 16f);
                int left = (int)(npc.Infernum().arenaRectangle.Center().X / 16 - arenaArea.X * 0.5f);
                int right = (int)(npc.Infernum().arenaRectangle.Center().X / 16 + arenaArea.X * 0.5f);
                int top = (int)(npc.Infernum().arenaRectangle.Center().Y / 16 - arenaArea.Y * 0.5f);
                int bottom = (int)(npc.Infernum().arenaRectangle.Center().Y / 16 + arenaArea.Y * 0.5f);
                int arenaTileType = ModContent.TileType<ArenaTile>();

                for (int i = left; i <= right; i++)
                {
                    for (int j = top; j <= bottom; j++)
                    {
                        if (!WorldGen.InWorld(i, j))
                            continue;

                        // Create arena tiles.
                        if ((i == left || i == right || j == top || j == bottom) && !Main.tile[i, j].active())
                        {
                            Main.tile[i, j].type = (ushort)arenaTileType;
                            Main.tile[i, j].active(true);
                            if (Main.netMode == NetmodeID.Server)
                                NetMessage.SendTileSquare(-1, i, j, 1, TileChangeType.None);
                            else
                                WorldGen.SquareTileFrame(i, j, true);
                        }
                    }
                }

                // Teleport above the player.
                Vector2 oldPosition = npc.Center;
                npc.Center = target.Center - Vector2.UnitY * 160f;
                Dust.QuickDustLine(oldPosition, npc.Center, 300f, Color.Red);

                typeof(SCalBoss).GetField("initialRitualPosition", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(npc.modNPC, npc.Center + Vector2.UnitY * 24f);
                attackDelay = 180f;
                npc.localAI[0] = 2f;
                npc.netUpdate = true;
            }

            // Vanish if the target is gone.
            if (!target.active || target.dead)
            {
                npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.1f, 0f, 1f);

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
                attackDelay--;
                return false;
            }

            switch ((SCalAttackType)attackType)
            {
                case SCalAttackType.HorizontalDarkSoulRelease:
                    DoBehavior_HorizontalDarkSoulRelease(npc, target, handPosition, ref frameType, ref frameChangeSpeed, ref attackTimer);
                    break;
                case SCalAttackType.CondemnationFanBurst:
                    DoBehavior_CondemnationFanBurst(npc, target, handPosition, ref frameType, ref frameChangeSpeed, ref attackTimer);
                    break;
                case SCalAttackType.ExplosiveCharges:
                    DoBehavior_ExplosiveCharges(npc, target, ref frameType, ref frameChangeSpeed, ref attackTimer);
                    break;
            }

            attackTimer++;

            return false;
        }

        public static void DoBehavior_HorizontalDarkSoulRelease(NPC npc, Player target, Vector2 handPosition, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)
        {
            int boltBurstReleaseCount = 2;
            int shootDelay = 60;
            int shootTime = 180;
            int shootRate = 8;
            float soulShootSpeed = 17f;
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
            Vector2 hoverDestination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * 700f;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 32f, 1.2f);

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
                    Main.PlaySound(SoundID.NPCDeath52, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int shootCounter = (int)((attackTimer - shootDelay) / shootRate);
                        float offsetAngle = MathHelper.Lerp(-0.67f, 0.67f, shootCounter % 3f / 2f) + Main.rand.NextFloatDirection() * 0.25f;
                        Vector2 soulVelocity = (Vector2.UnitX * (target.Center.X > npc.Center.X).ToDirectionInt()).RotatedBy(offsetAngle) * soulShootSpeed;
                        Utilities.NewProjectileBetter(handPosition, soulVelocity, ModContent.ProjectileType<RedirectingDarkSoul>(), 500, 0f);
                    }
                }

                if (attackTimer >= shootDelay + shootTime)
                {
                    attackTimer = 0f;
                    boltBurstCounter++;

                    if (boltBurstCounter >= boltBurstReleaseCount)
                        SelectNewAttack(npc);
                }
            }
        }

        public static void DoBehavior_CondemnationFanBurst(NPC npc, Player target, Vector2 handPosition, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)
        {
            int chargeupTime = 120;
            int condemnationSpinTime = 48;
            int condemnationChargePuffRate = 15;
            int fanShootTime = 52;
            int shootRate = 2;
            int shootCount = 3;
            float shootSpeed = 11.25f;
            float angularVariance = 2.94f;
            float fanAngularOffsetInterpolant = Utils.InverseLerp(chargeupTime - 45f, chargeupTime - 8f, attackTimer, true);
            float fanCompletionInterpolant = Utils.InverseLerp(0f, fanShootTime, attackTimer - chargeupTime, true);
            float hoverSpeedFactor = Utils.InverseLerp(chargeupTime * 0.75f, 0f, attackTimer, true) * 0.65f + 0.35f;
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
            Vector2 hoverDestination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * 600f;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeedFactor * 32f, hoverSpeedFactor * 1.2f);

            // Create Condemnation on the first frame and decide which direction the fan will go in.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 1f)
            {
                condemnationIndex = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<CondemnationProj>(), 0, 0f);
                fanDirection = Main.rand.NextBool().ToDirectionInt();
                npc.netUpdate = true;
            }

            // Spin condemnation around before aiming it at the target.
            float spinRotation = MathHelper.WrapAngle(MathHelper.Pi * attackTimer / condemnationSpinTime * 6f);
            float aimAtTargetRotation = (target.Center - handPosition + target.velocity * 10f).ToRotation();
            if (playerAimLockonDirection != 0f)
                aimAtTargetRotation = playerAimLockonDirection;

            // Define the lock-on direction.
            if (playerAimLockonDirection == 0f && fanCompletionInterpolant > 0f)
            {
                playerAimLockonDirection = aimAtTargetRotation;
                npc.netUpdate = true;
            }

            // Make the aim direction move upward before firing, in anticipation of the fan.
            aimAtTargetRotation -= angularVariance * fanDirection * fanAngularOffsetInterpolant * MathHelper.Lerp(-0.5f, 0.5f, fanCompletionInterpolant);

            // Adjust Condemnation's rotation.
            float condemnationSpinInterpolant = Utils.InverseLerp(condemnationSpinTime + 10f, condemnationSpinTime, attackTimer, true);
            if (condemnationRef != null)
                condemnationRef.rotation = aimAtTargetRotation.AngleLerp(spinRotation, condemnationSpinInterpolant);

            // Create puffs of energy at the tip of Condemnation after the spin completes.
            if (condemnationRef != null && attackTimer >= condemnationSpinTime && attackTimer < chargeupTime &&
                attackTimer % condemnationChargePuffRate == condemnationChargePuffRate - 1f)
            {
                // Play a sound for additional notification that an arrow has been loaded.
                var loadSound = Main.PlaySound(SoundID.Item108);
                if (loadSound != null)
                    loadSound.Volume *= 0.3f;

                Vector2 condemnationTip = condemnationRef.ModProjectile<CondemnationProj>().TipPosition;
                for (int i = 0; i < 36; i++)
                {
                    Dust chargeMagic = Dust.NewDustPerfect(condemnationTip, 267);
                    chargeMagic.velocity = (MathHelper.TwoPi * i / 36f).ToRotationVector2() * 5f + npc.velocity;
                    chargeMagic.scale = Main.rand.NextFloat(1f, 1.5f);
                    chargeMagic.color = Color.Violet;
                    chargeMagic.noGravity = true;
                }
            }

            // Release arrows from condemnation's tip once ready to fire.
            if (condemnationRef != null && fanCompletionInterpolant > 0f && attackTimer % shootRate == shootRate - 1f)
            {
                Main.PlaySound(SoundID.Item73, handPosition);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 shootVelocity = condemnationRef.rotation.ToRotationVector2() * shootSpeed;
                    Utilities.NewProjectileBetter(condemnationRef.ModProjectile<CondemnationProj>().TipPosition, shootVelocity, ModContent.ProjectileType<CondemnationArrowSCal>(), 500, 0f);
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
                    SelectNewAttack(npc);
                }
                else
                    attackTimer = condemnationSpinTime;

                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_ExplosiveCharges(NPC npc, Player target, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)
        {
            int chargeDelay = 50;
            int chargeTime = 32;
            int chargeCount = 6;
            int explosionDelay = 120;
            float chargeSpeed = 40f;
            float bombShootSpeed = 20f;
            float bombExplosionRadius = 720f;
            ref float chargeCounter = ref npc.Infernum().ExtraAI[0];

            // Use the updraft animation.
            frameChangeSpeed = 0.2f;
            frameType = (int)SCalFrameType.UpwardDraft;

            // Hover near the target and have the shield laugh at the target before charging.
            if (attackTimer < chargeDelay)
            {
                ShieldOpacity = MathHelper.Clamp(ShieldOpacity + 0.1f, 0f, 1f);
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 500f, -270f) - npc.velocity;

                npc.Center = npc.Center.MoveTowards(hoverDestination, 10f);
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 28f, 1.1f);

                // Aim the shield.
                float idealRotation = npc.AngleTo(target.Center);
                ShieldRotation = ShieldRotation.AngleLerp(idealRotation, 0.125f);
                ShieldRotation = ShieldRotation.AngleTowards(idealRotation, 0.18f);
            }

            // Charge rapid-fire.
            // TODO -- Create a motion blur effect for this in the drawcode.
            if (attackTimer >= chargeDelay)
            {
                if ((attackTimer - chargeDelay) % chargeTime == 0f)
                {
                    npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                    npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                    ShieldRotation = npc.AngleTo(target.Center);

                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/SCalSounds/SCalDash"), npc.Center);

                    // Release a bomb at the target.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 bombShootVelocity = npc.SafeDirectionTo(target.Center) * bombShootSpeed;
                        int bomb = Utilities.NewProjectileBetter(npc.Center, bombShootVelocity, ModContent.ProjectileType<DemonicBomb>(), 0, 0f);
                        if (Main.projectile.IndexInRange(bomb))
                        {
                            Main.projectile[bomb].ai[0] = bombExplosionRadius;
                            Main.projectile[bomb].timeLeft = (int)(chargeDelay + chargeTime * chargeCount - attackTimer) + explosionDelay;
                        }

                        npc.netUpdate = true;
                    }
                }

                // Slow down a bit after charging.
                else
                    npc.velocity *= 0.987f;
            }

            if (attackTimer >= chargeDelay + chargeTime * chargeCount)
                SelectNewAttack(npc);
        }

        public static void SelectNewAttack(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            npc.ai[0] = 3f;
            npc.ai[1] = 0f;
            npc.netUpdate = true;
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
        #endregion Frames and Drawcode
    }
}