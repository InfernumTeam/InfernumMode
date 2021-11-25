using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class DarkEnergyBolt : ModProjectile
    {
        public PrimitiveTrailCopy TrailDrawer = null;
        public ref float Time => ref projectile.ai[0];
        public bool HasCollidedWithSomething
        {
            get => projectile.ai[1] == 1f;
            set => projectile.ai[1] = value.ToInt();
        }
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Energy Bolt");
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 20;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 52;
            projectile.ignoreWater = true;
            projectile.tileCollide = true;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.timeLeft = BossRushEvent.BossRushActive ? 120 : 240;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.InverseLerp(0f, 60f, Time, true);
            projectile.tileCollide = Time > 45f;

            // Thin out and disappear once collision has happened.
            if (projectile.timeLeft == 1 && HasCollidedWithSomething)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(projectile.Center, Vector2.Zero, ModContent.ProjectileType<MysteriousMatter>(), 250, 0f);

                projectile.Kill();
            }

            if (HasCollidedWithSomething)
                projectile.scale *= 0.9f;

            Time++;
        }

        public float PrimitiveWidthFunction(float completionRatio)
        {
            float arrowheadCutoff = 0.33f;
            float width = projectile.width;
            if (completionRatio <= arrowheadCutoff)
                width = MathHelper.Lerp(0.02f, width, Utils.InverseLerp(0f, arrowheadCutoff, completionRatio, true));
            return width * projectile.scale + 1f;
        }

        public Color PrimitiveColorFunction(float completionRatio)
        {
            Color shaderColor1 = Color.Lerp(Color.Black, Color.Purple, 0.35f);
            Color shaderColor2 = Color.Lerp(Color.Black, Color.Blue, 0.3f);

            float endFadeRatio = 0.9f;

            float endFadeTerm = Utils.InverseLerp(0f, endFadeRatio * 0.5f, completionRatio, true) * 3.2f;
            float sinusoidalTime = completionRatio * 2.7f - Main.GlobalTime * 5.3f + endFadeTerm;
            float startingInterpolant = (float)Math.Cos(sinusoidalTime) * 0.5f + 0.5f;

            float colorLerpFactor = 0.6f;
            Color startingColor = Color.Lerp(shaderColor1, shaderColor2, startingInterpolant * colorLerpFactor) * projectile.Opacity;
            return Color.Lerp(startingColor, Color.Transparent, MathHelper.SmoothStep(0f, 1f, Utils.InverseLerp(0f, endFadeRatio, completionRatio, true)));
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (TrailDrawer is null)
                TrailDrawer = new PrimitiveTrailCopy(PrimitiveWidthFunction, PrimitiveColorFunction, null, true, GameShaders.Misc["Infernum:TwinsFlameTrail"]);

            if (Time < 30f)
			{
                Vector2 start = projectile.Center;
                Vector2 end = projectile.Center + projectile.velocity.SafeNormalize(Vector2.Zero) * 5000f;
                spriteBatch.DrawLineBetter(start, end, Color.Purple, projectile.Opacity * 7.5f);
                return false;
			}

            GameShaders.Misc["Infernum:TwinsFlameTrail"].UseImage("Images/Misc/Perlin");
            Vector2[] drawPositions = new Vector2[]
            {
                projectile.oldPos.First(),
                projectile.oldPos.Last()
            };
            drawPositions = new Vector2[]
            {
                Vector2.Lerp(drawPositions.First(), drawPositions.Last(), 0f),
                Vector2.Lerp(drawPositions.First(), drawPositions.Last(), 0.5f),
                Vector2.Lerp(drawPositions.First(), drawPositions.Last(), 1f),
            };

            if (projectile.timeLeft >= 2)
                TrailDrawer.Draw(drawPositions, projectile.Size * 0.5f - Main.screenPosition, 43);

            // This state reset is necessary to ensure that the backbuffer is flushed immediately and the
            // trail is drawn before anything else. Not doing this may cause problems with vertex/index buffers down the line.
            spriteBatch.ResetBlendState();
            return false;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Time < 30f)
                return false;

            if (!HasCollidedWithSomething)
            {
                HasCollidedWithSomething = true;
                projectile.timeLeft = ProjectileID.Sets.TrailCacheLength[projectile.type] - 2;
                projectile.netUpdate = true;
            }
            return false;
        }

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
            if (Time < 30f)
                return false;

            for (int i = 0; i < projectile.oldPos.Length - 1; i++)
            {
                float _ = 0f;
                Vector2 top = projectile.oldPos[i] + projectile.Size * 0.5f;
                Vector2 bottom = projectile.oldPos[i + 1] + projectile.Size * 0.5f;

                if (projectile.oldPos[i] == Vector2.Zero || projectile.oldPos[i + 1] == Vector2.Zero)
                    continue;

                float width = PrimitiveWidthFunction(i / (float)(projectile.oldPos.Length - 2f));
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), top, bottom, width, ref _))
                    return true;
            }
            return false;
        }

		public override bool ShouldUpdatePosition() => !HasCollidedWithSomething && Time > 30f;
    }
}
