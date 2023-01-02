using CalamityMod.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.WallOfFlesh
{
    public class TileTentacle : ModProjectile
    {
        internal PrimitiveTrailCopy TentacleDrawer;
        internal Vector2 RestingSpot = -Vector2.One;
        internal Vector2[] ControlPoints = new Vector2[15];
        internal ref float Time => ref Projectile.ai[0];
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Tentacle");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            Projectile.timeLeft = 150;
        }

        public override void AI()
        {
            if (RestingSpot == -Vector2.One)
                RestingSpot = Projectile.Center;

            for (int i = 0; i < ControlPoints.Length; i++)
            {
                Vector2 basePosition = Vector2.Lerp(RestingSpot, Projectile.Center, i / (float)ControlPoints.Length);

                // Create an offset orthogonal to the direction to the sitting position.
                Vector2 offset = (RestingSpot - Projectile.Center).SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2) * 40f;

                // And make it sway a bit, like a flag.
                offset *= (float)Math.Sin(i / (float)ControlPoints.Length * MathHelper.TwoPi + Time / 12f);

                // Incorporate more sway the faster the tentacle is moving.
                offset *= Projectile.velocity.Length() * 0.05f;
                ControlPoints[i] = basePosition + offset;
            }

            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];

            if (Projectile.timeLeft > 60f)
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * 9f, 0.08f);
            else
            {
                Projectile.velocity.X *= 0.975f;

                if (Projectile.velocity.Y < 11f)
                    Projectile.velocity.Y += 0.18f;
            }

            if (!Projectile.WithinRange(RestingSpot, 600f))
                Projectile.Center = RestingSpot + Projectile.DirectionFrom(RestingSpot) * 600f;

            Time++;
        }

        internal Color ColorFunction(float completionRatio)
        {
            Color endColors = new(158, 48, 83);
            Color middleColor = new(184, 78, 113);
            Color witherColor = new(61, 28, 32);
            Color baseColor = Color.Lerp(endColors, middleColor, (float)Math.Abs(Math.Sin(completionRatio * MathHelper.Pi * 0.7f)));
            return Color.Lerp(baseColor, witherColor, Utils.GetLerpValue(60f, 0f, Projectile.timeLeft, true)) * Projectile.Opacity;
        }

        internal float WidthFunction(float completionRatio)
        {
            float widthCompletion = 1f;
            widthCompletion *= 1f - (float)Math.Pow(1f - Utils.GetLerpValue(0.04f, 0.3f, 1f - completionRatio, true), 2D);
            widthCompletion *= 1f - (float)Math.Pow(1f - Utils.GetLerpValue(0.96f, 0.9f, 1f - completionRatio, true), 2D);
            return MathHelper.Lerp(0f, 9f, widthCompletion) * Utils.GetLerpValue(0f, 60f, Projectile.timeLeft, true);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < ControlPoints.Length - 1; i++)
            {
                float _ = 0f;
                float width = WidthFunction(i / (float)ControlPoints.Length);
                Vector2 start = ControlPoints[i];
                Vector2 end = ControlPoints[i + 1];
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, width, ref _))
                    return true;
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (TentacleDrawer is null)
                TentacleDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.WoFTentacleVertexShader);

            InfernumEffectsRegistry.WoFTentacleVertexShader.UseColor(new Color(108, 23, 23));
            InfernumEffectsRegistry.WoFTentacleVertexShader.UseSecondaryColor(new Color(184, 78, 113));
            InfernumEffectsRegistry.WoFTentacleVertexShader.SetShaderTexture(ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin"));

            List<Vector2> points = new()
            {
                RestingSpot
            };
            points.AddRange(ControlPoints);
            points.Add(Projectile.Center);
            TentacleDrawer.Draw(new BezierCurve(points.ToArray()).GetPoints(20), -Main.screenPosition, 35);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI, List<int> overWiresUI)
        {
            drawCacheProjsBehindNPCs.Add(index);
        }
    }
}
