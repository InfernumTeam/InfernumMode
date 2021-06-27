using CalamityMod.NPCs.DesertScourge;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.DesertScourge
{
	public class DesertScourgeHeadSmallBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<DesertScourgeHeadSmall>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            // Select a new target if an old one was lost.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();

            npc.alpha = Utils.Clamp(npc.alpha - 20, 0, 255);

            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Become more aggressive if the other worm is dead.
            bool onlyWormAlive = NPC.CountNPCS(npc.type) == 1;
            if (onlyWormAlive)
                lifeRatio = MathHelper.Clamp(lifeRatio + 0.5f, 0f, 1f);

            ref float initializedFlag = ref npc.Infernum().ExtraAI[1];
            ref float time = ref npc.Infernum().ExtraAI[2];

            if (Main.netMode != NetmodeID.MultiplayerClient && initializedFlag == 0f)
            {
                DesertScourgeHeadBigBehaviorOverride.CreateSegments(npc, 16, ModContent.NPCType<DesertScourgeBodySmall>(), ModContent.NPCType<DesertScourgeTailSmall>());
                initializedFlag = 1f;
                npc.netUpdate = true;
            }

            // If there still was no valid target, dig away.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
            {
                DesertScourgeHeadBigBehaviorOverride.DoAttack_Despawn(npc);
                return false;
            }

            time++;
            Player target = Main.player[npc.target];

            switch ((int)npc.Infernum().ExtraAI[0])
            {
                // Aggressive worm.
                case 0:
                    Vector2 destination = target.Center;

                    // If close to the target, determine the destination based on the current direction of the worm.
                    if (npc.WithinRange(target.Center, 140f))
                        destination += npc.velocity.SafeNormalize(Vector2.UnitY) * 180f;

                    float distanceFromDestination = npc.Distance(destination);
                    float turnSpeed = MathHelper.Lerp(0.008f, 0.055f, Utils.InverseLerp(175f, 475f, distanceFromDestination, true));

                    float newSpeed = npc.velocity.Length();
                    float idealSpeed = MathHelper.Lerp(6.7f, 13f, 1f - lifeRatio);

                    // Accelerate quickly if relatively far from the destination.
                    if (distanceFromDestination > 1250f)
                        newSpeed += 0.06f;

                    // Otherwise slow down if relatively close to the destination.
                    if (distanceFromDestination < 300f)
                        newSpeed -= 0.04f;

                    // Slowly regress back to the ideal speed over time.
                    newSpeed = MathHelper.Lerp(newSpeed, idealSpeed, 0.018f);
                    newSpeed = MathHelper.Clamp(newSpeed, 4f, 21.5f);

                    npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(destination), turnSpeed, true) * newSpeed;
                    break;

                // Sand spewing worm.
                case 1:
                    destination = target.Center;
                    if (npc.WithinRange(destination, 360f))
                        destination += (time / 105f * MathHelper.TwoPi).ToRotationVector2() * 270f;

                    distanceFromDestination = npc.Distance(destination);
                    turnSpeed = MathHelper.Lerp(0.04f, 0.11f, Utils.InverseLerp(175f, 475f, distanceFromDestination, true));

                    newSpeed = npc.velocity.Length();
                    idealSpeed = MathHelper.Lerp(5.5f, 10f, 1f - lifeRatio);

                    // Accelerate quickly if relatively far from the destination.
                    if (distanceFromDestination > 1250f)
                        newSpeed += 0.04f;

                    // Otherwise slow down if relatively close to the destination.
                    if (distanceFromDestination < 300f)
                        newSpeed -= 0.04f;

                    // Slowly regress back to the ideal speed over time.
                    newSpeed = MathHelper.Lerp(newSpeed, idealSpeed, 0.02f);
                    newSpeed = MathHelper.Clamp(newSpeed, 5f, 18f);

                    npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(destination), turnSpeed, true) * newSpeed;

                    // Release sand from the mouth every so often.
                    int sandShootRate = onlyWormAlive ? 90 : 130;
                    if (Main.netMode != NetmodeID.MultiplayerClient && time % sandShootRate == sandShootRate - 1f)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            Vector2 spawnPosition = npc.Center;
                            Vector2 shootVelocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.47f) * Main.rand.NextFloat(7f, 9f);
                            spawnPosition += shootVelocity * 2f;

                            int sand = Utilities.NewProjectileBetter(spawnPosition, shootVelocity, ModContent.ProjectileType<SandBlast>(), 52, 0f);
                            if (Main.projectile.IndexInRange(sand))
                                Main.projectile[sand].tileCollide = false;
                        }
                    }
                    break;
            }
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
            return false;
        }
    }
}
