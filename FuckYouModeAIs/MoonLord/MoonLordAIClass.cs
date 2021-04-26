using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs;
using InfernumMode.FuckYouModeAIs.NPCs;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Utilities;
using static InfernumMode.FuckYouModeAIs.MainAI.FuckYouModeAIsGlobal;

namespace InfernumMode.FuckYouModeAIs.MoonLord
{
	public class MoonLordAIClass
    {
		#region Attack States
        public enum MoonLordHeadAttackState
        {
            Death = -3,
            FreeEye = -2,
            DeathrayPupilAdjusetments = 0,
            FireDeathray = 1,
            LookAtTarget = 2,
            FirePhantasmalBolts = 3,
            Reset = 4
        }
		#endregion

		#region Variables and Documentation

		internal const int BoltDamage = 180;
        internal const int SphereDamage = 215;
        internal const int EyeDamage = 170;
        internal const int BlastDamage = 240;
        internal const int LaserDamage = 450;

        internal static readonly Vector2 EllipseSizeHead = new Vector2(27f, 59f);
        internal static readonly Vector2 EllipseSizeHand = new Vector2(30f, 66f);

        // Variable meanings in various parts:

        // Head:
        // ai[0] = ai state. -3 = true death. Occurs when the core is killed instead of the head. -2 = dead, don't do anything.
        //         0 = prepare deathray. 1 = do deathray. 2 = do nothing for a bit. 3 = spawn phantasmal bolts. (5) = go to next ai state
        // ai[1] = weird tentacle thing animation counter when dead (frame is determined by this divided by 8)
        // ai[2] = value used in determining the angle of the pupil while deathray is active
        // ai[3] = core index
        // localAI[0] = angle of pupil when eye is open
        // localAI[1] = 0-1 multiplier of the imaginary ellipse used to determine where the pupil is when eye is open
        // localAI[2] = scale of the pupil when eye is open
        // localAI[3] = eye open/close animation counter (does not apply when dead)
        // ExtraAI[0] = state counter (used in check for what the next attack will be)
        // ExtraAI[1] = ai counter
        // ExtraAI[2] and newAI[0] = things to make sure the eye appears properly. these can be ignored.

        // Hands:
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

        // Core:
        // ai[0] = ai state. -2 = early death state and effects. -1 = spawn body parts. 0 = fly near target (invulnerable). 1 = fly near target (vulnerable)
        //             2 = death state and effects. 3 = despawn
        // ai[1] = ai counter variable
        // ai[2] = appears to be unused
        // ai[3] = appears to be unused
        // localAI[0] = left hand npc index
        // localAI[1] = right hand npc index
        // localAI[2] = head npc index
        // localAI[3] = initialization value, spawning the arena, ect.
        // ExtraAI[0] = enrage flag. 1 is normal, 0 is enraged
        // ExtraAI[1] = counter for summoning seal waves

        // True Eyes:
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
        #endregion

        #region AI Code

        public const int HeadLifemax = 52525;
        public const int HandLifemax = 43390;
        public const int CoreLifemax = 99990;


