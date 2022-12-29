using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.Sounds;
using InfernumMode.BehaviorOverrides.BossAIs.DoG;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.ComboAttacks;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.DraedonBehaviorOverride;
using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.Thanatos.ThanatosHeadBehaviorOverride;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Thanatos
{
    public class ThanatosBody1BehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<ThanatosBody1>();

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
            frameType = (int)ThanatosFrameType.Closed;

            // Die if necessary segments are not present.
            if (!Main.npc.IndexInRange(npc.realLife) || !Main.npc[npc.realLife].active || !Main.npc.IndexInRange((int)npc.ai[1]) || !Main.npc[(int)npc.ai[1]].active)
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
            npc.Center = aheadSegment.Center - directionToNextSegment.SafeNormalize(Vector2.Zero) * npc.width * npc.scale * 0.9f;
            npc.spriteDirection = (directionToNextSegment.X > 0).ToDirectionInt();

            // Locate the head as an NPC.
            NPC head = Main.npc[npc.realLife];

            // Shamelessly steal variables from the head.
            npc.Opacity = head.Opacity;
            npc.target = head.target;
            npc.dontTakeDamage = head.dontTakeDamage;
            npc.damage = head.damage > 0 ? npc.defDamage : 0;
            npc.Infernum().ExtraAI[ExoMechManagement.FinalPhaseTimerIndex] = head.Infernum().ExtraAI[ExoMechManagement.FinalPhaseTimerIndex];
            npc.Infernum().ExtraAI[ExoMechManagement.DeathAnimationTimerIndex] = head.Infernum().ExtraAI[ExoMechManagement.DeathAnimationTimerIndex];
            npc.Infernum().ExtraAI[ExoMechManagement.DeathAnimationHasStartedIndex] = head.Infernum().ExtraAI[ExoMechManagement.DeathAnimationHasStartedIndex];
            Player target = Main.player[npc.target];

            // Kill debuffs.
            DoGPhase1BodyBehaviorOverride.KillUnbalancedDebuffs(npc);

            // Handle open behavior and frames.
            ThanatosHeadAttackType headAttackType = (ThanatosHeadAttackType)(int)head.ai[0];
            float totalSegmentsToFire = head.Infernum().ExtraAI[0];
            bool canBeOpen = (int)Math.Round(segmentAttackIndex % (SegmentCount / totalSegmentsToFire)) == 0;
            bool thanatosIsFiring = headAttackType != ThanatosHeadAttackType.AggressiveCharge && head.Infernum().ExtraAI[1] > 0f;
            bool segmentShouldContiuouslyBeOpen = headAttackType == ThanatosHeadAttackType.MaximumOverdrive && head.Infernum().ExtraAI[0] == 1f;

            // Handle death animation stuff.
            if (npc.Infernum().ExtraAI[ExoMechManagement.DeathAnimationHasStartedIndex] != 0f)
            {
                thanatosIsFiring = false;
                canBeOpen = true;
                segmentShouldContiuouslyBeOpen = true;
                DoBehavior_DeathAnimation(npc, target, ref npc.Infernum().ExtraAI[ExoMechManagement.DeathAnimationTimerIndex], ref frameType);
            }

            // Keep a handful of segments open if the head is open, for the sake of making it easier to damage Thanatos.
            int openSegmentPeriodWhenHeadIsOpen = 12;
            if (head.localAI[0] == (int)ThanatosFrameType.Open && segmentAttackIndex % openSegmentPeriodWhenHeadIsOpen == openSegmentPeriodWhenHeadIsOpen - 1f)
            {
                thanatosIsFiring = false;
                canBeOpen = true;
                segmentShouldContiuouslyBeOpen = true;
            }

            // Handle segment opening/closing and projectile firing.
            if (thanatosIsFiring && canBeOpen)
            {
                float segmentFireTime = head.Infernum().ExtraAI[1];
                float segmentFireCountdown = head.Infernum().ExtraAI[2];
                int fireDelay = (int)MathHelper.Lerp(segmentFireCountdown * -0.32f, segmentFireCountdown * 0.32f, segmentAttackIndex / SegmentCount);

                frameType = (int)ThanatosFrameType.Open;
                npc.frameCounter = Utils.GetLerpValue(0f, segmentFireTime * 0.5f + fireDelay, segmentFireCountdown, true);
                npc.frameCounter *= Utils.GetLerpValue(segmentFireTime, segmentFireTime * 0.5f + fireDelay, segmentFireCountdown, true);
                npc.frameCounter = (int)Math.Round(npc.frameCounter * (Main.npcFrameCount[npc.type] - 1f));

                if (segmentFireCountdown == (int)(segmentFireTime / 2) + fireDelay)
                {
                    bool willShootProjectile = (int)headAttackType != (int)ExoMechComboAttackContent.ExoMechComboAttackType.ThanatosAres_ElectricCage;

                    // Decide what sound to play.
                    if (willShootProjectile)
                        SoundEngine.PlaySound(CommonCalamitySounds.LaserCannonSound with { Volume = 0.5f }, target.Center);
                    SoundEngine.PlaySound(ThanatosHead.VentSound with { Volume = 0.33f }, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient && willShootProjectile)
                    {
                        float generalShootSpeedFactor = 1.425f;
                        if (ExoMechManagement.CurrentThanatosPhase == 4)
                            generalShootSpeedFactor *= 0.65f;
                        else
                        {
                            if (ExoMechManagement.CurrentThanatosPhase >= 2)
                                generalShootSpeedFactor *= 1.15f;
                            if (ExoMechManagement.CurrentThanatosPhase >= 3)
                                generalShootSpeedFactor *= 1.15f;
                        }

                        if ((int)headAttackType == (int)ExoMechComboAttackContent.ExoMechComboAttackType.ThanatosAres_LaserCircle)
                            generalShootSpeedFactor *= ExoMechManagement.CurrentThanatosPhase != 4f ? 0.36f : 0.5f;

                        switch ((int)headAttackType)
                        {
                            // Fire regular lasers.
                            case (int)ExoMechComboAttackContent.ExoMechComboAttackType.ThanatosAres_LaserCircle:
                                int type = ModContent.ProjectileType<ThanatosAresComboLaser>();
                                float shootSpeed = generalShootSpeedFactor * 10f;
                                Vector2 projectileDestination = Main.npc[CalamityGlobalNPC.draedonExoMechPrime].Center + Vector2.UnitY * 34f;
                                int laser = Utilities.NewProjectileBetter(npc.Center, npc.SafeDirectionTo(projectileDestination) * shootSpeed, type, StrongerNormalShotDamage, 0f, Main.myPlayer, 0f, npc.whoAmI);
                                if (Main.projectile.IndexInRange(laser))
                                {
                                    Main.projectile[laser].owner = npc.target;
                                    Main.projectile[laser].ModProjectile<ThanatosAresComboLaser>().InitialDestination = projectileDestination;
                                    Main.projectile[laser].ai[1] = npc.whoAmI;
                                    Main.projectile[laser].netUpdate = true;
                                }
                                break;
                        }
                    }
                }
            }
            else if (segmentShouldContiuouslyBeOpen)
            {
                frameType = (int)ThanatosFrameType.Open;
                if (npc.frameCounter > Main.npcFrameCount[npc.type] - 1f)
                {
                    npc.frameCounter = Main.npcFrameCount[npc.type] - 1f;
                    SoundEngine.PlaySound(ThanatosHead.VentSound with { Volume = 0.5f }, npc.Center);
                }
                else if (npc.frameCounter < Main.npcFrameCount[npc.type] - 1f)
                    npc.frameCounter += 0.26f;
            }
            else
                npc.frameCounter = 0f;

            // Handle smoke venting and open/closed DR.
            npc.Calamity().DR = ClosedSegmentDR;
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
            if (frameType == (int)ThanatosFrameType.Open)
            {
                // Emit light.
                Lighting.AddLight(npc.Center, 0.35f * npc.Opacity, 0.05f * npc.Opacity, 0.05f * npc.Opacity);

                // Emit smoke.
                npc.takenDamageMultiplier = 242f;
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
                npc.Calamity().DR = OpenSegmentDR;
                if (head.ai[0] >= 100f)
                    npc.takenDamageMultiplier *= 2f;

                npc.Calamity().unbreakableDR = false;
                npc.chaseable = true;
            }
            // Emit light.
            else
                Lighting.AddLight(npc.Center, 0.05f * npc.Opacity, 0.2f * npc.Opacity, 0.2f * npc.Opacity);

            if (head.Calamity().DR >= 0.999f)
            {
                npc.Calamity().DR = 0.9999999f;
                npc.takenDamageMultiplier = 0.01f;
            }

            if (head.Infernum().ExtraAI[17] >= 1f)
                npc.takenDamageMultiplier *= 0.5f;

            // Handle smoke updating.
            if (npc.type == ModContent.NPCType<ThanatosBody1>())
                npc.ModNPC<ThanatosBody1>().SmokeDrawer.Update();
            if (npc.type == ModContent.NPCType<ThanatosBody2>())
                npc.ModNPC<ThanatosBody2>().SmokeDrawer.Update();
            if (npc.type == ModContent.NPCType<ThanatosTail>())
                npc.ModNPC<ThanatosTail>().SmokeDrawer.Update();

            // Become vulnerable on the map.
            npc.ModNPC.GetType().GetField("vulnerable", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(npc.ModNPC, frameType == (int)ThanatosFrameType.Open);
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frame.Y = (int)npc.frameCounter * frameHeight;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Vector2 origin = npc.frame.Size() * 0.5f;

            Vector2 center = npc.Center - Main.screenPosition;

            ExoMechAIUtilities.DrawFinalPhaseGlow(spriteBatch, npc, texture, center, npc.frame, origin);
            Main.spriteBatch.Draw(texture, center, npc.frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, spriteEffects, 0f);

            texture = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Thanatos/ThanatosBody1Glow").Value;
            Main.spriteBatch.Draw(texture, center, npc.frame, Color.White * npc.Opacity, npc.rotation, origin, npc.scale, spriteEffects, 0f);
            npc.ModNPC<ThanatosBody1>().SmokeDrawer.DrawSet(npc.Center);
            return false;
        }

        public override bool CheckDead(NPC npc) => ExoMechManagement.HandleDeathEffects(npc);
    }

    public class ThanatosBody2BehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<ThanatosBody2>();

        public override bool PreAI(NPC npc)
        {
            ThanatosBody1BehaviorOverride.DoSegmentAI(npc);
            return false;
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frame.Y = (int)(npc.frameCounter * frameHeight);
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Vector2 origin = npc.frame.Size() * 0.5f;

            Vector2 center = npc.Center - Main.screenPosition;

            ExoMechAIUtilities.DrawFinalPhaseGlow(spriteBatch, npc, texture, center, npc.frame, origin);
            Main.spriteBatch.Draw(texture, center, npc.frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, spriteEffects, 0f);

            texture = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Thanatos/ThanatosBody2Glow").Value;
            Main.spriteBatch.Draw(texture, center, npc.frame, Color.White * npc.Opacity, npc.rotation, origin, npc.scale, spriteEffects, 0f);
            npc.ModNPC<ThanatosBody2>().SmokeDrawer.DrawSet(npc.Center);
            return false;
        }

        public override bool CheckDead(NPC npc) => ExoMechManagement.HandleDeathEffects(npc);
    }

    public class ThanatosTailBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<ThanatosTail>();

        public override bool PreAI(NPC npc)
        {
            ThanatosBody1BehaviorOverride.DoSegmentAI(npc);
            return false;
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frame.Y = (int)(npc.frameCounter * frameHeight);
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Vector2 origin = npc.frame.Size() * 0.5f;

            Vector2 center = npc.Center - Main.screenPosition;

            ExoMechAIUtilities.DrawFinalPhaseGlow(spriteBatch, npc, texture, center, npc.frame, origin);
            Main.spriteBatch.Draw(texture, center, npc.frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, spriteEffects, 0f);

            texture = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Thanatos/ThanatosTailGlow").Value;
            Main.spriteBatch.Draw(texture, center, npc.frame, Color.White * npc.Opacity, npc.rotation, origin, npc.scale, spriteEffects, 0f);
            return false;
        }

        public override bool CheckDead(NPC npc) => ExoMechManagement.HandleDeathEffects(npc);
    }
}
