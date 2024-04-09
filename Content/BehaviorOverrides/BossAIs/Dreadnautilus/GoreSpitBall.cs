using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Dreadnautilus
{
    public class GoreSpitBall : ModProjectile
    {
        public Player Target => Main.player[Projectile.owner];

        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.DripplerFlail}";

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Gore Spit Ball");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 28;
            Projectile.hostile = true;
            Projectile.timeLeft = 100;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.velocity.X *= 0.98f;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, -Vector2.UnitY * 7f, 0.012f);
            Projectile.rotation += Projectile.velocity.X * 0.01f;
            Lighting.AddLight(Projectile.Center, Color.PaleVioletRed.ToVector3() * 0.5f);
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.NPCDeath11, Projectile.Center);

            for (int i = 0; i < 12; i++)
            {
                Dust blood = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f), 5);
                blood.velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 5.5f);
                blood.scale = Main.rand.NextFloat(1f, 1.8f);
                blood.fadeIn = 0.7f;
                blood.noGravity = true;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 2; i++)
            {
                float aimAtTargetInterpolant = Utils.Remap(Target.velocity.Length(), 6f, 0.4f, 0.4f, 0.95f);
                Vector2 spikeVelocity = (TwoPi * i / 2f).ToRotationVector2();
                spikeVelocity = Vector2.Lerp(spikeVelocity, Projectile.SafeDirectionTo(Target.Center), aimAtTargetInterpolant).SafeNormalize(Vector2.UnitY) * 6f;
                Utilities.NewProjectileBetter(Projectile.Center, spikeVelocity, ModContent.ProjectileType<GoreSpike>(), DreadnautilusBehaviorOverride.GoreSpikeDamage, 0f, Projectile.owner);
            }
        }
    }
}
