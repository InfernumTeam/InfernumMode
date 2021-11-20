using CalamityMod;
using CalamityMod.NPCs.ExoMechs.Thanatos;
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

            // Die if the head is not present.
            if (!Main.npc.IndexInRange((int)npc.realLife) || !Main.npc[(int)npc.realLife].active)
            {
                npc.active = false;
                return;
            }

            // Die if the ahead segment is not present.
            if (!Main.npc.IndexInRange((int)npc.ai[1]) || !Main.npc[(int)npc.ai[1]].active)
            {
                npc.active = false;
                return;
            }

            // Necessary to ensure that vanilla behaviors (specifically on the map) happen.
            npc.ai[2] = npc.realLife;

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
            npc.dontTakeDamage = head.dontTakeDamage;
            Player target = Main.player[npc.target];

            // Handle open behavior and frames.
            ThanatosHeadBehaviorOverride.ThanatosHeadAttackType headAttackType = (ThanatosHeadBehaviorOverride.ThanatosHeadAttackType)(int)head.ai[0];
            float totalSegmentsToFire = head.Infernum().ExtraAI[0];
            bool canBeOpen = (int)Math.Round(segmentAttackIndex % (ThanatosHeadBehaviorOverride.SegmentCount / totalSegmentsToFire)) == 0;
            bool thanatosIsFiring = headAttackType != ThanatosHeadBehaviorOverride.ThanatosHeadAttackType.AggressiveCharge && head.Infernum().ExtraAI[1] > 0f;
            if (thanatosIsFiring && canBeOpen)
            {
                float segmentFireTime = head.Infernum().ExtraAI[1];
                float segmentFireCountdown = head.Infernum().ExtraAI[2];
                int fireDelay = (int)MathHelper.Lerp(segmentFireCountdown * -0.32f, segmentFireCountdown * 0.32f, segmentAttackIndex / ThanatosHeadBehaviorOverride.SegmentCount);
                frameType = (int)ThanatosHeadBehaviorOverride.ThanatosFrameType.Open;
                npc.frameCounter = Utils.InverseLerp(0f, segmentFireTime * 0.5f + fireDelay, segmentFireCountdown, true);
                npc.frameCounter *= Utils.InverseLerp(segmentFireTime, segmentFireTime * 0.5f + fireDelay, segmentFireCountdown, true);
                npc.frameCounter = (int)Math.Round(npc.frameCounter * (Main.npcFrameCount[npc.type] - 1f));

                if (segmentFireCountdown == (int)(segmentFireTime / 2) + fireDelay)
                {
                    bool willShootLaser = headAttackType != ThanatosHeadBehaviorOverride.ThanatosHeadAttackType.ProjectileShooting_GreenLaser || segmentAttackIndex % 2f == 0f;

                    if (willShootLaser)
                    {
                        string soundType = headAttackType == ThanatosHeadBehaviorOverride.ThanatosHeadAttackType.ProjectileShooting_GreenLaser ? "PlasmaCasterFire" : "LaserCannon";
                        SoundEffectInstance sound = Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, $"Sounds/Item/{soundType}"), npc.Center);
                        if (sound != null)
                            sound.Volume *= 0.25f;
                    }
                    SoundEffectInstance ventSound = Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ThanatosVent"), npc.Center);
                    if (ventSound != null)
                        ventSound.Volume *= 0.1f;

                    if (Main.netMode != NetmodeID.MultiplayerClient && willShootLaser)
                    {
                        float generalShootSpeedFactor = 1f;
                        if (ExoMechManagement.CurrentThanatosPhase == 4)
                            generalShootSpeedFactor *= 0.5f;
                        else
                        {
                            if (ExoMechManagement.CurrentThanatosPhase >= 2)
                                generalShootSpeedFactor *= 1.15f;
                            if (ExoMechManagement.CurrentThanatosPhase >= 3)
                                generalShootSpeedFactor *= 1.15f;
                        }
                        
                        switch (headAttackType)
                        {
                            case ThanatosHeadBehaviorOverride.ThanatosHeadAttackType.ProjectileShooting_RedLaser:
                                int type = ModContent.ProjectileType<ThanatosLaser>();
                                float predictionFactor = 21f;
                                float shootSpeed = generalShootSpeedFactor * 11f;

                                // Predictive laser.
                                Vector2 projectileDestination = target.Center + target.velocity * predictionFactor;
                                int laser = Utilities.NewProjectileBetter(npc.Center, npc.SafeDirectionTo(projectileDestination) * shootSpeed, type, 550, 0f, Main.myPlayer, 0f, npc.whoAmI);
                                if (Main.projectile.IndexInRange(laser))
                                {
                                    Main.projectile[laser].owner = npc.target;
                                    Main.projectile[laser].ModProjectile<ThanatosLaser>().InitialDestination = projectileDestination;
                                    Main.projectile[laser].ai[1] = npc.whoAmI;
                                }

                                // Opposite laser.
                                projectileDestination = target.Center - target.velocity * predictionFactor;
                                laser = Utilities.NewProjectileBetter(npc.Center, npc.SafeDirectionTo(projectileDestination) * shootSpeed, type, 550, 0f, Main.myPlayer, 0f, npc.whoAmI);
                                if (Main.projectile.IndexInRange(laser))
                                {
                                    Main.projectile[laser].owner = npc.target;
                                    Main.projectile[laser].ModProjectile<ThanatosLaser>().InitialDestination = projectileDestination;
                                    Main.projectile[laser].ai[1] = npc.whoAmI;
                                }
                                break;
                            case ThanatosHeadBehaviorOverride.ThanatosHeadAttackType.ProjectileShooting_PurpleLaser:
                                type = ModContent.ProjectileType<PulseLaser>();
                                shootSpeed = generalShootSpeedFactor * 10f;

                                projectileDestination = target.Center;
                                laser = Utilities.NewProjectileBetter(npc.Center, npc.SafeDirectionTo(projectileDestination) * shootSpeed, type, 600, 0f, Main.myPlayer, 0f, npc.whoAmI);
                                if (Main.projectile.IndexInRange(laser))
                                {
                                    Main.projectile[laser].owner = npc.target;
                                    Main.projectile[laser].ModProjectile<PulseLaser>().InitialDestination = projectileDestination;
                                    Main.projectile[laser].ai[1] = npc.whoAmI;
                                }
                                break;
                            case ThanatosHeadBehaviorOverride.ThanatosHeadAttackType.ProjectileShooting_GreenLaser:
                                type = ModContent.ProjectileType<PlasmaLaser>();
                                shootSpeed = generalShootSpeedFactor * 5.6f;

                                projectileDestination = target.Center;
                                laser = Utilities.NewProjectileBetter(npc.Center, npc.SafeDirectionTo(projectileDestination) * shootSpeed, type, 550, 0f, Main.myPlayer, 0f, npc.whoAmI);
                                if (Main.projectile.IndexInRange(laser))
                                {
                                    Main.projectile[laser].owner = npc.target;
                                    Main.projectile[laser].ModProjectile<PlasmaLaser>().InitialDestination = projectileDestination;
                                    Main.projectile[laser].ai[1] = npc.whoAmI;
                                }
                                break;
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
            npc.takenDamageMultiplier = 5.6f;

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
                npc.takenDamageMultiplier = 42.35f;
                if (npc.Opacity > 0.6f)
                {
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
                }
                npc.Calamity().DR = ThanatosHeadBehaviorOverride.OpenSegmentDR;
                npc.Calamity().unbreakableDR = false;
                npc.chaseable = true;
            }
            // Emit light.
            else
                Lighting.AddLight(npc.Center, 0.05f * npc.Opacity, 0.2f * npc.Opacity, 0.2f * npc.Opacity);

            // Handle smoke updating.
            if (npc.type == ModContent.NPCType<ThanatosBody1>())
                npc.ModNPC<ThanatosBody1>().SmokeDrawer.Update();
            if (npc.type == ModContent.NPCType<ThanatosBody2>())
                npc.ModNPC<ThanatosBody2>().SmokeDrawer.Update();
            if (npc.type == ModContent.NPCType<ThanatosTail>())
                npc.ModNPC<ThanatosTail>().SmokeDrawer.Update();

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
        public override int NPCOverrideType => ModContent.NPCType<ThanatosBody2>();

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
