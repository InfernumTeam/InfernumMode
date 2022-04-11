using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class RedLightningTelegraph2 : ModProjectile
    {
        public ref float Lifetime => ref Projectile.ai[0];
        public ref float Time => ref Projectile.ai[1];
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Telegraph");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 90;
        }

        public override void AI()
        {
            if (Lifetime == 0f)
                Lifetime = 30f;

            Projectile.scale = (float)Math.Sin(MathHelper.Pi * Time / Lifetime);
            if (Time > Lifetime)
                Projectile.Kill();
            Time++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Vector2 start = Projectile.Center - Vector2.UnitY * 4000f;
            Vector2 end = Projectile.Center + Vector2.UnitY * 4000f;
            spriteBatch.DrawLineBetter(start, end, Color.Red, Projectile.scale * 4f);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Vector2 lightningSpawnPosition = Projectile.Center - Vector2.UnitY * 1600f;
            int lightning = Utilities.NewProjectileBetter(lightningSpawnPosition, Vector2.UnitY * 20f, ModContent.ProjectileType<RedLightning3>(), 550, 0f);
            if (Main.projectile.IndexInRange(lightning))
            {
                Main.projectile[lightning].ai[0] = Main.projectile[lightning].velocity.ToRotation();
                Main.projectile[lightning].ai[1] = Main.rand.Next(100);
            }
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
