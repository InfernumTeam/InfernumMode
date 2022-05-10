using CalamityMod;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Dragonfolly
{
    public class RedLightningRedirectingFeather : ModProjectile
    {
        public const int AimTime = 16;
        public const int RedirectDelay = 40;
        public const int FlyTime = 240;
        public Player Target => Main.player[(int)projectile.ai[1]];
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Lightning Feather");
            Main.projFrames[projectile.type] = 4;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 20;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = RedirectDelay + FlyTime;
            projectile.extraUpdates = BossRushEvent.BossRushActive ? 1 : 0;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.frameCounter++;
            if (projectile.frameCounter % 5 == 4)
                projectile.frame = (projectile.frame + 1) % Main.projFrames[projectile.type];

            Lighting.AddLight(projectile.Center, Color.Red.ToVector3());
            projectile.Opacity = Utils.InverseLerp(0f, 20f, projectile.timeLeft, true);

            if (Time < RedirectDelay - AimTime)
            {
                if (projectile.velocity.Length() > 0.04f)
                    projectile.velocity *= 0.945f;
                projectile.rotation = projectile.velocity.ToRotation();
            }
            else if (Time < RedirectDelay)
            {
                projectile.rotation = projectile.rotation.AngleLerp(projectile.AngleTo(Target.Center), 0.15f);
                projectile.rotation = projectile.rotation.AngleTowards(projectile.AngleTo(Target.Center), 0.15f);
            }
            else if (Time == RedirectDelay)
            {
                projectile.velocity = projectile.rotation.ToRotationVector2() * (BossRushEvent.BossRushActive ? 25f : 34f);
                Main.PlaySound(SoundID.Item109, projectile.Center);
                for (int i = 0; i < 16; i++)
                {
                    Vector2 spawnOffset = Vector2.UnitX * projectile.width * -0.5f;
                    spawnOffset -= Vector2.UnitY.RotatedBy(MathHelper.TwoPi * i / 16f + projectile.rotation) * new Vector2(8f, 16f);
                    Dust redLightning = Dust.NewDustDirect(projectile.Center, 0, 0, 267, 0f, 0f, 160, default, 1f);
                    redLightning.position = projectile.Center + spawnOffset;
                    redLightning.color = Color.Red;
                    redLightning.velocity = spawnOffset.SafeNormalize(Vector2.UnitY) * new Vector2(1f, 2f);
                    redLightning.scale = 1.1f;
                    redLightning.fadeIn = 1.6f;
                    redLightning.noGravity = true;
                }
                projectile.netUpdate = true;
            }
            Time++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 1, Main.projectileTexture[projectile.type], false);
            return false;
        }

        public override bool CanDamage() => projectile.timeLeft > RedirectDelay;

        public override void Kill(int timeLeft)
        {
            projectile.position = projectile.Center;
            projectile.width = projectile.height = 64;
            projectile.position.X = projectile.position.X - (projectile.width / 2);
            projectile.position.Y = projectile.position.Y - (projectile.height / 2);
            projectile.Damage();
        }

        public override bool CanHitPlayer(Player target) => projectile.Opacity == 1f;

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (projectile.Opacity != 1f)
                return;

            target.AddBuff(BuffID.Electrified, 90);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = projectile;
        }
    }
}
