using CalamityMod.Projectiles.BaseProjectiles;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

using TwinsRedLightning = InfernumMode.Content.BehaviorOverrides.BossAIs.Twins.RedLightning;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Twins
{
    public class LightningTelegraph : BaseLaserbeamProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override float MaxScale => 0.3f;
        public override float MaxLaserLength => 2820f;
        public override float Lifetime => 120f;
        public override Color LaserOverlayColor => Color.Lerp(Color.IndianRed, Color.OrangeRed, 0.6f) * 1.2f;
        public override Color LightCastColor => LaserOverlayColor;
        public override Texture2D LaserBeginTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/UltimaRayStart", AssetRequestMode.ImmediateLoad).Value;
        public override Texture2D LaserMiddleTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/UltimaRayMid", AssetRequestMode.ImmediateLoad).Value;
        public override Texture2D LaserEndTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/UltimaRayEnd", AssetRequestMode.ImmediateLoad).Value;

        // To allow easy, static access from different locations.
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Telegraph Ray");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 28;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.tileCollide = false;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(HolyBlast.ImpactSound, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            Vector2 lightningDirection = Projectile.velocity.RotateTowards(Projectile.AngleTo(target.Center + target.velocity * 10f), Pi / 32f);
            lightningDirection = lightningDirection.RotatedByRandom(0.05f);

            Utilities.NewProjectileBetter(Projectile.Center, lightningDirection * 7f, ModContent.ProjectileType<TwinsRedLightning>(), TwinsAttackSynchronizer.RedLightningDamage, 0f, -1, lightningDirection.ToRotation(), Main.rand.Next(100));
        }
    }
}
