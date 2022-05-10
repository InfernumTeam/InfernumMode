using CalamityMod.Projectiles.BaseProjectiles;
using InfernumMode.BehaviorOverrides.BossAIs.Twins;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class LightningTelegraph : BaseLaserbeamProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override float MaxScale => 0.3f;
        public override float MaxLaserLength => 2820f;
        public override float Lifetime => 120f;
        public override Color LaserOverlayColor => Color.Lerp(Color.IndianRed, Color.OrangeRed, 0.6f) * 1.2f;
        public override Color LightCastColor => LaserOverlayColor;
        public override Texture2D LaserBeginTexture => ModContent.GetTexture("CalamityMod/ExtraTextures/Lasers/UltimaRayStart");
        public override Texture2D LaserMiddleTexture => ModContent.GetTexture("CalamityMod/ExtraTextures/Lasers/UltimaRayMid");
        public override Texture2D LaserEndTexture => ModContent.GetTexture("CalamityMod/ExtraTextures/Lasers/UltimaRayEnd");

        // To allow easy, static access from different locations.
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Telegraph Ray");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 28;
            projectile.hostile = true;
            projectile.penetrate = -1;
            projectile.alpha = 255;
            projectile.tileCollide = false;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool CanDamage() => false;

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ProvidenceHolyBlastImpact"), projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
            Vector2 lightningDirection = projectile.velocity.RotateTowards(projectile.AngleTo(target.Center + target.velocity * 10f), MathHelper.Pi / 32f);
            lightningDirection = lightningDirection.RotatedByRandom(0.05f);

            int lightning = Utilities.NewProjectileBetter(projectile.Center, lightningDirection * 7f, ModContent.ProjectileType<RedLightning>(), 120, 0f);
            Main.projectile[lightning].ai[0] = lightningDirection.ToRotation();
            Main.projectile[lightning].ai[1] = Main.rand.Next(100);
        }
    }
}
