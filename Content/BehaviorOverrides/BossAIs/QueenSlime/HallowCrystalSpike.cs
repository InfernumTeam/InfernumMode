using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.ScreenEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.QueenSlime
{
    public class HallowCrystalSpike : ModProjectile, IScreenCullDrawer
    {
        public bool HasHitTile
        {
            get => Projectile.localAI[0] == 1f;
            set => Projectile.localAI[0] = value.ToInt();
        }

        public ref float Time => ref Projectile.ai[0];

        public ref float CurrentLength => ref Projectile.ai[1];

        public static float MaxLength => 3200f;

        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.CrystalVileShardHead}";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Crystal Spike");
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 50;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Time++;

            // Ensure that the velocity is normalized.
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY);

            // Decide rotation
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;

            // Make the spike extend outward until it hits a tile.
            if (!HasHitTile)
            {
                float stretchInterpolant = Utils.Remap(CurrentLength, 600f, 1600f, 0.018f, 0.055f);
                float nextLength = Lerp(CurrentLength, MaxLength, stretchInterpolant);
                Vector2 previousPosition = Projectile.Center + Projectile.velocity * CurrentLength;
                Vector2 nextPosition = Projectile.Center + Projectile.velocity * nextLength;

                // Get raycast distance information and determine whether it suggests that there's more distance to travel.
                float[] sampleDistances = new float[18];
                Collision.LaserScan(previousPosition, Projectile.velocity, Projectile.width, MaxLength, sampleDistances);
                float distanceToHit = sampleDistances.Average();
                bool canReachNextPosition = distanceToHit >= previousPosition.Distance(nextPosition) || CurrentLength <= 200f;

                // Adjust the length to the ideal there is no impeding tile.
                if (canReachNextPosition)
                    CurrentLength = nextLength;

                // Otherwise move forward just enough to meet the tile.
                else if (Math.Abs(distanceToHit) >= 0.01f)
                {
                    CurrentLength += distanceToHit;
                    HasHitTile = true;
                    Projectile.netUpdate = true;

                    // Create impact effects.
                    SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceBurnSound, Projectile.Center);
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            // Explode into a barrage of crystals.
            SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceCrystalPillarShatterSound, Projectile.Center + Projectile.velocity * CurrentLength * 0.5f);
            if (Main.netMode == NetmodeID.Server)
                return;

            Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 15f;
            ScreenEffectSystem.SetBlurEffect(Main.LocalPlayer.Center - Vector2.UnitY * 300f, 1.45f, 30);

            for (float k = 0; k < CurrentLength; k += Main.rand.NextFloat(9f, 16f))
            {
                Vector2 crystalSpawnPosition = Projectile.Center + Projectile.velocity * k + Main.rand.NextVector2Circular(10f, 10f);
                Vector2 crystalVelocity = Projectile.velocity.RotatedByRandom(1.06f) * Main.rand.NextFloat(4f, 10f);

                if (!Collision.SolidCollision(crystalSpawnPosition, 1, 1))
                    Gore.NewGore(Projectile.GetSource_Death(), crystalSpawnPosition, crystalVelocity, Mod.Find<ModGore>($"ProvidenceDoor{Main.rand.Next(1, 3)}").Type, 0.4f);
            }

            for (float k = 0; k < CurrentLength; k += Main.rand.NextFloat(9f, 16f))
            {
                Vector2 crystalShardSpawnPosition = Projectile.Center + Projectile.velocity * k + Main.rand.NextVector2Circular(6f, 6f);
                Vector2 shardVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3.6f, 13.6f);
                Dust shard = Dust.NewDustPerfect(crystalShardSpawnPosition, Main.rand.NextBool() ? 255 : 187, shardVelocity);
                shard.noGravity = Main.rand.NextBool();
                shard.scale = Main.rand.NextFloat(1.3f, 1.925f);
                shard.velocity.Y -= 5f;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity * CurrentLength;
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, Projectile.width, ref _);
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void CullDraw(SpriteBatch spriteBatch)
        {
            Texture2D crystalSegment = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 start = Projectile.Center - Main.screenPosition;
            Vector2 end = start + Projectile.velocity * CurrentLength;
            Vector2 drawPosition = end;
            Vector2 scale = Vector2.One * Projectile.width / crystalSegment.Width;

            float glowOpacity = Utils.GetLerpValue(30f, 50f, Time, true);

            for (int i = 0; i < 1000; i++)
            {
                drawPosition += (start - end).SafeNormalize(Vector2.UnitY) * crystalSegment.Height * scale * 0.8f;
                if (drawPosition.WithinRange(start, 35f))
                    break;

                if (glowOpacity > 0f)
                {
                    for (int j = 0; j < 12; j++)
                    {
                        Vector2 offset = (Tau * j / 12f).ToRotationVector2() * (4f * glowOpacity);
                        spriteBatch.Draw(crystalSegment, drawPosition + offset, null, Color.White with { A = 0 } * Projectile.Opacity * glowOpacity, Projectile.rotation,
                            crystalSegment.Size() * new Vector2(0.5f, 1f), scale, 0, 0f);
                    }
                }

                spriteBatch.Draw(crystalSegment, drawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, crystalSegment.Size() * new Vector2(0.5f, 1f), scale, 0, 0f);
            }
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
