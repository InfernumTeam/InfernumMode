using CalamityMod;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Thanatos
{
    public class LightRayTelegraph : ModProjectile
    {
        public int Lifetime;

        public static NPC Thanatos => Main.npc[CalamityGlobalNPC.draedonExoMechWorm];

        public Vector2 StartingPosition => Thanatos.Center - (Thanatos.rotation - MathHelper.PiOver2 + CurrentSpread).ToRotationVector2() * Projectile.Opacity * 275f;

        public Color RayColor => CalamityUtils.MulticolorLerp(RayHue, CalamityUtils.ExoPalette);

        public Color HueDownscaledRayColor => RayColor * 0.66f;

        public ref float RayHue => ref Projectile.ai[0];

        public ref float MaximumSpread => ref Projectile.ai[1];

        public ref float CurrentSpread => ref Projectile.localAI[0];

        public ref float Time => ref Projectile.localAI[1];
        
        public override string Texture => "CalamityMod/Projectiles/StarProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Prismatic Light");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 14;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 900;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Lifetime);
            writer.Write(CurrentSpread);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Lifetime = reader.ReadInt32();
            CurrentSpread = reader.ReadSingle();
        }

        public override void AI()
        {
            if (CalamityGlobalNPC.draedonExoMechWorm == -1)
            {
                Projectile.Kill();
                return;
            }

            DelegateMethods.v3_1 = RayColor.ToVector3() * 0.5f;
            Utils.PlotTileLine(StartingPosition, Projectile.Center, 8f, DelegateMethods.CastLight);

            Projectile.alpha = Utils.Clamp(Projectile.alpha - 18, 0, 255);
            Projectile.velocity = Vector2.Zero;

            // Fade in, grow, and spread out.
            float fadeInInterpolant = Utils.GetLerpValue(0f, Lifetime * 0.45f, Time, true) * Utils.GetLerpValue(Lifetime, Lifetime - 12f, Time, true);
            float lightOffset = MathHelper.Lerp(20f, (float)Math.Sin(Time / 7f) * 75f + 1850f, fadeInInterpolant);
            float offsetAngleFactor = MathHelper.Lerp(0.7f, 1f, (float)Math.Cos(Time / 23f) * 0.5f + 0.5f);
            CurrentSpread = MathHelper.Lerp(CurrentSpread, MaximumSpread, 0.015f);
            if (MathHelper.Distance(CurrentSpread, MaximumSpread) < 0.03f)
                CurrentSpread = MaximumSpread;

            Projectile.Opacity = fadeInInterpolant;
            Projectile.Center = StartingPosition + (Thanatos.rotation - MathHelper.PiOver2 + CurrentSpread * offsetAngleFactor).ToRotationVector2() * lightOffset;

            Time++;
            if (Time >= Lifetime)
                Projectile.Kill();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), StartingPosition, Projectile.Center, Projectile.scale * 22f, ref _);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 baseDrawPosition = Projectile.Center + Vector2.UnitY * Projectile.gfxOffY - Main.screenPosition;
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            Vector2 origin = frame.Size() / 2f;
            Color fadedRayColor = Projectile.GetAlpha(lightColor);
            Color fullbrightRayColor = HueDownscaledRayColor.MultiplyRGBA(new Color(255, 255, 255, 0)) * Projectile.Opacity;

            // Draw the shimmering ray.
            if (Projectile.Opacity > 0.3f)
            {
                Vector2 drawOffset = (StartingPosition - Projectile.Center) * 0.5f;
                Vector2 scale = new(1.2f, drawOffset.Length() * 2f / texture.Height);
                float rotation = drawOffset.ToRotation() + MathHelper.PiOver2;
                for (int i = 0; i < 8; i++)
                {
                    Vector2 drawPosition = baseDrawPosition;
                    drawPosition += (MathHelper.TwoPi * i / 8f + Main.GlobalTimeWrappedHourly * 3f).ToRotationVector2() * Projectile.Opacity * 1.5f;
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
            if (Main.myPlayer != Projectile.owner)
                return;

            Vector2 oldSize = Projectile.Size;
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = 60;
            Projectile.Center = Projectile.position;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.Damage();
            Projectile.position = Projectile.Center;
            Projectile.Size = oldSize;
            Projectile.Center = Projectile.position;
        }

        public void CreateKillExplosionBurstDust(int dustCount)
        {
            if (Main.dedServ)
                return;

            Vector2 baseExplosionDirection = -Vector2.UnitY.RotatedByRandom(MathHelper.Pi) * 3f;
            Vector2 outwardFireSpeedFactor = new(2.1f, 2f);
            Color brightenedRayColor = RayColor;
            brightenedRayColor.A = 255;

            for (float i = 0f; i < dustCount; i++)
            {
                Dust explosionDust = Dust.NewDustDirect(Projectile.Center, 0, 0, 267, 0f, 0f, 0, brightenedRayColor, 1f);
                explosionDust.position = Projectile.Center;
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
                Dust explosionDust = Dust.NewDustDirect(Projectile.Center, 0, 0, 267, 0f, 0f, 0, brightenedRayColor, 1f);
                explosionDust.position = Projectile.Center;
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

        public override Color? GetAlpha(Color lightColor) => new Color(Projectile.Opacity, Projectile.Opacity, Projectile.Opacity, 0);
    }
}
