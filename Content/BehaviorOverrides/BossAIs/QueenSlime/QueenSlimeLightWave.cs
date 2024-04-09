using System.IO;
using InfernumMode.Common.BaseEntities;
using InfernumMode.Content.BehaviorOverrides.BossAIs.EmpressOfLight;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.QueenSlime
{
    public class QueenSlimeLightWave : BaseWaveExplosionProjectile
    {
        public override int Lifetime => 54;

        public override float MaxRadius => 3000f;

        public override float RadiusExpandRateInterpolant => 0.15f;

        public override string Texture => "InfernumMode/Assets/ExtraTextures/GreyscaleObjects/Gleam";

        public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer)
        {
            float baseShakePower = Lerp(1f, 5f, Sin(Pi * lifetimeCompletionRatio));
            return baseShakePower * Utils.GetLerpValue(2200f, 1050f, distanceFromPlayer, true);
        }

        public override Color DetermineExplosionColor(float lifetimeCompletionRatio)
        {
            Color color = Color.Lerp(Color.HotPink, Color.Cyan, lifetimeCompletionRatio) * Utils.GetLerpValue(1f, 0.75f, lifetimeCompletionRatio, true);
            return Color.Lerp(color, new Color(1f, 1f, 1f, 0f), Clamp(lifetimeCompletionRatio * 1.35f, 0f, 1f)) * 0.7f;
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
