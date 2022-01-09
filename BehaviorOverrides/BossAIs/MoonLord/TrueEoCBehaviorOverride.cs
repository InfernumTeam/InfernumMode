using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using static InfernumMode.Utilities;
using static InfernumMode.GlobalInstances.GlobalNPCOverrides;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class TrueEoCBehaviorOverride : NPCBehaviorOverride
    {
        public enum TrueEoCAttackType
        {
            PhantasmalCharge
        }

        public override int NPCOverrideType => NPCID.MoonLordFreeEye;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        // ai[0] = AI state. The attack all three eyes will perform together
        // ai[1] = AI counter. Can only be changed the "executive decision maker"
        // ai[2] = value used once for determining pupil rotation. Not very important
        // ai[3] = check value to prevent a second eye from popping out sometimes (weird bug, don't ask). Normally, it spawns with a 0 value,
        // but when spawned in a special way from this modified Moon Lord, it has -1. Those who do not have -1 are instantly deleted.
        // localAI[0] = see head
        // localAI[1] = see head
        // localAI[2] = see head
        // localAI[3] = see head
        // ExtraAI[0] = "group index" of the individual eye. Determines what role it plays in the synchronized attack. Only one can make executive decisions
        // ExtraAI[1] = value that varies based on the AI state and group index of the eye
        // ExtraAI[2] = copy of old velocity X value. Used in the sphere shooting AI state to change sphere X velocity
        // ExtraAI[3] = copy of old velocity Y value. Used in the sphere shooting AI state to change sphere Y velocity
        public override bool PreAI(NPC npc)
        {
            if (!NPC.AnyNPCs(NPCID.MoonLordCore))
                npc.active = false;

            // Odd bug fix.
            // If the reason behind this can be found again and better dealt with, that'd be great.
            if (npc.ai[3] != -1f)
                npc.active = false;

            // Die if the moon lord is not present.
            if (!NPC.AnyNPCs(NPCID.MoonLordCore))
            {
                npc.active = false;
                return false;
            }

            NPC core = Main.npc[NPC.FindFirstNPC(NPCID.MoonLordCore)];

            npc.target = core.target;
            Player target = Main.player[npc.target];

            bool enraged = core.Infernum().ExtraAI[0] == 0f;
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float groupIndex = ref npc.Infernum().ExtraAI[0];
            ref float pupilRotation = ref npc.localAI[0];
            ref float pupilOutwardness = ref npc.localAI[1];
            ref float pupilScale = ref npc.localAI[2];
            ref float hasInitialized = ref npc.localAI[3];
            bool executiveDecisionMaker = groupIndex == 1f;

            // If a brand new eye appears, reset all other eyes to the charge phase
            if (hasInitialized == 0f)
            {
                attackState = 0f;
                attackTimer = 0f;
                pupilRotation = 0f;

                // Use a group index which is successive to the previous one.
                if (MoonLordHandBehaviorOverride.GetTrueEyes.Length > 0)
                    groupIndex = (int)MoonLordHandBehaviorOverride.GetTrueEyes.Max(eye => eye.Infernum().ExtraAI[0]) + 1;

                EyeSyncVariables(npc);
                hasInitialized = 1f;
            }
            bool anyNonSpecialSeals = Main.npc.Any(seal => seal.type == ModContent.NPCType<EldritchSeal>() && seal.active && seal.ai[0] != 3f);
            if (anyNonSpecialSeals)
            {
                npc.Infernum().canTelegraph = false;
                npc.rotation = npc.rotation.AngleLerp(0f, 0.2f);
                npc.velocity *= 0.9f;
                return false;
            }

            // Attack Delay
            const float delay = 160f;

            // Universal Counter
            if (executiveDecisionMaker)
                attackTimer++;

            // Phantasmal Charge.
            attackState = 0f;
            if (attackTimer > 500f)
                attackTimer = 0f;
            if (attackState == 0f)
                DoBehavior_PhantasmalCharge(npc, target, enraged, groupIndex, ref attackTimer, ref pupilRotation, ref pupilOutwardness, ref pupilScale);

            // Phantasmal Barrage
            else if (attackState == 1f)
            {
            }

            // Phantasmal Storm
            else if (attackState == 2f)
            {
                Vector2 idealPosition = default;
                switch (groupIndex)
                {
                    // Spin and release a burst of phantasmal eyes
                    case 0:
                    case 1:
                        const float seekTimeSpin = 190f;
                        const float spinTime = 90f;
                        const int phantasmalEyeCount = 9;
                        const float maxVelocity = 13f;
                        // Get into a general area of the target position
                        if (attackTimer < seekTimeSpin)
                        {
                            idealPosition = target.Center + new Vector2(0f, -420f);
                            npc.SimpleFlyMovement(npc.SafeDirectionTo(idealPosition) * maxVelocity, 0.3f);
                            npc.rotation = npc.rotation.AngleLerp(0f, 0.2f);
                            pupilRotation = npc.rotation;
                            pupilScale = MathHelper.Lerp(pupilScale, 0.5f, 0.05f);
                        }
                        // Adjust velocity to unit rotation vector
                        else if (attackTimer == seekTimeSpin + 1)
                        {
                            npc.velocity = new Vector2(0f, -16f);
                            Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, Main.rand.Next(100, 101), 1f, 0f);
                        }
                        // Spin
                        else if (attackTimer < seekTimeSpin + spinTime)
                        {
                            npc.velocity = npc.velocity.RotatedBy(MathHelper.ToRadians(360f / spinTime));
                            npc.rotation = npc.rotation.AngleLerp(npc.velocity.ToRotation() + MathHelper.PiOver2, 0.2f);
                            pupilRotation = npc.rotation;
                            if (attackTimer % (int)(spinTime / phantasmalEyeCount / (enraged ? 2f : 1f)) ==
                                (int)(spinTime / phantasmalEyeCount / (enraged ? 2f : 1f)) - 1)
                            {
                                NewProjectileBetter(npc.Center - Vector2.UnitX * Main.rand.Next(-npc.width + 12, npc.width - 12),
                                    new Vector2(0f, -1f * Main.rand.NextFloat(8f, 14f)).RotatedByRandom(MathHelper.ToRadians(36f)),
                                    ProjectileID.PhantasmalEye, 190, 1f);
                            }
                        }
                        // Slow down
                        else if (attackTimer >= seekTimeSpin + spinTime)
                        {
                            npc.rotation = npc.rotation.AngleLerp(0f, 0.2f);
                            npc.velocity = Vector2.Lerp(npc.velocity, Vector2.Zero, 0.2f);
                        }
                        break;
                    // Charge and release phantasmal eyes
                    case 2:
                    case 3:
                        const float seekTimeCharge = 190f;
                        const float chargeTime = 90f;
                        const float xSeekPosition = 960f;
                        const float maxChargeVelocity = 26f;
                        const float acceleration = 0.9f;
                        const int phantasmalEyeCount2 = 15;
                        attackTimer += 1f;
                        // Get into a general area of the target position
                        if (attackTimer < seekTimeCharge + delay)
                        {
                            float signX = 1f;
                            if (Math.Abs(target.Center.X - xSeekPosition) < Math.Abs(target.Center.X + xSeekPosition))
                                signX = -1f;
                            idealPosition = target.Center + new Vector2(signX * xSeekPosition, -480f);
                            npc.SimpleFlyMovement(npc.SafeDirectionTo(idealPosition) * maxVelocity, 0.3f);
                            npc.rotation = npc.rotation.AngleLerp(0f, 0.2f);
                            pupilScale = MathHelper.Lerp(pupilScale, 0.5f, 0.05f);
                        }
                        // Charge while releasing eyes
                        else if (attackTimer < seekTimeCharge + chargeTime + delay)
                        {
                            if (attackTimer == seekTimeCharge + delay + 1f)
                                Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, Main.rand.Next(100, 101), 1f, 0f);
                            pupilScale = 0.5f;
                            float signX = 1f;
                            if (Math.Abs(target.Center.X - xSeekPosition) < Math.Abs(target.Center.X + xSeekPosition))
                                signX = -1f;
                            npc.velocity.X = MathHelper.Lerp(npc.velocity.X, -signX * maxChargeVelocity, acceleration);
                            npc.velocity.Y = 0f;
                            if (attackTimer % (int)(chargeTime / phantasmalEyeCount2) == (int)(chargeTime / phantasmalEyeCount2) - 1)
                            {
                                NewProjectileBetter(npc.Center - Vector2.UnitX * Main.rand.Next(-npc.width + 12, npc.width - 12),
                                    new Vector2(0f, -1f * Main.rand.NextFloat(8f, 14f)).RotatedByRandom(MathHelper.ToRadians(36f)),
                                    ProjectileID.PhantasmalEye, 190, 1f);
                            }
                            npc.rotation = npc.velocity.X / 16.55211f;
                        }
                        // Slow down
                        else if (attackTimer >= seekTimeCharge + chargeTime + delay)
                        {
                            npc.rotation = npc.rotation.AngleLerp(0f, 0.2f);
                            npc.velocity = Vector2.Lerp(npc.velocity, Vector2.Zero, 0.2f);
                        }
                        break;
                    // Spew out phantasmal blasts
                    default:
                        attackTimer += 1f;
                        const float floatTime = 140f;
                        const float aimTime = 45f;
                        const float blastTime = 60f;
                        const int blastCount = 12;
                        // Get into a general area of the target position
                        if (attackTimer < floatTime + 2f * delay)
                        {
                            float signX = 1f;
                            if (Math.Abs(target.Center.X - xSeekPosition) < Math.Abs(target.Center.X + xSeekPosition))
                                signX = -1f;
                            signX *= -1f;
                            idealPosition = target.Center + new Vector2(signX * xSeekPosition, -480f);
                            npc.SimpleFlyMovement(npc.SafeDirectionTo(idealPosition) * maxVelocity, 0.3f);
                            npc.rotation = pupilRotation = npc.velocity.X / 14f;
                            pupilScale = MathHelper.Lerp(pupilScale, 0.5f, 0.05f);
                        }
                        // Choose a target and telegraph
                        else if (attackTimer < floatTime + blastTime + 2f * delay)
                        {
                            npc.rotation = npc.rotation.AngleLerp(0f, 0.2f);
                            npc.velocity = Vector2.Lerp(npc.velocity, Vector2.Zero, 0.2f);
                            npc.Infernum().angleTarget = target.Center;
                            npc.Infernum().canTelegraph = true;
                        }
                        // Stop telegraphing and shoot
                        else if (attackTimer < floatTime + blastTime + aimTime + 2f * delay)
                        {
                            if (npc.Infernum().canTelegraph)
                            {
                                npc.Infernum().canTelegraph = false;
                                Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, Main.rand.Next(100, 101), 1f, 0f);
                            }
                            if (attackTimer % (int)(blastTime / blastCount / (enraged ? 2f : 1f)) ==
                                (int)(blastTime / blastCount / (enraged ? 2f : 1f)) - 1)
                            {
                                pupilRotation = npc.AngleTo(npc.Infernum().angleTarget);
                                NewProjectileBetter(npc.Center, npc.SafeDirectionTo(npc.Infernum().angleTarget) * 15f, ModContent.ProjectileType<PhantasmalBlast>(), 200, 2.6f);
                            }
                        }
                        break;
                }
                // Go to next AI state
                if (attackTimer >= 280f + 2f * delay)
                {
                    MLSealTeleport = true;
                    attackState = core.life / (float)core.lifeMax > 0.4f ? 0f : 3f;
                    attackTimer =
                        npc.Infernum().ExtraAI[1] =
                        npc.Infernum().ExtraAI[2] =
                        npc.Infernum().ExtraAI[3] = 0f;
                    EyeSyncVariables(npc);
                }
            }

            // Phantasmal Assault
            else if (attackState == 3f)
            {
                Vector2 idealPosition = default;
                const float maxVelocity = 36f;
                const float seekTimeCharge = 80f;
                const float chargeTime = 300f;
                switch (groupIndex)
                {
                    // "Pressure" laser
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                        const float xSeekPosition = 1200f;
                        float xVelocity = (core.Infernum().arenaRectangle.Width - 50) / chargeTime * 0.45f;
                        float signX = (groupIndex == 3).ToDirectionInt();
                        if (groupIndex == 3)
                            attackTimer += 1f;
                        // Move to appropriate position
                        if (attackTimer < seekTimeCharge)
                        {
                            idealPosition = target.Center + new Vector2(xSeekPosition * signX, core.Infernum().arenaRectangle.Y - 50f);
                            idealPosition.X = core.Infernum().arenaRectangle.X + core.Infernum().arenaRectangle.Width * (groupIndex == 3).ToInt();
                            idealPosition.Y = core.Infernum().arenaRectangle.Y + 16f;
                            npc.SimpleFlyMovement(npc.SafeDirectionTo(idealPosition) * maxVelocity, 1.5f);
                            pupilScale = MathHelper.Lerp(pupilScale, 0.5f, 0.05f);
                            npc.rotation = 0f;
                        }
                        // Release laser and go twoards the center
                        else if (attackTimer < seekTimeCharge + chargeTime)
                        {
                            if (attackTimer == seekTimeCharge + chargeTime + 1f)
                                Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, Main.rand.Next(100, 101), 1f, 0f);
                            pupilScale = 0.5f;
                            npc.velocity = -signX * xVelocity * Vector2.UnitX;
                            if (attackTimer == seekTimeCharge + 2)
                            {
                                Vector2 fromEyeCoordinates = Utils.Vector2FromElipse(pupilRotation.ToRotationVector2(), new Vector2(27f, 59f) * pupilOutwardness);
                                int idx = NewProjectileBetter(npc.Center + fromEyeCoordinates, Vector2.UnitY, ModContent.ProjectileType<MoonlordPendulum>(), 425, 0f, 255, 0f, npc.whoAmI);
                                Main.projectile[idx].ai[0] = 0f;
                                Main.projectile[idx].ai[1] = npc.whoAmI;
                            }
                            npc.rotation = npc.rotation.AngleTowards(-MathHelper.Pi, 0.1f);
                            pupilRotation = npc.rotation - MathHelper.PiOver2;
                        }
                        break;
                    // Bolts
                    default:
                        attackTimer += 1f;
                        idealPosition = target.Center + new Vector2(Main.rand.NextFloat(-120f, 120f), -580f);
                        // Adjust rotation and size of pupil
                        if (attackTimer < seekTimeCharge)
                        {
                            pupilScale = MathHelper.Lerp(pupilScale, 0.5f, 0.05f);
                            npc.rotation = 0f;
                        }
                        // Fly above player and release bolts
                        if (attackTimer < seekTimeCharge + chargeTime)
                        {
                            // Circular spread
                            if (attackTimer % 80f == 45f && attackTimer <= 240f)
                            {
                                int boltCount = 10;
                                if (enraged)
                                {
                                    boltCount += Main.rand.Next(6, 12);
                                }
                                for (int i = 0; i < 10; i++)
                                {
                                    float angle = MathHelper.TwoPi / boltCount * i;
                                    float velocityMultiplier = 2f;
                                    // Cause the bolt aimed at the player to go much faster than the other bolts
                                    if (i % 2 == 0)
                                    {
                                        velocityMultiplier = 3f;
                                    }
                                    Vector2 velocity = angle.ToRotationVector2() * velocityMultiplier;
                                    velocity = velocity.RotatedByRandom(MathHelper.ToRadians(360f / 10f) / 2f);
                                    NewProjectileBetter(npc.Center, velocity, ProjectileID.PhantasmalBolt, 185, 1f);
                                }
                            }
                            // Spiral
                            if (attackTimer >= 240f)
                            {
                                if (attackTimer % 3f == 2f)
                                {
                                    npc.Infernum().ExtraAI[1] += MathHelper.ToRadians(6f);
                                    pupilRotation = npc.Infernum().ExtraAI[1];
                                    NewProjectileBetter(npc.Center, npc.Infernum().ExtraAI[1].ToRotationVector2() * 1.5f * (enraged ? 3f : 1f), ProjectileID.PhantasmalBolt, 185, 1f);
                                }
                            }
                        }
                        npc.SimpleFlyMovement(npc.SafeDirectionTo(idealPosition) * maxVelocity, 0.5f);
                        break;
                }
                // Go to next AI state
                if (attackTimer >= chargeTime + seekTimeCharge)
                {
                    MLSealTeleport = true;
                    attackState = 0f;
                    attackTimer =
                        npc.Infernum().ExtraAI[1] =
                        npc.Infernum().ExtraAI[2] =
                        npc.Infernum().ExtraAI[3] = 0f;
                    EyeSyncVariables(npc);
                }
            }
            return false;
        }

        public static void DoBehavior_PhantasmalCharge(NPC npc, Player target, bool enraged, float groupIndex, ref float attackTimer, ref float pupilRotation, ref float pupilOutwardness, ref float pupilScale)
        {
            int attackDelay = 180;
            int sphereCount = 6;
            int sphereCreationTime = 42;
            int sphereCreationRate = sphereCreationTime / sphereCount;
            float sphereOffsetRadius = 60f;
            int lungeTime = 30;
            ref float initialChargeDirection = ref npc.Infernum().ExtraAI[2];

            MoonLordCoreBehaviorOverride.AffectAllEyes(eye => eye.ai[0] = 0f);

            Vector2 hoverDestination = default;
            switch (NPC.CountNPCS(NPCID.MoonLordFreeEye))
            {
                case 1:
                    hoverDestination = new Vector2(0f, -360f);
                    break;
                case 2:
                    switch (groupIndex)
                    {
                        case 1:
                            hoverDestination = new Vector2(-620f, -360f);
                            break;
                        default:
                            hoverDestination = new Vector2(620f, -360f);
                            break;
                    }
                    break;
                case 3:
                    switch (groupIndex)
                    {
                        case 1:
                            hoverDestination = new Vector2(-620f, -360f);
                            break;
                        default:
                            hoverDestination = new Vector2(0f, -410f);
                            break;
                        case 2:
                        case 3:
                            hoverDestination = new Vector2(620f, -360f);
                            break;
                    }
                    break;
            }

            // Attempt to fly above the player.
            if (attackTimer < attackDelay)
            {
                npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center + hoverDestination) * 13f, 0.3f);
                npc.rotation = npc.velocity.X / 10f;

                pupilRotation = npc.AngleTo(target.Center);
                pupilOutwardness = npc.velocity.X / 15f;

                // Make the pipil slowly dilate.
                float idealPupilScale = MathHelper.Lerp(0.35f, 0.75f, Utils.InverseLerp(attackDelay * 0.5f, attackDelay, attackTimer, true));
                pupilScale = MathHelper.Lerp(pupilScale, idealPupilScale, 0.15f);

                // And look at the target.
            }

            // Create a circle of spheres.
            else if (attackTimer < attackDelay + sphereCreationTime)
            {
                npc.velocity *= 0.5f;
                npc.rotation = npc.rotation.AngleLerp(0f, 0.2f);

                // Make the pupil rapidly dilate.
                pupilScale = MathHelper.Lerp(pupilScale, 0.9f, 0.2f);

                // Create spheres.
                float adjustedAttackTimer = attackTimer - attackDelay;
                if (adjustedAttackTimer % (sphereCreationTime / sphereCount) == 0f)
                {
                    float sphereOffsetAngle = Utils.InverseLerp(0f, sphereCreationTime, adjustedAttackTimer, true) * MathHelper.TwoPi;
                    Vector2 sphereOffset = sphereOffsetAngle.ToRotationVector2() * sphereOffsetRadius;

                    // And spawn it. The movement of the spheres are changed later in the code
                    int sphere = NewProjectileBetter(npc.Center + sphereOffset, Vector2.Zero, ProjectileID.PhantasmalSphere, 225, 0f, Main.myPlayer, 30f, npc.whoAmI);
                    if (Main.projectile.IndexInRange(sphere))
                    {
                        Main.projectile[sphere].ai[1] = npc.whoAmI;
                        Main.projectile[sphere].timeLeft = 195;
                    }
                }

                // Adjust the pupil rotation so that it looks at the player.
                pupilRotation = npc.AngleTo(target.Center);
                pupilOutwardness = MathHelper.Lerp(pupilOutwardness, 0.7f, 0.1f);
            }

            // Look at the player.
            else if (attackTimer < attackDelay + sphereCreationTime + lungeTime)
            {
                npc.Infernum().ExtraAI[1] = 0f;
                npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(target.Center) + MathHelper.PiOver2, 0.3f);

                // Have the pupil shrink, look in the direction of the eye, and approach an intermediate outwardness.
                pupilScale = MathHelper.Lerp(pupilScale, 0.35f, 0.15f);
                pupilOutwardness = MathHelper.Lerp(pupilOutwardness, 0.5f, 0.1f);
                pupilRotation = npc.rotation;
            }

            // Charge.
            else if (attackTimer == attackDelay + sphereCreationTime + lungeTime)
            {
                Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, Main.rand.Next(100, 101), 1f, 0f);
                npc.velocity = npc.SafeDirectionTo(target.Center) * 24f;
                initialChargeDirection = npc.velocity.ToRotation();

                // Make all sphere move forward after charging.
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].active && Main.projectile[i].type == ProjectileID.PhantasmalSphere && Main.projectile[i].ai[1] == npc.whoAmI && Main.projectile[i].ai[0] != -1f)
                    {
                        Main.projectile[i].velocity = initialChargeDirection.ToRotationVector2() * npc.velocity.Length() * (enraged ? 2f : 0.75f);
                        Main.projectile[i].netUpdate = true;
                    }
                }
            }

            // Slow down after charging.
            else if (attackTimer < attackDelay + sphereCreationTime + 2f * lungeTime)
            {
                pupilRotation = 0f;
                pupilOutwardness = 0.5f;
                if (pupilScale > 0.3f)
                    pupilScale -= 0.025f;
            }

            // Go to next attack
            else if (attackTimer == attackDelay + sphereCreationTime + 2f * lungeTime)
                SelectNextAttack(npc);

            // Control the other eyes if the caller is the executive decision maker.
            if (groupIndex == 1f)
                EyeSyncVariables(npc);
        }

        public static void DoBehavior_PhantasmalBarrage(NPC npc, Player target, bool enraged, float groupIndex, ref float attackTimer, ref float pupilRotation, ref float pupilOutwardness, ref float pupilScale)
        {
            int timeSpentPerAttack = 360;
            int staticTelegraphTime = 20;

            Vector2 hoverDestination;

            EyeSyncVariables(npc);

            switch (groupIndex)
            {
                // Release a circle of bolts. The bolt aimed at the player is faster than the others.
                case 1:
                    int seekTimeCircular = 190;
                    int boltSpreadTelegraphTime = 55;
                    hoverDestination = target.Center + new Vector2(MathHelper.Clamp(NPC.CountNPCS(NPCID.MoonLordFreeEye) - 2f, 0f, 1f) * -620f, -360);

                    // Hover above the target at the start of and after the bolt attack.
                    if (attackTimer < seekTimeCircular || attackTimer > seekTimeCircular + boltSpreadTelegraphTime)
                    {
                        npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 15f, 0.3f);
                        npc.rotation = npc.velocity.X / 14f;

                        pupilRotation = pupilRotation.AngleTowards(npc.AngleTo(target.Center), 0.3f);
                        pupilScale = MathHelper.Lerp(pupilScale, 0.4f, 0.1f);
                    }

                    // Look at the target and slow down, creating a line telegraph.
                    else if (attackTimer < seekTimeCircular + boltSpreadTelegraphTime)
                    {
                        pupilScale = MathHelper.Lerp(pupilScale, 0.6f, 0.05f);
                        npc.rotation = npc.rotation.AngleLerp(0f, 0.2f);
                        npc.velocity *= 0.9f;
                        if (attackTimer < seekTimeCircular + boltSpreadTelegraphTime - staticTelegraphTime)
                        {
                            npc.Infernum().angleTarget = target.Center;
                            pupilRotation = npc.Infernum().ExtraAI[1] = npc.AngleTo(npc.Infernum().angleTarget);
                        }
                        npc.Infernum().canTelegraph = true;
                    }

                    // Release the circle of bolts.
                    else if (attackTimer == seekTimeCircular + boltSpreadTelegraphTime)
                    {
                        npc.Infernum().canTelegraph = false;

                        int boltCount = enraged ? 32 : 20;
                        for (int i = 0; i < boltCount; i++)
                        {
                            float angle = MathHelper.TwoPi / boltCount * i + npc.Infernum().ExtraAI[1];
                            float boltSpeed = enraged ? 7f : 3f;

                            // Make the bolt aimed at the player to go much faster than the other bolts
                            if (Math.Abs(angle - npc.Infernum().ExtraAI[1]) < 0.04f)
                                boltSpeed = 9f;

                            NewProjectileBetter(npc.Center, angle.ToRotationVector2() * boltSpeed, ProjectileID.PhantasmalBolt, 185, 1f);
                        }
                    }
                    break;

                // Aim and release phantasmal blasts.
                case 2:
                case 3:
                    hoverDestination = target.Center + new Vector2(0f, 360);
                    float seekTimeBlaster = 130;
                    float aimTime = 30;

                    // Hover above the target at the start of and after the blast attack.
                    if (attackTimer < seekTimeBlaster + timeSpentPerAttack || attackTimer >= seekTimeBlaster + 2f * aimTime + timeSpentPerAttack)
                    {
                        npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 15f, 0.3f);
                        npc.rotation = pupilRotation = npc.velocity.X / 14f;
                        pupilScale = 0.4f;
                    }

                    // Aim at the target and slow down, creating a line telegraph.
                    else if (attackTimer < seekTimeBlaster + aimTime + timeSpentPerAttack)
                    {
                        pupilRotation = npc.AngleTo(target.Center);
                        pupilScale = MathHelper.Lerp(pupilScale, 0.6f, 0.05f);
                        npc.rotation = npc.rotation.AngleLerp(pupilRotation + MathHelper.PiOver2, 0.2f);
                        if (attackTimer < seekTimeBlaster + aimTime + timeSpentPerAttack - staticTelegraphTime)
                            npc.Infernum().angleTarget = target.Center;

                        npc.Infernum().canTelegraph = true;
                    }

                    // Release 3 phantasmal blasts, chaingun style
                    else if (attackTimer < seekTimeBlaster + aimTime * 2f + timeSpentPerAttack)
                    {
                        npc.Infernum().canTelegraph = false;

                        npc.velocity *= 0.9f;
                        if (attackTimer % 10f == 0f)
                        {
                            float blastShootSpeed = enraged ? 23f : 15f;
                            if (attackTimer == seekTimeBlaster + aimTime + timeSpentPerAttack + 10f)
                                Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, Main.rand.Next(100, 101), 1f, 0f);

                            NewProjectileBetter(npc.Center, npc.SafeDirectionTo(npc.Infernum().angleTarget) * blastShootSpeed, ModContent.ProjectileType<PhantasmalBlast>(), 200, 2.6f);
                        }
                    }
                    break;
                // Spiral of bolts
                default:
                    const float seekTimeSpiral = 170f;
                    hoverDestination = target.Center + new Vector2(620 * MathHelper.Clamp(NPC.CountNPCS(NPCID.MoonLordFreeEye) - 2, 0f, 1f), -360);
                    // Fly to the top right the player
                    if (attackTimer < seekTimeSpiral + timeSpentPerAttack * 2f)
                    {
                        const float acceleration = 0.3f;
                        const float maxVelocity = 15f;
                        npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * maxVelocity, acceleration);
                        npc.rotation = pupilRotation = npc.velocity.X / 14f;
                        pupilScale = 0.4f;
                    }
                    // Shoot a barrage of spirals
                    if (attackTimer >= seekTimeSpiral + timeSpentPerAttack * 2f)
                    {
                        npc.velocity *= 0.9f;
                        pupilRotation = npc.AngleTo(target.Center);
                        pupilScale = MathHelper.Lerp(pupilScale, 0.6f, 0.05f);
                        npc.rotation = npc.rotation.AngleLerp(pupilRotation + MathHelper.PiOver2, 0.2f);
                        const float ai1Skip = 3f;
                        if (attackTimer % ai1Skip == ai1Skip - 1)
                        {
                            npc.Infernum().ExtraAI[1] += MathHelper.ToRadians(360f / (250f - seekTimeSpiral) * ai1Skip);
                            Vector2 spawnPositon = npc.Center;
                            NewProjectileBetter(spawnPositon, npc.Infernum().ExtraAI[1].ToRotationVector2() * (enraged ? 4.25f : 3f), ProjectileID.PhantasmalBolt, 185, 1f);
                        }
                    }
                    break;
            }

            // Go to next AI state
            if (attackTimer >= 250f + 2f * timeSpentPerAttack)
                SelectNextAttack(npc);
        }

        public static void SelectNextAttack(NPC npc)
        {
            MLSealTeleport = true;

            npc.ai[0]++;

            NPC core = Main.npc[NPC.FindFirstNPC(NPCID.MoonLordCore)];
            if (npc.ai[0] >= (core.life / (float)core.lifeMax > 0.4f ? 3f : 4f))
                npc.ai[0] = 0f;

            npc.ai[1] = 0f;
            npc.Infernum().ExtraAI[1] = 0f;
            npc.Infernum().ExtraAI[2] = 0f;
            npc.Infernum().ExtraAI[3] = 0f;
            EyeSyncVariables(npc);
        }

        /// <summary>
        /// Causes all true eyes to adjust their primary AI states. AI3 is not changed.
        /// </summary>
        /// <param name="toExecute">The npc from which we wish to derive</param>
        public static void EyeSyncVariables(NPC toCopy)
        {
            foreach (var eye in MoonLordHandBehaviorOverride.GetTrueEyes)
            {
                if (eye.whoAmI == toCopy.whoAmI)
                    continue;
                for (int i = 0; i <= 2; i++)
                {
                    eye.ai[i] = toCopy.ai[i];
                }
            }
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            // Manually draw a telegraph from the True Eyes if they are ready to do so

            if (npc.Infernum().canTelegraph)
            {
                Color color = new Color(0, 160, 170);
                Vector2 fromEyeCoordinates = Utils.Vector2FromElipse(npc.localAI[0].ToRotationVector2(), new Vector2(27f, 59f) * npc.localAI[1]);

                Vector2 start = npc.Center + fromEyeCoordinates;
                Vector2 end = start + npc.AngleTo(npc.Infernum().angleTarget).ToRotationVector2() * 4500f;

                Vector2 directionToEnd = (end - start).SafeNormalize(Vector2.Zero);
                Vector2 currentDrawPosition = start;
                float rotation = directionToEnd.ToRotation();

                float telegraphLaserWidth = 4f;
                float scale = telegraphLaserWidth / 16f;
                for (float offset = 0f; offset <= (end - start).Length(); offset += telegraphLaserWidth)
                {
                    spriteBatch.Draw(Main.blackTileTexture, currentDrawPosition - Main.screenPosition, null, color, rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
                    currentDrawPosition = start + offset * directionToEnd;
                }
            }
            return true;
        }
    }
}
