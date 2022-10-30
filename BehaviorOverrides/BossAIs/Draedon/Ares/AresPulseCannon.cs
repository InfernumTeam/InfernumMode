using CalamityMod;
using CalamityMod.Items.Weapons.DraedonsArsenal;
using CalamityMod.NPCs;
using CalamityMod.Particles;
using CalamityMod.Sounds;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.ComboAttacks;
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

using CalamityModClass = CalamityMod.CalamityMod;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class AresPulseCannon : ModNPC
    {
        public AresCannonChargeParticleSet EnergyDrawer = new(-1, 15, 40f, Color.Fuchsia);

        public ref float AttackTimer => ref NPC.ai[0];

        public ref float ChargeDelay => ref NPC.ai[1];

        public Vector2 CoreSpritePosition => NPC.Center + NPC.spriteDirection * NPC.rotation.ToRotationVector2() * 35f + (NPC.rotation + MathHelper.PiOver2).ToRotationVector2() * 5f;

        // This stores the sound slot of the telegraph sound it makes, so it may be properly updated in terms of position.
        public SlotId TelegraphSoundSlot;

        public override void SetStaticDefaults()
        {
            this.HideFromBestiary();
            DisplayName.SetDefault("XF-09 Ares Pulse Cannon");
            Main.npcFrameCount[NPC.type] = 12;
            NPCID.Sets.TrailingMode[NPC.type] = 3;
            NPCID.Sets.TrailCacheLength[NPC.type] = NPC.oldPos.Length;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 5f;
            NPC.damage = 0;
            NPC.width = 170;
            NPC.height = 120;
            NPC.defense = 80;
            NPC.DR_NERD(0.35f);
            NPC.LifeMaxNERB(1250000, 1495000, 500000);
            double HPBoost = CalamityConfig.Instance.BossHealthBoost * 0.01;
            NPC.lifeMax += (int)(NPC.lifeMax * HPBoost);
            NPC.aiStyle = -1;
            AIType = -1;
            NPC.Opacity = 0f;
            NPC.knockBackResist = 0f;
            NPC.canGhostHeal = false;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.HitSound = null;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.netAlways = true;
            NPC.hide = true;
            Music = (InfernumMode.CalamityMod as CalamityModClass).GetMusicFromMusicMod("ExoMechs") ?? MusicID.Boss3;
        }

        public override void AI()
        {
            if (CalamityGlobalNPC.draedonExoMechPrime < 0)
            {
                NPC.life = 0;
                NPC.HitEffect();
                NPC.active = false;
                return;
            }

            // Update the energy drawer.
            EnergyDrawer.Update();

            // Locate Ares' body as an npc.
            NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];
            ExoMechAIUtilities.HaveArmsInheritAresBodyAttributes(NPC);

            bool performingDeathAnimation = ExoMechAIUtilities.PerformingDeathAnimation(NPC);
            Player target = Main.player[NPC.target];

            // Disable HP bars.
            NPC.Calamity().ShouldCloseHPBar = true;

            // Define attack variables.
            bool currentlyDisabled = AresBodyBehaviorOverride.ArmIsDisabled(NPC);
            int shootTime = 180;
            int totalPulseBlastsPerBurst = 4;
            float blastShootSpeed = 7.5f;
            float aimPredictiveness = 27f;
            ref float shouldPrepareToFire = ref NPC.Infernum().ExtraAI[1];

            // Nerf things while Ares' complement mech is present.
            if (ExoMechManagement.CurrentAresPhase == 4)
                blastShootSpeed *= 0.85f;

            if (ExoMechManagement.CurrentAresPhase >= 5)
            {
                shootTime += 60;
                totalPulseBlastsPerBurst += 2;
                blastShootSpeed *= 1.25f;
                aimPredictiveness += 5.5f;
            }
            if (ExoMechManagement.CurrentAresPhase >= 6)
            {
                shootTime -= 30;
                totalPulseBlastsPerBurst++;
            }
            if (aresBody.ai[0] == (int)AresBodyBehaviorOverride.AresBodyAttackType.PhotonRipperSlashes)
            {
                totalPulseBlastsPerBurst = 2;
                blastShootSpeed -= 1.96f;
            }

            // Get very pissed off if Ares is enraged.
            if (aresBody.Infernum().ExtraAI[13] == 1f)
                totalPulseBlastsPerBurst += 5;

            int shootRate = shootTime / totalPulseBlastsPerBurst;

            // Initialize delays and other timers.
            shouldPrepareToFire = 0f;
            if (ChargeDelay == 0f)
                ChargeDelay = AresBodyBehaviorOverride.Phase1ArmChargeupTime;

            // Don't do anything if this arm should be disabled.
            if (currentlyDisabled)
                AttackTimer = 1f;

            // Become more resistant to damage as necessary.
            NPC.takenDamageMultiplier = 1f;
            if (ExoMechManagement.ShouldHaveSecondComboPhaseResistance(NPC))
                NPC.takenDamageMultiplier *= 0.5f;

            // Hover near Ares.
            bool doingHoverCharge = aresBody.ai[0] == (int)AresBodyBehaviorOverride.AresBodyAttackType.HoverCharge && !performingDeathAnimation;
            float horizontalOffset = doingHoverCharge ? 380f : 575f;
            float verticalOffset = doingHoverCharge ? 150f : 0f;
            Vector2 hoverDestination = aresBody.Center + new Vector2((aresBody.Infernum().ExtraAI[15] == 1f ? -1f : 1f) * horizontalOffset, verticalOffset);
            ExoMechAIUtilities.DoSnapHoverMovement(NPC, hoverDestination, 65f, 115f);
            NPC.Infernum().ExtraAI[0] = MathHelper.Clamp(NPC.Infernum().ExtraAI[0] + doingHoverCharge.ToDirectionInt(), 0f, 15f);

            // Check to see if Ares is in the middle of a death animation. If it is, participate in the death animation.
            if (performingDeathAnimation)
            {
                AresBodyBehaviorOverride.HaveArmPerformDeathAnimation(NPC, new Vector2(horizontalOffset, verticalOffset));
                return;
            }

            // Check to see if this arm should be used for special things in a combo attack.
            float _ = 0f;
            if (ExoMechComboAttackContent.ArmCurrentlyBeingUsed(NPC))
            {
                ExoMechComboAttackContent.UseThanatosAresComboAttack(NPC, ref aresBody.ai[1], ref _);
                ExoMechComboAttackContent.UseTwinsAresComboAttack(NPC, 1f, ref aresBody.ai[1], ref _);
                return;
            }

            // Play a sound telegraph before firing.
            int telegraphTime = Math.Max((int)ChargeDelay - InfernumSoundRegistry.AresTelegraphSoundLength, 2);
            if (AttackTimer == telegraphTime && !currentlyDisabled)
                TelegraphSoundSlot = SoundEngine.PlaySound(InfernumSoundRegistry.AresPulseCannonChargeSound with { Volume = 1.6f }, NPC.Center);

            // Update the sound telegraph's position.
            if (SoundEngine.TryGetActiveSound(TelegraphSoundSlot, out var t) && t.IsPlaying)
            {
                t.Position = NPC.Center;
                if (doingHoverCharge)
                    t.Stop();
            }

            // Calculate the direction and rotation this arm should use.
            Vector2 aimDirection = NPC.SafeDirectionTo(target.Center + target.velocity * aimPredictiveness);
            ExoMechAIUtilities.PerformAresArmDirectioning(NPC, aresBody, target, aimDirection, currentlyDisabled, doingHoverCharge, ref _);
            float rotationToEndOfCannon = NPC.rotation;
            if (rotationToEndOfCannon < 0f)
                rotationToEndOfCannon += MathHelper.Pi;
            Vector2 endOfCannon = NPC.Center + rotationToEndOfCannon.ToRotationVector2() * 66f + Vector2.UnitY * 16f;

            // Create a dust telegraph before firing.
            if (AttackTimer > ChargeDelay * 0.7f && AttackTimer < ChargeDelay)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector2 offset = Main.rand.NextVector2Circular(30f, 30f);
                    Dust.NewDustPerfect(endOfCannon + offset, 234, Main.rand.NextVector2Circular(5f, 5f), 0, default, 1.35f).noGravity = true;
                    Dust.NewDustPerfect(endOfCannon - offset, 234, Main.rand.NextVector2Circular(5f, 5f), 0, default, 1.35f).noGravity = true;
                }
            }

            // Decide the state of the particle drawer.
            EnergyDrawer.ParticleSpawnRate = 99999999;
            if (AttackTimer > ChargeDelay * 0.45f)
            {
                shouldPrepareToFire = 1f;
                float chargeCompletion = MathHelper.Clamp(AttackTimer / ChargeDelay, 0f, 1f);
                EnergyDrawer.ParticleSpawnRate = 3;
                EnergyDrawer.SpawnAreaCompactness = 100f;
                EnergyDrawer.chargeProgress = chargeCompletion;

                if (AttackTimer % 15f == 14f && chargeCompletion < 1f)
                    EnergyDrawer.AddPulse(chargeCompletion * 6f);
            }

            // Fire a pulse blast.
            if (AttackTimer >= ChargeDelay && AttackTimer % shootRate == shootRate - 1f)
            {
                SoundEngine.PlaySound(PulseRifle.FireSound, NPC.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int blastDamage = AresBodyBehaviorOverride.ProjectileDamageBoost + DraedonBehaviorOverride.StrongerNormalShotDamage;
                    Vector2 blastShootVelocity = aimDirection * blastShootSpeed;
                    Vector2 blastSpawnPosition = endOfCannon + blastShootVelocity * 8.4f;
                    Utilities.NewProjectileBetter(blastSpawnPosition, blastShootVelocity, ModContent.ProjectileType<AresPulseBlast>(), blastDamage, 0f);

                    NPC.netUpdate = true;
                }
            }

            // Reset the attack timer after an attack cycle ends.
            if (AttackTimer >= ChargeDelay + shootTime)
            {
                AttackTimer = 0f;
                NPC.netUpdate = true;
            }
            AttackTimer++;
        }

        public override void DrawBehind(int index)
        {
            Main.instance.DrawCacheNPCProjectiles.Add(index);
        }

        public override void FindFrame(int frameHeight)
        {
            int currentFrame = (int)Math.Round(MathHelper.Lerp(0f, 35f, NPC.ai[0] / NPC.ai[1]));

            if (NPC.ai[0] > NPC.ai[1])
            {
                NPC.frameCounter++;
                if (NPC.frameCounter >= 66f)
                    NPC.frameCounter = 0D;
                currentFrame = (int)Math.Round(MathHelper.Lerp(36f, 47f, (float)NPC.frameCounter / 66f));
            }
            else
                NPC.frameCounter = 0D;

            if (ExoMechComboAttackContent.ArmCurrentlyBeingUsed(NPC))
                currentFrame = (int)Math.Round(MathHelper.Lerp(0f, 35f, NPC.ai[0] % 72f / 72f));

            NPC.frame = new Rectangle(currentFrame / 12 * 150, currentFrame % 12 * 148, 150, 148);
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            if (NPC.soundDelay == 1)
            {
                NPC.soundDelay = 3;
                SoundEngine.PlaySound(CommonCalamitySounds.ExoHitSound, NPC.Center);
            }

            for (int k = 0; k < 3; k++)
                Dust.NewDust(NPC.position, NPC.width, NPC.height, 107, 0f, 0f, 100, new Color(0, 255, 255), 1f);

            if (NPC.life <= 0)
            {
                for (int i = 0; i < 2; i++)
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 107, 0f, 0f, 100, new Color(0, 255, 255), 1.5f);

                for (int i = 0; i < 20; i++)
                {
                    Dust exoEnergy = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 107, 0f, 0f, 0, new Color(0, 255, 255), 2.5f);
                    exoEnergy.noGravity = true;
                    exoEnergy.velocity *= 3f;

                    exoEnergy = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 107, 0f, 0f, 100, new Color(0, 255, 255), 1.5f);
                    exoEnergy.velocity *= 2f;
                    exoEnergy.noGravity = true;
                }

                if (Main.netMode != NetmodeID.Server)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("AresPulseCannon1").Type, NPC.scale);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, InfernumMode.CalamityMod.Find<ModGore>("AresHandBase1").Type, NPC.scale);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, InfernumMode.CalamityMod.Find<ModGore>("AresHandBase2").Type, NPC.scale);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, InfernumMode.CalamityMod.Find<ModGore>("AresHandBase3").Type, NPC.scale);
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (NPC.Infernum().OptionalPrimitiveDrawer is null)
            {
                NPC.Infernum().OptionalPrimitiveDrawer = new PrimitiveTrailCopy(completionRatio => AresBodyBehaviorOverride.FlameTrailWidthFunctionBig(NPC, completionRatio),
                    completionRatio => AresBodyBehaviorOverride.FlameTrailColorFunctionBig(NPC, completionRatio),
                    null, true, GameShaders.Misc["Infernum:TwinsFlameTrail"]);
            }

            // Don't draw anything if the cannon is detached. The Exowl that has it will draw it manually.
            if (NPC.Infernum().ExtraAI[ExoMechManagement.Ares_CannonInUseByExowl] == 1f)
                return false;

            for (int i = 0; i < 2; i++)
            {
                if (NPC.Infernum().ExtraAI[0] > 0f)
                    NPC.Infernum().OptionalPrimitiveDrawer.Draw(NPC.oldPos, NPC.Size * 0.5f - Main.screenPosition, 54);
            }

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (NPC.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            // Locate Ares' body as an npc.
            NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];
            Texture2D texture = TextureAssets.Npc[NPC.type].Value;
            Rectangle frame = NPC.frame;
            Vector2 origin = NPC.Center - NPC.position;
            Vector2 center = NPC.Center - Main.screenPosition;
            bool enraged = aresBody.Infernum().ExtraAI[13] == 1f || ExoMechComboAttackContent.EnrageTimer > 0f;
            Color afterimageBaseColor = enraged ? Color.Red : Color.White;
            int numAfterimages = 5;

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < numAfterimages; i += 2)
                {
                    Color afterimageColor = NPC.GetAlpha(Color.Lerp(drawColor, afterimageBaseColor, 0.5f)) * ((numAfterimages - i) / 15f);
                    Vector2 afterimageCenter = NPC.oldPos[i] + origin - Main.screenPosition;
                    Main.spriteBatch.Draw(texture, afterimageCenter, NPC.frame, afterimageColor, NPC.oldRot[i], origin, NPC.scale, spriteEffects, 0f);
                }
            }

            ExoMechAIUtilities.DrawFinalPhaseGlow(Main.spriteBatch, NPC, texture, center, frame, origin);
            ExoMechAIUtilities.DrawAresArmTelegraphEffect(Main.spriteBatch, NPC, Color.Violet, texture, center, frame, origin);
            Main.spriteBatch.Draw(texture, center, frame, NPC.GetAlpha(drawColor), NPC.rotation, origin, NPC.scale, spriteEffects, 0f);

            texture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Draedon/Ares/AresPulseCannonGlow").Value;

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < numAfterimages; i += 2)
                {
                    Color afterimageColor = NPC.GetAlpha(Color.Lerp(drawColor, afterimageBaseColor, 0.5f)) * ((numAfterimages - i) / 15f);
                    Vector2 afterimageCenter = NPC.oldPos[i] + origin - Main.screenPosition;
                    Main.spriteBatch.Draw(texture, afterimageCenter, NPC.frame, afterimageColor, NPC.oldRot[i], origin, NPC.scale, spriteEffects, 0f);
                }
            }

            Main.spriteBatch.Draw(texture, center, frame, afterimageBaseColor * NPC.Opacity, NPC.rotation, origin, NPC.scale, spriteEffects, 0f);

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            if (NPC.Infernum().ExtraAI[1] == 1f)
                EnergyDrawer.DrawBloom(CoreSpritePosition);
            EnergyDrawer.DrawPulses(CoreSpritePosition);
            EnergyDrawer.DrawSet(CoreSpritePosition);

            Main.spriteBatch.ResetBlendState();
            return false;
        }

        public override bool CheckActive() => false;
    }
}
