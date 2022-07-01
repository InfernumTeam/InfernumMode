using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class PhotonRipperCrystal : ModProjectile
    {
        public PrimitiveTrail TrailDrawer;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Supercharged Exo Crystal");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 20;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 34;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 360;
            Projectile.Opacity = 0f;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.1f, 0f, 1f);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (Projectile.velocity.Length() < 14f)
                Projectile.velocity *= 1.0225f;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public float WidthFunction(float completionRatio)
        {
            float squishFactor = Utils.GetLerpValue(1f, 0.7f, completionRatio, true) * Utils.GetLerpValue(0f, 0.12f, completionRatio, true);
            return Projectile.scale * squishFactor * 24f + 1f;
        }

        public Color ColorFunction(float completionRatio)
        {
            float hue = (Projectile.identity % 9f / 9f + completionRatio * 0.7f) % 1f;
            return Color.Lerp(Color.White, Main.hslToRgb(hue, 0.95f, 0.55f), 0.35f) * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (TrailDrawer is null)
                TrailDrawer = new PrimitiveTrail(WidthFunction, ColorFunction, specialShader: GameShaders.Misc["CalamityMod:PrismaticStreak"]);

            GameShaders.Misc["CalamityMod:PrismaticStreak"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/ScarletDevilStreak"));

            TrailDrawer.Draw(Projectile.oldPos, Projectile.Size * 0.5f - Main.screenPosition, 32);
            return true;
        }
    }
}
