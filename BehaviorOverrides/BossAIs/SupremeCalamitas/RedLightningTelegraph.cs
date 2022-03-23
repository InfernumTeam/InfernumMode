using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class RedLightningTelegraph : ModProjectile
    {
        public ref float Lifetime => ref projectile.ai[0];
        public ref float Time => ref projectile.ai[1];
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Telegraph");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 2;
            projectile.hostile = false;
            projectile.friendly = false;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 90;
        }

        public override void AI()
        {
            projectile.scale = (float)Math.Sin(MathHelper.Pi * Time / Lifetime);
            if (Time > Lifetime)
                projectile.Kill();
            Time++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Vector2 start = projectile.Center;
            Vector2 end = projectile.Center + projectile.velocity * 7000f;
            spriteBatch.DrawLineBetter(start, end, Color.Red, projectile.scale * 4f);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int lightning = Utilities.NewProjectileBetter(projectile.Center, projectile.velocity * 10f, ModContent.ProjectileType<RedLightning2>(), 520, 0f);
            if (Main.projectile.IndexInRange(lightning))
            {
                Main.projectile[lightning].Center -= projectile.velocity * 400f;
                Main.projectile[lightning].ai[0] = Main.projectile[lightning].velocity.ToRotation();
                Main.projectile[lightning].ai[1] = Main.rand.Next(100);
            }
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
