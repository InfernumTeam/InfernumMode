using CalamityMod;
using CalamityMod.NPCs;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Providence;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class ProfanedSpearInfernum : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Profaned Spear");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.timeLeft = 300;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.tileCollide = Projectile.timeLeft < 210;
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4;
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.08f, 0f, 1f);

            // Accelerate.
            if (Projectile.velocity.Length() < 36f)
                Projectile.velocity *= 1.028f;

            Lighting.AddLight(Projectile.Center, Vector3.One);
        }

        public override Color? GetAlpha(Color lightColor)
        {
            if (CalamityGlobalNPC.holyBoss != -1 && ProvidenceBehaviorOverride.IsEnraged)
                return Color.Cyan * Projectile.Opacity;

            return Color.White * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float alpha = 1f - (float)Projectile.alpha / 255;
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor * alpha, 1);
            Projectile.DrawProjectileWithBackglowTemp(Projectile.GetAlpha(Color.White) with { A = 0 }, Color.White, 2f);
            return false;
        }
    }
}
