using CalamityMod;
using InfernumMode.Assets.Sounds;
using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Ranged
{
    public class GlassmakerHoldout : ModProjectile
    {
        public SlotId FlameIntroSoundSlot;

        // This stores the sound slot of the flame sound it makes, so it may be properly updated in terms of position.
        public SlotId FlameSoundSlot;

        public Player Owner => Main.player[Projectile.owner];

        public ref float Time => ref Projectile.ai[0];

        public override string Texture => "InfernumMode/Content/Items/Weapons/Ranged/TheGlassmaker";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Glassmaker");

        public override void SetDefaults()
        {
            Projectile.width = 64;
            Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.timeLeft = 7200;
            Projectile.penetrate = -1;
        }

        public override void AI()
        {
            Item heldItem = Owner.ActiveItem();

            // Die if no longer holding the click button or otherwise cannot use the item.
            if (!Owner.channel || Owner.dead || !Owner.active || Owner.noItems || Owner.CCed || heldItem is null)
            {
                Projectile.Kill();
                return;
            }

            // Stick to the owner.
            Projectile.Center = Owner.MountedCenter;
            AdjustPlayerValues();

            // Update damage dynamically, in case item stats change during the projectile's lifetime.
            Projectile.damage = Owner.GetWeaponDamage(Owner.ActiveItem());

            // Release flames outward.
            if (Main.myPlayer == Projectile.owner && Time % heldItem.useTime == heldItem.useTime - 1f && Owner.HasAmmo(heldItem))
            {
                Owner.PickAmmo(heldItem, out _, out float shootSpeed, out int fireDamage, out float knockback, out _);
                Vector2 fireSpawnPosition = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * heldItem.width * 0.5f;
                Vector2 fireShootVelocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * shootSpeed;

                Projectile.NewProjectile(Projectile.GetSource_FromThis(), fireSpawnPosition, fireShootVelocity, ModContent.ProjectileType<GlassmakerFire>(), fireDamage, knockback, Projectile.owner);
                if (Main.rand.NextBool(3))
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), fireSpawnPosition, fireShootVelocity.RotatedByRandom(0.43f) * 2.4f, ModContent.ProjectileType<GlassPiece>(), fireDamage, knockback, Projectile.owner);
            }

            if (Time == 0f)
                FlameIntroSoundSlot = SoundEngine.PlaySound(InfernumSoundRegistry.GlassmakerFireStartSound with { Volume = 0.85f }, Projectile.Center);

            bool startSoundBeingPlayed = SoundEngine.TryGetActiveSound(FlameIntroSoundSlot, out var startSound) && startSound.IsPlaying;
            if (startSoundBeingPlayed)
                startSound.Position = Projectile.Center;
            else
            {
                // Update the sound telegraph's position.
                if (SoundEngine.TryGetActiveSound(FlameSoundSlot, out var t) && t.IsPlaying)
                    t.Position = Projectile.Center;
                else
                    FlameSoundSlot = SoundEngine.PlaySound(InfernumSoundRegistry.GlassmakerFireSound with { Volume = 1.2f }, Projectile.Center);
            }

            Time++;
        }

        public void AdjustPlayerValues()
        {
            Projectile.timeLeft = 2;
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
            Owner.itemRotation = (Projectile.direction * Projectile.velocity).ToRotation();

            // Aim towards the mouse.
            if (Main.myPlayer == Projectile.owner)
            {
                Projectile.velocity = Projectile.SafeDirectionTo(Main.MouseWorld, Vector2.UnitX * Owner.direction);
                if (Projectile.velocity != Projectile.oldVelocity)
                    Projectile.netUpdate = true;
                Projectile.spriteDirection = (Projectile.velocity.X > 0f).ToDirectionInt();
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.spriteDirection == -1)
                Projectile.rotation += MathHelper.Pi;
            Owner.ChangeDir(Projectile.spriteDirection);

            Projectile.Center += Projectile.velocity * 20f;

            // Update the player's arm directions to make it look as though they're holding the flamethrower.
            float frontArmRotation = Projectile.rotation + Owner.direction * -0.4f;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, frontArmRotation);
        }

        public override void Kill(int timeLeft)
        {
            // Stop the flame sounds abruptly if the flamethrower is destroyed.
            if (SoundEngine.TryGetActiveSound(FlameIntroSoundSlot, out var t) && t.IsPlaying)
                t.Stop();
            if (SoundEngine.TryGetActiveSound(FlameSoundSlot, out t) && t.IsPlaying)
                t.Stop();

            SoundEngine.PlaySound(InfernumSoundRegistry.GlassmakerFireEndSound with { Volume = 0.85f }, Projectile.Center);
        }

        public override bool? CanDamage() => false;
    }
}
