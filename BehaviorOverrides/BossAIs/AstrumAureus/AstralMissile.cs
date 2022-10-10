using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumAureus
{
    public class AstralMissile : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Astral Missile");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 2;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 360;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Lighting.AddLight(Projectile.Center, 0.5f, 0.5f, 0.5f);

            Player closestPlayer = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];

            // Fly towards the closest player.
            if (Time < 45f && !Projectile.WithinRange(closestPlayer.Center, 75f))
            {
                float maxSpeed = BossRushEvent.BossRushActive ? 30f : 19f;
                Projectile.velocity = Projectile.velocity.RotateTowards(Projectile.AngleTo(closestPlayer.Center), 0.02f);
                if (Projectile.velocity.Length() < maxSpeed)
                    Projectile.velocity *= 1.016f;
            }

            if (Projectile.WithinRange(closestPlayer.Center, 30f))
                Projectile.Kill();

            if (Time >= 45f && Projectile.velocity.Length() < 35f)
                Projectile.velocity *= 1.03f;

            Vector2 backOfMissile = Projectile.Center - (Projectile.rotation - MathHelper.PiOver2).ToRotationVector2() * 20f;
            Dust.NewDustDirect(backOfMissile, 5, 5, ModContent.DustType<AstralOrange>());

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D glowmask = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/AstrumAureus/AstralMissileGlowmask").Value;
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 1);
            Utilities.DrawAfterimagesCentered(Projectile, Color.White, ProjectileID.Sets.TrailingMode[Projectile.type], 1, glowmask);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Zombie103, Projectile.Center);

            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = 96;
            Projectile.position -= Projectile.Size * 0.5f;

            for (int i = 0; i < 2; i++)
                Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, ModContent.DustType<AstralBlue>(), 0f, 0f, 50, default, 1f);

            for (int i = 0; i < 20; i++)
            {
                Dust fire = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, ModContent.DustType<AstralOrange>(), 0f, 0f, 0, default, 1.5f);
                fire.noGravity = true;
                fire.velocity *= 3f;
            }
            Projectile.Damage();
        }
    }
}
