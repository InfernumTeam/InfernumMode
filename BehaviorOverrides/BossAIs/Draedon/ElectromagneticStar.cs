using CalamityMod;
using CalamityMod.NPCs.AstrumDeus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
    public class ElectromagneticStar : ModProjectile
    {
        public PrimitiveTrailCopy FireDrawer;
        public ref float Time => ref projectile.ai[0];
        public ref float Radius => ref projectile.ai[1];
        public const int ChargeupTime = 210;
        public const int AttackTime = 180;
        public override void SetStaticDefaults() => DisplayName.SetDefault("Electromagnetic Exoplasma Star");

        public override void SetDefaults()
        {
            projectile.width = 164;
            projectile.height = 164;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.timeLeft = ChargeupTime + AttackTime;
            projectile.scale = 0.2f;
            projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            Radius = projectile.scale * 72f;

            // Play a telegraph sound prior to moving.
            if (Time == AttackTime - 90f)
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/CrystylCharge"), projectile.Center);

            if (projectile.timeLeft < 60f)
            {
                projectile.scale += 0.32f;
                projectile.velocity *= 0.95f;
                projectile.Opacity = projectile.timeLeft / 60f;
                Main.LocalPlayer.Infernum().CurrentScreenShakePower = Utils.InverseLerp(18f, 8f, projectile.timeLeft, true) * 12f;
            }

            if (Time <= ChargeupTime)
            {
                projectile.Opacity = Time / ChargeupTime;
                projectile.scale = MathHelper.Lerp(0.2f, 4f, projectile.Opacity);
            }
            else if (projectile.timeLeft > 60f)
            {
                Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
                float idealSpeed = projectile.Distance(target.Center) * 0.01f + 10f;
                projectile.velocity = (projectile.velocity * 44f + projectile.SafeDirectionTo(target.Center) * idealSpeed) / 45f;
                projectile.velocity = projectile.velocity.SafeNormalize(Vector2.UnitY) * idealSpeed;
            }

            Time++;
        }

        public float SunWidthFunction(float completionRatio) => Radius * (float)Math.Sin(MathHelper.Pi * completionRatio);

        public Color SunColorFunction(float completionRatio)
        {
            Color teslaColor = Color.Cyan;
            Color plasmaColor = Color.Lime;
            float teslaInterpolant = (float)Math.Pow(CalamityUtils.Convert01To010(completionRatio), 3D);
            Color color = Color.Lerp(plasmaColor, teslaColor, teslaInterpolant) * projectile.Opacity;
            color.A /= 2;
            return color;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (FireDrawer is null)
                FireDrawer = new PrimitiveTrailCopy(SunWidthFunction, SunColorFunction, null, true, GameShaders.Misc["Infernum:Fire"]);

            GameShaders.Misc["Infernum:Fire"].UseSaturation(0.45f);
            GameShaders.Misc["Infernum:Fire"].UseImage("Images/Misc/Perlin");

            List<float> rotationPoints = new List<float>();
            List<Vector2> drawPoints = new List<Vector2>();

            for (float offsetAngle = -MathHelper.PiOver2; offsetAngle <= MathHelper.PiOver2; offsetAngle += MathHelper.Pi / 60f)
            {
                rotationPoints.Clear();
                drawPoints.Clear();

                float adjustedAngle = offsetAngle + MathHelper.Pi * -0.27f;
                Vector2 offsetDirection = adjustedAngle.ToRotationVector2();
                for (int i = 0; i < 16; i++)
                {
                    rotationPoints.Add(adjustedAngle);
                    drawPoints.Add(Vector2.Lerp(projectile.Center - offsetDirection * Radius / 2f, projectile.Center + offsetDirection * Radius / 2f, i / 16f));
                }

                FireDrawer.Draw(drawPoints, -Main.screenPosition, 12);
            }
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => Utilities.CircularCollision(projectile.Center, targetHitbox, Radius * 0.85f) && Time >= ChargeupTime + 45f;
    }
}
