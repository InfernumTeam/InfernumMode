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
                case MoonLordCoreBehaviorOverride.MoonLordAttackState.PhantasmalBoltEyeBursts:
                    DoBehavior_DefaultHandHover(npc, core, handSide, attackTimer, ref idealFrame);
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

        public static void DoBehavior_DefaultHandHover(NPC npc, NPC core, int handSide, float attackTimer, ref int idealFrame)
        {
            idealFrame = 3;
            npc.dontTakeDamage = true;

            Vector2 idealPosition = core.Center + new Vector2(handSide * 650f, -70f);
            idealPosition += (attackTimer / 16f).ToRotationVector2() * new Vector2(10f, 30f);

            Vector2 idealVelocity = Vector2.Zero.MoveTowards(idealPosition - npc.Center, 15f);
            npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.125f).MoveTowards(idealVelocity, 2f);
        }

        public static void DoBehavior_PhantasmalSphereHandWaves(NPC npc, NPC core, Player target, int handSide, float attackTimer, ref float pupilRotation, ref float pupilOutwardness, ref float pupilScale, ref int idealFrame)
        {
            int waveTime = 270;
            int sphereShootDelay = 36;
            int sphereShootRate = 12;
            int attackTransitionDelay = 40;
            float sphereShootSpeed = 13f;
            float sphereSlamSpeed = 6f;
            float handCloseInterpolant = Utils.InverseLerp(0f, 16f, attackTimer - waveTime, true);

            Vector2 startingIdealPosition = core.Center + new Vector2(handSide * 300f, -125f);
            Vector2 endingIdealPosition = core.Center + new Vector2(handSide * 750f, -70f);
            Vector2 idealPosition = Vector2.SmoothStep(startingIdealPosition, endingIdealPosition, MathHelper.Clamp(attackTimer / waveTime - handCloseInterpolant, 0f, 1f));
            idealPosition += (attackTimer / 16f).ToRotationVector2() * new Vector2(10f, 30f);

            Vector2 idealVelocity = Vector2.Zero.MoveTowards(idealPosition - npc.Center, 15f);
            npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.1f).MoveTowards(idealVelocity, 1.6f);

            // Open the hand right before firing.
            if (attackTimer < sphereShootDelay - 12f || attackTimer >= waveTime)
            {
                pupilScale = MathHelper.Lerp(pupilScale, 0.3f, 0.1f);
                pupilOutwardness = MathHelper.Lerp(pupilOutwardness, 0f, 0.1f);
                idealFrame = 3;

                // Shut hands faster after the spheres have been released.
                if (attackTimer >= waveTime && attackTimer < waveTime + 16f)
                    npc.frameCounter = MathHelper.Clamp((float)npc.frameCounter + 1f, 0f, 21f);
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
                Main.PlaySound(SoundID.Item122, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float attackCompletion = Utils.InverseLerp(0f, waveTime, attackTimer, true);
                    float maximumAngularDisparity = MathHelper.TwoPi / 3f;
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
            if (attackTimer == waveTime + 16f && handSide == 1)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    var sound = Main.PlaySound(SoundID.DD2_PhantomPhoenixShot, target.Center);
                    if (sound != null)
                    {
                        sound.Volume = MathHelper.Clamp(sound.Volume * 1.85f, 0f, 1f);
                        sound.Pitch = -0.5f;
                    }
                }
                    
                foreach (Projectile sphere in Utilities.AllProjectilesByID(ProjectileID.PhantasmalSphere))
                {
                    sphere.ai[0] = -1f;
                    sphere.velocity = sphere.SafeDirectionTo(target.Center) * sphereSlamSpeed;
                    sphere.tileCollide = Collision.CanHit(sphere.Center, 0, 0, target.Center, 0, 0);
                    sphere.timeLeft = sphere.MaxUpdates * 270;
                    sphere.netUpdate = true;
                }
            }

            if (attackTimer >= waveTime + attackTransitionDelay)
                core.Infernum().ExtraAI[0] = 1f;
        }

        public static void DoBehavior_PhantasmalSphereSlams(NPC npc, NPC core, Player target, int handSide, float attackTimer, ref float pupilRotation, ref float pupilOutwardness, ref float pupilScale, ref int idealFrame)
        {

        }
    }
}
