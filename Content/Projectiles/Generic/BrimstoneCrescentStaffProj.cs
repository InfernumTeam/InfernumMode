using CalamityMod.Particles;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Content.Buffs;
using InfernumMode.Content.Items.Misc;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Generic
{
    public class BrimstoneCrescentStaffProj : ModProjectile
    {
        public enum BehaviorState
        {
            SpinInPlace,
            RaiseUpward
        }

        public BehaviorState CurrentState
        {
            get => (BehaviorState)Projectile.ai[0];
            set => Projectile.ai[0] = (int)value;
        }

        public Player Owner => Main.player[Projectile.owner];

        public Vector2 EndOfStaff => Projectile.Center + (Projectile.rotation - PiOver4).ToRotationVector2() * Projectile.scale * 58f;

        public ref float Time => ref Projectile.ai[1];

        public ref float InitialDirection => ref Projectile.localAI[0];

        public ref float OutwardExtension => ref Projectile.localAI[1];

        public override string Texture => "InfernumMode/Content/Items/Misc/BrimstoneCrescentStaff";

        public override void SetDefaults()
        {
            Projectile.width = 116;
            Projectile.height = 116;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.Opacity = 1f;
            Projectile.timeLeft = 14400;
            Projectile.penetrate = -1;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            // Die if no longer holding the click button or otherwise cannot use the item.
            if (!Owner.channel || Owner.dead || !Owner.active || Owner.noItems || Owner.CCed)
            {
                Projectile.Kill();
                return;
            }

            // Set the initial direction.
            if (InitialDirection == 0f)
                InitialDirection = -PiOver4;

            // Stick to the owner.
            AdjustPlayerValues();

            switch (CurrentState)
            {
                case BehaviorState.SpinInPlace:
                    DoBehavior_SpinInPlace();
                    break;
                case BehaviorState.RaiseUpward:
                    DoBehavior_RaiseUpward();
                    break;
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

            float rotationCosine = Cos(Projectile.rotation - PiOver4);
            if (Math.Abs(rotationCosine) >= 0.02f)
                Owner.ChangeDir((rotationCosine >= 0f).ToDirectionInt());

            Projectile.Center = Owner.MountedCenter + (Projectile.rotation - PiOver4).ToRotationVector2() * OutwardExtension - Vector2.UnitX * Owner.direction * 5f;

            // Update the player's arm directions to make it look as though they're holding the spear.
            float frontArmRotation = Projectile.rotation + Pi + PiOver4 - Owner.direction * 0.26f;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, frontArmRotation);
        }

        public void DoBehavior_SpinInPlace()
        {
            // Perform spin effects.
            float spinInterpolant = Utils.GetLerpValue(0f, BrimstoneCrescentStaff.SpinTime, Time, true);
            Projectile.rotation = InitialDirection + Pi * SmoothStep(0f, 1f, Pow(spinInterpolant, 0.8f)) * 4f;

            // Make the spear gradually move out a little bit.
            OutwardExtension = Pow(spinInterpolant, 2.3f) * 20f;

            // Release a streak of fire energy.
            CreateFlameEnergy(spinInterpolant >= 0.95f);

            if (spinInterpolant >= 1f)
            {
                CurrentState = BehaviorState.RaiseUpward;
                Time = 0f;
                Projectile.netUpdate = true;
            }
        }

        public void DoBehavior_RaiseUpward()
        {
            // Make the spear move further outward.
            OutwardExtension = Lerp(OutwardExtension, 45f, 0.08f);

            // Release a streak of fire energy.
            CreateFlameEnergy(true);

            // Make the staff disappear into a burst of flames and enable the forcefield after enough time has passed.
            if (Time >= BrimstoneCrescentStaff.RaiseUpwardsTime)
            {
                Owner.Infernum().SetValue<bool>("BrimstoneCrescentForcefieldIsActive", !Owner.Infernum().GetValue<bool>("BrimstoneCrescentForcefieldIsActive"));

                if (Owner.HasBuff<BrimstoneExhaustion>())
                    Owner.Infernum().SetValue<bool>("BrimstoneCrescentForcefieldIsActive", false);

                SoundEngine.PlaySound(SoundID.DD2_BetsyFireballShot, Owner.Center);
                SoundEngine.PlaySound(InfernumSoundRegistry.CalShadowTeleportSound, Owner.Center);
                for (int i = 0; i < 36; i++)
                {
                    Color fireColor = Color.Lerp(Color.OrangeRed, Color.Orange, Main.rand.NextFloat());
                    Vector2 fireSpawnPosition = Projectile.Center + Main.rand.NextVector2Circular(0.4f, 1f) * Projectile.Size.RotatedBy(Projectile.rotation - PiOver4);
                    CloudParticle fire = new(fireSpawnPosition, Main.rand.NextVector2Circular(4f, 4f), fireColor, Color.DarkGray, Main.rand.Next(25, 36), Main.rand.NextFloat(0.8f, 1.3f));
                    GeneralParticleHandler.SpawnParticle(fire);
                }

                Projectile.Kill();
            }
        }

        public void CreateFlameEnergy(bool aimUpwards)
        {
            int energyCount = 9;
            Vector2 energyDirection = -(Projectile.rotation + PiOver4).ToRotationVector2();
            if (aimUpwards)
            {
                energyCount = 2;
                energyDirection = -Vector2.UnitY;
            }

            for (int i = 0; i < energyCount; i++)
            {
                float energySizeInterpolant = Main.rand.NextFloat();
                float energyScale = Lerp(0.3f, 0.9f, energySizeInterpolant);
                int energyLifetime = (int)Lerp(20f, 45f, energySizeInterpolant);
                Color energyColor = Color.Lerp(Color.Orange, Color.Red, energySizeInterpolant * 0.75f);
                SquishyLightParticle energy = new(EndOfStaff, energyDirection.RotatedByRandom(0.69f) * Main.rand.NextFloat(0.4f, 2.7f), energyScale, energyColor, energyLifetime, 1f, 3f);
                GeneralParticleHandler.SpawnParticle(energy);
            }
        }
    }
}
