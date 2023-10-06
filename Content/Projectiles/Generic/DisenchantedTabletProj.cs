using CalamityMod.Particles;
using InfernumMode.Assets.Sounds;
using InfernumMode.Content.Items.Pets;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Events;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Generic
{
    public class DisenchantedTabletProj : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public static int Lifetime => 210;

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Disenchanted Tablet");

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 28;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            // Rise upward.
            float upwardRiseSpeed = Utils.Remap(Time, 30f, 96f, 2.7f, 0f);
            Projectile.velocity = -Vector2.UnitY * upwardRiseSpeed;

            // Release inward pulses and emit energy after movement ceases.
            if (upwardRiseSpeed <= 0f)
            {
                if (Time % 20f == 0f)
                {
                    Color energyColor = Color.Lerp(Color.Turquoise, Color.Cyan, Main.rand.NextFloat(0.5f));
                    PulseRing ring = new(Projectile.Center, Vector2.Zero, energyColor, 1.95f, 0f, 60);
                    GeneralParticleHandler.SpawnParticle(ring);
                }

                // Create energy emissions.
                Vector2 firePosition = Projectile.Center + Main.rand.NextVector2Circular(60f, 60f);
                Color fireColor = Color.Lerp(Color.Cyan, Color.Lime, Main.rand.NextFloat(0.18f, 0.7f));
                float fireScale = Main.rand.NextFloat(0.9f, 1.25f);
                float fireRotationSpeed = Main.rand.NextFloat(-0.05f, 0.05f);

                var particle = new HeavySmokeParticle(firePosition, Vector2.Zero, fireColor, Main.rand.Next(18, 25), fireScale, 1, fireRotationSpeed, true, 0f, true);
                GeneralParticleHandler.SpawnParticle(particle);
            }

            Time++;

            MoonlordDeathDrama.RequestLight(Utils.GetLerpValue(90f, 25f, Projectile.timeLeft, true), Projectile.Center);
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(InfernumSoundRegistry.CeaselessVoidStrikeSound with { Pitch = 0.5f }, Projectile.Center);
            Item.NewItem(Projectile.GetSource_Death(), Projectile.Center, ModContent.ItemType<RisingWarriorsSoulstone>());
        }
    }
}
