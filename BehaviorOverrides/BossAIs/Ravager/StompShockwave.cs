using CalamityMod;
using InfernumMode.Dusts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Ravager
{
    public class StompShockwave : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Shockwave");

        public override void SetDefaults()
        {
            projectile.width = 36;
            projectile.height = 36;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 240;
            projectile.scale = 0.15f;
            projectile.extraUpdates = 3;
            projectile.hide = true;
            projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.InverseLerp(240f, 225f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 50f, projectile.timeLeft, true);
            if (projectile.Opacity > 1f)
                projectile.Opacity = 1f;
            projectile.Opacity *= 0.75f;

            projectile.scale = projectile.scale * 1.018f + 0.055f;
        }

        public override bool CanDamage() => projectile.Opacity > 0.75f;

        public override void Kill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 30; i++)
            {
                Dust dust = Dust.NewDustPerfect(projectile.Center, ModContent.DustType<RavagerMagicDust>());
                dust.velocity = Main.rand.NextVector2Circular(8f, 8f);
                dust.noGravity = true;
            }
        }

        public Vector2 Scale
        {
            get
            {
                Vector2 scale = new Vector2(projectile.scale, projectile.scale * 0.3f);
                if (scale.Y > 4f)
                    scale.Y = 4f;
                scale *= 0.1f;
                return scale;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.SetBlendState(BlendState.Additive);
            Texture2D texture = Main.projectileTexture[projectile.type];

            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            spriteBatch.Draw(texture, drawPosition, null, projectile.GetAlpha(Color.White), 0f, texture.Size() * 0.5f, Scale, SpriteEffects.None, 0f);
            spriteBatch.ResetBlendState();
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Utils.CenteredRectangle(projectile.Center, Scale * new Vector2(670f, 440f)).Intersects(targetHitbox);
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI)
        {
            drawCacheProjsBehindNPCsAndTiles.Add(index);
        }
    }
}
