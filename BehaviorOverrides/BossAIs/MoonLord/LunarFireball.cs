using CalamityMod;
using CalamityMod.Dusts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class LunarFireball : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public ref float InitialSpeed => ref Projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Lunar Flame");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 420;
            Projectile.alpha = 225;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.rotation += (Projectile.velocity.X > 0f).ToDirectionInt();
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.04f, 0f, 1f);

            if (InitialSpeed == 0f)
                InitialSpeed = Projectile.velocity.Length();

            bool horizontalVariant = Projectile.identity % 2 == 1;
            if (Time < 60f)
            {
                Vector2 idealVelocity = Vector2.UnitX * (Projectile.velocity.X > 0f).ToDirectionInt() * InitialSpeed;
                if (!horizontalVariant)
                    idealVelocity = Vector2.UnitY * (Projectile.velocity.Y > 0f).ToDirectionInt() * InitialSpeed;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, idealVelocity, Time / 300f);
            }
            else if (Time > 90f && Projectile.velocity.Length() < 36f)
            {
                if (horizontalVariant)
                    Projectile.velocity *= new Vector2(1.01f, 0.98f);
                else
                    Projectile.velocity *= new Vector2(0.98f, 1.01f);
            }

            Lighting.AddLight(Projectile.Center, 0f, Projectile.Opacity * 0.4f, Projectile.Opacity * 0.4f);

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, Color.White, ProjectileID.Sets.TrailingMode[Projectile.type], 1);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item, (int)Projectile.position.X, (int)Projectile.position.Y, 20);
            for (int dust = 0; dust < 4; dust++)
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, (int)CalamityDusts.Nightwither, 0f, 0f);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}
