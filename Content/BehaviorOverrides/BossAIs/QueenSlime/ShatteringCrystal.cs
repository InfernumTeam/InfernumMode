using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.QueenSlime
{
    public class ShatteringCrystal : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public ref float AngularOffset => ref Projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Royal Crystal");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 26;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 40f, Time, true) * Utils.GetLerpValue(0f, 12f, Projectile.timeLeft, true);

            // Shatter if colliding with another crystal.
            bool justDieAlreadyYouStupidCunt = Time >= 108f;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (MathHelper.Distance(AngularOffset, 0f) >= 0.02f)
                    break;

                if (i != Projectile.whoAmI && Main.projectile[i].type == Projectile.type && Main.projectile[i].Hitbox.Intersects(Projectile.Hitbox) && Main.projectile[i].active || justDieAlreadyYouStupidCunt)
                {
                    Projectile.Center = Main.projectile[i].Center;
                    Main.projectile[i].Kill();
                    Projectile.Kill();
                }
            }

            float flySpeed = MathHelper.SmoothStep(-3f, 20f, Utils.GetLerpValue(15f, 72f, Time, true));
            Vector2 directionToCenter = -AngularOffset.ToRotationVector2();
            Projectile.velocity = directionToCenter * flySpeed;
            Projectile.rotation = directionToCenter.ToRotation() + MathHelper.PiOver2;

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 drawPosition = Projectile.Center + Vector2.UnitY * Projectile.gfxOffY - Main.screenPosition;
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Color backimageColor = Projectile.GetAlpha(lightColor);
            backimageColor = Color.Lerp(backimageColor, Color.HotPink, 0.6f);
            backimageColor.A /= 6;

            Vector2 origin = texture.Size() * 0.5f;
            float drawOffsetFactor = (float)Math.Cos(MathHelper.TwoPi * Time / 30f) * 3f + 8f;
            drawOffsetFactor *= Utils.GetLerpValue(30f, 20f, Time, true);
            for (int i = 0; i < 4; i++)
            {
                double angle = i * MathHelper.PiOver2;
                Vector2 drawOffset = Vector2.UnitY.RotatedBy(angle) * drawOffsetFactor;
                Main.spriteBatch.Draw(texture, drawPosition + drawOffset, null, backimageColor * 0.6f, Projectile.rotation, origin, Projectile.scale, 0, 0f);
            }
            Utilities.DrawAfterimagesCentered(Projectile, Color.White, ProjectileID.Sets.TrailingMode[Projectile.type], 1);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item27, Projectile.Center);

            if (AngularOffset != 0f || Main.netMode == NetmodeID.MultiplayerClient)
                return;

            float shootOffsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < 18; i++)
            {
                Vector2 shardVelocity = (MathHelper.TwoPi * i / 18f + shootOffsetAngle).ToRotationVector2() * Main.rand.NextFloat(6f, 8f);
                Utilities.NewProjectileBetter(Projectile.Center, shardVelocity, ModContent.ProjectileType<CrystalShard>(), 125, 0f);
            }
        }

        public override bool? CanDamage() => Projectile.Opacity > 0.9f ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(Projectile.Center, Projectile.scale * 14f, targetHitbox);
        }
    }
}
