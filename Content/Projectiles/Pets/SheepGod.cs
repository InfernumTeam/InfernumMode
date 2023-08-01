using CalamityMod;
using CalamityMod.Particles;
using InfernumMode.Assets.Sounds;
using InfernumMode.Content.Buffs;
using InfernumMode.Core.GlobalInstances.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Pets
{
    public class SheepGod : ModProjectile
    {
        public Player Owner => Main.player[Projectile.owner];

        public bool HasSpecialName
        {
            get
            {
                if (Owner.name == "Shade")
                    return true;
                if (Owner.name == "Bedman")
                    return true;
                if (Owner.name == "Delilah F. Neumann")
                    return true;
                if (Owner.name == "Romeo F. Neumann")
                    return true;

                return false;
            }
        }

        public ref float Time => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Sheep God");
            Main.projFrames[Projectile.type] = 1;
            Main.projPet[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 140;
            Projectile.height = 190;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90000;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            if (!Owner.active)
            {
                Projectile.active = false;
                return;
            }

            // Ensure that the sheep god spawns above the owner and not directly on top of them.
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.Center = Owner.Center - Vector2.UnitY * 600f;
                Projectile.localAI[0] = 1f;
            }

            HandlePetVariables();

            // Drift towards the owner. The speed at which the sheep god moves increases based on darkness.
            float brightness = Lighting.Brightness((int)(Owner.Center.X / 16f), (int)(Owner.Center.Y / 16f));
            float darknessInterpolant = Utils.GetLerpValue(0.54f, 0.01f, brightness, true);
            float flySpeed = SmoothStep(0.3f, 5.2f, darknessInterpolant);
            if (Owner.HasBuff<Sleepy>())
                flySpeed *= 0.2f;

            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(Owner.Center) * flySpeed, 0.332f);
            Projectile.spriteDirection = Projectile.velocity.X.DirectionalSign();

            // Enforce a hard limit on how far the sheep god can be from the owner, to prevent just running away forever.
            if (!Projectile.WithinRange(Owner.Center, 1600f))
                Projectile.Center = Owner.Center + Owner.SafeDirectionTo(Projectile.Center) * 1600f;

            // Fade out based on distance.
            Projectile.Opacity = Utils.GetLerpValue(720f, 300f, Projectile.Distance(Owner.Center), true);

            // Temporarily disappear, make the player fall asleep, and provide adrenaline if the sheep god touches the owner.
            if (Projectile.Hitbox.Intersects(Owner.Hitbox) && !HasSpecialName)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.VassalTeleportSound, Owner.Center);
                for (int i = 0; i < 32; i++)
                {
                    Vector2 fireSpawnPosition = Projectile.Center + Main.rand.NextVector2Circular(54f, 54f);
                    Color fireColor = Color.Lerp(Color.Red, Color.Yellow, Main.rand.NextFloat(0.72f));
                    HeavySmokeParticle fire = new(fireSpawnPosition, Main.rand.NextVector2Circular(4f, 4f), fireColor, 40, 0.8f, 1f, Main.rand.NextFloat(0.0025f), true);
                    GeneralParticleHandler.SpawnParticle(fire);
                }

                // Give the player the (de)buffs.
                Owner.Calamity().adrenaline = Clamp(Owner.Calamity().adrenaline + 20f, 0f, 100f);
                Owner.AddBuff(ModContent.BuffType<Sleepy>(), 300);

                Projectile.Center = Owner.Center - Vector2.UnitY * 950f + Main.rand.NextVector2Circular(80f, 80f);
                Projectile.netUpdate = true;
            }

            Time++;
        }

        public void HandlePetVariables()
        {
            PetsPlayer modPlayer = Owner.Infernum_Pet();
            if (Owner.dead)
                modPlayer.SheepGodPet = false;
            if (modPlayer.SheepGodPet)
                Projectile.timeLeft = 2;
        }
    }
}
