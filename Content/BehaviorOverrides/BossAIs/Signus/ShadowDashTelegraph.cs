using InfernumMode.Assets.Sounds;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Signus
{
    public class ShadowDashTelegraph : ModProjectile
    {
        public ref float LifetimeCountdown => ref Projectile.ai[0];
        public ref float AngularOffset => ref Projectile.ai[1];
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Telegraph");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 600;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            LifetimeCountdown--;
            if (LifetimeCountdown < 0f)
                Projectile.Kill();

            Projectile.scale = Utils.GetLerpValue(0f, 8f, LifetimeCountdown, true);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 start = Projectile.Center - AngularOffset.ToRotationVector2() * 2600f;
            Vector2 end = Projectile.Center + AngularOffset.ToRotationVector2() * 5600f;
            float width = Projectile.scale * 5f;
            Main.spriteBatch.DrawLineBetter(start, end, Color.DarkViolet, width);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(InfernumSoundRegistry.SignusChargeSound, Projectile.position);
        }
    }
}
