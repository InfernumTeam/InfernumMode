using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class SwervingDarkMagicBlast : ModProjectile
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
            Projectile.timeLeft = 180;
            Projectile.Calamity().canBreakPlayerDefense = true;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.075f, 0f, 1f);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];

            int swerveTime = 25;
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            ref float attackTimer = ref Projectile.ai[1];

            if (attackTimer <= swerveTime)
            {
                float moveOffsetAngle = (float)Math.Cos(Projectile.Center.Length() / 150f + Projectile.whoAmI % 10f / 10f * MathHelper.TwoPi);
                moveOffsetAngle *= MathHelper.Pi * 0.85f / swerveTime;

                Projectile.velocity = Projectile.velocity.RotatedBy(moveOffsetAngle) * 0.97f;
            }
            else
            {
                if (Projectile.WithinRange(target.Center, 700f))
                {
                    bool canNoLongerHome = attackTimer >= swerveTime + 125f;
                    float newSpeed = MathHelper.Clamp(Projectile.velocity.Length() + (canNoLongerHome ? 0.075f : 0.024f), 13f, canNoLongerHome ? 36f : 29.5f);
                    if (!target.dead && target.active && !Projectile.WithinRange(target.Center, 220f) && !canNoLongerHome)
                        Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * Projectile.velocity.Length(), 0.075f);

                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * newSpeed;
                }
                else
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * Projectile.velocity.Length(), 0.2f).SafeNormalize(Vector2.UnitY) * 21f;

                // Die on tile collision or after enough time.
                bool shouldDie = (Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height) && attackTimer >= swerveTime + 95f) || attackTimer >= swerveTime + 240f;
                if (attackTimer >= swerveTime + 90f && shouldDie)
                    Projectile.Kill();
            }
            attackTimer++;
        }

        public float FlameTrailWidthFunction(float completionRatio)
        {
            return MathHelper.SmoothStep(24f, 5f, completionRatio) * Projectile.Opacity;
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
            TrailDrawer.Draw(Projectile.oldPos, Projectile.Size * 0.5f - Main.screenPosition, 44);
            return true;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item74, Projectile.Center);
            Utilities.CreateGenericDustExplosion(Projectile.Center, 242, 10, 7f, 1.25f);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 shootVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2.25f, 4.25f);
                Utilities.NewProjectileBetter(Projectile.Center, shootVelocity, ModContent.ProjectileType<ShadowBlast>(), 500, 0f);
            }
        }
    }
}
