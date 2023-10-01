using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Ranged
{
    public class GlassPiece : ModProjectile
    {
        public float FireInterpolant = 1f;

        public FireParticleSet FlameDrawer = new(int.MaxValue, 1, Color.Red * 1.25f, Color.Red, 10f, 0.33f);

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Glass Piece");
            Main.projFrames[Type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.DamageType = DamageClass.Ranged;
        }

        public override void AI()
        {
            float oldSpeed = Projectile.velocity.Length();
            if (oldSpeed < 10f)
                oldSpeed = 10f;

            CalamityUtils.HomeInOnNPC(Projectile, !Projectile.tileCollide, 540f, oldSpeed, 25f);
            Projectile.rotation += Projectile.velocity.X * 0.02f;

            // Decide frames.
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.frame = Main.rand.Next(4);
                Projectile.rotation = Main.rand.NextFloat(TwoPi);
                Projectile.localAI[0] = 1f;
            }

            // Make the heat dissipate over time.
            if (Projectile.timeLeft < 140)
                FireInterpolant = Clamp(FireInterpolant - 0.019f, 0.25f, 1f);
        }

        public override Color? GetAlpha(Color lightColor) => Color.Lerp(lightColor, new(1f, 0.3f, 0.12f, 0f), FireInterpolant * 0.87f) * Projectile.Opacity;

        public override void OnKill(int timeLeft)
        {
            // Explode into a bunch of glass shards.
            SoundEngine.PlaySound(SoundID.Item27, Projectile.Center);
            for (int i = 0; i < 40; i++)
            {
                Dust glass = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(12f, 12f), 13);
                glass.velocity = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(0.5f) * 6f + Main.rand.NextVector2Circular(4f, 4f);
                glass.noGravity = Main.rand.NextBool();
            }
        }
    }
}
