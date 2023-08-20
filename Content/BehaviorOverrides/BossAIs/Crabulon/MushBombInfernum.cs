using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Crabulon
{
    public class MushBombInfernum : ModProjectile
    {
        public override string Texture => ModContent.GetModProjectile(ModContent.ProjectileType<MushBomb>()).Texture;

        public override void SetStaticDefaults() => Main.projFrames[Projectile.type] = 4;

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.Opacity = 0.25f;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.aiStyle = 1;
            AIType = 1;
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 4)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }

            if (Projectile.frame > 3)
                Projectile.frame = 0;

            if (Projectile.position.Y > Projectile.ai[1])
                Projectile.tileCollide = true;

            Lighting.AddLight(Projectile.Center, 0f, 0.15f, 0.3f);
            Projectile.velocity.X *= 0.995f;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(in SoundID.NPCDeath1, Projectile.Center);
            Projectile.position.X = Projectile.position.X + (Projectile.width / 2);
            Projectile.position.Y = Projectile.position.Y + (Projectile.height / 2);
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.position.X = Projectile.position.X - (Projectile.width / 2);
            Projectile.position.Y = Projectile.position.Y - (Projectile.height / 2);

            for (int i = 0; i < 4; i++)
            {
                int dustIndex = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.BlueFairy, 0f, 0f, 100, default, 2f);
                Main.dust[dustIndex].velocity *= 1.5f;
                if (Main.rand.NextBool(2))
                {
                    Main.dust[dustIndex].scale = 0.5f;
                    Main.dust[dustIndex].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                }
            }

            for (int j = 0; j < 12; j++)
            {
                int dustIndex = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.BlueFairy, 0f, 0f, 100, default, 3f);
                Main.dust[dustIndex].noGravity = true;
                Main.dust[dustIndex].velocity *= 2f;
                Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.BlueFairy, 0f, 0f, 100, default, 2f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            int height = texture.Height / Main.projFrames[Projectile.type];
            int y = height * Projectile.frame;
            Rectangle frame = new(0, y, texture.Width, height);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Color.White * Projectile.Opacity, Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
