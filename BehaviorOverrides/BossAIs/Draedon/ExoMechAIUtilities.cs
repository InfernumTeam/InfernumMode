using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Ares;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
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
        }

        public static bool PerformingDeathAnimation(NPC npc) => npc.Infernum().ExtraAI[ExoMechManagement.DeathAnimationHasStartedIndex] != 0f;

        public static bool ShouldExoMechVanish(NPC npc)
        {
            NPC finalMech = ExoMechManagement.FindFinalMech();
            NPC checkNPC = npc.realLife >= 0 ? Main.npc[npc.realLife] : npc;

            // If the final mech is present, all other mechs should vanish.
            if (finalMech != null && finalMech != checkNPC)
                return true;

            // If a death animation is ongoing that isn't being performed by the check NPC, all other mechs should vanish.
            if (ExoMechManagement.ExoMechIsPerformingDeathAnimation && !PerformingDeathAnimation(checkNPC))
                return true;

            return false;
        }

        public static void HaveArmsInheritAresBodyAttributes(NPC npc)
        {
            // Do nothing if Ares is not present.
            if (CalamityGlobalNPC.draedonExoMechPrime == -1)
                return;

            // Locate Ares' body as an NPC.
            NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];

            // Define the life ratio.
            npc.life = aresBody.life;
            npc.lifeMax = aresBody.lifeMax;

            // Shamelessly steal variables from Ares.
            npc.target = aresBody.target;
            npc.Opacity = aresBody.Opacity;
            npc.dontTakeDamage = aresBody.dontTakeDamage;

            // Inherit death animation variables from Ares.
            npc.Infernum().ExtraAI[ExoMechManagement.DeathAnimationTimerIndex] = aresBody.Infernum().ExtraAI[ExoMechManagement.DeathAnimationTimerIndex];
            npc.Infernum().ExtraAI[ExoMechManagement.DeathAnimationHasStartedIndex] = aresBody.Infernum().ExtraAI[ExoMechManagement.DeathAnimationHasStartedIndex];
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
            npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.065f);
            currentDirection = npc.rotation;
            if (Math.Sin(currentDirection) < 0f)
                currentDirection += MathHelper.Pi;

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

        public static void DrawAresArmTelegraphEffect(SpriteBatch spriteBatch, NPC npc, Color telegraphColor, Texture2D texture, Vector2 drawCenter, Rectangle frame, Vector2 origin)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            float telegraphGlowInterpolant = npc.ai[0] / npc.ai[1];
            if (telegraphGlowInterpolant >= 1f)
                telegraphGlowInterpolant = 0f;

            if (telegraphGlowInterpolant > 0f)
            {
                float whiteFade = (float)Math.Sin(Main.GlobalTime * 20f) * 0.5f + 0.5f;
                telegraphColor = Color.Lerp(telegraphColor, Color.White, whiteFade);
                telegraphColor *= Utils.InverseLerp(0f, 0.65f, telegraphGlowInterpolant, true) * (float)Math.Pow(Utils.InverseLerp(1f, 0.85f, telegraphGlowInterpolant, true), 2D);

                float backAfterimageOffset = telegraphGlowInterpolant * 10f;
                backAfterimageOffset += Utils.InverseLerp(0.85f, 1f, telegraphGlowInterpolant, true) * 20f;
                for (int i = 0; i < 13; i++)
                {
                    Color color = telegraphColor * 0.6f;
                    color.A = 0;
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 13f).ToRotationVector2() * backAfterimageOffset;
                    Main.spriteBatch.Draw(texture, drawCenter + drawOffset, frame, npc.GetAlpha(color), npc.rotation, origin, npc.scale, spriteEffects, 0f);
                }
            }
        }

        public static void DrawFinalPhaseGlow(SpriteBatch spriteBatch, NPC npc, Texture2D texture, Vector2 drawCenter, Rectangle frame, Vector2 origin)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            float finalPhaseGlowTimer = npc.Infernum().ExtraAI[ExoMechManagement.FinalPhaseTimerIndex];
            if (npc.realLife >= 0)
            {
                finalPhaseGlowTimer = Main.npc[npc.realLife].Infernum().ExtraAI[ExoMechManagement.FinalPhaseTimerIndex];
                if (Main.npc[npc.realLife].type == ModContent.NPCType<AresBody>())
                {
                    float telegraphGlowInterpolant = npc.ai[0] / npc.ai[1];
                    if (telegraphGlowInterpolant >= 1f)
                        telegraphGlowInterpolant = 0f;
                    finalPhaseGlowTimer *= 1f - Utils.InverseLerp(0f, 0.85f, telegraphGlowInterpolant, true) * Utils.InverseLerp(1f, 0.85f, telegraphGlowInterpolant, true);
                }
            }

            float finalPhaseGlowInterpolant = Utils.InverseLerp(0f, ExoMechManagement.FinalPhaseTransitionTime * 0.75f, finalPhaseGlowTimer, true);
            if (finalPhaseGlowInterpolant > 0f)
            {
                float backAfterimageOffset = finalPhaseGlowInterpolant * 6f;
                for (int i = 0; i < 6; i++)
                {
                    Color color = Main.hslToRgb((i / 12f + Main.GlobalTime * 0.6f + npc.whoAmI * 0.54f) % 1f, 1f, 0.56f);
                    color.A = 0;
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 6f + Main.GlobalTime * 0.8f).ToRotationVector2() * backAfterimageOffset;
                    Main.spriteBatch.Draw(texture, drawCenter + drawOffset, frame, npc.GetAlpha(color), npc.rotation, origin, npc.scale, spriteEffects, 0f);
                }
            }
        }
    }
}
