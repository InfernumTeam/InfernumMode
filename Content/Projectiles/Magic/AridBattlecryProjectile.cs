using CalamityMod;
using InfernumMode.Assets.Sounds;
using InfernumMode.Content.Items.Weapons.Magic;
using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Magic
{
    public class AridBattlecryProjectile : ModProjectile
    {
        // This stores the sound slot of the horn sound it makes, so it may be properly updated in terms of position.
        public SlotId HornSoundSlot;

        public Player Owner => Main.player[Projectile.owner];

        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults() => DisplayName.SetDefault("Arid Battlecry");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 7200;
            Projectile.penetrate = -1;
        }

        public override void AI()
        {
            // Die if no longer holding the click button or otherwise cannot use the item.
            if (!Owner.channel || Owner.dead || !Owner.active || Owner.noItems || Owner.CCed)
            {
                Projectile.Kill();
                return;
            }

            // Stick to the owner.
            Projectile.Center = Owner.MountedCenter - Vector2.UnitX * Projectile.spriteDirection * 18f;
            AdjustPlayerValues();

            // Release sharks from below.
            // CheckMana returns true if the mana cost can be paid. If mana isn't consumed this frame, the CheckMana short-circuits out of being evaluated.
            if (Main.myPlayer == Projectile.owner && Time % AridBattlecry.SharkSummonRate == AridBattlecry.SharkSummonRate - 1f && Owner.CheckMana(Owner.ActiveItem(), -1, true))
            {
                Vector2 sharkSpawnPosition = Main.MouseWorld + new Vector2(Main.rand.NextFloatDirection() * 96f, 600f);
                Vector2 sharkSpawnVelocity = (Main.MouseWorld - sharkSpawnPosition).SafeNormalize(Vector2.UnitY) * 8.5f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), sharkSpawnPosition, sharkSpawnVelocity, ModContent.ProjectileType<MiniSandShark>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            }

            // Play the horn sound on the first frame.
            if (Time == 1f)
                HornSoundSlot = SoundEngine.PlaySound(InfernumSoundRegistry.VassalHornSound with { Volume = 0.8f }, Projectile.Center);

            // Update the sound telegraph's position.
            if (SoundEngine.TryGetActiveSound(HornSoundSlot, out var t) && t.IsPlaying)
                t.Position = Projectile.Center;

            Time++;
        }

        public void AdjustPlayerValues()
        {
            Projectile.spriteDirection = Projectile.direction = -Owner.direction;
            Projectile.timeLeft = 2;
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
            Owner.itemRotation = (Projectile.direction * Projectile.velocity).ToRotation();

            // Update the player's arm directions to make it look as though they're holding the horn.
            float frontArmRotation = (MathHelper.PiOver2 - 0.31f) * -Owner.direction;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, frontArmRotation);

            Owner.eyeHelper.BlinkBecausePlayerGotHurt();
        }

        public override void Kill(int timeLeft)
        {
            // Stop the horn sound abruptly if the horn is destroyed.
            if (SoundEngine.TryGetActiveSound(HornSoundSlot, out var t) && t.IsPlaying)
                t.Stop();
        }
    }
}
