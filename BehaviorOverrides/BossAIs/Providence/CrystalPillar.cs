using CalamityMod;
using InfernumMode.Miscellaneous;
using InfernumMode.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace InfernumMode.BehaviorOverrides.BossAIs.Providence
{
    public class CrystalPillar : ModProjectile
    {
        public float Time = 0f;

        public float CurrentHeight = 0f;

        public ref float MaxPillarHeight => ref Projectile.ai[1];

        public const int Lifetime = 180;

        public const float StartingHeight = 50f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crystal Spike");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 42;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.MaxUpdates = 2;
            Projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Time);
            writer.Write(CurrentHeight);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Time = reader.ReadSingle();
            CurrentHeight = reader.ReadSingle();
        }

        public override void AI()
        {
            Time++;

            if (Time < 60f)
                Projectile.Opacity = MathHelper.Lerp(Projectile.Opacity, 1f, 0.35f);
            else if (Projectile.timeLeft < 40f)
            {
                Projectile.damage = 0;
                Projectile.Opacity = MathHelper.Lerp(Projectile.Opacity, 0f, 0.25f);
            }

            // Initialize the pillar.
            if (MaxPillarHeight == 0f)
            {
                Point newBottom;
                if (Projectile.velocity.Y != 0f)
                {
                    WorldUtils.Find(new Vector2(Projectile.Top.X, Projectile.Top.Y - 160).ToTileCoordinates(), Searches.Chain(new Searches.Down(6000), new GenCondition[]
                    {
                        new Conditions.IsSolid(),
                        new CustomTileConditions.ActiveAndNotActuated()
                    }), out newBottom);
                    bool isHalfTile = CalamityUtils.ParanoidTileRetrieval(newBottom.X, newBottom.Y - 1).IsHalfBlock;
                    Projectile.Bottom = newBottom.ToWorldCoordinates(8, isHalfTile ? 8 : 0);
                    MaxPillarHeight = (WorldSaveSystem.ProvidenceArena.Bottom - WorldSaveSystem.ProvidenceArena.Top) * 16f;
                }
                else
                {
                    WorldUtils.Find(new Vector2(Projectile.Top.X - 160, Projectile.Top.Y).ToTileCoordinates(), Searches.Chain(new Searches.Right(6000), new GenCondition[]
                    {
                        new Conditions.IsSolid(),
                        new CustomTileConditions.ActiveAndNotActuated()
                    }), out newBottom);
                    bool isHalfTile = CalamityUtils.ParanoidTileRetrieval(newBottom.X - 1, newBottom.Y).IsHalfBlock;
                    Projectile.Bottom = newBottom.ToWorldCoordinates(isHalfTile ? 8 : 0, 8);
                    MaxPillarHeight = (WorldSaveSystem.ProvidenceArena.Right - WorldSaveSystem.ProvidenceArena.Left) * 20f;
                }

                CurrentHeight = StartingHeight;
                Projectile.netUpdate = true;
            }

            // Quickly rise.
            if (Time is >= 60f and < 90f)
            {
                CurrentHeight = MathHelper.Lerp(StartingHeight, MaxPillarHeight, Utils.GetLerpValue(60f, 90f, Time, true));
                if (Time == 74 || Time % 6 == 0)
                    Projectile.netUpdate = true;
            }

            // Play a sound when rising.
            if (Time == 80f)
            {
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                SoundEngine.PlaySound(SoundID.Item73, target.Center);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            float rotation = direction.Y == 0f ? -MathHelper.PiOver2 : 0f;
            if (Time < 60f)
            {
                float scale = (float)Math.Sin(Time / 60f * MathHelper.Pi) * 5f;
                if (scale > 1f)
                    scale = 1f;
                scale *= 2f;

                Vector2 lineOffset = Vector2.Zero;
                if (direction.Y == 0f)
                    lineOffset.Y += 42f;

                Utils.DrawLine(Main.spriteBatch, Projectile.Top + lineOffset, Projectile.Top - direction * (-MaxPillarHeight + 240f) + lineOffset, Color.LightGoldenrodYellow, Color.LightGoldenrodYellow, scale);
            }

            Texture2D tipTexture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D pillarTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Providence/CrystalPillarBodyPiece").Value;

            float tipBottom = 0f;
            Color drawColor = Projectile.GetAlpha(Color.White);
            for (int i = pillarTexture.Height; i < CurrentHeight + pillarTexture.Height; i += pillarTexture.Height)
            {
                Vector2 drawPosition = Projectile.Bottom + direction * i - Main.screenPosition;
                Main.spriteBatch.Draw(pillarTexture, drawPosition, null, drawColor, rotation, pillarTexture.Size() * new Vector2(0.5f, 0f), Projectile.scale, SpriteEffects.None, 0f);
                tipBottom = i;
            }

            Vector2 tipDrawPosition = Projectile.Bottom + direction * (tipBottom - 8f) - Main.screenPosition;
            Main.spriteBatch.Draw(tipTexture, tipDrawPosition, null, drawColor, rotation, tipTexture.Size() * new Vector2(0.5f, 1f), Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Time < 60f)
                return false;

            float _ = 0f;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 start = Projectile.Bottom;
            Vector2 end = Projectile.Bottom + direction * (CurrentHeight - 8f);
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, Projectile.width * Projectile.scale, ref _);
        }
    }
}
