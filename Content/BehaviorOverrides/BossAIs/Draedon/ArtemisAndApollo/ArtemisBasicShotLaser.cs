using CalamityMod;
using CalamityMod.DataStructures;
using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.ArtemisAndApollo
{
    public class ArtemisBasicShotLaser : ModProjectile, IAdditiveDrawer
    {
        public NPC ThingToAttachTo => Main.npc.IndexInRange((int)Projectile.ai[0]) ? Main.npc[(int)Projectile.ai[0]] : null;

        public const int Lifetime = 30;

        public const float LaserLength = 2300f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Exo Flame Laser");
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.hide = true;
            Projectile.MaxUpdates = 5;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.scale = CalamityUtils.Convert01To010(Projectile.timeLeft / (float)Lifetime) * 1.2f;
            if (Projectile.scale > 1f)
                Projectile.scale = 1f;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // Stick to Artemis.
            float positionOffset = ExoMechManagement.ExoTwinsAreInSecondPhase ? 102f : 70f;
            Projectile.Center = ThingToAttachTo.Center + (ThingToAttachTo.rotation - MathHelper.PiOver2).ToRotationVector2() * positionOffset;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, Projectile.width * Projectile.scale, ref _);
        }

        public void AdditiveDraw(SpriteBatch spriteBatch)
        {
            // Draw the telegraph line.
            Vector2 start = Projectile.Center - Main.screenPosition;
            Texture2D line = InfernumTextureRegistry.BloomLine.Value;

            Vector2 beamOrigin = new(line.Width / 2f, line.Height);
            Vector2 beamScale = new(Projectile.scale * Projectile.width / line.Width * 1.5f, LaserLength / line.Height);
            Main.spriteBatch.Draw(line, start, null, Color.Orange, Projectile.rotation, beamOrigin, beamScale, 0, 0f);
            Main.spriteBatch.Draw(line, start, null, Color.Red, Projectile.rotation, beamOrigin, beamScale * new Vector2(0.7f, 1f), 0, 0f);
            Main.spriteBatch.Draw(line, start, null, Color.White, Projectile.rotation, beamOrigin, beamScale * new Vector2(0.3f, 1f), 0, 0f);

            // Draw the energy focus at the start.
            Texture2D energyFocusTexture = InfernumTextureRegistry.LaserCircle.Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            spriteBatch.Draw(energyFocusTexture, drawPosition, null, Color.White * Projectile.scale, Projectile.rotation, energyFocusTexture.Size() * 0.5f, 0.7f, 0, 0f);
        }
    }
}
