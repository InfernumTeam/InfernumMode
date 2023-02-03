using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.AbyssAIs
{
    public class InkBlob : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ink Blob");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 3;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
        }

        public override void AI()
        {
            // Slow down.
            Projectile.velocity *= 0.98f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Projectile.DrawProjectileWithBackglowTemp(Color.White with { A = 0 }, lightColor, 4f);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item86, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // Explode into a burst of ink bolts.
            for (int i = 0; i < 4; i++)
            {
                Vector2 inkShootVelocity = (MathHelper.TwoPi * i / 4f + MathHelper.PiOver4).ToRotationVector2() * 6f;
                Utilities.NewProjectileBetter(Projectile.Center, inkShootVelocity, ModContent.ProjectileType<InkBolt>(), 250, 0f);
            }
        }
    }
}
