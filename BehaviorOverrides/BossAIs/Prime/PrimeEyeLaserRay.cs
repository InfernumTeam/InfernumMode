using CalamityMod;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Prime
{
    public class PrimeEyeLaserRay : BaseLaserbeamProjectile
    {
        public PrimitiveTrail LaserDrawer
        {
            get;
            set;
        } = null;

        public ref float AngularVelocity => ref Projectile.ai[0];
        public int OwnerIndex => (int)Projectile.ai[1];
        public Vector2 OwnerEyePosition => Main.npc[OwnerIndex].Center + new Vector2((AngularVelocity > 0f).ToDirectionInt() * 16f, -7f).RotatedBy(Main.npc[OwnerIndex].rotation) + Projectile.velocity * 2f;
        public override float Lifetime => 120;
        public override Color LaserOverlayColor => Color.White;
        public override Color LightCastColor => Color.White;
        public override Texture2D LaserBeginTexture => ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/PrimeBeamBegin", AssetRequestMode.ImmediateLoad).Value;
        public override Texture2D LaserMiddleTexture => ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/PrimeBeamMid", AssetRequestMode.ImmediateLoad).Value;
        public override Texture2D LaserEndTexture => ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/PrimeBeamEnd", AssetRequestMode.ImmediateLoad).Value;
        public override float MaxLaserLength => 2400f;
        public override float MaxScale => 1f;
        public override string Texture => "InfernumMode/ExtraTextures/PrimeBeamBegin";
        public override void SetStaticDefaults() => DisplayName.SetDefault("Deathray");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 14;
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
            if (!Main.npc.IndexInRange(OwnerIndex))
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = OwnerEyePosition;
            Projectile.Center -= Projectile.velocity * 30;
            Projectile.velocity = Projectile.velocity.RotatedBy(AngularVelocity).SafeNormalize(Vector2.UnitY);
        }

        public float LaserWidthFunction(float _) => Projectile.scale * Projectile.width;

        public static Color LaserColorFunction(float completionRatio)
        {
            float colorInterpolant = (float)Math.Sin(Main.GlobalTimeWrappedHourly * -3.2f + completionRatio * 23f) * 0.5f + 0.5f;
            return Color.Lerp(new(221, 1, 3), new(255, 130, 130), colorInterpolant * 0.67f);
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
            GameShaders.Misc["CalamityMod:ArtemisLaser"].UseColor(Color.DeepPink);
            GameShaders.Misc["CalamityMod:ArtemisLaser"].SetShaderTexture(ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/Streak2"));
            GameShaders.Misc["CalamityMod:ArtemisLaser"].UseImage2("Images/Misc/Perlin");

            LaserDrawer.Draw(baseDrawPoints, -Main.screenPosition, 54);

            return false;
        }
    }
}
