using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Signus
{
    public class ShadowDashTelegraph : ModProjectile
    {
        public ref float LifetimeCountdown => ref projectile.ai[0];
        public ref float AngularOffset => ref projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Telegraph");
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 2;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 600;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            LifetimeCountdown--;
            if (LifetimeCountdown < 0f)
                projectile.Kill();

            projectile.scale = Utils.InverseLerp(0f, 8f, LifetimeCountdown, true);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Vector2 start = projectile.Center - AngularOffset.ToRotationVector2() * 2600f;
            Vector2 end = projectile.Center + AngularOffset.ToRotationVector2() * 5600f;
            float width = projectile.scale * 5f;
            spriteBatch.DrawLineBetter(start, end, Color.DarkViolet, width);
            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<WhisperingDeath>(), 300);
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Item74, projectile.position);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = projectile;
        }
    }
}
