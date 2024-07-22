using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Dragonfolly
{
    public class RedLightningRedirectingFeather : ModProjectile
    {
        public const int AimTime = 16;
        public const int RedirectDelay = 40;
        public const int FlyTime = 240;
        public Player Target => Main.player[(int)Projectile.ai[1]];
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Lightning Feather");
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = RedirectDelay + FlyTime;
            Projectile.extraUpdates = BossRushEvent.BossRushActive ? 1 : 0;
            
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter % 5 == 4)
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];

            Lighting.AddLight(Projectile.Center, Color.Red.ToVector3());
            Projectile.Opacity = Utils.GetLerpValue(0f, 20f, Projectile.timeLeft, true);
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.velocity.Length() < 27f)
                Projectile.velocity *= 1.02f;

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 1, TextureAssets.Projectile[Projectile.type].Value, false);
            return false;
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Projectile.timeLeft > RedirectDelay;

        public override void OnKill(int timeLeft)
        {
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = 64;
            Projectile.position.X = Projectile.position.X - Projectile.width / 2;
            Projectile.position.Y = Projectile.position.Y - Projectile.height / 2;
            Projectile.Damage();
        }

        public override bool CanHitPlayer(Player target) => Projectile.Opacity == 1f;
    }
}
