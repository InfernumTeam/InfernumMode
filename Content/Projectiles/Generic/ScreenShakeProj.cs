using System.IO;
using CalamityMod;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Generic
{
    public class ScreenShakeProj : ModProjectile
    {
        public int RippleCount;

        public int RippleSize;

        public float RippleSpeed;

        public bool UseSecondaryVariant
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value.ToInt();
        }

        public Filter ScreenShader => UseSecondaryVariant ? InfernumEffectsRegistry.ScreenShakeScreenShader2 : InfernumEffectsRegistry.ScreenShakeScreenShader;

        public const int Lifetime = 105;

        public override string Texture => InfernumTextureRegistry.InvisPath;

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Screen Shake");

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
            if (Main.netMode == NetmodeID.Server || !(CalamityClientConfig.Instance.ScreenshakePower > 0f))
                return;

            if (!ScreenShader.IsActive())
            {
                string screenShaderKey = UseSecondaryVariant ? "InfernumMode:ScreenShake2" : "InfernumMode:ScreenShake";
                Filters.Scene.Activate(screenShaderKey, Projectile.Center).GetShader().UseColor(RippleCount, RippleSize, RippleSpeed).UseTargetPosition(Projectile.Center);
            }
            else
            {
                float progress = Utils.Remap(Projectile.timeLeft, Lifetime, 0f, 0f, 1f);
                ScreenShader.GetShader().UseProgress(progress).UseOpacity((1f - progress) * 30f);
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.Server && ScreenShader.IsActive())
                ScreenShader.Deactivate();
        }
    }
}
