using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.StormWeaver
{
    public class WindGust : ModProjectile
    {
        public Vector2 SpinCenter
        {
            get;
            set;
        }

        public float SpinDirection
        {
            get;
            set;
        }

        public ref float SpinOffsetAngle => ref Projectile.ai[0];

        public ref float Time => ref Projectile.ai[1];

        public static int Lifetime => 90;

        public static float SpinConvergencePower => 3.7f;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            // DisplayName.SetDefault("Wind Gust");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 42;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.timeLeft = Lifetime;
            Projectile.Opacity = 0f;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(SpinDirection);
            writer.WriteVector2(SpinCenter);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            SpinDirection = reader.ReadSingle();
            SpinCenter = reader.ReadVector2();
        }

        public override void AI()
        {
            // Spin in place.
            float spinAngularVelocity = Utils.Remap(Time, 0f, 45f, Pi / 75f, Pi / 359f);
            float spinRadius = (1f - Pow(Utils.GetLerpValue(0f, Lifetime - 4f, Time, true), SpinConvergencePower)) * 600f;
            SpinOffsetAngle += spinAngularVelocity * SpinDirection;
            Projectile.Center = SpinCenter + SpinOffsetAngle.ToRotationVector2() * spinRadius;

            // Rotate based on the positional difference from the last frame.
            Projectile.rotation = (Projectile.position - Projectile.oldPosition).X * 0.01f;

            // Animate frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Type];

            // Fade in and out.
            Projectile.Opacity = Utils.GetLerpValue(0f, 20f, Time, true) * Utils.GetLerpValue(0f, 20f, Projectile.timeLeft, true);

            // Emit some dust and air particles.
            float dustSpawnChance = Lerp(1f, 0.2f, Projectile.Opacity);
            for (int i = 0; i < 6; i++)
            {
                if (Main.rand.NextFloat() > dustSpawnChance)
                    continue;

                int d = Dust.NewDust(Projectile.Center, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 136, new Color(232, 251, 250, 200), 1.4f);
                Main.dust[d].noGravity = true;
                Main.dust[d].position = Projectile.Center + Main.rand.NextVector2Circular(20f, 20f);
                Main.dust[d].velocity = -Vector2.UnitY * 3f + (Projectile.position - Projectile.oldPosition).RotatedBy(PiOver2).RotatedByRandom(0.37f) * 0.1f;
            }

            if (Projectile.timeLeft <= 7)
            {
                MediumMistParticle mist = new(Projectile.Center + Main.rand.NextVector2Circular(12f, 12f), Vector2.Zero, new Color(172, 238, 255), new Color(145, 170, 188), Main.rand.NextFloat(0.5f, 1.5f), 245 - Main.rand.Next(50), 0.02f)
                {
                    Velocity = Main.rand.NextVector2Circular(7.5f, 7.5f)
                };
                GeneralParticleHandler.SpawnParticle(mist);
            }

            Time++;
        }

        public override bool? CanDamage() => Time >= 12f;

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, Color.White, ProjectileID.Sets.TrailingMode[Type]);

            int backglowStartTime = (int)(Pow(0.1f, 1f / SpinConvergencePower) * Lifetime);
            float backglowInterpolant = LumUtils.Convert01To010(Utils.GetLerpValue(backglowStartTime - 12f, backglowStartTime + 8f, Time, true));
            Projectile.DrawProjectileWithBackglowTemp(Color.Cyan with { A = 0 } * backglowInterpolant * Projectile.Opacity, Color.White, backglowInterpolant * 6f);
            return false;
        }
    }
}
