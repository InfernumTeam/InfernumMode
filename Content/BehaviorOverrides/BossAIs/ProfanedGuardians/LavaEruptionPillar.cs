using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Primitives;
using InfernumMode.Content.Projectiles.Wayfinder;
using Luminance.Common.Easings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class LavaEruptionPillar : ModProjectile, IPixelPrimitiveDrawer
    {
        public PrimitiveTrailCopy LavaDrawer { get; private set; }

        public PrimitiveTrailCopy TelegraphDrawer { get; private set; }

        public int Lifetime => (int)(MaxTime + TelegraphLength);

        public override string Texture => InfernumTextureRegistry.InvisPath;

        public float MaxLength => BigVersion ? 5000f : 4000f;

        public const int TelegraphLength = 30;

        public float MaxTime => BigVersion ? 240 : 150;

        public bool BigVersion;

        public ref float Timer => ref Projectile.ai[0];

        public ref float CurrentLength => ref Projectile.ai[1];

        public ref float StretchOffset => ref Projectile.localAI[0];

        public float Width => BigVersion ? 700 : 150f;

        public float VariableWidth => Width * (BigVersion ? EasingCurves.Sine.InOutFunction(CurrentLength / MaxLength) : EasingCurves.Cubic.InOutFunction(CurrentLength / MaxLength));


        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            
        }

        public override void AI()
        {
            if (StretchOffset == 0)
            {
                Projectile.timeLeft = Lifetime;
                StretchOffset = Main.rand.NextFloat(-0.1f, 0f) * Main.rand.NextFromList(-1, 1);
            }
            if (Timer >= TelegraphLength - 20)
                CurrentLength = MaxLength * Sin((Timer - TelegraphLength - 10) / (Lifetime - TelegraphLength - 10) * PI);
            Timer++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 start = Projectile.Center;
            Vector2 end = start - Vector2.UnitY * CurrentLength;
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, VariableWidth * 0.95f, ref _);
        }

        public float WidthFunction(float completionRatio) => /*Timer < Lifetime / 2f ? Width :*/ VariableWidth;

        public static Color ColorFunction(float completionRatio)
        {
            float interpolant = (1f + Sin(Main.GlobalTimeWrappedHourly * 2f)) / 2f;
            float colorInterpolant = Lerp(0.3f, 0.5f, interpolant);
            return Color.Lerp(Color.OrangeRed, Color.Gold, colorInterpolant);
        }

        public float TelegraphWidthFunction(float completionRatio) => Width * 0.75f;

        public static Color TelegraphColorFunction(float completionRatio)
        {
            Color orange = Color.Lerp(Color.OrangeRed, WayfinderSymbol.Colors[2], 0.5f);
            return Color.Lerp(orange, WayfinderSymbol.Colors[0], completionRatio);
        }

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            if (Timer < TelegraphLength + 20)
            {
                TelegraphDrawer ??= new PrimitiveTrailCopy(TelegraphWidthFunction, TelegraphColorFunction, null, true, InfernumEffectsRegistry.SideStreakVertexShader);

                InfernumEffectsRegistry.SideStreakVertexShader.SetShaderTexture(InfernumTextureRegistry.CultistRayMap);
                float opacityScalar = Sin(EasingCurves.Sine.InOutFunction(Timer / (TelegraphLength + 20)) * PI);
                InfernumEffectsRegistry.SideStreakVertexShader.UseOpacity(0.5f * opacityScalar);

                Vector2 startT = Projectile.Center;
                Vector2 endT = startT - Vector2.UnitY * MaxLength * 1.2f;
                Vector2[] drawPositionsT = new Vector2[8];
                for (int i = 0; i < drawPositionsT.Length; i++)
                    drawPositionsT[i] = Vector2.Lerp(startT, endT, (float)i / drawPositionsT.Length);

                TelegraphDrawer.DrawPixelated(drawPositionsT, -Main.screenPosition, 20);

                Texture2D warningSymbol = InfernumTextureRegistry.VolcanoWarning.Value;
                Vector2 drawPosition = (startT - Vector2.UnitY * 4000 * 0.575f) - Main.screenPosition;
                Color drawColor = Color.Lerp(TelegraphColorFunction(0.5f), Color.Orange, 0.5f) * Projectile.Opacity;
                drawColor.A = 0;
                Vector2 origin = warningSymbol.Size() * 0.5f;
                float scale = BigVersion ? 3f : 0.8f;

                spriteBatch.Draw(warningSymbol, drawPosition, null, drawColor * opacityScalar, 0f, origin, scale, SpriteEffects.None, 0f);
            }
            if (Timer > TelegraphLength)
            {
                LavaDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.GuardiansLaserVertexShader);

                InfernumEffectsRegistry.GuardiansLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.LavaNoise);
                InfernumEffectsRegistry.GuardiansLaserVertexShader.SetShaderTexture2(InfernumTextureRegistry.CultistRayMap);
                InfernumEffectsRegistry.GuardiansLaserVertexShader.UseColor(Color.LightGoldenrodYellow);
                InfernumEffectsRegistry.GuardiansLaserVertexShader.Shader.Parameters["flipY"].SetValue(false);
                float lengthScalar = CurrentLength / MaxLength;
                InfernumEffectsRegistry.GuardiansLaserVertexShader.Shader.Parameters["stretchAmount"].SetValue((BigVersion ? 0.6f : 1.3f) + StretchOffset * lengthScalar);
                InfernumEffectsRegistry.GuardiansLaserVertexShader.Shader.Parameters["pillarVarient"].SetValue(true);
                InfernumEffectsRegistry.GuardiansLaserVertexShader.Shader.Parameters["scrollSpeed"].SetValue(BigVersion ? 1f : 1.8f);
                Vector2 start = Projectile.Center;
                Vector2 end = start - Vector2.UnitY * CurrentLength;
                Vector2[] drawPositions = new Vector2[8];
                for (int i = 0; i < drawPositions.Length; i++)
                    drawPositions[i] = Vector2.Lerp(start, end, (float)i / drawPositions.Length);

                LavaDrawer.DrawPixelated(drawPositions, -Main.screenPosition, 30);
            }
        }
    }
}
