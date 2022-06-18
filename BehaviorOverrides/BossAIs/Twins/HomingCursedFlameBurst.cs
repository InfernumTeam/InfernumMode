using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Twins
{
    public class HomingCursedFlameBurst : ModProjectile
    {
        public PrimitiveTrailCopy FireDrawer;
        public const int HomeTime = 90;
        public const int Lifetime = 360;
        public const int TimeBeforeSwirl = 170;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Cursed Flame");
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 34;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 24;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = Lifetime;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.Opacity = (float)Math.Sin(projectile.timeLeft / (float)Lifetime * MathHelper.Pi) * 5f;
            if (projectile.Opacity > 1f)
                projectile.Opacity = 1f;

            projectile.rotation = projectile.velocity.ToRotation();
            Lighting.AddLight(projectile.Center, Color.Green.ToVector3() * 1.45f);

            if (projectile.timeLeft >= Lifetime - HomeTime)
            {
                Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
                float idealAngle = projectile.AngleTo(target.Center);
                projectile.velocity = projectile.velocity.ToRotation().AngleLerp(idealAngle, 0.008f).ToRotationVector2() * projectile.velocity.Length();
            }

            // Make an incomplete arc after a certain amount of time has passed.
            if (projectile.timeLeft <= Lifetime - TimeBeforeSwirl)
                projectile.velocity = projectile.velocity.RotatedBy(MathHelper.TwoPi * 0.66f / (Lifetime - TimeBeforeSwirl));

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

                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), top, bottom, 11f, ref _))
                    return true;
            }
            return false;
        }

        public float WidthFunction(float completionRatio)
        {
            float squeezeInterpolant = (float)Math.Pow(Utils.InverseLerp(0f, 0.27f, completionRatio, true), 0.4f) * Utils.InverseLerp(1f, 0.86f, completionRatio, true);
            return MathHelper.SmoothStep(3f, projectile.width, squeezeInterpolant) * projectile.Opacity;
        }

        public Color ColorFunction(float completionRatio)
        {
            Color color = Color.Lerp(Color.GreenYellow, Color.White, (float)Math.Pow(completionRatio, 2D));
            color *= 1f - 0.67f * (float)Math.Pow(completionRatio, 3D);
            return color * projectile.Opacity;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (FireDrawer is null)
                FireDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["Infernum:Fire"]);

            GameShaders.Misc["Infernum:Fire"].UseSaturation(projectile.velocity.Length() / 13f);
            GameShaders.Misc["Infernum:Fire"].UseImage("Images/Misc/Perlin");
            FireDrawer.Draw(projectile.oldPos, projectile.Size * 0.5f - Main.screenPosition, 32);
            return false;
        }
    }
}
