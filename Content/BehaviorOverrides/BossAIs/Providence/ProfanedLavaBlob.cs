using Terraria;
using Terraria.ModLoader;
using CalamityMod.Particles.Metaballs;
using InfernumMode.Common.Graphics.Metaballs;
using Microsoft.Xna.Framework;
using Terraria.ID;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Providence
{
    public class ProfanedLavaBlob : ModProjectile
    {
        public ref float Lifetime => ref Projectile.ai[0];

        public ref float BlobSize => ref Projectile.ai[1];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Lava Blob");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.Opacity = 0f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 3600;
            Projectile.MaxUpdates = 2;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            FusableParticleManager.GetParticleSetByType<ProfanedLavaParticleSet>()?.SpawnParticle(Projectile.Center + Main.rand.NextVector2Circular(BlobSize, BlobSize) / 6f, BlobSize);
            if (Projectile.timeLeft <= 3600f - Lifetime)
                Projectile.Kill();

            Projectile.velocity.Y += 0.06f;
            Projectile.Size = Vector2.One * BlobSize * 0.707f;
        }
    }
}
