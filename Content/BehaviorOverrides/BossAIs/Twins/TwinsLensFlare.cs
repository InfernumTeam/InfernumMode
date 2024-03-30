using CalamityMod.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Twins
{
    public class TwinsLensFlare : ModProjectile
    {
        public bool SpazmatismVariant
        {
            get => Projectile.ai[1] == 1f;
            set => Projectile.ai[1] = value.ToInt();
        }

        public const int Lifetime = 45;

        public override string Texture => "InfernumMode/Assets/ExtraTextures/GreyscaleObjects/LargeStar";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 6;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.scale = LumUtils.Convert01To010(Projectile.timeLeft / (float)Lifetime) * 1.67f;
        }

        public override Color? GetAlpha(Color lightColor) => SpazmatismVariant ? Color.Lime : Color.Red;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            for (float scale = 1f; scale > 0.3f; scale -= 0.1f)
            {
                Color c = Color.Lerp(Projectile.GetAlpha(Color.White), Color.White, 1f - scale);
                Main.spriteBatch.Draw(texture, drawPosition, null, c, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * scale, 0, 0f);
                Main.spriteBatch.Draw(texture, drawPosition, null, c, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * new Vector2(4f, 0.2f) * scale, 0, 0f);
            }
            Main.spriteBatch.ResetBlendState();
            return false;
        }
    }
}
