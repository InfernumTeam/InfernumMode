using CalamityMod;
using CalamityMod.NPCs;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasShadow
{
    public class LargeDarkFireOrb : ModProjectile, ISpecializedDrawRegion
    {
        public ref float Time => ref Projectile.ai[1];

        public static float MaxFireOrbRadius => 320f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Dark Fire Orb");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 20;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 7200;
            Projectile.Opacity = 0f;
            Projectile.netImportant = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.timeLeft);
            writer.Write(Projectile.tileCollide);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.timeLeft = reader.ReadInt32();
            Projectile.tileCollide = reader.ReadBoolean();
        }

        public override void AI()
        {
            // Disappear if Calamitas' shadow is not present.
            if (CalamityGlobalNPC.calamitas == -1)
                Projectile.Kill();

            Projectile.scale = Utils.Remap(Projectile.timeLeft, 25f, 1f, 1f, 10f);
            Projectile.Opacity = Utils.GetLerpValue(1f, 18f, Projectile.timeLeft, true);

            if (Math.Abs(Projectile.velocity.Y) >= 1f)
            {
                Projectile.velocity.Y *= 1.036f;
                Projectile.tileCollide = true;
            }

            Lighting.AddLight(Projectile.Center, Color.Yellow.ToVector3() * 0.76f);

            Time++;
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void SpecialDraw(SpriteBatch spriteBatch)
        {
            float circleFadeinInterpolant = Utils.GetLerpValue(0f, 36f, Time, true);
            float colorPulse = (Cos(Main.GlobalTimeWrappedHourly * 7.2f + Projectile.identity) * 0.5f + 0.5f) * 0.6f;
            colorPulse += (float)(Math.Cos(Main.GlobalTimeWrappedHourly * 6.1f + Projectile.identity * 1.3f) * 0.5f + 0.5f) * 0.4f;

            Color explosionTelegraphColor = Color.Lerp(Color.Red, Color.Purple, colorPulse * 0.3f + 0.4f) * circleFadeinInterpolant;

            Texture2D invisible = InfernumTextureRegistry.Invisible.Value;
            Texture2D noise = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/VoronoiShapes2").Value;
            Effect fireballShader = InfernumEffectsRegistry.FireballShader.GetShader().Shader;

            Vector2 scale = Vector2.One * MaxFireOrbRadius / invisible.Size() * circleFadeinInterpolant * Projectile.Opacity * Projectile.scale * 2f;
            fireballShader.Parameters["sampleTexture2"].SetValue(noise);
            fireballShader.Parameters["mainColor"].SetValue(explosionTelegraphColor.ToVector3());
            fireballShader.Parameters["resolution"].SetValue(Vector2.One * 250f);
            fireballShader.Parameters["speed"].SetValue(0.76f);
            fireballShader.Parameters["zoom"].SetValue(0.0004f);
            fireballShader.Parameters["dist"].SetValue(60f);
            fireballShader.Parameters["opacity"].SetValue(circleFadeinInterpolant * Projectile.Opacity);
            fireballShader.CurrentTechnique.Passes[0].Apply();

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            float[] scaleFactors =
            [
                1f, 0.8f, 0.7f, 0.57f, 0.44f, 0.32f, 0.22f
            ];

            for (int i = 0; i < scaleFactors.Length; i++)
            {
                fireballShader.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * (i * 0.04f + 0.32f));
                fireballShader.Parameters["mainColor"].SetValue(Color.Lerp(explosionTelegraphColor, Color.White, i / (float)(scaleFactors.Length - 1f)).ToVector3());
                fireballShader.CurrentTechnique.Passes[0].Apply();
                Main.spriteBatch.Draw(invisible, drawPosition, null, Color.White, Projectile.rotation, invisible.Size() * 0.5f, scale * scaleFactors[i], 0, 0f);
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.timeLeft >= 25)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceLavaEruptionSound with { Pitch = -0.4f, Volume = 0.7f }, Projectile.Center);
                Projectile.timeLeft = 25;
                Projectile.netUpdate = true;
            }
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (CalamityGlobalNPC.calamitas == -1)
                return;

            Main.npc[CalamityGlobalNPC.calamitas].ai[1] = 0f;
            Main.npc[CalamityGlobalNPC.calamitas].Infernum().ExtraAI[2] = 1f;
            Main.npc[CalamityGlobalNPC.calamitas].netUpdate = true;
        }

        public void PrepareSpriteBatch(SpriteBatch spriteBatch)
        {
            spriteBatch.EnterShaderRegion(BlendState.Additive);
        }
    }
}
