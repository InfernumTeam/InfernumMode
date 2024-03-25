using CalamityMod;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class DemonicTelegraphLine : ModProjectile
    {
        public bool DontMakeProjectile
        {
            get => Projectile.localAI[1] == 1f;
            set => Projectile.localAI[1] = value.ToInt();
        }

        public ref float Time => ref Projectile.ai[0];

        public ref float Lifetime => ref Projectile.ai[1];

        public ref float BombRadius => ref Projectile.localAI[0];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Telegraph");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 900;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(BombRadius);
            writer.Write(DontMakeProjectile);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            BombRadius = reader.ReadSingle();
            DontMakeProjectile = reader.ReadBoolean();
        }

        public override void AI()
        {
            Projectile.Opacity = LumUtils.Convert01To010(Time / Lifetime) * 3f;
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;
            if (Time >= Lifetime)
                Projectile.Kill();

            Time++;
        }

        public override void OnKill(int timeLeft)
        {
            if (DontMakeProjectile)
                return;

            SoundEngine.PlaySound(SoundID.Item74, Projectile.Center);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 bombShootVelocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * 19.5f;

                ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(bomb =>
                {
                    bomb.timeLeft = Main.rand.Next(135, 185);
                });
                Utilities.NewProjectileBetter(Projectile.Center, bombShootVelocity, ModContent.ProjectileType<DemonicBomb>(), 0, 0f, -1, BombRadius);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float telegraphWidth = Lerp(0.3f, 3f, LumUtils.Convert01To010(Time / Lifetime));

            // Draw a telegraph line outward.
            Vector2 start = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * 3000f;
            Vector2 end = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * 3000f;
            Main.spriteBatch.DrawLineBetter(start, end, Color.Red, telegraphWidth);
            return false;
        }
    }
}