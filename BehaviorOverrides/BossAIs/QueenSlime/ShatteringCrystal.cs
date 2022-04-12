using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.QueenSlime
{
    public class ShatteringCrystal : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public ref float AngularOffset => ref projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Royal Crystal");
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 26;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 300;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.GetLerpValue(0f, 30f, Time, true) * Utils.GetLerpValue(0f, 12f, projectile.timeLeft, true);

            // Shatter if colliding with another crystal.
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (i != projectile.whoAmI && Main.projectile[i].type == projectile.type && Main.projectile[i].Hitbox.Intersects(projectile.Hitbox))
                {
                    projectile.Center = Main.projectile[i].Center;
                    Main.projectile[i].Kill();
                    projectile.Kill();
                }
            }

            float flySpeed = MathHelper.SmoothStep(-3f, 26f, Utils.GetLerpValue(15f, 50f, Time, true));
            Vector2 directionToCenter = -AngularOffset.ToRotationVector2();
            projectile.velocity = directionToCenter * flySpeed;
            projectile.rotation = directionToCenter.ToRotation() + MathHelper.PiOver2;

            Time++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Vector2 drawPosition = projectile.Center + Vector2.UnitY * projectile.gfxOffY - Main.screenPosition;
            Texture2D texture = Main.projectileTexture[projectile.type];
            Color backimageColor = projectile.GetAlpha(lightColor);
            backimageColor = Color.Lerp(backimageColor, Color.HotPink, 0.6f);
            backimageColor.A /= 6;

            Vector2 origin = texture.Size() * 0.5f;
            float drawOffsetFactor = (float)Math.Cos(MathHelper.TwoPi * Time / 30f) * 3f + 8f;
            drawOffsetFactor *= Utils.GetLerpValue(30f, 20f, Time, true);
            for (int i = 0; i < 4; i++)
            {
                double angle = i * MathHelper.PiOver2;
                Vector2 drawOffset = Vector2.UnitY.RotatedBy(angle) * drawOffsetFactor;
                spriteBatch.Draw(texture, drawPosition + drawOffset, null, backimageColor * 0.6f, projectile.rotation, origin, projectile.scale, 0, 0f);
            }
            Utilities.DrawAfterimagesCentered(projectile, Color.White, ProjectileID.Sets.TrailingMode[projectile.type], 1);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Item27, projectile.Center);

            if (AngularOffset != 0f || Main.netMode == NetmodeID.MultiplayerClient)
                return;

            float shootOffsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < 18; i++)
            {
                Vector2 shardVelocity = (MathHelper.TwoPi * i / 18f + shootOffsetAngle).ToRotationVector2() * Main.rand.NextFloat(6f, 8f);
                Utilities.NewProjectileBetter(projectile.Center, shardVelocity, ModContent.ProjectileType<CrystalShard>(), 125, 0f);
            }
        }

        public override bool CanDamage() => projectile.Opacity > 0.9f;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(projectile.Center, projectile.scale * 14f, targetHitbox);
        }
    }
}
