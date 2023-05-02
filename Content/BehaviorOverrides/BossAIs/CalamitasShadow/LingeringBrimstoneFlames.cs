using CalamityMod;
using CalamityMod.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasShadow
{
    public class LingeringBrimstoneFlames : ModProjectile, IAdditiveDrawer
    {
        public ref float Time => ref Projectile.ai[0];

        public ref float LaserLength => ref Projectile.ai[1];

        public override string Texture => "InfernumMode/Assets/ExtraTextures/GreyscaleObjects/Smoke";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Brimstone Fire Cloud");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 112;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 32;
            Projectile.Opacity = 0f;
            Projectile.hide = true;
            Projectile.rotation = Main.rand?.NextFloat(MathHelper.TwoPi) ?? 0f;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Fade in.
            Projectile.scale = CalamityUtils.Convert01To010(Projectile.timeLeft / 32f) * 2f;
            if (Projectile.scale > 1f)
                Projectile.scale = 1f;
            Projectile.Opacity = Projectile.scale;
            Projectile.scale *= MathHelper.Lerp(0.47f, 0.64f, Projectile.identity % 9f / 9f);
            Projectile.Size = Vector2.One * Projectile.scale * 200f;
            Projectile.velocity *= 0.98f;
            Projectile.rotation += MathHelper.Clamp(Projectile.velocity.X * 0.04f, -0.06f, 0.06f) + Projectile.identity % 8f / 1200f;

            Time++;
        }

        public Color ColorFunction(float completionRatio)
        {
            Color color = Color.Lerp(Color.Orange, Color.DarkRed, 1f - MathF.Pow(completionRatio, 2f));
            color = Color.Lerp(color, Color.Red, 0.5f);
            return color * Projectile.Opacity * 0.7f;
        }

        public void AdditiveDraw(SpriteBatch spriteBatch)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Color color = Projectile.GetAlpha(Color.White);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            spriteBatch.Draw(texture, drawPosition, null, color, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, 0, 0f);
            spriteBatch.Draw(texture, drawPosition, null, Color.White * Projectile.Opacity * 0.7f, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, 0, 0f);
        }

        public override Color? GetAlpha(Color lightColor)
        {
            Color c = Color.Lerp(Color.Orange, Color.Red, Projectile.identity % 10f / 20f + 0.34f);
            return c * 1.18f;
        }

        public override void Kill(int timeLeft)
        {
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (Main.netMode != NetmodeID.MultiplayerClient && !Projectile.WithinRange(target.Center, 300f))
            {
                Utilities.NewProjectileBetter(Projectile.Center, Projectile.SafeDirectionTo(target.Center) * 19f, ModContent.ProjectileType<DarkMagicFlame>(), CalamitasShadowBehaviorOverride.DarkMagicFlameDamage, 0f);
                Utilities.NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<BrimstoneBoomExplosion>(), 0, 0f);
            }
        }
    }
}
