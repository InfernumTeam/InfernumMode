using CalamityMod;
using CalamityMod.NPCs.Abyss;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.AbyssAIs
{
    public class OarfishBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<OarfishHead>();

        #region AI and Behaviors

        public const int SegmentCount = 48;

        public override bool PreAI(NPC npc)
        {
            // Ensure that the oarfish can target critters.
            npc.Infernum().IsAbyssPredator = true;
            NPCID.Sets.UsesNewTargetting[npc.type] = true;

            int chargeDelay = 67;
            int chargeRepositionTime = 8;
            int chargeTime = 45;
            float chargeSpeed = 23f;
            ref float hasNoticedPlayer = ref npc.Infernum().ExtraAI[0];
            ref float hoverOffsetAngle = ref npc.Infernum().ExtraAI[1];
            ref float circleTimer = ref npc.Infernum().ExtraAI[2];
            ref float chargeTimer = ref npc.Infernum().ExtraAI[3];

            // Pick a target if a valid one isn't already decided.
            Utilities.TargetClosestAbyssPredator(npc, false, 500f, 1500f);
            NPCAimedTarget target = npc.GetTargetData();

            // Initialize.
            if (npc.localAI[0] == 0f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    SpawnSegments(npc);

                npc.RemoveWaterSlowness();
                npc.localAI[0] = 1f;
                npc.netUpdate = true;
            }

            // Disable knockback and go through tiles.
            npc.knockBackResist = 0f;
            npc.noTileCollide = true;

            // Reset damage.
            npc.damage = 0;

            // Swim around slowly if no target was found.
            bool insideTiles = Collision.SolidCollision(npc.TopLeft, npc.width, npc.height);
            bool targetInLineOfSight = Collision.CanHitLine(npc.TopLeft, npc.width, npc.height, target.Position, target.Width, target.Height) || insideTiles;
            bool canAttackTarget = npc.WithinRange(target.Center, hasNoticedPlayer == 1f && target.Type == NPCTargetType.Player ? 900f : 480f) && targetInLineOfSight;
            if (!canAttackTarget)
            {
                if (npc.velocity.Length() < 2f)
                    npc.velocity.Y -= 0.36f;
                npc.velocity = (npc.velocity.RotatedBy(0.01f) * 1.01f).ClampMagnitude(1f, 4f);
                npc.rotation = npc.velocity.ToRotation() + PiOver2;
                return false;
            }

            if (target.Type == NPCTargetType.Player)
                hasNoticedPlayer = 1f;

            // Attempt to circle around the target before striking.
            if (chargeTimer <= 0f)
            {
                float spinRadius = 250f;
                List<NPC> segments = GetSegments(npc);
                float circleSimilarityError = EvaluateCircleSimilarity(segments, target.Center, spinRadius);
                bool isDecentlyCircular = circleSimilarityError < 0.44f;

                // Attempt to circle around the target.
                Vector2 hoverDestination = target.Center + hoverOffsetAngle.ToRotationVector2() * spinRadius;
                npc.Center = npc.Center.MoveTowards(hoverDestination, Utils.Remap(circleTimer, chargeDelay - 25f, chargeDelay, 16f, 1f));
                npc.velocity *= 0.9f;

                if (isDecentlyCircular)
                    circleTimer++;
                if (circleTimer >= chargeDelay)
                {
                    circleTimer = 0f;
                    chargeTimer = 1f;
                    npc.netUpdate = true;
                }
                hoverOffsetAngle += TwoPi / 180f;
            }
            else
            {
                // Do damage.
                npc.damage = npc.defDamage;

                if (chargeTimer < chargeRepositionTime)
                {
                    npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * chargeSpeed, 0.084f);
                    if (chargeTimer == 1f)
                        SoundEngine.PlaySound(SoundID.Item92, npc.Center);
                }

                chargeTimer++;
                if (chargeTimer >= chargeTime)
                {
                    chargeTimer = 0f;
                    npc.netUpdate = true;
                }

                hoverOffsetAngle = -npc.velocity.ToRotation();
            }

            npc.rotation = (npc.position - npc.oldPosition).ToRotation() + PiOver2;

            return false;
        }

        public static void SpawnSegments(NPC npc)
        {
            int previousSegment = npc.whoAmI;
            for (int i = 0; i < SegmentCount; i++)
            {
                int nextSegmentIndex;
                if (i < SegmentCount - 1)
                    nextSegmentIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<OarfishBody>(), npc.whoAmI);
                else
                    nextSegmentIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<OarfishTail>(), npc.whoAmI);
                Main.npc[nextSegmentIndex].realLife = npc.whoAmI;
                Main.npc[nextSegmentIndex].ai[2] = npc.whoAmI;
                Main.npc[nextSegmentIndex].ai[1] = previousSegment;
                Main.npc[previousSegment].ai[0] = nextSegmentIndex;

                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, nextSegmentIndex, 0f, 0f, 0f, 0);

                previousSegment = nextSegmentIndex;
            }
        }

        public static List<NPC> GetSegments(NPC npc)
        {
            List<NPC> segments = new();

            // Loop to find all previous segments in the worm linked list until the tail is reached.
            int segmentIndex = (int)npc.ai[0];
            while (segmentIndex >= 0 && segmentIndex < Main.maxNPCs && Main.npc[segmentIndex].active && (Main.npc[segmentIndex].type == ModContent.NPCType<OarfishBody>()))
            {
                segments.Add(Main.npc[segmentIndex]);
                segmentIndex = (int)Main.npc[segmentIndex].ai[0];
            }

            return segments;
        }

        public static float EvaluateCircleSimilarity(List<NPC> segments, Vector2 centerOfMass, float circleRadius)
        {
            if (segments.Count <= 0)
                return 1f;

            float error = 0f;
            float currentAngle = 0f;
            float headOffsetAngle = segments.First().AngleFrom(centerOfMass);
            foreach (NPC n in segments)
            {
                Vector2 position = n.Center;

                // Penalize deviations from the ideal radius offset.
                float distanceFromCenter = position.Distance(centerOfMass);
                float deviationFromRadius = Distance(distanceFromCenter, circleRadius);
                float radiusError = 1f - Exp(-3f / circleRadius * deviationFromRadius);
                if (deviationFromRadius < 30f)
                    radiusError = 0f;

                // Penalize not being in the ideal angular position.

                // The circumferance of a circle = 2pi * r. If laid down as a line, that is how long the line would be.
                // The step of each segment on the line is exactly equal to how tall it is. This is how much it should "walk" on the imaginary line before
                // moving to the next segment.
                // Converting this to an angle (since we want to go across the entire circle), the step becomes 2pi * h / circumferance.
                // 2pi * h / (2pi * r) =
                // h / r, negating the need to explicitly calculate the circumferance.
                currentAngle += n.height * n.scale / circleRadius;

                // The angle calculations work under the assumption that the start of the circle is at an angle of 0. However, this is not true, since the head should be
                // allowed to move.
                // As such, all of this is offset based on whatever angle the head is at compared to the center of mass.
                Vector2 directionFromCenter = (n.Center - centerOfMass).SafeNormalize(Vector2.UnitY);
                Vector2 idealDirectionFromCenter = (currentAngle + headOffsetAngle).ToRotationVector2();
                float angleError = Utils.GetLerpValue(Vector2.Dot(directionFromCenter, idealDirectionFromCenter), 1f, 0.67f, true);

                // Collect both errors.
                error += (radiusError + angleError) * 0.5f;
            }

            // Average all errors, to make final calculations not reliant on how many segments there are.
            return error / segments.Count;
        }
        #endregion AI and Behaviors
    }
}
