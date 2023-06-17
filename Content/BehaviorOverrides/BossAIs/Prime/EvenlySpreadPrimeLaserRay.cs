using CalamityMod;
using CalamityMod.Projectiles.BaseProjectiles;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Prime
{
    public class EvenlySpreadPrimeLaserRay : BaseLaserbeamProjectile, IPixelPrimitiveDrawer
    {
        public PrimitiveTrailCopy BeamDrawer
        {
            get;
            set;
        } = null;

        public float InitialDirection = -100f;
        public int OwnerIndex => (int)Projectile.ai[1];
        public override float Lifetime => 260;
        public override Color LaserOverlayColor => Color.White;
        public override Color LightCastColor => Color.White;
        public override Texture2D LaserBeginTexture => ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/Lasers/PrimeBeamBegin", AssetRequestMode.ImmediateLoad).Value;
        public override Texture2D LaserMiddleTexture => ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/Lasers/PrimeBeamMid", AssetRequestMode.ImmediateLoad).Value;
        public override Texture2D LaserEndTexture => ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/Lasers/PrimeBeamEnd", AssetRequestMode.ImmediateLoad).Value;
        public override string Texture => "InfernumMode/Assets/ExtraTextures/Lasers/PrimeBeamBegin";
        public override float MaxLaserLength => 3100f;
        public override float MaxScale => 1f;
        public override void SetStaticDefaults() => DisplayName.SetDefault("Deathray");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = (int)Lifetime;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.localAI[0]);
            writer.Write(Projectile.localAI[1]);
            writer.Write(InitialDirection);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.ReadSingle();
            Projectile.localAI[1] = reader.ReadSingle();
            InitialDirection = reader.ReadSingle();
        }
        public override void AttachToSomething()
        {
            if (InitialDirection == -100f)
                InitialDirection = Projectile.velocity.ToRotation();

            if (!Main.npc.IndexInRange(OwnerIndex))
            {
                Projectile.Kill();
                return;
            }

            Projectile.velocity = (InitialDirection + Main.npc[OwnerIndex].Infernum().ExtraAI[5]).ToRotationVector2();
            Projectile.Center = Main.npc[OwnerIndex].Center - Vector2.UnitY * 16f + Projectile.velocity * 2f;
        }
        public float WidthFunction(float completionRatio)
        {
            return Clamp(Projectile.width * Projectile.scale * 1.7f, 0f, Projectile.width * 1.7f);
        }

        public Color ColorFunction(float completionRatio)
        {
            float colorInterpolant = Sin(Main.GlobalTimeWrappedHourly * -3.2f + completionRatio * 23f) * 0.5f + 0.5f;
            // new(221, 1, 3), new(255, 40, 30)
            Color color = Color.Lerp(new(221, 50, 50), new(255, 5, 1), colorInterpolant * 0.67f);
            return color;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D glowTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Color glowColor = Color.Brown;
            glowColor.A = 0;

            Main.EntitySpriteDraw(glowTexture, Projectile.Center - Main.screenPosition, null, glowColor, 0f, glowTexture.Size() * 0.5f, 3f * Projectile.scale, SpriteEffects.None, 0);

            InfernumEffectsRegistry.PulsatingLaserVertexShader.UseSaturation(1);
            InfernumEffectsRegistry.PulsatingLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakBigBackground);
            InfernumEffectsRegistry.PulsatingLaserVertexShader.Shader.Parameters["usePulsing"].SetValue(true);
            InfernumEffectsRegistry.PulsatingLaserVertexShader.Shader.Parameters["reverseDirection"].SetValue(false);
            InfernumEffectsRegistry.PulsatingLaserVertexShader.UseColor(ColorFunction(0.1f));

            List<Vector2> points = new();
            for (int i = 0; i <= 8; i++)
                points.Add(Vector2.Lerp(Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, i / 8f));

            BeamDrawer.Draw(points, -Main.screenPosition, 28);
            InfernumEffectsRegistry.PulsatingLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakBigInner);
            InfernumEffectsRegistry.PulsatingLaserVertexShader.Shader.Parameters["usePulsing"].SetValue(true);
            InfernumEffectsRegistry.PulsatingLaserVertexShader.UseColor(Color.Lerp(ColorFunction(0.5f), Color.White, 0.5f));
            InfernumEffectsRegistry.PulsatingLaserVertexShader.UseSaturation(1.5f);
            BeamDrawer.Draw(points, -Main.screenPosition, 28);
            return false;
        }

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            BeamDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.PulsatingLaserVertexShader);

            //InfernumEffectsRegistry.PulsatingLaserVertexShader.UseSaturation(1);
            //InfernumEffectsRegistry.PulsatingLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakBigBackground);
            //InfernumEffectsRegistry.PulsatingLaserVertexShader.Shader.Parameters["usePulsing"].SetValue(true);
            //InfernumEffectsRegistry.PulsatingLaserVertexShader.UseColor(ColorFunction(0.1f));

            //List<Vector2> points = new();
            //for (int i = 0; i <= 8; i++)
            //    points.Add(Vector2.Lerp(Projectile.Center + Projectile.velocity * 80f, Projectile.Center + Projectile.velocity * LaserLength, i / 8f));

            //BeamDrawer.DrawPixelated(points, -Main.screenPosition, 28);
            //InfernumEffectsRegistry.PulsatingLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakBigInner);
            //InfernumEffectsRegistry.PulsatingLaserVertexShader.Shader.Parameters["usePulsing"].SetValue(true);
            //InfernumEffectsRegistry.PulsatingLaserVertexShader.UseColor(Color.Lerp(ColorFunction(0.5f), Color.White, 0.2f));
            //InfernumEffectsRegistry.PulsatingLaserVertexShader.UseSaturation(3);
            //BeamDrawer.DrawPixelated(points, -Main.screenPosition, 28);
        }
    }
}
