using CalamityMod;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.MoonLord.MoonLordCoreBehaviorOverride;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.MoonLord
{
    public class MoonLordHandBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.MoonLordHand;

        public override int? NPCTypeToDeferToForTips => NPCID.MoonLordCore;

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

            int handSide = (npc.ai[2] == 1f).ToDirectionInt();
            bool hasPopped = npc.ai[0] == -2f;
            float attackTimer = core.ai[1];
            Player target = Main.player[npc.target];

            npc.dontTakeDamage = hasPopped;

            ref float pupilRotation = ref npc.localAI[0];
            ref float pupilOutwardness = ref npc.localAI[1];
            ref float pupilScale = ref npc.localAI[2];

            // Hacky workaround to problems with popping.
            // Does it still not work in multiplayer somehow? I don't give even the slighest of a fuck.
            if (npc.life < npc.lifeMax * 0.18)
                npc.life = (int)(npc.lifeMax * 0.18);

            int idealFrame = 0;

            switch ((MoonLordAttackState)(int)core.ai[0])
            {
                case MoonLordAttackState.PhantasmalSphereHandWaves:
                    if (!hasPopped)
                        DoBehavior_PhantasmalSphereHandWaves(npc, core, target, handSide, attackTimer, ref pupilRotation, ref pupilOutwardness, ref pupilScale, ref idealFrame);
                    break;
                case MoonLordAttackState.PhantasmalFlareBursts:
                    if (!hasPopped)
                        DoBehavior_PhantasmalFlareBursts(npc, core, target, handSide, attackTimer, ref pupilRotation, ref pupilOutwardness, ref pupilScale, ref idealFrame);
                    break;
                case MoonLordAttackState.ExplodingConstellations:
                    DoBehavior_ExplodingConstellations(npc, core, target, handSide, attackTimer, ref idealFrame);
                    break;
                default:
                    DoBehavior_DefaultHandHover(npc, core, handSide, attackTimer, ref idealFrame);
                    if (core.ai[0] == (int)MoonLordAttackState.PhantasmalSpin)
                    {
                        idealFrame = 0;
                        npc.dontTakeDamage = false;
                    }
                    break;
            }

            if (hasPopped)
            {
                npc.life = 1;

                DoBehavior_DefaultHandHover(npc, core, handSide, attackTimer, ref idealFrame);
                idealFrame = 0;
            }

            // Handle frames.
            int idealFrameCounter = idealFrame * 7;
            if (idealFrameCounter > npc.frameCounter)
                npc.frameCounter += 1D;
            if (idealFrameCounter < npc.frameCounter)
                npc.frameCounter -= 1D;
            npc.frameCounter = Clamp((float)npc.frameCounter, 0f, 21f);

            return false;
        }

        public static void DoBehavior_DefaultHandHover(NPC npc, NPC core, int handSide, float attackTimer, ref int idealFrame)
        {
            idealFrame = 3;
            npc.dontTakeDamage = true;

            Vector2 idealPosition = core.Center + new Vector2(handSide * 450f, -70f);
            idealPosition += (attackTimer / 32f + npc.whoAmI * 2.3f).ToRotationVector2() * 24f;

            Vector2 idealVelocity = Vector2.Zero.MoveTowards(idealPosition - npc.Center, 15f);
            npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.125f).MoveTowards(idealVelocity, 2f);
        }

        public static void DoBehavior_PhantasmalSphereHandWaves(NPC npc, NPC core, Player target, int handSide, float attackTimer, ref float pupilRotation, ref float pupilOutwardness, ref float pupilScale, ref int idealFrame)
        {
            int waveTime = 270;
            int sphereShootDelay = 36;
            int sphereShootRate = 8;
            int attackTransitionDelay = 40;
            float sphereShootSpeed = 12f;
            float sphereSlamSpeed = 6f;
            if (CurrentActiveArms <= 1)
            {
                sphereShootRate -= 4;
                sphereSlamSpeed += 3f;
            }
            if (IsEnraged)
            {
                sphereShootRate /= 2;
                sphereSlamSpeed += 7f;
            }

            float handCloseInterpolant = Utils.GetLerpValue(0f, 16f, attackTimer - waveTime, true);

            Vector2 startingIdealPosition = core.Center + new Vector2(handSide * 300f, -125f);
            Vector2 endingIdealPosition = core.Center + new Vector2(handSide * 750f, -70f);
            Vector2 idealPosition = Vector2.SmoothStep(startingIdealPosition, endingIdealPosition, Clamp(attackTimer / waveTime - handCloseInterpolant, 0f, 1f));
            idealPosition += (attackTimer / 16f).ToRotationVector2() * new Vector2(10f, 30f);

            Vector2 idealVelocity = Vector2.Zero.MoveTowards(idealPosition - npc.Center, 15f);
            npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.1f).MoveTowards(idealVelocity, 1.6f);

            // Open the hand right before firing.
            if (attackTimer < sphereShootDelay - 12f || attackTimer >= waveTime)
            {
                pupilScale = Lerp(pupilScale, 0.3f, 0.1f);
                pupilOutwardness = Lerp(pupilOutwardness, 0f, 0.1f);
                idealFrame = 3;

                // Shut hands faster after the spheres have been released.
                if (attackTimer >= waveTime && attackTimer < waveTime + 16f)
                    npc.frameCounter = Clamp((float)npc.frameCounter + 1f, 0f, 21f);
            }
            else
            {
                pupilScale = Lerp(pupilScale, 0.75f, 0.1f);
                pupilOutwardness = Lerp(pupilOutwardness, 0.5f, 0.1f);
                pupilRotation = pupilRotation.AngleLerp(npc.AngleTo(target.Center), 0.1f);
                idealFrame = 0;
            }

            // Become invulnerable if the hand is closed.
            if (npc.frameCounter > 7)
                npc.dontTakeDamage = true;

            bool canShootPhantasmalSpheres = true;
            if (attackTimer < sphereShootDelay)
                canShootPhantasmalSpheres = false;
            if (attackTimer >= waveTime)
                canShootPhantasmalSpheres = false;

            if (canShootPhantasmalSpheres)
            {
                float attackCompletion = Utils.GetLerpValue(0f, waveTime, attackTimer, true);
                float maximumAngularDisparity = TwoPi;
                float angularShootOffset = SmoothStep(0f, maximumAngularDisparity, attackCompletion) * -handSide;
                Vector2 sphereShootVelocity = -Vector2.UnitY.RotatedBy(angularShootOffset) * sphereShootSpeed;
                pupilRotation = sphereShootVelocity.ToRotation();

                if (attackTimer % sphereShootRate == sphereShootRate - 1f)
                {
                    SoundEngine.PlaySound(SoundID.Item122, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Utilities.NewProjectileBetter(npc.Center, sphereShootVelocity, ProjectileID.PhantasmalSphere, PhantasmalSphereDamage, 0f, -1, 0f, npc.whoAmI);

                        // Sync the entire moon lord's current state. This will be executed on the frame immediately after this one.
                        core.netUpdate = true;
                    }
                }
            }

            // Slam all phantasmal spheres at the target after they have been fired.
            if (attackTimer == waveTime + 16f && (handSide == 1 || CurrentActiveArms == 1))
            {
                if (Main.netMode != NetmodeID.Server)
                    SoundEngine.PlaySound(SoundID.DD2_PhantomPhoenixShot with { Volume = 1.85f, Pitch = -0.5f }, target.Center);

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
                core.Infernum().ExtraAI[5] = 1f;
        }

        public static void DoBehavior_PhantasmalFlareBursts(NPC npc, NPC core, Player target, int handSide, float attackTimer, ref float pupilRotation, ref float pupilOutwardness, ref float pupilScale, ref int idealFrame)
        {
            int flareCreationRate = 4;
            int flareTelegraphTime = 150;
            int flareReleaseDelay = 32;
            int flareShootTime = 60;
            float flareSpawnOffsetMax = 900f;
            if (IsEnraged)
            {
                flareCreationRate -= 2;
                flareSpawnOffsetMax += 400f;
            }

            idealFrame = 0;
            pupilRotation = pupilRotation.AngleLerp(0f, 0.1f);
            pupilOutwardness = Lerp(pupilOutwardness, 0f, 0.1f);
            pupilScale = Lerp(pupilScale, 0.35f, 0.1f);

            float handCloseInterpolant = Utils.GetLerpValue(0f, flareReleaseDelay, attackTimer - flareTelegraphTime, true);
            Vector2 startingIdealPosition = core.Center + new Vector2(handSide * 300f, -100f);
            Vector2 endingIdealPosition = core.Center + new Vector2(handSide * 750f, -150f);
            Vector2 idealPosition = Vector2.SmoothStep(startingIdealPosition, endingIdealPosition, Clamp(attackTimer / flareTelegraphTime - handCloseInterpolant, 0f, 1f));
            idealPosition += (attackTimer / 16f).ToRotationVector2() * 12f;

            Vector2 idealVelocity = Vector2.Zero.MoveTowards(idealPosition - npc.Center, 15f);
            npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.1f).MoveTowards(idealVelocity, 1.6f);

            // Create flare telegraphs.
            if (attackTimer < flareTelegraphTime && attackTimer % flareCreationRate == flareCreationRate - 1f && (handSide == 1 || CurrentActiveArms == 1))
            {
                SoundEngine.PlaySound(SoundID.Item72, target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 flareSpawnPosition = target.Center + Vector2.UnitX * Main.rand.NextFloatDirection() * flareSpawnOffsetMax;

                    int shootDelay = (int)(flareTelegraphTime - attackTimer + flareReleaseDelay);
                    bool telegraphPlaysSound = Main.rand.NextBool(8);
                    Utilities.NewProjectileBetter(flareSpawnPosition, Vector2.Zero, ModContent.ProjectileType<LunarFlareTelegraph>(), 0, 0f, -1, shootDelay, telegraphPlaysSound.ToInt());
                }
            }

            if (attackTimer >= flareTelegraphTime + flareReleaseDelay + flareShootTime)
                core.Infernum().ExtraAI[5] = 1f;
        }

        public static void DoBehavior_ExplodingConstellations(NPC npc, NPC core, Player target, int handSide, float attackTimer, ref int idealFrame)
        {
            idealFrame = 0;
            int initialAnimationTime = 54;
            int starCreationRate = 4;
            int totalStarsToCreate = 15;
            int explosionTime = 130;
            int constellationCount = 3;

            if (InFinalPhase)
            {
                starCreationRate--;
                totalStarsToCreate += 3;
            }

            if (IsEnraged)
            {
                starCreationRate = 2;
                explosionTime -= 50;
            }

            int starCreationTime = totalStarsToCreate * starCreationRate;
            float animationCompletionRatio = Clamp(attackTimer / initialAnimationTime, 0f, 1f);
            float wrappedAttackTimer = (attackTimer + (handSide == 0f ? 0f : 36f)) % (initialAnimationTime + starCreationTime + explosionTime);
            Vector2 startingIdealPosition = core.Center + new Vector2(handSide * 300f, -125f);
            Vector2 endingIdealPosition = core.Center + new Vector2(handSide * 450f, -350f);

            ref float constellationPatternType = ref npc.Infernum().ExtraAI[0];
            ref float constellationSeed = ref npc.Infernum().ExtraAI[1];

            // Create charge dust and close hands before the attack begins.
            if (wrappedAttackTimer < initialAnimationTime - 12f)
            {
                float chargePowerup = Utils.GetLerpValue(0f, 0.5f, animationCompletionRatio, true);
                int chargeDustCount = (int)Math.Round(Lerp(1f, 3f, chargePowerup));
                float chargeDustOffset = Lerp(30f, 75f, chargePowerup);

                for (int i = 0; i < chargeDustCount; i++)
                {
                    Vector2 chargeDustSpawnPosition = npc.Center + Main.rand.NextVector2CircularEdge(chargeDustOffset, chargeDustOffset) * Main.rand.NextFloat(0.8f, 1f);
                    Vector2 chargeDustVelocity = (npc.Center - chargeDustSpawnPosition) * 0.05f;
                    Dust electricity = Dust.NewDustPerfect(chargeDustSpawnPosition, 229);
                    electricity.velocity = chargeDustVelocity * Main.rand.NextFloat(0.9f, 1.1f);
                    electricity.scale = Lerp(1f, 1.45f, chargePowerup);
                    electricity.alpha = 84;
                    electricity.noGravity = true;
                }

                idealFrame = 3;
            }

            float hoverInterpolant = CalamityUtils.Convert01To010(animationCompletionRatio);
            Vector2 idealPosition = Vector2.SmoothStep(startingIdealPosition, endingIdealPosition, hoverInterpolant);
            idealPosition += (attackTimer / 16f).ToRotationVector2() * new Vector2(10f, 30f);

            Vector2 idealVelocity = Vector2.Zero.MoveTowards(idealPosition - npc.Center, 18f);
            npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.18f).MoveTowards(idealVelocity, 1.8f);

            // Determine what constellation pattern this arm will use. Each arm has their own pattern that they create.
            if (Main.netMode != NetmodeID.MultiplayerClient && wrappedAttackTimer == initialAnimationTime - 30f)
            {
                constellationSeed = Main.rand.NextFloat();
                constellationPatternType = Main.rand.Next(3);
                npc.netUpdate = true;
            }

            // Create stars.
            if (wrappedAttackTimer >= initialAnimationTime &&
                wrappedAttackTimer < initialAnimationTime + starCreationTime &&
                (wrappedAttackTimer - initialAnimationTime) % starCreationRate == 0f)
            {
                float patternCompletion = Utils.GetLerpValue(initialAnimationTime, initialAnimationTime + starCreationTime, wrappedAttackTimer, true);
                Vector2 currentPoint;
                switch ((int)constellationPatternType)
                {
                    // Diagonal stars from top left to bottom right.
                    case 0:
                        Vector2 startingPoint = target.Center + new Vector2(-800f, -600f);
                        Vector2 endingPoint = target.Center + new Vector2(800f, 600f);
                        currentPoint = Vector2.Lerp(startingPoint, endingPoint, patternCompletion);
                        break;

                    // Diagonal stars from top right to bottom left.
                    case 1:
                        startingPoint = target.Center + new Vector2(800f, -600f);
                        endingPoint = target.Center + new Vector2(-800f, 600f);
                        currentPoint = Vector2.Lerp(startingPoint, endingPoint, patternCompletion);
                        break;

                    // Horizontal sinusoid.
                    case 2:
                    default:
                        float horizontalOffset = Lerp(-775f, 775f, patternCompletion);
                        float verticalOffset = Cos(patternCompletion * Pi + constellationSeed * TwoPi) * 420f;
                        currentPoint = target.Center + new Vector2(horizontalOffset, verticalOffset);
                        break;
                }

                SoundEngine.PlaySound(SoundID.Item72, currentPoint);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(currentPoint, Vector2.Zero, ModContent.ProjectileType<StardustConstellation>(), 0, 0f, -1, (int)(patternCompletion * totalStarsToCreate), npc.whoAmI);
            }

            // Make all constellations spawned by this hand prepare to explode.
            if (wrappedAttackTimer == initialAnimationTime + starCreationTime)
            {
                foreach (Projectile star in Utilities.AllProjectilesByID(ModContent.ProjectileType<StardustConstellation>()).Where(p => p.ai[1] == npc.whoAmI))
                    star.timeLeft = 50;
            }

            if (attackTimer >= (initialAnimationTime + starCreationTime + explosionTime) * constellationCount - 1f)
                core.Infernum().ExtraAI[5] = 1f;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            // Hideous code from vanilla. Don't mind it too much.
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Vector2 shoulderOffset = new(220f, -60f);
            Texture2D armTexture = TextureAssets.Extra[15].Value;
            Vector2 coreCenter = Main.npc[(int)npc.ai[3]].Center;
            Point centerTileCoords = npc.Center.ToTileCoordinates();
            Color color = npc.GetAlpha(Color.Lerp(Lighting.GetColor(centerTileCoords.X, centerTileCoords.Y), Color.White, 0.3f));
            bool isLeftHand = npc.ai[2] == 0f;
            Vector2 directionThing = new((!isLeftHand).ToDirectionInt(), 1f);
            Vector2 handOrigin = new(120f, 180f);
            if (!isLeftHand)
                handOrigin.X = texture.Width - handOrigin.X;

            Texture2D scleraTexture = TextureAssets.Extra[17].Value;
            Texture2D pupilTexture = TextureAssets.Extra[19].Value;
            Vector2 scleraFrame = new(26f, 42f);
            if (!isLeftHand)
                scleraFrame.X = scleraTexture.Width - scleraFrame.X;

            Texture2D exposedEyeTexture = TextureAssets.Extra[26].Value;
            Rectangle exposedEyeFrame = exposedEyeTexture.Frame(1, 1, 0, 0);
            exposedEyeFrame.Height /= 4;
            Vector2 shoulderCenter = coreCenter + shoulderOffset * directionThing;
            Vector2 handBottom = npc.Center + new Vector2(0f, 76f);
            Vector2 v = (shoulderCenter - handBottom) * 0.5f;
            Vector2 armOrigin = new(60f, 30f);
            SpriteEffects direction = npc.ai[2] != 1f ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            if (!isLeftHand)
                armOrigin.X = armTexture.Width - armOrigin.X;

            float armAngularOffset = Acos(Clamp(v.Length() / 340f, 0f, 1f)) * -directionThing.X;
            float armRotation = v.ToRotation() + armAngularOffset - PiOver2;
            Main.spriteBatch.Draw(armTexture, handBottom - Main.screenPosition, null, color, armRotation, armOrigin, 1f, direction, 0f);
            if (npc.ai[0] == -2f)
            {
                int frame = (int)(Main.GlobalTimeWrappedHourly * 9.3f) % 4;
                exposedEyeFrame.Y += exposedEyeFrame.Height * frame;
                Vector2 exposedEyeDrawPosition = npc.Center - Main.screenPosition;
                Main.spriteBatch.Draw(exposedEyeTexture, exposedEyeDrawPosition, exposedEyeFrame, color, 0f, scleraFrame - new Vector2(4f, 4f), 1f, direction, 0f);
            }
            else
            {
                Vector2 scleraDrawPosition = npc.Center - Main.screenPosition;
                Main.spriteBatch.Draw(scleraTexture, scleraDrawPosition, null, Color.White * npc.Opacity * 0.6f, 0f, scleraFrame, 1f, direction, 0f);
                Vector2 pupilOffset = Utils.Vector2FromElipse(npc.localAI[0].ToRotationVector2(), new Vector2(30f, 66f) * npc.localAI[1]) + new Vector2(-directionThing.X, 3f);
                Main.spriteBatch.Draw(pupilTexture, npc.Center - Main.screenPosition + pupilOffset, null, Color.White * npc.Opacity * 0.6f, 0f, pupilTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            }
            Main.spriteBatch.Draw(texture, npc.Center - Main.screenPosition, npc.frame, color, 0f, handOrigin, 1f, direction, 0f);
            return false;
        }
    }
}
