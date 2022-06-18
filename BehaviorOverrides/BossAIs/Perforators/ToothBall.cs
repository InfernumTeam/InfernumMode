using CalamityMod;
using InfernumMode.BehaviorOverrides.BossAIs.BoC;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Perforators
{
    public class ToothBall : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Tooth Ball");
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            projectile.width = 32;
            projectile.height = 32;
            projectile.ignoreWater = true;
            projectile.timeLeft = 150;
            projectile.scale = 1f;
            projectile.tileCollide = false;
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            projectile.velocity.X *= 0.98f;
            projectile.rotation += projectile.velocity.X * 0.02f;

            if (projectile.timeLeft > 80)
                projectile.velocity.Y = MathHelper.Clamp(projectile.velocity.Y - 0.12f, -5.5f, 17.5f);
            else
            {
                projectile.velocity.Y *= 0.98f;
                if (projectile.timeLeft < 60f)
                    projectile.Center += Main.rand.NextVector2Circular(1.5f, 1.5f);
            }
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Item14, projectile.Center);
            for (int i = 0; i < 6; i++)
                Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 5);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                float offsetAngle = Main.rand.NextBool().ToInt() * MathHelper.Pi / 6f;
                for (int i = 0; i < 6; i++)
                {
                    Vector2 ichorShootVelocity = (MathHelper.TwoPi * i / 6f + offsetAngle).ToRotationVector2() * 9f;
                    Utilities.NewProjectileBetter(projectile.Center, ichorShootVelocity, ModContent.ProjectileType<IchorSpit>(), 75, 0f);
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(projectile, Color.White, ProjectileID.Sets.TrailingMode[projectile.type], 3);
            return false;
        }
    }
}
