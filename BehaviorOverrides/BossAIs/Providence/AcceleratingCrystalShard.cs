using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Providence
{
    public class AcceleratingCrystalShard : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Holy Crystal Shard");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.Opacity = 0f;
            Projectile.timeLeft = 300;
        }

        public override void AI()
        {
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.1f, 0f, 1f);
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            if (Projectile.velocity.Length() < 33f)
                Projectile.velocity *= 1.035f;

            Lighting.AddLight(Projectile.Center, Color.Yellow.ToVector3() * 0.5f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float oldScale = Projectile.scale;
            Projectile.scale *= 1.2f;
            Color rainbowColor = Main.hslToRgb(Projectile.identity / 7f % 1f, 1f, 0.5f);
            if (ProvidenceBehaviorOverride.IsEnraged)
                rainbowColor = Color.Lerp(Color.Cyan, Color.Green, Projectile.identity / 7f % 0.6f);

            lightColor = Color.Lerp(lightColor, rainbowColor, 0.9f);
            lightColor.A = 0;
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type]);
            Projectile.scale = oldScale;

            lightColor = Color.Lerp(lightColor, Color.White, 0.2f);
            lightColor.A = 0;
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type]);

            return false;
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Projectile.alpha < 20;
    }
}
