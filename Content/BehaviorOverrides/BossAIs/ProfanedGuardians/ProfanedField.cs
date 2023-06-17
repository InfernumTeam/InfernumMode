using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class ProfanedField : ModProjectile
    {
        public ref float GeneralTimer => ref Projectile.ai[0];

        public ref float Radius => ref Projectile.ai[1];

        public const float MaxRadius = 336f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Profaned Defender Field");
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 240;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Emit light.
            Lighting.AddLight(Projectile.Center, Color.Yellow.ToVector3() * 0.45f);

            // Have the rocks expand outward.
            bool collapse = Projectile.timeLeft < 60;
            Radius = Lerp(Radius, collapse ? 0f : MaxRadius, 0.04f);
            if (collapse && Radius >= 3f)
                Radius -= 1.25f;

            // Create a bunch of fire inside of the field.
            for (int i = 0; i < 7; i++)
            {
                Vector2 fireSpawnPosition = Projectile.Center + Main.rand.NextVector2Circular(Radius, Radius) * 0.8f;
                if (!Main.LocalPlayer.WithinRange(fireSpawnPosition, 1000f))
                    continue;

                MediumMistParticle fire = new(fireSpawnPosition, -Vector2.UnitY.RotatedByRandom(0.85f) * Main.rand.NextFloat(1f, 5.6f), Color.Orange, Color.Yellow, 0.5f, 255f);
                GeneralParticleHandler.SpawnParticle(fire);
            }

            Projectile.rotation += Projectile.velocity.X * 0.006f;
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.04f, 0f, 1f);
            GeneralTimer++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float spinDirection = (Projectile.identity % 2 == 0).ToDirectionInt();
            Vector2 baseDrawPosition = Projectile.Center;

            // Draw lines.
            for (int i = 0; i < 6; i++)
            {
                Vector2 drawPosition = baseDrawPosition + (TwoPi * (i - 1f) / 6f + GeneralTimer * spinDirection / 54f).ToRotationVector2() * Radius;
                Vector2 drawPositionNext = baseDrawPosition + (TwoPi * i / 6f + GeneralTimer * spinDirection / 54f).ToRotationVector2() * Radius;
                Main.spriteBatch.DrawLineBetter(drawPosition, drawPositionNext, (Color.Orange * 0.6f) with { A = 0 }, 8f);
                Main.spriteBatch.DrawLineBetter(drawPosition, drawPositionNext, (Color.Yellow * 0.85f) with { A = 72 }, 5f);
                Main.spriteBatch.DrawLineBetter(drawPosition, drawPositionNext, Color.White with { A = 125 }, 2f);
            }

            for (int i = 1; i <= 6; i++)
            {
                Texture2D texture = ModContent.Request<Texture2D>($"CalamityMod/Projectiles/Typeless/ArtifactOfResilienceShard{i}").Value;
                Vector2 origin = texture.Size() * 0.5f;
                Vector2 drawPosition = baseDrawPosition + (TwoPi * (i - 1f) / 6f + GeneralTimer * spinDirection / 54f).ToRotationVector2() * Radius - Main.screenPosition;
                Main.spriteBatch.Draw(texture, drawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, origin, Projectile.scale, 0, 0f);
            }

            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(Projectile.Center, Radius * 0.75f, targetHitbox);
        }

        public override bool? CanDamage() => Radius >= MaxRadius * 0.5f;
    }
}
