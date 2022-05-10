using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
    public class HomingDoGBurst : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Death Fire");
            Main.projFrames[projectile.type] = 6;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 3;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = 30;
            projectile.height = 30;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.timeLeft = 300;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            if (projectile.localAI[0] == 0f)
            {
                projectile.localAI[0] = 1f;
                Main.PlaySound(SoundID.Item20, projectile.position);
            }

            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 6 % Main.projFrames[projectile.type];
            projectile.Opacity = Utils.InverseLerp(300f, 285f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 35f, projectile.timeLeft, true);

            Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
            projectile.velocity = (projectile.velocity * 69f + projectile.SafeDirectionTo(target.Center) * 23f) / 70f;
            projectile.velocity = projectile.velocity.SafeNormalize(Vector2.UnitY) * 16f;
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(projectile, Color.White, ProjectileID.Sets.TrailingMode[projectile.type], 1);
            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(InfernumMode.CalamityMod.BuffType("GodSlayerInferno"), 300);
            target.AddBuff(BuffID.Frostburn, 300, true);
            target.AddBuff(BuffID.Darkness, 300, true);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;

        public override void Kill(int timeLeft) => Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 74);
    }
}
