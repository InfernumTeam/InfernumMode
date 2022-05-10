using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Prime
{
    public class TeslaBomb : ModProjectile
    {
        public ref float Lifetime => ref projectile.ai[0];
        public ref float Timer => ref projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Tesla Bomb");
            Main.projFrames[projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 28;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.penetrate = -1;
            projectile.Opacity = 0f;
            projectile.timeLeft = 300;
        }

        public override void AI()
        {
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.1f, 0f, 1f);

            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 5 % Main.projFrames[projectile.type];
            if (Timer > Lifetime)
                projectile.Kill();

            Lighting.AddLight(projectile.Center, Color.White.ToVector3() * 0.75f);
            Timer++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            lightColor = Color.Lerp(lightColor, Color.LightCyan, 0.7f);
            lightColor.A = 128;
            lightColor *= projectile.Opacity;
            Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type]);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 20; i++)
            {
                Vector2 cloudShootVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(0.2f, 4f);
                Utilities.NewProjectileBetter(projectile.Center + cloudShootVelocity * 3f, cloudShootVelocity, ModContent.ProjectileType<SmallElectricGasGloud>(), 150, 0f);
            }
        }

        public override bool CanDamage() => projectile.alpha < 20;
    }
}
