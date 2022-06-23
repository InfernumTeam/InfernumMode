using CalamityMod;
using InfernumMode.Dusts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Ravager
{
    public class StompShockwave : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Shockwave");

        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 240;
            Projectile.scale = 0.15f;
            Projectile.extraUpdates = 3;
            Projectile.hide = true;
            Projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(240f, 225f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 50f, Projectile.timeLeft, true);
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;
            Projectile.Opacity *= 0.75f;

            Projectile.scale = Projectile.scale * 1.018f + 0.055f;
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Projectile.Opacity > 0.75f;

        public override void Kill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 30; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<RavagerMagicDust>());
                dust.velocity = Main.rand.NextVector2Circular(8f, 8f);
                dust.noGravity = true;
            }
        }

        public Vector2 Scale
        {
            get
            {
                Vector2 scale = new(Projectile.scale, Projectile.scale * 0.3f);
                if (scale.Y > 4f)
                    scale.Y = 4f;
                scale *= 0.1f;
                return scale;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Main.spriteBatch.Draw(texture, drawPosition, null, Projectile.GetAlpha(Color.White), 0f, texture.Size() * 0.5f, Scale, SpriteEffects.None, 0f);
            Main.spriteBatch.ResetBlendState();
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Utils.CenteredRectangle(Projectile.Center, Scale * new Vector2(670f, 440f)).Intersects(targetHitbox);
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI, List<int> overWiresUI)
        {
            drawCacheProjsBehindNPCsAndTiles.Add(index);
        }
    }
}
