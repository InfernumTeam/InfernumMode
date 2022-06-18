using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Ares;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class AresPlasmaCannonBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<AresPlasmaFlamethrower>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame | NPCOverrideContext.NPCPreDraw;

        #region AI
        public override bool PreAI(NPC npc)
        {
            // Die if Ares is not present.
            if (CalamityGlobalNPC.draedonExoMechPrime == -1)
            {
                npc.life = 0;
                npc.HitEffect();
                npc.active = false;
                return false;
            }

            // Update the energy drawer.
            npc.ModNPC<AresPlasmaFlamethrower>().EnergyDrawer.Update();

            // Locate Ares' body as an NPC.
            NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];
            ExoMechAIUtilities.HaveArmsInheritAresBodyAttributes(npc);

            bool performingDeathAnimation = ExoMechAIUtilities.PerformingDeathAnimation(npc);
            Player target = Main.player[npc.target];

            // Define attack variables.
            bool currentlyDisabled = AresBodyBehaviorOverride.ArmIsDisabled(npc);
            int shootTime = 150;
            int totalFlamesPerBurst = 2;
            float flameShootSpeed = 9.33f;
            float aimPredictiveness = 20f;

            // Nerf things while Ares' complement mech is present.
            if (ExoMechManagement.CurrentAresPhase == 4)
                flameShootSpeed *= 0.85f;

            if (ExoMechManagement.CurrentAresPhase >= 5)
            {
                shootTime += 60;
                totalFlamesPerBurst += 2;
                flameShootSpeed *= 1.2f;
            }
            if (ExoMechManagement.CurrentAresPhase >= 6)
            {
                shootTime -= 30;
                totalFlamesPerBurst += 2;
                aimPredictiveness -= 2f;
            }

            // Get very pissed off if Ares is enraged.
            if (aresBody.Infernum().ExtraAI[13] == 1f)
                totalFlamesPerBurst += 5;

            int shootRate = shootTime / totalFlamesPerBurst;
            ref float attackTimer = ref npc.ai[0];
            ref float chargeDelay = ref npc.ai[1];
            ref float shouldPrepareToFire = ref npc.Infernum().ExtraAI[1];

            // Initialize delays and other timers.
            shouldPrepareToFire = 0f;
            if (chargeDelay == 0f)
                chargeDelay = AresBodyBehaviorOverride.Phase1ArmChargeupTime;

            // Don't do anything if this arm should be disabled.
            if (currentlyDisabled)
                attackTimer = 1f;

            // Become more resistant to damage as necessary.
            npc.takenDamageMultiplier = 1f;
            if (ExoMechManagement.ShouldHaveSecondComboPhaseResistance(npc))
                npc.takenDamageMultiplier *= 0.5f;

            // Hover near Ares.
            bool doingHoverCharge = aresBody.ai[0] == (int)AresBodyBehaviorOverride.AresBodyAttackType.HoverCharge && !performingDeathAnimation;
            float horizontalOffset = doingHoverCharge ? 250f : 375f;
            float verticalOffset = doingHoverCharge ? 150f : 100f;
            Vector2 hoverDestination = aresBody.Center + new Vector2(horizontalOffset, verticalOffset);
            ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 65f, 115f);
            npc.Infernum().ExtraAI[0] = MathHelper.Clamp(npc.Infernum().ExtraAI[0] + doingHoverCharge.ToDirectionInt(), 0f, 15f);

            // Check to see if Ares is in the middle of a death animation. If it is, participate in the death animation.
            if (performingDeathAnimation)
            {
                AresBodyBehaviorOverride.HaveArmPerformDeathAnimation(npc, new Vector2(horizontalOffset, verticalOffset));
                return false;
            }

            // Check to see if this arm should be used for special things in a combo attack.
            float _ = 0f;
            if (ExoMechComboAttackContent.ArmCurrentlyBeingUsed(npc))
            {
                ExoMechComboAttackContent.UseThanatosAresComboAttack(npc, ref aresBody.ai[1], ref _);
                ExoMechComboAttackContent.UseTwinsAresComboAttack(npc, 1f, ref aresBody.ai[1], ref _);
                return false;
            }

            // Calculate the direction and rotation this arm should use.
            Vector2 aimDirection = npc.SafeDirectionTo(target.Center + target.velocity * aimPredictiveness);
            ExoMechAIUtilities.PerformAresArmDirectioning(npc, aresBody, target, aimDirection, currentlyDisabled, doingHoverCharge, ref _);
            float rotationToEndOfCannon = npc.rotation;
            if (rotationToEndOfCannon < 0f)
                rotationToEndOfCannon += MathHelper.Pi;
            Vector2 endOfCannon = npc.Center + rotationToEndOfCannon.ToRotationVector2() * 66f + Vector2.UnitY * 16f;

            // Create a dust telegraph before firing.
            if (attackTimer > chargeDelay * 0.7f && attackTimer < chargeDelay)
            {
                Vector2 dustSpawnPosition = endOfCannon + Main.rand.NextVector2Circular(45f, 45f);
                Dust plasma = Dust.NewDustPerfect(dustSpawnPosition, 107);
                plasma.velocity = (endOfCannon - plasma.position) * 0.04f;
                plasma.scale = 1.25f;
                plasma.noGravity = true;
            }

            // Decide the state of the particle drawer.
            npc.ModNPC<AresPlasmaFlamethrower>().EnergyDrawer.ParticleSpawnRate = 99999999;
            if (attackTimer > chargeDelay * 0.45f)
            {
                shouldPrepareToFire = 1f;
                float chargeCompletion = MathHelper.Clamp(attackTimer / chargeDelay, 0f, 1f);
                npc.ModNPC<AresPlasmaFlamethrower>().EnergyDrawer.ParticleSpawnRate = 3;
                npc.ModNPC<AresPlasmaFlamethrower>().EnergyDrawer.SpawnAreaCompactness = 100f;
                npc.ModNPC<AresPlasmaFlamethrower>().EnergyDrawer.chargeProgress = chargeCompletion;

                if (attackTimer % 15f == 14f && chargeCompletion < 1f)
                    npc.ModNPC<AresPlasmaFlamethrower>().EnergyDrawer.AddPulse(chargeCompletion * 6f);
            }

            // Fire plasma.
            if (attackTimer >= chargeDelay && attackTimer % shootRate == shootRate - 1f)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PlasmaCasterFire"), npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int fireballCount = ExoMechManagement.CurrentAresPhase >= 3 ? 2 : 1;

                    for (int i = 0; i < fireballCount; i++)
                    {
                        int flameDamage = AresBodyBehaviorOverride.ProjectileDamageBoost + 500;
                        Vector2 flameShootVelocity = aimDirection * flameShootSpeed;
                        int fireballType = ModContent.ProjectileType<AresPlasmaFireball>();
                        if (ExoMechManagement.CurrentAresPhase >= 2)
                            fireballType = ModContent.ProjectileType<AresPlasmaFireball2>();
                        if (fireballCount > 1)
                        {
                            flameShootVelocity = flameShootVelocity.RotatedByRandom(0.34f);
                            if (i > 0)
                                flameShootVelocity *= Main.rand.NextFloat(0.6f, 0.9f);
                        }

                        Utilities.NewProjectileBetter(endOfCannon, flameShootVelocity, fireballType, flameDamage, 0f);
                    }

                    npc.netUpdate = true;
                }
            }

            // Reset the attack timer after an attack cycle ends.
            if (attackTimer >= chargeDelay + shootTime)
            {
                attackTimer = 0f;
                npc.netUpdate = true;
            }
            attackTimer++;
            return false;
        }

        #endregion AI

        #region Frames and Drawcode
        public override void FindFrame(NPC npc, int frameHeight)
        {
            int currentFrame = (int)Math.Round(MathHelper.Lerp(0f, 35f, npc.ai[0] / npc.ai[1]));

            if (npc.ai[0] > npc.ai[1])
            {
                npc.frameCounter++;
                if (npc.frameCounter >= 66f)
                    npc.frameCounter = 0D;
                currentFrame = (int)Math.Round(MathHelper.Lerp(36f, 47f, (float)npc.frameCounter / 66f));
            }
            else
                npc.frameCounter = 0D;

            if (ExoMechComboAttackContent.ArmCurrentlyBeingUsed(npc))
                currentFrame = (int)Math.Round(MathHelper.Lerp(0f, 35f, npc.ai[0] % 72f / 72f));

            npc.frame = new Rectangle(npc.width * (currentFrame / 8), npc.height * (currentFrame % 8), npc.width, npc.height);
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            if (npc.Infernum().OptionalPrimitiveDrawer is null)
            {
                npc.Infernum().OptionalPrimitiveDrawer = new PrimitiveTrailCopy(completionRatio => AresBodyBehaviorOverride.FlameTrailWidthFunctionBig(npc, completionRatio),
                    completionRatio => AresBodyBehaviorOverride.FlameTrailColorFunctionBig(npc, completionRatio),
                    null, true, GameShaders.Misc["Infernum:TwinsFlameTrail"]);
            }

            for (int i = 0; i < 2; i++)
            {
                if (npc.Infernum().ExtraAI[0] > 0f)
                    npc.Infernum().OptionalPrimitiveDrawer.Draw(npc.oldPos, npc.Size * 0.5f - Main.screenPosition, 54);
            }

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            // Locate Ares' body as an NPC.
            NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];
            Texture2D texture = Main.npcTexture[npc.type];
            Rectangle frame = npc.frame;
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 center = npc.Center - Main.screenPosition;
            Color afterimageBaseColor = aresBody.Infernum().ExtraAI[13] == 1f ? Color.Red : Color.White;
            int numAfterimages = 5;

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < numAfterimages; i += 2)
                {
                    Color afterimageColor = npc.GetAlpha(Color.Lerp(lightColor, afterimageBaseColor, 0.5f)) * ((numAfterimages - i) / 15f);
                    Vector2 afterimageCenter = npc.oldPos[i] + npc.frame.Size() * 0.5f - Main.screenPosition;
                    Main.spriteBatch.Draw(texture, afterimageCenter, npc.frame, afterimageColor, npc.oldRot[i], origin, npc.scale, spriteEffects, 0f);
                }
            }

            ExoMechAIUtilities.DrawFinalPhaseGlow(spriteBatch, npc, texture, center, frame, origin);
            ExoMechAIUtilities.DrawAresArmTelegraphEffect(spriteBatch, npc, Color.Lime, texture, center, frame, origin);
            Main.spriteBatch.Draw(texture, center, frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, spriteEffects, 0f);

            texture = ModContent.GetTexture("CalamityMod/NPCs/ExoMechs/Ares/AresPlasmaFlamethrowerGlow");

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < numAfterimages; i += 2)
                {
                    Color afterimageColor = npc.GetAlpha(Color.Lerp(lightColor, afterimageBaseColor, 0.5f)) * ((numAfterimages - i) / 15f);
                    Vector2 afterimageCenter = npc.oldPos[i] + npc.frame.Size() * 0.5f - Main.screenPosition;
                    Main.spriteBatch.Draw(texture, afterimageCenter, npc.frame, afterimageColor, npc.oldRot[i], origin, npc.scale, spriteEffects, 0f);
                }
            }

            Main.spriteBatch.Draw(texture, center, frame, afterimageBaseColor * npc.Opacity, npc.rotation, origin, npc.scale, spriteEffects, 0f);

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            if (npc.Infernum().ExtraAI[1] == 1f)
                npc.ModNPC<AresPlasmaFlamethrower>().EnergyDrawer.DrawBloom(npc.ModNPC<AresPlasmaFlamethrower>().CoreSpritePosition);
            npc.ModNPC<AresPlasmaFlamethrower>().EnergyDrawer.DrawPulses(npc.ModNPC<AresPlasmaFlamethrower>().CoreSpritePosition);
            npc.ModNPC<AresPlasmaFlamethrower>().EnergyDrawer.DrawSet(npc.ModNPC<AresPlasmaFlamethrower>().CoreSpritePosition);

            Main.spriteBatch.ResetBlendState();
            return false;
        }
        #endregion Frames and Drawcode
    }
}
