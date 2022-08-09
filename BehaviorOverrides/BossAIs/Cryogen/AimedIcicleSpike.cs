using CalamityMod;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Cryogen
{
    public class AimedIcicleSpike : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public ref float AimAheadFactor => ref Projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Icicle Spike");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 28;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 240;
            Projectile.extraUpdates = BossRushEvent.BossRushActive ? 1 : 0;
            Projectile.Calamity().DealsDefenseDamage = true;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 12f, Time, true) * Utils.GetLerpValue(0f, 12f, Projectile.timeLeft, true);

            Player closestPlayer = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (Time < 60f)
            {
                float spinSlowdown = Utils.GetLerpValue(56f, 40f, Time, true);
                Projectile.velocity *= 0.93f;
                Projectile.rotation += (Projectile.velocity.X > 0f).ToDirectionInt() * spinSlowdown * 0.3f;
                if (spinSlowdown < 1f)
                {
                    Vector2 aimAhead = closestPlayer.velocity * AimAheadFactor;
                    Projectile.rotation = Projectile.rotation.AngleLerp(Projectile.AngleTo(closestPlayer.Center + aimAhead) - MathHelper.PiOver2, (1f - spinSlowdown) * 0.6f);
                }
            }

            if (Time == 60f)
            {
                Projectile.velocity = Projectile.SafeDirectionTo(closestPlayer.Center + closestPlayer.velocity * AimAheadFactor) * 9f;
                SoundEngine.PlaySound(SoundID.Item8, Projectile.Center);
            }
            if (Time > 60f && Projectile.velocity.Length() < 18f)
                Projectile.velocity *= BossRushEvent.BossRushActive ? 1.02f : 1.01f;

            Lighting.AddLight(Projectile.Center, Vector3.One * Projectile.Opacity * 0.4f);
            Time++;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return Main.dayTime ? new Color(50, 50, 255, 255 - Projectile.alpha) : new Color(255, 255, 255, Projectile.alpha);
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Time >= 60f;
    }
}
