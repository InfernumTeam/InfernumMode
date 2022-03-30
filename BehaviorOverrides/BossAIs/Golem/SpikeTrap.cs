using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Golem
{
    public class SpikeTrap : ModProjectile
    {
        public ref float SpikeReach => ref projectile.ai[0];

        public ref float SpikeDirection => ref projectile.ai[1];

        public override void SetStaticDefaults() => DisplayName.SetDefault("Spike Trap");

        public override void SetDefaults()
        {
            projectile.width = 16;
            projectile.height = 16;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.hostile = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 600;
        }

        public override void AI()
        {
            if (NPC.golemBoss == -1)
            {
                projectile.Kill();
                return;
            }

            NPC golem = Main.npc[NPC.golemBoss];

            // Die if the trap leaves the arena.
            if (!golem.Infernum().arenaRectangle.Intersects(projectile.Hitbox))
            {
                projectile.Kill();
                return;
            }

            // Play a sound to accomodate the release of the spear.
            if (projectile.timeLeft == 595)
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/SwiftSlice"), projectile.Center);

            SpikeReach = 840f;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), projectile.Center, projectile.Center + Vector2.UnitY * SpikeDirection * SpikeReach, 4f, ref _);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            // Draw the spike.
            Main.instance.LoadProjectile(ProjectileID.SpearTrap);
            Texture2D spikeTipTexture = Main.projectileTexture[ProjectileID.SpearTrap];
            Vector2 spikeTip = projectile.Center + Vector2.UnitY * SpikeDirection * SpikeReach;
            float frameHeight = Vector2.Distance(projectile.Center, spikeTip) - projectile.velocity.Length();
            float frameTop = Main.chain17Texture.Height - frameHeight;
            if (frameHeight > 0f)
            {
                float spikeRotation = SpikeDirection == -1f ? 0f : MathHelper.Pi;
                Rectangle spikeFrame = new Rectangle(0, (int)frameTop, Main.chain17Texture.Width, (int)frameHeight);
                spriteBatch.Draw(Main.chain17Texture, spikeTip - Main.screenPosition, spikeFrame, Color.OrangeRed, spikeRotation, new Vector2(Main.chain17Texture.Width / 2f, 0f), 1f, 0, 0f);
                spriteBatch.Draw(spikeTipTexture, spikeTip - Main.screenPosition, null, Color.OrangeRed, spikeRotation + MathHelper.Pi, new Vector2(spikeTipTexture.Width / 2f, 0f), 1f, 0, 0f);
            }

            Texture2D texture = Main.projectileTexture[projectile.type];
            Rectangle rectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            Vector2 origin = rectangle.Size() * 0.5f;
            Color drawColor = projectile.GetAlpha(lightColor);

            spriteBatch.Draw(texture, projectile.Center - Main.screenPosition, rectangle, drawColor, projectile.rotation, origin, projectile.scale, 0, 0f);
            return false;
        }
    }
}
