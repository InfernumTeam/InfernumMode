using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class HotMetal : ModProjectile, IPixelPrimitiveDrawer
    {
        internal PrimitiveTrailCopy TrailDrawer;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Superheated Metal");
            Main.projFrames[Type] = 3;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 26;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 360;
            Projectile.Opacity = 0f;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Initialize frames.
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.frame = Main.rand.Next(Main.projFrames[Type]);
                Projectile.localAI[0] = 1f;
            }

            Projectile.Opacity = Clamp(Projectile.Opacity + 0.067f, 0f, 1f);
            Projectile.rotation += Projectile.velocity.Y * 0.02f;

            Projectile.velocity.X *= 0.993f;
            if (Projectile.velocity.Y < 11f)
                Projectile.velocity.Y += 0.25f;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return Color.Lerp(Color.White with { A = 192 }, Color.Red with { A = 156 }, Projectile.ai[0]) * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Rectangle frame = TextureAssets.Projectile[Projectile.type].Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Projectile.DrawProjectileWithBackglowTemp(Color.White with { A = 0 }, Color.White, 4f, frame);
            return false;
        }

        public float WidthFunction(float completionRatio)
        {
            float baseWidth = Projectile.scale * Projectile.width * 1.5f;
            return SmoothStep(baseWidth, 3.5f, completionRatio);
        }

        public Color ColorFunction(float completionRatio) => Color.Lerp(Color.Lerp(Color.Red, Color.OrangeRed, 0.5f), Color.Transparent, completionRatio) * 0.7f * Projectile.Opacity;

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            TrailDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);
            GameShaders.Misc["CalamityMod:ImpFlameTrail"].UseImage1("Images/Extra_189");
            TrailDrawer.DrawPixelated(Projectile.oldPos, Projectile.Size * 0.5f - Main.screenPosition, 25);
        }
    }
}
