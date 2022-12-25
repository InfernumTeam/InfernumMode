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
        
        public ref float Owner => ref Projectile.ai[0];

        public float AnimationTimer => Main.npc[(int)Owner].Infernum().ExtraAI[6];

        public override void SetStaticDefaults() => DisplayName.SetDefault("Death Animation");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 9000;
        }

        public override void AI()
        {
            if (!Main.npc.IndexInRange((int)Owner) || !Main.npc[(int)Owner].active)
            {
                Projectile.Kill();
                return;
            }

            NPC core = Main.npc[(int)Owner];
            Projectile.Center = core.Center;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.EnterShaderRegion();

            float deathAnimationTimer = AnimationTimer;
            float totalDeathRays = MathHelper.Lerp(0f, 8f, Utils.GetLerpValue(0f, 180f, deathAnimationTimer, true));
            float rayExpandFactor = MathHelper.Lerp(1f, 2f, MathHelper.Clamp((deathAnimationTimer - 230f) / 90f, 0f, 1000f));

            for (int i = 0; i < (int)totalDeathRays; i++)
            {
                float rayAnimationCompletion = 1f;
                if (i == (int)totalDeathRays - 1f)
                    rayAnimationCompletion = totalDeathRays - (int)totalDeathRays;
                rayAnimationCompletion *= rayExpandFactor;

                ulong seed = (ulong)(i + 1) * 3141592uL;
                float rayDirection = MathHelper.TwoPi * i / 8f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * (i + 1f) * 0.3f) * 0.51f;
                rayDirection += Main.GlobalTimeWrappedHourly * 0.48f;
                DrawLightRay(seed, rayDirection, rayAnimationCompletion, Projectile.Center);
            }
            Main.spriteBatch.ExitShaderRegion();

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            float coreBloomPower = Utils.GetLerpValue(0f, 120f, deathAnimationTimer, true);

            // Create bloom on the core.
            if (coreBloomPower > 0f)
            {
                Texture2D bloomCircle = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Thanatos/THanosAura").Value;
                Vector2 drawPosition = Projectile.Center - Main.screenPosition;
                Vector2 bloomSize = new Vector2(200f) / bloomCircle.Size() * (float)Math.Pow(coreBloomPower, 2D);
                bloomSize *= 1f + (rayExpandFactor - 1f) * 2f;

                Main.spriteBatch.Draw(bloomCircle, drawPosition, null, Color.Turquoise * coreBloomPower, 0f, bloomCircle.Size() * 0.5f, bloomSize, 0, 0f);
            }

            Main.spriteBatch.ResetBlendState();

            float giantTwinkleSize = Utils.GetLerpValue(570f, 530f, deathAnimationTimer, true) * Utils.GetLerpValue(450f, 510f, deathAnimationTimer, true);
            if (giantTwinkleSize > 0f)
            {
                float twinkleScale = giantTwinkleSize * 10f;
                Texture2D twinkleTexture = InfernumTextureRegistry.LargeStar.Value;
                Vector2 drawPosition = Projectile.Center - Main.screenPosition;
                float secondaryTwinkleRotation = Main.GlobalTimeWrappedHourly * 5.13f;

                Main.spriteBatch.SetBlendState(BlendState.Additive);

                for (int i = 0; i < 2; i++)
                {
                    Main.spriteBatch.Draw(twinkleTexture, drawPosition, null, Color.White, 0f, twinkleTexture.Size() * 0.5f, twinkleScale * new Vector2(1f, 1.85f), SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(twinkleTexture, drawPosition, null, Color.White, secondaryTwinkleRotation, twinkleTexture.Size() * 0.5f, twinkleScale * new Vector2(1.3f, 1f), SpriteEffects.None, 0f);
                }
                Main.spriteBatch.ResetBlendState();
            }

            return false;
        }

        public void DrawLightRay(ulong seed, float initialRayRotation, float rayBrightness, Vector2 rayStartingPoint)
        {
            // Parameters are not correctly passed into the delegates after the primitive drawer is created.
            // As a substitute, a direct NPC variable is used as storage to allow for access.
            Projectile.Infernum().ExtraAI[8] = rayBrightness;

            float rayWidthFunction(float completionRatio, float rayBrightness2)
            {
                return MathHelper.Lerp(2f, 28f, completionRatio) * (1f + (rayBrightness2 - 1f) * 1.6f);
            }
            Color rayColorFunction(float completionRatio, float rayBrightness2)
            {
                return Color.White * Projectile.Opacity * Utils.GetLerpValue(0.8f, 0.5f, completionRatio, true) * MathHelper.Clamp(0f, 1.5f, rayBrightness2) * 0.6f;
            }

            LightDrawer ??= new PrimitiveTrailCopy(c => rayWidthFunction(c, Projectile.Infernum().ExtraAI[8]), c => rayColorFunction(c, Projectile.Infernum().ExtraAI[8]), null, false);
            
            Vector2 currentRayDirection = initialRayRotation.ToRotationVector2();
            float length = MathHelper.Lerp(225f, 360f, Utils.RandomFloat(ref seed)) * rayBrightness;
            List<Vector2> points = new();
            for (int i = 0; i <= 12; i++)
                points.Add(Vector2.Lerp(rayStartingPoint, rayStartingPoint + initialRayRotation.ToRotationVector2() * length, i / 12f));

            LightDrawer.Draw(points, -Main.screenPosition, 47);
        }
    }
}
