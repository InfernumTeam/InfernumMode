using CalamityMod;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using NuclearTerrorNPC = CalamityMod.NPCs.AcidRain.NuclearTerror;

namespace InfernumMode.Content.BehaviorOverrides.MinibossAIs.NuclearTerror
{
    public class GammaSuperDeathray : ModProjectile, IPixelPrimitiveDrawer
    {
        public PrimitiveTrailCopy BeamDrawer
        {
            get;
            set;
        }

        public float LaserLength
        {
            get;
            set;
        }

        public ref float Time => ref Projectile.ai[0];

        public ref float Lifetime => ref Projectile.ai[1];

        public const float MaxLaserLength = 4000f;

        public override string Texture => InfernumTextureRegistry.InvisPath;

        public override void SetStaticDefaults() => DisplayName.SetDefault("Gamma Deathray");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 96;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.netImportant = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 9000;
            Projectile.alpha = 255;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(LaserLength);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            LaserLength = reader.ReadSingle();
        }

        public override void AI()
        {
            int nuclearTerrorIndex = NPC.FindFirstNPC(ModContent.NPCType<NuclearTerrorNPC>());
            if (!Main.npc.IndexInRange(nuclearTerrorIndex) || !Main.npc[nuclearTerrorIndex].active)
            {
                Projectile.Kill();
                return;
            }

            NPC nuclearTerror = Main.npc[nuclearTerrorIndex];
            Projectile.Center = nuclearTerror.Center + new Vector2(nuclearTerror.spriteDirection * -60f, -40f).RotatedBy(nuclearTerror.rotation);

            // Fade in.
            Projectile.alpha = Utils.Clamp(Projectile.alpha - 25, 0, 255);

            // Determine the scale of the laser.
            CalculateScale();

            float moveInterpolant = Utils.GetLerpValue(5f, 25f, Time, true);
            Projectile.velocity = (Projectile.velocity - Vector2.UnitY * moveInterpolant * 0.04f).SafeNormalize(-Vector2.UnitY);
            if (Time >= Lifetime)
                Projectile.Kill();

            // Make the laser quickly move outward.
            LaserLength = MathF.Pow(Utils.GetLerpValue(4f, 30f, Time, true), 2.4f) * MaxLaserLength;

            // And create bright light.
            Lighting.AddLight(Projectile.Center, Color.Purple.ToVector3() * 1.4f);

            Time++;
        }

        public void CalculateScale()
        {
            Projectile.scale = CalamityUtils.Convert01To010(Time / Lifetime) * 1.45f;
            if (Projectile.scale > 1f)
                Projectile.scale = 1f;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            float width = Projectile.width * 0.8f;
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity * (MaxLaserLength - 80f);
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, width, ref _);
        }

        public float WidthFunction(float completionRatio)
        {
            float squeezeInterpolant = Utils.GetLerpValue(1f, 0.92f, completionRatio, true);
            return MathHelper.SmoothStep(2f, Projectile.width, squeezeInterpolant) * MathHelper.Clamp(Projectile.scale, 0.04f, 1f);
        }

        public Color ColorFunction(float completionRatio)
        {
            Color color = Color.Lerp(Color.Yellow, Color.DarkOliveGreen, 0.6f);
            return color * Projectile.Opacity * 1.15f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            DrawFrontGlow();
            DrawBloomFlare();
            Main.spriteBatch.ResetBlendState();

            return false;
        }

        public void DrawFrontGlow()
        {
            float pulse = MathF.Cos(Main.GlobalTimeWrappedHourly * 36f);
            Texture2D backglowTexture = ModContent.Request<Texture2D>("CalamityMod/Skies/XerocLight").Value;
            Vector2 origin = backglowTexture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + Vector2.UnitY * Projectile.scale * 20f;
            Vector2 baseScale = new Vector2(1f + pulse * 0.05f, 1f) * Projectile.scale * 0.7f;
            Main.spriteBatch.Draw(backglowTexture, drawPosition, null, Color.White, 0f, origin, baseScale * 0.7f, 0, 0f);
            Main.spriteBatch.Draw(backglowTexture, drawPosition, null, Color.Yellow * 0.4f, 0f, origin, baseScale * 1.2f, 0, 0f);
            Main.spriteBatch.Draw(backglowTexture, drawPosition, null, Color.Orange * 0.3f, 0f, origin, baseScale * 1.7f, 0, 0f);
        }

        public void DrawBloomFlare()
        {
            Texture2D bloomFlare = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleObjects/BloomFlare").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + Vector2.UnitY * Projectile.scale * 20f;
            Color bloomFlareColor = Color.Lerp(Color.Wheat, Color.Lime, 0.7f);
            float bloomFlareRotation = Main.GlobalTimeWrappedHourly * 1.76f;
            float bloomFlareScale = Projectile.scale * 0.33f;
            Main.spriteBatch.Draw(bloomFlare, drawPosition, null, bloomFlareColor, -bloomFlareRotation, bloomFlare.Size() * 0.5f, bloomFlareScale, 0, 0f);

            bloomFlareColor = Color.Lerp(Color.Wheat, Main.hslToRgb((Main.GlobalTimeWrappedHourly * 0.2f + 0.5f) % 1f, 1f, 0.55f), 0.7f);
            bloomFlareColor = Color.Lerp(bloomFlareColor, Color.DarkOliveGreen, 0.63f);
            Main.spriteBatch.Draw(bloomFlare, drawPosition, null, bloomFlareColor, bloomFlareRotation, bloomFlare.Size() * 0.5f, bloomFlareScale, 0, 0f);
        }

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            BeamDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.ArtemisLaserVertexShader);

            var oldBlendState = Main.instance.GraphicsDevice.BlendState;
            Main.instance.GraphicsDevice.BlendState = BlendState.Additive;
            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseSaturation(1.5f);
            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseOpacity(-0.1f);
            InfernumEffectsRegistry.ArtemisLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.CrustyNoise);
            InfernumEffectsRegistry.ArtemisLaserVertexShader.Shader.Parameters["uStretchReverseFactor"].SetValue((LaserLength + 1f) / MaxLaserLength);

            List<float> originalRotations = new();
            List<Vector2> points = new();
            for (int i = 0; i <= 16; i++)
            {
                points.Add(Vector2.Lerp(Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, i / 16f));
                originalRotations.Add(MathHelper.PiOver2);
            }

            if (Time >= 2f)
            {
                float backwardsOffset = MathHelper.Min(LaserLength * 0.1f, 100f);
                BeamDrawer.DrawPixelated(points, -Main.screenPosition - Projectile.velocity * backwardsOffset, 47);
            }
            Main.instance.GraphicsDevice.BlendState = oldBlendState;
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
