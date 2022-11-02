using CalamityMod;
using CalamityMod.Particles;
using InfernumMode.Sounds;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Projectiles
{
    public class ScreenShakeProj : ModProjectile
    {
        public const int Lifetime = 60;

        public override void SetStaticDefaults() => DisplayName.SetDefault("Screen Shake");

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.penetrate = -1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.MaxUpdates = 3;
            Projectile.timeLeft = 105;
        }

        public override void AI()
        {
            if (Main.netMode == NetmodeID.Server || !CalamityConfig.Instance.Screenshake)
                return;

            if (!Filters.Scene["InfernumMode:ScreenShake"].IsActive())
                Filters.Scene.Activate("InfernumMode:ScreenShake", Projectile.Center).GetShader().UseColor(5f, 8f, 75f).UseTargetPosition(Projectile.Center);
            else
            {
                float progress = Utils.Remap(Projectile.timeLeft, 105f, 0f, 0f, 1f);
                Filters.Scene["InfernumMode:ScreenShake"].GetShader().UseProgress(progress).UseOpacity((1f - progress) * 400f);
            }
        }

        public static void CreateShockwave(Vector2 shockwavePosition, Color shockwaveColor, float maxParticleScale)
        {
            Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<ScreenShakeProj>());
            SoundEngine.PlaySound(InfernumSoundRegistry.SonicBoomSound, Vector2.Lerp(shockwavePosition, Main.LocalPlayer.Center, 0.84f));

            if (Main.netMode != NetmodeID.MultiplayerClient)
                Utilities.NewProjectileBetter(shockwavePosition, Vector2.Zero, ModContent.ProjectileType<ScreenShakeProj>(), 0, 0f);
            GeneralParticleHandler.SpawnParticle(new PulseRing(shockwavePosition, Vector2.Zero, shockwaveColor, 0f, maxParticleScale, 60));
        }

        public override void Kill(int timeLeft)
        {
            if (!CalamityConfig.Instance.Screenshake)
                return;
            
            if (Main.netMode != NetmodeID.Server && Filters.Scene["InfernumMode:ScreenShake"].IsActive())
                Filters.Scene["InfernumMode:ScreenShake"].Deactivate();
        }
    }
}
