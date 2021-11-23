using CalamityMod;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.ID;

using static InfernumMode.Utilities;
using CalamityMod.NPCs;
using CalamityMod.Events;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class MoonLordHandBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.MoonLordHand;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;
        
        // ai[0] = ai state. -2 = dead, don't do anything. 0 = move around. 1 = spawn phantasmal eyes. 2 = spawn phantasmal spheres. 3 = spawn phantasmal bolts. (5) = go to next ai state
        // ai[1] = see head
        // ai[2] = left/right hand. If 0, left hand. Otherwise, right hand
        // ai[3] = see head
        // localAI[0] = see head
        // localAI[1] = see head
        // localAI[2] = see head
        // localAI[3] = see head
        // ExtraAI[0] = see head
        // ExtraAI[1] = see head
        // ExtraAI[2] = see head
        public override bool PreAI(NPC npc)
        {
            CalamityGlobalNPC calamityGlobalNPC = npc.Calamity();

            // Hands life ratio
            NPC[] hands = Main.npc.Where(n => n.type == npc.type && n.active).ToArray();
            float handsRatio = hands.Sum(h => h.life / (float)h.lifeMax);

            if ((calamityGlobalNPC.newAI[0] == 1f || npc.life < 1700) && npc.Infernum().ExtraAI[2] != -2f)
            {
                SummonTrueEye(npc);
            }

            // Despawn
            if (!Main.npc[(int)npc.ai[3]].active || Main.npc[(int)npc.ai[3]].type != NPCID.MoonLordCore)
            {
                npc.life = 0;
                npc.HitEffect();
                npc.active = false;
            }

            // Variables
            int idealFrame = 0;
            bool leftHand = npc.ai[2] == 0f;
            float handSign = -leftHand.ToDirectionInt();
            bool enrage = Main.npc[(int)npc.ai[3]].Infernum().ExtraAI[0] == 0f;

            npc.spriteDirection = (int)handSign;
            npc.dontTakeDamage = npc.frameCounter >= 21.0 || !Main.npc[NPC.FindFirstNPC(NPCID.MoonLordHead)].dontTakeDamage || calamityGlobalNPC.newAI[0] == 1f;

            // Go to die
            if (Main.npc[(int)npc.ai[3]].ai[0] == 2f)
                npc.ai[0] = -2f;

            if (npc.Infernum().ExtraAI[2] == -2f)
                npc.ai[0] = -2f;

            // Choose attacks
            if (npc.ai[0] != -2f || (npc.ai[0] == -2f && Main.npc[(int)npc.ai[3]].ai[0] != 2f))
            {
                if (npc.ai[0] == -2f && Main.npc[(int)npc.ai[3]].ai[0] != 2f)
                {
                    if (calamityGlobalNPC.newAI[0] != 1f)
                        calamityGlobalNPC.newAI[0] = 1f;

                    npc.life = npc.lifeMax;
                    npc.netUpdate = true;
                    npc.dontTakeDamage = true;

                    // For animating the weird tentacle hand thingy
                    npc.ai[1] += 1f;
                    if (npc.ai[1] >= 32f)
                    {
                        npc.ai[1] = 0f;
                    }
                    if (npc.ai[1] < 0f)
                    {
                        npc.ai[1] = 0f;
                    }
                }
            }

            const int ai0Reset = 5;
            Vector2 coreCenter = Main.npc[(int)npc.ai[3]].Center;
            Vector2 perfectHandPosition = coreCenter + new Vector2(350f * handSign, -100f);
            Vector2 ellipseVector = Utils.Vector2FromElipse(npc.localAI[0].ToRotationVector2(), new Vector2(30f, 66f) * npc.localAI[1]);

            if (npc.ai[0] == -2f)
            {
                Vector2 distanceToPerfect = perfectHandPosition - npc.Center;

                if (distanceToPerfect.Length() > 20f)
                {
                    distanceToPerfect.Normalize();
                    float velocity = BossRushEvent.BossRushActive ? 10.5f : 7.5f;
                    distanceToPerfect *= velocity;
                    Vector2 oldVelocity = npc.velocity;

                    if (distanceToPerfect != Vector2.Zero)
                        npc.SimpleFlyMovement(distanceToPerfect, 0.3f);

                    npc.velocity = Vector2.Lerp(oldVelocity, npc.velocity, 0.5f);
                }
                npc.Calamity().newAI[1] = 2f;
                npc.dontTakeDamage = true;
            }

            // Move
            else if (npc.ai[0] == 0f)
            {
                idealFrame = 3;
                npc.localAI[1] -= 0.05f;
                if (npc.localAI[1] < 0f)
                    npc.localAI[1] = 0f;

                Vector2 distanceToPerfect = perfectHandPosition - npc.Center;

                if (distanceToPerfect.Length() > 20f)
                {
                    distanceToPerfect.Normalize();
                    float velocity = BossRushEvent.BossRushActive ? 10.5f : 7.5f;
                    distanceToPerfect *= velocity;
                    Vector2 velocity5 = npc.velocity;

                    if (distanceToPerfect != Vector2.Zero)
                        npc.SimpleFlyMovement(distanceToPerfect, 0.3f);

                    npc.velocity = Vector2.Lerp(velocity5, npc.velocity, 0.5f);
                }
                if (npc.Infernum().ExtraAI[1] >= 90)
                {
                    if (npc.life > 1700)
                        npc.ai[0] = ai0Reset;
                    npc.Infernum().ExtraAI[1] = 0f;
                }
            }

            // Phantasmal Eyes
            else if (npc.ai[0] == 1f)
            {
                idealFrame = 0;
                int phantasmalEyeCount = 14;
                int shootRate = 4;
                if (enrage)
                {
                    phantasmalEyeCount = 24;
                    shootRate = 2;
                }

                if (npc.Infernum().ExtraAI[1] >= phantasmalEyeCount * shootRate * 2)
                {
                    npc.localAI[1] -= 0.07f;
                    if (npc.localAI[1] < 0f)
                        npc.localAI[1] = 0f;
                }
                else if (npc.Infernum().ExtraAI[1] >= phantasmalEyeCount * shootRate)
                {
                    npc.localAI[1] += 0.05f;
                    if (npc.localAI[1] > 0.75f)
                        npc.localAI[1] = 0.75f;

                    float pupilAngle = MathHelper.TwoPi * (npc.Infernum().ExtraAI[1] % (phantasmalEyeCount * shootRate)) / (phantasmalEyeCount * shootRate) - 1.57079637f;
                    npc.localAI[0] = (pupilAngle.ToRotationVector2() * ellipseVector).ToRotation();

                    if (npc.Infernum().ExtraAI[1] % shootRate == 0f)
                    {
                        // Vector2 value11 = new Vector2(1f * -handSign, 3f);
                        // Let it also be known that some nerd was multiplying a value by 1.

                        Vector2 eyeSpawnDelta = new Vector2(-handSign, 3f);
                        Vector2 eyeSpawnPos = npc.Center + Vector2.Normalize(ellipseVector) * ellipseVector.Length() * 0.4f + eyeSpawnDelta;
                        float velocity = BossRushEvent.BossRushActive ? 9f : 5.45f;
                        Vector2 eyeVelocity = Vector2.Normalize(ellipseVector) * velocity;
                        float ai = (MathHelper.TwoPi * (float)Main.rand.NextDouble() - MathHelper.Pi) / 30f + MathHelper.ToRadians(handSign);
                        NewProjectileBetter(eyeSpawnPos, eyeVelocity, ProjectileID.PhantasmalEye, 175, 0f, Main.myPlayer, 0f, ai);
                    }
                }
                else
                {
                    npc.localAI[1] -= 0.02f;
                    if (npc.localAI[1] < 0f)
                        npc.localAI[1] = 0f;

                    npc.localAI[0] = npc.localAI[0].AngleTowards(0f, 0.7f);
                }
                if (npc.Infernum().ExtraAI[1] >= phantasmalEyeCount * shootRate + 10)
                {
                    if (npc.life > 1700)
                        npc.ai[0] = ai0Reset;
                    npc.Infernum().ExtraAI[1] = 0f;
                }
            }

            // Phantasmal Spheres
            else if (npc.ai[0] == 2f)
            {
                // No stupid spheres during deathray
                if (Main.npc[NPC.FindFirstNPC(NPCID.MoonLordHead)].ai[0] == 1f)
                {
                    npc.ai[0] = Utils.SelectRandom(Main.rand, 1f, 3f);
                }
                npc.localAI[1] -= 0.05f;
                if (npc.localAI[1] < 0f)
                    npc.localAI[1] = 0f;

                Vector2 idealBase = new Vector2(220f * handSign, -60f) + coreCenter;
                idealBase += new Vector2(handSign * 100f, -50f);
                Vector2 idealDelta = new Vector2(400f * handSign, -60f);

                float velocityMultiplier = BossRushEvent.BossRushActive ? 0.87f : 0.885f;
                if (npc.Infernum().ExtraAI[1] < 30f)
                {
                    Vector2 intialVelocity = idealBase - npc.Center;
                    if (intialVelocity != Vector2.Zero)
                    {
                        float velocityMult = 16f;
                        npc.velocity = Vector2.SmoothStep(npc.velocity, Vector2.Normalize(intialVelocity) * Math.Min(velocityMult, intialVelocity.Length()), 0.2f);
                    }
                }
                else if (npc.Infernum().ExtraAI[1] < 210f)
                {
                    idealFrame = 1;
                    int modifiedAICounter = (int)npc.Infernum().ExtraAI[1] - 30;
                    int shootRate = enrage ? 8 : 24;
                    if (modifiedAICounter % shootRate == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 sphereVelocity = new Vector2(5f * handSign, -8f);
                        int counterDivided = modifiedAICounter / 30;
                        sphereVelocity.X += (counterDivided - 3.5f) * handSign * 3f;
                        sphereVelocity.Y += (counterDivided - 4.5f) * 1f;
                        sphereVelocity *= 1.35f;
                        int idx = NewProjectileBetter(npc.Center, sphereVelocity, ProjectileID.PhantasmalSphere, 215, 1f, Main.myPlayer, 0f, npc.whoAmI);
                        Main.projectile[idx].timeLeft = 540 + Main.rand.Next(0, 120);
                    }

                    Vector2 smoothDistance = Vector2.SmoothStep(idealBase, idealBase + idealDelta, (npc.Infernum().ExtraAI[1] - 30f) / 180f) - npc.Center;
                    if (smoothDistance != Vector2.Zero)
                    {
                        float velocity = BossRushEvent.BossRushActive ? 35f : 25f;
                        npc.velocity = Vector2.Lerp(npc.velocity, Vector2.Normalize(smoothDistance) * Math.Min(velocity, smoothDistance.Length()), 0.5f);
                    }
                }
                else if (npc.Infernum().ExtraAI[1] < 282f)
                {
                    idealFrame = 0;
                    npc.velocity *= velocityMultiplier;
                }
                else if (npc.Infernum().ExtraAI[1] < 287f)
                {
                    idealFrame = 1;
                    npc.velocity *= velocityMultiplier;
                }
                else if (npc.Infernum().ExtraAI[1] < 292f)
                {
                    idealFrame = 2;
                    npc.velocity *= velocityMultiplier;
                }
                else if (npc.Infernum().ExtraAI[1] < 300f)
                {
                    idealFrame = 3;
                    if (npc.Infernum().ExtraAI[1] == 292f && Main.netMode != NetmodeID.MultiplayerClient && enrage)
                    {
                        int closestPlayerIdx = Player.FindClosest(npc.position, npc.width, npc.height);
                        float velocityMult = BossRushEvent.BossRushActive ? 18f : 14f;
                        Vector2 closestPlayerDistNorm = (Main.player[closestPlayerIdx].Center - (npc.Center + Vector2.UnitY * -350f)).SafeNormalize(Vector2.UnitY) * velocityMult;

                        for (int projectileIdx = 0; projectileIdx < 1000; projectileIdx++)
                        {
                            Projectile projectile = Main.projectile[projectileIdx];
                            if (projectile.active && projectile.type == ProjectileID.PhantasmalSphere && projectile.ai[1] == npc.whoAmI && projectile.ai[0] != -1f)
                            {
                                projectile.ai[0] = -1f;
                                projectile.velocity = closestPlayerDistNorm;
                                projectile.netUpdate = true;
                            }
                        }
                    }
                    Vector2 smoothDistance = Vector2.SmoothStep(idealBase, idealBase + idealDelta, 1f - (npc.Infernum().ExtraAI[1] - 270f) / 30f) - npc.Center;
                    if (smoothDistance != Vector2.Zero)
                    {
                        float velocityMult = BossRushEvent.BossRushActive ? 24.5f : 17.5f;
                        npc.velocity = Vector2.Lerp(npc.velocity, Vector2.Normalize(smoothDistance) * Math.Min(velocityMult, smoothDistance.Length()), 0.1f);
                    }
                }
                else
                {
                    idealFrame = 3;

                    Vector2 distanceFromIdeal = idealBase - npc.Center;
                    float velocityMult = BossRushEvent.BossRushActive ? 14f : 10f;
                    npc.velocity = Vector2.SmoothStep(npc.velocity, distanceFromIdeal.SafeNormalize(Vector2.Zero) * Math.Min(velocityMult, distanceFromIdeal.Length()), 0.2f);
                }
                if (npc.Infernum().ExtraAI[1] >= 330f)
                {
                    if (npc.life > 1700)
                        npc.ai[0] = ai0Reset;
                    npc.Infernum().ExtraAI[1] = 0f;
                }
            }

            // Phantasmal Bolts
            else if (npc.ai[0] == 3f)
            {
                if (npc.Infernum().ExtraAI[1] == 1f)
                {
                    npc.netUpdate = true;
                }
                npc.TargetClosest(false);
                Vector2 playerDistance = Main.player[npc.target].Center + Main.player[npc.target].velocity * 20f - npc.Center;
                npc.localAI[0] = playerDistance.ToRotation();

                npc.localAI[1] += 0.05f;
                if (npc.localAI[1] > 1f)
                    npc.localAI[1] = 1f;

                if (npc.Infernum().ExtraAI[1] == 20f)
                    Main.PlaySound(SoundID.NPCDeath6, npc.position);

                if (npc.Infernum().ExtraAI[1] >= 20f && npc.Infernum().ExtraAI[1] % 5f == 4f && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float velocity = BossRushEvent.BossRushActive ? 7f : 5f;
                    if (enrage)
                        velocity *= 1.8f;
                    Vector2 boltVelocity = Vector2.Normalize(playerDistance) * velocity;
                    NewProjectileBetter(npc.Center + ellipseVector, boltVelocity, ProjectileID.PhantasmalBolt, 185, 0f, Main.myPlayer, 0f, 0f);
                }
                if (npc.Infernum().ExtraAI[1] >= 70f)
                {
                    if (npc.life > 1700)
                        npc.ai[0] = ai0Reset;
                    npc.Infernum().ExtraAI[1] = 0f;
                }
            }

            // Reset
            else if (npc.ai[0] == ai0Reset)
            {
                npc.Infernum().ExtraAI[0] += 1f;
                switch ((int)npc.Infernum().ExtraAI[0] % 8)
                {
                    case 0:
                        npc.ai[0] = 1f;
                        break;
                    case 1:
                        npc.ai[0] = 2f;
                        break;
                    case 2:
                        npc.ai[0] = 1f;
                        break;
                    case 3:
                        npc.ai[0] = 0f;
                        break;
                    case 4:
                        npc.ai[0] = 2f;
                        break;
                    case 5:
                        npc.ai[0] = 0f;
                        break;
                    case 6:
                        npc.ai[0] = 3f;
                        break;
                    case 7:
                        npc.ai[0] = 2f;
                        break;
                }
                npc.netSpam = 0;
                npc.netUpdate = true;
            }
            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);

            npc.Infernum().ExtraAI[1] += 1f;
            Vector2 baseHandIdeal = new Vector2(220f * handSign, -60f) + coreCenter;
            Vector2 handIdeal = baseHandIdeal + new Vector2(handSign * 110f, -150f);
            Vector2 handIdeal2 = handIdeal + new Vector2(handSign * 370f, 150f);

            if (handIdeal.X > handIdeal2.X)
                Utils.Swap(ref handIdeal.X, ref handIdeal2.X);
            if (handIdeal.Y > handIdeal2.Y)
                Utils.Swap(ref handIdeal.Y, ref handIdeal2.Y);
            Vector2 clippedPositionDelta = Vector2.Clamp(npc.Center + npc.velocity, handIdeal, handIdeal2);
            if (clippedPositionDelta != npc.Center + npc.velocity)
                npc.Center = clippedPositionDelta - npc.velocity;
            // Frames
            int idealFrameCounter = idealFrame * 7;
            if (idealFrameCounter > npc.frameCounter)
            {
                npc.frameCounter += 1.0;
            }
            if (idealFrameCounter < npc.frameCounter)
            {
                npc.frameCounter -= 1.0;
            }

            if (npc.frameCounter < 0.0)
                npc.frameCounter = 0.0;
            if (npc.frameCounter > 21.0)
                npc.frameCounter = 21.0;
            return false;
        }

        public static int[] GetTrueEyesIndex => Main.npc.Where(entity => entity.active && entity.type == NPCID.MoonLordFreeEye).Select(entity => entity.whoAmI).ToArray();
        public static NPC[] GetTrueEyes => Main.npc.Where(entity => entity.active && entity.type == NPCID.MoonLordFreeEye).Select(entity => Main.npc[entity.whoAmI]).ToArray();

        /// <summary>
        /// Summons a true eye of cthulhu from a body part and then adjusts said body part so it doesn't do this again
        /// </summary>
        /// <param name="npc">The body part from which to modify/derive from</param>
        public static void SummonTrueEye(NPC npc)
        {
            npc.Infernum().ExtraAI[2] = -2f;
            npc.ai[0] = -2f;
            npc.life = npc.lifeMax;
            npc.netUpdate = true;
            npc.dontTakeDamage = true;
            int groupIndex = 0;
            if (GetTrueEyes.Length > 0)
                groupIndex = (int)GetTrueEyes.Max(eye => eye.Infernum().ExtraAI[0]) + 1;
            npc.Infernum().ExtraAI[0] = groupIndex;
            int idx = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCID.MoonLordFreeEye, 0, 0f, 0f, 0f, 0f, 255);
            Main.npc[idx].ai[3] = -1;
            Main.npc[idx].Infernum().ExtraAI[0] = groupIndex;
            Main.npc[idx].netUpdate = true;
        }
    }
}
