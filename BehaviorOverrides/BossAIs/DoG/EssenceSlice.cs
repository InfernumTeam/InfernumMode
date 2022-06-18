using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
    public class EssenceSlice : ModProjectile
    {
        public PrimitiveTrailCopy FireDrawer;
        public const int Lifetime = 240;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Essence Slice");
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 12;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 42;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.MaxUpdates = 2;
            projectile.timeLeft = Lifetime * projectile.MaxUpdates;
            projectile.Calamity().canBreakPlayerDefense = true;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.Opacity = (float)Math.Sin(projectile.timeLeft / (float)Lifetime * MathHelper.Pi) * 5f;
            if (projectile.Opacity > 1f)
                projectile.Opacity = 1f;

            projectile.rotation = projectile.velocity.ToRotation();

            if (projectile.velocity.Length() < 50f)
                projectile.velocity *= 1.045f;

            Lighting.AddLight(projectile.Center, Color.Red.ToVector3() * 1.4f);

            if (!Main.dedServ && Main.rand.NextBool(5))
            {
                Dust fire = Dust.NewDustPerfect(projectile.Center, 27);
                fire.velocity = Main.rand.NextVector2CircularEdge(3f, 3f);
                fire.scale *= 1.1f;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < projectile.oldPos.Length - 1; i++)
            {
                float _ = 0f;
                Vector2 top = projectile.oldPos[i] + projectile.Size * 0.5f;
                Vector2 bottom = projectile.oldPos[i + 1] + projectile.Size * 0.5f;

                if (projectile.oldPos[i] == Vector2.Zero || projectile.oldPos[i + 1] == Vector2.Zero)
                    continue;

                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), top, bottom, 24f, ref _))
                    return true;
            }
            return false;
        }

        private float WidthFunction(float completionRatio)
        {
            float arrowheadCutoff = 0.36f;
            float width = 20f;
            float minHeadWidth = 0.02f;
            float maxHeadWidth = width;
            if (completionRatio <= arrowheadCutoff)
                width = MathHelper.Lerp(minHeadWidth, maxHeadWidth, Utils.InverseLerp(0f, arrowheadCutoff, completionRatio, true));
            return width;
        }

        private Color ColorFunction(float completionRatio)
        {
            float endFadeRatio = 0.41f;

            float completionRatioFactor = 2.7f;
            float globalTimeFactor = 5.3f;
            float endFadeFactor = 3.2f;
            float endFadeTerm = Utils.InverseLerp(0f, endFadeRatio * 0.5f, completionRatio, true) * endFadeFactor;
            float cosArgument = completionRatio * completionRatioFactor - Main.GlobalTime * globalTimeFactor + endFadeTerm;
            float startingInterpolant = (float)Math.Cos(cosArgument) * 0.5f + 0.5f;
            float opacity = Utils.InverseLerp(1f, 0.6f, completionRatio, true);

            float colorLerpFactor = 0.6f;
            Color startingColor = Color.Lerp(Color.Cyan, Color.DeepSkyBlue, startingInterpolant * colorLerpFactor);

            return Color.Lerp(startingColor, Color.White, MathHelper.SmoothStep(0f, 0.4f, Utils.InverseLerp(0f, endFadeRatio, completionRatio, true))) * opacity;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (FireDrawer is null)
                FireDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["Infernum:Fire"]);

            GameShaders.Misc["Infernum:Fire"].UseSaturation(0.7f);
            GameShaders.Misc["Infernum:Fire"].SetShaderTexture(ModContent.GetTexture("InfernumMode/ExtraTextures/CultistRayMap"));
            FireDrawer.Draw(projectile.oldPos, projectile.Size * 0.5f - Main.screenPosition, 23);
            return false;
        }
    }
}
