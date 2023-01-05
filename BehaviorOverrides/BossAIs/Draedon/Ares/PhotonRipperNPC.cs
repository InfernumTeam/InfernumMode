using CalamityMod;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.NPCs;
using CalamityMod.Particles;
using CalamityMod.Sounds;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.ComboAttacks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

using CalamityModClass = CalamityMod.CalamityMod;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class PhotonRipperNPC : ModNPC
    {
        public AresCannonChargeParticleSet EnergyDrawer = new(-1, 15, 40f, Color.Red);

        public ref float AttackTimer => ref NPC.ai[0];
        public ref float ChargeDelay => ref NPC.ai[1];
        public Vector2 CoreSpritePosition => NPC.Center + NPC.spriteDirection * NPC.rotation.ToRotationVector2() * 35f + (NPC.rotation + MathHelper.PiOver2).ToRotationVector2() * 5f;

        public override void SetStaticDefaults()
        {
            this.HideFromBestiary();
            DisplayName.SetDefault("XF-09 Ares Photon Ripper");
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
            NPC.scale = 1.25f;
            NPC.Opacity = 0f;
            NPC.knockBackResist = 0f;
            NPC.canGhostHeal = false;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.HitSound = null;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.netAlways = true;
            NPC.boss = true;
            NPC.hide = true;
            NPC.Calamity().canBreakPlayerDefense = true;
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
            float horizontalOffsetDirection = NPC.Infernum().ExtraAI[0];
            bool currentlyDisabled = AresBodyBehaviorOverride.ArmIsDisabled(NPC);
            int chargeCycleTime = 105;
            int chargeDelay = 36;
            int chargeSlowdownTime = 12;
            int crystalsPerBurst = 3;
            int chargeTelegraphTime = (int)(chargeCycleTime * 0.6f);
            int initialChargeupTime = 180;
            float crystalShootSpeed = 11.5f;
            float crystalSpread = 0.61f;
            float chargeSpeed = 40f;
            float wrappedTimer = (AttackTimer + (horizontalOffsetDirection == 1f).ToInt() * chargeCycleTime / 3f - initialChargeupTime) % chargeCycleTime;

            // Rev up and create charge particles prior to firing.
            if (AttackTimer <= initialChargeupTime && !currentlyDisabled)
            {
                wrappedTimer = 1f;
                NPC.Center += Main.rand.NextVector2Circular(6f, 6f);
                if (AttackTimer % 5f == 4f)
                    SoundEngine.PlaySound(SoundID.Item22, NPC.Center);

                bool useElectricity = Main.rand.NextBool(3);
                Dust smoke = Dust.NewDustPerfect(NPC.Center, useElectricity ? 182 : 31);
                smoke.velocity = NPC.rotation.ToRotationVector2() * NPC.spriteDirection * Main.rand.NextFloat(3f, 7f) + Main.rand.NextVector2Circular(2f, 2f);
                smoke.position += smoke.velocity * 3f;
                smoke.scale = 1.7f;
                smoke.noGravity = true;
            }

            bool willCharge = wrappedTimer > chargeCycleTime - chargeTelegraphTime;

            // Determine whether the active chainsaw frames should be used or not.
            NPC.Infernum().ExtraAI[1] = willCharge.ToInt();

            // Get very pissed off if Ares is enraged.
            if (aresBody.Infernum().ExtraAI[13] == 1f)
            {
                chargeCycleTime = 85;
                chargeDelay = 25;
                crystalsPerBurst = 7;
                chargeSpeed = 75f;
            }

            // Initialize delays and other timers.
            if (ChargeDelay == 0f)
                ChargeDelay = AresBodyBehaviorOverride.Phase1ArmChargeupTime;

            // Don't do anything if this arm should be disabled.
            if (currentlyDisabled)
            {
                AttackTimer = 1f;
                wrappedTimer = 1f;
                willCharge = false;
            }

            // Become more resistant to damage as necessary.
            NPC.takenDamageMultiplier = 1f;
            if (ExoMechManagement.ShouldHaveSecondComboPhaseResistance(NPC))
                NPC.takenDamageMultiplier *= 0.5f;

            // Hover near Ares.
            float _ = 0f;
            float horizontalOffset = 490f;
            float verticalOffset = 100f;
            Vector2 hoverDestination = aresBody.Center + new Vector2(horizontalOffsetDirection * horizontalOffset, verticalOffset);
            if (!willCharge)
            {
                Vector2 aimDirection = NPC.SafeDirectionTo(target.Center + target.velocity * 10f);
                ExoMechAIUtilities.PerformAresArmDirectioning(NPC, aresBody, target, aimDirection, currentlyDisabled, false, ref _);
                ExoMechAIUtilities.DoSnapHoverMovement(NPC, hoverDestination, 65f, 115f);
            }

            // Check to see if Ares is in the middle of a death animation. If it is, participate in the death animation.
            if (performingDeathAnimation)
            {
                AresBodyBehaviorOverride.HaveArmPerformDeathAnimation(NPC, new Vector2(horizontalOffset, verticalOffset));
                return;
            }

            // Check to see if this arm should be used for special things in a combo attack.
            if (ExoMechComboAttackContent.ArmCurrentlyBeingUsed(NPC))
            {
                ExoMechComboAttackContent.UseThanatosAresComboAttack(NPC, ref aresBody.ai[1], ref _);
                ExoMechComboAttackContent.UseTwinsAresComboAttack(NPC, 1f, ref aresBody.ai[1], ref _);
                return;
            }

            // Handle charge behaviors.
            NPC.damage = 0;
            if (willCharge)
            {
                if (wrappedTimer > chargeCycleTime - chargeDelay - 5f)
                {
                    if (wrappedTimer % 5f == 4f)
                        SoundEngine.PlaySound(SoundID.Item22, NPC.Center);

                    // Charge at the target and release prism crystals at them.
                    if (wrappedTimer == chargeCycleTime - chargeDelay)
                    {
                        SoundEngine.PlaySound(CommonCalamitySounds.LightningSound, NPC.Center);
                        SoundEngine.PlaySound(ScorchedEarth.ShootSound, NPC.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int i = 0; i < crystalsPerBurst; i++)
                            {
                                float offsetAngle = MathHelper.Lerp(-crystalSpread, crystalSpread, i / (float)(crystalsPerBurst - 1f));
                                Vector2 crystalShootVelocity = NPC.SafeDirectionTo(target.Center).RotatedBy(offsetAngle) * crystalShootSpeed;
                                Utilities.NewProjectileBetter(NPC.Center + crystalShootVelocity * 4f, crystalShootVelocity, ModContent.ProjectileType<PhotonRipperCrystal>(), DraedonBehaviorOverride.StrongerNormalShotDamage, 0f);
                            }
                        }

                        NPC.velocity = NPC.SafeDirectionTo(target.Center) * chargeSpeed;
                        NPC.netUpdate = true;
                    }

                    if (wrappedTimer > chargeCycleTime - chargeSlowdownTime)
                        NPC.velocity *= 0.9f;

                    // Attempt to weakly redirect towards the target after charging.
                    else if (wrappedTimer > chargeCycleTime - chargeDelay)
                    {
                        NPC.damage = DraedonBehaviorOverride.AresPhotonRipperContactDamage;
                        NPC.velocity = NPC.velocity.RotateTowards(NPC.AngleTo(target.Center), 0.026f);
                        NPC.rotation = NPC.velocity.ToRotation() + (NPC.spriteDirection == 1f).ToInt() * MathHelper.Pi;
                    }
                }
                else
                {
                    NPC.spriteDirection = (target.Center.X < NPC.Center.X).ToDirectionInt();
                    NPC.rotation = NPC.rotation.AngleLerp(NPC.AngleTo(target.Center) + (NPC.spriteDirection == 1f).ToInt() * MathHelper.Pi, 0.2f);
                }
            }

            // Decide the state of the particle drawer.
            EnergyDrawer.ParticleSpawnRate = 99999999;
            if (!willCharge && !currentlyDisabled)
            {
                float chargeCompletion = MathHelper.Clamp(wrappedTimer / chargeTelegraphTime, 0f, 1f);
                if (AttackTimer <= initialChargeupTime)
                    chargeCompletion = AttackTimer / initialChargeupTime;

                EnergyDrawer.ParticleSpawnRate = 3;
                EnergyDrawer.SpawnAreaCompactness = 100f;
                EnergyDrawer.chargeProgress = chargeCompletion;

                if (AttackTimer % 15f == 14f && chargeCompletion < 1f)
                    EnergyDrawer.AddPulse(chargeCompletion * 6f);
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
            if (CalamityGlobalNPC.draedonExoMechPrime == -1)
                return false;

            if (NPC.Infernum().OptionalPrimitiveDrawer is null)
            {
                NPC.Infernum().OptionalPrimitiveDrawer = new PrimitiveTrailCopy(completionRatio => AresBodyBehaviorOverride.FlameTrailWidthFunctionBig(NPC, completionRatio),
                    completionRatio => AresBodyBehaviorOverride.FlameTrailColorFunctionBig(NPC, completionRatio),
                    null, true, InfernumEffectsRegistry.TwinsFlameTrailVertexShader);
            }

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
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D glowmaskTexture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Melee/PhotonRipperGlowmask").Value;
            if (NPC.Infernum().ExtraAI[1] == 0f)
                glowmaskTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Draedon/Ares/PhotonRipperGlowmask").Value;

            Rectangle glowmaskFrame = glowmaskTexture.Frame(1, 6, 0, (int)(Main.GlobalTimeWrappedHourly * 13f) % 6);
            Vector2 origin = glowmaskFrame.Size() * 0.5f;
            Rectangle frame = NPC.frame;
            Vector2 center = NPC.Center - Main.screenPosition;
            bool enraged = aresBody.Infernum().ExtraAI[13] == 1f || ExoMechComboAttackContent.EnrageTimer > 0f;
            Color afterimageBaseColor = enraged ? Color.Red : Color.White;
            int numAfterimages = 5;

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < numAfterimages; i += 2)
                {
                    float afterimageFade = (numAfterimages - i) / 15f;
                    Color afterimageColor = Color.Lerp(drawColor, afterimageBaseColor, 0.5f) * afterimageFade;
                    Vector2 afterimageCenter = NPC.oldPos[i] + NPC.Size * 0.5f - Main.screenPosition;
                    Main.spriteBatch.Draw(texture, afterimageCenter, null, NPC.GetAlpha(Color.White) * afterimageFade, NPC.rotation, origin, NPC.scale, spriteEffects, 0f);
                    Main.spriteBatch.Draw(glowmaskTexture, afterimageCenter, glowmaskFrame, afterimageColor, NPC.rotation, origin, NPC.scale, spriteEffects, 0f);
                }
            }

            ExoMechAIUtilities.DrawFinalPhaseGlow(Main.spriteBatch, NPC, texture, center, frame, origin);
            ExoMechAIUtilities.DrawAresArmTelegraphEffect(Main.spriteBatch, NPC, Color.Red, texture, center, frame, origin);
            Main.spriteBatch.Draw(texture, center, null, NPC.GetAlpha(Color.White), NPC.rotation, origin, NPC.scale, spriteEffects, 0f);
            Main.spriteBatch.Draw(glowmaskTexture, center, glowmaskFrame, afterimageBaseColor, NPC.rotation, origin, NPC.scale, spriteEffects, 0f);

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
