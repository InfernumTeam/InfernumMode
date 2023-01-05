using CalamityMod.Particles;
using InfernumMode.BehaviorOverrides.BossAIs.Cryogen;
using InfernumMode.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace InfernumMode.BehaviorOverrides.MinibossAIs.CloudElemental
{
    public class SmallHail : IceRain2
    {
        public ref float Timer => ref Projectile.ai[1];

        public override void SetStaticDefaults() => DisplayName.SetDefault("Small Hail");

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.width = 10;
            Projectile.height = 16;
        }
        public override void AI()
        {
            // Release a trail of ice.
            if (Timer % 10 == 0)
            {
                Particle iceParticle = new SnowyIceParticle(Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * 5, Projectile.velocity * 0.5f, Color.White, Main.rand.NextFloat(0.75f, 0.95f), 30);
                GeneralParticleHandler.SpawnParticle(iceParticle);
            }
            Timer++;
            base.AI();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;

            // Draw backglow effects.
            for (int i = 0; i < 12; i++)
            {
                Vector2 afterimageOffset = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * 2f;
                Color afterimageColor = new Color(90, 206, 244, 0f) * 0.7f;
                Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition + afterimageOffset, null, Projectile.GetAlpha(afterimageColor), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            }

            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, texture.Size() * 0.5f, 1, 0, 0);
            return false;
        }
    }
}
