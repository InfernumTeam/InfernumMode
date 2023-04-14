using CalamityMod;
using CalamityMod.Particles;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Content.Items.Weapons.Rogue;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles
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

        public ref float Time => ref Projectile.ai[1];

        public override string Texture => "InfernumMode/Content/Items/BrimstoneCrescentStaff";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Brimstone Crescent Staff");

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
            Projectile.usesLocalNPCImmunity = true;
        }

        public override void AI()
        {
            switch (CurrentState)
            {
                case BehaviorState.SpinInPlace:
                    DoBehavior_SpinInPlace();
                    break;
            }

            // Stick to the owner.
            AdjustPlayerValues();

            Time++;
        }

        public void AdjustPlayerValues()
        {
            Projectile.timeLeft = 2;
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
            Owner.itemRotation = (Projectile.direction * Projectile.velocity).ToRotation();

            // Update the player's arm directions to make it look as though they're holding the spear.
            float frontArmRotation = Projectile.rotation + Owner.direction * -0.4f;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, frontArmRotation);
        }

        public void DoBehavior_SpinInPlace()
        {
            
        }
    }
}
