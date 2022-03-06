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
            get => projectile.ai[0] == 1f;
            set => projectile.ai[0] = value.ToInt();
        }
        public const int Lifetime = 240;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Psionic Ray");
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 24;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 26;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = true;
            projectile.penetrate = -1;
            projectile.timeLeft = Lifetime;
        }

        public override void AI()
        {
            projectile.Opacity = (float)Math.Sin(projectile.timeLeft / (float)Lifetime * MathHelper.Pi) * 5f;
            if (projectile.Opacity > 1f)
                projectile.Opacity = 1f;

            // Die if the last and current position have caught up if an obstacle has been hit.
            if (HasHitAnObstacle && projectile.oldPos[0] == projectile.oldPos.Last())
                projectile.Kill();

            projectile.rotation = projectile.velocity.ToRotation();

            // Accelerate quickly until reaching a specific speed.
            if (projectile.velocity.Length() < 16f)
                projectile.velocity *= 1.064f;

            Lighting.AddLight(projectile.Center, Color.Cyan.ToVector3() * 1.6f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < projectile.oldPos.Length / 3; i++)
            {
                float _ = 0f;
                Vector2 top = projectile.oldPos[i] + projectile.Size * 0.8f;
                Vector2 bottom = projectile.oldPos[i + 1] + projectile.Size * 0.8f;
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), top, bottom, 8f, ref _))
                    return true;
            }
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;

        public float WidthFunction(float completionRatio)
        {
            float squeezeInterpolant = (float)Math.Pow(Utils.InverseLerp(1f, 0.4f, completionRatio, true), 0.4f);
            return MathHelper.SmoothStep(projectile.width * 0.5f, projectile.width, squeezeInterpolant) * Utils.InverseLerp(0.27f, 1f, completionRatio, true) * 1.6f * projectile.Opacity;
        }

        public Color ColorFunction(float completionRatio)
        {
            Color color = Color.Lerp(Color.Cyan, Color.White, (float)Math.Pow(completionRatio, 2D));
            return color * projectile.Opacity * Utils.InverseLerp(1f, 0.3f, completionRatio, true) * 1.45f;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (!HasHitAnObstacle)
            {
                HasHitAnObstacle = true;
                projectile.position += projectile.velocity.SafeNormalize(Vector2.UnitY) * 24f;
                projectile.velocity = Vector2.Zero;
                projectile.netUpdate = true;
            }
            return false;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (RayDrawer is null)
                RayDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["Infernum:BrainPsychic"]);

            GameShaders.Misc["Infernum:BrainPsychic"].UseSaturation(1f);
            GameShaders.Misc["Infernum:BrainPsychic"].UseImage("Images/Misc/Perlin");
            RayDrawer.Draw(projectile.oldPos.Where((x, i) => i % 2 == 0), projectile.Size * 0.5f - Main.screenPosition, 38);
            return false;
        }
    }
}
