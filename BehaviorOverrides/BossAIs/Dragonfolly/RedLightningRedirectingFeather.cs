using CalamityMod;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.Dragonfolly
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
            DisplayName.SetDefault("Lightning Feather");
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
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter % 5 == 4)
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];

            Lighting.AddLight(Projectile.Center, Color.Red.ToVector3());
            Projectile.Opacity = Utils.GetLerpValue(0f, 20f, Projectile.timeLeft, true);

            if (Time < RedirectDelay - AimTime)
            {
                if (Projectile.velocity.Length() > 0.04f)
                    Projectile.velocity *= 0.945f;
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
            else if (Time < RedirectDelay)
            {
                Projectile.rotation = Projectile.rotation.AngleLerp(Projectile.AngleTo(Target.Center), 0.15f);
                Projectile.rotation = Projectile.rotation.AngleTowards(Projectile.AngleTo(Target.Center), 0.15f);
            }
            else if (Time == RedirectDelay)
            {
                Projectile.velocity = Projectile.rotation.ToRotationVector2() * (BossRushEvent.BossRushActive ? 25f : 34f);
                SoundEngine.PlaySound(SoundID.Item109, Projectile.Center);
                for (int i = 0; i < 16; i++)
                {
                    Vector2 spawnOffset = Vector2.UnitX * Projectile.width * -0.5f;
                    spawnOffset -= Vector2.UnitY.RotatedBy(MathHelper.TwoPi * i / 16f + Projectile.rotation) * new Vector2(8f, 16f);
                    Dust redLightning = Dust.NewDustDirect(Projectile.Center, 0, 0, 267, 0f, 0f, 160, default, 1f);
                    redLightning.position = Projectile.Center + spawnOffset;
                    redLightning.color = Color.Red;
                    redLightning.velocity = spawnOffset.SafeNormalize(Vector2.UnitY) * new Vector2(1f, 2f);
                    redLightning.scale = 1.1f;
                    redLightning.fadeIn = 1.6f;
                    redLightning.noGravity = true;
                }
                Projectile.netUpdate = true;
            }
            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 1, Utilities.ProjTexture(Projectile.type), false);
            return false;
        }

        public override bool? CanDamage() => Projectile.timeLeft > RedirectDelay ? null : false;

        public override void Kill(int timeLeft)
        {
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = 64;
            Projectile.position.X = Projectile.position.X - (Projectile.width / 2);
            Projectile.position.Y = Projectile.position.Y - (Projectile.height / 2);
            Projectile.Damage();
        }

        public override bool CanHitPlayer(Player target) => Projectile.Opacity == 1f;

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (Projectile.Opacity != 1f)
                return;

            target.AddBuff(BuffID.Electrified, 90);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}
