using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.Particles;
using InfernumMode.OverridingSystem;
using InfernumMode.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class AresTeslaCannonBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<AresTeslaCannon>();

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
            npc.ModNPC<AresTeslaCannon>().EnergyDrawer.Update();

            // Locate Ares' body as an NPC.
            NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];
            ExoMechAIUtilities.HaveArmsInheritAresBodyAttributes(npc);

            bool performingDeathAnimation = ExoMechAIUtilities.PerformingDeathAnimation(npc);
            Player target = Main.player[npc.target];

            // Define attack variables.
            bool currentlyDisabled = AresBodyBehaviorOverride.ArmIsDisabled(npc);
            int shootTime = 135;
            int totalOrbsPerBurst = 4;
            float aimPredictiveness = 25f;
            float orbShootSpeed = 12f;
            Vector2 aimDirection = npc.SafeDirectionTo(target.Center + target.velocity * aimPredictiveness);

            // Shoot slower if pointing downward.
            orbShootSpeed *= MathHelper.Lerp(1f, 0.8f, Utils.InverseLerp(0.61f, 0.24f, aimDirection.AngleBetween(Vector2.UnitY), true));

            if (ExoMechManagement.CurrentAresPhase >= 2)
            {
                totalOrbsPerBurst = 6;
                orbShootSpeed *= 0.75f;
            }

            // Nerf things while Ares' complement mech is present.
            if (ExoMechManagement.CurrentAresPhase == 4)
                totalOrbsPerBurst = 7;

            if (ExoMechManagement.CurrentAresPhase >= 5)
            {
                shootTime += 40;
                totalOrbsPerBurst = 9;
                orbShootSpeed *= 1.33f;
            }
            if (ExoMechManagement.CurrentAresPhase >= 6)
            {
                shootTime += 40;
                totalOrbsPerBurst = 11;
            }

            // Get very pissed off if Ares is enraged.
            if (aresBody.Infernum().ExtraAI[13] == 1f)
                totalOrbsPerBurst += 6;

            int shootRate = shootTime / totalOrbsPerBurst;
            ref float attackTimer = ref npc.ai[0];
            ref float chargeDelay = ref npc.ai[1];
            ref float orbCounter = ref npc.ai[2];
            ref float shouldPrepareToFire = ref npc.ai[3];

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
            Vector2 hoverDestination = aresBody.Center + new Vector2(-horizontalOffset, verticalOffset);
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
            ExoMechAIUtilities.PerformAresArmDirectioning(npc, aresBody, target, aimDirection, currentlyDisabled, doingHoverCharge, ref _);
            float rotationToEndOfCannon = npc.rotation;
            if (rotationToEndOfCannon < 0f)
                rotationToEndOfCannon += MathHelper.Pi;
            Vector2 endOfCannon = npc.Center + rotationToEndOfCannon.ToRotationVector2() * 84f + Vector2.UnitY * 8f;

            // Create a dust telegraph and electricity arcs before firing.
            if (attackTimer > chargeDelay * 0.7f && attackTimer < chargeDelay)
            {
                Vector2 dustSpawnPosition = endOfCannon + Main.rand.NextVector2Circular(45f, 45f);
                Dust electricity = Dust.NewDustPerfect(dustSpawnPosition, 229);
                electricity.velocity = (endOfCannon - electricity.position) * 0.04f;
                electricity.scale = 1.25f;
                electricity.noGravity = true;
            }

            // Decide the state of the particle drawer.
            npc.ModNPC<AresTeslaCannon>().EnergyDrawer.ParticleSpawnRate = 99999999;
            if (attackTimer > chargeDelay * 0.45f)
            {
                shouldPrepareToFire = 1f;
                float chargeCompletion = MathHelper.Clamp(attackTimer / chargeDelay, 0f, 1f);
                npc.ModNPC<AresTeslaCannon>().EnergyDrawer.ParticleSpawnRate = 3;
                npc.ModNPC<AresTeslaCannon>().EnergyDrawer.SpawnAreaCompactness = 100f;
                npc.ModNPC<AresTeslaCannon>().EnergyDrawer.chargeProgress = chargeCompletion;
                if (Main.rand.NextBool(3) && chargeCompletion < 1f)
                {
                    Vector2 arcVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(8f, 13f);
                    Vector2 arcPosition = npc.Center - rotationToEndOfCannon.ToRotationVector2() * 38f + Vector2.UnitY * 8f;
                    GeneralParticleHandler.SpawnParticle(new ElectricArc(arcPosition, arcVelocity, Color.Cyan, Main.rand.NextFloat(0.8f, 1.15f), 32));
                }

                if (attackTimer % 15f == 14f && chargeCompletion < 1f)
                    npc.ModNPC<AresTeslaCannon>().EnergyDrawer.AddPulse(chargeCompletion * 6f);
            }

            // Fire orbs.
            if (attackTimer >= chargeDelay && attackTimer % shootRate == shootRate - 1f)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PlasmaBolt"), npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int teslaOrbDamage = AresBodyBehaviorOverride.ProjectileDamageBoost + 500;
                    int electricOrb = Utilities.NewProjectileBetter(endOfCannon, aimDirection * orbShootSpeed, ModContent.ProjectileType<AresTeslaOrb>(), teslaOrbDamage, 0f);
                    if (Main.projectile.IndexInRange(electricOrb))
                        Main.projectile[electricOrb].ai[0] = orbCounter;

                    orbCounter++;
                    npc.netUpdate = true;
                }
            }

            // Reset the attack and orb timer after an attack cycle ends.
            if (attackTimer >= chargeDelay + shootTime)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // Release sparks once Ares is in the second phase.
                    if (ExoMechManagement.CurrentAresPhase >= 2)
                    {
                        int teslaSparkDamage = AresBodyBehaviorOverride.ProjectileDamageBoost + 500;
                        float offsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                        for (int i = 0; i < 7; i++)
                        {
                            Vector2 sparkVelocity = (MathHelper.TwoPi * i / 7f + offsetAngle).ToRotationVector2() * 6.5f;
                            Utilities.NewProjectileBetter(npc.Center + sparkVelocity * 6f, sparkVelocity, ModContent.ProjectileType<TeslaSpark>(), teslaSparkDamage, 0f);
                        }
                    }

                    // As well as a of electric clouds in the third phase.
                    if (ExoMechManagement.CurrentAresPhase >= 3)
                    {
                        int teslaGasDamage = AresBodyBehaviorOverride.ProjectileDamageBoost + 530;
                        for (int i = 0; i < 85; i++)
                        {
                            Vector2 cloudShootVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(4f, 23f) - npc.velocity.SafeNormalize(-Vector2.UnitY) * 10f;
                            Utilities.NewProjectileBetter(npc.Center + cloudShootVelocity * 3f, cloudShootVelocity, ModContent.ProjectileType<ElectricGas>(), teslaGasDamage, 0f);
                        }
                    }
                }

                attackTimer = 0f;
                orbCounter = 0f;
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
            ExoMechAIUtilities.DrawAresArmTelegraphEffect(spriteBatch, npc, Color.Cyan, texture, center, frame, origin);
            Main.spriteBatch.Draw(texture, center, frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, spriteEffects, 0f);

            texture = ModContent.GetTexture("CalamityMod/NPCs/ExoMechs/Ares/AresTeslaCannonGlow");

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

            if (npc.ai[3] == 1f)
                npc.ModNPC<AresTeslaCannon>().EnergyDrawer.DrawBloom(npc.ModNPC<AresTeslaCannon>().CoreSpritePosition);
            npc.ModNPC<AresTeslaCannon>().EnergyDrawer.DrawPulses(npc.ModNPC<AresTeslaCannon>().CoreSpritePosition);
            npc.ModNPC<AresTeslaCannon>().EnergyDrawer.DrawSet(npc.ModNPC<AresTeslaCannon>().CoreSpritePosition);

            Main.spriteBatch.ResetBlendState();

            return false;
        }
        #endregion Frames and Drawcode
    }
}
