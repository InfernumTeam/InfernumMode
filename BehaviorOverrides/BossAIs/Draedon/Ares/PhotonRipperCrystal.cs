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
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 20;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 34;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.hostile = true;
            projectile.timeLeft = 360;
            projectile.Opacity = 0f;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.1f, 0f, 1f);
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (projectile.velocity.Length() < 14f)
                projectile.velocity *= 1.0225f;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * projectile.Opacity;

        public float WidthFunction(float completionRatio)
        {
            float squishFactor = Utils.InverseLerp(1f, 0.7f, completionRatio, true) * Utils.InverseLerp(0f, 0.12f, completionRatio, true);
            return projectile.scale * squishFactor * 24f + 1f;
        }

        public Color ColorFunction(float completionRatio)
        {
            float hue = (projectile.identity % 9f / 9f + completionRatio * 0.7f) % 1f;
            return Color.Lerp(Color.White, Main.hslToRgb(hue, 0.95f, 0.55f), 0.35f) * projectile.Opacity;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (TrailDrawer is null)
                TrailDrawer = new PrimitiveTrail(WidthFunction, ColorFunction, specialShader: GameShaders.Misc["CalamityMod:PrismaticStreak"]);

            GameShaders.Misc["CalamityMod:PrismaticStreak"].SetShaderTexture(ModContent.GetTexture("CalamityMod/ExtraTextures/ScarletDevilStreak"));

            TrailDrawer.Draw(projectile.oldPos, projectile.Size * 0.5f - Main.screenPosition, 32);
            return true;
        }
    }
}
