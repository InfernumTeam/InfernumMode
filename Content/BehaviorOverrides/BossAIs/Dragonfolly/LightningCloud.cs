using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

using TwinsRedLightning = InfernumMode.Content.BehaviorOverrides.BossAIs.Twins.RedLightning;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Dragonfolly
{
    public class LightningCloud : ModProjectile
    {
        public float AngularOffset;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Lightning");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 64;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 45;
        }

        public override void AI()
        {
            Projectile.scale = (float)Math.Sin(MathHelper.Pi * Projectile.timeLeft / 45f);
            for (int i = 0; i < 16; i++)
            {
                Dust redLightning = Dust.NewDustPerfect(Projectile.Center, 60, Main.rand.NextVector2Circular(3f, 3f));
                redLightning.velocity *= Main.rand.NextFloat(1f, 1.9f);
                redLightning.scale *= Main.rand.NextFloat(1.85f, 2.25f);
                redLightning.color = Color.Lerp(Color.White, Color.Red, Main.rand.NextFloat(0.5f, 1f));
                redLightning.fadeIn = 1f;
                redLightning.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 offsetDirection = Vector2.UnitY.RotatedBy(AngularOffset);
            Main.spriteBatch.DrawLineBetter(Projectile.Center - offsetDirection * 4000f, Projectile.Center + offsetDirection * 4000f, Color.Red, Projectile.scale * 4f);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            Vector2 offsetDirection = Vector2.UnitY.RotatedBy(AngularOffset);
            SoundEngine.PlaySound(HolyBlast.ImpactSound, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < Main.rand.Next(1, 3 + 1); i++)
            {
                Vector2 spawnPosition = Projectile.Center + Vector2.UnitX * Main.rand.NextFloat(-10f, 10f);
                spawnPosition -= offsetDirection * 2500f;

                Utilities.NewProjectileBetter(spawnPosition, offsetDirection * 12f, ModContent.ProjectileType<TwinsRedLightning>(), 260, 0f, -1, offsetDirection.ToRotation(), Main.rand.Next(100));
            }
        }
    }
}
