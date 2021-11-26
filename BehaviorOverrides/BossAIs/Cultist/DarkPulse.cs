using CalamityMod;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Cultist
{
    public class DarkPulse : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Pulse");
            Main.projFrames[projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 20;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.netImportant = true;
            projectile.hostile = true;
            projectile.timeLeft = 600;
            projectile.Opacity = 0f;
            projectile.extraUpdates = 1;
            projectile.penetrate = -1;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.InverseLerp(0f, 10f, Time, true);
            projectile.velocity = projectile.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Clamp(projectile.velocity.Length() + 0.164f, 1f, BossRushEvent.BossRushActive ? 29f : 17f);
            projectile.frameCounter++;
            if (projectile.frameCounter % 5 == 4)
                projectile.frame = (projectile.frame + 1) % Main.projFrames[projectile.type];
            Time++;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;

        public override bool CanDamage() => projectile.Opacity >= 1f;

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            lightColor = Color.Lerp(lightColor, Color.Purple, 0.5f);
            Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type]);
            return false;
        }
    }
}
