using CalamityMod;
using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AquaticScourge
{
    public class SulphurousRockRubble : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public static int TelegraphTime => 32;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sulphuric Rubble");
            Main.projFrames[Type] = 3;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 3;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 240;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Accelerate.
            if (Projectile.velocity.Length() < 20f)
                Projectile.velocity *= 1.023f;
            Projectile.rotation += Projectile.velocity.X * 0.014f;

            // Interact with tiles after enough time has passed.
            Projectile.tileCollide = Time >= 90f;
            Time++;

            // Decide the frame.
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.frame = Main.rand.Next(Main.projFrames[Type]);
                Projectile.localAI[0] = 1f;
            }
        }

        public override void Kill(int timeLeft)
        {
            // Emit rubble.
            if (Main.netMode == NetmodeID.Server)
                return;

            SoundEngine.PlaySound(SoundID.Item51, Projectile.Center);
            Gore.NewGore(Projectile.GetSource_Death(), Projectile.position, Projectile.velocity, Mod.Find<ModGore>("SulphurousRubble1").Type, Projectile.scale);
            Gore.NewGore(Projectile.GetSource_Death(), Projectile.position, Projectile.velocity, Mod.Find<ModGore>("SulphurousRubble2").Type, Projectile.scale);
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            // Cast a telegraph line before the acceleration gets super strong.
            float opacity = CalamityUtils.Convert01To010(Time / TelegraphTime);
            if (opacity > 0f)
            {
                Texture2D invisible = InfernumTextureRegistry.Invisible.Value;
                Vector2 drawPosition = Projectile.Center - Main.screenPosition;

                Effect laserScopeEffect = Filters.Scene["CalamityMod:PixelatedSightLine"].GetShader().Shader;
                laserScopeEffect.Parameters["sampleTexture2"].SetValue(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/CertifiedCrustyNoise").Value);
                laserScopeEffect.Parameters["noiseOffset"].SetValue(Main.GameUpdateCount * -0.003f);
                laserScopeEffect.Parameters["mainOpacity"].SetValue((float)Math.Sqrt(opacity));
                laserScopeEffect.Parameters["Resolution"].SetValue(new Vector2(425f));
                laserScopeEffect.Parameters["laserAngle"].SetValue(-Projectile.velocity.ToRotation());
                laserScopeEffect.Parameters["laserWidth"].SetValue(0.003f + (float)Math.Pow(opacity, 4D) * ((float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.001f + 0.001f));
                laserScopeEffect.Parameters["laserLightStrenght"].SetValue(5f);
                laserScopeEffect.Parameters["color"].SetValue(Color.Lerp(Color.Lime, Color.Olive, 0.7f).ToVector3());
                laserScopeEffect.Parameters["darkerColor"].SetValue(Color.Lerp(Color.Yellow, Color.SaddleBrown, 0.8f).ToVector3());
                laserScopeEffect.Parameters["bloomSize"].SetValue(0.06f + (1f - opacity) * 0.1f);
                laserScopeEffect.Parameters["bloomMaxOpacity"].SetValue(0.4f);
                laserScopeEffect.Parameters["bloomFadeStrenght"].SetValue(3f);

                Main.spriteBatch.EnterShaderRegion(BlendState.Additive);

                laserScopeEffect.CurrentTechnique.Passes[0].Apply();
                Main.spriteBatch.Draw(invisible, drawPosition, null, Color.White, 0f, invisible.Size() * 0.5f, opacity * 2500f, SpriteEffects.None, 0f);
                Main.spriteBatch.ExitShaderRegion();
            }
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, 0);
            Utilities.DrawProjectileWithBackglowTemp(Projectile, Color.Lime with { A = 0 } * opacity, lightColor, opacity * 6f);

            return false;
        }
    }
}