        [OverrideAppliesTo(NPCID.MoonLordHead, typeof(MoonLordAIClass), "MoonlordHeadAI", EntityOverrideContext.NPCAI)]
        public static bool MoonlordHeadAI(NPC npc)
        {
            CalamityGlobalNPC calamityGlobalNPC = npc.Calamity();

            // Despawn instantly if anything happened to the core.
            if (!Main.npc.IndexInRange((int)npc.ai[3]) || !Main.npc[(int)npc.ai[3]].active || Main.npc[(int)npc.ai[3]].type != NPCID.MoonLordCore)
            {
                npc.life = 0;
                npc.HitEffect();
                npc.active = false;
                return false;
            }

            // Ref local variables.
            ref float attackStateValue = ref npc.ai[0];
            MoonLordHeadAttackState attackState = (MoonLordHeadAttackState)attackStateValue;
            ref float tentacleAnimationFrameCounter = ref npc.ai[1];
            Projectile currentlyFiredRay = Main.projectile[(int)npc.ai[2]];
            NPC core = Main.npc[(int)npc.ai[3]];
            ref float pupilAngle = ref npc.localAI[0];
            ref float pupilOutwardness = ref npc.localAI[1];
            ref float pupilScale = ref npc.localAI[2];
            ref float eyeAnimationFrameCounter = ref npc.localAI[3];

            // Adjust lifeMax
            if (npc.lifeMax != HeadLifemax)
            {
                npc.life = npc.lifeMax = HeadLifemax;
                npc.netUpdate = true;
            }

            // Kill and spawn true eyes
            if ((calamityGlobalNPC.newAI[0] == 1f || npc.life < 1700) && npc.Infernum().ExtraAI[2] != -2f)
            {
                SummonTrueEye(npc);
            }

            // Variables
            npc.dontTakeDamage = eyeAnimationFrameCounter >= 15f || calamityGlobalNPC.newAI[0] == 1f || (npc.ai[0] <= 1f && npc.life < 1800);

            // Enrage if the player leaves the arena
            bool enrage = core.Infernum().ExtraAI[0] == 0f;

            npc.velocity = Vector2.Zero;
            npc.Center = core.Center + new Vector2(0f, -400f);

            Vector2 ellipseVector = Utils.Vector2FromElipse(pupilAngle.ToRotationVector2(), EllipseSizeHead * pupilOutwardness);

            int idealFrame = 0;
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Invulnerablility state. Does nothing.
            if (npc.ai[0] >= 0f || attackState == MoonLordHeadAttackState.FreeEye)
            {
                if (attackState == MoonLordHeadAttackState.FreeEye)
                {
                    if (calamityGlobalNPC.newAI[0] != 1f)
                        calamityGlobalNPC.newAI[0] = 1f;

                    npc.life = npc.lifeMax;
                    npc.netUpdate = true;
                    npc.dontTakeDamage = true;

                    // Tentacle animation.
                    tentacleAnimationFrameCounter++;
                    if (tentacleAnimationFrameCounter >= 32f)
                        tentacleAnimationFrameCounter = 0f;
                    if (tentacleAnimationFrameCounter < 0f)
                        tentacleAnimationFrameCounter = 0f;
                }

                // Prepare for death.
                if (core.ai[0] == 2f)
                {
                    attackStateValue = (int)MoonLordHeadAttackState.Death;
                    return false;
                }
            }

            switch (attackState)
            {
                // Death effects.
                case MoonLordHeadAttackState.Death:
                    // Don't take damage, and snap neck by rotating the head pi/12 radians.
                    npc.dontTakeDamage = true;
                    npc.rotation = MathHelper.Lerp(npc.rotation, MathHelper.Pi / 12f, 0.07f);

                    // Tentacle animation.
                    tentacleAnimationFrameCounter++;
                    if (tentacleAnimationFrameCounter >= 32f)
                        tentacleAnimationFrameCounter = 0f;
                    if (tentacleAnimationFrameCounter < 0f)
                        tentacleAnimationFrameCounter = 0f;

                    if (pupilScale < 14f)
                        pupilScale += 1f;
                    break;
                case MoonLordHeadAttackState.DeathrayPupilAdjusetments:
                    idealFrame = 3;
                    npc.TargetClosest(false);

                    Vector2 distanceFromPlayer = Main.player[npc.target].Center - npc.Center + Vector2.UnitY * -22f;
                    pupilOutwardness = distanceFromPlayer.Length() / 500f;
                    pupilOutwardness = Utils.Clamp(2f * (1f - pupilOutwardness), 0f, 1f);

                    // Look in the direction of the player.
                    pupilAngle = distanceFromPlayer.ToRotation();

                    pupilScale = MathHelper.Lerp(pupilScale, 1f, 0.2f);

                    if (npc.Infernum().ExtraAI[1] >= 5f)
                    {
                        npc.Infernum().ExtraAI[1] = 0f;
                        attackStateValue = (int)MoonLordHeadAttackState.FireDeathray;
                    }
                    break;
                case MoonLordHeadAttackState.FireDeathray:
                    // Charge energy for a little bit.
                    if (npc.Infernum().ExtraAI[1] < 180f)
                    {
                        pupilOutwardness -= 0.05f;
                        if (pupilOutwardness < 0f)
                            pupilOutwardness = 0f;

                        if (npc.Infernum().ExtraAI[1] >= 60f)
                        {
                            int dustCount = 1;
                            if (npc.Infernum().ExtraAI[1] >= 120f)
                                dustCount = 2;

                            for (int i = 0; i < dustCount; i++)
                            {
                                float scale = 0.8f;
                                if (i % 2 == 1)
                                    scale = 1.65f;

                                Vector2 dustSpawnPosition = npc.Center + Main.rand.NextVector2CircularEdge(EllipseSizeHead.X, EllipseSizeHead.Y) * 0.5f;

                                Dust dust = Dust.NewDustDirect(dustSpawnPosition - Vector2.One * 8f, 16, 16, 229, npc.velocity.X / 2f, npc.velocity.Y / 2f, 0, default, 1f);
                                dust.velocity = Vector2.Normalize(npc.Center - dustSpawnPosition) * 3.5f * (10f - (dustCount - 1f) * 2f) / 10f;
                                dust.noGravity = true;
                                dust.scale = scale;
                                dust.customData = npc;
                            }
                        }
                    }
                    else if (npc.Infernum().ExtraAI[1] < 360f)
                    {
                        float laserTurnSpeed = 560f;
                        if (calamityGlobalNPC.newAI[0] == 1f)
                            laserTurnSpeed -= 60f;
                        if (enrage)
                            laserTurnSpeed /= 2f;
                        if (npc.Infernum().ExtraAI[1] == 180f && Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            npc.TargetClosest(false);
                            Vector2 angleVector = npc.DirectionTo(Main.player[npc.target].Center);

                            float angleSign = -1f;
                            if (angleVector.X < 0f)
                                angleSign = 1f;

                            angleVector = angleVector.RotatedBy(-angleSign * MathHelper.TwoPi / 6f);
                            int deathray = NewProjectileBetter(npc.Center, angleVector, ProjectileID.PhantasmalDeathray, LaserDamage, 0f, Main.myPlayer, angleSign * MathHelper.TwoPi / laserTurnSpeed, npc.whoAmI);
                            npc.ai[2] = deathray;
                            npc.netUpdate = true;
                        }

                        pupilOutwardness += 0.05f;
                        if (pupilOutwardness > 1f)
                            pupilOutwardness = 1f;

                        pupilAngle = currentlyFiredRay.velocity.ToRotation();
                    }
                    else
                    {
                        pupilOutwardness -= 0.07f;
                        if (pupilOutwardness < 0f)
                            pupilOutwardness = 0f;

                        idealFrame = 3;
                    }
                    if (npc.Infernum().ExtraAI[1] >= 375f)
                    {
                        npc.Infernum().ExtraAI[1] = 0f;
                        attackStateValue = (int)MoonLordHeadAttackState.Reset;
                    }
                    break;
                case MoonLordHeadAttackState.LookAtTarget:
                    idealFrame = 3;
                    pupilOutwardness = MathHelper.Lerp(pupilOutwardness, 0.7f, 0.2f);
                    pupilAngle = pupilAngle.AngleTowards(npc.AngleTo(Main.player[npc.target].Center), MathHelper.Pi / 12f);
                    if (npc.Infernum().ExtraAI[1] >= 360f)
                    {
                        npc.Infernum().ExtraAI[1] = 0f;
                        attackStateValue = (int)MoonLordHeadAttackState.Reset;
                    }
                    break;
                case MoonLordHeadAttackState.FirePhantasmalBolts:
                    if (npc.Infernum().ExtraAI[1] == 0f)
                    {
                        npc.TargetClosest(false);
                        npc.netUpdate = true;
                    }

                    Vector2 directionAheadOfPlayer = npc.DirectionTo(Main.player[npc.target].Center + Main.player[npc.target].velocity * 20f);
                    pupilAngle = pupilAngle.AngleLerp(directionAheadOfPlayer.ToRotation(), 0.5f);

                    // Cause the pupil to go outward.
                    pupilOutwardness += 0.05f;
                    if (pupilOutwardness > 1f)
                        pupilOutwardness = 1f;

                    if (npc.Infernum().ExtraAI[1] == 150f - 35f)
                        Main.PlaySound(SoundID.NPCDeath6, npc.position);

                    if ((npc.Infernum().ExtraAI[1] == 150f - 14f ||
                        npc.Infernum().ExtraAI[1] == 150f - 7f ||
                        npc.Infernum().ExtraAI[1] == 150f)
                        && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float boltSpeed = enrage ? 4.5f : 2.7f;
                        NewProjectileBetter(npc.Center + ellipseVector, directionAheadOfPlayer * boltSpeed, ProjectileID.PhantasmalBolt, BoltDamage, 0f, Main.myPlayer, 0f, 0f);
                    }
                    if (npc.Infernum().ExtraAI[1] >= 150f)
                    {
                        npc.Infernum().ExtraAI[1] = 0f;
                        attackStateValue = (int)MoonLordHeadAttackState.Reset;
                    }
                    break;
                case MoonLordHeadAttackState.Reset:
                    npc.Infernum().ExtraAI[0]++;
                    switch ((int)npc.Infernum().ExtraAI[0] % 12)
                    {
                        case 0:
                            attackStateValue = (int)MoonLordHeadAttackState.LookAtTarget;
                            break;
                        case 1:
                            attackStateValue = (int)MoonLordHeadAttackState.DeathrayPupilAdjusetments;
                            break;
                        case 2:
                            attackStateValue = (int)MoonLordHeadAttackState.FirePhantasmalBolts;
                            break;
                        case 3:
                            attackStateValue = (int)MoonLordHeadAttackState.LookAtTarget;
                            break;
                        case 4:
                            attackStateValue = (int)MoonLordHeadAttackState.FirePhantasmalBolts;
                            break;
                        case 5:
                            attackStateValue = (int)MoonLordHeadAttackState.FirePhantasmalBolts;
                            break;
                        case 6:
                            attackStateValue = NPC.AnyNPCs(NPCID.MoonLordFreeEye) ? (int)MoonLordHeadAttackState.LookAtTarget : (int)MoonLordHeadAttackState.DeathrayPupilAdjusetments;
                            break;
                        case 7:
                            attackStateValue = lifeRatio < 0.4f ? 3f : (int)MoonLordHeadAttackState.DeathrayPupilAdjusetments;
                            break;
                        case 8:
                            attackStateValue = (int)MoonLordHeadAttackState.DeathrayPupilAdjusetments;
                            break;
                        case 9:
                            attackStateValue = (int)MoonLordHeadAttackState.FirePhantasmalBolts;
                            break;
                        case 10:
                            attackStateValue = (int)MoonLordHeadAttackState.FirePhantasmalBolts;
                            break;
                        case 11:
                            attackStateValue = (int)MoonLordHeadAttackState.DeathrayPupilAdjusetments;
                            break;
                    }
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (Main.npc[i].type == NPCID.MoonLordHand && Main.npc[i].active)
                        {
                            Main.npc[i].ai[0] = npc.ai[0];
                            Main.npc[i].netSpam = 0;
                            Main.npc[i].netUpdate = true;
                        }
                    }
                    npc.netSpam = 0;
                    npc.netUpdate = true;
                    break;
            }

