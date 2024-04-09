using CalamityMod.Particles;
using InfernumMode.Common.Graphics.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using NuclearTerrorNPC = CalamityMod.NPCs.AcidRain.NuclearTerror;

namespace InfernumMode.Content.BehaviorOverrides.MinibossAIs.NuclearTerror
{
    public class GammaRain : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Gamma Rain");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 22;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];

            Projectile.Opacity = Utils.GetLerpValue(0f, 16f, Projectile.timeLeft, true);
            Projectile.rotation = Projectile.velocity.ToRotation();

            float acceleration = 1.0167f;
            float maxSpeed = 20.5f;
            if (acceleration > 1f && Projectile.velocity.Length() < maxSpeed)
                Projectile.velocity *= acceleration;

            // Explode once past the tile collision line.
            Projectile.tileCollide = Projectile.Top.Y >= Projectile.ai[1];

            Time++;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool? CanDamage() => Projectile.Opacity >= 1f;

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = Color.Lerp(lightColor, Color.White, 0.5f);
            lightColor.A /= 3;
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 1);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(NuclearTerrorNPC.HitSound with { Pitch = 0.4f }, Projectile.Center);

            for (int i = 0; i < 6; i++)
            {
                Color acidColor = Main.rand.NextBool() ? Color.Yellow : Color.Lime;
                CloudParticle acidCloud = new(Projectile.Center, (TwoPi * i / 6f).ToRotationVector2() * 2f + Main.rand.NextVector2Circular(0.3f, 0.3f), acidColor, Color.DarkGray, 27, Main.rand.NextFloat(1.1f, 1.32f))
                {
                    Rotation = Main.rand.NextFloat(TwoPi)
                };
                GeneralParticleHandler.SpawnParticle(acidCloud);
            }
        }

        public override bool ShouldUpdatePosition() => true;
    }
}
