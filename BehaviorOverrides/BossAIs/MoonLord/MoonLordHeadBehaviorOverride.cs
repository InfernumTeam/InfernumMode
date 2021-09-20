using CalamityMod;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

using static InfernumMode.Utilities;
using CalamityMod.NPCs;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class MoonLordHeadBehaviorOverride : NPCBehaviorOverride
    {
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

        public const int HeadLifeMax = 52525;
        public override int NPCOverrideType => NPCID.MoonLordHead;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

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

        public override bool PreAI(NPC npc)
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
            if (npc.lifeMax != HeadLifeMax)
            {
                npc.life = npc.lifeMax = HeadLifeMax;
                npc.netUpdate = true;
            }

            // Kill and spawn true eyes
            if ((calamityGlobalNPC.newAI[0] == 1f || npc.life < 1700) && npc.Infernum().ExtraAI[2] != -2f)
                MoonLordHandBehaviorOverride.SummonTrueEye(npc);

            // Variables
            npc.dontTakeDamage = eyeAnimationFrameCounter >= 15f || calamityGlobalNPC.newAI[0] == 1f || (npc.ai[0] <= 1f && npc.life < 1800);

            // Enrage if the player leaves the arena
            bool enrage = core.Infernum().ExtraAI[0] == 0f;

            npc.velocity = Vector2.Zero;
            npc.Center = core.Center + new Vector2(0f, -400f);

            Vector2 ellipseVector = Utils.Vector2FromElipse(pupilAngle.ToRotationVector2(), new Vector2(27f, 59f) * pupilOutwardness);

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

                                Vector2 dustSpawnPosition = npc.Center + Main.rand.NextVector2CircularEdge(27f, 59f) * 0.5f;

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
                            int deathray = NewProjectileBetter(npc.Center, angleVector, ProjectileID.PhantasmalDeathray, 425, 0f, Main.myPlayer, angleSign * MathHelper.TwoPi / laserTurnSpeed, npc.whoAmI);
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
                        NewProjectileBetter(npc.Center + ellipseVector, directionAheadOfPlayer * boltSpeed, ProjectileID.PhantasmalBolt, 185, 0f, Main.myPlayer, 0f, 0f);
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
    }
}
