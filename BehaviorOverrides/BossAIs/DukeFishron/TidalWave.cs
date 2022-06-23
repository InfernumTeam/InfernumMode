using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.DukeFishron
{
    public class TidalWave : ModProjectile
    {
        internal PrimitiveTrailCopy TornadoDrawer;
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public ref float Time => ref Projectile.ai[0];
        public ref float WaveHeight => ref Projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Tidal Wave");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 150;
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
            Projectile.timeLeft = 360;
            Projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            Time++;

            if (WaveHeight < 60f)
                WaveHeight = 60f;
            WaveHeight = MathHelper.Lerp(WaveHeight, 640f, 0.04f);
            Projectile.Opacity = (float)Math.Sin(Projectile.timeLeft / 360f * MathHelper.Pi) * 3f;
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;
        }

        internal Color ColorFunction(float completionRatio)
        {
            Color c = Color.Lerp(Color.DeepSkyBlue, Color.Turquoise, (float)Math.Abs(Math.Sin(completionRatio * MathHelper.Pi + Main.GlobalTimeWrappedHourly)) * 0.5f);
            if (Main.dayTime)
                c = Color.Lerp(c, Color.Navy, 0.4f);

            return c * Projectile.Opacity;
        }

        internal float WidthFunction(float completionRatio) => WaveHeight;

        internal Vector2 OffsetFunction(float completionRatio) => Vector2.UnitY * (float)Math.Sin(completionRatio * MathHelper.Pi + Time / 11f) * 60f;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float _ = 0f;
                float completionRatio = i / (float)Projectile.oldPos.Length;
                Vector2 top = Projectile.oldPos[i] + OffsetFunction(completionRatio);
                Vector2 bottom = Projectile.oldPos[i] + Vector2.UnitY * WaveHeight + OffsetFunction(completionRatio);
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), top, bottom, (int)Math.Abs(Projectile.velocity.X) * 2f, ref _))
                    return true;
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (TornadoDrawer is null)
                TornadoDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, OffsetFunction, false, GameShaders.Misc["Infernum:DukeTornado"]);

            GameShaders.Misc["Infernum:DukeTornado"].SetShaderTexture(ModContent.GetTexture("Terraria/Misc/Perlin"));

            for (int i = 0; i < 3; i++)
                TornadoDrawer.Draw(Projectile.oldPos, Vector2.UnitY * WaveHeight * 0.5f - Main.screenPosition, 35, 0f);
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}
