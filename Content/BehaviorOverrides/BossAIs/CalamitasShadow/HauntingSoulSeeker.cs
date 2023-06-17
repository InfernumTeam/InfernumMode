using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasShadow
{
    public class HauntingSoulSeeker : ModProjectile
    {
        public Player Owner => Main.player[Projectile.owner];

        public ref float SpinOffsetAngle => ref Projectile.ai[0];

        public override string Texture => "CalamityMod/NPCs/CalClone/SoulSeeker";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Soul Seeker");
            Main.projFrames[Projectile.type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 7200;
            Projectile.Opacity = 0f;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];
            Projectile.rotation = Projectile.AngleTo(Owner.Center) + Pi;

            // Spin around the target.
            SpinOffsetAngle += Pi / 150f;
            Projectile.Center = Owner.Center - Vector2.UnitY.RotatedBy(SpinOffsetAngle) * 550f;

            // Fade in and release fire mist.
            Projectile.Opacity = Owner.Infernum_CalShadowHex().HexStatuses["Indignation"].Intensity;
            if (Projectile.Opacity < 0.85f)
            {
                Color fireMistColor = Color.Lerp(Color.Red, Color.Yellow, Main.rand.NextFloat(0.66f));
                var mist = new MediumMistParticle(Projectile.Center + Main.rand.NextVector2Circular(24f, 24f), Main.rand.NextVector2Circular(4.5f, 4.5f), fireMistColor, Color.Gray, Main.rand.NextFloat(0.6f, 1.3f), 208 - Main.rand.Next(50), 0.02f);
                GeneralParticleHandler.SpawnParticle(mist);
            }

            // Periodically release dark magic bolts.
            int shootRate = 40;
            if (Projectile.timeLeft % shootRate == shootRate - 1f && Projectile.Opacity >= 0.9f)
            {
                // Release some fire mist.
                Vector2 magicVelocity = Projectile.SafeDirectionTo(Owner.Center) * Main.rand.NextFloat(7.5f, 9f);
                for (int i = 0; i < 8; i++)
                {
                    Color fireMistColor = Color.Lerp(Color.Red, Color.Yellow, Main.rand.NextFloat(0.66f));
                    var mist = new MediumMistParticle(Projectile.Center + magicVelocity * 2f + Main.rand.NextVector2Circular(10f, 10f), Vector2.Zero, fireMistColor, Color.Gray, Main.rand.NextFloat(0.6f, 1.3f), 195 - Main.rand.Next(50), 0.02f)
                    {
                        Velocity = magicVelocity.RotatedByRandom(0.2f) * Main.rand.NextFloat(0.9f, 2.4f)
                    };
                    GeneralParticleHandler.SpawnParticle(mist);
                }

                SoundEngine.PlaySound(SoundID.Item72, Projectile.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(Projectile.Center + magicVelocity * 2f, magicVelocity, ModContent.ProjectileType<DarkMagicFlame>(), CalamitasShadowBehaviorOverride.DarkMagicFlameDamage, 0f);
            }

            // Fade away if the hex has been lifted.
            if (Projectile.timeLeft < 7140 && Projectile.Opacity <= 0.02f)
                Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Projectile.DrawBackglow(Color.IndianRed with { A = 0 }, 12f);
            Projectile.DrawProjectileWithBackglowTemp(Color.Yellow with { A = 0 } * 0.4f, Color.White * 0.5f, 5f);
            return false;
        }
    }
}
