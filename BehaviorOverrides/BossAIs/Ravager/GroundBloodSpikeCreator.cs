using CalamityMod;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Ravager
{
    public class GroundBloodSpikeCreator : ModProjectile
    {
        public ref float Time => ref projectile.ai[1];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Blood Spike Creator");

        public override void SetDefaults()
        {
            projectile.width = 30;
            projectile.height = 30;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 420;
            projectile.penetrate = -1;
        }

        public override void AI()
        {
            projectile.Opacity = (float)CalamityUtils.Convert01To010(projectile.timeLeft / 420f) * 8f;
            if (projectile.Opacity > 1f)
                projectile.Opacity = 1f;
            Time++;

            // Create spikes.
            if (Time % 6f == 5f)
            {
                Main.PlaySound(SoundID.DD2_BetsyFireballShot, projectile.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float direction = Math.Sign(projectile.velocity.X);
                    Vector2 velocity = -Vector2.UnitY.RotatedBy(direction * 0.7f + Main.rand.NextFloatDirection() * MathHelper.Pi / 10f);
                    Projectile.NewProjectile(projectile.Center - Vector2.UnitY * 60f, velocity, ModContent.ProjectileType<GroundBloodSpike>(), projectile.damage, 0f);
                }
            }
        }

        public override bool CanDamage() => false;
    }
}
