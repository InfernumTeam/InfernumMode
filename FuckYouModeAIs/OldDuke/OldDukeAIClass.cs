using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Items.Tools.ClimateChange;
using CalamityMod.NPCs.OldDuke;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.Shaders;
using Terraria.Graphics.Effects;
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
            DisgustingBelch,
            AcidicMonsoonWave,
            TeleportCharge
        }

        public enum OldDukeFrameDrawingType
        {
            FinFlapping,
            IdleFins,
            OpenMouth
        }
		#endregion

		#region Pattern Lists
		public static readonly OldDukeAttackType[] Phase1Pattern = new OldDukeAttackType[]
        {
            OldDukeAttackType.AcidVortexSpin,
            OldDukeAttackType.Charge,
            OldDukeAttackType.Charge,
            OldDukeAttackType.Charge,
            OldDukeAttackType.Charge,
            OldDukeAttackType.SideSharkSummon,
            OldDukeAttackType.Charge,
            OldDukeAttackType.Charge,
            OldDukeAttackType.Charge,
            OldDukeAttackType.Charge,
            OldDukeAttackType.SharkSummon,
            OldDukeAttackType.Charge,
            OldDukeAttackType.Charge,
            OldDukeAttackType.Charge,
            OldDukeAttackType.Charge,
            OldDukeAttackType.UpwardSpin,
            OldDukeAttackType.AcidVortexSpin,
            OldDukeAttackType.SideSharkSummon,
            OldDukeAttackType.Charge,
            OldDukeAttackType.Charge,
            OldDukeAttackType.Charge,
            OldDukeAttackType.Charge,
            OldDukeAttackType.UpwardSpin,
            OldDukeAttackType.Charge,
            OldDukeAttackType.Charge,
            OldDukeAttackType.Charge,
            OldDukeAttackType.Charge,
            OldDukeAttackType.SideSharkSummon,
            OldDukeAttackType.Charge,
            OldDukeAttackType.Charge,
            OldDukeAttackType.Charge,
            OldDukeAttackType.Charge,
        };

        public static readonly OldDukeAttackType[] Phase2Pattern = new OldDukeAttackType[]
        {
            OldDukeAttackType.SideSharkSummon,
            OldDukeAttackType.Charge,
            OldDukeAttackType.Charge,
            OldDukeAttackType.Charge,
            OldDukeAttackType.Charge,
            OldDukeAttackType.UpwardSpin,
            OldDukeAttackType.TeleportCharge,
            OldDukeAttackType.TeleportCharge,
            OldDukeAttackType.TeleportCharge,
            OldDukeAttackType.AcidVortexSpin,
            OldDukeAttackType.AcidicMonsoonWave,
            OldDukeAttackType.DisgustingBelch,
            OldDukeAttackType.Charge,
            OldDukeAttackType.Charge,
            OldDukeAttackType.Charge,
            OldDukeAttackType.Charge,
            OldDukeAttackType.SharkSummon,
            OldDukeAttackType.TeleportCharge,
            OldDukeAttackType.TeleportCharge,
            OldDukeAttackType.TeleportCharge,
            OldDukeAttackType.DisgustingBelch,
            OldDukeAttackType.AcidicMonsoonWave,
            OldDukeAttackType.AcidicMonsoonWave,
            OldDukeAttackType.Charge,
            OldDukeAttackType.Charge,
            OldDukeAttackType.Charge,
            OldDukeAttackType.Charge,
            OldDukeAttackType.AcidVortexSpin,
            OldDukeAttackType.TeleportCharge,
            OldDukeAttackType.TeleportCharge,
            OldDukeAttackType.TeleportCharge,
        };

        public static readonly Dictionary<OldDukeAttackType[], Func<NPC, bool>> SubphaseTable = new Dictionary<OldDukeAttackType[], Func<NPC, bool>>()
        {
            [Phase1Pattern] = (npc) => npc.life / (float)npc.lifeMax > Phase2LifeRatio,
            [Phase2Pattern] = (npc) => npc.life / (float)npc.lifeMax > 0f,
        };

        public const float Phase2LifeRatio = 0.6f;
        public const float Phase3LifeRatio = 0.25f;
        #endregion

        #region AI
        [OverrideAppliesTo("OldDuke", typeof(OldDukeAIClass), "OldDukeAI", EntityOverrideContext.NPCAI)]
        public static bool OldDukeAI(NPC npc)
        {
            Player target = Main.player[npc.target];

            npc.Calamity().DR = 0.2f;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float aiState = ref npc.ai[0];
            ref float aiStateIndex = ref npc.ai[1];
            ref float attackTimer = ref npc.ai[2];
            ref float frameDrawType = ref npc.ai[3];
            ref float transitionCountdown = ref npc.Infernum().ExtraAI[5];
            ref float phase2Flag = ref npc.Infernum().ExtraAI[6];
            ref float phase3TransitionCountdown = ref npc.Infernum().ExtraAI[7];
            ref float phase3Flag = ref npc.Infernum().ExtraAI[8];
            ref float despawnTimer = ref npc.Infernum().ExtraAI[9];

            bool enraged = target.position.Y < 300f || target.position.Y > Main.worldSurface * 16.0 ||
                           target.position.X > 8000f && target.position.X < (Main.maxTilesX * 16 - 8000);
            bool inPhase2 = phase2Flag == 1f && transitionCountdown < 35f;
            bool inPhase3 = phase3Flag == 1f && phase3TransitionCountdown < 35f;

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

                Main.npc[shark].ai[1] = inPhase2.ToInt();
                Main.npc[shark].netUpdate = true;

                shark = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<BigOldDukeSharkron>());
                Main.npc[shark].velocity = directionToTarget.RotatedBy(-MathHelper.PiOver2) * 10f;
                Main.npc[shark].rotation = Main.npc[shark].velocity.ToRotation();
                Main.npc[shark].spriteDirection = (Math.Cos(Main.npc[shark].rotation) > 0).ToDirectionInt();
                if (Main.npc[shark].spriteDirection == -1)
                    Main.npc[shark].rotation += MathHelper.Pi;

                Main.npc[shark].ai[1] = inPhase2.ToInt();
                Main.npc[shark].netUpdate = true;
            }

            void GotoNextAIState()
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
                npc.Opacity = 1f;

                // And the misc ai slots.
                for (int i = 0; i < 5; i++)
                    npc.Infernum().ExtraAI[i] = 0f;
            }

            if (!target.active || target.dead)
            {
                npc.TargetClosest();
                target = Main.player[npc.target];
                if (!target.active || target.dead)
                    despawnTimer++;
            }
            else
            {
                if (despawnTimer > 0f)
                {
                    npc.Opacity = 1f;
                    despawnTimer = 0f;
                }
            }

            if (despawnTimer > 0f)
            {
                npc.rotation = npc.rotation.AngleLerp(0f, 0.3f);
                npc.velocity *= 0.92f;

                npc.Opacity = Utils.InverseLerp(45f, 0f, despawnTimer, true);
                if (npc.Opacity <= 0f)
                {
                    npc.active = false;
                    npc.netUpdate = true;
                }
                return false;
            }

            // Transition stuff.
            if (transitionCountdown > 0f)
			{
                npc.velocity *= 0.96f;
                npc.rotation = npc.rotation.AngleTowards(0f, 0.2f);

                if (transitionCountdown > 35f || transitionCountdown <= 2f)
                    frameDrawType = (int)OldDukeFrameDrawingType.FinFlapping;
				else
				{
                    frameDrawType = (int)OldDukeFrameDrawingType.OpenMouth;
                    if (transitionCountdown == 32f)
                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/OldDukeRoar"), npc.Center);
                }

                transitionCountdown--;
                if (transitionCountdown == 0f)
                    GotoNextAIState();
                return false;
            }

            if (phase3TransitionCountdown > 0f)
            {
                npc.velocity *= 0.96f;
                npc.rotation = npc.rotation.AngleTowards(0f, 0.2f);

                if (transitionCountdown > 35f || transitionCountdown <= 2f)
                    frameDrawType = (int)OldDukeFrameDrawingType.FinFlapping;
                else
                {
                    frameDrawType = (int)OldDukeFrameDrawingType.OpenMouth;
                    if (transitionCountdown == 32f)
                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/OldDukeRoar"), npc.Center);
                }

                phase3TransitionCountdown--;
                if (phase3TransitionCountdown == 0f)
                    GotoNextAIState();
                return false;
            }

            // Enforce transition triggers.
            if (lifeRatio < Phase2LifeRatio && phase2Flag == 0f)
            {
                phase2Flag = 1f;
                transitionCountdown = 120f;
                npc.netUpdate = true;
                return false;
            }

            if (lifeRatio < Phase3LifeRatio && phase3Flag == 0f)
            {
                phase3Flag = 1f;
                phase3TransitionCountdown = 120f;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Main.rainTime = 300;
                    Main.raining = true;
                    TorrentialTear.AdjustRainSeverity(true);
                    CalamityNetcode.SyncWorld();
                }

                npc.netUpdate = true;
                return false;
            }

            if (inPhase3)
            {
                Main.rainTime = 150;
                Main.windSpeed = Main.windSpeedTemp = 1.045f;
                Main.cloudBGActive = 1.5f;
                Main.maxRaining = 0.9f;
            }
            else
                CalamityMod.CalamityMod.StopRain();

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

                        diveDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        npc.spriteDirection = (int)diveDirection;
                        npc.velocity = new Vector2(diveDirection * -16f, -16);

                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/OldDukeRoar"), npc.Center);
                    }

                    // Fly upward and dive until we're nearly only falling downward.
                    if (Vector2.Dot(npc.velocity.SafeNormalize(Vector2.Zero), Vector2.UnitY) < 0.95f || diveTimer <= diveTime)
                    {
                        attackTimer = 0f;

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
                        if (attackTimer == 2f && Main.netMode != NetmodeID.MultiplayerClient)
                            spawnSideSharks();

                        npc.velocity *= 0.96f;

                        npc.rotation = npc.AngleTo(target.Center);
                        npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                        if (npc.spriteDirection == 1)
                            npc.rotation += MathHelper.Pi;

                        if (attackTimer % 45 == 44)
                            Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/OldDukeHuff"), npc.Center);

                        if (attackTimer >= slowdownTime)
                        {
                            // This is done to ensure that the attack starts at the 0 index instead of 1.
                            aiStateIndex = -1f;
                            GotoNextAIState();
                        }
                    }

                    break;

                case OldDukeAttackType.Charge:
                    int reelbackDelay = 34;
                    int chargeTime = enraged ? 32 : 48;
                    float reelbackSpeed = enraged ? 12f : 7f;
                    float chargeSpeed = enraged ? 34f : 25.5f;
                    float chargeAcceleration = enraged ? 1.015f : 1.01f;
                    if (inPhase2)
                    {
                        chargeSpeed *= 1.2f;
						reelbackSpeed *= 1.1f;
                        reelbackDelay -= 10;
                    }

                    if (inPhase3)
                    {
                        chargeSpeed *= 1.15f;
                        reelbackSpeed *= 1.1f;
                        reelbackDelay -= 5;
                    }

                    // Reel back for a bit.
                    if (attackTimer < reelbackDelay)
                    {
                        frameDrawType = (int)OldDukeFrameDrawingType.FinFlapping;

                        npc.velocity = Vector2.Lerp(npc.velocity, npc.DirectionTo(target.Center) * -reelbackSpeed, 0.35f);
                        npc.rotation = npc.AngleTo(target.Center + target.velocity * 21f);
                        npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                        if (npc.spriteDirection == 1)
                            npc.rotation += MathHelper.Pi;

                        if (npc.Distance(target.Center) > 1400f)
                            attackTimer = reelbackDelay;
                    }

                    // And then charge at the player.
                    if (attackTimer == reelbackDelay)
                    {
                        frameDrawType = (int)OldDukeFrameDrawingType.FinFlapping;

                        float aimAhead = target.WithinRange(npc.Center, 400f) ? 35f : 21f; 
                        npc.velocity = npc.DirectionTo(target.Center + target.velocity * aimAhead) * chargeSpeed;
                        npc.rotation = npc.velocity.ToRotation();
                        npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                        if (npc.spriteDirection == 1)
                            npc.rotation += MathHelper.Pi;
                    }

                    if (attackTimer > reelbackDelay)
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

                    if (attackTimer >= reelbackDelay + chargeTime)
                        GotoNextAIState();

                    break;
                case OldDukeAttackType.SharkSummon:
                    int rotationCorrectionTime = 60;
                    int totalSharksToSummon = enraged ? 6 : 4;
                    int sharkSummonRate = 15;
                    int roarTime = 20;

                    frameDrawType = (int)OldDukeFrameDrawingType.FinFlapping;

                    if (attackTimer <= rotationCorrectionTime)
                    {
                        npc.velocity *= 0.93f;
                        npc.rotation = npc.rotation.AngleTowards(0f, 0.2f);
                    }
                    if (attackTimer > rotationCorrectionTime)
                    {
                        if (attackTimer < rotationCorrectionTime + roarTime)
                        {
                            if (attackTimer == rotationCorrectionTime + 2f)
                                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/OldDukeVomit"), target.Center);

                            frameDrawType = (int)OldDukeFrameDrawingType.OpenMouth;
                        }
                        if (attackTimer % sharkSummonRate == sharkSummonRate - 1)
                        {
                            int sharkCounter = (int)((attackTimer - rotationCorrectionTime) / sharkSummonRate);
                            int ySummonOffset = sharkCounter * 100;
                            NPC.NewNPC((int)npc.Center.X + 360 + sharkCounter * 60, (int)npc.Center.Y - ySummonOffset, ModContent.NPCType<OldDukeSharkron>(), 0, 0f, 0f, npc.whoAmI, 0f, 255);
                            NPC.NewNPC((int)npc.Center.X - 360 - sharkCounter * 60, (int)npc.Center.Y - ySummonOffset, ModContent.NPCType<OldDukeSharkron>(), 0, 0f, 0f, npc.whoAmI, 0f, 255);
                        }
                    }

                    if (attackTimer > rotationCorrectionTime + sharkSummonRate * totalSharksToSummon)
                        GotoNextAIState();

                    break;
                case OldDukeAttackType.UpwardSpin:
                    int riseTime = 120;
                    int redirectTime = 15;
                    chargeSpeed = 20f;
                    int delayPerSpin = 30;
                    int spinTime = 45;
                    float spinSpeed = 24f;
                    float spinArcs = 1f;
                    float acidSpeed = 14f;
                    int totalSpins = 2;
                    int acidShootRate = 4;
                    if (inPhase2)
					{
                        chargeSpeed *= 1.15f;
                        delayPerSpin -= 8;
                        spinTime = 60;
                        acidShootRate = 3;
					}
                    if (inPhase3)
                    {
                        acidSpeed *= 1.3f;
                        chargeSpeed *= 1.15f;
                        delayPerSpin -= 5;
                        acidShootRate = 2;
                    }

                    if (attackTimer < riseTime)
                    {
                        Vector2 destination = target.Center - new Vector2(1000f, 420f);
                        npc.velocity = npc.DirectionTo(destination) * 22f - npc.SafeDirectionTo(target.Center) * 8f;
                        float idealAngle = npc.AngleTo(target.Center);

                        if (npc.spriteDirection == 1)
                            idealAngle += MathHelper.Pi;
                        npc.rotation = idealAngle;

                        if (npc.Distance(destination) < 50f)
                            attackTimer = riseTime;

                        frameDrawType = (int)OldDukeFrameDrawingType.FinFlapping;
                    }
                    
                    if (attackTimer >= riseTime && attackTimer <= riseTime + redirectTime)
                    {
                        npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitX * 23f, 0.19f);
                        npc.rotation = npc.velocity.ToRotation();
                    }

                    if (attackTimer >= riseTime + redirectTime)
                    {
                        float timeRelativeToSpin = (attackTimer - riseTime - redirectTime) % (delayPerSpin + spinTime);

                        if (attackTimer == riseTime + redirectTime + 1f)
                            Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/OldDukeRoar"), target.Center);

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
                                Vector2 acidVelocity = npc.velocity.SafeNormalize(Vector2.UnitX * npc.spriteDirection) * acidSpeed + npc.velocity / 3f;
                                int acid = Utilities.NewProjectileBetter(mouthPosition, acidVelocity, ModContent.ProjectileType<HomingAcid>(), 300, 0f);
                                if (Main.projectile.IndexInRange(acid))
                                    Main.projectile[acid].ai[1] = acidSpeed;
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

                    if (attackTimer >= riseTime + redirectTime + (delayPerSpin + spinTime) * totalSpins)
                        GotoNextAIState();
                    break;
                case OldDukeAttackType.AcidVortexSpin:
                    int teleportDelay = 30;
                    int chargeDelay = 20;
                    chargeSpeed = 24f;
                    chargeTime = 45;

                    totalSpins = 2;
                    spinTime = 120;
                    spinSpeed = 35f;

                    int secondaryChargeTime = 45;
                    
                    if (inPhase2)
					{
                        chargeSpeed *= 1.2f;
                        chargeTime -= 10;
                        secondaryChargeTime -= 10;
					}

                    frameDrawType = (int)OldDukeFrameDrawingType.FinFlapping;

                    // Slowdown before initial teleport. Also roar as a telegraph.
                    if (attackTimer < teleportDelay)
                    {
                        npc.velocity *= 0.93f;

                        if (attackTimer == 5f)
                            Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/OldDukeRoar"), npc.Center);
                    }

                    // Teleport,
                    if (attackTimer == teleportDelay)
                    {
                        npc.Center = target.Center + new Vector2(-target.direction * 620f, 410f);
                        npc.rotation = npc.AngleTo(target.Center);
                        npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                        if (npc.spriteDirection == 1)
                            npc.rotation += MathHelper.Pi;

                        npc.netUpdate = true;
                    }

                    // Charge,
                    if (attackTimer == teleportDelay + chargeDelay)
                    {
                        npc.velocity = npc.DirectionTo(target.Center) * chargeSpeed;
                        npc.rotation = npc.velocity.ToRotation();
                        npc.direction = (target.Center.X > npc.Center.X).ToDirectionInt();
                        if (npc.spriteDirection == 1)
                            npc.rotation += MathHelper.Pi;
                    }

                    // And spin.
                    if (attackTimer >= teleportDelay + chargeDelay + chargeTime &&
                        attackTimer < teleportDelay + chargeDelay + chargeTime + spinTime)
                    {
                        if (npc.WithinRange(target.Center, 600f) && attackTimer < teleportDelay + chargeDelay + chargeTime + 1)
                        {
                            attackTimer = teleportDelay + chargeDelay + chargeTime - 10f;
                            return false;
                        }

                        npc.spriteDirection = -1;

                        if (attackTimer == teleportDelay + chargeDelay + chargeTime + 1 && Main.netMode != NetmodeID.MultiplayerClient)
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
                        if (attackTimer >= teleportDelay + chargeDelay + chargeTime + spinTime / 2f &&
                            Vector2.Dot(npc.velocity.SafeNormalize(Vector2.Zero), npc.DirectionTo(target.Center)) > 0.93f)
                        {
                            attackTimer = teleportDelay + chargeDelay + chargeTime + spinTime;
                            npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * chargeSpeed;

                            npc.rotation = npc.velocity.ToRotation();
                            npc.direction = (target.Center.X > npc.Center.X).ToDirectionInt();
                            if (npc.spriteDirection == 1)
                                npc.rotation += MathHelper.Pi;
                        }
                    }

                    if (attackTimer == teleportDelay + chargeDelay + chargeTime + spinTime + secondaryChargeTime)
                        GotoNextAIState();
                    break;

                case OldDukeAttackType.SideSharkSummon:
                    rotationCorrectionTime = 60;
                    totalSharksToSummon = enraged ? 3 : 2;
                    roarTime = 20;
                    slowdownTime = 225;

                    frameDrawType = (int)OldDukeFrameDrawingType.FinFlapping;

                    if (attackTimer <= rotationCorrectionTime)
                    {
                        npc.velocity *= 0.93f;
                        npc.rotation = npc.rotation.AngleTowards(0f, 0.2f);
                    }
                    if (attackTimer == rotationCorrectionTime)
                    {
                        npc.velocity = Vector2.Zero;
                        spawnSideSharks();
                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/OldDukeRoar"), npc.Center);
                        npc.netUpdate = true;
                    }
                    
                    if (attackTimer >= rotationCorrectionTime &&
                        attackTimer <= rotationCorrectionTime + roarTime)
                    {
                        frameDrawType = (int)OldDukeFrameDrawingType.OpenMouth;
                    }

                    if (attackTimer >= rotationCorrectionTime + roarTime + slowdownTime)
                        GotoNextAIState();
                    break;
                case OldDukeAttackType.DisgustingBelch:
                    int hoverTime = 60;
                    int rotationRedirectTime = 25;
                    int attackDelay = 90;
                    float idealRotation = MathHelper.Pi / 4f * npc.spriteDirection;
                    int toothBallCount = 3;
                    int goreCount = 16;

                    if (inPhase3)
                    {
                        toothBallCount = 5;
                        goreCount = 30;
                    }

                    if (attackTimer < hoverTime)
                    {
                        npc.velocity *= 0.97f;
                        npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, 0f, 0.03f);
                        npc.rotation = npc.rotation.AngleTowards(0f, 0.02f);
                        frameDrawType = (int)OldDukeFrameDrawingType.FinFlapping;
                    }
                    else if (attackTimer < hoverTime + rotationRedirectTime)
                    {
                        frameDrawType = (int)OldDukeFrameDrawingType.OpenMouth;
                        npc.velocity *= 0.9f;
                        npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.05f);
                    }

                    if (attackTimer == hoverTime + rotationRedirectTime)
					{
                        // Vomit a bunch of tooth balls and shark gore.
                        if (Main.netMode != NetmodeID.MultiplayerClient)
						{
                            Vector2 mouthDirection = npc.SafeDirectionTo(mouthPosition);
                            for (int i = 0; i < toothBallCount; i++)
                            {
								int toothBall = NPC.NewNPC((int)mouthPosition.X, (int)mouthPosition.Y, ModContent.NPCType<OldDukeToothBall>());
                                Main.npc[toothBall].velocity = mouthDirection.RotatedByRandom(0.3f) * Main.rand.NextFloat(9f, 12f);
                            }

                            for (int i = 0; i < goreCount; i++)
                            {
                                Vector2 goreVelocity = new Vector2(-npc.spriteDirection * Main.rand.NextFloat(4f, 9f), -Main.rand.NextFloat(5f, 13f));
                                Utilities.NewProjectileBetter(mouthPosition, goreVelocity, ModContent.ProjectileType<OldDukeGore>(), 270, 0f);
                            }
                        }
                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/OldDukeVomit"), npc.Center);
                    }

                    if (attackTimer > hoverTime + rotationRedirectTime + 15)
                        frameDrawType = (int)OldDukeFrameDrawingType.FinFlapping;

                    if (attackTimer > hoverTime + rotationRedirectTime + attackDelay)
                        GotoNextAIState();
                    break;
                case OldDukeAttackType.AcidicMonsoonWave:
                    redirectTime = 65;
                    float lungeSpeed = enraged ? 31f : 23f;
                    float waveSpeed = enraged ? 20f : 13f;
                    int lungeMaxTime = 180;

                    if (inPhase3)
                    {
                        lungeSpeed *= 1.4f;
                        waveSpeed *= 0.85f;
                    }

                    frameDrawType = (int)OldDukeFrameDrawingType.FinFlapping;
                    if (attackTimer < redirectTime)
                    {
                        Vector2 destination = target.Center - Vector2.UnitY.RotatedBy(target.velocity.X / 20f * MathHelper.ToRadians(26f)) * 520f;
                        npc.SimpleFlyMovement(npc.DirectionTo(destination) * 23f, 0.75f);
                        float idealAngle = npc.AngleTo(target.Center);
                        npc.spriteDirection = (npc.Center.X > target.Center.X).ToDirectionInt();

                        if (npc.spriteDirection == 1)
                            idealAngle += MathHelper.Pi;
                        npc.rotation = idealAngle;
                    }
                    if (attackTimer == redirectTime)
                    {
                        // Roar.
                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/OldDukeVomit"), npc.Center);

                        npc.velocity = npc.DirectionTo(target.Center) * lungeSpeed;
                        npc.velocity.Y = Math.Abs(npc.velocity.Y);
                        npc.netUpdate = true;
                    }

                    if (attackTimer > redirectTime)
                    {
                        float idealAngle = npc.velocity.ToRotation();
                        npc.spriteDirection = (npc.Center.X > target.Center.X).ToDirectionInt();

                        if (npc.spriteDirection == 1)
                            idealAngle += MathHelper.Pi;
                        npc.rotation = idealAngle;

                        frameDrawType = (int)OldDukeFrameDrawingType.FinFlapping;
                        if (Collision.SolidCollision(npc.position, npc.width, npc.width) ||
                            Collision.WetCollision(npc.position, npc.width, npc.width) ||
                            attackTimer >= redirectTime + lungeMaxTime)
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                for (int i = -1; i <= 1; i += 2)
                                {
                                    int wave = Utilities.NewProjectileBetter(npc.Center, Vector2.UnitX * waveSpeed * i, ModContent.ProjectileType<AcidicTidalWave>(), 360, 0f);
                                    Main.projectile[wave].Bottom = npc.Center + Vector2.UnitY * 700f;
                                }
                            }

                            // Very heavily disturb water.
                            if (Main.netMode != NetmodeID.Server)
                            {
                                WaterShaderData ripple = (WaterShaderData)Filters.Scene["WaterDistortion"].GetShader();
                                float waveSine = 0.1f * (float)Math.Sin(Main.GlobalTime * 20f);
                                Vector2 ripplePos = npc.Center + npc.velocity * 7f;
                                Color waveData = new Color(0.5f, 0.1f * Math.Sign(waveSine) + 0.5f, 0f, 1f) * Math.Abs(waveSine);
                                ripple.QueueRipple(ripplePos, waveData, Vector2.One * 860f, RippleShape.Circle, npc.rotation);
                            }
                            npc.velocity *= -0.5f;
                            GotoNextAIState();
                        }
                    }
                    break;
                case OldDukeAttackType.TeleportCharge:
                    int teleportFadeinTime = 20;
                    int teleportFadeoutTime = 24;
                    chargeSpeed = enraged ? 39f : 29f;
                    chargeTime = 32;
                    slowdownTime = 12;

                    if (inPhase3)
                    {
                        chargeSpeed *= 1.2f;
                        chargeTime -= 6;
                        teleportFadeinTime -= 4;
                        teleportFadeoutTime -= 2;
                    }

                    frameDrawType = (int)OldDukeFrameDrawingType.FinFlapping;

                    if (attackTimer < teleportFadeinTime)
                    {
                        npc.velocity *= 0.97f;
                        npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, 0f, 0.04f);
                        npc.Opacity = Utils.InverseLerp(teleportFadeinTime - 1f, 0f, attackTimer, true);
                    }

                    if (attackTimer == teleportFadeinTime)
                    {
                        npc.Center = target.Center + new Vector2(Math.Sign((npc.Center - target.Center).X) * 550f, -270f);
                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/OldDukeRoar"), npc.Center);
                        npc.netUpdate = true;
                    }

                    if (attackTimer > teleportFadeinTime && attackTimer < teleportFadeinTime + teleportFadeoutTime)
                    {
                        Vector2 hoverDestination = target.Center + new Vector2(Math.Sign((npc.Center - target.Center).X) * 550f, -270f) - npc.velocity;
                        npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 20f, 3f);

                        npc.rotation = npc.AngleTo(target.Center + target.velocity * 15f);
                        npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                        if (npc.spriteDirection == 1)
                            npc.rotation += MathHelper.Pi;

                        npc.Opacity = Utils.InverseLerp(teleportFadeinTime, teleportFadeinTime + teleportFadeoutTime - 1, attackTimer, true);
                    }

                    if (attackTimer == teleportFadeinTime + teleportFadeoutTime)
                    {
                        npc.velocity = npc.SafeDirectionTo(target.Center + target.velocity * 15f) * chargeSpeed;
                        npc.rotation = npc.velocity.ToRotation();
                        npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                        if (npc.spriteDirection == 1)
                            npc.rotation += MathHelper.Pi;
                        npc.netUpdate = true;
                    }

                    if (attackTimer > teleportFadeinTime + teleportFadeoutTime && attackTimer < teleportFadeinTime + teleportFadeoutTime + chargeTime)
                        npc.velocity *= 1.018f;

                    if (attackTimer > teleportFadeinTime + teleportFadeoutTime + chargeTime)
                    {
                        npc.velocity *= 0.97f;
                        npc.velocity = Vector2.Lerp(npc.velocity, Vector2.Zero, 0.03f);
                    }

                    if (attackTimer > teleportFadeinTime + teleportFadeoutTime + chargeTime + slowdownTime)
                        GotoNextAIState();
                    break;
            }

            attackTimer++;
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
            Texture2D oldDukeEyeTexture = ModContent.GetTexture("CalamityMod/NPCs/OldDuke/OldDukeGlow");
            Color eyeColor = Color.Lerp(Color.White, Color.Yellow, 0.5f);
            Vector2 origin = oldDukeTexture.Size() / new Vector2(1f, Main.npcFrameCount[npc.type]) / 2f;
            float transitionCountdown = npc.Infernum().ExtraAI[5];
            if (npc.Infernum().ExtraAI[7] > 0f)
                transitionCountdown = npc.Infernum().ExtraAI[7];

            float transitionTimer = 120f - transitionCountdown;
            bool inPhase2 = npc.Infernum().ExtraAI[6] == 1f && transitionCountdown <= 35f;
            bool inPhase3 = npc.Infernum().ExtraAI[8] == 1f && npc.Infernum().ExtraAI[7] <= 35f;

            void drawOldDukeInstance(Color color, Vector2 drawPosition, int direction)
            {
                SpriteEffects spriteEffects = direction == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

                int afterimageCount = transitionCountdown > 0f ? 6 : 0;
                float afterimageFade = (1f - (float)Math.Cos(Utils.InverseLerp(60f, 110f, transitionTimer, true) * MathHelper.TwoPi)) * 0.333f;
                for (int i = 0; i < afterimageCount; i++)
                {
                    Color afterimageColor = lightColor * (1f - afterimageFade) * npc.Opacity * 0.5f;
                    Vector2 afterimageDrawRotationalOffset = (i / (float)afterimageCount * MathHelper.TwoPi + npc.rotation).ToRotationVector2() * afterimageFade * 30f;
                    Vector2 afterimageDrawPosition = npc.Center + afterimageDrawRotationalOffset - Main.screenPosition;
                    spriteBatch.Draw(oldDukeTexture, afterimageDrawPosition, npc.frame, afterimageColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, spriteEffects, 0f);
                }
                spriteBatch.Draw(oldDukeTexture, drawPosition - Main.screenPosition, npc.frame, color, npc.rotation, origin, npc.scale, spriteEffects, 0f);
                if (inPhase2)
                    spriteBatch.Draw(oldDukeEyeTexture, drawPosition - Main.screenPosition, npc.frame, eyeColor, npc.rotation, origin, npc.scale, spriteEffects, 0f);
            }

            OldDukeAttackType currentAIState = (OldDukeAttackType)(int)npc.ai[0];

            // Turn green in phase 3.
            if (npc.Infernum().ExtraAI[7] > 0f)
                lightColor = Color.Lerp(lightColor, Color.Lime, Utils.InverseLerp(70f, 30f, transitionTimer, true) * 0.5f);
            else if (inPhase3)
                lightColor = Color.Lerp(lightColor, Color.Lime, 0.5f);

            if ((currentAIState == OldDukeAttackType.Charge || currentAIState == OldDukeAttackType.TeleportCharge) && npc.velocity.Length() > 20f)
            {
                Color baseColor = Color.Lerp(lightColor, Color.Lime, 0.5f) * npc.Opacity;
                for (int i = 1; i < npc.oldPos.Length; i += 2)
                    drawOldDukeInstance(baseColor * (1f - i / (float)npc.oldPos.Length), npc.oldPos[i] + npc.Size * 0.5f, npc.spriteDirection);
            }

            drawOldDukeInstance(lightColor * npc.Opacity, npc.Center, npc.spriteDirection);
            return false;
        }
        #endregion
    }
}
