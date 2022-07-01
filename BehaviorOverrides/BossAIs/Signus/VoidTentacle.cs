using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Signus
{
    public class VoidTentacle : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Void Tentacle");

        public override void SetDefaults()
        {
            Projectile.height = 160;
            Projectile.width = 160;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.MaxUpdates = 3;
            Projectile.penetrate = -1;
        }

        public override void AI()
        {
            Vector2 originalCenter = Projectile.Center;
            Projectile.scale = 1f - Projectile.localAI[0];
            Projectile.width = (int)(20f * Projectile.scale);
            Projectile.height = Projectile.width;
            Projectile.position.X = originalCenter.X - (float)(Projectile.width / 2);
            Projectile.position.Y = originalCenter.Y - (float)(Projectile.height / 2);

            if (Projectile.localAI[0] < 0.1)
            {
                Projectile.localAI[0] += 0.01f;
            }
            else
            {
                Projectile.localAI[0] += 0.025f;
            }
            if (Projectile.localAI[0] >= 0.95f)
            {
                Projectile.Kill();
            }
            Projectile.velocity.X = Projectile.velocity.X + Projectile.ai[0] * 1.5f;
            Projectile.velocity.Y = Projectile.velocity.Y + Projectile.ai[1] * 1.5f;
            if (Projectile.velocity.Length() > 16f)
            {
                Projectile.velocity.Normalize();
                Projectile.velocity *= 16f;
            }

            Projectile.ai[0] *= 1.05f;
            Projectile.ai[1] *= 1.05f;
            if (Projectile.scale < 1f)
            {
                int i = 0;
                while (i < Projectile.scale * 4f)
                {
                    int dustID = Main.rand.NextBool(5) ? 199 : 175;
                    int idx = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, dustID, Projectile.velocity.X, Projectile.velocity.Y, 100, default, 1.1f);
                    Main.dust[idx].position = (Main.dust[idx].position + Projectile.Center) / 2f;
                    Main.dust[idx].noGravity = true;
                    Main.dust[idx].velocity *= 0.1f;
                    Main.dust[idx].velocity -= Projectile.velocity * (1.3f - Projectile.scale);
                    Main.dust[idx].fadeIn = 100;
                    Main.dust[idx].scale += Projectile.scale * 0.75f;
                    i++;
                }
            }
        }
    }
}
