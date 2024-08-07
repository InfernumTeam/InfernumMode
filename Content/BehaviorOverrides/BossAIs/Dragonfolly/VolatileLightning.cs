﻿using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Dragonfolly
{
    public class VolatileLightning : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Lightning Cloud");
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
            for (int i = 0; i < 7; i++)
            {
                Dust redLightning = Dust.NewDustPerfect(Projectile.Center, 267, Main.rand.NextVector2Circular(2f, 2f));
                redLightning.velocity *= Main.rand.NextFloat(1f, 1.7f);
                redLightning.scale *= Main.rand.NextFloat(1.85f, 2.25f);
                redLightning.color = Color.Lerp(Color.White, Color.Red, Main.rand.NextFloat(0.5f, 1f));
                redLightning.fadeIn = 1f;
                redLightning.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            for (float speed = 2f; speed <= 6f; speed += 0.7f)
            {
                float lifePersistance = Main.rand.NextFloat(0.8f, 1.7f);
                for (int i = 0; i < 60; i++)
                {
                    Dust energy = Dust.NewDustPerfect(Projectile.Center, 267);
                    energy.velocity = (TwoPi * i / 60f).ToRotationVector2() * speed;
                    energy.noGravity = true;
                    energy.color = Main.hslToRgb(Main.rand.NextFloat(0f, 0.08f), 0.85f, 0.6f);
                    energy.fadeIn = lifePersistance;
                    energy.scale = 1.4f;
                }
            }

            SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, Projectile.Center);

            Projectile.ExpandHitboxBy(105);
            Projectile.damage = 80;
            Projectile.Damage();
        }
    }
}
