using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
    public class StarFuelProjectileThingIdk : ModProjectile
    {
        public Color FuelColor;
        public override void SetStaticDefaults() => DisplayName.SetDefault("Star Fueler");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 14;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.timeLeft = 360;
            projectile.Opacity = 0f;
            projectile.hide = true;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.18f, 0f, 1f);
            projectile.rotation += projectile.velocity.X * 0.025f;

            // Emit dust.
            for (int i = 0; i < 2; i++)
            {
                Dust fire = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 267);
                fire.scale *= 0.8f;
                fire.color = FuelColor;
                fire.velocity = fire.velocity * 0.4f + Main.rand.NextVector2Circular(0.4f, 0.4f);
                fire.fadeIn = 0.4f;
                fire.noGravity = true;
            }

            List<Projectile> stars = Utilities.AllProjectilesByID(ModContent.ProjectileType<ElectromagneticStar>()).ToList();
            if (stars.Count == 0 || stars.First().scale > 7f)
            {
                projectile.Kill();
                return;
            }

            if (projectile.WithinRange(stars.First().Center, (stars.First().modProjectile as ElectromagneticStar).Radius * 0.925f))
                projectile.Kill();

            projectile.velocity = projectile.velocity.RotateTowards(projectile.AngleTo(stars.First().Center), 0.085f) * 1.02f;
            projectile.velocity = projectile.velocity.MoveTowards(projectile.SafeDirectionTo(stars.First().Center) * 30f, projectile.velocity.Length() / 20f);
            projectile.rotation = projectile.rotation.AngleLerp(projectile.velocity.ToRotation() + MathHelper.PiOver2, 0.2f);
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(FuelColor.R, FuelColor.G, FuelColor.B, 0) * projectile.Opacity;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 origin = texture.Size() * 0.5f;

            for (int i = 0; i < 4; i++)
            {
                Vector2 drawOffset = -projectile.velocity.SafeNormalize(Vector2.Zero) * i * 12f;
                Vector2 afterimageDrawPosition = projectile.Center + drawOffset - Main.screenPosition;
                Color backAfterimageColor = projectile.GetAlpha(lightColor) * ((4f - i) / 4f);
                spriteBatch.Draw(texture, afterimageDrawPosition, null, backAfterimageColor, projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0f);
            }

            Color frontAfterimageColor = projectile.GetAlpha(lightColor) * 0.2f;
            for (int i = 0; i < 9; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 9f + projectile.rotation - MathHelper.PiOver2).ToRotationVector2() * 2f;
                Vector2 afterimageDrawPosition = projectile.Center + drawOffset - Main.screenPosition;
                spriteBatch.Draw(texture, afterimageDrawPosition, null, frontAfterimageColor, projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0f);
            }
            return false;
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI)
        {
            drawCacheProjsBehindProjectiles.Add(index);
        }
    }
}
