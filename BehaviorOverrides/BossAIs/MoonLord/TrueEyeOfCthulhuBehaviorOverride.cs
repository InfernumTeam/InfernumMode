using CalamityMod;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class TrueEyeOfCthulhuBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.MoonLordFreeEye;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public override bool PreAI(NPC npc)
        {
            // Disappear if the body is not present.
            if (!Main.npc.IndexInRange((int)npc.ai[3]) || !Main.npc[(int)npc.ai[3]].active)
            {
                npc.active = false;
                return false;
            }

            // Define the core NPC.
            NPC core = Main.npc[(int)npc.ai[3]];

            npc.target = core.target;
            npc.damage = 0;
            npc.defDamage = 220;

            Player target = Main.player[npc.target];
            float attackTimer = core.ai[1];
            ref float groupIndex = ref npc.ai[0];
            ref float pupilRotation = ref npc.localAI[0];
            ref float pupilOutwardness = ref npc.localAI[1];
            ref float pupilScale = ref npc.localAI[2];

            // Define an initial group index.
            if (groupIndex == 0f)
            {
                groupIndex = NPC.CountNPCS(npc.type);
                npc.netUpdate = true;
            }

            switch ((MoonLordCoreBehaviorOverride.MoonLordAttackState)(int)core.ai[0])
            {
                case MoonLordCoreBehaviorOverride.MoonLordAttackState.PhantasmalRush:
                    DoBehavior_PhantasmalRush(npc, target, attackTimer, groupIndex, ref pupilRotation, ref pupilOutwardness, ref pupilScale);
                    break;
                case MoonLordCoreBehaviorOverride.MoonLordAttackState.PhantasmalDance:
                    DoBehavior_PhantasmalDance(npc, target, attackTimer, groupIndex, ref pupilRotation, ref pupilOutwardness, ref pupilScale);
                    break;
                default:
                    DoBehavior_IdleObserve(npc, target, groupIndex, ref pupilRotation, ref pupilOutwardness, ref pupilScale);
                    break;
            }

            return false;
        }
        public static void DoBehavior_PhantasmalRush(NPC npc, Player target, float attackTimer, float groupIndex, ref float pupilRotation, ref float pupilOutwardness, ref float pupilScale)
        {
            int fireDelay = 120;
            int chargeRate = 15;
            int laserLifetime = PressurePhantasmalDeathray.LifetimeConstant;
            float pressureLaserStartingAngularOffset = 0.63f;
            float pressureLaserEndingAngularOffset = 0.14f;
            float chargeVerticalOffset = 330f;
            float chargeSpeed = chargeVerticalOffset / chargeRate * 2f;
            float boltShootSpeed = 2f;
            ref float telegraphAngularOffset = ref npc.Infernum().ExtraAI[0];
            ref float lineTelegraphInterpolant = ref npc.Infernum().ExtraAI[1];

            lineTelegraphInterpolant = 0f;

            // The left eye shoots pressure lasers while the right eye does zigzag charges.
            if (groupIndex == 1f)
            {
                Vector2 hoverDestination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * 600f;
                if (attackTimer < fireDelay)
                {
                    float movementSpeedInterpolant = 1f - Utils.InverseLerp(fireDelay * 0.6f, fireDelay * 0.85f, attackTimer, true);
                    npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(target.Center), 0.25f);
                    npc.Center = npc.Center.MoveTowards(hoverDestination, movementSpeedInterpolant * 3f);
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * movementSpeedInterpolant * 21f, 0.85f);

                    pupilRotation = npc.rotation;
                    pupilOutwardness = MathHelper.Lerp(pupilOutwardness, 0.45f, 0.15f);
                    pupilScale = MathHelper.Lerp(pupilScale, 0.7f, 0.15f);
                    lineTelegraphInterpolant = attackTimer / fireDelay;
                    telegraphAngularOffset = MathHelper.Lerp(1.5f, 1f, lineTelegraphInterpolant) * pressureLaserStartingAngularOffset;
                }
                else
                {
                    npc.velocity = Vector2.Zero;
                    lineTelegraphInterpolant = 1f;
                    telegraphAngularOffset = -1000f;
                }

                // Release lasers.
                if (attackTimer == fireDelay)
                {
                    Main.PlaySound(SoundID.Zombie, target.Center, 104);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = -1; i <= 1; i += 2)
                        {
                            float angularVelocity = (pressureLaserEndingAngularOffset - pressureLaserStartingAngularOffset) / laserLifetime * i * 0.5f;
                            Vector2 laserDirection = npc.SafeDirectionTo(target.Center).RotatedBy(pressureLaserStartingAngularOffset * i);
                            int telegraph = Utilities.NewProjectileBetter(npc.Center, laserDirection, ModContent.ProjectileType<PressurePhantasmalDeathray>(), 300, 0f);
                            if (Main.projectile.IndexInRange(telegraph))
                            {
                                Main.projectile[telegraph].ai[1] = npc.whoAmI;
                                Main.projectile[telegraph].ModProjectile<PressurePhantasmalDeathray>().AngularVelocity = angularVelocity;
                            }
                        }
                    }
                }
            }
            else if (groupIndex == 2f)
            {
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 600f, chargeVerticalOffset);
                if (attackTimer < fireDelay)
                {
                    float movementSpeedInterpolant = 1f - Utils.InverseLerp(fireDelay * 0.6f, fireDelay * 0.85f, attackTimer, true);
                    npc.Center = npc.Center.MoveTowards(hoverDestination, movementSpeedInterpolant * 3f);
                    npc.rotation = npc.spriteDirection == 1 ? MathHelper.Pi : 0f;
                    npc.velocity = npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(npc.Distance(hoverDestination), 25f);

                    pupilRotation = npc.rotation;
                    pupilOutwardness = MathHelper.Lerp(pupilOutwardness, 0.45f, 0.15f);
                    pupilScale = MathHelper.Lerp(pupilScale, 0.75f, 0.15f);
                }
                else
                {
                    pupilOutwardness = MathHelper.Lerp(pupilOutwardness, 0f, 0.25f);
                    npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                    if (npc.spriteDirection == 1)
                        npc.rotation += MathHelper.Pi;

                    // Prepare the charges.
                    if ((attackTimer - fireDelay) % chargeRate == 0f)
                    {
                        if (attackTimer == fireDelay)
                            npc.velocity = new Vector2(Math.Sign(target.Center.X - npc.Center.X), -3.4f).SafeNormalize(Vector2.UnitY) * chargeSpeed;
                        else
                            npc.velocity = Vector2.Reflect(npc.velocity, Vector2.UnitY) * new Vector2(1f, 0.85f);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            float circularSpreadOffsetAngle = Main.rand.NextBool() ? MathHelper.Pi / 6f : 0f;
                            for (int i = 0; i < 6; i++)
                            {
                                Vector2 boltShootVelocity = (MathHelper.TwoPi * i / 6f + circularSpreadOffsetAngle).ToRotationVector2() * boltShootSpeed;
                                Utilities.NewProjectileBetter(npc.Center, boltShootVelocity, ProjectileID.PhantasmalBolt, 200, 0f);
                            }
                        }

                        npc.netUpdate = true;
                    }
                }
            }
            else
                DoBehavior_IdleObserve(npc, target, groupIndex, ref pupilRotation, ref pupilOutwardness, ref pupilScale);

            npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
            if (attackTimer >= fireDelay + laserLifetime)
                Main.npc[(int)npc.ai[3]].Infernum().ExtraAI[5] = 1f;
        }

        public static void DoBehavior_PhantasmalDance(NPC npc, Player target, float attackTimer, float groupIndex, ref float pupilRotation, ref float pupilOutwardness, ref float pupilScale)
        {
            int spinTime = 60;
            int chargeTelegraphTime = 28;
            int chargeTime = 36;
            int chargeCounter = (int)(attackTimer / (spinTime + chargeTelegraphTime + chargeTime));
            int chargeCount = 4;
            float wrappedAttackTimer = attackTimer % (spinTime + chargeTelegraphTime + chargeTime);
            float spinOffset = 400f;
            float chargeSpeed = 37f;
            float chargePredictiveness = 20f;
            float spinDirection = (chargeCounter % 2f == 0f).ToDirectionInt();
            ref float telegraphInterpolant = ref npc.Infernum().ExtraAI[0];
            ref float telegraphDirection = ref npc.Infernum().ExtraAI[1];

            // Snap into place for the spin.
            if (wrappedAttackTimer < spinTime)
            {
                float angularOffest = MathHelper.TwoPi * (groupIndex - 1f) / NPC.CountNPCS(npc.type);
                float spinArc = MathHelper.Pi * spinDirection;
                float hoverSlowdown = Utils.InverseLerp(1f, 0.8f, wrappedAttackTimer / spinTime, true);
                Vector2 idealPosition = target.Center + (spinArc * wrappedAttackTimer / spinTime + angularOffest).ToRotationVector2() * spinOffset;
                Vector2 aheadPosition = target.Center + (spinArc * (wrappedAttackTimer + 1f) / spinTime + angularOffest).ToRotationVector2() * spinOffset;
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                npc.rotation = (aheadPosition - idealPosition).ToRotation() + MathHelper.PiOver2;
                npc.velocity = npc.SafeDirectionTo(idealPosition) * MathHelper.Min(npc.Distance(idealPosition), hoverSlowdown * 37f);
                if (npc.spriteDirection == 1)
                    npc.rotation += MathHelper.Pi;

                telegraphInterpolant = 0f;
                pupilRotation = npc.rotation - MathHelper.PiOver2;
                pupilOutwardness = MathHelper.Lerp(pupilOutwardness, 0.7f, 0.15f);
                pupilScale = MathHelper.Lerp(pupilScale, 0.4f, 0.15f);
            }

            // Stop in place and look at the target before charging.
            else if (wrappedAttackTimer < spinTime + chargeTelegraphTime)
            {
                float telegraphCompletion = Utils.InverseLerp(0f, chargeTelegraphTime, wrappedAttackTimer - spinTime, true);
                float pupilDilation = Utils.InverseLerp(0f, 0.6f, telegraphCompletion, true);
                telegraphInterpolant = Utils.InverseLerp(0f, 0.65f, telegraphCompletion, true) * Utils.InverseLerp(1f, 0.75f, telegraphCompletion, true);

                // Define the telegraph direction.
                if (telegraphCompletion < 0.9f)
                    telegraphDirection = npc.AngleTo(target.Center + target.velocity * chargePredictiveness);

                // Slow down.
                npc.velocity = (npc.velocity * 0.825f).MoveTowards(Vector2.Zero, 1.5f);
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();

                float idealRotation = telegraphDirection + MathHelper.PiOver2;
                if (npc.spriteDirection == 1)
                    idealRotation += MathHelper.Pi;
                npc.rotation = npc.rotation.AngleLerp(idealRotation, 0.15f);

                pupilRotation = telegraphDirection;
                pupilOutwardness = MathHelper.Lerp(pupilOutwardness, 0.3f, 0.15f);
                pupilScale = MathHelper.SmoothStep(0.4f, 0.95f, pupilDilation);
            }

            // Do the charge.
            else if (wrappedAttackTimer == spinTime + chargeTelegraphTime)
            {
                if (groupIndex == 1f)
                    Main.PlaySound(SoundID.DD2_WyvernDiveDown, target.Center);

                telegraphInterpolant = 0f;
                npc.velocity = telegraphDirection.ToRotationVector2() * chargeSpeed;
                if (chargeCounter == 0)
                    npc.velocity *= 1.325f;

                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                if (npc.spriteDirection == 1)
                    npc.rotation += MathHelper.Pi;

                npc.netUpdate = true;
            }

            // Do contact damage and release phantasmal orbs when charging.
            else
            {
                npc.damage = npc.defDamage;
                if (chargeCounter % 2f == 1f && wrappedAttackTimer % 5f == 4f)
                {
                    Main.PlaySound(SoundID.Item72, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 orbVelocity = npc.velocity.SafeNormalize(Vector2.UnitY) * 6f;
                        Utilities.NewProjectileBetter(npc.Center, orbVelocity, ModContent.ProjectileType<PhantasmalOrb>(), 215, 0f);
                    }
                }
            }

            if (chargeCounter >= chargeCount)
                Main.npc[(int)npc.ai[3]].Infernum().ExtraAI[5] = 1f;
        }

        public static void DoBehavior_IdleObserve(NPC npc, Player target, float groupIndex, ref float pupilRotation, ref float pupilOutwardness, ref float pupilScale)
        {
            int eyeCount = NPC.CountNPCS(NPCID.MoonLordFreeEye);
            Vector2 hoverOffset = -Vector2.UnitY * 475f;

            // Define pupil variables.
            pupilRotation = pupilRotation.AngleLerp(npc.AngleTo(target.Center), 0.15f);
            pupilOutwardness = MathHelper.Lerp(0.2f, 0.8f, Utils.InverseLerp(150f, 400f, npc.Distance(target.Center), true));
            pupilScale = MathHelper.Lerp(pupilScale, 0.5f, 0.1f);

            if (eyeCount > 1)
            {
                float hoverOffsetAngle = MathHelper.Lerp(-0.75f, 0.75f, (groupIndex - 1f) / (float)(eyeCount - 1f));
                hoverOffset = hoverOffset.RotatedBy(hoverOffsetAngle);
            }

            npc.rotation = npc.velocity.X * 0.03f;
            npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
            if (npc.spriteDirection == 1)
                npc.rotation += MathHelper.Pi;

            npc.Center = npc.Center.MoveTowards(target.Center + hoverOffset, 2f);
            npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center + hoverOffset) * 19f, 0.75f);
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.npcTexture[npc.type];
            Texture2D pupilTexture = Main.extraTexture[19];
            Vector2 baseDrawPosition = npc.Center - Main.screenPosition - (npc.rotation + MathHelper.PiOver2).ToRotationVector2() * npc.spriteDirection * 32f;
            SpriteEffects direction = npc.spriteDirection == 1 ? SpriteEffects.FlipVertically : SpriteEffects.FlipHorizontally;
            Color color = npc.GetAlpha(Color.Lerp(lightColor, Color.White, 0.3f));
            spriteBatch.Draw(texture, baseDrawPosition, npc.frame, color, npc.rotation, npc.frame.Size() * 0.5f, 1f, direction, 0f);
            Vector2 pupilOffset = npc.localAI[0].ToRotationVector2() * npc.localAI[1] * 25f - Vector2.UnitY.RotatedBy(npc.rotation) * -npc.spriteDirection * 20f;

            spriteBatch.Draw(pupilTexture, baseDrawPosition + pupilOffset, null, color, npc.rotation, pupilTexture.Size() / 2f, npc.localAI[2], SpriteEffects.None, 0f);

            // Draw line telegraphs as necessary.
            NPC core = Main.npc[(int)npc.ai[3]];
            if (core.ai[0] == (int)MoonLordCoreBehaviorOverride.MoonLordAttackState.PhantasmalRush)
            {
                float lineTelegraphInterpolant = npc.Infernum().ExtraAI[1];
                if (lineTelegraphInterpolant > 0f)
                {
                    spriteBatch.SetBlendState(BlendState.Additive);

                    Texture2D line = ModContent.GetTexture("InfernumMode/ExtraTextures/BloomLineSmall");
                    Texture2D bloomCircle = ModContent.GetTexture("CalamityMod/ExtraTextures/THanosAura");

                    Color outlineColor = Color.Lerp(Color.Turquoise, Color.White, lineTelegraphInterpolant);
                    Vector2 origin = new Vector2(line.Width / 2f, line.Height);
                    Vector2 beamScale = new Vector2(lineTelegraphInterpolant * 0.5f, 2.4f);
                    Vector2 drawPosition = baseDrawPosition + pupilOffset;

                    // Create bloom on the pupil.
                    Vector2 bloomSize = new Vector2(30f) / bloomCircle.Size() * (float)Math.Pow(lineTelegraphInterpolant, 2D);
                    spriteBatch.Draw(bloomCircle, drawPosition, null, Color.Turquoise, 0f, bloomCircle.Size() * 0.5f, bloomSize, 0, 0f);

                    if (npc.Infernum().ExtraAI[0] >= -100f)
                    {
                        for (int i = -1; i <= 1; i += 2)
                        {
                            Vector2 beamDirection = -npc.SafeDirectionTo(Main.player[npc.target].Center).RotatedBy(npc.Infernum().ExtraAI[0] * i);
                            float beamRotation = beamDirection.ToRotation() - MathHelper.PiOver2;
                            spriteBatch.Draw(line, drawPosition, null, outlineColor, beamRotation, origin, beamScale, 0, 0f);
                        }
                    }

                    spriteBatch.ResetBlendState();
                }
            }
            if (core.ai[0] == (int)MoonLordCoreBehaviorOverride.MoonLordAttackState.PhantasmalDance)
            {
                float lineTelegraphInterpolant = npc.Infernum().ExtraAI[0];
                if (lineTelegraphInterpolant > 0f)
                {
                    spriteBatch.SetBlendState(BlendState.Additive);

                    Texture2D line = ModContent.GetTexture("InfernumMode/ExtraTextures/BloomLineSmall");
                    Texture2D bloomCircle = ModContent.GetTexture("CalamityMod/ExtraTextures/THanosAura");

                    Color outlineColor = Color.Lerp(Color.Turquoise, Color.White, lineTelegraphInterpolant);
                    Vector2 origin = new Vector2(line.Width / 2f, line.Height);
                    Vector2 beamScale = new Vector2(lineTelegraphInterpolant * 1.3f, 2.4f);
                    Vector2 drawPosition = baseDrawPosition + pupilOffset;

                    // Create bloom on the pupil.
                    Vector2 bloomSize = new Vector2(30f) / bloomCircle.Size() * (float)Math.Pow(lineTelegraphInterpolant, 2D);
                    spriteBatch.Draw(bloomCircle, drawPosition, null, Color.Turquoise, 0f, bloomCircle.Size() * 0.5f, bloomSize, 0, 0f);

                    Vector2 beamDirection = -npc.Infernum().ExtraAI[1].ToRotationVector2();
                    float beamRotation = beamDirection.ToRotation() - MathHelper.PiOver2;
                    spriteBatch.Draw(line, drawPosition, null, outlineColor, beamRotation, origin, beamScale, 0, 0f);

                    spriteBatch.ResetBlendState();
                }
            }
            return false;
        }
    }
}
