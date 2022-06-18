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
    public class AresPulseCannon : ModNPC
    {
        public AresCannonChargeParticleSet EnergyDrawer = new AresCannonChargeParticleSet(-1, 15, 40f, Color.Fuchsia);

        public ref float AttackTimer => ref npc.ai[0];
        public ref float ChargeDelay => ref npc.ai[1];
        public Vector2 CoreSpritePosition => npc.Center + npc.spriteDirection * npc.rotation.ToRotationVector2() * 35f + (npc.rotation + MathHelper.PiOver2).ToRotationVector2() * 5f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("XF-09 Ares Pulse Cannon");
            Main.npcFrameCount[npc.type] = 12;
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
            bool currentlyDisabled = AresBodyBehaviorOverride.ArmIsDisabled(npc);
            int shootTime = 180;
            int totalPulseBlastsPerBurst = 3;
            float blastShootSpeed = 7.5f;
            float aimPredictiveness = 27f;
            ref float shouldPrepareToFire = ref npc.Infernum().ExtraAI[1];

            // Nerf things while Ares' complement mech is present.
            if (ExoMechManagement.CurrentAresPhase == 4)
                blastShootSpeed *= 0.85f;

            if (ExoMechManagement.CurrentAresPhase >= 5)
            {
                shootTime += 60;
                totalPulseBlastsPerBurst += 2;
                blastShootSpeed *= 1.25f;
            }
            if (ExoMechManagement.CurrentAresPhase >= 6)
            {
                shootTime -= 30;
                totalPulseBlastsPerBurst++;
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
                return;
            }

            // Check to see if this arm should be used for special things in a combo attack.
            float _ = 0f;
            if (ExoMechComboAttackContent.ArmCurrentlyBeingUsed(npc))
            {
                ExoMechComboAttackContent.UseThanatosAresComboAttack(npc, ref aresBody.ai[1], ref _);
                ExoMechComboAttackContent.UseTwinsAresComboAttack(npc, 1f, ref aresBody.ai[1], ref _);
                return;
            }

            // Calculate the direction and rotation this arm should use.
            Vector2 aimDirection = npc.SafeDirectionTo(target.Center + target.velocity * aimPredictiveness);
            ExoMechAIUtilities.PerformAresArmDirectioning(npc, aresBody, target, aimDirection, currentlyDisabled, doingHoverCharge, ref _);
            float rotationToEndOfCannon = npc.rotation;
            if (rotationToEndOfCannon < 0f)
                rotationToEndOfCannon += MathHelper.Pi;
            Vector2 endOfCannon = npc.Center + rotationToEndOfCannon.ToRotationVector2() * 66f + Vector2.UnitY * 16f;

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
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PulseRifleFire"), npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int blastDamage = AresBodyBehaviorOverride.ProjectileDamageBoost + 500;
                    Vector2 blastShootVelocity = aimDirection * blastShootSpeed;
                    Vector2 blastSpawnPosition = endOfCannon + blastShootVelocity * 8.4f;
                    Utilities.NewProjectileBetter(blastSpawnPosition, blastShootVelocity, ModContent.ProjectileType<AresPulseBlast>(), blastDamage, 0f);

                    npc.netUpdate = true;
                }
            }

            // Reset the attack timer after an attack cycle ends.
            if (AttackTimer >= ChargeDelay + shootTime)
            {
                AttackTimer = 0f;
                npc.netUpdate = true;
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
            Texture2D texture = Main.npcTexture[npc.type];
            Rectangle frame = npc.frame;
            Vector2 origin = npc.Center - npc.position;
            Vector2 center = npc.Center - Main.screenPosition;
            Color afterimageBaseColor = aresBody.Infernum().ExtraAI[13] == 1f ? Color.Red : Color.White;
            int numAfterimages = 5;

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < numAfterimages; i += 2)
                {
                    Color afterimageColor = npc.GetAlpha(Color.Lerp(drawColor, afterimageBaseColor, 0.5f)) * ((numAfterimages - i) / 15f);
                    Vector2 afterimageCenter = npc.oldPos[i] + origin - Main.screenPosition;
                    Main.spriteBatch.Draw(texture, afterimageCenter, npc.frame, afterimageColor, npc.oldRot[i], origin, npc.scale, spriteEffects, 0f);
                }
            }

            ExoMechAIUtilities.DrawFinalPhaseGlow(Main.spriteBatch, npc, texture, center, frame, origin);
            ExoMechAIUtilities.DrawAresArmTelegraphEffect(Main.spriteBatch, npc, Color.Violet, texture, center, frame, origin);
            Main.spriteBatch.Draw(texture, center, frame, npc.GetAlpha(drawColor), npc.rotation, origin, npc.scale, spriteEffects, 0f);

            texture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Draedon/Ares/AresPulseCannonGlow");

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < numAfterimages; i += 2)
                {
                    Color afterimageColor = npc.GetAlpha(Color.Lerp(drawColor, afterimageBaseColor, 0.5f)) * ((numAfterimages - i) / 15f);
                    Vector2 afterimageCenter = npc.oldPos[i] + origin - Main.screenPosition;
                    Main.spriteBatch.Draw(texture, afterimageCenter, npc.frame, afterimageColor, npc.oldRot[i], origin, npc.scale, spriteEffects, 0f);
                }
            }

            Main.spriteBatch.Draw(texture, center, frame, afterimageBaseColor * npc.Opacity, npc.rotation, origin, npc.scale, spriteEffects, 0f);

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
