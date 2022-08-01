using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class BrimstoneFlamePillar : ModProjectile
    {
        public int OwnerIndex;

        public PrimitiveTrailCopy FireDrawer;

        public ref float Time => ref Projectile.ai[0];

        public ref float InitialRotationalOffset => ref Projectile.localAI[0];

        public const int Lifetime = 105;

        public float Height => MathHelper.Lerp(4f, Projectile.height, Projectile.scale * Projectile.Opacity);

        public float Width => MathHelper.Lerp(3f, Projectile.width, Projectile.scale * Projectile.Opacity);

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Brimstone Flame Pillar");

        public override void SetDefaults()
        {
            Projectile.width = 90;
            Projectile.height = 2400;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.alpha = 255;
            Projectile.Calamity().canBreakPlayerDefense = true;
            Projectile.MaxUpdates = 2;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            // Fade in.
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.04f, 0f, 1f);

            Projectile.scale = (float)Math.Sin(MathHelper.Pi * Time / Lifetime) * 2f;
            if (Projectile.scale > 1f)
                Projectile.scale = 1f;

            // Create bright light.
            Lighting.AddLight(Projectile.Center, Color.Purple.ToVector3() * 1.4f);

            if (Time == 1f)
            {
                SoundEngine.PlaySound(SoundID.Item74, Projectile.Bottom);
                Projectile.position.Y += Projectile.height * 0.5f + 40f;
                Projectile.position.X -= Projectile.width * 0.5f;
                Projectile.netUpdate = true;
            }

            Time++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            Vector2 start = Projectile.Bottom - Vector2.UnitY.RotatedBy(Projectile.rotation) * Height * 0.5f;
            Vector2 end = start - Vector2.UnitY.RotatedBy(Projectile.rotation) * Height;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, Width * 0.5f, ref _);
        }

        public override bool CanHitPlayer(Player target) => Projectile.Opacity >= 0.9f;

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = Projectile;

        public float WidthFunction(float completionRatio)
        {
            float tipFadeoffInterpolant = MathHelper.SmoothStep(0f, 1f, Utils.GetLerpValue(1f, 0.75f, completionRatio, true));
            float baseFadeoffInterpolant = MathHelper.SmoothStep(2.4f, 1f, 1f - CalamityUtils.Convert01To010(Utils.GetLerpValue(0f, 0.19f, completionRatio, true)));
            float widthAdditionFactor = (float)Math.Sin(Main.GlobalTimeWrappedHourly * -13f + Projectile.identity + completionRatio * MathHelper.Pi * 4f) * 0.2f;
            return Width * tipFadeoffInterpolant * baseFadeoffInterpolant * (1f + widthAdditionFactor);
        }

        public Color ColorFunction(float completionRatio)
        {
            Color darkFlameColor = new(249, 59, 91);
            Color lightFlameColor = new(174, 45, 237);
            float colorShiftInterpolant = (float)Math.Sin(-Main.GlobalTimeWrappedHourly * 2.7f + completionRatio * MathHelper.TwoPi) * 0.5f + 0.5f;
            Color color = Color.Lerp(darkFlameColor, lightFlameColor, (float)Math.Pow(colorShiftInterpolant, 1.64f));
            return color * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (FireDrawer is null)
                FireDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["Infernum:DarkFlamePillar"]);

            // Create a telegraph line upward that fades away away the pillar fades in.
            Vector2 start = Projectile.Top;
            Vector2 end = start - Vector2.UnitY.RotatedBy(Projectile.rotation) * Height;
            if (Projectile.Opacity < 1f)
                Main.spriteBatch.DrawLineBetter(start + Projectile.Size * 0.5f, end + Projectile.Size * 0.5f, Color.Cyan * (1f - Projectile.Opacity), Projectile.Opacity * 6f);

            var oldBlendState = Main.instance.GraphicsDevice.BlendState;
            Main.instance.GraphicsDevice.BlendState = BlendState.Additive;
            GameShaders.Misc["Infernum:DarkFlamePillar"].UseSaturation(1.4f);
            GameShaders.Misc["Infernum:DarkFlamePillar"].SetShaderTexture(ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/PrismaticLaserbeamStreak2"));
            Main.instance.GraphicsDevice.Textures[2] = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/PrismaticLaserbeamStreak2").Value;

            List<Vector2> points = new();
            for (int i = 0; i <= 8; i++)
                points.Add(Vector2.Lerp(start, end, i / 8f));

            if (Time >= 2f)
                FireDrawer.Draw(points, Projectile.Size * new Vector2(0f, 0.5f) - Main.screenPosition, 166);
            Main.instance.GraphicsDevice.BlendState = oldBlendState;
            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<VulnerabilityHex>(), 300);

        public override bool ShouldUpdatePosition() => false;
    }
}
