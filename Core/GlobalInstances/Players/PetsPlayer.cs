using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Achievements;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Players
{
    public class PetsPlayer : ModPlayer
    {
        internal int ProjectileThatsBeingPetted
        {
            get;
            private set;
        } = -1;

        public bool BronzePet
        {
            get;
            set;
        }

        public bool AsterPet
        {
            get;
            set;
        }

        public bool SheepGodPet
        {
            get;
            set;
        }

        public bool BlahajPet
        {
            get;
            set;
        }

        public bool IsPettingSomething => ProjectileThatsBeingPetted != -1;

        public Vector2 PlayerPositionWhenPetting
        {
            get
            {
                int horizontalDirection = Math.Sign(Main.projectile[ProjectileThatsBeingPetted].Center.X - Player.Center.X);
                return (Main.projectile[ProjectileThatsBeingPetted].Bottom + new Vector2(-horizontalDirection * 30f, 0f)).Floor();
            }
        }

        public override void ResetEffects()
        {
            BronzePet = false;
            AsterPet = false;
            SheepGodPet = false;
            BlahajPet = false;
        }

        public override void PostUpdate()
        {
            // Determine if the petting animation needs to be stopped.
            if (!IsPettingSomething)
                return;

            // Bootleg the animation with vanilla's existing pet code for town NPCs.
            Player.isPettingAnimal = true;

            if (!Main.projectile[ProjectileThatsBeingPetted].active)
            {
                StopPetAnimation();
                return;
            }

            int horizontalDirection = Math.Sign(Main.projectile[ProjectileThatsBeingPetted].Center.X - Player.Center.X);
            if (Player.controlLeft || Player.controlRight || Player.controlUp || Player.controlDown || Player.controlJump || Player.pulley || Player.mount.Active || horizontalDirection != Player.direction || Player.itemAnimation > 0)
            {
                StopPetAnimation();
                return;
            }

            if (Player.Bottom.Distance(PlayerPositionWhenPetting) > 2f)
                StopPetAnimation();
        }

        public void StopPetAnimation() => ProjectileThatsBeingPetted = -1;

        public void PetProjectile(Projectile projectile)
        {
            ProjectileThatsBeingPetted = projectile.whoAmI;
            bool canPerformAnimation = Player.CanSnapToPosition(PlayerPositionWhenPetting - Player.Bottom);
            if (canPerformAnimation && !WorldGen.SolidTileAllowBottomSlope((int)PlayerPositionWhenPetting.X / 16, (int)PlayerPositionWhenPetting.Y / 16))
                canPerformAnimation = false;

            if (canPerformAnimation)
            {
                if (IsPettingSomething && Player.Bottom == PlayerPositionWhenPetting)
                {
                    StopPetAnimation();
                    return;
                }
                Player.StopVanityActions(true);
                Player.RemoveAllGrapplingHooks();
                if (Player.mount.Active)
                {
                    Player.mount.Dismount(Player);
                }
                Player.Bottom = PlayerPositionWhenPetting;

                Player.ChangeDir(Math.Sign(Main.projectile[ProjectileThatsBeingPetted].Center.X - Player.Center.X));
                Player.isPettingAnimal = true;
                Player.velocity = Vector2.Zero;
                Player.gravDir = 1f;
                if (Player.whoAmI == Main.myPlayer)
                    AchievementsHelper.HandleSpecialEvent(Player, 21);
            }
        }
    }
}