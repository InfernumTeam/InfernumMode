using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasShadow
{
    public class CatharsisSoul : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Burning Soul of Catharsis");
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 28;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.Opacity = 0f;
            Projectile.timeLeft = 160;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 12f, Time, true) * Utils.GetLerpValue(0f, 25f, Projectile.timeLeft, true);
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.spriteDirection = (Math.Cos(Projectile.rotation) > 0f).ToDirectionInt();
            if (Projectile.spriteDirection == -1)
                Projectile.rotation += Pi;

            if (Time < 60f)
                Projectile.velocity *= 0.98f;
            else
            {
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                if (target.dead || !target.active)
                {
                    Projectile.Kill();
                    return;
                }
                Projectile.velocity = (Projectile.velocity * 37f + Projectile.SafeDirectionTo(target.Center) * 11f) / 38f;
            }

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float oldScale = Projectile.scale;
            Projectile.scale *= 1.2f;
            lightColor = Color.Lerp(lightColor, Color.Red, 0.9f);
            lightColor.A = 24;
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type]);
            Projectile.scale = oldScale;

            lightColor = Color.Lerp(lightColor, Color.White, 0.5f);
            lightColor.A = 24;
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type]);

            return false;
        }

        public override bool? CanDamage() => Time >= 60f && Projectile.Opacity >= 0.65f;
    }
}
