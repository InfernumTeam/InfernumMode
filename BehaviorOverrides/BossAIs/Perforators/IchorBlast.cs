using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Perforators
{
    public class IchorBlast : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ichor Blast");
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 5;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 28;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 360;
            projectile.penetrate = -1;
        }

        public override void AI()
        {
            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;
            if (Math.Abs(projectile.velocity.X) < 18.5f)
                projectile.velocity.X *= 1.02f;

            // Release blood idly.
            Dust blood = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, DustID.Blood, 0f, 0f, 100, default, 0.5f);
            blood.velocity = Vector2.Zero;
            blood.noGravity = true;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<BurningBlood>(), 120);

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type]);
            return false;
        }

        public override Color? GetAlpha(Color lightColor) => new Color(246, 195, 80, projectile.alpha);
    }
}
