using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class MoonLordDeathAnimationHandler : ModProjectile
    {
        public PrimitiveTrailCopy LightDrawer = null;
        public ref float Owner => ref projectile.ai[0];
        public float AnimationTimer => Main.npc[(int)Owner].Infernum().ExtraAI[6];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Death Animation");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 2;
            projectile.ignoreWater = true;
            projectile.tileCollide = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 9000;
        }

        public override void AI()
        {
            if (!Main.npc.IndexInRange((int)Owner) || !Main.npc[(int)Owner].active)
            {
                projectile.Kill();
                return;
            }

            NPC core = Main.npc[(int)Owner];
            projectile.Center = core.Center;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.EnterShaderRegion();

            float deathAnimationTimer = AnimationTimer;
            float totalDeathRays = MathHelper.Lerp(0f, 8f, Utils.InverseLerp(0f, 180f, deathAnimationTimer, true));
            float rayExpandFactor = MathHelper.Lerp(1f, 2f, MathHelper.Clamp((deathAnimationTimer - 230f) / 90f, 0f, 1000f));

            for (int i = 0; i < (int)totalDeathRays; i++)
            {
                float rayAnimationCompletion = 1f;
                if (i == (int)totalDeathRays - 1f)
                    rayAnimationCompletion = totalDeathRays - (int)totalDeathRays;
                rayAnimationCompletion *= rayExpandFactor;

                ulong seed = (ulong)(i + 1) * 3141592uL;
                float rayDirection = MathHelper.TwoPi * i / 8f + (float)Math.Sin(Main.GlobalTime * (i + 1f) * 0.3f) * 0.51f;
                rayDirection += Main.GlobalTime * 0.48f;
                DrawLightRay(seed, rayDirection, rayAnimationCompletion, projectile.Center);
            }
            spriteBatch.ExitShaderRegion();

            spriteBatch.SetBlendState(BlendState.Additive);

            float coreBloomPower = Utils.InverseLerp(0f, 120f, deathAnimationTimer, true);

            // Create bloom on the core.
            if (coreBloomPower > 0f)
            {
                Texture2D bloomCircle = ModContent.GetTexture("CalamityMod/ExtraTextures/THanosAura");
                Vector2 drawPosition = projectile.Center - Main.screenPosition;
                Vector2 bloomSize = new Vector2(200f) / bloomCircle.Size() * (float)Math.Pow(coreBloomPower, 2D);
                bloomSize *= 1f + (rayExpandFactor - 1f) * 2f;

                spriteBatch.Draw(bloomCircle, drawPosition, null, Color.Turquoise * coreBloomPower, 0f, bloomCircle.Size() * 0.5f, bloomSize, 0, 0f);
            }

            spriteBatch.ResetBlendState();

            float giantTwinkleSize = Utils.InverseLerp(570f, 530f, deathAnimationTimer, true) * Utils.InverseLerp(450f, 510f, deathAnimationTimer, true);
            if (giantTwinkleSize > 0f)
            {
                float twinkleScale = giantTwinkleSize * 10f;
                Texture2D twinkleTexture = ModContent.GetTexture("InfernumMode/ExtraTextures/LargeStar");
                Vector2 drawPosition = projectile.Center - Main.screenPosition;
                float secondaryTwinkleRotation = Main.GlobalTime * 5.13f;

                spriteBatch.SetBlendState(BlendState.Additive);

                for (int i = 0; i < 2; i++)
                {
                    spriteBatch.Draw(twinkleTexture, drawPosition, null, Color.White, 0f, twinkleTexture.Size() * 0.5f, twinkleScale * new Vector2(1f, 1.85f), SpriteEffects.None, 0f);
                    spriteBatch.Draw(twinkleTexture, drawPosition, null, Color.White, secondaryTwinkleRotation, twinkleTexture.Size() * 0.5f, twinkleScale * new Vector2(1.3f, 1f), SpriteEffects.None, 0f);
                }
                spriteBatch.ResetBlendState();
            }

            return false;
        }

        public void DrawLightRay(ulong seed, float initialRayRotation, float rayBrightness, Vector2 rayStartingPoint)
        {
            // Parameters are not correctly passed into the delegates after the primitive drawer is created.
            // As a substitute, a direct NPC variable is used as storage to allow for access.
            projectile.Infernum().ExtraAI[8] = rayBrightness;

            float rayWidthFunction(float completionRatio, float rayBrightness2)
            {
                return MathHelper.Lerp(2f, 28f, completionRatio) * (1f + (rayBrightness2 - 1f) * 1.6f);
            }
            Color rayColorFunction(float completionRatio, float rayBrightness2)
            {
                return Color.White * projectile.Opacity * Utils.InverseLerp(0.8f, 0.5f, completionRatio, true) * MathHelper.Clamp(0f, 1.5f, rayBrightness2) * 0.6f;
            }

            if (LightDrawer is null)
                LightDrawer = new PrimitiveTrailCopy(c => rayWidthFunction(c, projectile.Infernum().ExtraAI[8]), c => rayColorFunction(c, projectile.Infernum().ExtraAI[8]), null, false);
            Vector2 currentRayDirection = initialRayRotation.ToRotationVector2();

            float length = MathHelper.Lerp(225f, 360f, Utils.RandomFloat(ref seed)) * rayBrightness;
            List<Vector2> points = new List<Vector2>();
            for (int i = 0; i <= 12; i++)
                points.Add(Vector2.Lerp(rayStartingPoint, rayStartingPoint + initialRayRotation.ToRotationVector2() * length, i / 12f));

            LightDrawer.Draw(points, -Main.screenPosition, 47);
        }
    }
}
