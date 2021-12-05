using CalamityMod.Dusts;
using CalamityMod.Events;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.CalamitasClone
{
    public class RisingBrimstoneFireball : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Brimstone Bomb");
            Main.projFrames[projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 5;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 36;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 120;
            projectile.Opacity = 0f;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.075f, 0f, 1f);
            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;
            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 5 % Main.projFrames[projectile.type];

            if (projectile.velocity.Y > -16f)
                projectile.velocity.Y -= 0.12f;

            if (projectile.timeLeft < Main.rand.Next(0, 90))
                projectile.Kill();
        }

		public override void Kill(int timeLeft)
		{
            Main.PlaySound(SoundID.Item73, projectile.Center);
            Utilities.CreateGenericDustExplosion(projectile.Center, (int)CalamityDusts.Brimstone, 10, 7f, 1.25f);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            bool shouldBeBuffed = CalamityWorld.downedProvidence && !BossRushEvent.BossRushActive && CalamitasCloneBehaviorOverride.ReadyToUseBuffedAI;
            int fireDamage = shouldBeBuffed ? 380 : 160;

            Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
            Vector2 shootVelocity = projectile.SafeDirectionTo(target.Center) * 15f;
            if (shouldBeBuffed)
                shootVelocity *= 1.7f;

            Utilities.NewProjectileBetter(projectile.Center + shootVelocity * 5f, shootVelocity, ModContent.ProjectileType<HomingBrimstoneBurst>(), fireDamage, 0f);
        }

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(projectile, lightColor, 0);
            return false;
        }
    }
}
