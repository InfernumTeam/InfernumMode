using CalamityMod;
using CalamityMod.Projectiles.BaseProjectiles;
using InfernumMode.GlobalInstances;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumAureus
{
    public class OrangeLaserbeam : BaseLaserbeamProjectile
    {
        public PrimitiveTrail LaserDrawer
        {
            get;
            set;
        } = null;

        public int OwnerIndex => (int)Projectile.ai[1];
        public const int LaserLifetime = 132;
        public const float FullCircleRotationFactor = 0.84f;
        public override float Lifetime => LaserLifetime;
        public override Color LaserOverlayColor => Color.White;
        public override Color LightCastColor => Color.Orange;
        public override Texture2D LaserBeginTexture => TextureAssets.Projectile[Projectile.type].Value;
        public override Texture2D LaserMiddleTexture => ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/OrangeLaserbeamMid", AssetRequestMode.ImmediateLoad).Value;
        public override Texture2D LaserEndTexture => ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/OrangeLaserbeamEnd", AssetRequestMode.ImmediateLoad).Value;
        public override float MaxLaserLength => 3100f;
        public override float MaxScale => 1f;
        public override void SetStaticDefaults() => DisplayName.SetDefault("Astral Deathray");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 17;
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
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.ReadSingle();
            Projectile.localAI[1] = reader.ReadSingle();
        }
        public override void AttachToSomething()
        {
            if (!Main.npc.IndexInRange(GlobalNPCOverrides.AstrumAureus))
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = Main.npc[GlobalNPCOverrides.AstrumAureus].Center - Vector2.UnitY * 12f;
            Projectile.Opacity = 1f;
            RotationalSpeed = MathHelper.Pi / Lifetime * FullCircleRotationFactor;
        }

        public override bool? CanDamage() => Time > 35f ? null : false;
        public float LaserWidthFunction(float _) => Projectile.scale * Projectile.width;

        public Color LaserColorFunction(float completionRatio)
        {
            float colorInterpolant = (float)Math.Sin(Main.GlobalTimeWrappedHourly * -3.2f + completionRatio * 23f) * 0.5f + 0.5f;
            return Color.Lerp(new(255, 164, 94), new(237, 93, 83), colorInterpolant * 0.67f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // This should never happen, but just in case.
            if (Projectile.velocity == Vector2.Zero)
                return false;
            LaserDrawer ??= new(LaserWidthFunction, LaserColorFunction, null, GameShaders.Misc["CalamityMod:ArtemisLaser"]);
            Vector2 laserEnd = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * LaserLength;
            Vector2[] baseDrawPoints = new Vector2[8];
            for (int i = 0; i < baseDrawPoints.Length; i++)
                baseDrawPoints[i] = Vector2.Lerp(Projectile.Center, laserEnd, i / (float)(baseDrawPoints.Length - 1f));

            // Select textures to pass to the shader, along with the electricity color.
            GameShaders.Misc["CalamityMod:ArtemisLaser"].UseColor(187, 220, 237);
            GameShaders.Misc["CalamityMod:ArtemisLaser"].UseImage1("Images/Extra_189");
            GameShaders.Misc["CalamityMod:ArtemisLaser"].UseImage2("Images/Misc/Perlin");

            LaserDrawer.Draw(baseDrawPoints, -Main.screenPosition, 54);

            return false;
        }
    }
}