            npc.Infernum().ExtraAI[1]++;

            // Dictates whether this npc is vulnerable or not

            int frameCounterFromIdeal = idealFrame * 5;
            if (frameCounterFromIdeal > eyeAnimationFrameCounter)
                eyeAnimationFrameCounter += 1f;
            if (frameCounterFromIdeal < eyeAnimationFrameCounter)
                eyeAnimationFrameCounter -= 1f;

            if (eyeAnimationFrameCounter < 0f)
                pupilScale = 0f;
            if (eyeAnimationFrameCounter > 15f)
                pupilScale = 15f;

            return false;
        }

        [OverrideAppliesTo(NPCID.MoonLordHand, typeof(MoonLordAIClass), "MoonlordHandAI", EntityOverrideContext.NPCAI)]
        public static bool MoonlordHandAI(NPC npc)
        {
            // Adjust lifeMax
            if (npc.lifeMax != HandLifemax)
            {
                npc.life = npc.lifeMax = HandLifemax;
                npc.netUpdate = true;
            }
            CalamityGlobalNPC calamityGlobalNPC = npc.Calamity();
            // Hands life ratio
            NPC[] hands = Main.npc.Where(entity => entity.type == npc.type && entity.active).ToArray();
            float handsRatio = hands.Sum(entity => entity.life) / (float)hands.Sum(entity => entity.lifeMax);

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
            Vector2 ellipseVector = Utils.Vector2FromElipse(npc.localAI[0].ToRotationVector2(), EllipseSizeHand * npc.localAI[1]);

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
                int phantasmalEyeCount = 5;
                int modulo = 4;
                if (enrage)
                {
                    phantasmalEyeCount = 12;
                    modulo = 2;
                }

                if (npc.Infernum().ExtraAI[1] >= phantasmalEyeCount * modulo * 2)
                {
                    npc.localAI[1] -= 0.07f;
                    if (npc.localAI[1] < 0f)
                        npc.localAI[1] = 0f;
                }
                else if (npc.Infernum().ExtraAI[1] >= phantasmalEyeCount * modulo)
                {
                    npc.localAI[1] += 0.05f;
                    if (npc.localAI[1] > 0.75f)
                        npc.localAI[1] = 0.75f;

                    float pupilAngle = MathHelper.TwoPi * (npc.Infernum().ExtraAI[1] % (phantasmalEyeCount * modulo)) / (phantasmalEyeCount * modulo) - 1.57079637f;
                    npc.localAI[0] = (pupilAngle.ToRotationVector2() * ellipseVector).ToRotation();

                    if (npc.Infernum().ExtraAI[1] % modulo == 0f)
                    {
                        // Vector2 value11 = new Vector2(1f * -handSign, 3f);
                        // Let it also be known that some idiot was multiplying a value by 1.

                        Vector2 eyeSpawnDelta = new Vector2(1f * -handSign, 3f);
                        Vector2 eyeSpawnPos = npc.Center + Vector2.Normalize(ellipseVector) * ellipseVector.Length() * 0.4f + eyeSpawnDelta;
                        float velocity = BossRushEvent.BossRushActive ? 9f : 5.45f;
                        Vector2 eyeVelocity = Vector2.Normalize(ellipseVector) * velocity;
                        float ai = (MathHelper.TwoPi * (float)Main.rand.NextDouble() - MathHelper.Pi) / 30f + MathHelper.ToRadians(handSign);
                        NewProjectileBetter(eyeSpawnPos, eyeVelocity, ProjectileID.PhantasmalEye, EyeDamage, 0f, Main.myPlayer, 0f, ai);
                    }
                }
                else
                {
                    npc.localAI[1] += 0.02f;
                    if (npc.localAI[1] > 0.75f)
                        npc.localAI[1] = 0.75f;

                    float pupilAngle = MathHelper.TwoPi * (npc.Infernum().ExtraAI[1] % (phantasmalEyeCount * modulo)) / (phantasmalEyeCount * modulo) - 1.57079637f;
                    npc.localAI[0] = (pupilAngle.ToRotationVector2() * ellipseVector).ToRotation();
                }
                if (npc.Infernum().ExtraAI[1] >= phantasmalEyeCount * modulo + 10)
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
                        float velocityMult = BossRushEvent.BossRushActive ? 14f : 10f;
                        npc.velocity = Vector2.SmoothStep(npc.velocity, Vector2.Normalize(intialVelocity) * Math.Min(velocityMult, intialVelocity.Length()), 0.2f);
                    }
                }
                else if (npc.Infernum().ExtraAI[1] < 210f)
                {
                    idealFrame = 1;
                    int modifiedAICounter = (int)npc.Infernum().ExtraAI[1] - 30;
                    int modulo = enrage ? 10 : 60;
                    if (modifiedAICounter % modulo == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 sphereVelocity = new Vector2(5f * handSign, -8f);
                        int counterDivided = modifiedAICounter / 30;
                        sphereVelocity.X += (counterDivided - 3.5f) * handSign * 3f;
                        sphereVelocity.Y += (counterDivided - 4.5f) * 1f;
                        sphereVelocity *= 1.35f;
                        int idx = NewProjectileBetter(npc.Center, sphereVelocity, ProjectileID.PhantasmalSphere, SphereDamage, 1f, Main.myPlayer, 0f, npc.whoAmI);
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
                npc.localAI[0] = npc.localAI[0].AngleLerp(playerDistance.ToRotation(), 0.5f);

                npc.localAI[1] += 0.05f;
                if (npc.localAI[1] > 1f)
                    npc.localAI[1] = 1f;

                if (npc.Infernum().ExtraAI[1] == 70 - 35f)
                    Main.PlaySound(SoundID.NPCDeath6, npc.position);

                if ((npc.Infernum().ExtraAI[1] == 70 - 14f ||
                    npc.Infernum().ExtraAI[1] == 70 - 7f ||
                    npc.Infernum().ExtraAI[1] == 70) &&
                    Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float velocity = BossRushEvent.BossRushActive ? 4f : 2.7f;
                    if (enrage)
                        velocity *= 1.8f;
                    Vector2 boltVelocity = Vector2.Normalize(playerDistance) * velocity;
                    NewProjectileBetter(npc.Center + ellipseVector, boltVelocity, ProjectileID.PhantasmalBolt, BoltDamage, 0f, Main.myPlayer, 0f, 0f);
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

        [OverrideAppliesTo(NPCID.MoonLordCore, typeof(MoonLordAIClass), "MoonlordCoreAI", EntityOverrideContext.NPCAI)]
        public static bool MoonlordCoreAI(NPC npc)
        {
            // Adjust lifeMax
            CalamityMod.CalamityMod.StopRain();

            if (npc.lifeMax != CoreLifemax)
            {
                npc.life = npc.lifeMax = CoreLifemax;
                npc.netUpdate = true;
            }
            // Play a random Moon Lord sound
            if (npc.ai[0] != -1f && npc.ai[0] != 2f && Main.rand.NextBool(200))
                Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, Main.rand.Next(93, 100), 1f, 0f);

            if (npc.Infernum().arenaRectangle != null)
            {
                Rectangle rect = npc.Infernum().arenaRectangle;
                // 1 is normal. 0 is enraged.
                npc.Infernum().ExtraAI[0] =
                    Main.player[npc.target].Hitbox.Intersects(npc.Infernum().arenaRectangle).ToInt();
                npc.TargetClosest(false);
            }

            npc.dontTakeDamage = NPC.CountNPCS(NPCID.MoonLordFreeEye) >= 3;

            // Life Ratio
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Start the AI
            if (npc.localAI[3] == 0f)
            {
                npc.netUpdate = true;

                DeleteMLArena();
                Player closest = Main.player[Player.FindClosest(npc.Center, 1, 1)];
                if (npc.Infernum().arenaRectangle == null)
                {
                    npc.Infernum().arenaRectangle = default;
                }
                Point closestTileCoords = closest.Center.ToTileCoordinates();
                const int width = 200;
                const int height = 150;
                npc.Infernum().arenaRectangle = new Rectangle((int)closest.position.X - width * 8, (int)closest.position.Y - height * 8 + 20, width * 16, height * 16);
                const int standSpaceX = 70;
                const int standHeight = 19;
                for (int i = closestTileCoords.X - width / 2; i <= closestTileCoords.X + width / 2; i++)
                {
                    for (int j = closestTileCoords.Y - height / 2; j <= closestTileCoords.Y + height / 2; j++)
                    {
                        int iClipped = i - closestTileCoords.X + width / 2;
                        int jClipped = j - closestTileCoords.Y + height / 2;
                        bool withinArenaStand = iClipped > standSpaceX && iClipped < width - standSpaceX &&
                                                jClipped > height - standHeight;
                        if ((Math.Abs(closestTileCoords.X - i) == width / 2 ||
                            Math.Abs(closestTileCoords.Y - j) == height / 2 ||
                                withinArenaStand)
                            && !Main.tile[i, j].active())
                        {
                            Main.tile[i, j].type = (ushort)ModContent.TileType<Tiles.MoonlordArena>();
                            Main.tile[i, j].active(true);
                            if (Main.netMode == NetmodeID.Server)
                            {
                                NetMessage.SendTileSquare(-1, i, j, 1, TileChangeType.None);
                            }
                            else
                            {
                                WorldGen.SquareTileFrame(i, j, true);
                            }
                        }
                    }
                }
                npc.localAI[3] = 1f;
                npc.ai[0] = -1f;
            }

            // Death effects (Early)
            if (npc.ai[0] == -2f)
            {
                npc.ai[1] += 1f;
                if (npc.ai[1] == 30f)
                    Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, 92, 1f, 0f);

                if (npc.ai[1] < 60f)
                    MoonlordDeathDrama.RequestLight(npc.ai[1] / 30f, npc.Center);

                if (npc.ai[1] == 60f)
                {
                    npc.ai[1] = 0f;
                    npc.ai[0] = 0f;
                    if (Main.netMode != NetmodeID.MultiplayerClient && npc.type == NPCID.MoonLordCore)
                    {
                        npc.netUpdate = true;
                    }
                }
            }

            // Spawn head and hands
            if (npc.ai[0] == -1f)
            {
                npc.dontTakeDamage = true;

                npc.ai[1] += 1f;
                if (npc.ai[1] == 30f)
                    Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, 92, 1f, 0f);

                if (npc.ai[1] < 60f)
                    MoonlordDeathDrama.RequestLight(npc.ai[1] / 30f, npc.Center);

                if (npc.ai[1] == 60f)
                {
                    npc.ai[1] = 0f;
                    npc.ai[0] = 0f;

                    if (Main.netMode != NetmodeID.MultiplayerClient && npc.type == NPCID.MoonLordCore)
                    {
                        // Let it be known that some fool did this shit in the moon lord code
                        // npc.ai[2] = (float)Main.rand.Next(3);
                        // npc.ai[2] = 0f;
                        npc.ai[2] = 0f;

                        npc.netUpdate = true;
                        int[] bodyPartIndices = new int[3];

                        for (int i = 0; i < 2; i++)
                        {
                            int handIndex = NPC.NewNPC((int)npc.Center.X + i * 800 - 400, (int)npc.Center.Y - 100, NPCID.MoonLordHand, npc.whoAmI, 0f, 0f, 0f, 0f, 255);
                            Main.npc[handIndex].ai[2] = i;
                            Main.npc[handIndex].netUpdate = true;
                            bodyPartIndices[i] = handIndex;
                        }

                        int headIndex = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y - 400, NPCID.MoonLordHead, npc.whoAmI, 0f, 0f, 0f, 0f, 255);
                        Main.npc[headIndex].netUpdate = true;
                        bodyPartIndices[2] = headIndex;

                        for (int i = 0; i < 3; i++)
                        {
                            Main.npc[bodyPartIndices[i]].ai[3] = npc.whoAmI;
                        }
                        for (int i = 0; i < 3; i++)
                        {
                            npc.localAI[i] = bodyPartIndices[i];
                        }

                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            if (Main.npc[i].type == NPCID.MoonLordHand && Main.npc[i].active)
                            {
                                Main.npc[i].ai[0] = 0f;
                            }
                        }
                    }
                }
            }

            Vector2 idealDistance = Main.player[npc.target].Center - npc.Center + new Vector2(0f, 130f);

            // Fly near target, don't take damage
            if (npc.ai[0] == 0f)
            {
                npc.dontTakeDamage = true;
                npc.TargetClosest(false);

                if (idealDistance.Length() > 20f)
                {
                    float velocity = 9f;
                    if (Main.npc[(int)npc.localAI[2]].ai[0] == 1f)
                        velocity = 7f;

                    Vector2 desiredVelocity = Vector2.Normalize(idealDistance - npc.velocity) * velocity;
                    Vector2 oldVelocity = npc.velocity;
                    npc.SimpleFlyMovement(desiredVelocity, 0.5f);
                    npc.velocity = Vector2.Lerp(npc.velocity, oldVelocity, 0.5f);
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // Despawn if other parts aren't there
                    bool fuckingDie = false;
                    if (npc.localAI[0] < 0f || npc.localAI[1] < 0f || npc.localAI[2] < 0f)
                        fuckingDie = true;
                    else if (!Main.npc[(int)npc.localAI[0]].active || Main.npc[(int)npc.localAI[0]].type != NPCID.MoonLordHand)
                        fuckingDie = true;
                    else if (!Main.npc[(int)npc.localAI[1]].active || Main.npc[(int)npc.localAI[1]].type != NPCID.MoonLordHand)
                        fuckingDie = true;
                    else if (!Main.npc[(int)npc.localAI[2]].active || Main.npc[(int)npc.localAI[2]].type != NPCID.MoonLordHead)
                        fuckingDie = true;

                    if (fuckingDie)
                    {
                        npc.life = 0;
                        npc.HitEffect();
                        npc.active = false;
                    }

                    // Take damage if other parts are down
                    bool takeDamage = true;
                    if (Main.npc[(int)npc.localAI[0]].Calamity().newAI[0] != 1f)
                        takeDamage = false;
                    if (Main.npc[(int)npc.localAI[1]].Calamity().newAI[0] != 1f)
                        takeDamage = false;
                    if (Main.npc[(int)npc.localAI[2]].Calamity().newAI[0] != 1f)
                        takeDamage = false;

                    if (takeDamage)
                    {
                        npc.ai[0] = 1f;
                        npc.dontTakeDamage = false;
                        npc.netUpdate = true;
                    }
                }
            }

            // Fly near target, take damage
            else if (npc.ai[0] == 1f)
            {
                npc.dontTakeDamage = false;
                npc.TargetClosest(false);

                if (idealDistance.Length() > 20f)
                {
                    float velocity = 8f;
                    if (Main.npc[(int)npc.localAI[2]].ai[0] == 1f)
                        velocity = 6f;

                    Vector2 desiredVelocity2 = Vector2.Normalize(idealDistance - npc.velocity) * velocity;
                    Vector2 oldVelocity = npc.velocity;
                    npc.SimpleFlyMovement(desiredVelocity2, 0.5f);
                    npc.velocity = Vector2.Lerp(npc.velocity, oldVelocity, 0.5f);
                }
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
            }

            // Death effects
            else if (npc.ai[0] == 2f)
            {
                npc.dontTakeDamage = true;
                Vector2 idealVelocity = new Vector2(npc.direction, -0.5f);
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.98f);

                npc.ai[1] += 1f;
                if (npc.ai[1] < 60f)
                    MoonlordDeathDrama.RequestLight(npc.ai[1] / 60f, npc.Center);

                if (npc.ai[1] == 60f)
                {
                    for (int k = 0; k < Main.maxProjectiles; k++)
                    {
                        Projectile projectile = Main.projectile[k];
                        if (projectile.active && (projectile.type == ProjectileID.MoonLeech || projectile.type == ProjectileID.PhantasmalBolt ||
                            projectile.type == ProjectileID.PhantasmalDeathray || projectile.type == ProjectileID.PhantasmalEye ||
                            projectile.type == ProjectileID.PhantasmalSphere || projectile.type == ModContent.ProjectileType<PhantasmalBlast>() ||
                            projectile.type == ModContent.ProjectileType<PhantasmalSpark>()))
                            projectile.Kill();
                    }
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (Main.npc[i].type == NPCID.MoonLordFreeEye)
                        {
                            Main.npc[i].active = false;
                        }
                    }
                }

                if (npc.ai[1] % 3f == 0f && npc.ai[1] < 580f && npc.ai[1] > 60f)
                {
                    Vector2 spawnAdditive = Utils.RandomVector2(Main.rand, -1f, 1f);
                    if (spawnAdditive != Vector2.Zero)
                        spawnAdditive.Normalize();

                    spawnAdditive *= 20f + Main.rand.NextFloat() * 400f;
                    Vector2 dustSpawnPos = npc.Center + spawnAdditive;
                    Point dustSpawnTileCoords = dustSpawnPos.ToTileCoordinates();

                    bool canSpawnDust = true;
                    if (!WorldGen.InWorld(dustSpawnTileCoords.X, dustSpawnTileCoords.Y, 0))
                        canSpawnDust = false;
                    if (canSpawnDust && WorldGen.SolidTile(dustSpawnTileCoords.X, dustSpawnTileCoords.Y))
                        canSpawnDust = false;

                    float dustCounter = Main.rand.Next(6, 19);
                    float angularChange = MathHelper.TwoPi / dustCounter;
                    float rand2pi = MathHelper.TwoPi * Main.rand.NextFloat();
                    float velocityMult = 1f + Main.rand.NextFloat() * 2f;
                    float scale = 1f + Main.rand.NextFloat();
                    float fadeIn = 0.4f + Main.rand.NextFloat();
                    int dustType = Utils.SelectRandom(Main.rand, new int[]
                    {
                        31,
                        229
                    });
                    float ai1 = npc.ai[1];
                    if (canSpawnDust)
                    {
                        // MoonlordDeathDrama.AddExplosion(dustSpawnPos);
                        for (float i = 0f; i < dustCounter * 2f; i = ai1 + 1f)
                        {
                            Dust dust = Main.dust[Dust.NewDust(dustSpawnPos, 0, 0, 229, 0f, 0f, 0, default, 1f)];
                            dust.noGravity = true;
                            dust.position = dustSpawnPos;
                            dust.velocity = Vector2.UnitY.RotatedBy(rand2pi + angularChange * i) * velocityMult * (Main.rand.NextFloat() * 1.6f + 1.6f);
                            dust.fadeIn = fadeIn;
                            dust.scale = scale;
                            ai1 = i;
                        }
                    }

                    for (float i = 0f; i < npc.ai[1] / 60f; i = ai1 + 1f)
                    {
                        spawnAdditive = Utils.RandomVector2(Main.rand, -1f, 1f);
                        if (spawnAdditive != Vector2.Zero)
                            spawnAdditive.Normalize();

                        spawnAdditive *= 20f + Main.rand.NextFloat() * 800f;
                        dustSpawnPos = npc.Center + spawnAdditive;
                        dustSpawnTileCoords = dustSpawnPos.ToTileCoordinates();

                        bool canSpawndust = true;
                        if (!WorldGen.InWorld(dustSpawnTileCoords.X, dustSpawnTileCoords.Y, 0))
                            canSpawndust = false;
                        if (canSpawndust && WorldGen.SolidTile(dustSpawnTileCoords.X, dustSpawnTileCoords.Y))
                            canSpawndust = false;

                        if (canSpawndust)
                        {
                            Dust dust = Main.dust[Dust.NewDust(dustSpawnPos, 0, 0, dustType, 0f, 0f, 0, default, 1f)];
                            dust.noGravity = true;
                            dust.position = dustSpawnPos;
                            dust.velocity = -Vector2.UnitY * velocityMult * (Main.rand.NextFloat() * 0.9f + 1.6f);
                            dust.fadeIn = fadeIn;
                            dust.scale = scale;
                        }

                        ai1 = i;
                    }
                }

                if (npc.ai[1] % 15f == 0f && npc.ai[1] < 480f && npc.ai[1] >= 90f && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 spawnAdditive = Utils.RandomVector2(Main.rand, -1f, 1f);
                    if (spawnAdditive != Vector2.Zero)
                        spawnAdditive.Normalize();

                    spawnAdditive *= 20f + Main.rand.NextFloat() * 400f;
                    bool canSpawnDust = true;
                    Vector2 dustSpawnPos = npc.Center + spawnAdditive;
                    Point dustSpawnTileCoords = dustSpawnPos.ToTileCoordinates();

                    if (!WorldGen.InWorld(dustSpawnTileCoords.X, dustSpawnTileCoords.Y, 0))
                        canSpawnDust = false;
                    if (canSpawnDust && WorldGen.SolidTile(dustSpawnTileCoords.X, dustSpawnTileCoords.Y))
                        canSpawnDust = false;

                    if (canSpawnDust)
                    {
                        float smokeProjIndex = (Main.rand.Next(4) < 2).ToDirectionInt() * (MathHelper.Pi / 8f + (MathHelper.PiOver4 * Main.rand.NextFloat()));
                        Vector2 smokeVelocity = new Vector2(0f, -Main.rand.NextFloat() * 0.5f - 0.5f).RotatedBy(smokeProjIndex) * 6f;
                        Utilities.NewProjectileBetter(dustSpawnPos, smokeVelocity, ProjectileID.BlowupSmokeMoonlord, 0, 0f, Main.myPlayer, 0f, 0f);
                    }
                }

                if (npc.ai[1] == 1f)
                    Main.PlaySound(SoundID.NPCDeath61, npc.Center);

                if (npc.ai[1] >= 480f)
                    MoonlordDeathDrama.RequestLight((npc.ai[1] - 480f) / 120f, npc.Center);

                if (npc.ai[1] >= 600f)
                {
                    DeleteMLArena();
                    npc.life = 0;
                    npc.HitEffect(0, 1337.0);
                    npc.checkDead();

                    if (!BossRushEvent.BossRushActive)
                        typeof(CalamityGlobalAI).GetMethod("MoonLordLoot", Utilities.UniversalBindingFlags).Invoke(null, new object[]
                            {
                                npc
                            });

                    for (int npcIdx = 0; npcIdx < Main.maxNPCs; npcIdx++)
                    {
                        NPC npcFromArray = Main.npc[npcIdx];
                        if (npcFromArray.active && (npcFromArray.type == NPCID.MoonLordHand || npcFromArray.type == NPCID.MoonLordHead))
                        {
                            npcFromArray.active = false;
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npcFromArray.whoAmI, 0f, 0f, 0f, 0, 0, 0);
                        }
                    }

                    npc.active = false;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI, 0f, 0f, 0f, 0, 0, 0);

                    return false;
                }
            }

            // Despawn effects
            else if (npc.ai[0] == 3f)
            {
                npc.dontTakeDamage = true;
                Vector2 idealVelocity = new Vector2(npc.direction, -0.5f);
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.98f);

                npc.ai[1] += 1f;
                if (npc.ai[1] < 60f)
                    MoonlordDeathDrama.RequestLight(npc.ai[1] / 40f, npc.Center);

                if (npc.ai[1] == 40f)
                {
                    for (int projectileIdx = 0; projectileIdx < 1000; projectileIdx++)
                    {
                        Projectile projectile = Main.projectile[projectileIdx];
                        if (projectile.active && (projectile.type == ProjectileID.MoonLeech || projectile.type == ProjectileID.PhantasmalBolt ||
                            projectile.type == ProjectileID.PhantasmalDeathray || projectile.type == ProjectileID.PhantasmalEye ||
                            projectile.type == ProjectileID.PhantasmalSphere || projectile.type == ModContent.ProjectileType<PhantasmalBlast>() ||
                            projectile.type == ModContent.ProjectileType<PhantasmalSpark>()))
                        {
                            projectile.active = false;
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                                NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, projectileIdx, 0f, 0f, 0f, 0, 0, 0);
                        }
                    }
                    for (int goreIdx = 0; goreIdx < 500; goreIdx++)
                    {
                        Gore gore = Main.gore[goreIdx];
                        if (gore.active && gore.type >= 619 && gore.type <= 622)
                            gore.active = false;
                    }
                }

                if (npc.ai[1] >= 60f)
                {
                    for (int npcIdx = 0; npcIdx < Main.maxNPCs; npcIdx++)
                    {
                        NPC npcFromArray = Main.npc[npcIdx];
                        if (npcFromArray.active && (npcFromArray.type == NPCID.MoonLordHand || npcFromArray.type == NPCID.MoonLordHead))
                        {
                            npcFromArray.active = false;
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npcFromArray.whoAmI, 0f, 0f, 0f, 0, 0, 0);
                        }
                    }

                    DeleteMLArena();
                    npc.active = false;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI, 0f, 0f, 0f, 0, 0, 0);

                    NPC.LunarApocalypseIsUp = false;
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.WorldData, -1, -1, null, 0, 0f, 0f, 0f, 0, 0, 0);

                    return false;
                }
            }

            // Waves of seals
            if (npc.Infernum().ExtraAI[1] == 0f && lifeRatio < 0.7f)
            {
                for (int i = 0; i < 6; i++)
                {
                    float angle = MathHelper.TwoPi / 6f * i;
                    // Will be 0 or 1, the designated AI types for this wave
                    float ai0 = i % 2f;
                    int idx = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<EldritchSeal>(), 0, ai0, i / 2 * -25, 0f, npc.whoAmI);
                    Main.npc[idx].Infernum().ExtraAI[0] = angle;
                }
                npc.Infernum().ExtraAI[1] = 1f;
            }
            if (npc.Infernum().ExtraAI[1] == 1f && lifeRatio < 0.4f)
            {
                for (int i = 0; i < 9; i++)
                {
                    float angle = MathHelper.TwoPi / 9f * i;
                    float ai0 = i % 3;
                    int idx = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<EldritchSeal>(), 0, ai0, i / 2 * -30, 0f, npc.whoAmI);
                    Main.npc[idx].Infernum().ExtraAI[0] = angle;
                }
                npc.Infernum().ExtraAI[1] = 2f;
            }
            if (npc.Infernum().ExtraAI[1] == 2f && lifeRatio < 0.15f)
            {
                for (int i = 0; i < 9; i++)
                {
                    float angle = MathHelper.TwoPi / 9f * i;
                    float ai0 = i % 3;
                    int idx = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<EldritchSeal>(), 0, ai0, i / 2 * -30, 0f, npc.whoAmI);
                    Main.npc[idx].Infernum().ExtraAI[0] = angle;
                }
                for (int i = 0; i < 3; i++)
                {
                    int idx = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<EldritchSeal>(), 0, 3f, 0f, 0f, npc.whoAmI);
                    Main.npc[idx].life = Main.npc[idx].lifeMax = 9800;
                }
                npc.Infernum().ExtraAI[1] = 3f;
            }
            if (NPC.AnyNPCs(ModContent.NPCType<EldritchSeal>()))
            {
                npc.dontTakeDamage = true;
            }

            // Despawn
            bool despawn = false;
            if (npc.ai[0] == -2f || npc.ai[0] == -1f || npc.ai[0] == -2f || npc.ai[0] == 3f)
                despawn = true;
            if (Main.player[npc.target].active && !Main.player[npc.target].dead)
                despawn = true;

            // If unsure on despawning, check
            if (!despawn)
            {
                for (int playerIdx = 0; playerIdx < 255; playerIdx++)
                {
                    if (Main.player[playerIdx].active && !Main.player[playerIdx].dead)
                    {
                        despawn = true;
                        break;
                    }
                }
            }
            if (!despawn)
            {
                npc.ai[0] = 3f;
                npc.ai[1] = 0f;
                npc.netUpdate = true;
            }

            // Teleport
            if (npc.ai[0] >= 0f && npc.ai[0] < 2f && Main.netMode != NetmodeID.MultiplayerClient && npc.Distance(Main.player[npc.target].Center) > 2300f)
            {
                npc.ai[0] = -2f;
                npc.netUpdate = true;
                Vector2 teleportDelta = Main.player[npc.target].Center - Vector2.UnitY * 150f - npc.Center;
                npc.position += teleportDelta;

                if (Main.npc[(int)npc.localAI[0]].active)
                {
                    NPC nPC6 = Main.npc[(int)npc.localAI[0]];
                    nPC6.position += teleportDelta;
                    Main.npc[(int)npc.localAI[0]].netUpdate = true;
                }
                if (Main.npc[(int)npc.localAI[1]].active)
                {
                    NPC nPC6 = Main.npc[(int)npc.localAI[1]];
                    nPC6.position += teleportDelta;
                    Main.npc[(int)npc.localAI[1]].netUpdate = true;
                }
                if (Main.npc[(int)npc.localAI[2]].active)
                {
                    NPC nPC6 = Main.npc[(int)npc.localAI[2]];
                    nPC6.position += teleportDelta;
                    Main.npc[(int)npc.localAI[2]].netUpdate = true;
                }

                AffectAllEyes(eye => eye.Center = npc.Center);
            }
            return false;
        }

        [OverrideAppliesTo(NPCID.MoonLordFreeEye, typeof(MoonLordAIClass), "TrueEoCAI", EntityOverrideContext.NPCAI)]
        public static bool TrueEoCAI(NPC npc)
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
                if (GetTrueEyes.Length > 0)
                    groupIndex = (int)GetTrueEyes.Max(eye => eye.Infernum().ExtraAI[0]) + 1;
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
                AffectAllEyes(eye => eye.ai[0] = 0f);
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
                    npc.SimpleFlyMovement(npc.DirectionTo(player.Center + idealPosition) * maxVelocity, acceleration);

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
                            Vector2.Zero, ProjectileID.PhantasmalSphere, SphereDamage,
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
                        npc.velocity = npc.DirectionTo(Main.LocalPlayer.Center) * 12f;
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
                            npc.SimpleFlyMovement(npc.DirectionTo(idealPosition) * maxVelocity, acceleration);
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
                            int boltCount = enrage ? 25 : 12;
                            for (int i = 0; i < boltCount; i++)
                            {
                                float angle = MathHelper.TwoPi / boltCount * i + npc.Infernum().ExtraAI[1];
                                float velocityMultiplier = enrage ? 7f : 3f;
                                // Cause the bolt aimed at the player to go much faster than the other bolts
                                if (Math.Abs(angle - npc.Infernum().ExtraAI[1]) < 0.04f)
                                {
                                    velocityMultiplier = 9f;
                                }
                                NewProjectileBetter(npc.Center, angle.ToRotationVector2() * velocityMultiplier, ProjectileID.PhantasmalBolt, BoltDamage, 1f);
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
                            npc.SimpleFlyMovement(npc.DirectionTo(idealPosition) * maxVelocity, acceleration);
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
                                Utilities.NewProjectileBetter(npc.Center, npc.DirectionTo(npc.Infernum().angleTarget) * 15f * (enrage ? 1.4f : 1f), ModContent.ProjectileType<PhantasmalBlast>(), BlastDamage, 2.6f);
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
                            npc.SimpleFlyMovement(npc.DirectionTo(idealPosition) * maxVelocity, acceleration);
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
                                NewProjectileBetter(spawnPositon, npc.Infernum().ExtraAI[1].ToRotationVector2() * (enrage ? 3.5f : 2f), ProjectileID.PhantasmalBolt, BoltDamage, 1f);
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
                            npc.SimpleFlyMovement(npc.DirectionTo(idealPosition) * maxVelocity, 0.3f);
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
                                    ProjectileID.PhantasmalEye, EyeDamage, 1f);
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
                            npc.SimpleFlyMovement(npc.DirectionTo(idealPosition) * maxVelocity, 0.3f);
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
                                    ProjectileID.PhantasmalEye, EyeDamage, 1f);
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
                        const int blastCount = 6;
                        // Get into a general area of the target position
                        if (npc.ai[1] < floatTime + 2f * delay)
                        {
                            float signX = 1f;
                            if (Math.Abs(player.Center.X - xSeekPosition) < Math.Abs(player.Center.X + xSeekPosition))
                                signX = -1f;
                            signX *= -1f;
                            idealPosition = player.Center + new Vector2(signX * xSeekPosition, -480f);
                            npc.SimpleFlyMovement(npc.DirectionTo(idealPosition) * maxVelocity, 0.3f);
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
                                NewProjectileBetter(npc.Center, npc.DirectionTo(npc.Infernum().angleTarget) * 15f, ModContent.ProjectileType<PhantasmalBlast>(), BlastDamage, 2.6f);
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
                            npc.SimpleFlyMovement(npc.DirectionTo(idealPosition) * maxVelocity, 1.5f);
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
                                int idx = Utilities.NewProjectileBetter(npc.Center + fromEyeCoordinates, Vector2.UnitY, ModContent.ProjectileType<MoonlordPendulum>(), LaserDamage, 0f, 255, 0f, npc.whoAmI);
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
                                    Utilities.NewProjectileBetter(npc.Center, velocity, ProjectileID.PhantasmalBolt, BoltDamage, 1f);
                                }
                            }
                            // Spiral
                            if (npc.ai[1] >= 240f)
                            {
                                if (npc.ai[1] % 3f == 2f)
                                {
                                    npc.Infernum().ExtraAI[1] += MathHelper.ToRadians(6f);
                                    npc.localAI[0] = npc.Infernum().ExtraAI[1];
                                    Utilities.NewProjectileBetter(npc.Center, npc.Infernum().ExtraAI[1].ToRotationVector2() * 1.5f * (enrage ? 3f : 1f), ProjectileID.PhantasmalBolt, BoltDamage, 1f);
                                }
                            }
                        }
                        npc.SimpleFlyMovement(npc.DirectionTo(idealPosition) * maxVelocity, 0.5f);
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
        #endregion

        #region Draw Code

        [OverrideAppliesTo(NPCID.MoonLordFreeEye, typeof(MoonLordAIClass), "TrueEoCPreDraw", EntityOverrideContext.NPCPreDraw)]

