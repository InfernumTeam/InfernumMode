using CalamityMod;
using InfernumMode.Assets.Effects;
using System.IO;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles
{
    public class ScreenShakeProj : ModProjectile
    {
        public int RippleCount;

        public int RippleSize;

        public float RippleSpeed;

        public const int Lifetime = 105;

        public override void SetStaticDefaults() => DisplayName.SetDefault("Screen Shake");

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.penetrate = -1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.MaxUpdates = 1;
            Projectile.timeLeft = Lifetime;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(RippleCount);
            writer.Write(RippleSize);
            writer.Write(RippleSpeed);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            RippleCount = reader.ReadInt32();
            RippleSize = reader.ReadInt32();
            RippleSpeed = reader.ReadSingle();
        }

        public override void AI()
        {
            // Don't do anything if running server-side or if screen shake effects are disabled in the config.
            if (Main.netMode == NetmodeID.Server || !CalamityConfig.Instance.Screenshake)
                return;

            if (!InfernumEffectsRegistry.ScreenShakeScreenShader.IsActive())
                Filters.Scene.Activate("InfernumMode:ScreenShake", Projectile.Center).GetShader().UseColor(RippleCount, RippleSize, RippleSpeed).UseTargetPosition(Projectile.Center);
            else
            {
                float progress = Utils.Remap(Projectile.timeLeft, Lifetime, 0f, 0f, 1f);
                InfernumEffectsRegistry.ScreenShakeScreenShader.GetShader().UseProgress(progress).UseOpacity((1f - progress) * 30f);
            }
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.Server && InfernumEffectsRegistry.ScreenShakeScreenShader.IsActive())
                InfernumEffectsRegistry.ScreenShakeScreenShader.Deactivate();
        }
    }
}
