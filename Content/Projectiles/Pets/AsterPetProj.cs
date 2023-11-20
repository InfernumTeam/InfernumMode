using CalamityMod;
using CalamityMod.Particles;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Content.Items.Pets;
using InfernumMode.Core.GlobalInstances.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Pets
{
    public class AsterPetProj : ModProjectile
    {
        public enum AsterAIState
        {
            SitInPlace,
            WalkToOwner,
            FlyToOwner,
            BeingPet
        }

        public bool SaidBossText
        {
            get;
            set;
        }

        public AsterAIState AIState
        {
            get => (AsterAIState)Projectile.ai[0];
            set => Projectile.ai[0] = (int)value;
        }

        public ref float Time => ref Projectile.ai[1];

        public ref float IdleTextCountdown => ref Projectile.localAI[1];

        public Player Owner => Main.player[Projectile.owner];

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Aster");
            Main.projFrames[Projectile.type] = 12;
            Main.projPet[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 52;
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

            // Say stuff when a boss is summoned.
            if (Utilities.CurrentlyFoughtBoss is not null && !Utilities.CurrentlyFoughtBoss.dontTakeDamage)
            {
                if (!SaidBossText)
                {
                    if (Projectile.localAI[0] != 0f)
                        SaySnarkyComment(Language.GetTextValue($"Mods.InfernumMode.PetDialog.AsterBossSpawn{Main.rand.Next(1, 11)}"));
                    SaidBossText = true;
                }
            }
            else if (Utilities.CurrentlyFoughtBoss is null)
                SaidBossText = false;

            // Bark and say some snarky text on the first frame.
            if (Projectile.localAI[0] == 0f)
            {
                SaySnarkyComment(Language.GetTextValue($"Mods.InfernumMode.PetDialog.AsterSummon{Main.rand.Next(1, 8)}"));
                IdleTextCountdown = 3600f;
                Projectile.localAI[0] = 1f;
            }

            HandlePetVariables();

            switch (AIState)
            {
                case AsterAIState.SitInPlace:
                    DoBehavior_SitInPlace();
                    break;
                case AsterAIState.WalkToOwner:
                    DoBehavior_WalkToOwner();
                    break;
                case AsterAIState.FlyToOwner:
                    DoBehavior_FlyToOwner();
                    break;
                case AsterAIState.BeingPet:
                    DoBehavior_BeingPet();
                    break;
            }

            Time++;
            Projectile.gfxOffY = 6;
        }

        public void SaySnarkyComment(string comment)
        {
            SoundEngine.PlaySound(InfernumSoundRegistry.AsterBarkSound, Projectile.Center);
            CombatText.NewText(Projectile.Hitbox, RisingWarriorsSoulstone.TextColor, comment, true);
        }

        public void DoBehavior_SitInPlace()
        {
            // Allow tile collision.
            Projectile.tileCollide = true;

            // Cease all horizontal and upward vertical velocity.
            Projectile.velocity.X *= 0.84f;
            Projectile.velocity.Y = Clamp(Projectile.velocity.Y + 0.3f, 0f, 8f);

            // Use sitting frames.
            Projectile.frame = 0;
            Projectile.spriteDirection = (Owner.Center.X < Projectile.Center.X).ToDirectionInt();

            // Walk again if the owner moved away.
            if (!Projectile.WithinRange(Owner.Center, 300f))
            {
                AIState = AsterAIState.WalkToOwner;
                Time = 0f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(Owner.Center) * new Vector2(6f, 0.1f), 0.6f);
                Projectile.netUpdate = true;
            }

            // Be pet if the player right clicks on the projectile.
            if (Main.myPlayer == Projectile.owner && Utils.CenteredRectangle(Main.MouseWorld, Vector2.One * 2f).Intersects(Projectile.Hitbox) && Owner.WithinRange(Projectile.Center, 120f) && Main.mouseRight && Main.mouseRightRelease)
            {
                AIState = AsterAIState.BeingPet;
                Owner.Infernum_Pet().PetProjectile(Projectile);
                Time = 0f;
                Projectile.netUpdate = true;
            }

            if (Owner.ZoneForest)
            {
                IdleTextCountdown--;
                if (IdleTextCountdown <= 0f)
                {
                    IdleTextCountdown = Main.rand.Next(1800, 2400);
                    SaySnarkyComment(Language.GetTextValue($"Mods.InfernumMode.PetDialog.AsterPassive{Main.rand.Next(1, 8)}"));
                }
            }

            Projectile.rotation = 0f;
        }

        public void DoBehavior_WalkToOwner()
        {
            // Allow tile collision.
            Projectile.tileCollide = true;

            // Move towards the target.
            Projectile.velocity.X = Lerp(Projectile.velocity.X, Projectile.SafeDirectionTo(Owner.Center).X * 10f, 0.1f);

            // Jump if there's an obstacle.
            float? distanceToObstacle = CalamityUtils.DistanceToTileCollisionHit(Projectile.Center, Vector2.UnitX * Sign(Projectile.velocity.X));
            if ((distanceToObstacle ?? 100f) <= 10f || (Projectile.oldPosition.X == Projectile.position.X && Projectile.velocity.Y == 0f))
            {
                Projectile.velocity.Y = -8f;
                Projectile.netUpdate = true;
            }

            // Adhere to gravity.
            Projectile.velocity.Y = Clamp(Projectile.velocity.Y + 0.3f, -15f, 10f);

            // Sit in place if the owner is close.
            if (Projectile.WithinRange(Owner.Center, 180f))
            {
                AIState = AsterAIState.SitInPlace;
                Time = 0f;
                Projectile.netUpdate = true;
            }

            // Use walking frames.
            Projectile.frameCounter++;
            Projectile.frame = (int)Round(Lerp(3f, 5f, Projectile.frameCounter / 14f % 1f));

            // Fly towards the owner if they're super far away or if there are obstacles in the way.
            if (!Collision.CanHitLine(Projectile.Center, 1, 1, Owner.Center, 1, 1) || !Projectile.WithinRange(Owner.Center, 800f) || Collision.CanHitLine(Owner.Center, 1, 1, Owner.Center + Vector2.UnitY * 300f, 1, 1))
            {
                AIState = AsterAIState.FlyToOwner;
                Time = 0f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(Owner.Center) * 8f, 0.64f);
                Projectile.netUpdate = true;
            }

            Projectile.rotation = 0f;
        }

        public void DoBehavior_FlyToOwner()
        {
            Vector2 hoverDestination = Owner.Top + new Vector2(-Owner.direction * 60f, -10f);
            Vector2 idealVelocity = Projectile.SafeDirectionTo(hoverDestination) * 14f;

            // Teleport to the owner if absurdly far away from them.
            if (!Projectile.WithinRange(Owner.Center, 2400f))
                Projectile.Center = Owner.Center;

            // Fly towards the hover destination.
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, idealVelocity, 0.012f).MoveTowards(idealVelocity, 0.15f);

            // Slow down if sufficiently close to the hover destination.
            if (Projectile.WithinRange(hoverDestination, 180f))
                Projectile.velocity *= 0.8f;

            // Use flying frames.
            Projectile.frameCounter++;
            Projectile.frame = (int)Round(Lerp(6f, 11f, Projectile.frameCounter / 25f % 1f));

            // Prevent natural tile collision.
            Projectile.tileCollide = false;

            // Decide rotation and direction.
            Projectile.rotation = Projectile.velocity.X * 0.017f;
            if (Distance(hoverDestination.X, Projectile.Center.X) >= 30f)
                Projectile.spriteDirection = (Projectile.velocity.X < 0f).ToDirectionInt();
            else
                Projectile.spriteDirection = -Owner.direction;

            // Return to walking if close to the target and ground.
            // Fly towards the owner if they're super far away or if there are obstacles in the way.
            if (!Collision.CanHitLine(Projectile.Center, 1, 1, Projectile.Center + Vector2.UnitY * 300f, 1, 1) && Projectile.WithinRange(hoverDestination, 240f) && !Collision.SolidCollision(Projectile.TopLeft, Projectile.width, Projectile.height))
            {
                AIState = AsterAIState.WalkToOwner;
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

                SaySnarkyComment(Language.GetTextValue($"Mods.InfernumMode.PetDialog.AsterPet{Main.rand.Next(1, 5)}"));
            }

            // Use petting frames.
            if (Time >= 3f)
            {
                Projectile.frameCounter++;
                Projectile.frame = (int)Round(Lerp(6f, 11f, Projectile.frameCounter / 25f % 1f));
                Projectile.spriteDirection = -Owner.direction;
            }

            // Sit down as usual again if the owner stopped giving pets.
            if (Main.myPlayer == Projectile.owner && Owner.Infernum_Pet().ProjectileThatsBeingPetted != Projectile.whoAmI)
            {
                Owner.Infernum_Pet().StopPetAnimation();
                AIState = AsterAIState.SitInPlace;
                Time = 0f;
                Projectile.netUpdate = true;
                return;
            }

            Player.CompositeArmStretchAmount stretch = Player.CompositeArmStretchAmount.ThreeQuarters;
            if (Owner.miscCounter % 14 >= 7)
                stretch = Player.CompositeArmStretchAmount.Full;
            Owner.SetCompositeArmBack(true, stretch, -TwoPi * Owner.direction * 0.27f);
        }

        public void HandlePetVariables()
        {
            PetsPlayer modPlayer = Owner.Infernum_Pet();
            if (Owner.dead)
                modPlayer.AsterPet = false;
            if (modPlayer.AsterPet)
                Projectile.timeLeft = 2;
        }

        // Prevent dying when touching tiles.
        public override bool OnTileCollide(Vector2 oldVelocity) => false;

        // Fall through tiles if far enough from the owner.
        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            fallThrough = !Projectile.WithinRange(Owner.Center, 180f);
            return true;
        }

        // Yes this is shit. Yes this is intended. Don't ask.
        public override bool PreDraw(ref Color lightColor)
        {
            Projectile.position.X -= 5f;
            return true;
        }

        public override void PostDraw(Color lightColor)
        {
            Projectile.position.X += 5f;
        }
    }
}
