using InfernumMode.OverridingSystem;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod.NPCs.Perforator;
using InfernumMode.BehaviorOverrides.BossAIs.Perforators;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using System;
using Terraria.Audio;
using CalamityMod;

namespace InfernumMode.BehaviorOverrides.BossAIs.Cultist
{
    public class SmallPerforatorHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<PerforatorHeadSmall>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            int fallingIchorCount = 12;
            ref float attackTimer = ref npc.Infernum().ExtraAI[0];
            ref float hasReleasedFallingIchor = ref npc.Infernum().ExtraAI[1];

            // Create segments.
            if (npc.localAI[3] == 0f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    PerforatorHiveBehaviorOverride.CreateSegments(npc, 12, ModContent.NPCType<PerforatorBodySmall>(), ModContent.NPCType<PerforatorTailSmall>());

                npc.localAI[3] = 1f;
            }

            // Fuck off if the hive is dead.
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.perfHive) || !Main.npc[CalamityGlobalNPC.perfHive].active)
            {
                npc.active = false;
                return false;
            }

            float wrappedAttackTimer = attackTimer % 330f;
            npc.target = Main.npc[CalamityGlobalNPC.perfHive].target;
            Player target = Main.player[npc.target];

            // Fade in.
            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.1f, 0f, 1f);

            // Attempt to hover below the target.
            if (wrappedAttackTimer < 150f)
            {
                // Reset the falling ichor flag for later.
                hasReleasedFallingIchor = 0f;

                float xDamp = Utils.Remap(Math.Abs(Vector2.Dot(npc.velocity.SafeNormalize(Vector2.Zero), Vector2.UnitX)), 0f, 1f, 0.3f, 1f);
                float yDamp = Utils.Remap(Math.Abs(Vector2.Dot(npc.velocity.SafeNormalize(Vector2.Zero), Vector2.UnitY)), 0f, 1f, 0.3f, 1f);
                Vector2 flyDestination = target.Center + Vector2.UnitY * 550f;
                Vector2 velocityStep = npc.SafeDirectionTo(flyDestination) * new Vector2(xDamp, yDamp) * 0.8f;
                npc.velocity = (npc.velocity + velocityStep).ClampMagnitude(0f, 20f);

                if (MathHelper.Distance(npc.Center.X, target.Center.X) > 500f)
                {
                    npc.velocity.X *= 0.9f;
                    npc.position.X += npc.SafeDirectionTo(target.Center).X * 12f;
                }
            }

            // Play and indicator sound and make the screen shake to tell the player that the worm is ready to lunge upward.
            if (wrappedAttackTimer == 180f)
            {
                target.Calamity().GeneralScreenShakePower = 5f;
                SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact, target.Center);
                SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode, target.Center);
            }

            // Rise upward.
            if (wrappedAttackTimer is >= 180f and < 255f)
			{
                npc.velocity.X *= 0.96f;
                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y - 0.55f, -14.5f, 15f);

                // Release ichor into the air once above the target and in air.
                if (hasReleasedFallingIchor == 0f && npc.Top.Y < target.Bottom.Y && !Collision.SolidCollision(npc.position, npc.width, npc.height))
                {
                    SoundEngine.PlaySound(SoundID.NPCHit20, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient && !npc.WithinRange(target.Center, 170f))
                    {
                        for (int i = 0; i < fallingIchorCount; i++)
                        {
                            float projectileOffsetInterpolant = i / (float)(fallingIchorCount - 1f);
                            float horizontalSpeed = MathHelper.Lerp(-21f, 21f, projectileOffsetInterpolant) + Main.rand.NextFloatDirection() / fallingIchorCount * 6f;
                            float verticalSpeed = Main.rand.NextFloat(-12f, -11f);
                            Vector2 ichorVelocity = new(horizontalSpeed, verticalSpeed);
                            Utilities.NewProjectileBetter(npc.Top + Vector2.UnitY * 10f, ichorVelocity, ModContent.ProjectileType<FallingIchor>(), 80, 0f);
                        }
                        npc.netUpdate = true;
                    }
                    hasReleasedFallingIchor = 1f;
                }
			}

            // Fall.
            if (wrappedAttackTimer >= 255f)
            {
                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + 0.16f, -14.5f, 10f);
            }

            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
            attackTimer++;

            return false;
        }
    }
}
