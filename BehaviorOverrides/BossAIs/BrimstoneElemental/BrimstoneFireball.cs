using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.BrimstoneElemental
{
    public class BrimstoneFireball : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public ref float InitialSpeed => ref Projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Brimstone Flame");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 2;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
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

            Lighting.AddLight(Projectile.Center, Projectile.Opacity * 0.6f, 0f, 0f);

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor.R = (byte)(255 * Projectile.Opacity);
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 1);
            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if ((DownedBossSystem.downedProvidence || BossRushEvent.BossRushActive) && BrimstoneElementalBehaviorOverride.ReadyToUseBuffedAI)
                target.AddBuff(ModContent.BuffType<VulnerabilityHex>(), 120);
            else
                target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 60);
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item20, Projectile.Center);
            for (int dust = 0; dust < 4; dust++)
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, (int)CalamityDusts.Brimstone, 0f, 0f);
        }
    }
}
