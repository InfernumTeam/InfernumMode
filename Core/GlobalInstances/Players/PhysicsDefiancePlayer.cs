using CalamityMod.Items.Mounts;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Players
{
    public class PhysicsDefiancePlayer : ModPlayer
    {
        public bool PhysicsDefianceIsEnabled
        {
            get;
            set;
        }

        public override void PreUpdateMovement()
        {
            // Remove acceleration when in physics defiance mode.
            if (!PhysicsDefianceIsEnabled || Player.grappling[0] >= 0 || Player.mount.Active)
                return;

            // Grant the player infinite flight time.
            Player.wingTime = Player.wingTimeMax;
            Player.legFrame.Y = -Player.legFrame.Height;

            float speed = DraedonGamerChairMount.MovementSpeed * 2f;
            if (Player.controlLeft)
            {
                Player.velocity.X = -speed;
                Player.direction = -1;
            }
            else if (Player.controlRight)
            {
                Player.velocity.X = speed;
                Player.direction = 1;
            }
            else
                Player.velocity.X = 0f;

            if (Player.controlUp || Player.controlJump)
                Player.velocity.Y = -speed;

            else if (Player.controlDown)
            {
                Player.velocity.Y = speed;
                if (Collision.TileCollision(Player.position, Player.velocity, Player.width, Player.height, true, false, (int)Player.gravDir).Y == 0f)
                    Player.velocity.Y = 0.5f;
            }
            else
                Player.velocity.Y = 0f;
        }

        public void ToggleEffect()
        {
            PhysicsDefianceIsEnabled = !PhysicsDefianceIsEnabled;
        }
    }
}