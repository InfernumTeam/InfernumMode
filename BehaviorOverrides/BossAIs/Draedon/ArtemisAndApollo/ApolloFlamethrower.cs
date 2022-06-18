using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.ArtemisAndApollo
{
    public class ApolloFlamethrower : ModProjectile
    {
        public NPC Owner => Main.npc[(int)projectile.ai[1]];
        public ref float Time => ref projectile.ai[0];
        public const float FlameRotation = MathHelper.Pi / 25f;
        public const float FadeinTime = 30f;
        public const float FadeoutTime = 45f;
        public const float Lifetime = FadeinTime + FadeoutTime;
        public const float FireMaxLength = 1950f;

        public override void SetStaticDefaults() => DisplayName.SetDefault("Exoplasma Flamethrower");

        public override void SetDefaults()
        {
            projectile.width = 30;
            projectile.height = 30;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            projectile.Center = Owner.Center + (Owner.rotation - MathHelper.PiOver2).ToRotationVector2() * 85f;
            projectile.rotation = Owner.AngleTo(projectile.Center);

            Vector2 flameDirection = projectile.rotation.ToRotationVector2();

            // Set the cast light color. It is primarily yellow with a hint of green.
            DelegateMethods.v3_1 = new Vector3(1.2f, 1f, 0.3f);
            float fadeIn = Time / FadeinTime;
            if (fadeIn > 1f)
                fadeIn = 1f;

            float fadeOut = (Time - FadeoutTime) / FadeinTime;
            if (fadeOut < 0f)
                fadeOut = 0f;

            // Release plasma sparks periodically.
            if (Main.netMode != NetmodeID.MultiplayerClient && Time % 6f == 5f)
            {
                Vector2 sparkShootVelocity = new Vector2(Owner.direction * -0.3f, -1f).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.77f);
                sparkShootVelocity = sparkShootVelocity.ToRotation().AngleLerp(-MathHelper.PiOver2, 0.75f).ToRotationVector2() * Main.rand.NextFloat(34f, 41f);
                Utilities.NewProjectileBetter(projectile.Center, sparkShootVelocity, ModContent.ProjectileType<ApolloFallingPlasmaSpark>(), 500, 0f);
            }

            // Cast lights.
            Utils.PlotTileLine(projectile.Center + flameDirection * FireMaxLength * fadeOut, projectile.Center + flameDirection * FireMaxLength * fadeIn, 16f, DelegateMethods.CastLight);
            Utils.PlotTileLine(projectile.Center + flameDirection.RotatedBy(FlameRotation) * FireMaxLength * fadeOut, projectile.Center + flameDirection.RotatedBy(FlameRotation) * FireMaxLength * fadeIn, 16f, DelegateMethods.CastLight);
            Utils.PlotTileLine(projectile.Center + flameDirection.RotatedBy(-FlameRotation) * FireMaxLength * fadeOut, projectile.Center + flameDirection.RotatedBy(-FlameRotation) * FireMaxLength * fadeIn, 16f, DelegateMethods.CastLight);

            // Create fire and smoke effects.
            if (fadeOut == 0f && fadeIn > 0.1f)
            {
                for (int i = 0; i < 3; i++)
                {
                    Dust fire = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 107, 0f, 0f, 0, default, 1f);
                    fire.fadeIn = 1.5f;
                    fire.velocity = flameDirection.RotatedBy(Main.rand.NextFloatDirection() * FlameRotation * 2f) * Main.rand.NextFloat(0.5f, 3f) * FireMaxLength / 27f;
                    fire.velocity += Owner.velocity * 2f;
                    fire.noLight = true;
                    fire.noGravity = true;
                    fire.alpha = 200;
                }
            }
            if (Main.rand.NextBool(5) && Time >= 15f)
            {
                Vector2 vector = projectile.Center + flameDirection * FireMaxLength * 0.75f + Main.rand.NextVector2Square(-20f, 20f);
                Gore smoke = Gore.NewGoreDirect(vector, Vector2.Zero, Main.rand.Next(61, 64), 0.5f);
                smoke.velocity *= 0.3f;
                smoke.velocity += flameDirection * 4f;
            }

            Dust smokeDust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 31, 0f, 0f, 0, default, 1f);
            smokeDust.fadeIn = 1.5f;
            smokeDust.scale = 0.4f;
            smokeDust.velocity = flameDirection.RotatedBy(Main.rand.NextFloatDirection() * MathHelper.Pi / 8f) * (0.5f + Main.rand.NextFloat() * 2.5f) * 15f;
            smokeDust.velocity += Owner.velocity * 2f;
            smokeDust.velocity *= 0.3f;
            smokeDust.noLight = true;
            smokeDust.noGravity = true;

            float smokeOffsetInterpolant = Main.rand.NextFloat();
            smokeDust.position = Vector2.Lerp(projectile.Center + flameDirection * FireMaxLength * fadeOut, projectile.Center + flameDirection * FireMaxLength * fadeIn, smokeOffsetInterpolant);
            smokeDust.position += flameDirection.RotatedBy(MathHelper.PiOver2) * (20f + 100f * (smokeOffsetInterpolant - 0.5f));

            // Update timers.
            Time++;
            projectile.frameCounter++;

            // Die after enough time has passed.
            if (Time >= Lifetime)
                projectile.Kill();
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Vector2 startOfFlame = projectile.Center - Main.screenPosition;
            float relativeFrameCompletion = projectile.frameCounter / 40f;
            Texture2D texture2D5 = Main.projectileTexture[projectile.type];
            Color flameDrawColor;
            Color startingFlameColor = new Color(255, 255, 255, 0);
            Color midFlameColor = new Color(167, 232, 30, 40);
            Color endFlameColor = new Color(0, 0, 0, 30);

            ulong flameDrawerSeed = (ulong)(projectile.identity + 958);

            int flameCount = (int)(FireMaxLength / 6f);
            for (float i = 0f; i < flameCount; i++)
            {
                float flameOffsetDirectionAngle = MathHelper.Lerp(-0.05f, 0.05f, Utils.RandomFloat(ref flameDrawerSeed));
                Vector2 flameDirection = (projectile.rotation + flameOffsetDirectionAngle).ToRotationVector2();
                Vector2 endOfFlame = startOfFlame + flameDirection * FireMaxLength;
                float flameDrawInterpolant = relativeFrameCompletion + i / flameCount;
                float flameRotation = projectile.rotation + MathHelper.Pi * (flameDrawInterpolant + Main.GlobalTime * 1.2f) * 0.1f + (int)(flameDrawInterpolant * flameCount) * MathHelper.Pi * 0.4f;
                flameDrawInterpolant %= 1f;

                if ((flameDrawInterpolant <= relativeFrameCompletion % 1f || projectile.frameCounter >= 40f) &&
                    (flameDrawInterpolant >= relativeFrameCompletion % 1f || projectile.frameCounter < 40f))
                {
                    if (flameDrawInterpolant < 0.1f)
                        flameDrawColor = Color.Lerp(Color.Transparent, startingFlameColor, Utils.InverseLerp(0f, 0.1f, flameDrawInterpolant, true));

                    else if (flameDrawInterpolant < 0.35f)
                        flameDrawColor = startingFlameColor;

                    else if (flameDrawInterpolant < 0.7f)
                        flameDrawColor = Color.Lerp(startingFlameColor, midFlameColor, Utils.InverseLerp(0.35f, 0.7f, flameDrawInterpolant, true));

                    else if (flameDrawInterpolant < 0.9f)
                        flameDrawColor = Color.Lerp(midFlameColor, endFlameColor, Utils.InverseLerp(0.7f, 0.9f, flameDrawInterpolant, true));

                    else if (flameDrawInterpolant < 1f)
                        flameDrawColor = Color.Lerp(endFlameColor, Color.Transparent, Utils.InverseLerp(0.9f, 1f, flameDrawInterpolant, true));

                    else
                        flameDrawColor = Color.Transparent;

                    float flameScale = (float)Math.Pow(MathHelper.Lerp(0.9f, 1.7f, flameDrawInterpolant), 2D) * 0.8f;

                    Vector2 currentFlameDrawPosition = Vector2.SmoothStep(startOfFlame, endOfFlame, flameDrawInterpolant);
                    Rectangle frame = texture2D5.Frame(1, 7, 0, (int)(flameDrawInterpolant * 7f));
                    Main.spriteBatch.Draw(texture2D5, currentFlameDrawPosition, frame, flameDrawColor, flameRotation, frame.Size() / 2f, flameScale, SpriteEffects.None, 0f);
                }
            }
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Time < 32f)
                return false;

            float completelyUselessFuckYouLmao = 0f;
            float fadeIn = projectile.ai[0] / 25f;
            if (fadeIn > 1f)
                fadeIn = 1f;

            float fadeOut = (projectile.ai[0] - FadeoutTime) / FadeinTime;
            if (fadeOut < 0f)
                fadeOut = 0f;

            Vector2 lineStart = projectile.Center + projectile.rotation.ToRotationVector2() * FireMaxLength * fadeOut;
            Vector2 lineEnd = projectile.Center + projectile.rotation.ToRotationVector2() * FireMaxLength * fadeIn;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), lineStart, lineEnd, projectile.scale * 66f, ref completelyUselessFuckYouLmao);
        }
    }
}
