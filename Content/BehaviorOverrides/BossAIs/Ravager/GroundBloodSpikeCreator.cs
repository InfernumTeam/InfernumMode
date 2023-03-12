using CalamityMod;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Ravager
{
    public class GroundBloodSpikeCreator : ModProjectile
    {
        public ref float Time => ref Projectile.ai[1];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Blood Spike Creator");

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 420;
            Projectile.penetrate = -1;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.Opacity = (float)CalamityUtils.Convert01To010(Projectile.timeLeft / 420f) * 8f;
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;
            Time++;

            // Create spikes.
            if (Time % 6f == 5f)
            {
                SoundEngine.PlaySound(SoundID.DD2_BetsyFireballShot, Projectile.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float direction = Math.Sign(Projectile.velocity.X);
                    Vector2 velocity = -Vector2.UnitY.RotatedBy(direction * 0.7f + Main.rand.NextFloatDirection() * MathHelper.Pi / 10f);
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center - Vector2.UnitY * 60f, velocity, ModContent.ProjectileType<GroundBloodSpike>(), Projectile.damage, 0f);
                }
            }
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => false;
    }
}
