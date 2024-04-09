using CalamityMod.Events;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Cultist
{
    public class PhantasmDragonHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.CultistDragonHead;

        #region AI
        public override bool PreAI(NPC npc)
        {
            ref float attackTimer = ref npc.Infernum().ExtraAI[0];
            ref float openMouthFlag = ref npc.localAI[2];

            // Make a roar sound on summoning.
            if (npc.localAI[3] == 0f)
            {
                SoundEngine.PlaySound(SoundID.Item119, npc.position);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    CreatePhantasmDragonSegments(npc, 40);

                npc.localAI[3] = 1f;
            }

            npc.dontTakeDamage = npc.alpha > 0;
            npc.alpha = Utils.Clamp(npc.alpha - 42, 0, 255);
            npc.spriteDirection = (npc.velocity.X < 0f).ToDirectionInt();

            attackTimer++;

            if (!Main.player.IndexInRange(npc.target) || !Main.player[npc.target].active || Main.player[npc.target].dead)
            {
                npc.TargetClosest();

                // Fly into the sky and disappear if no target exists.
                if (!Main.player.IndexInRange(npc.target) || !Main.player[npc.target].active || Main.player[npc.target].dead)
                {
                    if (npc.timeLeft > 180)
                        npc.timeLeft = 180;
                    npc.velocity = Vector2.Lerp(npc.velocity, -Vector2.UnitY * (npc.velocity.Length() + 1.5f), 0.06f);
                    npc.rotation = npc.velocity.ToRotation() + PiOver2;
                    return false;
                }
            }

            // Disappear if the tail is missing.
            if (!NPC.AnyNPCs(NPCID.CultistDragonTail))
                npc.active = false;

            Player target = Main.player[npc.target];
            if (attackTimer % 300f > 210f)
            {
                // If close to the target, speed up. Otherwise attempt to rotate towards them.
                if (!npc.WithinRange(target.Center, 280f))
                {
                    float newSpeed = Lerp(npc.velocity.Length(), BossRushEvent.BossRushActive ? 33f : 23.75f, 0.05f);
                    npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.0415f, true) * newSpeed;
                }
                else if (npc.velocity.Length() < 24f)
                    npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * Lerp(npc.velocity.Length() + 0.1f, BossRushEvent.BossRushActive ? 43f : 33f, 0.06f);

                openMouthFlag = 1f;

                // Release bursts of shadow fireballs at the target.
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 24f == 23f)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 fireballVelocity = npc.velocity.SafeNormalize(Vector2.UnitY) * 7.8f;
                        fireballVelocity = fireballVelocity.RotateTowards(npc.AngleTo(target.Center), Pi / 5f).RotatedByRandom(0.16f);
                        fireballVelocity = fireballVelocity.RotatedBy(Lerp(-0.27f, 0.27f, i / 2f));

                        int fireball = Utilities.NewProjectileBetter(npc.Center + fireballVelocity, fireballVelocity, ProjectileID.CultistBossFireBallClone, CultistBehaviorOverride.ShadowFireballDamage, 0f);
                        if (!Main.projectile.IndexInRange(fireball))
                            Main.projectile[fireball].tileCollide = false;
                    }
                }
            }
            else
            {
                openMouthFlag = ((npc.rotation - PiOver2).AngleTowards(npc.AngleTo(target.Center), 0.84f) == npc.AngleTo(target.Center)).ToInt();
                openMouthFlag = (int)openMouthFlag & npc.WithinRange(target.Center, 300f).ToInt();

                float newSpeed = npc.velocity.Length();
                if (newSpeed < 11f)
                    newSpeed += 0.045f;

                if (newSpeed > 16f)
                    newSpeed -= 0.045f;

                float angleBetweenDirectionAndTarget = npc.velocity.AngleBetween(npc.SafeDirectionTo(target.Center));
#pragma warning disable IDE0078 // Use pattern matching
                if (angleBetweenDirectionAndTarget < 0.55f && angleBetweenDirectionAndTarget > (Pi / 3f))
                    newSpeed += 0.09f;

                if (angleBetweenDirectionAndTarget < Pi / 3 && angleBetweenDirectionAndTarget > Pi * 0.75f)
                    newSpeed -= 0.0725f;
#pragma warning restore IDE0078 // Use pattern matching

                newSpeed = Clamp(newSpeed, 8.5f, 19f) * (BossRushEvent.BossRushActive ? 1.8f : 1.32f);

                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.04f, true) * newSpeed;
            }

            npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(target.Center) * npc.velocity.Length(), 0.45f);
            npc.rotation = npc.velocity.ToRotation() + PiOver2;
            return false;
        }

        public static void CreatePhantasmDragonSegments(NPC npc, int segmentCount)
        {
            npc.ai[3] = npc.whoAmI;
            npc.realLife = npc.whoAmI;

            int previousSegment = npc.whoAmI;
            for (int i = 0; i < segmentCount; i++)
            {
                // Make every 4th body type an arm type.
                // Otherwise, go with an ordinary type.
                int bodyType = (i - 2) % 4 == 0 ? NPCID.CultistDragonBody1 : NPCID.CultistDragonBody2;
                if (i == segmentCount - 3)
                    bodyType = NPCID.CultistDragonBody3;
                if (i == segmentCount - 2)
                    bodyType = NPCID.CultistDragonBody4;
                if (i == segmentCount - 1)
                    bodyType = NPCID.CultistDragonTail;

                int nextSegment = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.position.X, (int)npc.position.Y, bodyType, previousSegment, 0f, 0f, 0f, 0f, 255);
                Main.npc[nextSegment].ai[3] = npc.whoAmI;
                Main.npc[nextSegment].ai[1] = previousSegment;
                Main.npc[nextSegment].realLife = npc.whoAmI;
                Main.npc[previousSegment].ai[0] = nextSegment;
                previousSegment = nextSegment;

                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, nextSegment, 0f, 0f, 0f, 0, 0, 0);
            }
        }
        #endregion AI

        #region Frames

        public override void FindFrame(NPC npc, int frameHeight)
        {
            int frame = 0;
            if (npc.localAI[2] == 1f)
                frame = 10;

            if (frame > npc.frameCounter)
                npc.frameCounter++;
            if (frame < npc.frameCounter)
                npc.frameCounter--;

            npc.frame.Y = (int)(npc.frameCounter / 5) * frameHeight;
        }
        #endregion Frames
    }
}
