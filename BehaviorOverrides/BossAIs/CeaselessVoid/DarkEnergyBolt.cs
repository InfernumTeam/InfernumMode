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
        public ref float Time => ref Projectile.ai[0];
        public bool HasCollidedWithSomething
        {
            get => Projectile.ai[1] == 1f;
            set => Projectile.ai[1] = value.ToInt();
        }
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Energy Bolt");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 20;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 52;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = BossRushEvent.BossRushActive ? 120 : 240;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 60f, Time, true);
            Projectile.tileCollide = Time > 45f;

            // Thin out and disappear once collision has happened.
            if (Projectile.timeLeft == 1 && HasCollidedWithSomething)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<MysteriousMatter>(), 250, 0f);

                Projectile.Kill();
            }

            if (HasCollidedWithSomething)
                Projectile.scale *= 0.9f;

            Time++;
        }

        public float PrimitiveWidthFunction(float completionRatio)
        {
            float arrowheadCutoff = 0.33f;
            float width = Projectile.width;
            if (completionRatio <= arrowheadCutoff)
                width = MathHelper.Lerp(0.02f, width, Utils.GetLerpValue(0f, arrowheadCutoff, completionRatio, true));
            return width * Projectile.scale + 1f;
        }

        public Color PrimitiveColorFunction(float completionRatio)
        {
            Color shaderColor1 = Color.Lerp(Color.Black, Color.Purple, 0.35f);
            Color shaderColor2 = Color.Lerp(Color.Black, Color.Blue, 0.3f);

            float endFadeRatio = 0.9f;

            float endFadeTerm = Utils.GetLerpValue(0f, endFadeRatio * 0.5f, completionRatio, true) * 3.2f;
            float sinusoidalTime = completionRatio * 2.7f - Main.GlobalTimeWrappedHourly * 5.3f + endFadeTerm;
            float startingInterpolant = (float)Math.Cos(sinusoidalTime) * 0.5f + 0.5f;

            float colorLerpFactor = 0.6f;
            Color startingColor = Color.Lerp(shaderColor1, shaderColor2, startingInterpolant * colorLerpFactor) * Projectile.Opacity;
            return Color.Lerp(startingColor, Color.Transparent, MathHelper.SmoothStep(0f, 1f, Utils.GetLerpValue(0f, endFadeRatio, completionRatio, true)));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (TrailDrawer is null)
                TrailDrawer = new PrimitiveTrailCopy(PrimitiveWidthFunction, PrimitiveColorFunction, null, true, GameShaders.Misc["Infernum:TwinsFlameTrail"]);

            if (Time < 30f)
            {
                Vector2 start = Projectile.Center;
                Vector2 end = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * 5000f;
                spriteBatch.DrawLineBetter(start, end, Color.Purple, Projectile.Opacity * 7.5f);
                return false;
            }

            GameShaders.Misc["Infernum:TwinsFlameTrail"].UseImage("Images/Misc/Perlin");
            Vector2[] drawPositions = new Vector2[]
            {
                Projectile.oldPos.First(),
                Projectile.oldPos.Last()
            };
            drawPositions = new Vector2[]
            {
                Vector2.Lerp(drawPositions.First(), drawPositions.Last(), 0f),
                Vector2.Lerp(drawPositions.First(), drawPositions.Last(), 0.5f),
                Vector2.Lerp(drawPositions.First(), drawPositions.Last(), 1f),
            };

            if (Projectile.timeLeft >= 2)
                TrailDrawer.Draw(drawPositions, Projectile.Size * 0.5f - Main.screenPosition, 43);

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
                Projectile.timeLeft = ProjectileID.Sets.TrailCacheLength[Projectile.type] - 2;
                Projectile.netUpdate = true;
            }
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Time < 30f)
                return false;

            for (int i = 0; i < Projectile.oldPos.Length - 1; i++)
            {
                float _ = 0f;
                Vector2 top = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                Vector2 bottom = Projectile.oldPos[i + 1] + Projectile.Size * 0.5f;

                if (Projectile.oldPos[i] == Vector2.Zero || Projectile.oldPos[i + 1] == Vector2.Zero)
                    continue;

                float width = PrimitiveWidthFunction(i / (float)(Projectile.oldPos.Length - 2f));
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), top, bottom, width, ref _))
                    return true;
            }
            return false;
        }

        public override bool ShouldUpdatePosition() => !HasCollidedWithSomething && Time > 30f;
    }
}
