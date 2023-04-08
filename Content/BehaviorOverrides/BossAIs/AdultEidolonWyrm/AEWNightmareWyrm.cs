using CalamityMod;
using InfernumMode.Common.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class AEWNightmareWyrm : ModProjectile
    {
        public const int SegmentCount = 27;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Eidolon Wyrm");
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = SegmentCount + 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 480;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Emit particles.
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (Projectile.WithinRange(target.Center, 1000f) && Projectile.FinalExtraUpdate())
                EmitParticles();

            // Slither around.
            Projectile.velocity.X = (float)Math.Cos(MathHelper.TwoPi * Projectile.timeLeft / 35f) * 6f;

            // Decide rotation.
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public void EmitParticles()
        {
            Vector2 idealParticleVelocity = -Projectile.velocity.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(2f, 6f);

            for (int i = 0; i < 4; i++)
            {
                Dust darkMatter = Dust.NewDustDirect(Projectile.TopLeft, Projectile.width, Projectile.height, DustID.Asphalt, 0f, -3f, 0, default, 1.4f);
                darkMatter.noGravity = true;
                darkMatter.velocity = Vector2.Lerp(darkMatter.velocity, idealParticleVelocity, 0.55f);
                darkMatter.fadeIn = Main.rand.NextFloat(0.3f, 0.8f);
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return Color.White * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 decideDrawPosition(int index)
            {
                return Projectile.oldPos[index] + Projectile.Size * 0.5f - Main.screenPosition;
            }

            static Texture2D decideSegmentTexture(int index)
            {
                // By default, segments are heads.
                Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/NPCs/Abyss/EidolonWyrmHead").Value;

                // After the head is drawn, use body segments.
                if (index >= 1)
                {
                    string bodyTexturePath = "CalamityMod/NPCs/Abyss/EidolonWyrmBody";
                    if (index % 2 == 1)
                        bodyTexturePath += "Alt";

                    texture = ModContent.Request<Texture2D>(bodyTexturePath).Value;
                }

                // The last segment should be a tail.
                if (index == SegmentCount - 1)
                    texture = ModContent.Request<Texture2D>("CalamityMod/NPCs/Abyss/EidolonWyrmTail").Value;

                return texture;
            }

            // Draw the main body.
            for (int i = 0; i < SegmentCount; i++)
            {
                Texture2D texture = decideSegmentTexture(i);
                Color color = Projectile.GetAlpha(Color.White);
                AEWShadowFormDrawSystem.LightAndDarkEffectsCache.Add(new(texture, decideDrawPosition(i), null, color, Projectile.oldRot[i], texture.Size() * 0.5f, Projectile.scale, 0, 0));
            }

            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < SegmentCount; i++)
            {
                Vector2 segmentCenter = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                if (Utils.CenteredRectangle(segmentCenter, Projectile.Size).Intersects(targetHitbox))
                    return true;
            }

            return false;
        }
    }
}