#pragma warning disable IDE0060
        public static bool TrueEoCPreDraw(NPC npc, SpriteBatch spriteBatch, Color drawColor)
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
                return true;
            }
            return true;
        }
        #endregion

        #region True Eye Sync
        public static int[] GetTrueEyesIndex => Main.npc.Where(entity => entity.active && entity.type == NPCID.MoonLordFreeEye).Select(entity => entity.whoAmI).ToArray();
        public static NPC[] GetTrueEyes => Main.npc.Where(entity => entity.active && entity.type == NPCID.MoonLordFreeEye).Select(entity => Main.npc[entity.whoAmI]).ToArray();

        /// <summary>
        /// Causes a section of code to affect all true eyes.
        /// </summary>
        /// <param name="toExecute">The code to execute among all eyes</param>
        public static void AffectAllEyes(Action<NPC> toExecute)
        {
            foreach (var eye in GetTrueEyes)
            {
                toExecute(eye);
            }
        }

        /// <summary>
        /// Causes all true eyes to adjust their primary AI states. AI3 is not changed.
        /// </summary>
        /// <param name="toExecute">The npc from which we wish to derive</param>
        public static void EyeSyncVariables(NPC toCopy)
        {
            foreach (var eye in GetTrueEyes)
            {
                if (eye.whoAmI == toCopy.whoAmI)
                    continue;
                for (int i = 0; i <= 2; i++)
                {
                    eye.ai[i] = toCopy.ai[i];
                }
            }
        }
        #endregion

        #region Delete Arena
        public static void DeleteMLArena()
        {
            int surface = (int)Main.worldSurface;
            for (int i = 0; i < Main.maxTilesX; i++)
            {
                for (int j = 0; j < surface; j++)
                {
                    if (Main.tile[i, j] != null)
                    {
                        if (Main.tile[i, j].type == ModContent.TileType<Tiles.MoonlordArena>())
                        {
                            Main.tile[i, j] = new Tile();
                            if (Main.netMode == NetmodeID.Server)
                            {
                                NetMessage.SendTileSquare(-1, i, j, 1, TileChangeType.None);
                            }
                            else
                            {
                                WorldGen.SquareTileFrame(i, j, true);
                            }
                        }
                    }
                }
            }
        }
        #endregion
    }
}
