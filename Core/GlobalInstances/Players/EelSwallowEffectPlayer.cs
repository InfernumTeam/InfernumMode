using CalamityMod.NPCs.Abyss;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Players
{
    public class EelSwallowEffectPlayer : ModPlayer
    {
        public int EelSwallowIndex = -1;

        public override void PostUpdate()
        {
            // Handle eel swallow behaviors.
            if (EelSwallowIndex >= 0 && Main.npc[EelSwallowIndex].active && Main.npc[EelSwallowIndex].type == ModContent.NPCType<GulperEelHead>() && !Collision.SolidCollision(Player.TopLeft, Player.width, Player.height))
            {
                // Be completely invisible when stuck, so as to give the illusion that they're inside of the eel.
                Player.immuneAlpha = 260;

                // Stick to the Gulper eel's mouth, changing the player's field of view.
                Player.Center = Main.npc[EelSwallowIndex].Center;
                Player.velocity = Vector2.Zero;

                Player.mount?.Dismount(Player);
            }

            // Reset the swallow index if it's no longer applicable.
            else if (EelSwallowIndex != -1)
                EelSwallowIndex = -1;
        }
    }
}