using CalamityMod;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics;
using InfernumMode.Content.Projectiles.Wayfinder;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class ProfanedLavaWave : ModProjectile, IPixelPrimitiveDrawer
    {
        public PrimitiveTrailCopy TelegraphDrawer = null;
        public PrimitiveTrailCopy LavaDrawer = null;

        public int Lifetime = 120;

        public static float TelegraphTime = 45f;

        public static float MoveTime = 120f;

        public float MaxHeight = 2300f;

        public float Length = 14000f;

        public ref float Timer => ref Projectile.ai[0];

        public ref float WaveHeight => ref Projectile.ai[1];


        public override string Texture => InfernumTextureRegistry.InvisPath;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Lava Wave");
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.hide = true;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            if (Timer >= TelegraphTime && Timer <= TelegraphTime + MoveTime)
            {
                WaveHeight = CalamityUtils.SineInOutEasing((Timer - TelegraphTime) / MoveTime, 0) * MaxHeight;
            }
            else if (Timer <= Lifetime && Timer >= Lifetime - MoveTime)
            {
                WaveHeight = (1f - CalamityUtils.SineInOutEasing((Timer - (Lifetime - MoveTime)) / MoveTime, 0)) * MaxHeight;
            }
            Timer++;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => overPlayers.Add(index);

        public override bool ShouldUpdatePosition() => false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 start = Projectile.Center;
            Vector2 end = start - Vector2.UnitX * Length;
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, WaveHeight, ref _);
        }

        internal float WidthFunction(float completionRatio) => WaveHeight;

        internal Color ColorFunction(float completionRatio)
        {
            //return Color.OrangeRed;
            float sine = (1f + MathF.Sin(Main.GlobalTimeWrappedHourly)) / 2f;
            return Color.Lerp(WayfinderSymbol.Colors[1], Color.Lerp(WayfinderSymbol.Colors[0], WayfinderSymbol.Colors[1], 1f), sine);
        }

        internal Vector2 OffsetFunction(float completionRatio) => new Vector2(0f, WaveHeight / MaxHeight) * (float)Math.Sin(completionRatio * 300 + Main.GlobalTimeWrappedHourly * 4f) * 60f;

        internal float TelegraphWidthFunction(float completionRatio) => MaxHeight * 1.45f;

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2[] drawPositions = new Vector2[10];
            for (int i = 0; i < drawPositions.Length; i++)
                drawPositions[i] = Vector2.Lerp(Projectile.Center, Projectile.Center - Vector2.UnitX * Length, (float)i / drawPositions.Length);

            if (Timer <= TelegraphTime + 20f)
            {
                //Texture2D texture = InfernumTextureRegistry.SolidEdgeGradient.Value;
                //float opacity = (1f + MathF.Sin(Timer / TelegraphTime * MathF.PI)) / 2f;

                //Main.NewText(opacity);
                //Color color = WayfinderSymbol.Colors[1] * opacity;
                //Vector2 scale = new(Length, 7.8f);
                //Main.spriteBatch.Draw(texture, Projectile.Center - (Vector2.UnitX * Length * 0.5f) - Main.screenPosition, null, color with { A = 0 }, 0f, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);

                TelegraphDrawer ??= new(TelegraphWidthFunction, ColorFunction, OffsetFunction, true, InfernumEffectsRegistry.TelegraphVertexShader);
                InfernumEffectsRegistry.TelegraphVertexShader.SetShaderTexture(InfernumTextureRegistry.CrustyNoise);
                float opacityScalar = MathF.Sin(CalamityUtils.SineInOutEasing(Timer / (TelegraphTime + 20f), 0) * MathF.PI);
                InfernumEffectsRegistry.TelegraphVertexShader.UseOpacity(0.5f * opacityScalar);

                TelegraphDrawer.Draw(drawPositions, -Main.screenPosition, 40);
            }
            else
            {
                LavaDrawer ??= new(WidthFunction, ColorFunction, OffsetFunction, true, InfernumEffectsRegistry.LavaVertexShader);
                InfernumEffectsRegistry.LavaVertexShader.SetShaderTexture(InfernumTextureRegistry.GrayscaleWater);
                InfernumEffectsRegistry.LavaVertexShader.UseColor(Color.Lerp(WayfinderSymbol.Colors[0], Color.White, 0.5f));

                LavaDrawer.Draw(drawPositions, -Main.screenPosition, 40);
            }
            return false;
        }

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {

        }
    }
}
