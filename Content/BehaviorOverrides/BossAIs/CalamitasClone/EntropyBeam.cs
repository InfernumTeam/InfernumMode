using CalamityMod;
using CalamityMod.NPCs;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasClone
{
    public class EntropyBeam : ModProjectile, IPixelPrimitiveDrawer
    {
        public PrimitiveTrailCopy BeamDrawer
        {
            get;
            set;
        }

        public ref float Time => ref Projectile.ai[0];

        public ref float LaserLength => ref Projectile.ai[1];

        public const int Lifetime = 90;

        public const float MaxLaserLength = 5500f;

        public static NPC CalClone => Main.npc[CalamityGlobalNPC.calamitas];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Entropic Beam");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 42;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.alpha = 255;
            Projectile.Calamity().DealsDefenseDamage = true;
        }

        public override void AI()
        {
            // Disappear if CalClone is not present.
            if (CalamityGlobalNPC.calamitas == -1)
            {
                Projectile.Kill();
                return;
            }

            // Fade in.
            Projectile.alpha = Utils.Clamp(Projectile.alpha - 25, 0, 255);

            Projectile.scale = CalamityUtils.Convert01To010(Projectile.timeLeft / (float)Lifetime) * 3f;
            if (Projectile.scale > 1f)
                Projectile.scale = 1f;

            // Calculate the laser length.
            LaserLength = Utils.GetLerpValue(-1f, 10f, Time, true) * MaxLaserLength;

            // Inherit the direction from CalClone's arm.
            Projectile.velocity = (CalClone.Infernum().ExtraAI[CalamitasCloneBehaviorOverride.ArmRotationIndex] + MathHelper.PiOver2).ToRotationVector2();
            Projectile.BottomRight = CalClone.Center;

            Time++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            float width = Projectile.width * 0.8f;
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity * (LaserLength - 80f) * 0.65f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, width, ref _);
        }

        public float WidthFunction(float completionRatio)
        {
            float squeezeInterpolant = Utils.GetLerpValue(1f, 0.95f, completionRatio, true);
            float baseWidth = MathHelper.SmoothStep(2f, Projectile.width, squeezeInterpolant) * MathHelper.Clamp(Projectile.scale, 0.01f, 1f);
            return baseWidth * MathHelper.Lerp(1f, 2.3f, Projectile.localAI[0]);
        }

        public override bool ShouldUpdatePosition() => false;

        public Color ColorFunction(float completionRatio)
        {
            float opacity = Utils.GetLerpValue(0.97f, 0.6f, completionRatio, true) * MathHelper.Lerp(1f, 0.45f, Projectile.localAI[0]) * Projectile.Opacity * 0.95f;
            Color color = Color.Lerp(Color.Red, Color.Yellow, (float)Math.Abs(Math.Sin(completionRatio * MathHelper.Pi * 10f - 4f * Main.GlobalTimeWrappedHourly)) * 0.5f + 0.2f);
            return color * opacity;
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            BeamDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.ArtemisLaserVertexShader);

            // Select textures to pass to the shader, along with the electricity color.
            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseColor(Color.Red);
            InfernumEffectsRegistry.ArtemisLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakMagma);
            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseImage2("Images/Misc/Perlin");
            InfernumEffectsRegistry.ArtemisLaserVertexShader.Shader.Parameters["uStretchReverseFactor"].SetValue((LaserLength + 1f) / MaxLaserLength * 4f);

            List<Vector2> points = new();
            for (int i = 0; i <= 8; i++)
                points.Add(Vector2.Lerp(Projectile.Center - Projectile.velocity * 18f, Projectile.Center + Projectile.velocity * LaserLength, i / 8f));

            BeamDrawer.DrawPixelated(points, Projectile.Size * 0.5f - Main.screenPosition, 60);
            Main.spriteBatch.ExitShaderRegion();
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Time >= 8f;
    }
}
