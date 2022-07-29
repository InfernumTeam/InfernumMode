using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Tiles;
using InfernumMode.OverridingSystem;
using InfernumMode.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
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
            HellblastBarrage,
            LostSoulBarrage,
            BecomeBerserk,
            SummonSuicideBomberDemons,
            BrimstoneJewelBeam,
            DarkMagicBombWalls,
            SummonBrothers
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

        public static SCalAttackType[] Phase1AttackCycle => new SCalAttackType[]
        {
            // TEST ATTACK. REMOVE LATER.
            SCalAttackType.SummonBrothers,

            SCalAttackType.HorizontalDarkSoulRelease,
            SCalAttackType.CondemnationFanBurst,
            SCalAttackType.ExplosiveCharges,
            SCalAttackType.LostSoulBarrage,
            SCalAttackType.SummonSuicideBomberDemons,
            SCalAttackType.HorizontalDarkSoulRelease,
            SCalAttackType.CondemnationFanBurst,
            SCalAttackType.ExplosiveCharges,
            SCalAttackType.LostSoulBarrage,
            SCalAttackType.SummonSuicideBomberDemons,
        };

        public override int NPCOverrideType => ModContent.NPCType<SCalBoss>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame | NPCOverrideContext.NPCPreDraw;

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
            ref float berserkPhaseInterpolant = ref npc.ai[3];
            ref float frameChangeSpeed = ref npc.localAI[1];
            ref float frameType = ref npc.localAI[2];

            // Set the whoAmI variable.
            CalamityGlobalNPC.SCal = npc.whoAmI;

            // Handle initializations.
            if (npc.localAI[0] == 0f)
            {
                // Define the arena.
                Vector2 arenaArea = new(140f, 140f);
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
                        if ((i == left || i == right || j == top || j == bottom) && !Main.tile[i, j].HasTile)
                        {
                            Main.tile[i, j].TileType = (ushort)arenaTileType;
                            Main.tile[i, j].Get<TileWallWireStateData>().HasTile = true;
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

                npc.ModNPC<SCalBoss>().initialRitualPosition = npc.Center + Vector2.UnitY * 24f;
                attackDelay = 270f;
                npc.localAI[0] = 2f;
                ShieldOpacity = 0f;
                npc.netUpdate = true;
            }

            // Reset things every frame.
            npc.localAI[3] = 0f;

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

            bool inBerserkPhase = berserkPhaseInterpolant > 0f;
            switch ((SCalAttackType)attackType)
            {
                case SCalAttackType.HorizontalDarkSoulRelease:
                    DoBehavior_HorizontalDarkSoulRelease(npc, target, handPosition, inBerserkPhase, ref frameType, ref frameChangeSpeed, ref attackTimer);
                    break;
                case SCalAttackType.CondemnationFanBurst:
                    DoBehavior_CondemnationFanBurst(npc, target, handPosition, inBerserkPhase, ref frameType, ref frameChangeSpeed, ref attackTimer);
                    break;
                case SCalAttackType.ExplosiveCharges:
                    DoBehavior_ExplosiveCharges(npc, target, inBerserkPhase, ref frameType, ref frameChangeSpeed, ref attackTimer);
                    break;
                case SCalAttackType.HellblastBarrage:
                    DoBehavior_HellblastBarrage(npc, target, inBerserkPhase, ref frameType, ref frameChangeSpeed, ref attackTimer);
                    break;
                case SCalAttackType.LostSoulBarrage:
                    DoBehavior_LostSoulBarrage(npc, target, inBerserkPhase, ref frameType, ref frameChangeSpeed, ref attackTimer);
                    break;
                case SCalAttackType.BecomeBerserk:
                    DoBehavior_BecomeBerserk(npc, target, ref frameType, ref frameChangeSpeed, ref attackTimer);
                    break;
                case SCalAttackType.SummonSuicideBomberDemons:
                    DoBehavior_SummonSuicideBomberDemons(npc, target, ref frameType, ref frameChangeSpeed, ref attackTimer);
                    break;
                case SCalAttackType.BrimstoneJewelBeam:
                    DoBehavior_BrimstoneJewelBeam(npc, target, ref frameType, ref frameChangeSpeed, ref attackTimer);
                    break;
                case SCalAttackType.DarkMagicBombWalls:
                    DoBehavior_DarkMagicBombWalls(npc, target, ref frameType, ref frameChangeSpeed, ref attackTimer);
                    break;
                case SCalAttackType.SummonBrothers:
                    DoBehavior_SummonBrothers(npc, target, ref frameType, ref frameChangeSpeed, ref attackTimer);
                    break;
            }

            attackTimer++;

            return false;
        }

        public static void DoBehavior_HorizontalDarkSoulRelease(NPC npc, Player target, Vector2 handPosition, bool inBerserkPhase, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)
        {
            int boltBurstReleaseCount = 2;
            int shootDelay = 60;
            int shootTime = 180;
            int shootRate = 8;
            float soulShootSpeed = 17f;

            if (inBerserkPhase)
            {
                shootRate -= 2;
                soulShootSpeed += 5.6f;
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
                    SoundEngine.PlaySound(SoundID.NPCDeath52, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int shootCounter = (int)((attackTimer - shootDelay) / shootRate);
                        float offsetAngle = MathHelper.Lerp(-0.67f, 0.67f, shootCounter % 3f / 2f) + Main.rand.NextFloatDirection() * 0.25f;
                        Vector2 soulVelocity = (Vector2.UnitX * (target.Center.X > npc.Center.X).ToDirectionInt()).RotatedBy(offsetAngle) * soulShootSpeed;
                        soulVelocity.Y += target.velocity.Y;

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

        public static void DoBehavior_CondemnationFanBurst(NPC npc, Player target, Vector2 handPosition, bool inBerserkPhase, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)
        {
            int chargeupTime = 120;
            int condemnationSpinTime = 48;
            int condemnationChargePuffRate = 15;
            int fanShootTime = 52;
            int shootRate = 2;
            int shootCount = 3;
            float shootSpeed = 11.25f;
            float angularVariance = 2.94f;

            if (inBerserkPhase)
            {
                condemnationSpinTime -= 6;
                condemnationChargePuffRate -= 2;
                shootSpeed += 5f;
                angularVariance *= 0.8f;
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
            float condemnationSpinInterpolant = Utils.GetLerpValue(condemnationSpinTime + 10f, condemnationSpinTime, attackTimer, true);
            if (condemnationRef != null)
                condemnationRef.rotation = aimAtTargetRotation.AngleLerp(spinRotation, condemnationSpinInterpolant);

            // Create puffs of energy at the tip of Condemnation after the spin completes.
            if (condemnationRef != null && attackTimer >= condemnationSpinTime && attackTimer < chargeupTime &&
                attackTimer % condemnationChargePuffRate == condemnationChargePuffRate - 1f)
            {
                // Play a sound for additional notification that an arrow has been loaded.
                SoundEngine.PlaySound(SoundID.Item108 with { Volume = 0.3f });
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
                SoundEngine.PlaySound(SoundID.Item73, handPosition);
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
        
        public static void DoBehavior_ExplosiveCharges(NPC npc, Player target, bool inBerserkPhase, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)
        {
            int chargeDelay = 50;
            int chargeTime = 36;
            int chargeCount = 6;
            int explosionDelay = 120;
            float chargeSpeed = 43f;
            float bombShootSpeed = 20f;
            float bombExplosionRadius = 1020f;
            ref float chargeCounter = ref npc.Infernum().ExtraAI[0];

            if (inBerserkPhase)
            {
                chargeCount--;
                explosionDelay -= 25;
                chargeSpeed += 3.5f;
            }
            
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

                // Aim the shield and use laughing frames.
                float idealRotation = npc.AngleTo(target.Center);
                ShieldRotation = ShieldRotation.AngleLerp(idealRotation, 0.125f);
                ShieldRotation = ShieldRotation.AngleTowards(idealRotation, 0.18f);
                npc.localAI[3] = 1f;
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

                    SoundEngine.PlaySound(SCalBoss.DashSound, npc.Center);

                    // Release a bomb at the target.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 bombShootVelocity = npc.SafeDirectionTo(target.Center) * bombShootSpeed;
                        int bomb = Utilities.NewProjectileBetter(npc.Center, bombShootVelocity, ModContent.ProjectileType<DemonicBomb>(), 500, 0f);
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
            {
                ShieldRotation = 0f;
                ShieldOpacity = 0f;
                SelectNewAttack(npc);
            }
        }

        public static void DoBehavior_HellblastBarrage(NPC npc, Player target, bool inBerserkPhase, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)
        {
            int shootDelay = 105;
            int hellblastShootRate = 12;
            int verticalBobPeriod = 56;
            int shootTime = 360;
            int endOfAttackShootBlockTime = 90;
            int dartBurstPeriod = 5;
            int dartCount = 7;
            float dartSpeed = 8.4f;
            float verticalBobAmplitude = 330f;
            float hoverSpeedFactor = Utilities.Remap(attackTimer, 0f, shootDelay * 0.65f, 0.36f, 1f);
            bool hasBegunFiring = attackTimer >= shootDelay;
            Vector2 handPosition = CalculateHandPosition();
            ref float shootCounter = ref npc.Infernum().ExtraAI[0];

            if (inBerserkPhase)
            {
                hellblastShootRate -= 3;
                verticalBobAmplitude += 50f;
                dartSpeed += 3.6f;
            }

            // Hover to the side of the target. Once she begins firing, SCal bobs up and down.
            npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
            Vector2 hoverDestination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * 700f;
            if (hasBegunFiring)
                hoverDestination.Y += (float)Math.Sin((attackTimer - shootDelay) * MathHelper.Pi / verticalBobPeriod) * verticalBobAmplitude;

            Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * hoverSpeedFactor * MathHelper.Min(npc.Distance(hoverDestination), 32f);
            npc.SimpleFlyMovement(idealVelocity, hoverSpeedFactor * 2.25f);
            npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.1f);

            // Use the magic cast animation when firing and a magic circle prior to that, as a charge-up effect.
            frameChangeSpeed = 0.2f;
            frameType = (int)(hasBegunFiring ? SCalFrameType.OutwardHandCast : SCalFrameType.MagicCircle);

            // Create an explosion effect prior to firing.
            if (attackTimer == shootDelay)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.CalThunderStrikeSound, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int explosion = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<DemonicExplosion>(), 0, 0f);
                    if (Main.projectile.IndexInRange(explosion))
                        Main.projectile[explosion].ModProjectile<DemonicExplosion>().MaxRadius = 300f;
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
                    Vector2 projectileVelocity = (npc.SafeDirectionTo(target.Center) * new Vector2(1f, 0.1f)).SafeNormalize(Vector2.UnitY) * 25f;
                    Vector2 hellblastSpawnPosition = npc.Center + projectileVelocity * 0.4f;
                    int projectileType = ModContent.ProjectileType<BrimstoneHellblast>();
                    Utilities.NewProjectileBetter(hellblastSpawnPosition, projectileVelocity, projectileType, 500, 0f, Main.myPlayer);

                    // Release a burst of darts after a certain number of hellblasts have been fired.
                    if (shootCounter % dartBurstPeriod == dartBurstPeriod - 1f)
                    {
                        for (int i = 0; i < dartCount; i++)
                        {
                            float dartOffsetAngle = MathHelper.Lerp(-0.45f, 0.45f, i / (float)(dartCount - 1f));
                            Vector2 dartVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(dartOffsetAngle) * dartSpeed;
                            Utilities.NewProjectileBetter(npc.Center, dartVelocity, ModContent.ProjectileType<BrimstoneBarrage>(), 500, 0f, Main.myPlayer);
                        }
                    }

                    shootCounter++;
                }
            }

            if (attackTimer >= shootDelay + shootTime + endOfAttackShootBlockTime)
                SelectNewAttack(npc);
        }

        public static void DoBehavior_DarkMagicBombWalls(NPC npc, Player target, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)
        {
            int chargeupTime = HeresyProjSCal.ChargeupTime;
            int cindersPerBurst = 3;
            int shootRate = 20;
            int shootTime = 330;
            int endOfAttackShootBlockTime = 90;
            int bombReleasePeriod = 3;
            int bombShootDelay = shootRate * bombReleasePeriod;
            int totalBombsToShootPerBurst = 13;
            int telegraphReleaseRate = 7;
            float totalBombOffset = 1800f;
            float shootSpeed = 2.7f;
            float bombExplosionRadius = 1080f;

            int telegraphTime = totalBombsToShootPerBurst * telegraphReleaseRate;
            int bombShootTime = telegraphTime + 16;
            float wrappedBombShootTimer = (attackTimer - chargeupTime) % (bombShootDelay + bombShootTime);
            bool hasBegunFiring = attackTimer >= chargeupTime;
            Vector2 handPosition = CalculateHandPosition();
            ref float shootCounter = ref npc.Infernum().ExtraAI[0];
            ref float bombFireOffsetAngle = ref npc.Infernum().ExtraAI[1];
            ref float bombFirePositionX = ref npc.Infernum().ExtraAI[2];
            ref float bombFirePositionY = ref npc.Infernum().ExtraAI[3];

            // Hover to the side of the target. Once she begins firing, SCal bobs up and down.
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
                SoundEngine.PlaySound(InfernumSoundRegistry.CalThunderStrikeSound, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int explosion = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<DemonicExplosion>(), 0, 0f);
                    if (Main.projectile.IndexInRange(explosion))
                        Main.projectile[explosion].ModProjectile<DemonicExplosion>().MaxRadius = 300f;
                }
            }

            // Release bursts of cinders.
            if (hasBegunFiring && attackTimer % shootRate == shootRate - 1f && attackTimer < chargeupTime + shootTime)
            {
                SoundEngine.PlaySound(SCalBoss.HellblastSound, npc.Center);
                float cinderSpawnOffsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                for (int i = 0; i < cindersPerBurst; i++)
                {
                    Vector2 shootOffset = (MathHelper.TwoPi * i / cindersPerBurst + cinderSpawnOffsetAngle).ToRotationVector2() * 1000f;
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
                        Utilities.NewProjectileBetter(target.Center + shootOffset, cinderShootVelocity, ModContent.ProjectileType<AcceleratingDarkMagicFlame>(), 500, 0f);
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
                        bombFireOffsetAngle = MathHelper.TwoPi * Main.rand.NextFloat(8) / 8f;
                    while (bombFireOffsetAngle.ToRotationVector2().AngleBetween(target.velocity) < 0.91f);
                    bombFirePositionX = target.Center.X + (float)Math.Cos(bombFireOffsetAngle) * 1150f;
                    bombFirePositionY = target.Center.Y + (float)Math.Sin(bombFireOffsetAngle) * 1150f;
                    npc.netUpdate = true;
                }

                // Create telegraph lines.
                if (wrappedBombShootTimer <= bombShootDelay + telegraphTime && wrappedBombShootTimer % telegraphReleaseRate == telegraphReleaseRate - 1f)
                {
                    SoundEngine.PlaySound(SCalBoss.BrimstoneShotSound, target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float bombFireOffset = MathHelper.Lerp(-totalBombOffset, totalBombOffset, Utils.GetLerpValue(0f, telegraphTime, wrappedBombShootTimer - bombShootDelay)) * 0.5f;
                        Vector2 bombShootPosition = new Vector2(bombFirePositionX, bombFirePositionY) + (bombFireOffsetAngle + MathHelper.PiOver2).ToRotationVector2() * bombFireOffset;
                        Vector2 telegraphDirection = bombFireOffsetAngle.ToRotationVector2() * -0.001f;
                        int telegraph = Utilities.NewProjectileBetter(bombShootPosition, telegraphDirection, ModContent.ProjectileType<DemonicTelegraphLine>(), 0, 0f);
                        if (Main.projectile.IndexInRange(telegraph))
                        {
                            Main.projectile[telegraph].ai[1] = 45f;
                            Main.projectile[telegraph].localAI[0] = bombExplosionRadius;
                        }
                    }
                }
            }

            if (attackTimer >= chargeupTime + shootTime + endOfAttackShootBlockTime)
            {
                Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<HeresyProjSCal>());
                SelectNewAttack(npc);
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
                    int explosion = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<DemonicExplosion>(), 0, 0f);
                    if (Main.projectile.IndexInRange(explosion))
                        Main.projectile[explosion].ModProjectile<DemonicExplosion>().MaxRadius = 500f;
                    npc.ai[3] = 1f;
                }
                SelectNewAttack(npc);
            }
        }

        public static void DoBehavior_SummonSuicideBomberDemons(NPC npc, Player target, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)
        {
            int demonSummonRate = 6;
            int demonSummonCount = 6;
            int dartShootRate = 32;
            int dartCount = 5;
            int castTime = demonSummonRate * demonSummonCount + SuicideBomberRitual.Lifetime + 45;
            float dartSpeed = 7.5f;
            Vector2 handPosition = CalculateHandPosition();
            bool doneAttacking = attackTimer >= castTime + SuicideBomberDemonHostile.AttackDuration;
            ref float demonCircleCounter = ref npc.Infernum().ExtraAI[0];
            ref float dartShootCounter = ref npc.Infernum().ExtraAI[1];
            ref float hoverOffsetDirection = ref npc.Infernum().ExtraAI[2];

            // Define the frame change speed.
            frameChangeSpeed = 0.2f;

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
                    Vector2 circleSpawnPosition = handPosition + (MathHelper.TwoPi * demonCircleCounter / demonSummonCount).ToRotationVector2() * 225f;

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
                        float dartOffsetAngle = MathHelper.Lerp(-0.45f, 0.45f, i / (float)(dartCount - 1f));
                        Vector2 dartVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(dartOffsetAngle) * dartSpeed;
                        Utilities.NewProjectileBetter(npc.Center, dartVelocity, ModContent.ProjectileType<BrimstoneBarrage>(), 500, 0f, Main.myPlayer);
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

                        int explosion = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<DemonicExplosion>(), 0, 0f);
                        if (Main.projectile.IndexInRange(explosion))
                            Main.projectile[explosion].ModProjectile<DemonicExplosion>().MaxRadius = 300f;
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
                SelectNewAttack(npc);
            }
        }

        public static void DoBehavior_BrimstoneJewelBeam(NPC npc, Player target, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)
        {
            int jewelChargeupTime = BrimstoneJewelProj.ChargeupTime;
            int laserbeamLifetime = BrimstoneLaserbeam.Lifetime;
            int dartReleaseRate = 8;
            int bombReleaseRate = 60;
            int ritualCreationRate = 85;
            float dartShootSpeed = 16f;
            float bombExplosionRadius = 1100f;
            float spinArc = MathHelper.TwoPi * 2.2f;
            Vector2 handPosition = CalculateHandPosition();
            ref float brimstoneJewelIndex = ref npc.Infernum().ExtraAI[0];
            ref float spinDirection = ref npc.Infernum().ExtraAI[1];

            // Define the projectile as a convenient reference type variable, for easy manipulation of its attributes.
            Projectile jewelRef = Main.projectile[(int)brimstoneJewelIndex];
            if (jewelRef.type != ModContent.ProjectileType<BrimstoneJewelProj>())
                jewelRef = null;

            // Use the hands out casting animation.
            frameChangeSpeed = 0.25f;
            frameType = (int)SCalFrameType.BlastCast;

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

                // Teleport to the center of the arena.
                npc.Center = npc.Infernum().arenaRectangle.Center.ToVector2();
                npc.velocity = Vector2.Zero;
                SoundEngine.PlaySound(InfernumSoundRegistry.CalThunderStrikeSound, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int explosion = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<DemonicExplosion>(), 0, 0f);
                    if (Main.projectile.IndexInRange(explosion))
                        Main.projectile[explosion].ModProjectile<DemonicExplosion>().MaxRadius = 300f;
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
                float angularTurnSpeed = Utilities.Remap(attackTimer, 0f, jewelChargeupTime * 0.67f, MathHelper.Pi / 16f, MathHelper.Pi / 355f);
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
                SoundEngine.PlaySound(InfernumSoundRegistry.CalThunderStrikeSound, npc.Center);
                SoundEngine.PlaySound(HolyBlast.ImpactSound, npc.Center);
                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceHolyRaySound, npc.Center);

                Vector2 aimDirection = (jewelRef.rotation + MathHelper.PiOver2).ToRotationVector2();
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center, aimDirection, ModContent.ProjectileType<BrimstoneLaserbeam>(), 900, 0f);
            }

            // Make the laserbeam spin after it's created.
            // Also release bursts of bombs and darts rapid-fire.
            else if (jewelRef != null && attackTimer > jewelChargeupTime)
            {
                // Initialize the spin direction.
                if (spinDirection == 0f)
                {
                    spinDirection = (MathHelper.WrapAngle(jewelRef.AngleTo(target.Center) - jewelRef.rotation) > 0f).ToDirectionInt();
                    npc.netUpdate = true;
                }

                jewelRef.rotation += spinArc / laserbeamLifetime * spinDirection;
                npc.spriteDirection = (Math.Cos(jewelRef.rotation) < 0f).ToDirectionInt();

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
                        Vector2 bombShootVelocity = npc.SafeDirectionTo(target.Center) * dartShootSpeed * 1.6f;
                        int bomb = Utilities.NewProjectileBetter(npc.Center, bombShootVelocity, ModContent.ProjectileType<DemonicBomb>(), 0, 0f);
                        if (Main.projectile.IndexInRange(bomb))
                        {
                            Main.projectile[bomb].ai[0] = bombExplosionRadius;
                            Main.projectile[bomb].timeLeft = 120;
                        }
                    }
                }

                // Summon rituals.
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % ritualCreationRate == ritualCreationRate - 1f)
                {
                    Vector2 circleSpawnPosition = target.Center + target.velocity * 48f;
                    Utilities.NewProjectileBetter(circleSpawnPosition, Vector2.Zero, ModContent.ProjectileType<SuicideBomberRitual>(), 0, 0f);
                }
            }

            if (attackTimer >= jewelChargeupTime + laserbeamLifetime)
            {
                Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<BrimstoneJewelProj>(), ModContent.ProjectileType<BrimstoneLaserbeam>());
                SelectNewAttack(npc);
            }
        }

        public static void DoBehavior_LostSoulBarrage(NPC npc, Player target, bool inBerserkPhase, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)
        {
            int shootDelay = 105;
            int shootCycleTime = 150;
            int shootTime = 480;
            int endOfAttackShootBlockTime = 90;
            int soulReleaseRate = 2;
            float hoverSpeedFactor = Utilities.Remap(attackTimer, 0f, shootDelay * 0.65f, 0.36f, 1f);
            float maxFanOffsetAngle = 1.09f;
            float soulSpeed = 14.5f;

            if (inBerserkPhase)
            {
                maxFanOffsetAngle += 0.19f;
                soulSpeed += 2.7f;
            }

            float wrappedAttackTimer = (attackTimer - shootDelay) % shootCycleTime;
            bool hasBegunFiring = attackTimer >= shootDelay;
            Vector2 handPosition = CalculateHandPosition();
            ref float shootCounter = ref npc.Infernum().ExtraAI[0];
            ref float initialDirection = ref npc.Infernum().ExtraAI[1];

            // Handle slowdown effects once firing.
            if (hasBegunFiring)
                hoverSpeedFactor = Utilities.Remap(wrappedAttackTimer, 0f, shootCycleTime * 0.4f, 1f, 0.08f);

            // Hover to the side of the target. Once she begins firing, SCal bobs up and down.
            npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
            Vector2 hoverDestination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * 700f;
            Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * hoverSpeedFactor * MathHelper.Min(npc.Distance(hoverDestination), 32f);
            npc.SimpleFlyMovement(idealVelocity, hoverSpeedFactor * 2.25f);
            npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.1f);

            // Use the magic cast animation when firing and a magic circle prior to that, as a charge-up effect.
            frameChangeSpeed = 0.2f;
            frameType = (int)(hasBegunFiring ? SCalFrameType.OutwardHandCast : SCalFrameType.MagicCircle);

            // Create an explosion effect prior to firing.
            if (attackTimer == shootDelay)
            {
                npc.Center = target.Center - Vector2.UnitY * 475f + Main.rand.NextVector2Circular(5f, 5f);
                SoundEngine.PlaySound(InfernumSoundRegistry.CalThunderStrikeSound, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int explosion = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<DemonicExplosion>(), 0, 0f);
                    if (Main.projectile.IndexInRange(explosion))
                        Main.projectile[explosion].ModProjectile<DemonicExplosion>().MaxRadius = 300f;
                    npc.netUpdate = true;
                }
            }

            // Release a spread of lost spirits.
            if (wrappedAttackTimer >= shootCycleTime * 0.4f && attackTimer < shootDelay + shootTime)
            {
                // Initialize the shoot direction of the souls.
                if (wrappedAttackTimer == (int)Math.Ceiling(shootCycleTime * 0.4f))
                {
                    initialDirection = (target.Center - handPosition).ToRotation();
                    npc.netUpdate = true;
                }

                if (attackTimer % soulReleaseRate == 0f)
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath52, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float fanInterpolant = Utils.GetLerpValue(shootCycleTime * 0.4f, shootCycleTime, wrappedAttackTimer, true);
                        float offsetAngle = (float)Math.Sin(MathHelper.Pi * 3f * fanInterpolant) * maxFanOffsetAngle;
                        Vector2 shootVelocity = (initialDirection + offsetAngle).ToRotationVector2() * soulSpeed;
                        Utilities.NewProjectileBetter(handPosition, shootVelocity, ModContent.ProjectileType<LostSoulProj>(), 500, 0f);
                    }
                }
            }

            if (attackTimer >= shootDelay + shootTime + endOfAttackShootBlockTime)
                SelectNewAttack(npc);
        }

        public static void DoBehavior_SummonBrothers(NPC npc, Player target, ref float frameType, ref float frameChangeSpeed, ref float attackTimer)
        {
            int screenShakeTime = 135;

            // Laugh on the first frame.
            if (attackTimer == 1f)
                SoundEngine.PlaySound(SCalBoss.SpawnSound, target.Center);

            // Slow down and look at the target.
            npc.velocity *= 0.95f;
            if (npc.velocity.Length() < 8f)
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();

            // Disable contact damage.
            npc.damage = 0;

            // Use the magic circle animation, as a charge-up effect.
            frameChangeSpeed = 0.2f;
            frameType = (int)SCalFrameType.MagicCircle;

            // Shake the screen.
            float screenShakeDistanceFade = Utils.GetLerpValue(npc.Distance(target.Center), 2600f, 1375f, true);
            float screenShakeFactor = Utils.Remap(attackTimer, 25f, screenShakeTime, 2f, 12.5f) * screenShakeDistanceFade;
            if (attackTimer >= screenShakeTime)
                screenShakeFactor = 0f;

            target.Calamity().GeneralScreenShakePower = screenShakeFactor;

            // Create the portals.
            if (attackTimer == screenShakeTime - 50f)
            {
                SoundEngine.PlaySound(SoundID.Item103, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int portal = Utilities.NewProjectileBetter(npc.Center - Vector2.UnitX * 600f, Vector2.Zero, ModContent.ProjectileType<SupremeCalamitasBrotherPortal>(), 0, 0f);
                    if (Main.projectile.IndexInRange(portal))
                        Main.projectile[portal].ai[0] = ModContent.NPCType<SupremeCataclysm>();

                    portal = Utilities.NewProjectileBetter(npc.Center + Vector2.UnitX * 600f, Vector2.Zero, ModContent.ProjectileType<SupremeCalamitasBrotherPortal>(), 0, 0f);
                    if (Main.projectile.IndexInRange(portal))
                        Main.projectile[portal].ai[0] = ModContent.NPCType<SupremeCatastrophe>();

                    npc.netUpdate = true;
                }
            }

            if (attackTimer >= screenShakeTime + SupremeCalamitasBrotherPortal.Lifetime && !NPC.AnyNPCs(ModContent.NPCType<SupremeCataclysm>()))
                SelectNewAttack(npc);
        }

        public static void SelectNewAttack(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            // Reset the berserk phase.
            npc.ai[3] = 0f;
            npc.ai[0] = (int)Phase1AttackCycle[(int)npc.Infernum().ExtraAI[5] % Phase1AttackCycle.Length];
            npc.Infernum().ExtraAI[5]++;

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

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            float berserkPhaseInterpolant = npc.ai[3];
            Texture2D energyChargeupEffect = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/SupremeCalamitas/PowerEffect").Value;
            Texture2D texture2D15 = DownedBossSystem.downedSCal && !BossRushEvent.BossRushActive ? TextureAssets.Npc[npc.type].Value : ModContent.Request<Texture2D>("CalamityMod/NPCs/SupremeCalamitas/SupremeCalamitasHooded").Value;

            // Draw a chargeup effect behind SCal if berserk.
            if (berserkPhaseInterpolant > 0f)
            {
                Color chargeupColor = Color.White * berserkPhaseInterpolant;
                Vector2 chargeupDrawPosition = npc.Bottom - Main.screenPosition + Vector2.UnitY * 20f;
                Rectangle chargeupFrame = energyChargeupEffect.Frame(1, 5, 0, (int)(Main.GlobalTimeWrappedHourly * 15.6f) % 5);
                Main.spriteBatch.Draw(energyChargeupEffect, chargeupDrawPosition, chargeupFrame, chargeupColor, npc.rotation, chargeupFrame.Size() * new Vector2(0.5f, 1f), npc.scale * 1.4f, 0, 0f);
            }

            Vector2 vector11 = new(texture2D15.Width / 2f, texture2D15.Height / Main.npcFrameCount[npc.type] / 2f);
            Color color36 = Color.White;
            float amount9 = 0.5f;
            int num153 = 7;

            Rectangle frame = texture2D15.Frame(2, Main.npcFrameCount[npc.type], npc.frame.Y / Main.npcFrameCount[npc.type], npc.frame.Y % Main.npcFrameCount[npc.type]);

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int num155 = 1; num155 < num153; num155 += 2)
                {
                    Color color38 = lightColor;
                    color38 = Color.Lerp(color38, color36, amount9);
                    color38 = npc.GetAlpha(color38);
                    color38 *= (num153 - num155) / 15f;
                    Vector2 vector41 = npc.oldPos[num155] + new Vector2(npc.width, npc.height) / 2f - Main.screenPosition;
                    vector41 -= new Vector2(texture2D15.Width / 2f, texture2D15.Height / Main.npcFrameCount[npc.type]) * npc.scale / 2f;
                    vector41 += vector11 * npc.scale + new Vector2(0f, npc.gfxOffY);
                    Main.spriteBatch.Draw(texture2D15, vector41, frame, color38, npc.rotation, vector11, npc.scale, spriteEffects, 0f);
                }
            }

            bool inPhase2 = npc.ai[0] >= 3f && npc.life > npc.lifeMax * 0.01 || berserkPhaseInterpolant > 0f;
            Vector2 vector43 = npc.Center - Main.screenPosition;
            vector43 -= new Vector2(texture2D15.Width / 2f, texture2D15.Height / Main.npcFrameCount[npc.type]) * npc.scale / 2f;
            vector43 += vector11 * npc.scale + new Vector2(0f, npc.gfxOffY);

            if (inPhase2)
            {
                // Make the sprite jitter with rage in phase 2. This does not happen in rematches since it would make little sense logically.
                if (!DownedBossSystem.downedSCal)
                    vector43 += Main.rand.NextVector2Circular(0.8f, 2f);

                // And gain a flaming aura.
                Color auraColor = npc.GetAlpha(Color.Red) * 0.4f;
                for (int i = 0; i < 7; i++)
                {
                    Vector2 rotationalDrawOffset = (MathHelper.TwoPi * i / 7f + Main.GlobalTimeWrappedHourly * 4f).ToRotationVector2();
                    rotationalDrawOffset *= MathHelper.Lerp(3f, 4.25f, (float)Math.Cos(Main.GlobalTimeWrappedHourly * 4f) * 0.5f + 0.5f);
                    Main.spriteBatch.Draw(texture2D15, vector43 + rotationalDrawOffset, frame, auraColor, npc.rotation, vector11, npc.scale * 1.1f, spriteEffects, 0f);
                }
            }
            Main.spriteBatch.Draw(texture2D15, vector43, frame, npc.GetAlpha(lightColor), npc.rotation, vector11, npc.scale, spriteEffects, 0f);

            // Draw special effects in SCal's berserk phase.
            if (berserkPhaseInterpolant > 0f)
            {
                float eyePulse = Main.GlobalTimeWrappedHourly * 0.84f % 1f;
                Texture2D eyeGleam = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/Gleam").Value;
                Vector2 eyePosition = npc.Center + new Vector2(npc.spriteDirection * -4f, -14f);
                Vector2 horizontalGleamScaleSmall = new Vector2(berserkPhaseInterpolant * 3f, 1f) * 0.36f;
                Vector2 verticalGleamScaleSmall = new Vector2(1f, berserkPhaseInterpolant * 2f) * 0.36f;
                Vector2 horizontalGleamScaleBig = horizontalGleamScaleSmall * (1f + eyePulse * 2f);
                Vector2 verticalGleamScaleBig = verticalGleamScaleSmall * (1f + eyePulse * 2f);
                Color eyeGleamColorSmall = Color.Violet * berserkPhaseInterpolant;
                Color eyeGleamColorBig = eyeGleamColorSmall * (1f - eyePulse);

                // Draw a pulsating red eye.
                Main.spriteBatch.Draw(eyeGleam, eyePosition - Main.screenPosition, null, eyeGleamColorSmall, 0f, eyeGleam.Size() * 0.5f, horizontalGleamScaleSmall, 0, 0f);
                Main.spriteBatch.Draw(eyeGleam, eyePosition - Main.screenPosition, null, eyeGleamColorSmall, 0f, eyeGleam.Size() * 0.5f, verticalGleamScaleSmall, 0, 0f);
                Main.spriteBatch.Draw(eyeGleam, eyePosition - Main.screenPosition, null, eyeGleamColorBig, 0f, eyeGleam.Size() * 0.5f, horizontalGleamScaleBig, 0, 0f);
                Main.spriteBatch.Draw(eyeGleam, eyePosition - Main.screenPosition, null, eyeGleamColorBig, 0f, eyeGleam.Size() * 0.5f, verticalGleamScaleBig, 0, 0f);
            }

            DrawForcefield(spriteBatch, npc);
            DrawShield(spriteBatch, npc);
            return false;
        }


        public static void DrawForcefield(SpriteBatch spriteBatch, NPC npc)
        {
            Main.spriteBatch.EnterShaderRegion();

            float intensity = 0.25f;

            // Shield intensity is always high during invincibility.
            if (npc.dontTakeDamage)
                intensity = 0.75f + Math.Abs((float)Math.Cos(Main.GlobalTimeWrappedHourly * 1.7f)) * 0.1f;

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
            float opacity = MathHelper.Lerp(1f, MathHelper.Max(1f - flickerPower, 0.56f), (float)Math.Pow(Math.Cos(Main.GlobalTimeWrappedHourly * MathHelper.Lerp(3f, 5f, flickerPower)), 24D));

            // During/prior to a charge the forcefield is always darker than usual and thus its intensity is also higher.
            if (!npc.dontTakeDamage && ShieldOpacity > 0f)
                intensity = 1.1f;

            // Dampen the opacity and intensity slightly, to allow SCal to be more easily visible inside of the forcefield.
            intensity *= 0.75f;
            opacity *= 0.75f;

            Texture2D forcefieldTexture = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/CalamitasShield").Value;
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

        public static void DrawShield(SpriteBatch spriteBatch, NPC npc)
        {
            float jawRotation = ShieldRotation;
            float jawRotationOffset = 0f;
            bool shouldUseShieldLaughAnimation = npc.localAI[3] != 0f;

            // Have an agape mouth when charging.
            if (npc.ai[1] == 2f)
                jawRotationOffset -= 0.71f;

            // And a laugh right before the charge.
            else if (shouldUseShieldLaughAnimation)
                jawRotationOffset += MathHelper.Lerp(0.04f, -0.82f, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 17.2f) * 0.5f + 0.5f);

            Color shieldColor = Color.White * ShieldOpacity;
            Texture2D shieldSkullTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/SupremeCalamitas/SupremeShieldTop").Value;
            Texture2D shieldJawTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/SupremeCalamitas/SupremeShieldBottom").Value;
            Vector2 drawPosition = npc.Center + ShieldRotation.ToRotationVector2() * 24f - Main.screenPosition;
            Vector2 jawDrawPosition = drawPosition;
            SpriteEffects direction = Math.Cos(ShieldRotation) > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically;
            if (direction == SpriteEffects.FlipVertically)
                jawDrawPosition += (ShieldRotation - MathHelper.PiOver2).ToRotationVector2() * 42f;
            else
            {
                jawDrawPosition += (ShieldRotation + MathHelper.PiOver2).ToRotationVector2() * 42f;
                jawRotationOffset *= -1f;
            }

            Main.spriteBatch.Draw(shieldJawTexture, jawDrawPosition, null, shieldColor, jawRotation + jawRotationOffset, shieldJawTexture.Size() * 0.5f, 1f, direction, 0f);
            Main.spriteBatch.Draw(shieldSkullTexture, drawPosition, null, shieldColor, ShieldRotation, shieldSkullTexture.Size() * 0.5f, 1f, direction, 0f);
        }
        #endregion Frames and Drawcode
    }
}