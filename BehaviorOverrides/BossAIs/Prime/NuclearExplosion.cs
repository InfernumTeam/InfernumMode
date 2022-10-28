using CalamityMod;
using CalamityMod.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Prime
{
    public class NuclearExplosion : ModProjectile, IAdditiveDrawer
    {
        public const int Lifetime = 180;

        public const float BaseScale = 0.15f;

        public const float ScaleExpandRate = 0.06f;

        public const float RadiusScaleFactor = 135f;

        public override void SetStaticDefaults() => DisplayName.SetDefault("Explosion");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.extraUpdates = 1;
            Projectile.penetrate = -1;
            Projectile.scale = BaseScale;
            Projectile.Calamity().DealsDefenseDamage = true;
        }

        public override void AI()
        {
            Projectile.scale += ScaleExpandRate;
            Projectile.Opacity = Utils.GetLerpValue(0f, 50f, Projectile.timeLeft, true);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (Projectile.velocity.Length() < 18f)
                Projectile.velocity *= 1.02f;

            if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.Opacity > 0.74f)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 spawnPosition = Projectile.Center + Main.rand.NextVector2Circular(35f, 35f);
                    Vector2 smokeVelocity = -Vector2.UnitY.RotatedByRandom(2.16f) * Main.rand.NextFloat(6f, 29f);
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), spawnPosition, smokeVelocity, ModContent.ProjectileType<NukeSmoke>(), 0, 0f);
                }
            }

            Lighting.AddLight(Projectile.Center, Color.Red.ToVector3());
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void AdditiveDraw(SpriteBatch spriteBatch)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Color explosionColor = Color.OrangeRed * Projectile.Opacity * 0.65f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            for (int i = 0; i < 2; i++)
                Main.spriteBatch.Draw(texture, drawPosition, null, explosionColor, 0f, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Utilities.CircularCollision(Projectile.Center, targetHitbox, Projectile.scale * RadiusScaleFactor);
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Projectile.Opacity > 0.45f;
    }
}
