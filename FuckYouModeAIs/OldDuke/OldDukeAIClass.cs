using CalamityMod.Dusts;
using CalamityMod.NPCs.OldDuke;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.OldDuke
{
	public class OldDukeAIClass
    {
        #region Enumerations
        public enum OldDukeAttackType
        {
            SpawnDive,
            Charge,
            SharkSummon,
            UpwardSpin,
            AcidVortexSpin,
            SideSharkSummon,
        }

        public enum OldDukeFrameDrawingType
        {
            FinFlapping,
            IdleFins,
            OpenMouth
        }
		#endregion

		#region Pattern Lists
		public static readonly OldDukeAttackType[] Subphase1Pattern = new OldDukeAttackType[]
        {
            OldDukeAttackType.AcidVortexSpin,
            OldDukeAttackType.Charge,
            OldDukeAttackType.Charge,
            OldDukeAttackType.SideSharkSummon,
            OldDukeAttackType.Charge,
            OldDukeAttackType.Charge,
            OldDukeAttackType.SharkSummon,
            OldDukeAttackType.UpwardSpin,
            OldDukeAttackType.SideSharkSummon,
            OldDukeAttackType.Charge,
            OldDukeAttackType.Charge,
            OldDukeAttackType.UpwardSpin,
            OldDukeAttackType.AcidVortexSpin,
            OldDukeAttackType.SideSharkSummon,
            OldDukeAttackType.Charge,
            OldDukeAttackType.UpwardSpin,
            OldDukeAttackType.Charge,
            OldDukeAttackType.SharkSummon,
            OldDukeAttackType.Charge,
            OldDukeAttackType.SideSharkSummon,
            OldDukeAttackType.Charge,
        };

        public static readonly Dictionary<OldDukeAttackType[], Func<NPC, bool>> SubphaseTable = new Dictionary<OldDukeAttackType[], Func<NPC, bool>>()
        {
            [Subphase1Pattern] = (npc) => npc.life / (float)npc.lifeMax > 0f,
        };
		#endregion

		#region AI
		[OverrideAppliesTo("OldDuke", typeof(OldDukeAIClass), "OldDukeAI", EntityOverrideContext.NPCAI, true)]
        public static bool OldDukeAI(NPC npc)
        {
            Player target = Main.player[npc.target];

            ref float aiState = ref npc.ai[0];
            ref float aiStateIndex = ref npc.ai[1];
            ref float aiTimer = ref npc.ai[2];
            ref float frameDrawType = ref npc.ai[3];

            bool enraged = target.position.Y < 300f || target.position.Y > Main.worldSurface * 16.0 ||
                           target.position.X > 8000f && target.position.X < (Main.maxTilesX * 16 - 8000);

            Vector2 mouthPosition = npc.velocity.SafeNormalize(Vector2.UnitX * npc.spriteDirection) * (npc.width + 20) / 2f + npc.Center;

            void spawnSideSharks()
            {
                Vector2 directionToTarget = npc.DirectionTo(target.Center);

                int shark = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<BigOldDukeSharkron>());
                Main.npc[shark].velocity = directionToTarget.RotatedBy(MathHelper.PiOver2) * 10f;
                Main.npc[shark].rotation = Main.npc[shark].velocity.ToRotation();
                Main.npc[shark].spriteDirection = (Math.Cos(Main.npc[shark].rotation) > 0).ToDirectionInt();
                if (Main.npc[shark].spriteDirection == -1)
                    Main.npc[shark].rotation += MathHelper.Pi;
                Main.npc[shark].netUpdate = true;

                shark = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<BigOldDukeSharkron>());
                Main.npc[shark].velocity = directionToTarget.RotatedBy(-MathHelper.PiOver2) * 10f;
                Main.npc[shark].rotation = Main.npc[shark].velocity.ToRotation();
                Main.npc[shark].spriteDirection = (Math.Cos(Main.npc[shark].rotation) > 0).ToDirectionInt();
                if (Main.npc[shark].spriteDirection == -1)
                    Main.npc[shark].rotation += MathHelper.Pi;

                Main.npc[shark].netUpdate = true;
            }

            void goToNextAIState()
            {
                // You cannot use ref locals inside of a delegate context.
                // You should be able to find most important, universal locals above, anyway.
                // Any others that don't have an explicit reference above are exclusively for
                // AI state manipulation.

                npc.ai[1]++;

                OldDukeAttackType[] patternToUse = SubphaseTable.First(table => table.Value(npc)).Key;
                OldDukeAttackType nextAttackType = patternToUse[(int)(npc.ai[1] % patternToUse.Length)];

                // Going to the next AI state.
                npc.ai[0] = (int)nextAttackType;

                // Resetting the attack timer.
                npc.ai[2] = 0f;

                // And the misc ai slots.
                for (int i = 0; i < 5; i++)
                {
                    npc.Infernum().ExtraAI[i] = 0f;
                }
            }

            switch ((OldDukeAttackType)(int)aiState)
            {
                case OldDukeAttackType.SpawnDive:

                    int diveTime = 110;
                    int slowdownTime = 225;
                    ref float diveDirection = ref npc.Infernum().ExtraAI[0];
                    ref float diveTimer = ref npc.Infernum().ExtraAI[1];

                    frameDrawType = (int)OldDukeFrameDrawingType.FinFlapping;

                    // Get a target, direction, and roar.
                    if (diveDirection == 0f)
                    {
                        npc.TargetClosest();
                        target = Main.player[npc.target];

                        diveDirection = Main.rand.NextBool(2).ToDirectionInt();
                        npc.spriteDirection = (int)diveDirection;
                        npc.velocity = new Vector2(diveDirection * -16f, -16);

                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/OldDukeRoar"), npc.Center);
                    }

                    // Fly upward and dive until we're nearly only falling downward.
                    if (Vector2.Dot(npc.velocity.SafeNormalize(Vector2.Zero), Vector2.UnitY) < 0.95f || diveTimer <= diveTime)
                    {
                        aiTimer = 0f;

                        npc.rotation = npc.velocity.ToRotation();
                        if (npc.spriteDirection == 1)
                            npc.rotation += MathHelper.Pi;

                        npc.velocity.X *= 0.985f;
                        if (npc.velocity.Y < 12f)
                            npc.velocity.Y += 0.4f;

                        diveTimer++;
                    }
                    else
                    {
                        // Spawn sharks that attempt to hit the player at the start.
                        if (aiTimer == 2f && Main.netMode != NetmodeID.MultiplayerClient)
                            spawnSideSharks();

                        npc.velocity *= 0.96f;

                        npc.rotation = npc.AngleTo(target.Center);
                        npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                        if (npc.spriteDirection == 1)
                            npc.rotation += MathHelper.Pi;

                        if (aiTimer % 45 == 44)
                            Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/OldDukeHuff"), npc.Center);

                        if (aiTimer >= slowdownTime)
                        {
                            // This is done to ensure that the attack starts at the 0 index instead of 1.
                            aiStateIndex = -1f;
                            goToNextAIState();
                        }
                    }

                    break;

                case OldDukeAttackType.Charge:
                    int reelbackDelay = 36;
                    int chargeTime = enraged ? 30 : 40;
                    float reelbackSpeed = enraged ? 12f : 7f;
                    float chargeSpeed = enraged ? 34f : 26f;
                    float chargeAcceleration = enraged ? 1.015f : 1.01f;

                    // Reel back for a bit.
                    if (aiTimer < reelbackDelay)
                    {
                        frameDrawType = (int)OldDukeFrameDrawingType.FinFlapping;

                        npc.velocity = Vector2.Lerp(npc.velocity, npc.DirectionTo(target.Center) * -reelbackSpeed, 0.35f);
                        npc.rotation = npc.AngleTo(target.Center);
                        npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                        if (npc.spriteDirection == 1)
                            npc.rotation += MathHelper.Pi;

                        if (npc.Distance(target.Center) > 1400f)
                            aiTimer = reelbackDelay;
                    }

                    // And then charge at the player.
                    if (aiTimer == reelbackDelay)
                    {
                        frameDrawType = (int)OldDukeFrameDrawingType.IdleFins;

                        // The typical value here is 20. This has been lowered a bit to make it more fair.
                        float chargeAheadFactor = 15f;
                        float distanceFromTarget = npc.Distance(target.Center);

                        if (distanceFromTarget < 320f)
                            chargeAheadFactor *= 0.3f;

                        if (distanceFromTarget < 160f)
                            chargeAheadFactor *= 0.3f;

                        if (distanceFromTarget < 85f)
                            chargeAheadFactor *= 0.3f;

                        npc.velocity = npc.DirectionTo(target.Center + target.velocity * chargeAheadFactor) * chargeSpeed;
                        npc.rotation = npc.velocity.ToRotation();
                        npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                        if (npc.spriteDirection == 1)
                            npc.rotation += MathHelper.Pi;
                    }

                    if (aiTimer > reelbackDelay)
                    {
                        npc.velocity *= chargeAcceleration;

                        // Release a bit of dust.
                        if (!Main.dedServ)
                        {
                            int dustCount = 7;
                            for (int i = 0; i < dustCount; i++)
                            {
                                Vector2 dustSpawnPosition = (Vector2.Normalize(npc.velocity) * new Vector2((npc.width + 50) / 2f, npc.height) * 0.75f).RotatedBy(MathHelper.TwoPi * i / dustCount) + npc.Center;
                                Vector2 dustVelocity = Main.rand.NextVector2Unit() * Main.rand.Next(3, 8);
                                Dust acid = Dust.NewDustDirect(dustSpawnPosition + dustVelocity, 0, 0, (int)CalamityDusts.SulfurousSeaAcid, dustVelocity.X * 2f, dustVelocity.Y * 2f, 100, default, 1.4f);
                                acid.noGravity = true;
                                acid.noLight = true;
                                acid.velocity /= 4f;
                                acid.velocity -= npc.velocity;
                            }
                        }
                    }

                    if (aiTimer >= reelbackDelay + chargeTime)
                        goToNextAIState();

                    break;
                case OldDukeAttackType.SharkSummon:
                    int rotationCorrectionTime = 60;
                    int totalSharksToSummon = enraged ? 6 : 4;
                    int sharkSummonRate = 15;
                    int roarTime = 20;

                    frameDrawType = (int)OldDukeFrameDrawingType.FinFlapping;

                    if (aiTimer <= rotationCorrectionTime)
                    {
                        npc.velocity *= 0.93f;
                        npc.rotation = npc.rotation.AngleTowards(0f, 0.2f);
                    }
                    if (aiTimer > rotationCorrectionTime)
                    {
                        if (aiTimer < rotationCorrectionTime + roarTime)
                        {
                            if (aiTimer == rotationCorrectionTime + 2f)
                                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/OldDukeRoar"), npc.Center);

                            frameDrawType = (int)OldDukeFrameDrawingType.OpenMouth;
                        }
                        if (aiTimer % sharkSummonRate == sharkSummonRate - 1)
                        {
                            int sharkCounter = (int)((aiTimer - rotationCorrectionTime) / sharkSummonRate);
                            int ySummonOffset = sharkCounter * 100;
                            NPC.NewNPC((int)npc.Center.X + 360 + sharkCounter * 60, (int)npc.Center.Y - ySummonOffset, ModContent.NPCType<OldDukeSharkron>(), 0, 0f, 0f, npc.whoAmI, 0f, 255);
                            NPC.NewNPC((int)npc.Center.X - 360 - sharkCounter * 60, (int)npc.Center.Y - ySummonOffset, ModContent.NPCType<OldDukeSharkron>(), 0, 0f, 0f, npc.whoAmI, 0f, 255);
                        }
                    }

                    if (aiTimer > rotationCorrectionTime + sharkSummonRate * totalSharksToSummon)
                        goToNextAIState();

                    break;
                case OldDukeAttackType.UpwardSpin:
                    int riseTime = 120;
                    int redirectTime = 15;
                    chargeSpeed = 20f;
                    int delayPerSpin = 30;
                    int spinTime = 45;
                    float spinSpeed = 24f;
                    float spinArcs = 1f;
                    int totalSpins = 2;
                    int acidShootRate = 2;

                    if (aiTimer < riseTime)
                    {
                        Vector2 destination = target.Center - new Vector2(1000f, 420f);
                        npc.velocity = npc.DirectionTo(destination) * 24f;
                        npc.rotation = npc.velocity.ToRotation();

                        npc.spriteDirection = (destination.X > npc.Center.X).ToDirectionInt();
                        if (npc.spriteDirection == 1)
                            npc.rotation += MathHelper.Pi;

                        if (npc.Distance(destination) < 50f)
                            aiTimer = riseTime;

                        frameDrawType = (int)OldDukeFrameDrawingType.FinFlapping;
                    }
                    
                    if (aiTimer >= riseTime && aiTimer <= riseTime + redirectTime)
                    {
                        npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitX * 23f, 0.19f);
                        npc.rotation = npc.velocity.ToRotation();
                    }

                    if (aiTimer >= riseTime + redirectTime)
                    {
                        float timeRelativeToSpin = (aiTimer - riseTime - redirectTime) % (delayPerSpin + spinTime);

                        // Spin and horizontal charge.
                        // Spins should have an open mouth with acid leaking.
                        if (timeRelativeToSpin >= delayPerSpin)
                        {
                            float spinArcPerFrame = spinArcs * MathHelper.TwoPi / spinTime;
                            npc.velocity = npc.velocity.RotatedBy(-spinArcPerFrame);
                            npc.rotation -= spinArcPerFrame;

                            frameDrawType = (int)OldDukeFrameDrawingType.OpenMouth;

                            if (timeRelativeToSpin % acidShootRate == acidShootRate - 1 && Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Vector2 acidVelocity = npc.velocity.SafeNormalize(Vector2.UnitX * npc.spriteDirection) * 14f + npc.velocity / 3f;
                                Utilities.NewProjectileBetter(mouthPosition, acidVelocity, ModContent.ProjectileType<HomingAcid>(), 300, 0f);
                            }
                        }
                        else
                        {
                            npc.rotation = npc.velocity.ToRotation();

                            frameDrawType = (int)OldDukeFrameDrawingType.FinFlapping;
                        }

                        npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitX) * (timeRelativeToSpin >= delayPerSpin ? spinSpeed : chargeSpeed);
                    }

                    npc.spriteDirection = -1;

                    if (aiTimer >= riseTime + redirectTime + (delayPerSpin + spinTime) * totalSpins)
                        goToNextAIState();
                    break;
                case OldDukeAttackType.AcidVortexSpin:
                    int teleportDelay = 30;
                    int chargeDelay = 20;
                    chargeSpeed = 27f;
                    chargeTime = 45;

                    totalSpins = 2;
                    spinTime = 120;
                    spinSpeed = 35f;

                    int secondaryChargeTime = 45;

                    frameDrawType = (int)OldDukeFrameDrawingType.FinFlapping;

                    // Slowdown before initial teleport.
                    if (aiTimer < teleportDelay)
                        npc.velocity *= 0.93f;

                    // Teleport,
                    if (aiTimer == teleportDelay)
                    {
                        npc.Center = target.Center + new Vector2(target.direction * 440f, 200f * Main.rand.NextBool(2).ToDirectionInt());
                        npc.rotation = npc.AngleTo(target.Center);
                        npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                        if (npc.spriteDirection == 1)
                            npc.rotation += MathHelper.Pi;

                        npc.netUpdate = true;
                    }

                    // Charge,
                    if (aiTimer == teleportDelay + chargeDelay)
                    {
                        npc.velocity = npc.DirectionTo(target.Center) * chargeSpeed;
                        npc.rotation = npc.velocity.ToRotation();
                        npc.direction = (target.Center.X > npc.Center.X).ToDirectionInt();
                        if (npc.spriteDirection == 1)
                            npc.rotation += MathHelper.Pi;
                    }

                    // And spin.
                    if (aiTimer >= teleportDelay + chargeDelay + chargeTime &&
                        aiTimer < teleportDelay + chargeDelay + chargeTime + spinTime)
                    {
                        npc.spriteDirection = -1;

                        if (aiTimer == teleportDelay + chargeDelay + chargeTime + 1 && Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            npc.rotation = npc.velocity.ToRotation();
                            Vector2 vortexSpawnPosition = npc.Center + npc.velocity.RotatedBy(MathHelper.PiOver2) * spinTime / MathHelper.TwoPi / totalSpins;

                            Utilities.NewProjectileBetter(vortexSpawnPosition, Vector2.Zero, ModContent.ProjectileType<AcidSpawningVortex>(), 400, 0f, Main.myPlayer, vortexSpawnPosition.X, vortexSpawnPosition.Y);
                            Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/OldDukeRoar"), npc.Center);
                        }

                        npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * spinSpeed;

                        frameDrawType = (int)OldDukeFrameDrawingType.OpenMouth;

                        float spinArcPerFrame = totalSpins * MathHelper.TwoPi / spinTime;
                        npc.velocity = npc.velocity.RotatedBy(spinArcPerFrame);
                        npc.rotation += spinArcPerFrame;

                        // Charge again based on the spin momentum.
                        if (aiTimer >= teleportDelay + chargeDelay + chargeTime + spinTime / 2f &&
                            Vector2.Dot(npc.velocity.SafeNormalize(Vector2.Zero), npc.DirectionTo(target.Center)) > 0.93f)
                        {
                            aiTimer = teleportDelay + chargeDelay + chargeTime + spinTime;
                        }
                    }

                    if (aiTimer == teleportDelay + chargeDelay + chargeTime + spinTime + secondaryChargeTime)
                        goToNextAIState();
                    break;

                case OldDukeAttackType.SideSharkSummon:
                    rotationCorrectionTime = 60;
                    totalSharksToSummon = enraged ? 3 : 2;
                    roarTime = 20;
                    slowdownTime = 225;

                    frameDrawType = (int)OldDukeFrameDrawingType.FinFlapping;

                    if (aiTimer <= rotationCorrectionTime)
                    {
                        npc.velocity *= 0.93f;
                        npc.rotation = npc.rotation.AngleTowards(0f, 0.2f);
                    }
                    if (aiTimer == rotationCorrectionTime)
                    {
                        spawnSideSharks();
                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/OldDukeRoar"), npc.Center);
                    }
                    
                    if (aiTimer >= rotationCorrectionTime &&
                        aiTimer <= rotationCorrectionTime + roarTime)
                    {
                        frameDrawType = (int)OldDukeFrameDrawingType.OpenMouth;
                    }

                    if (aiTimer >= rotationCorrectionTime + roarTime + slowdownTime)
                        goToNextAIState();

                    break;
            }

            aiTimer++;
            return false;
        }
        #endregion

        #region Frames and Drawcode

        [OverrideAppliesTo("OldDuke", typeof(OldDukeAIClass), "OldDukeFindFrame", EntityOverrideContext.NPCFindFrame)]
        public static void OldDukeFindFrame(NPC npc, int frameHeight)
        {
            OldDukeFrameDrawingType frameDrawType = (OldDukeFrameDrawingType)(int)npc.ai[3];
            switch (frameDrawType)
            {
                case OldDukeFrameDrawingType.FinFlapping:
                    int frame = (int)(npc.frameCounter / 7) % 6;
                    npc.frame.Y = frame * frameHeight;
                    break;
                case OldDukeFrameDrawingType.IdleFins:
                    npc.frame.Y = 0;
                    break;
                case OldDukeFrameDrawingType.OpenMouth:
                    npc.frame.Y = 6 * frameHeight;
                    break;
            }
            npc.frameCounter++;
        }

        [OverrideAppliesTo("OldDuke", typeof(OldDukeAIClass), "OldDukePreDraw", EntityOverrideContext.NPCPreDraw)]
        public static bool OldDukePreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D oldDukeTexture = Main.npcTexture[npc.type];
            Vector2 origin = oldDukeTexture.Size() / new Vector2(1f, Main.npcFrameCount[npc.type]) / 2f;
            void drawOldDukeInstance(Color color, Vector2 drawPosition, int direction)
            {
                SpriteEffects spriteEffects = direction == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                spriteBatch.Draw(oldDukeTexture, drawPosition - Main.screenPosition, npc.frame, color, npc.rotation, origin, npc.scale, spriteEffects, 0f);
            }

            OldDukeAttackType currentAIState = (OldDukeAttackType)(int)npc.ai[0];

            if (currentAIState == OldDukeAttackType.Charge && npc.velocity.Length() > 20f)
            {
                Color baseColor = npc.GetAlpha(Color.Lerp(lightColor, Color.Lime, 0.5f));
                for (int i = 1; i < npc.oldPos.Length; i += 2)
                {
                    drawOldDukeInstance(baseColor * (1f - i / (float)npc.oldPos.Length), npc.oldPos[i] + npc.Size * 0.5f, npc.spriteDirection);
                }
            }

            drawOldDukeInstance(lightColor, npc.Center, npc.spriteDirection);
            return false;
        }
        #endregion
    }
}
