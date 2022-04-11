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
        public ref float TornadoHeight => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Tornado");
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 1020;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.timeLeft = 480;
            Projectile.Calamity().canBreakPlayerDefense = true;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.width = (int)MathHelper.Lerp(Projectile.width, 200f, 0.05f);

            float height = BossRushEvent.BossRushActive ? 3200f : 1600f;
            if (Projectile.ai[1] == 1f)
                height *= 4f;

            TornadoHeight = MathHelper.Lerp(TornadoHeight, height, 0.05f);
            if (!CalamityPlayer.areThereAnyDamnBosses)
            {
                Projectile.active = false;
                Projectile.netUpdate = true;
                return;
            }

            Projectile.Opacity = (float)Math.Sin(Projectile.timeLeft / 480f) * 10f;
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;
        }

        internal Color ColorFunction(float completionRatio)
        {
            Color c = Color.Lerp(Color.DeepSkyBlue, Color.Turquoise, (float)Math.Abs(Math.Sin(completionRatio * MathHelper.Pi + Main.GlobalTimeWrappedHourly)) * 0.5f);
            if (Main.dayTime)
                c = Color.Lerp(c, Color.Navy, 0.4f);

            return c * Projectile.Opacity * 1.6f;
        }

        public override bool CanDamage() => Projectile.Opacity >= 0.8f;

        internal float WidthFunction(float completionRatio)
        {
            return MathHelper.Lerp(Projectile.width * 0.6f, Projectile.width + 16f, 1f - completionRatio);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Projectile.Bottom,
                Projectile.Bottom - Vector2.UnitY * TornadoHeight * 0.8f,
                (int)(Projectile.width * 0.525),
                ref _);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (TornadoDrawer is null)
                TornadoDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["Infernum:DukeTornado"]);

            GameShaders.Misc["Infernum:DukeTornado"].SetShaderTexture(ModContent.Request<Texture2D>("Terraria/Misc/Perlin").Value);
            Vector2 upwardAscent = Vector2.UnitY * TornadoHeight;
            Vector2 top = Projectile.Bottom - upwardAscent;
            List<Vector2> drawPoints = new()
            {
                top
            };
            for (int i = 0; i < 20; i++)
                drawPoints.Add(Vector2.Lerp(top, Projectile.Bottom, i / 19f) + Vector2.UnitY * 75f);

            for (int i = 0; i < 2; i++)
                TornadoDrawer.Draw(drawPoints, -Main.screenPosition, 85);
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}
