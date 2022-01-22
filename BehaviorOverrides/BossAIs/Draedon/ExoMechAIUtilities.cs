using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
    public static class ExoMechAIUtilities
    {
        public static void DoSnapHoverMovement(NPC npc, Vector2 destination, float flySpeed, float hyperSpeedCap)
        {
            float distanceFromDestination = npc.Distance(destination);
            float hyperSpeedInterpolant = Utils.InverseLerp(50f, 2400f, distanceFromDestination, true);

            // Scale up velocity over time if too far from destination.
            float speedUpFactor = Utils.InverseLerp(50f, 1600f, npc.Distance(destination), true) * 1.76f;
            flySpeed *= 1f + speedUpFactor;

            // Reduce speed when very close to the destination, to prevent swerving movement.
            if (flySpeed > distanceFromDestination)
                flySpeed = distanceFromDestination;

            // Define the max velocity.
            Vector2 maxVelocity = (destination - npc.Center) / 24f;
            if (maxVelocity.Length() > hyperSpeedCap)
                maxVelocity = maxVelocity.SafeNormalize(Vector2.Zero) * hyperSpeedCap;

            npc.velocity = Vector2.Lerp(npc.SafeDirectionTo(destination) * flySpeed, maxVelocity, hyperSpeedInterpolant);
            if (npc.WithinRange(destination, 30f) && Vector2.Distance(npc.oldPosition + npc.Size * 0.5f, destination) >= 30f)
            {
                npc.Center = destination;
                npc.velocity = Vector2.Zero;
                npc.netUpdate = true;
                npc.netSpam = 0;
            }
        }

        public static Vector2 PerformAresArmDirectioning(NPC npc, NPC aresBody, Player target, Vector2 aimDirection, bool currentlyDisabled, bool doingHoverCharge, ref float currentDirection)
        {
            // Choose a direction and rotation.
            // Rotation is relative to predictiveness.
            float idealRotation = aimDirection.ToRotation();
            if (currentlyDisabled)
                idealRotation = MathHelper.Clamp(npc.velocity.X * -0.016f, -0.81f, 0.81f) + MathHelper.PiOver2;
            if (doingHoverCharge)
                idealRotation = aresBody.velocity.ToRotation() - MathHelper.PiOver2;

            if (npc.spriteDirection == 1)
                idealRotation += MathHelper.Pi;
            if (idealRotation < 0f)
                idealRotation += MathHelper.TwoPi;
            if (idealRotation > MathHelper.TwoPi)
                idealRotation -= MathHelper.TwoPi;
            currentDirection = idealRotation;
            npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.065f);

            int direction = Math.Sign(target.Center.X - npc.Center.X);
            if (direction != 0)
            {
                npc.direction = direction;

                if (npc.spriteDirection != -npc.direction)
                    npc.rotation += MathHelper.Pi;

                npc.spriteDirection = -npc.direction;
            }
            return aimDirection;
        }
    }
}
