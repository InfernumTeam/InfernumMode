using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class SwervingDarkMagicBlast : ModProjectile
    {
        public PrimitiveTrailCopy TrailDrawer = null;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Magic Burst");
            Main.projFrames[projectile.type] = 4;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 15;
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 26;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 180;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.075f, 0f, 1f);
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 5 % Main.projFrames[projectile.type];

            int swerveTime = 25;
            Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
            ref float attackTimer = ref projectile.ai[1];

            if (attackTimer <= swerveTime)
            {
                float moveOffsetAngle = (float)Math.Cos(projectile.Center.Length() / 150f + projectile.whoAmI % 10f / 10f * MathHelper.TwoPi);
                moveOffsetAngle *= MathHelper.Pi * 0.85f / swerveTime;

                projectile.velocity = projectile.velocity.RotatedBy(moveOffsetAngle) * 0.97f;
            }
            else
            {
                if (projectile.WithinRange(target.Center, 700f))
                {
                    bool canNoLongerHome = attackTimer >= swerveTime + 125f;
                    float newSpeed = MathHelper.Clamp(projectile.velocity.Length() + (canNoLongerHome ? 0.075f : 0.024f), 13f, canNoLongerHome ? 32f : 25f);
                    if (!target.dead && target.active && !projectile.WithinRange(target.Center, 220f) && !canNoLongerHome)
                        projectile.velocity = Vector2.Lerp(projectile.velocity, projectile.SafeDirectionTo(target.Center) * projectile.velocity.Length(), 0.075f);

                    projectile.velocity = projectile.velocity.SafeNormalize(Vector2.UnitY) * newSpeed;
                }
                else
                    projectile.velocity = Vector2.Lerp(projectile.velocity, projectile.SafeDirectionTo(target.Center) * projectile.velocity.Length(), 0.2f).SafeNormalize(Vector2.UnitY) * 24f;

                // Die on tile collision or after enough time.
                bool shouldDie = (Collision.SolidCollision(projectile.position, projectile.width, projectile.height) && attackTimer >= swerveTime + 95f) || attackTimer >= swerveTime + 240f;
                if (attackTimer >= swerveTime + 90f && shouldDie)
                    projectile.Kill();
            }
            attackTimer++;
        }

        public float FlameTrailWidthFunction(float completionRatio)
        {
            return MathHelper.SmoothStep(24f, 5f, completionRatio) * projectile.Opacity;
        }

        public Color FlameTrailColorFunction(float completionRatio)
        {
            float trailOpacity = Utils.InverseLerp(0.8f, 0.27f, completionRatio, true) * Utils.InverseLerp(0f, 0.067f, completionRatio, true) * 0.9f;
            Color startingColor = Color.Lerp(Color.White, Color.IndianRed, 0.25f);
            Color middleColor = Color.Lerp(Color.Maroon, Color.Red, 0.4f);
            Color endColor = Color.Lerp(Color.Purple, Color.Black, 0.35f);
            Color color = CalamityUtils.MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * trailOpacity;
            color.A = 184;
            return color * projectile.Opacity;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (TrailDrawer is null)
                TrailDrawer = new PrimitiveTrailCopy(FlameTrailWidthFunction, FlameTrailColorFunction, null, true, GameShaders.Misc["Infernum:TwinsFlameTrail"]);
            GameShaders.Misc["Infernum:TwinsFlameTrail"].UseImage("Images/Misc/Perlin");
            TrailDrawer.Draw(projectile.oldPos, projectile.Size * 0.5f - Main.screenPosition, 44);
            return true;
		}

		public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Item74, projectile.Center);
            Utilities.CreateGenericDustExplosion(projectile.Center, 242, 10, 7f, 1.25f);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 shootVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2.25f, 4.25f);
                    Utilities.NewProjectileBetter(projectile.Center, shootVelocity, ModContent.ProjectileType<ShadowBlast>(), 550, 0f);
                }
            }
        }
    }
}
