using CalamityMod.Dusts;
using CalamityMod.Events;
using CalamityMod.Projectiles.Boss;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.CalamitasClone
{
    public class ExplodingBrimstoneFireball : ModProjectile
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
            projectile.timeLeft = 75;
            projectile.Opacity = 0f;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.075f, 0f, 1f);

            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;
            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 5 % Main.projFrames[projectile.type];
        }

		public override void Kill(int timeLeft)
		{
            Main.PlaySound(SoundID.Item74, projectile.Center);
            Utilities.CreateGenericDustExplosion(projectile.Center, (int)CalamityDusts.Brimstone, 10, 7f, 1.25f);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            bool shouldBeBuffed = CalamityWorld.downedProvidence && !BossRushEvent.BossRushActive && CalamitasCloneBehaviorOverride.ReadyToUseBuffedAI;
            Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
            for (int i = 0; i < 4; i++)
            {
                int dartDamage = shouldBeBuffed ? 340 : 145;
                Vector2 shootVelocity = projectile.SafeDirectionTo(target.Center).RotatedByRandom(0.91f) * projectile.velocity.Length() * Main.rand.NextFloat(0.55f, 0.85f);
                int dart = Utilities.NewProjectileBetter(projectile.Center + shootVelocity, shootVelocity, ModContent.ProjectileType<BrimstoneBarrage>(), dartDamage, 0f);
                if (Main.projectile.IndexInRange(dart))
                {
                    Main.projectile[dart].ai[0] = 1f;
                    Main.projectile[dart].tileCollide = false;
                    Main.projectile[dart].netUpdate = true;
                }
            }
        }

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(projectile, lightColor, 0);
            return false;
        }
    }
}
