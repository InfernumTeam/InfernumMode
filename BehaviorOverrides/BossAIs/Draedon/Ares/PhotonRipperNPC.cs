using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

using CalamityModClass = CalamityMod.CalamityMod;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class PhotonRipperNPC : ModNPC
    {
        public AresCannonChargeParticleSet EnergyDrawer = new AresCannonChargeParticleSet(-1, 15, 40f, Color.Red);

        public ref float AttackTimer => ref npc.ai[0];
        public ref float ChargeDelay => ref npc.ai[1];
        public Vector2 CoreSpritePosition => npc.Center + npc.spriteDirection * npc.rotation.ToRotationVector2() * 35f + (npc.rotation + MathHelper.PiOver2).ToRotationVector2() * 5f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("XF-09 Ares Photon Ripper");
            NPCID.Sets.TrailingMode[npc.type] = 3;
            NPCID.Sets.TrailCacheLength[npc.type] = npc.oldPos.Length;
        }

        public override void SetDefaults()
        {
            npc.npcSlots = 5f;
            npc.damage = 0;
            npc.width = 170;
            npc.height = 120;
            npc.defense = 80;
            npc.DR_NERD(0.35f);
            npc.LifeMaxNERB(1250000, 1495000, 500000);
            double HPBoost = CalamityConfig.Instance.BossHealthBoost * 0.01;
            npc.lifeMax += (int)(npc.lifeMax * HPBoost);
            npc.aiStyle = -1;
            aiType = -1;
            npc.scale = 1.25f;
            npc.Opacity = 0f;
            npc.knockBackResist = 0f;
            npc.canGhostHeal = false;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.HitSound = SoundID.NPCHit4;
            npc.DeathSound = SoundID.NPCDeath14;
            npc.netAlways = true;
            npc.boss = true;
            npc.hide = true;
            npc.Calamity().canBreakPlayerDefense = true;
            music = (InfernumMode.CalamityMod as CalamityModClass).GetMusicFromMusicMod("ExoMechs") ?? MusicID.Boss3;
        }

        public override void AI()
        {
            if (CalamityGlobalNPC.draedonExoMechPrime < 0)
            {
                npc.life = 0;
                npc.HitEffect();
                npc.active = false;
                return;
            }

            // Update the energy drawer.
            EnergyDrawer.Update();

            // Locate Ares' body as an npc.
            NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];
            ExoMechAIUtilities.HaveArmsInheritAresBodyAttributes(npc);

            bool performingDeathAnimation = ExoMechAIUtilities.PerformingDeathAnimation(npc);
            Player target = Main.player[npc.target];

            // Disable HP bars.
            npc.Calamity().ShouldCloseHPBar = true;

            // Define attack variables.
            float horizontalOffsetDirection = npc.Infernum().ExtraAI[0];
            bool currentlyDisabled = AresBodyBehaviorOverride.ArmIsDisabled(npc);
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
                npc.Center += Main.rand.NextVector2Circular(6f, 6f);
                if (AttackTimer % 5f == 4f)
                    Main.PlaySound(SoundID.Item22, npc.Center);

                bool useElectricity = Main.rand.NextBool(3);
                Dust smoke = Dust.NewDustPerfect(npc.Center, useElectricity ? 182 : 31);
                smoke.velocity = npc.rotation.ToRotationVector2() * npc.spriteDirection * Main.rand.NextFloat(3f, 7f) + Main.rand.NextVector2Circular(2f, 2f);
                smoke.position += smoke.velocity * 3f;
                smoke.scale = 1.7f;
                smoke.noGravity = true;
            }

            bool willCharge = wrappedTimer > chargeCycleTime - chargeTelegraphTime;

            // Determine whether the active chainsaw frames should be used or not.
            npc.Infernum().ExtraAI[1] = willCharge.ToInt();

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
            npc.takenDamageMultiplier = 1f;
            if (ExoMechManagement.ShouldHaveSecondComboPhaseResistance(npc))
                npc.takenDamageMultiplier *= 0.5f;

            // Hover near Ares.
            float _ = 0f;
            float horizontalOffset = 490f;
            float verticalOffset = 100f;
            Vector2 hoverDestination = aresBody.Center + new Vector2(horizontalOffsetDirection * horizontalOffset, verticalOffset);
            if (!willCharge)
            {
                Vector2 aimDirection = npc.SafeDirectionTo(target.Center + target.velocity * 10f);
                ExoMechAIUtilities.PerformAresArmDirectioning(npc, aresBody, target, aimDirection, currentlyDisabled, false, ref _);
                ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 65f, 115f);
            }

            // Check to see if Ares is in the middle of a death animation. If it is, participate in the death animation.
            if (performingDeathAnimation)
            {
                AresBodyBehaviorOverride.HaveArmPerformDeathAnimation(npc, new Vector2(horizontalOffset, verticalOffset));
                return;
            }

            // Check to see if this arm should be used for special things in a combo attack.
            if (ExoMechComboAttackContent.ArmCurrentlyBeingUsed(npc))
            {
                ExoMechComboAttackContent.UseThanatosAresComboAttack(npc, ref aresBody.ai[1], ref _);
                ExoMechComboAttackContent.UseTwinsAresComboAttack(npc, 1f, ref aresBody.ai[1], ref _);
                return;
            }

            // Handle charge behaviors.
            npc.damage = 0;
            if (willCharge)
            {
                if (wrappedTimer > chargeCycleTime - chargeDelay - 5f)
                {
                    if (wrappedTimer % 5f == 4f)
                        Main.PlaySound(SoundID.Item22, npc.Center);

                    // Charge at the target and release prism crystals at them.
                    if (wrappedTimer == chargeCycleTime - chargeDelay)
                    {
                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ThunderStrike"), npc.Center);
                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/ScorchedEarthShot3"), npc.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int i = 0; i < crystalsPerBurst; i++)
                            {
                                float offsetAngle = MathHelper.Lerp(-crystalSpread, crystalSpread, i / (float)(crystalsPerBurst - 1f));
                                Vector2 crystalShootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(offsetAngle) * crystalShootSpeed;
                                Utilities.NewProjectileBetter(npc.Center + crystalShootVelocity * 4f, crystalShootVelocity, ModContent.ProjectileType<PhotonRipperCrystal>(), 550, 0f);
                            }
                        }

                        npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                        npc.netUpdate = true;
                    }

                    if (wrappedTimer > chargeCycleTime - chargeSlowdownTime)
                        npc.velocity *= 0.9f;

                    // Attempt to weakly redirect towards the target after charging.
                    else if (wrappedTimer > chargeCycleTime - chargeDelay)
                    {
                        npc.damage = 600;
                        npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.026f);
                        npc.rotation = npc.velocity.ToRotation() + (npc.spriteDirection == 1f).ToInt() * MathHelper.Pi;
                    }
                }
                else
                {
                    npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                    npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(target.Center) + (npc.spriteDirection == 1f).ToInt() * MathHelper.Pi, 0.2f);
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

            npc.frame = new Rectangle(currentFrame / 12 * 150, currentFrame % 12 * 148, 150, 148);
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            for (int k = 0; k < 3; k++)
                Dust.NewDust(npc.position, npc.width, npc.height, 107, 0f, 0f, 100, new Color(0, 255, 255), 1f);

            if (npc.life <= 0)
            {
                for (int i = 0; i < 2; i++)
                    Dust.NewDust(npc.position, npc.width, npc.height, 107, 0f, 0f, 100, new Color(0, 255, 255), 1.5f);

                for (int i = 0; i < 20; i++)
                {
                    Dust exoEnergy = Dust.NewDustDirect(npc.position, npc.width, npc.height, 107, 0f, 0f, 0, new Color(0, 255, 255), 2.5f);
                    exoEnergy.noGravity = true;
                    exoEnergy.velocity *= 3f;

                    exoEnergy = Dust.NewDustDirect(npc.position, npc.width, npc.height, 107, 0f, 0f, 100, new Color(0, 255, 255), 1.5f);
                    exoEnergy.velocity *= 2f;
                    exoEnergy.noGravity = true;
                }

                Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("AresPulseCannon1"), npc.scale);
                Gore.NewGore(npc.position, npc.velocity, InfernumMode.CalamityMod.GetGoreSlot("AresHandBase1"), npc.scale);
                Gore.NewGore(npc.position, npc.velocity, InfernumMode.CalamityMod.GetGoreSlot("AresHandBase2"), npc.scale);
                Gore.NewGore(npc.position, npc.velocity, InfernumMode.CalamityMod.GetGoreSlot("AresHandBase3"), npc.scale);
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            if (CalamityGlobalNPC.draedonExoMechPrime == -1)
                return false;

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

            // Locate Ares' body as an npc.
            NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];
            Texture2D texture = ModContent.GetTexture(Texture);
            Texture2D glowmaskTexture = ModContent.GetTexture("CalamityMod/Projectiles/Melee/PhotonRipperGlowmask");
            if (npc.Infernum().ExtraAI[1] == 0f)
                glowmaskTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Draedon/Ares/PhotonRipperGlowmask");

            Rectangle glowmaskFrame = glowmaskTexture.Frame(1, 6, 0, (int)(Main.GlobalTime * 13f) % 6);
            Vector2 origin = glowmaskFrame.Size() * 0.5f;
            Rectangle frame = npc.frame;
            Vector2 center = npc.Center - Main.screenPosition;
            Color afterimageBaseColor = aresBody.Infernum().ExtraAI[13] == 1f ? Color.Red : Color.White;
            int numAfterimages = 5;

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < numAfterimages; i += 2)
                {
                    float afterimageFade = (numAfterimages - i) / 15f;
                    Color afterimageColor = Color.Lerp(drawColor, afterimageBaseColor, 0.5f) * afterimageFade;
                    Vector2 afterimageCenter = npc.oldPos[i] + npc.Size * 0.5f - Main.screenPosition;
                    Main.spriteBatch.Draw(texture, afterimageCenter, null, npc.GetAlpha(Color.White) * afterimageFade, npc.rotation, origin, npc.scale, spriteEffects, 0f);
                    Main.spriteBatch.Draw(glowmaskTexture, afterimageCenter, glowmaskFrame, afterimageColor, npc.rotation, origin, npc.scale, spriteEffects, 0f);
                }
            }

            ExoMechAIUtilities.DrawFinalPhaseGlow(Main.spriteBatch, npc, texture, center, frame, origin);
            ExoMechAIUtilities.DrawAresArmTelegraphEffect(Main.spriteBatch, npc, Color.Red, texture, center, frame, origin);
            Main.spriteBatch.Draw(texture, center, null, npc.GetAlpha(Color.White), npc.rotation, origin, npc.scale, spriteEffects, 0f);
            Main.spriteBatch.Draw(glowmaskTexture, center, glowmaskFrame, afterimageBaseColor, npc.rotation, origin, npc.scale, spriteEffects, 0f);

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            if (npc.Infernum().ExtraAI[1] == 1f)
                EnergyDrawer.DrawBloom(CoreSpritePosition);
            EnergyDrawer.DrawPulses(CoreSpritePosition);
            EnergyDrawer.DrawSet(CoreSpritePosition);

            Main.spriteBatch.ResetBlendState();
            return false;
        }

        public override bool CheckActive() => false;
    }
}
