using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.PlaguebringerGoliath
{
    public class PlagueVomit : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        // public override void SetStaticDefaults() => DisplayName.SetDefault("Plague Vomit");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 10f, Time, true);
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;
            Time++;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item42, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 7; i++)
            {
                Vector2 seekerVelocity = (TwoPi * (i + 0.5f) / 7f).ToRotationVector2() * 13.5f;
                Utilities.NewProjectileBetter(Projectile.Center, seekerVelocity, ModContent.ProjectileType<HostilePlagueSeeker>(), PlaguebringerGoliathBehaviorOverride.PlagueSeekerDamage, 0f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Projectile.DrawProjectileWithBackglowTemp(Color.White with { A = 0 }, lightColor, 4f);
            return false;
        }

        public override Color? GetAlpha(Color lightColor) => Color.Lerp(Color.White, Color.DarkGreen, Utils.GetLerpValue(45f, 0f, Projectile.timeLeft, true)) * Projectile.Opacity;

        public override bool? CanDamage() => Projectile.Opacity >= 0.8f;
    }
}
