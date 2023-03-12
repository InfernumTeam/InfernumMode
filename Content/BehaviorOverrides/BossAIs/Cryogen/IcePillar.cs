using CalamityMod;
using InfernumMode.Common;
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
        public ref float MaxPillarHeight => ref Projectile.ai[0];
        public ref float Time => ref Projectile.ai[1];
        public float CurrentHeight = 0f;
        public const float StartingHeight = 30f;
        public override void SetStaticDefaults() => DisplayName.SetDefault("Ice Pillar");

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

            Projectile.extraUpdates = Time < 60f ? 0 : 1;

            // Fade in at the beginning of the projectile's life.
            if (Time < 60f)
                Projectile.Opacity = MathHelper.Lerp(Projectile.Opacity, 1f, 0.35f);

            // Stop doing damage at the end of the projectile's life.
            else if (Projectile.timeLeft < 40f)
                Projectile.damage = 0;

            // Initialize the pillar.
            if (Main.netMode != NetmodeID.MultiplayerClient && MaxPillarHeight == 0f)
                InitializePillarProperties();

            // Quickly rise.
            if (Main.netMode != NetmodeID.MultiplayerClient && Time >= 60f && Time < 75f)
            {
                CurrentHeight = MathHelper.Lerp(StartingHeight, MaxPillarHeight, Utils.GetLerpValue(60f, 75f, Time, true));
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
            MaxPillarHeight = MathHelper.Max(0f, Projectile.Top.Y - target.Top.Y) + StartingHeight + 100f + Math.Abs(target.velocity.Y * 15f);

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
                float telegraphLineWidth = (float)Math.Sin(Time / 60f * MathHelper.Pi) * 5f;
                if (telegraphLineWidth > 3f)
                    telegraphLineWidth = 3f;
                Main.spriteBatch.DrawLineBetter(Projectile.Top + aimDirection * 10f, Projectile.Top + aimDirection * -MaxPillarHeight, Color.LightCyan, telegraphLineWidth);
            }

            float tipBottom = 0f;
            Vector2 scale = new(Projectile.scale, 1f);

            DrawPillar(Main.spriteBatch, scale, aimDirection, ref tipBottom);

            Vector2 tipDrawPosition = Projectile.Bottom - aimDirection * (tipBottom + 4f) - Main.screenPosition;
            Main.spriteBatch.Draw(tipTexture, tipDrawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, tipTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            return false;
        }

        public void DrawPillar(SpriteBatch spriteBatch, Vector2 scale, Vector2 aimDirection, ref float tipBottom)
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

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item51, Projectile.Center);

            int spikeCount = (int)MathHelper.Lerp(1f, 4f, Utils.GetLerpValue(100f, 960f, CurrentHeight, true));
            Vector2 aimDirection = Vector2.UnitY.RotatedBy(Projectile.rotation);
            for (int i = 0; i < spikeCount; i++)
            {
                Vector2 icicleSpawnPosition = Projectile.Bottom - aimDirection * CurrentHeight * i / spikeCount;
                icicleSpawnPosition -= aimDirection * Main.rand.NextFloatDirection() * 20f + Main.rand.NextVector2Circular(8f, 8f);
                Vector2 icicleShootVelocity = Main.rand.NextVector2Unit() * 4f;
                Utilities.NewProjectileBetter(icicleSpawnPosition, icicleShootVelocity, ModContent.ProjectileType<AimedIcicleSpike>(), 145, 0f);
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return Main.dayTime ? new Color(50, 50, 255, 255 - Projectile.alpha) : new Color(255, 255, 255, Projectile.alpha);
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

