using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class MagicCrystalShot : ModProjectile, IScreenCullDrawer
    {
        public Color StreakBaseColor => LumUtils.MulticolorLerp(Projectile.localAI[0] % 0.999f, MagicSpiralCrystalShot.ColorSet);

        public ref float Timer => ref Projectile.ai[0];

        public ref float Direction => ref Projectile.ai[1];

        public const int TelegraphLength = 30;

        public override string Texture => "CalamityMod/Projectiles/StarProj";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Crystalline Light");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 12;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 30;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            
        }

        public override void AI()
        {
            if (Projectile.timeLeft < 15)
                Projectile.damage = 0;

            if (Projectile.velocity.Length() < 40f)
                Projectile.velocity *= 1.04f;
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.1f, 0f, 1f);
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;
            Timer++;
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void CullDraw(SpriteBatch spriteBatch)
        {
            if (Timer <= TelegraphLength)
            {
                float interpolant = Timer / TelegraphLength;
                float scalar = Sin(interpolant * PI);
                float yScale = Lerp(0f, 1f, scalar);
                Color telegraphColor = StreakBaseColor;
                telegraphColor.A = 0;
                Texture2D telegraphTexture = InfernumTextureRegistry.BloomLineSmall.Value;
                Vector2 scaleInner = new(yScale, InfernumTextureRegistry.BloomLineSmall.Value.Height);
                Vector2 scaleOuter = scaleInner * new Vector2(1.5f, 1f);
                Vector2 origin = InfernumTextureRegistry.BloomLineSmall.Value.Size() * new Vector2(0.5f, 0f);

                Main.spriteBatch.Draw(telegraphTexture, Projectile.Center - Main.screenPosition, null, Color.HotPink with { A = 0 } * 2, Projectile.velocity.ToRotation() + PiOver2, origin, scaleOuter, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(telegraphTexture, Projectile.Center - Main.screenPosition, null, telegraphColor * 2, Projectile.velocity.ToRotation() + PiOver2, origin, scaleInner, SpriteEffects.None, 0f);
            }

            Texture2D streakTexture = TextureAssets.Projectile[Projectile.type].Value;
            for (int i = 1; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i - 1] == Vector2.Zero || Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completionRatio = i / (float)Projectile.oldPos.Length;
                float fade = Pow(completionRatio, 2f);
                float scale = Projectile.scale * Lerp(1.3f, 0.9f, Utils.GetLerpValue(0f, 0.24f, completionRatio, true)) *
                    Lerp(0.9f, 0.56f, Utils.GetLerpValue(0.5f, 0.78f, completionRatio, true));
                Color drawColor = Color.Lerp(StreakBaseColor, new Color(229, 255, 255), fade) * (1f - fade) * Projectile.Opacity;
                drawColor.A = 0;

                Vector2 drawPosition = Projectile.oldPos[i - 1] + Projectile.Size * 0.5f - Main.screenPosition;
                Vector2 drawPosition2 = Vector2.Lerp(drawPosition, Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition, 0.5f);
                Main.spriteBatch.Draw(streakTexture, drawPosition, null, drawColor, Projectile.oldRot[i], streakTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(streakTexture, drawPosition2, null, drawColor, Projectile.oldRot[i], streakTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }
        }
    }
}
