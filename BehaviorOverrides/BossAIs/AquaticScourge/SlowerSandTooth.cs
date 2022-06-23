using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AquaticScourge
{
    public class SlowerSandTooth : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public Player Target => Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Sand Tooth");

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 450;
        }

        public override void AI()
        {
            if (Time < 250f && Time > 60f)
            {
                float oldSpeed = Projectile.velocity.Length();
                Projectile.velocity = (Projectile.velocity * 24f + Projectile.SafeDirectionTo(Target.Center) * oldSpeed) / 25f;
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * oldSpeed;
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
