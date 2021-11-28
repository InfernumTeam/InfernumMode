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
    // TODO: REFACTOR THIS SHIT HOLY FUCKING SHIT I AM LOSING MY MIND THIS IS NOT OK GODDAMN!!!!!!
    public class TrueEoCBehaviorOverride : NPCBehaviorOverride
    {
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

            if (npc.ai[3] != -1f)
            {
                npc.active = false;
            }
            if (!NPC.AnyNPCs(NPCID.MoonLordCore))
            {
                npc.active = false;
                return false;
            }
            NPC core = Main.npc[NPC.FindFirstNPC(NPCID.MoonLordCore)];
            bool enrage = core.Infernum().ExtraAI[0] == 0f;
            Player player = Main.LocalPlayer;
            int trueEyeCount = NPC.CountNPCS(NPCID.MoonLordFreeEye);
            int groupIndex = (int)npc.Infernum().ExtraAI[0];
            bool executiveDecisionMaker = groupIndex == 1;
            // If a brand new eye appears, reset all other eyes to the charge phase
            if (npc.localAI[3] == 0f)
            {
                float ai3 = npc.ai[3];
                npc.ai = new float[]
                {
                    0f, 0f, 0f, 0f
                };
                npc.ai[3] = ai3;
                groupIndex = 0;
                if (MoonLordHandBehaviorOverride.GetTrueEyes.Length > 0)
                    groupIndex = (int)MoonLordHandBehaviorOverride.GetTrueEyes.Max(eye => eye.Infernum().ExtraAI[0]) + 1;
                npc.Infernum().ExtraAI[0] = groupIndex;
                EyeSyncVariables(npc);
                npc.localAI[3] = 1f;
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
                npc.ai[1] += 1f;
            // Phantasmal Charge
            if (npc.ai[0] == 0f)
            {
                MoonLordCoreBehaviorOverride.AffectAllEyes(eye => eye.ai[0] = 0f);
                const float seekTime = 180f;
                const float sphereCreationTime = 42f;
                Vector2 idealPosition = default;
                switch (trueEyeCount)
                {
                    case 1:
                        idealPosition = new Vector2(0f, -360f);
                        break;
                    case 2:
                        switch (groupIndex)
                        {
                            case 1:
                                idealPosition = new Vector2(-620f, -360f);
                                break;
                            default:
                                idealPosition = new Vector2(620f, -360f);
                                break;
                        }
                        break;
                    case 3:
                        switch (groupIndex)
                        {
                            case 1:
                                idealPosition = new Vector2(-620f, -360f);
                                break;
                            default:
                                idealPosition = new Vector2(0f, -410f);
                                break;
                            case 2:
                            case 3:
                                idealPosition = new Vector2(620f, -360f);
                                break;
                        }
                        break;
                }
                const float lungeTime = 30f;
                // Attempt to fly above the player
                if (npc.ai[1] < seekTime)
                {
                    npc.rotation = npc.velocity.X / 10f;

                    const float acceleration = 0.3f;
                    const float maxVelocity = 13f;
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(player.Center + idealPosition) * maxVelocity, acceleration);

                    npc.localAI[0] = npc.rotation;
                    npc.localAI[1] = npc.velocity.X / 15f;
                    npc.localAI[2] = 1f;

                    npc.localAI[0] = 0f;
                }
                // Create a hexagon of spheres
                else if (npc.ai[1] <= seekTime + sphereCreationTime)
                {
                    const int sphereCount = 6;
                    const float sphereRadius = 60f;

                    npc.velocity *= 0.8f;
                    npc.rotation = npc.rotation.AngleLerp(0f, 0.2f);
                    // Create sphereCount phantasmal spheres in sphereCreationTime frames.
                    if (npc.ai[1] % (int)(sphereCreationTime / sphereCount) == (int)(sphereCreationTime / sphereCount) - 1f)
                    {
                        // Get the respective angle based on which sphere we're spawning
                        float angle = (npc.ai[1] - seekTime) / sphereCreationTime * MathHelper.TwoPi;

                        // And spawn it. The movement of the spheres are changed later in the code
                        int sphereProjectileIndex = NewProjectileBetter(npc.Center + angle.ToRotationVector2() * sphereRadius,
                            Vector2.Zero, ProjectileID.PhantasmalSphere, 225,
                            0f, Main.myPlayer, 30f, npc.whoAmI);
                        Main.projectile[sphereProjectileIndex].timeLeft = 195;
                    }
                    // Adjust the True Eye's pupil so that it looks at the player
                    npc.ai[2] = npc.AngleTo(player.Center) + MathHelper.PiOver2;
                }
                // Look at the player
                else if (npc.ai[1] < seekTime + sphereCreationTime + lungeTime)
                {
                    npc.Infernum().ExtraAI[1] = 0f;
                    npc.rotation = npc.rotation.AngleTowards(npc.ai[2], MathHelper.ToRadians(6f));
                    npc.localAI[0] = npc.rotation;
                    npc.localAI[1] = 0.5f;
                    npc.localAI[2] = 0.6f;
                }
                // Lunge
                else if (npc.ai[1] == seekTime + sphereCreationTime + lungeTime)
                {
                    if (npc.Distance(Main.LocalPlayer.Center) >= 90f || Math.Abs(npc.velocity.Length() - 12f) > 0.1f)
                    {
                        Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, Main.rand.Next(100, 101), 1f, 0f);
                        npc.velocity = npc.SafeDirectionTo(Main.LocalPlayer.Center) * 12f;
                    }
                }
                // Slow down
                else if (npc.ai[1] < seekTime + sphereCreationTime + 2f * lungeTime)
                {
                    npc.Infernum().ExtraAI[2] = npc.velocity.X;
                    npc.Infernum().ExtraAI[3] = npc.velocity.Y;
                    npc.localAI[0] = 0f;
                    npc.localAI[1] = 0.5f;
                    if (npc.localAI[2] > 0.3f)
                    {
                        npc.localAI[2] -= 0.025f;
                    }
                }
                // Go to next attack
                else if (npc.ai[1] == seekTime + sphereCreationTime + 2f * lungeTime)
                {
                    MLSealTeleport = true;
                    npc.ai[0] = 1f;
                    npc.ai[1] =
                        npc.Infernum().ExtraAI[1] =
                        npc.Infernum().ExtraAI[2] =
                        npc.Infernum().ExtraAI[3] = 0f;
                    EyeSyncVariables(npc);
                }
                // Modify static spheres
                if (npc.ai[1] >= seekTime + sphereCreationTime + lungeTime)
                {
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        if (Main.projectile[i].active && Main.projectile[i].type == 454 && Main.projectile[i].ai[1] == npc.whoAmI && Main.projectile[i].ai[0] != -1f)
                        {
                            Main.projectile[i].velocity = new Vector2(npc.Infernum().ExtraAI[2], npc.Infernum().ExtraAI[3])
                                * 1.5f * (enrage ? 2f : 1f);
                        }
                    }
                }
                if (executiveDecisionMaker)
                    EyeSyncVariables(npc);
            }

            // Phantasmal Barrage
            else if (npc.ai[0] == 1f)
            {
                // TOTAL TIME FOR ALL 3 OF THESE ATTACKS IS 250 FRAMES PLUS A DELAY
                Vector2 idealPosition = default;
                EyeSyncVariables(npc);
                switch (groupIndex)
                {
                    // Release a circle of bolts. Stay to the left of the player
                    case 1:
                        const float seekTimeCircular = 190f;
                        const float waitTime = 55f;
                        const float staticTelegraphTime = 20f;
                        // trueEyeCount - 2 is so that it is on top of the player if there's only two, but to the left if there's 3
                        idealPosition = player.Center + new Vector2(-620 * MathHelper.Clamp(trueEyeCount - 2, 0f, 1f), -360);
                        // Fly over the top left of the player
                        if (npc.ai[1] < seekTimeCircular)
                        {
                            const float acceleration = 0.3f;
                            const float maxVelocity = 15f;
                            npc.SimpleFlyMovement(npc.SafeDirectionTo(idealPosition) * maxVelocity, acceleration);
                            npc.rotation = npc.localAI[0] = npc.velocity.X / 14f;
                            npc.localAI[2] = 0.4f;
                        }
                        // Look at the player
                        else if (npc.ai[1] < seekTimeCircular + waitTime)
                        {
                            npc.localAI[2] = MathHelper.Lerp(npc.localAI[2], 0.6f, 0.05f);
                            npc.rotation = npc.rotation.AngleLerp(0f, 0.2f);
                            npc.velocity *= 0.9f;
                            if (npc.ai[1] < seekTimeCircular + waitTime - staticTelegraphTime)
                            {
                                npc.Infernum().angleTarget = player.Center;
                                npc.localAI[0] = npc.Infernum().ExtraAI[1] = npc.AngleTo(npc.Infernum().angleTarget);
                            }
                            npc.Infernum().canTelegraph = true;
                        }
                        // Release circle of bolts
                        else if (npc.ai[1] == seekTimeCircular + waitTime)
                        {
                            npc.Infernum().canTelegraph = false;
                            int boltCount = enrage ? 32 : 20;
                            for (int i = 0; i < boltCount; i++)
                            {
                                float angle = MathHelper.TwoPi / boltCount * i + npc.Infernum().ExtraAI[1];
                                float velocityMultiplier = enrage ? 7f : 3f;
                                // Cause the bolt aimed at the player to go much faster than the other bolts
                                if (Math.Abs(angle - npc.Infernum().ExtraAI[1]) < 0.04f)
                                {
                                    velocityMultiplier = 9f;
                                }
                                NewProjectileBetter(npc.Center, angle.ToRotationVector2() * velocityMultiplier, ProjectileID.PhantasmalBolt, 185, 1f);
                            }
                        }
                        break;
                    // Aim and release 3 phantasmal blasts
                    case 2:
                    case 3:
                        idealPosition = player.Center + new Vector2(0f, 360);
                        const float seekTimeBlaster = 130f;
                        const float aimTime = 30f;
                        // Fly below the player
                        if (npc.ai[1] < seekTimeBlaster + delay)
                        {
                            const float acceleration = 0.3f;
                            const float maxVelocity = 15f;
                            npc.SimpleFlyMovement(npc.SafeDirectionTo(idealPosition) * maxVelocity, acceleration);
                            npc.rotation = npc.localAI[0] = npc.velocity.X / 14f;
                            npc.localAI[2] = 0.4f;
                        }
                        // Aim and telegraph
                        else if (npc.ai[1] < seekTimeBlaster + aimTime + delay)
                        {
                            npc.localAI[0] = npc.AngleTo(player.Center);
                            npc.localAI[2] = MathHelper.Lerp(npc.localAI[2], 0.6f, 0.05f);
                            npc.rotation = npc.rotation.AngleLerp(npc.localAI[0] + MathHelper.PiOver2, 0.2f);
                            if (npc.ai[1] < seekTimeBlaster + aimTime + delay - staticTelegraphTime)
                            {
                                npc.Infernum().angleTarget = player.Center;
                            }
                            npc.Infernum().canTelegraph = true;
                        }
                        // Release 3 phantasmal blasts, chaingun style
                        else if (npc.ai[1] < seekTimeBlaster + 2f * aimTime + delay)
                        {
                            npc.velocity *= 0.9f;
                            npc.Infernum().canTelegraph = false;
                            if (npc.ai[1] % 10 == 0)
                            {
                                if (npc.ai[1] == seekTimeBlaster + aimTime + delay + 10f)
                                {
                                    Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, Main.rand.Next(100, 101), 1f, 0f);
                                }
                                Utilities.NewProjectileBetter(npc.Center, npc.SafeDirectionTo(npc.Infernum().angleTarget) * 15f * (enrage ? 1.4f : 1f), ModContent.ProjectileType<PhantasmalBlast>(), 200, 2.6f);
                            }
                        }
                        break;
                    // Spiral of bolts
                    default:
                        const float seekTimeSpiral = 170f;
                        idealPosition = player.Center + new Vector2(620 * MathHelper.Clamp(trueEyeCount - 2, 0f, 1f), -360);
                        // Fly to the top right the player
                        if (npc.ai[1] < seekTimeSpiral + delay * 2f)
                        {
                            const float acceleration = 0.3f;
                            const float maxVelocity = 15f;
                            npc.SimpleFlyMovement(npc.SafeDirectionTo(idealPosition) * maxVelocity, acceleration);
                            npc.rotation = npc.localAI[0] = npc.velocity.X / 14f;
                            npc.localAI[2] = 0.4f;
                        }
                        // Shoot a barrage of spirals
                        if (npc.ai[1] >= seekTimeSpiral + delay * 2f)
                        {
                            npc.velocity *= 0.9f;
                            npc.localAI[0] = npc.AngleTo(player.Center);
                            npc.localAI[2] = MathHelper.Lerp(npc.localAI[2], 0.6f, 0.05f);
                            npc.rotation = npc.rotation.AngleLerp(npc.localAI[0] + MathHelper.PiOver2, 0.2f);
                            const float ai1Skip = 3f;
                            if (npc.ai[1] % ai1Skip == ai1Skip - 1)
                            {
                                npc.Infernum().ExtraAI[1] += MathHelper.ToRadians(360f / (250f - seekTimeSpiral) * ai1Skip);
                                Vector2 spawnPositon = npc.Center;
                                NewProjectileBetter(spawnPositon, npc.Infernum().ExtraAI[1].ToRotationVector2() * (enrage ? 4.25f : 3f), ProjectileID.PhantasmalBolt, 185, 1f);
                            }
                        }
                        break;
                }
                // Go to next AI state
                if (npc.ai[1] >= 250f + 2f * delay)
                {
                    MLSealTeleport = true;
                    npc.ai[0] = 2f;
                    npc.ai[1] =
                        npc.Infernum().ExtraAI[1] =
                        npc.Infernum().ExtraAI[2] =
                        npc.Infernum().ExtraAI[3] = 0f;
                    EyeSyncVariables(npc);
                }
            }

            // Phantasmal Storm
            else if (npc.ai[0] == 2f)
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
                        if (npc.ai[1] < seekTimeSpin)
                        {
                            idealPosition = player.Center + new Vector2(0f, -420f);
                            npc.SimpleFlyMovement(npc.SafeDirectionTo(idealPosition) * maxVelocity, 0.3f);
                            npc.rotation = npc.rotation.AngleLerp(0f, 0.2f);
                            npc.localAI[0] = npc.rotation;
                            npc.localAI[2] = MathHelper.Lerp(npc.localAI[2], 0.5f, 0.05f);
                        }
                        // Adjust velocity to unit rotation vector
                        else if (npc.ai[1] == seekTimeSpin + 1)
                        {
                            npc.velocity = new Vector2(0f, -16f);
                            Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, Main.rand.Next(100, 101), 1f, 0f);
                        }
                        // Spin
                        else if (npc.ai[1] < seekTimeSpin + spinTime)
                        {
                            npc.velocity = npc.velocity.RotatedBy(MathHelper.ToRadians(360f / spinTime));
                            npc.rotation = npc.rotation.AngleLerp(npc.velocity.ToRotation() + MathHelper.PiOver2, 0.2f);
                            npc.localAI[0] = npc.rotation;
                            if (npc.ai[1] % (int)(spinTime / phantasmalEyeCount / (enrage ? 2f : 1f)) ==
                                (int)(spinTime / phantasmalEyeCount / (enrage ? 2f : 1f)) - 1)
                            {
                                NewProjectileBetter(npc.Center - Vector2.UnitX * Main.rand.Next(-npc.width + 12, npc.width - 12),
                                    new Vector2(0f, -1f * Main.rand.NextFloat(8f, 14f)).RotatedByRandom(MathHelper.ToRadians(36f)),
                                    ProjectileID.PhantasmalEye, 190, 1f);
                            }
                        }
                        // Slow down
                        else if (npc.ai[1] >= seekTimeSpin + spinTime)
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
                        npc.ai[1] += 1f;
                        // Get into a general area of the target position
                        if (npc.ai[1] < seekTimeCharge + delay)
                        {
                            float signX = 1f;
                            if (Math.Abs(player.Center.X - xSeekPosition) < Math.Abs(player.Center.X + xSeekPosition))
                                signX = -1f;
                            idealPosition = player.Center + new Vector2(signX * xSeekPosition, -480f);
                            npc.SimpleFlyMovement(npc.SafeDirectionTo(idealPosition) * maxVelocity, 0.3f);
                            npc.rotation = npc.rotation.AngleLerp(0f, 0.2f);
                            npc.localAI[2] = MathHelper.Lerp(npc.localAI[2], 0.5f, 0.05f);
                        }
                        // Charge while releasing eyes
                        else if (npc.ai[1] < seekTimeCharge + chargeTime + delay)
                        {
                            if (npc.ai[1] == seekTimeCharge + delay + 1f)
                                Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, Main.rand.Next(100, 101), 1f, 0f);
                            npc.localAI[2] = 0.5f;
                            float signX = 1f;
                            if (Math.Abs(player.Center.X - xSeekPosition) < Math.Abs(player.Center.X + xSeekPosition))
                                signX = -1f;
                            npc.velocity.X = MathHelper.Lerp(npc.velocity.X, -signX * maxChargeVelocity, acceleration);
                            npc.velocity.Y = 0f;
                            if (npc.ai[1] % (int)(chargeTime / phantasmalEyeCount2) == (int)(chargeTime / phantasmalEyeCount2) - 1)
                            {
                                Utilities.NewProjectileBetter(npc.Center - Vector2.UnitX * Main.rand.Next(-npc.width + 12, npc.width - 12),
                                    new Vector2(0f, -1f * Main.rand.NextFloat(8f, 14f)).RotatedByRandom(MathHelper.ToRadians(36f)),
                                    ProjectileID.PhantasmalEye, 190, 1f);
                            }
                            npc.rotation = npc.velocity.X / 16.55211f;
                        }
                        // Slow down
                        else if (npc.ai[1] >= seekTimeCharge + chargeTime + delay)
                        {
                            npc.rotation = npc.rotation.AngleLerp(0f, 0.2f);
                            npc.velocity = Vector2.Lerp(npc.velocity, Vector2.Zero, 0.2f);
                        }
                        break;
                    // Spew out phantasmal blasts
                    default:
                        npc.ai[1] += 1f;
                        const float floatTime = 140f;
                        const float aimTime = 45f;
                        const float blastTime = 60f;
                        const int blastCount = 12;
                        // Get into a general area of the target position
                        if (npc.ai[1] < floatTime + 2f * delay)
                        {
                            float signX = 1f;
                            if (Math.Abs(player.Center.X - xSeekPosition) < Math.Abs(player.Center.X + xSeekPosition))
                                signX = -1f;
                            signX *= -1f;
                            idealPosition = player.Center + new Vector2(signX * xSeekPosition, -480f);
                            npc.SimpleFlyMovement(npc.SafeDirectionTo(idealPosition) * maxVelocity, 0.3f);
                            npc.rotation = npc.localAI[0] = npc.velocity.X / 14f;
                            npc.localAI[2] = MathHelper.Lerp(npc.localAI[2], 0.5f, 0.05f);
                        }
                        // Choose a target and telegraph
                        else if (npc.ai[1] < floatTime + blastTime + 2f * delay)
                        {
                            npc.rotation = npc.rotation.AngleLerp(0f, 0.2f);
                            npc.velocity = Vector2.Lerp(npc.velocity, Vector2.Zero, 0.2f);
                            npc.Infernum().angleTarget = player.Center;
                            npc.Infernum().canTelegraph = true;
                        }
                        // Stop telegraphing and shoot
                        else if (npc.ai[1] < floatTime + blastTime + aimTime + 2f * delay)
                        {
                            if (npc.Infernum().canTelegraph)
                            {
                                npc.Infernum().canTelegraph = false;
                                Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, Main.rand.Next(100, 101), 1f, 0f);
                            }
                            if (npc.ai[1] % (int)(blastTime / blastCount / (enrage ? 2f : 1f)) ==
                                (int)(blastTime / blastCount / (enrage ? 2f : 1f)) - 1)
                            {
                                npc.localAI[0] = npc.AngleTo(npc.Infernum().angleTarget);
                                NewProjectileBetter(npc.Center, npc.SafeDirectionTo(npc.Infernum().angleTarget) * 15f, ModContent.ProjectileType<PhantasmalBlast>(), 200, 2.6f);
                            }
                        }
                        break;
                }
                // Go to next AI state
                if (npc.ai[1] >= 280f + 2f * delay)
                {
                    MLSealTeleport = true;
                    npc.ai[0] = core.life / (float)core.lifeMax > 0.4f ? 0f : 3f;
                    npc.ai[1] =
                        npc.Infernum().ExtraAI[1] =
                        npc.Infernum().ExtraAI[2] =
                        npc.Infernum().ExtraAI[3] = 0f;
                    EyeSyncVariables(npc);
                }
            }

            // Phantasmal Assault
            else if (npc.ai[0] == 3f)
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
                            npc.ai[1] += 1f;
                        // Move to appropriate position
                        if (npc.ai[1] < seekTimeCharge)
                        {
                            idealPosition = player.Center + new Vector2(xSeekPosition * signX, core.Infernum().arenaRectangle.Y - 50f);
                            idealPosition.X = core.Infernum().arenaRectangle.X + core.Infernum().arenaRectangle.Width * (groupIndex == 3).ToInt();
                            idealPosition.Y = core.Infernum().arenaRectangle.Y + 16f;
                            npc.SimpleFlyMovement(npc.SafeDirectionTo(idealPosition) * maxVelocity, 1.5f);
                            npc.localAI[2] = MathHelper.Lerp(npc.localAI[2], 0.5f, 0.05f);
                            npc.rotation = 0f;
                        }
                        // Release laser and go twoards the center
                        else if (npc.ai[1] < seekTimeCharge + chargeTime)
                        {
                            if (npc.ai[1] == seekTimeCharge + chargeTime + 1f)
                                Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, Main.rand.Next(100, 101), 1f, 0f);
                            npc.localAI[2] = 0.5f;
                            npc.velocity = -signX * xVelocity * Vector2.UnitX;
                            if (npc.ai[1] == seekTimeCharge + 2)
                            {
                                Vector2 fromEyeCoordinates = Utils.Vector2FromElipse(npc.localAI[0].ToRotationVector2(), new Vector2(27f, 59f) * npc.localAI[1]);
                                int idx = Utilities.NewProjectileBetter(npc.Center + fromEyeCoordinates, Vector2.UnitY, ModContent.ProjectileType<MoonlordPendulum>(), 425, 0f, 255, 0f, npc.whoAmI);
                                Main.projectile[idx].ai[0] = 0f;
                                Main.projectile[idx].ai[1] = npc.whoAmI;
                            }
                            npc.rotation = npc.rotation.AngleTowards(-MathHelper.Pi, 0.1f);
                            npc.localAI[0] = npc.rotation - MathHelper.PiOver2;
                        }
                        break;
                    // Bolts
                    default:
                        npc.ai[1] += 1f;
                        idealPosition = player.Center + new Vector2(Main.rand.NextFloat(-120f, 120f), -580f);
                        // Adjust rotation and size of pupil
                        if (npc.ai[1] < seekTimeCharge)
                        {
                            npc.localAI[2] = MathHelper.Lerp(npc.localAI[2], 0.5f, 0.05f);
                            npc.rotation = 0f;
                        }
                        // Fly above player and release bolts
                        if (npc.ai[1] < seekTimeCharge + chargeTime)
                        {
                            // Circular spread
                            if (npc.ai[1] % 80f == 45f && npc.ai[1] <= 240f)
                            {
                                int boltCount = 10;
                                if (enrage)
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
                                    Utilities.NewProjectileBetter(npc.Center, velocity, ProjectileID.PhantasmalBolt, 185, 1f);
                                }
                            }
                            // Spiral
                            if (npc.ai[1] >= 240f)
                            {
                                if (npc.ai[1] % 3f == 2f)
                                {
                                    npc.Infernum().ExtraAI[1] += MathHelper.ToRadians(6f);
                                    npc.localAI[0] = npc.Infernum().ExtraAI[1];
                                    Utilities.NewProjectileBetter(npc.Center, npc.Infernum().ExtraAI[1].ToRotationVector2() * 1.5f * (enrage ? 3f : 1f), ProjectileID.PhantasmalBolt, 185, 1f);
                                }
                            }
                        }
                        npc.SimpleFlyMovement(npc.SafeDirectionTo(idealPosition) * maxVelocity, 0.5f);
                        break;
                }
                // Go to next AI state
                if (npc.ai[1] >= chargeTime + seekTimeCharge)
                {
                    MLSealTeleport = true;
                    npc.ai[0] = 0f;
                    npc.ai[1] =
                        npc.Infernum().ExtraAI[1] =
                        npc.Infernum().ExtraAI[2] =
                        npc.Infernum().ExtraAI[3] = 0f;
                    EyeSyncVariables(npc);
                }
            }
            return false;
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

#pragma warning disable IDE0060
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
#pragma warning restore IDE0060

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
