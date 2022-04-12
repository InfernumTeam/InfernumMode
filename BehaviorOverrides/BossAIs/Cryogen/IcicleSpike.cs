using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Cryogen
{
    public class IcicleSpike : ModProjectile
    {
        public ref float Time => ref Projectile.localAI[0];
        public ref float SpeedPower => ref Projectile.localAI[1];
        public ref float OffsetRotation => ref Projectile.ai[0];
        public NPC Owner => Main.npc[(int)Projectile.ai[1]];
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
        }

        public override void AI()
        {
            if (!Main.npc.IndexInRange((int)Projectile.ai[1]) || !Owner.active)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Opacity = Utils.GetLerpValue(0f, 12f, Time, true) * Utils.GetLerpValue(0f, 12f, Projectile.timeLeft, true);

            if (Time < 65f)
                OffsetRotation += MathHelper.TwoPi * 2f / 55f * Utils.GetLerpValue(30f, 60f, Time, true);
            if (Time == 80f)
                Projectile.velocity = Owner.SafeDirectionTo(Projectile.Center) * SpeedPower * 9f;
            if (Time > 80f && Projectile.velocity.Length() < SpeedPower * 33f)
                Projectile.velocity *= 1f + SpeedPower * 0.03f;

            if (Time <= 80f)
                Projectile.Center = Owner.Center + OffsetRotation.ToRotationVector2() * MathHelper.Lerp(110f, 72f, SpeedPower);

            Projectile.rotation = Time > 80f ? Projectile.velocity.ToRotation() : Owner.AngleTo(Projectile.Center);
            Projectile.rotation -= MathHelper.PiOver2;

            Time++;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return Main.dayTime ? new Color(50, 50, 255, 255 - Projectile.alpha) : new Color(255, 255, 255, Projectile.alpha);
        }

        public override bool? CanDamage() => Projectile.alpha < 20 ? null : false;
    }
}
