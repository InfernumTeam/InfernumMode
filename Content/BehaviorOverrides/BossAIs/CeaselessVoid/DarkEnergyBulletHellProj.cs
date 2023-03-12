using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class DarkEnergyBulletHellProj : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Energy");
            Main.projFrames[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 66;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 20f, Time, true) * Utils.GetLerpValue(0f, 20f, Projectile.timeLeft, true);
            Projectile.scale = Projectile.Opacity * 0.7f;

            // Accelerate.
            if (Projectile.velocity.Length() < 19f)
                Projectile.velocity *= 1.0145f;

            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];

            Time++;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            float alpha = Utils.GetLerpValue(0f, 30f, Time, true);
            return lightColor * Projectile.Opacity * MathHelper.Lerp(0.6f, 1f, alpha);
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Projectile.Opacity >= 1f;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D glowmask1 = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/CeaselessVoid/DarkEnergyGlow").Value;
            Texture2D glowmask2 = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/CeaselessVoid/DarkEnergyGlow2").Value;

            Utilities.DrawAfterimagesCentered(Projectile, Color.Fuchsia with { A = 0 }, ProjectileID.Sets.TrailingMode[Projectile.type], 1);
            Utilities.DrawAfterimagesCentered(Projectile, lightColor with { A = 0 }, ProjectileID.Sets.TrailingMode[Projectile.type], 1);
            Utilities.DrawAfterimagesCentered(Projectile, Color.White, ProjectileID.Sets.TrailingMode[Projectile.type], 1, glowmask1);
            Utilities.DrawAfterimagesCentered(Projectile, Color.White, ProjectileID.Sets.TrailingMode[Projectile.type], 1, glowmask2);
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Utilities.CircularCollision(Projectile.Center, targetHitbox, Projectile.Size.Length() * Projectile.scale * 0.5f);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {

        }
    }
}