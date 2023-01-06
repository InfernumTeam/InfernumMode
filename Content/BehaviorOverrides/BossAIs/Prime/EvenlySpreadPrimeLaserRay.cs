using CalamityMod;
using CalamityMod.Projectiles.BaseProjectiles;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Graphics.Shaders;
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
            Projectile.width = Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = (int)Lifetime;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = 1;
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
            return MathHelper.Clamp(Projectile.width * Projectile.scale, 0f, Projectile.width);
        }

        public Color ColorFunction(float completionRatio)
        {
            float colorInterpolant = (float)Math.Sin(Main.GlobalTimeWrappedHourly * -3.2f + completionRatio * 23f) * 0.5f + 0.5f;
            Color color = Color.Lerp(new(221, 1, 3), new(255, 130, 130), colorInterpolant * 0.67f);
            color.A = 5;
            return color * 1.32f;
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            BeamDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["CalamityMod:Bordernado"]);

            GameShaders.Misc["CalamityMod:Bordernado"].UseSaturation(MathHelper.Lerp(0.23f, 0.29f, Projectile.identity / 9f % 1f));
            GameShaders.Misc["CalamityMod:Bordernado"].SetShaderTexture(InfernumTextureRegistry.CultistRayMap);

            List<Vector2> points = new();
            for (int i = 0; i <= 8; i++)
                points.Add(Vector2.Lerp(Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, i / 8f));

            if (Time >= 2f)
            {
                for (float offset = 0f; offset < 6f; offset += 0.75f)
                {
                    BeamDrawer.DrawPixelated(points, -Main.screenPosition, 28);
                    BeamDrawer.DrawPixelated(points, (Main.GlobalTimeWrappedHourly * 1.8f).ToRotationVector2() * offset - Main.screenPosition, 28);
                    BeamDrawer.DrawPixelated(points, -(Main.GlobalTimeWrappedHourly * 1.8f).ToRotationVector2() * offset - Main.screenPosition, 28);
                }
            }
        }
    }
}
