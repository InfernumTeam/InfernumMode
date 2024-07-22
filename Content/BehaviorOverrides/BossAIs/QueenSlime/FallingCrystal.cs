using System;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.QueenSlime
{
    public class FallingCrystal : ModProjectile, IPixelPrimitiveDrawer
    {
        public PrimitiveTrailCopy TrailDrawer
        {
            get;
            private set;
        }

        public ref float Time => ref Projectile.ai[1];

        public override string Texture => $"Terraria/Images/Extra_{ExtrasID.QueenSlimeCrystalCore}";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Hallow Crystal");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.penetrate = -1;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 720;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
        }

        public override void AI()
        {
            if (Projectile.ai[0] == 1f)
            {
                Projectile.velocity.Y = Clamp(Projectile.velocity.Y + 0.4f * Math.Sign(Projectile.velocity.Y), -19f, 19f);
                Projectile.tileCollide = true;
            }

            // Jitter in place slightly if not accelerating.
            else
                Projectile.Center += Main.rand.NextVector2Circular(0.65f, 0.65f);

            Projectile.Opacity = Utils.GetLerpValue(0f, 20f, Time, true);

            Time++;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            // Create a crystal shatter sound.
            SoundEngine.PlaySound(SoundID.Item27, Projectile.Center);

            // Create a bunch of crystal shards.
            for (int i = 0; i < 15; i++)
            {
                Dust crystalShard = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(24f, 24f), Main.rand.Next(DustID.BlueCrystalShard, DustID.PurpleCrystalShard + 1));
                crystalShard.velocity = Main.rand.NextVector2Circular(4f, 4f) - Vector2.UnitY * 2.5f;
                crystalShard.noGravity = Main.rand.NextBool();
                crystalShard.scale = Main.rand.NextFloat(0.9f, 1.3f);
            }

            for (int i = 1; i <= 3; i++)
                Gore.NewGore(Projectile.GetSource_Death(), Projectile.Center, Main.rand.NextVector2CircularEdge(4f, 4f), Mod.Find<ModGore>($"QSCrystal{i}").Type, Projectile.scale);
        }

        public override bool? CanDamage() => Time >= 16f;

        public override Color? GetAlpha(Color lightColor) => Color.Lerp(Color.HotPink with { A = 0 }, Color.White, Utils.GetLerpValue(0f, 35f, Time, true)) * Projectile.Opacity;

        public float WidthFunction(float completionRatio) => SmoothStep(34f, 5f, completionRatio) * Projectile.Opacity;


        public Color ColorFunction(float completionRatio)
        {
            float trailOpacity = Utils.GetLerpValue(1f, 0.8f, completionRatio, true);
            Color color = Color.Lerp(Color.Purple, Color.CornflowerBlue, completionRatio + 0.3f) * trailOpacity;
            color.A = (byte)(trailOpacity * 255);
            return color * Projectile.Opacity;
        }

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            if (Projectile.velocity == Vector2.Zero)
                return;

            TrailDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].UseImage1(InfernumTextureRegistry.StreakFaded);
            TrailDrawer.DrawPixelated(Projectile.oldPos, Projectile.Size * 0.5f - Main.screenPosition, 30);
        }
    }
}
