using CalamityMod;
using CalamityMod.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Twins
{
    public class TwinsLensFlare : ModProjectile, IAdditiveDrawer
    {
        public bool SpazmatismVariant
        {
            get => Projectile.ai[1] == 1f;
            set => Projectile.ai[1] = value.ToInt();
        }

        public const int Lifetime = 45;
        
        public override string Texture => "InfernumMode/ExtraTextures/LargeStar";
        
        public override void SetStaticDefaults() => DisplayName.SetDefault("Lens Flare");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 6;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.scale = CalamityUtils.Convert01To010(Projectile.timeLeft / (float)Lifetime) * 1.67f;
        }

        public void AdditiveDraw(SpriteBatch spriteBatch)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            for (float scale = 1f; scale > 0.3f; scale -= 0.1f)
            {
                Color c = Color.Lerp(Projectile.GetAlpha(Color.White), Color.White, 1f - scale);
                spriteBatch.Draw(texture, drawPosition, null, c, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * scale, 0, 0f);
                spriteBatch.Draw(texture, drawPosition, null, c, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * new Vector2(4f, 0.2f) * scale, 0, 0f);
            }
        }

        public override Color? GetAlpha(Color lightColor) => SpazmatismVariant ? Color.Lime : Color.Red;

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
