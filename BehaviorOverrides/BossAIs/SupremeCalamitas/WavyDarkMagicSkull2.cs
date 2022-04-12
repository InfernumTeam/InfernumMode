using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class WavyDarkMagicSkull2 : ModProjectile
    {
        public ref float IdealDirection => ref Projectile.ai[0];
        public ref float Time => ref Projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Magic Skull");
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 2;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.Calamity().canBreakPlayerDefense = true;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];

            if (Projectile.timeLeft > 270)
                return;

            Projectile.velocity.Y = (float)Math.Sin(Time / 18f + Projectile.identity) * 10f;
            Projectile.Opacity = Utils.GetLerpValue(0f, 5f, Time, true) * Utils.GetLerpValue(0f, 5f, Projectile.timeLeft, true);
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.spriteDirection = (Math.Cos(Projectile.rotation) > 0f).ToDirectionInt();
            if (Projectile.spriteDirection == -1)
                Projectile.rotation += MathHelper.Pi;

            Lighting.AddLight(Projectile.Center, Projectile.Opacity * 0.9f, 0f, 0f);

            Time++;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool? CanDamage() => Projectile.Opacity >= 1f ? null : false;

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<AbyssalFlames>(), 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.timeLeft > 270)
            {
                Vector2 start = Projectile.Center;
                Vector2 end = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * 4000f;
                float width = Utils.GetLerpValue(300f, 285f, Projectile.timeLeft, true) * Utils.GetLerpValue(270f, 285f, Projectile.timeLeft, true) * 4f + 1f;
                Main.spriteBatch.DrawLineBetter(start, end, Color.Red, width);
                return false;
            }

            lightColor.R = (byte)(255 * Projectile.Opacity);
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 1);
            return false;
        }

        public override bool ShouldUpdatePosition() => Projectile.timeLeft <= 270;

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}
