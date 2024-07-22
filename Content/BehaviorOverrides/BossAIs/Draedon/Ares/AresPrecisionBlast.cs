using CalamityMod;
using CalamityMod.NPCs.ExoMechs.Ares;
using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class AresPrecisionBlast : ModProjectile
    {
        public NPC ThingToAttachTo => Main.npc.IndexInRange((int)Projectile.ai[0]) ? Main.npc[(int)Projectile.ai[0]] : null;

        public Color BlastColor
        {
            get
            {
                int cannonID = ThingToAttachTo.type;
                if (cannonID == ModContent.NPCType<AresLaserCannon>())
                    return Color.Red;
                if (cannonID == ModContent.NPCType<AresTeslaCannon>())
                    return Color.Lerp(Color.Cyan, Color.White, 0.32f);
                if (cannonID == ModContent.NPCType<AresPlasmaFlamethrower>())
                    return Color.ForestGreen;
                if (cannonID == ModContent.NPCType<AresPulseCannon>())
                    return Color.MediumVioletRed;

                return Color.Red;
            }
        }

        public const int Lifetime = 30;

        public const float LaserLength = 2300f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Precision Blast");

        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.MaxUpdates = 4;
            Projectile.Calamity().DealsDefenseDamage = true;
            
        }

        public override void AI()
        {
            Projectile.scale = LumUtils.Convert01To010(Projectile.timeLeft / (float)Lifetime) * 1.2f;
            if (Projectile.scale > 1f)
                Projectile.scale = 1f;
            Projectile.hide = Projectile.timeLeft >= 27;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, Projectile.width * Projectile.scale, ref _);
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;

            // Draw the telegraph line.
            Vector2 start = Projectile.Center - Main.screenPosition;
            Texture2D line = InfernumTextureRegistry.BloomLine.Value;

            Vector2 beamOrigin = new(line.Width / 2f, line.Height);
            Vector2 beamScale = new(Projectile.scale * Projectile.width / line.Width * 1.5f, LaserLength / line.Height);
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.spriteBatch.Draw(line, start, null, Color.Lerp(BlastColor, Color.DarkGray, 0.27f), Projectile.rotation, beamOrigin, beamScale, 0, 0f);
            Main.spriteBatch.Draw(line, start, null, BlastColor, Projectile.rotation, beamOrigin, beamScale * new Vector2(0.7f, 1f), 0, 0f);
            Main.spriteBatch.Draw(line, start, null, Color.White, Projectile.rotation, beamOrigin, beamScale * new Vector2(0.3f, 1f), 0, 0f);
            // Draw the energy focus at the start.
            Texture2D energyFocusTexture = InfernumTextureRegistry.LaserCircle.Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Main.spriteBatch.Draw(energyFocusTexture, drawPosition, null, Color.White * Projectile.scale, Projectile.rotation, energyFocusTexture.Size() * 0.5f, 0.7f, 0, 0f);
            Main.spriteBatch.ResetBlendState();
            return false;
        }
    }
}
