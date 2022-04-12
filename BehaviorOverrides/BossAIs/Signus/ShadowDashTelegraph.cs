using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.Signus
{
    public class ShadowDashTelegraph : ModProjectile
    {
        public ref float LifetimeCountdown => ref Projectile.ai[0];
        public ref float AngularOffset => ref Projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Telegraph");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 600;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            LifetimeCountdown--;
            if (LifetimeCountdown < 0f)
                Projectile.Kill();

            Projectile.scale = Utils.GetLerpValue(0f, 8f, LifetimeCountdown, true);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 start = Projectile.Center - AngularOffset.ToRotationVector2() * 2600f;
            Vector2 end = Projectile.Center + AngularOffset.ToRotationVector2() * 5600f;
            float width = Projectile.scale * 5f;
            Main.spriteBatch.DrawLineBetter(start, end, Color.DarkViolet, width);
            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<WhisperingDeath>(), 300);
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item74, Projectile.position);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}
