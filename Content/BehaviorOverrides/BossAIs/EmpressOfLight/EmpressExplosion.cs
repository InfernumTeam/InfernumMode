using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class EmpressExplosion : ModProjectile
    {
        public ref float Countdown => ref Projectile.ai[0];
        public Player Target => Main.player[Projectile.owner];
        public override string Texture => "CalamityMod/Skies/XerocLight";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Explosion");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 130;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.scale = 1f;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.scale = CalamityUtils.Convert01To010(Projectile.timeLeft / 60f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.SetBlendState(BlendState.Additive);

            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 scale = Projectile.Size / texture.Size();

            Color color = Main.hslToRgb(Main.GlobalTimeWrappedHourly * 0.64f % 1f, 1f, 0.65f);
            color = Color.Lerp(color, Color.White, Pow(Projectile.scale, 5f)) * Projectile.scale;

            Main.spriteBatch.Draw(texture, drawPosition, null, color, 0f, texture.Size() * 0.5f, scale * Pow(Projectile.scale, 1.5f), 0, 0f);
            for (int j = 0; j < 3; j++)
            {
                float rotation = Lerp(-PiOver4, PiOver4, j / 2f);
                Main.spriteBatch.Draw(texture, drawPosition, null, color, rotation, texture.Size() * 0.5f, scale * new Vector2(0.15f, 1f) * 1.45f, 0, 0f);
            }
            Main.spriteBatch.ResetBlendState();

            return false;
        }
    }
}
