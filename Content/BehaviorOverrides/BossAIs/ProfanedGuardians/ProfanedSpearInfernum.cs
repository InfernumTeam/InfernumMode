using CalamityMod;
using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class ProfanedSpearInfernum : ModProjectile
    {
        public ref float Timer => ref Projectile.ai[0];

        public Projectile Parent => Main.projectile[(int)Projectile.ai[1]];

        public Vector2 OriginalVelocity;

        public int TelegraphDuration => 30;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Profaned Spear");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.timeLeft = 300;
            Projectile.Calamity().DealsDefenseDamage = true;
        }

        public override void AI()
        {
            if (Timer == 0)
            {
                OriginalVelocity = Projectile.velocity;
                Projectile.velocity = Vector2.Zero;
            }
            else if (Timer == TelegraphDuration)
                Projectile.velocity = OriginalVelocity;

            Projectile.tileCollide = Timer - TelegraphDuration > 90;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.08f, 0f, 1f);

            if (Timer > TelegraphDuration)
            {
                // Accelerate.
                if (Projectile.velocity.Length() < 36f)
                    Projectile.velocity *= 1.028f;
            }
            else
            {
                if (!Parent.active && Parent.type != ModContent.ProjectileType<HolyPushbackWall>())
                {
                    Projectile.Kill();
                    return;
                }
                Projectile.Center = new(Parent.Center.X, Projectile.Center.Y);
            }

            Lighting.AddLight(Projectile.Center, Vector3.One);
            Timer++;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            if (Timer < TelegraphDuration)
            {
                Texture2D texture = InfernumTextureRegistry.BloomLineSmall.Value;
                Vector2 position = Projectile.Center - Main.screenPosition;
                Color colorInner = Color.Gold * 0.75f;
                colorInner.A = 0;
                Color colorOuter = Color.Lerp(colorInner, Color.White, 0.5f) * 0.75f;
                colorOuter.A = 0;
                float rotation = MathHelper.PiOver2;

                float scaleInterpolant = MathHelper.Clamp(MathF.Sin(Timer / TelegraphDuration * MathHelper.Pi) * 3f, 0f, 1f);
                Vector2 scaleInner = new(0.75f * scaleInterpolant, 2550f / texture.Height);
                Vector2 scaleOuter = scaleInner * new Vector2(1.5f, 1f);
                Vector2 origin = texture.Size() * new Vector2(0.5f, 0f);
                Main.EntitySpriteDraw(texture, position, null, colorOuter, rotation, origin, scaleOuter, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(texture, position, null, colorInner, rotation, origin, scaleInner, SpriteEffects.None, 0);
            }
            else
            {
                float alpha = 1f - (float)Projectile.alpha / 255;
                CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor * alpha, 1);
                Projectile.DrawProjectileWithBackglowTemp(Color.White with { A = 0 }, Color.White, 2f);
            }
            return false;
        }
    }
}
