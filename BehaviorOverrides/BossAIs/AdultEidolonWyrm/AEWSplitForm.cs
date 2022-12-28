using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class AEWSplitForm : ModProjectile
    {
        public bool DarkForm
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value.ToInt();
        }

        public const int SegmentCount = 60;

        public const float OffsetPerSegment = 72f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Adult Eidolon Wyrm");

        public override void SetDefaults()
        {
            Projectile.width = 78;
            Projectile.height = 78;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.MaxUpdates = 6;
            Projectile.timeLeft = Projectile.MaxUpdates * 240;
        }

        public override void AI()
        {
            // Decide rotation.
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 decideDrawPosition(int index)
            {
                return Projectile.Center - Main.screenPosition - Projectile.velocity.SafeNormalize(Vector2.UnitY) * OffsetPerSegment * Projectile.scale * index;
            }
            
            static Texture2D decideSegmentTexture(int index)
            {
                // By default, segments are heads.
                Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/NPCs/AdultEidolonWyrm/AdultEidolonWyrmHead").Value;

                // After the head is drawn, use body segments.
                if (index >= 1)
                {
                    string bodyTexturePath = "CalamityMod/NPCs/AdultEidolonWyrm/AdultEidolonWyrmBody";
                    if (index % 2 == 1)
                        bodyTexturePath += "Alt";

                    texture = ModContent.Request<Texture2D>(bodyTexturePath).Value;
                }
                
                // The last segment should be a tail.
                if (index == SegmentCount)
                    texture = ModContent.Request<Texture2D>("CalamityMod/NPCs/AdultEidolonWyrm/AdultEidolonWyrmTail").Value;

                return texture;
            }
            
            // Draw shadow afterimages. This cannot be performed in the main loop due to layering problems, specifically with new segments overlapping the afterimages.
            for (int i = 0; i < SegmentCount + 1; i++)
            {
                Texture2D texture = decideSegmentTexture(i);
                Color color = DarkForm ? new(103, 84, 164, 0) : new(244, 207, 112, 0);
                Vector2 drawPosition = decideDrawPosition(i);

                for (int j = 0; j < 3; j++)
                {
                    Vector2 drawOffset = Projectile.rotation.ToRotationVector2() * Projectile.scale * new Vector2(10f, 5f);
                    Main.EntitySpriteDraw(texture, drawPosition + drawOffset, null, color, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, 0, 0);
                }
            }

            // Draw the main body.
            for (int i = 0; i < SegmentCount + 1; i++)
            {
                Texture2D texture = decideSegmentTexture(i);
                Color color = Projectile.GetAlpha(Color.White);
                Main.EntitySpriteDraw(texture, decideDrawPosition(i), null, color, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, 0, 0);
            }

            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < SegmentCount + 1; i++)
            {
                Vector2 segmentCenter = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * OffsetPerSegment * Projectile.scale * i;
                if (Utils.CenteredRectangle(segmentCenter, Projectile.Size).Intersects(targetHitbox))
                    return true;
            }

            return false;
        }
    }
}
