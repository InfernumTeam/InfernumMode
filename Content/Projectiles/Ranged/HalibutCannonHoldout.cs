using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Ranged
{
    public class HalibutCannonHoldout : ModProjectile
    {
        public ref Player Owner => ref Main.player[Projectile.owner];

        public ref float Time => ref Projectile.ai[0];

        public static int InitialFireDelayTime => 90;

        public override void SetDefaults()
        {
            Projectile.width = 118;
            Projectile.height = 56;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.timeLeft = 7200;
            Projectile.penetrate = -1;
            Projectile.hide = true;
            Projectile.Opacity = 0;
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

            // Fade in.
            if (Time < 20)
                Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.1f, 0f, 1f);

            // Stick to the owner.
            Vector2 ownerCenter = Owner.RotatedRelativePoint(Owner.MountedCenter, true);
            Projectile.Center = ownerCenter;
            AdjustPlayerValues();

            float holdDistance = 20f;
            Vector2 aimDirection = Owner.SafeDirectionTo(Main.MouseWorld);
            Projectile.rotation = aimDirection.ToRotation();

            if (Time > InitialFireDelayTime)
                FireStuff();
            Time++;
        }

        public void AdjustPlayerValues()
        {
            Projectile.timeLeft = 2;
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
            Owner.itemRotation = (Projectile.direction * Projectile.velocity).ToRotation();

            if (Main.myPlayer == Projectile.owner)
            {
                if (Projectile.velocity != Projectile.oldVelocity)
                    Projectile.netUpdate = true;
                Projectile.spriteDirection = Owner.direction;
            }

            Owner.ChangeDir(Projectile.spriteDirection);

            // Update the player's arm directions to make it look as though they're holding the flamethrower.
            float frontArmRotation = -Projectile.velocity.ToRotation() + Owner.direction * -0.4f;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, frontArmRotation);
        }

        public void FireStuff()
        {
            float fireRate = 30f;
            float speed = 30f;
            Vector2 muzzelPos = Projectile.Center;
            if (Time % fireRate == fireRate - 1f)
            {
                Vector2 velocity = muzzelPos.DirectionTo(Main.MouseScreen) * speed;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(muzzelPos, velocity, ProjectileID.Bullet, Owner.HeldItem.damage, 0f);
            }
        }

        public override bool? CanDamage() => false;

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => overPlayers.Add(index);

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            SpriteEffects direction = SpriteEffects.None;
            if (Owner.direction == -1)
                direction = SpriteEffects.FlipHorizontally;

            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, direction, 0f);
            return false;
        }
    }
}
