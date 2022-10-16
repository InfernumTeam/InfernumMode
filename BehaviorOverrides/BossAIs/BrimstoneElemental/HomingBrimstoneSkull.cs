using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.BrimstoneElemental
{
    public class HomingBrimstoneSkull : ModProjectile
    {
        public Vector2 StartingVelocity;

        public ref float Time => ref Projectile.ai[0];

        public static float MaxSpeed => BossRushEvent.BossRushActive ? 17f : 13f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Brimstone Hellblast");
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 2;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 420;
            Projectile.alpha = 225;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 8)
            {
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
                Projectile.frameCounter = 0;
            }

            if (StartingVelocity == Vector2.Zero)
                StartingVelocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * 2f;

            if (Time < 0f)
            {
                float speedInterpolant = (float)Math.Pow(Utils.GetLerpValue(-150f, -1f, Time, true), 4D);
                Vector2 endingVelocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * MaxSpeed;
                Projectile.velocity = Vector2.Lerp(StartingVelocity, endingVelocity, speedInterpolant);
            }
            else if (Time < 50f)
            {
                float initialSpeed = Projectile.velocity.Length();
                Player closestTarget = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                Projectile.velocity = (Projectile.velocity * 34f + Projectile.SafeDirectionTo(closestTarget.Center) * initialSpeed) / 35f;
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * initialSpeed;
            }
            else
                Projectile.velocity *= 1.022f;

            Projectile.spriteDirection = (Projectile.velocity.X > 0f).ToDirectionInt();
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.04f, 0f, 1f);
            if (Projectile.spriteDirection == -1)
                Projectile.rotation += MathHelper.Pi;

            Lighting.AddLight(Projectile.Center, Projectile.Opacity * 0.9f, 0f, 0f);

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor.R = (byte)(255 * Projectile.Opacity);
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 1);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item20, Projectile.Center);
            for (int dust = 0; dust < 6; dust++)
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, (int)CalamityDusts.Brimstone, 0f, 0f);
        }
    }
}
