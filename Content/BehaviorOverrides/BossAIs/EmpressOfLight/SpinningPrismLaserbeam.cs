using CalamityMod;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class SpinningPrismLaserbeam : ModProjectile, IPixelPrimitiveDrawer
    {
        public int LaserCount;

        public float LaserbeamIDRatio;

        public float AngularOffset;

        public float VerticalSpinDirection;

        public PrimitiveTrailCopy RayDrawer;

        public ref float LaserLength => ref Projectile.ai[0];

        public ref float Time => ref Projectile.localAI[0];

        public const int Lifetime = 300;

        public const float MaxLaserLength = 4800f;

        public const float SpinRate = 5f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Prismatic Ray");
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.hide = true;
            Projectile.netImportant = true;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(LaserCount);
            writer.Write(LaserbeamIDRatio);
            writer.Write(AngularOffset);
            writer.Write(VerticalSpinDirection);
            writer.Write(Time);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            LaserCount = reader.ReadInt32();
            LaserbeamIDRatio = reader.ReadSingle();
            AngularOffset = reader.ReadSingle();
            VerticalSpinDirection = reader.ReadSingle();
            Time = reader.ReadSingle();
        }

        public override void AI()
        {
            // Grow bigger up to a point.
            float maxScale = Lerp(0.051f, 1.5f, Utils.GetLerpValue(0f, 30f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 16f, Time, true));
            Projectile.scale = Clamp(Projectile.scale + 0.02f, 0.05f, maxScale);

            // Spin the laserbeam.
            float deviationAngle = (Time * TwoPi / 40f + LaserbeamIDRatio * SpinRate) / (LaserCount * SpinRate) * TwoPi;
            float sinusoidYOffset = Cos(deviationAngle) * AngularOffset;
            Projectile.velocity = Vector2.UnitY.RotatedBy(sinusoidYOffset) * VerticalSpinDirection;

            // Update the laser length.
            LaserLength = MaxLaserLength;

            // Make the beam cast light along its length. The brightness of the light is reliant on the scale of the beam.
            DelegateMethods.v3_1 = Color.White.ToVector3() * Projectile.scale * 0.6f;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, Projectile.width * Projectile.scale, DelegateMethods.CastLight);
            Time++;
        }

        internal float PrimitiveWidthFunction(float completionRatio) => Projectile.scale * 12f;

        internal Color PrimitiveColorFunction(float completionRatio)
        {
            float opacity = Projectile.Opacity * Utils.GetLerpValue(0.97f, 0.9f, completionRatio, true) *
                Utils.GetLerpValue(0f, Clamp(15f / LaserLength, 0f, 0.5f), completionRatio, true) *
                Pow(Utils.GetLerpValue(60f, 270f, LaserLength, true), 3f);
            Color c = Main.hslToRgb((completionRatio * 8f + Main.GlobalTimeWrappedHourly * 0.5f + Projectile.identity * 0.3156f) % 1f, 1f, 0.7f) * opacity;
            c.A = 0;

            return c;
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            RayDrawer ??= new(PrimitiveWidthFunction, PrimitiveColorFunction, specialShader: InfernumEffectsRegistry.PrismaticRayVertexShader);

            InfernumEffectsRegistry.PrismaticRayVertexShader.UseImage1("Images/Misc/Perlin");
            Main.instance.GraphicsDevice.Textures[2] = InfernumTextureRegistry.StreakSolid.Value;

            Vector2[] basePoints = new Vector2[8];
            for (int i = 0; i < basePoints.Length; i++)
                basePoints[i] = Projectile.Center + Projectile.velocity * i / (basePoints.Length - 1f) * LaserLength;

            Vector2 overallOffset = -Main.screenPosition;
            RayDrawer.DrawPixelated(basePoints, overallOffset, 16);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, Projectile.scale * 12f, ref _);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overWiresUI.Add(index);
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
