using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class MoonLordHandBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.MoonLordHand;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            // Disappear if the body is not present.
            if (!Main.npc.IndexInRange((int)npc.ai[3]) || !Main.npc[(int)npc.ai[3]].active)
            {
                npc.active = false;
                return false;
            }

            // Define the core NPC and inherit properties from it.
            NPC core = Main.npc[(int)npc.ai[3]];

            npc.target = core.target;
            npc.dontTakeDamage = false;

            int handSide = (npc.ai[2] == 1f).ToDirectionInt();
            float attackTimer = core.ai[1];
            Player target = Main.player[npc.target];
            ref float pupilRotation = ref npc.localAI[0];
            ref float pupilOutwardness = ref npc.localAI[1];
            ref float pupilScale = ref npc.localAI[2];

            int idealFrame = 0;

            switch ((MoonLordCoreBehaviorOverride.MoonLordAttackState)(int)core.ai[0])
            {
                case MoonLordCoreBehaviorOverride.MoonLordAttackState.PhantasmalSphereHandWaves:
                    DoBehavior_PhantasmalSphereHandWaves(npc, core, target, handSide, attackTimer, ref pupilRotation, ref pupilOutwardness, ref pupilScale, ref idealFrame);
                    break;
            }

            // Handle frames.
            int idealFrameCounter = idealFrame * 7;
            if (idealFrameCounter > npc.frameCounter)
                npc.frameCounter += 1D;
            if (idealFrameCounter < npc.frameCounter)
                npc.frameCounter -= 1D;
            npc.frameCounter = MathHelper.Clamp((float)npc.frameCounter, 0f, 21f);

            return false;
        }

        public static void DoBehavior_PhantasmalSphereHandWaves(NPC npc, NPC core, Player target, int handSide, float attackTimer, ref float pupilRotation, ref float pupilOutwardness, ref float pupilScale, ref int idealFrame)
        {
            int waveTime = 150;
            int sphereShootDelay = 36;
            int sphereShootRate = 12;
            int attackTransitionDelay = 40;
            float sphereShootSpeed = 8f;
            float sphereSlamSpeed = 6f;

            Vector2 startingIdealPosition = core.Center + new Vector2(handSide * 300f, -125f);
            Vector2 endingIdealPosition = core.Center + new Vector2(handSide * 750f, -70f);
            Vector2 idealPosition = Vector2.SmoothStep(startingIdealPosition, endingIdealPosition, MathHelper.Clamp(attackTimer / waveTime, 0f, 1f));
            idealPosition += (attackTimer / 16f).ToRotationVector2() * new Vector2(10f, 30f);

            Vector2 idealVelocity = Vector2.Zero.MoveTowards(idealPosition - npc.Center, 12f);
            npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.1f).MoveTowards(idealPosition, 0.4f);

            // Open the hand right before firing.
            if (attackTimer < sphereShootDelay - 12f || attackTimer >= waveTime)
            {
                pupilScale = MathHelper.Lerp(pupilScale, 0.3f, 0.1f);
                pupilOutwardness = MathHelper.Lerp(pupilOutwardness, 0f, 0.1f);
                idealFrame = 3;
            }
            else
            {
                pupilScale = MathHelper.Lerp(pupilScale, 0.75f, 0.1f);
                pupilOutwardness = MathHelper.Lerp(pupilOutwardness, 0.64f, 0.1f);
                pupilRotation = pupilRotation.AngleLerp(npc.AngleTo(target.Center), 0.1f);
                idealFrame = 0;
            }

            // Become invulnerable if the hand is closed.
            if (npc.frameCounter > 7)
                npc.dontTakeDamage = true;

            bool canShootPhantasmalSpheres = attackTimer % sphereShootRate == sphereShootRate - 1f;
            if (attackTimer < sphereShootDelay)
                canShootPhantasmalSpheres = false;
            if (attackTimer >= waveTime)
                canShootPhantasmalSpheres = false;

            if (canShootPhantasmalSpheres)
            {
                // TODO - Play some sort of sound to accompany the firing of the phantasmal spheres.

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float attackCompletion = Utils.InverseLerp(0f, waveTime, attackTimer, true);
                    float maximumAngularDisparity = startingIdealPosition.AngleBetween(endingIdealPosition);
                    float angularShootOffset = MathHelper.SmoothStep(0f, maximumAngularDisparity, attackCompletion) * handSide;
                    Vector2 sphereShootVelocity = -Vector2.UnitY.RotatedBy(angularShootOffset) * sphereShootSpeed;
                    int sphere = Utilities.NewProjectileBetter(npc.Center, sphereShootVelocity, ProjectileID.PhantasmalSphere, 215, 0f, npc.target);
                    if (Main.projectile.IndexInRange(sphere))
                    {
                        Main.projectile[sphere].ai[1] = npc.whoAmI;
                        Main.projectile[sphere].netUpdate = true;
                    }

                    // Sync the entire moon lord's current state. This will be executed on the frame immediately after this one.
                    core.netUpdate = true;
                }
            }

            // Slam all phantasmal spheres at the target after they have been fired.
            if (attackTimer == waveTime + 8f)
            {
                foreach (Projectile sphere in Utilities.AllProjectilesByID(ProjectileID.PhantasmalSphere))
                {
                    sphere.ai[0] = -1f;
                    sphere.velocity = sphere.SafeDirectionTo(target.Center) * sphereSlamSpeed;
                    sphere.tileCollide = true;
                    sphere.timeLeft = sphere.MaxUpdates * 270;
                    sphere.netUpdate = true;
                }
            }

            if (attackTimer >= waveTime + attackTransitionDelay)
                core.Infernum().ExtraAI[0] = 1f;
        }
    }
}
