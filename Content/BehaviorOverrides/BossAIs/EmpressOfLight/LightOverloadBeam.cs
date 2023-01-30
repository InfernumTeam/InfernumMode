using CalamityMod;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class LightOverloadBeam : ModProjectile, IPixelPrimitiveDrawer
    {
        public PrimitiveTrailCopy RayDrawer = null;

        public float Hue
        {
            get;
            set;
        }

        public float ConvergingState
        {
            get;
            set;
        }

        public NPC Owner => Main.npc[(int)Projectile.ai[0]];

        public ref float Time => ref Projectile.ai[1];

        public static int TelegraphTime => 36;

        public const int FadeInTime = 12;

        public const int FadeOutTime = 12;

        public static int Lifetime => TelegraphTime + FadeInTime + FadeOutTime + 540;

        public const float MaxLaserLength = 5250f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Prismatic Overload Ray");
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 72;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            Projectile.netImportant = true;
            Projectile.timeLeft = Lifetime;
            Projectile.Opacity = 0f;
            Projectile.Calamity().DealsDefenseDamage = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Hue);
            writer.Write(ConvergingState);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Hue = reader.ReadSingle();
            ConvergingState = reader.ReadInt32();
        }

        public override void AI()
        {
            // Die if the owner is no longer present.
            if (!Owner.active)
            {
                Projectile.Kill();
                return;
            }

            if (Projectile.localAI[0] == 0f)
            {
                Projectile.scale = 0.05f;
                Projectile.localAI[0] = 1f;
            }

            // Grow bigger up to a point.
            Projectile.scale = Utils.GetLerpValue(TelegraphTime, TelegraphTime + FadeInTime, Time, true) * Utils.GetLerpValue(0f, -FadeOutTime, Time - Lifetime, true);
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.03f, 0f, 1f);

            // Stick near the owner.
            Projectile.Center = Owner.Center + Projectile.velocity * 350f;

            // Converge on the opposite side of the target.
            if (ConvergingState == 1)
            {
                Player target = Main.player[Owner.target];
                Projectile.velocity = Projectile.velocity.RotateTowards(Owner.AngleTo(target.Center) + MathHelper.Pi, 0.1f);
                Projectile.timeLeft = 480;
                Time = Lifetime - Projectile.timeLeft;
            }

            // Sweep.
            if (ConvergingState == 2)
                Projectile.velocity = Projectile.velocity.RotatedBy(MathHelper.Pi * 0.0085f);

            if (ConvergingState >= 1)
                Projectile.Center += Projectile.velocity.RotatedBy(MathHelper.PiOver2) * MathHelper.Lerp(-156f, 156f, Hue);

            // Make the beam cast light along its length. The brightness of the light is reliant on the scale of the beam.
            DelegateMethods.v3_1 = Color.White.ToVector3() * Projectile.scale * 0.6f;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * MaxLaserLength, Projectile.width * Projectile.scale, DelegateMethods.CastLight);

            Time++;
        }

        internal float PrimitiveWidthFunction(float completionRatio)
        {
            return Projectile.scale * Projectile.width;
        }

        internal Color PrimitiveColorFunction(float completionRatio)
        {
            float hueOffset = (float)Math.Cos(completionRatio * 12f + Main.GlobalTimeWrappedHourly * 0.8f) * 0.5f + 0.5f;
            float opacity = Projectile.Opacity * Utils.GetLerpValue(1f, 0.9f, completionRatio, true) * Utils.GetLerpValue(0f, 0.16f, completionRatio, true);
            Color c = Main.hslToRgb((hueOffset * 0.08f + Hue) % 1f, 1f, 0.7f) * opacity;
            return c;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Time >= TelegraphTime)
                return false;

            Color telegraphColor = Main.hslToRgb((Main.GlobalTimeWrappedHourly * 0.1f + Projectile.identity * 0.3156f) % 1f, 1f, 0.7f);
            Vector2 end = Projectile.Center + Projectile.velocity * MaxLaserLength;
            Main.spriteBatch.DrawLineBetter(Projectile.Center, end, telegraphColor, Projectile.Opacity * Time / TelegraphTime * 4f);

            return false;
        }

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            if (Time < TelegraphTime)
                return;

            RayDrawer ??= new(PrimitiveWidthFunction, PrimitiveColorFunction, specialShader: InfernumEffectsRegistry.ArtemisLaserVertexShader);

            Vector2 overallOffset = -Main.screenPosition;
            Vector2[] basePoints = new Vector2[6];
            for (int i = 0; i < basePoints.Length; i++)
                basePoints[i] = Projectile.Center + Projectile.velocity * i / (basePoints.Length - 1f) * MaxLaserLength;

            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseSaturation(0.3f);
            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseColor(Color.HotPink * Projectile.Opacity);
            InfernumEffectsRegistry.ArtemisLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakThickGlow);
            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseImage2("Images/Misc/Perlin");
            RayDrawer.DrawPixelated(basePoints, overallOffset, 26);
        }

        public override bool? CanDamage() => Projectile.Opacity >= 0.9f && ConvergingState != 1;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + Projectile.velocity * (MaxLaserLength - 50f), Projectile.width, ref _);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overWiresUI.Add(index);
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
