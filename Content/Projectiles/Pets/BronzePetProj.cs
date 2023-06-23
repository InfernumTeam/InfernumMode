using CalamityMod.Particles;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Core.GlobalInstances.Players;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Pets
{
    public class BronzePetProj : ModProjectile
    {
        public enum BirbAIState
        {
            Flying,
            DescendToGround,
            SitDown,
            BeingPet
        }

        public enum BirbFrameState
        {
            Flying,
            Sitting,
            Walking,
            BeingPet
        }

        public Player Owner => Main.player[Projectile.owner];

        public BirbAIState AIState
        {
            get => (BirbAIState)Projectile.ai[0];
            set => Projectile.ai[0] = (int)value;
        }

        public BirbFrameState FrameState
        {
            get => (BirbFrameState)Projectile.localAI[0];
            set
            {
                if (FrameState != value)
                {
                    Projectile.frameCounter = 0;
                    Projectile.localAI[0] = (int)value;
                }
            }
        }

        public Vector2 GroundRestingPosition =>
            Utilities.GetGroundPositionFrom(Owner.Top + Vector2.UnitX * -Owner.direction * 60f);

        public ref float Time => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Yharn");
            Main.projFrames[Projectile.type] = 9;
            Main.projPet[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 30;
            Projectile.friendly = true;
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

            HandlePetVariables();

            switch (AIState)
            {
                case BirbAIState.Flying:
                    DoBehavior_Flying();
                    break;
                case BirbAIState.DescendToGround:
                    DoBehavior_DescendToGround();
                    break;
                case BirbAIState.SitDown:
                    DoBehavior_SitDown();
                    break;
                case BirbAIState.BeingPet:
                    DoBehavior_BeingPet();
                    break;
            }

            Projectile.gfxOffY = -6;
            DecideFrames();
            Time++;
        }

        public void HandlePetVariables()
        {
            PetsPlayer modPlayer = Owner.Infernum_Pet();
            if (Owner.dead)
                modPlayer.BronzePet = false;
            if (modPlayer.BronzePet)
                Projectile.timeLeft = 2;
        }

        public void DecideFrames()
        {
            switch (FrameState)
            {
                case BirbFrameState.Flying:
                    Projectile.frameCounter++;

                    float flyFrameInterpolant = Projectile.frameCounter / 20f % 1f;
                    Projectile.frame = (int)Round(Lerp(4f, 7f, flyFrameInterpolant));
                    if (Projectile.frame == 4)
                        Projectile.gfxOffY -= 2;

                    break;
                case BirbFrameState.Sitting:
                    Projectile.frame = 0;
                    Projectile.gfxOffY = -12;
                    break;
                case BirbFrameState.BeingPet:
                    Projectile.frame = 8;
                    break;
            }
        }

        public void DoBehavior_Flying()
        {
            float verticalOffset = Cos(TwoPi * Time / 150f) * 30f - 50f;
            Vector2 hoverDestination = Owner.Top + new Vector2(-Owner.direction * 60f, verticalOffset);
            Vector2 idealVelocity = Projectile.SafeDirectionTo(hoverDestination) * 18f;

            // Teleport to the owner if absurdly far away from them.
            if (!Projectile.WithinRange(Owner.Center, 2400f))
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.BirbCrySound, Projectile.Center);
                Projectile.Center = Owner.Center;
            }

            // Fly towards the hover destination.
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, idealVelocity, 0.04f).MoveTowards(idealVelocity, 0.15f);

            // Slow down if sufficiently close to the hover destination.
            if (Projectile.WithinRange(hoverDestination, 180f))
                Projectile.velocity *= 0.8f;

            // Use flying frames.
            FrameState = BirbFrameState.Flying;

            // Prevent natural tile collision.
            Projectile.tileCollide = false;

            // Decide rotation and direction.
            Projectile.rotation = Projectile.velocity.X * 0.01f;
            if (Distance(hoverDestination.X, Projectile.Center.X) >= 30f)
                Projectile.spriteDirection = (Projectile.velocity.X < 0f).ToDirectionInt();
            else
                Projectile.spriteDirection = -Owner.direction;

            // Check if the birb should try to move to the ground.
            // This has a delay, to prevent the birb from going to and from the ground/fly state over and over, and considers whether there's any ground nearby for
            // the birb and its owner.
            bool ownerNearGround = Owner.WithinRange(GroundRestingPosition, 150f);
            bool birbNearGround = Projectile.WithinRange(GroundRestingPosition, 360f);
            bool tiredOfFlying = Time >= 240f;
            if (ownerNearGround && birbNearGround && tiredOfFlying)
            {
                AIState = BirbAIState.DescendToGround;
                Time = 0f;
                Projectile.netUpdate = true;
            }
        }

        public void DoBehavior_DescendToGround()
        {
            // Slowly fly towards the ground position.
            Vector2 idealVelocity = Projectile.SafeDirectionTo(GroundRestingPosition) * 4f;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, idealVelocity, 0.08f);

            // Use flying frames.
            FrameState = BirbFrameState.Flying;

            // Allow tile collision.
            Projectile.tileCollide = true;

            // Decide rotation and direction.
            Projectile.rotation = Projectile.velocity.X * 0.016f;
            if (Distance(GroundRestingPosition.X, Projectile.Center.X) >= 30f)
                Projectile.spriteDirection = (Projectile.velocity.X < 0f).ToDirectionInt();
            else
                Projectile.spriteDirection = -Owner.direction;

            // Rest if the the destination was reached.
            if (Projectile.WithinRange(GroundRestingPosition, 45f))
            {
                AIState = BirbAIState.SitDown;
                Time = 0f;
                Projectile.velocity.X = 0f;
                Projectile.netUpdate = true;
            }

            // Fly again if the owner moved away.
            if (!Projectile.WithinRange(Owner.Center, 400f))
            {
                AIState = BirbAIState.Flying;
                Time = 0f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(Owner.Center) * 8f, 0.6f);
                Projectile.netUpdate = true;
            }
        }

        public void DoBehavior_SitDown()
        {
            // Allow tile collision.
            Projectile.tileCollide = true;

            // Cease all horizontal and upward vertical velocity.
            Projectile.velocity.X = 0f;
            Projectile.velocity.Y = Clamp(Projectile.velocity.Y + 0.3f, 0f, 4f);

            // Use sitting frames.
            FrameState = BirbFrameState.Sitting;
            Projectile.spriteDirection = (Owner.Center.X < Projectile.Center.X).ToDirectionInt();

            // Fly again if the owner moved away.
            if (!Projectile.WithinRange(Owner.Center, 300f))
            {
                AIState = BirbAIState.Flying;
                Time = 0f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(Owner.Center) * 6f, 0.6f);
                Projectile.netUpdate = true;
            }

            // Be pet if the player right clicks on the birb.
            if (Main.myPlayer == Projectile.owner && Utils.CenteredRectangle(Main.MouseWorld, Vector2.One * 2f).Intersects(Projectile.Hitbox) && Owner.WithinRange(Projectile.Center, 120f) && Main.mouseRight && Main.mouseRightRelease)
            {
                AIState = BirbAIState.BeingPet;
                Owner.Infernum_Pet().PetProjectile(Projectile);
                Time = 0f;
                Projectile.netUpdate = true;
            }
        }

        public void DoBehavior_BeingPet()
        {
            if (Time == 2f)
            {
                // Create some heart particles.
                for (int i = 0; i < 5; i++)
                {
                    Vector2 heartVelocity = -Vector2.UnitY.RotatedByRandom(0.74f) * Main.rand.NextFloat(0.3f, 2f);
                    HeartParticle heart = new(Projectile.Top + Main.rand.NextVector2Circular(15f, 6f), Color.Red, Color.DarkRed, heartVelocity, Main.rand.Next(60, 96), heartVelocity.X * 0.3f, Main.rand.NextFloat(0.2f, 0.36f));
                    GeneralParticleHandler.SpawnParticle(heart);
                }

                SoundEngine.PlaySound(InfernumSoundRegistry.BirbCrySound, Projectile.Center);
            }

            // Use petting frames.
            if (Time >= 3f)
            {
                FrameState = BirbFrameState.BeingPet;
                Projectile.spriteDirection = Owner.direction;
            }

            // Sit down as usual again if the owner stopped giving pets.
            if (Main.myPlayer == Projectile.owner && Owner.Infernum_Pet().ProjectileThatsBeingPetted != Projectile.whoAmI)
            {
                Owner.Infernum_Pet().StopPetAnimation();
                AIState = BirbAIState.SitDown;
                Time = 0f;
                Projectile.netUpdate = true;
                return;
            }

            Player.CompositeArmStretchAmount stretch = Player.CompositeArmStretchAmount.ThreeQuarters;
            if (Owner.miscCounter % 14 / 7 == 1)
                stretch = Player.CompositeArmStretchAmount.Full;
            Owner.SetCompositeArmBack(true, stretch, -TwoPi * Owner.direction * 0.27f);
        }

        // Prevent dying when touching tiles.
        public override bool OnTileCollide(Vector2 oldVelocity) => false;
    }
}
