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
        public bool ShouldExplodeDiagonally => projectile.ai[0] == 1f;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Exopulse Energy Burst");
        }

        public override void SetDefaults()
        {
            projectile.width = 128;
            projectile.height = 128;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.Opacity = 0f;
            projectile.timeLeft = 80;
            cooldownSlot = 1;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(projectile.localAI[0]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            projectile.localAI[0] = reader.ReadSingle();
        }

        public override void AI()
        {
            projectile.velocity *= 1.065f;
            projectile.rotation = projectile.velocity.ToRotation();
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.1f, 0f, 1f);
        }

        public override bool CanHitPlayer(Player target) => projectile.Opacity == 1f;

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.SetBlendState(BlendState.Additive);

            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Vector2 scale = new Vector2(projectile.velocity.Length() * 0.12f + 1f, 1f) / texture.Size() * projectile.Size;
            Color color = projectile.GetAlpha(Color.Lerp(Color.Violet, Color.White, 0.45f)) * 0.45f;

            for (int i = 0; i < 6; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 1.6f;
                spriteBatch.Draw(texture, drawPosition + drawOffset, null, color, projectile.rotation, origin, scale, SpriteEffects.None, 0f);
            }
            spriteBatch.Draw(texture, drawPosition, null, color, projectile.rotation, origin, scale, SpriteEffects.None, 0f);

            spriteBatch.ResetBlendState();
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(projectile.Center, projectile.Size.Length() / 2.6f, targetHitbox);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = projectile;
        }
    }
}
