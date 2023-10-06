using CalamityMod;
using CalamityMod.Particles;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Worldgen;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Cryogen
{
    public class IcePillar : ModProjectile
    {
        public float CurrentHeight;

        public ref float MaxPillarHeight => ref Projectile.ai[0];

        public ref float Time => ref Projectile.ai[1];

        public const float StartingHeight = 30f;

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Ice Pillar");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 28;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(CurrentHeight);
            writer.Write(Projectile.rotation);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            CurrentHeight = reader.ReadSingle();
            Projectile.rotation = reader.ReadSingle();
        }

        public override void AI()
        {
            Time++;

            Projectile.MaxUpdates = Time < 60f ? 1 : 2;

            // Fade in at the beginning of the projectile's life.
            if (Time < 60f)
                Projectile.Opacity = Lerp(Projectile.Opacity, 1f, 0.35f);

            // Stop doing damage at the end of the projectile's life.
            else if (Projectile.timeLeft < 40f)
                Projectile.damage = 0;

            // Initialize the pillar.
            if (Main.netMode != NetmodeID.MultiplayerClient && MaxPillarHeight == 0f)
                InitializePillarProperties();

            // Quickly rise.
            if (Main.netMode != NetmodeID.MultiplayerClient && Time >= 60f && Time < 75f)
            {
                CurrentHeight = Lerp(StartingHeight, MaxPillarHeight, Utils.GetLerpValue(60f, 75f, Time, true));
                if (Time % 6 == 0)
                    Projectile.netUpdate = true;
            }

            // Play a sound when rising.
            if (Time == 70)
            {
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                SoundEngine.PlaySound(SoundID.Item45, target.Center);
            }
        }

        public void InitializePillarProperties()
        {
            WorldUtils.Find(Projectile.Top.ToTileCoordinates(), Searches.Chain(new Searches.Down(6000), new GenCondition[]
            {
                new Conditions.IsSolid(),
                new CustomTileConditions.ActiveAndNotActuated(),
                new CustomTileConditions.NotPlatform()
            }), out Point newBottom);

            bool isHalfTile = CalamityUtils.ParanoidTileRetrieval(newBottom.X, newBottom.Y - 1).IsHalfBlock;
            Projectile.Bottom = newBottom.ToWorldCoordinates(8, isHalfTile ? 8 : 0);

            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            MaxPillarHeight = MathF.Max(0f, Projectile.Top.Y - target.Top.Y) + StartingHeight + 100f + Math.Abs(target.velocity.Y * 15f);

            CurrentHeight = StartingHeight;

            if (!Collision.CanHit(Projectile.Bottom - Vector2.UnitY * 10f, 2, 2, Projectile.Bottom - Vector2.UnitY * 32f, 2, 2))
                Projectile.Kill();

            Projectile.netUpdate = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tipTexture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 aimDirection = Vector2.UnitY.RotatedBy(Projectile.rotation);
            if (Time < 60f)
            {
                float telegraphOpacity = Pow(CalamityUtils.Convert01To010(Time / 60f), 0.6f);
                float telegraphLineWidth = telegraphOpacity * 6f;
                if (telegraphLineWidth > 5f)
                    telegraphLineWidth = 5f;
                Main.spriteBatch.DrawLineBetter(Projectile.Top + aimDirection * 10f, Projectile.Top + aimDirection * -MaxPillarHeight, Color.LightCyan * telegraphOpacity, telegraphLineWidth);
            }

            float tipBottom = 0f;
            Vector2 scale = new(Projectile.scale, 1f);

            DrawPillar(scale, aimDirection, ref tipBottom);

            Vector2 tipDrawPosition = Projectile.Bottom - aimDirection * (tipBottom + 4f) - Main.screenPosition;
            Main.spriteBatch.Draw(tipTexture, tipDrawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, tipTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            return false;
        }

        public void DrawPillar(Vector2 scale, Vector2 aimDirection, ref float tipBottom)
        {
            Texture2D pillarBodyPiece = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Cryogen/IcePillarPiece").Value;

            for (int i = pillarBodyPiece.Height; i < CurrentHeight + pillarBodyPiece.Height; i += pillarBodyPiece.Height)
            {
                Vector2 drawPosition = Projectile.Bottom - aimDirection * i - Main.screenPosition;
                Main.spriteBatch.Draw(pillarBodyPiece, drawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, pillarBodyPiece.Size() * new Vector2(0.5f, 0f), scale, SpriteEffects.None, 0f);
                tipBottom = i;
            }
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Time >= 70f;

        public override void OnKill(int timeLeft)
        {
            // Play a break sound.
            SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceCrystalPillarShatterSound with { Pitch = 0.4f }, Projectile.Center);

            // Emit a bunch of ice cloud particles and shatter particles.
            Vector2 aimDirection = Vector2.UnitY.RotatedBy(Projectile.rotation);
            for (float offset = 0f; offset < CurrentHeight; offset += Main.rand.NextFloat(4f, 11f))
            {
                Vector2 crystalShardSpawnPosition = Projectile.Center - aimDirection * offset + Main.rand.NextVector2Circular(6f, 6f);
                Vector2 shardVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3.6f, 13.6f);

                Dust shard = Dust.NewDustPerfect(crystalShardSpawnPosition, 68, shardVelocity);
                shard.noGravity = Main.rand.NextBool();
                shard.scale = Main.rand.NextFloat(0.9f, 1.4f);
                shard.velocity.Y -= 5f;

                // Create ice mist.
                if (Main.rand.NextBool())
                {
                    MediumMistParticle mist = new(crystalShardSpawnPosition + Main.rand.NextVector2Circular(12f, 12f), Vector2.Zero, new Color(172, 238, 255), new Color(145, 170, 188), Main.rand.NextFloat(0.5f, 1.5f), 245 - Main.rand.Next(50), 0.02f)
                    {
                        Velocity = Main.rand.NextVector2Circular(7.5f, 7.5f)
                    };
                    GeneralParticleHandler.SpawnParticle(mist);
                }
            }

            // Release some ice spikes that redirect and accelerate towards the target.
            int spikeCount = (int)Lerp(1f, 4f, Utils.GetLerpValue(100f, 960f, CurrentHeight, true));
            for (int i = 0; i < spikeCount; i++)
            {
                Vector2 icicleSpawnPosition = Projectile.Bottom - aimDirection * CurrentHeight * i / spikeCount;
                icicleSpawnPosition -= aimDirection * Main.rand.NextFloatDirection() * 20f + Main.rand.NextVector2Circular(8f, 8f);
                Vector2 icicleShootVelocity = Main.rand.NextVector2Unit() * 4f;

                Utilities.NewProjectileBetter(icicleSpawnPosition, icicleShootVelocity, ModContent.ProjectileType<AimedIcicleSpike>(), CryogenBehaviorOverride.IcicleSpikeDamage, 0f);
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            Color color = Main.dayTime ? new Color(50, 50, 255, 255 - Projectile.alpha) : new Color(255, 255, 255, Projectile.alpha);
            return color * Utils.GetLerpValue(15f, 75f, Time, true);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            Vector2 start = Projectile.Bottom;
            Vector2 end = Projectile.Bottom - Vector2.UnitY.RotatedBy(Projectile.rotation) * CurrentHeight;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, Projectile.width * Projectile.scale, ref _);
        }
    }
}

