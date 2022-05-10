using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.OldDuke
{
    public class AcidFountainBubble : ModProjectile
    {
        public PrimitiveTrailCopy WaterDrawer;
        public ref float Time => ref projectile.ai[0];
        public const float Radius = 24f;
        public override void SetStaticDefaults() => DisplayName.SetDefault("Acid Bubble");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = (int)Radius;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.timeLeft = 150;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.Opacity = (float)Math.Sin(MathHelper.Pi * projectile.timeLeft / 150f) * 4f;
            if (projectile.Opacity > 1f)
                projectile.Opacity = 1f;
            projectile.scale = projectile.Opacity;

            Time++;
        }

        public float WidthFunction(float completionRatio) => Radius * projectile.scale * (float)Math.Sin(MathHelper.Pi * completionRatio);

        public Color ColorFunction(float completionRatio)
        {
            float colorInterpolant = (float)Math.Pow(Math.Abs(Math.Sin(completionRatio * MathHelper.Pi + Main.GlobalTime)), 3D) * 0.5f;
            return Color.Lerp(new Color(140, 234, 87), new Color(144, 114, 166), colorInterpolant) * projectile.Opacity;
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 3; i++)
            {
                Vector2 shootVelocity = (MathHelper.TwoPi * i / 3f).ToRotationVector2() * 9f;
                Utilities.NewProjectileBetter(projectile.Center, shootVelocity, ModContent.ProjectileType<HomingAcid>(), 275, 0f);
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (WaterDrawer is null)
                WaterDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["Infernum:DukeTornado"]);

            GameShaders.Misc["Infernum:DukeTornado"].UseImage("Images/Misc/Perlin");
            List<Vector2> drawPoints = new List<Vector2>();

            for (float offsetAngle = -MathHelper.PiOver2; offsetAngle <= MathHelper.PiOver2; offsetAngle += MathHelper.Pi / 6f)
            {
                drawPoints.Clear();

                float adjustedAngle = offsetAngle + Main.GlobalTime * 2.2f;
                Vector2 offsetDirection = adjustedAngle.ToRotationVector2();
                Vector2 radius = Vector2.One * Radius;
                radius.Y *= MathHelper.Lerp(1f, 2f, (float)Math.Abs(Math.Cos(Main.GlobalTime * 1.9f)));

                for (int i = 0; i <= 8; i++)
                {
                    drawPoints.Add(Vector2.Lerp(projectile.Center - offsetDirection * radius * 0.8f, projectile.Center + offsetDirection * radius * 0.8f, i / 8f));
                }

                WaterDrawer.Draw(drawPoints, -Main.screenPosition, 42, adjustedAngle);
            }
            return false;
        }
    }
}
