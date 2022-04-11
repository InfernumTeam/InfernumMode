using CalamityMod.Dusts;
using CalamityMod.Events;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.CalamitasClone
{
    public class RisingBrimstoneFireball : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Brimstone Bomb");
            Main.projFrames[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 36;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
            Projectile.Opacity = 0f;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.075f, 0f, 1f);
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];

            if (Projectile.velocity.Y > -16f)
                Projectile.velocity.Y -= 0.12f;

            if (Projectile.timeLeft < Main.rand.Next(0, 90))
                Projectile.Kill();
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item73, Projectile.Center);
            Utilities.CreateGenericDustExplosion(Projectile.Center, (int)CalamityDusts.Brimstone, 10, 7f, 1.25f);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            bool shouldBeBuffed = DownedBossSystem.downedProvidence && !BossRushEvent.BossRushActive && CalamitasCloneBehaviorOverride.ReadyToUseBuffedAI;
            int fireDamage = shouldBeBuffed ? 380 : 160;

            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            Vector2 shootVelocity = Projectile.SafeDirectionTo(target.Center) * 18.5f;
            if (shouldBeBuffed)
                shootVelocity *= 1.7f;

            Utilities.NewProjectileBetter(Projectile.Center + shootVelocity * 5f, shootVelocity, ModContent.ProjectileType<HomingBrimstoneBurst>(), fireDamage, 0f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, 0);
            return false;
        }
    }
}
