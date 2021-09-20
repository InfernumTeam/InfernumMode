using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
    public class OtherworldlyScythe : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Scythe");
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = 26;
            projectile.height = 26;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 150;
            projectile.alpha = 100;
            projectile.penetrate = -1;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.rotation += 0.6f * projectile.direction;
            if (projectile.localAI[0] == 0f)
            {
                Main.PlaySound(SoundID.Item73, projectile.position);
                projectile.localAI[0] = 1f;
            }
            Player closest = Main.player[Player.FindClosest(projectile.Center, 1, 1)];

            if (!projectile.WithinRange(closest.Center, 200f))
                projectile.velocity = (projectile.velocity * 40f + projectile.DirectionTo(closest.Center) * 19f) / 41f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 1);
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(InfernumMode.CalamityMod.BuffType("GodSlayerInferno"), 240);
        }
    }
}
