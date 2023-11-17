using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Wayfinder
{
    public class WayfinderSymbol : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public ref float Initialized => ref Projectile.ai[1];

        public float MaxScale;

        public int ColorVariation;

        public float RotationAmount;

        public float Speed;

        public static Color[] Colors => new Color[]
        {
            // Light yellow
            new Color (255 ,255 ,150),
            // Golden
            new Color(255, 191, 73),
            // Orange
            new Color(206, 116, 59)
        };

        public const int Lifetime = 240;

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Wayfinder Gate Symbol");

        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.netImportant = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.Opacity = 0f;
        }

        public override void AI()
        {
            // Die if the gate position is not set.
            if (WorldSaveSystem.WayfinderGateLocation == Vector2.Zero)
            {
                Projectile.Kill();
                return;
            }
            // Initialize the random fields, to add variation to each symbol.
            if (Initialized == 0 && Main.netMode is not NetmodeID.MultiplayerClient)
            {
                MaxScale = Main.rand.NextFloat(0.3f, 0.5f);
                ColorVariation = Main.rand.Next(0, 3);
                RotationAmount = Main.rand.NextFloat(-0.03f, 0.03f);
                Speed = Main.rand.NextFloat(0.25f, 0.45f);
                Projectile.velocity = (-Vector2.UnitY * Speed).RotatedByRandom(0.3f);
                Initialized = 1;
                Projectile.netUpdate = true;
            }

            // Fade in and out.
            Projectile.Opacity = Utils.GetLerpValue(0f, 108f, Time, true) * Utils.GetLerpValue(0f, 60f, Projectile.timeLeft, true);
            Projectile.scale = Clamp(Utils.GetLerpValue(0f, 108f, Time, true) * Utils.GetLerpValue(0f, 60f, Projectile.timeLeft, true), 0, MaxScale);
            Projectile.rotation += RotationAmount;
            Time++;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(MaxScale);
            writer.Write(ColorVariation);
            writer.Write(RotationAmount);
            writer.Write(Speed);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            MaxScale = reader.ReadSingle();
            ColorVariation = reader.ReadInt32();
            RotationAmount = reader.ReadSingle();
            Speed = reader.ReadSingle();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPos = Projectile.position - Main.screenPosition;
            Color color = Colors[ColorVariation];
            Vector2 origin = texture.Size() * 0.5f;

            Main.spriteBatch.Draw(bloomTexture, drawPos, null, color with { A = 0 } * Projectile.Opacity * 0.35f, 0f, bloomTexture.Size() * 0.5f, 0.7f * MaxScale, 0, 0);
            Main.spriteBatch.Draw(texture, drawPos, null, color * Projectile.Opacity * 0.5f, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
