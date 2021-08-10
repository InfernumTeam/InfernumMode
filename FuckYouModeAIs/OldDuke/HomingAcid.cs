using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod.Buffs.DamageOverTime;

namespace InfernumMode.FuckYouModeAIs.OldDuke
{
	public class HomingAcid : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public Player ClosestPlayer => Main.player[Player.FindClosest(projectile.Center, 1, 1)];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Acid");
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = 18;
            projectile.height = 20;
            projectile.hostile = true;
            projectile.tileCollide = true;
            projectile.ignoreWater = true;
            projectile.timeLeft = 480;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.InverseLerp(0f, 35f, Time, true) * Utils.InverseLerp(0f, 35f, projectile.timeLeft, true);
            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;
            Time++;

            if (Time < 80f)
                return;

            if (!projectile.WithinRange(ClosestPlayer.Center, 150f))
                projectile.velocity = (projectile.velocity * 69f + projectile.SafeDirectionTo(ClosestPlayer.Center) * 12f) / 70f;

            if (projectile.WithinRange(ClosestPlayer.Center, 20f))
                projectile.Kill();
        }

		public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<SulphuricPoisoning>(), 120);

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(projectile, new Color(255, 255, 255, 127) * projectile.Opacity, ProjectileID.Sets.TrailingMode[projectile.type], 2);
            return false;
        }
    }
}
