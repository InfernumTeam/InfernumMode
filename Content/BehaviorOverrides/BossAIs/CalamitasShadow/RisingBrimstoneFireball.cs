using CalamityMod.Dusts;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasShadow
{
    public class RisingBrimstoneFireball : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Brimstone Bomb");
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
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.075f, 0f, 1f);
            Projectile.rotation = Projectile.velocity.ToRotation() - PiOver2;
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];

            if (Projectile.velocity.Y > -16f)
                Projectile.velocity.Y -= 0.12f;

            if (Projectile.timeLeft < Main.rand.Next(90))
                Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item73, Projectile.Center);
            Utilities.CreateGenericDustExplosion(Projectile.Center, (int)CalamityDusts.Brimstone, 10, 7f, 1.25f);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            Vector2 shootVelocity = Projectile.SafeDirectionTo(target.Center) * 18f;
            Utilities.NewProjectileBetter(Projectile.Center + shootVelocity * 5f, shootVelocity, ModContent.ProjectileType<HomingBrimstoneBurst>(), CalamitasShadowBehaviorOverride.BrimstoneBombDamage, 0f);

            for (int i = 0; i < 7; i++)
            {
                shootVelocity = (TwoPi * i / 6f + PiOver4).ToRotationVector2() * 8f;
                Utilities.NewProjectileBetter(Projectile.Center + shootVelocity * 5f, shootVelocity, ModContent.ProjectileType<DarkMagicFlame>(), CalamitasShadowBehaviorOverride.DarkMagicFlameDamage, 0f);

                shootVelocity = (TwoPi * i / 6f + PiOver4 + Pi / 6f).ToRotationVector2() * 11.5f;
                Utilities.NewProjectileBetter(Projectile.Center + shootVelocity * 5f, shootVelocity, ModContent.ProjectileType<DarkMagicFlame>(), CalamitasShadowBehaviorOverride.DarkMagicFlameDamage, 0f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, Color.White, 0);
            return false;
        }
    }
}