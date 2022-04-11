using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class DarkMagicFlame : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Flame");
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter % 5 == 4)
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
            Projectile.Opacity = Utils.GetLerpValue(0f, 24f, Time, true);

            // Attempt to hover above the target.
            Vector2 destination = Main.player[Player.FindClosest(Projectile.Center, 1, 1)].Center;
            if (Time < 15f)
            {
                float flySpeed = MathHelper.Lerp(8f, 20f, Time / 15f);
                Projectile.velocity = (Projectile.velocity * 29f + Projectile.SafeDirectionTo(destination) * flySpeed) / 30f;
            }
            else if (Projectile.velocity.Length() < 43f)
            {
                Projectile.velocity *= 1.035f;
                if (Time < 45f)
                    Projectile.velocity = Projectile.velocity.RotateTowards(Projectile.AngleTo(destination), 0.04f);
            }
            Projectile.tileCollide = Time > 60f;

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Time++;
        }

        public override void Kill(int timeLeft)
        {
            for (int i = 0; i < 5; i++)
            {
                Dust fire = Dust.NewDustDirect(Projectile.Center - Vector2.One * 12f, 6, 6, 267);
                fire.color = Color.Red;
                fire.noGravity = true;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 1, Main.projectileTexture[Projectile.type], false);
            return false;
        }
    }
}
