using CalamityMod;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Thanatos
{
    public class LightOverloadRay : ModProjectile
    {
        public PrimitiveTrailCopy LaserDrawer;
        public static NPC Thanatos => Main.npc[CalamityGlobalNPC.draedonExoMechWorm];
        public Vector2 StartingPosition => Thanatos.Center - (Thanatos.rotation - MathHelper.PiOver2).ToRotationVector2() * projectile.Opacity * 5f;

        // This is only used in drawing to represent increments as a semi-hack. Don't mess with it.
        public float RayHue = 0f;

        public const int Lifetime = 65;
        public ref float Time => ref projectile.ai[0];
        public ref float LaserSpread => ref projectile.ai[1];
        public ref float LaserLength => ref projectile.localAI[0];
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Prismatic Overload Beam");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 14;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = Lifetime;
            projectile.hostile = true;
        }

        public override void AI()
        {
            if (CalamityGlobalNPC.draedonExoMechWorm == -1)
            {
                projectile.Kill();
                return;
            }

            DelegateMethods.v3_1 = Vector3.One * 0.7f;
            Utils.PlotTileLine(StartingPosition, StartingPosition + (Thanatos.rotation - MathHelper.PiOver2).ToRotationVector2() * projectile.Opacity * 400f, 8f, DelegateMethods.CastLight);

            projectile.Opacity = Utils.InverseLerp(0f, 10f, Time, true) * Utils.InverseLerp(0f, 8f, projectile.timeLeft, true);
            projectile.velocity = Vector2.Zero;
            projectile.Center = StartingPosition;
            LaserLength = MathHelper.Lerp(LaserLength, 9000f, 0.1f);
            Time++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (projectile.Opacity < 1f)
                return false;

            for (int i = 0; i < 45; i++)
            {
                float _ = 0f;
                float offsetAngle = MathHelper.Lerp(-LaserSpread, LaserSpread, i / 44f);
                Vector2 start = StartingPosition;
                Vector2 end = start + (Thanatos.rotation - MathHelper.PiOver2 + offsetAngle).ToRotationVector2() * LaserLength;
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, projectile.scale * 50f, ref _))
                    return true;
            }
            return false;
        }

        public float LaserWidthFunction(float completionRatio) => projectile.Opacity * Utils.InverseLerp(0.96f, 0.8f, completionRatio, true) * 45f;

        public Color LaserColorFunction(float completionRatio)
        {
            float hue = (RayHue * 2.4f + Main.GlobalTime * 0.77f) % 1f;
            Color color = CalamityUtils.MulticolorLerp(hue, CalamityUtils.ExoPalette);
            color = Color.Lerp(color, Color.Wheat, 0.4f) * projectile.Opacity;
            color *= Utils.InverseLerp(0.96f, 0.8f, completionRatio, true);
            return color;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Main.spriteBatch.SetBlendState(BlendState.Additive);

            if (LaserDrawer is null)
                LaserDrawer = new PrimitiveTrailCopy(LaserWidthFunction, LaserColorFunction, null, true, GameShaders.Misc["Infernum:Fire"]);

            GameShaders.Misc["Infernum:Fire"].UseSaturation(0.14f);
            GameShaders.Misc["Infernum:Fire"].SetShaderTexture(ModContent.GetTexture("InfernumMode/ExtraTextures/CultistRayMap"));

            List<float> rotationPoints = new List<float>();
            List<Vector2> drawPoints = new List<Vector2>();

            var oldBlendState = Main.instance.GraphicsDevice.BlendState;
            Main.instance.GraphicsDevice.BlendState = BlendState.Additive;
            for (int i = 0; i < 45; i++)
            {
                RayHue = i / 44f;
                rotationPoints.Clear();
                drawPoints.Clear();

                float offsetAngle = Thanatos.rotation - MathHelper.PiOver2 + MathHelper.Lerp(-LaserSpread * projectile.Opacity, LaserSpread * projectile.Opacity, i / 44f);
                for (int j = 0; j < 8; j++)
                {
                    rotationPoints.Add(offsetAngle);
                    Vector2 start = StartingPosition;
                    Vector2 end = start + offsetAngle.ToRotationVector2() * LaserLength;
                    drawPoints.Add(Vector2.Lerp(start, end, j / 8f));
                }

                LaserDrawer.Draw(drawPoints, -Main.screenPosition, 20);
                LaserDrawer.Draw(drawPoints, -Main.screenPosition, 20);
            }
            Main.instance.GraphicsDevice.BlendState = oldBlendState;
            return false;
        }

        public override Color? GetAlpha(Color lightColor) => new Color(projectile.Opacity, projectile.Opacity, projectile.Opacity, 0);
    }
}
