using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Events;

namespace InfernumMode.BehaviorOverrides.BossAIs.OldDuke
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
            projectile.timeLeft = 240;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.InverseLerp(0f, 35f, Time, true) * Utils.InverseLerp(0f, 35f, projectile.timeLeft, true);
            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;
            Time++;

            if (Time < 80f)
                return;

            float idealFlySpeed = BossRushEvent.BossRushActive ? 29f : 20.25f;
            if (!projectile.WithinRange(ClosestPlayer.Center, 150f))
                projectile.velocity = (projectile.velocity * 49f + projectile.SafeDirectionTo(ClosestPlayer.Center) * idealFlySpeed) / 50f;

            if (projectile.WithinRange(ClosestPlayer.Center, 20f))
                projectile.Kill();
        }

		public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<SulphuricPoisoning>(), 120);

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 drawPosition = projectile.position + projectile.Size * 0.5f - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            Color backAfterimageColor = projectile.GetAlpha(new Color(0, 203, 255, 0) * 0.35f);
            for (int i = 0; i < 8; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 4f;
                spriteBatch.Draw(texture, drawPosition + drawOffset, null, backAfterimageColor, projectile.rotation, origin, projectile.scale, 0, 0f);
            }
            Utilities.DrawAfterimagesCentered(projectile, new Color(117, 95, 133, 184) * projectile.Opacity, ProjectileID.Sets.TrailingMode[projectile.type], 2);

            return false;
        }
    }
}
