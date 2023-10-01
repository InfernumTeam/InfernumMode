using CalamityMod.DataStructures;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.ScreenEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class AresEnergyDeathrayTelegraph : ModProjectile, IAdditiveDrawer
    {
        public float LifetimeCompletion => 1f - Projectile.timeLeft / (float)Lifetime;

        public static int Lifetime => 27;

        public override string Texture => InfernumTextureRegistry.InvisPath;

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Exo Energy Burst Telegraph");

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.Opacity = 0f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Rapidly fade in.
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.15f, 0f, 1f);
        }

        public override void OnKill(int timeLeft)
        {
            Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 12f;
            ScreenEffectSystem.SetBlurEffect(Projectile.Center, 0.3f, 16);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Utilities.NewProjectileBetter(Projectile.Center + Projectile.velocity * 80f, Projectile.velocity, ModContent.ProjectileType<AresEnergyDeathray>(), DraedonBehaviorOverride.PowerfulShotDamage, 0f);
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool PreDraw(ref Color lightColor) => false;

        public void AdditiveDraw(SpriteBatch spriteBatch)
        {
            float opacity = Utils.GetLerpValue(1f, 0.75f, LifetimeCompletion, true) * Projectile.Opacity;
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity * 4000f;
            spriteBatch.DrawBloomLine(start, end, Color.Lerp(Color.Red, Color.Wheat, LifetimeCompletion) * opacity, LifetimeCompletion * 15f + 20f);
        }
    }
}
