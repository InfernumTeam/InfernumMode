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
        public ref float IdealDirection => ref projectile.ai[0];
        public ref float Time => ref projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Magic Skull");
            Main.projFrames[projectile.type] = 4;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 2;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 40;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 300;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 5 % Main.projFrames[projectile.type];

            if (projectile.timeLeft > 270)
                return;

            projectile.velocity.Y = (float)Math.Sin(Time / 13f + projectile.identity) * 10f;
            projectile.Opacity = Utils.InverseLerp(0f, 5f, Time, true) * Utils.InverseLerp(0f, 5f, projectile.timeLeft, true);
            projectile.rotation = projectile.velocity.ToRotation();
            projectile.spriteDirection = (Math.Cos(projectile.rotation) > 0f).ToDirectionInt();
            if (projectile.spriteDirection == -1)
                projectile.rotation += MathHelper.Pi;

            Lighting.AddLight(projectile.Center, projectile.Opacity * 0.9f, 0f, 0f);

            Time++;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * projectile.Opacity;

        public override bool CanDamage() => projectile.Opacity >= 1f;

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<AbyssalFlames>(), 180);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (projectile.timeLeft > 270)
            {
                Vector2 start = projectile.Center;
                Vector2 end = projectile.Center + projectile.velocity.SafeNormalize(Vector2.Zero) * 4000f;
                float width = Utils.InverseLerp(300f, 285f, projectile.timeLeft, true) * Utils.InverseLerp(270f, 285f, projectile.timeLeft, true) * 4f + 1f;
                spriteBatch.DrawLineBetter(start, end, Color.Red, width);
                return false;
            }    

            lightColor.R = (byte)(255 * projectile.Opacity);
            Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 1);
            return false;
        }

        public override bool ShouldUpdatePosition() => projectile.timeLeft <= 270;

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)	
        {
			target.Calamity().lastProjectileHit = projectile;
		}
    }
}
