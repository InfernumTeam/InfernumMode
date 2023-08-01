using CalamityMod;
using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class CeaselessVoidLineTelegraph : ModProjectile
    {
        public static int Lifetime => 72;

        public override string Texture => InfernumTextureRegistry.InvisPath;

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Line Telegraph");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = Lifetime;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.scale = CalamityUtils.Convert01To010(Projectile.timeLeft / (float)Lifetime) * 6f;
            if (Projectile.scale > 1.5f)
                Projectile.scale = 1.5f;
        }

        public override bool ShouldUpdatePosition() => true;

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.SetBlendState(BlendState.Additive);

            float lineDistance = Utils.GetLerpValue(Lifetime, 0f, Projectile.timeLeft, true) * 7000f;
            Vector2 start = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * 2000f;
            Vector2 end = start + Projectile.velocity.SafeNormalize(Vector2.UnitY) * lineDistance;
            Main.spriteBatch.DrawBloomLine(start, end, Color.Wheat * 0.65f, 160f * Projectile.scale);
            Main.spriteBatch.DrawBloomLine(start, end, Color.HotPink * 0.6f, 45f * Projectile.scale);
            Main.spriteBatch.DrawBloomLine(start, end, Color.Lerp(Color.Purple, Color.DarkBlue, 0.55f), 80f * Projectile.scale);

            Main.spriteBatch.ResetBlendState();
            return false;
        }
    }
}
