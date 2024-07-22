using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasShadow
{
    public class LingeringBrimstoneFlames : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public ref float LaserLength => ref Projectile.ai[1];

        public override string Texture => "InfernumMode/Assets/ExtraTextures/GreyscaleObjects/Smoke";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 112;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 32;
            Projectile.Opacity = 0f;
            Projectile.rotation = Main.rand?.NextFloat(TwoPi) ?? 0f;
            
        }

        public override void AI()
        {
            // Fade in.
            Projectile.scale = LumUtils.Convert01To010(Projectile.timeLeft / 32f) * 2f;
            if (Projectile.scale > 1f)
                Projectile.scale = 1f;
            Projectile.Opacity = Projectile.scale;
            Projectile.scale *= Lerp(0.47f, 0.64f, Projectile.identity % 9f / 9f);
            Projectile.Size = Vector2.One * Projectile.scale * 200f;
            Projectile.velocity *= 0.98f;
            Projectile.rotation += Clamp(Projectile.velocity.X * 0.04f, -0.06f, 0.06f) + Projectile.identity % 8f / 1200f;

            Time++;
        }

        public Color ColorFunction(float completionRatio)
        {
            Color color = Color.Lerp(Color.Orange, Color.DarkRed, 1f - Pow(completionRatio, 2f));
            color = Color.Lerp(color, Color.Red, 0.5f);
            return color * Projectile.Opacity * 0.7f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Color color = Projectile.GetAlpha(Color.White);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Main.spriteBatch.Draw(texture, drawPosition, null, color with { A = 0 }, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, 0, 0f);
            Main.spriteBatch.Draw(texture, drawPosition, null, Color.White with { A = 0 } * Projectile.Opacity * 0.7f, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, 0, 0f);
            return false;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            Color c = Color.Lerp(Color.Orange, Color.Red, Projectile.identity % 10f / 20f + 0.34f);
            return c * 1.18f;
        }

        public override void OnKill(int timeLeft)
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
