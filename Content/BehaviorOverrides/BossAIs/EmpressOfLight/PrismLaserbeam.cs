using CalamityMod;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class PrismLaserbeam : ModProjectile, IPixelPrimitiveDrawer
    {
        public PrimitiveTrailCopy RayDrawer;

        public Projectile Prism
        {
            get
            {
                int prismID = ModContent.ProjectileType<EmpressPrism>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].identity != Projectile.ai[0] || !Main.projectile[i].active || Main.projectile[i].type != prismID)
                        continue;

                    return Main.projectile[i];
                }
                return null;
            }
        }

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public const float MaxLaserLength = 3330f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Prismatic Ray");
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            Projectile.netImportant = true;
            Projectile.scale = 0.4f;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // If the prism is no longer able to cast the beam, kill it.
            if (Prism is null)
            {
                Projectile.Kill();
                return;
            }

            // Grow bigger up to a point.
            float maxScale = Lerp(0.051f, 1.5f, Utils.GetLerpValue(0f, 30f, Prism.timeLeft, true));
            Projectile.scale = Clamp(Projectile.scale + 0.04f, 0.05f, maxScale);

            // Decide where to position the laserbeam.
            Projectile.Center = Prism.Top;

            // Make the beam cast light along its length. The brightness of the light is reliant on the scale of the beam.
            DelegateMethods.v3_1 = Color.White.ToVector3() * Projectile.scale * 0.6f;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * MaxLaserLength, Projectile.width * Projectile.scale, DelegateMethods.CastLight);
        }

        internal float PrimitiveWidthFunction(float completionRatio) => Projectile.scale * 20f;

        internal Color PrimitiveColorFunction(float completionRatio)
        {
            float hue = (Projectile.ai[1] + Sin(Main.GlobalTimeWrappedHourly * 12.2f) * 0.04f) % 1f;
            float opacity = Projectile.Opacity * Utils.GetLerpValue(0.97f, 0.9f, completionRatio, true);
            Color c = Main.hslToRgb(hue, 1f, 0.7f) * opacity;
            c.A /= 16;

            return c;
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            RayDrawer ??= new(PrimitiveWidthFunction, PrimitiveColorFunction, specialShader: InfernumEffectsRegistry.PrismaticRayVertexShader);

            InfernumEffectsRegistry.PrismaticRayVertexShader.UseImage1("Images/Misc/Perlin");
            Main.instance.GraphicsDevice.Textures[2] = InfernumTextureRegistry.StreakSolid.Value;

            Vector2[] basePoints = new Vector2[24];
            for (int i = 0; i < basePoints.Length; i++)
                basePoints[i] = Projectile.Center + Projectile.velocity * i / (basePoints.Length - 1f) * MaxLaserLength;

            Vector2 overallOffset = -Main.screenPosition;
            RayDrawer.DrawPixelated(basePoints, overallOffset, 13);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + Projectile.velocity * MaxLaserLength, Projectile.scale * 25f, ref _);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overWiresUI.Add(index);
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
