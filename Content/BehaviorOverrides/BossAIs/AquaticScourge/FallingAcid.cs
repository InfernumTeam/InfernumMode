using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AquaticScourge
{
    public class FallingAcid : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Acid");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 480;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 35f, Time, true) * Utils.GetLerpValue(0f, 56f, Projectile.timeLeft, true);

            // Fall downward.
            Projectile.velocity.X *= 0.987f;
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + 0.4f, -40f, 13f);
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.position + Projectile.Size * 0.5f - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            Color backAfterimageColor = Projectile.GetAlpha(new Color(85, 224, 60, 0) * 0.5f);
            for (int i = 0; i < 8; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 4f;
                Main.spriteBatch.Draw(texture, drawPosition + drawOffset, null, backAfterimageColor, Projectile.rotation, origin, Projectile.scale, 0, 0f);
            }
            Utilities.DrawAfterimagesCentered(Projectile, new Color(117, 95, 133, 184) * Projectile.Opacity, ProjectileID.Sets.TrailingMode[Projectile.type], 2);

            return false;
        }
    }
}
