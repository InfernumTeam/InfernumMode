using CalamityMod.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Golem
{
    public class SpikeTrap : ModProjectile
    {
        public ref float SpikeReach => ref Projectile.ai[0];

        public ref float SpikeDirection => ref Projectile.ai[1];

        public override void SetStaticDefaults() => DisplayName.SetDefault("Spike Trap");

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
        }

        public override void AI()
        {
            if (NPC.golemBoss == -1)
            {
                Projectile.Kill();
                return;
            }

            NPC golem = Main.npc[NPC.golemBoss];

            // Die if the trap leaves the arena.
            if (!golem.Infernum().Arena.Intersects(Projectile.Hitbox))
            {
                Projectile.Kill();
                return;
            }

            // Play a sound to accomodate the release of the spear.
            if (Projectile.timeLeft == 595)
                SoundEngine.PlaySound(CommonCalamitySounds.SwiftSliceSound, Projectile.Center);

            SpikeReach = 840f;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + Vector2.UnitY * SpikeDirection * SpikeReach, 4f, ref _);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Draw the spike.
            Main.instance.LoadProjectile(ProjectileID.SpearTrap);
            Texture2D spikeTipTexture = TextureAssets.Projectile[ProjectileID.SpearTrap].Value;
            Vector2 spikeTip = Projectile.Center + Vector2.UnitY * SpikeDirection * SpikeReach;
            float frameHeight = Vector2.Distance(Projectile.Center, spikeTip) - Projectile.velocity.Length();
            float frameTop = TextureAssets.Chain17.Value.Height - frameHeight;

            Color spikeColor = new(1f, 1f, 0f, 0f);
            if (frameHeight > 0f)
            {
                float spikeRotation = SpikeDirection == -1f ? 0f : MathHelper.Pi;
                Rectangle spikeFrame = new(0, (int)frameTop, TextureAssets.Chain17.Value.Width, (int)frameHeight);
                Main.spriteBatch.Draw(TextureAssets.Chain17.Value, spikeTip - Main.screenPosition, spikeFrame, spikeColor, spikeRotation, new Vector2(TextureAssets.Chain17.Value.Width / 2f, 0f), 1f, 0, 0f);
                Main.spriteBatch.Draw(spikeTipTexture, spikeTip - Main.screenPosition, null, spikeColor, spikeRotation + MathHelper.Pi, new Vector2(spikeTipTexture.Width / 2f, 0f), 1f, 0, 0f);
            }

            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Rectangle rectangle = new(0, 0, texture.Width, texture.Height);
            Vector2 origin = rectangle.Size() * 0.5f;
            Color drawColor = Projectile.GetAlpha(lightColor);

            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, rectangle, drawColor, Projectile.rotation, origin, Projectile.scale, 0, 0f);
            return false;
        }
    }
}
