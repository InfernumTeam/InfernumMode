using InfernumMode.Common.BaseEntities;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class ShimmeringLightWave : BaseWaveExplosionProjectile
    {
        public override int Lifetime => 80;

        public override float MaxRadius => 1000f;

        public override float RadiusExpandRateInterpolant => 0.15f;

        public override string Texture => "InfernumMode/Assets/ExtraTextures/GreyscaleObjects/Gleam";

        public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer)
        {
            float baseShakePower = MathHelper.Lerp(1f, 5f, (float)Math.Sin(MathHelper.Pi * lifetimeCompletionRatio));
            return baseShakePower * Utils.GetLerpValue(2200f, 1050f, distanceFromPlayer, true);
        }

        public override Color DetermineExplosionColor(float lifetimeCompletionRatio)
        {
            Color rainbow = Main.hslToRgb((lifetimeCompletionRatio * 2f + Projectile.identity * 2.3f) % 1f, 1f, 0.5f);
            return Color.Lerp(rainbow, new Color(1f, 1f, 1f, 0f), MathHelper.Clamp(lifetimeCompletionRatio * 1.35f, 0f, 1f));
        }

        public override void PostAI()
        {
            // Create sparkles.
            if (Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool(2))
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 sparkleSpawnPosition = Projectile.Center + Main.rand.NextVector2Circular(Radius, Radius) * 0.85f;
                    Utilities.NewProjectileBetter(sparkleSpawnPosition, Vector2.Zero, ModContent.ProjectileType<EmpressSparkle>(), 0, 0f);
                }
            }
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write((int)Projectile.localAI[1]);

        public override void ReceiveExtraAI(BinaryReader reader) => Projectile.localAI[1] = reader.ReadInt32();

        public override void PostDraw(Color lightColor)
        {
            for (int i = 0; i < 2; i++)
                PreDraw(ref lightColor);
        }
    }
}
