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
    public class AnahitaBubble : ModProjectile
    {
        public PrimitiveTrailCopy WaterDrawer;
        public ref float Time => ref Projectile.ai[0];
        public const float Radius = 32f;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Bubble");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = (int)Radius;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 150;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.Opacity = (float)Math.Sin(Projectile.timeLeft / 150f * MathHelper.Pi) * 3f;
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;
            Projectile.scale = Projectile.Opacity;

            if (Projectile.timeLeft < 90)
                Projectile.velocity *= 0.98f;

            Time++;
        }

        public float WidthFunction(float completionRatio) => Radius * Projectile.scale * (float)Math.Sin(MathHelper.Pi * completionRatio);

        public Color ColorFunction(float completionRatio) => Color.Lerp(Color.DeepSkyBlue, Color.Turquoise, (float)Math.Abs(Math.Sin(completionRatio * MathHelper.Pi + Main.GlobalTimeWrappedHourly)) * 0.5f) * Projectile.Opacity;

        public override void Kill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 7; i++)
            {
                Vector2 shootVelocity = (MathHelper.TwoPi * i / 7f).ToRotationVector2() * 18f;
                if (BossRushEvent.BossRushActive)
                    shootVelocity *= 1.6f;

                Utilities.NewProjectileBetter(Projectile.Center, shootVelocity, ModContent.ProjectileType<WaterSpear>(), 155, 0f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (WaterDrawer is null)
                WaterDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["Infernum:DukeTornado"]);

            GameShaders.Misc["Infernum:DukeTornado"].UseImage("Images/Misc/Perlin");
            List<Vector2> drawPoints = new();

            for (float offsetAngle = -MathHelper.PiOver2; offsetAngle <= MathHelper.PiOver2; offsetAngle += MathHelper.Pi / 6f)
            {
                drawPoints.Clear();

                float adjustedAngle = offsetAngle + Main.GlobalTimeWrappedHourly * 2.2f;
                Vector2 offsetDirection = adjustedAngle.ToRotationVector2();
                Vector2 radius = Vector2.One * Radius;
                radius.Y *= MathHelper.Lerp(1f, 2f, (float)Math.Abs(Math.Cos(Main.GlobalTimeWrappedHourly * 1.9f)));

                for (int i = 0; i <= 8; i++)
                {
                    drawPoints.Add(Vector2.Lerp(Projectile.Center - offsetDirection * radius * 0.8f, Projectile.Center + offsetDirection * radius * 0.8f, i / 8f));
                }

                WaterDrawer.Draw(drawPoints, -Main.screenPosition, 42, adjustedAngle);
            }
            return false;
        }
    }
}
