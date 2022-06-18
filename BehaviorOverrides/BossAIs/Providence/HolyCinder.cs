using CalamityMod.Dusts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Providence
{
    public class HolyCinder : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Holy Cinder");
            Main.projFrames[projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 16;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 360;
        }

        public override void AI()
        {
            if (projectile.velocity.Length() < 25f && Time >= 25f)
                projectile.velocity *= 1.035f;

            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 5 % Main.projFrames[projectile.type];
            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;
            projectile.tileCollide = Time > 30f;
            Time++;
        }

        public override void Kill(int timeLeft)
        {
            for (int i = 0; i < 10; i++)
            {
                Dust holyFire = Dust.NewDustPerfect(projectile.Center, (int)CalamityDusts.ProfanedFire);
                holyFire.velocity = Main.rand.NextVector2Circular(14f, 14f);
                holyFire.scale = 1.7f;
                holyFire.noGravity = true;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            float telegraphInterpolant = Utils.InverseLerp(0f, 45f, Time, true);
            if (telegraphInterpolant >= 1f)
                telegraphInterpolant = 0f;

            Main.spriteBatch.DrawLineBetter(projectile.Center, projectile.Center + projectile.velocity.SafeNormalize(Vector2.UnitY) * 6000f, Color.Yellow * telegraphInterpolant, telegraphInterpolant * 3f);
            lightColor = Color.Lerp(lightColor, Color.White, 0.4f);
            lightColor.A = 128;
            Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type]);
            return false;
        }

        public override bool CanDamage() => projectile.alpha < 20;
    }
}
