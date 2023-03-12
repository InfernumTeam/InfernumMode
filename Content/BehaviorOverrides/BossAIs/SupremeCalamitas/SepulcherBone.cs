using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class SepulcherBone : ModProjectile
    {
        public float ExplosionRadius => Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Occult Bone");
            Main.projFrames[Projectile.type] = 2;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.Opacity = 0f;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.2f, 0f, 1f);

            if (Projectile.velocity.Length() < 34f)
                Projectile.velocity *= 1.03f;
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.frame = Main.rand.Next(Main.projFrames[Type]);
                Projectile.localAI[0] = 1f;
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Lighting.AddLight(Projectile.Center, Color.Red.ToVector3() * 0.5f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;

            for (int i = 0; i < 12; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * 6f;
                Main.spriteBatch.Draw(texture, drawPosition + drawOffset, frame, new Color(1f, 1f, 1f, 0f) * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor);
            return false;
        }
    }
}
