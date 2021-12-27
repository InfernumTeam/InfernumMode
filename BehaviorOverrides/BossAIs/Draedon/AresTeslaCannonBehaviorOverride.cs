using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Ares;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
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
                npc.active = false;
                return false;
            }

            // Locate Ares' body as an NPC.
            NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];

            // Define the life ratio.
            npc.life = aresBody.life;
            npc.lifeMax = aresBody.lifeMax;

            // Shamelessly steal variables from Ares.
            npc.target = aresBody.target;
            npc.Opacity = aresBody.Opacity;
            npc.dontTakeDamage = aresBody.dontTakeDamage;
            int projectileDamageBoost = (int)aresBody.Infernum().ExtraAI[8];
            Player target = Main.player[npc.target];

            // Define attack variables.
            bool currentlyDisabled = AresBodyBehaviorOverride.ArmIsDisabled(npc);
            int shootTime = 135;
            int totalOrbsPerBurst = 3;
            float aimPredictiveness = 25f;
            float orbShootSpeed = 10f;
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
                totalOrbsPerBurst = 5;

            if (ExoMechManagement.CurrentAresPhase >= 5)
            {
                shootTime += 40;
                totalOrbsPerBurst = 6;
                orbShootSpeed *= 1.1f;
            }
            if (ExoMechManagement.CurrentAresPhase >= 6)
            {
                shootTime += 40;
                totalOrbsPerBurst = 8;
            }

            // Get very pissed off if Ares is enraged.
            if (aresBody.Infernum().ExtraAI[13] == 1f)
                totalOrbsPerBurst += 6;

            int shootRate = shootTime / totalOrbsPerBurst;
            ref float attackTimer = ref npc.ai[0];
            ref float chargeDelay = ref npc.ai[1];
            ref float orbCounter = ref npc.ai[2];

            // Initialize delays and other timers.
            if (chargeDelay == 0f)
                chargeDelay = AresBodyBehaviorOverride.Phase1ArmChargeupTime;

            // Don't do anything if this arm should be disabled.
            if (currentlyDisabled && attackTimer >= chargeDelay)
                attackTimer = chargeDelay;

            // Hover near Ares.
            AresBodyBehaviorOverride.DoHoverMovement(npc, aresBody.Center + new Vector2(-375f, 100f), 45f, 90f);

            // Check to see if this arm should be used for special things in a combo attack.
            if (ExoMechComboAttackContent.ArmCurrentlyBeingUsed(npc))
            {
                float _ = 0f;
                ExoMechComboAttackContent.UseThanatosAresComboAttack(npc, ref aresBody.ai[1], ref _);
                return false;
            }

            // Choose a direction and rotation.
            // Rotation is relative to predictiveness.
            Vector2 endOfCannon = npc.Center + aimDirection.SafeNormalize(Vector2.Zero) * 84f + Vector2.UnitY * 8f;
            float idealRotation = aimDirection.ToRotation();
            if (currentlyDisabled)
                idealRotation = MathHelper.Clamp(npc.velocity.X * -0.016f, -0.81f, 0.81f) + MathHelper.PiOver2;

            if (npc.spriteDirection == 1)
                idealRotation += MathHelper.Pi;
            if (idealRotation < 0f)
                idealRotation += MathHelper.TwoPi;
            if (idealRotation > MathHelper.TwoPi)
                idealRotation -= MathHelper.TwoPi;
            npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.065f);

            int direction = Math.Sign(target.Center.X - npc.Center.X);
            if (direction != 0)
            {
                npc.direction = direction;

                if (npc.spriteDirection != -npc.direction)
                    npc.rotation += MathHelper.Pi;

                npc.spriteDirection = -npc.direction;
            }

            // Create a dust telegraph before firing.
            if (attackTimer > chargeDelay * 0.7f && attackTimer < chargeDelay)
            {
                Vector2 dustSpawnPosition = endOfCannon + Main.rand.NextVector2Circular(45f, 45f);
                Dust electricity = Dust.NewDustPerfect(dustSpawnPosition, 229);
                electricity.velocity = (endOfCannon - electricity.position) * 0.04f;
                electricity.scale = 1.25f;
                electricity.noGravity = true;
            }

            // Fire orbs.
            if (attackTimer >= chargeDelay && attackTimer % shootRate == shootRate - 1f)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PlasmaBolt"), npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int electricOrb = Utilities.NewProjectileBetter(endOfCannon, aimDirection * orbShootSpeed, ModContent.ProjectileType<AresTeslaOrb>(), projectileDamageBoost + 500, 0f);
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
                        float offsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                        for (int i = 0; i < 7; i++)
                        {
                            Vector2 sparkVelocity = (MathHelper.TwoPi * i / 7f + offsetAngle).ToRotationVector2() * 6.5f;
                            Utilities.NewProjectileBetter(npc.Center + sparkVelocity * 6f, sparkVelocity, ModContent.ProjectileType<TeslaSpark>(), projectileDamageBoost + 500, 0f);
                        }
                    }

                    // As well as a of electric clouds in the third phase.
                    if (ExoMechManagement.CurrentAresPhase >= 3)
                    {
                        for (int i = 0; i < 85; i++)
                        {
                            Vector2 cloudShootVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(4f, 23f) - npc.velocity.SafeNormalize(-Vector2.UnitY) * 10f;
                            Utilities.NewProjectileBetter(npc.Center + cloudShootVelocity * 3f, cloudShootVelocity, ModContent.ProjectileType<ElectricGas>(), projectileDamageBoost + 530, 0f);
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
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            // Locate Ares' body as an NPC.
            NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];
            Texture2D texture = Main.npcTexture[npc.type];
            Rectangle frame = npc.frame;
            Vector2 origin = frame.Size() * 0.5f;
            Color afterimageBaseColor = aresBody.Infernum().ExtraAI[13] == 1f ? Color.Red : Color.White;
            int numAfterimages = 5;

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < numAfterimages; i += 2)
                {
                    Color afterimageColor = npc.GetAlpha(Color.Lerp(lightColor, afterimageBaseColor, 0.5f)) * ((numAfterimages - i) / 15f);
                    Vector2 afterimageCenter = npc.oldPos[i] + npc.frame.Size() * 0.5f - Main.screenPosition;
                    spriteBatch.Draw(texture, afterimageCenter, npc.frame, afterimageColor, npc.oldRot[i], origin, npc.scale, spriteEffects, 0f);
                }
            }

            Vector2 center = npc.Center - Main.screenPosition;
            spriteBatch.Draw(texture, center, frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, spriteEffects, 0f);

            texture = ModContent.GetTexture("CalamityMod/NPCs/ExoMechs/Ares/AresTeslaCannonGlow");

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < numAfterimages; i += 2)
                {
                    Color afterimageColor = npc.GetAlpha(Color.Lerp(lightColor, afterimageBaseColor, 0.5f)) * ((numAfterimages - i) / 15f);
                    Vector2 afterimageCenter = npc.oldPos[i] + npc.frame.Size() * 0.5f - Main.screenPosition;
                    spriteBatch.Draw(texture, afterimageCenter, npc.frame, afterimageColor, npc.oldRot[i], origin, npc.scale, spriteEffects, 0f);
                }
            }

            spriteBatch.Draw(texture, center, frame, afterimageBaseColor * npc.Opacity, npc.rotation, origin, npc.scale, spriteEffects, 0f);
            return false;
        }
        #endregion Frames and Drawcode
    }
}
