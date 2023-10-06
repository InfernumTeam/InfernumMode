using CalamityMod;
using InfernumMode.Common.Worldgen;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria.WorldBuilding;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.EoW
{
    public class CorruptThorn : ModProjectile
    {
        public float CurrentHeight;

        public ref float MaxPillarHeight => ref Projectile.ai[0];

        public ref float Time => ref Projectile.ai[1];

        public const float StartingHeight = 22f;

        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.VilethornTip}";

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Corrupt Thorn");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 28;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.Calamity().DealsDefenseDamage = true;
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

            // Fade in at the beginning of the projectile's life.
            if (Time < 60f)
                Projectile.Opacity = Lerp(Projectile.Opacity, 1f, 0.35f);

            // Wither away at the end of the projectile's life.
            else if (Projectile.timeLeft < 40f)
            {
                Projectile.damage = 0;
                Projectile.scale = Lerp(Projectile.scale, 0.05f, 0.08f);
                Projectile.Opacity = Lerp(Projectile.Opacity, 0f, 0.25f);
            }

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
            WorldUtils.Find(new Vector2(Projectile.Top.X, Projectile.Top.Y - 160).ToTileCoordinates(), Searches.Chain(new Searches.Down(6000), new GenCondition[]
            {
                new Conditions.IsSolid(),
                new CustomTileConditions.ActiveAndNotActuated(),
                new CustomTileConditions.NotPlatform()
            }), out Point newBottom);

            bool isHalfTile = CalamityUtils.ParanoidTileRetrieval(newBottom.X, newBottom.Y - 1).IsHalfBlock;
            Projectile.Bottom = newBottom.ToWorldCoordinates(8, isHalfTile ? 8 : 0);

            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            MaxPillarHeight = MathF.Max(0f, Projectile.Top.Y - target.Top.Y) + StartingHeight + 320f + Math.Abs(target.velocity.Y * 25f);

            // Add some variance to the pillar height to make them feel a bit more alive.
            MaxPillarHeight += Lerp(0f, 100f, Projectile.identity / 7f % 7f) * Main.rand.NextFloat(0.45f, 1.55f);

            CurrentHeight = StartingHeight;
            Projectile.rotation = Main.rand.NextFloat(-0.15f, 0.15f);

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
                float telegraphLineWidth = Sin(Time / 60f * Pi) * 3f;
                if (telegraphLineWidth > 2f)
                    telegraphLineWidth = 2f;
                Main.spriteBatch.DrawLineBetter(Projectile.Top + aimDirection * 10f, Projectile.Top + aimDirection * -MaxPillarHeight, Color.Gray, telegraphLineWidth);
            }

            float tipBottom = 0f;
            Vector2 scale = new(Projectile.scale, 1f);

            DrawVine(scale, aimDirection, tipTexture, ref tipBottom);

            Vector2 tipDrawPosition = Projectile.Bottom - aimDirection * (tipBottom + 4f) - Main.screenPosition;
            Main.spriteBatch.Draw(tipTexture, tipDrawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, tipTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            return false;
        }

        public void DrawVine(Vector2 scale, Vector2 aimDirection, Texture2D tipTexture, ref float tipBottom)
        {
            Main.instance.LoadProjectile(ProjectileID.VilethornBase);
            Texture2D thornBodyPiece = TextureAssets.Projectile[ProjectileID.VilethornBase].Value;

            UnifiedRandom sideThornRNG = new(Projectile.identity);
            for (int i = thornBodyPiece.Height; i < CurrentHeight + thornBodyPiece.Height; i += thornBodyPiece.Height)
            {
                Vector2 drawPosition = Projectile.Bottom - aimDirection * i - Main.screenPosition;

                // Draw sideThorns on the side from time to time.
                if (sideThornRNG.NextFloat() < 0.7f && Math.Abs(i - (CurrentHeight + thornBodyPiece.Height)) > 60f)
                {
                    float offsetRotation = -sideThornRNG.NextFloat(0.25f);

                    // Sometimes draw sideThorns at an opposite angle.
                    if (sideThornRNG.NextBool(3))
                        offsetRotation = Pi - offsetRotation + PiOver4;

                    float sideThornRotation = aimDirection.RotatedBy(offsetRotation).ToRotation();
                    Vector2 sideThornPosition = drawPosition + sideThornRotation.ToRotationVector2().RotatedBy(-PiOver2) * 18f;
                    Main.spriteBatch.Draw(tipTexture, sideThornPosition, null, Projectile.GetAlpha(Color.White), sideThornRotation, tipTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                }

                Main.spriteBatch.Draw(thornBodyPiece, drawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, thornBodyPiece.Size() * new Vector2(0.5f, 0f), scale, SpriteEffects.None, 0f);
                tipBottom = i;
            }
        }

        public override bool? CanDamage() => Time >= 70f;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            Vector2 start = Projectile.Bottom;
            Vector2 end = Projectile.Bottom - Vector2.UnitY.RotatedBy(Projectile.rotation) * CurrentHeight;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, Projectile.width * Projectile.scale, ref _);
        }
    }
}

