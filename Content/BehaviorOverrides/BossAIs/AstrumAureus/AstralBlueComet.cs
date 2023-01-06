using CalamityMod.Dusts;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AstrumAureus
{
    public class AstralBlueComet : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Astral Comet");
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 50;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 100;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 420;
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 5)
            {
                Projectile.frame++;
                if (Projectile.frame >= Main.projFrames[Projectile.type])
                    Projectile.frame = 0;

                Projectile.frameCounter = 0;
            }

            Projectile.spriteDirection = (Projectile.velocity.X > 0f).ToDirectionInt();
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.spriteDirection == -1)
                Projectile.rotation += MathHelper.Pi;

            // Emit light.
            Lighting.AddLight(Projectile.Center, 0.3f, 0.5f, 0.1f);

            // Emit astral flame dust.
            Projectile.ai[0]++;
            if (Projectile.ai[0] > 15f)
            {
                Dust astralFire = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, ModContent.DustType<AstralOrange>(), 0f, 0f, 100, default, 0.8f);
                astralFire.noGravity = true;
                astralFire.velocity = Vector2.Zero;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 1);
            return false;
        }

        public override Color? GetAlpha(Color lightColor) => new Color(255, 255, 255, Projectile.alpha);

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Zombie103, Projectile.Center);
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = 96;
            Projectile.position -= Projectile.Size * 0.5f;
            for (int i = 0; i < 2; i++)
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, ModContent.DustType<AstralOrange>(), 0f, 0f, 50, default, 1f);

            for (int i = 0; i < 20; i++)
            {
                Dust astralFire = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, ModContent.DustType<AstralOrange>(), 0f, 0f, 0, default, 1.5f);
                astralFire.noGravity = true;
                astralFire.velocity *= 3f;

                astralFire = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 173, 0f, 0f, 50, default, 1f);
                astralFire.velocity *= 2f;
                astralFire.noGravity = true;
            }
            Projectile.Damage();
        }
    }
}
