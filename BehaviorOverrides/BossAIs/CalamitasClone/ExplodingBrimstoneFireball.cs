using CalamityMod.Dusts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.CalamitasClone
{
    public class ExplodingBrimstoneFireball : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Brimstone Bomb");
            Main.projFrames[projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 5;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 36;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 90;
            projectile.Opacity = 0f;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.075f, 0f, 1f);

            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;
            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 5 % Main.projFrames[projectile.type];
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Item74, projectile.Center);
            Utilities.CreateGenericDustExplosion(projectile.Center, (int)CalamityDusts.Brimstone, 10, 7f, 1.25f);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(projectile, lightColor, 0);
            return false;
        }
    }
}
