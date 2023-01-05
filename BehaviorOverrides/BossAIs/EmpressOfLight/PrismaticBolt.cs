using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class PrismaticBolt : ModProjectile
    {
        public Color MyColor
        {
            get
            {
                Color color = Main.hslToRgb((Projectile.ai[1] + 0.2f) % 1f, 1f, 0.5f) * Projectile.Opacity * 1.3f;
                if (EmpressOfLightBehaviorOverride.ShouldBeEnraged)
                    color = EmpressOfLightBehaviorOverride.GetDaytimeColor(Projectile.ai[1] % 1f) * Projectile.Opacity;

                color.A /= 8;
                return color;
            }
        }

        public override string Texture => "InfernumMode/ExtraTextures/GreyscaleObjects/Gleam";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Prismatic Bolt");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 30;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 230;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            bool canRotate = false;
            bool canMoveTowardsTarget = false;
            float stopMovingTime = 140f;
            float dissipateTime = 30f;

            if (Projectile.timeLeft > stopMovingTime)
            {
                canRotate = true;
            }
            else if (Projectile.timeLeft > dissipateTime)
            {
                canMoveTowardsTarget = true;
            }
            if (canRotate)
            {
                float offsetInterpolant = (float)Math.Cos(Projectile.whoAmI % 6f / 6f + Projectile.position.X / 320f + Projectile.position.Y / 160f);
                Projectile.velocity = Projectile.velocity.RotatedBy(MathHelper.Pi * offsetInterpolant / 120f) * 0.98f;
            }

            if (canMoveTowardsTarget)
            {
                int targetIndex = (int)Projectile.ai[0];
                Vector2 idealVelocity = Projectile.velocity;
                if (Projectile.hostile && Main.player.IndexInRange(targetIndex))
                {
                    idealVelocity = Projectile.SafeDirectionTo(Main.player[targetIndex].Center) * 34f;
                    if (Projectile.localAI[0] > 0f)
                        idealVelocity *= Projectile.localAI[0];
                }

                float amount = MathHelper.Lerp(0.056f, 0.12f, Utils.GetLerpValue(stopMovingTime, 30f, Projectile.timeLeft, true));
                Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, idealVelocity, amount);
            }
            Projectile.Opacity = Utils.GetLerpValue(240f, 220f, Projectile.timeLeft, true);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            int trailLength = Math.Min(Projectile.timeLeft, Projectile.oldPos.Length - 17);
            for (int i = 0; i < trailLength; i++)
            {
                int x = (int)Projectile.oldPos[i].X;
                int y = (int)Projectile.oldPos[i].Y;
                if (new Rectangle(x, y, 30, 30).Intersects(targetHitbox))
                    return true;
            }
            return false;
        }

        public override void Kill(int timeLeft)
        {
            int dustCount = 20;
            float angularOffset = Projectile.velocity.ToRotation();
            for (int i = 0; i < dustCount; i++)
            {
                Dust rainbowMagic = Dust.NewDustPerfect(Projectile.Center, 267);
                rainbowMagic.fadeIn = 1f;
                rainbowMagic.noGravity = true;
                rainbowMagic.alpha = 100;
                rainbowMagic.color = Color.Lerp(MyColor, Color.White, Main.rand.NextFloat(0.3f));
                if (i % 4 == 0)
                {
                    rainbowMagic.velocity = angularOffset.ToRotationVector2() * 3.2f;
                    rainbowMagic.scale = 2.3f;
                }
                else if (i % 2 == 0)
                {
                    rainbowMagic.velocity = angularOffset.ToRotationVector2() * 1.8f;
                    rainbowMagic.scale = 1.9f;
                }
                else
                {
                    rainbowMagic.velocity = angularOffset.ToRotationVector2();
                    rainbowMagic.scale = 1.6f;
                }
                angularOffset += MathHelper.TwoPi / dustCount;
                rainbowMagic.velocity += Projectile.velocity * Main.rand.NextFloat(0.5f);
            }
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
            Vector2 origin = texture.Size() * 0.5f;

            int trailLength = Math.Min(Projectile.timeLeft, Projectile.oldPos.Length);
            for (int i = 0; i < trailLength; ++i)
            {
                float afterimageRot = Projectile.oldRot[i];
                Vector2 drawPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
                Color baseColor = i <= 1 ? Color.White : MyColor;
                Color afterimageColor = baseColor * ((Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length);
                Main.spriteBatch.Draw(texture, drawPos, null, afterimageColor, afterimageRot, origin, Projectile.scale, 0, 0f);
            }

            Color color = MyColor * 0.5f;
            color.A = 0;

            Main.spriteBatch.Draw(texture, drawPosition, null, color, Projectile.rotation, origin, Projectile.scale * 0.9f, 0, 0);
            Color bigGleamColor = color;
            Color smallGleamColor = color * 0.5f;
            float opacity = Utils.GetLerpValue(15f, 30f, Projectile.timeLeft, true) * 
                Utils.GetLerpValue(240f, 200f, Projectile.timeLeft, true) * 
                (1f + 0.2f * (float)Math.Cos(Main.GlobalTimeWrappedHourly % 30f / 0.5f * MathHelper.Pi * 6f)) * 0.8f;
            Vector2 bigGleamScale = new Vector2(0.5f, 5f) * opacity;
            Vector2 smallGleamScale = new Vector2(0.5f, 2f) * opacity;
            bigGleamColor *= opacity;
            smallGleamColor *= opacity;

            Main.spriteBatch.Draw(texture, drawPosition, null, bigGleamColor, 1.57079637f, origin, bigGleamScale, 0, 0);
            Main.spriteBatch.Draw(texture, drawPosition, null, bigGleamColor, 0f, origin, smallGleamScale, 0, 0);
            Main.spriteBatch.Draw(texture, drawPosition, null, smallGleamColor, 1.57079637f, origin, bigGleamScale * 0.6f, 0, 0);
            Main.spriteBatch.Draw(texture, drawPosition, null, smallGleamColor, 0f, origin, smallGleamScale * 0.6f, 0, 0);
            return false;
        }
    }
}
