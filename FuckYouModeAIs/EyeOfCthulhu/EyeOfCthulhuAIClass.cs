using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.EyeOfCthulhu
{
	public class EyeOfCthulhuAIClass
    {
        #region Enumerations
        public enum EoCAttackType
        {
            HoverCharge,
            ChargingServants,
            HorizontalBloodCharge,
            TeethSpit,
            SpinDash
        }
        #endregion

        #region AI

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

        [OverrideAppliesTo(NPCID.EyeofCthulhu, typeof(EyeOfCthulhuAIClass), "EyeOfCthulhuAI", EntityOverrideContext.NPCAI)]
        public static bool EyeOfCthulhuAI(NPC npc)
        {
            Player target = Main.player[npc.target];
            if (Main.dayTime || target.dead)
            {
                npc.TargetClosest();
                if (Main.dayTime || target.dead)
                {
                    npc.velocity.Y -= 0.08f;
                    npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;

                    if (npc.timeLeft > 120)
                        npc.timeLeft = 120;
                    return false;
                }
            }

            npc.damage = npc.defDamage;
            npc.TargetClosest();

            ref float attackTimer = ref npc.ai[2];
            ref float phase2ResetTimer = ref npc.Infernum().ExtraAI[6];
            ref float gleamTimer = ref npc.localAI[0];

            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool phase2 = lifeRatio < 0.8f;
            bool phase3 = lifeRatio < 0.33f;

            void goToNextAIState()
            {
                // You cannot use ref locals inside of a delegate context.
                // You should be able to find most important, universal locals above, anyway.
                // Any others that don't have an explicit reference above are exclusively for
                // AI state manipulation.

                npc.ai[3]++;

                EoCAttackType[] patternToUse = phase2 ? Phase2AttackPattern : Phase1AttackPattern;
                EoCAttackType nextAttackType = patternToUse[(int)(npc.ai[3] % patternToUse.Length)];

                // Going to the next AI state.
                npc.ai[1] = (int)nextAttackType;

                // Resetting the attack timer.
                npc.ai[2] = 0f;

                // And the misc ai slots.
                for (int i = 0; i < 5; i++)
                {
                    npc.Infernum().ExtraAI[i] = 0f;
                }
            }

            // Phase 2 transition.
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
                    npc.Center = target.Center + Main.rand.NextVector2CircularEdge(325f, 325f);
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
                    int hoverTime = 60;
                    int chargeTime = phase2 ? 30 : 45;
                    float chargeSpeed = MathHelper.Lerp(8f, 13f, 1f - lifeRatio);
                    if (phase2)
                        chargeSpeed += 1.5f;
                    float hoverAcceleration = MathHelper.Lerp(0.1f, 0.25f, 1f - lifeRatio);
                    float hoverSpeed = MathHelper.Lerp(8.5f, 17f, 1f - lifeRatio);

                    if (attackTimer < hoverTime)
                    {
                        Vector2 destination = target.Center - Vector2.UnitY * 185f;
                        npc.SimpleFlyMovement(npc.DirectionTo(destination) * hoverSpeed, hoverAcceleration);
                        npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(target.Center) - MathHelper.PiOver2, 0.2f);
                    }
                    else if (attackTimer == hoverTime)
                    {
                        npc.velocity = npc.DirectionTo(target.Center) * chargeSpeed;
                        npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;

                        // Normal boss roar.
                        Main.PlaySound(SoundID.Roar, (int)npc.Center.X, (int)npc.Center.Y, 0, 1f, 0f);
                    }
                    else if (attackTimer >= hoverTime + chargeTime)
                        goToNextAIState();

                    break;
                case EoCAttackType.ChargingServants:
                    int servantSummonDelay = phase2 ? 35 : 50;
                    int servantsToSummon = phase2 ? 9 : 6;
                    int servantSummonTime = phase2 ? 60 : 120;
                    int servantSpawnRate = servantSummonTime / servantsToSummon;

                    hoverAcceleration = MathHelper.Lerp(0.15f, 0.35f, 1f - lifeRatio);
                    hoverSpeed = MathHelper.Lerp(14f, 18f, 1f - lifeRatio);
                    if (attackTimer < servantSummonDelay)
                    {
                        Vector2 destination = target.Center - Vector2.UnitY * 275f;
                        npc.SimpleFlyMovement(npc.DirectionTo(destination) * hoverSpeed, hoverAcceleration);
                    }
                    else if ((attackTimer - servantSummonDelay) % servantSpawnRate == servantSpawnRate - 1)
                    {
                        Vector2 spawnPosition = npc.Center + Main.rand.NextVector2CircularEdge(120f, 120f);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
						{
                            int eye = NPC.NewNPC((int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<ExplodingServant>());
                            Main.npc[eye].target = npc.target;
                            Main.npc[eye].velocity = Main.npc[eye].DirectionTo(target.Center) * 4.5f;
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

                    npc.velocity *= 0.95f;
                    npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(target.Center) - MathHelper.PiOver2, 0.2f);

                    if (attackTimer >= servantSummonDelay + servantSummonTime)
                        goToNextAIState();
                    break;

                case EoCAttackType.HorizontalBloodCharge:
                    int toothReleaseRate = phase2 ? 7 : 12;
                    ref float subState = ref npc.Infernum().ExtraAI[0];
                    ref float chargeDirection = ref npc.Infernum().ExtraAI[1];

                    if (chargeDirection == 0f)
                        chargeDirection = Main.rand.NextBool(2).ToDirectionInt();

                    // Redirect.
                    if (subState == 0f)
                    {
                        Vector2 destination = target.Center + new Vector2(-chargeDirection * 1100f, -300f);
                        npc.velocity = npc.DirectionTo(destination) * 27f;
                        npc.rotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;
                        if (npc.DistanceSQ(destination) < 32f * 32f)
                        {
                            subState = 1f;
                            npc.velocity = npc.DirectionTo(target.Center - Vector2.UnitY * 300f) * 17f;
                            npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
                            npc.netUpdate = true;
                            attackTimer = 0f;

                            // High pitched boss roar.
                            Main.PlaySound(SoundID.ForceRoar, (int)npc.Center.X, (int)npc.Center.Y, -1, 1f, 0f);
                        }
                    }
                    
                    // And shoot blood spit/balls.
                    if (subState == 1f)
                    {
                        bool closeToPlayer = Math.Abs(npc.Center.X - target.Center.X) <= 300f + target.velocity.X * 6f;
                        if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % toothReleaseRate == toothReleaseRate - 1 && !closeToPlayer)
                        {
                            Vector2 spawnPosition = npc.Center + npc.velocity.SafeNormalize(Vector2.Zero) * 72f;
                            Vector2 shootVelocity = npc.velocity;
                            shootVelocity.X *= Main.rand.NextFloat(0.35f, 0.65f);

                            Utilities.NewProjectileBetter(spawnPosition, shootVelocity, ModContent.ProjectileType<SittingBlood>(), 45, 0f);
                        }

                        if (attackTimer >= 90f || Math.Abs(npc.Center.X - target.Center.X) > 1200f)
                            goToNextAIState();
                    }

                    break;

                case EoCAttackType.TeethSpit:
                    int teethPerShot = phase3 ? 7 : 4;
                    int teethBurstTotal = phase3 ? 6 : 4;
                    float teethRadialSpread = phase3 ? 1.3f : 0.96f;
                    subState = ref npc.Infernum().ExtraAI[0];
                    ref float teethBurstCounter = ref npc.Infernum().ExtraAI[1];
                    ref float teethBurstDelay = ref npc.Infernum().ExtraAI[2];

                    // Redirect.
                    if (subState == 0f)
                    {
                        Vector2 destination = target.Center - Vector2.UnitY * 265f;
                        npc.velocity = npc.DirectionTo(destination) * 21f;
                        npc.rotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;
                        if (npc.DistanceSQ(destination) < 32f * 32f)
                        {
                            subState = 1f;
                            npc.netUpdate = true;

                            // High pitched boss roar.
                            Main.PlaySound(SoundID.ForceRoar, (int)npc.Center.X, (int)npc.Center.Y, -1, 1f, 0f);
                        }
                        npc.damage = 0;
                    }
                    
                    // Release teeth into the sky.
                    if (subState == 1f)
                    {
                        float idealAngle = MathHelper.Lerp(-teethRadialSpread, teethRadialSpread, teethBurstCounter / teethBurstTotal);
                        npc.rotation = npc.rotation.AngleTowards(idealAngle - MathHelper.Pi, 0.04f);
                        npc.velocity *= 0.945f;

                        if (teethBurstDelay <= 0f && Math.Abs(MathHelper.WrapAngle(idealAngle - npc.rotation - MathHelper.Pi)) < 0.07f)
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Vector2 spawnPosition = npc.Center - Vector2.UnitY * 14f;
                                for (int i = 0; i < teethPerShot; i++)
                                {
                                    float offsetAngle = MathHelper.Lerp(-0.56f, 0.56f, i / (float)teethPerShot) + Main.rand.NextFloat(-0.07f, 0.07f);
                                    Utilities.NewProjectileBetter(spawnPosition, -Vector2.UnitY.RotatedBy(offsetAngle) * 16f, ModContent.ProjectileType<EoCTooth>(), 53, 0f, 255, npc.target);
                                }
                            }
                            teethBurstDelay = 10f;
                            teethBurstCounter++;
                            npc.netUpdate = true;
                        }
                        if (teethBurstDelay > 0f)
                            teethBurstDelay--;
                    }
                    if (teethBurstCounter >= teethBurstTotal)
                        goToNextAIState();
                    break;
                case EoCAttackType.SpinDash:
                    int spinCycles = 2;
                    int spinTime = 120;
                    int chargeDelay = 35;
                    chargeTime = 60;
                    chargeSpeed = 14f;
                    float chargeAcceleration = 1.006f;
                    float spinRadius = 345f;

                    subState = ref npc.Infernum().ExtraAI[0];
                    ref float spinAngle = ref npc.Infernum().ExtraAI[1];
                    ref float redirectSpeed = ref npc.Infernum().ExtraAI[2];

                    // Redirect.
                    if (subState == 0f)
					{
                        if (spinAngle == 0f)
                        {
                            spinAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                            redirectSpeed = 13f;
                        }

                        if (redirectSpeed < 30f)
                            redirectSpeed *= 1.015f;

                        Vector2 destination = target.Center + spinAngle.ToRotationVector2() * spinRadius;
                        npc.velocity = (npc.velocity * 3f + npc.DirectionTo(destination) * redirectSpeed) / 4f;
                        npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;

                        if (npc.Distance(destination) < redirectSpeed + 8f)
                        {
                            attackTimer = 0f;
                            subState = 1f;
                            npc.velocity = Vector2.Zero;
                            npc.Center = target.Center + spinAngle.ToRotationVector2() * spinRadius;
                            npc.netUpdate = true;

                            // High pitched boss roar.
                            Main.PlaySound(SoundID.ForceRoar, (int)npc.Center.X, (int)npc.Center.Y, -1, 1f, 0f);
                        }
                    }

                    // Spin.
                    if (subState == 1f)
					{
                        spinAngle += MathHelper.TwoPi * spinCycles / spinTime;
                        npc.Center = target.Center + spinAngle.ToRotationVector2() * spinRadius;
                        npc.rotation = spinAngle;
                        if (attackTimer >= spinTime)
						{
                            attackTimer = 0f;
                            npc.velocity = (spinAngle + MathHelper.PiOver2).ToRotationVector2() * 10f;
                            subState = 2f;
                            npc.netUpdate = true;
                        }
                    }

                    // Slow down and aim.
                    if (subState == 2f)
                    {
                        float idealAngle = npc.AngleTo(target.Center);
                        npc.rotation = npc.rotation.AngleTowards(idealAngle - MathHelper.PiOver2, 0.1f);
                        npc.velocity *= 0.985f;
                        if (attackTimer >= chargeDelay)
                        {
                            attackTimer = 0f;
                            npc.velocity = npc.DirectionTo(target.Center) * chargeSpeed;
                            subState = 3f;
                            npc.netUpdate = true;

                            // Normal boss roar.
                            Main.PlaySound(SoundID.Roar, (int)npc.Center.X, (int)npc.Center.Y, 0, 1f, 0f);
                        }
                    }

                    // Accelerate while charging.
                    if (subState == 3f)
                    {
                        npc.velocity *= chargeAcceleration;
                        if (attackTimer >= chargeTime)
                            goToNextAIState();
                    }

                    break;
            }

            attackTimer++;
            return false;
		}
        #endregion

        #region Drawing

        internal const int GleamTime = 45;

        [OverrideAppliesTo(NPCID.EyeofCthulhu, typeof(EyeOfCthulhuAIClass), "EyeOfCthulhuPreDraw", EntityOverrideContext.NPCPreDraw)]
        public static bool EyeOfCthulhuPreDraw(NPC npc, SpriteBatch spriteBatch, Color drawColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            Texture2D eyeTexture = Main.npcTexture[npc.type];
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 eyeOrigin = eyeTexture.Size() / new Vector2(1f, Main.npcFrameCount[npc.type]) * 0.5f;
            spriteBatch.Draw(eyeTexture, drawPosition, npc.frame, npc.GetAlpha(drawColor), npc.rotation, eyeOrigin, npc.scale, spriteEffects, 0f);

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