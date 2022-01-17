using CalamityMod.Events;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Leviathan
{
    public class AnahitaExpandingBubble : ModProjectile
    {
        public PrimitiveTrailCopy WaterDrawer;
        public ref float Time => ref projectile.ai[0];
        public ref float Radius => ref projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Bubble");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = (int)Radius;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 240;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.Opacity = (float)Math.Sin(projectile.timeLeft / 240f * MathHelper.Pi) * 3f;
            if (projectile.Opacity > 1f)
                projectile.Opacity = 1f;
            projectile.scale = projectile.Opacity;

            if (projectile.timeLeft < 110)
                projectile.velocity *= 0.98f;

            Radius = MathHelper.Lerp(Radius, 700f, 0.0055f);

            Time++;
        }

        public float WidthFunction(float completionRatio) => Radius * projectile.scale * (float)Math.Sin(MathHelper.Pi * completionRatio);

        public Color ColorFunction(float completionRatio) => Color.Lerp(Color.DeepSkyBlue, Color.Turquoise, (float)Math.Abs(Math.Sin(completionRatio * MathHelper.Pi + Main.GlobalTime)) * 0.5f) * projectile.Opacity;

        public override void Kill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 18; i++)
            {
                Vector2 shootVelocity = (MathHelper.TwoPi * i / 18f).ToRotationVector2() * 13f;
                if (BossRushEvent.BossRushActive)
                    shootVelocity *= 2f;

                Utilities.NewProjectileBetter(projectile.Center, shootVelocity, ModContent.ProjectileType<FrostMist>(), 170, 0f);
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
