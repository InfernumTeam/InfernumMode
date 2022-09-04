using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Polterghast
{
    public class GhostlyVortex : ModProjectile
    {
        public ref float MaxSpeed => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Phantoplasm Vortex");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 38;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            float maxSpeed = MaxSpeed > 0f ? MaxSpeed : 27f;
            if (Projectile.timeLeft < 90f)
                maxSpeed += 12f;

            if (Projectile.velocity.Length() < maxSpeed)
                Projectile.velocity *= 1.045f;

            Projectile.rotation -= MathHelper.Pi / 12f;
            Projectile.Opacity = Utils.GetLerpValue(300f, 295f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 25f, Projectile.timeLeft, true);
            Lighting.AddLight(Projectile.Center, Color.White.ToVector3());

            // Spawn excessively complicated dust.
            if (Main.rand.NextBool(2))
            {
                Vector2 offsetDirection = Main.rand.NextVector2Unit();
                Dust phantoplasm = Dust.NewDustDirect(Projectile.Center - offsetDirection * 30f, 0, 0, 60, 0f, 0f, 0, default, 1f);
                phantoplasm.noGravity = true;
                phantoplasm.position = Projectile.Center - offsetDirection * Main.rand.Next(10, 21);
                phantoplasm.velocity = offsetDirection.RotatedBy(MathHelper.PiOver2) * 6f;
                phantoplasm.scale = Main.rand.NextFloat(0.9f, 1.9f);
                phantoplasm.fadeIn = 0.5f;
                phantoplasm.customData = Projectile;

                offsetDirection = Main.rand.NextVector2Unit();
                phantoplasm.noGravity = true;
                phantoplasm.position = Projectile.Center - offsetDirection * Main.rand.Next(10, 21);
                phantoplasm.velocity = offsetDirection.RotatedBy(MathHelper.PiOver2) * 6f;
                phantoplasm.scale = Main.rand.NextFloat(0.9f, 1.9f);
                phantoplasm.fadeIn = 0.5f;
                phantoplasm.customData = Projectile;
                phantoplasm.color = Color.Crimson;
            }
            else
            {
                Vector2 offsetDirection = Main.rand.NextVector2Unit();
                Dust phantoplasm = Dust.NewDustDirect(Projectile.Center - offsetDirection * 30f, 0, 0, 60, 0f, 0f, 0, default, 1f);
                phantoplasm.noGravity = true;
                phantoplasm.position = Projectile.Center - offsetDirection * Main.rand.Next(20, 31);
                phantoplasm.velocity = offsetDirection.RotatedBy(-MathHelper.PiOver2) * 5f;
                phantoplasm.scale = Main.rand.NextFloat(0.9f, 1.9f);
                phantoplasm.fadeIn = 0.5f;
                phantoplasm.customData = Projectile;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255 - Projectile.alpha, 255 - Projectile.alpha, 255 - Projectile.alpha, 255 - Projectile.alpha);
        }
        
        public override bool? CanDamage() => Projectile.Opacity >= 1f ? null : false;
    }
}
