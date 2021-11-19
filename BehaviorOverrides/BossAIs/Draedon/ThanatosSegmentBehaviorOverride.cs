using CalamityMod;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
    public class ThanatosBody1BehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<ThanatosBody1>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame;

        public override bool PreAI(NPC npc)
        {
            DoSegmentAI(npc);
            return false;
        }

        public static void DoSegmentAI(NPC npc)
        {
            // Reset frame states.
            ref float frameType = ref npc.localAI[0];
            ref float segmentAttackIndex = ref npc.ai[0];
            frameType = (int)ThanatosHeadBehaviorOverride.ThanatosFrameType.Closed;

            // Die if the ahead segment is not present.
            if (!Main.npc.IndexInRange((int)npc.ai[1]) || !Main.npc[(int)npc.ai[1]].active)
            {
                npc.active = false;
                return;
            }

            // Define rotation and direction.
            NPC aheadSegment = Main.npc[(int)npc.ai[1]];
            Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
            if (aheadSegment.rotation != npc.rotation)
                directionToNextSegment = directionToNextSegment.RotatedBy(MathHelper.WrapAngle(aheadSegment.rotation - npc.rotation) * 0.08f);

            npc.rotation = directionToNextSegment.ToRotation() + MathHelper.PiOver2;
            npc.Center = aheadSegment.Center - directionToNextSegment.SafeNormalize(Vector2.Zero) * npc.width * npc.scale;
            npc.spriteDirection = (directionToNextSegment.X > 0).ToDirectionInt();

            // Locate the head as an NPC.
            NPC head = Main.npc[npc.realLife];

            // Shamelessly steal variables from the head.
            npc.Opacity = head.Opacity;
            npc.target = head.target;
            Player target = Main.player[npc.target];

            // Handle open behavior and frames.
            bool canBeOpen = (int)Math.Round(segmentAttackIndex % (ThanatosHeadBehaviorOverride.SegmentCount / head.Infernum().ExtraAI[0])) == 0;
            if (head.ai[0] != (int)ThanatosHeadBehaviorOverride.ThanatosHeadAttackType.AggressiveCharge && head.Infernum().ExtraAI[2] > 0f && canBeOpen)
            {
                frameType = (int)ThanatosHeadBehaviorOverride.ThanatosFrameType.Open;
                npc.frameCounter = Utils.InverseLerp(0f, head.Infernum().ExtraAI[2] * 0.5f, head.Infernum().ExtraAI[3], true);
                npc.frameCounter *= Utils.InverseLerp(head.Infernum().ExtraAI[2], head.Infernum().ExtraAI[2] * 0.5f, head.Infernum().ExtraAI[3], true);
                npc.frameCounter = (int)Math.Round(npc.frameCounter * (Main.npcFrameCount[npc.type] - 1f));
                float segmentAdjustedTimer = head.Infernum().ExtraAI[3] * segmentAttackIndex / ThanatosHeadBehaviorOverride.SegmentCount;

                if (head.Infernum().ExtraAI[3] == (int)(head.Infernum().ExtraAI[2] / 2 + segmentAdjustedTimer))
                {
                    SoundEffectInstance sound = Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/LaserCannon"), npc.Center);
                    if (sound != null)
                        sound.Volume *= 0.25f;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int type = ModContent.ProjectileType<CannonLaser>();
                        float predictionAmt = 21f;

                        // Predictive laser.
                        Vector2 projectileDestination = target.Center + target.velocity * predictionAmt;
                        int laser = Utilities.NewProjectileBetter(npc.Center, npc.SafeDirectionTo(projectileDestination) * 7f, type, 550, 0f, Main.myPlayer, 0f, npc.whoAmI);
                        if (Main.projectile.IndexInRange(laser))
                        {
                            Main.projectile[laser].owner = npc.target;
                            Main.projectile[laser].ai[1] = npc.whoAmI;
                        }

                        // Opposite laser.
                        projectileDestination = target.Center - target.velocity * predictionAmt;
                        laser = Utilities.NewProjectileBetter(npc.Center, npc.SafeDirectionTo(projectileDestination) * 7f, type, 550, 0f, Main.myPlayer, 0f, npc.whoAmI);
                        if (Main.projectile.IndexInRange(laser))
                        {
                            Main.projectile[laser].owner = npc.target;
                            Main.projectile[laser].ai[1] = npc.whoAmI;
                        }
                    }
                }
            }
            else
                npc.frameCounter = 0f;
            
            // Handle smoke venting and open/closed DR.
            npc.Calamity().DR = ThanatosHeadBehaviorOverride.ClosedSegmentDR;
            npc.Calamity().unbreakableDR = true;
            npc.chaseable = false;
            npc.defense = 0;
            npc.takenDamageMultiplier = 1f;

            if (npc.type == ModContent.NPCType<ThanatosBody1>())
                npc.ModNPC<ThanatosBody1>().SmokeDrawer.ParticleSpawnRate = 9999999;
            if (npc.type == ModContent.NPCType<ThanatosBody2>())
                npc.ModNPC<ThanatosBody2>().SmokeDrawer.ParticleSpawnRate = 9999999;
            if (npc.type == ModContent.NPCType<ThanatosTail>())
                npc.ModNPC<ThanatosTail>().SmokeDrawer.ParticleSpawnRate = 9999999;
            if (frameType == (int)ThanatosHeadBehaviorOverride.ThanatosFrameType.Open)
            {
                // Emit light.
                Lighting.AddLight(npc.Center, 0.35f * npc.Opacity, 0.05f * npc.Opacity, 0.05f * npc.Opacity);

                // Emit smoke.
                npc.takenDamageMultiplier = 18.115f;
                if (npc.type == ModContent.NPCType<ThanatosBody1>())
                {
                    npc.ModNPC<ThanatosBody1>().SmokeDrawer.BaseMoveRotation = npc.rotation - MathHelper.PiOver2;
                    npc.ModNPC<ThanatosBody1>().SmokeDrawer.ParticleSpawnRate = 5;
                }
                if (npc.type == ModContent.NPCType<ThanatosBody2>())
                {
                    npc.ModNPC<ThanatosBody2>().SmokeDrawer.BaseMoveRotation = npc.rotation - MathHelper.PiOver2;
                    npc.ModNPC<ThanatosBody2>().SmokeDrawer.ParticleSpawnRate = 5;
                }
                if (npc.type == ModContent.NPCType<ThanatosTail>())
                {
                    npc.ModNPC<ThanatosTail>().SmokeDrawer.BaseMoveRotation = npc.rotation - MathHelper.PiOver2;
                    npc.ModNPC<ThanatosTail>().SmokeDrawer.ParticleSpawnRate = 5;
                }
                npc.Calamity().DR = ThanatosHeadBehaviorOverride.OpenSegmentDR;
                npc.Calamity().unbreakableDR = false;
                npc.chaseable = true;
            }
            // Emit light.
            else
                Lighting.AddLight(npc.Center, 0.05f * npc.Opacity, 0.2f * npc.Opacity, 0.2f * npc.Opacity);

            // Become vulnerable on the map.
            npc.modNPC.GetType().GetField("vulnerable", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(npc.modNPC, frameType == (int)ThanatosHeadBehaviorOverride.ThanatosFrameType.Open);
        }
        
        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frame.Y = (int)(npc.frameCounter * frameHeight);
        }
    }

    public class ThanatosBody2BehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<ThanatosBody1>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame;

        public override bool PreAI(NPC npc)
        {
            ThanatosBody1BehaviorOverride.DoSegmentAI(npc);
            return false;
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frame.Y = (int)(npc.frameCounter * frameHeight);
        }
    }

    public class ThanatosTailBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<ThanatosTail>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame;

        public override bool PreAI(NPC npc)
        {
            ThanatosBody1BehaviorOverride.DoSegmentAI(npc);
            return false;
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frame.Y = (int)(npc.frameCounter * frameHeight);
        }
    }
}
