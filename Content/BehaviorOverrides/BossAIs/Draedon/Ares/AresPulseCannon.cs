using CalamityMod;
using CalamityMod.Items.Weapons.DraedonsArsenal;
using CalamityMod.NPCs;
using CalamityMod.Particles;
using CalamityMod.Sounds;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.ComboAttacks;
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
using CalamityModClass = CalamityMod.CalamityMod;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class AresPulseCannon : ModNPC
    {
        public AresCannonChargeParticleSet EnergyDrawer = new(-1, 15, 40f, Color.Fuchsia);

        public ThanatosSmokeParticleSet SmokeDrawer = new(-1, 3, 0f, 16f, 1.5f);

        public static NPC Ares => AresCannonBehaviorOverride.Ares;

        public static int TotalPulseBlastsPerBurst
        {
            get
            {
                int totalPulseBlastsPerBurst = 4;

                if (ExoMechManagement.CurrentAresPhase >= 5)
                    totalPulseBlastsPerBurst += 2;
                if (ExoMechManagement.CurrentAresPhase >= 6)
                    totalPulseBlastsPerBurst++;
                if (Ares.ai[0] == (int)AresBodyBehaviorOverride.AresBodyAttackType.PhotonRipperSlashes)
                    totalPulseBlastsPerBurst = 2;

                return totalPulseBlastsPerBurst;
            }
        }

        public static float AimPredictiveness =>
            ExoMechManagement.CurrentAresPhase >= 5 ? 32.5f : 27f;

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
            // Die if Ares is not present.
            if (CalamityGlobalNPC.draedonExoMechPrime == -1)
            {
                NPC.life = 0;
                NPC.HitEffect();
                NPC.active = false;
                return;
            }

            // Update the energy drawers.
            EnergyDrawer.Update();
            SmokeDrawer.Update();

            NPC.dontTakeDamage = false;
            // Inherit a bunch of attributes such as opacity from the body.
            ExoMechAIUtilities.HaveArmsInheritAresBodyAttributes(NPC);

            // Ensure this does not take damage in the desperation attack.
            if (Ares.ai[0] == (int)AresBodyAttackType.PrecisionBlasts)
                NPC.dontTakeDamage = true;

            bool performingDeathAnimation = ExoMechAIUtilities.PerformingDeathAnimation(NPC);
            Player target = Main.player[NPC.target];

            // Define attack variables.
            int shootTime = 180;
            int shootRate = shootTime / TotalPulseBlastsPerBurst;
            bool currentlyDisabled = AresBodyBehaviorOverride.ArmIsDisabled(NPC);
            ref float attackTimer = ref NPC.ai[0];
            ref float chargeDelay = ref NPC.ai[1];
            ref float currentDirection = ref NPC.ai[3];
            ref float shouldPrepareToFire = ref NPC.Infernum().ExtraAI[1];
            ref float telegraphSound = ref NPC.Infernum().ExtraAI[2];

            // Initialize delays and other timers.
            shouldPrepareToFire = 0f;
            if (chargeDelay == 0f)
                chargeDelay = AresBodyBehaviorOverride.Phase1ArmChargeupTime;

            // Don't do anything if this arm should be disabled.
            if (currentlyDisabled)
                attackTimer = 1f;

            // Inherit the attack timer from Ares if he's performing the ultimate attack.
            bool doingUltimateAttack = Ares.ai[0] == (int)AresBodyBehaviorOverride.AresBodyAttackType.PrecisionBlasts && Ares.Infernum().ExtraAI[9] >= 1f;
            if (doingUltimateAttack)
            {
                chargeDelay = (int)Ares.Infernum().ExtraAI[2];
                attackTimer = Ares.Infernum().ExtraAI[4];
                shootRate = 1;
                shootTime = 1;
            }

            // Hover near Ares.
            bool performingCharge = Ares.ai[0] == (int)AresBodyBehaviorOverride.AresBodyAttackType.HoverCharge && !performingDeathAnimation;
            Vector2 hoverOffset = PerformHoverMovement(NPC, performingCharge);

            // Update the telegraph outline intensity timer.
            NPC.Infernum().ExtraAI[0] = MathHelper.Clamp(NPC.Infernum().ExtraAI[0] + performingCharge.ToDirectionInt(), 0f, 15f);

            // Check to see if Ares is in the middle of a death animation. If he is, participate in the death animation.
            if (performingDeathAnimation)
            {
                AresBodyBehaviorOverride.HaveArmPerformDeathAnimation(NPC, hoverOffset);
                return;
            }

            // Check to see if this arm should be used for special things in a combo attack.
            if (AresCannonBehaviorOverride.IsInUseByComboAttack(NPC))
                return;

            // Calculate the direction and rotation this arm should use.
            Vector2 predictivenessFactor = Vector2.One * AimPredictiveness;
            if (doingUltimateAttack)
            {
                predictivenessFactor.X *= 0.6f;
                predictivenessFactor.Y *= 0.33f;
            }

            Vector2 aimDirection = NPC.SafeDirectionTo(target.Center + target.velocity * predictivenessFactor);
            ExoMechAIUtilities.PerformAresArmDirectioning(NPC, Ares, target, aimDirection, currentlyDisabled, performingCharge, ref currentDirection);

            float rotationToEndOfCannon = NPC.rotation;
            if (rotationToEndOfCannon < 0f)
                rotationToEndOfCannon += MathHelper.Pi;
            Vector2 endOfCannon = NPC.Center + rotationToEndOfCannon.ToRotationVector2() * 74f + Vector2.UnitY * 8f;

            // Play a sound telegraph before firing.
            int telegraphTime = Math.Max((int)chargeDelay - InfernumSoundRegistry.AresTelegraphSoundLength, 2);
            if (attackTimer == telegraphTime && !currentlyDisabled)
                telegraphSound = SoundEngine.PlaySound(InfernumSoundRegistry.AresPulseCannonChargeSound with { Volume = 1.6f }, NPC.Center).ToFloat();

            // Update the sound telegraph's position.
            if (SoundEngine.TryGetActiveSound(SlotId.FromFloat(telegraphSound), out var t) && t.IsPlaying)
            {
                t.Position = NPC.Center;
                if (performingCharge)
                    t.Stop();
            }

            // Create a dust telegraph before firing.
            if (attackTimer > chargeDelay * 0.7f && attackTimer < chargeDelay)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector2 offset = Main.rand.NextVector2Circular(30f, 30f);
                    Dust.NewDustPerfect(endOfCannon + offset, 234, Main.rand.NextVector2Circular(5f, 5f), 0, default, 1.35f).noGravity = true;
                    Dust.NewDustPerfect(endOfCannon - offset, 234, Main.rand.NextVector2Circular(5f, 5f), 0, default, 1.35f).noGravity = true;
                }
            }

            // Decide the state of the particle drawers.
            EnergyDrawer.ParticleSpawnRate = int.MaxValue;
            SmokeDrawer.ParticleSpawnRate = int.MaxValue;
            if (attackTimer > chargeDelay * 0.45f)
            {
                shouldPrepareToFire = 1f;
                float chargeCompletion = MathHelper.Clamp(attackTimer / chargeDelay, 0f, 1f);
                EnergyDrawer.ParticleSpawnRate = 3;
                EnergyDrawer.SpawnAreaCompactness = 100f;
                EnergyDrawer.chargeProgress = chargeCompletion;

                if (attackTimer % 15f == 14f && chargeCompletion < 1f)
                    EnergyDrawer.AddPulse(chargeCompletion * 6f);
            }
            if (Ares.localAI[3] >= 0.36f)
            {
                SmokeDrawer.ParticleSpawnRate = 1;
                SmokeDrawer.BaseMoveRotation = MathHelper.PiOver2;
                SmokeDrawer.SpawnAreaCompactness = 40f;
            }

            // Fire lasers.
            if (attackTimer >= chargeDelay && attackTimer % shootRate == shootRate - 1f)
            {
                SoundEngine.PlaySound(PulseRifle.FireSound, NPC.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (!doingUltimateAttack)
                    {
                        int blastDamage = AresBodyBehaviorOverride.ProjectileDamageBoost + DraedonBehaviorOverride.StrongerNormalShotDamage;
                        Vector2 blastShootVelocity = aimDirection * 7.5f;
                        Vector2 blastSpawnPosition = endOfCannon + blastShootVelocity * 8.4f;
                        Utilities.NewProjectileBetter(blastSpawnPosition, blastShootVelocity, ModContent.ProjectileType<AresPulseBlast>(), blastDamage, 0f);
                    }
                    else
                        Utilities.NewProjectileBetter(endOfCannon, aimDirection, ModContent.ProjectileType<AresPrecisionBlast>(), DraedonBehaviorOverride.PowerfulShotDamage, 0f, -1, NPC.whoAmI);

                    NPC.netUpdate = true;
                }
            }

            // Reset the attack and laser counter after an attack cycle ends.
            if (attackTimer >= chargeDelay + shootTime)
            {
                attackTimer = 0f;
                NPC.netUpdate = true;
            }
            attackTimer++;
        }

        public static Vector2 PerformHoverMovement(NPC npc, bool performingCharge)
        {
            float backArmDirection = (Ares.Infernum().ExtraAI[ExoMechManagement.Ares_BackArmsAreSwappedIndex] != 1f).ToDirectionInt();
            Vector2 hoverOffset = new(backArmDirection * 575f, 0f);
            if (performingCharge)
                hoverOffset = new(backArmDirection * 380f, 150f);

            Vector2 hoverDestination = Ares.Center + hoverOffset;
            ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 64f, 115f);

            return hoverOffset;
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

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            SpriteEffects direction = SpriteEffects.None;
            if (NPC.spriteDirection == 1)
                direction = SpriteEffects.FlipHorizontally;

            // Locate Ares' body for reference with certain AI attributes.
            Texture2D texture = TextureAssets.Npc[NPC.type].Value;
            Rectangle frame = NPC.frame;
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 center = NPC.Center - Main.screenPosition;
            bool enraged = AresBodyBehaviorOverride.Enraged || ExoMechComboAttackContent.EnrageTimer > 0f;
            Color glowmaskColor = enraged ? Color.Red : Color.White;

            // Use the heat effect, just like the body.
            lightColor = Color.Lerp(lightColor, Color.Red with { A = 100 }, Ares.localAI[3] * 0.48f);

            // Draw telegraphs if necessary during the ultimate attack.
            float telegraphIntensity = 0f;
            if (Ares.ai[0] == (int)AresBodyBehaviorOverride.AresBodyAttackType.PrecisionBlasts)
                telegraphIntensity = Ares.Infernum().ExtraAI[4] / Ares.Infernum().ExtraAI[2];

            if (telegraphIntensity > 0f)
            {
                Main.spriteBatch.SetBlendState(BlendState.Additive);

                Texture2D line = InfernumTextureRegistry.BloomLine.Value;
                Color outlineColor = Color.Lerp(Color.Red, Color.White, telegraphIntensity) * Utils.GetLerpValue(1f, 0.7f, telegraphIntensity, true);
                Vector2 beamOrigin = new(line.Width / 2f, line.Height);
                Vector2 beamScale = new(telegraphIntensity * 0.5f, 2.4f);
                Vector2 beamDirection = NPC.rotation.ToRotationVector2();
                float beamRotation = beamDirection.ToRotation() - MathHelper.PiOver2 * NPC.spriteDirection;
                Vector2 beamCenter = center - beamDirection.RotatedBy(-MathHelper.PiOver2) * NPC.scale * 10f;

                Main.spriteBatch.Draw(line, beamCenter, null, outlineColor, beamRotation, beamOrigin, beamScale, 0, 0f);

                Main.spriteBatch.ResetBlendState();
            }

            // Draw backglow effects, telegraphs, and the base texture.
            ExoMechAIUtilities.DrawFinalPhaseGlow(spriteBatch, NPC, texture, center, frame, origin);
            ExoMechAIUtilities.DrawAresArmTelegraphEffect(spriteBatch, NPC, Color.Violet, texture, center, frame, origin);
            Main.spriteBatch.Draw(texture, center, frame, NPC.GetAlpha(lightColor), NPC.rotation, origin, NPC.scale, direction, 0f);

            // Draw glowmasks.
            texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Draedon/Ares/AresPulseCannonGlow").Value;

            // Draw the main texture.
            Main.spriteBatch.Draw(texture, center, frame, glowmaskColor * NPC.Opacity, NPC.rotation, origin, NPC.scale, direction, 0f);

            // Draw energy effects for telegraph purposes.
            Main.spriteBatch.SetBlendState(BlendState.Additive);

            Vector2 coreDrawPosition = CoreSpritePosition;
            if (NPC.Infernum().ExtraAI[1] == 1f)
                EnergyDrawer.DrawBloom(coreDrawPosition);
            EnergyDrawer.DrawPulses(coreDrawPosition);
            EnergyDrawer.DrawSet(coreDrawPosition);
            SmokeDrawer.DrawSet(coreDrawPosition);

            return false;
        }

        public override bool CheckDead() => ExoMechManagement.HandleDeathEffects(NPC);

        public override bool CheckActive() => false;
    }
}
