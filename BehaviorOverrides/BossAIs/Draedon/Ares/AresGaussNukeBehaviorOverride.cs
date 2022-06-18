using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

// NOTE: This AI is currently unused. For posterity, however, it remains here.
namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class AresGaussNukeBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<AresGaussNuke>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame | NPCOverrideContext.NPCPreDraw;

        #region AI
        public override bool PreAI(NPC npc)
        {
            // Die if Ares is not present.
            if (CalamityGlobalNPC.draedonExoMechPrime == -1)
            {
                npc.active = false;
                return false;
            }

            // Locate Ares' body as an NPC.
            NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];
            ExoMechAIUtilities.HaveArmsInheritAresBodyAttributes(npc);

            bool performingDeathAnimation = ExoMechAIUtilities.PerformingDeathAnimation(npc);
            Player target = Main.player[npc.target];

            // Define attack variables.
            bool currentlyDisabled = AresBodyBehaviorOverride.ArmIsDisabled(npc);
            int shootTime = 150;
            float aimPredictiveness = 10f;
            ref float attackTimer = ref npc.ai[0];
            ref float chargeDelay = ref npc.ai[1];
            ref float rechargeTime = ref npc.ai[2];

            // Initialize delays and other timers.
            float idealChargeDelay = AresBodyBehaviorOverride.Phase1ArmChargeupTime * 1.95f;
            float idealRechargeTime = AresBodyBehaviorOverride.Phase1ArmChargeupTime * 1.95f;

            if (ExoMechManagement.CurrentAresPhase >= 2)
            {
                idealChargeDelay *= 0.7f;
                idealRechargeTime *= 0.7f;
            }
            if (ExoMechManagement.CurrentAresPhase >= 3)
            {
                idealChargeDelay *= 0.7f;
                idealRechargeTime *= 0.7f;
            }
            if (ExoMechManagement.CurrentAresPhase >= 5)
                idealChargeDelay *= 0.65f;
            if (ExoMechManagement.CurrentAresPhase >= 6)
                idealRechargeTime *= 0.75f;

            // Nerf things while Ares' complement mech is present.
            if (ExoMechManagement.CurrentAresPhase == 4)
            {
                idealChargeDelay += 75f;
                idealRechargeTime += 75f;
            }

            // Get very pissed off if Ares is enraged.
            if (aresBody.Infernum().ExtraAI[13] == 1f)
            {
                idealChargeDelay /= 3;
                idealRechargeTime /= 3;
            }
            idealChargeDelay = (int)idealChargeDelay;
            idealRechargeTime = (int)idealRechargeTime;

            if (chargeDelay != idealChargeDelay || rechargeTime != idealRechargeTime)
            {
                chargeDelay = idealChargeDelay;
                rechargeTime = idealRechargeTime;
                attackTimer = 0f;
                npc.netUpdate = true;
            }

            // Don't do anything if this arm should be disabled.
            if (currentlyDisabled && attackTimer >= chargeDelay - 50f)
                attackTimer = 0f;

            // Become more resistant to damage as necessary.
            npc.takenDamageMultiplier = 1f;
            if (ExoMechManagement.ShouldHaveSecondComboPhaseResistance(npc))
                npc.takenDamageMultiplier *= 0.5f;

            // Hover near Ares.
            bool doingHoverCharge = aresBody.ai[0] == (int)AresBodyBehaviorOverride.AresBodyAttackType.HoverCharge && !performingDeathAnimation;
            float horizontalOffset = doingHoverCharge ? 380f : 575f;
            float verticalOffset = doingHoverCharge ? 150f : 0f;
            Vector2 hoverDestination = aresBody.Center + new Vector2((aresBody.Infernum().ExtraAI[15] == 1f ? -1f : 1f) * horizontalOffset, verticalOffset);
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
            Vector2 endOfCannon = npc.Center + rotationToEndOfCannon.ToRotationVector2() * 40f;

            // Fire the nuke.
            if (attackTimer == (int)chargeDelay)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/LargeWeaponFire"), npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float nukeShootSpeed = 13.5f;
                    Utilities.NewProjectileBetter(endOfCannon, aimDirection * nukeShootSpeed, ModContent.ProjectileType<AresGaussNukeProjectile>(), 1200, 0f, npc.target);

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
            int currentFrame;
            if (npc.ai[0] < npc.ai[1] - 30f)
                currentFrame = (int)Math.Round(MathHelper.Lerp(0f, 35f, npc.ai[0] / (npc.ai[1] - 30f)));
            else if (npc.ai[0] <= npc.ai[1] + 30f)
                currentFrame = (int)Math.Round(MathHelper.Lerp(35f, 47f, Utils.InverseLerp(npc.ai[1] - 30f, npc.ai[1] + 30f, npc.ai[0], true)));
            else
                currentFrame = (int)Math.Round(MathHelper.Lerp(49f, 107f, Utils.InverseLerp(npc.ai[1] + 30f, npc.ai[1] + npc.ai[2], npc.ai[0], true)));

            npc.frame = new Rectangle(npc.width * (currentFrame / 12), npc.height * (currentFrame % 12), npc.width, npc.height);
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
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
            Main.spriteBatch.Draw(texture, center, frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, spriteEffects, 0f);

            texture = ModContent.GetTexture("CalamityMod/NPCs/ExoMechs/Ares/AresGaussNukeGlow");

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
            return false;
        }
        #endregion Frames and Drawcode
    }
}
