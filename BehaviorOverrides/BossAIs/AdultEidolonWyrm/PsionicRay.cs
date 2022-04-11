using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class PsionicRay : ModProjectile
    {
        public PrimitiveTrailCopy RayDrawer;
        public bool HasHitAnObstacle
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value.ToInt();
        }
        public const int Lifetime = 240;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Psionic Ray");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 24;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 26;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override void AI()
        {
            Projectile.Opacity = (float)Math.Sin(Projectile.timeLeft / (float)Lifetime * MathHelper.Pi) * 5f;
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;

            // Die if the last and current position have caught up if an obstacle has been hit.
            if (HasHitAnObstacle && Projectile.oldPos[0] == Projectile.oldPos.Last())
                Projectile.Kill();

            Projectile.rotation = Projectile.velocity.ToRotation();

            // Accelerate quickly until reaching a specific speed.
            if (Projectile.velocity.Length() < 16f)
                Projectile.velocity *= 1.064f;

            Lighting.AddLight(Projectile.Center, Color.Cyan.ToVector3() * 1.6f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < Projectile.oldPos.Length / 3; i++)
            {
                float _ = 0f;
                Vector2 top = Projectile.oldPos[i] + Projectile.Size * 0.8f;
                Vector2 bottom = Projectile.oldPos[i + 1] + Projectile.Size * 0.8f;
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), top, bottom, 8f, ref _))
                    return true;
            }
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = Projectile;

        public float WidthFunction(float completionRatio)
        {
            float squeezeInterpolant = (float)Math.Pow(Utils.GetLerpValue(1f, 0.4f, completionRatio, true), 0.4f);
            return MathHelper.SmoothStep(Projectile.width * 0.5f, Projectile.width, squeezeInterpolant) * Utils.GetLerpValue(0.27f, 1f, completionRatio, true) * 1.6f * Projectile.Opacity;
        }

        public Color ColorFunction(float completionRatio)
        {
            Color color = Color.Lerp(Color.Cyan, Color.White, (float)Math.Pow(completionRatio, 2D));
            return color * Projectile.Opacity * Utils.GetLerpValue(1f, 0.3f, completionRatio, true) * 1.45f;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (!HasHitAnObstacle)
            {
                HasHitAnObstacle = true;
                Projectile.position += Projectile.velocity.SafeNormalize(Vector2.UnitY) * 24f;
                Projectile.velocity = Vector2.Zero;
                Projectile.netUpdate = true;
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (RayDrawer is null)
                RayDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["Infernum:BrainPsychic"]);

            GameShaders.Misc["Infernum:BrainPsychic"].UseSaturation(1f);
            GameShaders.Misc["Infernum:BrainPsychic"].UseImage1("Images/Misc/Perlin");
            RayDrawer.Draw(Projectile.oldPos.Where((x, i) => i % 2 == 0), Projectile.Size * 0.5f - Main.screenPosition, 38);
            return false;
        }
    }
}
