using CalamityMod;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Primitives;
using InfernumMode.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.MoonLord
{
    public class PhantasmalDeathray : ModProjectile, IPixelPrimitiveDrawer
    {
        internal PrimitiveTrailCopy BeamDrawer;

        public int OwnerIndex;

        public ref float Time => ref Projectile.ai[0];

        public ref float Lifetime => ref Projectile.ai[1];

        public ref float InitialRotationalOffset => ref Projectile.localAI[0];

        public const float LaserLength = 4000f;
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults() => DisplayName.SetDefault("Phantasmal Deathray");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 9000;
            Projectile.alpha = 255;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(OwnerIndex);
            writer.Write(InitialRotationalOffset);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            OwnerIndex = reader.ReadInt32();
            InitialRotationalOffset = reader.ReadSingle();
        }

        public override void AI()
        {
            if (OwnerIndex <= 0 || Main.npc[OwnerIndex - 1].ai[0] == -2f)
            {
                Projectile.Kill();
                return;
            }

            NPC head = Main.npc[OwnerIndex - 1];
            Projectile.Center = head.Center + new Vector2(-6f, -10f);

            // Fade in.
            Projectile.alpha = Utils.Clamp(Projectile.alpha - 25, 0, 255);

            Projectile.scale = Sin(Pi * Time / Lifetime) * 4f;
            if (Projectile.scale > 1f)
                Projectile.scale = 1f;
            if (Time >= Lifetime)
                Projectile.Kill();

            // And create bright light.
            Lighting.AddLight(Projectile.Center, Color.Purple.ToVector3() * 1.4f);

            Time++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            float width = Projectile.width * 0.6f;
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity * (LaserLength - 80f);
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, width, ref _);
        }

        public float WidthFunction(float completionRatio)
        {
            float squeezeInterpolant = Utils.GetLerpValue(1f, 0.92f, completionRatio, true);
            return SmoothStep(2f, Projectile.width, squeezeInterpolant) * Clamp(Projectile.scale, 0.04f, 1f);
        }

        public Color ColorFunction(float completionRatio)
        {
            Color color = Color.Lerp(Color.Turquoise, Color.Cyan, Pow(completionRatio, 2f));
            return color * Projectile.Opacity * 1.1f;
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            BeamDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.FireVertexShader);

            var oldBlendState = Main.instance.GraphicsDevice.BlendState;
            Main.instance.GraphicsDevice.BlendState = BlendState.Additive;
            InfernumEffectsRegistry.FireVertexShader.UseSaturation(1.4f);
            InfernumEffectsRegistry.FireVertexShader.SetShaderTexture(InfernumTextureRegistry.CultistRayMap);

            List<float> originalRotations = new();
            List<Vector2> points = new();
            for (int i = 0; i <= 8; i++)
            {
                points.Add(Vector2.Lerp(Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, i / 8f) - Projectile.velocity * 500f);
                originalRotations.Add(PiOver2);
            }

            if (Time >= 2f)
            {
                int pointCount = InfernumConfig.Instance.ReducedGraphicsConfig ? 15 : 26;
                BeamDrawer.DrawPixelated(points, Projectile.Size * 0.5f - Main.screenPosition, pointCount);
            }
            Main.instance.GraphicsDevice.BlendState = oldBlendState;
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
