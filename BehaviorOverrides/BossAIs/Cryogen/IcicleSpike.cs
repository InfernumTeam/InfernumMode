using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Cryogen
{
    public class IcicleSpike : ModProjectile
    {
        public ref float Time => ref projectile.localAI[0];
        public ref float SpeedPower => ref projectile.localAI[1];
        public ref float OffsetRotation => ref projectile.ai[0];
        public NPC Owner => Main.npc[(int)projectile.ai[1]];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Icicle Spike");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 28;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 240;
        }

        public override void AI()
        {
            if (!Main.npc.IndexInRange((int)projectile.ai[1]) || !Owner.active)
            {
                projectile.Kill();
                return;
            }

            projectile.Opacity = Utils.InverseLerp(0f, 12f, Time, true) * Utils.InverseLerp(0f, 12f, projectile.timeLeft, true);

            if (Time < 65f)
                OffsetRotation += MathHelper.TwoPi * 2f / 55f * Utils.InverseLerp(30f, 60f, Time, true);
            if (Time == 80f)
                projectile.velocity = Owner.SafeDirectionTo(projectile.Center) * SpeedPower * 9f;
            if (Time > 80f && projectile.velocity.Length() < SpeedPower * 33f)
                projectile.velocity *= 1f + SpeedPower * 0.03f;

            if (Time <= 80f)
                projectile.Center = Owner.Center + OffsetRotation.ToRotationVector2() * MathHelper.Lerp(110f, 72f, SpeedPower);

            projectile.rotation = Time > 80f ? projectile.velocity.ToRotation() : Owner.AngleTo(projectile.Center);
            projectile.rotation -= MathHelper.PiOver2;

            Time++;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return Main.dayTime ? new Color(50, 50, 255, 255 - projectile.alpha) : new Color(255, 255, 255, projectile.alpha);
        }

        public override bool CanDamage() => projectile.alpha < 20;
    }
}
