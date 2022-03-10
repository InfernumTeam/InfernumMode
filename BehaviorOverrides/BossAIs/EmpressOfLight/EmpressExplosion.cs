using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class EmpressExplosion : ModProjectile
    {
        public ref float Countdown => ref projectile.ai[0];
        public Player Target => Main.player[projectile.owner];
        public override string Texture => "CalamityMod/ExtraTextures/XerocLight";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Explosion");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 130;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 60;
            projectile.scale = 1f;
        }

        public override void AI()
        {
            projectile.scale = CalamityUtils.Convert01To010(projectile.timeLeft / 60f);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.SetBlendState(BlendState.Additive);

            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Vector2 scale = projectile.Size / texture.Size();

            Color color = Main.hslToRgb(Main.GlobalTime * 0.64f % 1f, 1f, 0.65f);
            color = Color.Lerp(color, Color.White, (float)Math.Pow(projectile.scale, 5D)) * projectile.scale;

            spriteBatch.Draw(texture, drawPosition, null, color, 0f, texture.Size() * 0.5f, scale * (float)Math.Pow(projectile.scale, 1.5), 0, 0f);
            for (int j = 0; j < 3; j++)
            {
                float rotation = MathHelper.Lerp(-MathHelper.PiOver4, MathHelper.PiOver4, j / 2f);
                spriteBatch.Draw(texture, drawPosition, null, color, rotation, texture.Size() * 0.5f, scale * new Vector2(0.15f, 1f) * 1.45f, 0, 0f);
            }
            spriteBatch.ResetBlendState();

            return false;
        }
    }
}
