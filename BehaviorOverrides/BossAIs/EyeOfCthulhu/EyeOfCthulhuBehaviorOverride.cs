using CalamityMod.Events;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.EyeOfCthulhu
{
    public class EyeOfCthulhuBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.EyeofCthulhu;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        #region Enumerations
        public enum EoCAttackType
        {
            HoverCharge,
            ChargingServants,
            HorizontalBloodCharge,
            TeethSpit,
            SpinDash,
            BloodShots
        }
        #endregion

        #region AI

        public const float Phase2LifeRatio = 0.8f;
        public const float Phase3LifeRatio = 0.35f;

        public static EoCAttackType[] Phase1AttackPattern = new EoCAttackType[]
        {
            EoCAttackType.HoverCharge,
            EoCAttackType.HoverCharge,
            EoCAttackType.ChargingServants,
            EoCAttackType.HoverCharge,
            EoCAttackType.HorizontalBloodCharge,
            EoCAttackType.HoverCharge,
            EoCAttackType.ChargingServants,
        };

        public static EoCAttackType[] Phase2AttackPattern = new EoCAttackType[]
        {
            EoCAttackType.HoverCharge,
            EoCAttackType.HoverCharge,
            EoCAttackType.ChargingServants,
            EoCAttackType.SpinDash,
            EoCAttackType.HoverCharge,
            EoCAttackType.HoverCharge,
            EoCAttackType.TeethSpit,
            EoCAttackType.HoverCharge,
            EoCAttackType.HoverCharge,
            EoCAttackType.SpinDash,
            EoCAttackType.HorizontalBloodCharge,
            EoCAttackType.HoverCharge,
            EoCAttackType.HoverCharge,
            EoCAttackType.TeethSpit,
            EoCAttackType.SpinDash,
        };

        public static EoCAttackType[] Phase3AttackPattern = new EoCAttackType[]
        {
            EoCAttackType.HoverCharge,
            EoCAttackType.HoverCharge,
            EoCAttackType.BloodShots,
            EoCAttackType.ChargingServants,
            EoCAttackType.SpinDash,
            EoCAttackType.HoverCharge,
            EoCAttackType.HoverCharge,
            EoCAttackType.BloodShots,
            EoCAttackType.HoverCharge,
            EoCAttackType.HoverCharge,
            EoCAttackType.HorizontalBloodCharge,
            EoCAttackType.BloodShots,
            EoCAttackType.HoverCharge,
            EoCAttackType.HoverCharge,
            EoCAttackType.TeethSpit,
            EoCAttackType.SpinDash,
        };

        public override bool PreAI(NPC npc)
        {
            Player target = Main.player[npc.target];

            bool canDespawn = Main.dayTime && !BossRushEvent.BossRushActive;
            if (canDespawn || target.dead)
            {
                npc.TargetClosest();
                target = Main.player[npc.target];

                if (canDespawn || target.dead)
                {
                    npc.velocity.Y -= 0.18f;
                    npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;

                    if (npc.timeLeft > 120)
                        npc.timeLeft = 120;
                    if (!npc.WithinRange(target.Center, 2300f))
                        npc.active = false;
                    return false;
                }
            }

            npc.TargetClosest();

            ref float attackTimer = ref npc.ai[2];
            ref float phase2ResetTimer = ref npc.Infernum().ExtraAI[6];
            ref float gleamTimer = ref npc.localAI[0];

            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool phase2 = lifeRatio < Phase2LifeRatio;
            bool phase3 = lifeRatio < Phase3LifeRatio;
            npc.damage = npc.defDamage + 12;
            if (phase2)
            {
                npc.defense = 4;
                npc.damage += 28;
            }

            // Handle the Phase 2 transition.
            if (phase2 && phase2ResetTimer < 180f)
            {
                phase2ResetTimer++;
                if (phase2ResetTimer < 120f)
                {
                    npc.Opacity = MathHelper.Lerp(1f, 0f, phase2ResetTimer / 120f);
                    npc.velocity *= 0.94f;
                    if (phase2ResetTimer >= 120f - GleamTime)
                        gleamTimer++;
                }
                if (phase2ResetTimer == 120f)
                {
                    npc.Center = target.Center + Main.rand.NextVector2CircularEdge(405f, 405f);
                    npc.ai[0] = 3f;
                    npc.ai[3] = 0f; // Reset the attack state index.
                    npc.netUpdate = true;
                }
                if (phase2ResetTimer > 120f)
                    npc.alpha = Utils.Clamp(npc.alpha - 25, 0, 255);
                npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(target.Center) - MathHelper.PiOver2, 0.2f);
                return false;
            }

            switch ((EoCAttackType)(int)npc.ai[1])
            {
                case EoCAttackType.HoverCharge:
                    DoBehavior_HoverCharge(npc, target, phase2, lifeRatio, ref attackTimer);
                    break;
                case EoCAttackType.ChargingServants:
                    DoBehavior_ChargingServants(npc, target, phase2, lifeRatio, ref attackTimer);
                    break;
                case EoCAttackType.HorizontalBloodCharge:
                    DoBehavior_HorizontalBloodCharge(npc, target, phase2, ref attackTimer);
                    break;
                case EoCAttackType.TeethSpit:
                    DoBehavior_TeethSpit(npc, target, phase3, ref attackTimer);
                    break;
                case EoCAttackType.SpinDash:
                    DoBehavior_SpinDash(npc, target, ref attackTimer);
                    break;
                case EoCAttackType.BloodShots:
                    DoBehavior_BloodShots(npc, target, ref attackTimer);
                    break;
            }

            attackTimer++;
            return false;
        }

        public static void DoBehavior_HoverCharge(NPC npc, Player target, bool phase2, float lifeRatio, ref float attackTimer)
        {
            int hoverTime = 60;
            int chargeTime = phase2 ? 30 : 45;
            float chargeSpeed = MathHelper.Lerp(10f, 13.33f, 1f - lifeRatio);
            if (phase2)
                chargeSpeed += 1.3f;
            float hoverAcceleration = MathHelper.Lerp(0.1f, 0.25f, 1f - lifeRatio);
            float hoverSpeed = MathHelper.Lerp(8.5f, 17f, 1f - lifeRatio);

            if (BossRushEvent.BossRushActive)
            {
                chargeSpeed *= 3f;
                hoverAcceleration *= 2.25f;
                hoverSpeed *= 1.75f;
            }

            if (attackTimer < hoverTime)
            {
                Vector2 destination = target.Center - Vector2.UnitY * 185f;
                npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * hoverSpeed, hoverAcceleration);
                npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(target.Center) - MathHelper.PiOver2, 0.2f);
            }
            else if (attackTimer == hoverTime)
            {
                npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;

                // Normal boss roar.
                Main.PlaySound(SoundID.Roar, (int)npc.Center.X, (int)npc.Center.Y, 0, 1f, 0f);
            }
            else if (attackTimer >= hoverTime + chargeTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_ChargingServants(NPC npc, Player target, bool phase2, float lifeRatio, ref float attackTimer)
        {
            npc.damage = 0;

            int servantSummonDelay = phase2 ? 35 : 42;
            int servantsToSummon = phase2 ? 9 : 6;
            int servantSummonTime = phase2 ? 54 : 85;
            int servantSpawnRate = servantSummonTime / servantsToSummon;

            float hoverAcceleration = MathHelper.Lerp(0.15f, 0.35f, 1f - lifeRatio);
            float hoverSpeed = MathHelper.Lerp(14f, 18f, 1f - lifeRatio);
            if (attackTimer < servantSummonDelay)
            {
                Vector2 destination = target.Center - Vector2.UnitY * 275f;
                npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * hoverSpeed, hoverAcceleration);
            }
            else
            {
                if ((attackTimer - servantSummonDelay) % servantSpawnRate == servantSpawnRate - 1)
                {
                    Vector2 spawnPosition = npc.Center + Main.rand.NextVector2CircularEdge(120f, 120f);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int eye = NPC.NewNPC((int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<ExplodingServant>());
                        Main.npc[eye].target = npc.target;
                        Main.npc[eye].velocity = Main.npc[eye].SafeDirectionTo(target.Center) * 4.5f;
                    }

                    if (!Main.dedServ)
                    {
                        for (int i = 0; i < 20; i++)
                        {
                            float angle = MathHelper.TwoPi * i / 20f;
                            Dust magicBlood = Dust.NewDustPerfect(spawnPosition + angle.ToRotationVector2() * 4f, 261);
                            magicBlood.color = Color.IndianRed;
                            magicBlood.velocity = angle.ToRotationVector2() * 5f;
                            magicBlood.noGravity = true;
                        }
                    }
                }

                Vector2 destination = target.Center - Vector2.UnitY * 300f;
                if (npc.WithinRange(destination, 400f))
                    npc.velocity *= 0.93f;
                else
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * hoverSpeed, hoverAcceleration);
            }

            npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(target.Center) - MathHelper.PiOver2, 0.2f);

            if (attackTimer >= servantSummonDelay + servantSummonTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_HorizontalBloodCharge(NPC npc, Player target, bool phase2, ref float attackTimer)
        {
            int toothReleaseRate = phase2 ? 9 : 15;
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float chargeDirection = ref npc.Infernum().ExtraAI[1];

            if (chargeDirection == 0f)
                chargeDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

            // Redirect.
            if (attackSubstate == 0f)
            {
                // Don't do damage while redirecting.
                npc.damage = 0;

                float redirectSpeed = attackTimer / 15f + 14f;
                Vector2 destination = target.Center + new Vector2(-chargeDirection * 1100f, -300f);
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(destination) * redirectSpeed, 0.06f);
                npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(target.Center) - MathHelper.PiOver2, 0.2f);
                if (npc.WithinRange(destination, 32f))
                {
                    attackSubstate = 1f;
                    npc.velocity = npc.SafeDirectionTo(target.Center - Vector2.UnitY * 300f) * 15f;
                    if (BossRushEvent.BossRushActive)
                        npc.velocity *= 2f;

                    npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
                    npc.netUpdate = true;
                    attackTimer = 0f;

                    // High pitched boss roar.
                    Main.PlaySound(SoundID.ForceRoar, (int)npc.Center.X, (int)npc.Center.Y, -1, 1f, 0f);
                }
            }

            // And shoot blood spit/balls.
            if (attackSubstate == 1f)
            {
                bool closeToPlayer = Math.Abs(npc.Center.X - target.Center.X) <= 300f + target.velocity.X * 6f;
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % toothReleaseRate == toothReleaseRate - 1 && !closeToPlayer)
                {
                    Vector2 spawnPosition = npc.Center + npc.velocity.SafeNormalize(Vector2.Zero) * 72f;
                    Vector2 shootVelocity = npc.velocity;
                    shootVelocity.X *= Main.rand.NextFloat(0.35f, 0.65f);

                    Utilities.NewProjectileBetter(spawnPosition, shootVelocity, ModContent.ProjectileType<SittingBlood>(), 60, 0f);
                }

                if (attackTimer >= 90f || Math.Abs(npc.Center.X - target.Center.X) > 1200f)
                    SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_TeethSpit(NPC npc, Player target, bool phase3, ref float attackTimer)
        {
            int teethPerShot = phase3 ? 7 : 4;
            int teethBurstTotal = phase3 ? 6 : 4;
            float teethRadialSpread = phase3 ? 1.42f : 1.21f;
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float teethBurstCounter = ref npc.Infernum().ExtraAI[1];
            ref float teethBurstDelay = ref npc.Infernum().ExtraAI[2];

            // Redirect.
            if (attackSubstate == 0f)
            {
                // Don't do damage while redirecting.
                npc.damage = 0;

                float redirectSpeed = attackTimer / 24f + 14f;
                Vector2 destination = target.Center - Vector2.UnitY * 265f;
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(destination) * redirectSpeed, 0.06f);
                npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(target.Center) - MathHelper.PiOver2, 0.2f);
                if (npc.WithinRange(destination, 32f))
                {
                    attackSubstate = 1f;
                    npc.netUpdate = true;

                    // High pitched boss roar.
                    Main.PlaySound(SoundID.ForceRoar, (int)npc.Center.X, (int)npc.Center.Y, -1, 1f, 0f);
                }
                if (npc.WithinRange(target.Center, 115f))
                    npc.Center -= npc.SafeDirectionTo(target.Center) * 15f;
            }

            // Release teeth into the sky.
            if (attackSubstate == 1f)
            {
                float idealAngle = MathHelper.Lerp(-teethRadialSpread, teethRadialSpread, teethBurstCounter / teethBurstTotal);
                npc.rotation = npc.rotation.AngleTowards(idealAngle - MathHelper.Pi, 0.08f);
                npc.velocity *= 0.9f;

                if (teethBurstDelay <= 0f && Math.Abs(MathHelper.WrapAngle(idealAngle - npc.rotation - MathHelper.Pi)) < 0.07f)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 spawnPosition = npc.Center - Vector2.UnitY * 20f;
                        for (int i = 0; i < teethPerShot; i++)
                        {
                            float offsetAngle = MathHelper.Lerp(-0.52f, 0.52f, i / (float)teethPerShot) + Main.rand.NextFloat(-0.07f, 0.07f);
                            Vector2 toothShootVelocity = -Vector2.UnitY.RotatedBy(offsetAngle) * 25.6f;
                            if (BossRushEvent.BossRushActive)
                                toothShootVelocity *= 1.6f;
                            Utilities.NewProjectileBetter(spawnPosition, toothShootVelocity, ModContent.ProjectileType<EoCTooth>(), 70, 0f, 255, npc.target);
                        }
                    }
                    teethBurstDelay = 8f;
                    teethBurstCounter++;
                    npc.netUpdate = true;
                }
                if (teethBurstDelay > 0f)
                    teethBurstDelay--;
            }
            if (teethBurstCounter >= teethBurstTotal)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_SpinDash(NPC npc, Player target, ref float attackTimer)
        {
            int spinCycles = 2;
            int spinTime = 120;
            int chargeDelay = 25;
            int chargeChainCount = 3;
            float chargeTime = 60;
            float chargeSpeed = 18f;
            float chargeAcceleration = 1.006f;
            float spinRadius = 345f;
            if (BossRushEvent.BossRushActive)
            {
                chargeChainCount = 2;
                chargeSpeed *= 2.25f;
            }

            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float spinAngle = ref npc.Infernum().ExtraAI[1];
            ref float redirectSpeed = ref npc.Infernum().ExtraAI[2];
            ref float chainChargeCounter = ref npc.Infernum().ExtraAI[3];

            // Redirect.
            if (attackSubstate == 0f)
            {
                // Don't do damage while redirecting.
                npc.damage = 0;

                if (spinAngle == 0f)
                {
                    spinAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                    redirectSpeed = 13f;
                    npc.netUpdate = true;
                }

                if (redirectSpeed < 30f)
                    redirectSpeed *= 1.015f;

                Vector2 destination = target.Center + spinAngle.ToRotationVector2() * spinRadius;
                npc.velocity = (npc.velocity * 3f + npc.SafeDirectionTo(destination) * redirectSpeed) / 4f;
                npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;

                if (npc.WithinRange(destination, redirectSpeed + 8f))
                {
                    attackTimer = 0f;
                    attackSubstate = 1f;
                    npc.velocity = Vector2.Zero;
                    npc.Center = target.Center + spinAngle.ToRotationVector2() * spinRadius;
                    npc.netUpdate = true;

                    // High pitched boss roar.
                    Main.PlaySound(SoundID.ForceRoar, (int)npc.Center.X, (int)npc.Center.Y, -1, 1f, 0f);
                }
            }

            // Spin.
            if (attackSubstate == 1f)
            {
                spinAngle += MathHelper.TwoPi * spinCycles / spinTime;
                npc.Center = target.Center + spinAngle.ToRotationVector2() * spinRadius;
                npc.rotation = spinAngle;
                if (attackTimer >= spinTime)
                {
                    attackTimer = 0f;
                    npc.velocity = (spinAngle + MathHelper.PiOver2).ToRotationVector2() * 9.5f;
                    attackSubstate = 2f;
                    npc.netUpdate = true;
                }
            }

            // Slow down and aim.
            if (attackSubstate == 2f)
            {
                npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(target.Center) - MathHelper.PiOver2, 0.2f);
                npc.velocity *= 0.985f;
                if (attackTimer >= chargeDelay)
                {
                    attackTimer = 0f;
                    npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                    attackSubstate = 3f;
                    npc.netUpdate = true;

                    // Normal boss roar.
                    Main.PlaySound(SoundID.Roar, (int)npc.Center.X, (int)npc.Center.Y, 0, 1f, 0f);
                }
            }

            // Accelerate while charging.
            if (attackSubstate == 3f)
            {
                npc.velocity *= chargeAcceleration;
                if (attackTimer >= chargeTime)
                {
                    npc.velocity *= 0.25f;
                    attackTimer = 0f;
                    attackSubstate = 4f;
                    npc.netUpdate = true;
                }
            }

            // Do a chain of multiple charges that become slower and slower.
            if (attackSubstate == 4f)
            {
                if (chainChargeCounter > chargeChainCount)
                    SelectNextAttack(npc);

                if (npc.velocity.Length() < 8f)
                {
                    float idealAngle = npc.AngleTo(target.Center);
                    npc.rotation = npc.rotation.AngleTowards(idealAngle - MathHelper.PiOver2, MathHelper.Pi * 0.08f);
                    npc.velocity *= 0.985f;

                    attackTimer++;
                    if (attackTimer >= chargeDelay)
                    {
                        attackTimer = 0f;
                        chainChargeCounter++;
                        npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed * 1.6f;
                        npc.velocity *= MathHelper.Lerp(1f, 0.67f, chainChargeCounter / chargeChainCount);
                        npc.netUpdate = true;

                        // High pitched boss roar.
                        Main.PlaySound(SoundID.ForceRoar, (int)npc.Center.X, (int)npc.Center.Y, -1, 1f, 0f);
                    }
                }
                else
                {
                    attackTimer = 0f;
                    npc.velocity *= 0.9785f;
                    if (BossRushEvent.BossRushActive)
                        npc.velocity *= 0.97f;
                }
            }
        }

        public static void DoBehavior_BloodShots(NPC npc, Player target, ref float attackTimer)
        {
            int shootDelay = 75;
            int shootTime = 90;
            int totalShots = 4;
            Vector2 shootDirection = (npc.rotation + MathHelper.PiOver2).ToRotationVector2();
            Vector2 shootCenter = npc.Center + shootDirection * 60f;
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];

            npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(target.Center) - MathHelper.PiOver2, 0.2f);
            if (attackTimer < shootDelay)
            {
                // Attempt to get close to the player.
                if (npc.WithinRange(target.Center, 500f))
                    npc.velocity *= 0.95f;
                else
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center) * 18f, 0.8f);
            }
            else if (attackTimer < shootDelay + shootTime)
            {
                npc.velocity *= 0.9f;

                float wrappedTimer = (attackTimer - shootDelay) % (shootTime / (float)totalShots);

                // Charge up.
                if (wrappedTimer < shootTime / (float)totalShots * 0.8f)
                {
                    Dust blood = Dust.NewDustPerfect(shootCenter + Main.rand.NextVector2Circular(8f, 8f), 267);
                    blood.velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 6f);
                    blood.color = Color.Lerp(Color.Red, Color.DarkRed, Main.rand.NextFloat());
                    blood.scale = Main.rand.NextFloat(1f, 1.4f);
                    blood.noLight = true;
                    blood.noGravity = true;
                }

                // Release blood.
                if ((int)wrappedTimer == 0)
                {
                    npc.velocity -= shootDirection * 8f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int bloodShotCount = Main.rand.Next(3, 6);
                        for (int i = 0; i < bloodShotCount; i++)
                        {
                            Vector2 velocity = shootDirection * 10f + Main.rand.NextVector2Square(-5f, 5f);
                            Utilities.NewProjectileBetter(shootCenter - shootDirection * 5f, velocity, ModContent.ProjectileType<BloodShot>(), 80, 0f);
                        }
                    }
                }
            }

            if (attackTimer >= shootDelay + shootTime)
                SelectNextAttack(npc);
        }

        public static void SelectNextAttack(NPC npc)
        {
            npc.ai[3]++;

            EoCAttackType[] patternToUse = Phase1AttackPattern;
            if (npc.life < npc.lifeMax * Phase2LifeRatio)
                patternToUse = Phase2AttackPattern;
            if (npc.life < npc.lifeMax * Phase3LifeRatio)
                patternToUse = Phase3AttackPattern;
            EoCAttackType nextAttackType = patternToUse[(int)(npc.ai[3] % patternToUse.Length)];

            // Select the next AI state.
            npc.ai[1] = (int)nextAttackType;

            // Reset the attack timer.
            npc.ai[2] = 0f;

            // And reset the misc ai slots.
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
        }
        #endregion

        #region Drawing

        internal const int GleamTime = 45;

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            Texture2D eyeTexture = Main.npcTexture[npc.type];
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 eyeOrigin = eyeTexture.Size() / new Vector2(1f, Main.npcFrameCount[npc.type]) * 0.5f;
            spriteBatch.Draw(eyeTexture, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, eyeOrigin, npc.scale, spriteEffects, 0f);

            float gleamTimer = npc.localAI[0];
            Vector2 pupilPosition = npc.Center + new Vector2(0f, 74f).RotatedBy(npc.rotation) - Main.screenPosition;
            Texture2D pupilStarTexture = ModContent.GetTexture("InfernumMode/ExtraTextures/Gleam");
            Vector2 pupilOrigin = pupilStarTexture.Size() * 0.5f;

            Vector2 pupilScale = new Vector2(0.7f, 1.5f) * Utils.InverseLerp(0f, 8f, gleamTimer, true) * Utils.InverseLerp(GleamTime, GleamTime - 8f, gleamTimer, true); ;
            Color pupilColor = Color.Red * 0.6f * Utils.InverseLerp(0f, 10f, gleamTimer, true) * Utils.InverseLerp(GleamTime, GleamTime - 10f, gleamTimer, true);
            spriteBatch.Draw(pupilStarTexture, pupilPosition, null, pupilColor, npc.rotation, pupilOrigin, pupilScale, SpriteEffects.None, 0f);
            pupilScale = new Vector2(0.7f, 2.7f);
            spriteBatch.Draw(pupilStarTexture, pupilPosition, null, pupilColor, npc.rotation + MathHelper.PiOver2, pupilOrigin, pupilScale, SpriteEffects.None, 0f);
            return false;
        }
        #endregion
    }
}