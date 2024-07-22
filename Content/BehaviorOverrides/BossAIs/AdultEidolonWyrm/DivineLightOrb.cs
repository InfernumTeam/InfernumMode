using System.Collections.Generic;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class DivineLightOrb : ModProjectile, IAboveWaterProjectileDrawer
    {
        public PrimitiveTrailCopy FireDrawer;

        public NPC Owner => Main.npc.IndexInRange((int)Projectile.ai[1]) && Main.npc[(int)Projectile.ai[1]].active ? Main.npc[(int)Projectile.ai[1]] : null;

        public float Radius => Owner.Infernum().ExtraAI[0];

        public ref float Time => ref Projectile.ai[0];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Divine Light Orb");
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 164;
            Projectile.height = 164;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 9000;
            Projectile.scale = 0.2f;
            
        }

        public override void AI()
        {
            if (Owner is null)
            {
                Projectile.Kill();
                return;
            }

            // Hover in front of the owner.
            Projectile.Center = GetHoverDestination(Owner);

            Time++;
        }

        public static Vector2 GetHoverDestination(NPC owner)
        {
            return owner.Center + (owner.rotation - PiOver2).ToRotationVector2() * owner.scale * 108f; ;
        }

        public float OrbWidthFunction(float completionRatio) => SmoothStep(0f, Radius, Sin(Pi * completionRatio));

        public Color OrbColorFunction(float completionRatio)
        {
            Color c = Color.Lerp(Color.Yellow, Color.Pink, Lerp(0.2f, 0.8f, Projectile.localAI[0] % 1f));
            c = Color.Lerp(c, Color.White, completionRatio * 0.5f);
            c.A = 0;
            return c;
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawAboveWater(SpriteBatch spriteBatch)
        {
            if (Owner is null || !Owner.active)
                return;

            FireDrawer ??= new PrimitiveTrailCopy(OrbWidthFunction, OrbColorFunction, null, true, InfernumEffectsRegistry.PrismaticRayVertexShader);
            InfernumEffectsRegistry.PrismaticRayVertexShader.UseOpacity(0.25f);
            InfernumEffectsRegistry.PrismaticRayVertexShader.UseImage1("Images/Misc/Perlin");
            Main.instance.GraphicsDevice.Textures[2] = InfernumTextureRegistry.StreakSolid.Value;

            List<float> rotationPoints = [];
            List<Vector2> drawPoints = [];

            spriteBatch.EnterShaderRegion();
            for (float offsetAngle = -PiOver2; offsetAngle <= PiOver2; offsetAngle += Pi / 30f)
            {
                Projectile.localAI[0] = Clamp((offsetAngle + PiOver2) / Pi, 0f, 1f);

                rotationPoints.Clear();
                drawPoints.Clear();

                float adjustedAngle = offsetAngle + LumUtils.PerlinNoise2D(offsetAngle, Main.GlobalTimeWrappedHourly * 0.02f, 3, 185) * 3f;
                Vector2 offsetDirection = adjustedAngle.ToRotationVector2();
                for (int i = 0; i < 8; i++)
                {
                    rotationPoints.Add(adjustedAngle);
                    drawPoints.Add(Vector2.Lerp(Projectile.Center - offsetDirection * Radius / 2f, Projectile.Center + offsetDirection * Radius / 2f, i / 7f));
                }

                FireDrawer.Draw(drawPoints, -Main.screenPosition, 30);
            }
            spriteBatch.ExitShaderRegion();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => Utilities.CircularCollision(Projectile.Center, targetHitbox, Radius * 0.85f);
    }
}
