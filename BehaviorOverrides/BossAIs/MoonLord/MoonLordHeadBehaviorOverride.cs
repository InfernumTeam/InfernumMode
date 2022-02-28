using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class MoonLordHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.MoonLordHead;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            // Disappear if the body is not present.
            if (!Main.npc.IndexInRange((int)npc.ai[3]) || !Main.npc[(int)npc.ai[3]].active)
            {
                npc.active = false;
                return false;
            }

            // Define the core NPC and inherit properties from it.
            NPC core = Main.npc[(int)npc.ai[3]];

            npc.target = core.target;
            npc.dontTakeDamage = false;

            float attackTimer = core.ai[1];
            Player target = Main.player[npc.target];
            ref float pupilRotation = ref npc.localAI[0];
            ref float pupilOutwardness = ref npc.localAI[1];
            ref float pupilScale = ref npc.localAI[2];

            int idealFrame = 0;

            // Glue the head above the body.
            npc.velocity = Vector2.Zero;
            npc.Center = core.Center - Vector2.UnitY * 400f;

            switch ((MoonLordCoreBehaviorOverride.MoonLordAttackState)(int)core.ai[0])
            {
                case MoonLordCoreBehaviorOverride.MoonLordAttackState.PhantasmalBoltEyeBursts:
                    DoBehavior_PhantasmalBoltEyeBursts(npc, core, target, attackTimer, ref pupilRotation, ref pupilOutwardness, ref pupilScale, ref idealFrame);
                    break;
            }

            // Handle frames.
            int idealFrameCounter = idealFrame * 7;
            if (idealFrameCounter > npc.frameCounter)
                npc.frameCounter += 1D;
            if (idealFrameCounter < npc.frameCounter)
                npc.frameCounter -= 1D;
            npc.frameCounter = MathHelper.Clamp((float)npc.frameCounter, 0f, 21f);

            return false;
        }

        public static void DoBehavior_PhantasmalBoltEyeBursts(NPC npc, NPC core, Player target, float attackTimer, ref float pupilRotation, ref float pupilOutwardness, ref float pupilScale, ref int idealFrame)
        {
            int initialShootDelay = 72;
            int boltBurstCount = 8;
            int boltShootDelay = 32;
            int circularSpreadBoltCount = 12;
            int randomBurstBoltCount = 6;
            float boltShootSpeed = 5f;
            float wrappedAttackTimer = (attackTimer - initialShootDelay) % boltShootDelay;

            // Have a small delay prior to attacking.
            if (attackTimer < initialShootDelay)
            {
                idealFrame = 3;
                return;
            }

            idealFrame = 0;

            Vector2 pupilPosition = npc.Center + Utils.Vector2FromElipse(pupilRotation.ToRotationVector2(), new Vector2(27f, 59f) * pupilOutwardness);

            // Create dust telegraphs prior to firing.
            if (wrappedAttackTimer < boltShootDelay * 0.7f)
            {
                int dustCount = (int)MathHelper.Lerp(1f, 4f, attackTimer / boltShootDelay / 0.7f);
                for (int i = 0; i < dustCount; i++)
                {
                    if (!Main.rand.NextBool(6))
                        continue;

                    Vector2 dustMoveDirection = Main.rand.NextVector2Unit();
                    Vector2 dustSpawnPosition = pupilPosition + dustMoveDirection * 8f;
                    Dust electricity = Dust.NewDustPerfect(dustSpawnPosition, 267);
                    electricity.color = Color.Lerp(Color.Cyan, Color.Wheat, Main.rand.NextFloat());
                    electricity.velocity = dustMoveDirection * 3.6f;
                    electricity.scale = 1.25f;
                    electricity.noGravity = true;

                    if (dustCount >= 3)
                        electricity.scale *= 1.5f;
                }
            }

            float pupilDilationInterpolant = Utils.InverseLerp(0f, 0.7f, attackTimer, true) * 0.5f + Utils.InverseLerp(0.7f, 1f, attackTimer, true) * 0.5f;
            pupilRotation = pupilRotation.AngleLerp(npc.AngleTo(target.Center), 0.1f);
            pupilScale = MathHelper.Lerp(0.3f, 0.75f, pupilDilationInterpolant);
            pupilOutwardness = MathHelper.Lerp(pupilOutwardness, 0.65f, 0.1f);

            // Create a burst of phantasmal bolts after the telegraph completes.
            if (wrappedAttackTimer == boltShootDelay - 1f)
            {
                // TODO - Play a sound to accompany the bolt shots.

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float circularSpreadOffsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                    for (int i = 0; i < circularSpreadBoltCount; i++)
                    {
                        Vector2 boltShootVelocity = (MathHelper.TwoPi * i / circularSpreadBoltCount + circularSpreadOffsetAngle).ToRotationVector2() * boltShootSpeed;
                        Utilities.NewProjectileBetter(pupilPosition, boltShootVelocity, ProjectileID.PhantasmalBolt, 200, 0f);
                    }

                    for (int i = 0; i < randomBurstBoltCount; i++)
                    {
                        Vector2 boltShootVelocity = npc.SafeDirectionTo(target.Center) * boltShootSpeed * 1.4f;
                        boltShootVelocity += Main.rand.NextVector2Circular(2.4f, 2.4f);
                        Utilities.NewProjectileBetter(pupilPosition, boltShootVelocity, ProjectileID.PhantasmalBolt, 200, 0f);
                    }
                }
            }

            if (attackTimer >= boltShootDelay * boltBurstCount)
                core.Infernum().ExtraAI[0] = 1f;
        }
    }
}
