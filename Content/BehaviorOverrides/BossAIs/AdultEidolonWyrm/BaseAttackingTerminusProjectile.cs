using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using InfernumMode.Common.Graphics;
using System;
using static CalamityMod.CalamityUtils;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public abstract class BaseAttackingTerminusProjectile : ModProjectile
    {
        public enum WingMotionState
        {
            RiseUpward,
            Flap
        }

        public struct XerocWing
        {
            public float WingRotation
            {
                get;
                set;
            }

            public float PreviousWingRotation
            {
                get;
                set;
            }

            public float WingRotationDifferenceMovingAverage
            {
                get;
                set;
            }

            public void Update(WingMotionState motionState, float animationCompletion, float instanceRatio)
            {
                PreviousWingRotation = WingRotation;

                switch (motionState)
                {
                    case WingMotionState.RiseUpward:
                        WingRotation = (-0.6f).AngleLerp(0.36f - instanceRatio * 0.15f, animationCompletion);
                        break;
                    case WingMotionState.Flap:
                        WingRotation = PiecewiseAnimation((animationCompletion + MathHelper.Lerp(instanceRatio, 0f, 0.5f)) % 1f, Anticipation, Flap, Rest, Recovery);
                        break;
                }

                WingRotationDifferenceMovingAverage = MathHelper.Lerp(WingRotationDifferenceMovingAverage, WingRotation - PreviousWingRotation, 0.15f);
            }
        }

        public Primitive3DStrip RuneStripDrawer
        {
            get;
            set;
        }

        public XerocWing[] Wings
        {
            get;
            set;
        }

        public abstract int WingCount { get; }

        public ref float RuneFadeInInterpolant => ref Projectile.localAI[0];

        public ref float WingsFadeInInterpolant => ref Projectile.localAI[1];

        // Piecewise function variables for determining the angular offset of wings when flapping.
        // Positive rotations = upward flaps.
        // Negative rotations = downward flaps.
        public static CurveSegment Anticipation => new(EasingType.PolyOut, 0f, -0.4f, 0.65f, 3);

        public static CurveSegment Flap => new(EasingType.PolyIn, 0.5f, Anticipation.EndingHeight(), -1.88f, 4);

        public static CurveSegment Rest => new(EasingType.PolyIn, 0.71f, Flap.EndingHeight(), 0.59f, 3);

        public static CurveSegment Recovery => new(EasingType.PolyIn, 0.9f, Rest.EndingHeight(), -0.4f - Rest.EndingHeight(), 2);

        public override string Texture => "CalamityMod/Items/SummonItems/Terminus";

        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.aiStyle = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 9600;
            Projectile.penetrate = -1;
            Wings = new XerocWing[WingCount];
        }

        public void UpdateWings(WingMotionState motionState, float animationCompletion)
        {
            for (int i = 0; i < Wings.Length; i++)
            {
                float instanceRatio = i / (float)Wings.Length;
                if (Wings.Length <= 1)
                    instanceRatio = 0f;

                Wings[i].Update(motionState, animationCompletion, instanceRatio);
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public virtual void DrawSelf(SpriteBatch spriteBatch)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            Color baseColor = Color.White * Projectile.Opacity;
            SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            // Draw wings behind the Terminus.
            for (int i = 0; i < Wings.Length; i++)
                DrawWings(drawPosition, Wings[i].WingRotation, Wings[i].WingRotationDifferenceMovingAverage, Projectile.rotation, WingsFadeInInterpolant);

            // Draw the Terminus.
            spriteBatch.Draw(texture, drawPosition, null, baseColor, Projectile.rotation, origin, Projectile.scale, direction, 0.4f);

            // Draw the 3D rune stripe.
            DrawRunes();
        }

        public float RuneHeightFunction(float _) => RuneFadeInInterpolant * 20f + 0.01f;

        public Color RuneColorFunction(float _) => Color.Lerp(Color.Pink, Color.Red, 0.4f) * RuneFadeInInterpolant;

        public void DrawRunes()
        {
            if (RuneFadeInInterpolant <= 0f)
                return;

            Main.spriteBatch.SetBlendState(BlendState.NonPremultiplied);

            Vector2 left = Projectile.Center - Vector2.UnitX * 60f - Main.screenPosition;
            Vector2 right = Projectile.Center + Vector2.UnitX * 60f - Main.screenPosition;
            RuneStripDrawer ??= new(RuneHeightFunction, RuneColorFunction);

            Main.instance.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            RuneStripDrawer.Draw(left, right, 0.3f, 4f, Main.GlobalTimeWrappedHourly * 2f);

            Main.spriteBatch.ExitShaderRegion();
        }

        public static void DrawWings(Vector2 drawPosition, float wingRotation, float rotationDifferenceMovingAverage, float generalRotation, float fadeInterpolant)
        {
            Main.spriteBatch.SetBlendState(BlendState.Additive);

            Texture2D wingsTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/AdultEidolonWyrm/TerminusWing").Value;
            Vector2 leftWingOrigin = wingsTexture.Size() * new Vector2(1f, 0.86f);
            Vector2 rightWingOrigin = leftWingOrigin;
            rightWingOrigin.X = wingsTexture.Width - rightWingOrigin.X;
            Color wingsDrawColor = Color.Lerp(Color.Transparent, Color.Wheat, fadeInterpolant);
            Color wingsDrawColorWeak = Color.Lerp(Color.Transparent, Color.Red * 0.4f, fadeInterpolant);

            // Wings become squished the faster they're moving, to give an illusion of 3D motion.
            float squishOffset = MathHelper.Min(0.7f, Math.Abs(rotationDifferenceMovingAverage) * 3.5f);

            // Draw multiple instances of the wings. This includes afterimages based on how quickly they're flapping.
            Vector2 scale = new Vector2(1f, 1f - squishOffset) * fadeInterpolant;
            for (int i = 4; i >= 0; i--)
            {
                // Make wings slightly brighter when they're moving at a fast angular pace.
                Color wingColor = Color.Lerp(wingsDrawColor, wingsDrawColorWeak, i / 4f) * Utils.Remap(rotationDifferenceMovingAverage, 0f, 0.04f, 0.66f, 0.75f);

                float rotationOffset = i * MathHelper.Min(rotationDifferenceMovingAverage, 0.16f) * (1f - squishOffset) * 0.5f;
                float currentWingRotation = wingRotation + rotationOffset;

                Main.spriteBatch.Draw(wingsTexture, drawPosition, null, wingColor, generalRotation + currentWingRotation, leftWingOrigin, scale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(wingsTexture, drawPosition, null, wingColor, generalRotation - currentWingRotation, rightWingOrigin, scale, SpriteEffects.FlipHorizontally, 0f);
            }

            Main.spriteBatch.ResetBlendState();
        }
    }
}
