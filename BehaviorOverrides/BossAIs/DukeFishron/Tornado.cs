using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.DukeFishron
{
    public class Tornado : ModProjectile
    {
        internal PrimitiveTrailCopy TornadoDrawer;
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public ref float TornadoHeight => ref projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Tornado");
        }

        public override void SetDefaults()
        {
            projectile.width = 40;
            projectile.height = 1020;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.penetrate = -1;
            projectile.alpha = 255;
            projectile.timeLeft = 480;
            projectile.Calamity().canBreakPlayerDefense = true;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.width = (int)MathHelper.Lerp(projectile.width, 200f, 0.05f);

            float height = BossRushEvent.BossRushActive ? 2700f : 1000f;
            if (projectile.ai[1] == 1f)
                height *= 5f;

            TornadoHeight = MathHelper.Lerp(TornadoHeight, height, 0.05f);
            if (!CalamityPlayer.areThereAnyDamnBosses)
            {
                projectile.active = false;
                projectile.netUpdate = true;
                return;
            }

            projectile.Opacity = (float)Math.Sin(projectile.timeLeft / 480f) * 10f;
            if (projectile.Opacity > 1f)
                projectile.Opacity = 1f;
        }

        internal Color ColorFunction(float completionRatio)
        {
            Color c = Color.Lerp(Color.DeepSkyBlue, Color.Turquoise, (float)Math.Abs(Math.Sin(completionRatio * MathHelper.Pi + Main.GlobalTime)) * 0.5f);
            if (Main.dayTime)
                c = Color.Lerp(c, Color.Navy, 0.4f);

            return c * projectile.Opacity * 1.6f;
        }

        public override bool CanDamage() => projectile.Opacity >= 0.8f;

        internal float WidthFunction(float completionRatio)
        {
            return MathHelper.Lerp(projectile.width * 0.6f, projectile.width + 16f, 1f - completionRatio);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(),
                targetHitbox.Size(),
                projectile.Bottom - Vector2.UnitY * 150f,
                projectile.Bottom - Vector2.UnitY * TornadoHeight * 0.8f,
                (int)(projectile.width * 0.525),
                ref _);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (TornadoDrawer is null)
                TornadoDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["Infernum:DukeTornado"]);

            GameShaders.Misc["Infernum:DukeTornado"].SetShaderTexture(ModContent.GetTexture("Terraria/Misc/Perlin"));
            Vector2 upwardAscent = Vector2.UnitY * TornadoHeight;
            Vector2 top = projectile.Bottom - upwardAscent;
            List<Vector2> drawPoints = new List<Vector2>()
            {
                top
            };
            for (int i = 0; i < 20; i++)
                drawPoints.Add(Vector2.Lerp(top, projectile.Bottom, i / 19f) + Vector2.UnitY * 75f);

            for (int i = 0; i < 2; i++)
                TornadoDrawer.Draw(drawPoints, -Main.screenPosition, 85);
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = projectile;
        }
    }
}
