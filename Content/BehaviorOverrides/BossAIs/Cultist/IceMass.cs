using CalamityMod.Events;
using InfernumMode.Common.Graphics.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Cultist
{
    public class IceMass : ModProjectile, IScreenCullDrawer
    {
        public ref float Time => ref Projectile.ai[0];

        public const int ShardBurstCount = 9;

        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.CultistBossIceMist}";

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Ice Mass");

        public override void SetDefaults()
        {
            Projectile.width = 92;
            Projectile.height = 102;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 135;
            Projectile.Opacity = 0f;
            Projectile.extraUpdates = 1;
            Projectile.penetrate = -1;
            
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 40f, Time, true);
            Projectile.rotation += Pi / 30f;

            if (Time >= 110)
                Projectile.velocity *= 0.975f;
            Time++;
        }

        public void CullDraw(SpriteBatch spriteBatch)
        {
            // Draw telegraph lines.
            // The amount of these will create a somewhat geometric pattern.
            if (Time is > 60f and < 170f)
            {
                float lineWidth = Utils.GetLerpValue(45f, 75f, Time, true) * Utils.GetLerpValue(0f, 30f, Projectile.timeLeft, true) * 2.5f + 0.2f;

                if (lineWidth > 1f)
                    lineWidth += Sin(Main.GlobalTimeWrappedHourly * 5f) * 0.15f;

                for (int i = 0; i < ShardBurstCount; i++)
                {
                    Vector2 lineDirection = (TwoPi * (i + 0.5f) / ShardBurstCount).ToRotationVector2();
                    Main.spriteBatch.DrawLineBetter(Projectile.Center, Projectile.Center + lineDirection * 5980f, Color.SkyBlue, lineWidth);
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item92, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // Release ice shards based on the telegraphs.
            for (int i = 0; i < ShardBurstCount; i++)
            {
                for (float speed = 6f; speed <= 21f; speed += 3.3f)
                {
                    Vector2 iceVelocity = (TwoPi * (i + 0.5f) / ShardBurstCount).ToRotationVector2() * speed * (BossRushEvent.BossRushActive ? 1.6f : 1f);
                    Utilities.NewProjectileBetter(Projectile.Center, iceVelocity, ModContent.ProjectileType<IceShard>(), CultistBehaviorOverride.IceShardDamage, 0f);
                }
            }
        }
    }
}
