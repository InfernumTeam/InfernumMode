using CalamityMod.NPCs;
using CalamityMod.Particles.Metaballs;
using InfernumMode.Common.Graphics.Metaballs.CalMetaballs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasShadow
{
    public class ShadowBlob : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public static NPC CalShadow => Main.npc[CalamityGlobalNPC.calamitas];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Shadow Blob");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 42;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 250;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Disappear if the shadow is not present.
            if (CalamityGlobalNPC.calamitas == -1)
            {
                Projectile.Kill();
                return;
            }

            if (Time >= 12f)
            {
                Vector2 idealVelocity = (CalShadow.Center - Projectile.Center) * 0.1f;
                idealVelocity = idealVelocity.ClampMagnitude(20f, 50f);

                // Die if touching the shadow.
                if (Projectile.WithinRange(CalShadow.Center, 45f))
                    Projectile.Kill();

                Projectile.velocity = Vector2.Lerp(Projectile.velocity, idealVelocity, 0.18f);
            }
            else
                Projectile.velocity *= 0.9f;

            Time++;

            // Create blob particles.
            FusableParticleManager.GetParticleSetByType<ShadowDemonParticleSet>()?.SpawnParticle(Projectile.Center + Main.rand.NextVector2Circular(12f, 12f), (20f + Projectile.velocity.Length()) * Projectile.scale);
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
