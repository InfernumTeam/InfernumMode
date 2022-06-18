using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.WallOfFlesh
{
    public class HellishScythe : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Demon Scythe");

        public override void SetDefaults()
        {
            projectile.width = 48;
            projectile.height = 48;
            projectile.alpha = 100;
            projectile.light = 0.2f;
            projectile.aiStyle = 18;
            projectile.hostile = true;
            projectile.penetrate = -1;
            projectile.tileCollide = true;
            projectile.scale = 0.9f;
        }

        public override void AI()
        {
            if (projectile.ai[1] == 0f && projectile.type == 44)
            {
                projectile.ai[1] = 1f;
                Main.PlaySound(SoundID.Item8, projectile.position);
            }
            Time++;

            projectile.rotation += projectile.direction * 0.8f;
            if (Time >= 30f)
            {
                if (Time < 100f)
                    projectile.velocity *= 1.046f;
                else
                    Time = 200f;
            }
            for (int i = 0; i < 2; i++)
            {
                Dust demonMagic = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 27, 0f, 0f, 100, default(Color), 1f);
                demonMagic.noGravity = true;
            }
        }

        public override Color? GetAlpha(Color lightColor) => Color.White;

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Vector2 top = projectile.Center - Vector2.UnitY * 2000f;
            Vector2 bottom = projectile.Center + Vector2.UnitY * 2000f;
            float telegraphWidth = CalamityUtils.Convert01To010(Time / 30f) * 4f;
            if (telegraphWidth > 0.1f)
                spriteBatch.DrawLineBetter(top, bottom, Color.Violet * Utils.InverseLerp(0f, 30f, Time, true), telegraphWidth);

            return true;
        }

        public override void Kill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            Main.PlaySound(SoundID.Item10, projectile.position);
            for (int num612 = 0; num612 < 30; num612++)
            {
                Dust demonMagic = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 27, projectile.velocity.X, projectile.velocity.Y, 100, default, 1.7f);
                demonMagic.noGravity = true;

                Dust.NewDust(projectile.position, projectile.width, projectile.height, 27, projectile.velocity.X, projectile.velocity.Y, 100, default, 1f);
            }
        }
    }
}
