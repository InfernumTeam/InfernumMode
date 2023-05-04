using CalamityMod;
using CalamityMod.Particles;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Yharon
{
    public class VortexOfFlame : ModProjectile, ISpecializedDrawRegion
    {
        public const int Lifetime = 420;

        public ref float Time => ref Projectile.ai[0];

        public override string Texture => InfernumTextureRegistry.InvisPath;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Vortex of Flame");
        }

        public override void SetDefaults()
        {
            Projectile.Size = Vector2.One * 450f;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.timeLeft = Lifetime;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(Projectile.timeLeft);

        public override void ReceiveExtraAI(BinaryReader reader) => Projectile.timeLeft = reader.ReadInt32();

        public override void AI()
        {
            Projectile.rotation += MathHelper.ToRadians(14f);
            Projectile.Opacity = Utils.GetLerpValue(0f, 40f, Time, true) * Utils.GetLerpValue(0f, 40f, Projectile.timeLeft, true);
            Time++;

            // Accelerate.
            Projectile.velocity *= 1.02f;

            // Release a lot of fire outward.
            CloudParticle fire = new(Projectile.Center, Main.rand.NextVector2Circular(16f, 16f) * Projectile.Opacity, Color.Lerp(Color.Yellow, Color.Red, Main.rand.NextFloat(0.55f)), Color.DarkGray, 36, 3f);
            GeneralParticleHandler.SpawnParticle(fire);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(Projectile.Center, Projectile.width * Projectile.Opacity, targetHitbox);
        }

        // Explode into fireballs on death.
        public override void Kill(int timeLeft)
        {
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];

            if (Projectile.WithinRange(target.Center, 360f))
                return;

            SoundEngine.PlaySound(InfernumSoundRegistry.YharonVortexExplosionSound, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 4; i++)
            {
                Vector2 fireballShootVelocity = Projectile.SafeDirectionTo(target.Center) * 12.5f + Main.rand.NextVector2Circular(6f, 6f);
                Utilities.NewProjectileBetter(Projectile.Center + Main.rand.NextVector2Circular(30f, 30f), fireballShootVelocity, ModContent.ProjectileType<HomingFireball>(), YharonBehaviorOverride.RegularFireballDamage, 0f);
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public override bool? CanDamage() => Projectile.Opacity >= 0.64f ? null : false;

        public void SpecialDraw(SpriteBatch spriteBatch)
        {
            // Draw the vortex.
            Texture2D fireNoise = InfernumTextureRegistry.WavyNoise.Value;
            Texture2D miscNoise = InfernumTextureRegistry.FireNoise.Value;

            Effect portal = InfernumEffectsRegistry.ProfanedPortalShader.Shader;
            portal.Parameters["sampleTexture"].SetValue(fireNoise);
            portal.Parameters["sampleTexture2"].SetValue(miscNoise);
            portal.Parameters["mainColor"].SetValue(Color.Orange.ToVector3());
            portal.Parameters["secondaryColor"].SetValue(Color.HotPink.ToVector3());
            portal.Parameters["resolution"].SetValue(new Vector2(240f));
            portal.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            portal.Parameters["opacity"].SetValue(Projectile.Opacity);
            portal.Parameters["innerGlowAmount"].SetValue(0.8f);
            portal.Parameters["innerGlowDistance"].SetValue(0.15f);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, portal, Main.GameViewMatrix.TransformationMatrix);
            spriteBatch.Draw(fireNoise, Projectile.Center - Main.screenPosition, null, Color.White, 0f, fireNoise.Size() * 0.5f, Projectile.Opacity * Projectile.width / fireNoise.Width * 4.6f, SpriteEffects.None, 0f);
        }

        public void PrepareSpriteBatch(SpriteBatch spriteBatch)
        {
            spriteBatch.EnterShaderRegion();
        }
    }
}