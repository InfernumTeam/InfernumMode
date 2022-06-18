using CalamityMod;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Thanatos
{
    public class LightRayTelegraph : ModProjectile
    {
        public int Lifetime;
        public static NPC Thanatos => Main.npc[CalamityGlobalNPC.draedonExoMechWorm];
        public Vector2 StartingPosition => Thanatos.Center - (Thanatos.rotation - MathHelper.PiOver2 + CurrentSpread).ToRotationVector2() * projectile.Opacity * 275f;

        public Color RayColor => CalamityUtils.MulticolorLerp(RayHue, CalamityUtils.ExoPalette);
        public Color HueDownscaledRayColor => RayColor * 0.66f;
        public ref float RayHue => ref projectile.ai[0];
        public ref float MaximumSpread => ref projectile.ai[1];
        public ref float CurrentSpread => ref projectile.localAI[0];
        public ref float Time => ref projectile.localAI[1];
        public override string Texture => "CalamityMod/Projectiles/StarProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Prismatic Light");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 14;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 900;
        }

        public override void AI()
        {
            if (CalamityGlobalNPC.draedonExoMechWorm == -1)
            {
                projectile.Kill();
                return;
            }

            DelegateMethods.v3_1 = RayColor.ToVector3() * 0.5f;
            Utils.PlotTileLine(StartingPosition, projectile.Center, 8f, DelegateMethods.CastLight);

            projectile.alpha = Utils.Clamp(projectile.alpha - 18, 0, 255);
            projectile.velocity = Vector2.Zero;

            // Fade in, grow, and spread out.
            float fadeInInterpolant = Utils.InverseLerp(0f, Lifetime * 0.45f, Time, true) * Utils.InverseLerp(Lifetime, Lifetime - 12f, Time, true);
            float lightOffset = MathHelper.Lerp(20f, (float)Math.Sin(Time / 7f) * 75f + 1850f, fadeInInterpolant);
            float offsetAngleFactor = MathHelper.Lerp(0.7f, 1f, (float)Math.Cos(Time / 23f) * 0.5f + 0.5f);
            CurrentSpread = MathHelper.Lerp(CurrentSpread, MaximumSpread, 0.015f);
            projectile.Opacity = fadeInInterpolant;
            projectile.Center = StartingPosition + (Thanatos.rotation - MathHelper.PiOver2 + CurrentSpread * offsetAngleFactor).ToRotationVector2() * lightOffset;

            Time++;
            if (Time >= Lifetime)
                projectile.Kill();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), StartingPosition, projectile.Center, projectile.scale * 22f, ref _);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Vector2 baseDrawPosition = projectile.Center + Vector2.UnitY * projectile.gfxOffY - Main.screenPosition;
            Texture2D texture = Main.projectileTexture[projectile.type];
            Rectangle frame = texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
            Vector2 origin = frame.Size() / 2f;
            Color fadedRayColor = projectile.GetAlpha(lightColor);
            Color fullbrightRayColor = HueDownscaledRayColor.MultiplyRGBA(new Color(255, 255, 255, 0)) * projectile.Opacity;

            // Draw the shimmering ray.
            if (projectile.Opacity > 0.3f)
            {
                Vector2 drawOffset = (StartingPosition - projectile.Center) * 0.5f;
                Vector2 scale = new Vector2(1.2f, drawOffset.Length() * 2f / texture.Height);
                float rotation = drawOffset.ToRotation() + MathHelper.PiOver2;
                for (int i = 0; i < 8; i++)
                {
                    Vector2 drawPosition = baseDrawPosition;
                    drawPosition += (MathHelper.TwoPi * i / 8f + Main.GlobalTime * 3f).ToRotationVector2() * projectile.Opacity * 1.5f;
                    drawPosition += drawOffset;

                    Main.spriteBatch.Draw(texture, drawPosition, frame, fullbrightRayColor, rotation, origin, scale, SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(texture, drawPosition, frame, fadedRayColor, rotation, origin, scale * 0.5f, SpriteEffects.None, 0f);
                }
            }
            return false;
        }

        public override void Kill(int timeLeft)
        {
            CreateKillExplosionBurstDust(Main.rand.Next(7, 13));

            // Adjust values and do damage before dying.
            if (Main.myPlayer != projectile.owner)
                return;

            Vector2 oldSize = projectile.Size;
            projectile.position = projectile.Center;
            projectile.width = projectile.height = 60;
            projectile.Center = projectile.position;
            projectile.usesLocalNPCImmunity = true;
            projectile.localNPCHitCooldown = 10;
            projectile.Damage();
            projectile.position = projectile.Center;
            projectile.Size = oldSize;
            projectile.Center = projectile.position;
        }

        public void CreateKillExplosionBurstDust(int dustCount)
        {
            if (Main.dedServ)
                return;

            Vector2 baseExplosionDirection = -Vector2.UnitY.RotatedByRandom(MathHelper.Pi) * 3f;
            Vector2 outwardFireSpeedFactor = new Vector2(2.1f, 2f);
            Color brightenedRayColor = RayColor;
            brightenedRayColor.A = 255;

            for (float i = 0f; i < dustCount; i++)
            {
                Dust explosionDust = Dust.NewDustDirect(projectile.Center, 0, 0, 267, 0f, 0f, 0, brightenedRayColor, 1f);
                explosionDust.position = projectile.Center;
                explosionDust.velocity = baseExplosionDirection.RotatedBy(MathHelper.TwoPi * i / dustCount) * outwardFireSpeedFactor * Main.rand.NextFloat(0.8f, 1.2f);
                explosionDust.noGravity = true;
                explosionDust.scale = 1.1f;
                explosionDust.fadeIn = Main.rand.NextFloat(1.4f, 2.4f);

                explosionDust = Dust.CloneDust(explosionDust);
                explosionDust.scale /= 2f;
                explosionDust.fadeIn /= 2f;
                explosionDust.color = new Color(255, 255, 255, 255);
            }
            for (float i = 0f; i < dustCount; i++)
            {
                Dust explosionDust = Dust.NewDustDirect(projectile.Center, 0, 0, 267, 0f, 0f, 0, brightenedRayColor, 1f);
                explosionDust.position = projectile.Center;
                explosionDust.velocity = baseExplosionDirection.RotatedBy(MathHelper.TwoPi * i / dustCount) * outwardFireSpeedFactor * Main.rand.NextFloat(0.8f, 1.2f);
                explosionDust.velocity *= Main.rand.NextFloat() * 0.8f;
                explosionDust.noGravity = true;
                explosionDust.scale = Main.rand.NextFloat();
                explosionDust.fadeIn = Main.rand.NextFloat(1.4f, 2.4f);

                explosionDust = Dust.CloneDust(explosionDust);
                explosionDust.scale /= 2f;
                explosionDust.fadeIn /= 2f;
                explosionDust.color = new Color(255, 255, 255, 255);
            }
        }

        public override Color? GetAlpha(Color lightColor) => new Color(projectile.Opacity, projectile.Opacity, projectile.Opacity, 0);
    }
}
