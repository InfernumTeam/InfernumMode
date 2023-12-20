using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.Particles;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.ComboAttacks;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares.AresBodyBehaviorOverride;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares
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

        public override int? NPCTypeToDeferToForTips => ModContent.NPCType<AresBody>();

        #region AI
        public override bool PreAI(NPC npc)
        {
            // Die if Ares is not present.
            if (CalamityGlobalNPC.draedonExoMechPrime == -1)
            {
                npc.life = 0;
                npc.active = false;
                return false;
            }

            // Update the energy drawers.
            ThanatosSmokeParticleSet smokeDrawer = GetSmokeDrawer(npc);
            smokeDrawer.Update();

            AresCannonChargeParticleSet energyDrawer = GetEnergyDrawer(npc);
            energyDrawer.Update();

            // Ensure the cannon does not take damage during the desperation attack.
            npc.dontTakeDamage = false;
            if (Ares.ai[0] == (int)AresBodyAttackType.PrecisionBlasts)
                npc.dontTakeDamage = true;

            // Inherit a bunch of attributes such as opacity from the body.
            ExoMechAIUtilities.HaveArmsInheritAresBodyAttributes(npc);

            bool performingDeathAnimation = ExoMechAIUtilities.PerformingDeathAnimation(npc);
            Player target = Main.player[npc.target];

            // Define attack variables.
            int shootTime = ShootTime;
            int shootRate = ShootRate;
            bool currentlyDisabled = ArmIsDisabled(npc);
            ref float attackTimer = ref npc.ai[0];
            ref float chargeDelay = ref npc.ai[1];
            ref float currentDirection = ref npc.ai[3];
            ref float shouldPrepareToFire = ref npc.Infernum().ExtraAI[1];
            ref float telegraphSound = ref npc.Infernum().ExtraAI[2];

            // Initialize delays and other timers.
            shouldPrepareToFire = 0f;
            if (chargeDelay == 0f)
                chargeDelay = Phase1ArmChargeupTime;

            // Don't do anything if this arm should be disabled.
            if (currentlyDisabled)
                attackTimer = 1f;

            // Inherit the attack timer from Ares if he's performing the ultimate attack.
            bool doingUltimateAttack = Ares.ai[0] == (int)AresBodyAttackType.PrecisionBlasts && Ares.Infernum().ExtraAI[9] >= 1f;
            if (doingUltimateAttack)
            {
                chargeDelay = (int)Ares.Infernum().ExtraAI[2];
                attackTimer = Ares.Infernum().ExtraAI[4];
                shootRate = 1;
                shootTime = 1;
            }

            // Hover near Ares.
            bool performingCharge = false;
            Vector2 hoverOffset = PerformHoverMovement(npc, performingCharge);

            // Update the telegraph outline intensity timer.
            npc.Infernum().ExtraAI[0] = Clamp(npc.Infernum().ExtraAI[0] + performingCharge.ToDirectionInt(), 0f, 15f);

            // Check to see if Ares is in the middle of a death animation. If he is, participate in the death animation.
            if (performingDeathAnimation)
                return false;

            // Check to see if this arm should be used for special things in a combo attack.
            if (IsInUseByComboAttack(npc))
                return false;

            // Calculate the direction and rotation this arm should use.
            Vector2 predictivenessFactor = Vector2.One * AimPredictiveness;
            if (doingUltimateAttack)
            {
                predictivenessFactor.X *= 0.6f;
                predictivenessFactor.Y *= 0.33f;
            }
            Vector2 aimDirection = npc.SafeDirectionTo(target.Center + target.velocity * predictivenessFactor);
            Vector2 endOfCannon = GetEndOfCannon(npc, target, aimDirection, currentlyDisabled, performingCharge, ref currentDirection);

            // Play a sound telegraph before firing.
            HandleTelegraphSounds(npc, FireTelegraphSound, currentlyDisabled, performingCharge, attackTimer, chargeDelay, ref telegraphSound);

            // Create a dust telegraph before firing.
            if (attackTimer > chargeDelay * 0.7f && attackTimer < chargeDelay)
                CreateDustTelegraphs(npc, endOfCannon);

            // Decide the state of the particle drawers.
            if (UpdateParticleDrawers(smokeDrawer, energyDrawer, attackTimer, chargeDelay))
                shouldPrepareToFire = 1f;

            // Shoot projectiles.
            if (attackTimer >= chargeDelay && attackTimer % shootRate == shootRate - 1f)
            {
                SoundEngine.PlaySound(ShootSound, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (!doingUltimateAttack)
                        ShootProjectiles(npc, endOfCannon, aimDirection);
                    else
                        Utilities.NewProjectileBetter(endOfCannon, aimDirection, ModContent.ProjectileType<AresPrecisionBlast>(), DraedonBehaviorOverride.PowerfulShotDamage, 0f, -1, npc.whoAmI);

                    npc.netUpdate = true;
                }
            }

            // Reset the attack and laser counter after an attack cycle ends.
            if (attackTimer >= chargeDelay + shootTime)
            {
                attackTimer = 0f;
                ResetAttackCycleEffects(npc);
                npc.netUpdate = true;
            }
            attackTimer++;
            return false;
        }

        public static Vector2 GetEndOfCannon(NPC npc, Player target, Vector2 aimDirection, bool currentlyDisabled, bool performingCharge, ref float currentDirection)
        {
            ExoMechAIUtilities.PerformAresArmDirectioning(npc, Ares, target, aimDirection, currentlyDisabled, performingCharge, ref currentDirection);

            float rotationToEndOfCannon = npc.rotation;
            if (rotationToEndOfCannon < 0f)
                rotationToEndOfCannon += Pi;
            return npc.Center + rotationToEndOfCannon.ToRotationVector2() * 74f + Vector2.UnitY * 8f;
        }

        public static void HandleTelegraphSounds(NPC npc, SoundStyle fireTelegraphSound, bool currentlyDisabled, bool stopSound, float attackTimer, float chargeDelay, ref float telegraphSound)
        {
            int telegraphTime = Math.Max((int)chargeDelay - InfernumSoundRegistry.AresTelegraphSoundLength, 2);
            if (attackTimer == telegraphTime && !currentlyDisabled)
                telegraphSound = SoundEngine.PlaySound(fireTelegraphSound with { Volume = 1.6f }, npc.Center).ToFloat();

            // Update the sound telegraph's position.
            if (SoundEngine.TryGetActiveSound(SlotId.FromFloat(telegraphSound), out var t) && t.IsPlaying)
            {
                t.Position = npc.Center;
                if (stopSound)
                    t.Stop();
            }
        }

        public static bool UpdateParticleDrawers(ThanatosSmokeParticleSet smokeDrawer, AresCannonChargeParticleSet energyDrawer, float attackTimer, float chargeDelay)
        {
            smokeDrawer.ParticleSpawnRate = int.MaxValue;
            energyDrawer.ParticleSpawnRate = int.MaxValue;
            bool charging = attackTimer > chargeDelay * 0.45f;
            if (charging)
            {
                float chargeCompletion = Clamp(attackTimer / chargeDelay, 0f, 1f);
                energyDrawer.ParticleSpawnRate = 3;
                energyDrawer.SpawnAreaCompactness = 100f;
                energyDrawer.chargeProgress = chargeCompletion;

                if (attackTimer % 15f == 14f && chargeCompletion < 1f)
                    energyDrawer.AddPulse(chargeCompletion * 6f);
            }
            if (Ares.localAI[3] >= 0.36f)
            {
                smokeDrawer.ParticleSpawnRate = 1;
                smokeDrawer.BaseMoveRotation = PiOver2;
                smokeDrawer.SpawnAreaCompactness = 40f;
            }

            return charging;
        }

        public static Vector2 PerformHoverMovement(NPC npc, bool performingCharge)
        {
            Vector2 hoverOffset = npc.BehaviorOverride<AresCannonBehaviorOverride>().GetHoverOffset(npc, performingCharge) * Ares.scale;
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
            int currentFrame = (int)Math.Round(Lerp(0f, 35f, npc.ai[0] / npc.ai[1]));

            if (npc.ai[0] > npc.ai[1])
            {
                npc.frameCounter++;
                if (npc.frameCounter >= 66f)
                    npc.frameCounter = 0D;
                currentFrame = (int)Math.Round(Lerp(36f, 47f, (float)npc.frameCounter / 66f));
            }
            else
                npc.frameCounter = 0D;

            if (ExoMechComboAttackContent.ArmCurrentlyBeingUsed(npc))
                currentFrame = (int)Math.Round(Lerp(0f, 35f, npc.ai[0] % 72f / 72f));

            npc.frame = new Rectangle(npc.width * (currentFrame / 8), npc.height * (currentFrame % 8), npc.width, npc.height);
        }

        public static void DrawUltimateAttackTelegraphs(NPC npc, Vector2 drawPosition)
        {
            // Draw telegraphs if necessary during the ultimate attack.
            float telegraphIntensity = 0f;
            if (CalamityGlobalNPC.draedonExoMechPrime != -1 && Ares.ai[0] == (int)AresBodyAttackType.PrecisionBlasts)
                telegraphIntensity = Ares.Infernum().ExtraAI[4] / Ares.Infernum().ExtraAI[2];

            if (telegraphIntensity > 0f && npc.type != ModContent.NPCType<AresEnergyKatana>())
            {
                Main.spriteBatch.SetBlendState(BlendState.Additive);

                Texture2D line = InfernumTextureRegistry.BloomLine.Value;
                Color outlineColor = Color.Lerp(Color.Red, Color.White, telegraphIntensity) * Utils.GetLerpValue(1f, 0.7f, telegraphIntensity, true);
                Vector2 beamOrigin = new(line.Width / 2f, line.Height);
                Vector2 beamScale = new(telegraphIntensity * 0.5f, 2.4f);
                Vector2 beamDirection = npc.rotation.ToRotationVector2();
                float beamRotation = beamDirection.ToRotation() - PiOver2 * npc.spriteDirection;
                Vector2 beamCenter = drawPosition - beamDirection.RotatedBy(-PiOver2) * npc.scale * 10f;
                Main.spriteBatch.Draw(line, beamCenter, null, outlineColor, beamRotation, beamOrigin, beamScale, 0, 0f);

                Main.spriteBatch.ResetBlendState();
            }
        }

        public static void DrawCannon(NPC npc, string glowmaskTexturePath, Color telegraphBackglowColor, Color lightColor, Vector2 coreDrawPosition, AresCannonChargeParticleSet energyDrawer, ThanatosSmokeParticleSet smokeDrawer)
        {
            SpriteEffects direction = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                direction = SpriteEffects.FlipHorizontally;

            // Locate Ares' body for reference with certain AI attributes.
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Rectangle frame = npc.frame;
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            bool enraged = Enraged || ExoMechComboAttackContent.EnrageTimer > 0f;
            Color glowmaskColor = enraged ? Color.Red : Color.White;

            // Use the heat effect, just like the body.
            if (CalamityGlobalNPC.draedonExoMechPrime != -1)
                lightColor = Color.Lerp(lightColor, Color.Red with { A = 100 }, Ares.localAI[3] * 0.48f);

            // Draw telegraphs if necessary during the ultimate attack.
            DrawUltimateAttackTelegraphs(npc, drawPosition);

            // Draw backglow effects, telegraphs, and the base texture.
            ExoMechAIUtilities.DrawFinalPhaseGlow(npc, texture, drawPosition, frame, origin);
            ExoMechAIUtilities.DrawAresArmTelegraphEffect(npc, telegraphBackglowColor, texture, drawPosition, frame, origin);
            Main.spriteBatch.Draw(texture, drawPosition, frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, direction, 0f);

            // Draw glowmasks.
            texture = ModContent.Request<Texture2D>(glowmaskTexturePath).Value;

            // Draw the main texture.
            Main.spriteBatch.Draw(texture, drawPosition, frame, glowmaskColor * npc.Opacity, npc.rotation, origin, npc.scale, direction, 0f);

            // Draw energy effects for telegraph purposes.
            Main.spriteBatch.SetBlendState(BlendState.Additive);

            if (npc.Infernum().ExtraAI[1] == 1f)
                energyDrawer.DrawBloom(coreDrawPosition);
            energyDrawer.DrawPulses(coreDrawPosition);
            energyDrawer.DrawSet(coreDrawPosition);
            smokeDrawer.DrawSet(coreDrawPosition);

            Main.spriteBatch.ResetBlendState();
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            DrawCannon(npc, GlowmaskTexturePath, TelegraphBackglowColor, lightColor, GetCoreSpritePosition(npc), GetEnergyDrawer(npc), GetSmokeDrawer(npc));
            return false;
        }

        public abstract AresCannonChargeParticleSet GetEnergyDrawer(NPC npc);

        public abstract ThanatosSmokeParticleSet GetSmokeDrawer(NPC npc);

        public abstract Vector2 GetCoreSpritePosition(NPC npc);
        #endregion Frames and Drawcode

        #region Death Effects
        public override bool CheckDead(NPC npc) => ExoMechManagement.HandleDeathEffects(npc);
        #endregion Death Effects
    }
}
