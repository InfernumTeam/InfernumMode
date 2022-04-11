using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.WallOfFlesh
{
    public class HellishScythe : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Demon Scythe");

        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.alpha = 100;
            Projectile.light = 0.2f;
            Projectile.aiStyle = 18;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.scale = 0.9f;
        }

        public override void AI()
        {
            if (Projectile.ai[1] == 0f && Projectile.type == 44)
            {
                Projectile.ai[1] = 1f;
                SoundEngine.PlaySound(SoundID.Item8, Projectile.position);
            }
            Time++;

            Projectile.rotation += Projectile.direction * 0.8f;
            if (Time >= 30f)
            {
                if (Time < 100f)
                    Projectile.velocity *= 1.046f;
                else
                    Time = 200f;
            }
            for (int i = 0; i < 2; i++)
            {
                Dust demonMagic = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 27, 0f, 0f, 100, default(Color), 1f);
                demonMagic.noGravity = true;
            }
        }

        public override Color? GetAlpha(Color lightColor) => Color.White;

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Vector2 top = Projectile.Center - Vector2.UnitY * 2000f;
            Vector2 bottom = Projectile.Center + Vector2.UnitY * 2000f;
            float telegraphWidth = CalamityUtils.Convert01To010(Time / 30f) * 4f;
            if (telegraphWidth > 0.1f)
                spriteBatch.DrawLineBetter(top, bottom, Color.Violet * Utils.GetLerpValue(0f, 30f, Time, true), telegraphWidth);

            return true;
        }

        public override void Kill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(SoundID.Item10, Projectile.position);
            for (int num612 = 0; num612 < 30; num612++)
            {
                Dust demonMagic = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 27, Projectile.velocity.X, Projectile.velocity.Y, 100, default, 1.7f);
                demonMagic.noGravity = true;

                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 27, Projectile.velocity.X, Projectile.velocity.Y, 100, default, 1f);
            }
        }
    }
}
