using CalamityMod.Dusts;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Providence
{
    public class GroundCrystalSpike : ModProjectile
    {
        public bool SpikesShouldExtendOutward;

        public ref float SpikeReach => ref Projectile.ai[0];

        public ref float SpikeDirection => ref Projectile.ai[1];

        public override void SetStaticDefaults() => DisplayName.SetDefault("Crystal Spike");

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 900000;
            Projectile.netImportant = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(SpikesShouldExtendOutward);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            SpikesShouldExtendOutward = reader.ReadBoolean();
        }

        public override void AI()
        {
            if (CalamityGlobalNPC.holyBoss == -1)
            {
                Projectile.Kill();
                return;
            }

            if (SpikesShouldExtendOutward)
                SpikeReach = MathHelper.Clamp(SpikeReach + 8f, 0f, 50f);

            // Create a visual warning effect on the ground before releasing spikes so that the player knows to avoid it.
            else
            {
                if (Main.rand.NextBool(4))
                {
                    Dust holyFire = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f), (int)CalamityDusts.ProfanedFire);
                    holyFire.velocity = SpikeDirection.ToRotationVector2().RotatedByRandom(0.64f) * Main.rand.NextFloat(2f, 6f);
                    holyFire.noGravity = true;
                    holyFire.scale *= 1.1f;
                    holyFire.fadeIn = 0.6f;
                }

                SpikeReach = 0f;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (SpikeReach <= 0f)
                return false;

            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + SpikeDirection.ToRotationVector2() * SpikeReach, 4f, ref _);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Draw the spike.
            Texture2D spikeTipTexture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 spikeTip = Projectile.Center + Vector2.UnitY * SpikeDirection * SpikeReach;
            float frameHeight = Vector2.Distance(Projectile.Center, spikeTip) - Projectile.velocity.Length();
            float frameTop = TextureAssets.Chain17.Value.Height - frameHeight;
            if (frameHeight > 0f)
            {
                float spikeRotation = SpikeDirection + MathHelper.PiOver2;
                Rectangle spikeFrame = new(0, (int)frameTop, TextureAssets.Chain17.Value.Width, (int)frameHeight);
                Main.spriteBatch.Draw(TextureAssets.Chain17.Value, spikeTip - Main.screenPosition, spikeFrame, Color.White, spikeRotation, new Vector2(TextureAssets.Chain17.Value.Width / 2f, 0f), 1f, 0, 0f);
                Main.spriteBatch.Draw(spikeTipTexture, spikeTip - Main.screenPosition, null, Color.White, spikeRotation + MathHelper.Pi, new Vector2(spikeTipTexture.Width / 2f, 0f), 1f, 0, 0f);
            }
            return false;
        }
    }
}
