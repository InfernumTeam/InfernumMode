using CalamityMod;
using CalamityMod.DataStructures;
using InfernumMode.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.PlaguebringerGoliath
{
    public class PlagueNuclearExplosion : ModProjectile, IAdditiveDrawer
    {
        public override string Texture => "CalamityMod/Skies/XerocLight";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Explosion");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 180;
            Projectile.extraUpdates = 1;
            Projectile.scale = 0.15f;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.scale += 0.1f;
            Projectile.Opacity = Utils.GetLerpValue(300f, 265f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 50f, Projectile.timeLeft, true);
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;

            if (Projectile.velocity.Length() < 18f)
                Projectile.velocity *= 1.02f;

            Lighting.AddLight(Projectile.Center, Color.Red.ToVector3());
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void AdditiveDraw(SpriteBatch spriteBatch)
        {
            int drawCount = InfernumConfig.Instance.ReducedGraphicsConfig ? 1 : 3;
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Color explosionColor = Color.LawnGreen * Projectile.Opacity * 0.65f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            for (int i = 0; i < drawCount; i++)
                Main.spriteBatch.Draw(texture, drawPosition, null, explosionColor, 0f, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Utilities.CircularCollision(Projectile.Center, targetHitbox, Projectile.scale * 135f);
        }

        public override bool? CanDamage() => Projectile.Opacity > 0.45f;
    }
}
