using CalamityMod;
using InfernumMode.Miscellaneous;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.Providence
{
    public class CrystalPillar : ModProjectile
    {
        public bool DarknessVariant
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value.ToInt();
        }
        public ref float MaxPillarHeight => ref Projectile.ai[1];
        public float Time = 0f;
        public float CurrentHeight = 0f;
        public const float StartingHeight = 82f;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crystal");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 28;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.Calamity().canBreakPlayerDefense = true;
            CooldownSlot = 1;
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
                WorldUtils.Find(new Vector2(Projectile.Top.X, Projectile.Top.Y - 160).ToTileCoordinates(), Searches.Chain(new Searches.Down(6000), new GenCondition[]
                {
                    new Conditions.IsSolid(),
                    new CustomTileConditions.ActiveAndNotActuated()
                }), out Point newBottom);

                bool isHalfTile = CalamityUtils.ParanoidTileRetrieval(newBottom.X, newBottom.Y - 1).halfBrick();
                Projectile.Bottom = newBottom.ToWorldCoordinates(8, isHalfTile ? 8 : 0);
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                MaxPillarHeight = MathHelper.Max(0f, Projectile.Top.Y - target.Top.Y) + StartingHeight + 180f + Math.Abs(target.velocity.Y * 60f);
                CurrentHeight = StartingHeight;

                Projectile.netUpdate = true;
            }

            // Quickly rise.
            if (Time is >= 60f and < 75f)
            {
                CurrentHeight = MathHelper.Lerp(StartingHeight, MaxPillarHeight, Utils.GetLerpValue(60f, 75f, Time, true));
                if (Time == 74 || Time % 6 == 0)
                    Projectile.netUpdate = true;
            }

            // Play a sound when rising.
            if (Time == 70)
            {
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                SoundEngine.PlaySound(SoundID.Item73, target.Center);
            }
        }


        public override bool PreDraw(ref Color lightColor)
        {
            if (Time < 60f)
            {
                float scale = (float)Math.Sin(Time / 60f * MathHelper.Pi) * 5f;
                if (scale > 1f)
                    scale = 1f;
                scale *= 2f;
                Utils.DrawLine(spriteBatch, Projectile.Top + Vector2.UnitY * 10f, Projectile.Top + Vector2.UnitY * (-MaxPillarHeight + 240f), Color.LightGoldenrodYellow, Color.LightGoldenrodYellow, scale);
            }

            Texture2D tipTexture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D pillarTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Providence/CrystalPillarBodyPiece").Value;

            float tipBottom = 0f;
            Color drawColor = Projectile.GetAlpha(Color.White);
            for (int i = pillarTexture.Height; i < CurrentHeight + pillarTexture.Height; i += pillarTexture.Height)
            {
                Vector2 drawPosition = Projectile.Bottom - Vector2.UnitY * i - Main.screenPosition;
                Main.spriteBatch.Draw(pillarTexture, drawPosition, null, drawColor, 0f, pillarTexture.Size() * new Vector2(0.5f, 0f), Projectile.scale, SpriteEffects.None, 0f);
                tipBottom = i;
            }

            Vector2 tipDrawPosition = Projectile.Bottom - Vector2.UnitY * (tipBottom - 8f) - Main.screenPosition;
            Main.spriteBatch.Draw(tipTexture, tipDrawPosition, null, drawColor, 0f, tipTexture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            Vector2 start = Projectile.Bottom;
            Vector2 end = Projectile.Bottom - Vector2.UnitY * (CurrentHeight - 8f);
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, Projectile.width * Projectile.scale, ref _);
        }
    }
}
