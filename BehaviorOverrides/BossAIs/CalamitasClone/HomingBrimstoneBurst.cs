using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.CalamitasClone
{
    public class HomingBrimstoneBurst : ModProjectile
    {
        public PrimitiveTrailCopy FireDrawer;
        public const int HomeTime = 90;
        public const int Lifetime = 360;
        public const int TimeBeforeSwirl = 170;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Brimstone Flame");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 24;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 18;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.Opacity = (float)Math.Sin(Projectile.timeLeft / (float)Lifetime * MathHelper.Pi) * 5f;
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;

            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, Color.Red.ToVector3() * 0.75f);

            if (Projectile.timeLeft >= Lifetime - HomeTime)
            {
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                float idealAngle = Projectile.AngleTo(target.Center);
                Projectile.velocity = Projectile.velocity.ToRotation().AngleLerp(idealAngle, 0.009f).ToRotationVector2() * Projectile.velocity.Length();
            }

            // Make an incomplete arc after a certain amount of time has passed.
            if (Projectile.timeLeft <= Lifetime - TimeBeforeSwirl)
                Projectile.velocity = Projectile.velocity.RotatedBy(MathHelper.TwoPi * 0.8f / (Lifetime - TimeBeforeSwirl));

            if (!Main.dedServ && Main.rand.NextBool(5))
            {
                Dust fire = Dust.NewDustPerfect(Projectile.Center, 267);
                fire.velocity = Main.rand.NextVector2CircularEdge(3f, 3f);
                fire.color = Color.Red;
                fire.scale *= 1.1f;
                fire.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < Projectile.oldPos.Length - 1; i++)
            {
                float _ = 0f;
                Vector2 top = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                Vector2 bottom = Projectile.oldPos[i + 1] + Projectile.Size * 0.5f;

                if (Projectile.oldPos[i] == Vector2.Zero || Projectile.oldPos[i + 1] == Vector2.Zero)
                    continue;

                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), top, bottom, 11f, ref _))
                    return true;
            }
            return false;
        }

        public float WidthFunction(float completionRatio)
        {
            float squeezeInterpolant = (float)Math.Pow(Utils.GetLerpValue(0f, 0.27f, completionRatio, true), 0.4f) * Utils.GetLerpValue(1f, 0.86f, completionRatio, true);
            return MathHelper.SmoothStep(3f, Projectile.width, squeezeInterpolant) * Projectile.Opacity;
        }

        public Color ColorFunction(float completionRatio)
        {
            Color color = Color.Lerp(Color.Red, Color.White, (float)Math.Pow(completionRatio, 2D));
            color *= (float)Math.Pow(1f - completionRatio, 2.5D);
            return color * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            FireDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.FireVertexShader);

            InfernumEffectsRegistry.FireVertexShader.UseSaturation(Projectile.velocity.Length() / 13f);
            InfernumEffectsRegistry.FireVertexShader.SetShaderTexture(InfernumTextureRegistry.CultistRayMap);
            FireDrawer.Draw(Projectile.oldPos, Projectile.Size * 0.5f - Main.screenPosition, 84);
            return false;
        }
    }
}
