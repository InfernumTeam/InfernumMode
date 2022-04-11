using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class AresPulseBlast : ModProjectile
    {
        public bool ShouldExplodeDiagonally => Projectile.ai[0] == 1f;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Exopulse Energy Burst");
        }

        public override void SetDefaults()
        {
            Projectile.width = 128;
            Projectile.height = 128;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.Opacity = 0f;
            Projectile.timeLeft = 80;
            CooldownSlot = 1;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.localAI[0]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.ReadSingle();
        }

        public override void AI()
        {
            Projectile.velocity *= 1.065f;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.1f, 0f, 1f);
        }

        public override bool CanHitPlayer(Player target) => Projectile.Opacity == 1f;

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.SetBlendState(BlendState.Additive);

            Texture2D texture = Main.projectileTexture[Projectile.type];
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 scale = new Vector2(Projectile.velocity.Length() * 0.12f + 1f, 1f) / texture.Size() * Projectile.Size;
            Color color = Projectile.GetAlpha(Color.Lerp(Color.Violet, Color.White, 0.45f)) * 0.45f;

            for (int i = 0; i < 6; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 1.6f;
                spriteBatch.Draw(texture, drawPosition + drawOffset, null, color, Projectile.rotation, origin, scale, SpriteEffects.None, 0f);
            }
            spriteBatch.Draw(texture, drawPosition, null, color, Projectile.rotation, origin, scale, SpriteEffects.None, 0f);

            spriteBatch.ResetBlendState();
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(Projectile.Center, Projectile.Size.Length() / 2.6f, targetHitbox);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}
