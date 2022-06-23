using CalamityMod;
using InfernumMode.BehaviorOverrides.BossAIs.BoC;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Perforators
{
    public class ToothBall : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Tooth Ball");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 150;
            Projectile.scale = 1f;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            Projectile.velocity.X *= 0.98f;
            Projectile.rotation += Projectile.velocity.X * 0.02f;

            if (Projectile.timeLeft > 80)
                Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y - 0.12f, -5.5f, 17.5f);
            else
            {
                Projectile.velocity.Y *= 0.98f;
                if (Projectile.timeLeft < 60f)
                    Projectile.Center += Main.rand.NextVector2Circular(1.5f, 1.5f);
            }
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            for (int i = 0; i < 6; i++)
                Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 5);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                float offsetAngle = Main.rand.NextBool().ToInt() * MathHelper.Pi / 6f;
                for (int i = 0; i < 6; i++)
                {
                    Vector2 ichorShootVelocity = (MathHelper.TwoPi * i / 6f + offsetAngle).ToRotationVector2() * 9f;
                    Utilities.NewProjectileBetter(Projectile.Center, ichorShootVelocity, ModContent.ProjectileType<IchorSpit>(), 75, 0f);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, Color.White, ProjectileID.Sets.TrailingMode[Projectile.type], 3);
            return false;
        }
    }
}
