using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Plantera
{
    public class NettlevineArenaSeparator : ModProjectile
    {
        public Vector2 StartingPosition
        {
            get
            {
                if (Projectile.ai[0] == 0f && Projectile.ai[1] == 0f)
                {
                    Projectile.ai[0] = Projectile.Center.X;
                    Projectile.ai[1] = Projectile.Center.Y;
                    Projectile.netUpdate = true;
                }
                return new Vector2(Projectile.ai[0], Projectile.ai[1]);
            }
        }
        public override void SetStaticDefaults() => DisplayName.SetDefault("Nettlevine");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 10;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 660;
            Projectile.penetrate = -1;
            Projectile.Calamity().DealsDefenseDamage = true;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            if (Projectile.timeLeft < 480)
                Projectile.velocity *= 0.985f;
            else
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.Opacity = Utils.GetLerpValue(660f, 620f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 30f, Projectile.timeLeft, true);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tipTexture = TextureAssets.Projectile[Projectile.type].Value;
            Texture2D body1Texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Plantera/NettlevineArenaSeparatorBody1").Value;
            Texture2D body2Texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Plantera/NettlevineArenaSeparatorBody2").Value;
            Vector2 bodyOrigin = body1Texture.Size() * new Vector2(0.5f, 1f);
            Vector2 tipOrigin = tipTexture.Size() * new Vector2(0.5f, 1f);
            Vector2 currentDrawPosition = StartingPosition;
            Color drawColor = Projectile.GetAlpha(Color.White);

            int fuck = 0;
            while (!Projectile.WithinRange(currentDrawPosition, 36f))
            {
                Texture2D textureToUse = fuck % 2 == 0 ? body1Texture : body2Texture;
                Main.spriteBatch.Draw(textureToUse, currentDrawPosition - Main.screenPosition, null, drawColor, Projectile.rotation, bodyOrigin, Projectile.scale, SpriteEffects.None, 0f);
                currentDrawPosition += (Projectile.rotation - MathHelper.PiOver2).ToRotationVector2() * body1Texture.Height;
                fuck++;
            }

            Main.spriteBatch.Draw(tipTexture, currentDrawPosition - Main.screenPosition, null, drawColor, Projectile.rotation, tipOrigin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 start = StartingPosition;
            Vector2 end = Projectile.Center;
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 8f, ref _);
        }

        public override bool? CanDamage() => !Projectile.WithinRange(StartingPosition, 720f);

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;
    }
}
