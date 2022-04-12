using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class AcceleratingDarkMagicBurst : ModProjectile
    {
        public PrimitiveTrailCopy TrailDrawer = null;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Magic Burst");
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 15;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 26;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 80;
            Projectile.Calamity().canBreakPlayerDefense = true;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.075f, 0f, 1f);

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];
            Projectile.velocity *= 1.025f;
        }

        public float FlameTrailWidthFunction(float completionRatio)
        {
            return MathHelper.SmoothStep(20f, 6f, completionRatio) * Projectile.Opacity;
        }

        public Color FlameTrailColorFunction(float completionRatio)
        {
            float trailOpacity = Utils.GetLerpValue(0.8f, 0.27f, completionRatio, true) * Utils.GetLerpValue(0f, 0.067f, completionRatio, true) * 0.9f;
            Color startingColor = Color.Lerp(Color.White, Color.IndianRed, 0.25f);
            Color middleColor = Color.Lerp(Color.Maroon, Color.Red, 0.4f);
            Color endColor = Color.Lerp(Color.Purple, Color.Black, 0.35f);
            Color color = CalamityUtils.MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * trailOpacity;
            color.A = 184;
            return color * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (TrailDrawer is null)
                TrailDrawer = new PrimitiveTrailCopy(FlameTrailWidthFunction, FlameTrailColorFunction, null, true, GameShaders.Misc["Infernum:TwinsFlameTrail"]);
            GameShaders.Misc["Infernum:TwinsFlameTrail"].UseImage1("Images/Misc/Perlin");
            TrailDrawer.Draw(Projectile.oldPos, Projectile.Size * 0.5f - Main.screenPosition, 24);
            return true;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item74, Projectile.Center);
            Utilities.CreateGenericDustExplosion(Projectile.Center, 242, 10, 7f, 1.25f);
        }
    }
}
