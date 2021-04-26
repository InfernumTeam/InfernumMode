using CalamityMod;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.EoW
{
	public class EoWAIClass
    {
        public const int TotalLifeAcrossWorm = 23000;
        public const int BodySegmentCount = 80;

		#region AI

		[OverrideAppliesTo(NPCID.EaterofWorldsHead, typeof(EoWAIClass), "EoWHeadAI", EntityOverrideContext.NPCAI)]
        public static bool EoWHeadAI(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float attackTimer = ref npc.ai[1];
            ref float initializedFlag = ref npc.localAI[0];
            ref float bombShootCounter = ref npc.localAI[1];
            ref float fallCountdown = ref npc.Infernum().ExtraAI[5];

            // Perform initialization logic.
            if (Main.netMode != NetmodeID.MultiplayerClient && initializedFlag == 0f)
            {
                CreateSegments(npc, NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail);
                initializedFlag = 1f;
            }

            // Count segments in the air.
            int totalSegmentsInAir = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                bool inAir = !Collision.SolidCollision(Main.npc[i].position, Main.npc[i].width, Main.npc[i].height);
                inAir &= !TileID.Sets.Platforms[CalamityUtils.ParanoidTileRetrieval((int)Main.npc[i].Center.X / 16, (int)Main.npc[i].Center.Y / 16).type];
                if (Main.npc[i].type == NPCID.EaterofWorldsBody && Main.npc[i].active && inAir)
                    totalSegmentsInAir++;
            }

            // Select a new target if an old one was lost.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
            {
                npc.TargetClosest();

                // If no valid targets exist, dig away.
                if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                {
                    DoAttack_Despawn(npc);
                    return false;
                }
            }

            Player target = Main.player[npc.target];

            float segmentsInAirTolerance = MathHelper.Lerp(0.45f, 0.8f, 1f - lifeRatio);

            // Release lingering cursed cinders everywhere.
            if (attackTimer % 480f > 270f && lifeRatio < 0.65f)
            {
                DoMovement(npc, target);

                Vector2 lookDirection = (npc.rotation - MathHelper.PiOver2).ToRotationVector2();
                bool obstacleInWayOfMouth = !Collision.CanHit(npc.Center, 2, 2, npc.Center + lookDirection * 160f, 2, 2);
                if (!npc.WithinRange(target.Center, 400f) && !obstacleInWayOfMouth && attackTimer % 60f == 59f)
                {
                    // Make a belch sound effect.
                    Main.PlaySound(SoundID.NPCDeath13, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            Vector2 cinderVelocity = lookDirection.RotatedByRandom(0.4f) * Main.rand.NextFloat(9f, 15f);
                            Utilities.NewProjectileBetter(npc.Center + cinderVelocity, cinderVelocity, ModContent.ProjectileType<CursedLingeringCinder>(), 65, 0f);
                        }
                    }
                }
            }

            // Fall into the ground.
            else if (fallCountdown > 0f)
            {
                fallCountdown--;

                npc.velocity.X *= 0.985f;
                float distanceFromTarget = npc.Distance(target.Center);
                if ((Collision.SolidCollision(npc.position, npc.width, npc.height) && distanceFromTarget > 620f) || distanceFromTarget > 1050f)
                    DoMovement(npc, target);

                else if (npc.velocity.Y < 12f)
                    npc.velocity.Y += 0.2f;
            }
            else
            {
                DoMovement(npc, target);
                if (totalSegmentsInAir > BodySegmentCount * segmentsInAirTolerance)
                    fallCountdown = 90f;
            }

            // Release cursed flame bombs from the mouth over time.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                bombShootCounter++;
                int shootRate = (int)MathHelper.Lerp(90f, 180f, lifeRatio);

                // Shoot more quickly if looking at the target.
                if (npc.velocity.AngleBetween(npc.SafeDirectionTo(target.Center)) < 0.47f && npc.WithinRange(target.Center, 600f))
                    shootRate = (int)MathHelper.Lerp(shootRate, 35f, 0.65f);

                if (bombShootCounter > shootRate)
                {
                    Utilities.NewProjectileBetter(npc.Center, (npc.rotation - MathHelper.PiOver2).ToRotationVector2() * 13f, ModContent.ProjectileType<CursedBomb>(), 65, 0f);
                    bombShootCounter = 0f;
                    npc.netUpdate = true;
                }
            }

            npc.rotation = npc.rotation.AngleTowards(npc.velocity.ToRotation() + MathHelper.PiOver2, 0.15f);
            attackTimer++;

            return false;
		}

        [OverrideAppliesTo(NPCID.EaterofWorldsBody, typeof(EoWAIClass), "EoWSegmentAI", EntityOverrideContext.NPCAI)]
        [OverrideAppliesTo(NPCID.EaterofWorldsTail, typeof(EoWAIClass), "EoWSegmentAI", EntityOverrideContext.NPCAI)]
        public static bool EoWSegmentAI(NPC npc)
        {
            NPC aheadSegment = Main.npc[(int)npc.ai[1]];
            NPC head = Main.npc[(int)npc.ai[2]];
            if (!aheadSegment.active)
            {
                npc.life = 0;
                npc.HitEffect();
                npc.active = false;
                npc.netUpdate = true;
            }

            // Inherit various attributes from the head segment.
            // This code will go upstream across every segment, until it reaches the head.
            npc.scale = aheadSegment.scale;
            npc.life = head.life;
            npc.lifeMax = head.lifeMax;

            Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
            if (aheadSegment.rotation != npc.rotation)
                directionToNextSegment = directionToNextSegment.RotatedBy(MathHelper.WrapAngle(aheadSegment.rotation - npc.rotation) * 0.04f);

            npc.rotation = directionToNextSegment.ToRotation() + MathHelper.PiOver2;
            npc.Center = aheadSegment.Center - directionToNextSegment.SafeNormalize(Vector2.Zero) * npc.width * npc.scale;

            return false;
        }

        #region AI Utility Methods

        public static void DoAttack_Despawn(NPC npc)
        {
            if (npc.timeLeft > 200)
                npc.timeLeft = 200;
            npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * 16f, 0.06f);
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public static void DoMovement(NPC npc, Player target)
        {
            Vector2 idealVelocity = npc.SafeDirectionTo(target.Center) * 14f;
            idealVelocity.Y *= 1.7f;
            npc.SimpleFlyMovement(idealVelocity, 0.125f);
            npc.velocity = npc.velocity.ClampMagnitude(6f, 25f);
        }

        public static void CreateSegments(NPC npc, int bodyType, int tailType)
		{
            int previousIndex = npc.whoAmI;
            for (int i = 0; i < BodySegmentCount + 1; i++)
            {
                int nextIndex;
                if (i < BodySegmentCount)
                {
                    if (i % 18 == 9)
                        nextIndex = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<WeakenedEaterOfWorldsBody>(), npc.whoAmI);
                    else
                        nextIndex = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, bodyType, npc.whoAmI);
                }
                else
                    nextIndex = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, tailType, npc.whoAmI);


                Main.npc[nextIndex].realLife = npc.whoAmI;
                Main.npc[nextIndex].ai[2] = npc.whoAmI;
                Main.npc[nextIndex].ai[1] = previousIndex;
                Main.npc[previousIndex].ai[0] = nextIndex;

                // Force sync the new segment into existence.
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, nextIndex, 0f, 0f, 0f, 0);

                previousIndex = nextIndex;
            }
        }

		#endregion AI Utility Methods

		#endregion AI
	}
}
