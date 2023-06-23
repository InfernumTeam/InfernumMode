using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.EyeOfCthulhu
{
    public class EoCTooth2 : ModProjectile
    {
        public Player Target => Main.player[(int)Projectile.ai[0]];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Tooth");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 22;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            if (Projectile.velocity.Y < 8f)
                Projectile.velocity.Y += 0.26f;
            Projectile.alpha = Utils.Clamp(Projectile.alpha - 72, 0, 255);

            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;

            for (int i = 0; i < 6; i++)
            {
                Vector2 drawOffset = (TwoPi * i / 6f).ToRotationVector2() * 4f;
                Main.spriteBatch.Draw(texture, drawPosition + drawOffset, null, Projectile.GetAlpha(Color.Red) * 0.65f, Projectile.rotation, origin, Projectile.scale, 0, 0f);
            }
            Main.spriteBatch.Draw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, 0, 0f);
            return false;
        }
    }
}
