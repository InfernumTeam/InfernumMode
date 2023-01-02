using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.Particles;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.ComboAttacks;
using InfernumMode.OverridingSystem;
using InfernumMode.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public abstract class AresCannonBehaviorOverride : NPCBehaviorOverride
    {
        public static NPC Ares => Main.npc[CalamityGlobalNPC.draedonExoMechPrime];

        public abstract string GlowmaskTexturePath { get; }

        public abstract float AimPredictiveness { get; }

        public abstract int ShootTime { get; }

        public abstract int ShootRate { get; }

        public abstract SoundStyle ShootSound { get; }

        public abstract SoundStyle FireTelegraphSound { get; }

        public abstract Color TelegraphBackglowColor { get; }

        public override int? NPCIDToDeferToForTips => ModContent.NPCType<AresBody>();

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
            AresCannonChargeParticleSet energyDrawer = GetEnergyDrawer(npc);
            energyDrawer.Update();

            // Inherit a bunch of attributes such as opacity from the body.
            ExoMechAIUtilities.HaveArmsInheritAresBodyAttributes(npc);

            bool performingDeathAnimation = ExoMechAIUtilities.PerformingDeathAnimation(npc);
            Player target = Main.player[npc.target];

            // Define attack variables.
            bool currentlyDisabled = AresBodyBehaviorOverride.ArmIsDisabled(npc);
            ref float attackTimer = ref npc.ai[0];
            ref float chargeDelay = ref npc.ai[1];
            ref float currentDirection = ref npc.ai[3];
            ref float shouldPrepareToFire = ref npc.Infernum().ExtraAI[1];
            ref float telegraphSound = ref npc.Infernum().ExtraAI[2];

            // Initialize delays and other timers.
            shouldPrepareToFire = 0f;
            if (chargeDelay == 0f)
                chargeDelay = AresBodyBehaviorOverride.Phase1ArmChargeupTime;

            // Don't do anything if this arm should be disabled.
            if (currentlyDisabled)
                attackTimer = 1f;

            // Hover near Ares.
            bool performingCharge = Ares.ai[0] == (int)AresBodyBehaviorOverride.AresBodyAttackType.HoverCharge && !performingDeathAnimation;
            Vector2 hoverOffset = PerformHoverMovement(npc, performingCharge);
            
            // Update the telegraph outline intensity timer.
            npc.Infernum().ExtraAI[0] = MathHelper.Clamp(npc.Infernum().ExtraAI[0] + performingCharge.ToDirectionInt(), 0f, 15f);

            // Check to see if Ares is in the middle of a death animation. If it is, participate in the death animation.
            if (performingDeathAnimation)
            {
                AresBodyBehaviorOverride.HaveArmPerformDeathAnimation(npc, hoverOffset);
                return false;
            }

            // Check to see if this arm should be used for special things in a combo attack.
            if (IsInUseByComboAttack(npc))
                return false;

            // Calculate the direction and rotation this arm should use.
            Vector2 aimDirection = npc.SafeDirectionTo(target.Center + target.velocity * AimPredictiveness);
            ExoMechAIUtilities.PerformAresArmDirectioning(npc, Ares, target, aimDirection, currentlyDisabled, performingCharge, ref currentDirection);

            float rotationToEndOfCannon = npc.rotation;
            if (rotationToEndOfCannon < 0f)
                rotationToEndOfCannon += MathHelper.Pi;
            Vector2 endOfCannon = npc.Center + rotationToEndOfCannon.ToRotationVector2() * 74f + Vector2.UnitY * 8f;

            // Play a sound telegraph before firing.
            int telegraphTime = Math.Max((int)chargeDelay - InfernumSoundRegistry.AresTelegraphSoundLength, 2);
            if (attackTimer == telegraphTime && !currentlyDisabled)
                telegraphSound = SoundEngine.PlaySound(FireTelegraphSound with { Volume = 1.6f }, npc.Center).ToFloat();

            // Update the sound telegraph's position.
            if (SoundEngine.TryGetActiveSound(SlotId.FromFloat(telegraphSound), out var t) && t.IsPlaying)
            {
                t.Position = npc.Center;
                if (performingCharge)
                    t.Stop();
            }

            // Create a dust telegraph before firing.
            if (attackTimer > chargeDelay * 0.7f && attackTimer < chargeDelay)
                CreateDustTelegraphs(npc, endOfCannon);

            // Decide the state of the particle drawer.
            energyDrawer.ParticleSpawnRate = int.MaxValue;
            if (attackTimer > chargeDelay * 0.45f)
            {
                shouldPrepareToFire = 1f;
                float chargeCompletion = MathHelper.Clamp(attackTimer / chargeDelay, 0f, 1f);
                energyDrawer.ParticleSpawnRate = 3;
                energyDrawer.SpawnAreaCompactness = 100f;
                energyDrawer.chargeProgress = chargeCompletion;

                if (attackTimer % 15f == 14f && chargeCompletion < 1f)
                    energyDrawer.AddPulse(chargeCompletion * 6f);
            }

            // Fire lasers.
            if (attackTimer >= chargeDelay && attackTimer % ShootRate == ShootRate - 1f)
            {
                SoundEngine.PlaySound(ShootSound, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    ShootProjectiles(npc, endOfCannon, aimDirection);
                    npc.netUpdate = true;
                }
            }

            // Reset the attack and laser counter after an attack cycle ends.
            if (attackTimer >= chargeDelay + ShootTime)
            {
                attackTimer = 0f;
                ResetAttackCycleEffects(npc);
                npc.netUpdate = true;
            }
            attackTimer++;
            return false;
        }

        public static Vector2 PerformHoverMovement(NPC npc, bool performingCharge)
        {
            Vector2 hoverOffset = npc.BehaviorOverride<AresCannonBehaviorOverride>().GetHoverOffset(npc, performingCharge);
            Vector2 hoverDestination = Ares.Center + hoverOffset;
            ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 64f, 115f);

            return hoverOffset;
        }

        public static bool IsInUseByComboAttack(NPC npc)
        {
            if (ExoMechComboAttackContent.ArmCurrentlyBeingUsed(npc))
            {
                float _ = 0f;
                ExoMechComboAttackContent.UseThanatosAresComboAttack(npc, ref Ares.ai[1], ref _);
                ExoMechComboAttackContent.UseTwinsAresComboAttack(npc, 1f, ref Ares.ai[1], ref _);
                return true;
            }

            return false;
        }

        public virtual void ResetAttackCycleEffects(NPC npc) { }

        public abstract void CreateDustTelegraphs(NPC npc, Vector2 endOfCannon);

        public abstract void ShootProjectiles(NPC npc, Vector2 endOfCannon, Vector2 aimDirection);

        public abstract Vector2 GetHoverOffset(NPC npc, bool performingCharge);

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
            // Initialize the back flame primitive drawer.
            if (npc.Infernum().OptionalPrimitiveDrawer is null)
            {
                npc.Infernum().OptionalPrimitiveDrawer = new PrimitiveTrailCopy(completionRatio => AresBodyBehaviorOverride.FlameTrailWidthFunctionBig(npc, completionRatio),
                    completionRatio => AresBodyBehaviorOverride.FlameTrailColorFunctionBig(npc, completionRatio),
                    null, true, InfernumEffectsRegistry.TwinsFlameTrailVertexShader);
            }
            
            // Draw the back flames if Ares is dashing.
            for (int i = 0; i < 2; i++)
            {
                if (npc.Infernum().ExtraAI[0] > 0f)
                    npc.Infernum().OptionalPrimitiveDrawer.Draw(npc.oldPos, npc.Size * 0.5f - Main.screenPosition, 54);
            }

            SpriteEffects direction = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                direction = SpriteEffects.FlipHorizontally;

            // Locate Ares' body for reference with certain AI attributes.
            NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Rectangle frame = npc.frame;
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 center = npc.Center - Main.screenPosition;
            bool enraged = AresBodyBehaviorOverride.Enraged || ExoMechComboAttackContent.EnrageTimer > 0f;
            Color glowmaskColor = enraged ? Color.Red : Color.White;

            // Draw backglow effects, telegraphs, and the base texture.
            ExoMechAIUtilities.DrawFinalPhaseGlow(spriteBatch, npc, texture, center, frame, origin);
            ExoMechAIUtilities.DrawAresArmTelegraphEffect(spriteBatch, npc, TelegraphBackglowColor, texture, center, frame, origin);
            Main.spriteBatch.Draw(texture, center, frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, direction, 0f);

            // Draw glowmasks.
            texture = ModContent.Request<Texture2D>(GlowmaskTexturePath).Value;

            Main.spriteBatch.Draw(texture, center, frame, glowmaskColor * npc.Opacity, npc.rotation, origin, npc.scale, direction, 0f);

            // Draw energy effects for telegraph purposes.
            Main.spriteBatch.SetBlendState(BlendState.Additive);

            AresCannonChargeParticleSet energyDrawer = GetEnergyDrawer(npc);
            Vector2 coreDrawPosition = GetCoreSpritePosition(npc);
            if (npc.Infernum().ExtraAI[1] == 1f)
                energyDrawer.DrawBloom(coreDrawPosition);
            energyDrawer.DrawPulses(coreDrawPosition);
            energyDrawer.DrawSet(coreDrawPosition);

            Main.spriteBatch.ResetBlendState();
            return false;
        }
        
        public abstract AresCannonChargeParticleSet GetEnergyDrawer(NPC npc);

        public abstract Vector2 GetCoreSpritePosition(NPC npc);
        #endregion Frames and Drawcode

        #region Death Effects
        public override bool CheckDead(NPC npc) => ExoMechManagement.HandleDeathEffects(npc);
        #endregion Death Effects
    }
}
